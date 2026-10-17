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
        private System.Windows.Threading.DispatcherTimer _toastTimer;

        public void ShowNotificationBanner(string message, string type = "info")
        {
            try
            {
                TxtFooterStatus.Text = message;
                ToastText.Text = message;

                ToastNotification.Background = FrozenTheme.NotificationBg;
                ToastNotification.BorderBrush = FrozenTheme.NotificationBorder;
                ToastText.Foreground = FrozenTheme.TextPrimary;

                if (ToastDot != null)
                {
                    if (type == "boost" || type == "emerald" || type == "success")
                    {
                        ToastDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    }
                    else if (type == "cooling" || type == "cyan")
                    {
                        ToastDot.Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                    }
                    else if (type == "warning" || type == "amber")
                    {
                        ToastDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    }
                    else if (type == "danger" || type == "error")
                    {
                        ToastDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    }
                    else
                    {
                        ToastDot.Fill = FrozenTheme.StatusDotActive;
                    }
                }

                ToastNotification.Visibility = Visibility.Visible;

                var animFade = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(180)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var animSlide = new DoubleAnimation(12.0, 0.0, new Duration(TimeSpan.FromMilliseconds(180)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                ToastNotification.BeginAnimation(UIElement.OpacityProperty, animFade);
                var tt = ToastNotification.RenderTransform as TranslateTransform;
                if (tt != null)
                {
                    tt.BeginAnimation(TranslateTransform.YProperty, animSlide);
                }

                if (_toastTimer == null)
                {
                    _toastTimer = new System.Windows.Threading.DispatcherTimer();
                    _toastTimer.Interval = TimeSpan.FromSeconds(3.0);
                    _toastTimer.Tick += (s, e) =>
                    {
                        var closeFade = new DoubleAnimation(ToastNotification.Opacity, 0.0, new Duration(TimeSpan.FromMilliseconds(140)))
                        {
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                        };
                        closeFade.Completed += (cs, ce) => { ToastNotification.Visibility = Visibility.Collapsed; };
                        ToastNotification.BeginAnimation(UIElement.OpacityProperty, closeFade);
                        _toastTimer.Stop();
                    };
                }
                else
                {
                    _toastTimer.Stop();
                }
                _toastTimer.Start();
            }
            catch { }
        }

        public void UpdateEnginePulseBadge(string statusText, Color dotColor)
        {
            try
            {
                if (TxtStatusBadge != null)
                {
                    TxtStatusBadge.Text = statusText;
                }
                if (StatusDot != null)
                {
                    StatusDot.Fill = new SolidColorBrush(dotColor);
                }
                if (StatusBadgeBorder != null)
                {
                    var fadeAnim = new DoubleAnimation(0.4, 1.0, new Duration(TimeSpan.FromMilliseconds(180)))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    StatusBadgeBorder.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                }
            }
            catch { }
        }

        private void OnTelemetryUpdated(SystemTelemetry telem)
        {
            try
            {
                if (Dispatcher == null || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
                    return;

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    try
                    {
                        if (_notifyIcon != null)
                        {
                            try
                            {
                                _notifyIcon.Text = telem.IsGameRunning 
                                    ? ("Zenith: " + telem.ActiveGameName) 
                                    : "Zenith Game Optimizer (Active)";
                            }
                            catch { }
                        }

                        if (this.Visibility != Visibility.Visible || this.WindowState == WindowState.Minimized)
                        {
                            return;
                        }

                        TxtTimerRes.Text = string.Format("{0:0.000} ms", telem.CurrentTimerResolutionMs);
                        TxtCpuLoad.Text = string.Format("{0:0.0} %", telem.CpuUsagePercent);
                        TxtCpuCores.Text = string.Format("{0} Cores - Ready", telem.PhysicalCoreCount);
                        TxtRamLoad.Text = string.Format("{0:0} %", telem.RamUsagePercent);
                        TxtRamDetails.Text = string.Format("{0:0.0} of {1:0.0} GB Used", telem.UsedRamGb, telem.TotalRamGb);
                        TxtPowerPlan.Text = telem.ActivePowerPlanName;
                        if (TxtThermalState != null)
                        {
                            TxtThermalState.Text = _monitorService.PowerService.IsInstantCooldownActive() 
                                ? "Cooldown (80%)" 
                                : (_monitorService.PowerService.IsLaptopThermalGuardActive() ? "Thermal Guard (99%)" : "Standard");
                        }

                        TxtDetectedCpu.Text = telem.CpuName;
                        TxtDetectedGpu.Text = telem.GpuName;
                        TxtGameModeState.Text = telem.IsGameModeActive ? "Active" : "Standard";
                        TxtGameDvrState.Text = telem.IsGameDvrDisabled ? "Disabled (Fast)" : "Active";

                        if (telem.IsGameRunning)
                        {
                            bool isPreArmed = telem.ActiveGameName.IndexOf("(Pre-Armed)", StringComparison.OrdinalIgnoreCase) >= 0;
                            DashHeroTitle.Text = isPreArmed ? telem.ActiveGameName : telem.ActiveGameName + " (Active & Optimized)";
                            DashHeroDesc.Text = isPreArmed
                                ? "Pre-armed mode active: 0.500ms kernel timer, high performance power scheme, and audio shield engaged."
                                : "Game mode engaged: 0.500ms ultra-low input response, audio shield, and priority scheduling are active.";
                            StatusText.Text = isPreArmed ? "Pre-Armed: " + telem.ActiveGameName.Replace(" (Pre-Armed)", "") : "Game Active: " + telem.ActiveGameName;
                            StatusBadgeBorder.Background = FrozenTheme.StatusBadgeActiveBg;
                            StatusDot.Fill = FrozenTheme.StatusDotActive;
                            TxtFooterStatus.Text = "Zenith Active: " + telem.ActiveGameName + " | Input Response: " + telem.CurrentTimerResolutionMs.ToString("0.000") + " ms";
                        }
                        else
                        {
                            if (BtnManualBoost.Content != null && BtnManualBoost.Content.ToString() != "Boosted (Active)")
                            {
                                DashHeroTitle.Text = "System Monitoring Active";
                                DashHeroDesc.Text = "Whenever a supported game starts, Zenith automatically engages 0.500ms precision scheduling, AudioDG isolation, and unparked CPU cores.";
                            }
                            StatusText.Text = "Zenith Active - Auto-Optimizing";
                            StatusBadgeBorder.Background = FrozenTheme.StatusBadgeActiveBg;
                            StatusDot.Fill = FrozenTheme.StatusDotActive;
                            TxtFooterStatus.Text = "Zenith Engine Ready. 0 games running. Input Response: " + telem.CurrentTimerResolutionMs.ToString("0.000") + " ms";
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            string errPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZenithOptimizer", "error.log");
                            System.IO.File.AppendAllText(errPath, "[TelemetryUI] " + ex.ToString() + "\r\n");
                        }
                        catch { }
                    }
                }));
            }
            catch (Exception ex)
            {
                try
                {
                    string errPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZenithOptimizer", "error.log");
                    System.IO.File.AppendAllText(errPath, "[TelemetryInvoke] " + ex.ToString() + "\r\n");
                }
                catch { }
            }
        }

        private void OnGameStarted(GameProfile profile)
        {
            Dispatcher.Invoke(new Action(delegate
            {
                Log(string.Format(">>> Game Detected: {0}. Engaged 0.500ms timer, audio shield, and low-latency priority.", profile.Name));
                PopulateGameProfiles();
            }));
        }

        private void OnGameExited(GameProfile profile)
        {
            Dispatcher.Invoke(new Action(delegate
            {
                Log(string.Format("<<< Game Closed: {0}. Cleanly restored standard power plan and priority states.", profile.Name));
                PopulateGameProfiles();
            }));
        }

        private void OnLogMessage(string msg)
        {
            Dispatcher.Invoke(new Action(delegate
            {
                Log(msg);
            }));
        }

        private void Log(string message)
        {
            try
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    string time = DateTime.Now.ToString("HH:mm:ss");
                    TxtEventLog.AppendText(string.Format("[{0}] {1}\r\n", time, message));
                    TxtEventLog.ScrollToEnd();
                }));
            }
            catch { }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtEventLog.Clear();
        }
    }
}