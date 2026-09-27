using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer
{
    public partial class MlbbOverlayWindow : Window
    {
        private readonly MlbbMetaService _metaService;
        private readonly MlbbRecommendationEngine _engine;
        private readonly MlbbScreenReader _screenReader;
        private readonly MlbbWindowSnapper _snapper;
        private string _activeRoleFilter = "All";
        private MlbbHero _currentSearchHero = null;

        // Fallback Brushes (Ensures zero runtime exceptions if resource keys drift)
        private static readonly Brush FallbackCyan = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        private static readonly Brush FallbackEmerald = new SolidColorBrush(Color.FromRgb(52, 211, 153));
        private static readonly Brush FallbackCoral = new SolidColorBrush(Color.FromRgb(248, 113, 113));
        private static readonly Brush FallbackAmber = new SolidColorBrush(Color.FromRgb(251, 191, 36));
        private static readonly Brush FallbackMuted = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private static readonly Brush FallbackBorder = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
        private static readonly Brush FallbackSlotEmpty = new SolidColorBrush(Color.FromArgb(77, 18, 20, 31));

        public MlbbOverlayWindow()
        {
            InitializeComponent();

            _metaService = MlbbMetaService.Instance;
            _engine = new MlbbRecommendationEngine(_metaService);
            _screenReader = new MlbbScreenReader(_metaService, _engine);
            _snapper = new MlbbWindowSnapper(this);

            _screenReader.OnDraftUpdated += UpdateDraftUI;
            _snapper.OnAttachmentChanged += Snapper_OnAttachmentChanged;

            Loaded += MlbbOverlayWindow_Loaded;
            Closing += MlbbOverlayWindow_Closing;
        }

        private Brush SafeBrush(string key, Brush fallback)
        {
            try
            {
                var brush = TryFindResource(key) as Brush;
                if (brush != null) return brush;
            }
            catch { }
            return fallback;
        }

        private void MlbbOverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _snapper.Start();
            _screenReader.StartAutoScan();
            _screenReader.Recalculate();
            PopulateQuickSuggestions();
            PopulateMetaBans();
        }

        private void MlbbOverlayWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _snapper.Stop();
            _screenReader.StopAutoScan();
        }

        private void Snapper_OnAttachmentChanged(bool attached)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                StatusDot.Background = attached
                    ? SafeBrush("AppleEmerald", FallbackEmerald)
                    : SafeBrush("AppleAmber", FallbackAmber);

                StatusDot.ToolTip = attached
                    ? "Attached to BlueStacks (HD-Player.exe)"
                    : "Searching for BlueStacks window...";

                if (attached)
                {
                    _screenReader.SetTargetWindow(_snapper.BlueStacksHwnd);
                }
            }));
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnFlipDock_Click(object sender, RoutedEventArgs e)
        {
            _snapper.CurrentDock = _snapper.CurrentDock == DockEdge.Right
                ? DockEdge.Left
                : DockEdge.Right;

            TxtPillArrow.Text = _snapper.CurrentDock == DockEdge.Right ? "‹" : "›";
            _snapper.ForceAlign();
        }

        private void BtnCollapse_Click(object sender, RoutedEventArgs e)
        {
            CardFullHud.Visibility = Visibility.Collapsed;
            CardMiniPill.Visibility = Visibility.Visible;
            this.Width = 24;
            _snapper.IsCollapsed = true;
            _snapper.ForceAlign();
        }

        private void CardMiniPill_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CardMiniPill.Visibility = Visibility.Collapsed;
            CardFullHud.Visibility = Visibility.Visible;
            this.Width = 315;
            _snapper.IsCollapsed = false;
            _snapper.ForceAlign();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public void UpdateDraftUI(MlbbDraftState draft)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    // 1. Live Win% Advantage Bar and Rating
                    double allyChance = draft.AllyWinChance > 0 ? draft.AllyWinChance : 50.0;
                    double enemyChance = draft.EnemyWinChance > 0 ? draft.EnemyWinChance : 50.0;
                    TxtAllyWinChance.Text = string.Format("{0:F1}%", allyChance);
                    TxtEnemyWinChance.Text = string.Format("{0:F1}%", enemyChance);

                    ColAllyAdvantage.Width = new GridLength(Math.Max(10, allyChance), GridUnitType.Star);
                    ColEnemyAdvantage.Width = new GridLength(Math.Max(10, enemyChance), GridUnitType.Star);

                    if (allyChance >= 54.0)
                    {
                        TxtDraftAdvantage.Text = string.Format("ALLY ADVANTAGE (+{0:F1}%)", allyChance - 50.0);
                        TxtDraftAdvantage.Foreground = SafeBrush("AppleEmerald", FallbackEmerald);
                    }
                    else if (enemyChance >= 54.0)
                    {
                        TxtDraftAdvantage.Text = string.Format("ENEMY THREAT (+{0:F1}%)", enemyChance - 50.0);
                        TxtDraftAdvantage.Foreground = SafeBrush("AppleCoral", FallbackCoral);
                    }
                    else
                    {
                        TxtDraftAdvantage.Text = "EVEN DRAFT";
                        TxtDraftAdvantage.Foreground = SafeBrush("TextMuted", FallbackMuted);
                    }

                    // 2. Role Status Banner
                    if (draft.MissingAllyRoles != null && draft.MissingAllyRoles.Count > 0)
                    {
                        TxtRoleStatus.Text = "Missing: " + string.Join(" • ", draft.MissingAllyRoles.ToArray());
                        TxtRoleStatus.Foreground = SafeBrush("AppleAmber", FallbackAmber);
                        RoleAlertBorder.Background = new SolidColorBrush(Color.FromArgb(40, 251, 191, 36));
                    }
                    else
                    {
                        TxtRoleStatus.Text = "Team Composition: Balanced";
                        TxtRoleStatus.Foreground = SafeBrush("AppleEmerald", FallbackEmerald);
                        RoleAlertBorder.Background = new SolidColorBrush(Color.FromArgb(40, 52, 211, 153));
                    }

                    // 3. Render 5 Ally Slots
                    PanelAllySlots.Children.Clear();
                    for (int i = 0; i < 5; i++)
                    {
                        string pick = (i < draft.AllyPicks.Count) ? draft.AllyPicks[i] : null;
                        PanelAllySlots.Children.Add(CreateDraftSlot(pick, isAlly: true, slotIndex: i + 1));
                    }

                    // 4. Render 5 Enemy Slots
                    PanelEnemySlots.Children.Clear();
                    for (int i = 0; i < 5; i++)
                    {
                        string pick = (i < draft.EnemyPicks.Count) ? draft.EnemyPicks[i] : null;
                        PanelEnemySlots.Children.Add(CreateDraftSlot(pick, isAlly: false, slotIndex: i + 1));
                    }

                    // 5. Render Ban Tokens
                    PanelBanSlots.Children.Clear();
                    if (draft.BannedHeroes.Count == 0)
                    {
                        PanelBanSlots.Children.Add(new TextBlock
                        {
                            Text = "None",
                            FontSize = 8,
                            Foreground = SafeBrush("TextMuted", FallbackMuted),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                    }
                    else
                    {
                        foreach (string ban in draft.BannedHeroes)
                        {
                            PanelBanSlots.Children.Add(CreateBanChip(ban));
                        }
                    }

                    // 6. Recommendations List
                    ItemsRecommendations.ItemsSource = null;
                    ItemsRecommendations.ItemsSource = draft.Recommendations;

                    // Refresh quick ban suggestions and quick picks
                    PopulateQuickSuggestions();
                    PopulateMetaBans();
                }
                catch { }
            }));
        }

        private Border CreateDraftSlot(string heroName, bool isAlly, int slotIndex)
        {
            bool hasPick = !string.IsNullOrEmpty(heroName);
            Brush accentColor = isAlly
                ? SafeBrush("AppleCyan", FallbackCyan)
                : SafeBrush("AppleCoral", FallbackCoral);

            var border = new Border
            {
                Background = hasPick
                    ? new SolidColorBrush(Color.FromArgb(50, 30, 35, 50))
                    : SafeBrush("GlassSlotEmpty", FallbackSlotEmpty),
                BorderBrush = hasPick ? accentColor : SafeBrush("BorderGlassSubtle", FallbackBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(0, 0, 3, 3),
                Cursor = hasPick ? Cursors.Hand : Cursors.Arrow,
                ToolTip = hasPick ? string.Format("{0} (Click to remove)", heroName) : string.Format("Slot {0} (Empty)", slotIndex)
            };

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            if (hasPick)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = heroName,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 3, 0)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = "✕",
                    FontSize = 7,
                    Foreground = SafeBrush("TextMuted", FallbackMuted),
                    VerticalAlignment = VerticalAlignment.Center
                });

                string captured = heroName;
                border.MouseLeftButtonDown += (s, e) =>
                {
                    if (isAlly)
                        _screenReader.RemoveAllyPick(captured);
                    else
                        _screenReader.RemoveEnemyPick(captured);
                };
            }
            else
            {
                sp.Children.Add(new TextBlock
                {
                    Text = string.Format("+ {0}", slotIndex),
                    FontSize = 8,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = SafeBrush("TextMuted", FallbackMuted)
                });
            }

            border.Child = sp;
            return border;
        }

        private Border CreateBanChip(string heroName)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 248, 113, 113)),
                BorderBrush = SafeBrush("BorderGlassSubtle", FallbackBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(0, 0, 3, 2),
                Cursor = Cursors.Hand,
                ToolTip = "Click to remove ban"
            };

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = heroName,
                FontSize = 8,
                Foreground = SafeBrush("TextMuted", FallbackMuted),
                Margin = new Thickness(0, 0, 3, 0)
            });

            sp.Children.Add(new TextBlock
            {
                Text = "✕",
                FontSize = 7,
                Foreground = SafeBrush("TextMuted", FallbackMuted)
            });

            border.Child = sp;
            string capturedName = heroName;
            border.MouseLeftButtonDown += (s, e) => _screenReader.RemoveBan(capturedName);

            return border;
        }

        private void PopulateMetaBans()
        {
            if (WrapMetaBans == null) return;
            WrapMetaBans.Children.Clear();

            var draft = _screenReader.CurrentDraft;
            var taken = new HashSet<string>(
                draft.AllyPicks.Concat(draft.EnemyPicks).Concat(draft.BannedHeroes),
                StringComparer.OrdinalIgnoreCase
            );

            // Priority meta tournament bans
            string[] topPriorityBans = new string[] { "Suyou", "Zhuxin", "Ling", "Fanny", "Chip", "Phoveus", "Hayabusa" };

            foreach (var banCandidate in topPriorityBans)
            {
                if (taken.Contains(banCandidate)) continue;

                var chip = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 248, 113, 113)),
                    BorderBrush = SafeBrush("BorderGlassSubtle", FallbackBorder),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(0, 0, 3, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = string.Format("Click to Ban {0}", banCandidate)
                };

                chip.Child = new TextBlock
                {
                    Text = banCandidate,
                    FontSize = 7,
                    FontWeight = FontWeights.Bold,
                    Foreground = SafeBrush("AppleCoral", FallbackCoral)
                };

                string heroToBan = banCandidate;
                chip.MouseLeftButtonDown += (s, e) =>
                {
                    _screenReader.AddBan(heroToBan);
                };

                WrapMetaBans.Children.Add(chip);
                if (WrapMetaBans.Children.Count >= 5) break;
            }
        }

        private void TxtHeroSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtHeroSearch != null ? TxtHeroSearch.Text.Trim() : "";

            if (BtnClearSearch != null)
            {
                BtnClearSearch.Visibility = string.IsNullOrEmpty(query) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (string.IsNullOrEmpty(query))
            {
                _currentSearchHero = null;
                CardSearchInspector.Visibility = Visibility.Collapsed;
                PanelDefaultRecs.Visibility = Visibility.Visible;
                PopulateQuickSuggestions();
                return;
            }

            // Find matching hero
            var match = _metaService.FindHero(query);
            if (match == null)
            {
                match = _metaService.Heroes.FirstOrDefault(h => h.Name.ToLowerInvariant().StartsWith(query.ToLowerInvariant()));
            }
            if (match == null)
            {
                match = _metaService.Heroes.FirstOrDefault(h => h.Name.ToLowerInvariant().Contains(query.ToLowerInvariant()));
            }

            if (match != null)
            {
                _currentSearchHero = match;
                CardSearchInspector.Visibility = Visibility.Visible;
                PanelDefaultRecs.Visibility = Visibility.Collapsed;

                TxtSearchHeroName.Text = match.Name;
                TxtSearchHeroTier.Text = match.MplTier;
                TxtSearchHeroRole.Text = string.Format("{0} • {1}", match.PrimaryRole, match.Lane);
                TxtSearchHeroWinRate.Text = string.Format("{0:F1}% WR", match.BaseWinRate);
                TxtSearchHeroBanRate.Text = string.Format("{0:F0}% Ban", match.BanRate);

                var countersList = match.Counters.Take(3).Select(c => c.HeroName).ToList();
                TxtSearchCounters.Text = countersList.Count > 0
                    ? "Hard counters: " + string.Join(", ", countersList.ToArray())
                    : "Hard counters: High-priority meta pick";

                var weakList = match.CounteredBy.Take(3).Select(c => c.HeroName).ToList();
                TxtSearchCounteredBy.Text = weakList.Count > 0
                    ? "Countered by: " + string.Join(", ", weakList.ToArray())
                    : "Countered by: Coordinated group CC / burst";
            }
            else
            {
                _currentSearchHero = null;
                CardSearchInspector.Visibility = Visibility.Collapsed;
                PanelDefaultRecs.Visibility = Visibility.Visible;
            }

            PopulateQuickSuggestions();
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (TxtHeroSearch != null)
            {
                TxtHeroSearch.Clear();
            }
        }

        private void PopulateQuickSuggestions()
        {
            if (WrapQuickSuggestions == null) return;

            WrapQuickSuggestions.Children.Clear();
            string query = TxtHeroSearch != null ? TxtHeroSearch.Text.Trim().ToLowerInvariant() : "";

            var draft = _screenReader.CurrentDraft;
            var taken = new HashSet<string>(
                draft.AllyPicks.Concat(draft.EnemyPicks).Concat(draft.BannedHeroes),
                StringComparer.OrdinalIgnoreCase
            );

            var heroes = _metaService.Heroes
                .Where(h => !taken.Contains(h.Name))
                .Where(h => _activeRoleFilter == "All" || h.PrimaryRole == _activeRoleFilter || h.SecondaryRole == _activeRoleFilter || h.Lane == _activeRoleFilter)
                .Where(h => string.IsNullOrEmpty(query) || h.Name.ToLowerInvariant().Contains(query))
                .Take(8);

            foreach (var h in heroes)
            {
                var pill = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    BorderBrush = SafeBrush("BorderGlassSubtle", FallbackBorder),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(0, 0, 3, 3),
                    Cursor = Cursors.Hand,
                    ToolTip = string.Format("{0} ({1} • {2})\nLeft Click: +Ally | Right Click: +Enemy", h.Name, h.PrimaryRole, h.Lane)
                };

                var tb = new TextBlock
                {
                    Text = h.Name,
                    FontSize = 8,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                };
                pill.Child = tb;

                string heroName = h.Name;
                pill.MouseLeftButtonDown += (s, e) =>
                {
                    _screenReader.AddAllyPick(heroName);
                    if (TxtHeroSearch != null) TxtHeroSearch.Clear();
                };

                pill.MouseRightButtonDown += (s, e) =>
                {
                    _screenReader.AddEnemyPick(heroName);
                    if (TxtHeroSearch != null) TxtHeroSearch.Clear();
                };

                WrapQuickSuggestions.Children.Add(pill);
            }
        }

        private void BtnSearchPickAlly_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSearchHero != null)
            {
                _screenReader.AddAllyPick(_currentSearchHero.Name);
                TxtHeroSearch.Clear();
            }
            else if (!string.IsNullOrWhiteSpace(TxtHeroSearch.Text))
            {
                _screenReader.AddAllyPick(TxtHeroSearch.Text.Trim());
                TxtHeroSearch.Clear();
            }
        }

        private void BtnSearchPickEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSearchHero != null)
            {
                _screenReader.AddEnemyPick(_currentSearchHero.Name);
                TxtHeroSearch.Clear();
            }
            else if (!string.IsNullOrWhiteSpace(TxtHeroSearch.Text))
            {
                _screenReader.AddEnemyPick(TxtHeroSearch.Text.Trim());
                TxtHeroSearch.Clear();
            }
        }

        private void BtnSearchBan_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSearchHero != null)
            {
                _screenReader.AddBan(_currentSearchHero.Name);
                TxtHeroSearch.Clear();
            }
            else if (!string.IsNullOrWhiteSpace(TxtHeroSearch.Text))
            {
                _screenReader.AddBan(TxtHeroSearch.Text.Trim());
                TxtHeroSearch.Clear();
            }
        }

        private void BtnPickAlly_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                string name = btn.Tag as string;
                if (!string.IsNullOrEmpty(name))
                {
                    _screenReader.AddAllyPick(name);
                }
            }
        }

        private void BtnPickEnemy_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                string name = btn.Tag as string;
                if (!string.IsNullOrEmpty(name))
                {
                    _screenReader.AddEnemyPick(name);
                }
            }
        }

        private void BtnAddAlly_Click(object sender, RoutedEventArgs e)
        {
            BtnSearchPickAlly_Click(sender, e);
        }

        private void BtnAddEnemy_Click(object sender, RoutedEventArgs e)
        {
            BtnSearchPickEnemy_Click(sender, e);
        }

        private void BtnAddBan_Click(object sender, RoutedEventArgs e)
        {
            BtnSearchBan_Click(sender, e);
        }

        private void BtnFirstPickAlly_Click(object sender, RoutedEventArgs e)
        {
            _screenReader.SetFirstPickTeam("Ally");
            TxtFirstPickStatus.Text = "Ally Priority";
            TxtFirstPickStatus.Foreground = SafeBrush("AppleCyan", FallbackCyan);
            BtnFirstPickAlly.BorderBrush = SafeBrush("AppleCyan", FallbackCyan);
            BtnFirstPickEnemy.BorderBrush = SafeBrush("BorderGlassSubtle", FallbackBorder);
        }

        private void BtnFirstPickEnemy_Click(object sender, RoutedEventArgs e)
        {
            _screenReader.SetFirstPickTeam("Enemy");
            TxtFirstPickStatus.Text = "Enemy Priority";
            TxtFirstPickStatus.Foreground = SafeBrush("AppleCoral", FallbackCoral);
            BtnFirstPickEnemy.BorderBrush = SafeBrush("AppleCoral", FallbackCoral);
            BtnFirstPickAlly.BorderBrush = SafeBrush("BorderGlassSubtle", FallbackBorder);
        }

        private void MyRole_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string selectedRole = btn.Tag.ToString();
                _screenReader.SetMyRole(selectedRole);
                if (TxtRecHeader != null)
                {
                    TxtRecHeader.Text = selectedRole == "All"
                        ? "OPTIMAL COUNTER PICKS"
                        : string.Format("BEST {0} PICKS", selectedRole.ToUpperInvariant());
                }
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _screenReader.ResetDraft();
            if (TxtHeroSearch != null) TxtHeroSearch.Clear();
        }

        private void TxtHeroSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSearchPickAlly_Click(sender, e);
            }
        }
    }
}
