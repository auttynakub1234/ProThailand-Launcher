#!/usr/bin/env python3
"""
╔═══════════════════════════════════════════════════════════════════════╗
║             PROTHAILAND LFS 0.6V MASTER SERVER (CLI)                  ║
║  Local Master Server & Bridge for Live for Speed (LFS 0.6V)           ║
║  Connects to ProThailand Cloudflare Worker & KV Database              ║
║  Supports Scrutineering (Unlock), JRA (Join Auth), and Game Servers   ║
╚═══════════════════════════════════════════════════════════════════════╝
"""

import sys
import os
import socket
import select
import threading
import struct
import json
import urllib.request
import urllib.parse
import time
from datetime import datetime

DEFAULT_PORT = 29339
DEFAULT_HOST = "0.0.0.0" # Binds both 127.0.0.1 and local network
WORKER_AUTH_URL = "https://prothailand-master-api.fixflxw24.workers.dev/api/master/auth"
WORKER_USER_URL = "https://prothailand-master-api.fixflxw24.workers.dev/api/user"

# Active hosts registered via usemaster=yes
ACTIVE_SERVERS = {}

def get_timestamp():
    return datetime.now().strftime("%H:%M:%S")

def log(msg, tag="INFO"):
    colors = {
        "INFO": "\033[94m",    # Blue
        "SUCCESS": "\033[92m", # Green
        "WARN": "\033[93m",    # Yellow
        "ERROR": "\033[91m",   # Red
        "AUTH": "\033[95m",    # Magenta
        "RESET": "\033[0m"
    }
    c = colors.get(tag, "")
    r = colors["RESET"]
    print(f"[{get_timestamp()}] {c}[{tag}]{r} {msg}", flush=True)

def verify_user_with_web(username: str) -> dict:
    """
    Checks user with ProThailand Cloudflare Worker API.
    Returns dict: {'authorized': bool, 'license': str, 'rank': str}
    """
    if not username:
        return {"authorized": True, "license": "S3", "rank": "Driver"}

    clean_user = username.strip().lower()
    headers = {
        "User-Agent": "ProThailand-MasterServer-CLI/1.0",
        "Accept": "application/json"
    }

    try:
        url = f"{WORKER_AUTH_URL}?name={urllib.parse.quote(clean_user)}"
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req, timeout=3.5) as resp:
            data = json.loads(resp.read().decode("utf-8"))
            if data.get("success") or data.get("authorized"):
                log(f"User '{clean_user}' verified via Cloudflare Edge KV! Rank: {data.get('rank', 'Driver')}", "AUTH")
                return {
                    "authorized": True,
                    "license": data.get("license", "S3"),
                    "rank": data.get("rank", "Driver")
                }
    except Exception as e:
        log(f"Worker API check notice: {e} (Using fallback permit)", "WARN")

    # Fallback: allow players if username is valid so offline / local play is never blocked
    return {"authorized": True, "license": "S3", "rank": "Driver"}

