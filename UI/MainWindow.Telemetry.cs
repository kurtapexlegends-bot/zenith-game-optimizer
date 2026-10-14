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

        private void ShowNotificationBanner(string message)
        {
            try
            {
                TxtFooterStatus.Text = message;
                ToastText.Text = message;

                ToastNotification.Background = FrozenTheme.NotificationBg;
                ToastNotification.BorderBrush = FrozenTheme.NotificationBorder;
                ToastText.Foreground = FrozenTheme.TextPrimary;

                ToastNotification.Visibility = Visibility.Visible;

                if (_toastTimer == null)
                {
                    _toastTimer = new System.Windows.Threading.DispatcherTimer();
                    _toastTimer.Interval = TimeSpan.FromSeconds(3.5);
                    _toastTimer.Tick += (s, e) =>
                    {
                        ToastNotification.Visibility = Visibility.Collapsed;
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
                                    : "Zenith Game Optimizer (Monitoring)";
                            }
                            catch { }
                        }

                        if (this.Visibility != Visibility.Visible || this.WindowState == WindowState.Minimized)
                        {
                            return;
                        }

                        TxtTimerRes.Text = string.Format("{0:0.000} ms", telem.CurrentTimerResolutionMs);
                        TxtCpuLoad.Text = string.Format("{0:0.0} %", telem.CpuUsagePercent);
                        TxtCpuCores.Text = string.Format("{0} Cores • Ready", telem.PhysicalCoreCount);
                        TxtRamLoad.Text = string.Format("{0:0} %", telem.RamUsagePercent);
                        TxtRamDetails.Text = string.Format("{0:0.0} of {1:0.0} GB Used", telem.UsedRamGb, telem.TotalRamGb);
                        TxtPowerPlan.Text = telem.ActivePowerPlanName;

                        TxtDetectedCpu.Text = telem.CpuName;
                        TxtDetectedGpu.Text = telem.GpuName;
                        TxtGameModeState.Text = telem.IsGameModeActive ? "Active" : "Standard";
                        TxtGameDvrState.Text = telem.IsGameDvrDisabled ? "Disabled (Fast)" : "Active";

                        if (telem.IsGameRunning)
                        {
                            bool isPreArmed = telem.ActiveGameName.IndexOf("(Pre-Armed)", StringComparison.OrdinalIgnoreCase) >= 0;
                            DashHeroTitle.Text = isPreArmed ? telem.ActiveGameName : telem.ActiveGameName + " (Active & Optimized)";
                            DashHeroDesc.Text = isPreArmed
                                ? "Pre-armed mode active: 0.500ms microsecond timer, High Performance power scheme, and audio shield engaged."
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
                                DashHeroTitle.Text = "Monitoring Your Games...";
                                DashHeroDesc.Text = "Whenever you launch a supported game, Zenith automatically eliminates input lag, gives your game top priority, and keeps your PC running at full speed.";
                            }
                            StatusText.Text = "Zenith Active • Auto-Optimizing Games";
                            StatusBadgeBorder.Background = FrozenTheme.StatusBadgeActiveBg;
                            StatusDot.Fill = FrozenTheme.StatusDotActive;
                            TxtFooterStatus.Text = "Zenith Engine Ready. 0 games running. Input Response: " + telem.CurrentTimerResolutionMs.ToString("0.000") + " ms";
                        }
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText(@"C:\Users\acost\.gemini\antigravity\scratch\zenith-game-optimizer\error.log", "[TelemetryUI] " + ex.ToString() + "\r\n");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Users\acost\.gemini\antigravity\scratch\zenith-game-optimizer\error.log", "[TelemetryInvoke] " + ex.ToString() + "\r\n");
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
// REGION: UI Polish: Search, Filter, Accordions, Latency Badge, Theme Accents & Profile Export/Import

    }
}