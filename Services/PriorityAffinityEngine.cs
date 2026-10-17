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
            "Discord", "Spotify", "steamwebhelper", "EpicGamesLauncher", "RiotClientServices", "slack", "teams"
        };

        public bool OptimizeGameProcess(Process process, bool enablePCores, bool enableHighPriority)
        {
            if (process == null) return false;

            try
            {
                bool hasExited = false;
                try { hasExited = process.HasExited; } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                if (hasExited) return false;

                int pid = process.Id;
                string pName = "";
                try { pName = (process.ProcessName ?? "").ToLower(); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                bool isEmulator = pName.Contains("hd-player") || pName.Contains("bluestacks") || pName.Contains("bstksvc");
                bool isValorant = pName.Contains("valorant");

                // 1. Save original states if not already saved
                if (!_originalPriorities.ContainsKey(pid))
                {
                    try { _originalPriorities[pid] = process.PriorityClass; } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                }
                if (!_originalAffinities.ContainsKey(pid))
                {
                    try { _originalAffinities[pid] = process.ProcessorAffinity; } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                }

                // 2. Dynamic Priority Scheduling
                try
                {
                    // Enable Windows priority boost for rapid response to input events
                    process.PriorityBoostEnabled = true;
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

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
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

                    // BlueStacks needs ALL threads for vCPUs, graphics translation, and virtualization.
                    // NEVER constrain affinity mask on emulators!
                    return true;
                }

                // For Valorant: Vanguard anti-cheat kernel driver and Windows raw input threads (csrss/dwm)
                // must never be starved. ProcessPriorityClass.AboveNormal gives highest game responsiveness
                // without anti-cheat timeouts (VAN 1067) or input drops.
                if (isValorant)
                {
                    try
                    {
                        if (process.PriorityClass != ProcessPriorityClass.AboveNormal)
                        {
                            process.PriorityClass = ProcessPriorityClass.AboveNormal;
                        }
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

                    // Deprioritize Riot Client launcher during active match
                    try
                    {
                        Process[] riotLaunchers = Process.GetProcessesByName("RiotClientServices");
                        if (riotLaunchers != null)
                        {
                            foreach (Process rp in riotLaunchers)
                            {
                                try
                                {
                                    if (!_throttledProcesses.Contains(rp.Id))
                                    {
                                        _throttledProcesses.Add(rp.Id);
                                        rp.PriorityClass = ProcessPriorityClass.BelowNormal;
                                    }
                                }
                                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                                finally { rp.Dispose(); }
                            }
                        }
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

                    return true;
                }

                // For Native PC Games (LoL, Tekken, etc.)
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
                        try { process.PriorityClass = ProcessPriorityClass.AboveNormal; } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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
                    try { process.PriorityClass = _originalPriorities[pid]; } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                    _originalPriorities.Remove(pid);
                }

                if (_originalAffinities.ContainsKey(pid))
                {
                    try { process.ProcessorAffinity = _originalAffinities[pid]; } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                    _originalAffinities.Remove(pid);
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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
                                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                                finally
                                {
                                    p.Dispose();
                                }
                            }
                        }
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                }
                _throttledProcesses.Clear();
            }
        }
    }
}
