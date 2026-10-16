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
        private static class FrozenTheme
        {
            public static SolidColorBrush Freeze(SolidColorBrush b) { if (b.CanFreeze) b.Freeze(); return b; }

            public static SolidColorBrush TextPrimary { get; private set; }
            public static SolidColorBrush TextSecondary { get; private set; }
            public static SolidColorBrush TextMuted { get; private set; }
            public static SolidColorBrush BadgeBg { get; private set; }
            public static SolidColorBrush BorderSubtle { get; private set; }
            public static SolidColorBrush BorderCard { get; private set; }
            public static SolidColorBrush BorderStrong { get; private set; }

            public static SolidColorBrush ToolsPanelBg { get; private set; }
            public static SolidColorBrush CardBg { get; private set; }
            public static SolidColorBrush ActiveBtnBg { get; private set; }
            public static SolidColorBrush ActiveBtnBorder { get; private set; }
            public static SolidColorBrush ActiveBtnFg { get; private set; }

            public static SolidColorBrush NotificationBg { get; private set; }
            public static SolidColorBrush NotificationBorder { get; private set; }
            public static SolidColorBrush CalloutBg { get; private set; }

            public static SolidColorBrush StatusBadgeActiveBg { get; private set; }
            public static SolidColorBrush StatusDotActive { get; private set; }

            public static SolidColorBrush AccordionHeaderBg { get; private set; }
            public static SolidColorBrush AccordionHeaderHover { get; private set; }
            public static SolidColorBrush SearchBoxBg { get; private set; }

            static FrozenTheme()
            {
                SetMode(false); // Default Light Mode (White First)
            }

            public static void SetMode(bool isDark)
            {
                if (isDark)
                {
                    TextPrimary = Freeze(new SolidColorBrush(Color.FromRgb(241, 245, 249)));
                    TextSecondary = Freeze(new SolidColorBrush(Color.FromRgb(148, 163, 184)));
                    TextMuted = Freeze(new SolidColorBrush(Color.FromRgb(100, 116, 139)));
                    BadgeBg = Freeze(new SolidColorBrush(Color.FromArgb(140, 24, 30, 42)));
                    BorderSubtle = Freeze(new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)));
                    BorderCard = Freeze(new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)));
                    BorderStrong = Freeze(new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)));

                    ToolsPanelBg = Freeze(new SolidColorBrush(Color.FromArgb(160, 14, 18, 26)));
                    CardBg = Freeze(new SolidColorBrush(Color.FromArgb(170, 20, 26, 36)));
                    ActiveBtnBg = Freeze(new SolidColorBrush(Color.FromArgb(180, 32, 40, 54)));
                    ActiveBtnBorder = Freeze(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)));
                    ActiveBtnFg = Freeze(new SolidColorBrush(Color.FromRgb(248, 250, 252)));

                    NotificationBg = Freeze(new SolidColorBrush(Color.FromArgb(220, 20, 26, 36)));
                    NotificationBorder = Freeze(new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)));
                    CalloutBg = Freeze(new SolidColorBrush(Color.FromArgb(160, 18, 23, 32)));

                    StatusBadgeActiveBg = Freeze(new SolidColorBrush(Color.FromArgb(140, 24, 30, 42)));
                    StatusDotActive = Freeze(new SolidColorBrush(Color.FromRgb(16, 185, 129))); // Crisp Emerald

                    AccordionHeaderBg = Freeze(new SolidColorBrush(Color.FromArgb(150, 20, 26, 36)));
                    AccordionHeaderHover = Freeze(new SolidColorBrush(Color.FromArgb(180, 30, 38, 50)));
                    SearchBoxBg = Freeze(new SolidColorBrush(Color.FromArgb(160, 18, 24, 34)));
                }
                else
                {
                    // Light Mode (Crisp White First - Pure Acrylic Glass)
                    TextPrimary = Freeze(new SolidColorBrush(Color.FromRgb(15, 23, 42)));
                    TextSecondary = Freeze(new SolidColorBrush(Color.FromRgb(71, 85, 105)));
                    TextMuted = Freeze(new SolidColorBrush(Color.FromRgb(148, 163, 184)));
                    BadgeBg = Freeze(new SolidColorBrush(Color.FromArgb(140, 226, 232, 240)));
                    BorderSubtle = Freeze(new SolidColorBrush(Color.FromArgb(16, 0, 0, 0)));
                    BorderCard = Freeze(new SolidColorBrush(Color.FromArgb(24, 0, 0, 0)));
                    BorderStrong = Freeze(new SolidColorBrush(Color.FromArgb(48, 0, 0, 0)));

                    ToolsPanelBg = Freeze(new SolidColorBrush(Color.FromArgb(180, 248, 250, 252)));
                    CardBg = Freeze(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)));
                    ActiveBtnBg = Freeze(new SolidColorBrush(Color.FromArgb(180, 226, 232, 240)));
                    ActiveBtnBorder = Freeze(new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)));
                    ActiveBtnFg = Freeze(new SolidColorBrush(Color.FromRgb(15, 23, 42)));

                    NotificationBg = Freeze(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)));
                    NotificationBorder = Freeze(new SolidColorBrush(Color.FromArgb(36, 0, 0, 0)));
                    CalloutBg = Freeze(new SolidColorBrush(Color.FromArgb(180, 241, 245, 249)));

                    StatusBadgeActiveBg = Freeze(new SolidColorBrush(Color.FromArgb(140, 226, 232, 240)));
                    StatusDotActive = Freeze(new SolidColorBrush(Color.FromRgb(16, 185, 129))); // Crisp Emerald

                    AccordionHeaderBg = Freeze(new SolidColorBrush(Color.FromArgb(170, 241, 245, 249)));
                    AccordionHeaderHover = Freeze(new SolidColorBrush(Color.FromArgb(195, 226, 232, 240)));
                    SearchBoxBg = Freeze(new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)));
                }
            }
        }

        private void UpdateLatencyReductionBadge()
        {
            try
            {
                if (BadgeLatencySaved == null || TxtLatencySaved == null) return;

                int activeSubsystems = 0;
                var breakdown = new System.Text.StringBuilder();
                breakdown.AppendLine("Active Kernel & Hardware Subsystems:");

                double currentTimerMs = _monitorService != null && _monitorService.TimerEngine != null 
                    ? _monitorService.TimerEngine.GetCurrentResolutionMs() 
                    : 1.0;
                bool timerActive = (_monitorService != null && _monitorService.TimerEngine != null && _monitorService.TimerEngine.IsHighResolutionActive)
                                   || (ChkTweakTimer != null && ChkTweakTimer.IsChecked == true);
                if (timerActive)
                {
                    activeSubsystems++;
                    breakdown.AppendLine(string.Format("- High-Precision Multimedia Timer: ACTIVE ({0:0.000}ms target resolution)", currentTimerMs));
                }
                else
                {
                    breakdown.AppendLine(string.Format("- Standard Windows Timer: {0:0.000}ms (Default)", currentTimerMs));
                }

                bool directFlipActive = false;
                foreach (var kvp in _toolActiveStates)
                {
                    if (kvp.Value && (kvp.Key.Contains("DirectFlip") || kvp.Key.Contains("Fullscreen") || kvp.Key.Contains("Borderless")))
                    {
                        directFlipActive = true;
                        break;
                    }
                }
                if (directFlipActive)
                {
                    activeSubsystems++;
                    breakdown.AppendLine("- DWM DirectFlip Presentation: ENGAGED (Zero composition latency)");
                }

                bool audioShieldActive = (ChkAudioStabilization != null && ChkAudioStabilization.IsChecked == true);
                if (audioShieldActive)
                {
                    activeSubsystems++;
                    breakdown.AppendLine("- AudioDG Multimedia Shield: ENGAGED (Isolated to Core 1 / High priority)");
                }

                bool powerPlanActive = (ChkPowerPlan != null && ChkPowerPlan.IsChecked == true);
                if (powerPlanActive)
                {
                    activeSubsystems++;
                    breakdown.AppendLine("- CPU Core Unparking & High Performance: ACTIVE (100% Core Frequency)");
                }

                bool rawMouseActive = false;
                foreach (var kvp in _toolActiveStates)
                {
                    if (kvp.Value && (kvp.Key.Contains("Mouse") || kvp.Key.Contains("Pointer") || kvp.Key.Contains("Raw")))
                    {
                        rawMouseActive = true;
                        break;
                    }
                }
                if (rawMouseActive)
                {
                    activeSubsystems++;
                    breakdown.AppendLine("- Hardware 1:1 Raw Mouse Aim: ACTIVE (Dynamic acceleration curves disabled)");
                }

                bool netActive = (_monitorService != null && _monitorService.NetworkService != null && _monitorService.NetworkService.IsOptimized());
                if (netActive)
                {
                    activeSubsystems++;
                    breakdown.AppendLine("- Network QoS & TCPNoDelay: ACTIVE (DSCP prioritized)");
                }

                if (activeSubsystems > 0)
                {
                    TxtLatencySaved.Text = timerActive 
                        ? string.Format("{0:0.000}ms Timer - {1} Subsystems Tuned", currentTimerMs, activeSubsystems)
                        : string.Format("{0} Subsystems Tuned", activeSubsystems);
                }
                else
                {
                    TxtLatencySaved.Text = "Standard Kernel State";
                }

                BadgeLatencySaved.ToolTip = breakdown.ToString().TrimEnd();
            }
            catch { }
        }

        private void ThemeAccent_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && border.Tag != null)
            {
                SwitchThemeAccent(border.Tag.ToString());
            }
        }

        public void SwitchThemeAccent(string themeName)
        {
            SetThemeMode(themeName.Equals("dark", StringComparison.OrdinalIgnoreCase));
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            SetThemeMode(!_isDarkMode);
        }

        public void SetThemeMode(bool isDark)
        {
            _isDarkMode = isDark;
            FrozenTheme.SetMode(isDark);
            if (_monitorService != null && _monitorService.Settings != null)
            {
                _monitorService.Settings.Theme = isDark ? "Dark" : "Light";
                _monitorService.Settings.Save();
            }

            if (isDark)
            {
                Resources["BgBase"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(250, 11, 14, 20)));
                Resources["BgCard"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(170, 19, 23, 34)));
                Resources["BgCardHover"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(200, 26, 32, 46)));
                Resources["BgSidebar"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(245, 14, 18, 26)));
                Resources["BgSunken"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(100, 26, 32, 46)));
                Resources["BorderSubtle"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)));
                Resources["BorderCard"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)));
                Resources["BorderStrong"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)));
                Resources["TextPrimary"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(241, 245, 249)));
                Resources["TextSecondary"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(148, 163, 184)));
                Resources["TextMuted"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(100, 116, 139)));
                Resources["AccentAction"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(241, 245, 249)));
                Resources["AccentActionFg"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(11, 14, 20)));
                Resources["AccentActionHover"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(255, 255, 255)));
                Resources["ActiveStateBg"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(180, 32, 40, 54)));
                Resources["ActiveStateBorder"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)));
                Resources["ActiveStateText"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(248, 250, 252)));

                if (IconSun != null) IconSun.Visibility = Visibility.Collapsed;
                if (IconMoon != null) IconMoon.Visibility = Visibility.Visible;
                if (BtnThemeToggle != null) BtnThemeToggle.ToolTip = "Switch to Light Mode";
                ShowNotificationBanner("Dark Mode Active");
            }
            else
            {
                Resources["BgBase"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(250, 248, 250, 252)));
                Resources["BgCard"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)));
                Resources["BgCardHover"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(240, 255, 255, 255)));
                Resources["BgSidebar"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(245, 241, 245, 249)));
                Resources["BgSunken"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(120, 226, 232, 240)));
                Resources["BorderSubtle"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(16, 0, 0, 0)));
                Resources["BorderCard"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(24, 0, 0, 0)));
                Resources["BorderStrong"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(48, 0, 0, 0)));
                Resources["TextPrimary"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(15, 23, 42)));
                Resources["TextSecondary"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(71, 85, 105)));
                Resources["TextMuted"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(148, 163, 184)));
                Resources["AccentAction"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(15, 23, 42)));
                Resources["AccentActionFg"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(255, 255, 255)));
                Resources["AccentActionHover"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(30, 41, 59)));
                Resources["ActiveStateBg"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(180, 226, 232, 240)));
                Resources["ActiveStateBorder"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)));
                Resources["ActiveStateText"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(15, 23, 42)));

                if (IconSun != null) IconSun.Visibility = Visibility.Visible;
                if (IconMoon != null) IconMoon.Visibility = Visibility.Collapsed;
                if (BtnThemeToggle != null) BtnThemeToggle.ToolTip = "Switch to Dark Mode";
                ShowNotificationBanner("Light Mode Active");
            }

            try
            {
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                UpdateDwmDarkMode(hwnd, isDark);
            }
            catch { }

            PopulateGameProfiles();
        }
    }
}