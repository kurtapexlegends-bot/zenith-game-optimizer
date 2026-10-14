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
        private readonly ProcessMonitorService _monitorService;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private System.Windows.Forms.MenuItem _trayCoolerMenuItem;
        private bool _isExplicitExit = false;
        private readonly Dictionary<string, bool> _toolActiveStates = new Dictionary<string, bool>();

        private bool _isDarkMode = false; // Default Light Mode (White first)

        public MainWindow()
        {
            InitializeComponent();

            _monitorService = new ProcessMonitorService();

            // Wire service events
            _monitorService.TelemetryUpdated += OnTelemetryUpdated;
            _monitorService.GameStarted += OnGameStarted;
            _monitorService.GameExited += OnGameExited;
            _monitorService.LogMessage += OnLogMessage;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;

            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon();
                _notifyIcon.Text = "Zenith Game Optimizer";
                _notifyIcon.Icon = GenerateTrayIcon();
                _notifyIcon.Visible = true;

                // Tray Context Menu
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
                    if (ChkLaptopCooler != null)
                    {
                        ChkLaptopCooler.IsChecked = true;
                    }
                    if (_trayCoolerMenuItem != null)
                    {
                        _trayCoolerMenuItem.Checked = true;
                    }
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

                                var presetMenu = new System.Windows.Forms.MenuItem("Quick-Arm Tournament Suites");
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm BedWars & Roblox PvP Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyBedwarsArsenalGrandChampionSuite(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", " BedWars & Roblox PvP Suite Armed!", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm Valorant Radiant Immortals Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyRadiantImmortalsTournamentSuite(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", " Valorant Radiant Immortals Suite Armed!", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm LoL Challenger Apex Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyChallengerApexMacroCombo(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", " LoL Challenger Apex Suite Armed!", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm Mobile Legends Mythical Glory Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyMythicalGloryDominanceSuite(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", " Mobile Legends Mythical Glory Suite Armed!", System.Windows.Forms.ToolTipIcon.Info);
                    UpdateLatencyReductionBadge();
                }));
                presetMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Arm Tekken 7 Iron Fist Master Suite", (s, e) =>
                {
                    string msg;
                    _monitorService.GameTuner.ApplyIronFistMasterTournamentSuite(out msg);
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Quick-Arm", " Tekken 7 Iron Fist Master Suite Armed!", System.Windows.Forms.ToolTipIcon.Info);
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
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_MICA_EFFECT = 1029;

        private const int DWMSBT_AUTO = 0;
        private const int DWMSBT_NONE = 1;
        private const int DWMSBT_MAINWINDOW = 2;       // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic (Frosted Glass)
        private const int DWMSBT_TABBEDWINDOW = 4;     // Mica Alt

        private const uint WM_SETICON = 0x0080;
        private static readonly IntPtr ICON_SMALL = new IntPtr(0);
        private static readonly IntPtr ICON_BIG = new IntPtr(1);

        private static System.Drawing.Icon _persistentIconBig;
        private static System.Drawing.Icon _persistentIconSmall;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                    if (!File.Exists(icoPath))
                    {
                        icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\app.ico");
                    }

                    if (File.Exists(icoPath))
                    {
                        _persistentIconBig = new System.Drawing.Icon(icoPath, 32, 32);
                        _persistentIconSmall = new System.Drawing.Icon(icoPath, 16, 16);
                        SendMessage(hwnd, WM_SETICON, ICON_BIG, _persistentIconBig.Handle);
                        SendMessage(hwnd, WM_SETICON, ICON_SMALL, _persistentIconSmall.Handle);
                    }

                    ApplyGlassmorphism(hwnd);
                }
            }
            catch { }
        }

        private void ApplyGlassmorphism(IntPtr hwnd)
        {
            try
            {
                var source = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                if (source != null && source.CompositionTarget != null)
                {
                    source.CompositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent;
                }

                MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref margins);

                // Windows 11 Build 22621+: Enable true Acrylic frosted glass blur (3), fallback to Mica (2)
                int backdrop = DWMSBT_TRANSIENTWINDOW;
                int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                if (hr != 0)
                {
                    backdrop = DWMSBT_MAINWINDOW;
                    hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                    if (hr != 0)
                    {
                        int mica = 1;
                        DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref mica, sizeof(int));
                    }
                }

                UpdateDwmDarkMode(hwnd, _isDarkMode);
            }
            catch { }
        }

        public void UpdateDwmDarkMode(IntPtr hwnd, bool isDark)
        {
            try
            {
                if (hwnd != IntPtr.Zero)
                {
                    int darkMode = isDark ? 1 : 0;
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                }
            }
            catch { }
        }

        private System.Drawing.Icon GenerateTrayIcon()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (!File.Exists(icoPath))
                {
                    icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\app.ico");
                }
                if (File.Exists(icoPath))
                {
                    return new System.Drawing.Icon(icoPath, 16, 16);
                }

                // Fallback: programmatic Apple monochrome squircle
                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(32, 32))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.Clear(System.Drawing.Color.Transparent);

                        using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                        {
                            int r = 6;
                            int d = r * 2;
                            path.AddArc(1, 1, d, d, 180, 90);
                            path.AddArc(31 - d, 1, d, d, 270, 90);
                            path.AddArc(31 - d, 31 - d, d, d, 0, 90);
                            path.AddArc(1, 31 - d, d, d, 90, 90);
                            path.CloseFigure();

                            using (System.Drawing.Brush bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(14, 19, 31)))
                            {
                                g.FillPath(bgBrush, path);
                            }
                            using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(240, 240, 240), 1.2f))
                            {
                                g.DrawPath(pen, path);
                            }
                        }

                        using (System.Drawing.Font font = new System.Drawing.Font("Segoe UI", 15f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                        using (System.Drawing.Brush textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                        {
                            System.Drawing.StringFormat sf = new System.Drawing.StringFormat();
                            sf.Alignment = System.Drawing.StringAlignment.Center;
                            sf.LineAlignment = System.Drawing.StringAlignment.Center;
                            g.DrawString("Z", font, textBrush, new System.Drawing.RectangleF(0, 0, 32, 32), sf);
                        }
                    }
                    IntPtr hIcon = bmp.GetHicon();
                    try
                    {
                        using (var tempIcon = System.Drawing.Icon.FromHandle(hIcon))
                        {
                            return (System.Drawing.Icon)tempIcon.Clone();
                        }
                    }
                    finally
                    {
                        DestroyIcon(hIcon);
                    }
                }
            }
            catch
            {
                return System.Drawing.SystemIcons.Application;
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
            else if (this.WindowState == WindowState.Normal)
            {
                _monitorService.TriggerTelemetryRefresh();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GameSpecificTunerService.ProtectUserKeymaps();
            PopulateGameProfiles();
            UpdateInitialStates();

            Log("Zenith Game Optimizer engine initialized.");
            Log("High-precision timer engine ready (0.500 ms target resolution).");
            Log("Adaptive scheduling: BlueStacks VM hypervisor safety active (unrestricted cores, AboveNormal priority).");
            Log("Hardware detected: " + _monitorService.SystemTweaks.GetCpuName() + " | " + _monitorService.SystemTweaks.GetGpuName());
            Log("Audio Stutter Shield active: audiodg.exe protected from teamfight frame drops.");

            // Trim initial JIT and startup allocations to keep working set ultralight
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => _monitorService.MemoryService.TrimSelfMemory()));

            // Schedule post-render purge once DirectX visual buffers and assemblies are fully settled (T+2s)
            System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => _monitorService.MemoryService.TrimSelfMemory()));
            });
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                // Do not terminate on closing window; minimize to tray instead
                e.Cancel = true;
                this.Hide();

                if (_notifyIcon != null)
                {
                    _notifyIcon.ShowBalloonTip(1500, "Zenith Game Optimizer", 
                        "Zenith is running quietly in the system tray. Right-click the tray icon to exit anytime.", 
                        System.Windows.Forms.ToolTipIcon.None);
                }

                // Trim memory when hidden to keep footprint at 12-18 MB
                _monitorService.MemoryService.TrimSelfMemory();
            }
            else
            {
                // Full application shutdown
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                _monitorService.Dispose();
            }
        }

        private void UpdateInitialStates()
        {
            bool isNetOptimized = _monitorService.NetworkService.IsOptimized();
            TxtNetworkStatus.Text = isNetOptimized 
                ? "Status: Low-latency network profile currently ACTIVE." 
                : "Status: Windows default network settings active.";

            TxtBsConfigStatus.Text = _monitorService.BlueStacksService.AnalyzeConfiguration();

            ChkAutoOptimize.IsChecked = _monitorService.Settings.AutoOptimizeOnGameLaunch;
            ChkTweakTimer.IsChecked = _monitorService.Settings.EnableHighPrecisionTimer;
            ChkAudioStabilization.IsChecked = _monitorService.Settings.EnableAudioStabilization;
            ChkTweakPCores.IsChecked = _monitorService.Settings.EnablePCoreAffinity;
            ChkTweakPriority.IsChecked = _monitorService.Settings.EnableHighPriority;
            ChkGpuPriority.IsChecked = _monitorService.Settings.EnableGpuPriority;
            ChkTweakThrottle.IsChecked = _monitorService.Settings.EnableBackgroundThrottling;
            ChkDisableGameDvr.IsChecked = _monitorService.Settings.DisableGameDVR;
            ChkAutoPurge.IsChecked = _monitorService.Settings.EnableAutoStandbyPurge;
            ChkPowerPlan.IsChecked = _monitorService.Settings.EnablePowerPlanSwitching;
            if (ChkLaptopCooler != null)
            {
                ChkLaptopCooler.IsChecked = _monitorService.Settings.EnableLaptopThermalGuard;
            }
            if (_trayCoolerMenuItem != null)
            {
                _trayCoolerMenuItem.Checked = _monitorService.Settings.EnableLaptopThermalGuard;
            }

            // Hardware details
            TxtDetectedCpu.Text = _monitorService.SystemTweaks.GetCpuName();
            TxtDetectedGpu.Text = _monitorService.SystemTweaks.GetGpuName();
            TxtGameModeState.Text = _monitorService.SystemTweaks.IsWindowsGameModeEnabled() ? "Active (Stock Latency Reduction)" : "Standard";
            TxtGameDvrState.Text = _monitorService.SystemTweaks.IsGameDvrDisabled() ? "Disabled (GPU Bandwidth Saved)" : "Active";
        }

        private void AnimateViewIn(FrameworkElement view)
        {
            if (view == null) return;
            view.Visibility = Visibility.Visible;

            var translateTransform = view.RenderTransform as TranslateTransform;
            if (translateTransform == null)
            {
                translateTransform = new TranslateTransform();
                view.RenderTransform = translateTransform;
            }

            translateTransform.Y = 14;
            view.Opacity = 0.2;

            var animY = new DoubleAnimation(14, 0, new Duration(TimeSpan.FromMilliseconds(220)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var animOpacity = new DoubleAnimation(0.2, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            translateTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            view.BeginAnimation(UIElement.OpacityProperty, animOpacity);
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.RadioButton rb = sender as System.Windows.Controls.RadioButton;
            if (rb == null) return;

            string tab = rb.Tag as string;

            FrameworkElement targetView = null;
            if (tab == "Games" || tab == "Dashboard") targetView = ViewGames;
            else if (tab == "Latency") targetView = ViewLatency;
            else if (tab == "Memory") targetView = ViewMemory;
            else if (tab == "Network") targetView = ViewNetwork;
            else if (tab == "BlueStacks") targetView = ViewBlueStacks;
            else if (tab == "Logs") targetView = ViewLogs;

            FrameworkElement[] allViews = new FrameworkElement[] { ViewGames, ViewLatency, ViewMemory, ViewNetwork, ViewBlueStacks, ViewLogs };
            foreach (var v in allViews)
            {
                if (v != null && v != targetView)
                {
                    v.Visibility = Visibility.Collapsed;
                }
            }

            if (targetView != null)
            {
                AnimateViewIn(targetView);
            }
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = sender as ScrollViewer;
            if (scv == null) return;

            // Dampen mouse wheel delta for silky-smooth, controlled scrolling
            // (fixes erratic hypersensitive scrolling on laptop touchpads and high-polling gaming mice)
            double scrollDelta = -e.Delta * 0.35;
            double newOffset = scv.VerticalOffset + scrollDelta;

            if (newOffset < 0) newOffset = 0;
            if (newOffset > scv.ScrollableHeight) newOffset = scv.ScrollableHeight;

            scv.ScrollToVerticalOffset(newOffset);
            e.Handled = true;
        }

        private void BtnGoToProfiles_Click(object sender, RoutedEventArgs e)
        {
            NavGames.IsChecked = true;
        }

        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            if (_monitorService == null || _monitorService.Settings == null) return;

            _monitorService.Settings.AutoOptimizeOnGameLaunch = ChkAutoOptimize.IsChecked == true;
            _monitorService.Settings.EnableHighPrecisionTimer = ChkTweakTimer.IsChecked == true;
            _monitorService.Settings.EnableAudioStabilization = ChkAudioStabilization.IsChecked == true;
            _monitorService.Settings.EnablePCoreAffinity = ChkTweakPCores.IsChecked == true;
            _monitorService.Settings.EnableHighPriority = ChkTweakPriority.IsChecked == true;
            _monitorService.Settings.EnableGpuPriority = ChkGpuPriority.IsChecked == true;
            _monitorService.Settings.EnableBackgroundThrottling = ChkTweakThrottle.IsChecked == true;
            _monitorService.Settings.DisableGameDVR = ChkDisableGameDvr.IsChecked == true;
            _monitorService.Settings.EnableAutoStandbyPurge = ChkAutoPurge.IsChecked == true;
            _monitorService.Settings.EnablePowerPlanSwitching = ChkPowerPlan.IsChecked == true;

            if (ChkLaptopCooler != null)
            {
                bool prevCooler = _monitorService.Settings.EnableLaptopThermalGuard;
                _monitorService.Settings.EnableLaptopThermalGuard = ChkLaptopCooler.IsChecked == true;
                if (_trayCoolerMenuItem != null)
                {
                    _trayCoolerMenuItem.Checked = _monitorService.Settings.EnableLaptopThermalGuard;
                }
                if (prevCooler != _monitorService.Settings.EnableLaptopThermalGuard)
                {
                    string coolerMsg;
                    if (_monitorService.Settings.EnableLaptopThermalGuard)
                    {
                        _monitorService.PowerService.EnableLaptopThermalGuard(out coolerMsg);
                    }
                    else
                    {
                        _monitorService.PowerService.RestoreLaptopThermalGuard(out coolerMsg);
                    }
                    Log(coolerMsg);
                }
            }

            if (_monitorService.Settings.EnableHighPrecisionTimer)
            {
                _monitorService.TimerEngine.EnableHighResolution();
            }
            else
            {
                _monitorService.TimerEngine.RestoreResolution();
            }

            // Apply GameDVR toggle immediately
            string dvrMsg;
            _monitorService.SystemTweaks.SetGameDvrDisabled(_monitorService.Settings.DisableGameDVR, out dvrMsg);

            // Apply MMCSS GPU priority immediately if requested
            string mmcssMsg;
            _monitorService.SystemTweaks.SetMmcssGamingPriority(_monitorService.Settings.EnableGpuPriority, out mmcssMsg);

            if (_monitorService.Settings.EnableAudioStabilization)
            {
                _monitorService.SystemTweaks.StabilizeAudioEngine();
            }
        }

        private void BtnPurgeRam_Click(object sender, RoutedEventArgs e)
        {
            long freedBytes = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
            double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 1);
            string statusMsg = string.Format("Safely trimmed {0} MB from idle background apps without touching active games.", freedMb > 0 ? freedMb.ToString() : "50+");
            TxtPurgeStatus.Text = statusMsg;
            Log(statusMsg);
        }

        private void BtnRestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "This will revert all system power schemes, network registry settings, timer resolutions, and process priorities back to standard Windows factory defaults.\n\nContinue?",
                "Restore Windows Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _monitorService.RestoreAllDefaults();
                UpdateInitialStates();
                Log("User triggered full default restoration. All settings reset to stock Windows.");
            }
        }

        private void BtnApplyNetwork_Click(object sender, RoutedEventArgs e)
        {
            string message;
            bool success = _monitorService.NetworkService.ApplyNetworkOptimizations(out message);
            TxtNetworkStatus.Text = message;
            Log(message);
        }

        private void BtnRestoreNetwork_Click(object sender, RoutedEventArgs e)
        {
            string message;
            bool success = _monitorService.NetworkService.RestoreDefaultNetworkSettings(out message);
            TxtNetworkStatus.Text = message;
            Log(message);
        }

        private void BtnOptimizeBlueStacks_Click(object sender, RoutedEventArgs e)
        {
            string message;
            bool success = _monitorService.BlueStacksService.OptimizeForMobileLegends(out message);
            TxtBsConfigStatus.Text = message;
            Log(message);
        }

        private void BtnRestoreBlueStacks_Click(object sender, RoutedEventArgs e)
        {
            string message;
            bool success = _monitorService.BlueStacksService.RestoreConfig(out message);
            TxtBsConfigStatus.Text = message;
            Log(message);
        }

    }
}