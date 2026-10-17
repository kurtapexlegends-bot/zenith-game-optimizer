using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ellipse = System.Windows.Shapes.Ellipse;

namespace ZenithOptimizer.UI.Library
{
    public static partial class GameCardRenderer
    {
        public static Border CreateGripHandle(Func<string, object> findResource)
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
                    Fill = (SolidColorBrush)findResource("TextMuted"),
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
                gripHandle.Background = (SolidColorBrush)findResource("BgSunken");
                gripHandle.BorderBrush = (SolidColorBrush)findResource("BorderSubtle");
                foreach (UIElement child in gripDots.Children)
                {
                    var el = child as Ellipse;
                    if (el != null) el.Fill = (SolidColorBrush)findResource("AccentAction");
                }
            };
            gripHandle.MouseLeave += (s, e) =>
            {
                gripHandle.Background = Brushes.Transparent;
                gripHandle.BorderBrush = Brushes.Transparent;
                foreach (UIElement child in gripDots.Children)
                {
                    var el = child as Ellipse;
                    if (el != null) el.Fill = (SolidColorBrush)findResource("TextMuted");
                }
            };

            return gripHandle;
        }

        public static void GetGameBadgeInfo(string gameName, out string monogram, out Color accentColor)
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

        public static Border CreateEmptyStateCard(string searchQuery, Func<string, object> findResource, Action onClearSearch, Action onScanRunning, Action onAddCustom)
        {
            Border emptyCard = new Border
            {
                Background = (SolidColorBrush)findResource("BgCard"),
                BorderBrush = (SolidColorBrush)findResource("BorderCard"),
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
                    Background = (SolidColorBrush)findResource("BgSunken"),
                    BorderBrush = (SolidColorBrush)findResource("BorderSubtle"),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10),
                    Child = new System.Windows.Shapes.Path
                    {
                        Data = Geometry.Parse("M 6,6 A 5,5 0 1,1 6,16 A 5,5 0 1,1 6,6 Z M 14,14 L 18,18"),
                        Stroke = (SolidColorBrush)findResource("TextMuted"),
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
                    Foreground = (SolidColorBrush)findResource("TextPrimary"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                TextBlock emptySearchDesc = new TextBlock
                {
                    Text = "Check your search keyword, process name, or clear the filter to show all installed games.",
                    FontSize = 11,
                    Foreground = (SolidColorBrush)findResource("TextSecondary"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                };
                Button clearFilterBtn = new Button
                {
                    Content = "✕ Clear Search Filter",
                    Style = (Style)findResource("SecondaryBtn"),
                    FontSize = 11,
                    Padding = new Thickness(14, 6, 14, 6),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                clearFilterBtn.Click += (s, e) => { if (onClearSearch != null) onClearSearch(); };

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
                    Background = (SolidColorBrush)findResource("BgSunken"),
                    BorderBrush = (SolidColorBrush)findResource("BorderSubtle"),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10),
                    Child = new System.Windows.Shapes.Path
                    {
                        Data = Geometry.Parse("M 2,7 A 5,5 0 0,1 18,7 L 18,13 A 2,2 0 0,1 15,15 L 14,13 L 6,13 L 5,15 A 2,2 0 0,1 2,13 Z M 5,6 L 7,6 M 6,5 L 6,7 M 13,6 L 15,6"),
                        Stroke = (SolidColorBrush)findResource("TextMuted"),
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
                    Foreground = (SolidColorBrush)findResource("TextPrimary"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                TextBlock emptyDesc = new TextBlock
                {
                    Text = "Install supported titles or click below to discover active user processes or add custom game executables.",
                    FontSize = 11,
                    Foreground = (SolidColorBrush)findResource("TextSecondary"),
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
                    Style = (Style)findResource("AccentBtn"),
                    FontSize = 11,
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                scanBtn.Click += (s, e) => { if (onScanRunning != null) onScanRunning(); };

                Button addBtn = new Button
                {
                    Content = "+ Add Executable (.exe)",
                    Style = (Style)findResource("SecondaryBtn"),
                    FontSize = 11,
                    Padding = new Thickness(12, 6, 12, 6)
                };
                addBtn.Click += (s, e) => { if (onAddCustom != null) onAddCustom(); };

                actionButtons.Children.Add(scanBtn);
                actionButtons.Children.Add(addBtn);

                emptyStack.Children.Add(iconBadge);
                emptyStack.Children.Add(emptyTitle);
                emptyStack.Children.Add(emptyDesc);
                emptyStack.Children.Add(actionButtons);
            }

            emptyCard.Child = emptyStack;
            return emptyCard;
        }
    }
}
