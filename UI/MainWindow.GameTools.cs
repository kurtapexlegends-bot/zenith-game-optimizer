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
using ZenithOptimizer.UI;

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
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 8, 0, 2),
                Visibility = Visibility.Collapsed
            };

            StackPanel content = new StackPanel();

            TextBlock statusLabel = new TextBlock()
            {
                Text = "Genuine OS & Hardware Optimizations - Safe & Zero-Drawback",
                FontSize = 11,
                Foreground = FrozenTheme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            content.Children.Add(statusLabel);

            WrapPanel toolsGrid = new WrapPanel();
            Border extraCallout = null;

            if (profile.Id == "bluestacks")
            {
                GameSpecificTunerService.ProtectUserKeymaps();

                toolsGrid.Children.Add(CreateToolButton("Force Discrete GPU", () =>
                {
                    string msg;
                    _monitorService.GameTuner.ForceBlueStacksHighPerformanceGpu(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Enforces high-performance discrete GPU in Windows Graphics Preferences",
                () => _toolActiveStates.ContainsKey("Force Discrete GPU") && _toolActiveStates["Force Discrete GPU"]));

                toolsGrid.Children.Add(CreateToolButton("Bypass DWM Fullscreen Hooks", () =>
                {
                    string msg;
                    _monitorService.BlueStacksService.OptimizeForMobileLegends(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Disables Fullscreen Optimizations to bypass DWM latency",
                () => _toolActiveStates.ContainsKey("Bypass DWM Fullscreen Hooks") && _toolActiveStates["Bypass DWM Fullscreen Hooks"]));

                toolsGrid.Children.Add(CreateToolButton("High Process Priority (IFEO)", () =>
                {
                    string msg;
                    _monitorService.BlueStacksService.OptimizeForMobileLegends(out msg);
                    statusLabel.Text = "IFEO High CPU Priority active for HD-Player.exe";
                    Log(statusLabel.Text);
                }, true, "Locks HD-Player.exe to High CPU priority in registry for stutter-free teamfights",
                () => _toolActiveStates.ContainsKey("High Process Priority (IFEO)") && _toolActiveStates["High Process Priority (IFEO)"]));

                toolsGrid.Children.Add(CreateToolButton("0.500ms Kernel Timer", () =>
                {
                    _monitorService.TimerEngine.EnableHighResolution();
                    statusLabel.Text = "Kernel timer locked to 0.500ms precision";
                    Log(statusLabel.Text);
                }, true, "Engages 0.500ms multimedia timer for smooth frame pacing",
                () => _monitorService.TimerEngine.IsHighResolutionActive,
                () =>
                {
                    _monitorService.TimerEngine.RestoreResolution();
                    statusLabel.Text = "Kernel timer restored to Windows default.";
                    Log(statusLabel.Text);
                }));

                toolsGrid.Children.Add(CreateToolButton("Unpark CPU Cores", () =>
                {
                    string msg;
                    _monitorService.PowerService.UnparkCpuCores(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Keeps CPU cores at 100% active state (zero wake latency)",
                () => _toolActiveStates.ContainsKey("Unpark CPU Cores") && _toolActiveStates["Unpark CPU Cores"]));

                toolsGrid.Children.Add(CreateToolButton("1:1 Raw Mouse Pacing", () =>
                {
                    string msg;
                    _monitorService.GameTuner.EnforceRawMouseInputPacing(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Removes dynamic pointer acceleration curves for pixel-perfect skill shots",
                () => _toolActiveStates.ContainsKey("1:1 Raw Mouse Pacing") && _toolActiveStates["1:1 Raw Mouse Pacing"],
                () =>
                {
                    string msg;
                    _monitorService.GameTuner.RestoreMouseInputPacing(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }));

                toolsGrid.Children.Add(CreateToolButton("Shrink Virtual Disk (VHD)", () =>
                {
                    long freed;
                    string msg;
                    _monitorService.BlueStacksService.CompactVirtualDisk(out freed, out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Safely compacts Data.vhdx to reclaim 10-25 GB of unallocated disk space"));

                toolsGrid.Children.Add(CreateToolButton("Restore Original Backup", () =>
                {
                    string msg;
                    _monitorService.BlueStacksService.RestoreConfig(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, false, "Restores BlueStacks configuration from backup"));
            }
            else if (profile.Id == "roblox")
            {
                toolsGrid.Children.Add(CreateToolButton("Zero-Drawback Roblox Engine & RAM Turbo", () =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyRobloxZeroDrawbackPerformanceSuite(out msg);
                    statusLabel.Text = "Roblox Zero-Drawback Engine & RAM Turbo Engaged (240 FPS, GPU Light Culling, Grass Stripped, RAM Streamlined).";
                    Log(msg);
                }, true, "Uncaps to 240 FPS, enables compute shader light culling, strips grass mesh overhead, disables texture preloading (frees 500MB+ RAM), locks 0.5ms timer and AboveNormal priority with zero visual defects",
                () => (_toolActiveStates.ContainsKey("Zero-Drawback Roblox Engine & RAM Turbo") && _toolActiveStates["Zero-Drawback Roblox Engine & RAM Turbo"]) || _monitorService.GameTuner.IsRobloxZeroDrawbackPerformanceSuiteActive(),
                () =>
                {
                    string msg;
                    _monitorService.GameTuner.RestoreRobloxZeroDrawbackPerformanceSuite(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }));

                toolsGrid.Children.Add(CreateToolButton("On-Screen FPS & Frametime Meter [F10]", () =>
                {
                    FpsMeterOverlay.Instance.Toggle();
                    bool fpsOn = FpsMeterOverlay.Instance.IsVisible;
                    statusLabel.Text = fpsOn ? "On-Screen FPS & Frametime Meter Active." : "On-Screen FPS Meter Closed.";
                    Log(statusLabel.Text);
                }, true, "Displays high-precision on-screen FPS, frametime in milliseconds, and 1% low frame stutter telemetry [F10]",
                () => FpsMeterOverlay.Instance.IsVisible,
                () =>
                {
                    if (FpsMeterOverlay.Instance.IsVisible)
                    {
                        FpsMeterOverlay.Instance.Toggle();
                    }
                    statusLabel.Text = "On-Screen FPS Meter Closed.";
                    Log(statusLabel.Text);
                }));

                toolsGrid.Children.Add(CreateToolButton("Auction Sweet-Spot Sniper HUD", () =>
                {
                    RobloxSniperOverlay.Instance.Toggle();
                    bool hudOn = RobloxSniperOverlay.Instance.IsVisible;
                    statusLabel.Text = hudOn ? "Roblox Bid Battles Sniper HUD Active." : "Roblox Sniper HUD Closed.";
                    Log(statusLabel.Text);
                }, true, "Toggles the optical sweet-spot bid sniper HUD on top of Roblox [F9]",
                () => RobloxSniperOverlay.Instance.IsVisible,
                () =>
                {
                    if (RobloxSniperOverlay.Instance.IsVisible)
                    {
                        RobloxSniperOverlay.Instance.Toggle();
                    }
                    statusLabel.Text = "Roblox Sniper HUD Closed.";
                    Log(statusLabel.Text);
                }));

                toolsGrid.Children.Add(CreateToolButton("Sniper Compact HUD Mode [Ctrl+F9]", () =>
                {
                    RobloxSniperOverlay.Instance.ToggleCompactMode();
                    bool compact = RobloxSniperOverlay.Instance.IsCompactMode;
                    statusLabel.Text = compact ? "Sniper Compact Micro-Pill HUD Active." : "Sniper Full HUD Panel Expanded.";
                    Log(statusLabel.Text);
                }, true, "Toggles between ultra-compact micro-pill HUD and full control deck [Ctrl+F9]",
                () => RobloxSniperOverlay.Instance.IsCompactMode,
                () =>
                {
                    RobloxSniperOverlay.Instance.SetCompactMode(false);
                    statusLabel.Text = "Sniper Full HUD Panel Expanded.";
                    Log(statusLabel.Text);
                }));

                toolsGrid.Children.Add(CreateToolButton("Roblox Native In-Game FPS Display", () =>
                {
                    string msg;
                    _monitorService.GameTuner.EnableNativeFpsDisplay(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Enables Roblox's built-in top-right FPS counter flag in ClientAppSettings.json without external software",
                () => _toolActiveStates.ContainsKey("Roblox Native In-Game FPS Display") && _toolActiveStates["Roblox Native In-Game FPS Display"]));

                toolsGrid.Children.Add(CreateToolButton("Unlock Draw Distance & Clear Fog", () =>
                {
                    string msg;
                    _monitorService.GameTuner.UnlockStorageHuntersRenderDistance(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Unlocks 100,000 studs draw distance, locks FRM quality at level 21, and clears dense fog shaders in Storage Hunters",
                () => (_toolActiveStates.ContainsKey("Unlock Draw Distance & Clear Fog") && _toolActiveStates["Unlock Draw Distance & Clear Fog"]) || _monitorService.GameTuner.IsStorageHuntersRenderDistanceUnlocked(),
                () =>
                {
                    string msg;
                    _monitorService.GameTuner.RestoreRobloxRenderDistance(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }));

                toolsGrid.Children.Add(CreateToolButton("1:1 Raw Mouse Pacing", () =>
                {
                    string msg;
                    _monitorService.GameTuner.EnforceRawMouseInputPacing(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Removes Windows dynamic mouse acceleration curves",
                () => _toolActiveStates.ContainsKey("1:1 Raw Mouse Pacing") && _toolActiveStates["1:1 Raw Mouse Pacing"],
                () =>
                {
                    string msg;
                    _monitorService.GameTuner.RestoreMouseInputPacing(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }));

                toolsGrid.Children.Add(CreateToolButton("Trim Inactive Working RAM", () =>
                {
                    long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
                    statusLabel.Text = string.Format("Trimmed {0:N0} MB of inactive background memory.", freed / (1024 * 1024));
                    Log(statusLabel.Text);
                }, false, "Safely flushes standby memory pages"));
            }
            else if (profile.Id == "valorant")
            {
                toolsGrid.Children.Add(CreateToolButton("Fix Alt+Tab Black Screen (DirectFlip)", () =>
                {
                    string msg;
                    _monitorService.GameTuner.FixValorantAltTabBlackScreen(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Fixes multi-second black screen when Alt-Tabbing by enabling DirectFlip borderless mode",
                () => _toolActiveStates.ContainsKey("Fix Alt+Tab Black Screen (DirectFlip)") && _toolActiveStates["Fix Alt+Tab Black Screen (DirectFlip)"]));

                toolsGrid.Children.Add(CreateToolButton("0.500ms Multimedia Timer", () =>
                {
                    _monitorService.TimerEngine.EnableHighResolution();
                    statusLabel.Text = "Kernel timer locked to 0.500ms precision";
                    Log(statusLabel.Text);
                }, true, "Locks system timer to sub-millisecond precision for minimal input lag",
                () => _monitorService.TimerEngine.IsHighResolutionActive,
                () =>
                {
                    _monitorService.TimerEngine.RestoreResolution();
                    statusLabel.Text = "Kernel timer restored to Windows default.";
                    Log(statusLabel.Text);
                }));

                toolsGrid.Children.Add(CreateToolButton("Verify Vanguard Safety", () =>
                {
                    string msg = _monitorService.GameTuner.GetVanguardSafetyAssurance();
                    statusLabel.Text = msg;
                    Log(msg);
                }, false, "Verifies zero external DLL hooks and Vanguard anti-cheat compliance"));
            }
            else if (profile.Id == "lol")
            {
                toolsGrid.Children.Add(CreateToolButton("Optimize Teamfight Config", () =>
                {
                    string msg;
                    _monitorService.GameTuner.OptimizeLeagueGameConfig(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Disables Inking and Godrays in game.cfg to eliminate render pass stalls"));

                toolsGrid.Children.Add(CreateToolButton("AboveNormal CPU Priority", () =>
                {
                    string msg;
                    _monitorService.ForceOptimizeGame(profile, true, out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Grants League of Legends process top scheduler priority in teamfights"));

                toolsGrid.Children.Add(CreateToolButton("Trim Working Set Memory", () =>
                {
                    long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
                    statusLabel.Text = string.Format("Trimmed {0:N0} MB from background processes.", freed / (1024 * 1024));
                    Log(statusLabel.Text);
                }, false, "Frees background memory without touching the active League client"));
            }
            else if (profile.Id == "tekken7")
            {
                toolsGrid.Children.Add(CreateToolButton("Lock 60 FPS Frame Pacing", () =>
                {
                    string msg;
                    _monitorService.GameTuner.LockTekken60FpsFramePacing(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Enforces locked 60 FPS frame timing for accurate combat mechanics"));

                toolsGrid.Children.Add(CreateToolButton("Minimise DPC Latency", () =>
                {
                    string msg;
                    _monitorService.GameTuner.MinimiseDpcLatency(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Sets Multimedia SystemProfile to High priority to prevent combo dropouts"));

                toolsGrid.Children.Add(CreateToolButton("AudioDG Priority Shield", () =>
                {
                    _monitorService.SystemTweaks.StabilizeAudioEngine();
                    statusLabel.Text = "AudioDG isolated to Core 1 at High Priority";
                    Log(statusLabel.Text);
                }, false, "Prevents audio buffer underruns and micro-hitches"));
            }
            else if (profile.Id == "cs2")
            {
                toolsGrid.Children.Add(CreateToolButton("Lock 0.500ms Timer Resolution", () =>
                {
                    _monitorService.TimerEngine.EnableHighResolution();
                    statusLabel.Text = "Windows kernel timer locked to 0.500ms precision";
                    Log(statusLabel.Text);
                }, true, "Eliminates sub-tick dispatch variance in CS2",
                () => _monitorService.TimerEngine.IsHighResolutionActive,
                () =>
                {
                    _monitorService.TimerEngine.RestoreResolution();
                    statusLabel.Text = "Windows standard timer resolution restored";
                    Log(statusLabel.Text);
                }));

                toolsGrid.Children.Add(CreateToolButton("Unpark CPU Cores", () =>
                {
                    string msg;
                    _monitorService.PowerService.UnparkCpuCores(out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Ensures all physical cores remain at maximum clock speed",
                () => _toolActiveStates.ContainsKey("Unpark CPU Cores") && _toolActiveStates["Unpark CPU Cores"]));

                toolsGrid.Children.Add(CreateToolButton("High Scheduling Priority", () =>
                {
                    string msg;
                    _monitorService.ForceOptimizeGame(profile, true, out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Sets cs2.exe scheduler priority to High",
                () => _toolActiveStates.ContainsKey("High Scheduling Priority") && _toolActiveStates["High Scheduling Priority"]));
            }
            else if (profile.Id == "acunity")
            {
                toolsGrid.Children.Add(CreateToolButton("AnvilNext High Thread Priority", () =>
                {
                    string msg;
                    _monitorService.ForceOptimizeGame(profile, true, out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Grants AnvilNext 2.0 engine threads high priority to prevent crowd stutters",
                () => _toolActiveStates.ContainsKey("AnvilNext High Thread Priority") && _toolActiveStates["AnvilNext High Thread Priority"]));

                toolsGrid.Children.Add(CreateToolButton("Bypass DWM Composition", () =>
                {
                    string msg;
                    _monitorService.SystemTweaks.SetGameDvrDisabled(true, out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Reduces presentation queue latency in Paris open-world roaming",
                () => _toolActiveStates.ContainsKey("Bypass DWM Composition") && _toolActiveStates["Bypass DWM Composition"]));
            }
            else if (profile.Id == "dota2")
            {
                toolsGrid.Children.Add(CreateToolButton("DirectX 11 / Vulkan High Priority", () =>
                {
                    string msg;
                    _monitorService.ForceOptimizeGame(profile, true, out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Grants Dota 2 top scheduler priority for teamfights",
                () => _toolActiveStates.ContainsKey("DirectX 11 / Vulkan High Priority") && _toolActiveStates["DirectX 11 / Vulkan High Priority"]));

                toolsGrid.Children.Add(CreateToolButton("0.500ms Kernel Timer", () =>
                {
                    _monitorService.TimerEngine.EnableHighResolution();
                    statusLabel.Text = "0.500ms precision timer active";
                    Log(statusLabel.Text);
                }, true, "Reduces command response time for last-hitting and animation cancels",
                () => _monitorService.TimerEngine.IsHighResolutionActive,
                () =>
                {
                    _monitorService.TimerEngine.RestoreResolution();
                    statusLabel.Text = "Windows standard timer resolution restored";
                    Log(statusLabel.Text);
                }));
            }
            else if (profile.Id == "minecraft")
            {
                toolsGrid.Children.Add(CreateToolButton("Java Working Set Memory Trim", () =>
                {
                    _monitorService.MemoryService.TrimSelfMemory();
                    long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
                    statusLabel.Text = string.Format("Trimmed {0:N0} MB of inactive memory.", freed / (1024 * 1024));
                    Log(statusLabel.Text);
                }, true, "Cleans background memory pages to prevent garbage collection spikes"));

                toolsGrid.Children.Add(CreateToolButton("High Process Scheduling Priority", () =>
                {
                    string msg;
                    _monitorService.ForceOptimizeGame(profile, true, out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Elevates javaw.exe thread priority in Windows scheduler"));
            }
            else
            {
                // Custom User Profile
                toolsGrid.Children.Add(CreateToolButton("Pre-Arm 0.500ms Timer & Priority", () =>
                {
                    string msg;
                    _monitorService.ForceOptimizeGame(profile, true, out msg);
                    statusLabel.Text = msg;
                    Log(msg);
                }, true, "Arms 0.500ms timer and elevates process priority"));

                toolsGrid.Children.Add(CreateToolButton("Trim Inactive Memory", () =>
                {
                    long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
                    statusLabel.Text = string.Format("Trimmed {0:N0} MB of background memory.", freed / (1024 * 1024));
                    Log(statusLabel.Text);
                }, false, "Frees idle standby memory pages"));
            }

            content.Children.Add(toolsGrid);
            if (extraCallout != null)
            {
                content.Children.Add(extraCallout);
            }
            panel.Child = content;
            return panel;
        }
    }
}