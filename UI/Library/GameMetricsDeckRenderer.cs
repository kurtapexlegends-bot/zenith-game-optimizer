using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.UI.Library
{
    public static class GameMetricsDeckRenderer
    {
        public static Border CreateMetricsMatrixHeader(Func<string, object> findResource)
        {
            Border header = new Border
            {
                Background = (SolidColorBrush)findResource("BgSunken"),
                BorderBrush = (SolidColorBrush)findResource("BorderSubtle"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 0, 6)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) }); // Title & Icon
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Status
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }); // FPS / Engine
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Kernel Pacing
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Network
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

            Action<string, int> addCol = (label, col) =>
            {
                TextBlock tb = new TextBlock
                {
                    Text = label,
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = (SolidColorBrush)findResource("TextMuted"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(tb, col);
                grid.Children.Add(tb);
            };

            addCol("GAME & TITLE", 0);
            addCol("STATUS", 1);
            addCol("FPS / ENGINE", 2);
            addCol("KERNEL PACING", 3);
            addCol("NETWORK", 4);

            TextBlock actionTb = new TextBlock
            {
                Text = "QUICK ACTIONS",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)findResource("TextMuted"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(actionTb, 5);
            grid.Children.Add(actionTb);

            header.Child = grid;
            return header;
        }

        public static Border CreateGameProfileMetricsRow(
            GameProfile profile,
            Func<string, object> findResource,
            Action<GameProfile> onSpotlight,
            Action<GameProfile> onToggleFavorite,
            Action<GameProfile, Button> onBoost,
            Func<GameProfile, Border> onBuildToolsPanel,
            Action<Border, GameProfile> onAttachDragDrop)
        {
            string monogram;
            Color accentColor;
            GameCardRenderer.GetGameBadgeInfo(profile.Name, out monogram, out accentColor);

            Border row = new Border
            {
                Background = (SolidColorBrush)findResource("BgCard"),
                BorderBrush = new SolidColorBrush(Color.FromArgb(70, accentColor.R, accentColor.G, accentColor.B)),
                BorderThickness = new Thickness(3, 1, 1, 1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand
            };

            row.MouseEnter += (s, e) =>
            {
                row.BorderBrush = new SolidColorBrush(Color.FromArgb(180, accentColor.R, accentColor.G, accentColor.B));
                row.Background = (SolidColorBrush)findResource("BgCardHover");
            };
            row.MouseLeave += (s, e) =>
            {
                row.BorderBrush = new SolidColorBrush(Color.FromArgb(70, accentColor.R, accentColor.G, accentColor.B));
                row.Background = (SolidColorBrush)findResource("BgCard");
            };
            row.MouseLeftButtonUp += (s, e) =>
            {
                if (onSpotlight != null) onSpotlight(profile);
            };

            StackPanel rowContainer = new StackPanel();

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) }); // Title & Icon
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Status
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }); // FPS / Engine
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Kernel Pacing
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Network
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

            // Col 0: Grip + Favorite + Icon + Name
            StackPanel titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border deckGrip = GameCardRenderer.CreateGripHandle(findResource);
            deckGrip.Height = 22;
            deckGrip.Margin = new Thickness(0, 0, 6, 0);
            titleStack.Children.Add(deckGrip);

            Button favBtn = new Button
            {
                Content = profile.IsFavorite ? "★" : "☆",
                FontSize = 12,
                Foreground = profile.IsFavorite ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : (SolidColorBrush)findResource("TextMuted"),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = profile.IsFavorite ? "Remove from Favorites" : "Add to Favorites"
            };
            favBtn.Click += (s, e) =>
            {
                e.Handled = true;
                if (onToggleFavorite != null) onToggleFavorite(profile);
            };
            titleStack.Children.Add(favBtn);

            Border iconBadge = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, accentColor.R, accentColor.G, accentColor.B)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            if (profile.IconImage != null)
            {
                Image iconImg = new Image
                {
                    Source = profile.IconImage,
                    Width = 18,
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);
                iconBadge.Child = iconImg;
            }
            else
            {
                TextBlock monogramTxt = new TextBlock
                {
                    Text = monogram.Length > 2 ? monogram.Substring(0, 2) : monogram,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accentColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconBadge.Child = monogramTxt;
            }
            titleStack.Children.Add(iconBadge);

            TextBlock titleTxt = new TextBlock
            {
                Text = profile.Name,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)findResource("TextPrimary"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            titleStack.Children.Add(titleTxt);
            Grid.SetColumn(titleStack, 0);
            grid.Children.Add(titleStack);

            // Col 1: Status Pill
            Border statusPill = new Border
            {
                Background = profile.IsActive ? new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)) : new SolidColorBrush(Color.FromArgb(30, 100, 116, 139)),
                BorderBrush = profile.IsActive ? new SolidColorBrush(Color.FromArgb(90, 16, 185, 129)) : new SolidColorBrush(Color.FromArgb(60, 100, 116, 139)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock statusTxt = new TextBlock
            {
                Text = profile.IsActive ? "ACTIVE" : "READY",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = profile.IsActive ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : (SolidColorBrush)findResource("TextSecondary")
            };
            statusPill.Child = statusTxt;
            Grid.SetColumn(statusPill, 1);
            grid.Children.Add(statusPill);

            // Col 2: FPS / Engine
            string fpsDesc;
            if (profile.Id == "roblox") fpsDesc = "240 FPS Native";
            else if (profile.Id == "bluestacks") fpsDesc = "144 FPS DirectX";
            else if (profile.Id == "valorant") fpsDesc = "DirectFlip Uncapped";
            else if (profile.Id == "lol") fpsDesc = "DirectX Uncapped";
            else if (profile.Id == "tekken7") fpsDesc = "60 FPS Fixed Sync";
            else fpsDesc = profile.TargetPacing ?? "Optimized";

            TextBlock fpsTxt = new TextBlock
            {
                Text = fpsDesc,
                FontSize = 10,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = (SolidColorBrush)findResource("TextSecondary"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(fpsTxt, 2);
            grid.Children.Add(fpsTxt);

            // Col 3: Kernel Pacing
            Border timerPill = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 245, 158, 11)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 245, 158, 11)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock timerTxt = new TextBlock
            {
                Text = "0.500ms Timer",
                FontSize = 8.5,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11))
            };
            timerPill.Child = timerTxt;
            Grid.SetColumn(timerPill, 3);
            grid.Children.Add(timerPill);

            // Col 4: Network / Latency
            TextBlock netTxt = new TextBlock
            {
                Text = "Low-Latency QoS",
                FontSize = 10,
                Foreground = (SolidColorBrush)findResource("TextSecondary"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(netTxt, 4);
            grid.Children.Add(netTxt);

            // Col 5: Quick Action Buttons
            StackPanel btnPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Button actionBtn = new Button
            {
                Content = profile.IsActive ? "Armed" : "1-Click Boost",
                Style = profile.IsActive ? (Style)findResource("SecondaryBtn") : (Style)findResource("AccentBtn"),
                Padding = new Thickness(10, 3, 10, 3),
                FontSize = 9.5,
                Margin = new Thickness(0, 0, 6, 0),
                Tag = profile
            };
            actionBtn.Click += (s, e) =>
            {
                e.Handled = true;
                if (onBoost != null) onBoost(profile, actionBtn);
            };
            btnPanel.Children.Add(actionBtn);

            Border toolsPanel = null;
            Button toolsToggleBtn = new Button
            {
                Content = "Tools ▼",
                Style = (Style)findResource("SecondaryBtn"),
                FontSize = 9.5,
                Padding = new Thickness(8, 3, 8, 3)
            };
            toolsToggleBtn.Click += (s, e) =>
            {
                e.Handled = true;
                if (toolsPanel == null)
                {
                    toolsToggleBtn.Content = "Loading...";
                    if (onBuildToolsPanel != null) toolsPanel = onBuildToolsPanel(profile);
                    if (toolsPanel == null) return;
                    toolsPanel.Tag = "ToolsPanel";
                    toolsPanel.Opacity = 0.0;
                    toolsPanel.MaxHeight = 0;
                    toolsPanel.Visibility = Visibility.Visible;
                    rowContainer.Children.Add(toolsPanel);

                    toolsPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double targetHeight = Math.Max(100, toolsPanel.DesiredSize.Height);

                    var openHeightAnim = new DoubleAnimation(0.0, targetHeight, new Duration(TimeSpan.FromMilliseconds(220)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var openFadeAnim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(180)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    openHeightAnim.Completed += (asender, aargs) =>
                    {
                        toolsPanel.MaxHeight = double.PositiveInfinity;
                    };
                    toolsPanel.BeginAnimation(FrameworkElement.MaxHeightProperty, openHeightAnim);
                    toolsPanel.BeginAnimation(UIElement.OpacityProperty, openFadeAnim);

                    toolsToggleBtn.Content = "Close ▲";
                    return;
                }

                bool isExpanded = (toolsToggleBtn.Content as string) == "Close ▲" || (toolsToggleBtn.Content as string) == "Close";
                if (isExpanded)
                {
                    toolsToggleBtn.Content = "Tools ▼";
                    double currentH = toolsPanel.ActualHeight > 0 ? toolsPanel.ActualHeight : 120;
                    toolsPanel.MaxHeight = currentH;

                    var closeHeightAnim = new DoubleAnimation(currentH, 0.0, new Duration(TimeSpan.FromMilliseconds(180)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    var closeFadeAnim = new DoubleAnimation(toolsPanel.Opacity, 0.0, new Duration(TimeSpan.FromMilliseconds(140)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    closeHeightAnim.Completed += (cs, ce) =>
                    {
                        if ((toolsToggleBtn.Content as string) == "Tools ▼")
                        {
                            toolsPanel.Visibility = Visibility.Collapsed;
                        }
                    };
                    toolsPanel.BeginAnimation(FrameworkElement.MaxHeightProperty, closeHeightAnim);
                    toolsPanel.BeginAnimation(UIElement.OpacityProperty, closeFadeAnim);
                }
                else
                {
                    toolsToggleBtn.Content = "Close ▲";
                    toolsPanel.Visibility = Visibility.Visible;
                    toolsPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double targetHeight = Math.Max(100, toolsPanel.DesiredSize.Height);

                    var openHeightAnim = new DoubleAnimation(0.0, targetHeight, new Duration(TimeSpan.FromMilliseconds(220)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var openFadeAnim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(180)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    openHeightAnim.Completed += (asender, aargs) =>
                    {
                        toolsPanel.MaxHeight = double.PositiveInfinity;
                    };
                    toolsPanel.BeginAnimation(FrameworkElement.MaxHeightProperty, openHeightAnim);
                    toolsPanel.BeginAnimation(UIElement.OpacityProperty, openFadeAnim);
                }
            };
            btnPanel.Children.Add(toolsToggleBtn);
            Grid.SetColumn(btnPanel, 5);
            grid.Children.Add(btnPanel);

            rowContainer.Children.Add(grid);
            row.Child = rowContainer;

            if (onAttachDragDrop != null) onAttachDragDrop(row, profile);
            return row;
        }
    }
}
