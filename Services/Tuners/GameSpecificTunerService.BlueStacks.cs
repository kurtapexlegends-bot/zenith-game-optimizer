using System;

namespace ZenithOptimizer.Services
{
    public partial class GameSpecificTunerService
    {
        public bool CheckHardwareVirtualization(out bool isEn, out string det) { return _blueStacks.CheckHardwareVirtualization(out isEn, out det); }
        public bool CheckVbsConflictStatus(out string det) { return _blueStacks.CheckVbsConflictStatus(out det); }
        public bool VerifyGuestMemoryShield(out string st) { return _blueStacks.VerifyGuestMemoryShield(out st); }
        public string AuditBlueStacksInstances() { return _blueStacks.AuditBlueStacksInstances(); }
        public string AuditAndroidRootStatus() { return _blueStacks.AuditAndroidRootStatus(); }
        public string AuditFairPlayAntiBanSafety() { return _blueStacks.AuditFairPlayAntiBanSafety(); }
        public bool TuneBlueStacksCores(int c, out string m) { return _blueStacks.TuneBlueStacksCores(c, out m); }
        public bool TuneBlueStacksRam(int r, out string m) { return _blueStacks.TuneBlueStacksRam(r, out m); }
        public bool AllocateDedicated8GbRam(out string m) { return _blueStacks.AllocateDedicated8GbRam(out m); }
        public bool EnableHighFpsMode(int fps, out string m) { return _blueStacks.EnableHighFpsMode(fps, out m); }
        public bool Unlock144HzHyperRefresh(out string m) { return _blueStacks.Unlock144HzHyperRefresh(out m); }
        public bool Set90FpsEsportsMode(out string m) { return _blueStacks.Set90FpsEsportsMode(out m); }
        public bool EnableAstcHardwareDecoding(out string m) { return _blueStacks.EnableAstcHardwareDecoding(out m); }
        public bool EnableHardwareAstcTextureDecoding(out string m) { return _blueStacks.EnableHardwareAstcTextureDecoding(out m); }
        public bool SetBlueStacksRenderer(string r, out string m) { return _blueStacks.SetBlueStacksRenderer(r, out m); }
        public bool EnforceBlueStacksDirectXBackend(out string m) { return _blueStacks.EnforceBlueStacksDirectXBackend(out m); }
        public bool ForceNvidiaOpenGlProfileForBlueStacks(out string m) { return _blueStacks.ForceNvidiaOpenGlProfileForBlueStacks(out m); }
        public bool SetVirtualDpiPreset(int dpi, out string m) { return _blueStacks.SetVirtualDpiPreset(dpi, out m); }
        public bool SetDisplayResolutionPreset(int w, int h, out string m) { return _blueStacks.SetDisplayResolutionPreset(w, h, out m); }
        public bool SetUltraWideAspectRatio(int w, int h, out string m) { return _blueStacks.SetUltraWideAspectRatio(w, h, out m); }
        public bool EnforceUltraWide21By9Fov(out string m) { return _blueStacks.EnforceUltraWide21By9Fov(out m); }
        public bool LockBlueStacksWindowAspect16By9(out string m) { return _blueStacks.LockBlueStacksWindowAspect16By9(out m); }
        public bool SetBlueStacksVsync(bool en, out string m) { return _blueStacks.SetBlueStacksVsync(en, out m); }
        public bool SetVirtualJoystickSensitivity(int s, out string m) { return _blueStacks.SetVirtualJoystickSensitivity(s, out m); }
        public bool ConfigureMicroAnalogDeadzone(out string m) { return _blueStacks.ConfigureMicroAnalogDeadzone(out m); }
        public bool SetBlueStacksJoystickDeadzone(int d, out string m) { return _blueStacks.SetBlueStacksJoystickDeadzone(d, out m); }
        public bool SetBlueStacksInputPollingRate(int hz, out string m) { return _blueStacks.SetBlueStacksInputPollingRate(hz, out m); }
        public bool ConfigureJoystickAutoRecenter(out string m) { return _blueStacks.ConfigureJoystickAutoRecenter(out m); }
        public bool ConfigureSkillCancelSwipeZone(out string m) { return _blueStacks.ConfigureSkillCancelSwipeZone(out m); }
        public bool ConfigureMicroAimingSniperSensitivity(out string m) { return _blueStacks.ConfigureMicroAimingSniperSensitivity(out m); }
        public bool TuneAdbFastInputDispatch(out string m) { return _blueStacks.TuneAdbFastInputDispatch(out m); }
        public bool DisableAdbBackgroundListener(out string m) { return _blueStacks.DisableAdbBackgroundListener(out m); }
        public bool IncreaseBlueStacksInputQueue(out string m) { return _blueStacks.IncreaseBlueStacksInputQueue(out m); }
        public bool LockMicrosecondTouchInputTimer(out string m) { return _blueStacks.LockMicrosecondTouchInputTimer(out m); }
        public bool ConfigureArm64DirectTranslation(out string m) { return _blueStacks.ConfigureArm64DirectTranslation(out m); }
        public bool DisableVhdCompactionTracing(out string m) { return _blueStacks.DisableVhdCompactionTracing(out m); }
        public bool DisableBlueStacksSidebarAds(out string m) { return _blueStacks.DisableBlueStacksSidebarAds(out m); }
        public bool SilenceAndroidBackgroundNotifications(out string m) { return _blueStacks.SilenceAndroidBackgroundNotifications(out m); }
        public bool ConfigureMlbbFastCastSmartKeys(out string m) { return _blueStacks.ConfigureMlbbFastCastSmartKeys(out m); }
        public bool SpoofRogPhone7Ultimate(out string m) { return _blueStacks.SpoofRogPhone7Ultimate(out m); }
        public bool SpoofBlackShark5Pro(out string m) { return _blueStacks.SpoofBlackShark5Pro(out m); }
        public bool SpoofSamsungS23UltraDeviceProfile(out string m) { return _blueStacks.SpoofSamsungS23UltraDeviceProfile(out m); }
        public bool ConfigureBstNetworkClamp(out string m) { return _blueStacks.ConfigureBstNetworkClamp(out m); }
        public bool PrioritizeMobaPacketsQoS(out string m) { return _blueStacks.PrioritizeMobaPacketsQoS(out m); }
        public bool OptimizeMlbbNetworkMtuBuffer(out string m) { return _blueStacks.OptimizeMlbbNetworkMtuBuffer(out m); }
        public bool FlushMlbSoutheastAsiaDns(out string m) { return _blueStacks.FlushMlbSoutheastAsiaDns(out m); }
        public bool RestoreBlueStacksDefaults(out string m) { return _blueStacks.RestoreBlueStacksDefaults(out m); }
        public bool RestoreStandardResolution(out string m) { return _blueStacks.RestoreStandardResolution(out m); }
        public bool RestoreStandardAnalogDeadzone(out string m) { return _blueStacks.RestoreStandardAnalogDeadzone(out m); }
        public bool IsBlueStacksSettingActive(string p) { return _blueStacks.IsBlueStacksSettingActive(p); }
        public bool CleanBlueStacksLogs(out long b, out string m) { return _blueStacks.CleanBlueStacksLogs(out b, out m); }
        public bool CleanBlueStacksTempMedia(out long b, out string m) { return _blueStacks.CleanBlueStacksTempMedia(out b, out m); }
        public bool CleanBlueStacksTempApkCache(out long b, out string m) { return _blueStacks.CleanBlueStacksTempApkCache(out b, out m); }
        public bool CleanBlueStacksCrashpadReports(out long b, out string m) { return _blueStacks.CleanBlueStacksCrashpadReports(out b, out m); }
        public bool PurgeAndroidShaderDiskCache(out long b, out string m) { return _blueStacks.PurgeAndroidShaderDiskCache(out b, out m); }
        public bool FlushMlbbShaderCache(out long b, out string m) { return _blueStacks.FlushMlbbShaderCache(out b, out m); }
        public bool OptimizeBlueStacksVirtualDisk(out long b, out string m) { return _blueStacks.OptimizeBlueStacksVirtualDisk(out b, out m); }
        public bool FlushBlueStacksMidSessionRam(out long b, out string m) { return _blueStacks.FlushBlueStacksMidSessionRam(out b, out m); }
        public bool AutoPurgeStandbyMemoryBeforeMatch(out long b, out string m) { return _blueStacks.AutoPurgeStandbyMemoryBeforeMatch(out b, out m); }
        public bool TrimBlueStacksWorkingSetAggressive(out long b, out string m) { return _blueStacks.TrimBlueStacksWorkingSetAggressive(out b, out m); }
        public bool LockEmulatorMemoryPagesAntiFreeze(out string m) { return _blueStacks.LockEmulatorMemoryPagesAntiFreeze(out m); }
        public bool ForceBlueStacksHighPerformanceGpu(out string m) { return _blueStacks.ForceBlueStacksHighPerformanceGpu(out m); }
        public bool KillFrozenBlueStacksServices(out string m) { return _blueStacks.KillFrozenBlueStacksServices(out m); }
        public bool DisableBlueStacksStartupServices(out string m) { return _blueStacks.DisableBlueStacksStartupServices(out m); }
        public bool UnparkBlueStacksAssignedCores(out string m) { return _blueStacks.UnparkBlueStacksAssignedCores(out m); }
        public bool PinBlueStacksToPerformanceCores(out string m) { return _blueStacks.PinBlueStacksToPerformanceCores(out m); }
        public bool ElevateBlueStacksIoPriority(out string m) { return _blueStacks.ElevateBlueStacksIoPriority(out m); }
        public bool DisableBlueStacksPowerThrottling(out string m) { return _blueStacks.DisableBlueStacksPowerThrottling(out m); }
        public bool ElevateBlueStacksPagePriority(out string m) { return _blueStacks.ElevateBlueStacksPagePriority(out m); }
        public bool ConfigureBlueStacksVirtualBridgeQoS(out string m) { return _blueStacks.ConfigureBlueStacksVirtualBridgeQoS(out m); }
        public bool VerifyAndAuditKeymapProtection(out string m) { return _blueStacks.VerifyAndAuditKeymapProtection(out m); }
        public bool UnparkBlueStacksCpuCores(out string m) { return _blueStacks.UnparkBlueStacksCpuCores(out m); }
        public bool TuneAudioBufferLatency(out string m) { return _blueStacks.TuneAudioBufferLatency(out m); }
        public bool AmplifyTurretAndLordSoundCues(out string m) { return _blueStacks.AmplifyTurretAndLordSoundCues(out m); }
        public bool AmplifyStealthHeroSoundCues(out string m) { return _blueStacks.AmplifyStealthHeroSoundCues(out m); }
        public bool AmplifySkillCooldownAudioCues(out string m) { return _blueStacks.AmplifySkillCooldownAudioCues(out m); }
        public string AmplifyBushAmbushAudioAlerts(out string m) { return _blueStacks.AmplifyBushAmbushAudioAlerts(out m); }
        public string AmplifyLowHealthWarningSound(out string m) { return _blueStacks.AmplifyLowHealthWarningSound(out m); }
        public string AmplifyTurtleLordSpawnDrums(out string m) { return _blueStacks.AmplifyTurtleLordSpawnDrums(out m); }
        public bool TuneMobaSoundCueEqualizer(out string m) { return _blueStacks.TuneMobaSoundCueEqualizer(out m); }
        public string GetLordAndTurtleTimersMatrix() { return _blueStacks.GetLordAndTurtleTimersMatrix(); }
        public string GetRetributionBreakpointGuide() { return _blueStacks.GetRetributionBreakpointGuide(); }
        public string GetTargetingPrioritySetupGuide() { return _blueStacks.GetTargetingPrioritySetupGuide(); }
        public string GetLordBuffAndTurtleEconomyMatrix() { return _blueStacks.GetLordBuffAndTurtleEconomyMatrix(); }
        public string GetHeroLockModeGuide() { return _blueStacks.GetHeroLockModeGuide(); }
        public string GetLaneMinionBountyMatrix() { return _blueStacks.GetLaneMinionBountyMatrix(); }
        public string GetRoamingEquipmentGuide() { return _blueStacks.GetRoamingEquipmentGuide(); }
        public string GetSkillAimSensitivityCalibratorGuide() { return _blueStacks.GetSkillAimSensitivityCalibratorGuide(); }
        public string GetBattleSpellCooldownMatrix() { return _blueStacks.GetBattleSpellCooldownMatrix(); }
        public bool ApplyOptimalMobileLegendsPreset(out string m) { return _blueStacks.ApplyOptimalMobileLegendsPreset(out m); }
        public bool ApplyUltraWideEsportsPreset(out string m) { return _blueStacks.ApplyUltraWideEsportsPreset(out m); }
        public bool ApplyMythicGlorySetup(out string m) { return _blueStacks.ApplyMythicGlorySetup(out m); }
        public bool ApplyMythicImmortalCombo(out string m) { return _blueStacks.ApplyMythicImmortalCombo(out m); }
        public bool ApplyMythicRankedPreMatchArm(out string m) { return _blueStacks.ApplyMythicRankedPreMatchArm(out m); }
        public bool ApplyMythicalGloryDominanceSuite(out string m) { return _blueStacks.ApplyMythicalGloryDominanceSuite(out m); }
    }
}