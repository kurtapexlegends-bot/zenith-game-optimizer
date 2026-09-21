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

        // Processes that should NEVER be purged (doing so forces page faults and game hitching)
        private static readonly string[] ProtectedGameAndSystemNames = new string[]
        {
            "hd-player", "bluestacks", "bstksvc",
            "valorant-win64-shipping", "valorant",
            "league of legends", "leagueclientux", "leagueclient",
            "tekkengame-win64-shipping", "tekkengame", "polaris-win64-shipping", "polaris",
            "cs2", "r5apex", "fortniteclient-win64-shipping", "dota2", "robloxplayerbeta",
            "dwm", "explorer", "audiodg", "csrss", "lsass", "smss", "wininit", "services", "system"
        };

        // Idle background apps that are safe to trim to free RAM for games
        private static readonly string[] SafeBackgroundAppsToTrim = new string[]
        {
            "chrome", "msedge", "firefox", "brave", "opera",
            "Discord", "Spotify", "steamwebhelper", "EpicGamesLauncher"
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

                // Trim Zenith's own working set
                TrimSelfMemory();
            }
            catch { }

            return freedBytes;
        }

        public void TrimSelfMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                using (Process current = Process.GetCurrentProcess())
                {
                    // -1, -1 tells Windows to trim all unneeded pages from our working set
                    SetProcessWorkingSetSize(current.Handle, new IntPtr(-1), new IntPtr(-1));
                }
            }
            catch { }
        }
    }
}
