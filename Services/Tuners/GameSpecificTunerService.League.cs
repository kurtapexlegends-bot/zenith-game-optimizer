using System;

namespace ZenithOptimizer.Services
{
    public partial class GameSpecificTunerService
    {
        public string GetLeagueConfigPath() { return _league.GetLeagueConfigPath(); }
        public string GetLeagueStatus() { return _league.GetLeagueStatus(); }
        public string GetLeagueSafetyStatus() { return _league.GetLeagueSafetyStatus(); }
        public bool OptimizeLeagueGameConfig(out string m) { return _league.OptimizeLeagueGameConfig(out m); }
        public bool RestoreLeagueGameConfig(out string m) { return _league.RestoreLeagueGameConfig(out m); }
        public bool IsLeagueSettingActive(string k, string exp) { return _league.IsLeagueSettingActive(k, exp); }
        public bool SetLeagueTargetFramerate(int fps, out string m) { return _league.SetLeagueTargetFramerate(fps, out m); }
        public bool DisableLeagueScreenShake(out string m) { return _league.DisableLeagueScreenShake(out m); }
        public bool EnableMovementPrediction(out string m) { return _league.EnableMovementPrediction(out m); }
        public bool DisableInkingShader(out string m) { return _league.DisableInkingShader(out m); }
        public bool DisableGodraysAndEyecandy(out string m) { return _league.DisableGodraysAndEyecandy(out m); }
        public bool ThrottleRiotClientBackground(out string m) { return _league.ThrottleRiotClientBackground(out m); }
        public bool DisableCombatTextSpam(out string m) { return _league.DisableCombatTextSpam(out m); }
        public bool PrioritizeLeagueProcess(out string m) { return _league.PrioritizeLeagueProcess(out m); }
        public bool OptimizeLeagueAudio(out string m) { return _league.OptimizeLeagueAudio(out m); }
        public bool DisableEyeCandyAndRiverCritters(out string m) { return _league.DisableEyeCandyAndRiverCritters(out m); }
        public bool DisableCharacterCelShading(out string m) { return _league.DisableCharacterCelShading(out m); }
        public bool SetEnvironmentQualityLow(out string m) { return _league.SetEnvironmentQualityLow(out m); }
        public bool EnableInstantSmartCast(out string m) { return _league.EnableInstantSmartCast(out m); }
        public bool DisableGameDvrInLeagueConfig(out string m) { return _league.DisableGameDvrInLeagueConfig(out m); }
        public bool DisableInGameMusicEngine(out string m) { return _league.DisableInGameMusicEngine(out m); }
        public bool ForceLeagueHighPerformanceGpu(out string m) { return _league.ForceLeagueHighPerformanceGpu(out m); }
        public bool ConfigureAttackMoveOnCursor(bool en, out string m) { return _league.ConfigureAttackMoveOnCursor(en, out m); }
        public bool MuteRiverAmbientNoise(out string m) { return _league.MuteRiverAmbientNoise(out m); }
        public bool UnmuteRiverAmbientNoise(out string m) { return _league.UnmuteRiverAmbientNoise(out m); }
        public bool OverrideMinimapScaleBeyondCap(double sc, out string m) { return _league.OverrideMinimapScaleBeyondCap(sc, out m); }
        public bool RestoreMinimapScale(out string m) { return _league.RestoreMinimapScale(out m); }
        public bool DisableScreenShakeAndLowHpFlash(out string m) { return _league.DisableScreenShakeAndLowHpFlash(out m); }
        public bool RestoreScreenShakeAndLowHpFlash(out string m) { return _league.RestoreScreenShakeAndLowHpFlash(out m); }
        public bool DisableAutoAcquireTarget(out string m) { return _league.DisableAutoAcquireTarget(out m); }
        public bool EnableAutoAcquireTarget(out string m) { return _league.EnableAutoAcquireTarget(out m); }
        public bool ApplyLeagueVanguardThreadShield(out string m) { return _league.ApplyLeagueVanguardThreadShield(out m); }
        public bool ConfigureQuickCastWithIndicator(bool en, out string m) { return _league.ConfigureQuickCastWithIndicator(en, out m); }
        public bool ConfigureCameraDecoupleOnRespawn(bool d, out string m) { return _league.ConfigureCameraDecoupleOnRespawn(d, out m); }
        public bool ConfigureEyeCandySuppression(bool s, out string m) { return _league.ConfigureEyeCandySuppression(s, out m); }
        public bool ConfigureTargetChampionsOnlyBorder(out string m) { return _league.ConfigureTargetChampionsOnlyBorder(out m); }
        public bool TuneMinimapFlipPosition(bool left, out string m) { return _league.TuneMinimapFlipPosition(left, out m); }
        public bool ConfigureChatScale(double sc, out string m) { return _league.ConfigureChatScale(sc, out m); }
        public bool ConfigureChampionRangeIndicator(bool s, out string m) { return _league.ConfigureChampionRangeIndicator(s, out m); }
        public bool OptimizeLeagueMovementQueueBuffer(out string m) { return _league.OptimizeLeagueMovementQueueBuffer(out m); }
        public bool ConfigureCloseClientOnGameStart(bool c, out string m) { return _league.ConfigureCloseClientOnGameStart(c, out m); }
        public bool DisableLeagueEmoteSpamDelay(out string m) { return _league.DisableLeagueEmoteSpamDelay(out m); }
        public bool LockLeagueRendererD3D9Bypass(out string m) { return _league.LockLeagueRendererD3D9Bypass(out m); }
        public bool ConfigureTargetChampionsOnlyToggle(bool en, out string m) { return _league.ConfigureTargetChampionsOnlyToggle(en, out m); }
        public bool ConfigureInstantWardCast(bool en, out string m) { return _league.ConfigureInstantWardCast(en, out m); }
        public bool HideSummonerNamesAboveHealthbars(bool h, out string m) { return _league.HideSummonerNamesAboveHealthbars(h, out m); }
        public bool DisableInGameEmoteBubbles(bool d, out string m) { return _league.DisableInGameEmoteBubbles(d, out m); }
        public bool SetPrecisionCursorScale(int sc, out string m) { return _league.SetPrecisionCursorScale(sc, out m); }
        public bool DisableDeathScreenDesaturation(bool d, out string m) { return _league.DisableDeathScreenDesaturation(d, out m); }
        public bool ConfigureAutoAttackRangeBorder(bool en, out string m) { return _league.ConfigureAutoAttackRangeBorder(en, out m); }
        public bool EnableRawMouseInputLeague(bool en, out string m) { return _league.EnableRawMouseInputLeague(en, out m); }
        public bool DisableGrassWindSwayLeague(bool d, out string m) { return _league.DisableGrassWindSwayLeague(d, out m); }
        public bool DisableCameraSmoothingLeague(bool d, out string m) { return _league.DisableCameraSmoothingLeague(d, out m); }
        public bool BypassD3D11MultithreadingOverhead(bool b, out string m) { return _league.BypassD3D11MultithreadingOverhead(b, out m); }
        public bool LockCursorToLeagueWindow(out string m) { return _league.LockCursorToLeagueWindow(out m); }
        public bool EnableAttackMoveOnCursor(out string m) { return _league.EnableAttackMoveOnCursor(out m); }
        public bool EnableTargetChampionsOnlyToggle(out string m) { return _league.EnableTargetChampionsOnlyToggle(out m); }
        public bool TuneCameraSmoothnessAndSnap(out string m) { return _league.TuneCameraSmoothnessAndSnap(out m); }
        public bool OptimizeLeaguePacketBuffering(out string m) { return _league.OptimizeLeaguePacketBuffering(out m); }
        public bool ShieldLeagueFromVanguardInterference(out string m) { return _league.ShieldLeagueFromVanguardInterference(out m); }
        public bool ConfigureHighContrastHealthbars(out string m) { return _league.ConfigureHighContrastHealthbars(out m); }
        public bool OptimizeComboInputBuffer(out string m) { return _league.OptimizeComboInputBuffer(out m); }
        public bool ConfigureSpacebarCenterLock(out string m) { return _league.ConfigureSpacebarCenterLock(out m); }
        public bool ConfigurePredictivePathfindingClamp(out string m) { return _league.ConfigurePredictivePathfindingClamp(out m); }
        public bool CleanLeagueLogs(out long b, out string m) { return _league.CleanLeagueLogs(out b, out m); }
        public bool CleanLeagueReplaysAndDumps(out long b, out string m) { return _league.CleanLeagueReplaysAndDumps(out b, out m); }
        public bool CleanLeagueChromiumCache(out long b, out string m) { return _league.CleanLeagueChromiumCache(out b, out m); }
        public bool CleanLeagueHighlightsAndReplays(out long b, out string m) { return _league.CleanLeagueHighlightsAndReplays(out b, out m); }
        public bool CleanLeagueLogsAndWebCaches(out long b, out string m) { return _league.CleanLeagueLogsAndWebCaches(out b, out m); }
        public bool CleanLeagueClientMemoryLeak(out long b, out string m) { return _league.CleanLeagueClientMemoryLeak(out b, out m); }
        public bool PurgeLeagueCustomItemSets(out long b, out string m) { return _league.PurgeLeagueCustomItemSets(out b, out m); }
        public bool PurgeLeagueCustomItemSetsCache(out long b, out string m) { return _league.PurgeLeagueCustomItemSetsCache(out b, out m); }
        public bool PurgeLeagueCrashReporterTelemetry(out long b, out string m) { return _league.PurgeLeagueCrashReporterTelemetry(out b, out m); }
        public bool PurgeLegacyLeaguePatcherLogs(out long b, out string m) { return _league.PurgeLegacyLeaguePatcherLogs(out b, out m); }
        public bool AmplifyDangerPingAudio(out string m) { return _league.AmplifyDangerPingAudio(out m); }
        public bool AmplifyTrueDamageAudioCues(out string m) { return _league.AmplifyTrueDamageAudioCues(out m); }
        public string AmplifyEpicMonsterRoarAudio(out string m) { return _league.AmplifyEpicMonsterRoarAudio(out m); }
        public string AmplifyStealthAudioCues(out string m) { return _league.AmplifyStealthAudioCues(out m); }
        public bool OptimizeTeamfightSoundFrequencies(out string m) { return _league.OptimizeTeamfightSoundFrequencies(out m); }
        public string GetObjectiveSmiteCalculatorGuide() { return _league.GetObjectiveSmiteCalculatorGuide(); }
        public string GetSummonerAndCampTimersMatrix() { return _league.GetSummonerAndCampTimersMatrix(); }
        public string GetWaveManagementAndFreezeMatrix() { return _league.GetWaveManagementAndFreezeMatrix(); }
        public string GetDragonSoulAndElderMatrix() { return _league.GetDragonSoulAndElderMatrix(); }
        public string GetDragonSoulAndElderStatsMatrix() { return _league.GetDragonSoulAndElderStatsMatrix(); }
        public string GetTrickWardVisionGuide() { return _league.GetTrickWardVisionGuide(); }
        public string GetJungleRespawnTimersDetailMatrix() { return _league.GetJungleRespawnTimersDetailMatrix(); }
        public string GetWaveManagementSlowPushGuide() { return _league.GetWaveManagementSlowPushGuide(); }
        public string GetJungleLeashPatienceRingGuide() { return _league.GetJungleLeashPatienceRingGuide(); }
        public string GetJungleLevelXpPathingMatrix() { return _league.GetJungleLevelXpPathingMatrix(); }
        public string GetTurretPlatingBountyMatrix() { return _league.GetTurretPlatingBountyMatrix(); }
        public string GetTeleportChannelMechanicsGuide() { return _league.GetTeleportChannelMechanicsGuide(); }
        public string GetSummonerSpellCooldownGuide() { return _league.GetSummonerSpellCooldownGuide(); }
        public string GetJungleMonsterScalingGuide() { return _league.GetJungleMonsterScalingGuide(); }
        public bool ElevateLeagueIoPriority(out string m) { return _league.ElevateLeagueIoPriority(out m); }
        public bool DisableLeaguePowerThrottling(out string m) { return _league.DisableLeaguePowerThrottling(out m); }
        public bool ConfigureLeagueNetworkDscpQoS(out string m) { return _league.ConfigureLeagueNetworkDscpQoS(out m); }
        public bool ProtectLeagueWorkingSetMemory(out string m) { return _league.ProtectLeagueWorkingSetMemory(out m); }
        public bool DisableLeagueStickyKeysAccessibility(out string m) { return _league.DisableLeagueStickyKeysAccessibility(out m); }
        public bool ApplyLeagueMasterPreset(out string m) { return _league.ApplyLeagueMasterPreset(out m); }
        public bool ApplyLeagueUltraPerformancePreset(out string m) { return _league.ApplyLeagueUltraPerformancePreset(out m); }
        public bool ApplyChallengerAdcConfig(out string m) { return _league.ApplyChallengerAdcConfig(out m); }
        public bool ApplyChallengerGrandmasterCombo(out string m) { return _league.ApplyChallengerGrandmasterCombo(out m); }
        public bool ApplyAdcEsportsCompetitivePreset(out string m) { return _league.ApplyAdcEsportsCompetitivePreset(out m); }
        public bool ApplyChallengerApexMacroCombo(out string m) { return _league.ApplyChallengerApexMacroCombo(out m); }
    }
}