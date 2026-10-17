using System;

namespace ZenithOptimizer.Services
{
    public partial class GameSpecificTunerService
    {
        public string GetCs2LaunchOptions() { return _otherGames.GetCs2LaunchOptions(); }
        public string GetCs2AutoExecEsports() { return _otherGames.GetCs2AutoExecEsports(); }
        public string GetCs2SafetyStatus() { return _otherGames.GetCs2SafetyStatus(); }
        public bool ForceCs2HighPerformanceGpu(out string m) { return _otherGames.ForceCs2HighPerformanceGpu(out m); }
        public bool DisableFullscreenOptimizationsForCs2(out string m) { return _otherGames.DisableFullscreenOptimizationsForCs2(out m); }
        public bool OptimizeNetworkMtuValidation(out string m) { return _otherGames.OptimizeNetworkMtuValidation(out m); }
        public bool CleanValveCrashHandlerArtifacts(out long b, out string m) { return _otherGames.CleanValveCrashHandlerArtifacts(out b, out m); }
        public string GetCs2CrispAudioEqGuide() { return _otherGames.GetCs2CrispAudioEqGuide(); }
        public bool ApplyCs2ProArenaPackage(out string m) { return _otherGames.ApplyCs2ProArenaPackage(out m); }
        public bool DeployRunJumpThrowBinding(out string m) { return _otherGames.DeployRunJumpThrowBinding(out m); }
        public bool DeployInstantBombDropBinding(out string m) { return _otherGames.DeployInstantBombDropBinding(out m); }
        public bool DeploySubTickDecalsAndAudioNormalizer(out string m) { return _otherGames.DeploySubTickDecalsAndAudioNormalizer(out m); }
        public bool DeployRadarZoomToggleBinding(out string m) { return _otherGames.DeployRadarZoomToggleBinding(out m); }
        public bool DeployViewmodelSteadyBinding(out string m) { return _otherGames.DeployViewmodelSteadyBinding(out m); }
        public bool CleanCs2WorkshopCustomAssets(out long b, out string m) { return _otherGames.CleanCs2WorkshopCustomAssets(out b, out m); }
        public bool DeploySubTickInterpClamping(out string m) { return _otherGames.DeploySubTickInterpClamping(out m); }
        public string Get4By3StretchedSetupGuide() { return _otherGames.Get4By3StretchedSetupGuide(); }
        public string AuditVacSessionIntegrity() { return _otherGames.AuditVacSessionIntegrity(); }
        public bool ApplyCs2MajorFinalPreArm(out string m) { return _otherGames.ApplyCs2MajorFinalPreArm(out m); }
        public bool ElevateCs2IoPriority(out string m) { return _otherGames.ElevateCs2IoPriority(out m); }
        public bool DisableCs2PowerThrottling(out string m) { return _otherGames.DisableCs2PowerThrottling(out m); }
        public bool ConfigureCs2NetworkDscpQoS(out string m) { return _otherGames.ConfigureCs2NetworkDscpQoS(out m); }
        public bool ProtectCs2WorkingSetMemory(out string m) { return _otherGames.ProtectCs2WorkingSetMemory(out m); }
        public bool DisableCs2StickyKeysAccessibility(out string m) { return _otherGames.DisableCs2StickyKeysAccessibility(out m); }
        public bool FlushCs2NetworkDns(out string m) { return _otherGames.FlushCs2NetworkDns(out m); }
        public string GetMinecraftJvmFlags(int r = 4) { return _otherGames.GetMinecraftJvmFlags(r); }
        public bool CleanMinecraftLogs(out long b, out string m) { return _otherGames.CleanMinecraftLogs(out b, out m); }
        public string GetChunkLoadingOptimizationTip() { return _otherGames.GetChunkLoadingOptimizationTip(); }
        public string GetDisableExplicitGcTip() { return _otherGames.GetDisableExplicitGcTip(); }
        public int GetRecommendedParallelGcThreads() { return _otherGames.GetRecommendedParallelGcThreads(); }
        public bool PrioritizeMinecraftProcess(out string m) { return _otherGames.PrioritizeMinecraftProcess(out m); }
        public string GetSodiumOptimizationGuide() { return _otherGames.GetSodiumOptimizationGuide(); }
        public string AuditMinecraftInstallation() { return _otherGames.AuditMinecraftInstallation(); }
        public bool LockMinecraftInputTimer(out string m) { return _otherGames.LockMinecraftInputTimer(out m); }
        public bool CleanJavaErrorLogs(out long b, out string m) { return _otherGames.CleanJavaErrorLogs(out b, out m); }
        public bool ApplyMinecraftLaptopPreset(out string m) { return _otherGames.ApplyMinecraftLaptopPreset(out m); }
        public bool ApplyMinecraftModdedPreset(out string m) { return _otherGames.ApplyMinecraftModdedPreset(out m); }
        public string DetectJavaVersion() { return _otherGames.DetectJavaVersion(); }
        public string GetOptiFineNote() { return _otherGames.GetOptiFineNote(); }
        public string GetMemoryAllocationSafetyTip() { return _otherGames.GetMemoryAllocationSafetyTip(); }
        public bool ForceMinecraftHighPerformanceGpu(out string m) { return _otherGames.ForceMinecraftHighPerformanceGpu(out m); }
        public bool DisableStickyKeysShortcuts(out string m) { return _otherGames.DisableStickyKeysShortcuts(out m); }
        public string GetShenandoahGcFlags(int r = 6) { return _otherGames.GetShenandoahGcFlags(r); }
        public string GetZgcUltraLowLatencyFlags(int r = 8) { return _otherGames.GetZgcUltraLowLatencyFlags(r); }
        public bool LockMinecraftAffinityToPCores(out string m) { return _otherGames.LockMinecraftAffinityToPCores(out m); }
        public bool CleanMinecraftOldBackups(out long b, out string m) { return _otherGames.CleanMinecraftOldBackups(out b, out m); }
        public bool CleanMinecraftCachedAssets(out long b, out string m) { return _otherGames.CleanMinecraftCachedAssets(out b, out m); }
        public string AuditInstalledModLoaders() { return _otherGames.AuditInstalledModLoaders(); }
        public bool FlushMinecraftMultiplayerDns(out string m) { return _otherGames.FlushMinecraftMultiplayerDns(out m); }
        public bool ApplyMinecraftPvPPreset(out string m) { return _otherGames.ApplyMinecraftPvPPreset(out m); }
        public bool CleanFortniteCache(out long b, out string m) { return _otherGames.CleanFortniteCache(out b, out m); }
        public string GetApexLaunchOptions() { return _otherGames.GetApexLaunchOptions(); }
        public bool CleanUnityShaderCache(out long b, out string m) { return _otherGames.CleanUnityShaderCache(out b, out m); }
        public string GetDota2LaunchOptions() { return _otherGames.GetDota2LaunchOptions(); }
        public bool ForceDedicatedGpuForGame(string exe, out string m) { return _otherGames.ForceDedicatedGpuForGame(exe, out m); }
        public bool DisableFullscreenOptimizationsForGame(string exe, out string m) { return _otherGames.DisableFullscreenOptimizationsForGame(exe, out m); }
        public bool PinProcessAffinityToPCores(string p, out string m) { return _otherGames.PinProcessAffinityToPCores(p, out m); }
        public bool IsolateAudioEngineThread(out string m) { return _otherGames.IsolateAudioEngineThread(out m); }
        public bool CleanDirectXAndVulkanCaches(out long b, out string m) { return _otherGames.CleanDirectXAndVulkanCaches(out b, out m); }
        public bool FlushDnsForOnlineMatch(out string m) { return _otherGames.FlushDnsForOnlineMatch(out m); }
        public bool ConfigureLowLatencyNetworkScheduler(out string m) { return _otherGames.ConfigureLowLatencyNetworkScheduler(out m); }
        public bool PurgeStandbyRamBeforeMatch(out long b, out string m) { return _otherGames.PurgeStandbyRamBeforeMatch(out b, out m); }
        public string ValidateAntiCheatSafeProfile(string g) { return _otherGames.ValidateAntiCheatSafeProfile(g); }
        public bool ApplyUniversalEsportsPreArm(string prof, string exe, out string m) { return _otherGames.ApplyUniversalEsportsPreArm(prof, exe, out m); }
        public bool Optimize189PvPHitRegistration(out string m) { return _otherGames.Optimize189PvPHitRegistration(out m); }
        public bool DisableStickyKeysAccessibilityPopups(out string m) { return _otherGames.DisableStickyKeysAccessibilityPopups(out m); }
        public bool ConfigureLowFogAndParticlesMode(out string m) { return _otherGames.ConfigureLowFogAndParticlesMode(out m); }
        public string CalculateDynamicJvmHeap() { return _otherGames.CalculateDynamicJvmHeap(); }
        public bool PrioritizeChunkWorkerThreads(out string m) { return _otherGames.PrioritizeChunkWorkerThreads(out m); }
        public bool CleanMinecraftOldScreenshotsAndCrashes(out long b, out string m) { return _otherGames.CleanMinecraftOldScreenshotsAndCrashes(out b, out m); }
        public bool EnableFullbrightGammaHack(out string m) { return _otherGames.EnableFullbrightGammaHack(out m); }
        public bool DisableCaveAmbientSoundSpikes(out string m) { return _otherGames.DisableCaveAmbientSoundSpikes(out m); }
        public string AuditSystemJavaRuntimeArchitecture() { return _otherGames.AuditSystemJavaRuntimeArchitecture(); }
        public bool ApplyHypixelPvPArenaPreArm(out string m) { return _otherGames.ApplyHypixelPvPArenaPreArm(out m); }
        public bool ConfigureFortnitePerformanceMode(out string m) { return _otherGames.ConfigureFortnitePerformanceMode(out m); }
        public string ConfigureApexSuperglideFpsCap() { return _otherGames.ConfigureApexSuperglideFpsCap(); }
        public bool DeployDota2FastRightClickAttack(out string m) { return _otherGames.DeployDota2FastRightClickAttack(out m); }
        public bool OptimizeGenshinStarRailPacing(out string m) { return _otherGames.OptimizeGenshinStarRailPacing(out m); }
        public string GetUniversalCrosshairContrastGuide() { return _otherGames.GetUniversalCrosshairContrastGuide(); }
        public bool ThrottleBackgroundBrowsersDuringMatch(out string m) { return _otherGames.ThrottleBackgroundBrowsersDuringMatch(out m); }
        public bool StabilizeAudioEngineAffinity(out string m) { return _otherGames.StabilizeAudioEngineAffinity(out m); }
        public bool ConfigureRouterQoSDscpTagging(out string m) { return _otherGames.ConfigureRouterQoSDscpTagging(out m); }
        public bool PurgeUncompressedStandbyRam(out long b, out string m) { return _otherGames.PurgeUncompressedStandbyRam(out b, out m); }
        public bool ApplyUniversalEsportsTournamentPreArm(string prof, string exe, out string m) { return _otherGames.ApplyUniversalEsportsTournamentPreArm(prof, exe, out m); }
        public bool ElevateMinecraftIoPriority(out string m) { return _otherGames.ElevateMinecraftIoPriority(out m); }
        public bool DisableMinecraftPowerThrottling(out string m) { return _otherGames.DisableMinecraftPowerThrottling(out m); }
        public bool ConfigureMinecraftNetworkDscpQoS(out string m) { return _otherGames.ConfigureMinecraftNetworkDscpQoS(out m); }
        public bool ProtectMinecraftWorkingSetMemory(out string m) { return _otherGames.ProtectMinecraftWorkingSetMemory(out m); }
        public bool CleanMinecraftTemporaryCaches(out long b, out string m) { return _otherGames.CleanMinecraftTemporaryCaches(out b, out m); }
    }
}
