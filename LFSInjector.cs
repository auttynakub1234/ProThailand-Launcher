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
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_VM_WRITE = 0x0020;

        // LFS 0.6V Memory Offsets (32-bit PE, ImageBase = 0x400000)
        public const int OFFSET_MASTER_SERVER = 0x1C890C;    // "m.lfspro.net" (16 bytes)
        public const int OFFSET_SKINS_SERVER = 0x1C6CD4;     // "skins.lfs.net" -> redirect (16 bytes)
        public const int OFFSET_HTTP_LFSNET = 0x16FBE4;      // "lfs.net" -> redirect (8 bytes)
        public const int OFFSET_UPDATER = 0x16FBD0;          // "updates.lfs.net" -> redirect (16 bytes)
        public const int OFFSET_USERNAME = 0x5B0AA4;         // Master Username Buffer (24 bytes)
        public const int OFFSET_PASSWORD = 0x5B0A8C;         // Master Password Buffer (24 bytes)
        
        // Critical memory offsets for player name display (verified from task)
        public const int OFFSET_PROFILE_NAME = 0x3B9CF4;     // Currently loaded player profile name (24 bytes)
        public const int OFFSET_ACTIVE_PROFILE = 0x7B9CF4;   // Active Player Profile Name buffer (24 bytes) [from task]
        public const int OFFSET_INGAME_NAME = 0x229214;      // Active In-game Display Name (32 bytes) [from task]
        public const int OFFSET_INGAME_NAME_V2 = 0x629214;   // Active In-game Name buffer (32 bytes) [alternative from task]
        
        // Cached Winsock Master Socket Structure in LFS memory
        public const int OFFSET_MASTER_FAMILY = 0x406FC8;    // AF_INET = 2 (2 bytes)
        public const int OFFSET_MASTER_IP = 0x406FCC;        // in_addr: 127.0.0.1 = 0x0100007F (4 bytes)
        public const int OFFSET_MASTER_ERR = 0x406FB8;       // Error flag: 0 = OK (4 bytes)

        /// <summary>
        /// Comprehensive memory injection for username display and authentication
        /// Injects into ALL critical memory locations for in-game name display
        /// </summary>
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

                // Prepare username bytes (ASCII, null-terminated, max 24 chars)
                byte[] userBytes = new byte[24];
                if (!string.IsNullOrEmpty(username))
                {
                    string cleanUser = username.Trim().ToLower();
                    byte[] rawUser = Encoding.ASCII.GetBytes(cleanUser);
                    Array.Copy(rawUser, userBytes, Math.Min(rawUser.Length, 23));
                }

                // Prepare password bytes
                byte[] passBytes = new byte[24];
                if (!string.IsNullOrEmpty(password))
                {
                    byte[] rawPass = Encoding.ASCII.GetBytes(password);
                    Array.Copy(rawPass, passBytes, Math.Min(rawPass.Length, 23));
                }

                // ========== CRITICAL: Inject username into ALL name buffers ==========
                
                // 1. Master Username Buffer (VA 0x9B0AA4) - for authentication
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_USERNAME), userBytes, userBytes.Length, out written);

                // 2. Active Player Profile Name (VA 0x7B9CF4) - MOST IMPORTANT for profile loading
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_ACTIVE_PROFILE), userBytes, userBytes.Length, out written);

                // 3. Currently loaded player profile name (VA 0x3B9CF4) - secondary profile buffer
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_PROFILE_NAME), userBytes, userBytes.Length, out written);

                // 4. In-game Display Name (VA 0x629214) - Primary in-game display (from task)
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_INGAME_NAME_V2), userBytes, userBytes.Length, out written);

                // 5. Alternative In-game Name buffer (VA 0x229214) - backup display buffer
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_INGAME_NAME), userBytes, userBytes.Length, out written);

                // ========== Inject password ==========
                if (!string.IsNullOrEmpty(password))
                {
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_PASSWORD), passBytes, passBytes.Length, out written);
                }

                // ========== Redirect master server domains ==========
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

                // ========== Force Winsock cached IP to 127.0.0.1 & Clear error flag ==========
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
