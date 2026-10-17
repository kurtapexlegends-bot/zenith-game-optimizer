using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZenithOptimizer.Services;

namespace ZenithOptimizer.UI
{
    /// <summary>
    /// High-Visibility Gaming On-Screen HUD Overlay for Roblox Bid Battles.
    /// Features a floating telemetry pill, a draggable sensor reticle, and an optional click target marker.
    /// Operates on top of fullscreen borderless Roblox with zero latency and full DPI scaling immunity.
    /// Reticle uses a 100% hollow center aperture and WDA_EXCLUDEFROMCAPTURE to eliminate self-blinding.
    /// </summary>
    public class RobloxSniperOverlay : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private static RobloxSniperOverlay _instance;
        private static readonly object _syncRoot = new object();

        public static RobloxSniperOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new RobloxSniperOverlay();
                        }
                    }
                }
                return _instance;
            }
        }

        private Window _hudWindow;
        private Window _reticleWindow;
        private Window _clickTargetWindow;
        private Border _hudCard;
        private TextBlock _statusBadge;
        private Border _statusBadgeBorder;
        private TextBlock _lockBadge;
        private Border _lockBadgeBorder;
        private Border _colorSwatch;
        private TextBlock _colorText;
        private TextBlock _statsText;
        private TextBlock _modeBadge;
        private Border _trackBadgeBorder;
        private TextBlock _trackBadge;
        private Border _corridorBox;
        private Border _trackLockIndicator;
        private TextBlock _corridorLabel;
        private TextBlock _actionFlashText;
        private Border _btnPresetGreen;
        private Border _btnPresetGold;
        private Border _btnPresetCyan;
        private Border _btnPresetPurple;
        private Border _btnPresetOrange;
        private Border _btnPresetCustom;
        private Ellipse _reticleRing;
        private Line _reticleHLine;
        private Line _reticleVLine;
        private Line _tickRight;
        private Line _tickBottom;
        private DispatcherTimer _flashResetTimer;
        private bool _isReticleLocked = false;

        private bool _isVisible = false;
        public bool IsVisible { get { return _isVisible; } }
        public bool IsReticleLocked { get { return _isReticleLocked; } }

        public void ToggleMarkersLock()
        {
            SetMarkersClickThrough(!_isReticleLocked);
        }

        public void Initialize()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_hudWindow != null) return;

                CreateHudWindow();
                CreateReticleWindow();
                CreateClickTargetWindow();

                // Wire up events from RobloxBidSniperService
                var service = RobloxBidSniperService.Instance;
                service.OnColorSampled += OnColorSampled;
                service.OnSnipeFired += OnSnipeFired;
                service.OnArmedChanged += OnArmedChanged;
                service.OnSensorPositionChanged += OnSensorPositionChanged;
                service.OnClickTargetChanged += OnClickTargetChanged;
                service.OnTargetColorChanged += OnTargetColorChanged;
                service.OnTrackPositionChanged += OnTrackPositionChanged;
                service.OnDynamicTrackingToggled += OnDynamicTrackingToggled;
            });
        }

        public void Show()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Initialize();
                if (_hudWindow != null)
                {
                    _hudWindow.Show();
                    _reticleWindow.Show();
                    if (RobloxBidSniperService.Instance.CalibratedClickPosition.HasValue && _clickTargetWindow != null)
                    {
                        _clickTargetWindow.Show();
                    }
                    _isVisible = true;
                    UpdateDisplay();
                }
            });
        }

        public void Hide()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_hudWindow != null)
                {
                    _hudWindow.Hide();
                    _reticleWindow.Hide();
                    if (_clickTargetWindow != null) _clickTargetWindow.Hide();
                    _isVisible = false;
                }
            });
        }

        public void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }

        private static void GetDpi(Visual visual, out double scaleX, out double scaleY)
        {
            scaleX = 1.0;
            scaleY = 1.0;
            try
            {
                if (visual != null)
                {
                    var dpi = VisualTreeHelper.GetDpi(visual);
                    scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                    scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
                }
            }
            catch { }
        }

        private void CreateHudWindow()
        {
            _hudWindow = new Window
            {
                Title = "Zenith Sniper Gaming HUD",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Width = 560,
                Height = 148,
                Left = Math.Max(20, (SystemParameters.PrimaryScreenWidth - 560) / 2),
                Top = 16
            };

            _hudWindow.Loaded += (s, e) =>
            {
                try
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(_hudWindow);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        SetWindowDisplayAffinity(helper.Handle, WDA_EXCLUDEFROMCAPTURE);
                    }
                }
                catch { }
            };

            _hudCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 15, 23, 42)), // Deep Slate Glass
                BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Emerald accent
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 3,
                    Opacity = 0.6,
                    BlurRadius = 8
                }
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Header with title, click-thru lock, mode badge, live status badge, and close button
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            headerGrid.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    try { _hudWindow.DragMove(); } catch { }
                }
            };

            StackPanel titlePanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.SizeAll,
                ToolTip = "Drag HUD to position anywhere on screen"
            };
            titlePanel.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    e.Handled = true;
                    try { _hudWindow.DragMove(); } catch { }
                }
            };
            TextBlock title = new TextBlock
            {
                Text = "ZENITH BID SNIPER",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock dragGrip = new TextBlock
            {
                Text = " ⠿ Drag",
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromArgb(140, 148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            titlePanel.Children.Add(title);
            titlePanel.Children.Add(dragGrip);
            Grid.SetColumn(titlePanel, 0);
            headerGrid.Children.Add(titlePanel);

            _lockBadge = new TextBlock
            {
                Text = _isReticleLocked ? "[🔒 Pass-Through]" : "[⇱ Draggable]",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _isReticleLocked
                    ? new SolidColorBrush(Color.FromRgb(148, 163, 184))
                    : new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                VerticalAlignment = VerticalAlignment.Center
            };
            _lockBadgeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)),
                BorderBrush = _isReticleLocked 
                    ? new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)) 
                    : new SolidColorBrush(Color.FromArgb(160, 250, 204, 21)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Click to toggle: [⇱ Draggable] (move HUD & markers) / [🔒 Pass-Through] (clicks pass to Roblox)",
                Child = _lockBadge
            };
            _lockBadgeBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                SetMarkersClickThrough(!_isReticleLocked);
            };
            Grid.SetColumn(_lockBadgeBorder, 1);
            headerGrid.Children.Add(_lockBadgeBorder);

            _modeBadge = new TextBlock
            {
                Text = "[Auto]",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Border modeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 147, 197, 253)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Click to cycle trigger mode: Auto -> Edge -> Continuous -> Disruption",
                Child = _modeBadge
            };
            modeBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                var sniper = RobloxBidSniperService.Instance;
                string current = sniper.TriggerMode;
                string next = "Auto";
                if (current.Equals("Auto", StringComparison.OrdinalIgnoreCase)) next = "Edge";
                else if (current.Equals("Edge", StringComparison.OrdinalIgnoreCase)) next = "Continuous";
                else if (current.Equals("Continuous", StringComparison.OrdinalIgnoreCase)) next = "Disruption";
                else next = "Auto";
                sniper.TriggerMode = next;
                _modeBadge.Text = "[" + next + "]";
            };
            Grid.SetColumn(modeBorder, 2);
            headerGrid.Children.Add(modeBorder);

            var currentSniper = RobloxBidSniperService.Instance;
            _trackBadge = new TextBlock
            {
                Text = currentSniper.DynamicTrackingEnabled ? "[TRACK: ON]" : "[TRACK: OFF]",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = currentSniper.DynamicTrackingEnabled
                    ? new SolidColorBrush(Color.FromRgb(56, 189, 248))
                    : new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            _trackBadgeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Click to toggle Dynamic Screen Tracking Corridor [Ctrl+F7] (auto-scans meter to follow sweet spot)",
                Child = _trackBadge
            };
            _trackBadgeBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                RobloxBidSniperService.Instance.ToggleDynamicTracking();
            };
            Grid.SetColumn(_trackBadgeBorder, 3);
            headerGrid.Children.Add(_trackBadgeBorder);

            _statusBadge = new TextBlock
            {
                Text = "[ARMED]",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Border statusBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 16, 185, 129)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 6, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Click to toggle Armed / Standby [F7]",
                Child = _statusBadge
            };
            _statusBadgeBorder = statusBorder;
            statusBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                var sniper = RobloxBidSniperService.Instance;
                sniper.IsArmed = !sniper.IsArmed;
            };
            Grid.SetColumn(statusBorder, 4);
            headerGrid.Children.Add(statusBorder);

            TextBlock closeBtn = new TextBlock
            {
                Text = "✕",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Border closeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Cursor = Cursors.Hand,
                ToolTip = "Close Overlay HUD [F9]",
                Child = closeBtn
            };
            closeBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                Hide();
            };
            Grid.SetColumn(closeBorder, 5);
            headerGrid.Children.Add(closeBorder);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // Row 1: Real-time telemetry, color swatch, coordinates
            StackPanel row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 3) };

            _colorSwatch = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            row1.Children.Add(_colorSwatch);

            _colorText = new TextBlock
            {
                Text = "RGB(0,0,0) • No Match",
                FontSize = 10,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            row1.Children.Add(_colorText);

            _statsText = new TextBlock
            {
                Text = "Hits: 0 • Spot: (960, 540) • Target: Active Mouse",
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            row1.Children.Add(_statsText);

            Grid.SetRow(row1, 1);
            mainGrid.Children.Add(row1);

            // Row 2: Storage Hunters Sweet-Spot Color Presets
            StackPanel presetRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 3) };
            TextBlock presetLbl = new TextBlock
            {
                Text = "Sweet-Spot:",
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            presetRow.Children.Add(presetLbl);

            _btnPresetGreen = CreatePresetPill("Green (Common)", Color.FromRgb(34, 197, 94), () =>
            {
                RobloxBidSniperService.Instance.SetColorPreset("Green");
                UpdatePresetButtonsVisuals("Green");
            });
            presetRow.Children.Add(_btnPresetGreen);

            _btnPresetGold = CreatePresetPill("Gold (Legendary)", Color.FromRgb(250, 204, 21), () =>
            {
                RobloxBidSniperService.Instance.SetColorPreset("Gold");
                UpdatePresetButtonsVisuals("Gold");
            });
            presetRow.Children.Add(_btnPresetGold);

            _btnPresetCyan = CreatePresetPill("Cyan (Rare)", Color.FromRgb(56, 189, 248), () =>
            {
                RobloxBidSniperService.Instance.SetColorPreset("Cyan");
                UpdatePresetButtonsVisuals("Cyan");
            });
            presetRow.Children.Add(_btnPresetCyan);

            _btnPresetPurple = CreatePresetPill("Purple (Epic)", Color.FromRgb(168, 85, 247), () =>
            {
                RobloxBidSniperService.Instance.SetColorPreset("Purple");
                UpdatePresetButtonsVisuals("Purple");
            });
            presetRow.Children.Add(_btnPresetPurple);

            _btnPresetOrange = CreatePresetPill("Orange/Red (Frenzy)", Color.FromRgb(249, 115, 22), () =>
            {
                RobloxBidSniperService.Instance.SetColorPreset("Orange");
                UpdatePresetButtonsVisuals("Orange");
            });
            presetRow.Children.Add(_btnPresetOrange);

            _btnPresetCustom = CreatePresetPill("Eyedropper [Alt+F8]", Color.FromRgb(244, 114, 182), () =>
            {
                int cr, cg, cb;
                if (RobloxBidSniperService.Instance.SampleTargetColorAtCursor(out cr, out cg, out cb))
                {
                    UpdatePresetButtonsVisuals("Custom");
                    FlashActionText("EYEDROPPER CALIBRATED", Color.FromRgb(244, 114, 182));
                }
            });
            presetRow.Children.Add(_btnPresetCustom);

            Grid.SetRow(presetRow, 2);
            mainGrid.Children.Add(presetRow);

            // Row 3: Hotkey reminders and action flash
            Grid row3Grid = new Grid();
            row3Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row3Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel hotkeysPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            hotkeysPanel.Children.Add(CreateKeycapPill("F6", "Drag/Lock", "Toggle HUD & reticle dragging vs click pass-through [F6]", () =>
            {
                ToggleMarkersLock();
                FlashActionText(_isReticleLocked ? "LOCK ENGAGED" : "DRAGGABLE", Color.FromRgb(250, 204, 21));
            }));
            hotkeysPanel.Children.Add(CreateKeycapPill("F7", "Arm", "Toggle auto-sniper armed / standby [F7]", () =>
            {
                var sniper = RobloxBidSniperService.Instance;
                sniper.IsArmed = !sniper.IsArmed;
                FlashActionText(sniper.IsArmed ? "ARMED" : "STANDBY", sniper.IsArmed ? Color.FromRgb(52, 211, 153) : Color.FromRgb(148, 163, 184));
            }));
            hotkeysPanel.Children.Add(CreateKeycapPill("F8", "Sensor", "Set sweet-spot optical sensor coordinate to mouse [F8]", () =>
            {
                RobloxBidSniperService.Instance.SetSensorPositionToCursor();
                FlashActionText("SENSOR SET", Color.FromRgb(56, 189, 248));
            }));
            hotkeysPanel.Children.Add(CreateKeycapPill("Alt+F8", "Eyedropper", "Sample target RGB color under mouse cursor [Alt+F8]", () =>
            {
                int cr, cg, cb;
                if (RobloxBidSniperService.Instance.SampleTargetColorAtCursor(out cr, out cg, out cb))
                {
                    UpdatePresetButtonsVisuals("Custom");
                    FlashActionText("EYEDROPPER CALIBRATED", Color.FromRgb(244, 114, 182));
                }
            }));
            hotkeysPanel.Children.Add(CreateKeycapPill("F9", "HUD", "Toggle overlay HUD visibility [F9]", () =>
            {
                Hide();
            }));
            hotkeysPanel.Children.Add(CreateKeycapPill("V", "Turbo", "Turbo bid click burst [V]", () =>
            {
                RobloxBidSniperService.Instance.TurboBurstClick(5);
                FlashActionText("⚡ TURBO BURST (5x)", Color.FromRgb(250, 204, 21));
            }));

            Grid.SetColumn(hotkeysPanel, 0);
            row3Grid.Children.Add(hotkeysPanel);

            _actionFlashText = new TextBlock
            {
                Text = "READY",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_actionFlashText, 1);
            row3Grid.Children.Add(_actionFlashText);

            Grid.SetRow(row3Grid, 3);
            mainGrid.Children.Add(row3Grid);

            _hudCard.Child = mainGrid;
            _hudWindow.Content = _hudCard;

            _flashResetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _flashResetTimer.Tick += (s, e) =>
            {
                _flashResetTimer.Stop();
                _actionFlashText.Text = "READY";
                _actionFlashText.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
                _hudCard.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            };
        }

        private void FlashActionText(string text, Color color)
        {
            if (_actionFlashText == null) return;
            _actionFlashText.Text = text;
            _actionFlashText.Foreground = new SolidColorBrush(color);
            if (_hudCard != null)
            {
                _hudCard.BorderBrush = new SolidColorBrush(color);
            }
            if (_flashResetTimer != null)
            {
                _flashResetTimer.Stop();
                _flashResetTimer.Start();
            }
        }

        private Border CreateKeycapPill(string key, string label, string tooltip, Action onClick = null)
        {
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            Border keyCap = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 148, 163, 184)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(3, 1, 3, 1),
                Margin = new Thickness(0, 0, 3, 0),
                Child = new TextBlock
                {
                    Text = key,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    FontFamily = new FontFamily("Consolas, Segoe UI")
                }
            };
            sp.Children.Add(keyCap);

            TextBlock lbl = new TextBlock
            {
                Text = label,
                FontSize = 8,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(lbl);

            Border pill = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 148, 163, 184)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 5, 1),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = onClick != null ? Cursors.Hand : Cursors.Help,
                ToolTip = tooltip,
                Child = sp
            };

            pill.MouseEnter += (s, e) =>
            {
                pill.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 56, 189, 248));
                pill.Background = new SolidColorBrush(Color.FromArgb(180, 30, 41, 59));
                lbl.Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            };
            pill.MouseLeave += (s, e) =>
            {
                pill.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 148, 163, 184));
                pill.Background = new SolidColorBrush(Color.FromArgb(120, 30, 41, 59));
                lbl.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            };

            if (onClick != null)
            {
                pill.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    onClick();
                };
            }

            return pill;
        }

        private Border CreatePresetPill(string label, Color accent, Action onClick)
        {
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            Border dot = new Border
            {
                Width = 6,
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(accent),
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(dot);

            TextBlock tb = new TextBlock
            {
                Text = label,
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(tb);

            Border border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(70, accent.R, accent.G, accent.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 6, 1),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Click to set active Storage Hunters sweet-spot to " + label,
                Child = sp
            };

            border.MouseEnter += (s, e) =>
            {
                if ((border.Tag as string) != "active")
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(200, 30, 41, 59));
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(160, accent.R, accent.G, accent.B));
                }
            };
            border.MouseLeave += (s, e) =>
            {
                if ((border.Tag as string) != "active")
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59));
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(70, accent.R, accent.G, accent.B));
                }
            };

            border.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return border;
        }

        private void UpdatePresetButtonsVisuals(string currentPreset)
        {
            if (string.IsNullOrEmpty(currentPreset)) currentPreset = "Green";
            Action<Border, bool, Color> stylePill = (b, active, color) =>
            {
                if (b == null) return;
                b.Tag = active ? "active" : null;
                b.Background = active ? new SolidColorBrush(Color.FromArgb(220, color.R, color.G, color.B)) : new SolidColorBrush(Color.FromArgb(140, 30, 41, 59));
                b.BorderBrush = active ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(70, color.R, color.G, color.B));

                StackPanel sp = b.Child as StackPanel;
                if (sp != null)
                {
                    Border dot = sp.Children.Count > 0 ? sp.Children[0] as Border : null;
                    TextBlock tb = sp.Children.Count > 1 ? sp.Children[1] as TextBlock : null;

                    if (dot != null)
                    {
                        dot.Background = active ? new SolidColorBrush(Color.FromRgb(15, 23, 42)) : new SolidColorBrush(color);
                    }
                    if (tb != null)
                    {
                        tb.Foreground = active ? new SolidColorBrush(Color.FromRgb(15, 23, 42)) : new SolidColorBrush(color);
                        tb.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
                    }
                }
                else
                {
                    var tb = b.Child as TextBlock;
                    if (tb != null)
                    {
                        tb.Foreground = active ? new SolidColorBrush(Color.FromRgb(15, 23, 42)) : new SolidColorBrush(color);
                        tb.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
                    }
                }
            };

            stylePill(_btnPresetGreen, currentPreset.Equals("Green", StringComparison.OrdinalIgnoreCase), Color.FromRgb(34, 197, 94));
            stylePill(_btnPresetGold, currentPreset.Equals("Gold", StringComparison.OrdinalIgnoreCase) || currentPreset.Equals("Yellow", StringComparison.OrdinalIgnoreCase), Color.FromRgb(250, 204, 21));
            stylePill(_btnPresetCyan, currentPreset.Equals("Cyan", StringComparison.OrdinalIgnoreCase) || currentPreset.Equals("Blue", StringComparison.OrdinalIgnoreCase), Color.FromRgb(56, 189, 248));
            stylePill(_btnPresetPurple, currentPreset.Equals("Purple", StringComparison.OrdinalIgnoreCase) || currentPreset.Equals("Violet", StringComparison.OrdinalIgnoreCase), Color.FromRgb(168, 85, 247));
            stylePill(_btnPresetOrange, currentPreset.Equals("Orange", StringComparison.OrdinalIgnoreCase) || currentPreset.Equals("Red", StringComparison.OrdinalIgnoreCase) || currentPreset.Equals("Frenzy", StringComparison.OrdinalIgnoreCase), Color.FromRgb(249, 115, 22));
            stylePill(_btnPresetCustom, currentPreset.Equals("Custom", StringComparison.OrdinalIgnoreCase), Color.FromRgb(244, 114, 182));
        }

        private void CreateReticleWindow()
        {
            var sniper = RobloxBidSniperService.Instance;

            _reticleWindow = new Window
            {
                Title = "Zenith Sensor Target Reticle",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Width = 320,
                Height = 64,
                Cursor = Cursors.SizeAll
            };

            // Set initial position using DPI translation and exclude from screen capture (GDI GetPixel safety)
            double scaleX = 1.0, scaleY = 1.0;
            _reticleWindow.Loaded += (s, e) =>
            {
                GetDpi(_reticleWindow, out scaleX, out scaleY);
                _reticleWindow.Left = (sniper.SensorPosition.X / scaleX) - 160;
                _reticleWindow.Top = (sniper.SensorPosition.Y / scaleY) - 32;
                try
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(_reticleWindow);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        SetWindowDisplayAffinity(helper.Handle, WDA_EXCLUDEFROMCAPTURE);
                        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                        if (_isReticleLocked)
                        {
                            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
                        }
                        else
                        {
                            SetWindowLong(helper.Handle, GWL_EXSTYLE, (exStyle & ~WS_EX_TRANSPARENT & ~WS_EX_NOACTIVATE));
                        }
                    }
                }
                catch { }
            };

            Grid reticleGrid = new Grid
            {
                Width = 320,
                Height = 64,
                Background = new SolidColorBrush(Color.FromArgb(2, 16, 185, 129))
            };

            // Dynamic tracking search corridor guide band
            _corridorBox = new Border
            {
                Width = Math.Max(260, sniper.TrackCorridorWidth),
                Height = Math.Max(32, sniper.TrackCorridorHeight),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromArgb(10, 56, 189, 248)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Visibility = sniper.DynamicTrackingEnabled ? Visibility.Visible : Visibility.Collapsed
            };
            _corridorLabel = new TextBlock
            {
                Text = "TRACK CORRIDOR (±130px)",
                FontSize = 7.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)),
                Margin = new Thickness(4, 1, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            _corridorBox.Child = _corridorLabel;
            reticleGrid.Children.Add(_corridorBox);

            // Dynamic sweet-spot lock marker inside corridor
            _trackLockIndicator = new Border
            {
                Width = 24,
                Height = 24,
                BorderBrush = new SolidColorBrush(Color.FromRgb(52, 211, 153)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(25, 52, 211, 153)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            reticleGrid.Children.Add(_trackLockIndicator);

            // Center sensor boundary ring (36px diameter).
            // Fill MUST be Brushes.Transparent so the entire central sampling aperture has ZERO drawn pixels.
            // This prevents the reticle window from occluding the game during desktop GDI GetPixel sampling.
            _reticleRing = new Ellipse
            {
                Width = 36,
                Height = 36,
                Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            reticleGrid.Children.Add(_reticleRing);

            // 4 Outer crosshair ticks located OUTSIDE the 36px ring centered at (160, 32)
            _reticleHLine = new Line
            {
                X1 = 138,
                Y1 = 32,
                X2 = 142,
                Y2 = 32,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            reticleGrid.Children.Add(_reticleHLine);

            _tickRight = new Line
            {
                X1 = 178,
                Y1 = 32,
                X2 = 182,
                Y2 = 32,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            reticleGrid.Children.Add(_tickRight);

            _reticleVLine = new Line
            {
                X1 = 160,
                Y1 = 10,
                X2 = 160,
                Y2 = 14,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            reticleGrid.Children.Add(_reticleVLine);

            _tickBottom = new Line
            {
                X1 = 160,
                Y1 = 50,
                X2 = 160,
                Y2 = 54,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            reticleGrid.Children.Add(_tickBottom);

            // Drag reticle directly to place it right over the auction green zone.
            // Uses smooth DragMove with live DPI-immune physical coordinate synchronization.
            _reticleWindow.LocationChanged += (s, e) =>
            {
                if (!_isReticleLocked && _reticleWindow != null && _reticleWindow.IsLoaded)
                {
                    try
                    {
                        Point center = _reticleWindow.PointToScreen(new Point(160, 32));
                        RobloxBidSniperService.Instance.SetSensorPosition((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch { }
                }
            };

            reticleGrid.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && !_isReticleLocked)
                {
                    e.Handled = true;
                    try
                    {
                        _reticleWindow.DragMove();
                        Point center = _reticleWindow.PointToScreen(new Point(160, 32));
                        RobloxBidSniperService.Instance.SetSensorPosition((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch { }
                }
            };

            _reticleWindow.Content = reticleGrid;
        }

        private void CreateClickTargetWindow()
        {
            var sniper = RobloxBidSniperService.Instance;

            _clickTargetWindow = new Window
            {
                Title = "Zenith Bid Button Click Target",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Width = 36,
                Height = 36,
                Cursor = Cursors.Hand
            };

            double scaleX = 1.0, scaleY = 1.0;
            _clickTargetWindow.Loaded += (s, e) =>
            {
                GetDpi(_clickTargetWindow, out scaleX, out scaleY);
                if (sniper.CalibratedClickPosition.HasValue)
                {
                    _clickTargetWindow.Left = (sniper.CalibratedClickPosition.Value.X / scaleX) - 18;
                    _clickTargetWindow.Top = (sniper.CalibratedClickPosition.Value.Y / scaleY) - 18;
                }
                try
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(_clickTargetWindow);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        SetWindowDisplayAffinity(helper.Handle, WDA_EXCLUDEFROMCAPTURE);
                        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                        if (_isReticleLocked)
                        {
                            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
                        }
                        else
                        {
                            SetWindowLong(helper.Handle, GWL_EXSTYLE, (exStyle & ~WS_EX_TRANSPARENT & ~WS_EX_NOACTIVATE));
                        }
                    }
                }
                catch { }
            };

            Grid clickGrid = new Grid
            {
                Width = 36,
                Height = 36,
                Background = new SolidColorBrush(Color.FromArgb(2, 56, 189, 248))
            };

            Ellipse clickRing = new Ellipse
            {
                Width = 32,
                Height = 32,
                Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)), // Cyan ring
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            clickGrid.Children.Add(clickRing);

            TextBlock clickLabel = new TextBlock
            {
                Text = "BID",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            clickGrid.Children.Add(clickLabel);

            // Allow dragging the click target marker directly with smooth DragMove
            _clickTargetWindow.LocationChanged += (s, e) =>
            {
                if (!_isReticleLocked && _clickTargetWindow != null && _clickTargetWindow.IsLoaded)
                {
                    try
                    {
                        Point center = _clickTargetWindow.PointToScreen(new Point(18, 18));
                        RobloxBidSniperService.Instance.CalibrateTarget((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch { }
                }
            };

            clickGrid.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && !_isReticleLocked)
                {
                    e.Handled = true;
                    try
                    {
                        _clickTargetWindow.DragMove();
                        Point center = _clickTargetWindow.PointToScreen(new Point(18, 18));
                        RobloxBidSniperService.Instance.CalibrateTarget((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch { }
                }
            };

            _clickTargetWindow.Content = clickGrid;
        }

        private void OnColorSampled(int r, int g, int b, bool isMatch)
        {
            if (!_isVisible || _colorSwatch == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _colorSwatch.Background = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));

                var sniper = RobloxBidSniperService.Instance;
                string modeLabel = sniper.UseCalibratedColor ? "Custom Target" : (sniper.ColorPreset ?? "HSV Green");
                string triggerName = sniper.TriggerMode;

                _colorText.Text = string.Format("#{0:X2}{1:X2}{2:X2} [{3}] • {4}", 
                    r, g, b, 
                    triggerName,
                    isMatch ? "MATCH (Sweet-Spot)" : "Searching...");

                _colorText.Foreground = isMatch 
                    ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) 
                    : new SolidColorBrush(Color.FromRgb(148, 163, 184));

                // Visual reticle ring turns bright emerald on sweet spot match
                // Fill MUST stay Brushes.Transparent to avoid blinding optical sampling
                if (_reticleRing != null)
                {
                    _reticleRing.Stroke = isMatch ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) : new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    _reticleRing.Fill = Brushes.Transparent;
                }
                var tickBrush = isMatch ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) : new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
                if (_reticleHLine != null) _reticleHLine.Stroke = tickBrush;
                if (_tickRight != null) _tickRight.Stroke = tickBrush;
                if (_reticleVLine != null) _reticleVLine.Stroke = tickBrush;
                if (_tickBottom != null) _tickBottom.Stroke = tickBrush;
            }, DispatcherPriority.Background);
        }

        private void OnSnipeFired(int x, int y, int totalHits)
        {
            if (!_isVisible || _hudCard == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _actionFlashText.Text = string.Format("SNIPE #{0} FIRED", totalHits);
                _actionFlashText.Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21)); // Gold pulse
                _hudCard.BorderBrush = new SolidColorBrush(Color.FromRgb(250, 204, 21));

                var sniper = RobloxBidSniperService.Instance;
                string clickDesc = sniper.CalibratedClickPosition.HasValue
                    ? string.Format("({0}, {1})", sniper.CalibratedClickPosition.Value.X, sniper.CalibratedClickPosition.Value.Y)
                    : "Active Mouse";

                _statsText.Text = string.Format("Hits: {0} • Spot: ({1}, {2}) • Click: {3}", totalHits, x, y, clickDesc);

                _flashResetTimer.Stop();
                _flashResetTimer.Start();
            });
        }

        private void OnArmedChanged(bool isArmed)
        {
            if (!_isVisible || _statusBadge == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _statusBadge.Text = isArmed ? "[ARMED]" : "[STANDBY]";
                _statusBadge.Foreground = isArmed ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                if (_statusBadgeBorder != null)
                {
                    _statusBadgeBorder.BorderBrush = isArmed
                        ? new SolidColorBrush(Color.FromArgb(160, 16, 185, 129))
                        : new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                    _statusBadgeBorder.Background = isArmed
                        ? new SolidColorBrush(Color.FromArgb(40, 16, 185, 129))
                        : new SolidColorBrush(Color.FromArgb(160, 30, 41, 59));
                }
            });
        }

        public void SetMarkersClickThrough(bool transparent)
        {
            _isReticleLocked = transparent;
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApplyClickThrough(_reticleWindow, transparent);
                    ApplyClickThrough(_clickTargetWindow, transparent);
                    UpdateLockBadgeVisuals();
                });
            }
        }

        private void UpdateLockBadgeVisuals()
        {
            if (_lockBadge != null && _lockBadgeBorder != null)
            {
                if (_isReticleLocked)
                {
                    _lockBadge.Text = "[🔒 Pass-Through]";
                    _lockBadge.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                    _lockBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                    _lockBadgeBorder.ToolTip = "Clicks pass straight through to Roblox. Click to unlock & drag reticle/bid target.";
                }
                else
                {
                    _lockBadge.Text = "[⇱ Draggable]";
                    _lockBadge.Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21));
                    _lockBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(160, 250, 204, 21));
                    _lockBadgeBorder.ToolTip = "Overlays are freely draggable! Click to lock so clicks pass to Roblox.";
                }
            }
        }

        private static void ApplyClickThrough(Window win, bool transparent)
        {
            if (win == null) return;
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(win);
                if (helper.Handle != IntPtr.Zero)
                {
                    int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                    if (transparent)
                    {
                        SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
                    }
                    else
                    {
                        SetWindowLong(helper.Handle, GWL_EXSTYLE, (exStyle & ~WS_EX_TRANSPARENT & ~WS_EX_NOACTIVATE));
                    }
                }
            }
            catch { }
        }

        private void OnSensorPositionChanged(int physX, int physY)
        {
            if (!_isVisible || _reticleWindow == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                double scaleX = 1.0, scaleY = 1.0;
                GetDpi(_reticleWindow, out scaleX, out scaleY);
                _reticleWindow.Left = (physX / scaleX) - 160;
                _reticleWindow.Top = (physY / scaleY) - 32;

                UpdateStatsText();
            });
        }

        private void OnTrackPositionChanged(int activeX, int activeY, bool hasLock)
        {
            if (!_isVisible || _reticleWindow == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var sniper = RobloxBidSniperService.Instance;
                if (hasLock && sniper.DynamicTrackingEnabled && _trackLockIndicator != null)
                {
                    double scaleX = 1.0, scaleY = 1.0;
                    GetDpi(_reticleWindow, out scaleX, out scaleY);
                    double offsetX = (activeX - sniper.SensorPosition.X) / scaleX;
                    double offsetY = (activeY - sniper.SensorPosition.Y) / scaleY;

                    _trackLockIndicator.RenderTransform = new TranslateTransform(offsetX, offsetY);
                    _trackLockIndicator.Visibility = Visibility.Visible;

                    if (_corridorBox != null)
                    {
                        _corridorBox.BorderBrush = new SolidColorBrush(Color.FromArgb(220, 52, 211, 153));
                        _corridorBox.Background = new SolidColorBrush(Color.FromArgb(30, 52, 211, 153));
                    }
                    if (_corridorLabel != null)
                    {
                        _corridorLabel.Text = string.Format("🎯 SWEET-SPOT LOCKED ({0}, {1})", activeX, activeY);
                        _corridorLabel.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
                    }
                    if (_trackBadge != null)
                    {
                        _trackBadge.Text = "[TRACK: LOCKED]";
                        _trackBadge.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
                    }
                    if (_trackBadgeBorder != null)
                    {
                        _trackBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(200, 52, 211, 153));
                        _trackBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(50, 52, 211, 153));
                    }
                }
                else
                {
                    if (_trackLockIndicator != null)
                    {
                        _trackLockIndicator.Visibility = Visibility.Collapsed;
                    }
                    if (_corridorBox != null)
                    {
                        _corridorBox.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 56, 189, 248));
                        _corridorBox.Background = new SolidColorBrush(Color.FromArgb(10, 56, 189, 248));
                    }
                    if (_corridorLabel != null)
                    {
                        _corridorLabel.Text = sniper.DynamicTrackingEnabled ? "TRACK CORRIDOR (SCANNING)" : "TRACK CORRIDOR (OFF)";
                        _corridorLabel.Foreground = new SolidColorBrush(Color.FromArgb(160, 56, 189, 248));
                    }
                    if (_trackBadge != null)
                    {
                        _trackBadge.Text = sniper.DynamicTrackingEnabled ? "[TRACK: ON]" : "[TRACK: OFF]";
                        _trackBadge.Foreground = sniper.DynamicTrackingEnabled
                            ? new SolidColorBrush(Color.FromRgb(56, 189, 248))
                            : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                    }
                    if (_trackBadgeBorder != null)
                    {
                        _trackBadgeBorder.BorderBrush = sniper.DynamicTrackingEnabled
                            ? new SolidColorBrush(Color.FromArgb(120, 56, 189, 248))
                            : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                        _trackBadgeBorder.Background = sniper.DynamicTrackingEnabled
                            ? new SolidColorBrush(Color.FromArgb(40, 14, 116, 144))
                            : new SolidColorBrush(Color.FromArgb(160, 30, 41, 59));
                    }
                }

                UpdateStatsText();
            }, DispatcherPriority.Background);
        }

        private void OnDynamicTrackingToggled(bool enabled)
        {
            if (!_isVisible || _trackBadge == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _trackBadge.Text = enabled ? "[TRACK: ON]" : "[TRACK: OFF]";
                _trackBadge.Foreground = enabled
                    ? new SolidColorBrush(Color.FromRgb(56, 189, 248))
                    : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                if (_trackBadgeBorder != null)
                {
                    _trackBadgeBorder.BorderBrush = enabled
                        ? new SolidColorBrush(Color.FromArgb(120, 56, 189, 248))
                        : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                    _trackBadgeBorder.Background = enabled
                        ? new SolidColorBrush(Color.FromArgb(40, 14, 116, 144))
                        : new SolidColorBrush(Color.FromArgb(160, 30, 41, 59));
                }
                if (_corridorBox != null)
                {
                    _corridorBox.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
                }
                UpdateStatsText();
            });
        }

        private void OnClickTargetChanged(int? physX, int? physY)
        {
            if (!_isVisible) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (physX.HasValue && physY.HasValue && _clickTargetWindow != null)
                {
                    double scaleX = 1.0, scaleY = 1.0;
                    GetDpi(_clickTargetWindow, out scaleX, out scaleY);
                    _clickTargetWindow.Left = (physX.Value / scaleX) - 18;
                    _clickTargetWindow.Top = (physY.Value / scaleY) - 18;
                    _clickTargetWindow.Show();
                }
                else
                {
                    if (_clickTargetWindow != null) _clickTargetWindow.Hide();
                }
                UpdateStatsText();
            });
        }

        private void OnTargetColorChanged(int r, int g, int b, bool useCalibrated)
        {
            if (!_isVisible) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var sniper = RobloxBidSniperService.Instance;
                string triggerName = sniper.TriggerMode;
                string preset = sniper.ColorPreset ?? "Green";
                if (_modeBadge != null)
                {
                    _modeBadge.Text = useCalibrated
                        ? string.Format("[Custom #{0:X2}{1:X2}{2:X2} • {3}]", r, g, b, triggerName)
                        : string.Format("[{0} • {1}]", triggerName, preset);
                }
                UpdatePresetButtonsVisuals(useCalibrated ? "Custom" : preset);
            });
        }

        private void UpdateStatsText()
        {
            if (_statsText == null) return;
            var sniper = RobloxBidSniperService.Instance;
            string clickDesc = sniper.CalibratedClickPosition.HasValue
                ? string.Format("({0}, {1})", sniper.CalibratedClickPosition.Value.X, sniper.CalibratedClickPosition.Value.Y)
                : "Active Mouse";

            string trackDesc = sniper.DynamicTrackingEnabled
                ? (sniper.HasTrackLock && sniper.CurrentTrackedPosition.HasValue
                    ? string.Format("Track: LOCKED ({0}, {1})", sniper.CurrentTrackedPosition.Value.X, sniper.CurrentTrackedPosition.Value.Y)
                    : "Track: SCANNING")
                : "Track: OFF";

            _statsText.Text = string.Format("Hits: {0} • Spot: ({1}, {2}) • {3} • Bid: {4}", 
                sniper.SuccessfulSnipes, 
                sniper.SensorPosition.X, 
                sniper.SensorPosition.Y, 
                trackDesc,
                clickDesc);
        }

        private void UpdateDisplay()
        {
            var sniper = RobloxBidSniperService.Instance;
            OnArmedChanged(sniper.IsArmed);
            OnDynamicTrackingToggled(sniper.DynamicTrackingEnabled);
            OnSensorPositionChanged(sniper.SensorPosition.X, sniper.SensorPosition.Y);
            if (sniper.CalibratedClickPosition.HasValue)
            {
                OnClickTargetChanged(sniper.CalibratedClickPosition.Value.X, sniper.CalibratedClickPosition.Value.Y);
            }
            OnTargetColorChanged(sniper.CalibratedR, sniper.CalibratedG, sniper.CalibratedB, sniper.UseCalibratedColor);
            UpdateLockBadgeVisuals();
            UpdatePresetButtonsVisuals(sniper.UseCalibratedColor ? "Custom" : (sniper.ColorPreset ?? "Green"));
        }

        public void Dispose()
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try { if (_hudWindow != null) _hudWindow.Close(); } catch { }
                    try { if (_reticleWindow != null) _reticleWindow.Close(); } catch { }
                    try { if (_clickTargetWindow != null) _clickTargetWindow.Close(); } catch { }
                    _hudWindow = null;
                    _reticleWindow = null;
                    _clickTargetWindow = null;
                });
            }
        }
    }
}
