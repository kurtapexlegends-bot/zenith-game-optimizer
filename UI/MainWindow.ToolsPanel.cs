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
        private static TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock()
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = FrozenTheme.TextMuted,
                Margin = new Thickness(0, 10, 0, 6),
                Tag = "SectionHeader"
            };
        }

        private System.Windows.Controls.Button CreateToolButton(
            string label, 
            Action onClick, 
            bool isAccent = false, 
            string tooltip = null, 
            Func<bool> isInitiallyActive = null,
            Action onDeactivate = null)
        {
            var btn = new System.Windows.Controls.Button()
            {
                Content = label,
                Style = (Style)FindResource(isAccent ? "AccentBtn" : "SecondaryBtn"),
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1.0, 1.0)
            };

            if (!string.IsNullOrEmpty(tooltip))
            {
                btn.ToolTip = tooltip;
            }

            bool isActive = false;

            Action applyInactiveVisuals = () =>
            {
                btn.Content = label;
                btn.Style = (Style)FindResource(isAccent ? "AccentBtn" : "SecondaryBtn");
                btn.ClearValue(System.Windows.Controls.Button.BackgroundProperty);
                btn.ClearValue(System.Windows.Controls.Button.ForegroundProperty);
                btn.ClearValue(System.Windows.Controls.Button.BorderBrushProperty);
                btn.FontWeight = FontWeights.Medium;
                isActive = false;
                _toolActiveStates[label] = false;
                SaveToolStates();
                UpdateLatencyReductionBadge();
            };

            Action applyActiveVisuals = () =>
            {
                btn.Content = "✓ " + label;
                btn.Background = FrozenTheme.ActiveBtnBg;
                btn.BorderBrush = (SolidColorBrush)FindResource("ToggleActiveGreen");
                btn.BorderThickness = new Thickness(1);
                btn.Foreground = FrozenTheme.ActiveBtnFg;
                btn.FontWeight = FontWeights.SemiBold;
                isActive = true;
                _toolActiveStates[label] = true;
                SaveToolStates();
                UpdateLatencyReductionBadge();
            };

            try
            {
                if (_toolActiveStates.ContainsKey(label) && _toolActiveStates[label])
                {
                    applyActiveVisuals();
                }
                else if (isInitiallyActive != null && isInitiallyActive())
                {
                    applyActiveVisuals();
                }
            }
            catch { }

            btn.Click += (s, e) =>
            {
                try
                {
                    if (onDeactivate != null && isActive)
                    {
                        onDeactivate();
                        applyInactiveVisuals();
                        ShowNotificationBanner(string.Format("{0}: Deactivated", label), "warning");
                        return;
                    }

                    if (onClick != null)
                    {
                        onClick();
                    }

                    if (onDeactivate != null)
                    {
                        applyActiveVisuals();
                        ShowNotificationBanner(string.Format("{0}: Activated", label), "emerald");
                    }
                    else
                    {
                        bool isOneShot = label.Contains("Trim") || label.Contains("Shrink") || label.Contains("Scan") || label.Contains("Verify") || label.Contains("Restore");
                        if (!isOneShot)
                        {
                            applyActiveVisuals();
                            ShowNotificationBanner(string.Format("{0}: Activated", label), "emerald");
                        }
                        else
                        {
                            ShowNotificationBanner(string.Format("{0}: Applied", label), "emerald");
                        }

                        // Action executed feedback pulse
                        var scaleAnim = new DoubleAnimation(1.0, 1.05, new Duration(TimeSpan.FromMilliseconds(100)))
                        {
                            AutoReverse = true,
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var tr = btn.RenderTransform as ScaleTransform;
                        if (tr == null || tr.IsFrozen)
                        {
                            btn.RenderTransform = new ScaleTransform(1.0, 1.0);
                            tr = (ScaleTransform)btn.RenderTransform;
                        }
                        tr.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                        tr.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                    }
                }
                catch (Exception ex)
                {
                    ShowNotificationBanner("Error: " + ex.Message, "danger");
                }
            };

            return btn;
        }

        private void SaveToolStates()
        {
            try
            {
                if (_monitorService != null && _monitorService.Settings != null)
                {
                    _monitorService.Settings.ToolActiveStates = new Dictionary<string, bool>(_toolActiveStates);
                    _monitorService.Settings.Save();
                }
            }
            catch { }
        }
    }
}