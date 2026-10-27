using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace ProThailand
{
    public static class LFSManager
    {
        private static string _customLfsPath = null;
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prothailand.ini");

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
                try
                {
                    File.WriteAllText(SettingsFile, "LfsPath=" + (value ?? ""));
                }
                catch { }
            }
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

        public static bool LaunchLfs(string lfsPath, string username = null, string password = null, string masterHost = null, string extraArgs = "")
        {
            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
                lfsPath = FindLfsExecutable();

            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
                return false;

            string lfsDir = Path.GetDirectoryName(lfsPath);

            // Update cfg.txt with credentials & usemaster
            if (!string.IsNullOrEmpty(username))
            {
                UpdateLfsConfig(lfsDir, username, password ?? "");
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
                        int[] delays = new int[] { 50, 150, 400, 800 };
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

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
