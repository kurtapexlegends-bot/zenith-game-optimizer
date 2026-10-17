using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer
{
    public partial class MainWindow : Window
    {
        private void PopulateCompetitiveGameTools(GameProfile profile, WrapPanel toolsGrid, TextBlock statusLabel)
        {
            if (profile.Id == "valorant")
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
        }
    }
}
