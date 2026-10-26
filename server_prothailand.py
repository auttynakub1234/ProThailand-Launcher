"""
ProThailand Master Server (Local Mock & Standalone Server)
Simulates https://api.prothailand.net/api.php
Handles incoming encrypted authentication requests from ProThailand.exe.
"""

import json
import urllib.parse
from http.server import HTTPServer, BaseHTTPRequestHandler
import base64
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives import hashes, padding

MASTER_PASSWORD = b'config.sys.input=UTF8'
ITERATIONS = 100000

def decrypt_payload(b64_str: str) -> str:
    raw = base64.b64decode(b64_str)
    if len(raw) < 48:
        raise ValueError("Payload too short")
    salt = raw[:16]
    iv = raw[16:32]
    ct = raw[32:]
    
    kdf = PBKDF2HMAC(
        algorithm=hashes.SHA256(),
        length=32,
        salt=salt,
        iterations=ITERATIONS
    )
    key = kdf.derive(MASTER_PASSWORD)
    cipher = Cipher(algorithms.AES(key), modes.CBC(iv))
    decryptor = cipher.decryptor()
    padded = decryptor.update(ct) + decryptor.finalize()
    unpadder = padding.PKCS7(128).unpadder()
    decrypted_bytes = unpadder.update(padded) + unpadder.finalize()
    return decrypted_bytes.decode('utf-8').lstrip('\ufeff')

# Simple in-memory user database
# username -> { "pass_hash": ..., "hwid": ... }
REGISTERED_USERS = {}

class ProThailandMasterHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        url = urllib.parse.urlparse(self.path)
        path = url.path
        query = urllib.parse.parse_qs(url.query)

        # Handle /api.php or /
        if path.endswith("/api.php") or path == "/":
            data_enc = query.get("data", [""])[0]
            info = query.get("info", [""])[0]

            if not data_enc:
                self.send_response(200)
                self.send_header("Content-Type", "application/json")
                self.end_headers()
                self.wfile.write(json.dumps({
                    "Server": "ProThailand Master API v1.0",
                    "Status": "ONLINE",
                    "Endpoint": "https://api.prothailand.net/api.php"
                }).encode('utf-8'))
                return

            print("\n=======================================================")
            print("[MasterServer] Inbound encrypted auth request received!")
            try:
                decrypted_str = decrypt_payload(data_enc)
                print(f"[MasterServer] Decrypted Payload:\n  {decrypted_str}")

                # Format: username|passHash|hdd|mb|mac|bios|tag|osUser|osMachine|version
                parts = decrypted_str.split('|')
                username = parts[0] if len(parts) > 0 else "Unknown"
                pass_hash = parts[1] if len(parts) > 1 else ""
                hdd_serial = parts[2] if len(parts) > 2 else ""
                mb_serial = parts[3] if len(parts) > 3 else ""
                mac_addr = parts[4] if len(parts) > 4 else ""
                bios_uuid = parts[5] if len(parts) > 5 else ""
                client_tag = parts[6] if len(parts) > 6 else ""
                os_user = parts[7] if len(parts) > 7 else ""
                os_machine = parts[8] if len(parts) > 8 else ""
                client_ver = parts[9] if len(parts) > 9 else ""

                print(f"[MasterServer] User:               {username}")
                print(f"[MasterServer] Password SHA256:    {pass_hash[:16]}...")
                print(f"[MasterServer] MAC Address:        {mac_addr}")
                print(f"[MasterServer] Motherboard Serial: {mb_serial}")
                print(f"[MasterServer] BIOS UUID:          {bios_uuid}")
                print(f"[MasterServer] HDD Serial:         {hdd_serial}")
                print(f"[MasterServer] Client Machine:     {os_user}@{os_machine} (Tag: {client_tag}, Ver: {client_ver})")

                # Verify or auto-register user
                if username not in REGISTERED_USERS:
                    REGISTERED_USERS[username] = {
                        "pass_hash": pass_hash,
                        "mac": mac_addr,
                        "mb": mb_serial,
                        "bios": bios_uuid
                    }
                    print(f"[MasterServer] New user '{username}' registered with current HWID.")
                    response_obj = {
                        "Status": "LOGINOK",
                        "Username": username,
                        "Rank": "Driver",
                        "Credits": 1000,
                        "ServerMessage": f"Welcome {username}! Registered successfully to ProThailand."
                    }
                else:
                    user_data = REGISTERED_USERS[username]
                    if user_data["pass_hash"] != pass_hash:
                        print(f"[MasterServer] Login rejected: Incorrect password for '{username}'.")
                        response_obj = {
                            "Status": "WRONGPASS",
                            "Message": "Password does not match."
                        }
                    else:
                        print(f"[MasterServer] Login accepted for user '{username}'.")
                        response_obj = {
                            "Status": "LOGINOK",
                            "Username": username,
                            "Rank": "Driver",
                            "Credits": 1000,
                            "ServerMessage": f"Welcome back, {username}!"
                        }

            except Exception as e:
                print(f"[MasterServer] Decryption/Parsing error: {e}")
                response_obj = {
                    "Status": "ERROR",
                    "Message": f"Decryption error: {str(e)}"
                }

            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(json.dumps(response_obj).encode('utf-8'))
            print("=======================================================\n")

def run_server(host="0.0.0.0", port=8080):
    server = HTTPServer((host, port), ProThailandMasterHandler)
    print(f"[*] ProThailand Master Server listening on http://{host}:{port}/api.php")
    print(f"[*] Ready to receive HWID & Login from ProThailand.exe")
    server.serve_forever()

if __name__ == "__main__":
    run_server()
