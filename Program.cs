using System;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;

namespace ProThailand
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // Set up global crash handler so crashes are never silent
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Exception ex = e.ExceptionObject as Exception;
                string msg = (ex != null) ? ex.ToString() : "Unknown application error";
                MessageBox.Show(msg, "ProThailand Launcher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            try
            {
                // Force TLS 1.2 and bypass self-signed / proxy certificates
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
                }
                catch { }

                // CLI arguments handler
                if (args != null && args.Length > 0)
                {
                    string cmd = args[0].ToLower();

                    if (cmd == "--auto" || cmd == "-a")
                    {
                        string user = LFSManager.GetSetting("Username", "Pilot01");
                        string pass = LFSManager.GetSetting("Password", "123456");
                        string lfsPath = LFSManager.FindLfsExecutable();

                        if (string.IsNullOrEmpty(lfsPath))
                        {
                            MessageBox.Show("Could not locate LFS.exe. Please run ProThailand.exe normally once to set your game path.", "LFS Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return 1;
                        }

                        LocalMasterBridge.Start(LoginHandle.DefaultRemoteServer, user);
                        Process proc = LFSManager.LaunchLfsProcess(lfsPath, user, pass, "127.0.0.1");
                        if (proc != null)
                        {
                            proc.WaitForExit();
                        }
                        LocalMasterBridge.Stop();
                        return 0;
                    }

                    if (cmd == "--inspect-hwid" || cmd == "-i")
                    {
                        Console.WriteLine("=== HARDWARE FINGERPRINT (HWID) ===");
                        Console.WriteLine("MAC Address:        " + HardwareInfo.GetMacAddress());
                        Console.WriteLine("Motherboard Serial: " + HardwareInfo.GetMotherboardSerial());
                        Console.WriteLine("BIOS UUID:          " + HardwareInfo.GetBiosUuid());
                        Console.WriteLine("HDD Serial:         " + HardwareInfo.GetHddSerial());
                        Console.WriteLine("OS User / Machine:  " + Environment.UserName + " @ " + Environment.MachineName);
                        Console.WriteLine("Sample Payload:     " + HardwareInfo.BuildPayloadString("TestUser", "Pass123"));
                        return 0;
                    }

                    if (cmd == "--login" || cmd == "-l")
                    {
                        string user = (args.Length > 1) ? args[1] : "Pilot01";
                        string pass = (args.Length > 2) ? args[2] : "123456";
                        string server = (args.Length > 3) ? args[3] : LoginHandle.DefaultLocalServer;

                        Console.WriteLine("[*] Authenticating with: " + server);
                        Console.WriteLine("[*] User: " + user);
                        var res = LoginHandle.DoLogin(user, pass, server);

                        Console.WriteLine("[*] Success: " + res.Success);
                        Console.WriteLine("[*] Status:  " + res.Status);
                        Console.WriteLine("[*] Message: " + res.Message);
                        if (!string.IsNullOrEmpty(res.RawResponse))
                            Console.WriteLine("[*] Raw:     " + res.RawResponse);
                        return res.Success ? 0 : 1;
                    }

                    if (cmd == "--bridge" || cmd == "-b")
                    {
                        string user = (args.Length > 1) ? args[1] : "Pilot01";
                        string server = (args.Length > 2) ? args[2] : LoginHandle.DefaultRemoteServer;
                        LocalMasterBridge.OnLog += Console.WriteLine;
                        LocalMasterBridge.Start(server, user);
                        Console.WriteLine("[*] Bridge active for user: " + user + ". Press Enter or Ctrl+C to stop.");
                        Console.ReadLine();
                        LocalMasterBridge.Stop();
                        return 0;
                    }

                    if (cmd == "--help" || cmd == "-h")
                    {
                        Console.WriteLine("ProThailand Launcher (LFS 0.6V)");
                        Console.WriteLine("Usage:");
                        Console.WriteLine("  ProThailand.exe                 (Runs WinForms GUI)");
                        Console.WriteLine("  ProThailand.exe --inspect-hwid  (Displays Hardware Fingerprint)");
                        Console.WriteLine("  ProThailand.exe --login <user> <pass> [server_url]");
                        Console.WriteLine("  ProThailand.exe --bridge [username] [server_url]");
                        return 0;
                    }
                }

                // Default: Run WinForms GUI
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FormMain());
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Startup Error:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                    "ProThailand Launcher Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
