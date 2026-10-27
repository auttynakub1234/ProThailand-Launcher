using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;

namespace ProThailand
{
    public static class LFSManager
    {
        private static string _customLfsPath = null;
        public static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prothailand.ini");

        // LFS file format constants
        public const string PLY_MAGIC = "LFSPLY";
        public const string USR_MAGIC = "LFSUSR";
        public const int USR_RECORD_SIZE = 32;

        public static string CustomLfsPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_customLfsPath))
                    return _customLfsPath;

                // Load from ini if saved
                try
                {
                    if (File.Exists(SettingsFile))
                    {
                        string[] lines = File.ReadAllLines(SettingsFile);
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("LfsPath=") && line.Length > 8)
                            {
                                string p = line.Substring(8).Trim();
                                if (File.Exists(p))
                                {
                                    _customLfsPath = p;
                                    return _customLfsPath;
                                }
                            }
                        }
                    }
                }
                catch { }

                return null;
            }
            set
            {
                _customLfsPath = value;
                SaveSetting("LfsPath", value ?? "");
            }
        }

        public static string GetSetting(string key, string defaultValue = "")
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string[] lines = File.ReadAllLines(SettingsFile);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith(key + "="))
                            return line.Substring(key.Length + 1).Trim();
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        public static void SaveSetting(string key, string value)
        {
            try
            {
                string content = "";
                if (File.Exists(SettingsFile))
                    content = File.ReadAllText(SettingsFile);

                string pattern = @"(?m)^" + Regex.Escape(key) + @"=.*$";
                if (Regex.IsMatch(content, pattern))
                    content = Regex.Replace(content, pattern, key + "=" + value);
                else
                    content = content.TrimEnd() + "\r\n" + key + "=" + value + "\r\n";

                File.WriteAllText(SettingsFile, content.Trim() + "\r\n");
            }
            catch { }
        }

        public static string FindLfsExecutable()
        {
            // 1. Check custom path first
            string custom = CustomLfsPath;
            if (!string.IsNullOrEmpty(custom) && File.Exists(custom))
                return Path.GetFullPath(custom);

            // 2. Candidate search locations
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidatePaths = new string[]
            {
                Path.Combine(baseDir, "LFS.exe"),
                Path.Combine(baseDir, "..", "LFS.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "LFS.exe"),
                "C:\\LFS\\LFS.exe",
                "D:\\LFS\\LFS.exe",
                "E:\\LFS\\LFS.exe",
                "C:\\Live for Speed\\LFS.exe",
                "D:\\Live for Speed\\LFS.exe",
                "E:\\Live for Speed\\LFS.exe",
                "C:\\Games\\LFS\\LFS.exe",
                "D:\\Games\\LFS\\LFS.exe",
                "C:\\Program Files\\LFS\\LFS.exe",
                "C:\\Program Files (x86)\\LFS\\LFS.exe",
                "/sdcard/reverse-skill/work/newproject/LFS.exe"
            };

            foreach (string p in candidatePaths)
            {
                try
                {
                    if (File.Exists(p))
                    {
                        CustomLfsPath = Path.GetFullPath(p);
                        return CustomLfsPath;
                    }
                }
                catch { }
            }
            return null;
        }

        public static bool IsLfsRunning()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("LFS");
                return procs != null && procs.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a standard LFS player profile (.ply) file
        /// </summary>
        public static bool CreatePlayerProfile(string lfsDir, string username)
        {
            try
            {
                if (string.IsNullOrEmpty(lfsDir) || !Directory.Exists(lfsDir))
                    return false;

                string miscDir = Path.Combine(lfsDir, "data", "misc");
                if (!Directory.Exists(miscDir))
                    Directory.CreateDirectory(miscDir);

                string plyPath = Path.Combine(miscDir, $"{username}.ply");

                // Create a new .ply file based on the observed structure
                using (FileStream fs = new FileStream(plyPath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    // Magic header (6 bytes): "LFSPLY"
                    writer.Write(Encoding.ASCII.GetBytes(PLY_MAGIC));

                    // Version (2 bytes): 0x0007 (observed in sample)
                    writer.Write((short)0x0007);

                    // Reserved zeros (4 bytes)
                    writer.Write((int)0x00000000);

                    // Player data size offset (4 bytes): 0x0000001E (30 decimal)
                    writer.Write((int)0x0000001E);

                    // Unknown flags/values from sample
                    writer.Write((int)0x00000000); // 0x10-0x13
                    writer.Write((short)0x0100);   // 0x14-0x15
                    writer.Write((short)0x0100);   // 0x16-0x17
                    writer.Write((int)0x00000000); // 0x18-0x1B
                    writer.Write((int)0x00000000); // 0x1C-0x1F
                    writer.Write((int)0x00010000); // 0x20-0x23

                    // Fill rest with zeros (based on sample size ~1360 bytes)
                    for (int i = 0x24; i < 1360; i++)
                    {
                        writer.Write((byte)0);
                    }
                }

                Console.WriteLine($"Created player profile: {plyPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating player profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates or updates the user database file (usr.bin)
        /// </summary>
        public static bool UpdateUserDatabase(string lfsDir, string username)
        {
            try
            {
                if (string.IsNullOrEmpty(lfsDir) || !Directory.Exists(lfsDir))
                    return false;

                string miscDir = Path.Combine(lfsDir, "data", "misc");
                if (!Directory.Exists(miscDir))
                    Directory.CreateDirectory(miscDir);

                string usrPath = Path.Combine(miscDir, "usr.bin");

                byte[] existingRecords = null;
                if (File.Exists(usrPath))
                {
                    existingRecords = File.ReadAllBytes(usrPath);
                    // Check if user already exists
                    for (int i = 0; i < existingRecords.Length; i += USR_RECORD_SIZE)
                    {
                        if (i + USR_RECORD_SIZE > existingRecords.Length)
                            break;

                        string recordUser = Encoding.ASCII.GetString(existingRecords, i, 24).TrimEnd('\0');
                        if (recordUser.Equals(username, StringComparison.OrdinalIgnoreCase))
                        {
                            // User already exists
                            return true;
                        }
                    }
                }

                // Create new user record
                byte[] userRecord = new byte[USR_RECORD_SIZE];
                Encoding.ASCII.GetBytes(username).CopyTo(userRecord, 0);

                // Append to file
                using (FileStream fs = new FileStream(usrPath, FileMode.Append, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    // Write magic if new file
                    if (fs.Position == 0)
                    {
                        writer.Write(Encoding.ASCII.GetBytes(USR_MAGIC));
                        writer.Write((short)0x0000); // Padding
                    }

                    writer.Write(userRecord);
                }

                Console.WriteLine($"Updated user database: {usrPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user database: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets up complete player profile system
        /// </summary>
        public static bool SetupPlayerProfile(string lfsDir, string username)
        {
            try
            {
                bool plySuccess = CreatePlayerProfile(lfsDir, username);
                bool usrSuccess = UpdateUserDatabase(lfsDir, username);

                // Create drv.nam if it doesn't exist
                string drvPath = Path.Combine(lfsDir, "data", "misc", "drv.nam");
                if (!File.Exists(drvPath))
                {
                    File.WriteAllText(drvPath, username, Encoding.ASCII);
                }

                return plySuccess && usrSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up player profile: {ex.Message}");
                return false;
            }
        }

        public static void UpdateLfsConfig(string lfsDir, string username, string password)
        {
            if (string.IsNullOrEmpty(lfsDir) || !Directory.Exists(lfsDir))
                return;

            string cfgPath = Path.Combine(lfsDir, "cfg.txt");
            try
            {
                string content = "";
                if (File.Exists(cfgPath))
                {
                    // Remove read-only attribute if set
                    File.SetAttributes(cfgPath, FileAttributes.Normal);
                    content = File.ReadAllText(cfgPath);
                }

                // Update or append user_name
                if (Regex.IsMatch(content, @"(?m)^user_name\s+.*$"))
                    content = Regex.Replace(content, @"(?m)^user_name\s+.*$", "user_name " + username.ToLower());
                else
                    content += "\r\nuser_name " + username.ToLower();

                // Update or append user_pass
                if (Regex.IsMatch(content, @"(?m)^user_pass\s+.*$"))
                    content = Regex.Replace(content, @"(?m)^user_pass\s+.*$", "user_pass " + password);
                else
                    content += "\r\nuser_pass " + password;

                // Update or append usemaster yes
                if (Regex.IsMatch(content, @"(?m)^usemaster\s+.*$"))
                    content = Regex.Replace(content, @"(?m)^usemaster\s+.*$", "usemaster yes");
                else
                    content += "\r\nusemaster yes";

                File.WriteAllText(cfgPath, content);
            }
            catch { }
        }

        public static bool TryAddHostsEntry()
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                if (File.Exists(hostsPath))
                {
                    string text = File.ReadAllText(hostsPath);
                    if (!text.Contains("m.lfspro.net"))
                    {
                        File.AppendAllText(hostsPath, "\r\n127.0.0.1 m.lfspro.net\r\n");
                        return true;
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static Process LaunchLfsProcess(string lfsPath, string username = null, string password = null, string masterHost = null, string extraArgs = "")
        {
            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
                lfsPath = FindLfsExecutable();

            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
                return null;

            string lfsDir = Path.GetDirectoryName(lfsPath);

            // Update cfg.txt with credentials & usemaster
            if (!string.IsNullOrEmpty(username))
            {
                UpdateLfsConfig(lfsDir, username, password ?? "");
                
                // Create player profile files BEFORE launching
                SetupPlayerProfile(lfsDir, username.ToLower());
            }

            // Attempt to ensure hosts entry
            TryAddHostsEntry();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = lfsPath,
                    Arguments = (extraArgs ?? "").Trim(),
                    WorkingDirectory = lfsDir,
                    UseShellExecute = true
                };

                Process proc = Process.Start(psi);

                // Multi-stage memory injection to beat any race condition
                if (proc != null && !string.IsNullOrEmpty(username))
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        int[] delays = new int[] { 50, 150, 400, 800, 1200 };
                        foreach (int delay in delays)
                        {
                            try
                            {
                                Thread.Sleep(delay);
                                if (!proc.HasExited)
                                {
                                    LFSInjector.InjectCredentials(proc, username, password, masterHost ?? "127.0.0.1");
                                }
                            }
                            catch { }
                        }
                    });
                }

                return proc;
            }
            catch
            {
                return null;
            }
        }

        public static bool LaunchLfs(string lfsPath, string username = null, string password = null, string masterHost = null, string extraArgs = "")
        {
            return LaunchLfsProcess(lfsPath, username, password, masterHost, extraArgs) != null;
        }
    }
}
