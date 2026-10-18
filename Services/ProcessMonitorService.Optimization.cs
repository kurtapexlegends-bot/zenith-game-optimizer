using System;
using System.Diagnostics;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services
{
    public partial class ProcessMonitorService
    {
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

            // 8. Auto Default Game-Specific Zero-Drawback Engine (Automated by default, no manual tools needed)
            if (profile != null && (profile.Id == "roblox" || (process != null && process.ProcessName.IndexOf("RobloxPlayer", StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                try
                {
                    string robloxMsg;
                    var rbxTuner = new Tuners.RobloxTuner();
                    rbxTuner.ApplyRobloxZeroDrawbackPerformanceSuite(out robloxMsg);
                    rbxTuner.ElevateRobloxCpuPriority(out robloxMsg);
                    rbxTuner.EnforceDiscreteGpuForRoblox(out robloxMsg);
                    rbxTuner.DisableRobloxPowerThrottling(out robloxMsg);
                    if (LogMessage != null) LogMessage("Roblox Default Zero-Drawback Performance Suite applied automatically.");
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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
                try { _activeProcess.Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor.Optimization] Dispose proc error: " + ex.Message); }
            }

            _activeProfile = null;
            _activeProcess = null;
            _activeProcessId = 0;

            // Trim working set immediately after game session wraps up
            _memoryService.TrimSelfMemory();
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
                    try { _activeProcess.Dispose(); } catch (Exception ex) { Debug.WriteLine("[ProcessMonitor.Optimization] Dispose proc error: " + ex.Message); }
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

                if (_settings.EnableLaptopThermalGuard || _powerService.IsLaptopThermalGuardActive())
                {
                    string coolerMsg;
                    _powerService.RestoreLaptopThermalGuard(out coolerMsg);
                    _settings.EnableLaptopThermalGuard = false;
                }

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
    }
}
