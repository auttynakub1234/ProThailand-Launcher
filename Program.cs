using System;
using System.Windows.Forms;

namespace ProThailand
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // If arguments are provided for headless / CLI testing
            if (args != null && args.Length > 0)
            {
                string cmd = args[0].ToLower();

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

                if (cmd == "--help" || cmd == "-h")
                {
                    Console.WriteLine("ProThailand Launcher (LFS 0.6V)");
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  ProThailand.exe                 (Runs WinForms GUI)");
                    Console.WriteLine("  ProThailand.exe --inspect-hwid  (Displays Hardware Fingerprint)");
                    Console.WriteLine("  ProThailand.exe --login <user> <pass> [server_url]");
                    return 0;
                }
            }

            // Default: Run WinForms GUI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormMain());
            return 0;
        }
    }
}
