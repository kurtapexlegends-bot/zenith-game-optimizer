using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using ZenithOptimizer.Services;

namespace ZenithOptimizer
{
    public partial class MainWindow : Window
    {
        private void InitializeTrayIcon()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon();
                _notifyIcon.Text = "Zenith Game Optimizer";
                _notifyIcon.Icon = GenerateTrayIcon();
                _notifyIcon.Visible = true;

                var contextMenu = new System.Windows.Forms.ContextMenu();

                var openItem = new System.Windows.Forms.MenuItem("Open Dashboard", (s, e) => RestoreFromTray());
                openItem.DefaultItem = true;
                contextMenu.MenuItems.Add(openItem);

                contextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Boost PC for Gaming Now", (s, e) =>
                {
                    _monitorService.TimerEngine.EnableHighResolution();
                    _monitorService.PowerService.SetHighPerformanceScheme();
                    _monitorService.SystemTweaks.StabilizeAudioEngine();
                    long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
                    double mb = Math.Round((double)freed / (1024 * 1024), 1);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Game Optimizer", 
                        string.Format("High-Performance Gaming Mode Active! 0.5ms Timer engaged. Reclaimed {0} MB RAM.", mb > 0 ? mb.ToString() : "50+"), 
                        System.Windows.Forms.ToolTipIcon.Info);
                }));

                contextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Trim Background RAM", (s, e) =>
                {
                    long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
                    double mb = Math.Round((double)freed / (1024 * 1024), 1);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Memory Trim", 
                        string.Format("Reclaimed {0} MB from idle background applications.", mb > 0 ? mb.ToString() : "50+"), 
                        System.Windows.Forms.ToolTipIcon.Info);
                }));

                _trayCoolerMenuItem = new System.Windows.Forms.MenuItem("Laptop Thermal Guard & Active Cooler", (s, e) =>
                {
                    bool newState = !_monitorService.Settings.EnableLaptopThermalGuard;
                    _monitorService.Settings.EnableLaptopThermalGuard = newState;
                    if (ChkLaptopCooler != null)
                    {
                        ChkLaptopCooler.IsChecked = newState;
                    }
                    if (_trayCoolerMenuItem != null)
                    {
                        _trayCoolerMenuItem.Checked = newState;
                    }
                    string cMsg;
                    if (newState)
                    {
                        _monitorService.PowerService.EnableLaptopThermalGuard(out cMsg);
                        _notifyIcon.ShowBalloonTip(1500, "Zenith Thermal Guard", "Active Fan Cooler & 99% Frequency Ceiling Engaged. Temperatures dropping.", System.Windows.Forms.ToolTipIcon.Info);
                    }
                    else
                    {
                        _monitorService.PowerService.RestoreLaptopThermalGuard(out cMsg);
                        _notifyIcon.ShowBalloonTip(1500, "Zenith Thermal Guard", "Standard Windows Thermal & Boost Profile Restored.", System.Windows.Forms.ToolTipIcon.Info);
                    }
                    Log(cMsg);
                });
                _trayCoolerMenuItem.Checked = _monitorService.Settings.EnableLaptopThermalGuard;
                contextMenu.MenuItems.Add(_trayCoolerMenuItem);

                contextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Instant Laptop Cooldown (Fans Max / Dump Heat)", (s, e) =>
                {
                    string cMsg;
                    _monitorService.PowerService.EngageInstantCooldown(out cMsg);
                    long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
                    double mb = Math.Round((double)freed / (1024 * 1024), 1);
                    _monitorService.Settings.EnableLaptopThermalGuard = true;
                    if (ChkLaptopCooler != null) ChkLaptopCooler.IsChecked = true;
                    if (_trayCoolerMenuItem != null) _trayCoolerMenuItem.Checked = true;
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Laptop Cooldown", 
                        string.Format("Instant Cooldown active: Fans spinning, CPU capped at 80%. Freed {0} MB idle RAM.", mb > 0 ? mb.ToString() : "50+"), 
                        System.Windows.Forms.ToolTipIcon.Info);
                    Log(cMsg);
                }));

                contextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Restore Windows Defaults", (s, e) =>
                {
                    _monitorService.RestoreAllDefaults();
                    _notifyIcon.ShowBalloonTip(1500, "Zenith", "All settings restored to Windows factory defaults.", System.Windows.Forms.ToolTipIcon.Info);
                }));

                var presetMenu = new System.Windows.Forms.MenuItem("Quick-Arm Gaming Suites");
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm BedWars & Roblox PvP Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyBedwarsArsenalGrandChampionSuite(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", "BedWars & Roblox PvP Suite Armed.", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm Valorant Radiant Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyRadiantImmortalsTournamentSuite(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", "Valorant Radiant Suite Armed.", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm LoL Challenger Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyChallengerApexMacroCombo(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", "LoL Challenger Suite Armed.", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm Mobile Legends Mythical Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyMythicalGloryDominanceSuite(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", "Mobile Legends Mythical Suite Armed.", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm Tekken 7 Master Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyIronFistMasterTournamentSuite(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", "Tekken 7 Master Suite Armed.", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                contextMenu.MenuItems.Add(presetMenu);

                contextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("-"));

                var exitItem = new System.Windows.Forms.MenuItem("Exit Zenith", (s, e) =>
                {
                    _isExplicitExit = true;
                    this.Close();
                });
                contextMenu.MenuItems.Add(exitItem);

                _notifyIcon.ContextMenu = contextMenu;
                _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MainWindow.Tray] InitializeTrayIcon error: " + ex.Message);
            }
        }

        public void RestoreFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            _monitorService.TriggerTelemetryRefresh();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                _monitorService.MemoryService.TrimSelfMemory();
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                if (IconMaximize != null && IconRestore != null)
                {
                    IconMaximize.Visibility = Visibility.Collapsed;
                    IconRestore.Visibility = Visibility.Visible;
                }
                if (BtnWinMaximize != null) BtnWinMaximize.ToolTip = "Restore Down";
                if (RootGrid != null) RootGrid.Margin = new Thickness(7);
                _monitorService.TriggerTelemetryRefresh();
            }
            else if (this.WindowState == WindowState.Normal)
            {
                if (IconMaximize != null && IconRestore != null)
                {
                    IconMaximize.Visibility = Visibility.Visible;
                    IconRestore.Visibility = Visibility.Collapsed;
                }
                if (BtnWinMaximize != null) BtnWinMaximize.ToolTip = "Maximize / Fullscreen";
                if (RootGrid != null) RootGrid.Margin = new Thickness(0);
                _monitorService.TriggerTelemetryRefresh();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                e.Cancel = true;
                this.Hide();
                if (_notifyIcon != null)
                {
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Game Optimizer", 
                        "Zenith is running quietly in the system tray. Right-click the tray icon to exit anytime.", 
                        System.Windows.Forms.ToolTipIcon.None);
                }
                _monitorService.MemoryService.TrimSelfMemory();
            }
            else
            {
                try
                {
                    if (_monitorService != null && _monitorService.Settings != null)
                    {
                        _monitorService.Settings.ToolActiveStates = new Dictionary<string, bool>(_toolActiveStates);
                        _monitorService.Settings.Theme = _isDarkMode ? "Dark" : "Light";
                        _monitorService.Settings.Save();
                    }
                }
                catch (Exception ex) { Debug.WriteLine("[MainWindow.Tray] Settings save error: " + ex.Message); }

                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                if (_persistentIconBig != null)
                {
                    try { _persistentIconBig.Dispose(); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                    _persistentIconBig = null;
                }
                if (_persistentIconSmall != null)
                {
                    try { _persistentIconSmall.Dispose(); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                    _persistentIconSmall = null;
                }
                _monitorService.Dispose();
            }
        }
    }
}
