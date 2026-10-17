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
using ZenithOptimizer.UI.Library;

namespace ZenithOptimizer
{
    public partial class MainWindow : Window
    {
        private GameDragDropManager _gameDragDropManager;

        private GameDragDropManager DragDropManager
        {
            get
            {
                if (_gameDragDropManager == null)
                {
                    _gameDragDropManager = new GameDragDropManager(ReorderCustomGames);
                }
                return _gameDragDropManager;
            }
        }

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

            // Filter ONLY games installed on user machine
            string searchQuery = (TxtSearchGames != null && !string.IsNullOrWhiteSpace(TxtSearchGames.Text))
                ? TxtSearchGames.Text.Trim()
                : null;

            int totalInstalledCount;
            var profilesList = GameSearchFilter.FilterProfiles(_monitorService.Profiles, searchQuery, out totalInstalledCount);

            // Sort profiles
            string sortMode = _monitorService.Settings.GameSortMode ?? "Custom";
            GameSearchFilter.SortProfiles(profilesList, sortMode, _monitorService.Settings.CustomGameOrder);

            // Update header count badge with search telemetry
            if (TxtGamesCountBadge != null)
            {
                TxtGamesCountBadge.Text = GameSearchFilter.FormatCountBadge(profilesList.Count, totalInstalledCount, !string.IsNullOrEmpty(searchQuery));
            }

            // Update view switcher visual buttons
            string viewMode = _monitorService.Settings.GameViewMode ?? "Cards";
            UpdateViewSwitcherVisuals(viewMode);
            UpdateSortComboVisuals(sortMode);

            if (profilesList.Count == 0)
            {
                Border emptyCard = GameCardRenderer.CreateEmptyStateCard(
                    searchQuery,
                    FindResource,
                    () => { if (TxtSearchGames != null) TxtSearchGames.Text = string.Empty; },
                    () => { BtnDetectGames_Click(null, null); },
                    () => { BtnAddCustomGame_Click(null, null); });
                PnlGameProfilesList.Children.Add(emptyCard);
                return;
            }

            if (viewMode == "List" || viewMode == "Deck")
            {
                PnlGameProfilesList.Children.Add(GameMetricsDeckRenderer.CreateMetricsMatrixHeader(FindResource));
            }

            // Populate items
            foreach (var profile in profilesList)
            {
                if (viewMode == "List" || viewMode == "Deck")
                {
                    Border row = GameMetricsDeckRenderer.CreateGameProfileMetricsRow(
                        profile,
                        FindResource,
                        AnimateHeroSpotlight,
                        ToggleGameFavorite,
                        HandleGameBoost,
                        BuildGameSpecificToolsPanel,
                        (el, p) => DragDropManager.Attach(el, p));
                    PnlGameProfilesList.Children.Add(row);
                }
                else
                {
                    Border card = GameCardRenderer.CreateGameProfileCard(
                        profile,
                        FindResource,
                        AnimateHeroSpotlight,
                        ToggleGameFavorite,
                        HandleGameBoost,
                        BuildGameSpecificToolsPanel,
                        (el, p) => DragDropManager.Attach(el, p));
                    PnlGameProfilesList.Children.Add(card);
                }
            }
        }

        private void HandleGameBoost(GameProfile p, Button actionBtn)
        {
            string statusMsg;
            bool launched = _monitorService.ForceOptimizeGame(p, true, out statusMsg);
            if (actionBtn != null)
            {
                actionBtn.Content = launched ? "Armed & Active" : "Pre-Armed";
                actionBtn.Style = (Style)FindResource("SecondaryBtn");
            }
            if (TxtFooterStatus != null) TxtFooterStatus.Text = statusMsg;
            if (DashHeroTitle != null) DashHeroTitle.Text = p.Name + (launched ? " (Active & Optimized)" : " (Pre-Armed)");
            if (DashHeroDesc != null) DashHeroDesc.Text = statusMsg;

            ShowNotificationBanner(launched ? string.Format("{0} Active & Optimized", p.Name) : string.Format("{0} Pre-Armed for Low Latency", p.Name), "emerald");
            UpdateEnginePulseBadge(launched ? "Boosted (" + p.Name + ")" : "Pre-Armed (" + p.Name + ")", Color.FromRgb(16, 185, 129));

            var fadeAnim = new DoubleAnimation(0.4, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            if (DashHeroTitle != null) DashHeroTitle.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            if (DashHeroDesc != null) DashHeroDesc.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
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

        private void ReorderCustomGames(string sourceId, string targetId)
        {
            if (_monitorService == null || _monitorService.Settings == null) return;
            string movedName;
            if (GameDragDropManager.Reorder(_monitorService.Settings, _monitorService.Profiles, sourceId, targetId, out movedName))
            {
                PopulateGameProfiles();
                ShowNotificationBanner(string.Format("Reordered: {0}", movedName), "emerald");
            }
        }

        private void ToggleGameFavorite(GameProfile profile)
        {
            if (profile == null || _monitorService == null || _monitorService.Settings == null) return;
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

        private void BtnDetectGames_Click(object sender, RoutedEventArgs e)
        {
            if (PnlDiscoveredList == null || _monitorService == null) return;
            PnlDiscoveredList.Children.Clear();
            var apps = _monitorService.GetRunningUserApplications();

            if (apps.Count == 0)
            {
                TextBlock none = new TextBlock
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
                    Border item = new Border
                    {
                        Background = (SolidColorBrush)FindResource("BgSunken"),
                        BorderBrush = (SolidColorBrush)FindResource("BorderSubtle"),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 0, 0, 6)
                    };

                    Grid g = new Grid();
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    StackPanel sp = new StackPanel();
                    TextBlock nameTxt = new TextBlock
                    {
                        Text = app.WindowTitle,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (SolidColorBrush)FindResource("TextPrimary"),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    TextBlock procTxt = new TextBlock
                    {
                        Text = "Process: " + app.ProcessName + ".exe (PID " + app.ProcessId + ")",
                        FontSize = 10,
                        Foreground = (SolidColorBrush)FindResource("TextMuted")
                    };
                    sp.Children.Add(nameTxt);
                    sp.Children.Add(procTxt);
                    Grid.SetColumn(sp, 0);
                    g.Children.Add(sp);

                    Button addBtn = new Button
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
                        var targetApp = (RunningAppInfo)((Button)btnSender).Tag;
                        _monitorService.AddCustomProfile(targetApp.WindowTitle, targetApp.ProcessName, "User Game");
                        PopulateGameProfiles();
                        if (PnlDiscoveredApps != null) PnlDiscoveredApps.Visibility = Visibility.Collapsed;
                        Log("Added detected game: " + targetApp.WindowTitle + " (" + targetApp.ProcessName + ".exe)");
                    };
                    Grid.SetColumn(addBtn, 1);
                    g.Children.Add(addBtn);

                    item.Child = g;
                    PnlDiscoveredList.Children.Add(item);
                }
            }

            if (PnlDiscoveredApps != null) PnlDiscoveredApps.Visibility = Visibility.Visible;
        }

        private void BtnCloseDiscovered_Click(object sender, RoutedEventArgs e)
        {
            if (PnlDiscoveredApps != null) PnlDiscoveredApps.Visibility = Visibility.Collapsed;
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

    }
}