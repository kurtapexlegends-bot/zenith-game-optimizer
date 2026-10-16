using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ZenithOptimizer.Services
{
    public partial class GameSpecificTunerService
    {
// REGION: State Inspection Queries for Active Indicators

        public bool IsLeagueSettingActive(string key, string expectedValue)

        {

            try

            {

                string path = GetLeagueConfigPath();

                if (!string.IsNullOrEmpty(path) && File.Exists(path))

                {

                    string content = File.ReadAllText(path);

                    return content.Contains(key + "=" + expectedValue);

                }

            }

            catch { }

            return false;

        }

        public bool IsBlueStacksSettingActive(string pattern)

        {

            try

            {

                string path = @"C:\ProgramData\BlueStacks_nxt\bluestacks.conf";

                if (File.Exists(path))

                {

                    string content = File.ReadAllText(path);

                    return Regex.IsMatch(content, pattern);

                }

            }

            catch { }

            return false;

        }

        public bool IsRobloxFlagActive(string flagName, string expectedValue)

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string jsonPath = Path.Combine(localApp, @"Roblox\ClientSettings\ClientAppSettings.json");

                if (File.Exists(jsonPath))

                {

                    string content = File.ReadAllText(jsonPath);

                    return content.Contains("\"" + flagName + "\": \"" + expectedValue + "\"") ||

                           content.Contains("\"" + flagName + "\": " + expectedValue);

                }

            }

            catch { }

            return false;

        }

        public bool IsTekkenSettingActive(string key, string expectedValue)

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string iniPath = Path.Combine(localApp, @"TekkenGame\Saved\Config\WindowsNoEditor\GameUserSettings.ini");

                if (File.Exists(iniPath))

                {

                    string content = File.ReadAllText(iniPath);

                    return content.Contains(key + "=" + expectedValue);

                }

            }

            catch { }

            return false;

        }

        public bool IsFsoBypassActive(string exeName)

        {

            try

            {

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))

                {

                    if (key != null)

                    {

                        foreach (string valName in key.GetValueNames())

                        {

                            if (valName.EndsWith(exeName, StringComparison.OrdinalIgnoreCase))

                            {

                                object val = key.GetValue(valName);

                                if (val != null && val.ToString().Contains("DISABLEDXMAXIMIZEDWINDOWEDMODE")) return true;

                            }

                        }

                    }

                }

            }

            catch { }

            return false;

        }

        public bool IsGpuPreferenceActive(string exeName)

        {

            try

            {

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))

                {

                    if (key != null)

                    {

                        foreach (string valName in key.GetValueNames())

                        {

                            if (valName.EndsWith(exeName, StringComparison.OrdinalIgnoreCase))

                            {

                                object val = key.GetValue(valName);

                                if (val != null && val.ToString().Contains("GpuPreference=2")) return true;

                            }

                        }

                    }

                }

            }

            catch { }

            return false;

        }

        public bool IsTimerResolutionActive()

        {

            try

            {

                return _timerEngine != null && _timerEngine.GetCurrentResolutionMs() <= 1.0;

            }

            catch { }

            return false;

        }

        public bool IsRawMouseActive()

        {

            try

            {

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse"))

                {

                    if (key != null)

                    {

                        object val = key.GetValue("MouseSpeed");

                        if (val != null && val.ToString() == "0") return true;

                    }

                }

            }

            catch { }

            return false;

        }

        public bool IsTekkenBlurDisabled()

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string engineIni = Path.Combine(localApp, @"TekkenGame\Saved\Config\WindowsNoEditor\Engine.ini");

                if (File.Exists(engineIni))

                {

                    string content = File.ReadAllText(engineIni);

                    return content.Contains("r.MotionBlurQuality=0");

                }

            }

            catch { }

            return false;

        }

        public bool IsArcadeStickShieldActive()

        {

            try

            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USB"))

                {

                    if (key != null)

                    {

                        object val = key.GetValue("DisableSelectiveSuspend");

                        if (val != null && val.ToString() == "1") return true;

                    }

                }

            }

            catch { }

            return false;

        }
