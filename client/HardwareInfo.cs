using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Management;

namespace ProThailand
{
    public static class HardwareInfo
    {
        public static string GetMacAddress()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        string mac = nic.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac) && mac.Length >= 8)
                            return mac;
                    }
                }
            }
            catch { }
            return "0011223344AA";
        }

        public static string GetMotherboardSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber, Manufacturer, Product FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object serial = mo["SerialNumber"];
                        if (serial != null && !string.IsNullOrWhiteSpace(serial.ToString()))
                            return Clean(serial.ToString());
                    }
                }
            }
            catch { }

            // Linux / Wine fallback
            try
            {
                if (File.Exists("/sys/devices/virtual/dmi/id/board_serial"))
                {
                    string s = File.ReadAllText("/sys/devices/virtual/dmi/id/board_serial").Trim();
                    if (!string.IsNullOrEmpty(s)) return Clean(s);
                }
            }
            catch { }

            return "MB-TH-UNKNOWN-01";
        }

        public static string GetBiosUuid()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object serial = mo["SerialNumber"];
                        if (serial != null && !string.IsNullOrWhiteSpace(serial.ToString()))
                            return Clean(serial.ToString());
                    }
                }
            }
            catch { }

            // Linux / Wine fallback
            try
            {
                if (File.Exists("/sys/devices/virtual/dmi/id/product_uuid"))
                {
                    string s = File.ReadAllText("/sys/devices/virtual/dmi/id/product_uuid").Trim();
                    if (!string.IsNullOrEmpty(s)) return Clean(s);
                }
                if (File.Exists("/etc/machine-id"))
                {
                    string s = File.ReadAllText("/etc/machine-id").Trim();
                    if (!string.IsNullOrEmpty(s)) return Clean(s);
                }
            }
            catch { }

            return "BIOS-TH-DEFAULT-99";
        }

        public static string GetHddSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Index, Model, InterfaceType, DeviceID FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object model = mo["Model"];
                        if (model != null && !string.IsNullOrWhiteSpace(model.ToString()))
                            return Clean(model.ToString());
                    }
                }
            }
            catch { }

            return "HDD-TH-STORAGE-01";
        }

        public static string GenerateHash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string Clean(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "UNKNOWN";
            return input.Replace("|", "_").Replace("\r", "").Replace("\n", "").Trim();
        }

        public static string BuildPayloadString(string username, string password)
        {
            // Exact sequence reverse engineered from ProLight <doLogin>d__2:
            // username | Hash(password) | HDD | Motherboard | MAC | BIOS | "PT" | UserName | MachineName | "1.0.0"
            string passHash = GenerateHash(password);
            string hdd = GetHddSerial();
            string mb = GetMotherboardSerial();
            string mac = GetMacAddress();
            string bios = GetBiosUuid();
            string launcherTag = "PT"; // ProThailand launcher tag
            string osUser = Clean(Environment.UserName);
            string osMachine = Clean(Environment.MachineName);
            string version = "1.0.0";

            return string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}",
                username, passHash, hdd, mb, mac, bios, launcherTag, osUser, osMachine, version);
        }
    }
}
