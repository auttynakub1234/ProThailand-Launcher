using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ProThailand
{
    public static class LFSInjector
    {
        // Win32 API imports
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_READ      = 0x0010;
        private const uint PROCESS_VM_WRITE     = 0x0020;

        // LFS 0.6V Memory Offsets from BaseAddress (0x400000)
        public const int OFFSET_MASTER_SERVER = 0x1C890C; // "m.lfspro.net" (16 bytes)
        public const int OFFSET_SKINS_SERVER  = 0x1C6CD4; // "skins.lfs.net" -> redirect (16 bytes)
        public const int OFFSET_HTTP_LFSNET   = 0x16FBE4; // "lfs.net" -> redirect (8 bytes)
        public const int OFFSET_UPDATER       = 0x16FBD0; // "updates.lfs.net" -> redirect (16 bytes)
        public const int OFFSET_USERNAME      = 0x5B0AA4; // LFS username buffer (24 bytes)
        public const int OFFSET_PASSWORD      = 0x5B0A8C; // LFS password buffer (24 bytes)
        public const int OFFSET_PROFILE_NAME  = 0x3B9CF4; // Active player profile name (24 bytes)
        public const int OFFSET_INGAME_NAME   = 0x229214; // In-game display name (24 bytes)

        // Cached Winsock Master Socket Structure in LFS memory
        public const int OFFSET_MASTER_FAMILY = 0x406FC8; // AF_INET = 2 (2 bytes)
        public const int OFFSET_MASTER_IP     = 0x406FCC; // in_addr: 127.0.0.1 = 0x0100007F (4 bytes)
        public const int OFFSET_MASTER_ERR    = 0x406FB8; // Error flag: 0 = OK (4 bytes)

        public static bool InjectCredentials(Process proc, string username, string password, string masterHost = null)
        {
            if (proc == null || proc.HasExited)
                return false;

            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, proc.Id);
                if (hProcess == IntPtr.Zero)
                    return false;

                IntPtr baseAddr = proc.MainModule.BaseAddress;
                IntPtr written;

                // 1. Write Username
                if (!string.IsNullOrEmpty(username))
                {
                    byte[] userBytes = new byte[24];
                    byte[] raw = Encoding.ASCII.GetBytes(username.Trim().ToLower());
                    Array.Copy(raw, userBytes, Math.Min(raw.Length, 23));
                    
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_USERNAME), userBytes, userBytes.Length, out written);
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_PROFILE_NAME), userBytes, userBytes.Length, out written);
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_INGAME_NAME), userBytes, userBytes.Length, out written);
                }

                // 2. Write Password
                if (!string.IsNullOrEmpty(password))
                {
                    byte[] passBytes = new byte[24];
                    byte[] raw = Encoding.ASCII.GetBytes(password);
                    Array.Copy(raw, passBytes, Math.Min(raw.Length, 23));
                    
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_PASSWORD), passBytes, passBytes.Length, out written);
                }

                // 3. Redirect master host strings
                string redirectTarget = string.IsNullOrEmpty(masterHost) ? "127.0.0.1" : masterHost.Trim();
                byte[] hostBytes = new byte[16];
                byte[] rawHost = Encoding.ASCII.GetBytes(redirectTarget);
                Array.Copy(rawHost, hostBytes, Math.Min(rawHost.Length, 15));

                // Master Server (m.lfspro.net -> 127.0.0.1)
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_MASTER_SERVER), hostBytes, hostBytes.Length, out written);

                // HTTP host (lfs.net -> 127.0.0.1)
                byte[] shortHost = new byte[8];
                Array.Copy(rawHost, shortHost, Math.Min(rawHost.Length, 7));
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_HTTP_LFSNET), shortHost, shortHost.Length, out written);

                // Skins & Updater redirects
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_SKINS_SERVER), hostBytes, hostBytes.Length, out written);
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_UPDATER), hostBytes, hostBytes.Length, out written);

                // 4. Force Winsock cached IP to 127.0.0.1 (0x0100007F) & Clear error flag
                byte[] afInet = BitConverter.GetBytes((short)2);
                byte[] loopbackIp = new byte[] { 127, 0, 0, 1 };
                byte[] zeroErr = BitConverter.GetBytes((int)0);

                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_MASTER_FAMILY), afInet, afInet.Length, out written);
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_MASTER_IP), loopbackIp, loopbackIp.Length, out written);
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_MASTER_ERR), zeroErr, zeroErr.Length, out written);

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }
    }
}
