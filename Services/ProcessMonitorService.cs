using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public partial class ProcessMonitorService : IDisposable
    {
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
        private int _idleTrimCounter = 0;

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
            _settings = OptimizationSettings.Load();

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
            profile.IsInstalled = true;
            _profiles.Add(profile);

            if (LogMessage != null)
            {
                LogMessage("Added custom profile for: " + name);
            }
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
                        catch (Exception ex)
                        {
                            Debug.WriteLine("[ProcessMonitor] HasExited check error: " + ex.Message);
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
                                        try { procs[i].Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor] Dispose proc: " + ex.Message); }
                                    }

                                    HandleGameStarted(_activeProfile, gameProc);
                                    break;
                                }
                                else if (procs != null)
                                {
                                    for (int i = 0; i < procs.Length; i++)
                                    {
                                        try { procs[i].Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor] Dispose proc: " + ex.Message); }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("[ProcessMonitor] Pre-armed process scan error: " + ex.Message);
                            }
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
                                        try { procs[i].Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor] Dispose proc: " + ex.Message); }
                                    }

                                    HandleGameStarted(profile, gameProc);
                                    break;
                                }
                                else if (procs != null)
                                {
                                    for (int i = 0; i < procs.Length; i++)
                                    {
                                        try { procs[i].Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor] Dispose proc: " + ex.Message); }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("[ProcessMonitor] Auto-optimize scan error: " + ex.Message);
                            }
                        }
                        if (_activeProfile != null) break;
                    }
                }

                // 3. Periodic idle memory trim (every 15s when no game is active to maintain sub-25MB working set)
                if (_activeProfile == null)
                {
                    _idleTrimCounter++;
                    if (_idleTrimCounter >= 15)
                    {
                        _idleTrimCounter = 0;
                        _memoryService.TrimSelfMemory();
                    }
                }
                else
                {
                    _idleTrimCounter = 0;
                }

                // 4. Collect Telemetry with zero CPU overhead
                CollectTelemetry();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ProcessMonitor] OnMonitorTick error: " + ex.Message);
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            try { _monitorTimer.Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor] Monitor timer dispose error: " + ex.Message); }
            try { _timerEngine.Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor] Timer engine dispose error: " + ex.Message); }
            if (_activeProcess != null)
            {
                try { _activeProcess.Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor] Active process dispose error: " + ex.Message); }
                _activeProcess = null;
            }
            RestoreAllDefaults();
        }
    }
}
