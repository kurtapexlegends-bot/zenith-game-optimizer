using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZenithOptimizer.Services
{
    public class MemoryPurgeService
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("psapi.dll", EntryPoint = "EmptyWorkingSet")]
        private static extern int EmptyWorkingSetRaw(IntPtr hwProc);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetProcessId(IntPtr Process);

        /// <summary>
        /// Safely trims working set for background helper utilities.
        /// Hardened with root-cause zero-drawback protection: completely blocks working set eviction
        /// on active game engines (Roblox, BlueStacks, League, Valorant, CS2, Tekken) and critical OS processes
        /// to prevent hard page-fault storms, disk thrashing, and asset streaming freezes.
        /// </summary>
        public static int EmptyWorkingSet(IntPtr hwProc)
        {
            if (hwProc == IntPtr.Zero) return 0;
            try
            {
                uint pid = GetProcessId(hwProc);
                if (pid != 0)
                {
                    // Allow self process trim
                    if (pid == (uint)Process.GetCurrentProcess().Id)
                    {
                        return EmptyWorkingSetRaw(hwProc);
                    }

                    try
                    {
                        using (var proc = Process.GetProcessById((int)pid))
                        {
                            string name = proc.ProcessName.ToLowerInvariant();
                            for (int i = 0; i < ProtectedGameAndSystemNames.Length; i++)
                            {
                                if (name.IndexOf(ProtectedGameAndSystemNames[i], StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    // Game or system process protected! Block eviction to prevent page-fault storm.
                                    return 1;
                                }
                            }
                        }
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                }
                return EmptyWorkingSetRaw(hwProc);
            }
            catch
            {
                return 0;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("ntdll.dll")]
        private static extern int NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

        private const int SystemMemoryListInformation = 80;
        private const int MemoryPurgeStandbyList = 4;
        private const int MemoryPurgeLowPriorityStandbyList = 5;

        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        private const string SE_PROFILE_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, ref IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;

        private static bool EnablePrivilege(string privilegeName)
        {
            IntPtr token = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref token))
                    return false;

                LUID luid = new LUID();
                if (!LookupPrivilegeValue(null, privilegeName, ref luid))
                    return false;

                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Privileges.Luid = luid;
                tp.Privileges.Attributes = SE_PRIVILEGE_ENABLED;

                return AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (token != IntPtr.Zero) CloseHandle(token);
            }
        }

        public static bool FlushWindowsStandbyList()
        {
            try
            {
                EnablePrivilege(SE_INCREASE_QUOTA_NAME);
                EnablePrivilege(SE_PROFILE_SINGLE_PROCESS_NAME);

                int command = MemoryPurgeStandbyList;
                GCHandle handle = GCHandle.Alloc(command, GCHandleType.Pinned);
                try
                {
                    int status = NtSetSystemInformation(SystemMemoryListInformation, handle.AddrOfPinnedObject(), Marshal.SizeOf(typeof(int)));
                    return status == 0;
                }
                finally
                {
                    handle.Free();
                }
            }
            catch
            {
                return false;
            }
        }

        // Processes that should NEVER be purged (doing so forces page faults, game hitching, or anti-cheat flags)
        private static readonly string[] ProtectedGameAndSystemNames = new string[]
        {
            "hd-player", "bluestacks", "bstksvc",
            "valorant-win64-shipping", "valorant", "vgc", "vgtray",
            "league of legends", "leagueclientux", "leagueclient",
            "tekkengame-win64-shipping", "tekkengame", "polaris-win64-shipping", "polaris",
            "cs2", "r5apex", "fortniteclient-win64-shipping", "dota2", "robloxplayerbeta",
            "easyanticheat", "beservice",
            "dwm", "explorer", "audiodg", "csrss", "lsass", "smss", "wininit", "services", "system"
        };

        public void GetMemoryMetrics(out double totalGb, out double freeGb, out double usedGb, out double usedPercent)
        {
            totalGb = 16.0;
            freeGb = 8.0;
            usedGb = 8.0;
            usedPercent = 50.0;

            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    totalGb = Math.Round((double)memStatus.ullTotalPhys / (1024 * 1024 * 1024), 1);
                    freeGb = Math.Round((double)memStatus.ullAvailPhys / (1024 * 1024 * 1024), 1);
                    usedGb = Math.Round(totalGb - freeGb, 1);
                    usedPercent = Math.Round((double)memStatus.dwMemoryLoad, 0);
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
        }

        public long PurgeSafeBackgroundMemory()
        {
            long freedBytes = 0;
            try
            {
                MEMORYSTATUSEX before = new MEMORYSTATUSEX();
                GlobalMemoryStatusEx(before);

                // Flush system standby list cache safely (SystemMemoryListInformation) without touching third-party working sets
                FlushWindowsStandbyList();

                // Generational GC and trim Zenith's self-working set
                TrimSelfMemory();

                MEMORYSTATUSEX after = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(after) && after.ullAvailPhys > before.ullAvailPhys)
                {
                    freedBytes = (long)(after.ullAvailPhys - before.ullAvailPhys);
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            return freedBytes;
        }

        public void TrimSelfMemory()
        {
            TrimCurrentProcessMemory();
        }

        public static void TrimCurrentProcessMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                using (Process current = Process.GetCurrentProcess())
                {
                    EmptyWorkingSet(current.Handle);
                    // -1, -1 tells Windows to trim all unneeded pages from our working set
                    SetProcessWorkingSetSize(current.Handle, new IntPtr(-1), new IntPtr(-1));
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
        }

        public static long PurgeStandbyList()
        {
            FlushWindowsStandbyList();
            var service = new MemoryPurgeService();
            return service.PurgeSafeBackgroundMemory();
        }
    }
}
