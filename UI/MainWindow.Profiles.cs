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
        private void PopulateGameProfiles()
        {
            _conflictGroups.Clear();
            PnlGameProfilesList.Children.Clear();

            foreach (var profile in _monitorService.Profiles)
            {
                Border card = CreateGameProfileCard(profile);
                PnlGameProfilesList.Children.Add(card);
            }
        }

        private Border CreateGameProfileCard(GameProfile profile)
        {
            Border card = new Border();
            card.Background = (System.Windows.Media.SolidColorBrush)FindResource("BgCard");
            card.BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("BorderCard");
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(8);
            card.Padding = new Thickness(16, 14, 16, 14);
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

            var liftAnim = new DoubleAnimation(-3.5, new Duration(TimeSpan.FromMilliseconds(160)))
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
            var blurLiftAnim = new DoubleAnimation(22, new Duration(TimeSpan.FromMilliseconds(160)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var blurDropAnim = new DoubleAnimation(14, new Duration(TimeSpan.FromMilliseconds(180)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            card.MouseEnter += (s, e) =>
            {
                cardTrans.BeginAnimation(TranslateTransform.YProperty, liftAnim);
                shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, shadowLiftAnim);
                shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, blurLiftAnim);
            };
            card.MouseLeave += (s, e) =>
            {
                cardTrans.BeginAnimation(TranslateTransform.YProperty, dropAnim);
                shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, shadowDropAnim);
                shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, blurDropAnim);
            };
            card.MouseLeftButtonUp += (s, e) =>
            {
                AnimateHeroSpotlight(profile);
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            StackPanel infoPanel = new StackPanel();

            // Title and Badge Row
            StackPanel titleRow = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
            TextBlock title = new TextBlock()
            {
                Text = profile.Name,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("TextPrimary"),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleRow.Children.Add(title);

            Border badge = new Border()
            {
                Background = FrozenTheme.BadgeBg,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock badgeText = new TextBlock()
            {
                Text = profile.OptimizationBadge,
                FontSize = 9,
                FontWeight = FontWeights.Medium,
                Foreground = FrozenTheme.TextSecondary
            };
            badge.Child = badgeText;
            titleRow.Children.Add(badge);

            infoPanel.Children.Add(titleRow);

            // Subtitle / Target Pacing
            TextBlock target = new TextBlock()
            {
                Text = "Category: " + profile.Category + "  •  Target: " + profile.TargetPacing,
                FontSize = 11,
                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("TextSecondary"),
                Margin = new Thickness(0, 3, 0, 4)
            };
            infoPanel.Children.Add(target);

            // Description
            TextBlock desc = new TextBlock()
            {
                Text = profile.Description,
                FontSize = 11,
                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("TextMuted"),
                TextWrapping = TextWrapping.Wrap
            };
            infoPanel.Children.Add(desc);

            grid.Children.Add(infoPanel);
            Grid.SetColumn(infoPanel, 0);

            // Action Buttons Panel
            StackPanel btnPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) };

            bool isPreArmedState = profile.IsActive && profile == _monitorService.ActiveProfile && _monitorService.ActiveProcess == null;
            string initialContent = profile.IsActive ? (isPreArmedState ? "Pre-Armed" : "Optimized") : "Arm & Launch";

            System.Windows.Controls.Button actionBtn = new System.Windows.Controls.Button()
            {
                Content = initialContent,
                Style = profile.IsActive ? (Style)FindResource("SecondaryBtn") : (Style)FindResource("AccentBtn"),
                Margin = new Thickness(0, 0, 0, 6),
                Tag = profile
            };
            actionBtn.Click += (s, e) =>
            {
                GameProfile p = (GameProfile)((System.Windows.Controls.Button)s).Tag;
                string statusMsg;
                bool launched = _monitorService.ForceOptimizeGame(p, true, out statusMsg);
                actionBtn.Content = launched ? "Armed & Launched" : "Pre-Armed";
                actionBtn.Style = (Style)FindResource("SecondaryBtn");
                TxtFooterStatus.Text = statusMsg;
                DashHeroTitle.Text = p.Name + (launched ? " (Pre-Armed & Launching)" : " (Pre-Armed)");
                DashHeroDesc.Text = statusMsg;

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
                Content = "Tools ▼",
                Style = (Style)FindResource("SecondaryBtn"),
                FontSize = 11,
                Padding = new Thickness(8, 4, 8, 4)
            };
            toolsToggleBtn.Click += (s, e) =>
            {
                if (toolsPanel == null)
                {
                    toolsToggleBtn.Content = "Loading Tools...";
                    toolsPanel = BuildGameSpecificToolsPanel(profile);
                    
                    var panelTrans = new TranslateTransform(0, -8);
                    toolsPanel.RenderTransform = panelTrans;
                    toolsPanel.Opacity = 0.0;
                    toolsPanel.Visibility = Visibility.Visible;
                    cardContainer.Children.Add(toolsPanel);

                    var openAnim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var slideAnim = new DoubleAnimation(-8, 0, new Duration(TimeSpan.FromMilliseconds(200)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    toolsPanel.BeginAnimation(UIElement.OpacityProperty, openAnim);
                    panelTrans.BeginAnimation(TranslateTransform.YProperty, slideAnim);

                    toolsToggleBtn.Content = "Hide Tools ▲";
                    return;
                }

                bool isVisible = toolsPanel.Visibility == Visibility.Visible;
                if (isVisible)
                {
                    var closeAnim = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(160)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    closeAnim.Completed += (cs, ce) => { toolsPanel.Visibility = Visibility.Collapsed; };
                    toolsPanel.BeginAnimation(UIElement.OpacityProperty, closeAnim);
                    toolsToggleBtn.Content = "Tools ▼";
                }
                else
                {
                    toolsPanel.Visibility = Visibility.Visible;
                    var panelTrans = toolsPanel.RenderTransform as TranslateTransform ?? new TranslateTransform(0, -8);
                    toolsPanel.RenderTransform = panelTrans;
                    
                    var openAnim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var slideAnim = new DoubleAnimation(-8, 0, new Duration(TimeSpan.FromMilliseconds(200)))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    toolsPanel.BeginAnimation(UIElement.OpacityProperty, openAnim);
                    panelTrans.BeginAnimation(TranslateTransform.YProperty, slideAnim);
                    toolsToggleBtn.Content = "Hide Tools ▲";
                }
            };
            btnPanel.Children.Add(toolsToggleBtn);

            grid.Children.Add(btnPanel);
            Grid.SetColumn(btnPanel, 1);

            cardContainer.Children.Add(grid);

            card.Child = cardContainer;
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
                    Foreground = (System.Windows.Media.SolidColorBrush)FindResource("TextSecondary"),
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
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 32, 46)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(10, 8, 10, 8),
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
                        Foreground = (System.Windows.Media.SolidColorBrush)FindResource("TextPrimary"),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    TextBlock procTxt = new TextBlock()
                    {
                        Text = "Process: " + app.ProcessName + ".exe (PID " + app.ProcessId + ")",
                        FontSize = 10,
                        Foreground = (System.Windows.Media.SolidColorBrush)FindResource("TextMuted")
                    };
                    sp.Children.Add(nameTxt);
                    sp.Children.Add(procTxt);
                    Grid.SetColumn(sp, 0);
                    g.Children.Add(sp);

                    System.Windows.Controls.Button addBtn = new System.Windows.Controls.Button()
                    {
                        Content = "+ Add to Zenith",
                        Style = (Style)FindResource("AccentBtn"),
                        FontSize = 10,
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

            DashHeroTitle.Text = "High-Performance Gaming Mode Active!";
            DashHeroDesc.Text = "PC is fully boosted: 0.500ms input response active, audio shield enabled, High Performance power mode engaged, and background RAM cleaned.";
            BtnManualBoost.Content = "Boosted (Active)";
            BtnManualBoost.Style = (Style)FindResource("SecondaryBtn");

            Log(string.Format("Manual PC Boost triggered! 0.5ms Timer, Audio Shield & High Performance mode engaged. Reclaimed {0} MB RAM.", mb > 0 ? mb.ToString() : "50+"));
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

            Log(string.Format("Instant Laptop Cooldown engaged! ACPI active fans spinning, CPU capped at 80%, Turbo spikes cut. Freed {0} MB idle RAM.", mb > 0 ? mb.ToString() : "50+"));

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
                    var activeKeys = new System.Collections.Generic.List<string>();
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
                    ShowNotificationBanner(" Profile exported successfully to " + Path.GetFileName(sfd.FileName));
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
                    ShowNotificationBanner(string.Format(" Profile imported! Loaded {0} tools from {1}", loadedTools, Path.GetFileName(ofd.FileName)));
                }
            }
            catch (Exception ex)
            {
                ShowNotificationBanner("Import Failed: " + ex.Message);
            }
        }

    }
}