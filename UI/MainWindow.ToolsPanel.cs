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
                Margin = new Thickness(0, 8, 0, 4),
                Tag = "SectionHeader"
            };
        }

        private class ConflictItem
        {
            public string Label;
            public Func<bool> IsActive;
            public Action Deactivate;
        }

        private static readonly Dictionary<string, List<ConflictItem>> _conflictGroups = new Dictionary<string, List<ConflictItem>>();

        private static string GetFriendlyConflictGroupName(string group)
        {
            if (string.IsNullOrEmpty(group)) return string.Empty;
            switch (group.ToLowerInvariant())
            {
                case "bst_fps": return "FPS Cap";
                case "bst_renderer": return "Graphics Engine";
                case "bst_resolution": return "Resolution";
                case "bst_device": return "Phone Profile";
                case "roblox_fps": return "FPS Cap";
                case "roblox_fov": return "Field of View";
                case "roblox_lighting": return "Lighting Mode";
                case "roblox_textures": return "Texture Quality";
                case "lol_fps": return "FPS Target";
                case "lol_minimap_pos": return "Minimap Position";
                case "lol_renderer": return "DirectX Version";
                case "valo_pointer": return "Mouse Tracking";
                case "valo_power": return "Power State";
                case "tekken_vsync": return "Frame Sync";
                case "tekken_framerate": return "FPS Window";
                case "tekken_directx": return "DirectX API";
                case "tekken_dof": return "Depth of Field";
                case "tekken_res": return "Display Mode";
                default: return group.Replace('_', ' ').ToUpper();
            }
        }

        private Border CreateConflictCallout(string groupName, string description)
        {
            Border callout = new Border()
            {
                Background = FrozenTheme.CalloutBg,
                BorderBrush = FrozenTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 4, 0, 8)
            };
            TextBlock text = new TextBlock()
            {
                Text = "Exclusive [" + groupName + "]: " + description,
                FontSize = 10,
                FontWeight = FontWeights.Medium,
                Foreground = FrozenTheme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            callout.Child = text;
            return callout;
        }

        private System.Windows.Controls.Button CreateToolButton(
            string label, 
            Action onClick, 
            bool isAccent = false, 
            string tooltip = null, 
            Func<bool> isInitiallyActive = null,
            Action onDeactivate = null,
            string conflictGroup = null)
        {
            string friendlyConflict = GetFriendlyConflictGroupName(conflictGroup);
            string baseLabel = !string.IsNullOrEmpty(friendlyConflict) && !label.StartsWith("[")
                ? ("[" + friendlyConflict + "] " + label)
                : label;

            var btn = new System.Windows.Controls.Button()
            {
                Content = baseLabel,
                Style = (Style)FindResource(isAccent ? "AccentBtn" : "SecondaryBtn"),
                FontSize = 10,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand
            };

            if (!string.IsNullOrEmpty(conflictGroup))
            {
                btn.BorderBrush = FrozenTheme.BorderStrong;
                btn.BorderThickness = new Thickness(1);
            }

            string resolvedTooltip = tooltip ?? "";
            if (!string.IsNullOrEmpty(conflictGroup))
            {
                string note = " [Exclusive setting in '" + friendlyConflict + "']";
                resolvedTooltip = string.IsNullOrEmpty(resolvedTooltip) ? note.Trim() : (resolvedTooltip + note);
            }
            if (!string.IsNullOrEmpty(resolvedTooltip)) btn.ToolTip = resolvedTooltip;

            bool isActive = false;

            Action applyInactiveVisuals = () =>
            {
                btn.Content = baseLabel;
                btn.Style = (Style)FindResource(isAccent ? "AccentBtn" : "SecondaryBtn");
                btn.ClearValue(System.Windows.Controls.Button.BackgroundProperty);
                btn.ClearValue(System.Windows.Controls.Button.ForegroundProperty);
                if (!string.IsNullOrEmpty(conflictGroup))
                {
                    btn.BorderBrush = FrozenTheme.BorderStrong;
                    btn.BorderThickness = new Thickness(1);
                }
                else
                {
                    btn.ClearValue(System.Windows.Controls.Button.BorderBrushProperty);
                }
                btn.FontWeight = FontWeights.Normal;
                isActive = false;
                _toolActiveStates[baseLabel] = false;
                UpdateLatencyReductionBadge();
            };

            Action applyActiveVisuals = () =>
            {
                btn.Content = "[Active] " + baseLabel;
                btn.Background = FrozenTheme.ActiveBtnBg;
                btn.BorderBrush = FrozenTheme.ActiveBtnBorder;
                btn.BorderThickness = new Thickness(1);
                btn.Foreground = FrozenTheme.ActiveBtnFg;
                btn.FontWeight = FontWeights.SemiBold;
                isActive = true;
                _toolActiveStates[baseLabel] = true;
                UpdateLatencyReductionBadge();
            };

            if (!string.IsNullOrEmpty(conflictGroup))
            {
                if (!_conflictGroups.ContainsKey(conflictGroup))
                {
                    _conflictGroups[conflictGroup] = new List<ConflictItem>();
                }
                _conflictGroups[conflictGroup].Add(new ConflictItem()
                {
                    Label = baseLabel,
                    IsActive = () => isActive,
                    Deactivate = () =>
                    {
                        if (isActive)
                        {
                            if (onDeactivate != null)
                            {
                                try { onDeactivate(); } catch { }
                            }
                            applyInactiveVisuals();
                        }
                    }
                });
            }

            try
            {
                if (_toolActiveStates.ContainsKey(baseLabel) && _toolActiveStates[baseLabel])
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
                    if (isActive)
                    {
                        if (onDeactivate != null)
                        {
                            onDeactivate();
                        }
                        applyInactiveVisuals();
                        ShowNotificationBanner(" Deactivated: " + baseLabel + " (Defaults Restored)");
                    }
                    else
                    {
                        string replacedSetting = null;
                        if (!string.IsNullOrEmpty(conflictGroup) && _conflictGroups.ContainsKey(conflictGroup))
                        {
                            foreach (var item in _conflictGroups[conflictGroup])
                            {
                                if (item.IsActive != null && item.IsActive())
                                {
                                    try
                                    {
                                        item.Deactivate();
                                        replacedSetting = item.Label;
                                    }
                                    catch { }
                                }
                            }
                        }

                        onClick();
                        applyActiveVisuals();
                        if (!string.IsNullOrEmpty(replacedSetting))
                        {
                            ShowNotificationBanner(" Activated: " + baseLabel + "  (Deactivated conflicting: " + replacedSetting + ")");
                        }
                        else
                        {
                            ShowNotificationBanner(" Activated: " + baseLabel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    btn.Content = "" + baseLabel;
                    btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113));
                    ShowNotificationBanner(" Notice: " + ex.Message);
                }
            };
            return btn;
        }


        private void SetupToolsPanelHeaderAndAccordion(StackPanel content, GameProfile profile)
        {
            var sectionPairs = new List<Tuple<Border, WrapPanel, string>>();

            // 1. Transform Section Headers into interactive Accordions
            for (int i = 0; i < content.Children.Count; i++)
            {
                var tb = content.Children[i] as TextBlock;
                if (tb != null && tb.Tag as string == "SectionHeader")
                {
                    string headerText = tb.Text;
                    WrapPanel wp = null;
                    for (int j = i + 1; j < content.Children.Count; j++)
                    {
                        if (content.Children[j] is WrapPanel)
                        {
                            wp = content.Children[j] as WrapPanel;
                            break;
                        }
                        if (content.Children[j] is TextBlock && ((TextBlock)content.Children[j]).Tag as string == "SectionHeader")
                        {
                            break;
                        }
                    }

                    if (wp != null)
                    {
                        int toolCount = wp.Children.Count;

                        Border accordionHeader = new Border()
                        {
                            Background = FrozenTheme.AccordionHeaderBg,
                            BorderBrush = FrozenTheme.BorderSubtle,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(10, 6, 10, 6),
                            Margin = new Thickness(0, 8, 0, 4),
                            Cursor = Cursors.Hand,
                            Tag = "AccordionHeader"
                        };
                        accordionHeader.MouseEnter += (s, e) => accordionHeader.Background = FrozenTheme.AccordionHeaderHover;
                        accordionHeader.MouseLeave += (s, e) => accordionHeader.Background = FrozenTheme.AccordionHeaderBg;

                        Grid hGrid = new Grid();
                        hGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        hGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                        StackPanel leftStack = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                        TextBlock arrow = new TextBlock()
                        {
                            Text = "▼ ",
                            Foreground = FrozenTheme.TextSecondary,
                            FontWeight = FontWeights.Bold,
                            FontSize = 10,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        TextBlock title = new TextBlock()
                        {
                            Text = headerText,
                            Foreground = FrozenTheme.TextPrimary,
                            FontWeight = FontWeights.Bold,
                            FontSize = 10,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        leftStack.Children.Add(arrow);
                        leftStack.Children.Add(title);

                        Border badge = new Border()
                        {
                            Background = FrozenTheme.BadgeBg,
                            CornerRadius = new CornerRadius(10),
                            Padding = new Thickness(8, 2, 8, 2),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        TextBlock badgeText = new TextBlock()
                        {
                            Text = string.Format("{0} Tools", toolCount),
                            FontSize = 9,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = FrozenTheme.TextSecondary
                        };
                        badge.Child = badgeText;

                        hGrid.Children.Add(leftStack);
                        Grid.SetColumn(leftStack, 0);
                        hGrid.Children.Add(badge);
                        Grid.SetColumn(badge, 1);

                        accordionHeader.Child = hGrid;

                        // Click to toggle accordion
                        WrapPanel targetWp = wp;
                        accordionHeader.MouseLeftButtonDown += (s, e) =>
                        {
                            bool isVis = targetWp.Visibility == Visibility.Visible;
                            targetWp.Visibility = isVis ? Visibility.Collapsed : Visibility.Visible;
                            arrow.Text = isVis ? "▶ " : "▼ ";
                        };

                        content.Children.RemoveAt(i);
                        content.Children.Insert(i, accordionHeader);

                        sectionPairs.Add(new Tuple<Border, WrapPanel, string>(accordionHeader, wp, headerText));
                    }
                }
            }

            // 2. Build the Search and Category Filter Bar
            Border searchBarCard = new Border()
            {
                Background = FrozenTheme.ToolsPanelBg,
                BorderBrush = FrozenTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 4, 0, 8)
            };

            StackPanel searchStack = new StackPanel();

            // Top Row: Search Input Box + Clear Button + Expand/Collapse All
            Grid topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBox searchBox = new TextBox()
            {
                Background = FrozenTheme.SearchBoxBg,
                Foreground = FrozenTheme.TextPrimary,
                BorderBrush = FrozenTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 5, 8, 5),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };

            Button btnClear = new Button()
            {
                Content = "Clear",
                Style = (Style)FindResource("SecondaryBtn"),
                FontSize = 10,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            StackPanel expandButtons = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            Button btnExpandAll = new Button()
            {
                Content = "Expand All",
                Style = (Style)FindResource("SecondaryBtn"),
                FontSize = 10,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 4, 0)
            };
            Button btnCollapseAll = new Button()
            {
                Content = "Collapse All",
                Style = (Style)FindResource("SecondaryBtn"),
                FontSize = 10,
                Padding = new Thickness(6, 4, 6, 4)
            };
            expandButtons.Children.Add(btnExpandAll);
            expandButtons.Children.Add(btnCollapseAll);

            topRow.Children.Add(searchBox);
            Grid.SetColumn(searchBox, 0);
            topRow.Children.Add(btnClear);
            Grid.SetColumn(btnClear, 1);
            topRow.Children.Add(expandButtons);
            Grid.SetColumn(expandButtons, 2);

            searchStack.Children.Add(topRow);

            // Bottom Row: Category Filter Chips
            WrapPanel chipPanel = new WrapPanel() { Margin = new Thickness(0, 6, 0, 0) };
            string[] categories = new[] { "All", "Graphics", "CPU & RAM", "Network", "Audio", "Suites" };
            string activeCategory = "All";
            var chipButtons = new List<Button>();

            Action filterAction = () =>
            {
                string query = searchBox.Text.Trim().ToLowerInvariant();

                foreach (var pair in sectionPairs)
                {
                    Border sHeader = pair.Item1;
                    WrapPanel sWp = pair.Item2;
                    int sectionMatches = 0;

                    foreach (UIElement child in sWp.Children)
                    {
                        var btn = child as Button;
                        if (btn != null)
                        {
                            string btnText = (btn.Content != null ? btn.Content.ToString() : "").ToLowerInvariant();
                            string tooltip = (btn.ToolTip != null ? btn.ToolTip.ToString() : "").ToLowerInvariant();

                            bool queryMatch = string.IsNullOrEmpty(query) || btnText.Contains(query) || tooltip.Contains(query);
                            bool categoryMatch = true;

                            if (activeCategory == "Graphics")
                            {
                                categoryMatch = btnText.Contains("fps") || btnText.Contains("render") || btnText.Contains("d3d") || btnText.Contains("dx") || btnText.Contains("screen") || btnText.Contains("shadow") || btnText.Contains("light") || btnText.Contains("fov") || btnText.Contains("blur") || btnText.Contains("texture") || btnText.Contains("lod") || tooltip.Contains("graphic");
                            }
                            else if (activeCategory == "CPU & RAM")
                            {
                                categoryMatch = btnText.Contains("cpu") || btnText.Contains("ram") || btnText.Contains("core") || btnText.Contains("memory") || btnText.Contains("priority") || btnText.Contains("cache") || btnText.Contains("standby") || btnText.Contains("working set");
                            }
                            else if (activeCategory == "Network")
                            {
                                categoryMatch = btnText.Contains("net") || btnText.Contains("ping") || btnText.Contains("socket") || btnText.Contains("tcp") || btnText.Contains("udp") || btnText.Contains("dns") || btnText.Contains("qos") || btnText.Contains("rollback");
                            }
                            else if (activeCategory == "Audio")
                            {
                                categoryMatch = btnText.Contains("audio") || btnText.Contains("sound") || btnText.Contains("hear") || btnText.Contains("dink") || btnText.Contains("roar") || btnText.Contains("footstep") || btnText.Contains("voice") || btnText.Contains("sfx") || btnText.Contains("mute");
                            }
                            else if (activeCategory == " Guides")
                            {
                                categoryMatch = btnText.Contains("guide") || btnText.Contains("playbook") || btnText.Contains("matrix") || btnText.Contains("breakpoint") || btn.Tag as string == "Guide";
                            }
                            else if (activeCategory == "Suites")
                            {
                                categoryMatch = btnText.Contains("1-click") || btnText.Contains("suite") || btnText.Contains("combo") || btnText.Contains("preset");
                            }

                            if (queryMatch && categoryMatch)
                            {
                                btn.Visibility = Visibility.Visible;
                                sectionMatches++;
                            }
                            else
                            {
                                btn.Visibility = Visibility.Collapsed;
                            }
                        }
                    }

                    if (sectionMatches > 0)
                    {
                        sHeader.Visibility = Visibility.Visible;
                        sWp.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        sHeader.Visibility = Visibility.Collapsed;
                        sWp.Visibility = Visibility.Collapsed;
                    }
                }
            };

            foreach (string cat in categories)
            {
                Button chip = new Button()
                {
                    Content = cat,
                    Style = (Style)FindResource("SecondaryBtn"),
                    FontSize = 9,
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(0, 0, 4, 4),
                    Tag = cat
                };

                chip.Click += (s, e) =>
                {
                    activeCategory = ((Button)s).Tag.ToString();
                    foreach (var b in chipButtons)
                    {
                        b.Style = (b.Tag.ToString() == activeCategory) ? (Style)FindResource("AccentBtn") : (Style)FindResource("SecondaryBtn");
                    }
                    filterAction();
                };

                chipButtons.Add(chip);
                chipPanel.Children.Add(chip);
            }

            if (chipButtons.Count > 0) chipButtons[0].Style = (Style)FindResource("AccentBtn");

            searchStack.Children.Add(chipPanel);
            searchBarCard.Child = searchStack;

            // Search events
            searchBox.TextChanged += (s, e) => filterAction();
            btnClear.Click += (s, e) =>
            {
                searchBox.Text = "";
                activeCategory = "All";
                foreach (var b in chipButtons) b.Style = (b.Tag.ToString() == "All") ? (Style)FindResource("AccentBtn") : (Style)FindResource("SecondaryBtn");
                filterAction();
            };

            btnExpandAll.Click += (s, e) =>
            {
                foreach (var p in sectionPairs)
                {
                    p.Item1.Visibility = Visibility.Visible;
                    p.Item2.Visibility = Visibility.Visible;
                    var sp = p.Item1.Child as Grid;
                    if (sp != null && sp.Children.Count > 0 && sp.Children[0] is StackPanel)
                    {
                        var st = sp.Children[0] as StackPanel;
                        if (st.Children.Count > 0 && st.Children[0] is TextBlock)
                            ((TextBlock)st.Children[0]).Text = "▼ ";
                    }
                }
            };

            btnCollapseAll.Click += (s, e) =>
            {
                foreach (var p in sectionPairs)
                {
                    p.Item2.Visibility = Visibility.Collapsed;
                    var sp = p.Item1.Child as Grid;
                    if (sp != null && sp.Children.Count > 0 && sp.Children[0] is StackPanel)
                    {
                        var st = sp.Children[0] as StackPanel;
                        if (st.Children.Count > 0 && st.Children[0] is TextBlock)
                            ((TextBlock)st.Children[0]).Text = "▶ ";
                    }
                }
            };

            content.Children.Insert(0, searchBarCard);
        }

    }
}