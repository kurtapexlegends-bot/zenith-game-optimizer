using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Ellipse = System.Windows.Shapes.Ellipse;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.UI.Library
{
    public static partial class GameCardRenderer
    {




        public static Border CreateGameProfileCard(
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
            GetGameBadgeInfo(profile.Name, out monogram, out accentColor);

            Border card = new Border();
            card.Background = (SolidColorBrush)findResource("BgCard");
            card.BorderBrush = new SolidColorBrush(Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B));
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(10);
            card.Padding = new Thickness(14, 14, 16, 14);
            card.Margin = new Thickness(0, 0, 0, 10);

            var shadow = new System.Windows.Media.Effects.DropShadowEffect()
            {
                BlurRadius = 14,
                ShadowDepth = 2,
                Direction = 270,
                Color = Colors.Black,
                Opacity = 0.04
            };
            card.Effect = shadow;

            var cardTrans = new TranslateTransform(0, 0);
            card.RenderTransformOrigin = new Point(0.5, 0.5);
            card.RenderTransform = cardTrans;

            var liftAnim = new DoubleAnimation(-2.5, new Duration(TimeSpan.FromMilliseconds(160)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var dropAnim = new DoubleAnimation(0.0, new Duration(TimeSpan.FromMilliseconds(180)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var shadowLiftAnim = new DoubleAnimation(0.09, new Duration(TimeSpan.FromMilliseconds(160)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var shadowDropAnim = new DoubleAnimation(0.04, new Duration(TimeSpan.FromMilliseconds(180)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            card.MouseEnter += (s, e) =>
            {
                cardTrans.BeginAnimation(TranslateTransform.YProperty, liftAnim);
                shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, shadowLiftAnim);
                card.BorderBrush = new SolidColorBrush(Color.FromArgb(160, accentColor.R, accentColor.G, accentColor.B));
            };
            card.MouseLeave += (s, e) =>
            {
                cardTrans.BeginAnimation(TranslateTransform.YProperty, dropAnim);
                shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, shadowDropAnim);
                card.BorderBrush = new SolidColorBrush(Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B));
            };
            card.MouseLeftButtonUp += (s, e) =>
            {
                if (onSpotlight != null) onSpotlight(profile);
            };

            Border monogramBadge = new Border()
            {
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, accentColor.R, accentColor.G, accentColor.B)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            if (profile.IconImage != null)
            {
                Image iconImg = new Image()
                {
                    Source = profile.IconImage,
                    Width = 32,
                    Height = 32,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);
                monogramBadge.Child = iconImg;
            }
            else
            {
                TextBlock monogramTxt = new TextBlock()
                {
                    Text = monogram,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accentColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                monogramBadge.Child = monogramTxt;
            }

            StackPanel textPanel = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            StackPanel titleRow = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };

            Button favBtn = new Button()
            {
                Content = profile.IsFavorite ? "★" : "☆",
                FontSize = 14,
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
            titleRow.Children.Add(favBtn);

            TextBlock titleTxt = new TextBlock()
            {
                Text = profile.Name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)findResource("TextPrimary"),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleRow.Children.Add(titleTxt);

            if (profile.IsInstalled)
            {
                Border installedPill = new Border()
                {
                    Background = new SolidColorBrush(Color.FromArgb(25, 16, 185, 129)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(70, 16, 185, 129)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock installedTxt = new TextBlock
                {
                    Text = "INSTALLED",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                };
                installedPill.Child = installedTxt;
                titleRow.Children.Add(installedPill);
            }

            if (profile.IsActive)
            {
                Border activePill = new Border()
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(90, 16, 185, 129)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 1, 6, 1),
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock activeTxt = new TextBlock
                {
                    Text = "ACTIVE",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                };
                activePill.Child = activeTxt;
                titleRow.Children.Add(activePill);
            }

            textPanel.Children.Add(titleRow);

            TextBlock descTxt = new TextBlock()
            {
                Text = string.Format("{0} • {1} • {2}", profile.PrimaryProcessName + ".exe", profile.TargetPacing, profile.Category),
                FontSize = 11,
                Foreground = (SolidColorBrush)findResource("TextSecondary"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            textPanel.Children.Add(descTxt);

            StackPanel btnPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            string initialContent = profile.IsActive ? "Armed & Active" : "Pre-Arm / Boost";
            Button actionBtn = new Button()
            {
                Content = initialContent,
                Style = profile.IsActive ? (Style)findResource("SecondaryBtn") : (Style)findResource("AccentBtn"),
                Padding = new Thickness(14, 6, 14, 6),
                FontSize = 11,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = profile
            };
            actionBtn.Click += (s, e) =>
            {
                if (onBoost != null) onBoost(profile, actionBtn);
            };
            btnPanel.Children.Add(actionBtn);

            Border toolsPanel = null;
            StackPanel cardContainer = new StackPanel();

            Button toolsToggleBtn = new Button()
            {
                Content = "Options ▼",
                Style = (Style)findResource("SecondaryBtn"),
                FontSize = 11,
                Padding = new Thickness(12, 6, 12, 6)
            };
            toolsToggleBtn.Click += (s, e) =>
            {
                if (toolsPanel == null)
                {
                    toolsToggleBtn.Content = "Loading...";
                    if (onBuildToolsPanel != null) toolsPanel = onBuildToolsPanel(profile);
                    if (toolsPanel == null) return;
                    toolsPanel.Tag = "ToolsPanel";
                    toolsPanel.Opacity = 0.0;
                    toolsPanel.MaxHeight = 0;
                    toolsPanel.Visibility = Visibility.Visible;
                    cardContainer.Children.Add(toolsPanel);

                    toolsPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double targetHeight = Math.Max(120, toolsPanel.DesiredSize.Height);

                    var openHeightAnim = new DoubleAnimation(0.0, targetHeight, new Duration(TimeSpan.FromMilliseconds(240)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var openFadeAnim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
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
                    toolsToggleBtn.Content = "Options ▼";
                    double currentH = toolsPanel.ActualHeight > 0 ? toolsPanel.ActualHeight : 150;
                    toolsPanel.MaxHeight = currentH;

                    var closeHeightAnim = new DoubleAnimation(currentH, 0.0, new Duration(TimeSpan.FromMilliseconds(200)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    var closeFadeAnim = new DoubleAnimation(toolsPanel.Opacity, 0.0, new Duration(TimeSpan.FromMilliseconds(160)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    closeHeightAnim.Completed += (cs, ce) =>
                    {
                        if ((toolsToggleBtn.Content as string) == "Options ▼")
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
                    double targetHeight = Math.Max(120, toolsPanel.DesiredSize.Height);

                    var openHeightAnim = new DoubleAnimation(0.0, targetHeight, new Duration(TimeSpan.FromMilliseconds(240)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var openFadeAnim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
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

            Border gripHandle = CreateGripHandle(findResource);

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // 0: Grip handle
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // 1: Monogram badge
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // 2: Info
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // 3: Actions

            Grid.SetColumn(gripHandle, 0);
            Grid.SetColumn(monogramBadge, 1);
            Grid.SetColumn(textPanel, 2);
            Grid.SetColumn(btnPanel, 3);

            headerGrid.Children.Add(gripHandle);
            headerGrid.Children.Add(monogramBadge);
            headerGrid.Children.Add(textPanel);
            headerGrid.Children.Add(btnPanel);

            cardContainer.Children.Add(headerGrid);
            card.Child = cardContainer;
            if (onAttachDragDrop != null) onAttachDragDrop(card, profile);
            return card;
        }
    }
}