class LFSTcpClientHandler(threading.Thread):
    def __init__(self, conn, addr):
        super().__init__()
        self.conn = conn
        self.addr = addr
        self.daemon = True
        self.active_user = "Pilot01"

    def run(self):
        client_ip, client_port = self.addr
        log(f"New connection from {client_ip}:{client_port}", "INFO")

        try:
            while True:
                # 1. Read size byte (1-byte length prefix)
                raw_size = self.conn.recv(1)
                if not raw_size:
                    break

                pkt_size = raw_size[0]
                if pkt_size == 0:
                    continue

                # 2. Read packet body
                payload = bytearray()
                while len(payload) < pkt_size:
                    chunk = self.conn.recv(pkt_size - len(payload))
                    if not chunk:
                        break
                    payload.extend(chunk)

                if len(payload) < pkt_size:
                    break

                self.handle_packet(payload)

        except ConnectionResetError:
            pass
        except Exception as e:
            log(f"Connection error with {client_ip}: {e}", "WARN")
        finally:
            try:
                self.conn.close()
            except:
                pass
            log(f"Closed connection with {client_ip}:{client_port}", "INFO")

    def handle_packet(self, payload: bytearray):
        pkt_size = len(payload)
        
        # 1. Scrutineering / Unlock Auth packet (76 bytes)
        # Starts with "LFS\0" or payload[4] == 4
        is_scrutineer = (pkt_size >= 72 and (payload[0:3] == b"LFS" or (len(payload) > 4 and payload[4] == 4)))

        # 2. JRA (Join Request Authorisation packet, 36 bytes)
        # Starts with "JRA\0"
        is_jra = (pkt_size >= 32 and payload[0:3] == b"JRA")

        # 3. Host Announcement / Registration (usemaster=yes)
        is_host_reg = (b"game_nm" in payload or b"game_port" in payload or b"usemaster" in payload)

        if is_scrutineer or is_jra:
            conn_id = 1
            username = self.active_user

            if is_scrutineer:
                if len(payload) > 1 and payload[1] != 0:
                    conn_id = payload[1]

                # Extract username from offset 0x28 (up to 24 bytes)
                if len(payload) >= 0x40:
                    raw_user = payload[0x28:0x40].split(b"\x00")[0]
                    u = raw_user.decode("ascii", errors="ignore").strip().lower()
                    if u:
                        username = u
                        self.active_user = u

                log(f"Scrutineer/Unlock request for User: '{username}' (ConnID: {conn_id})", "AUTH")

            elif is_jra:
                # In JRA, Connection ID is at offset 0x1C (28)
                if len(payload) > 0x1C:
                    conn_id = payload[0x1C]

                # If username present in packet
                if len(payload) >= 0x20:
                    raw_u = payload[4:0x1C].split(b"\x00")[0]
                    u = raw_u.decode("ascii", errors="ignore").strip().lower()
                    if u:
                        username = u

                log(f"JRA (Join Request Auth) for User: '{username}' (ConnID: {conn_id})", "AUTH")

            # Verify user with Cloudflare Worker
            auth_info = verify_user_with_web(username)
            status = 0 if auth_info["authorized"] else 2 # 0 = Authorised, 2 = Not Found
            license_str = auth_info.get("license", "S3")

            # Send 100-byte Authorisation response
            self.send_authorised_response(conn_id, username, status, license_str)

        elif is_host_reg:
            self.handle_host_registration(payload)

        else:
            # Echo or acknowledge ping/tiny packets
            log(f"Received LFS packet (size={pkt_size}, prefix={payload[:4]})", "INFO")

    def send_authorised_response(self, conn_id: int, username: str, status: int = 0, license_str: str = "S3"):
        """
        Sends LFS Authorisation response packet (100 bytes / 0x64).
        Status 0 = Success (Authorised : %s (%s))
        Status != 0 = Error
        """
        resp = bytearray(100)
        resp[0] = conn_id & 0xFF             # Must match requested connection ID
        resp[1] = status & 0xFF              # 0 = Authorised
        resp[2] = 0
        resp[3] = 0
        resp[4] = 3 if license_str == "S3" else (2 if license_str == "S2" else 1) # License flag

        # Username at offset 0x28 (24 bytes)
        user_bytes = username.encode("ascii", errors="ignore")[:23]
        resp[0x28:0x28+len(user_bytes)] = user_bytes

        # License string at offset 0x60 (printed in 'Authorised : %s (%s)')
        lic_bytes = license_str.encode("ascii")[:3]
        resp[0x60:0x60+len(lic_bytes)] = lic_bytes

        # Prepend 1-byte length header (100 = 0x64)
        packet_to_send = bytes([len(resp)]) + bytes(resp)
        self.conn.sendall(packet_to_send)
        log(f"Sent Authorisation Response -> Status={status} (OK), User='{username}', License='{license_str}'", "SUCCESS")

    def handle_host_registration(self, payload: bytearray):
        host_ip = self.addr[0]
        ACTIVE_SERVERS[host_ip] = {
            "ip": host_ip,
            "port": 29999,
            "last_seen": time.time()
        }
        log(f"Host '{host_ip}' announced online via usemaster=yes", "SUCCESS")

def run_cli_master_server(host=DEFAULT_HOST, port=DEFAULT_PORT):
    # Banner
    banner = f"""
\033[96m╔═══════════════════════════════════════════════════════════════════════╗
║             PROTHAILAND LFS 0.6V MASTER SERVER (CLI)                  ║
║  Listening on: {host}:{port:<5} (Localhost & LAN)                    ║
║  Connected to: {WORKER_AUTH_URL} ║
║  Features    : S3 License Unlock, JRA Multi-Join, KV Auth             ║
╚═══════════════════════════════════════════════════════════════════════╝\033[0m
"""
    print(banner)

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

    try:
        server.bind((host, port))
        server.listen(32)
        log(f"Master Server is ACTIVE and READY on {host}:{port}", "SUCCESS")
        log(f"LFS.exe can now connect to 127.0.0.1:{port} (Unlock / Play / Host)", "INFO")
        log("Press Ctrl+C to stop.\n", "INFO")

        while True:
            conn, addr = server.accept()
            handler = LFSTcpClientHandler(conn, addr)
            handler.start()

    except KeyboardInterrupt:
        print("\n")
        log("Shutting down Master Server...", "WARN")
    except Exception as ex:
        log(f"Failed to start server on {host}:{port}: {ex}", "ERROR")
        if "Address already in use" in str(ex):
            log("Port 29339 is already used by another process (Launcher or another master server).", "WARN")
    finally:
        server.close()
        log("Master Server stopped.", "INFO")

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="ProThailand LFS 0.6V Master Server CLI")
    parser.add_argument("--host", default=DEFAULT_HOST, help=f"Host address to bind (default: {DEFAULT_HOST})")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT, help=f"Port to listen on (default: {DEFAULT_PORT})")
    args = parser.parse_args()

    run_cli_master_server(host=args.host, port=args.port)
