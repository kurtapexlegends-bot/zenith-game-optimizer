using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services
{
    public partial class ProcessMonitorService
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(
            out System.Runtime.InteropServices.ComTypes.FILETIME idleTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME kernelTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME userTime);

        // Fast CPU calculation state
        private long _prevIdleTime = 0;
        private long _prevTotalTime = 0;
        private double _cachedCpuUsage = 0.0;
        private string _cachedPowerPlan = null;
        private int _telemetryTickCount = 0;

        public List<RunningAppInfo> GetRunningUserApplications()
        {
            var list = new List<RunningAppInfo>();
            try
            {
                Process[] all = Process.GetProcesses();
                if (all != null)
                {
                    foreach (Process p in all)
                    {
                        try
                        {
                            if (p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(p.MainWindowTitle))
                            {
                                string pName = p.ProcessName;
                                if (!pName.Equals("explorer", StringComparison.OrdinalIgnoreCase) &&
                                    !pName.Equals("ZenithOptimizer", StringComparison.OrdinalIgnoreCase) &&
                                    !pName.Equals("ZenithTests", StringComparison.OrdinalIgnoreCase) &&
                                    !pName.Equals("ShellExperienceHost", StringComparison.OrdinalIgnoreCase) &&
                                    !pName.Equals("SearchHost", StringComparison.OrdinalIgnoreCase) &&
                                    !pName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) &&
                                    !pName.Equals("Taskmgr", StringComparison.OrdinalIgnoreCase))
                                {
                                    list.Add(new RunningAppInfo
                                    {
                                        ProcessName = pName,
                                        WindowTitle = p.MainWindowTitle,
                                        ProcessId = p.Id
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("[ProcessMonitor.Telemetry] App inspect error: " + ex.Message);
                        }
                        finally
                        {
                            p.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ProcessMonitor.Telemetry] GetRunningUserApplications error: " + ex.Message);
            }
            return list;
        }

        private void CollectTelemetry()
        {
            var telem = new SystemTelemetry();

            // Fast CPU load
            telem.CpuUsagePercent = GetCpuUsageFast();

            // CPU & GPU names
            telem.CpuName = _systemTweaks.GetCpuName();
            telem.GpuName = _systemTweaks.GetGpuName();

            // RAM
            double totalGb, freeGb, usedGb, usedPercent;
            _memoryService.GetMemoryMetrics(out totalGb, out freeGb, out usedGb, out usedPercent);
            telem.TotalRamGb = totalGb;
            telem.FreeRamGb = freeGb;
            telem.UsedRamGb = usedGb;
            telem.RamUsagePercent = usedPercent;

            // Timer
            telem.CurrentTimerResolutionMs = _timerEngine.GetCurrentResolutionMs();

            // Power (cached to reduce overhead)
            _telemetryTickCount++;
            if (_cachedPowerPlan == null || _telemetryTickCount % 4 == 0)
            {
                _cachedPowerPlan = _powerService.GetActiveSchemeName();
            }
            telem.ActivePowerPlanName = _cachedPowerPlan;

            // System states
            telem.IsHagsActive = _systemTweaks.GetHagsStatus().Contains("Active");
            telem.IsGameModeActive = _systemTweaks.IsWindowsGameModeEnabled();
            telem.IsGameDvrDisabled = _systemTweaks.IsGameDvrDisabled();

            // Game
            if (_activeProfile != null)
            {
                telem.ActiveGameName = _activeProfile.Name + (_activeProcess == null ? " (Pre-Armed)" : "");
                telem.IsGameRunning = true;
            }
            else
            {
                telem.ActiveGameName = "None (Monitoring Active)";
                telem.IsGameRunning = false;
            }

            if (TelemetryUpdated != null)
            {
                TelemetryUpdated(telem);
            }
        }

        public void TriggerTelemetryRefresh()
        {
            CollectTelemetry();
        }

        private double GetCpuUsageFast()
        {
            try
            {
                System.Runtime.InteropServices.ComTypes.FILETIME idleTime, kernelTime, userTime;
                if (GetSystemTimes(out idleTime, out kernelTime, out userTime))
                {
                    long idle = FileTimeToLong(idleTime);
                    long kernel = FileTimeToLong(kernelTime);
                    long user = FileTimeToLong(userTime);
                    long total = kernel + user;

                    if (_prevTotalTime != 0)
                    {
                        long totalDiff = total - _prevTotalTime;
                        long idleDiff = idle - _prevIdleTime;

                        if (totalDiff > 0)
                        {
                            double usage = (double)(totalDiff - idleDiff) * 100.0 / totalDiff;
                            if (usage < 0.0) usage = 0.0;
                            if (usage > 100.0) usage = 100.0;
                            _cachedCpuUsage = Math.Round(usage, 1);
                        }
                    }

                    _prevIdleTime = idle;
                    _prevTotalTime = total;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ProcessMonitor.Telemetry] GetCpuUsageFast error: " + ex.Message);
            }

            return _cachedCpuUsage;
        }

        private static long FileTimeToLong(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            return ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        }
    }
}
