using System;

namespace ZenithOptimizer.Models
{
    public class OptimizationSettings
    {
        public bool AutoOptimizeOnGameLaunch { get; set; }
        public bool EnableHighPrecisionTimer { get; set; }
        public bool EnablePCoreAffinity { get; set; }
        public bool EnableHighPriority { get; set; }
        public bool EnableBackgroundThrottling { get; set; }
        public bool EnableAutoStandbyPurge { get; set; }
        public bool EnablePowerPlanSwitching { get; set; }
        public bool EnableNetworkOptimization { get; set; }
        public bool DisableGameDVR { get; set; }
        public bool EnableAudioStabilization { get; set; }
        public bool EnableGpuPriority { get; set; }
        public bool EnableWindowsGameMode { get; set; }
        public bool MinimizeToSystemTray { get; set; }

        public OptimizationSettings()
        {
            AutoOptimizeOnGameLaunch = true;
            EnableHighPrecisionTimer = true;
            EnableHighPriority = true;
            EnablePCoreAffinity = false; // Safe default for mobile/hybrid CPUs & emulators
            EnableBackgroundThrottling = false; // Safe default
            EnableAutoStandbyPurge = false; // Safe default: NEVER purge RAM while game/VM is loading
            EnablePowerPlanSwitching = true;
            EnableNetworkOptimization = false;
            DisableGameDVR = true; // Turn off background screen recorder to save GPU encoder
            EnableAudioStabilization = true; // Safe priority elevation for audiodg.exe to prevent sound hitching
            EnableGpuPriority = false; // DirectX MMCSS scheduling (user opt-in)
            EnableWindowsGameMode = true;
            MinimizeToSystemTray = true;
        }
    }
}
