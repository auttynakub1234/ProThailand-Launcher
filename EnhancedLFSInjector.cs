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

        // LFS 0.6V Memory Offsets (32-bit PE, BaseAddress = 0x400000)
        // RVAs (Offsets from BaseAddress)
        public const int RVA_MASTER_SERVER   = 0x1C890C; // "m.lfspro.net" (16 bytes) -> VA 0x5C890C
        public const int RVA_SKINS_SERVER    = 0x1C6CD4; // "skins.lfs.net" -> redirect (16 bytes) -> VA 0x5C6CD4
        public const int RVA_HTTP_LFSNET     = 0x16FBE4; // "lfs.net" -> redirect (8 bytes) -> VA 0x56FBE4
        public const int RVA_UPDATER         = 0x16FBD0; // "updates.lfs.net" -> redirect (16 bytes) -> VA 0x56FBD0
        public const int RVA_USERNAME        = 0x5B0AA4; // Master Username Buffer (24 bytes) -> VA 0x9B0AA4
        public const int RVA_PASSWORD        = 0x5B0A8C; // Master Password Buffer (24 bytes) -> VA 0x9B0A8C
        public const int RVA_PROFILE_NAME    = 0x3B9CF4; // Active Player Profile Name (24 bytes) -> VA 0x7B9CF4
        public const int RVA_INGAME_NAME     = 0x229214; // Active In-game Display Name (32 bytes) -> VA 0x629214

        // Cached Winsock Master Socket Structure in LFS memory
        public const int RVA_MASTER_FAMILY   = 0x406FC8; // AF_INET = 2 (2 bytes) -> VA 0x806FC8
        public const int RVA_MASTER_IP       = 0x406FCC; // in_addr: 127.0.0.1 = 0x0100007F (4 bytes) -> VA 0x806FCC
        public const int RVA_MASTER_ERR      = 0x406FB8; // Error flag: 0 = OK (4 bytes) -> VA 0x806FB8

        // Backward compatibility alias definitions
        public const int OFFSET_MASTER_SERVER  = RVA_MASTER_SERVER;
        public const int OFFSET_SKINS_SERVER   = RVA_SKINS_SERVER;
        public const int OFFSET_HTTP_LFSNET    = RVA_HTTP_LFSNET;
        public const int OFFSET_UPDATER        = RVA_UPDATER;
        public const int OFFSET_USERNAME       = RVA_USERNAME;
        public const int OFFSET_PASSWORD       = RVA_PASSWORD;
        public const int OFFSET_PROFILE_NAME   = RVA_PROFILE_NAME;
        public const int OFFSET_INGAME_NAME    = RVA_INGAME_NAME;
        public const int OFFSET_MASTER_FAMILY  = RVA_MASTER_FAMILY;
        public const int OFFSET_MASTER_IP      = RVA_MASTER_IP;
        public const int OFFSET_MASTER_ERR     = RVA_MASTER_ERR;

        /// <summary>
        /// Comprehensive memory injection for username display and master server authentication.
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

                // 1. Prepare username bytes (ASCII, null-terminated, max 24 chars)
                byte[] userBytes = new byte[24];
                if (!string.IsNullOrEmpty(username))
                {
                    string cleanUser = username.Trim().ToLower();
                    byte[] rawUser = Encoding.ASCII.GetBytes(cleanUser);
                    Array.Copy(rawUser, userBytes, Math.Min(rawUser.Length, 23));
                }

                // Prepare 32-byte ingame display buffer
                byte[] inGameBytes = new byte[32];
                if (!string.IsNullOrEmpty(username))
                {
                    string cleanUser = username.Trim().ToLower();
                    byte[] rawUser = Encoding.ASCII.GetBytes(cleanUser);
                    Array.Copy(rawUser, inGameBytes, Math.Min(rawUser.Length, 31));
                }

                // 2. Prepare password bytes
                byte[] passBytes = new byte[24];
                if (!string.IsNullOrEmpty(password))
                {
                    byte[] rawPass = Encoding.ASCII.GetBytes(password);
                    Array.Copy(rawPass, passBytes, Math.Min(rawPass.Length, 23));
                }

                // ========== CRITICAL: Inject username into all relevant buffers ==========
                if (!string.IsNullOrEmpty(username))
                {
                    // 1. Master Username Buffer (VA 0x9B0AA4)
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_USERNAME), userBytes, userBytes.Length, out written);
                    WriteProcessMemory(hProcess, (IntPtr)0x9B0AA4, userBytes, userBytes.Length, out written);

                    // 2. Active Player Profile Name (VA 0x7B9CF4)
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_PROFILE_NAME), userBytes, userBytes.Length, out written);
                    WriteProcessMemory(hProcess, (IntPtr)0x7B9CF4, userBytes, userBytes.Length, out written);

                    // 3. Active In-game Display Name (VA 0x629214)
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_INGAME_NAME), inGameBytes, inGameBytes.Length, out written);
                    WriteProcessMemory(hProcess, (IntPtr)0x629214, inGameBytes, inGameBytes.Length, out written);
                }

                // ========== Inject password ==========
                if (!string.IsNullOrEmpty(password))
                {
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_PASSWORD), passBytes, passBytes.Length, out written);
                    WriteProcessMemory(hProcess, (IntPtr)0x9B0A8C, passBytes, passBytes.Length, out written);
                }

                // ========== Redirect master server domains ==========
                string redirectTarget = string.IsNullOrEmpty(masterHost) ? "127.0.0.1" : masterHost.Trim();
                byte[] hostBytes = new byte[16];
                byte[] rawHost = Encoding.ASCII.GetBytes(redirectTarget);
                Array.Copy(rawHost, hostBytes, Math.Min(rawHost.Length, 15));

                // Master Server (m.lfspro.net -> 127.0.0.1)
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_MASTER_SERVER), hostBytes, hostBytes.Length, out written);
                WriteProcessMemory(hProcess, (IntPtr)0x5C890C, hostBytes, hostBytes.Length, out written);

                // HTTP host (lfs.net -> 127.0.0.1)
                byte[] shortHost = new byte[8];
                Array.Copy(rawHost, shortHost, Math.Min(rawHost.Length, 7));
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_HTTP_LFSNET), shortHost, shortHost.Length, out written);

                // Skins & Updater redirects
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_SKINS_SERVER), hostBytes, hostBytes.Length, out written);
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_UPDATER), hostBytes, hostBytes.Length, out written);

                // ========== Force Winsock cached IP to 127.0.0.1 & Clear error flag ==========
                byte[] afInet = BitConverter.GetBytes((short)2);
                byte[] loopbackIp = new byte[] { 127, 0, 0, 1 };
                byte[] zeroErr = BitConverter.GetBytes((int)0);

                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_MASTER_FAMILY), afInet, afInet.Length, out written);
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_MASTER_IP), loopbackIp, loopbackIp.Length, out written);
                WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, RVA_MASTER_ERR), zeroErr, zeroErr.Length, out written);

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
