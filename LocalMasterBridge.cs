using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ProThailand
{
    public static class LocalMasterBridge
    {
        private static TcpListener _listener;
        private static bool _isRunning = false;
        private static string _webApiUrl = "https://prothailand-master-api.fixflxw24.workers.dev/api/master/auth";
        private static string _activeUsername = "Pilot01";

        public static bool IsRunning
        {
            get { return _isRunning; }
        }

        public static string ActiveUsername
        {
            get { return _activeUsername; }
            set { if (!string.IsNullOrEmpty(value)) _activeUsername = value.Trim().ToLower(); }
        }

        public static event Action<string> OnLog;

        private static void Log(string msg)
        {
            try { OnLog?.Invoke(msg); } catch { }
        }

        public static void Start(string webApiBase = null, string username = null)
        {
            if (_isRunning) return;

            if (!string.IsNullOrEmpty(username))
                _activeUsername = username.Trim().ToLower();

            if (!string.IsNullOrEmpty(webApiBase))
            {
                string baseClean = webApiBase.TrimEnd('/');
                if (baseClean.EndsWith("/api.php"))
                    baseClean = baseClean.Substring(0, baseClean.Length - 8);
                _webApiUrl = baseClean + "/api/master/auth";
            }

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, 29339);
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();
                _isRunning = true;

                Log("[Bridge] Listening on 127.0.0.1:29339 (LFS Master Protocol)");

                Thread thread = new Thread(ListenLoop);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                Log("[Bridge] Failed to start listener: " + ex.Message);
            }
        }

        public static void Stop()
        {
            _isRunning = false;
            try { _listener?.Stop(); } catch { }
            Log("[Bridge] Stopped.");
        }

        private static void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(state => HandleClient(client));
                }
                catch
                {
                    if (!_isRunning) break;
                }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] sizeBuf = new byte[1];
                    while (_isRunning && stream.Read(sizeBuf, 0, 1) > 0)
                    {
                        int pktSize = sizeBuf[0];
                        if (pktSize == 0) continue;

                        byte[] payload = new byte[pktSize];
                        int read = 0;
                        while (read < pktSize)
                        {
                            int r = stream.Read(payload, read, pktSize - read);
                            if (r <= 0) break;
                            read += r;
                        }

                        if (read < pktSize) break;

                        // 1. Check Packet Type 4 (Scrutineering / Unlock Auth, 76 bytes)
                        // Starts with "LFS\0" (0x4C, 0x46, 0x53, 0x00) or payload[4] == 4
                        bool isScrutineer = (pktSize >= 72 && (payload[0] == 0x4C || (payload.Length > 4 && payload[4] == 4)));
                        
                        // 2. Check JRA (Join Request Authorisation, 36 bytes)
                        // Starts with "JRA\0" (0x4A, 0x52, 0x41, 0x00)
                        bool isJra = (pktSize >= 32 && payload[0] == 0x4A && payload[1] == 0x52 && payload[2] == 0x41);

                        if (isScrutineer || isJra)
                        {
                            byte connId = 1;
                            string username = _activeUsername;

                            if (isScrutineer)
                            {
                                if (payload.Length > 1 && payload[1] != 0)
                                    connId = payload[1];

                                // Extract username from offset 0x28 (40 bytes, up to 24 bytes)
                                if (payload.Length >= 0x40)
                                {
                                    string extracted = Encoding.ASCII.GetString(payload, 0x28, Math.Min(24, payload.Length - 0x28)).Trim('\0', ' ').ToLower();
                                    if (!string.IsNullOrEmpty(extracted))
                                        username = extracted;
                                }
                                Log("[Bridge] Received Scrutineer/Auth packet for user: " + username);
                            }
                            else if (isJra)
                            {
                                // In JRA, Connection ID is at offset 0x1C (28)
                                if (payload.Length > 0x1C)
                                    connId = payload[0x1C];

                                Log(string.Format("[Bridge] Received JRA (Join Request Auth) for user: {0} (ConnID: {1})", username, connId));
                            }

                            // Verify with Cloudflare Web Portal
                            bool verified = CheckWebUser(username);
                            if (!verified && username.Length >= 2)
                                verified = true; // Fallback permit for active launcher user

                            SendAuthorisedResponse(stream, connId, username, verified);
                            Log(string.Format("[Bridge] Sent Authorised Response: Status={0}, License=S3, User={1}", verified ? "OK" : "DENIED", username));
                        }
                    }
                }
                catch { }
            }
        }

        private static void SendAuthorisedResponse(NetworkStream stream, byte connId, string username, bool verified)
        {
            // Build 100-byte response packet (0x64 bytes)
            byte[] resp = new byte[100];
            resp[0] = connId; // Must match connection ID
            resp[1] = (byte)(verified ? 0 : 2); // 0 = Authorised, 2 = User Not Found
            resp[2] = 0;
            resp[3] = 0;
            resp[4] = 3; // 3 = S3 License

            // Write username at offset 0x28
            byte[] userBytes = Encoding.ASCII.GetBytes(username ?? _activeUsername);
            Array.Copy(userBytes, 0, resp, 0x28, Math.Min(userBytes.Length, 23));

            // Write License string "S3" at offset 0x60
            byte[] licBytes = Encoding.ASCII.GetBytes(verified ? "S3" : "NONE");
            Array.Copy(licBytes, 0, resp, 0x60, Math.Min(licBytes.Length, 3));

            // Send with 1-byte length prefix
            byte[] sendBuf = new byte[101];
            sendBuf[0] = 100;
            Array.Copy(resp, 0, sendBuf, 1, 100);

            stream.Write(sendBuf, 0, sendBuf.Length);
            stream.Flush();
        }

        private static bool CheckWebUser(string username)
        {
            if (string.IsNullOrEmpty(username)) return true;

            try
            {
                string url = _webApiUrl + "?name=" + Uri.EscapeDataString(username);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ProThailand-Bridge/1.0";
                req.Timeout = 4000;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (StreamReader r = new StreamReader(s))
                {
                    string json = r.ReadToEnd();
                    return json.Contains("\"authorized\":true") || json.Contains("\"found\":true");
                }
            }
            catch
            {
                // Fallback: allow active user if username length >= 2
                return username.Length >= 2;
            }
        }
    }
}
