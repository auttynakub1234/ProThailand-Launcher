using System;
using System.IO;
using System.Diagnostics;

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

        public static bool LaunchLfs(string lfsPath, string args = "")
        {
            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
                lfsPath = FindLfsExecutable();

            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
                return false;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = lfsPath,
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(lfsPath),
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
