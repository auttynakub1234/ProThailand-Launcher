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
        private static string _webApiUrl = "https://prothailand-master-api.fixflxw24.workers.dev/api/user";

        public static bool IsRunning
        {
            get { return _isRunning; }
        }

        public static void Start(string webApiBase = null)
        {
            if (_isRunning) return;

            if (!string.IsNullOrEmpty(webApiBase))
            {
                string baseClean = webApiBase.TrimEnd('/');
                if (baseClean.EndsWith("/api.php"))
                    baseClean = baseClean.Substring(0, baseClean.Length - 8);
                _webApiUrl = baseClean + "/api/user";
            }

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, 29339);
                _listener.Start();
                _isRunning = true;

                Thread thread = new Thread(ListenLoop);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                _isRunning = false;
            }
        }

        public static void Stop()
        {
            _isRunning = false;
            try { _listener?.Stop(); } catch { }
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

                        // Check Packet Type 4 (Scrutineering / Auth)
                        if (pktSize >= 76 && payload[4] == 4)
                        {
                            // Extract username from offset 0x28 (24 bytes)
                            string username = Encoding.ASCII.GetString(payload, 0x28, 24).Trim('\0', ' ').ToLower();
                            
                            // Check with Cloudflare Web Portal
                            bool verified = CheckWebUser(username);

                            // Send Authorisation Response (100 bytes / 0x64)
                            byte[] resp = new byte[100];
                            resp[0] = 1; // Connection ID
                            resp[1] = (byte)(verified ? 0 : 2); // 0 = Authorised, 2 = User Not Found
                            resp[4] = 3; // S3 License

                            // Write username
                            byte[] userBytes = Encoding.ASCII.GetBytes(username);
                            Array.Copy(userBytes, 0, resp, 0x28, Math.Min(userBytes.Length, 23));

                            // Write License string "S3"
                            byte[] licBytes = Encoding.ASCII.GetBytes(verified ? "S3" : "NONE");
                            Array.Copy(licBytes, 0, resp, 0x60, Math.Min(licBytes.Length, 3));

                            // Send with 1-byte length prefix
                            byte[] sendBuf = new byte[101];
                            sendBuf[0] = 100;
                            Array.Copy(resp, 0, sendBuf, 1, 100);

                            stream.Write(sendBuf, 0, sendBuf.Length);
                            stream.Flush();
                        }
                    }
                }
                catch { }
            }
        }

        private static bool CheckWebUser(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;

            try
            {
                string url = _webApiUrl + "?name=" + Uri.EscapeDataString(username);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ProThailand-Bridge/1.0";
                req.Timeout = 5000;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (StreamReader r = new StreamReader(s))
                {
                    string json = r.ReadToEnd();
                    return json.Contains("\"found\":true");
                }
            }
            catch
            {
                // Fallback: allow if username length >= 3
                return username.Length >= 3;
            }
        }
    }
}
