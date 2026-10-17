using System;

namespace ZenithOptimizer.Services
{
    public partial class GameSpecificTunerService
    {
        public string GetTekkenFramePacingStatus() { return _tekken.GetTekkenFramePacingStatus(); }
        public string GetTekkenSafetyStatus() { return _tekken.GetTekkenSafetyStatus(); }
        public bool LockTekken60FpsFramePacing(out string m) { return _tekken.LockTekken60FpsFramePacing(out m); }
        public bool EnforceTekkenStrict60FpsNoVsync(out string m) { return _tekken.EnforceTekkenStrict60FpsNoVsync(out m); }
        public bool LockTekken16msPacing(out string m) { return _tekken.LockTekken16msPacing(out m); }
        public bool LockTekken60FpsLimiter(out string m) { return _tekken.LockTekken60FpsLimiter(out m); }
        public bool RestoreTekkenUncappedFps(out string m) { return _tekken.RestoreTekkenUncappedFps(out m); }
        public bool EnforceBorderless60FpsMode(out string m) { return _tekken.EnforceBorderless60FpsMode(out m); }
        public bool EnforceTekkenBorderlessWindow(out string m) { return _tekken.EnforceTekkenBorderlessWindow(out m); }
        public bool EnableTekkenVSync(out string m) { return _tekken.EnableTekkenVSync(out m); }
        public bool EnableTekkenMotionBlur(out string m) { return _tekken.EnableTekkenMotionBlur(out m); }
        public bool DisableTekkenMotionBlurAndAberration(out string m) { return _tekken.DisableTekkenMotionBlurAndAberration(out m); }
        public bool DisableTekkenMotionBlurEngineIni(out string m) { return _tekken.DisableTekkenMotionBlurEngineIni(out m); }
        public bool DisableTekkenDepthOfField(out string m) { return _tekken.DisableTekkenDepthOfField(out m); }
        public bool RestoreTekkenDepthOfField(out string m) { return _tekken.RestoreTekkenDepthOfField(out m); }
        public bool DisableFilmGrainAndLensFlare(out string m) { return _tekken.DisableFilmGrainAndLensFlare(out m); }
        public bool RestoreFilmGrainAndLensFlare(out string m) { return _tekken.RestoreFilmGrainAndLensFlare(out m); }
        public bool DisableChromaticAberrationTekken(out string m) { return _tekken.DisableChromaticAberrationTekken(out m); }
        public bool DisableDynamicResolutionScalingTekken(out string m) { return _tekken.DisableDynamicResolutionScalingTekken(out m); }
        public bool DisableLensDistortionAndFlaresTekken(out string m) { return _tekken.DisableLensDistortionAndFlaresTekken(out m); }
        public bool DisableSsaoAmbientOcclusionTekken(out string m) { return _tekken.DisableSsaoAmbientOcclusionTekken(out m); }
        public bool LockMaxPreRenderedFramesToOneTekken(out string m) { return _tekken.LockMaxPreRenderedFramesToOneTekken(out m); }
        public bool ForceTekkenDirectX11Pipeline(out string m) { return _tekken.ForceTekkenDirectX11Pipeline(out m); }
        public string GetTekken7Dx11LaunchOption() { return _tekken.GetTekken7Dx11LaunchOption(); }
        public string GetTekkenVsyncAdvice() { return _tekken.GetTekkenVsyncAdvice(); }
        public string GetUe5ShaderAdvice() { return _tekken.GetUe5ShaderAdvice(); }
        public bool RestoreTekkenDefaults(out string m) { return _tekken.RestoreTekkenDefaults(out m); }
        public bool IsTekkenSettingActive(string k, string exp) { return _tekken.IsTekkenSettingActive(k, exp); }
        public bool IsTekkenBlurDisabled() { return _tekken.IsTekkenBlurDisabled(); }
        public bool VerifyGamepadPolling(out string m) { return _tekken.VerifyGamepadPolling(out m); }
        public bool AuditControllerPollingRate(out string m) { return _tekken.AuditControllerPollingRate(out m); }
        public bool LockJustFrameInputPolling(out string m) { return _tekken.LockJustFrameInputPolling(out m); }
        public bool ShieldArcadeStickUsbBuffer(out string m) { return _tekken.ShieldArcadeStickUsbBuffer(out m); }
        public bool ShieldArcadeStickUsbLatency(out string m) { return _tekken.ShieldArcadeStickUsbLatency(out m); }
        public bool UnshieldArcadeStickUsbBuffer(out string m) { return _tekken.UnshieldArcadeStickUsbBuffer(out m); }
        public bool RestoreStandardUsbLatency(out string m) { return _tekken.RestoreStandardUsbLatency(out m); }
        public bool IsArcadeStickShieldActive() { return _tekken.IsArcadeStickShieldActive(); }
        public bool ConfigureZeroDelayKeyboardDebounce(out string m) { return _tekken.ConfigureZeroDelayKeyboardDebounce(out m); }
        public bool RestoreDefaultKeyboardDebounce(out string m) { return _tekken.RestoreDefaultKeyboardDebounce(out m); }
        public bool DisableGamepadPowerSleepSuspension(out string m) { return _tekken.DisableGamepadPowerSleepSuspension(out m); }
        public bool RestoreGamepadPowerSleepSuspension(out string m) { return _tekken.RestoreGamepadPowerSleepSuspension(out m); }
        public bool DisableMouseCursorTrappingLagTekken(out string m) { return _tekken.DisableMouseCursorTrappingLagTekken(out m); }
        public bool SetLowLatencyControllerDriver(out string m) { return _tekken.SetLowLatencyControllerDriver(out m); }
        public bool OptimizeFightStickUsbPolling(out string m) { return _tekken.OptimizeFightStickUsbPolling(out m); }
        public bool PreventGamepadUsbSelectiveSuspend(out string m) { return _tekken.PreventGamepadUsbSelectiveSuspend(out m); }
        public string GetTekkenControllerLatencyGuide() { return _tekken.GetTekkenControllerLatencyGuide(); }
        public string GetSocdCleanerGuide() { return _tekken.GetSocdCleanerGuide(); }
        public string GetSteamInputPassThroughGuide() { return _tekken.GetSteamInputPassThroughGuide(); }
        public string AuditFightingGameRollbackLatency() { return _tekken.AuditFightingGameRollbackLatency(); }
        public string AuditRollbackNetcodePacketJitter() { return _tekken.AuditRollbackNetcodePacketJitter(); }
        public bool OptimizeTekkenRollbackP2pSockets(out string m) { return _tekken.OptimizeTekkenRollbackP2pSockets(out m); }
        public bool OptimizeTekkenRollbackTcpNoDelay(out string m) { return _tekken.OptimizeTekkenRollbackTcpNoDelay(out m); }
        public bool ConfigureTekkenRollbackDscpQoS(out string m) { return _tekken.ConfigureTekkenRollbackDscpQoS(out m); }
        public bool StabilizeFightingAudioBuffer(out string m) { return _tekken.StabilizeFightingAudioBuffer(out m); }
        public bool AccentuateCounterHitAndWallSplatAudio(out string m) { return _tekken.AccentuateCounterHitAndWallSplatAudio(out m); }
        public bool AmplifyLowParryAudioCues(out string m) { return _tekken.AmplifyLowParryAudioCues(out m); }
        public bool AmplifyBreakThrowAudioCues(out string m) { return _tekken.AmplifyBreakThrowAudioCues(out m); }
        public string AmplifyBlockImpactSoundLevels(out string m) { return _tekken.AmplifyBlockImpactSoundLevels(out m); }
        public string AmplifyCounterHitVoiceGrunts(out string m) { return _tekken.AmplifyCounterHitVoiceGrunts(out m); }
        public bool BoostCounterHitSoundCues(out string m) { return _tekken.BoostCounterHitSoundCues(out m); }
        public string GetTekkenPunishmentFrameDataMatrix() { return _tekken.GetTekkenPunishmentFrameDataMatrix(); }
        public string GetElectricWindGodFistTimingMatrix() { return _tekken.GetElectricWindGodFistTimingMatrix(); }
        public string GetWhiffPunishFrameMatrix() { return _tekken.GetWhiffPunishFrameMatrix(); }
        public string GetWallBounceAndFloorBreakMatrix() { return _tekken.GetWallBounceAndFloorBreakMatrix(); }
        public string GetCounterHitConfirmGuide() { return _tekken.GetCounterHitConfirmGuide(); }
        public string GetRageArtVsRageDriveMatrix() { return _tekken.GetRageArtVsRageDriveMatrix(); }
        public string GetPunishFrameCheatSheet() { return _tekken.GetPunishFrameCheatSheet(); }
        public string GetFloorBreakBalconyBreakGuide() { return _tekken.GetFloorBreakBalconyBreakGuide(); }
        public string GetThrowBreakVisualAudioGuide() { return _tekken.GetThrowBreakVisualAudioGuide(); }
        public string GetSidestepTrackingGuide() { return _tekken.GetSidestepTrackingGuide(); }
        public string GetKoreanBackdashGuide() { return _tekken.GetKoreanBackdashGuide(); }
        public bool ForceTekkenHighPerformanceGpu(out string m) { return _tekken.ForceTekkenHighPerformanceGpu(out m); }
        public bool EnforceTekkenHighPerformanceGpuAffinity(out string m) { return _tekken.EnforceTekkenHighPerformanceGpuAffinity(out m); }
        public bool DisableFullscreenOptimizationsForTekken(out string m) { return _tekken.DisableFullscreenOptimizationsForTekken(out m); }
        public bool LockTekkenProcessAffinityToPCores(out string m) { return _tekken.LockTekkenProcessAffinityToPCores(out m); }
        public bool PinTekkenToPerformanceCores(out string m) { return _tekken.PinTekkenToPerformanceCores(out m); }
        public bool PreventCpuCoreParkingDuringMatch(out string m) { return _tekken.PreventCpuCoreParkingDuringMatch(out m); }
        public bool MinimiseDpcLatency(out string m) { return _tekken.MinimiseDpcLatency(out m); }
        public bool PrioritizeTekkenProcess(out string m) { return _tekken.PrioritizeTekkenProcess(out m); }
        public bool ElevateTekkenRenderPriority(out string m) { return _tekken.ElevateTekkenRenderPriority(out m); }
        public bool BoostTekkenTextureStreamingPool(out string m) { return _tekken.BoostTekkenTextureStreamingPool(out m); }
        public bool LockHighResolution05msTimerTekken(out string m) { return _tekken.LockHighResolution05msTimerTekken(out m); }
        public bool DisableGameModeProcessThrottlingTekken(out string m) { return _tekken.DisableGameModeProcessThrottlingTekken(out m); }
        public bool DisablePowerThrottlingForTekken(out string m) { return _tekken.DisablePowerThrottlingForTekken(out m); }
        public bool DisableWindowsGameBarRecording(out string m) { return _tekken.DisableWindowsGameBarRecording(out m); }
        public bool SuppressDpcLatencyForJustFrames(out string m) { return _tekken.SuppressDpcLatencyForJustFrames(out m); }
        public bool EnforceDirectFlipExclusive60Fps(out string m) { return _tekken.EnforceDirectFlipExclusive60Fps(out m); }
        public bool ElevateTekkenIoPriority(out string m) { return _tekken.ElevateTekkenIoPriority(out m); }
        public bool DisableTekkenPowerThrottling(out string m) { return _tekken.DisableTekkenPowerThrottling(out m); }
        public bool ProtectTekkenWorkingSetMemory(out string m) { return _tekken.ProtectTekkenWorkingSetMemory(out m); }
        public bool DisableTekkenStickyKeysAccessibility(out string m) { return _tekken.DisableTekkenStickyKeysAccessibility(out m); }
        public bool CleanTekken7Logs(out long b, out string m) { return _tekken.CleanTekken7Logs(out b, out m); }
        public bool CleanTekken8Logs(out long b, out string m) { return _tekken.CleanTekken8Logs(out b, out m); }
        public bool CleanTekkenCrashDumps(out long b, out string m) { return _tekken.CleanTekkenCrashDumps(out b, out m); }
        public bool CleanUnrealEngineCrashTelemetry(out long b, out string m) { return _tekken.CleanUnrealEngineCrashTelemetry(out b, out m); }
        public bool CleanTekkenSavedSaveGamesTemp(out long b, out string m) { return _tekken.CleanTekkenSavedSaveGamesTemp(out b, out m); }
        public bool CleanTekkenGhostAndReplayData(out long b, out string m) { return _tekken.CleanTekkenGhostAndReplayData(out b, out m); }
        public bool PurgeGhostBattlesAndTelemetry(out long b, out string m) { return _tekken.PurgeGhostBattlesAndTelemetry(out b, out m); }
        public bool PurgeGhostBattleTelemetry(out long b, out string m) { return _tekken.PurgeGhostBattleTelemetry(out b, out m); }
        public bool PurgeTekkenGhostBattleData(out long b, out string m) { return _tekken.PurgeTekkenGhostBattleData(out b, out m); }
        public bool CleanTekken8GhostAndReplayTelemetry(out long b, out string m) { return _tekken.CleanTekken8GhostAndReplayTelemetry(out b, out m); }
        public bool CleanDirectXShaderCacheTekken(out long b, out string m) { return _tekken.CleanDirectXShaderCacheTekken(out b, out m); }
        public bool PurgeTekkenPipelineShaders(out long b, out string m) { return _tekken.PurgeTekkenPipelineShaders(out b, out m); }
        public bool FlushTekkenPipelineShaders(out string m) { return _tekken.FlushTekkenPipelineShaders(out m); }
        public bool OptimizeWindowsGameDvrForTekken(out string m) { return _tekken.OptimizeWindowsGameDvrForTekken(out m); }
        public bool ApplyTekkenTournamentMode(out string m) { return _tekken.ApplyTekkenTournamentMode(out m); }
        public bool ApplyTekkenEvoRankedPackage(out string m) { return _tekken.ApplyTekkenEvoRankedPackage(out m); }
        public bool ApplyEvoGrandFinalsPreArm(out string m) { return _tekken.ApplyEvoGrandFinalsPreArm(out m); }
        public bool ApplyEvoChampionCombo(out string m) { return _tekken.ApplyEvoChampionCombo(out m); }
        public bool ApplyTekkenGrandFinalsPreArm(out string m) { return _tekken.ApplyTekkenGrandFinalsPreArm(out m); }
        public bool ApplyIronFistMasterTournamentSuite(out string m) { return _tekken.ApplyIronFistMasterTournamentSuite(out m); }
    }
}