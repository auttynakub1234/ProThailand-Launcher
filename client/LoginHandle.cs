using System;
using System.IO;
using System.Net;
using System.Text;

namespace ProThailand
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string RawResponse { get; set; }
        public string DecryptedPayload { get; set; }
    }

    public static class LoginHandle
    {
        public const string DefaultLocalServer = "http://localhost:8080/api.php";
        public const string DefaultRemoteServer = "https://prothailand-master-api.fixflxw24.workers.dev/api.php";

        public static LoginResult DoLogin(string username, string password, string serverBaseUrl)
        {
            var result = new LoginResult { Success = false };

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                result.Message = "Username and password cannot be empty.";
                return result;
            }

            try
            {
                // 1. Gather hardware fingerprint and build payload string
                string payload = HardwareInfo.BuildPayloadString(username, password);
                result.DecryptedPayload = payload;

                // 2. Encrypt payload using LFSPro master crypto
                string encryptedData = SecureCrypto.Encrypt(payload);

                // 3. Construct URL
                // e.g. http://localhost:8080/api.php?sv=lfspro&info=login&data=...
                string delimiter = serverBaseUrl.Contains("?") ? "&" : "?";
                string fullUrl = string.Format("{0}{1}sv=lfspro&info=login&data={2}",
                    serverBaseUrl, delimiter, Uri.EscapeDataString(encryptedData));

                // 4. Send HTTP Request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUrl);
                request.Method = "GET";
                request.Timeout = 10000;
                request.UserAgent = "ProThailand-Launcher/1.0";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string responseBody = reader.ReadToEnd().Trim();
                    result.RawResponse = responseBody;

                    // Parse responses
                    if (responseBody.Contains("\"LOGINOK\"") || responseBody.Contains("LOGINOK"))
                    {
                        result.Success = true;
                        result.Status = "LOGINOK";
                        result.Message = "Login Successful! Welcome to ProThailand.";
                    }
                    else if (responseBody.Contains("WRONGPASS"))
                    {
                        result.Status = "WRONGPASS";
                        result.Message = "Incorrect password! Please try again.";
                    }
                    else if (responseBody.Contains("USERNOTEXIST"))
                    {
                        result.Status = "USERNOTEXIST";
                        result.Message = "User does not exist on ProThailand Master Server.";
                    }
                    else if (responseBody.Contains("USERISMULTIACC"))
                    {
                        result.Status = "USERISMULTIACC";
                        result.Message = "Multiple accounts detected for this hardware fingerprint.";
                    }
                    else
                    {
                        result.Status = "UNKNOWN";
                        result.Message = "Server Response: " + responseBody;
                        // If the server returns valid JSON with status
                        if (responseBody.StartsWith("{") && responseBody.Contains("\"Status\""))
                        {
                            result.Success = responseBody.Contains("LOGINOK");
                        }
                    }
                }
            }
            catch (WebException wex)
            {
                result.Message = "Network/Server Error: " + wex.Message;
                if (wex.Response != null)
                {
                    using (var errReader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        result.RawResponse = errReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = "Error: " + ex.Message;
            }

            return result;
        }
    }
}
