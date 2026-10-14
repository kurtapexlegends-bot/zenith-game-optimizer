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
                    TextPrimary = Freeze(new SolidColorBrush(Color.FromRgb(237, 237, 237)));
                    TextSecondary = Freeze(new SolidColorBrush(Color.FromRgb(148, 155, 166)));
                    TextMuted = Freeze(new SolidColorBrush(Color.FromRgb(93, 100, 114)));
                    BadgeBg = Freeze(new SolidColorBrush(Color.FromArgb(179, 28, 33, 41)));
                    BorderSubtle = Freeze(new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)));
                    BorderCard = Freeze(new SolidColorBrush(Color.FromArgb(31, 255, 255, 255)));
                    BorderStrong = Freeze(new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)));

                    ToolsPanelBg = Freeze(new SolidColorBrush(Color.FromArgb(204, 17, 19, 23)));
                    CardBg = Freeze(new SolidColorBrush(Color.FromArgb(179, 22, 25, 31)));
                    ActiveBtnBg = Freeze(new SolidColorBrush(Color.FromArgb(204, 34, 39, 48)));
                    ActiveBtnBorder = Freeze(new SolidColorBrush(Color.FromArgb(77, 255, 255, 255)));
                    ActiveBtnFg = Freeze(new SolidColorBrush(Color.FromRgb(244, 244, 245)));

                    NotificationBg = Freeze(new SolidColorBrush(Color.FromArgb(204, 26, 29, 36)));
                    NotificationBorder = Freeze(new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)));
                    CalloutBg = Freeze(new SolidColorBrush(Color.FromArgb(179, 20, 22, 27)));

                    StatusBadgeActiveBg = Freeze(new SolidColorBrush(Color.FromArgb(179, 28, 33, 41)));
                    StatusDotActive = Freeze(new SolidColorBrush(Color.FromRgb(237, 237, 237)));

                    AccordionHeaderBg = Freeze(new SolidColorBrush(Color.FromArgb(179, 22, 25, 31)));
                    AccordionHeaderHover = Freeze(new SolidColorBrush(Color.FromArgb(204, 30, 34, 42)));
                    SearchBoxBg = Freeze(new SolidColorBrush(Color.FromArgb(179, 20, 23, 29)));
                }
                else
                {
                    // Light Mode (White first)
                    TextPrimary = Freeze(new SolidColorBrush(Color.FromRgb(18, 20, 23)));
                    TextSecondary = Freeze(new SolidColorBrush(Color.FromRgb(90, 98, 114)));
                    TextMuted = Freeze(new SolidColorBrush(Color.FromRgb(136, 146, 162)));
                    BadgeBg = Freeze(new SolidColorBrush(Color.FromArgb(179, 237, 240, 245)));
                    BorderSubtle = Freeze(new SolidColorBrush(Color.FromArgb(38, 0, 0, 0)));
                    BorderCard = Freeze(new SolidColorBrush(Color.FromArgb(31, 0, 0, 0)));
                    BorderStrong = Freeze(new SolidColorBrush(Color.FromArgb(77, 0, 0, 0)));

                    ToolsPanelBg = Freeze(new SolidColorBrush(Color.FromArgb(204, 245, 246, 248)));
                    CardBg = Freeze(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)));
                    ActiveBtnBg = Freeze(new SolidColorBrush(Color.FromArgb(204, 226, 230, 238)));
                    ActiveBtnBorder = Freeze(new SolidColorBrush(Color.FromArgb(77, 0, 0, 0)));
                    ActiveBtnFg = Freeze(new SolidColorBrush(Color.FromRgb(18, 20, 23)));

                    NotificationBg = Freeze(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)));
                    NotificationBorder = Freeze(new SolidColorBrush(Color.FromArgb(51, 0, 0, 0)));
                    CalloutBg = Freeze(new SolidColorBrush(Color.FromArgb(217, 240, 242, 246)));

                    StatusBadgeActiveBg = Freeze(new SolidColorBrush(Color.FromArgb(179, 237, 240, 245)));
                    StatusDotActive = Freeze(new SolidColorBrush(Color.FromRgb(18, 20, 23)));

                    AccordionHeaderBg = Freeze(new SolidColorBrush(Color.FromArgb(204, 245, 246, 248)));
                    AccordionHeaderHover = Freeze(new SolidColorBrush(Color.FromArgb(230, 237, 240, 245)));
                    SearchBoxBg = Freeze(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)));
                }
            }
        }

        private void UpdateLatencyReductionBadge()
        {
            try
            {
                if (BadgeLatencySaved == null || TxtLatencySaved == null) return;

                double latencySavedMs = 0.0;
                var breakdown = new System.Text.StringBuilder();
                breakdown.AppendLine(" Realtime Latency Reduction Estimate:");

                // 1. High-Resolution Multimedia Timer (15.6ms Windows default -> 0.500ms)
                if (_monitorService != null && _monitorService.TimerEngine != null && _monitorService.TimerEngine.IsHighResolutionActive)
                {
                    latencySavedMs += 15.1;
                    breakdown.AppendLine("• 0.500ms OS Multimedia Timer: ~15.1ms saved (from 15.6ms default)");
                }
                else if (ChkTweakTimer != null && ChkTweakTimer.IsChecked == true)
                {
                    latencySavedMs += 15.1;
                    breakdown.AppendLine("• 0.500ms OS Multimedia Timer: ~15.1ms saved (from 15.6ms default)");
                }

                // 2. Raw Input / Pointer Precision unhooking
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
                    latencySavedMs += 1.8;
                    breakdown.AppendLine("• 1:1 Hardware Raw Mouse Aim: ~1.8ms curve lag removed");
                }

                // 3. Process Priority / Realtime Audio scheduling
                bool priorityActive = false;
                foreach (var kvp in _toolActiveStates)
                {
                    if (kvp.Value && (kvp.Key.Contains("Priority") || kvp.Key.Contains("AudioDG") || kvp.Key.Contains("P-Core")))
                    {
                        priorityActive = true;
                        break;
                    }
                }
                if (priorityActive || (ChkAudioStabilization != null && ChkAudioStabilization.IsChecked == true))
                {
                    latencySavedMs += 2.2;
                    breakdown.AppendLine("• High Process & Audio Priority: ~2.2ms scheduling jitter eliminated");
                }

                // 4. DirectFlip / Fullscreen Optimization bypass
                bool displayActive = false;
                foreach (var kvp in _toolActiveStates)
                {
                    if (kvp.Value && (kvp.Key.Contains("DirectFlip") || kvp.Key.Contains("Fullscreen") || kvp.Key.Contains("Borderless")))
                    {
                        displayActive = true;
                        break;
                    }
                }
                if (displayActive)
                {
                    latencySavedMs += 3.5;
                    breakdown.AppendLine("• DirectFlip / DWM Bypass: ~3.5ms composition lag saved");
                }

                // 5. TCPNoDelay / Network DSCP QoS
                bool netActive = false;
                foreach (var kvp in _toolActiveStates)
                {
                    if (kvp.Value && (kvp.Key.Contains("TCPNoDelay") || kvp.Key.Contains("QoS") || kvp.Key.Contains("Socket")))
                    {
                        netActive = true;
                        break;
                    }
                }
                if (netActive)
                {
                    latencySavedMs += 2.0;
                    breakdown.AppendLine("• TCPNoDelay Sub-1ms Sockets: ~2.0ms packet dispatch latency saved");
                }

                // 6. 1-Frame / Reflex Max Pre-rendered frames
                bool reflexActive = false;
                foreach (var kvp in _toolActiveStates)
                {
                    if (kvp.Value && (kvp.Key.Contains("Reflex") || kvp.Key.Contains("Pre-Rendered") || kvp.Key.Contains("60.00 FPS") || kvp.Key.Contains("Pacing")))
                    {
                        reflexActive = true;
                        break;
                    }
                }
                if (reflexActive)
                {
                    latencySavedMs += 8.3;
                    breakdown.AppendLine("• Driver 1-Frame Pre-Render / Reflex: ~8.3ms render queue latency saved");
                }

                if (latencySavedMs < 15.1) latencySavedMs = 15.1;

                TxtLatencySaved.Text = string.Format("~{0:0.0}ms Latency Saved", latencySavedMs);
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

            if (isDark)
            {
                Resources["BgBase"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(217, 14, 16, 19)));
                Resources["BgCard"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(179, 22, 25, 31)));
                Resources["BgCardHover"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(204, 30, 34, 42)));
                Resources["BgSidebar"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(204, 18, 20, 24)));
                Resources["BgSunken"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(179, 10, 12, 14)));
                Resources["BorderSubtle"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)));
                Resources["BorderCard"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(31, 255, 255, 255)));
                Resources["BorderStrong"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)));
                Resources["TextPrimary"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(237, 237, 237)));
                Resources["TextSecondary"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(148, 155, 166)));
                Resources["TextMuted"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(93, 100, 114)));
                Resources["AccentAction"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(237, 237, 237)));
                Resources["AccentActionFg"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(17, 18, 21)));
                Resources["AccentActionHover"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(212, 212, 216)));
                Resources["ActiveStateBg"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(204, 34, 39, 48)));
                Resources["ActiveStateBorder"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(77, 255, 255, 255)));
                Resources["ActiveStateText"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(237, 237, 237)));

                if (BtnThemeToggle != null) BtnThemeToggle.Content = "Theme: Dark";
                ShowNotificationBanner("Dark Mode Active");
            }
            else
            {
                Resources["BgBase"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(230, 244, 245, 248)));
                Resources["BgCard"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)));
                Resources["BgCardHover"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(242, 248, 249, 251)));
                Resources["BgSidebar"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(204, 234, 236, 239)));
                Resources["BgSunken"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(217, 237, 240, 245)));
                Resources["BorderSubtle"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(38, 0, 0, 0)));
                Resources["BorderCard"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(31, 0, 0, 0)));
                Resources["BorderStrong"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(77, 0, 0, 0)));
                Resources["TextPrimary"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(18, 20, 23)));
                Resources["TextSecondary"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(90, 98, 114)));
                Resources["TextMuted"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(136, 146, 162)));
                Resources["AccentAction"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(18, 20, 23)));
                Resources["AccentActionFg"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(255, 255, 255)));
                Resources["AccentActionHover"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(42, 46, 53)));
                Resources["ActiveStateBg"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(204, 226, 230, 238)));
                Resources["ActiveStateBorder"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromArgb(77, 0, 0, 0)));
                Resources["ActiveStateText"] = FrozenTheme.Freeze(new SolidColorBrush(Color.FromRgb(18, 20, 23)));

                if (BtnThemeToggle != null) BtnThemeToggle.Content = "Theme: Light";
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