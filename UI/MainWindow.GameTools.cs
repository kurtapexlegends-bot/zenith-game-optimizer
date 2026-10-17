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
                    statusLabel.Text = fpsOn ? "On-Screen FPS & Frametime Meter Active [F10]. Draggable anywhere on screen." : "On-Screen FPS Meter Closed.";
                    Log(statusLabel.Text);
                }, true, "Displays high-precision on-screen FPS, frametime in milliseconds, and 1% low frame stutter telemetry [F10]. Draggable anywhere on screen by default.",
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

                Action<bool> onFpsLockState = (unlocked) =>
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (FpsMeterOverlay.Instance.IsVisible)
                        {
                            statusLabel.Text = unlocked ? "FPS Meter Draggable [Ctrl+F10 to lock click-through]." : "FPS Meter Click-Through Active [Ctrl+F10 to unlock].";
                        }
                    }));
                };
                FpsMeterOverlay.Instance.OnLockStateChanged += onFpsLockState;

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
            else
            {
                PopulateCompetitiveGameTools(profile, toolsGrid, statusLabel);
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
