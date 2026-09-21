using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZenithOptimizer.Services
{
    public class PriorityAffinityEngine
    {
        private readonly Dictionary<int, ProcessPriorityClass> _originalPriorities = new Dictionary<int, ProcessPriorityClass>();
        private readonly Dictionary<int, IntPtr> _originalAffinities = new Dictionary<int, IntPtr>();
        private readonly List<int> _throttledProcesses = new List<int>();

        private static readonly string[] BackgroundAppsToThrottle = new string[]
        {
            "chrome", "msedge", "firefox", "brave", "opera",
            "Discord", "Spotify", "steamwebhelper", "EpicGamesLauncher"
        };

        public bool OptimizeGameProcess(Process process, bool enablePCores, bool enableHighPriority)
        {
            if (process == null || process.HasExited) return false;

            try
            {
                int pid = process.Id;
                string pName = process.ProcessName.ToLower();
                bool isEmulator = pName.Contains("hd-player") || pName.Contains("bluestacks") || pName.Contains("bstksvc");

                // 1. Save original states if not already saved
                if (!_originalPriorities.ContainsKey(pid))
                {
                    try { _originalPriorities[pid] = process.PriorityClass; } catch { }
                }
                if (!_originalAffinities.ContainsKey(pid))
                {
                    try { _originalAffinities[pid] = process.ProcessorAffinity; } catch { }
                }

                // 2. Dynamic Priority Scheduling
                try
                {
                    // Enable Windows priority boost for rapid response to input events
                    process.PriorityBoostEnabled = true;
                }
                catch { }

                if (isEmulator)
                {
                    // Emulators like BlueStacks need AboveNormal, NOT High.
                    // Setting High starves Desktop Window Manager (dwm.exe) and audio threads, causing stuttering.
                    try
                    {
                        if (process.PriorityClass != ProcessPriorityClass.AboveNormal)
                        {
                            process.PriorityClass = ProcessPriorityClass.AboveNormal;
                        }
                    }
                    catch { }

                    // BlueStacks needs ALL threads for vCPUs, graphics translation, and virtualization.
                    // NEVER constrain affinity mask on emulators!
                    return true;
                }

                // For Native PC Games (Valorant, LoL, Tekken, etc.)
                if (enableHighPriority)
                {
                    try
                    {
                        if (process.PriorityClass != ProcessPriorityClass.High)
                        {
                            process.PriorityClass = ProcessPriorityClass.High;
                        }
                    }
                    catch
                    {
                        try { process.PriorityClass = ProcessPriorityClass.AboveNormal; } catch { }
                    }
                }

                // 3. Performance Core Affinity (Optional, only for non-emulators when explicitly enabled on high-core desktops)
                if (enablePCores)
                {
                    try
                    {
                        int logicalCount = Environment.ProcessorCount;
                        // Only apply if user has a large multi-core desktop (>= 12 threads)
                        if (logicalCount >= 12)
                        {
                            long affinityMask = 0;
                            // Safe mask: enable first 8 logical processors (covers desktop P-cores)
                            for (int i = 0; i < 8 && i < logicalCount; i++)
                            {
                                affinityMask |= (1L << i);
                            }
                            if (affinityMask != 0)
                            {
                                process.ProcessorAffinity = new IntPtr(affinityMask);
                            }
                        }
                    }
                    catch { }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void RestoreGameProcess(Process process)
        {
            if (process == null) return;

            try
            {
                if (process.HasExited) return;
                int pid = process.Id;

                if (_originalPriorities.ContainsKey(pid))
                {
                    try { process.PriorityClass = _originalPriorities[pid]; } catch { }
                    _originalPriorities.Remove(pid);
                }

                if (_originalAffinities.ContainsKey(pid))
                {
                    try { process.ProcessorAffinity = _originalAffinities[pid]; } catch { }
                    _originalAffinities.Remove(pid);
                }
            }
            catch { }
        }

        public void ThrottleBackgroundApps(bool throttle)
        {
            if (throttle)
            {
                foreach (string appName in BackgroundAppsToThrottle)
                {
                    try
                    {
                        Process[] processes = Process.GetProcessesByName(appName);
                        if (processes != null)
                        {
                            foreach (Process p in processes)
                            {
                                try
                                {
                                    if (!_throttledProcesses.Contains(p.Id))
                                    {
                                        _throttledProcesses.Add(p.Id);
                                        p.PriorityClass = ProcessPriorityClass.BelowNormal;
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
            }
            else
            {
                foreach (int pid in _throttledProcesses)
                {
                    try
                    {
                        using (Process p = Process.GetProcessById(pid))
                        {
                            if (!p.HasExited)
                            {
                                p.PriorityClass = ProcessPriorityClass.Normal;
                            }
                        }
                    }
                    catch { }
                }
                _throttledProcesses.Clear();
            }
        }
    }
}
