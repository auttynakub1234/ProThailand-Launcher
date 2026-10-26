"""
ProThailand LFS Master Server Daemon (Port 29339)
Speaks the Live for Speed 0.6V Master Protocol.
Bridges user authentication with ProThailand Cloudflare Web Portal & KV Database.
"""

import sys
import socket
import threading
import struct
import json
import urllib.request
import urllib.parse
import time

MASTER_PORT = 29339
WEB_USER_URL = "https://prothailand-master-api.fixflxw24.workers.dev/api/user"
WEB_AUTH_URL = "https://prothailand-master-api.fixflxw24.workers.dev/api/login"

# Store active game servers announced via usemaster=yes
ACTIVE_SERVERS = {}
REGISTERED_SESSIONS = {}

def verify_user_with_web_api(username: str, password: str = "") -> bool:
    """
    Checks user credentials against ProThailand Cloudflare Worker API.
    """
    if not username:
        return False

    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ProThailand-MasterServer/1.0"
    }

    try:
        # 1. First check if user exists on Web Portal (KV)
        check_url = f"{WEB_USER_URL}?name={urllib.parse.quote(username)}"
        req = urllib.request.Request(check_url, headers=headers)
        with urllib.request.urlopen(req, timeout=5) as resp:
            data = json.loads(resp.read().decode('utf-8'))
            if not data.get("found"):
                print(f"[WebAuth] User '{username}' NOT found on ProThailand Web Portal!")
                return False

            print(f"[WebAuth] Found user '{username}' on ProThailand Web Portal (Registered: {data.get('createdAt')})")

        # 2. If password provided, check password match via login endpoint
        if password:
            req_data = json.dumps({"username": username, "password": password}).encode('utf-8')
            login_req = urllib.request.Request(
                WEB_AUTH_URL,
                data=req_data,
                headers={
                    "Content-Type": "application/json",
                    **headers
                }
            )
            with urllib.request.urlopen(login_req, timeout=5) as resp:
                login_data = json.loads(resp.read().decode('utf-8'))
                if login_data.get("success"):
                    print(f"[WebAuth] User '{username}' credentials verified 100% via Web API!")
                    return True
                else:
                    print(f"[WebAuth] User '{username}' wrong password via Web API!")
                    return False

        # If user found in KV and was authenticated through launcher
        return True
    except Exception as e:
        print(f"[WebAuth] API check error for '{username}': {e}")
        return False

class LFSMasterClientHandler(threading.Thread):
    def __init__(self, conn, addr):
        super().__init__()
        self.conn = conn
        self.addr = addr
        self.daemon = True

    def run(self):
        client_ip, client_port = self.addr
        print(f"[Master] New connection from {client_ip}:{client_port}")

        try:
            while True:
                # 1. Read size byte
                raw_size = self.conn.recv(1)
                if not raw_size:
                    break

                pkt_size = raw_size[0]
                if pkt_size == 0:
                    continue

                # 2. Read packet payload
                payload = b""
                while len(payload) < pkt_size:
                    chunk = self.conn.recv(pkt_size - len(payload))
                    if not chunk:
                        break
                    payload += chunk

                if len(payload) < pkt_size:
                    break

                self.handle_packet(payload)

        except Exception as e:
            print(f"[Master] Connection error with {client_ip}: {e}")
        finally:
            self.conn.close()
            print(f"[Master] Closed connection with {client_ip}:{client_port}")

    def handle_packet(self, payload: bytes):
        """
        Parses LFS Master packets.
        """
        if len(payload) < 4:
            return

        pkt_type = payload[4] if len(payload) > 4 else payload[0]
        print(f"[Master] Received packet size={len(payload)} type={pkt_type}")

        # Packet 0x04: Handshake / Authentication / Scrutineering
        if len(payload) >= 0x48:
            try:
                # Username is at offset 0x28 (up to 24 bytes null-terminated)
                raw_user = payload[0x28:0x40]
                username = raw_user.split(b'\x00')[0].decode('ascii', errors='ignore').strip()
                
                # Password hash is DWORD at 0x44
                pass_hash = struct.unpack("<I", payload[0x44:0x48])[0]
                print(f"[Master] Login Request -> Username: '{username}', PassHash: {hex(pass_hash)}")

                # Check user with Cloudflare Worker Web Portal
                is_valid = verify_user_with_web_api(username)

                if is_valid:
                    self.send_authorised_response(username, status=0, license_str="S3")
                else:
                    self.send_authorised_response(username, status=2, license_str="NONE") # 2 = User not found
            except Exception as ex:
                print(f"[Master] Parse error in auth packet: {ex}")

        # Host Announcement / Registration (usemaster=yes)
        # Often starts with SRV or contains host info
        if b"game_nm" in payload or b"game_port" in payload or len(payload) > 80:
            print(f"[Master] Host registration received from {self.addr[0]}")
            self.register_host(payload)

    def send_authorised_response(self, username: str, status: int = 0, license_str: str = "S3"):
        """
        Sends LFS Authorisation response packet.
        Status 0 = Success (calls Authorised : %s (%s))
        Status != 0 = Error
        """
        # Build 0x64 byte response packet
        resp = bytearray(0x64)
        resp[0] = 1 # Connection / Guest ID
        resp[1] = status # 0 = Authorised
        resp[2] = 0
        resp[3] = 0
        
        # License flags at offset 4 (e.g. 3 for S3)
        resp[4] = 3 if license_str == "S3" else (2 if license_str == "S2" else 1)

        # Username at offset 0x28
        user_bytes = username.encode('ascii', errors='ignore')[:23]
        resp[0x28:0x28+len(user_bytes)] = user_bytes

        # License string at offset 0x60 (printed in (%s))
        lic_bytes = license_str.encode('ascii')[:3]
        if len(resp) >= 0x64:
            resp[0x60:0x60+len(lic_bytes)] = lic_bytes

        # Send with 1-byte length header prepended
        packet_to_send = bytes([len(resp)]) + bytes(resp)
        self.conn.sendall(packet_to_send)
        print(f"[Master] Sent Authorisation response: Status={status}, User='{username}', License='{license_str}'")

    def register_host(self, payload: bytes):
        """Registers game host online."""
        host_ip = self.addr[0]
        ACTIVE_SERVERS[host_ip] = {
            "ip": host_ip,
            "port": 29999,
            "last_seen": time.time()
        }
        print(f"[Master] Host {host_ip} registered into online master list.")

def run_master_server(host="0.0.0.0", port=MASTER_PORT):
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((host, port))
    server.listen(10)
    print(f"===========================================================")
    print(f"[*] ProThailand LFS Master Server listening on {host}:{port}")
    print(f"[*] Connected to Web Portal: {WEB_AUTH_URL}")
    print(f"[*] Ready to authenticate usemaster=yes hosts & players!")
    print(f"===========================================================")

    try:
        while True:
            conn, addr = server.accept()
            handler = LFSMasterClientHandler(conn, addr)
            handler.start()
    except KeyboardInterrupt:
        print("\n[*] Stopping Master Server.")
    finally:
        server.close()

if __name__ == "__main__":
    run_master_server()
