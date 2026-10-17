using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer
{
    public partial class MainWindow : Window
    {
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
