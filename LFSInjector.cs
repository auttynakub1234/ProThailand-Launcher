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
        private const uint PROCESS_ALL_ACCESS   = 0x001F0FFF;

        // LFS 0.6V Memory Offsets from BaseAddress
        public const int OFFSET_MASTER_SERVER = 0x1C890C; // m.lfspro.net (16 bytes)
        public const int OFFSET_SKINS_SERVER  = 0x1C6CD4; // s.lfspro.net (16 bytes)
        public const int OFFSET_USERNAME      = 0x5B0AA4; // LFS username buffer (24 bytes)
        public const int OFFSET_PASSWORD      = 0x5B0A8C; // LFS password buffer (24 bytes)
        public const int OFFSET_INGAME_NAME   = 0x229214; // In-game display name (24 bytes)

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

                // 1. Write Username
                if (!string.IsNullOrEmpty(username))
                {
                    byte[] userBytes = new byte[24];
                    byte[] raw = Encoding.ASCII.GetBytes(username.Trim().ToLower());
                    Array.Copy(raw, userBytes, Math.Min(raw.Length, 23));
                    IntPtr written;
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_USERNAME), userBytes, userBytes.Length, out written);

                    // Also set in-game player display name
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_INGAME_NAME), userBytes, userBytes.Length, out written);
                }

                // 2. Write Password
                if (!string.IsNullOrEmpty(password))
                {
                    byte[] passBytes = new byte[24];
                    byte[] raw = Encoding.ASCII.GetBytes(password);
                    Array.Copy(raw, passBytes, Math.Min(raw.Length, 23));
                    IntPtr written;
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_PASSWORD), passBytes, passBytes.Length, out written);
                }

                // 3. Write Master Server Host (if specified)
                if (!string.IsNullOrEmpty(masterHost))
                {
                    byte[] hostBytes = new byte[16];
                    byte[] raw = Encoding.ASCII.GetBytes(masterHost.Trim());
                    Array.Copy(raw, hostBytes, Math.Min(raw.Length, 15));
                    IntPtr written;
                    WriteProcessMemory(hProcess, IntPtr.Add(baseAddr, OFFSET_MASTER_SERVER), hostBytes, hostBytes.Length, out written);
                }

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
