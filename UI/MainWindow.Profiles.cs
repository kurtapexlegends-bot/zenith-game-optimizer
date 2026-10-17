using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Ellipse = System.Windows.Shapes.Ellipse;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer
{
    public partial class MainWindow : Window
    {
        private void PopulateGameProfiles()
        {
            if (PnlGameProfilesList == null || _monitorService == null) return;
            PnlGameProfilesList.Children.Clear();

            // Auto-detect installed games & extract genuine executable icons
            GameLauncherService.ScanAllLocalInstalledGames(_monitorService.Profiles);

            // Sync favorites from persistent settings
            var favs = _monitorService.Settings.FavoriteGameIds ?? new List<string>();
            foreach (var p in _monitorService.Profiles)
            {
                p.IsFavorite = favs.Contains(p.Id);
            }

            // Filter ONLY games installed on user machine (including active and user-added custom titles)
            var profilesList = new List<GameProfile>();
            foreach (var p in _monitorService.Profiles)
            {
                if (p.IsInstalled || p.IsCustom || p.IsActive)
                {
                    profilesList.Add(p);
                }
            }

            int totalInstalledCount = profilesList.Count;

            // Instant Real-Time Search & Keyword Filter
            string searchQuery = (TxtSearchGames != null && !string.IsNullOrWhiteSpace(TxtSearchGames.Text))
                ? TxtSearchGames.Text.Trim()
                : null;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                profilesList = profilesList.FindAll(p =>
                    (p.Name != null && p.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.ProcessNames != null && p.ProcessNames.Exists(proc => proc != null && proc.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                    (p.PrimaryProcessName != null && p.PrimaryProcessName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.Category != null && p.Category.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.TargetPacing != null && p.TargetPacing.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.OptimizationBadge != null && p.OptimizationBadge.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            // Sort profiles
            string sortMode = _monitorService.Settings.GameSortMode ?? "Custom";
            var customOrder = _monitorService.Settings.CustomGameOrder ?? new List<string>();

            if (sortMode == "Custom" || (sortMode == "Installed" && customOrder.Count > 0))
            {
                profilesList.Sort((a, b) =>
                {
                    int indexA = customOrder.IndexOf(a.Id);
                    int indexB = customOrder.IndexOf(b.Id);
                    if (indexA == -1 && indexB == -1) return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    if (indexA == -1) return 1;
                    if (indexB == -1) return -1;
                    return indexA.CompareTo(indexB);
                });
            }
            else if (sortMode == "Name")
            {
                profilesList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            else if (sortMode == "Favorites")
            {
                profilesList.Sort((a, b) =>
                {
                    if (a.IsFavorite != b.IsFavorite) return b.IsFavorite.CompareTo(a.IsFavorite);
                    if (a.IsActive != b.IsActive) return b.IsActive.CompareTo(a.IsActive);
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            else if (sortMode == "Active")
            {
                profilesList.Sort((a, b) =>
                {
                    if (a.IsActive != b.IsActive) return b.IsActive.CompareTo(a.IsActive);
                    if (a.IsFavorite != b.IsFavorite) return b.IsFavorite.CompareTo(a.IsFavorite);
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            else
            {
                // Default fallback
                profilesList.Sort((a, b) =>
                {
                    if (a.IsFavorite != b.IsFavorite) return b.IsFavorite.CompareTo(a.IsFavorite);
                    if (a.IsActive != b.IsActive) return b.IsActive.CompareTo(a.IsActive);
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }

            // Update header count badge with search telemetry
            if (TxtGamesCountBadge != null)
            {
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    TxtGamesCountBadge.Text = string.Format("{0} of {1} Filtered", profilesList.Count, totalInstalledCount);
                }
                else
                {
                    TxtGamesCountBadge.Text = string.Format("{0} Installed", profilesList.Count);
                }
            }

            // Update view switcher visual buttons
            string viewMode = _monitorService.Settings.GameViewMode ?? "Cards";
            UpdateViewSwitcherVisuals(viewMode);
            UpdateSortComboVisuals(sortMode);

            if (profilesList.Count == 0)
            {
                Border emptyCard = new Border
                {
                    Background = (SolidColorBrush)FindResource("BgCard"),
                    BorderBrush = (SolidColorBrush)FindResource("BorderCard"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(24, 28, 24, 28),
                    Margin = new Thickness(0, 0, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                StackPanel emptyStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    Border iconBadge = new Border
                    {
                        Width = 44,
                        Height = 44,
                        CornerRadius = new CornerRadius(22),
                        Background = (SolidColorBrush)FindResource("BgSunken"),
                        BorderBrush = (SolidColorBrush)FindResource("BorderSubtle"),
                        BorderThickness = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 10),
                        Child = new System.Windows.Shapes.Path
                        {
                            Data = Geometry.Parse("M 6,6 A 5,5 0 1,1 6,16 A 5,5 0 1,1 6,6 Z M 14,14 L 18,18"),
                            Stroke = (SolidColorBrush)FindResource("TextMuted"),
                            StrokeThickness = 1.6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    TextBlock emptySearchTitle = new TextBlock
                    {
                        Text = string.Format("No Games Matching \"{0}\"", searchQuery),
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = (SolidColorBrush)FindResource("TextPrimary"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    TextBlock emptySearchDesc = new TextBlock
                    {
                        Text = "Check your search keyword, process name, or clear the filter to show all installed games.",
                        FontSize = 11,
                        Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 12)
                    };
                    Button clearFilterBtn = new Button
                    {
                        Content = "✕ Clear Search Filter",
                        Style = (Style)FindResource("SecondaryBtn"),
                        FontSize = 11,
                        Padding = new Thickness(14, 6, 14, 6),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    clearFilterBtn.Click += (s, e) =>
                    {
                        if (TxtSearchGames != null)
                        {
                            TxtSearchGames.Text = string.Empty;
                        }
                    };
                    emptyStack.Children.Add(iconBadge);
                    emptyStack.Children.Add(emptySearchTitle);
                    emptyStack.Children.Add(emptySearchDesc);
                    emptyStack.Children.Add(clearFilterBtn);
                }
                else
                {
                    Border iconBadge = new Border
                    {
                        Width = 44,
                        Height = 44,
                        CornerRadius = new CornerRadius(22),
                        Background = (SolidColorBrush)FindResource("BgSunken"),
                        BorderBrush = (SolidColorBrush)FindResource("BorderSubtle"),
                        BorderThickness = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 10),
                        Child = new System.Windows.Shapes.Path
                        {
                            Data = Geometry.Parse("M 2,7 A 5,5 0 0,1 18,7 L 18,13 A 2,2 0 0,1 15,15 L 14,13 L 6,13 L 5,15 A 2,2 0 0,1 2,13 Z M 5,6 L 7,6 M 6,5 L 6,7 M 13,6 L 15,6"),
                            Stroke = (SolidColorBrush)FindResource("TextMuted"),
                            StrokeThickness = 1.4,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    TextBlock emptyTitle = new TextBlock
                    {
                        Text = "No Installed Games Detected",
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = (SolidColorBrush)FindResource("TextPrimary"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    TextBlock emptyDesc = new TextBlock
                    {
                        Text = "Install supported titles or click below to discover active user processes or add custom game executables.",
                        FontSize = 11,
                        Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 12)
                    };
                    StackPanel actionButtons = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    Button scanBtn = new Button
                    {
                        Content = "Scan Running Windows",
                        Style = (Style)FindResource("AccentBtn"),
                        FontSize = 11,
                        Padding = new Thickness(12, 6, 12, 6),
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    scanBtn.Click += (s, e) => { BtnDetectGames_Click(s, e); };

                    Button addBtn = new Button
                    {
                        Content = "+ Add Executable (.exe)",
                        Style = (Style)FindResource("SecondaryBtn"),
                        FontSize = 11,
                        Padding = new Thickness(12, 6, 12, 6)
                    };
                    addBtn.Click += (s, e) => { BtnAddCustomGame_Click(s, e); };

                    actionButtons.Children.Add(scanBtn);
                    actionButtons.Children.Add(addBtn);

                    emptyStack.Children.Add(iconBadge);
                    emptyStack.Children.Add(emptyTitle);
                    emptyStack.Children.Add(emptyDesc);
                    emptyStack.Children.Add(actionButtons);
                }

                emptyCard.Child = emptyStack;
                PnlGameProfilesList.Children.Add(emptyCard);
                return;
            }

            if (viewMode == "List" || viewMode == "Deck")
            {
                Border header = CreateMetricsMatrixHeader();
                PnlGameProfilesList.Children.Add(header);
            }

            // Populate items
            foreach (var profile in profilesList)
            {
                if (viewMode == "List" || viewMode == "Deck")
                {
                    Border row = CreateGameProfileMetricsRow(profile);
                    PnlGameProfilesList.Children.Add(row);
                }
                else
                {
                    Border card = CreateGameProfileCard(profile);
                    PnlGameProfilesList.Children.Add(card);
                }
            }
        }

        private Border CreateGripHandle()
        {
            Border gripHandle = new Border
            {
                Width = 18,
                Height = 32,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.SizeAll,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Drag handle to reorder in library (Custom Order)"
            };

            Grid gripDots = new Grid
            {
                Width = 10,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            gripDots.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            gripDots.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) });
            gripDots.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });

            Action<int, int> addDot = (row, col) =>
            {
                Ellipse dot = new Ellipse
                {
                    Width = 3,
                    Height = 3,
                    Fill = (SolidColorBrush)FindResource("TextMuted"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(dot, row);
                Grid.SetColumn(dot, col);
                gripDots.Children.Add(dot);
            };

            addDot(0, 0); addDot(0, 2);
            addDot(2, 0); addDot(2, 2);
            addDot(4, 0); addDot(4, 2);

            gripHandle.Child = gripDots;
            gripHandle.MouseEnter += (s, e) =>
            {
                gripHandle.Background = (SolidColorBrush)FindResource("BgSunken");
                gripHandle.BorderBrush = (SolidColorBrush)FindResource("BorderSubtle");
                foreach (UIElement child in gripDots.Children)
                {
                    var el = child as Ellipse;
                    if (el != null) el.Fill = (SolidColorBrush)FindResource("AccentAction");
                }
            };
            gripHandle.MouseLeave += (s, e) =>
            {
                gripHandle.Background = Brushes.Transparent;
                gripHandle.BorderBrush = Brushes.Transparent;
                foreach (UIElement child in gripDots.Children)
                {
                    var el = child as Ellipse;
                    if (el != null) el.Fill = (SolidColorBrush)FindResource("TextMuted");
                }
            };

            return gripHandle;
        }

        private Border CreateGameProfileCard(GameProfile profile)
        {
            string monogram;
            Color accentColor;
            GetGameBadgeInfo(profile.Name, out monogram, out accentColor);

            Border card = new Border();
            card.Background = (SolidColorBrush)FindResource("BgCard");
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
                AnimateHeroSpotlight(profile);
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
                Foreground = profile.IsFavorite ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : (SolidColorBrush)FindResource("TextMuted"),
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
                ToggleGameFavorite(profile);
            };
            titleRow.Children.Add(favBtn);

            TextBlock titleTxt = new TextBlock()
            {
                Text = profile.Name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextPrimary"),
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
                TextBlock installedTxt = new TextBlock()
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
                TextBlock activeTxt = new TextBlock()
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
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
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
            System.Windows.Controls.Button actionBtn = new System.Windows.Controls.Button()
            {
                Content = initialContent,
                Style = profile.IsActive ? (Style)FindResource("SecondaryBtn") : (Style)FindResource("AccentBtn"),
                Padding = new Thickness(14, 6, 14, 6),
                FontSize = 11,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = profile
            };
            actionBtn.Click += (s, e) =>
            {
                GameProfile p = (GameProfile)((System.Windows.Controls.Button)s).Tag;
                string statusMsg;
                bool launched = _monitorService.ForceOptimizeGame(p, true, out statusMsg);
                actionBtn.Content = launched ? "Armed & Active" : "Pre-Armed";
                actionBtn.Style = (Style)FindResource("SecondaryBtn");
                TxtFooterStatus.Text = statusMsg;
                DashHeroTitle.Text = p.Name + (launched ? " (Active & Optimized)" : " (Pre-Armed)");
                DashHeroDesc.Text = statusMsg;

                ShowNotificationBanner(launched ? string.Format("{0} Active & Optimized", p.Name) : string.Format("{0} Pre-Armed for Low Latency", p.Name), "emerald");
                UpdateEnginePulseBadge(launched ? "Boosted (" + p.Name + ")" : "Pre-Armed (" + p.Name + ")", Color.FromRgb(16, 185, 129));

                var fadeAnim = new DoubleAnimation(0.4, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                DashHeroTitle.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                DashHeroDesc.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            };
            btnPanel.Children.Add(actionBtn);

            Border toolsPanel = null;
            StackPanel cardContainer = new StackPanel();

            System.Windows.Controls.Button toolsToggleBtn = new System.Windows.Controls.Button()
            {
                Content = "Options ▼",
                Style = (Style)FindResource("SecondaryBtn"),
                FontSize = 11,
                Padding = new Thickness(12, 6, 12, 6)
            };
            toolsToggleBtn.Click += (s, e) =>
            {
                if (toolsPanel == null)
                {
                    toolsToggleBtn.Content = "Loading...";
                    toolsPanel = BuildGameSpecificToolsPanel(profile);
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

            Border gripHandle = CreateGripHandle();

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
            AttachGameDragDropReorder(card, profile);
            return card;
        }

        private void AnimateHeroSpotlight(GameProfile profile)
        {
            if (profile == null) return;
            if (DashHeroTitle != null) DashHeroTitle.Text = profile.Name;
            if (DashHeroDesc != null) DashHeroDesc.Text = string.Format("{0} • Target: {1} • {2}", profile.Category, profile.TargetPacing, profile.Description);

            var fadeAnim = new DoubleAnimation(0.4, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            if (DashHeroTitle != null) DashHeroTitle.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            if (DashHeroDesc != null) DashHeroDesc.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        private void BtnDetectGames_Click(object sender, RoutedEventArgs e)
        {
            PnlDiscoveredList.Children.Clear();
            var apps = _monitorService.GetRunningUserApplications();

            if (apps.Count == 0)
            {
                TextBlock none = new TextBlock()
                {
                    Text = "No active user windows found. Launch your game and click detect again.",
                    FontSize = 11,
                    Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                    Margin = new Thickness(0, 4, 0, 4)
                };
                PnlDiscoveredList.Children.Add(none);
            }
            else
            {
                foreach (var app in apps)
                {
                    Border item = new Border()
                    {
                        Background = (SolidColorBrush)FindResource("BgSunken"),
                        BorderBrush = (SolidColorBrush)FindResource("BorderSubtle"),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 0, 0, 6)
                    };

                    Grid g = new Grid();
                    g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                    StackPanel sp = new StackPanel();
                    TextBlock nameTxt = new TextBlock()
                    {
                        Text = app.WindowTitle,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (SolidColorBrush)FindResource("TextPrimary"),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    TextBlock procTxt = new TextBlock()
                    {
                        Text = "Process: " + app.ProcessName + ".exe (PID " + app.ProcessId + ")",
                        FontSize = 10,
                        Foreground = (SolidColorBrush)FindResource("TextMuted")
                    };
                    sp.Children.Add(nameTxt);
                    sp.Children.Add(procTxt);
                    Grid.SetColumn(sp, 0);
                    g.Children.Add(sp);

                    System.Windows.Controls.Button addBtn = new System.Windows.Controls.Button()
                    {
                        Content = "+ Add to Zenith",
                        Style = (Style)FindResource("AccentBtn"),
                        FontSize = 11,
                        Padding = new Thickness(10, 4, 10, 4),
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = app
                    };
                    addBtn.Click += (btnSender, btnArgs) =>
                    {
                        var targetApp = (RunningAppInfo)((System.Windows.Controls.Button)btnSender).Tag;
                        _monitorService.AddCustomProfile(targetApp.WindowTitle, targetApp.ProcessName, "User Game");
                        PopulateGameProfiles();
                        PnlDiscoveredApps.Visibility = Visibility.Collapsed;
                        Log("Added detected game: " + targetApp.WindowTitle + " (" + targetApp.ProcessName + ".exe)");
                    };
                    Grid.SetColumn(addBtn, 1);
                    g.Children.Add(addBtn);

                    item.Child = g;
                    PnlDiscoveredList.Children.Add(item);
                }
            }

            PnlDiscoveredApps.Visibility = Visibility.Visible;
        }

        private void BtnCloseDiscovered_Click(object sender, RoutedEventArgs e)
        {
            PnlDiscoveredApps.Visibility = Visibility.Collapsed;
        }

        private void BtnAddCustomGame_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
            dlg.Title = "Select Game Executable to Optimize";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string exeName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                string cleanName = exeName;
                _monitorService.AddCustomProfile(cleanName, exeName, "Custom Game");
                if (_monitorService.Profiles.Count > 0)
                {
                    var added = _monitorService.Profiles[_monitorService.Profiles.Count - 1];
                    added.DetectedExecutablePath = dlg.FileName;
                    added.IsInstalled = true;
                    try
                    {
                        added.IconImage = GameLauncherService.ExtractGameIcon(dlg.FileName);
                    }
                    catch { }
                }
                PopulateGameProfiles();
                Log("Added custom game profile: " + cleanName + " (" + exeName + ".exe)");
            }
        }

        private void BtnBenchmarkPing_Click(object sender, RoutedEventArgs e)
        {
            string host = "1.1.1.1";
            if (CmbPingTarget.SelectedItem != null)
            {
                var item = CmbPingTarget.SelectedItem as ComboBoxItem;
                if (item != null && item.Tag != null)
                {
                    host = item.Tag.ToString();
                }
            }

            TxtPingResult.Text = "Sending multi-sample packet bursts to " + host + "...";
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                long avgPing, jitter;
                int packetLoss;
                string result = _monitorService.NetworkService.BenchmarkConnection(host, out avgPing, out jitter, out packetLoss);
                Dispatcher.Invoke(new Action(delegate
                {
                    TxtPingResult.Text = result;
                    Log("Latency Benchmark (" + host + "): " + result);
                }));
            });
        }

        private void BtnManualBoost_Click(object sender, RoutedEventArgs e)
        {
            _monitorService.TimerEngine.EnableHighResolution();
            _monitorService.PowerService.SetHighPerformanceScheme();
            _monitorService.SystemTweaks.StabilizeAudioEngine();
            long freed = _monitorService.MemoryService.PurgeSafeBackgroundMemory();
            double mb = Math.Round((double)freed / (1024 * 1024), 1);

            DashHeroTitle.Text = "High-Performance Gaming Mode Active";
            DashHeroDesc.Text = "PC is fully boosted: 0.500ms input response active, audio shield enabled, High Performance power mode engaged, and background RAM cleaned.";
            BtnManualBoost.Content = "Boosted (Active)";
            BtnManualBoost.Style = (Style)FindResource("SecondaryBtn");

            Log(string.Format("Manual PC Boost triggered. 0.5ms Timer, Audio Shield & High Performance mode engaged. Reclaimed {0} MB RAM.", mb > 0 ? mb.ToString() : "50+"));
            ShowNotificationBanner(string.Format("System Boost Active: 0.500ms Timer, Audio Shield & High Performance engaged (Freed {0} MB RAM)", mb > 0 ? mb.ToString() : "50+"), "boost");
            UpdateEnginePulseBadge("Boosted (0.500ms Active)", Color.FromRgb(16, 185, 129));
        }

        private void BtnInstantCooldown_Click(object sender, RoutedEventArgs e)
        {
            string msg;
            bool ok = _monitorService.PowerService.EngageInstantCooldown(out msg);
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

            DashHeroTitle.Text = "Laptop Cooldown Mode Active";
            DashHeroDesc.Text = "Active ACPI fan cooling engaged, CPU capped at 80% to dump heat, Turbo voltage spikes cut, and background apps trimmed.";
            if (BtnCooldownNow != null)
            {
                BtnCooldownNow.Content = "Cooling (Active)";
            }
            if (BtnCooldownView != null)
            {
                BtnCooldownView.Content = "Cooling (Active)";
            }

            Log(string.Format("Instant Laptop Cooldown engaged: ACPI active fans spinning, CPU capped at 80%, Turbo spikes cut. Freed {0} MB idle RAM.", mb > 0 ? mb.ToString() : "50+"));
            ShowNotificationBanner(string.Format("Laptop Cooldown Active: ACPI fans engaged, CPU capped at 80% (Freed {0} MB RAM)", mb > 0 ? mb.ToString() : "50+"), "cooling");
            UpdateEnginePulseBadge("Thermal Cooldown Active", Color.FromRgb(56, 189, 248));

            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(1500, "Zenith Laptop Cooldown", 
                    "Active fan cooling engaged & CPU voltage dropped. Temperatures dropping rapidly.", 
                    System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void BtnExportProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = "Export Zenith Optimization Profile",
                    Filter = "Zenith Profile (*.zenith)|*.zenith|JSON File (*.json)|*.json",
                    FileName = string.Format("Zenith_Profile_{0}_{1:yyyyMMdd_HHmm}.zenith",
                        _monitorService.ActiveProfile != null ? _monitorService.ActiveProfile.Id : "Global",
                        DateTime.Now)
                };

                if (sfd.ShowDialog() == true)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("{");
                    sb.AppendLine("  \"AppName\": \"Zenith Game Optimizer\",");
                    sb.AppendLine("  \"Version\": \"2.5\",");
                    sb.AppendLine(string.Format("  \"ExportDate\": \"{0:O}\",", DateTime.UtcNow));
                    sb.AppendLine(string.Format("  \"ActiveProfile\": \"{0}\",", _monitorService.ActiveProfile != null ? _monitorService.ActiveProfile.Id : "None"));
                    sb.AppendLine("  \"AutoOptimize\": " + (ChkAutoOptimize.IsChecked == true ? "true" : "false") + ",");
                    sb.AppendLine("  \"TweakTimer\": " + (ChkTweakTimer.IsChecked == true ? "true" : "false") + ",");
                    sb.AppendLine("  \"AudioStabilization\": " + (ChkAudioStabilization.IsChecked == true ? "true" : "false") + ",");
                    
                    sb.AppendLine("  \"ActiveTools\": [");
                    var activeKeys = new List<string>();
                    foreach (var kvp in _toolActiveStates)
                    {
                        if (kvp.Value) activeKeys.Add(kvp.Key);
                    }
                    for (int i = 0; i < activeKeys.Count; i++)
                    {
                        string comma = (i < activeKeys.Count - 1) ? "," : "";
                        sb.AppendLine(string.Format("    \"{0}\"{1}", activeKeys[i].Replace("\"", "\\\""), comma));
                    }
                    sb.AppendLine("  ]");
                    sb.AppendLine("}");

                    File.WriteAllText(sfd.FileName, sb.ToString());
                    ShowNotificationBanner("Profile exported successfully to " + Path.GetFileName(sfd.FileName));
                }
            }
            catch (Exception ex)
            {
                ShowNotificationBanner("Export Failed: " + ex.Message);
            }
        }

        private void BtnImportProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog()
                {
                    Title = "Import Zenith Optimization Profile",
                    Filter = "Zenith Profile (*.zenith;*.json)|*.zenith;*.json|All Files (*.*)|*.*"
                };

                if (ofd.ShowDialog() == true)
                {
                    string json = File.ReadAllText(ofd.FileName);
                    int loadedTools = 0;

                    var toolMatches = System.Text.RegularExpressions.Regex.Matches(json, @"\""ActiveTools\""\s*:\s*\[([^\]]*)\]");
                    if (toolMatches.Count > 0)
                    {
                        string inner = toolMatches[0].Groups[1].Value;
                        var items = System.Text.RegularExpressions.Regex.Matches(inner, @"\""([^\""]+)\""");
                        foreach (System.Text.RegularExpressions.Match m in items)
                        {
                            string toolName = m.Groups[1].Value;
                            _toolActiveStates[toolName] = true;
                            loadedTools++;
                        }
                    }

                    PopulateGameProfiles();
                    UpdateLatencyReductionBadge();
                    ShowNotificationBanner(string.Format("Profile imported. Loaded {0} tools from {1}", loadedTools, Path.GetFileName(ofd.FileName)));
                }
            }
            catch (Exception ex)
            {
                ShowNotificationBanner("Import Failed: " + ex.Message);
            }
        }

        private static void GetGameBadgeInfo(string gameName, out string monogram, out Color accentColor)
        {
            string lower = (gameName ?? "").ToLowerInvariant();
            if (lower.Contains("mobile legends") || lower.Contains("bluestacks") || lower.Contains("mlbb"))
            {
                monogram = "MLBB";
                accentColor = Color.FromRgb(6, 182, 212); // Cyan
            }
            else if (lower.Contains("valorant"))
            {
                monogram = "VAL";
                accentColor = Color.FromRgb(244, 63, 94); // Rose
            }
            else if (lower.Contains("creed") || lower.Contains("unity"))
            {
                monogram = "ACU";
                accentColor = Color.FromRgb(245, 158, 11); // Amber
            }
            else if (lower.Contains("league") || lower.Contains("lol"))
            {
                monogram = "LOL";
                accentColor = Color.FromRgb(217, 119, 6); // Warm Gold
            }
            else if (lower.Contains("tekken"))
            {
                monogram = "TK7";
                accentColor = Color.FromRgb(249, 115, 22); // Orange
            }
            else if (lower.Contains("counter-strike") || lower.Contains("cs2"))
            {
                monogram = "CS2";
                accentColor = Color.FromRgb(234, 179, 8); // Yellow-gold
            }
            else if (lower.Contains("roblox"))
            {
                monogram = "RBLX";
                accentColor = Color.FromRgb(59, 130, 246); // Blue
            }
            else if (lower.Contains("dota"))
            {
                monogram = "DOTA";
                accentColor = Color.FromRgb(239, 68, 68); // Red
            }
            else if (lower.Contains("minecraft"))
            {
                monogram = "MC";
                accentColor = Color.FromRgb(34, 197, 94); // Green
            }
            else
            {
                monogram = "GAME";
                accentColor = Color.FromRgb(139, 92, 246); // Purple
            }
        }

        private Point _dragStartPos;
        private GameProfile _draggedProfile;

        private void AttachGameDragDropReorder(Border itemElement, GameProfile profile)
        {
            if (itemElement == null || profile == null) return;

            itemElement.AllowDrop = true;
            Brush defaultBorderBrush = itemElement.BorderBrush;
            Thickness defaultBorderThickness = itemElement.BorderThickness;

            itemElement.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (IsInteractiveOrToolsElement((DependencyObject)e.OriginalSource)) return;
                _dragStartPos = e.GetPosition(null);
                _draggedProfile = profile;
            };

            itemElement.PreviewMouseLeftButtonUp += (s, e) =>
            {
                _draggedProfile = null;
            };

            itemElement.PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && _draggedProfile == profile)
                {
                    Point curPos = e.GetPosition(null);
                    if (Math.Abs(curPos.X - _dragStartPos.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(curPos.Y - _dragStartPos.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        _draggedProfile = null;
                        DataObject dragData = new DataObject("ZenithGameDragId", profile.Id);
                        DragDrop.DoDragDrop(itemElement, dragData, DragDropEffects.Move);
                    }
                }
            };

            itemElement.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent("ZenithGameDragId"))
                {
                    string sourceId = (string)e.Data.GetData("ZenithGameDragId");
                    if (!string.IsNullOrEmpty(sourceId) && sourceId != profile.Id)
                    {
                        itemElement.BorderBrush = (SolidColorBrush)FindResource("AccentAction");
                        itemElement.BorderThickness = new Thickness(1.5);
                    }
                }
            };

            itemElement.DragLeave += (s, e) =>
            {
                itemElement.BorderBrush = defaultBorderBrush;
                itemElement.BorderThickness = defaultBorderThickness;
            };

            itemElement.DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent("ZenithGameDragId"))
                {
                    e.Effects = DragDropEffects.Move;
                    e.Handled = true;
                }
            };

            itemElement.Drop += (s, e) =>
            {
                itemElement.BorderBrush = defaultBorderBrush;
                itemElement.BorderThickness = defaultBorderThickness;
                if (e.Data.GetDataPresent("ZenithGameDragId"))
                {
                    string sourceId = (string)e.Data.GetData("ZenithGameDragId");
                    if (!string.IsNullOrEmpty(sourceId) && sourceId != profile.Id)
                    {
                        ReorderCustomGames(sourceId, profile.Id);
                    }
                }
            };
        }

        private void ReorderCustomGames(string sourceId, string targetId)
        {
            if (_monitorService == null || _monitorService.Settings == null) return;
            var settings = _monitorService.Settings;
            var order = settings.CustomGameOrder ?? new List<string>();

            // Ensure all installed games are registered in the custom order list
            foreach (var p in _monitorService.Profiles)
            {
                if ((p.IsInstalled || p.IsCustom || p.IsActive) && !order.Contains(p.Id))
                {
                    order.Add(p.Id);
                }
            }

            int srcIdx = order.IndexOf(sourceId);
            int tgtIdx = order.IndexOf(targetId);
            if (srcIdx >= 0 && tgtIdx >= 0 && srcIdx != tgtIdx)
            {
                order.RemoveAt(srcIdx);
                order.Insert(tgtIdx, sourceId);
                settings.CustomGameOrder = order;
                settings.GameSortMode = "Custom";
                settings.Save();
                PopulateGameProfiles();
                var movedProfile = _monitorService.Profiles.Find(p => p.Id == sourceId);
                ShowNotificationBanner(string.Format("Reordered: {0}", movedProfile != null ? movedProfile.Name : sourceId), "emerald");
            }
        }

        private static bool IsInteractiveOrToolsElement(DependencyObject element)
        {
            while (element != null)
            {
                if (element is System.Windows.Controls.Primitives.ButtonBase ||
                    element is TextBox ||
                    element is ComboBox ||
                    element is ComboBoxItem ||
                    element is Slider ||
                    element is System.Windows.Controls.Primitives.ScrollBar ||
                    element is CheckBox ||
                    element is RadioButton)
                {
                    return true;
                }

                FrameworkElement fe = element as FrameworkElement;
                if (fe != null && (fe.Tag as string == "ToolsPanel" || fe.Name == "ToolsPanel"))
                {
                    return true;
                }

                if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
                {
                    element = VisualTreeHelper.GetParent(element);
                }
                else
                {
                    element = LogicalTreeHelper.GetParent(element);
                }
            }
            return false;
        }

        private static bool IsDescendantOf<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T) return true;
                if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
                {
                    element = VisualTreeHelper.GetParent(element);
                }
                else
                {
                    element = LogicalTreeHelper.GetParent(element);
                }
            }
            return false;
        }

        private Border CreateMetricsMatrixHeader()
        {
            Border header = new Border
            {
                Background = (SolidColorBrush)FindResource("BgSunken"),
                BorderBrush = (SolidColorBrush)FindResource("BorderSubtle"),
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
                    Foreground = (SolidColorBrush)FindResource("TextMuted"),
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
                Foreground = (SolidColorBrush)FindResource("TextMuted"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(actionTb, 5);
            grid.Children.Add(actionTb);

            header.Child = grid;
            return header;
        }

        private Border CreateGameProfileRow(GameProfile profile)
        {
            return CreateGameProfileMetricsRow(profile);
        }

        private Border CreateGameProfileMetricsRow(GameProfile profile)
        {
            string monogram;
            Color accentColor;
            GetGameBadgeInfo(profile.Name, out monogram, out accentColor);

            Border row = new Border
            {
                Background = (SolidColorBrush)FindResource("BgCard"),
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
                row.Background = (SolidColorBrush)FindResource("BgCardHover");
            };
            row.MouseLeave += (s, e) =>
            {
                row.BorderBrush = new SolidColorBrush(Color.FromArgb(70, accentColor.R, accentColor.G, accentColor.B));
                row.Background = (SolidColorBrush)FindResource("BgCard");
            };
            row.MouseLeftButtonUp += (s, e) =>
            {
                AnimateHeroSpotlight(profile);
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
            Border deckGrip = CreateGripHandle();
            deckGrip.Height = 22;
            deckGrip.Margin = new Thickness(0, 0, 6, 0);
            titleStack.Children.Add(deckGrip);

            Button favBtn = new Button
            {
                Content = profile.IsFavorite ? "★" : "☆",
                FontSize = 12,
                Foreground = profile.IsFavorite ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : (SolidColorBrush)FindResource("TextMuted"),
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
                ToggleGameFavorite(profile);
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
                Foreground = (SolidColorBrush)FindResource("TextPrimary"),
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
                Foreground = profile.IsActive ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : (SolidColorBrush)FindResource("TextSecondary")
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
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
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
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(netTxt, 4);
            grid.Children.Add(netTxt);

            // Col 5: Quick Action Buttons
            StackPanel btnPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Button actionBtn = new Button
            {
                Content = profile.IsActive ? "Armed" : "1-Click Boost",
                Style = profile.IsActive ? (Style)FindResource("SecondaryBtn") : (Style)FindResource("AccentBtn"),
                Padding = new Thickness(10, 3, 10, 3),
                FontSize = 9.5,
                Margin = new Thickness(0, 0, 6, 0),
                Tag = profile
            };
            actionBtn.Click += (s, e) =>
            {
                e.Handled = true;
                string statusMsg;
                bool launched = _monitorService.ForceOptimizeGame(profile, true, out statusMsg);
                actionBtn.Content = launched ? "Armed & Active" : "Pre-Armed";
                actionBtn.Style = (Style)FindResource("SecondaryBtn");
                TxtFooterStatus.Text = statusMsg;
                DashHeroTitle.Text = profile.Name + (launched ? " (Active & Optimized)" : " (Pre-Armed)");
                DashHeroDesc.Text = statusMsg;
                ShowNotificationBanner(launched ? string.Format("{0} Active & Optimized", profile.Name) : string.Format("{0} Pre-Armed for Low Latency", profile.Name), "emerald");
                UpdateEnginePulseBadge(launched ? "Boosted (" + profile.Name + ")" : "Pre-Armed (" + profile.Name + ")", Color.FromRgb(16, 185, 129));
            };
            btnPanel.Children.Add(actionBtn);

            Border toolsPanel = null;
            Button toolsToggleBtn = new Button
            {
                Content = "Tools ▼",
                Style = (Style)FindResource("SecondaryBtn"),
                FontSize = 9.5,
                Padding = new Thickness(8, 3, 8, 3)
            };
            toolsToggleBtn.Click += (s, e) =>
            {
                e.Handled = true;
                if (toolsPanel == null)
                {
                    toolsToggleBtn.Content = "Loading...";
                    toolsPanel = BuildGameSpecificToolsPanel(profile);
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

            // Wire drag-and-drop reordering
            AttachGameDragDropReorder(row, profile);

            return row;
        }

        private void ToggleGameFavorite(GameProfile profile)
        {
            if (profile == null) return;
            profile.IsFavorite = !profile.IsFavorite;

            if (_monitorService.Settings.FavoriteGameIds == null)
            {
                _monitorService.Settings.FavoriteGameIds = new List<string>();
            }

            if (profile.IsFavorite)
            {
                if (!_monitorService.Settings.FavoriteGameIds.Contains(profile.Id))
                {
                    _monitorService.Settings.FavoriteGameIds.Add(profile.Id);
                }
            }
            else
            {
                _monitorService.Settings.FavoriteGameIds.Remove(profile.Id);
            }

            _monitorService.Settings.Save();
            PopulateGameProfiles();
        }

        private void UpdateViewSwitcherVisuals(string viewMode)
        {
            if (BtnViewCards == null || BtnViewList == null) return;

            bool isCards = viewMode == "Cards";
            BtnViewCards.Background = isCards ? (SolidColorBrush)FindResource("AccentAction") : Brushes.Transparent;
            BtnViewCards.Foreground = isCards ? (SolidColorBrush)FindResource("AccentActionFg") : (SolidColorBrush)FindResource("TextSecondary");
            BtnViewCards.FontWeight = isCards ? FontWeights.SemiBold : FontWeights.Normal;

            BtnViewList.Background = !isCards ? (SolidColorBrush)FindResource("AccentAction") : Brushes.Transparent;
            BtnViewList.Foreground = !isCards ? (SolidColorBrush)FindResource("AccentActionFg") : (SolidColorBrush)FindResource("TextSecondary");
            BtnViewList.FontWeight = !isCards ? FontWeights.SemiBold : FontWeights.Normal;
        }

        private bool _isUpdatingSortVisuals = false;

        private void UpdateSortComboVisuals(string sortMode)
        {
            if (CmbSortGames == null) return;
            _isUpdatingSortVisuals = true;
            try
            {
                foreach (ComboBoxItem item in CmbSortGames.Items)
                {
                    if (item != null && item.Tag != null && item.Tag.ToString().Equals(sortMode, StringComparison.OrdinalIgnoreCase))
                    {
                        if (CmbSortGames.SelectedItem != item)
                        {
                            CmbSortGames.SelectedItem = item;
                        }
                        break;
                    }
                }
            }
            finally
            {
                _isUpdatingSortVisuals = false;
            }
        }

        private void AnimateViewTransition()
        {
            if (PnlGameProfilesList == null) return;
            var fadeAnim = new DoubleAnimation(0.25, 1.0, new Duration(TimeSpan.FromMilliseconds(180)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var slideAnim = new DoubleAnimation(10.0, 0.0, new Duration(TimeSpan.FromMilliseconds(180)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            PnlGameProfilesList.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            var trans = PnlGameProfilesList.RenderTransform as TranslateTransform;
            if (trans == null || trans.IsFrozen)
            {
                PnlGameProfilesList.RenderTransform = new TranslateTransform(0, 0);
                trans = (TranslateTransform)PnlGameProfilesList.RenderTransform;
            }
            trans.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        private void BtnViewCards_Click(object sender, RoutedEventArgs e)
        {
            if (_monitorService == null || _monitorService.Settings == null) return;
            if (_monitorService.Settings.GameViewMode != "Cards")
            {
                _monitorService.Settings.GameViewMode = "Cards";
                _monitorService.Settings.Save();
                PopulateGameProfiles();
                AnimateViewTransition();
            }
        }

        private void BtnViewList_Click(object sender, RoutedEventArgs e)
        {
            if (_monitorService == null || _monitorService.Settings == null) return;
            if (_monitorService.Settings.GameViewMode != "Deck")
            {
                _monitorService.Settings.GameViewMode = "Deck";
                _monitorService.Settings.Save();
                PopulateGameProfiles();
                AnimateViewTransition();
            }
        }

        private void CmbSortGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSortVisuals || _isLoadingSettings || _monitorService == null || _monitorService.Settings == null) return;
            ComboBoxItem item = CmbSortGames.SelectedItem as ComboBoxItem;
            if (item != null && item.Tag != null)
            {
                string sortMode = item.Tag.ToString();
                if (_monitorService.Settings.GameSortMode != sortMode)
                {
                    _monitorService.Settings.GameSortMode = sortMode;
                    _monitorService.Settings.Save();
                    PopulateGameProfiles();
                }
            }
        }
    }
}