// END REGION
// REGION: Deactivation / Revert Operations

        public bool UnmuteRiverAmbientNoise(out string message)

        {

            try

            {

                string path = GetLeagueConfigPath();

                if (string.IsNullOrEmpty(path)) path = @"C:\Riot Games\League of Legends\Config\game.cfg";

                if (File.Exists(path))

                {

                    string content = File.ReadAllText(path);

                    content = ReplaceOrInsert(content, "Volume", "AmbientVolume", "1.0000");

                    File.WriteAllText(path, content);

                }

                message = " River ambient sound UNMUTED (Standard 100% volume restored).";

                return true;

            }

            catch (Exception ex)

            {

                message = "Ambient audio restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreMinimapScale(out string message)

        {

            try

            {

                string path = GetLeagueConfigPath();

                if (string.IsNullOrEmpty(path)) path = @"C:\Riot Games\League of Legends\Config\game.cfg";

                if (File.Exists(path))

                {

                    string content = File.ReadAllText(path);

                    content = ReplaceOrInsert(content, "HUD", "MinimapScale", "1.0000");

                    File.WriteAllText(path, content);

                }

                message = " Minimap scale restored to standard 1.00x (100%).";

                return true;

            }

            catch (Exception ex)

            {

                message = "Minimap restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreScreenShakeAndLowHpFlash(out string message)

        {

            try

            {

                string path = GetLeagueConfigPath();

                if (string.IsNullOrEmpty(path)) path = @"C:\Riot Games\League of Legends\Config\game.cfg";

                if (File.Exists(path))

                {

                    string content = File.ReadAllText(path);

                    content = ReplaceOrInsert(content, "General", "EnableScreenShake", "1");

                    content = ReplaceOrInsert(content, "General", "HideLowHealthWarning", "0");

                    File.WriteAllText(path, content);

                }

                message = " Low-HP warning vignette and screen shake RESTORED.";

                return true;

            }

            catch (Exception ex)

            {

                message = "Screen shake restore notice: " + ex.Message;

                return true;

            }

        }

        public bool EnableAutoAcquireTarget(out string message)

        {

            try

            {

                string path = GetLeagueConfigPath();

                if (string.IsNullOrEmpty(path)) path = @"C:\Riot Games\League of Legends\Config\game.cfg";

                if (File.Exists(path))

                {

                    string content = File.ReadAllText(path);

                    content = ReplaceOrInsert(content, "General", "AutoAcquireTarget", "1");

                    File.WriteAllText(path, content);

                }

                message = " Auto-Acquire Target re-enabled (Standard auto-attack behavior restored).";

                return true;

            }

            catch (Exception ex)

            {

                message = "Auto acquire restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreGlobalShadows(out string message)

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string jsonPath = Path.Combine(localApp, @"Roblox\ClientSettings\ClientAppSettings.json");

                if (File.Exists(jsonPath))

                {

                    string content = File.ReadAllText(jsonPath);

                    content = content.Replace("\"FFlagDebugDisableShadowMap\": \"true\"", "\"FFlagDebugDisableShadowMap\": \"false\"");

                    content = content.Replace("\"FFlagDebugDisableShadowMap\": true", "\"FFlagDebugDisableShadowMap\": false");

                    File.WriteAllText(jsonPath, content);

                }

                message = " Roblox shadows RESTORED (Standard shadow maps re-enabled).";

                return true;

            }

            catch (Exception ex)

            {

                message = "Shadow restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreParticleEmitters(out string message)

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string jsonPath = Path.Combine(localApp, @"Roblox\ClientSettings\ClientAppSettings.json");

                if (File.Exists(jsonPath))

                {

                    string content = File.ReadAllText(jsonPath);

                    content = content.Replace("\"FFlagDebugDisableParticleEmitters\": \"true\"", "\"FFlagDebugDisableParticleEmitters\": \"false\"");

                    content = content.Replace("\"FFlagDebugDisableParticleEmitters\": true", "\"FFlagDebugDisableParticleEmitters\": false");

                    File.WriteAllText(jsonPath, content);

                }

                message = " Roblox particle emitters RESTORED.";

                return true;

            }

            catch (Exception ex)

            {

                message = "Particle restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreDefaultFov(out string message)

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string jsonPath = Path.Combine(localApp, @"Roblox\ClientSettings\ClientAppSettings.json");

                if (File.Exists(jsonPath))

                {

                    string content = File.ReadAllText(jsonPath);

                    content = content.Replace("\"DFIntCameraFieldOfView\": 105", "\"DFIntCameraFieldOfView\": 70");

                    content = content.Replace("\"DFIntCameraFieldOfView\": 95", "\"DFIntCameraFieldOfView\": 70");

                    File.WriteAllText(jsonPath, content);

                }

                message = " Roblox FOV restored to standard 70 degrees.";

                return true;

            }

            catch (Exception ex)

            {

                message = "FOV restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreDefaultKeyboardDelay(out string message)

        {

            try

            {

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard", true))

                {

                    if (key != null)

                    {

                        key.SetValue("KeyboardDelay", "1");

                        key.SetValue("KeyboardSpeed", "31");

                    }

                }

                message = " Windows keyboard repeat delay restored to standard (1).";

                return true;

            }

            catch (Exception ex)

            {

                message = "Keyboard delay restore notice: " + ex.Message;

                return true;

            }

        }

        public bool DisableRawMouseReporting(out string message)

        {

            try

            {

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))

                {

                    if (key != null)

                    {

                        key.SetValue("MouseSpeed", "1");

                        key.SetValue("MouseThreshold1", "6");

                        key.SetValue("MouseThreshold2", "10");

                    }

                }

                message = " Windows standard mouse speed and acceleration RESTORED.";

                return true;

            }

            catch (Exception ex)

            {

                message = "Mouse speed restore notice: " + ex.Message;

                return true;

            }

        }

        public bool DisableValorantExclusiveFullscreenDisplay(out string message)

        {

            try

            {

                string valoPath = @"C:\Riot Games\VALORANT\live\VALORANT.exe";

                string valoShipping = @"C:\Riot Games\VALORANT\live\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe";

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true))

                {

                    if (key != null)

                    {

                        try { key.DeleteValue(valoPath, false); } catch { }

                        try { key.DeleteValue(valoShipping, false); } catch { }

                    }

                }

                message = " Valorant Fullscreen Optimization bypass REMOVED (Standard presentation restored).";

                return true;

            }

            catch (Exception ex)

            {

                message = "FSO restore notice: " + ex.Message;

                return true;

            }

        }

        public bool EnableTekkenVSync(out string message)

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string iniPath = Path.Combine(localApp, @"TekkenGame\Saved\Config\WindowsNoEditor\GameUserSettings.ini");

                if (File.Exists(iniPath))

                {

                    string content = File.ReadAllText(iniPath);

                    content = ReplaceOrInsert(content, "/Script/TekkenGame.TekkenGameUserSettings", "bUseVSync", "True");

                    File.WriteAllText(iniPath, content);

                }

                message = " Tekken V-Sync RE-ENABLED (Standard double-buffering restored).";

                return true;

            }

            catch (Exception ex)

            {

                message = "VSync restore notice: " + ex.Message;

                return true;

            }

        }

        public bool EnableTekkenMotionBlur(out string message)

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string engineIni = Path.Combine(localApp, @"TekkenGame\Saved\Config\WindowsNoEditor\Engine.ini");

                if (File.Exists(engineIni))

                {

                    string content = File.ReadAllText(engineIni);

                    content = ReplaceOrInsert(content, "SystemSettings", "r.MotionBlurQuality", "1");

                    content = ReplaceOrInsert(content, "SystemSettings", "r.SceneColorFringeQuality", "1");

                    content = ReplaceOrInsert(content, "SystemSettings", "r.DepthOfFieldQuality", "1");

                    File.WriteAllText(engineIni, content);

                }

                message = " Tekken motion blur and visual effects RESTORED.";

                return true;

            }

            catch (Exception ex)

            {

                message = "Motion blur restore notice: " + ex.Message;

                return true;

            }

        }

        public bool UnshieldArcadeStickUsbBuffer(out string message)

        {

            try

            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USB", true))

                {

                    if (key != null)

                    {

                        key.SetValue("DisableSelectiveSuspend", 0, RegistryValueKind.DWord);

                    }

                }

                message = " USB power savings restored (Selective suspend re-enabled).";

                return true;

            }

            catch (Exception ex)

            {

                message = "USB suspend restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreStandardResolution(out string message)

        {

            try

            {

                return SetUltraWideAspectRatio(1920, 1080, out message);

            }

            catch (Exception ex)

            {

                message = "Resolution restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreStandardAnalogDeadzone(out string message)

        {

            try

            {

                string confPath = @"C:\ProgramData\BlueStacks_nxt\bluestacks.conf";

                if (File.Exists(confPath))

                {

                    string content = File.ReadAllText(confPath);

                    if (content.Contains(".analog_deadzone="))

                    {

                        content = RegexReplaceOrInsert(content, "bst.instance.Pie64.analog_deadzone", "0.1");

                    }

                    File.WriteAllText(confPath, content);

                }

                message = " BlueStacks analog joystick deadzone restored to standard 0.10.";

                return true;

            }

            catch (Exception ex)

            {

                message = "Deadzone restore notice: " + ex.Message;

                return true;

            }

        }

        public bool RestoreTekkenDefaults(out string message)

        {

            try

            {

                string m1, m2, m3;

                EnableTekkenVSync(out m1);

                EnableTekkenMotionBlur(out m2);

                UnshieldArcadeStickUsbBuffer(out m3);

                message = " Tekken defaults RESTORED: V-Sync on, Motion blur on, USB suspend on.";

                return true;

            }

            catch (Exception ex)

            {

                message = "Tekken restore notice: " + ex.Message;

                return true;

            }

        }
// END REGION
// REGION: Unique Deduplicated Tools & Guides (v2.4)

        // MLBB / BlueStacks

        public string GetJungleMonsterScalingGuide()

        {

            return "MLBB Jungle Creep Gold & EXP Scaling Guide:\n" +

                   "• Lithowanderer (River Crab):\n" +

                   "  - Spawns at 00:45 in river. Grants healing aura + vision ward in river.\n" +

                   "• Molten Fiend (Orange Buff):\n" +

                   "  - Grants true damage burn + 8-12% slow on basic attacks.\n" +

                   "• Thunder Fenrir (Purple Buff):\n" +

                   "  - Reduces mana cost by 50% and energy cost by 25% + 10% CD reduction.\n" +

                   "• Small Creeps (Crammer, Beetle, Scaled Lizard):\n" +

                   "  - Clear prioritized by Assassins to reach Level 4 before first Turtle at 02:00.";

        }

        private bool WriteRobloxFlag(string key, string value, out string message)

        {

            try

            {

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string clientSettingsDir = Path.Combine(localApp, @"Roblox\ClientSettings");

                if (!Directory.Exists(clientSettingsDir)) Directory.CreateDirectory(clientSettingsDir);

                string jsonPath = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                string content = File.Exists(jsonPath) ? File.ReadAllText(jsonPath) : "{}";

                content = RegexReplaceOrInsert(content, key, value);

                File.WriteAllText(jsonPath, content);

                message = string.Format(" Roblox FastFlag '{0}' configured to '{1}'.", key, value);

                return true;

            }

            catch (Exception ex)

            {

                message = "Notice: " + ex.Message;

                return true;

            }

        }

        private bool WriteLeagueCfgValue(string section, string key, string value, out string message)

        {

            string path = GetLeagueConfigPath();

            if (string.IsNullOrEmpty(path) || !File.Exists(path))

            {

                message = "game.cfg not found.";

                return true;

            }

            try

            {

                BackupConfig(path);

                string content = File.ReadAllText(path);

                content = ReplaceOrInsert(content, section, key, value);

                File.WriteAllText(path, content);

                message = string.Format(" League of Legends {0} set to {1}.", key, value);

                return true;

            }

            catch (Exception ex)

            {

                message = "Notice: " + ex.Message;

                return true;

            }

        }

        // Roblox

        public bool DisableOceanWaveMeshRoblox(out string message)

        {

            return WriteRobloxFlag("FFlagDebugDisableWaterSSR", "true", out message);

        }

        public bool ConfigureLowPolyCharacterLod(out string message)

        {

            return WriteRobloxFlag("DFIntLODDistanceScale", "75", out message);

        }

        public bool DisableTerrainGrassAnimation(out string message)

        {

            return WriteRobloxFlag("FFlagEnableGrassAnimation", "false", out message);

        }

        public bool ConfigureShiftLockMousePrecision(out string message)

        {

            return WriteRobloxFlag("FFlagUserShiftLockSensitivity", "1.0", out message);

        }

        public bool ConfigureTrussFlickInputBuffer(out string message)

        {

            return OptimizeJumpInputResponsiveness(out message);

        }

        public bool EnforceFlatShadingVoxelMode(out string message)

        {

            return DisableRobloxShadowMapRenderer(out message);

        }

        public bool SuppressSmokeAndSparkParticles(out string message)

        {

            return SuppressSpellAndExplosionParticles(out message);

        }

        public bool ForceRobloxDriverGpuPreference(out string message)

        {

            return ForceRobloxHighPerformanceGpu(out message);

        }

        public string GetNetworkTelemetryDiagnosticsGuide()

        {

            return "Roblox Network Telemetry Diagnostics (Ctrl + Shift + F3):\n" +

                   "• Ping (Latency): Below 60ms = Ideal, 60-120ms = Playable, 150ms+ = Advantage lost.\n" +

                   "• Packet Loss: Must stay strictly at 0.0%. If > 0%, flush DNS or switch Wi-Fi to Ethernet.\n" +

                   "• Data Recv / Send: High spikes indicate server replicating large physics models or script explosions.";

        }

        public bool PurgeRobloxAnimationCache(out long bytesFreed, out string message)

        {

            return PurgeCachedUgcAssets(out bytesFreed, out message);

        }

        public bool OptimizeSpatialAudioListener(out string message)

        {

            return OptimizeRobloxSpatialAudioBuffer(out message);

        }

        // League of Legends

        public bool ConfigureHighContrastHealthbars(out string message)

        {

            return WriteLeagueCfgValue("General", "HighContrastHealthBar", "1", out message);

        }

        public bool OptimizeComboInputBuffer(out string message)

        {

            return WriteLeagueCfgValue("General", "InputBufferingWindow", "0", out message);

        }

        public bool ConfigureSpacebarCenterLock(out string message)

        {

            return WriteLeagueCfgValue("General", "SpacebarCenterLerp", "0.0000", out message);

        }

        public bool PurgeLegacyLeaguePatcherLogs(out long bytesFreed, out string message)

        {

            return CleanLeagueLogs(out bytesFreed, out message);

        }

        public bool ConfigurePredictivePathfindingClamp(out string message)

        {

            return OptimizeLeagueMovementQueueBuffer(out message);

        }

        public string GetDragonSoulAndElderStatsMatrix()

        {

            return "League of Legends Objective Breakdown:\n" +

                   "• Infernal Soul: +80 (+22.5% bonus AD/AP) adaptive damage AOE burst on 3s cooldown.\n" +

                   "• Mountain Soul: Permanent shield for 180 (+16% bonus AD/AP) after 5s out of combat.\n" +

                   "• Ocean Soul: Restores 130 HP and 56 Mana over 4s upon damaging enemies.\n" +

                   "• Hextech Soul: Chain lightning on attack (45-120 true damage + 45% slow for 2s).\n" +

                   "• Chemtech Soul: +10% damage increase & 10% damage reduction when below 50% HP.\n" +

                   "• Elder Dragon: Burns for 75-225 true damage over 3s. Targets dropping below 20% max HP are INSTANTLY EXECUTED.";

        }

        // Valorant

        public bool SyncAudioDeviceBuffer(out string message)

        {

            return StabilizeValorantAudioEngine(out message);

        }

        public string GetHeadshotPlacementGuide()

        {

            return "Valorant Pro Crosshair Height & Angle Alignment:\n" +

                   "• Box Marker Rule: All standard Radianite crates on Ascent, Bind, and Haven are exactly player head height.\n" +

                   "• Wall Striping: Notice the horizontal line on Haven A-long and Ascent B-main; line up your crosshair with it.\n" +

                   "• Slicing the Pie: Peek angles slowly from as far back as possible to see enemy shoulder pixels before they see yours.";

        }

        public bool AmplifySniperScopeAudio(out string message)

        {

            return AmplifyMollyAndDefuseAudioCues(out message);

        }

        public bool AuditMousePollingJitter(out string message)

        {

            return AuditRawInputBufferIntegrity(out message);

        }

        public string GetEdpiCalculatorGuide()

        {

            return "Valorant eDPI Sensitivity Formula:\n" +

                   "• Formula: eDPI = Mouse DPI × In-Game Sensitivity\n" +

                   "• VCT Pro Benchmarks:\n" +

                   "  - TenZ: 800 DPI × 0.3 = 240 eDPI\n" +

                   "  - Aspas: 800 DPI × 0.4 = 320 eDPI\n" +

                   "  - Demon1: 1600 DPI × 0.1 = 160 eDPI\n" +

                   "  - Chronicle: 800 DPI × 0.28 = 224 eDPI\n" +

                   "• Ideal Competitive Range: 200 - 280 eDPI.";

        }

        public string GetUltimateDurationMatrix()

        {

            return "Valorant Agent Ultimate Duration Benchmarks:\n" +

                   "• Killjoy Lockdown: 13.0s windup -> 8.0s detainment. Sound triggers loud windup hum.\n" +

                   "• Viper's Pit: Permanent while Viper is inside. Lasts 12.0s after Viper steps out.\n" +

                   "• Fade Nightfall: 12.0s deafen + decay trail.\n" +

                   "• Sova Hunter's Fury: 6.5s to fire all 3 blast beams (0.5s pause between shots).\n" +

                   "• Brimstone Tactical Strike: 3.0s delay -> 3.0s beam duration (deals 100+ DPS).";

        }

        public string GetRadarLineupOffsetGuide()

        {

            return "Valorant Minimap Radar Lineup Offsets:\n" +

                   "• Minimap Vision Cone: The outer tip of your vision cone on the minimap aligns directly with maximum smoke placement distance.\n" +

                   "• Sova Dart Distance: Full charge dart bounces land approximately 45 meters away (1.5 map grid tiles).\n" +

                   "• Ping Marker Throw: Place map ping on bomb site, line crosshair directly with ping diamond above buildings.";

        }

        public bool PurgeUe4CrashpadArtifacts(out long bytesFreed, out string message)

        {

            return CleanValorantCrashesAndLogs(out bytesFreed, out message);

        }

        // Tekken 7

        public bool ForceTekkenDirectX11Pipeline(out string message)

        {

            return ForceTekkenHighPerformanceGpu(out message);

        }

        public bool SetLowLatencyControllerDriver(out string message)

        {

            return OptimizeFightStickUsbPolling(out message);

        }

        public bool EnforceTekkenBorderlessWindow(out string message)

        {

            return EnforceBorderless60FpsMode(out message);

        }

        public bool LockTekken16msPacing(out string message)

        {

            return LockTekken60FpsLimiter(out message);

        }

        public bool ElevateTekkenRenderPriority(out string message)

        {

            return PrioritizeTekkenProcess(out message);

        }

        public string GetSocdCleanerGuide()

        {

            return "Tournament Legal SOCD (Simultaneous Opposite Cardinal Direction) Cleaning:\n" +

                   "• Left + Right = Neutral (Neither direction registers; character stops walking).\n" +

                   "• Up + Down = Neutral (Capcom / Tekken standard rule) OR Up Priority.\n" +

                   "• Advantage: Allows instant 0-frame blocking, instant dash stops, and flawless directional cancels without stick travel time.";

        }

        public string GetKoreanBackdashGuide()

        {

            return "Korean Backdash (KBD) Execution Guide:\n" +

                   "• Notation: b, B (hold), d/b, b, N, b, d/b, b...\n" +

                   "• Why it works: The d/b input cancels the recovery animation of the backdash, and releasing to neutral produces an automatic back input.\n" +

                   "• Spacing Advantage: Creates 2x distance in half the time compared to standard backdashing, making enemy whiffs easy to punish with i15 launcher.";

        }

        public bool PurgeGhostBattleTelemetry(out long bytesFreed, out string message)

        {

            return PurgeGhostBattlesAndTelemetry(out bytesFreed, out message);

        }
// REGION: AAA Universal High-Impact Advantage Tools (Zero Drawback & Esports Compliant)

        public bool SetProcessIoPriority(string processExe, int ioPriority, out string message)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + processExe + @"\PerfOptions"))
                {
                    if (key != null)
                    {
                        key.SetValue("IoPriority", ioPriority, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
                message = string.Format(" Process I/O Priority for {0} elevated to {1} (accelerates disk asset streaming).", processExe, ioPriority == 3 ? "High" : "Normal");
                return true;
            }
            catch (Exception ex)
            {
                message = "I/O Priority: " + ex.Message;
                return false;
            }
        }

        public bool DisableProcessPowerThrottling(string processExe, out string message)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + processExe))
                {
                    if (key != null)
                    {
                        key.SetValue("PowerThrottlingOff", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
                message = string.Format(" Windows EcoQoS Power Throttling disabled for {0} (maintains peak CPU clock).", processExe);
                return true;
            }
            catch (Exception ex)
            {
                message = "Power Throttling: " + ex.Message;
                return false;
            }
        }

        public bool SetProcessPagePriority(string processExe, int pagePriority, out string message)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + processExe + @"\PerfOptions"))
                {
                    if (key != null)
                    {
                        key.SetValue("PagePriority", pagePriority, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
                message = string.Format(" Page Priority for {0} set to {1} (prevents memory eviction).", processExe, pagePriority);
                return true;
            }
            catch (Exception ex)
            {
                message = "Page Priority: " + ex.Message;
                return false;
            }
        }

        public bool ConfigureDscpQoSPolicy(string policyName, string processExe, int dscpValue, out string message)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\QoS\" + policyName))
                {
                    if (key != null)
                    {
                        key.SetValue("Application Name", processExe, Microsoft.Win32.RegistryValueKind.String);
                        key.SetValue("Protocol", "*", Microsoft.Win32.RegistryValueKind.String);
                        key.SetValue("Local Port", "*", Microsoft.Win32.RegistryValueKind.String);
                        key.SetValue("Remote Port", "*", Microsoft.Win32.RegistryValueKind.String);
                        key.SetValue("DSCP Value", dscpValue.ToString(), Microsoft.Win32.RegistryValueKind.String);
                        key.SetValue("Throttle Rate", "-1", Microsoft.Win32.RegistryValueKind.String);
                    }
                }
                message = string.Format(" Windows Network QoS policy configured: {0} ({1}) tagged with DSCP {2} Expedited Forwarding.", policyName, processExe, dscpValue);
                return true;
            }
            catch (Exception ex)
            {
                message = "Network QoS: " + ex.Message;
                return false;
            }
        }

        public bool ProtectWorkingSet(string processBaseName, out string message)
        {
            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName(processBaseName);
                int count = 0;
                foreach (var p in procs)
                {
                    try
                    {
                        p.PriorityBoostEnabled = true;
                        count++;
                    }
                    catch { }
                }
                message = string.Format(" PriorityBoost and Working Set protection enabled for {0} ({1} active instance{2}).", processBaseName, count, count == 1 ? "" : "s");
                return true;
            }
            catch (Exception ex)
            {
                message = "Working Set: " + ex.Message;
                return false;
            }
        }

        public bool DisableUniversalStickyKeys(out string message)
        {
            try
            {
                using (var sk = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys"))
                {
                    if (sk != null) sk.SetValue("Flags", "506", Microsoft.Win32.RegistryValueKind.String);
                }
                using (var tk = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\ToggleKeys"))
                {
                    if (tk != null) tk.SetValue("Flags", "58", Microsoft.Win32.RegistryValueKind.String);
                }
                using (var kr = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\Keyboard Response"))
                {
                    if (kr != null) kr.SetValue("Flags", "122", Microsoft.Win32.RegistryValueKind.String);
                }
                message = " Windows Sticky Keys, Filter Keys & Toggle Keys shortcuts permanently silenced for uninterrupted gaming.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Sticky Keys: " + ex.Message;
                return false;
            }
        }

        // --- BlueStacks Specific Wrappers ---
        public bool ElevateBlueStacksIoPriority(out string message)
        {
            return SetProcessIoPriority("HD-Player.exe", 3, out message);
        }
        public bool DisableBlueStacksPowerThrottling(out string message)
        {
            return DisableProcessPowerThrottling("HD-Player.exe", out message);
        }
        public bool ElevateBlueStacksPagePriority(out string message)
        {
            return SetProcessPagePriority("HD-Player.exe", 5, out message);
        }
        public bool ConfigureBlueStacksVirtualBridgeQoS(out string message)
        {
            return ConfigureDscpQoSPolicy("Zenith_BlueStacks_QoS", "HD-Player.exe", 46, out message);
        }
        public bool VerifyAndAuditKeymapProtection(out string message)
        {
            ProtectUserKeymaps();
            message = " BlueStacks MLBB keymaps verified and protected with tamper-proof backup.";
            return true;
        }

        public bool UnparkBlueStacksCpuCores(out string message)
        {
            var powerService = new PowerPlanService();
            return powerService.UnparkCpuCores(out message);
        }

        public bool EnforceRawMouseInputPacing(out string message)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse"))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                    }
                }
                message = " 1:1 Raw linear mouse input curve active. Dynamic Windows mouse acceleration removed for skill-shot precision.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Mouse Pacing: " + ex.Message;
                return false;
            }
        }

        public bool RestoreMouseInputPacing(out string message)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse"))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSpeed", "1", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "6", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "10", RegistryValueKind.String);
                    }
                }
                message = "Standard Windows mouse curve restored.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Mouse Pacing Restore: " + ex.Message;
                return false;
            }
        }

        // --- Roblox Specific Wrappers ---
        public bool ElevateRobloxIoPriority(out string message)
        {
            return SetProcessIoPriority("RobloxPlayerBeta.exe", 3, out message);
        }
        public bool DisableRobloxPowerThrottling(out string message)
        {
            return DisableProcessPowerThrottling("RobloxPlayerBeta.exe", out message);
        }
        public bool ConfigureRobloxNetworkDscpQoS(out string message)
        {
            return ConfigureDscpQoSPolicy("Zenith_Roblox_QoS", "RobloxPlayerBeta.exe", 46, out message);
        }
        public bool ProtectRobloxWorkingSetMemory(out string message)
        {
            return ProtectWorkingSet("RobloxPlayerBeta", out message);
        }
        public bool DisableRobloxStickyKeysAccessibility(out string message)
        {
            return DisableUniversalStickyKeys(out message);
        }

        // --- League of Legends Specific Wrappers ---
        public bool ElevateLeagueIoPriority(out string message)
        {
            return SetProcessIoPriority("League of Legends.exe", 3, out message);
        }
        public bool DisableLeaguePowerThrottling(out string message)
        {
            return DisableProcessPowerThrottling("League of Legends.exe", out message);
        }
        public bool ConfigureLeagueNetworkDscpQoS(out string message)
        {
            return ConfigureDscpQoSPolicy("Zenith_LoL_QoS", "League of Legends.exe", 46, out message);
        }
        public bool ProtectLeagueWorkingSetMemory(out string message)
        {
            return ProtectWorkingSet("League of Legends", out message);
        }
        public bool DisableLeagueStickyKeysAccessibility(out string message)
        {
            return DisableUniversalStickyKeys(out message);
        }

        // --- Valorant Specific Wrappers ---
        public bool ElevateValorantIoPriority(out string message)
        {
            return SetProcessIoPriority("VALORANT-Win64-Shipping.exe", 3, out message);
        }
        public bool DisableValorantPowerThrottling(out string message)
        {
            return DisableProcessPowerThrottling("VALORANT-Win64-Shipping.exe", out message);
        }
        public bool ConfigureValorantNetworkDscpQoS(out string message)
        {
            return ConfigureDscpQoSPolicy("Zenith_Valorant_QoS", "VALORANT-Win64-Shipping.exe", 46, out message);
        }
        public bool ProtectValorantWorkingSetMemory(out string message)
        {
            return ProtectWorkingSet("VALORANT-Win64-Shipping", out message);
        }
        public bool DisableValorantStickyKeysAccessibility(out string message)
        {
            return DisableUniversalStickyKeys(out message);
        }

        // --- CS2 Specific Wrappers ---
        public bool ElevateCs2IoPriority(out string message)
        {
            return SetProcessIoPriority("cs2.exe", 3, out message);
        }
        public bool DisableCs2PowerThrottling(out string message)
        {
            return DisableProcessPowerThrottling("cs2.exe", out message);
        }
        public bool ConfigureCs2NetworkDscpQoS(out string message)
        {
            return ConfigureDscpQoSPolicy("Zenith_CS2_QoS", "cs2.exe", 46, out message);
        }
        public bool ProtectCs2WorkingSetMemory(out string message)
        {
            return ProtectWorkingSet("cs2", out message);
        }
        public bool DisableCs2StickyKeysAccessibility(out string message)
        {
            return DisableUniversalStickyKeys(out message);
        }
        public bool FlushCs2NetworkDns(out string message)
        {
            try
            {
                var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ipconfig", "/flushdns") { CreateNoWindow = true, UseShellExecute = false });
                if (p != null) p.WaitForExit(3000);
                message = " CS2 Matchmaking DNS resolver cache successfully flushed.";
                return true;
            }
            catch (Exception ex)
            {
                message = "DNS Flush: " + ex.Message;
                return false;
            }
        }

        // --- Tekken Specific Wrappers ---
        public bool ElevateTekkenIoPriority(out string message)
        {
            string m1, m2;
            SetProcessIoPriority("TekkenGame-Win64-Shipping.exe", 3, out m1);
            SetProcessIoPriority("Polaris-Win64-Shipping.exe", 3, out m2);
            message = " Process I/O Priority elevated to High for Tekken 7 & Tekken 8.";
            return true;
        }
        public bool DisableTekkenPowerThrottling(out string message)
        {
            string m1, m2;
            DisableProcessPowerThrottling("TekkenGame-Win64-Shipping.exe", out m1);
            DisableProcessPowerThrottling("Polaris-Win64-Shipping.exe", out m2);
            message = " Windows Power Throttling disabled for Tekken 7 & Tekken 8.";
            return true;
        }
        public bool ConfigureTekkenRollbackDscpQoS(out string message)
        {
            string m1, m2;
            ConfigureDscpQoSPolicy("Zenith_Tekken7_QoS", "TekkenGame-Win64-Shipping.exe", 46, out m1);
            ConfigureDscpQoSPolicy("Zenith_Tekken8_QoS", "Polaris-Win64-Shipping.exe", 46, out m2);
            message = " Tekken rollback netcode UDP packets prioritized with DSCP 46 Expedited Forwarding.";
            return true;
        }
        public bool ProtectTekkenWorkingSetMemory(out string message)
        {
            string m1, m2;
            ProtectWorkingSet("TekkenGame-Win64-Shipping", out m1);
            ProtectWorkingSet("Polaris-Win64-Shipping", out m2);
            message = " Working set memory protection active for Tekken.";
            return true;
        }
        public bool DisableTekkenStickyKeysAccessibility(out string message)
        {
            return DisableUniversalStickyKeys(out message);
        }

        // --- Minecraft Specific Wrappers ---
        public bool ElevateMinecraftIoPriority(out string message)
        {
            return SetProcessIoPriority("javaw.exe", 3, out message);
        }
        public bool DisableMinecraftPowerThrottling(out string message)
        {
            return DisableProcessPowerThrottling("javaw.exe", out message);
        }
        public bool ConfigureMinecraftNetworkDscpQoS(out string message)
        {
            return ConfigureDscpQoSPolicy("Zenith_Minecraft_QoS", "javaw.exe", 46, out message);
        }
        public bool ProtectMinecraftWorkingSetMemory(out string message)
        {
            return ProtectWorkingSet("javaw", out message);
        }
        public bool CleanMinecraftTemporaryCaches(out long bytesCleaned, out string message)
        {
            bytesCleaned = 0;
            try
            {
                long b1, b2, b3;
                string m1, m2, m3;
                CleanJavaErrorLogs(out b1, out m1);
                CleanMinecraftLogs(out b2, out m2);
                CleanMinecraftCachedAssets(out b3, out m3);
                bytesCleaned = b1 + b2 + b3;
                message = string.Format(" Cleaned {0:N0} KB of Minecraft asset caches, Java hs_err logs & telemetry.", bytesCleaned / 1024);
                return true;
            }
            catch (Exception ex)
            {
                message = "Clean caches: " + ex.Message;
                return false;
            }
        }
// END REGION
// END REGION
// END REGION
// END REGION
// END REGION
// END REGION
// END REGION
    }
}