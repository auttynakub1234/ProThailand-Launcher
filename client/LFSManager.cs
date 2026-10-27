using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace ProThailand
{
    public static class LFSManager
    {
        public static string FindLfsExecutable()
        {
            string[] candidatePaths = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LFS.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "LFS.exe"),
                "/sdcard/reverse-skill/work/newproject/LFS.exe",
                "C:\\LFS\\LFS.exe",
                "C:\\Live for Speed\\LFS.exe"
            };

            foreach (string p in candidatePaths)
            {
                try
                {
                    if (File.Exists(p))
                        return Path.GetFullPath(p);
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
                    content = File.ReadAllText(cfgPath);

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

        public static bool LaunchLfs(string lfsPath, string username = null, string password = null, string masterHost = null, string extraArgs = "")
        {
            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
                lfsPath = FindLfsExecutable();

            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
                return false;

            string lfsDir = Path.GetDirectoryName(lfsPath);

            // Update cfg.txt if credentials provided
            if (!string.IsNullOrEmpty(username))
            {
                UpdateLfsConfig(lfsDir, username, password ?? "");
            }

            // Build launch arguments
            string args = extraArgs ?? "";
            if (!string.IsNullOrEmpty(username))
            {
                args += string.Format(" /user={0} /pass={1} /usemaster=yes", username.ToLower(), password ?? "");
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = lfsPath,
                    Arguments = args.Trim(),
                    WorkingDirectory = lfsDir,
                    UseShellExecute = true
                };

                Process proc = Process.Start(psi);

                // Perform memory injection in background thread once process is initialized
                if (proc != null && !string.IsNullOrEmpty(username))
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        try
                        {
                            Thread.Sleep(800); // Give LFS time to map PE sections
                            LFSInjector.InjectCredentials(proc, username, password, masterHost);
                        }
                        catch { }
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
