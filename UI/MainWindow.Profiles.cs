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
    }
}