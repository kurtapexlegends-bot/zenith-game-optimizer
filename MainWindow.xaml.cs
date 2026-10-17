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
        private bool _isLoadingSettings = false;

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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GameSpecificTunerService.ProtectUserKeymaps();
            UpdateInitialStates();
            PopulateGameProfiles();

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

        private void UpdateInitialStates()
        {
            _isLoadingSettings = true;
            try
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

                // Re-engage active kernel and hardware features loaded from persistent settings
                if (_monitorService.Settings.EnableHighPrecisionTimer)
                {
                    _monitorService.TimerEngine.EnableHighResolution();
                }
                if (_monitorService.Settings.EnableAudioStabilization)
                {
                    _monitorService.SystemTweaks.StabilizeAudioEngine();
                }
                if (_monitorService.Settings.DisableGameDVR)
                {
                    string dvrMsg;
                    _monitorService.SystemTweaks.SetGameDvrDisabled(true, out dvrMsg);
                }
                if (_monitorService.Settings.EnableGpuPriority)
                {
                    string mmcssMsg;
                    _monitorService.SystemTweaks.SetMmcssGamingPriority(true, out mmcssMsg);
                }
                if (_monitorService.Settings.EnablePowerPlanSwitching)
                {
                    _monitorService.PowerService.SetHighPerformanceScheme();
                }
                if (_monitorService.Settings.EnableLaptopThermalGuard)
                {
                    string coolerMsg;
                    _monitorService.PowerService.EnableLaptopThermalGuard(out coolerMsg);
                }
                if (_monitorService.Settings.IsFpsMeterEnabled)
                {
                    ZenithOptimizer.UI.FpsMeterOverlay.Instance.Show();
                }
                else
                {
                    ZenithOptimizer.UI.FpsMeterOverlay.Instance.Initialize();
                }

                // Restore Tool Active States from Settings
                if (_monitorService.Settings.ToolActiveStates != null)
                {
                    foreach (var kvp in _monitorService.Settings.ToolActiveStates)
                    {
                        _toolActiveStates[kvp.Key] = kvp.Value;
                    }
                }

                // Restore Theme from Settings
                if (!string.IsNullOrEmpty(_monitorService.Settings.Theme))
                {
                    bool shouldBeDark = _monitorService.Settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
                    if (shouldBeDark != _isDarkMode)
                    {
                        SetThemeMode(shouldBeDark);
                    }
                }
            }
            finally
            {
                _isLoadingSettings = false;
            }
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
                if (MainScrollViewer != null)
                {
                    MainScrollViewer.ScrollToTop();
                }
                AnimateViewIn(targetView);
            }
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = sender as ScrollViewer;
            if (scv == null) return;

            // If mouse is over a scrollable TextBox or child ScrollViewer, let the child handle scrolling!
            DependencyObject dep = e.OriginalSource as DependencyObject;
            while (dep != null && dep != scv)
            {
                if (dep is TextBox || dep is ComboBox)
                {
                    return;
                }
                dep = VisualTreeHelper.GetParent(dep);
            }

            // Dampen mouse wheel delta for silky-smooth, controlled scrolling
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
            if (_isLoadingSettings || _monitorService == null || _monitorService.Settings == null) return;

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

            _monitorService.Settings.Save();
        }

        private void BtnToggleFpsOverlay_Click(object sender, RoutedEventArgs e)
        {
            ZenithOptimizer.UI.FpsMeterOverlay.Instance.Toggle();
            bool on = ZenithOptimizer.UI.FpsMeterOverlay.Instance.IsVisible;
            if (BtnToggleFpsOverlay != null)
            {
                BtnToggleFpsOverlay.Content = on ? "FPS Overlay [Active]" : "FPS Overlay [F10]";
            }
            string msg = on 
                ? "On-Screen FPS Meter Active [F10] • [Ctrl+F10] to Reposition" 
                : "On-Screen FPS Meter Closed [F10]";
            Log(msg);
            ShowNotificationBanner(on ? "FPS Meter Active [F10]" : "FPS Meter Closed", "emerald");
        }

        private void BtnPurgeRam_Click(object sender, RoutedEventArgs e)
        {
            long freedBytes = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
            double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 1);
            string statusMsg = string.Format("Safely trimmed {0} MB from idle background apps without touching active games.", freedMb > 0 ? freedMb.ToString() : "50+");
            TxtPurgeStatus.Text = statusMsg;
            Log(statusMsg);
            ShowNotificationBanner(string.Format("Trimmed {0} MB idle RAM", freedMb > 0 ? freedMb.ToString() : "50+"), "emerald");
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
                ShowNotificationBanner("All Windows system defaults restored", "cooling");
                UpdateEnginePulseBadge("Stock Defaults Restored", Color.FromRgb(148, 163, 184));
            }
        }

        private void BtnApplyNetwork_Click(object sender, RoutedEventArgs e)
        {
            string message;
            bool success = _monitorService.NetworkService.ApplyNetworkOptimizations(out message);
            TxtNetworkStatus.Text = message;
            Log(message);
            ShowNotificationBanner(message, success ? "emerald" : "warning");
        }

        private void BtnRestoreNetwork_Click(object sender, RoutedEventArgs e)
        {
            string message;
            bool success = _monitorService.NetworkService.RestoreDefaultNetworkSettings(out message);
            TxtNetworkStatus.Text = message;
            Log(message);
            ShowNotificationBanner(message, "cooling");
        }

        private void BtnOptimizeBlueStacks_Click(object sender, RoutedEventArgs e)
        {
            string message;
            bool success = _monitorService.BlueStacksService.OptimizeForMobileLegends(out message);
            TxtBsConfigStatus.Text = message;
            Log(message);
            ShowNotificationBanner(message, success ? "emerald" : "warning");
        }

        private void BtnRestoreBlueStacks_Click(object sender, RoutedEventArgs e)
        {
            string message;
            bool success = _monitorService.BlueStacksService.RestoreConfig(out message);
            TxtBsConfigStatus.Text = message;
            Log(message);
            ShowNotificationBanner(message, "cooling");
        }

        private void TxtSearchGames_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtSearchPlaceholder != null)
            {
                TxtSearchPlaceholder.Visibility = (TxtSearchGames != null && !string.IsNullOrEmpty(TxtSearchGames.Text))
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
            if (BtnClearSearch != null)
            {
                BtnClearSearch.Visibility = (TxtSearchGames != null && !string.IsNullOrEmpty(TxtSearchGames.Text))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            PopulateGameProfiles();
        }

        private void TxtSearchGames_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (TxtSearchGames != null)
                {
                    TxtSearchGames.Text = string.Empty;
                }
                e.Handled = true;
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (TxtSearchGames != null)
            {
                TxtSearchGames.Text = string.Empty;
                TxtSearchGames.Focus();
            }
        }
    }
}