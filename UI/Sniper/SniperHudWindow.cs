using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer.UI.Sniper
{
    public class SniperHudWindow
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private Window _window;
        private Border _hudCard;
        private TextBlock _statusBadge, _lockBadge, _colorText, _statsText, _modeBadge, _targetModeBadge, _trackBadge, _actionFlashText, _compactToggleBtn, _markersToggleBtn, _compactColorIndicator;
        private Border _statusBadgeBorder, _lockBadgeBorder, _targetModeBadgeBorder, _trackBadgeBorder, _colorSwatch, _btnPresetGreen, _btnPresetGold, _btnPresetCyan, _btnPresetPurple, _btnPresetOrange, _btnPresetCustom, _compactToggleBorder, _markersToggleBorder, _compactColorDot;
        private StackPanel _row1Panel, _row2Panel;
        private Grid _row3Grid;
        private DispatcherTimer _flashResetTimer;

        private Action _onToggleMarkersLock;
        private Action _onToggleCompactMode;
        private Action _onToggleMarkersVisibility;
        private Action _onHide;

        public Window Window { get { return _window; } }

        public void Initialize(
            bool isCompactMode,
            bool isReticleLocked,
            bool areMarkersHidden,
            Action onToggleMarkersLock,
            Action onToggleCompactMode,
            Action onToggleMarkersVisibility,
            Action onHide)
        {
            _onToggleMarkersLock = onToggleMarkersLock;
            _onToggleCompactMode = onToggleCompactMode;
            _onToggleMarkersVisibility = onToggleMarkersVisibility;
            _onHide = onHide;

            _window = new Window
            {
                Title = "Zenith Sniper Gaming HUD",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.Height,
                Width = isCompactMode ? 510 : 680,
                Left = Math.Max(20, (SystemParameters.PrimaryScreenWidth - 680) / 2),
                Top = 16
            };

            _window.Loaded += (s, e) =>
            {
                try
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(_window);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        SetWindowDisplayAffinity(helper.Handle, WDA_EXCLUDEFROMCAPTURE);
                        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                        SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
                        SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    }
                }
                catch { }
            };

            _hudCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = isCompactMode ? new Thickness(8, 4, 8, 4) : new Thickness(12, 8, 12, 8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Direction = 270, ShadowDepth = 3, Opacity = 0.6, BlurRadius = 8 }
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            headerGrid.MouseLeftButtonDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) { try { _window.DragMove(); } catch { } } };

            StackPanel titlePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Cursor = Cursors.SizeAll };
            titlePanel.MouseLeftButtonDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) { e.Handled = true; try { _window.DragMove(); } catch { } } };

            TextBlock title = new TextBlock { Text = "ZENITH BID SNIPER", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)), VerticalAlignment = VerticalAlignment.Center };
            titlePanel.Children.Add(title);

            _compactColorDot = new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)), Margin = new Thickness(8, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center, Visibility = isCompactMode ? Visibility.Visible : Visibility.Collapsed };
            titlePanel.Children.Add(_compactColorDot);

            _compactColorIndicator = new TextBlock { Text = "RGB", FontSize = 8.5, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0), Visibility = isCompactMode ? Visibility.Visible : Visibility.Collapsed };
            titlePanel.Children.Add(_compactColorIndicator);

            Grid.SetColumn(titlePanel, 0);
            headerGrid.Children.Add(titlePanel);

            _lockBadge = new TextBlock { Text = isReticleLocked ? "[PASS-THROUGH]" : "[DRAGGABLE]", FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = isReticleLocked ? new SolidColorBrush(Color.FromRgb(148, 163, 184)) : new SolidColorBrush(Color.FromRgb(250, 204, 21)), VerticalAlignment = VerticalAlignment.Center };
            _lockBadgeBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), BorderBrush = isReticleLocked ? new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(160, 250, 204, 21)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(4, 0, 4, 0), Cursor = Cursors.Hand, Visibility = isCompactMode ? Visibility.Collapsed : Visibility.Visible, Child = _lockBadge };
            _lockBadgeBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (_onToggleMarkersLock != null) _onToggleMarkersLock(); };
            Grid.SetColumn(_lockBadgeBorder, 1);
            headerGrid.Children.Add(_lockBadgeBorder);

            _modeBadge = new TextBlock { Text = "[Auto]", FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(147, 197, 253)), VerticalAlignment = VerticalAlignment.Center };
            Border modeBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(60, 147, 197, 253)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(4, 0, 4, 0), Cursor = Cursors.Hand, Visibility = isCompactMode ? Visibility.Collapsed : Visibility.Visible, Child = _modeBadge };
            modeBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                var sniper = RobloxBidSniperService.Instance;
                string current = sniper.TriggerMode;
                string next = "Auto";
                if (current.Equals("Auto", StringComparison.OrdinalIgnoreCase)) next = "Edge";
                else if (current.Equals("Edge", StringComparison.OrdinalIgnoreCase)) next = "Continuous";
                else if (current.Equals("Continuous", StringComparison.OrdinalIgnoreCase)) next = "Disruption";
                else if (current.Equals("Disruption", StringComparison.OrdinalIgnoreCase)) next = "Turbo";
                else next = "Auto";
                sniper.TriggerMode = next;
                _modeBadge.Text = "[" + next + "]";
            };
            Grid.SetColumn(modeBorder, 2);
            headerGrid.Children.Add(modeBorder);

            var curSniper = RobloxBidSniperService.Instance;
            _targetModeBadge = new TextBlock { Text = curSniper.ClickTargetMode == "Hover" ? "[BID: HOVER]" : "[BID: TARGET]", FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = curSniper.ClickTargetMode == "Hover" ? new SolidColorBrush(Color.FromRgb(250, 204, 21)) : new SolidColorBrush(Color.FromRgb(56, 189, 248)), VerticalAlignment = VerticalAlignment.Center };
            _targetModeBadgeBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(4, 0, 4, 0), Cursor = Cursors.Hand, Visibility = isCompactMode ? Visibility.Collapsed : Visibility.Visible, Child = _targetModeBadge };
            _targetModeBadgeBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                var sniper = RobloxBidSniperService.Instance;
                string nextMode = sniper.ClickTargetMode == "Hover" ? "FixedTarget" : "Hover";
                sniper.ClickTargetMode = nextMode;
                _targetModeBadge.Text = nextMode == "Hover" ? "[BID: HOVER]" : "[BID: TARGET]";
                _targetModeBadge.Foreground = nextMode == "Hover" ? new SolidColorBrush(Color.FromRgb(250, 204, 21)) : new SolidColorBrush(Color.FromRgb(56, 189, 248));
                FlashActionText("BID TARGET: " + (nextMode == "Hover" ? "MOUSE HOVER" : "FIXED TARGET"), Color.FromRgb(56, 189, 248));
            };
            Grid.SetColumn(_targetModeBadgeBorder, 3);
            headerGrid.Children.Add(_targetModeBadgeBorder);

            _trackBadge = new TextBlock { Text = curSniper.DynamicTrackingEnabled ? "[TRACK: ON]" : "[TRACK: OFF]", FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = curSniper.DynamicTrackingEnabled ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center };
            _trackBadgeBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(4, 0, 4, 0), Cursor = Cursors.Hand, Child = _trackBadge };
            _trackBadgeBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; RobloxBidSniperService.Instance.ToggleDynamicTracking(); };
            Grid.SetColumn(_trackBadgeBorder, 4);
            headerGrid.Children.Add(_trackBadgeBorder);

            _statusBadge = new TextBlock { Text = "[ARMED]", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)), VerticalAlignment = VerticalAlignment.Center };
            _statusBadgeBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(120, 16, 185, 129)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(4, 0, 4, 0), Cursor = Cursors.Hand, Child = _statusBadge };
            _statusBadgeBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; var sniper = RobloxBidSniperService.Instance; sniper.IsArmed = !sniper.IsArmed; };
            Grid.SetColumn(_statusBadgeBorder, 5);
            headerGrid.Children.Add(_statusBadgeBorder);

            _markersToggleBtn = new TextBlock { Text = areMarkersHidden ? "[RINGS: OFF]" : "[RINGS: ON]", FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = areMarkersHidden ? new SolidColorBrush(Color.FromRgb(148, 163, 184)) : new SolidColorBrush(Color.FromRgb(52, 211, 153)), VerticalAlignment = VerticalAlignment.Center };
            _markersToggleBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), BorderBrush = areMarkersHidden ? new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(80, 52, 211, 153)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(4, 0, 4, 0), Cursor = Cursors.Hand, Child = _markersToggleBtn };
            _markersToggleBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (_onToggleMarkersVisibility != null) _onToggleMarkersVisibility(); };
            Grid.SetColumn(_markersToggleBorder, 6);
            headerGrid.Children.Add(_markersToggleBorder);

            _compactToggleBtn = new TextBlock { Text = isCompactMode ? "▾" : "▴", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)), VerticalAlignment = VerticalAlignment.Center };
            _compactToggleBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(4, 0, 4, 0), Cursor = Cursors.Hand, Child = _compactToggleBtn };
            _compactToggleBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (_onToggleCompactMode != null) _onToggleCompactMode(); };
            Grid.SetColumn(_compactToggleBorder, 7);
            headerGrid.Children.Add(_compactToggleBorder);

            TextBlock closeBtn = new TextBlock { Text = "✕", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center };
            Border closeBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Cursor = Cursors.Hand, Child = closeBtn };
            closeBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (_onHide != null) _onHide(); };
            Grid.SetColumn(closeBorder, 8);
            headerGrid.Children.Add(closeBorder);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // Row 1: Real-time telemetry, color swatch, coordinates
            _row1Panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 3), Visibility = isCompactMode ? Visibility.Collapsed : Visibility.Visible };
            _colorSwatch = new Border { Width = 16, Height = 16, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)), BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
            _row1Panel.Children.Add(_colorSwatch);

            _colorText = new TextBlock { Text = "RGB(0,0,0) • No Match", FontSize = 10, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            _row1Panel.Children.Add(_colorText);

            _statsText = new TextBlock { Text = "Hits: 0 • Spot: (960, 540) • Target: Active Mouse", FontSize = 9.5, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center };
            _row1Panel.Children.Add(_statsText);

            Grid.SetRow(_row1Panel, 1);
            mainGrid.Children.Add(_row1Panel);

            // Row 2: Presets
            _row2Panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 3), Visibility = isCompactMode ? Visibility.Collapsed : Visibility.Visible };
            TextBlock presetLbl = new TextBlock { Text = "Sweet-Spot:", FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
            _row2Panel.Children.Add(presetLbl);

            _btnPresetGreen = CreatePresetPill("Green (Common)", Color.FromRgb(34, 197, 94), () => { RobloxBidSniperService.Instance.SetColorPreset("Green"); UpdatePresetButtonsVisuals("Green"); });
            _row2Panel.Children.Add(_btnPresetGreen);

            _btnPresetGold = CreatePresetPill("Gold (Legendary)", Color.FromRgb(250, 204, 21), () => { RobloxBidSniperService.Instance.SetColorPreset("Gold"); UpdatePresetButtonsVisuals("Gold"); });
            _row2Panel.Children.Add(_btnPresetGold);

            _btnPresetCyan = CreatePresetPill("Cyan (Rare)", Color.FromRgb(56, 189, 248), () => { RobloxBidSniperService.Instance.SetColorPreset("Cyan"); UpdatePresetButtonsVisuals("Cyan"); });
            _row2Panel.Children.Add(_btnPresetCyan);

            _btnPresetPurple = CreatePresetPill("Purple (Epic)", Color.FromRgb(168, 85, 247), () => { RobloxBidSniperService.Instance.SetColorPreset("Purple"); UpdatePresetButtonsVisuals("Purple"); });
            _row2Panel.Children.Add(_btnPresetPurple);

            _btnPresetOrange = CreatePresetPill("Orange/Red (Frenzy)", Color.FromRgb(249, 115, 22), () => { RobloxBidSniperService.Instance.SetColorPreset("Orange"); UpdatePresetButtonsVisuals("Orange"); });
            _row2Panel.Children.Add(_btnPresetOrange);

            _btnPresetCustom = CreatePresetPill("Eyedropper [Alt+F8]", Color.FromRgb(244, 114, 182), () =>
            {
                int cr, cg, cb;
                if (RobloxBidSniperService.Instance.SampleTargetColorAtCursor(out cr, out cg, out cb))
                {
                    UpdatePresetButtonsVisuals("Custom");
                    FlashActionText("EYEDROPPER CALIBRATED", Color.FromRgb(244, 114, 182));
                }
            });
            _row2Panel.Children.Add(_btnPresetCustom);

            Grid.SetRow(_row2Panel, 2);
            mainGrid.Children.Add(_row2Panel);

            // Row 3: Hotkeys & Action Flash
            _row3Grid = new Grid { Visibility = isCompactMode ? Visibility.Collapsed : Visibility.Visible };
            _row3Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _row3Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel hotkeysPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            hotkeysPanel.Children.Add(CreateKeycapPill("F6", "Drag/Lock", "Toggle HUD & reticle dragging vs click pass-through [F6]", () => { if (_onToggleMarkersLock != null) _onToggleMarkersLock(); }));
            hotkeysPanel.Children.Add(CreateKeycapPill("Ctrl+F6", "Rings", "Toggle reticle & target rings on-screen visibility [Ctrl+F6]", () => { if (_onToggleMarkersVisibility != null) _onToggleMarkersVisibility(); }));
            hotkeysPanel.Children.Add(CreateKeycapPill("F7", "Arm", "Toggle auto-sniper armed / standby [F7]", () => { var s = RobloxBidSniperService.Instance; s.IsArmed = !s.IsArmed; FlashActionText(s.IsArmed ? "ARMED" : "STANDBY", s.IsArmed ? Color.FromRgb(52, 211, 153) : Color.FromRgb(148, 163, 184)); }));
            hotkeysPanel.Children.Add(CreateKeycapPill("F8", "Sensor", "Set sweet-spot optical sensor coordinate to mouse [F8]", () => { RobloxBidSniperService.Instance.SetSensorPositionToCursor(); FlashActionText("SENSOR SET", Color.FromRgb(56, 189, 248)); }));
            hotkeysPanel.Children.Add(CreateKeycapPill("Alt+F8", "Eyedropper", "Sample target RGB color under mouse cursor [Alt+F8]", () => { int cr, cg, cb; if (RobloxBidSniperService.Instance.SampleTargetColorAtCursor(out cr, out cg, out cb)) { UpdatePresetButtonsVisuals("Custom"); FlashActionText("EYEDROPPER CALIBRATED", Color.FromRgb(244, 114, 182)); } }));
            hotkeysPanel.Children.Add(CreateKeycapPill("F9", "HUD", "Toggle overlay HUD visibility [F9]", () => { if (_onHide != null) _onHide(); }));
            hotkeysPanel.Children.Add(CreateKeycapPill("Ctrl+F9", "Compact", "Toggle compact micro-pill vs full HUD panel [Ctrl+F9]", () => { if (_onToggleCompactMode != null) _onToggleCompactMode(); }));
            hotkeysPanel.Children.Add(CreateKeycapPill("F10", "FPS", "Toggle on-screen FPS and frametime meter [F10] • [Ctrl+F10] to Drag/Lock", () => { FpsMeterOverlay.Instance.Toggle(); }));
            hotkeysPanel.Children.Add(CreateKeycapPill("V", "Turbo", "Turbo bid click burst [V]", () => { RobloxBidSniperService.Instance.TurboBurstClick(5); FlashActionText("TURBO BURST (5x)", Color.FromRgb(250, 204, 21)); }));
            hotkeysPanel.Children.Add(CreateKeycapPill("Ctrl+F8", "Set Bid", "Lock BID button click target to cursor [Ctrl+F8]", () => { int cx, cy; if (RobloxBidSniperService.Instance.SetClickTargetToCurrentCursor(out cx, out cy)) FlashActionText(string.Format("BID TARGET LOCKED ({0}, {1})", cx, cy), Color.FromRgb(56, 189, 248)); }));
            hotkeysPanel.Children.Add(CreateActionPill("[TEST FIRE]", Color.FromRgb(56, 189, 248), "Dispatch single test snipe click to verify feedback loop", () => { var s = RobloxBidSniperService.Instance; s.TriggerTestClick(); FlashActionText("TEST CLICK FIRED", Color.FromRgb(56, 189, 248)); }));
            hotkeysPanel.Children.Add(CreateActionPill("[5s SNIPE]", Color.FromRgb(250, 204, 21), "Arm 5-second countdown timer snipe", () => { RobloxBidSniperService.Instance.TriggerAuctionTimerSnipe(5.0, 4); FlashActionText("5s TIMER SNIPE ARMED", Color.FromRgb(250, 204, 21)); }));

            Grid.SetColumn(hotkeysPanel, 0);
            _row3Grid.Children.Add(hotkeysPanel);

            _actionFlashText = new TextBlock { Text = "", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(6, 0, 0, 0) };
            Grid.SetColumn(_actionFlashText, 1);
            _row3Grid.Children.Add(_actionFlashText);

            Grid.SetRow(_row3Grid, 3);
            mainGrid.Children.Add(_row3Grid);

            _hudCard.Child = mainGrid;
            _window.Content = _hudCard;

            UpdatePresetButtonsVisuals(RobloxBidSniperService.Instance.ColorPreset);
        }

        public void ApplyCompactModeLayout(bool isCompact)
        {
            if (_window == null) return;
            if (isCompact)
            {
                if (_row1Panel != null) _row1Panel.Visibility = Visibility.Collapsed;
                if (_row2Panel != null) _row2Panel.Visibility = Visibility.Collapsed;
                if (_row3Grid != null) _row3Grid.Visibility = Visibility.Collapsed;
                if (_lockBadgeBorder != null) _lockBadgeBorder.Visibility = Visibility.Collapsed;
                if (_targetModeBadgeBorder != null) _targetModeBadgeBorder.Visibility = Visibility.Collapsed;
                if (_compactToggleBtn != null) _compactToggleBtn.Text = "▾";
                if (_compactColorDot != null) _compactColorDot.Visibility = Visibility.Visible;
                if (_compactColorIndicator != null) _compactColorIndicator.Visibility = Visibility.Visible;
                _hudCard.Padding = new Thickness(8, 4, 8, 4);
                _window.Width = 510;
            }
            else
            {
                if (_row1Panel != null) _row1Panel.Visibility = Visibility.Visible;
                if (_row2Panel != null) _row2Panel.Visibility = Visibility.Visible;
                if (_row3Grid != null) _row3Grid.Visibility = Visibility.Visible;
                if (_lockBadgeBorder != null) _lockBadgeBorder.Visibility = Visibility.Visible;
                if (_targetModeBadgeBorder != null) _targetModeBadgeBorder.Visibility = Visibility.Visible;
                if (_compactToggleBtn != null) _compactToggleBtn.Text = "▴";
                if (_compactColorDot != null) _compactColorDot.Visibility = Visibility.Collapsed;
                if (_compactColorIndicator != null) _compactColorIndicator.Visibility = Visibility.Collapsed;
                _hudCard.Padding = new Thickness(12, 8, 12, 8);
                _window.Width = 680;
            }
        }

        public void UpdateLockVisuals(bool isLocked)
        {
            if (_lockBadge != null && _lockBadgeBorder != null)
            {
                _lockBadge.Text = isLocked ? "[PASS-THROUGH]" : "[DRAGGABLE]";
                _lockBadge.Foreground = isLocked ? new SolidColorBrush(Color.FromRgb(148, 163, 184)) : new SolidColorBrush(Color.FromRgb(250, 204, 21));
                _lockBadgeBorder.BorderBrush = isLocked ? new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(160, 250, 204, 21));
            }
        }

        public void UpdateMarkersVisuals(bool areHidden)
        {
            if (_markersToggleBtn != null)
            {
                _markersToggleBtn.Text = areHidden ? "[RINGS: OFF]" : "[RINGS: ON]";
                _markersToggleBtn.Foreground = areHidden ? new SolidColorBrush(Color.FromRgb(148, 163, 184)) : new SolidColorBrush(Color.FromRgb(52, 211, 153));
            }
        }

        public void UpdateTelemetry(int r, int g, int b, bool isMatch, string captureSource)
        {
            if (_colorSwatch != null) _colorSwatch.Background = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
            if (_compactColorDot != null) _compactColorDot.Background = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));

            var sniper = RobloxBidSniperService.Instance;
            string triggerName = sniper.TriggerMode;
            if (_colorText != null)
            {
                _colorText.Text = string.Format("RGB({0},{1},{2}) • {3} • {4}", r, g, b, isMatch ? "MATCH (" + triggerName + ")" : "Scanning", captureSource ?? "Desktop DC");
                _colorText.Foreground = isMatch ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) : new SolidColorBrush(Color.FromRgb(203, 213, 225));
            }
            if (_compactColorIndicator != null)
            {
                _compactColorIndicator.Text = string.Format("{0},{1},{2}", r, g, b);
                _compactColorIndicator.Foreground = isMatch ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }

            if (_statsText != null)
            {
                string targetDesc = sniper.ClickTargetMode == "Hover" ? "Mouse Hover" : (sniper.CalibratedClickPosition.HasValue ? string.Format("BID Target ({0}, {1})", sniper.CalibratedClickPosition.Value.X, sniper.CalibratedClickPosition.Value.Y) : "Auto (+120px)");
                string trackDesc = sniper.DynamicTrackingEnabled ? (sniper.HasTrackLock && sniper.CurrentTrackedPosition.HasValue ? string.Format("Track: Lock ({0}, {1})", sniper.CurrentTrackedPosition.Value.X, sniper.CurrentTrackedPosition.Value.Y) : "Track: Scanning...") : "Track: Off";
                _statsText.Text = string.Format("Hits: {0} • Spot: ({1}, {2}) • {3} • {4}", sniper.SuccessfulSnipes, sniper.SensorPosition.X, sniper.SensorPosition.Y, targetDesc, trackDesc);
            }

            if (_statusBadge != null && _statusBadgeBorder != null)
            {
                _statusBadge.Text = sniper.IsArmed ? "[ARMED]" : "[STANDBY]";
                _statusBadge.Foreground = sniper.IsArmed ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                _statusBadgeBorder.BorderBrush = sniper.IsArmed ? new SolidColorBrush(Color.FromArgb(120, 16, 185, 129)) : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            }

            if (_trackBadge != null && _trackBadgeBorder != null)
            {
                _trackBadge.Text = sniper.DynamicTrackingEnabled ? "[TRACK: ON]" : "[TRACK: OFF]";
                _trackBadge.Foreground = sniper.DynamicTrackingEnabled ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }
        }

        public void FlashActionText(string text, Color color)
        {
            if (_actionFlashText == null) return;
            if (_flashResetTimer != null) { _flashResetTimer.Stop(); _flashResetTimer = null; }
            _actionFlashText.Text = text;
            _actionFlashText.Foreground = new SolidColorBrush(color);
            _flashResetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1600) };
            _flashResetTimer.Tick += (s, e) => { if (_actionFlashText != null) _actionFlashText.Text = ""; _flashResetTimer.Stop(); _flashResetTimer = null; };
            _flashResetTimer.Start();
        }

        private void UpdatePresetButtonsVisuals(string selectedPreset)
        {
            string p = (selectedPreset ?? "Green").ToLowerInvariant();
            SetPillActive(_btnPresetGreen, p == "green");
            SetPillActive(_btnPresetGold, p == "gold" || p == "yellow");
            SetPillActive(_btnPresetCyan, p == "cyan" || p == "blue");
            SetPillActive(_btnPresetPurple, p == "purple" || p == "violet");
            SetPillActive(_btnPresetOrange, p == "orange" || p == "red" || p == "frenzy");
            SetPillActive(_btnPresetCustom, p == "custom");
        }

        private static void SetPillActive(Border pill, bool isActive)
        {
            if (pill == null) return;
            pill.Background = isActive ? new SolidColorBrush(Color.FromArgb(180, 51, 65, 85)) : new SolidColorBrush(Color.FromArgb(120, 30, 41, 59));
            pill.BorderThickness = new Thickness(isActive ? 1.5 : 1);
        }

        private Border CreatePresetPill(string label, Color color, Action onClick)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            Border dot = new Border { Width = 7, Height = 7, CornerRadius = new CornerRadius(3.5), Background = new SolidColorBrush(color), Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(dot);
            TextBlock txt = new TextBlock { Text = label, FontSize = 8.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)), VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(txt);
            Border border = new Border { Background = new SolidColorBrush(Color.FromArgb(120, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(60, color.R, color.G, color.B)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Padding = new Thickness(4, 2, 4, 2), Margin = new Thickness(0, 0, 4, 0), Cursor = Cursors.Hand, Child = sp };
            border.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (onClick != null) onClick(); };
            return border;
        }

        private Border CreateKeycapPill(string keyText, string actionText, string tooltip, Action onClick)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            Border keyBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(180, 51, 65, 85)), CornerRadius = new CornerRadius(2), Padding = new Thickness(3, 0.5, 3, 0.5), Margin = new Thickness(0, 0, 3, 0), Child = new TextBlock { Text = keyText, FontSize = 8, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21)) } };
            sp.Children.Add(keyBorder);
            sp.Children.Add(new TextBlock { Text = actionText, FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), VerticalAlignment = VerticalAlignment.Center });
            Border border = new Border { Background = new SolidColorBrush(Color.FromArgb(100, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Padding = new Thickness(4, 1.5, 4, 1.5), Margin = new Thickness(0, 0, 4, 0), Cursor = Cursors.Hand, ToolTip = tooltip, Child = sp };
            border.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (onClick != null) onClick(); };
            return border;
        }

        private Border CreateActionPill(string text, Color color, string tooltip, Action onClick)
        {
            TextBlock tb = new TextBlock { Text = text, FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center };
            Border border = new Border { Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Padding = new Thickness(5, 1.5, 5, 1.5), Margin = new Thickness(0, 0, 4, 0), Cursor = Cursors.Hand, ToolTip = tooltip, Child = tb };
            border.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (onClick != null) onClick(); };
            return border;
        }

        public void Show() { if (_window != null) _window.Show(); }
        public void Hide() { if (_window != null) _window.Hide(); }
        public void Close() { if (_window != null) { try { _window.Close(); } catch { } _window = null; } }
    }
}
