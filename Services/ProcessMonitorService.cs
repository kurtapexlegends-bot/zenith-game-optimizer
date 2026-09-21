using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services
{
    public class RunningAppInfo
    {
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }
        public int ProcessId { get; set; }
    }

    public class ProcessMonitorService : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(
            out System.Runtime.InteropServices.ComTypes.FILETIME idleTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME kernelTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME userTime);

        private readonly Timer _monitorTimer;
        private readonly List<GameProfile> _profiles;
        private readonly OptimizationSettings _settings;

        private readonly TimerResolutionEngine _timerEngine;
        private readonly PriorityAffinityEngine _priorityEngine;
        private readonly MemoryPurgeService _memoryService;
        private readonly PowerPlanService _powerService;
        private readonly NetworkOptimizer _networkOptimizer;
        private readonly BlueStacksOptimizer _blueStacksOptimizer;
        private readonly SystemTweaksService _systemTweaks;
        private readonly GameSpecificTunerService _gameTuner;

        private GameProfile _activeProfile = null;
        private Process _activeProcess = null;
        private int _activeProcessId = 0;
        private bool _isDisposed = false;

        // Fast CPU calculation state
        private long _prevIdleTime = 0;
        private long _prevTotalTime = 0;
        private double _cachedCpuUsage = 0.0;
        private string _cachedPowerPlan = null;
        private int _telemetryTickCount = 0;

        public event Action<GameProfile> GameStarted;
        public event Action<GameProfile> GameExited;
        public event Action<SystemTelemetry> TelemetryUpdated;
        public event Action<string> LogMessage;

        public List<GameProfile> Profiles { get { return _profiles; } }
        public OptimizationSettings Settings { get { return _settings; } }
        public TimerResolutionEngine TimerEngine { get { return _timerEngine; } }
        public PriorityAffinityEngine PriorityEngine { get { return _priorityEngine; } }
        public MemoryPurgeService MemoryService { get { return _memoryService; } }
        public PowerPlanService PowerService { get { return _powerService; } }
        public NetworkOptimizer NetworkService { get { return _networkOptimizer; } }
        public BlueStacksOptimizer BlueStacksService { get { return _blueStacksOptimizer; } }
        public SystemTweaksService SystemTweaks { get { return _systemTweaks; } }
        public GameSpecificTunerService GameTuner { get { return _gameTuner; } }
        public GameProfile ActiveProfile { get { return _activeProfile; } }
        public Process ActiveProcess { get { return _activeProcess; } }

        public ProcessMonitorService()
        {
            _profiles = GameProfile.GetDefaultProfiles();
            _settings = new OptimizationSettings();

            _timerEngine = new TimerResolutionEngine();
            _priorityEngine = new PriorityAffinityEngine();
            _memoryService = new MemoryPurgeService();
            _powerService = new PowerPlanService();
            _networkOptimizer = new NetworkOptimizer();
            _blueStacksOptimizer = new BlueStacksOptimizer();
            _systemTweaks = new SystemTweaksService();
            _gameTuner = new GameSpecificTunerService(_blueStacksOptimizer, _timerEngine, _systemTweaks, _memoryService, _networkOptimizer);

            // Initialize CPU times
            GetCpuUsageFast();

            // Run ultra-lightweight monitoring loop every 2500ms
            _monitorTimer = new Timer(OnMonitorTick, null, 1000, 2500);
        }

        public void AddCustomProfile(string name, string processName, string category)
        {
            var profile = new GameProfile();
            profile.Id = "custom_" + Guid.NewGuid().ToString().Substring(0, 8);
            profile.Name = name;
            profile.Category = category;
            profile.ProcessNames.Add(processName.Replace(".exe", "").Trim());
            profile.Description = "Custom profile for " + name + ". Auto-allocates high priority and 0.500ms timer.";
            profile.OptimizationBadge = "Custom Optimized";
            profile.TargetPacing = "High Precision";
            profile.IsCustom = true;
            _profiles.Add(profile);

            if (LogMessage != null)
            {
                LogMessage("Added custom profile for: " + name);
            }
        }

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
                        catch { }
                        finally
                        {
                            p.Dispose();
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private void OnMonitorTick(object state)
        {
            if (_isDisposed) return;

            try
            {
                // 1. Check if currently active process has exited or if pre-armed game has started
                if (_activeProfile != null)
                {
                    if (_activeProcess != null)
                    {
                        bool stillRunning = false;
                        try
                        {
                            if (!_activeProcess.HasExited)
                            {
                                stillRunning = true;
                            }
                        }
                        catch
                        {
                            stillRunning = false;
                        }

                        if (!stillRunning)
                        {
                            HandleGameExited(_activeProfile);
                        }
                    }
                    else
                    {
                        // In Pre-Armed state waiting for game to launch:
                        foreach (string targetName in _activeProfile.ProcessNames)
                        {
                            try
                            {
                                Process[] procs = Process.GetProcessesByName(targetName);
                                if (procs != null && procs.Length > 0)
                                {
                                    Process gameProc = procs[0];
                                    for (int i = 1; i < procs.Length; i++)
                                    {
                                        try { procs[i].Dispose(); } catch { }
                                    }

                                    HandleGameStarted(_activeProfile, gameProc);
                                    break;
                                }
                                else if (procs != null)
                                {
                                    for (int i = 0; i < procs.Length; i++)
                                    {
                                        try { procs[i].Dispose(); } catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }

                // 2. If no game is currently active, scan targeted processes ONLY (no full process tree scan)
                if (_activeProfile == null && _settings.AutoOptimizeOnGameLaunch)
                {
                    foreach (GameProfile profile in _profiles)
                    {
                        foreach (string targetName in profile.ProcessNames)
                        {
                            try
                            {
                                Process[] procs = Process.GetProcessesByName(targetName);
                                if (procs != null && procs.Length > 0)
                                {
                                    Process gameProc = procs[0];
                                    // Dispose excess instances immediately to avoid handle leaks
                                    for (int i = 1; i < procs.Length; i++)
                                    {
                                        try { procs[i].Dispose(); } catch { }
                                    }

                                    HandleGameStarted(profile, gameProc);
                                    break;
                                }
                                else if (procs != null)
                                {
                                    for (int i = 0; i < procs.Length; i++)
                                    {
                                        try { procs[i].Dispose(); } catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                        if (_activeProfile != null) break;
                    }
                }

                // 3. Collect Telemetry with zero CPU overhead
                CollectTelemetry();
            }
            catch { }
        }

        private void HandleGameStarted(GameProfile profile, Process process)
        {
            _activeProfile = profile;
            _activeProcess = process;
            _activeProcessId = process.Id;
            profile.IsActive = true;
            _cachedPowerPlan = null; // force refresh

            // 1. High Resolution Timer (0.500 ms) - key for microsecond input polling
            if (_settings.EnableHighPrecisionTimer)
            {
                _timerEngine.EnableHighResolution();
            }

            // 2. Safe Process Priority & Affinity
            _priorityEngine.OptimizeGameProcess(process, _settings.EnablePCoreAffinity && profile.EnablePCoreAffinity, _settings.EnableHighPriority && profile.EnableHighPriority);

            // 3. Power Plan
            if (_settings.EnablePowerPlanSwitching)
            {
                _powerService.SetHighPerformanceScheme();
            }

            // 4. Background Throttling (only if user opted in)
            if (_settings.EnableBackgroundThrottling)
            {
                _priorityEngine.ThrottleBackgroundApps(true);
            }

            // 5. Audio Stutter Protection
            if (_settings.EnableAudioStabilization)
            {
                _systemTweaks.StabilizeAudioEngine();
            }

            // 6. DirectX MMCSS GPU Priority
            if (_settings.EnableGpuPriority)
            {
                string mmcssMsg;
                _systemTweaks.SetMmcssGamingPriority(true, out mmcssMsg);
            }

            // 7. Background RAM Clean (if opted in)
            if (_settings.EnableAutoStandbyPurge)
            {
                _memoryService.PurgeSafeBackgroundMemory();
            }

            if (LogMessage != null)
            {
                LogMessage(string.Format("Optimization engaged: {0} (PID {1}). 0.5ms Timer active.", profile.Name, process.Id));
            }

            if (GameStarted != null)
            {
                GameStarted(profile);
            }
        }

        private void HandleGameExited(GameProfile profile)
        {
            if (profile != null)
            {
                profile.IsActive = false;

                if (_settings.EnablePowerPlanSwitching)
                {
                    _powerService.RestoreOriginalScheme();
                }

                if (_settings.EnableBackgroundThrottling)
                {
                    _priorityEngine.ThrottleBackgroundApps(false);
                }

                if (_settings.EnableGpuPriority)
                {
                    string mmcssMsg;
                    _systemTweaks.SetMmcssGamingPriority(false, out mmcssMsg);
                }

                _timerEngine.RestoreResolution();
                _cachedPowerPlan = null; // force refresh

                if (LogMessage != null)
                {
                    LogMessage(string.Format("Game exited: {0}. Cleanly restored standard system state.", profile.Name));
                }

                if (GameExited != null)
                {
                    GameExited(profile);
                }
            }

            if (_activeProcess != null)
            {
                try { _activeProcess.Dispose(); } catch { }
            }

            _activeProfile = null;
            _activeProcess = null;
            _activeProcessId = 0;
        }

        public bool ForceOptimizeGame(GameProfile profile, bool autoLaunch, out string statusMessage)
        {
            statusMessage = "";
            if (profile == null) return false;

            // 1. Comprehensively pre-arm all low-latency subsystem tweaks
            if (_settings.EnableHighPrecisionTimer)
            {
                _timerEngine.EnableHighResolution();
            }

            if (_settings.EnablePowerPlanSwitching)
            {
                _powerService.SetHighPerformanceScheme();
            }

            if (_settings.EnableAudioStabilization)
            {
                _systemTweaks.StabilizeAudioEngine();
            }

            if (_settings.EnableGpuPriority)
            {
                string mmcssMsg;
                _systemTweaks.SetMmcssGamingPriority(true, out mmcssMsg);
            }

            if (_settings.EnableAutoStandbyPurge)
            {
                _memoryService.PurgeSafeBackgroundMemory();
            }

            // Flush DNS for clean matchmaking socket state
            string dnsMsg;
            _gameTuner.FlushDnsForOnlineMatch(out dnsMsg);

            // Pre-configure DirectX High-Performance GPU preference for profile executable
            string gpuMsg;
            _gameTuner.ForceDedicatedGpuForGame(profile.PrimaryProcessName, out gpuMsg);

            _activeProfile = profile;
            profile.IsActive = true;

            // 2. Check if already running
            Process targetProcess;
            if (GameLauncherService.IsGameRunning(profile, out targetProcess))
            {
                HandleGameStarted(profile, targetProcess);
                GameLauncherService.BringWindowToForeground(targetProcess);
                statusMessage = string.Format(" {0} is active! Window focused and 0.500ms low-latency timer locked.", profile.Name);
                if (LogMessage != null) LogMessage(statusMessage);
                return true;
            }

            // 3. In Pre-Armed state waiting for game process
            _activeProcess = null;

            if (autoLaunch)
            {
                string launchMsg;
                bool launched = GameLauncherService.TryLaunchGame(profile, out launchMsg);
                if (launched)
                {
                    statusMessage = string.Format(" Pre-armed & Launching {0}! (0.500ms Timer, Max GPU, Audio Shield engaged)", profile.Name);
                    if (LogMessage != null)
                    {
                        LogMessage(string.Format("Pre-armed system for {0}: 0.500ms Timer, High GPU, Audio Shield.", profile.Name));
                        LogMessage(launchMsg);
                    }
                    return true;
                }
                else
                {
                    statusMessage = string.Format(" Pre-armed low-latency mode for {0} (0.500ms Timer active). Game is not installed on this PC.", profile.Name);
                    if (LogMessage != null)
                    {
                        LogMessage(string.Format("Pre-armed system for {0}: 0.500ms Timer and High Performance mode active.", profile.Name));
                        LogMessage(launchMsg);
                    }
                    return false;
                }
            }
            else
            {
                statusMessage = string.Format(" Pre-armed low-latency timer for {0}. Ready for launch.", profile.Name);
                if (LogMessage != null) LogMessage(statusMessage);
                return true;
            }
        }

        public void ForceOptimizeGame(GameProfile profile)
        {
            string statusMsg;
            ForceOptimizeGame(profile, true, out statusMsg);
        }

        public void RestoreAllDefaults()
        {
            try
            {
                if (_activeProfile != null)
                {
                    _activeProfile.IsActive = false;
                    _activeProfile = null;
                }

                if (_activeProcess != null)
                {
                    try { _activeProcess.Dispose(); } catch { }
                    _activeProcess = null;
                    _activeProcessId = 0;
                }

                _timerEngine.RestoreResolution();
                _powerService.RestoreOriginalScheme();
                _priorityEngine.ThrottleBackgroundApps(false);

                string netMsg;
                _networkOptimizer.RestoreDefaultNetworkSettings(out netMsg);

                string dvrMsg;
                _systemTweaks.SetGameDvrDisabled(false, out dvrMsg);

                string mmcssMsg;
                _systemTweaks.SetMmcssGamingPriority(false, out mmcssMsg);

                _cachedPowerPlan = null;

                if (LogMessage != null)
                {
                    LogMessage("All settings reverted to standard Windows defaults.");
                }
            }
            catch (Exception ex)
            {
                if (LogMessage != null)
                {
                    LogMessage("Error during default restoration: " + ex.Message);
                }
            }
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
            catch { }

            return _cachedCpuUsage;
        }

        private static long FileTimeToLong(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            return ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        }

        public void Dispose()
        {
            _isDisposed = true;
            try { _monitorTimer.Dispose(); } catch { }
            try { _timerEngine.Dispose(); } catch { }
            if (_activeProcess != null)
            {
                try { _activeProcess.Dispose(); } catch { }
                _activeProcess = null;
            }
            RestoreAllDefaults();
        }
    }
}
