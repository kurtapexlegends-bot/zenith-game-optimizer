using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;
using ZenithOptimizer.Services.Sniper;

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
        private const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010, SWP_SHOWWINDOW = 0x0040;

        private Window _window;
        private Border _hudCard, _statusBadgeBorder, _colorSwatch, _btnPresetGreen, _btnPresetGold, _btnPresetCyan, _btnPresetPurple, _btnPresetOrange, _btnPresetCustom;
        private TextBlock _statusBadge, _colorText, _sensorPosText, _bidPosText, _bidModeBadge, _actionFlashText, _compactToggleBtn, _markersToggleBtn;
        private StackPanel _expandedBody;
        private DispatcherTimer _flashResetTimer;
        private bool _isPickerActive = false;
        private string _pickerPrompt = null;

        private Action _onToggleMarkersLock, _onToggleCompactMode, _onToggleMarkersVisibility, _onHide;

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
                Width = isCompactMode ? 440 : 580,
                Left = Math.Max(20, (SystemParameters.PrimaryScreenWidth - (isCompactMode ? 440 : 580)) / 2),
                Top = 18
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
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            };

            _hudCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(245, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = isCompactMode ? new Thickness(10, 5, 10, 5) : new Thickness(12, 8, 12, 8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Direction = 270, ShadowDepth = 3, Opacity = 0.65, BlurRadius = 10 }
            };

            StackPanel rootPanel = new StackPanel { Orientation = Orientation.Vertical };

            // === HEADER ROW ===
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 0: Title & Swatch
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 1: Hero Status Pill
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 2: Flash text & controls
            headerGrid.MouseLeftButtonDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) try { _window.DragMove(); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); } };

            StackPanel titlePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Cursor = Cursors.SizeAll };
            titlePanel.Children.Add(new TextBlock { Text = "ZENITH SNIPER", FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)), VerticalAlignment = VerticalAlignment.Center });
            _colorSwatch = new Border { Width = 11, Height = 11, CornerRadius = new CornerRadius(5.5), Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)), BorderBrush = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), BorderThickness = new Thickness(1), Margin = new Thickness(7, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            titlePanel.Children.Add(_colorSwatch);
            _colorText = new TextBlock { Text = "RGB(0,0,0)", FontSize = 8.5, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center };
            titlePanel.Children.Add(_colorText);
            Grid.SetColumn(titlePanel, 0); headerGrid.Children.Add(titlePanel);

            // Center Hero Status Button
            _statusBadge = new TextBlock { Text = "[ARMED • CLICK/ESC TO STOP]", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _statusBadgeBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(200, 6, 78, 59)), BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(9, 3, 9, 3), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Cursor = Cursors.Hand, ToolTip = "Click or press F7 / ESC to STOP / Disarm", Child = _statusBadge };
            _statusBadgeBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; var sn = RobloxBidSniperService.Instance; sn.IsArmed = !sn.IsArmed; };
            Grid.SetColumn(_statusBadgeBorder, 1); headerGrid.Children.Add(_statusBadgeBorder);

            // Right Actions
            StackPanel rightControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _actionFlashText = new TextBlock { Text = "", FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
            rightControls.Children.Add(_actionFlashText);

            Border testFirePill = SniperHudControls.CreateActionPill("[TEST]", Color.FromRgb(56, 189, 248), "Dispatch single test snipe to verify input loop", () => { RobloxBidSniperService.Instance.TriggerTestClick(); FlashActionText("TEST CLICK FIRED", Color.FromRgb(56, 189, 248)); });
            rightControls.Children.Add(testFirePill);

            _compactToggleBtn = new TextBlock { Text = isCompactMode ? "▾" : "▴", FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)), VerticalAlignment = VerticalAlignment.Center };
            Border compactBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Padding = new Thickness(5, 1.5, 5, 1.5), Margin = new Thickness(3, 0, 3, 0), Cursor = Cursors.Hand, ToolTip = "Toggle compact micro-pill vs full control deck [Ctrl+F9]", Child = _compactToggleBtn };
            compactBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (_onToggleCompactMode != null) _onToggleCompactMode(); };
            rightControls.Children.Add(compactBorder);

            TextBlock closeTxt = new TextBlock { Text = "✕", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center };
            Border closeBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Padding = new Thickness(5, 1.5, 5, 1.5), Cursor = Cursors.Hand, ToolTip = "Close HUD [F9]", Child = closeTxt };
            closeBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (_onHide != null) _onHide(); };
            rightControls.Children.Add(closeBorder);

            Grid.SetColumn(rightControls, 2); headerGrid.Children.Add(rightControls);
            rootPanel.Children.Add(headerGrid);

            // === EXPANDED BODY ===
            _expandedBody = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 6, 0, 0), Visibility = isCompactMode ? Visibility.Collapsed : Visibility.Visible };

            // Row 1: Target Setup Cards (Watch Zone & Bid Target)
            Grid setupGrid = new Grid();
            setupGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            setupGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Card 1: Optical Watch Zone
            Border watchCard = new Border { Background = new SolidColorBrush(Color.FromArgb(110, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(60, 56, 189, 248)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 4, 0) };
            StackPanel watchPanel = new StackPanel { Orientation = Orientation.Vertical };
            watchPanel.Children.Add(new TextBlock { Text = "WATCH ZONE (SENSOR)", FontSize = 8, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)) });
            var curSniper = RobloxBidSniperService.Instance;
            _sensorPosText = new TextBlock { Text = string.Format("Spot: ({0}, {1})", curSniper.SensorPosition.X, curSniper.SensorPosition.Y), FontSize = 8.5, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), Margin = new Thickness(0, 1, 0, 3) };
            watchPanel.Children.Add(_sensorPosText);
            Border pickSpotBtn = SniperHudControls.CreatePickerButton("1-Click Pick Spot", "F8", Color.FromRgb(56, 189, 248), "Click anywhere on screen to lock optical sensor spot", () =>
            {
                SetPickerStatus("CLICK SWEET SPOT ON SCREEN...");
                SniperScreenPicker.PickCoordinate("LOCK OPTICAL SENSOR SPOT", (x, y) =>
                {
                    ClearPickerStatus();
                    RobloxBidSniperService.Instance.SetSensorPosition(x, y);
                    FlashActionText(string.Format("SENSOR SPOT ({0}, {1})", x, y), Color.FromRgb(56, 189, 248));
                    ClickDispatchEngine.PlayBeep(1400, 80);
                }, () =>
                {
                    ClearPickerStatus();
                    FlashActionText("SELECTION CANCELLED", Color.FromRgb(148, 163, 184));
                });
            });
            watchPanel.Children.Add(pickSpotBtn);
            watchCard.Child = watchPanel;
            Grid.SetColumn(watchCard, 0); setupGrid.Children.Add(watchCard);

            // Card 2: Bid Button
            Border bidCard = new Border { Background = new SolidColorBrush(Color.FromArgb(110, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(60, 52, 211, 153)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(4, 0, 0, 0) };
            StackPanel bidPanel = new StackPanel { Orientation = Orientation.Vertical };
            StackPanel bidHeader = new StackPanel { Orientation = Orientation.Horizontal };
            bidHeader.Children.Add(new TextBlock { Text = "BID ACTION", FontSize = 8, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)), VerticalAlignment = VerticalAlignment.Center });
            _bidModeBadge = new TextBlock { Text = curSniper.ClickTargetMode == "Hover" ? "[HOVER]" : "[TARGET]", FontSize = 7.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21)), Margin = new Thickness(5, 0, 0, 0), Cursor = Cursors.Hand, ToolTip = "Click to toggle Fixed Target vs Hover in-place" };
            _bidModeBadge.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true; var sn = RobloxBidSniperService.Instance;
                sn.ClickTargetMode = sn.ClickTargetMode == "Hover" ? "FixedTarget" : "Hover";
                _bidModeBadge.Text = sn.ClickTargetMode == "Hover" ? "[HOVER]" : "[TARGET]";
                FlashActionText("BID: " + (sn.ClickTargetMode == "Hover" ? "IN-PLACE HOVER" : "FIXED TARGET"), Color.FromRgb(52, 211, 153));
            };
            bidHeader.Children.Add(_bidModeBadge);
            bidPanel.Children.Add(bidHeader);
            _bidPosText = new TextBlock { Text = curSniper.ClickTargetMode == "Hover" ? "Target: Mouse Hover" : (curSniper.CalibratedClickPosition.HasValue ? string.Format("Target: ({0}, {1})", curSniper.CalibratedClickPosition.Value.X, curSniper.CalibratedClickPosition.Value.Y) : "Target: Auto (+120px)"), FontSize = 8.5, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), Margin = new Thickness(0, 1, 0, 3) };
            bidPanel.Children.Add(_bidPosText);
            Border pickBidBtn = SniperHudControls.CreatePickerButton("1-Click Pick Bid", "Ctrl+F8", Color.FromRgb(52, 211, 153), "Click the in-game BID button on screen to lock target", () =>
            {
                SetPickerStatus("CLICK BID BUTTON ON SCREEN...");
                SniperScreenPicker.PickCoordinate("LOCK BID BUTTON TARGET", (x, y) =>
                {
                    ClearPickerStatus();
                    RobloxBidSniperService.Instance.CalibrateTarget(x, y);
                    FlashActionText(string.Format("BID TARGET LOCKED ({0}, {1})", x, y), Color.FromRgb(52, 211, 153));
                    ClickDispatchEngine.PlayBeep(1600, 90);
                }, () =>
                {
                    ClearPickerStatus();
                    FlashActionText("SELECTION CANCELLED", Color.FromRgb(148, 163, 184));
                });
            });
            bidPanel.Children.Add(pickBidBtn);
            bidCard.Child = bidPanel;
            Grid.SetColumn(bidCard, 1); setupGrid.Children.Add(bidCard);

            _expandedBody.Children.Add(setupGrid);

            // Row 2: Presets & Eyedropper
            StackPanel presetsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 4) };
            presetsRow.Children.Add(new TextBlock { Text = "PRESETS:", FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            _btnPresetGreen = SniperHudControls.CreatePresetPill("Green", Color.FromRgb(34, 197, 94), () => { RobloxBidSniperService.Instance.SetColorPreset("Green"); UpdatePresetButtonsVisuals("Green"); });
            presetsRow.Children.Add(_btnPresetGreen);
            _btnPresetGold = SniperHudControls.CreatePresetPill("Gold", Color.FromRgb(250, 204, 21), () => { RobloxBidSniperService.Instance.SetColorPreset("Gold"); UpdatePresetButtonsVisuals("Gold"); });
            presetsRow.Children.Add(_btnPresetGold);
            _btnPresetCyan = SniperHudControls.CreatePresetPill("Cyan", Color.FromRgb(56, 189, 248), () => { RobloxBidSniperService.Instance.SetColorPreset("Cyan"); UpdatePresetButtonsVisuals("Cyan"); });
            presetsRow.Children.Add(_btnPresetCyan);
            _btnPresetPurple = SniperHudControls.CreatePresetPill("Purple", Color.FromRgb(168, 85, 247), () => { RobloxBidSniperService.Instance.SetColorPreset("Purple"); UpdatePresetButtonsVisuals("Purple"); });
            presetsRow.Children.Add(_btnPresetPurple);
            _btnPresetOrange = SniperHudControls.CreatePresetPill("Orange", Color.FromRgb(249, 115, 22), () => { RobloxBidSniperService.Instance.SetColorPreset("Orange"); UpdatePresetButtonsVisuals("Orange"); });
            presetsRow.Children.Add(_btnPresetOrange);
            _btnPresetCustom = SniperHudControls.CreatePresetPill("Eyedropper", Color.FromRgb(244, 114, 182), () =>
            {
                SetPickerStatus("CLICK SCREEN COLOR TO SAMPLE...");
                SniperScreenPicker.PickColor((r, g, b) =>
                {
                    ClearPickerStatus();
                    RobloxBidSniperService.Instance.CalibrateTargetColor(r, g, b, 45);
                    UpdatePresetButtonsVisuals("Custom");
                    FlashActionText(string.Format("SAMPLED RGB({0},{1},{2})", r, g, b), Color.FromRgb(244, 114, 182));
                    ClickDispatchEngine.PlayBeep(1800, 90);
                }, () =>
                {
                    ClearPickerStatus();
                    FlashActionText("CANCELLED", Color.FromRgb(148, 163, 184));
                });
            });
            presetsRow.Children.Add(_btnPresetCustom);
            _expandedBody.Children.Add(presetsRow);

            // Row 3: Action Deck
            StackPanel actionDeck = new StackPanel { Orientation = Orientation.Horizontal };
            actionDeck.Children.Add(SniperHudControls.CreateActionPill("[TURBO 5x (V)]", Color.FromRgb(250, 204, 21), "Execute 5 rapid bids in 250ms [V]", () => { RobloxBidSniperService.Instance.TurboBurstClick(5); FlashActionText("TURBO BURST (5x)", Color.FromRgb(250, 204, 21)); }));
            actionDeck.Children.Add(SniperHudControls.CreateActionPill("[5s SNIPE]", Color.FromRgb(249, 115, 22), "Arm countdown snipe for last 250ms of auction", () => { RobloxBidSniperService.Instance.TriggerAuctionTimerSnipe(5.0, 4); FlashActionText("5s TIMER SNIPE ARMED", Color.FromRgb(249, 115, 22)); }));

            _markersToggleBtn = new TextBlock { Text = areMarkersHidden ? "[RINGS: OFF]" : "[RINGS: ON]", FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = areMarkersHidden ? new SolidColorBrush(Color.FromRgb(148, 163, 184)) : new SolidColorBrush(Color.FromRgb(52, 211, 153)), VerticalAlignment = VerticalAlignment.Center };
            Border markersBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)), BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2.5, 6, 2.5), Margin = new Thickness(0, 0, 4, 0), Cursor = Cursors.Hand, ToolTip = "Toggle on-screen sensor/target rings [Ctrl+F6]", Child = _markersToggleBtn };
            markersBorder.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; if (_onToggleMarkersVisibility != null) _onToggleMarkersVisibility(); };
            actionDeck.Children.Add(markersBorder);

            _expandedBody.Children.Add(actionDeck);
            rootPanel.Children.Add(_expandedBody);

            _hudCard.Child = rootPanel;
            _window.Content = _hudCard;
            UpdatePresetButtonsVisuals(RobloxBidSniperService.Instance.ColorPreset);
        }

        public void ApplyCompactModeLayout(bool isCompact)
        {
            if (_window == null) return;
            if (_expandedBody != null) _expandedBody.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
            if (_compactToggleBtn != null) _compactToggleBtn.Text = isCompact ? "▾" : "▴";
            _hudCard.Padding = isCompact ? new Thickness(10, 5, 10, 5) : new Thickness(12, 8, 12, 8);
            _window.Width = isCompact ? 440 : 580;
        }

        public void UpdateLockVisuals(bool isLocked) { }

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
            var sn = RobloxBidSniperService.Instance;
            if (_colorText != null)
            {
                _colorText.Text = string.Format("RGB({0},{1},{2}) {3}", r, g, b, isMatch ? "• MATCH!" : "");
                _colorText.Foreground = isMatch ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }
            if (_sensorPosText != null) _sensorPosText.Text = string.Format("Spot: ({0}, {1}) • {2}", sn.SensorPosition.X, sn.SensorPosition.Y, isMatch ? "MATCH!" : (captureSource ?? "DC"));
            if (_bidPosText != null) _bidPosText.Text = sn.ClickTargetMode == "Hover" ? "Target: Mouse Hover" : (sn.CalibratedClickPosition.HasValue ? string.Format("Target: ({0}, {1})", sn.CalibratedClickPosition.Value.X, sn.CalibratedClickPosition.Value.Y) : "Target: Auto (+120px)");
            if (!_isPickerActive && _statusBadge != null && _statusBadgeBorder != null)
            {
                _statusBadge.Text = sn.IsArmed ? (isMatch ? "[MATCH! CLICK/ESC TO STOP]" : "[ARMED • CLICK/ESC TO STOP]") : "[STANDBY • CLICK/F7 TO ARM]";
                _statusBadge.Foreground = sn.IsArmed ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                _statusBadgeBorder.Background = sn.IsArmed ? new SolidColorBrush(Color.FromArgb(200, 6, 78, 59)) : new SolidColorBrush(Color.FromArgb(160, 30, 41, 59));
                _statusBadgeBorder.BorderBrush = sn.IsArmed ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                _statusBadgeBorder.ToolTip = sn.IsArmed ? "Click or press F7 / ESC to STOP / Disarm" : "Click or press F7 to ARM auto-sniper";
                _hudCard.BorderBrush = sn.IsArmed ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(71, 85, 105));
            }
        }

        private void SetPickerStatus(string prompt)
        {
            _isPickerActive = true;
            _pickerPrompt = prompt;
            if (_statusBadge != null) { _statusBadge.Text = prompt; _statusBadge.Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21)); }
            if (_statusBadgeBorder != null) { _statusBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(200, 120, 53, 15)); _statusBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)); }
        }

        public void ClearPickerStatus()
        {
            _isPickerActive = false;
            _pickerPrompt = null;
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
            SniperHudControls.SetPillActive(_btnPresetGreen, p == "green");
            SniperHudControls.SetPillActive(_btnPresetGold, p == "gold" || p == "yellow");
            SniperHudControls.SetPillActive(_btnPresetCyan, p == "cyan" || p == "blue");
            SniperHudControls.SetPillActive(_btnPresetPurple, p == "purple" || p == "violet");
            SniperHudControls.SetPillActive(_btnPresetOrange, p == "orange" || p == "red" || p == "frenzy");
            SniperHudControls.SetPillActive(_btnPresetCustom, p == "custom");
        }

        public void Show() { if (_window != null) _window.Show(); }
        public void Hide() { if (_window != null) _window.Hide(); }
        public void Close() { if (_window != null) { try { _window.Close(); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); } _window = null; } }
    }
}
