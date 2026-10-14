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

        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

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

        // Idle background apps that are safe to trim to free RAM for games
        private static readonly string[] SafeBackgroundAppsToTrim = new string[]
        {
            "chrome", "msedge", "firefox", "brave", "opera",
            "Discord", "Spotify", "steamwebhelper", "EpicGamesLauncher",
            "RiotClientServices", "slack", "teams", "battle.net", "origin", "eaBackgroundService", "galaxyclient"
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
            catch { }
        }

        public long PurgeSafeBackgroundMemory()
        {
            long freedBytes = 0;
            try
            {
                // Only trim known memory-heavy background applications
                foreach (string appName in SafeBackgroundAppsToTrim)
                {
                    try
                    {
                        Process[] procs = Process.GetProcessesByName(appName);
                        if (procs != null)
                        {
                            foreach (Process p in procs)
                            {
                                try
                                {
                                    long before = p.WorkingSet64;
                                    EmptyWorkingSet(p.Handle);
                                    p.Refresh();
                                    long after = p.WorkingSet64;
                                    if (before > after)
                                    {
                                        freedBytes += (before - after);
                                    }
                                }
                                catch { }
                                finally
                                {
                                    p.Dispose();
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Flush system standby cache if permissions allow
                FlushWindowsStandbyList();

                // Trim Zenith's own working set
                TrimSelfMemory();
            }
            catch { }

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
            catch { }
        }

        public static long PurgeStandbyList()
        {
            FlushWindowsStandbyList();
            var service = new MemoryPurgeService();
            return service.PurgeSafeBackgroundMemory();
        }
    }
}
