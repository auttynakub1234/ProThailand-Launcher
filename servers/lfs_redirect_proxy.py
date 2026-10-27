#!/usr/bin/env python3
"""
Local Proxy for LFS to redirect m.lfspro.net to Cloudflare Worker
Run this on localhost before launching LFS.exe
"""

import socket
import threading
import http.client
import re

WORKER_HOST = "prothailand-master-api.fixflxw24.workers.dev"
LOCAL_HOST = "127.0.0.1"
LOCAL_PORT = 80  # Standard HTTP port for m.lfspro.net

def handle_client(client_socket):
    try:
        request = client_socket.recv(4096).decode('utf-8', errors='ignore')
        
        # Extract Host header
        host_match = re.search(r'Host:\s*([^\r\n]+)', request, re.IGNORECASE)
        if not host_match:
            client_socket.close()
            return
            
        # Replace Host header with worker host
        request = re.sub(
            r'Host:\s*[^\r\n]+',
            f'Host: {WORKER_HOST}',
            request,
            flags=re.IGNORECASE
        )
        
        # Forward to Cloudflare Worker
        conn = http.client.HTTPSConnection(WORKER_HOST, 443)
        conn.request("GET", "/api.php", body=request)
        response = conn.getresponse()
        
        # Send response back to LFS
        response_data = response.read()
        client_socket.send(b"HTTP/1.1 " + str(response.status).encode() + b" " + 
                          response.reason.encode() + b"\r\n")
        for header, value in response.getheaders():
            client_socket.send(f"{header}: {value}\r\n".encode())
        client_socket.send(b"\r\n")
        client_socket.send(response_data)
        
        conn.close()
        client_socket.close()
        
    except Exception as e:
        print(f"Error: {e}")
        client_socket.close()

def start_proxy():
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((LOCAL_HOST, LOCAL_PORT))
    server.listen(5)
    
    print(f"[*] LFS Redirect Proxy listening on {LOCAL_HOST}:{LOCAL_PORT}")
    print(f"[*] Redirecting m.lfspro.net -> {WORKER_HOST}")
    print("[*] Add to hosts file: 127.0.0.1 m.lfspro.net")
    
    while True:
        client_socket, addr = server.accept()
        client_handler = threading.Thread(target=handle_client, args=(client_socket,))
        client_handler.start()

if __name__ == "__main__":
    start_proxy()
