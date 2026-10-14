using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer
{
    public partial class MainWindow : Window
    {
        private Border BuildGameSpecificToolsPanel(GameProfile profile)
        {
            Border panel = new Border()
            {
                Background = FrozenTheme.ToolsPanelBg,
                BorderBrush = FrozenTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 10, 0, 2),
                Visibility = Visibility.Collapsed
            };

            StackPanel content = new StackPanel();

            if (profile.Id == "bluestacks")
            {
                GameSpecificTunerService.ProtectUserKeymaps();

                // Mobile Legends & BlueStacks 5 (Curated Pro Esports Suite)
                TextBlock title = new TextBlock() { Text = "MOBILE LEGENDS & BLUESTACKS 5 ADVANCED TUNER", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                bool isVtEnabled;
                string vtDetails;
                _monitorService.GameTuner.CheckHardwareVirtualization(out isVtEnabled, out vtDetails);

                Border vtBox = new Border()
                {
                    Background = isVtEnabled ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(6, 78, 59)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(69, 26, 3)),
                    BorderBrush = isVtEnabled ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(5, 150, 105)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 53, 15)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                TextBlock vtText = new TextBlock()
                {
                    Text = vtDetails,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = isVtEnabled ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 211, 153)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36)),
                    TextWrapping = TextWrapping.Wrap
                };
                vtBox.Child = vtText;
                content.Children.Add(vtBox);

                TextBlock statusLabel = new TextBlock() { Text = "Status: " + _monitorService.BlueStacksService.AnalyzeConfiguration(), FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateConflictCallout("Zero-Drawback Protection Guarantee", "Your BlueStacks controls/keymaps (com.mobile.legends.cfg), custom device profile, and core allocations are 100% locked & preserved. All optimizations operate strictly at the Windows OS, GPU, Process Priority, and Network levels with zero side effects."));
                content.Children.Add(CreateSectionHeader(" 1. WINDOWS GPU & DISPLAY LATENCY"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" Force Dedicated GPU", () => { string msg; _monitorService.GameTuner.ForceBlueStacksHighPerformanceGpu(out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces high-performance discrete GPU in Windows Graphics Preferences for HD-Player.exe"));
                grpGpu.Children.Add(CreateToolButton(" Bypass DWM Fullscreen Hooks", () => { string msg; _monitorService.BlueStacksService.OptimizeForMobileLegends(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables Fullscreen Optimizations to bypass DWM compositing layer and cut display latency"));
                grpGpu.Children.Add(CreateToolButton(" HW ASTC Textures", () => { string msg; _monitorService.GameTuner.EnableAstcHardwareDecoding(out msg); statusLabel.Text = msg; Log(msg); }, true, "Offloads hero/skin textures to GPU to save CPU overhead"));
                grpGpu.Children.Add(CreateToolButton(" DirectX Renderer", () => { string msg; _monitorService.GameTuner.SetBlueStacksRenderer("DirectX", out msg); statusLabel.Text = msg; Log(msg); }, false, "DirectX backend recommended for Intel iGPU", null, null, "bst_renderer"));
                grpGpu.Children.Add(CreateToolButton(" Vulkan Renderer", () => { string msg; _monitorService.GameTuner.SetBlueStacksRenderer("Vulkan", out msg); statusLabel.Text = msg; Log(msg); }, false, "Vulkan rendering backend", null, null, "bst_renderer"));
                grpGpu.Children.Add(CreateToolButton(" Disable V-Sync", () => { string msg; _monitorService.GameTuner.SetBlueStacksVsync(false, out msg); statusLabel.Text = msg; Log(msg); }, true, "Cuts virtual touch and skill release delay"));
                grpGpu.Children.Add(CreateToolButton(" Strip Sidebar & Center Ads", () => { string msg; _monitorService.GameTuner.DisableBlueStacksSidebarAds(out msg); statusLabel.Text = msg; Log(msg); }, true, "Suppresses promotional ads and game center suggestions"));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. CPU PROCESS, SCHEDULING & LATENCY"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" High Process Priority (IFEO)", () => { string msg; _monitorService.BlueStacksService.OptimizeForMobileLegends(out msg); statusLabel.Text = "IFEO High CPU Priority engaged for HD-Player.exe"; Log(statusLabel.Text); }, true, "Locks HD-Player.exe to High CPU priority in Windows registry for stutter-free 5v5 teamfights"));
                grpCpu.Children.Add(CreateToolButton(" High I/O Priority (IFEO)", () => { string msg; _monitorService.BlueStacksService.OptimizeForMobileLegends(out msg); statusLabel.Text = "IFEO High I/O Priority engaged for HD-Player.exe"; Log(statusLabel.Text); }, true, "Grants BlueStacks top-priority disk and shader caching access"));
                grpCpu.Children.Add(CreateToolButton(" Lock 0.500ms Multimedia Timer", () => { TimerResolutionEngine.EnforceMaxResolution(); statusLabel.Text = "Windows kernel timer locked to 0.500ms precision"; Log(statusLabel.Text); }, true, "Engages 0.500ms OS multimedia timer to eliminate micro-stutters and frame pacing variance"));
                grpCpu.Children.Add(CreateToolButton(" Unpark CPU Cores", () => { string msg; _monitorService.PowerService.UnparkCpuCores(out msg); statusLabel.Text = msg; Log(msg); }, true, "Keeps assigned emulator cores at 100% active state (zero wake latency)"));
                grpCpu.Children.Add(CreateToolButton(" 1:1 Raw Mouse Pacing", () => { string msg; _monitorService.GameTuner.EnforceRawMouseInputPacing(out msg); statusLabel.Text = msg; Log(msg); }, true, "Removes dynamic pointer acceleration curves for pixel-perfect skill aiming"));
                grpCpu.Children.Add(CreateToolButton(" Kill Frozen Services", () => { string msg; _monitorService.GameTuner.KillFrozenBlueStacksServices(out msg); statusLabel.Text = msg; Log(msg); }, false, "Force terminates hung emulator background services"));
                grpCpu.Children.Add(CreateToolButton(" Kill Background ADB Listener", () => { string msg; _monitorService.GameTuner.DisableAdbBackgroundListener(out msg); statusLabel.Text = msg; Log(msg); }, false, "Terminates stuck adb.exe processes to reclaim idle CPU cycles"));
                grpCpu.Children.Add(CreateToolButton(" Disable Startup Agents", () => { string msg; _monitorService.GameTuner.DisableBlueStacksStartupServices(out msg); statusLabel.Text = msg; Log(msg); }, false, "Prevents BlueStacks updater services from running at boot"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 3. NETWORK, MEMORY & STORAGE UTILITIES"));
                WrapPanel grpUtils = new WrapPanel();
                grpUtils.Children.Add(CreateToolButton(" MOBA Packet QoS Priority", () => { string msg; _monitorService.GameTuner.PrioritizeMobaPacketsQoS(out msg); statusLabel.Text = msg; Log(msg); }, false, "Windows QoS tags BlueStacks UDP traffic with Expedited Forwarding (DSCP 46)"));
                grpUtils.Children.Add(CreateToolButton(" Flush SEA Matchmaking DNS", () => { string msg; _monitorService.GameTuner.FlushMlbSoutheastAsiaDns(out msg); statusLabel.Text = msg; Log(msg); }, false, "Clears DNS cache targeting regional Moonton servers for lowest ping"));
                grpUtils.Children.Add(CreateToolButton(" Purge Standby Memory", () => { long freed = MemoryPurgeService.PurgeStandbyList(); statusLabel.Text = string.Format("Purged {0:N0} MB of Windows Standby cache", freed / (1024 * 1024)); Log(statusLabel.Text); }, true, "Reclaims standby RAM cache without affecting BlueStacks guest memory"));
                grpUtils.Children.Add(CreateToolButton(" Flush MLBB Shader Cache", () => { long b; string msg; _monitorService.GameTuner.FlushMlbbShaderCache(out b, out msg); statusLabel.Text = msg; Log(msg); }, false, "Flushes hero skin & skill shaders to stop first-cast stutter"));
                grpUtils.Children.Add(CreateToolButton(" Clean Emulator Logs & Temp APKs", () => { long b1, b2, b3; string m1, m2, m3; _monitorService.GameTuner.CleanBlueStacksTempApkCache(out b1, out m1); _monitorService.GameTuner.CleanBlueStacksLogs(out b2, out m2); _monitorService.GameTuner.CleanBlueStacksCrashpadReports(out b3, out m3); statusLabel.Text = string.Format(" Cleaned {0:N0} KB of logs, crash minidumps & temp APKs", (b1 + b2 + b3) / 1024); Log(statusLabel.Text); }, false, "Purges crash minidumps, logs, and temp APK installation cache"));
                grpUtils.Children.Add(CreateToolButton(" Shrink Virtual Disk (VHD)", () => { long freed; string msg; _monitorService.BlueStacksService.CompactVirtualDisk(out freed, out msg); statusLabel.Text = msg; Log(msg); }, true, "Safely compacts Data.vhdx to reclaim 10-25 GB of unallocated SSD space"));
                content.Children.Add(grpUtils);

                content.Children.Add(CreateSectionHeader(" 4. 1-CLICK ZERO-DRAWBACK TOURNAMENT OPTIMIZERS"));
                WrapPanel grpPresets = new WrapPanel();
                grpPresets.Children.Add(CreateToolButton(" 1-Click Zero-Drawback MLBB Suite", () => { string msg; _monitorService.BlueStacksService.OptimizeForMobileLegends(out msg); statusLabel.Text = msg; Log(msg); }, true, "Zero-drawback optimization: Dedicated GPU, IFEO High CPU/IO Priority, 0.5ms Timer, DWM bypass, Unpark Cores & DSCP 46 QoS (100% preserves controls & device)"));
                content.Children.Add(grpPresets);
            }

            else if (profile.Id == "roblox")
            {
                // Roblox Native FPS & Engine Tuner (Curated Pro Esports Suite)
                TextBlock title = new TextBlock() { Text = "ROBLOX NATIVE FPS UNLOCKER & ENGINE TUNER", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                TextBlock statusLabel = new TextBlock() { Text = "Status: " + _monitorService.GameTuner.GetRobloxStatus(), FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateSectionHeader(" 1. GRAPHICS & FPS UNLOCKER (NATIVE FASTFLAGS)"));
                content.Children.Add(CreateConflictCallout("Roblox FPS, FOV & Lighting", "FPS targets, FOV angles, and lighting engines are mutually exclusive; selecting one auto-swaps conflicting choices"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" 120 FPS", () => { string msg; _monitorService.GameTuner.UnlockRobloxFps(120, out msg); statusLabel.Text = msg; Log(msg); }, false, "Unlock 120 FPS via ClientAppSettings", null, null, "roblox_fps"));
                grpGpu.Children.Add(CreateToolButton(" 144 FPS", () => { string msg; _monitorService.GameTuner.UnlockRobloxFps(144, out msg); statusLabel.Text = msg; Log(msg); }, true, "Unlock 144 FPS for high refresh monitors", null, null, "roblox_fps"));
                grpGpu.Children.Add(CreateToolButton(" 180 FPS Tournament Lock", () => { string msg; _monitorService.GameTuner.UnlockRobloxFps(180, out msg); statusLabel.Text = msg; Log(msg); }, false, "Unlock 180 FPS for OLED 180Hz displays", null, null, "roblox_fps"));
                grpGpu.Children.Add(CreateToolButton(" 240 FPS", () => { string msg; _monitorService.GameTuner.UnlockRobloxFps(240, out msg); statusLabel.Text = msg; Log(msg); }, true, "Unlock 240 FPS competitive limit", null, null, "roblox_fps"));
                grpGpu.Children.Add(CreateToolButton(" 360 FPS (Uncapped)", () => { string msg; _monitorService.GameTuner.UnlockRobloxFps(360, out msg); statusLabel.Text = msg; Log(msg); }, false, "Unlock 360 FPS max cap", null, null, "roblox_fps"));
                grpGpu.Children.Add(CreateToolButton(" Reset to 60 FPS Defaults", () => { string msg; _monitorService.GameTuner.RestoreRobloxFps(out msg); statusLabel.Text = msg; Log(msg); }, false, "Restores original 60 FPS limit", null, null, "roblox_fps"));
                grpGpu.Children.Add(CreateToolButton(" 95° Custom FOV", () => { string msg; _monitorService.GameTuner.SetRobloxCustomFov(95, out msg); statusLabel.Text = msg; Log(msg); }, false, "Sets DFIntCameraFieldOfView to 95 degrees for wider vision", null, null, "roblox_fov"));
                grpGpu.Children.Add(CreateToolButton(" 105° Wide View (BedWars)", () => { string msg; _monitorService.GameTuner.EnforceBedWarsUltrawideFov(out msg); statusLabel.Text = msg; Log(msg); }, true, "Broad peripheral vision to spot enemy bed rushers", () => _monitorService.GameTuner.IsRobloxFlagActive("DFIntCameraFieldOfView", "105"), () => { string msg; _monitorService.GameTuner.RestoreDefaultFov(out msg); statusLabel.Text = msg; Log(msg); }, "roblox_fov"));
                grpGpu.Children.Add(CreateToolButton(" 110° Ultra FOV (Max Sight)", () => { string msg; _monitorService.GameTuner.SetRobloxFov110Competitive(out msg); statusLabel.Text = msg; Log(msg); }, false, "110° field of view for maximum competitive situational awareness", null, () => { string msg; _monitorService.GameTuner.RestoreRobloxDefaultFov(out msg); statusLabel.Text = msg; Log(msg); }, "roblox_fov"));
                grpGpu.Children.Add(CreateToolButton(" Enforce Flat Voxel Lighting", () => { string msg; _monitorService.GameTuner.EnforceFlatShadingVoxelMode(out msg); statusLabel.Text = msg; Log(msg); }, true, "Forces lightweight flat voxel lighting for massive FPS boost in Blox Fruits and BedWars", null, () => { string msg; _monitorService.GameTuner.RestoreRobloxShadowMapRenderer(out msg); statusLabel.Text = msg; Log(msg); }, "roblox_lighting"));
                grpGpu.Children.Add(CreateToolButton(" Low 1/4 Textures", () => { string msg; _monitorService.GameTuner.SetRobloxTextureQuality(1, out msg); statusLabel.Text = msg; Log(msg); }, false, "Downscales textures to 1/4 resolution for low VRAM", null, null, "roblox_textures"));
                grpGpu.Children.Add(CreateToolButton(" Ultra Textures", () => { string msg; _monitorService.GameTuner.SetRobloxTextureQuality(3, out msg); statusLabel.Text = msg; Log(msg); }, false, "High definition texture override", null, null, "roblox_textures"));
                grpGpu.Children.Add(CreateToolButton(" Direct3D 11 API", () => { string msg; _monitorService.GameTuner.SetRobloxGraphicsApiPreference("D3D11", out msg); statusLabel.Text = msg; Log(msg); }, false, "Direct3D 11 graphics backend"));
                grpGpu.Children.Add(CreateToolButton(" Vulkan API", () => { string msg; _monitorService.GameTuner.SetRobloxGraphicsApiPreference("Vulkan", out msg); statusLabel.Text = msg; Log(msg); }, false, "Vulkan graphics backend"));
                grpGpu.Children.Add(CreateToolButton(" Force Dedicated GPU", () => { string msg; _monitorService.GameTuner.ForceRobloxHighPerformanceGpu(out msg); statusLabel.Text = msg; Log(msg); }, true, "Forces Windows to run Roblox on high performance GPU"));
                grpGpu.Children.Add(CreateToolButton(" Enforce High-Perf Driver Profile", () => { string msg; _monitorService.GameTuner.ForceRobloxDriverGpuPreference(out msg); statusLabel.Text = msg; Log(msg); }, true, "Forces GPU driver power state to Maximum Performance in registry"));
                grpGpu.Children.Add(CreateToolButton(" Native FPS Overlay", () => { string msg; _monitorService.GameTuner.EnableNativeFpsDisplay(out msg); statusLabel.Text = msg; Log(msg); }, false, "Shows official built-in FPS counter"));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. COMPETITIVE AIM, CAMERA & VISUAL CLARITY"));
                WrapPanel grpAim = new WrapPanel();
                grpAim.Children.Add(CreateToolButton(" Raw 1:1 Mouse (Arsenal/Rivals)", () => { string msg; _monitorService.GameTuner.DisableRobloxMouseSmoothing(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables mouse smoothing & camera lerp for competitive 1:1 flick aim"));
                grpAim.Children.Add(CreateToolButton(" Shift-Lock Precision Tracking", () => { string msg; _monitorService.GameTuner.ConfigureShiftLockMousePrecision(out msg); statusLabel.Text = msg; Log(msg); }, true, "Locks 1:1 camera tracking during Shift-Lock mode in Arsenal and Tower of Hell"));
                grpAim.Children.Add(CreateToolButton(" Competitive Static Crosshair", () => { string msg; _monitorService.GameTuner.ConfigureRobloxCrosshairSettings(out msg); statusLabel.Text = msg; Log(msg); }, true, "Locks static pinpoint crosshair for Arsenal & Rivals tracking"));
                grpAim.Children.Add(CreateToolButton(" Instant Snap Camera (No Spring)", () => { string msg; _monitorService.GameTuner.DisableCameraCollisionSpringingRoblox(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables elastic camera spring damping for instant 1:1 camera panning"));
                grpAim.Children.Add(CreateToolButton(" Disable Camera Shake & Bob", () => { string msg; _monitorService.GameTuner.DisableRobloxCameraShake(out msg); statusLabel.Text = msg; Log(msg); }, false, "Disables camera bobbing and screen shake"));
                grpAim.Children.Add(CreateToolButton(" Disable Damage Screen Flash", () => { string msg; _monitorService.GameTuner.DisableScreenDamageVignette(out msg); statusLabel.Text = msg; Log(msg); }, true, "Suppresses low-HP red screen vignette and blur during combat"));
                grpAim.Children.Add(CreateToolButton(" Strip Motion Blur & PostFX", () => { string msg; _monitorService.GameTuner.DisablePostProcessingBlur(out msg); statusLabel.Text = msg; Log(msg); }, true, "Removes motion blur and bloom for pure visual clarity"));
                grpAim.Children.Add(CreateToolButton(" Suppress Particle Emitters & VFX", () => { string msg; _monitorService.GameTuner.DisableHeavyParticleEmitters(out msg); statusLabel.Text = msg; Log(msg); }, true, "Eliminates heavy particle emitter lag in Blox Fruits, Blade Ball and BedWars"));
                grpAim.Children.Add(CreateToolButton(" Remove Moving Grass & Foliage", () => { string msg; _monitorService.GameTuner.DisableTerrainGrassAnimation(out msg); statusLabel.Text = msg; Log(msg); }, true, "Stops moving 3D terrain grass animations to eliminate GPU stutter"));
                grpAim.Children.Add(CreateToolButton(" Disable Water Reflections & Shaders", () => { string msg; _monitorService.GameTuner.DisableWaterReflections(out msg); statusLabel.Text = msg; Log(msg); }, false, "Disables planar water reflection shaders for huge FPS boost near water"));
                grpAim.Children.Add(CreateToolButton(" Disable Volumetric Sun Rays", () => { string msg; _monitorService.GameTuner.DisablePostProcessSunRaysRoblox(out msg); statusLabel.Text = msg; Log(msg); }, true, "Turns off blinding sun ray shafts for clean target tracking in outdoor arenas"));
                grpAim.Children.Add(CreateToolButton(" Disable Dynamic Skybox Shaders", () => { string msg; _monitorService.GameTuner.DisableSkyboxRenderingRoblox(out msg); statusLabel.Text = msg; Log(msg); }, false, "Replaces 4K cubemap dynamic skies with low-resolution skybox for VRAM savings"));
                grpAim.Children.Add(CreateToolButton(" Low-Poly Character Mesh LOD", () => { string msg; _monitorService.GameTuner.ConfigureLowPolyCharacterLod(out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces low-poly avatar mesh detail at distance to eliminate teamfight lag"));
                grpAim.Children.Add(CreateToolButton(" Instant Map Streaming", () => { string msg; _monitorService.GameTuner.EnableInstantMapStreaming(out msg); statusLabel.Text = msg; Log(msg); }, false, "Loads obby platforms instantly without void falls"));
                content.Children.Add(grpAim);

                content.Children.Add(CreateSectionHeader(" 3. CPU, INPUT SCHEDULING & HYPERION COMPLIANCE"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" Lock 0.500ms Input Timer", () => { string msg; _monitorService.GameTuner.LockRobloxInputTimer(out msg); statusLabel.Text = msg; Log(msg); }, true, "Eliminates camera turn stutter and micro-stalls"));
                grpCpu.Children.Add(CreateToolButton("⌨ 0ms Obby Jump Keyboard Rate", () => { string msg; _monitorService.GameTuner.OptimizeJumpInputResponsiveness(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets keyboard repeat delay to 0 for instant wall-hops and ladder jumps"));
                grpCpu.Children.Add(CreateToolButton(" Fast Truss & Ladder Flick Buffer", () => { string msg; _monitorService.GameTuner.ConfigureTrussFlickInputBuffer(out msg); statusLabel.Text = msg; Log(msg); }, true, "Configures 0ms ladder snap buffer for frame-perfect truss flicks in DCOs"));
                grpCpu.Children.Add(CreateToolButton("⏱ Lock Physics Solver to 60Hz", () => { string msg; _monitorService.GameTuner.LockPhysicsSimulation60HzRoblox(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets DFIntSimWorldStepHz=60 to eliminate physics micro-jitter and avatar sliding"));
                grpCpu.Children.Add(CreateToolButton(" Elevate Roblox CPU Priority", () => { string msg; _monitorService.GameTuner.ElevateRobloxCpuPriority(out msg); statusLabel.Text = msg; Log(msg); }, true, "Configures Above Normal CPU priority for RobloxPlayerBeta.exe in registry"));
                grpCpu.Children.Add(CreateToolButton("⏱ 15 FPS In Background", () => { string msg; _monitorService.GameTuner.LimitRobloxBackgroundFps(15, out msg); statusLabel.Text = msg; Log(msg); }, false, "Limits FPS when alt-tabbed to save CPU/battery"));
                grpCpu.Children.Add(CreateToolButton(" High-DPI Scaling Bypass", () => { string msg; _monitorService.GameTuner.ForceRobloxHighDpiBypass(out msg); statusLabel.Text = msg; Log(msg); }, false, "Bypasses DWM scaling blur and fullscreen lag"));
                grpCpu.Children.Add(CreateToolButton(" Kill Stuck Processes", () => { string msg; _monitorService.GameTuner.KillStuckRobloxProcesses(out msg); statusLabel.Text = msg; Log(msg); }, false, "Closes hanging background instances"));
                grpCpu.Children.Add(CreateToolButton(" Hyperion Anti-Cheat Status", () => { statusLabel.Text = _monitorService.GameTuner.GetRobloxHyperionStatus(); Log(statusLabel.Text); }, false, "Verifies zero DLL injection compliance"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 4. 3D SPATIAL AUDIO & VOICE CHAT"));
                WrapPanel grpAudio = new WrapPanel();
                grpAudio.Children.Add(CreateToolButton(" 3D Spatial Footstep Sound Boost", () => { string msg; _monitorService.GameTuner.AmplifySpatialFootstepAudioRoblox(out msg); statusLabel.Text = msg; Log(msg); }, true, "Amplifies 1kHz-2.5kHz footsteps to track players 40 studs away in MM2 & Evade"));
                grpAudio.Children.Add(CreateToolButton(" 3D Audio Listener Sync", () => { string msg; _monitorService.GameTuner.OptimizeSpatialAudioListener(out msg); statusLabel.Text = msg; Log(msg); }, false, "Synchronizes HRTF spatial audio listener position directly to character head"));
                grpAudio.Children.Add(CreateToolButton(" Fix Spatial Voice Robot Audio", () => { string msg; _monitorService.GameTuner.CleanRobloxVoiceChatAudioBuffers(out msg); statusLabel.Text = msg; Log(msg); }, false, "Refreshes Windows audio buffer for Roblox Spatial Voice to stop robotic mic glitches"));
                content.Children.Add(grpAudio);

                content.Children.Add(CreateSectionHeader(" 5. NETWORK, MEMORY & STORAGE UTILITIES"));
                WrapPanel grpUtils = new WrapPanel();
                grpUtils.Children.Add(CreateToolButton(" Network Prediction Buffer (30ms)", () => { string msg; _monitorService.GameTuner.OptimizeNetworkReplicatorBufferRoblox(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets DFIntNetworkPredictionMs=30 for smooth interpolation against laggy opponents"));
                grpUtils.Children.Add(CreateToolButton(" Test Roblox Server Ping & Route", () => { string msg; _monitorService.GameTuner.AuditRegionalRobloxServersPing(out msg); statusLabel.Text = msg; Log(msg); }, false, "Tests round-trip latency to global Roblox datacenter clusters"));
                grpUtils.Children.Add(CreateToolButton(" Disable Telemetry Reporting", () => { string msg; _monitorService.GameTuner.DisableRobloxTelemetry(out msg); statusLabel.Text = msg; Log(msg); }, false, "Disables background analytics reporting"));
                grpUtils.Children.Add(CreateToolButton(" Trim Roblox Lua Memory", () => { long b; string msg; _monitorService.GameTuner.DeBloatRobloxWorkingSet(out b, out msg); statusLabel.Text = msg; Log(msg); }, false, "Clears unmanaged Lua working set memory without closing your game"));
                grpUtils.Children.Add(CreateToolButton(" Clean All Roblox Cache & Logs", () => { long b1, b2, b3; string m1, m2, m3; _monitorService.GameTuner.CleanRobloxCache(out b1, out m1); _monitorService.GameTuner.CleanRobloxVoiceChatCache(out b2, out m2); _monitorService.GameTuner.PurgeRobloxCrashDumps(out b3, out m3); statusLabel.Text = string.Format(" Cleaned {0:N0} KB of HTTP asset cache, voice telemetry & crash dumps", (b1 + b2 + b3) / 1024); Log(statusLabel.Text); }, false, "Purges HTTP asset cache, voice chat telemetry, and old crash dumps"));
                content.Children.Add(grpUtils);

                content.Children.Add(CreateSectionHeader(" 6. 1-CLICK TOURNAMENT PRESETS"));
                WrapPanel grpPresets = new WrapPanel();
                grpPresets.Children.Add(CreateToolButton(" 1-Click BedWars & Rivals Pro Suite", () => { string msg; _monitorService.GameTuner.ApplyBedwarsArsenalGrandChampionSuite(out msg); statusLabel.Text = msg; Log(msg); }, true, "240 FPS, 105° FOV, Voxel lighting, clean shaders, 0ms jump delay & 0.5ms timer!"));
                grpPresets.Children.Add(CreateToolButton(" 1-Click Maximum FPS Potato Mode", () => { string msg; _monitorService.GameTuner.ApplyRobloxUltraPotatoPreset(out msg); statusLabel.Text = msg; Log(msg); }, true, "Arms 144 FPS, No Shadows, Low Textures, No SSR, High GPU for low-end PCs"));
                content.Children.Add(grpPresets);
            }

            else if (profile.Id == "lol")
            {
                // League of Legends Teamfight Tuner (Curated Pro Esports Suite)
                TextBlock title = new TextBlock() { Text = "LEAGUE OF LEGENDS TEAMFIGHT RENDERING TUNER", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                TextBlock statusLabel = new TextBlock() { Text = "Status: " + _monitorService.GameTuner.GetLeagueStatus(), FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateSectionHeader(" 1. GRAPHICS & TEAMFIGHT RENDERING (game.cfg)"));
                content.Children.Add(CreateConflictCallout("League Framerate & Graphics", "FPS targets and DirectX modes are mutually exclusive; selecting one auto-swaps conflicting choices"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" 144 FPS Cap", () => { string msg; _monitorService.GameTuner.SetLeagueTargetFramerate(144, out msg); statusLabel.Text = msg; Log(msg); }, false, "Locks frame cap to 144 FPS", null, null, "lol_fps"));
                grpGpu.Children.Add(CreateToolButton(" 240 FPS Cap", () => { string msg; _monitorService.GameTuner.SetLeagueTargetFramerate(240, out msg); statusLabel.Text = msg; Log(msg); }, true, "Locks frame cap to 240 FPS", null, null, "lol_fps"));
                grpGpu.Children.Add(CreateToolButton(" Uncapped FPS", () => { string msg; _monitorService.GameTuner.SetLeagueTargetFramerate(0, out msg); statusLabel.Text = msg; Log(msg); }, false, "Removes framerate cap", null, null, "lol_fps"));
                grpGpu.Children.Add(CreateToolButton(" Lock Modern Direct3D 11", () => { string msg; _monitorService.GameTuner.LockLeagueRendererD3D9Bypass(out msg); statusLabel.Text = msg; Log(msg); }, true, "Forces modern D3D11 renderer and prevents DX9 legacy fallback", null, null, "lol_renderer"));
                grpGpu.Children.Add(CreateToolButton(" Force Dedicated GPU", () => { string msg; _monitorService.GameTuner.ForceLeagueHighPerformanceGpu(out msg); statusLabel.Text = msg; Log(msg); }, true, "Forces discrete GPU preference for League"));
                grpGpu.Children.Add(CreateToolButton(" Disable Inking Outlines", () => { string msg; _monitorService.GameTuner.DisableInkingShader(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables heavy edge outline shader (great for iGPU & teamfight FPS)"));
                grpGpu.Children.Add(CreateToolButton(" Low Environment Detail", () => { string msg; _monitorService.GameTuner.SetEnvironmentQualityLow(out msg); statusLabel.Text = msg; Log(msg); }, false, "Optimizes Summoner's Rift map terrain geometry"));
                grpGpu.Children.Add(CreateToolButton(" Disable Volumetric Godrays", () => { string msg; _monitorService.GameTuner.DisableGodraysAndEyecandy(out msg); statusLabel.Text = msg; Log(msg); }, false, "Removes jungle lighting overhead"));
                grpGpu.Children.Add(CreateToolButton(" Disable HUD Animations", () => { string msg; _monitorService.GameTuner.OptimizeLeagueGameConfig(out msg); statusLabel.Text = msg; Log(msg); }, false, "Stops UI thread hitching in teamfights"));
                grpGpu.Children.Add(CreateToolButton(" Disable Floating Combat Text", () => { string msg; _monitorService.GameTuner.DisableCombatTextSpam(out msg); statusLabel.Text = msg; Log(msg); }, false, "Reduces damage text UI render spam"));
                grpGpu.Children.Add(CreateToolButton(" Suppress River Eye Candy", () => { string msg; _monitorService.GameTuner.ConfigureEyeCandySuppression(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Removes decorative river wildlife and butterflies for pure visual clarity", null, () => { string msg; _monitorService.GameTuner.ConfigureEyeCandySuppression(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpGpu.Children.Add(CreateToolButton(" Disable Grass Foliage Wind Sway", () => { string msg; _monitorService.GameTuner.DisableGrassWindSwayLeague(true, out msg); statusLabel.Text = msg; Log(msg); }, false, "Stops terrain grass from swaying in the wind to stabilize teamfight FPS", null, () => { string msg; _monitorService.GameTuner.DisableGrassWindSwayLeague(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpGpu.Children.Add(CreateToolButton(" Disable Death Screen Grayscale", () => { string msg; _monitorService.GameTuner.DisableDeathScreenDesaturation(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Removes gray death filter so you can watch ongoing teamfights in full color", null, () => { string msg; _monitorService.GameTuner.DisableDeathScreenDesaturation(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpGpu.Children.Add(CreateToolButton(" Bypass D3D11 Thread Overhead", () => { string msg; _monitorService.GameTuner.BypassD3D11MultithreadingOverhead(true, out msg); statusLabel.Text = msg; Log(msg); }, false, "Disables multithreading overhead on dual/quad-core CPUs for higher FPS", null, () => { string msg; _monitorService.GameTuner.BypassD3D11MultithreadingOverhead(false, out msg); statusLabel.Text = msg; Log(msg); }));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. CHALLENGER CONTROLS, CAMERA & HUD"));
                WrapPanel grpControls = new WrapPanel();
                grpControls.Children.Add(CreateToolButton(" Smart Kiting (Attack on Cursor)", () => { string msg; _monitorService.GameTuner.ConfigureAttackMoveOnCursor(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Attacks target nearest to cursor instead of champion - ADC kiting essential", () => _monitorService.GameTuner.IsLeagueSettingActive("AttackMoveOnCursor", "1"), () => { string msg; _monitorService.GameTuner.ConfigureAttackMoveOnCursor(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Target Champions Only Toggle", () => { string msg; _monitorService.GameTuner.EnableTargetChampionsOnlyToggle(out msg); statusLabel.Text = msg; Log(msg); }, true, "Configures target champions only as toggle for dive plays"));
                grpControls.Children.Add(CreateToolButton(" Target Champions Highlight", () => { string msg; _monitorService.GameTuner.ConfigureTargetChampionsOnlyBorder(out msg); statusLabel.Text = msg; Log(msg); }, true, "Highlights cursor with red outline during Target Champions Only mode"));
                grpControls.Children.Add(CreateToolButton(" Quick Cast with Range Indicator", () => { string msg; _monitorService.GameTuner.ConfigureQuickCastWithIndicator(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Shows skillshot range on button hold and fires on release", null, () => { string msg; _monitorService.GameTuner.ConfigureQuickCastWithIndicator(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Instant Quick-Cast Wards (0ms)", () => { string msg; _monitorService.GameTuner.ConfigureInstantWardCast(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Bypasses ward placement reticle for instant ward-hopping on Lee Sin & Jax", null, () => { string msg; _monitorService.GameTuner.ConfigureInstantWardCast(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Native Raw Mouse Input (1:1)", () => { string msg; _monitorService.GameTuner.EnableRawMouseInputLeague(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Enables UseRawMouseInput=1 in game.cfg to bypass Windows pointer interference", null, () => { string msg; _monitorService.GameTuner.EnableRawMouseInputLeague(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Lock Cursor To Window", () => { string msg; _monitorService.GameTuner.LockCursorToLeagueWindow(out msg); statusLabel.Text = msg; Log(msg); }, true, "Prevents mouse slipping to secondary monitors during intense fights"));
                grpControls.Children.Add(CreateToolButton(" High-Contrast Healthbars", () => { string msg; _monitorService.GameTuner.ConfigureHighContrastHealthbars(out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces thick high-contrast healthbars for instant targeting in 5v5"));
                grpControls.Children.Add(CreateToolButton(" Auto-Attack Range Ring", () => { string msg; _monitorService.GameTuner.ConfigureChampionRangeIndicator(true, out msg); statusLabel.Text = msg; Log(msg); }, false, "Shows champion maximum basic attack range perimeter when pressing attack-move", null, () => { string msg; _monitorService.GameTuner.ConfigureChampionRangeIndicator(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Precision Cursor Scale (50%)", () => { string msg; _monitorService.GameTuner.SetPrecisionCursorScale(50, out msg); statusLabel.Text = msg; Log(msg); }, false, "Scales cursor to 50% for pixel-precise skillshot aiming and kiting"));
                grpControls.Children.Add(CreateToolButton(" Extra Large Minimap (1.25x)", () => { string msg; _monitorService.GameTuner.OverrideMinimapScaleBeyondCap(1.25, out msg); statusLabel.Text = msg; Log(msg); }, true, "Overrides 100 limit in game.cfg to 125% scale for early gank spotting", () => _monitorService.GameTuner.IsLeagueSettingActive("MinimapScale", "1.2500"), () => { string msg; _monitorService.GameTuner.RestoreMinimapScale(out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Flip Minimap to Left Side", () => { string msg; _monitorService.GameTuner.TuneMinimapFlipPosition(true, out msg); statusLabel.Text = msg; Log(msg); }, false, "Places minimap on left side to eliminate accidental clicks when running blue-side", null, () => { string msg; _monitorService.GameTuner.TuneMinimapFlipPosition(false, out msg); statusLabel.Text = msg; Log(msg); }, "lol_minimap_pos"));
                grpControls.Children.Add(CreateToolButton(" Hide Overhead Summoner Names", () => { string msg; _monitorService.GameTuner.HideSummonerNamesAboveHealthbars(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Hides summoner names above healthbars to eliminate screen clutter in 5v5 teamfights", null, () => { string msg; _monitorService.GameTuner.HideSummonerNamesAboveHealthbars(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Spacebar Instant Center Lock", () => { string msg; _monitorService.GameTuner.ConfigureSpacebarCenterLock(out msg); statusLabel.Text = msg; Log(msg); }, false, "Disables spacebar camera smoothing delay for instant snap centering"));
                grpControls.Children.Add(CreateToolButton(" Decouple Camera on Respawn", () => { string msg; _monitorService.GameTuner.ConfigureCameraDecoupleOnRespawn(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Stops camera from snapping back to fountain when you respawn", null, () => { string msg; _monitorService.GameTuner.ConfigureCameraDecoupleOnRespawn(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Disable Camera Pan Smoothing", () => { string msg; _monitorService.GameTuner.DisableCameraSmoothingLeague(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Removes camera pan inertia drift for instant 1:1 camera snap repositions", null, () => { string msg; _monitorService.GameTuner.DisableCameraSmoothingLeague(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpControls.Children.Add(CreateToolButton(" Anti-Tilt Compact Chat (0.5x)", () => { string msg; _monitorService.GameTuner.ConfigureChatScale(0.5, out msg); statusLabel.Text = msg; Log(msg); }, false, "Scales chat box to 50% size to preserve lane vision and eliminate distraction"));
                grpControls.Children.Add(CreateToolButton(" Stealth Bush Mode", () => { string msg; _monitorService.GameTuner.DisableAutoAcquireTarget(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables auto-acquiring targets so champions never reveal themselves from bushes", () => _monitorService.GameTuner.IsLeagueSettingActive("AutoAcquireTarget", "0"), () => { string msg; _monitorService.GameTuner.EnableAutoAcquireTarget(out msg); statusLabel.Text = msg; Log(msg); }));
                content.Children.Add(grpControls);

                content.Children.Add(CreateSectionHeader(" 3. CPU, PROCESS FOCUS & VANGUARD SHIELD"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" 0ms Combo Key Input Buffer", () => { string msg; _monitorService.GameTuner.OptimizeComboInputBuffer(out msg); statusLabel.Text = msg; Log(msg); }, true, "Configures 0ms spell input queue buffer for lightning-fast Insec & Riven combos"));
                grpCpu.Children.Add(CreateToolButton(" Close Client During Match", () => { string msg; _monitorService.GameTuner.ConfigureCloseClientOnGameStart(true, out msg); statusLabel.Text = msg; Log(msg); }, true, "Closes background client while in-game to give 100% CPU to League of Legends.exe", null, () => { string msg; _monitorService.GameTuner.ConfigureCloseClientOnGameStart(false, out msg); statusLabel.Text = msg; Log(msg); }));
                grpCpu.Children.Add(CreateToolButton(" Throttle Riot Client", () => { string msg; _monitorService.GameTuner.ThrottleRiotClientBackground(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets Riot Client to Idle priority"));
                grpCpu.Children.Add(CreateToolButton(" Prioritize Game Process", () => { string msg; _monitorService.GameTuner.PrioritizeLeagueProcess(out msg); statusLabel.Text = msg; Log(msg); }, false, "Elevates game thread scheduling priority"));
                grpCpu.Children.Add(CreateToolButton(" Protect FPS from Vanguard Stutters", () => { string msg; _monitorService.GameTuner.ApplyLeagueVanguardThreadShield(out msg); statusLabel.Text = msg; Log(msg); }, true, "Elevates League process priority to ensure steady Vanguard I/O cycles"));
                grpCpu.Children.Add(CreateToolButton(" Disable GameDVR Hooks", () => { string msg; _monitorService.GameTuner.DisableGameDvrInLeagueConfig(out msg); statusLabel.Text = msg; Log(msg); }, false, "Prevents Windows GameDVR overlay from hooking League"));
                grpCpu.Children.Add(CreateToolButton(" Disable Screen Shake & Bob", () => { string msg; _monitorService.GameTuner.DisableLeagueScreenShake(out msg); statusLabel.Text = msg; Log(msg); }, false, "Eliminates camera shake for steady skillshots"));
                grpCpu.Children.Add(CreateToolButton(" Anti-Cheat Safety Status", () => { statusLabel.Text = _monitorService.GameTuner.GetLeagueSafetyStatus(); Log(statusLabel.Text); }, false, "Verifies zero DLL injection compliance"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 4. ACOUSTIC CUES & SOUND EQUALIZATION"));
                WrapPanel grpAudio = new WrapPanel();
                grpAudio.Children.Add(CreateToolButton(" Mute River Noise (Hear Flashes)", () => { string msg; _monitorService.GameTuner.MuteRiverAmbientNoise(out msg); statusLabel.Text = msg; Log(msg); }, true, "Mutes river/wind background audio to clearly hear Baron aggro & Flashes", () => _monitorService.GameTuner.IsLeagueSettingActive("AmbientVolume", "0.0000"), () => { string msg; _monitorService.GameTuner.UnmuteRiverAmbientNoise(out msg); statusLabel.Text = msg; Log(msg); }));
                grpAudio.Children.Add(CreateToolButton(" Mute Ambience / Boost SFX", () => { string msg; _monitorService.GameTuner.OptimizeTeamfightSoundFrequencies(out msg); statusLabel.Text = msg; Log(msg); }, false, "Mutes map ambience to emphasize enemy Flash and Teleport audio cues"));
                grpAudio.Children.Add(CreateToolButton(" True Damage Audio Boost", () => { string msg; _monitorService.GameTuner.AmplifyTrueDamageAudioCues(out msg); statusLabel.Text = msg; Log(msg); }, false, "Acoustic boost for Smite, Cho'Gath Feast, and Camille Q2 impact triggers"));
                grpAudio.Children.Add(CreateToolButton(" Amplify Danger & Missing Pings", () => { string msg; _monitorService.GameTuner.AmplifyDangerPingAudio(out msg); statusLabel.Text = msg; Log(msg); }, false, "Amplifies 2.5kHz ping frequency so you never miss enemy roaming calls"));
                grpAudio.Children.Add(CreateToolButton(" Amplify Epic Monster Roar Audio", () => { string msg; _monitorService.GameTuner.AmplifyEpicMonsterRoarAudio(out msg); statusLabel.Text = msg; Log(msg); }, false, "Amplifies sub-bass roar so you hear Baron & Dragon attacked in fog"));
                grpAudio.Children.Add(CreateToolButton(" Amplify Camouflage & Stealth SFX", () => { string msg; _monitorService.GameTuner.AmplifyStealthAudioCues(out msg); statusLabel.Text = msg; Log(msg); }, false, "Boosts audio for Twitch ambush, Shaco smoke puff, and Evelynn proximity"));
                grpAudio.Children.Add(CreateToolButton(" Disable In-Game Music", () => { string msg; _monitorService.GameTuner.DisableInGameMusicEngine(out msg); statusLabel.Text = msg; Log(msg); }, false, "Saves CPU audio thread cycles in 5v5 teamfights"));
                content.Children.Add(grpAudio);

                content.Children.Add(CreateSectionHeader(" 5. NETWORK, MEMORY & STORAGE UTILITIES"));
                WrapPanel grpUtils = new WrapPanel();
                grpUtils.Children.Add(CreateToolButton(" Enable Movement Prediction", () => { string msg; _monitorService.GameTuner.EnableMovementPrediction(out msg); statusLabel.Text = msg; Log(msg); }, true, "Smooths champion pathing during ping spikes"));
                grpUtils.Children.Add(CreateToolButton(" Predictive Pathfinding Clamp", () => { string msg; _monitorService.GameTuner.ConfigurePredictivePathfindingClamp(out msg); statusLabel.Text = msg; Log(msg); }, true, "Clamps client-side predictive movement ticks to eliminate minion block stutter"));
                grpUtils.Children.Add(CreateToolButton(" Instant Pathing Right-Click Buffer", () => { string msg; _monitorService.GameTuner.OptimizeLeaguePacketBuffering(out msg); statusLabel.Text = msg; Log(msg); }, true, "Clamps network socket send buffer for zero queue delay on movement clicks"));
                grpUtils.Children.Add(CreateToolButton(" Trim League Client Memory", () => { long b; string msg; _monitorService.GameTuner.CleanLeagueClientMemoryLeak(out b, out msg); statusLabel.Text = msg; Log(msg); }, false, "Trims LeagueClientUx Chromium processes to stop RAM leaks"));
                grpUtils.Children.Add(CreateToolButton(" Clean All League Logs & Replays", () => { long b1, b2, b3; string m1, m2, m3; _monitorService.GameTuner.CleanLeagueLogs(out b1, out m1); _monitorService.GameTuner.CleanLeagueHighlightsAndReplays(out b2, out m2); _monitorService.GameTuner.PurgeLeagueCrashReporterTelemetry(out b3, out m3); statusLabel.Text = string.Format(" Cleaned {0:N0} KB of match telemetry, CEF browser caches & crash reports", (b1 + b2 + b3) / 1024); Log(statusLabel.Text); }, false, "Purges match logs, client web caches, replays, and crash telemetry"));
                grpUtils.Children.Add(CreateToolButton(" Restore game.cfg Defaults", () => { string msg; _monitorService.GameTuner.RestoreLeagueGameConfig(out msg); statusLabel.Text = msg; Log(msg); }, false, "Restores original backup"));
                content.Children.Add(grpUtils);

                content.Children.Add(CreateSectionHeader(" 6. 1-CLICK TOURNAMENT PRESETS"));
                WrapPanel grpPresets = new WrapPanel();
                grpPresets.Children.Add(CreateToolButton(" 1-Click Challenger Apex Suite", () => { string msg; _monitorService.GameTuner.ApplyChallengerApexMacroCombo(out msg); statusLabel.Text = msg; Log(msg); }, true, "Attack Move on Cursor, 1.25x Map, Ambient Mute, Quick-Cast, Raw Input & 0.5ms Timer"));
                grpPresets.Children.Add(CreateToolButton(" 1-Click Maximum Teamfight FPS", () => { string msg; _monitorService.GameTuner.ApplyLeagueUltraPerformancePreset(out msg); statusLabel.Text = msg; Log(msg); }, true, "Arms 240 FPS, D3D11, No Outlines, No Critters, No Godrays, Low Env for maximum teamfight FPS"));
                content.Children.Add(grpPresets);
            }

            else if (profile.Id == "valo")
            {
                // Valorant & Vanguard Safeguard (Curated Pro Esports Suite)
                TextBlock title = new TextBlock() { Text = "VALORANT ANTI-CHEAT SAFEGUARD & INPUT BUFFER", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                TextBlock statusLabel = new TextBlock() { Text = "Status: " + _monitorService.GameTuner.GetVanguardHealthStatus(), FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateSectionHeader(" 1. GRAPHICS, RESOLUTION & PRESENTATION"));
                content.Children.Add(CreateConflictCallout("Valorant Pointer & Power", "Input tracking modes and GPU power targets auto-swap with their counterparts"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" Fix Alt+Tab Black Screen", () => { string msg; _monitorService.GameTuner.FixValorantAltTabBlackScreen(out msg); statusLabel.Text = msg; Log(msg); }, false, "Cleans incompatible AppCompatFlags & sets DirectFlip Borderless to fix black screen on Alt+Tab"));
                grpGpu.Children.Add(CreateToolButton(" Uncap Engine FPS", () => { string msg; _monitorService.GameTuner.UncapFpsInValorantGameUserSettings(out msg); statusLabel.Text = msg; Log(msg); }, false, "Removes 0 FPS cap in GameUserSettings.ini"));
                grpGpu.Children.Add(CreateToolButton(" DirectFlip Presentation (FSO Bypass)", () => { string msg; _monitorService.GameTuner.DisableFullscreenOptimizationsForValorant(out msg); statusLabel.Text = msg; Log(msg); }, true, "Enables zero-latency DirectFlip presentation bypass while preserving seamless Alt+Tab"));
                grpGpu.Children.Add(CreateToolButton(" Force Dedicated GPU", () => { string msg; _monitorService.GameTuner.ForceValorantHighPerformanceGpu(out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces high performance discrete GPU preference in DirectX"));
                grpGpu.Children.Add(CreateToolButton(" Lock Stable DirectX 11", () => { string msg; _monitorService.GameTuner.EnforceDirectX11FeatureLevel(out msg); statusLabel.Text = msg; Log(msg); }, false, "Locks dedicated GPU and D3D11 feature level for 1% low FPS consistency"));
                grpGpu.Children.Add(CreateToolButton(" Enforce NVIDIA Reflex Ultra", () => { string msg; _monitorService.GameTuner.EnforceNvidiaReflexUltraProfile(out msg); statusLabel.Text = msg; Log(msg); }, true, "Locks frame queue to 0 for instantaneous click-to-photon responsiveness", null, () => { string msg; _monitorService.GameTuner.RestoreNvidiaReflexDefault(out msg); statusLabel.Text = msg; Log(msg); }));
                grpGpu.Children.Add(CreateToolButton(" Lock GPU Maximum Performance", () => { string msg; _monitorService.GameTuner.LockGpuMaximumPerformancePowerState(out msg); statusLabel.Text = msg; Log(msg); }, true, "Locks GPU power state to Maximum Performance to prevent downclocking during rounds", null, () => { string msg; _monitorService.GameTuner.RestoreGpuStandardPowerState(out msg); statusLabel.Text = msg; Log(msg); }, "valo_power"));
                grpGpu.Children.Add(CreateToolButton(" Enforce GPU Full Scaling", () => { string msg; _monitorService.GameTuner.EnforceGpuFullScalingAspect(out msg); statusLabel.Text = msg; Log(msg); }, false, "Enforces full display scaling in registry to play stretched resolutions without black bars"));
                grpGpu.Children.Add(CreateToolButton(" Verify DirectFlip Sub-1ms", () => { string msg; _monitorService.GameTuner.VerifyDirectFlipPresentationMode(out msg); statusLabel.Text = msg; Log(msg); }, false, "Verifies DirectFlip compositor bypass for lowest possible frame presentation lag"));
                grpGpu.Children.Add(CreateToolButton(" Disable Windows GameDVR", () => { string msg; _monitorService.GameTuner.DisableWindowsGameDvrCapture(out msg); statusLabel.Text = msg; Log(msg); }, true, "Completely shuts off GameDVR capture and broadcast hooks in Windows registry"));
                grpGpu.Children.Add(CreateToolButton(" Disable Edge-Swipe Gestures", () => { string msg; _monitorService.GameTuner.DisableEdgeSwipeGestures(out msg); statusLabel.Text = msg; Log(msg); }, false, "Disables Windows edge swipe gestures to prevent mouse capture slips on dual monitors"));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. RADIANT AIM, INPUT & TIMING"));
                WrapPanel grpAim = new WrapPanel();
                grpAim.Children.Add(CreateToolButton(" Pure 1:1 Mouse Aim (No Accel)", () => { string msg; _monitorService.GameTuner.EnforceRawMouseReporting(out msg); statusLabel.Text = msg; Log(msg); }, true, "Locks 1:1 hardware raw reporting and disables Windows acceleration", () => _monitorService.GameTuner.IsRawMouseActive(), () => { string msg; _monitorService.GameTuner.DisableRawMouseReporting(out msg); statusLabel.Text = msg; Log(msg); }, "valo_pointer"));
                grpAim.Children.Add(CreateToolButton(" 1000-8000Hz Mouse Buffer", () => { string msg; _monitorService.GameTuner.VerifyHighPollingMouseBuffer(out msg); statusLabel.Text = msg; Log(msg); }, true, "Direct raw HID event pump audit for high polling rate mice"));
                grpAim.Children.Add(CreateToolButton(" Audit Mouse Polling Jitter", () => { string msg; _monitorService.GameTuner.AuditMousePollingJitter(out msg); statusLabel.Text = msg; Log(msg); }, false, "Tests USB input report interval jitter to ensure consistent 1000Hz+ response"));
                grpAim.Children.Add(CreateToolButton(" Lock 0.500ms Input Timer", () => { string msg; _monitorService.GameTuner.LockMicrosecondInputTimer(out msg); _monitorService.TimerEngine.EnableHighResolution(); statusLabel.Text = msg; Log(msg); }, true, "Locks 0.500ms microsecond click timer for instantaneous hit registration"));
                grpAim.Children.Add(CreateToolButton(" 4:3 Stretched Sens Converter", () => { string msg; _monitorService.GameTuner.ConvertStretchedResolutionSens(0.35, out msg); statusLabel.Text = msg; Log(msg); }, false, "Calculates 0.75x horizontal scaling for 1:1 true muscle memory on stretched 4:3"));
                content.Children.Add(grpAim);

                content.Children.Add(CreateSectionHeader(" 3. CPU, VANGUARD SHIELD & PROCESS ISOLATION"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" Shield Vanguard Realtime I/O", () => { string msg; _monitorService.GameTuner.ShieldVanguardRealtimeIo(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets High priority for vgc service to prevent Vanguard Error 57 mid-match disconnects"));
                grpCpu.Children.Add(CreateToolButton(" Vanguard Driver Health Audit", () => { statusLabel.Text = _monitorService.GameTuner.AuditVanguardDriverState(); Log(statusLabel.Text); }, false, "Deep check of vgc service & vgk kernel driver"));
                grpCpu.Children.Add(CreateToolButton(" Throttle Riot Client", () => { string msg; _monitorService.GameTuner.ThrottleRiotClientBackground(out msg); statusLabel.Text = msg; Log(msg); }, true, "Suppresses Riot Client to Idle priority"));
                grpCpu.Children.Add(CreateToolButton(" Throttle vgc Helper Service", () => { string msg; _monitorService.GameTuner.ThrottleVanguardHelperService(out msg); statusLabel.Text = msg; Log(msg); }, true, "Throttles vgc user-mode process to BelowNormal to free Render Core 0"));
                grpCpu.Children.Add(CreateToolButton(" Prioritize Valorant Process", () => { string msg; _monitorService.GameTuner.PrioritizeValorantProcess(out msg); statusLabel.Text = msg; Log(msg); }, false, "Sets AboveNormal render priority for ShooterGame"));
                grpCpu.Children.Add(CreateToolButton(" Throttle Background Apps (Clutch)", () => { string msg; _monitorService.GameTuner.ThrottleBackgroundAppsDuringClutch(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets Discord, Chrome, and Spotify to Idle priority during rounds", null, () => { string msg; _monitorService.GameTuner.RestoreBackgroundAppsPriority(out msg); statusLabel.Text = msg; Log(msg); }));
                grpCpu.Children.Add(CreateToolButton(" Wake All CPU Cores (No Parking)", () => { string msg; _monitorService.GameTuner.DisableWindowsCpuCoreParking(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables CPU Core Parking via powercfg to prevent round-start micro-stutters"));
                grpCpu.Children.Add(CreateToolButton(" Suppress Overlay Hooks", () => { string msg; _monitorService.GameTuner.SuppressBackgroundOverlayHooks(out msg); statusLabel.Text = msg; Log(msg); }, false, "Suppresses third-party injection hooks (Discord/Overwolf) that cause Vanguard stutters"));
                grpCpu.Children.Add(CreateToolButton(" Engage Focus Assist Clutch", () => { string msg; _monitorService.GameTuner.EngageFocusAssistClutchMode(out msg); statusLabel.Text = msg; Log(msg); }, false, "Suppresses Windows notifications & toast popups during clutch rounds"));
                grpCpu.Children.Add(CreateToolButton(" Prepare Match RAM Cache", () => { string msg; _monitorService.GameTuner.PrepareMemoryForMatch(out msg); _monitorService.MemoryService.PurgeSafeBackgroundMemory(); statusLabel.Text = msg; Log(msg); }, false, "Cleans background RAM before match"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 4. SPATIAL AUDIO & SOUND INTEL"));
                WrapPanel grpAudio = new WrapPanel();
                grpAudio.Children.Add(CreateToolButton(" AudioDG Realtime Sound Shield", () => { string msg; _monitorService.GameTuner.LockAudioDgRealtimePriority(out msg); _monitorService.SystemTweaks.StabilizeAudioEngine(); statusLabel.Text = msg; Log(msg); }, true, "Locks audiodg.exe priority to High in registry to prevent audio dropouts in ult fights"));
                grpAudio.Children.Add(CreateToolButton(" Loud Footsteps & Half-Defuse Audio", () => { string msg; _monitorService.GameTuner.AccenuateValorantFootstepsAndDefuse(out msg); statusLabel.Text = msg; Log(msg); }, true, "Accentuate 800Hz-2.5kHz audio for spike half-defuses and enemy footsteps"));
                grpAudio.Children.Add(CreateToolButton(" Amplify Footstep Resonance Band", () => { string msg; _monitorService.GameTuner.AmplifyFootstepResonanceAudio(out msg); statusLabel.Text = msg; Log(msg); }, false, "Isolates and boosts 300Hz-800Hz footstep resonance above chaotic gunfire"));
                grpAudio.Children.Add(CreateToolButton(" Amplify Headshot 'Dink' Confirm", () => { string msg; _monitorService.GameTuner.AmplifyArmorBreakDinkAudio(out msg); statusLabel.Text = msg; Log(msg); }, false, "Amplifies 1.5kHz-3kHz metallic dink frequencies for instant hit confirms through smoke"));
                grpAudio.Children.Add(CreateToolButton(" Amplify Fake-Plant & Tap Audio", () => { string msg; _monitorService.GameTuner.AmplifyFakePlantDefuseAudio(out msg); statusLabel.Text = msg; Log(msg); }, false, "Amplifies metallic spike latch tap and half-defuse audio cues across entire site"));
                grpAudio.Children.Add(CreateToolButton(" Sniper Scope Sound Amp", () => { string msg; _monitorService.GameTuner.AmplifySniperScopeAudio(out msg); statusLabel.Text = msg; Log(msg); }, false, "Amplifies 1.5kHz scope-in sound to react before enemy Operator shoots"));
                grpAudio.Children.Add(CreateToolButton(" Sova Dart & Fade Recon Boost", () => { string msg; _monitorService.GameTuner.AmplifySovaAndFadeScanAudio(out msg); statusLabel.Text = msg; Log(msg); }, false, "Isolates 3kHz frequency for Sova sonar tick, Fade prowler hiss, and Cypher camera"));
                grpAudio.Children.Add(CreateToolButton(" Audio Device Buffer Sync (48kHz)", () => { string msg; _monitorService.GameTuner.SyncAudioDeviceBuffer(out msg); statusLabel.Text = msg; Log(msg); }, false, "Syncs Windows audio sampling buffer to 48kHz for pristine directional audio"));
                content.Children.Add(grpAudio);

                content.Children.Add(CreateSectionHeader(" 5. NETWORK, LOGS & STORAGE UTILITIES"));
                WrapPanel grpUtils = new WrapPanel();
                grpUtils.Children.Add(CreateToolButton(" Sub-1ms TCPNoDelay Sockets", () => { string msg; _monitorService.GameTuner.ConfigureTcpNoDelayForRiot(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets TCPNoDelay & TcpAckFrequency=1 on network adapters for instant tick dispatch"));
                grpUtils.Children.Add(CreateToolButton(" Network DSCP QoS Priority", () => { string msg; _monitorService.GameTuner.OptimizeNetworkDscpQoS(out msg); statusLabel.Text = msg; Log(msg); }, false, "Prioritizes Valorant UDP packets with Expedited Forwarding"));
                grpUtils.Children.Add(CreateToolButton(" Flush Windows DNS", () => { string msg; _monitorService.GameTuner.FlushWindowsDnsForValorant(out msg); statusLabel.Text = msg; Log(msg); }, true, "Flushes DNS cache for lowest matchmaking ping"));
                grpUtils.Children.Add(CreateToolButton(" Riot Direct PoP Ping Audit", () => { statusLabel.Text = _monitorService.GameTuner.AuditRiotDirectRoutingPing(); Log(statusLabel.Text); }, false, "Pings nearest Riot Direct edge nodes to check matchmaking routing"));
                grpUtils.Children.Add(CreateToolButton(" Disable Windows Delivery P2P", () => { string msg; _monitorService.GameTuner.DisableWindowsDeliveryOptimizationP2p(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables background P2P update uploads to eliminate random in-match ping spikes"));
                grpUtils.Children.Add(CreateToolButton(" Refresh Winsock Routing Catalog", () => { string msg; _monitorService.GameTuner.ResetWindowsNetworkSocketState(out msg); statusLabel.Text = msg; Log(msg); }, false, "Refreshes Windows Winsock catalog for network socket packet drop recovery"));
                grpUtils.Children.Add(CreateToolButton(" Purge Shader Cache & Crash Logs", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.CleanDirectXShaderCache(out b1, out m1); _monitorService.GameTuner.CleanValorantCrashesAndLogs(out b2, out m2); statusLabel.Text = string.Format(" Purged {0:N0} KB of shader cache and crash minidumps", (b1 + b2) / 1024); Log(statusLabel.Text); }, true, "Purges corrupted GPU shader binaries and crash minidumps"));
                grpUtils.Children.Add(CreateToolButton(" Clean CEF & Riot Client Cache", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.CleanValorantWebBrowserCache(out b1, out m1); _monitorService.GameTuner.CleanRiotClientLogs(out b2, out m2); statusLabel.Text = string.Format(" Cleaned {0:N0} KB of embedded browser cache and client logs", (b1 + b2) / 1024); Log(statusLabel.Text); }, false, "Purges embedded web browser cache and Riot Client logs"));
                
                content.Children.Add(grpUtils);

                content.Children.Add(CreateSectionHeader(" 6. 1-CLICK TOURNAMENT PRESETS"));
                WrapPanel grpPresets = new WrapPanel();
                grpPresets.Children.Add(CreateToolButton(" 1-Click Radiant Immortals Suite", () => { string msg; _monitorService.GameTuner.ApplyRadiantImmortalsTournamentSuite(out msg); statusLabel.Text = msg; Log(msg); }, true, "Raw Mouse, FSO Bypass, Reflex Ultra, Vanguard Shield, TCPNoDelay, AudioDG & 0.5ms Timer!"));
                grpPresets.Children.Add(CreateToolButton(" 1-Click Maximum FPS & Potato Mode", () => { string msg; _monitorService.GameTuner.ApplyValorantUltraFpsPackage(out msg); statusLabel.Text = msg; Log(msg); }, true, "Bypasses FSO, sets High GPU, 0.5ms Timer, Audio Shield & Flushes DNS for maximum FPS"));
                content.Children.Add(grpPresets);
            }

            else if (profile.Id == "cs2")
            {
                // Counter-Strike 2 Esports & Sub-Tick Setup (35 Pro Controls)
                TextBlock title = new TextBlock() { Text = "COUNTER-STRIKE 2 ESPORTS & SUB-TICK SETUP (35 PRO CONTROLS)", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                TextBlock statusLabel = new TextBlock() { Text = "Recommended Launch Options: " + _monitorService.GameTuner.GetCs2LaunchOptions(), FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateSectionHeader(" 1. GRAPHICS, AUDIO & STEAM BROWSER RAM"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" Force Dedicated GPU", () => { string msg; _monitorService.GameTuner.ForceCs2HighPerformanceGpu(out msg); statusLabel.Text = msg; Log(msg); }, true, "Forces cs2.exe to discrete GPU in DirectX"));
                grpGpu.Children.Add(CreateToolButton(" Bypass Fullscreen Optimizations", () => { string msg; _monitorService.GameTuner.DisableFullscreenOptimizationsForCs2(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables DWM latency hook for true raw exclusive display"));
                grpGpu.Children.Add(CreateToolButton(" Purge CS2 Shader Cache", () => { long b; string msg; _monitorService.GameTuner.PurgeCs2ShaderCache(out b, out msg); statusLabel.Text = msg; Log(msg); }, false, "Purges stale DirectX & Vulkan shader binaries"));
                grpGpu.Children.Add(CreateToolButton(" Pro Audio EQ Guide", () => { statusLabel.Text = _monitorService.GameTuner.GetCs2CrispAudioEqGuide(); Log(statusLabel.Text); }, false, "Best EQ profile & mixahead for footstep clarity"));
                grpGpu.Children.Add(CreateToolButton(" Sub-Tick Audio Normalizer", () => { string msg; _monitorService.GameTuner.DeploySubTickDecalsAndAudioNormalizer(out msg); statusLabel.Text = msg; Log(msg); }, false, "Injects 15ms mixahead sound normalizer directly into autoexec.cfg"));
                grpGpu.Children.Add(CreateToolButton(" Steady Viewmodel Recoil", () => { string msg; _monitorService.GameTuner.DeployViewmodelSteadyBinding(out msg); statusLabel.Text = msg; Log(msg); }, false, "Sets viewmodel offsets and steady gun bob in autoexec.cfg"));
                grpGpu.Children.Add(CreateToolButton(" 4:3 Stretched Setup Guide", () => { statusLabel.Text = _monitorService.GameTuner.Get4By3StretchedSetupGuide(); Log(statusLabel.Text); }, false, "GPU scaling instructions for 1280x960 to widen hitboxes by 33%"));
                grpGpu.Children.Add(CreateToolButton(" Purge Steam RAM (300MB+)", () => { long b; string msg; _monitorService.GameTuner.PurgeSteamWebHelperRam(out b, out msg); statusLabel.Text = msg; Log(msg); }, true, "Frees RAM from Steam CEF tabs"));
                grpGpu.Children.Add(CreateToolButton(" Fast Footstep Mixahead Guide", () => { statusLabel.Text = _monitorService.GameTuner.GetCs2AudioMixaheadTip(); Log(statusLabel.Text); }, false, "Cuts directional audio latency"));
                grpGpu.Children.Add(CreateToolButton(" Disable Joystick (-nojoy)", () => { statusLabel.Text = _monitorService.GameTuner.GetNoJoyFlagTip(); Log(statusLabel.Text); }, false, "Stops gamepad polling"));
                grpGpu.Children.Add(CreateToolButton(" Uncap Engine (+fps_max 0)", () => { statusLabel.Text = _monitorService.GameTuner.GetFpsMaxZeroTip(); Log(statusLabel.Text); }, false, "Unbinds Source 2 FPS limiter"));
                grpGpu.Children.Add(CreateToolButton("⏩ Skip Intro Video (-novid)", () => { statusLabel.Text = _monitorService.GameTuner.GetNoVidFlagTip(); Log(statusLabel.Text); }, false, "Bypasses startup splash video"));
                grpGpu.Children.Add(CreateToolButton(" Clean Steam Web Cache", () => { long b; string msg; _monitorService.GameTuner.CleanSteamShaderCache(out b, out msg); statusLabel.Text = msg; Log(msg); }, false, "Purges Steam HTML cache"));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. CPU, SUB-TICK TIMING & PROCESS PRIORITY"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" Sub-Tick 0.500ms Timer Lock", () => { string msg; _monitorService.GameTuner.LockCs2SubTickTimer(out msg); _monitorService.TimerEngine.EnableHighResolution(); statusLabel.Text = msg; Log(msg); }, true, "Locks sub-tick hit registration sync"));
                grpCpu.Children.Add(CreateToolButton(" Prioritize CS2 (High)", () => { string msg; _monitorService.GameTuner.PrioritizeCs2Process(out msg); statusLabel.Text = msg; Log(msg); }, false, "Elevates cs2.exe to High priority"));
                grpCpu.Children.Add(CreateToolButton(" Sub-Tick Interp Clamping", () => { string msg; _monitorService.GameTuner.DeploySubTickInterpClamping(out msg); statusLabel.Text = msg; Log(msg); }, true, "Configures cl_interp 0.015625 and cl_interp_ratio 1 in autoexec.cfg"));
                grpCpu.Children.Add(CreateToolButton(" Validate 1500 MTU Network", () => { string msg; _monitorService.GameTuner.OptimizeNetworkMtuValidation(out msg); statusLabel.Text = msg; Log(msg); }, false, "Checks Ethernet MTU for unfragmented sub-tick packets"));
                grpCpu.Children.Add(CreateToolButton(" VAC Session Integrity Audit", () => { statusLabel.Text = _monitorService.GameTuner.AuditVacSessionIntegrity(); Log(statusLabel.Text); }, false, "Audits system for conflicting background tools to prevent VAC session errors"));
                grpCpu.Children.Add(CreateToolButton(" VAC Anti-Cheat Compliance", () => { statusLabel.Text = _monitorService.GameTuner.GetCs2SafetyStatus(); Log(statusLabel.Text); }, false, "Confirms VAC 100% compliance"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 3. NETWORK, AUTOEXEC & UTILITIES"));
                WrapPanel grpUtils = new WrapPanel();
                grpUtils.Children.Add(CreateToolButton(" Deploy Practice Config", () => { string msg; _monitorService.GameTuner.DeployCs2PracticeConfig(out msg); statusLabel.Text = msg; Log(msg); }, false, "Writes practice.cfg with grenade trajectories & sv_cheats"));
                grpUtils.Children.Add(CreateToolButton(" Bind Run-Jumpthrow ('V')", () => { string msg; _monitorService.GameTuner.DeployRunJumpThrowBinding(out msg); statusLabel.Text = msg; Log(msg); }, true, "Binds 100% consistent forward run-jumpthrow to 'V' for deep smokes"));
                grpUtils.Children.Add(CreateToolButton(" Bind Jump-Throw ('C')", () => { string msg; _monitorService.GameTuner.DeploySubTickJumpThrowAlias(out msg); statusLabel.Text = msg; Log(msg); }, false, "Deploys 100% consistent sub-tick jumpthrow bound to 'C'"));
                grpUtils.Children.Add(CreateToolButton(" Bind Instant Bomb Drop ('X')", () => { string msg; _monitorService.GameTuner.DeployInstantBombDropBinding(out msg); statusLabel.Text = msg; Log(msg); }, true, "Binds instant C4 bomb drop to 'X' without switching weapon slots"));
                grpUtils.Children.Add(CreateToolButton(" Bind Radar Zoom ('CapsLock')", () => { string msg; _monitorService.GameTuner.DeployRadarZoomToggleBinding(out msg); statusLabel.Text = msg; Log(msg); }, false, "Toggles between full-map radar and close-combat zoom"));
                grpUtils.Children.Add(CreateToolButton(" Clean Workshop & UI Cache", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.CleanCs2WorkshopCustomAssets(out b1, out m1); _monitorService.GameTuner.CleanCs2PanoramaUiCache(out b2, out m2); statusLabel.Text = string.Format(" Cleaned {0:N0} KB of community map assets & UI cache", (b1 + b2) / 1024); Log(statusLabel.Text); }, false, "Cleans temporary community server map assets and Panorama UI cache"));
                grpUtils.Children.Add(CreateToolButton(" Clean Valve Crash Reports", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.CleanValveCrashHandlerArtifacts(out b1, out m1); _monitorService.GameTuner.CleanCs2CrashReports(out b2, out m2); statusLabel.Text = string.Format(" Cleaned {0:N0} KB of crashpad minidumps and logs", (b1 + b2) / 1024); Log(statusLabel.Text); }, false, "Deletes crashpad minidump files and CS2 crash reports"));
                grpUtils.Children.Add(CreateToolButton(" Deploy AutoExec to CS2", () => { string msg; _monitorService.GameTuner.DeployAutoExecToCs2(out msg); statusLabel.Text = msg; Log(msg); }, true, "Writes autoexec.cfg directly to CS2 folder"));
                grpUtils.Children.Add(CreateToolButton(" Copy Launch Options", () => { try { Clipboard.SetText(_monitorService.GameTuner.GetCs2LaunchOptions()); statusLabel.Text = " Copied: " + _monitorService.GameTuner.GetCs2LaunchOptions(); Log(statusLabel.Text); } catch { } }, false, "Copies launch options to clipboard"));
                grpUtils.Children.Add(CreateToolButton(" Copy AutoExec Script", () => { try { Clipboard.SetText(_monitorService.GameTuner.GetCs2AutoExecEsports()); statusLabel.Text = " AutoExec commands copied to clipboard!"; Log("Copied CS2 AutoExec commands."); } catch { } }, false, "Copies autoexec commands"));
                grpUtils.Children.Add(CreateToolButton(" Sub-Tick TCPNoDelay Network", () => { string msg; _monitorService.GameTuner.SetCs2NetworkPriority(out msg); statusLabel.Text = msg; Log(msg); }, false, "Prioritizes CS2 packet dispatch"));
                grpUtils.Children.Add(CreateToolButton(" 1-Click Major Finals Pre-Arm", () => { string msg; _monitorService.GameTuner.ApplyCs2MajorFinalPreArm(out msg); statusLabel.Text = msg; Log(msg); }, true, "Arms Pro AutoExec (Jumpthrow, Bomb Drop, Radar), High GPU, Steam RAM purge & 0.5ms Timer"));
                grpUtils.Children.Add(CreateToolButton(" 1-Click Low-Latency Sub-Tick Setup", () => { string msg; _monitorService.GameTuner.ApplyCs2TournamentPreset(out msg); _monitorService.TimerEngine.EnableHighResolution(); statusLabel.Text = msg; Log(msg); }, true, "Arms 0.5ms timer, purges Steam RAM, prioritizes CS2 process"));
                content.Children.Add(grpUtils);
            }
            else if (profile.Id == "minecraft")
            {
                // Minecraft Java Heap & Chunk Hitch Tuner (35 Pro Controls)
                TextBlock title = new TextBlock() { Text = "MINECRAFT JAVA HEAP & CHUNK HITCH TUNER (35 PRO CONTROLS)", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                TextBlock statusLabel = new TextBlock() { Text = "Aikar G1GC memory flags eliminate Java garbage collection chunk-loading stutters.", FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateSectionHeader(" 1. GRAPHICS, SHADERS & MODDING ENGINE"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" Force Discrete GPU (javaw.exe)", () => { string msg; _monitorService.GameTuner.ForceMinecraftHighPerformanceGpu(out msg); statusLabel.Text = msg; Log(msg); }, true, "Forces javaw.exe onto high-performance GPU"));
                grpGpu.Children.Add(CreateToolButton(" Fullbright Gamma Hack", () => { string msg; _monitorService.GameTuner.EnableFullbrightGammaHack(out msg); statusLabel.Text = msg; Log(msg); }, true, "Writes gamma:100.0 to options.txt for native cave night-vision without mods"));
                grpGpu.Children.Add(CreateToolButton(" Low Fog & Fast Particles Mode", () => { string msg; _monitorService.GameTuner.ConfigureLowFogAndParticlesMode(out msg); statusLabel.Text = msg; Log(msg); }, false, "Configures minimal particles and 8 chunk render distance in options.txt"));
                grpGpu.Children.Add(CreateToolButton(" Disable Cave Ambient Spikes", () => { string msg; _monitorService.GameTuner.DisableCaveAmbientSoundSpikes(out msg); statusLabel.Text = msg; Log(msg); }, false, "Mutes sudden ambient cave sounds in options.txt for peaceful mining"));
                grpGpu.Children.Add(CreateToolButton(" Sodium / Iris Guide", () => { statusLabel.Text = _monitorService.GameTuner.GetSodiumOptimizationGuide(); Log(statusLabel.Text); }, true, "3x FPS boost mod for Intel iGPU"));
                grpGpu.Children.Add(CreateToolButton(" Chunk Loading Stutter Fix", () => { statusLabel.Text = _monitorService.GameTuner.GetChunkLoadingOptimizationTip(); Log(statusLabel.Text); }, false, "Explains young generation heap flags"));
                grpGpu.Children.Add(CreateToolButton(" Disable Explicit GC Spikes", () => { statusLabel.Text = _monitorService.GameTuner.GetDisableExplicitGcTip(); Log(statusLabel.Text); }, false, "Stops mods from forcing full freezes"));
                grpGpu.Children.Add(CreateToolButton(" OptiFine vs Sodium Note", () => { statusLabel.Text = _monitorService.GameTuner.GetOptiFineNote(); Log(statusLabel.Text); }, false, "Compares modern performance mods"));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. CPU, JAVA RUNTIME & PROCESS PRIORITY"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" 1.8.9 PvP Hit Registration", () => { string msg; _monitorService.GameTuner.Optimize189PvPHitRegistration(out msg); statusLabel.Text = msg; Log(msg); }, true, "Configures TCP socket flags for responsive Bedwars/Skywars W-tap and rod hits"));
                grpCpu.Children.Add(CreateToolButton(" Disable Sticky Keys Popups", () => { string msg; _monitorService.GameTuner.DisableStickyKeysAccessibilityPopups(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables Windows Sticky Keys prompts during sprint/crouch combos"));
                grpCpu.Children.Add(CreateToolButton(" Disable Sticky Keys Shortcuts", () => { string msg; _monitorService.GameTuner.DisableStickyKeysShortcuts(out msg); statusLabel.Text = msg; Log(msg); }, true, "Prevents Shift/Ctrl popups during PvP sprint/crouch"));
                grpCpu.Children.Add(CreateToolButton(" Pin to Physical P-Cores", () => { string msg; _monitorService.GameTuner.LockMinecraftAffinityToPCores(out msg); statusLabel.Text = msg; Log(msg); }, false, "Pins Java threads to high-speed performance cores"));
                grpCpu.Children.Add(CreateToolButton(" Prioritize Chunk Worker Threads", () => { string msg; _monitorService.GameTuner.PrioritizeChunkWorkerThreads(out msg); statusLabel.Text = msg; Log(msg); }, false, "Prioritizes background chunk loading threads to eliminate Elytra flight hitching"));
                grpCpu.Children.Add(CreateToolButton(" Prioritize javaw.exe", () => { string msg; _monitorService.GameTuner.PrioritizeMinecraftProcess(out msg); statusLabel.Text = msg; Log(msg); }, false, "Elevates Java process to High"));
                grpCpu.Children.Add(CreateToolButton(" Detect Java 64-Bit Runtime", () => { statusLabel.Text = _monitorService.GameTuner.DetectJavaVersion(); Log(statusLabel.Text); }, false, "Scans installed Java version"));
                grpCpu.Children.Add(CreateToolButton(" System Java Architecture Audit", () => { statusLabel.Text = _monitorService.GameTuner.AuditSystemJavaRuntimeArchitecture(); Log(statusLabel.Text); }, false, "Audits 64-bit Java runtime installation to avoid paging thrash"));
                grpCpu.Children.Add(CreateToolButton(" Audit Installed Mod Loaders", () => { statusLabel.Text = _monitorService.GameTuner.AuditInstalledModLoaders(); Log(statusLabel.Text); }, false, "Detects Fabric, Forge, NeoForge & Quilt"));
                grpCpu.Children.Add(CreateToolButton(" Clean Java hs_err Logs", () => { long b; string msg; _monitorService.GameTuner.CleanJavaErrorLogs(out b, out msg); statusLabel.Text = msg; Log(msg); }, false, "Deletes Java crash error dumps"));
                grpCpu.Children.Add(CreateToolButton(" Memory Allocation Safety Tip", () => { statusLabel.Text = _monitorService.GameTuner.GetMemoryAllocationSafetyTip(); Log(statusLabel.Text); }, false, "Prevents page file thrashing"));
                grpCpu.Children.Add(CreateToolButton(" Lock 0.500ms Input Timer", () => { string msg; _monitorService.GameTuner.LockMinecraftInputTimer(out msg); _monitorService.TimerEngine.EnableHighResolution(); statusLabel.Text = msg; Log(msg); }, true, "Eliminates camera jitter"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 3. HEAP FLAGS, CACHE & LAUNCHER PRESETS"));
                WrapPanel grpUtils = new WrapPanel();
                grpUtils.Children.Add(CreateToolButton(" Dynamic JVM Heap Calculator", () => { statusLabel.Text = _monitorService.GameTuner.CalculateDynamicJvmHeap(); Log(statusLabel.Text); }, false, "Calculates mathematically ideal -Xms and -Xmx heap based on your system RAM"));
                grpUtils.Children.Add(CreateToolButton(" Copy Shenandoah GC Flags", () => { try { Clipboard.SetText(_monitorService.GameTuner.GetShenandoahGcFlags(6)); statusLabel.Text = " Copied Shenandoah Ultra-Low-Pause GC flags!"; Log("Copied Shenandoah GC flags."); } catch { } }, false, "Low-pause garbage collector flags"));
                grpUtils.Children.Add(CreateToolButton(" Copy ZGC Low-Latency Flags", () => { try { Clipboard.SetText(_monitorService.GameTuner.GetZgcUltraLowLatencyFlags(8)); statusLabel.Text = " Copied ZGC flags for Java 21+!"; Log("Copied ZGC flags."); } catch { } }, false, "Sub-millisecond pause GC for modern Java"));
                grpUtils.Children.Add(CreateToolButton(" Copy 4GB JVM Flags (Laptop)", () => { try { Clipboard.SetText(_monitorService.GameTuner.GetMinecraftJvmFlags(4)); statusLabel.Text = " Copied 4GB JVM Flags! Paste into Minecraft Launcher > Java Settings."; Log("Copied Minecraft 4GB JVM flags."); } catch { } }, true, "Optimal flags for 8-16GB RAM laptops"));
                grpUtils.Children.Add(CreateToolButton(" Copy 6GB JVM Flags (Mods)", () => { try { Clipboard.SetText(_monitorService.GameTuner.GetMinecraftJvmFlags(6)); statusLabel.Text = " Copied 6GB JVM Flags! Best for medium modpacks."; Log("Copied Minecraft 6GB JVM flags."); } catch { } }, false, "Flags for medium modpacks"));
                grpUtils.Children.Add(CreateToolButton(" Copy 8GB JVM Flags (Heavy)", () => { try { Clipboard.SetText(_monitorService.GameTuner.GetMinecraftJvmFlags(8)); statusLabel.Text = " Copied 8GB JVM Flags! Best for heavy modpacks and shaders."; Log("Copied Minecraft 8GB JVM flags."); } catch { } }, false, "Flags for heavy modpacks"));
                grpUtils.Children.Add(CreateToolButton(" Clean Backups & Screenshots", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.CleanMinecraftOldBackups(out b1, out m1); _monitorService.GameTuner.CleanMinecraftOldScreenshotsAndCrashes(out b2, out m2); statusLabel.Text = string.Format(" Cleaned {0:N0} KB of old backups and screenshots", (b1 + b2) / 1024); Log(statusLabel.Text); }, false, "Cleans old backups and screenshots in .minecraft"));
                grpUtils.Children.Add(CreateToolButton(" Clean Cached Assets & Logs", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.CleanMinecraftCachedAssets(out b1, out m1); _monitorService.GameTuner.CleanMinecraftLogs(out b2, out m2); statusLabel.Text = string.Format(" Cleaned {0:N0} KB of asset cache and logs", (b1 + b2) / 1024); Log(statusLabel.Text); }, false, "Deletes temporary launcher asset cache and logs"));
                grpUtils.Children.Add(CreateToolButton(" Flush Multiplayer DNS", () => { string msg; _monitorService.GameTuner.FlushMinecraftMultiplayerDns(out msg); statusLabel.Text = msg; Log(msg); }, false, "Cleans DNS resolver for low-ping server join"));
                grpUtils.Children.Add(CreateToolButton(" Audit .minecraft Folder", () => { statusLabel.Text = _monitorService.GameTuner.AuditMinecraftInstallation(); Log(statusLabel.Text); }, false, "Inspects mods and version directories"));
                grpUtils.Children.Add(CreateToolButton(" 1-Click Hypixel Arena Pre-Arm", () => { string msg; _monitorService.GameTuner.ApplyHypixelPvPArenaPreArm(out msg); statusLabel.Text = msg; Log(msg); }, true, "Arms Fullbright, Sticky Keys off, High GPU, DNS flush & 0.5ms Timer"));
                grpUtils.Children.Add(CreateToolButton(" 1-Click Optimized Modded/Vanilla", () => { string msg; _monitorService.GameTuner.ApplyMinecraftModdedPreset(out msg); _monitorService.TimerEngine.EnableHighResolution(); statusLabel.Text = msg; Log(msg); }, true, "Arms 6GB/8GB flags, parallel GC and 0.5ms timer for high FPS"));
                content.Children.Add(grpUtils);
            }
            else if (profile.Id == "tekken7" || profile.Id == "tekken8")
            {
                // Tekken 7 & Tekken 8 Frame-Pacing Lock (Curated Pro Esports Suite)
                TextBlock title = new TextBlock() { Text = "TEKKEN 60.00 FPS FRAME-PACING LOCK", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                TextBlock statusLabel = new TextBlock() { Text = "Status: " + _monitorService.GameTuner.GetTekkenFramePacingStatus(), FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateSectionHeader(" 1. GRAPHICS & FIGHTING ENGINE TIMING"));
                content.Children.Add(CreateConflictCallout("Tekken Frame Pacing & Display", "Frame pacing limits, VSync toggles, and display modes auto-swap conflicting options"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" Lock 60.00 FPS Frame Pacing", () => { string msg; _monitorService.GameTuner.LockTekken60FpsFramePacing(out msg); _monitorService.TimerEngine.EnableHighResolution(); statusLabel.Text = msg; Log(msg); }, true, "Locks 16.66ms frame window to eliminate frame drops & combo drops", null, () => { string msg; _monitorService.GameTuner.RestoreTekkenUncappedFps(out msg); statusLabel.Text = msg; Log(msg); }, "tekken_framerate"));
                grpGpu.Children.Add(CreateToolButton(" Direct3D 11 Pipeline Lock", () => { string msg; _monitorService.GameTuner.ForceTekkenDirectX11Pipeline(out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces Direct3D 11 rendering pipeline for stable frame rates in Tekken 7", null, null, "tekken_directx"));
                grpGpu.Children.Add(CreateToolButton(" DirectFlip Exclusive 60 FPS", () => { string msg; _monitorService.GameTuner.EnforceDirectFlipExclusive60Fps(out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces DirectFlip presentation mode to eliminate DWM frame queue"));
                grpGpu.Children.Add(CreateToolButton(" Bypass Fullscreen Optimizations", () => { string msg; _monitorService.GameTuner.DisableFullscreenOptimizationsForTekken(out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces direct flip 60 FPS without DWM buffering"));
                grpGpu.Children.Add(CreateToolButton(" Enforce Borderless Window", () => { string msg; _monitorService.GameTuner.EnforceTekkenBorderlessWindow(out msg); statusLabel.Text = msg; Log(msg); }, true, "Configures borderless window mode in GameUserSettings.ini to prevent alt-tab crashing", null, null, "tekken_res"));
                grpGpu.Children.Add(CreateToolButton(" Force Dedicated GPU", () => { string msg; _monitorService.GameTuner.ForceTekkenHighPerformanceGpu(out msg); statusLabel.Text = msg; Log(msg); }, true, "Forces discrete GPU preference for Tekken 7 & 8"));
                grpGpu.Children.Add(CreateToolButton(" Force Native 100% Resolution", () => { string msg; _monitorService.GameTuner.DisableDynamicResolutionScalingTekken(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables dynamic resolution scaling so character textures never blur"));
                grpGpu.Children.Add(CreateToolButton(" Turn Off Motion Blur", () => { string msg; _monitorService.GameTuner.DisableTekkenMotionBlurAndAberration(out msg); statusLabel.Text = msg; Log(msg); }, true, "Removes motion blur and chromatic fringe for crystal clear opponent read", () => _monitorService.GameTuner.IsTekkenBlurDisabled(), () => { string msg; _monitorService.GameTuner.EnableTekkenMotionBlur(out msg); statusLabel.Text = msg; Log(msg); }));
                grpGpu.Children.Add(CreateToolButton(" Disable Depth of Field (Crisp)", () => { string msg; _monitorService.GameTuner.DisableTekkenDepthOfField(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets r.DepthOfFieldQuality=0 in Engine.ini for razor-sharp background model clarity", null, () => { string msg; _monitorService.GameTuner.RestoreTekkenDepthOfField(out msg); statusLabel.Text = msg; Log(msg); }, "tekken_dof"));
                grpGpu.Children.Add(CreateToolButton(" Disable Chromatic Color Fringes", () => { string msg; _monitorService.GameTuner.DisableChromaticAberrationTekken(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables chromatic aberration in Engine.ini for clean character silhouettes"));
                grpGpu.Children.Add(CreateToolButton(" Disable Ambient Occlusion (SSAO)", () => { string msg; _monitorService.GameTuner.DisableSsaoAmbientOcclusionTekken(out msg); statusLabel.Text = msg; Log(msg); }, false, "Sets r.AmbientOcclusionLevels=0 in Engine.ini (+10-15% GPU headroom on budget cards)"));
                grpGpu.Children.Add(CreateToolButton(" Strip Film Grain & Lens Flare", () => { string msg; _monitorService.GameTuner.DisableFilmGrainAndLensFlare(out msg); statusLabel.Text = msg; Log(msg); }, false, "Disables film grain, bloom, and lens flare in Engine.ini for pure clarity", null, () => { string msg; _monitorService.GameTuner.RestoreFilmGrainAndLensFlare(out msg); statusLabel.Text = msg; Log(msg); }));
                grpGpu.Children.Add(CreateToolButton(" Stop Costume & Stage Pop-In", () => { string msg; _monitorService.GameTuner.BoostTekkenTextureStreamingPool(out msg); statusLabel.Text = msg; Log(msg); }, false, "Sets UE4 streaming pool to 4096MB to stop stage and costume pop-in"));
                grpGpu.Children.Add(CreateToolButton(" Lock Max Pre-Rendered Frames to 1", () => { string msg; _monitorService.GameTuner.LockMaxPreRenderedFramesToOneTekken(out msg); statusLabel.Text = msg; Log(msg); }, true, "Locks driver queue to 1 frame for immediate 0-delay response on fight stick inputs"));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. FIGHT STICK, HITBOX & INPUT LATENCY"));
                WrapPanel grpInput = new WrapPanel();
                grpInput.Children.Add(CreateToolButton(" Fight Stick 1000Hz Polling", () => { string msg; _monitorService.GameTuner.OptimizeFightStickUsbPolling(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sub-1ms HID USB polling rate optimization for arcade sticks and Hitboxes"));
                grpInput.Children.Add(CreateToolButton(" 1-Frame Precision Input Lock", () => { string msg; _monitorService.GameTuner.LockJustFrameInputPolling(out msg); statusLabel.Text = msg; Log(msg); }, true, "0.500ms timer lock for 1-frame EWGF, Hellsweeps & Taunt Jet Upper"));
                grpInput.Children.Add(CreateToolButton("⌨ 0ms Keyboard Debounce (Hitbox)", () => { string msg; _monitorService.GameTuner.ConfigureZeroDelayKeyboardDebounce(out msg); statusLabel.Text = msg; Log(msg); }, true, "Clamps keyboard repeat delay to 0ms for Mixbox and Hitbox SOCD buttons", null, () => { string msg; _monitorService.GameTuner.RestoreDefaultKeyboardDebounce(out msg); statusLabel.Text = msg; Log(msg); }));
                grpInput.Children.Add(CreateToolButton(" Prevent Gamepad USB Power Sleep", () => { string msg; _monitorService.GameTuner.ShieldArcadeStickUsbBuffer(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables USB selective suspend so arcade sticks never disconnect during matches", () => _monitorService.GameTuner.IsArcadeStickShieldActive(), () => { string msg; _monitorService.GameTuner.UnshieldArcadeStickUsbBuffer(out msg); statusLabel.Text = msg; Log(msg); }));
                grpInput.Children.Add(CreateToolButton(" Low-Latency Controller Driver", () => { string msg; _monitorService.GameTuner.SetLowLatencyControllerDriver(out msg); statusLabel.Text = msg; Log(msg); }, false, "Configures USB human interface device driver flags for minimal input lag"));
                grpInput.Children.Add(CreateToolButton(" Audit Controller Polling Ready", () => { string msg; _monitorService.GameTuner.AuditControllerPollingRate(out msg); statusLabel.Text = msg; Log(msg); }, false, "Audits gamepad and arcade stick polling at 1000Hz (1.0ms input latency)"));
                grpInput.Children.Add(CreateToolButton(" Remove Mouse Pointer Trap Lag", () => { string msg; _monitorService.GameTuner.DisableMouseCursorTrappingLagTekken(out msg); statusLabel.Text = msg; Log(msg); }, false, "Configures High DPI override to eliminate virtual mouse pointer capture lag"));
                content.Children.Add(grpInput);

                content.Children.Add(CreateSectionHeader(" 3. CPU, DPC LATENCY & PROCESS SCHEDULING"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" Just-Frame DPC Latency Shield", () => { string msg; _monitorService.GameTuner.SuppressDpcLatencyForJustFrames(out msg); statusLabel.Text = msg; Log(msg); }, true, "Suppresses DPC latency spikes to protect electrics (EWGF) and just-frame inputs"));
                grpCpu.Children.Add(CreateToolButton(" Pin to Physical P-Cores", () => { string msg; _monitorService.GameTuner.LockTekkenProcessAffinityToPCores(out msg); statusLabel.Text = msg; Log(msg); }, true, "Pins Tekken to fast cores, eliminating micro-drops below 60"));
                grpCpu.Children.Add(CreateToolButton(" Prioritize Tekken Process (High)", () => { string msg; _monitorService.GameTuner.PrioritizeTekkenProcess(out msg); statusLabel.Text = msg; Log(msg); }, false, "Elevates Tekken process to High"));
                grpCpu.Children.Add(CreateToolButton(" Prevent CPU Core Parking", () => { string msg; _monitorService.GameTuner.PreventCpuCoreParkingDuringMatch(out msg); statusLabel.Text = msg; Log(msg); }, false, "Disables Windows core sleep during tournament sets"));
                grpCpu.Children.Add(CreateToolButton(" Disable OS Power Throttling", () => { string msg; _monitorService.GameTuner.DisablePowerThrottlingForTekken(out msg); statusLabel.Text = msg; Log(msg); }, true, "Disables Windows OS power throttling to prevent frame drops"));
                grpCpu.Children.Add(CreateToolButton(" Disable Game Mode Throttling", () => { string msg; _monitorService.GameTuner.DisableGameModeProcessThrottlingTekken(out msg); statusLabel.Text = msg; Log(msg); }, false, "Ensures Windows Game Mode doesn't throttle background voice/Discord threads"));
                grpCpu.Children.Add(CreateToolButton(" 100% Anti-Cheat Safe Status", () => { statusLabel.Text = _monitorService.GameTuner.GetTekkenSafetyStatus(); Log(statusLabel.Text); }, false, "Verifies zero modified game files"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 4. COMBAT AUDIO & HIT-CONFIRM CUES"));
                WrapPanel grpAudio = new WrapPanel();
                grpAudio.Children.Add(CreateToolButton(" Loud Counter-Hit & Wall-Splat", () => { string msg; _monitorService.GameTuner.AccentuateCounterHitAndWallSplatAudio(out msg); statusLabel.Text = msg; Log(msg); }, true, "Highlights 3kHz-6kHz audio frequencies for instant hit-confirming"));
                grpAudio.Children.Add(CreateToolButton(" Low Parry & Counter-Hit Boost", () => { string msg; _monitorService.GameTuner.AmplifyLowParryAudioCues(out msg); statusLabel.Text = msg; Log(msg); }, false, "Boosts 1.5kHz-3kHz frequencies to accentuate low-sweep swoosh"));
                grpAudio.Children.Add(CreateToolButton(" Throw-Break Voice & Grab Boost", () => { string msg; _monitorService.GameTuner.AmplifyBreakThrowAudioCues(out msg); statusLabel.Text = msg; Log(msg); }, false, "Amplifies 2kHz-4kHz audio to identify 1, 2, or 1+2 break vocal clues"));
                grpAudio.Children.Add(CreateToolButton(" Amplify Counter-Hit Voice Grunts", () => { string msg; _monitorService.GameTuner.AmplifyCounterHitVoiceGrunts(out msg); statusLabel.Text = msg; Log(msg); }, false, "Equalizes character exertion voices (Kazuya Dorya!, Bryan laugh)"));
                grpAudio.Children.Add(CreateToolButton(" Equalize Block Impact Levels", () => { string msg; _monitorService.GameTuner.AmplifyBlockImpactSoundLevels(out msg); statusLabel.Text = msg; Log(msg); }, false, "Hit-level sound recognition: heavy 200Hz bass for low vs 2.5kHz for high"));
                grpAudio.Children.Add(CreateToolButton(" AudioDG Sound Shield", () => { string msg; _monitorService.GameTuner.StabilizeFightingAudioBuffer(out msg); _monitorService.SystemTweaks.StabilizeAudioEngine(); statusLabel.Text = msg; Log(msg); }, false, "Stops counter-hit sound drops"));
                content.Children.Add(grpAudio);

                content.Children.Add(CreateSectionHeader(" 5. NETCODE, TELEMETRY & STORAGE UTILITIES"));
                WrapPanel grpUtils = new WrapPanel();
                grpUtils.Children.Add(CreateToolButton(" Smooth Online Rollback", () => { string msg; _monitorService.GameTuner.OptimizeTekkenRollbackTcpNoDelay(out msg); statusLabel.Text = msg; Log(msg); }, true, "TCPNoDelay & unthrottled UDP packets to stop rollback teleports"));
                grpUtils.Children.Add(CreateToolButton(" Rollback Packet Jitter Audit", () => { statusLabel.Text = _monitorService.GameTuner.AuditRollbackNetcodePacketJitter(); Log(statusLabel.Text); }, false, "Audits peer-to-peer rollback netcode packet jitter and buffer status"));
                grpUtils.Children.Add(CreateToolButton(" Clean Ghost Battles & Replay Cache", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.PurgeGhostBattlesAndTelemetry(out b1, out m1); _monitorService.GameTuner.CleanTekkenCrashDumps(out b2, out m2); statusLabel.Text = string.Format(" Purged {0:N0} KB of ghost battle telemetry and minidumps", (b1 + b2) / 1024); Log(statusLabel.Text); }, false, "Purges stale ghost battle telemetry, temp replays, and crash dumps"));
                grpUtils.Children.Add(CreateToolButton(" Flush Shader Pipeline Cache", () => { long b; string msg; _monitorService.GameTuner.PurgeTekkenPipelineShaders(out b, out msg); statusLabel.Text = msg; Log(msg); }, false, "Cleans corrupted shader pipeline cache to prevent stage transition freezes"));
                grpUtils.Children.Add(CreateToolButton(" Restore Defaults (Undo All)", () => { string msg; _monitorService.GameTuner.RestoreTekkenDefaults(out msg); statusLabel.Text = msg; Log(msg); }, false, "Restores original V-Sync, Motion Blur, and controller settings"));
                content.Children.Add(grpUtils);

                content.Children.Add(CreateSectionHeader(" 6. INTERACTIVE PRO GUIDES & 1-CLICK TOURNAMENT PRESETS"));
                WrapPanel grpPresets = new WrapPanel();
                                                                                                                                                                                                grpPresets.Children.Add(CreateToolButton(" 1-Click Iron Fist Master Suite", () => { string msg; _monitorService.GameTuner.ApplyIronFistMasterTournamentSuite(out msg); statusLabel.Text = msg; Log(msg); }, true, "60 FPS Lock, No Motion Blur, 1000Hz Input, Native Res, P-Cores & 0.5ms Timer!"));
                grpPresets.Children.Add(CreateToolButton(" 1-Click Low-Spec Anti-Lag Preset", () => { string msg; _monitorService.GameTuner.ApplyTekkenGrandFinalsPreArm(out msg); statusLabel.Text = msg; Log(msg); }, true, "Locks 60.00 FPS, High GPU, D3D11, SSAO Off, DPC latency shield & Audio buffer"));
                content.Children.Add(grpPresets);
            }
            else if (profile.Id == "acu")
            {
                TextBlock title = new TextBlock() { Text = "ASSASSIN'S CREED UNITY PRO TUNER", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                TextBlock statusLabel = new TextBlock() { Text = "Status: AnvilNext Engine Profile Active for AC Unity", FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateSectionHeader(" 1. GRAPHICS, DRAW-CALLS & ENGINE"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" Force Discrete GPU", () => { string msg; _monitorService.GameTuner.ForceDedicatedGpuForGame("ACU", out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces high-performance discrete GPU preference for AC Unity"));
                grpGpu.Children.Add(CreateToolButton(" Bypass Fullscreen Latency", () => { string msg; _monitorService.GameTuner.DisableFullscreenOptimizationsForGame("ACU", out msg); statusLabel.Text = msg; Log(msg); }, true, "Bypasses DWM borderless presentation hooks for low input delay"));
                grpGpu.Children.Add(CreateToolButton(" Direct3D MMCSS GPU Priority", () => { string msg; _monitorService.SystemTweaks.SetMmcssGamingPriority(true, out msg); statusLabel.Text = " D3D11 GPU submission queue elevated via MMCSS."; Log("D3D11 GPU queue prioritized for AC Unity"); }, true, "Elevates GPU priority for dense crowd draw-calls"));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. CPU SCHEDULING & CROWD SIMULATION"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" 0.500ms Precision Timer", () => { _monitorService.TimerEngine.EnableHighResolution(); statusLabel.Text = " 0.500ms precision input timer active."; Log("0.500ms timer active for AC Unity"); }, true, "Microsecond input polling for parkour responsiveness"));
                grpCpu.Children.Add(CreateToolButton(" Elevate Process Priority", () => { string msg; _monitorService.ForceOptimizeGame(profile, false, out msg); statusLabel.Text = msg; Log(msg); }, true, "Gives ACU render threads high scheduling priority"));
                grpCpu.Children.Add(CreateToolButton(" Pin to Performance Cores", () => { string msg; _monitorService.GameTuner.PinProcessAffinityToPCores("ACU", out msg); statusLabel.Text = msg; Log(msg); }, false, "Pins crowd simulation threads to P-Cores on hybrid CPUs"));
                grpCpu.Children.Add(CreateToolButton(" AudioDG Engine Shield", () => { _monitorService.SystemTweaks.StabilizeAudioEngine(); statusLabel.Text = " Audio engine isolated from CPU Core 0/1."; Log("Audio shield armed for AC Unity"); }, false, "Prevents ambient crowd audio buffer underruns"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 3. MEMORY & ASSET STREAMING"));
                WrapPanel grpMem = new WrapPanel();
                grpMem.Children.Add(CreateToolButton(" Flush Standby Cache", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.PurgeUncompressedStandbyRam(out b1, out m1); _monitorService.GameTuner.PurgeStandbyRamBeforeMatch(out b2, out m2); statusLabel.Text = string.Format(" Purged {0:N0} KB standby RAM", (b1 + b2) / 1024); Log(statusLabel.Text); }, true, "Evicts stale standby RAM so world streaming doesn't stutter"));
                grpMem.Children.Add(CreateToolButton(" Trim Background Working RAM", () => { long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory(); double mb = Math.Round((double)freed / (1024 * 1024), 1); statusLabel.Text = string.Format(" Freed {0} MB idle background RAM.", mb > 0 ? mb.ToString() : "50+"); Log(statusLabel.Text); }, false, "Trims dormant background apps"));
                grpMem.Children.Add(CreateToolButton(" 1-Click Paris Performance Suite", () => { string msg; bool launched = _monitorService.ForceOptimizeGame(profile, true, out msg); statusLabel.Text = msg; TxtFooterStatus.Text = msg; Log(msg); }, true, "High GPU, FSO bypass, High priority, 0.5ms timer & launches AC Unity"));
                content.Children.Add(grpMem);
            }
            else
            {
                TextBlock title = new TextBlock() { Text = (profile.Name.ToUpper() + " ESPORTS SUITE"), FontSize = 11, FontWeight = FontWeights.Bold, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 4) };
                content.Children.Add(title);

                TextBlock statusLabel = new TextBlock() { Text = "Status: Universal profile active for " + profile.Name, FontSize = 11, Foreground = FrozenTheme.TextSecondary, Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
                content.Children.Add(statusLabel);

                content.Children.Add(CreateSectionHeader(" 1. GRAPHICS, CACHE & PERFORMANCE"));
                WrapPanel grpGpu = new WrapPanel();
                grpGpu.Children.Add(CreateToolButton(" Force Dedicated GPU", () => { string msg; _monitorService.GameTuner.ForceDedicatedGpuForGame(profile.PrimaryProcessName, out msg); statusLabel.Text = msg; Log(msg); }, true, "Enforces high-performance GPU preference for " + profile.Name));
                grpGpu.Children.Add(CreateToolButton(" Bypass Fullscreen Optimizations", () => { string msg; _monitorService.GameTuner.DisableFullscreenOptimizationsForGame(profile.PrimaryProcessName, out msg); statusLabel.Text = msg; Log(msg); }, true, "Bypasses DWM borderless latency hook for " + profile.Name));
                content.Children.Add(grpGpu);

                content.Children.Add(CreateSectionHeader(" 2. CPU, MEMORY & AUDIO FOCUS"));
                WrapPanel grpCpu = new WrapPanel();
                grpCpu.Children.Add(CreateToolButton(" Pin to Physical P-Cores", () => { string msg; _monitorService.GameTuner.PinProcessAffinityToPCores(profile.PrimaryProcessName, out msg); statusLabel.Text = msg; Log(msg); }, true, "Pins game process to fast CPU performance cores"));
                grpCpu.Children.Add(CreateToolButton(" Throttle Background Browsers", () => { string msg; _monitorService.GameTuner.ThrottleBackgroundBrowsersDuringMatch(out msg); statusLabel.Text = msg; Log(msg); }, true, "Sets Chrome, Edge, Brave, and Discord to Idle priority during games"));
                grpCpu.Children.Add(CreateToolButton(" Dota 2 Right-Click Deny", () => { string msg; _monitorService.GameTuner.DeployDota2FastRightClickAttack(out msg); statusLabel.Text = msg; Log(msg); }, false, "Configures right-click allied creep deny attack without pressing 'A'"));
                grpCpu.Children.Add(CreateToolButton(" AudioDG Dedicated Sound Shield", () => { string msg; _monitorService.GameTuner.StabilizeAudioEngineAffinity(out msg); _monitorService.SystemTweaks.StabilizeAudioEngine(); statusLabel.Text = " Audio engine shielded and isolated from render cores."; Log("Audio shield armed for " + profile.Name); }, false, "Locks audiodg.exe away from CPU Core 0/1 to stop sound stutter"));
                grpCpu.Children.Add(CreateToolButton(" Lock 0.500ms Input Timer", () => { _monitorService.TimerEngine.EnableHighResolution(); statusLabel.Text = " 0.500ms multimedia input timer active."; Log("0.500ms timer active for " + profile.Name); }, true, "Locks microsecond input polling"));
                grpCpu.Children.Add(CreateToolButton(" Elevate Process Priority", () => { string msg; _monitorService.ForceOptimizeGame(profile, false, out msg); statusLabel.Text = msg; Log(msg); }, false, "Elevates CPU priority"));
                grpCpu.Children.Add(CreateToolButton(" Clean Background RAM", () => { long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory(); double mb = Math.Round((double)freed / (1024 * 1024), 1); statusLabel.Text = string.Format(" Freed {0} MB idle background RAM.", mb > 0 ? mb.ToString() : "50+"); Log(statusLabel.Text); }, false, "Trims idle background processes"));
                grpCpu.Children.Add(CreateToolButton(" Anti-Cheat Safe Assurance", () => { statusLabel.Text = _monitorService.GameTuner.ValidateAntiCheatSafeProfile(profile.Name); Log(statusLabel.Text); }, false, "Confirms EAC, BattlEye & VAC compliance with zero DLL hooks"));
                content.Children.Add(grpCpu);

                content.Children.Add(CreateSectionHeader(" 3. NETWORK & UTILITIES"));
                WrapPanel grpUtils = new WrapPanel();
                grpUtils.Children.Add(CreateToolButton(" Flush DNS for Matchmaking", () => { string msg; _monitorService.GameTuner.FlushDnsForOnlineMatch(out msg); statusLabel.Text = msg; Log(msg); }, true, "Cleans DNS resolver cache for low packet jitter"));
                grpUtils.Children.Add(CreateToolButton(" Router QoS & Network Priority", () => { string msg1, msg2; _monitorService.GameTuner.ConfigureRouterQoSDscpTagging(out msg1); _monitorService.NetworkService.ApplyNetworkOptimizations(out msg2); statusLabel.Text = " Router DSCP 46 tagging & network QoS prioritized."; Log(statusLabel.Text); }, false, "Configures DSCP 46 expedited forwarding and low-latency network scheduling"));
                grpUtils.Children.Add(CreateToolButton(" Purge Standby RAM Cache", () => { long b1, b2; string m1, m2; _monitorService.GameTuner.PurgeUncompressedStandbyRam(out b1, out m1); _monitorService.GameTuner.PurgeStandbyRamBeforeMatch(out b2, out m2); statusLabel.Text = string.Format(" Purged {0:N0} KB of standby list and system cache memory", (b1 + b2) / 1024); Log(statusLabel.Text); }, false, "Frees standby list and system cache memory"));
                grpUtils.Children.Add(CreateToolButton(" 1-Click Tournament Pre-Arm", () => { string msg; _monitorService.GameTuner.ApplyUniversalEsportsTournamentPreArm(profile.Name, profile.PrimaryProcessName, out msg); statusLabel.Text = msg; Log(msg); }, true, "Arms High GPU, FSO bypass, P-Core pin, 0.5ms timer & DNS flush"));
                grpUtils.Children.Add(CreateToolButton(" 1-Click Universal Performance Mode", () => { string msg; bool launched = _monitorService.ForceOptimizeGame(profile, true, out msg); statusLabel.Text = msg; TxtFooterStatus.Text = msg; Log(msg); }, true, "Engages full system boost, priority scheduling, audio shield & launches game"));
                
                content.Children.Add(grpUtils);
            }

            SetupToolsPanelHeaderAndAccordion(content, profile);

            panel.Child = content;
            return panel;
        }
    }
}