using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZenithOptimizer.Services;

namespace ZenithOptimizer.UI.Sniper
{
    public static class SniperHudControls
    {
        public static void SetPillActive(Border pill, bool isActive)
        {
            if (pill == null) return;
            pill.Background = isActive ? new SolidColorBrush(Color.FromArgb(180, 51, 65, 85)) : new SolidColorBrush(Color.FromArgb(120, 30, 41, 59));
            pill.BorderThickness = new Thickness(isActive ? 1.5 : 1);
        }

        public static Border CreatePresetPill(string label, Color color, Action onClick)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            Border dot = new Border
            {
                Width = 7, Height = 7, CornerRadius = new CornerRadius(3.5),
                Background = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(dot);
            TextBlock txt = new TextBlock
            {
                Text = label, FontSize = 9, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(txt);

            Border border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(70, color.R, color.G, color.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2.5, 6, 2.5),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Child = sp
            };
            border.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (onClick != null) onClick(); };
            return border;
        }

        public static Border CreatePickerButton(string label, string keyHint, Color accentColor, string tooltip, Action onClick)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock txt = new TextBlock
            {
                Text = label, FontSize = 9, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accentColor),
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(txt);

            if (!string.IsNullOrEmpty(keyHint))
            {
                Border keycap = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(160, 51, 65, 85)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(3.5, 0.5, 3.5, 0.5),
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = keyHint, FontSize = 8, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21)) }
                };
                sp.Children.Add(keycap);
            }

            Border border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, accentColor.R, accentColor.G, accentColor.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(7, 3, 7, 3),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Child = sp
            };
            border.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (onClick != null) onClick(); };
            return border;
        }

        public static Border CreateActionPill(string text, Color color, string tooltip, Action onClick)
        {
            TextBlock tb = new TextBlock
            {
                Text = text, FontSize = 9, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                VerticalAlignment = VerticalAlignment.Center
            };
            Border border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, color.R, color.G, color.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2.5, 6, 2.5),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Child = tb
            };
            border.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (onClick != null) onClick(); };
            return border;
        }

        public static Border CreateKeycapPill(string keyText, string actionText, string tooltip, Action onClick)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            Border keyBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 51, 65, 85)),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(3, 0.5, 3, 0.5),
                Margin = new Thickness(0, 0, 3, 0),
                Child = new TextBlock { Text = keyText, FontSize = 8, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21)) }
            };
            sp.Children.Add(keyBorder);
            sp.Children.Add(new TextBlock { Text = actionText, FontSize = 8.5, Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), VerticalAlignment = VerticalAlignment.Center });

            Border border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1.5, 4, 1.5),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Child = sp
            };
            border.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (onClick != null) onClick(); };
            return border;
        }

        public static StackPanel BuildHotkeysPanel(
            Action onToggleMarkersLock,
            Action onToggleMarkersVisibility,
            Action onToggleCompactMode,
            Action onHide,
            Action<string, Color> flashActionText)
        {
            StackPanel hotkeysPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            hotkeysPanel.Children.Add(CreateKeycapPill("F7", "Arm/Stop", "Toggle auto-sniper Armed vs Standby Stop [F7 or ESC]", () =>
            {
                var s = RobloxBidSniperService.Instance; s.IsArmed = !s.IsArmed;
                flashActionText(s.IsArmed ? "SNIPER ARMED" : "SNIPER STOPPED", s.IsArmed ? Color.FromRgb(52, 211, 153) : Color.FromRgb(239, 68, 68));
            }));
            hotkeysPanel.Children.Add(CreateKeycapPill("F8", "Sensor", "Set sweet-spot optical sensor coordinate to mouse [F8]", () =>
            {
                RobloxBidSniperService.Instance.SetSensorPositionToCursor();
                flashActionText("SENSOR SET", Color.FromRgb(56, 189, 248));
            }));
            hotkeysPanel.Children.Add(CreateKeycapPill("Ctrl+F8", "Set Bid", "Lock BID button click target to cursor [Ctrl+F8]", () =>
            {
                int cx, cy;
                if (RobloxBidSniperService.Instance.SetClickTargetToCurrentCursor(out cx, out cy))
                    flashActionText(string.Format("BID TARGET LOCKED ({0}, {1})", cx, cy), Color.FromRgb(56, 189, 248));
            }));
            hotkeysPanel.Children.Add(CreateKeycapPill("Alt+F8", "Eyedropper", "Sample target RGB color under mouse cursor [Alt+F8]", () =>
            {
                int cr, cg, cb;
                if (RobloxBidSniperService.Instance.SampleTargetColorAtCursor(out cr, out cg, out cb))
                    flashActionText("EYEDROPPER CALIBRATED", Color.FromRgb(244, 114, 182));
            }));
            hotkeysPanel.Children.Add(CreateActionPill("[TEST FIRE]", Color.FromRgb(56, 189, 248), "Dispatch single test snipe click to verify feedback loop", () =>
            {
                var s = RobloxBidSniperService.Instance; s.TriggerTestClick();
                flashActionText("TEST CLICK FIRED", Color.FromRgb(56, 189, 248));
            }));
            hotkeysPanel.Children.Add(CreateActionPill("[TURBO 5x]", Color.FromRgb(250, 204, 21), "Turbo bid click burst [V]", () =>
            {
                RobloxBidSniperService.Instance.TurboBurstClick(5);
                flashActionText("TURBO BURST (5x)", Color.FromRgb(250, 204, 21));
            }));
            hotkeysPanel.Children.Add(CreateActionPill("[5s SNIPE]", Color.FromRgb(250, 204, 21), "Arm 5-second countdown timer snipe", () =>
            {
                RobloxBidSniperService.Instance.TriggerAuctionTimerSnipe(5.0, 4);
                flashActionText("5s TIMER SNIPE ARMED", Color.FromRgb(250, 204, 21));
            }));
            return hotkeysPanel;
        }
    }
}
