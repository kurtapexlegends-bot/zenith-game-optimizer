using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer.UI
{
    /// <summary>
    /// Sleek, transparent on-screen FPS and frametime meter badge.
    /// Operates as a topmost click-through micro-overlay with zero impact on game performance.
    /// Supports hotkey toggling (F10), lock/unlock drag mode (Ctrl+F10 / double-click),
    /// multi-monitor position persistence, and smooth color grading based on frame pacing.
    /// </summary>
    public class FpsMeterOverlay : IDisposable
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
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_TOGGLE = 0xF010;
        private const int HOTKEY_ID_LOCK = 0xF011;
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_F10 = 0x79;
        private const int WM_HOTKEY = 0x0312;

        private static FpsMeterOverlay _instance;
        private static readonly object _syncRoot = new object();

        public static FpsMeterOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new FpsMeterOverlay();
                        }
                    }
                }
                return _instance;
            }
        }

        private static SolidColorBrush FreezeBrush(SolidColorBrush brush)
        {
            if (brush.CanFreeze) brush.Freeze();
            return brush;
        }

        private static readonly SolidColorBrush BrushEmerald = FreezeBrush(new SolidColorBrush(Color.FromRgb(16, 185, 129)));
        private static readonly SolidColorBrush BrushAmber = FreezeBrush(new SolidColorBrush(Color.FromRgb(245, 158, 11)));
        private static readonly SolidColorBrush BrushCrimson = FreezeBrush(new SolidColorBrush(Color.FromRgb(239, 68, 68)));
        private static readonly SolidColorBrush BrushSlateBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(160, 51, 65, 85)));
        private static readonly SolidColorBrush BrushAmberBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(240, 245, 158, 11)));
        private static readonly SolidColorBrush BrushBgLocked = FreezeBrush(new SolidColorBrush(Color.FromArgb(235, 15, 23, 42)));
        private static readonly SolidColorBrush BrushBgUnlocked = FreezeBrush(new SolidColorBrush(Color.FromArgb(248, 15, 23, 42)));

        private Window _overlayWindow;
        private Border _cardBorder;
        private Border _dragGrip;
        private Ellipse _statusDot;
        private TextBlock _fpsText;
        private TextBlock _frameTimeText;
        private TextBlock _lowText;
        private Border _lockBadge;
        private TextBlock _lockBadgeText;
        private HwndSource _hwndSource;
        private volatile bool _isClickThrough = true;
        private volatile bool _isVisible = false;
        private bool _isHookRegistered = false;
        private bool _isDragging = false;
        private DateTime _lastToggleTime = DateTime.MinValue;
        private DateTime _lastLockToggleTime = DateTime.MinValue;
        private readonly object _toggleLock = new object();

        public bool IsVisible { get { return _isVisible; } }
        public bool IsClickThrough { get { return _isClickThrough; } }
        public bool IsLocked { get { return _isClickThrough; } }
        public Window OverlayWindow { get { return _overlayWindow; } }

        public int X
        {
            get { return _overlayWindow != null ? (int)Math.Round(_overlayWindow.Left) : -1; }
        }

        public int Y
        {
            get { return _overlayWindow != null ? (int)Math.Round(_overlayWindow.Top) : -1; }
        }

        public void Initialize()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (_overlayWindow != null) return;
                CreateWindow();
                try
                {
                    var helper = new WindowInteropHelper(_overlayWindow);
                    helper.EnsureHandle();
                    RegisterHotkeys();
                }
                catch { }
                FpsMeterService.Instance.OnFpsUpdated += OnFpsUpdated;
            }));
        }

        private void CreateWindow()
        {
            var settings = OptimizationSettings.Load();
            double vLeft = SystemParameters.VirtualScreenLeft;
            double vTop = SystemParameters.VirtualScreenTop;
            double vWidth = SystemParameters.VirtualScreenWidth;
            double vHeight = SystemParameters.VirtualScreenHeight;

            double left = Math.Max(20, SystemParameters.PrimaryScreenWidth - 225);
            double top = 16;

            if (settings != null && settings.FpsMeterX != -1 && settings.FpsMeterY != -1)
            {
                if (settings.FpsMeterX >= vLeft && settings.FpsMeterX < (vLeft + vWidth - 50) &&
                    settings.FpsMeterY >= vTop && settings.FpsMeterY < (vTop + vHeight - 30))
                {
                    left = settings.FpsMeterX;
                    top = settings.FpsMeterY;
                }
            }

            _overlayWindow = new Window
            {
                Title = "Zenith FPS Meter Overlay",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 195,
                MinHeight = 30,
                Left = left,
                Top = top
            };

            _overlayWindow.SourceInitialized += (s, e) =>
            {
                ApplyWindowStyles();
                RegisterHotkeys();
            };

            _overlayWindow.Loaded += (s, e) =>
            {
                ApplyWindowStyles();
            };

            // Dragging & Lock Toggle Handlers
            _overlayWindow.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    // Double-click toggles locked / draggable mode
                    ToggleLock();
                    e.Handled = true;
                    return;
                }

                if (e.ChangedButton == MouseButton.Left && !_isClickThrough)
                {
                    _isDragging = true;
                    try
                    {
                        _overlayWindow.DragMove();
                    }
                    catch { }
                    finally
                    {
                        _isDragging = false;
                        SavePosition();
                    }
                }
            };

            _overlayWindow.MouseRightButtonDown += (s, e) =>
            {
                // Right-click when unlocked locks back into click-through mode
                if (!_isClickThrough)
                {
                    Lock();
                    e.Handled = true;
                }
            };

            _overlayWindow.LocationChanged += (s, e) =>
            {
                // Never thrash disk writes during continuous mouse drag
                if (!_isClickThrough && !_isDragging && _overlayWindow != null)
                {
                    SavePosition();
                }
            };

            _cardBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 15, 23, 42)), // 92% Deep Slate Glass
                BorderBrush = new SolidColorBrush(Color.FromArgb(160, 51, 65, 85)), // Subtle Slate Border
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = _isClickThrough ? Cursors.Arrow : Cursors.SizeAll,
                ToolTip = "FPS Meter [LOCKED] • Click-through active. Press [Ctrl+F10] to unlock & reposition",
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 2,
                    Opacity = 0.5,
                    BlurRadius = 6
                }
            };

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Drag Grip indicator (visible when unlocked)
            _dragGrip = new Border
            {
                Width = 14,
                Height = 16,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                Cursor = Cursors.SizeAll
            };

            Grid gripDots = new Grid
            {
                Width = 8,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            gripDots.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
            gripDots.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) });
            gripDots.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2) });
            gripDots.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });

            Action<int, int> addDot = (r, c) =>
            {
                Ellipse d = new Ellipse
                {
                    Width = 2.5,
                    Height = 2.5,
                    Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Amber
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(d, r);
                Grid.SetColumn(d, c);
                gripDots.Children.Add(d);
            };
            addDot(0, 0); addDot(0, 2);
            addDot(2, 0); addDot(2, 2);
            addDot(4, 0); addDot(4, 2);
            _dragGrip.Child = gripDots;
            panel.Children.Add(_dragGrip);

            // Status LED dot
            _statusDot = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Emerald
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(_statusDot);

            // FPS Number
            _fpsText = new TextBlock
            {
                Text = "60 FPS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(_fpsText);

            // Separator
            panel.Children.Add(new TextBlock
            {
                Text = " • ",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(140, 148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            });

            // FrameTime (ms)
            _frameTimeText = new TextBlock
            {
                Text = "16.6ms",
                FontSize = 10,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(_frameTimeText);

            // Separator
            panel.Children.Add(new TextBlock
            {
                Text = " • ",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(140, 148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            });

            // 1% Low
            _lowText = new TextBlock
            {
                Text = "1% 58",
                FontSize = 9.5,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromArgb(200, 148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(_lowText);

            // Unlock / Drag Pill Badge (visible when unlocked)
            _lockBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(60, 245, 158, 11)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(160, 245, 158, 11)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                Cursor = Cursors.Hand
            };
            _lockBadge.MouseLeftButtonDown += (s, e) =>
            {
                Lock();
                e.Handled = true;
            };

            _lockBadgeText = new TextBlock
            {
                Text = "DRAG",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                VerticalAlignment = VerticalAlignment.Center
            };
            _lockBadge.Child = _lockBadgeText;
            panel.Children.Add(_lockBadge);

            _cardBorder.Child = panel;
            _overlayWindow.Content = _cardBorder;
        }

        private void RegisterHotkeys()
        {
            if (_overlayWindow == null || _isHookRegistered) return;

            try
            {
                var helper = new WindowInteropHelper(_overlayWindow);
                IntPtr hWnd = helper.Handle;
                if (hWnd != IntPtr.Zero)
                {
                    _hwndSource = HwndSource.FromHwnd(hWnd);
                    if (_hwndSource != null)
                    {
                        _hwndSource.AddHook(HwndMessageHook);
                        _isHookRegistered = true;
                    }
                    RegisterHotKey(hWnd, HOTKEY_ID_TOGGLE, MOD_NONE | MOD_NOREPEAT, VK_F10);
                    RegisterHotKey(hWnd, HOTKEY_ID_LOCK, MOD_CONTROL | MOD_NOREPEAT, VK_F10);
                }
            }
            catch { }
        }

        private void UnregisterHotkeys()
        {
            if (_overlayWindow == null) return;

            try
            {
                var helper = new WindowInteropHelper(_overlayWindow);
                IntPtr hWnd = helper.Handle;
                if (hWnd != IntPtr.Zero)
                {
                    UnregisterHotKey(hWnd, HOTKEY_ID_TOGGLE);
                    UnregisterHotKey(hWnd, HOTKEY_ID_LOCK);
                }
                if (_hwndSource != null && _isHookRegistered)
                {
                    _hwndSource.RemoveHook(HwndMessageHook);
                    _isHookRegistered = false;
                }
            }
            catch { }
        }

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_LOCK)
                {
                    ToggleLock();
                    handled = true;
                }
                else if (id == HOTKEY_ID_TOGGLE)
                {
                    Toggle();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void ApplyWindowStyles()
        {
            if (_overlayWindow == null) return;

            try
            {
                var helper = new WindowInteropHelper(_overlayWindow);
                IntPtr hWnd = helper.Handle;
                if (hWnd != IntPtr.Zero)
                {
                    SetWindowDisplayAffinity(hWnd, WDA_EXCLUDEFROMCAPTURE);
                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED;
                    if (_isClickThrough)
                    {
                        exStyle |= WS_EX_TRANSPARENT;
                    }
                    else
                    {
                        exStyle &= ~WS_EX_TRANSPARENT;
                    }
                    SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);
                    SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
            }
            catch { }
        }

        public void SetClickThrough(bool clickThrough)
        {
            _isClickThrough = clickThrough;
            if (Application.Current == null || _overlayWindow == null) return;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                ApplyWindowStyles();
                if (_cardBorder != null)
                {
                    _cardBorder.Cursor = _isClickThrough ? Cursors.Arrow : Cursors.SizeAll;
                    _cardBorder.BorderBrush = _isClickThrough ? BrushSlateBorder : BrushAmberBorder;
                    _cardBorder.Background = _isClickThrough ? BrushBgLocked : BrushBgUnlocked;
                    _cardBorder.ToolTip = _isClickThrough
                        ? "FPS Meter [LOCKED] • Click-through enabled. Press [Ctrl+F10] to unlock & move"
                        : "FPS Meter [UNLOCKED] • Drag badge anywhere. Double-click or Right-click to Lock [Ctrl+F10]";
                }

                if (_dragGrip != null)
                {
                    _dragGrip.Visibility = _isClickThrough ? Visibility.Collapsed : Visibility.Visible;
                }

                if (_lockBadge != null)
                {
                    _lockBadge.Visibility = _isClickThrough ? Visibility.Collapsed : Visibility.Visible;
                }
            }));
        }

        public void ToggleLock()
        {
            lock (_toggleLock)
            {
                if ((DateTime.UtcNow - _lastLockToggleTime).TotalMilliseconds < 250) return;
                _lastLockToggleTime = DateTime.UtcNow;
            }
            SetClickThrough(!_isClickThrough);
        }

        public void Lock()
        {
            SetClickThrough(true);
        }

        public void Unlock()
        {
            SetClickThrough(false);
        }

        public void SetPosition(int x, int y)
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (_overlayWindow == null) Initialize();
                if (_overlayWindow != null)
                {
                    _overlayWindow.Left = x;
                    _overlayWindow.Top = y;
                    SavePosition();
                }
            }));
        }

        public void Show()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                Initialize();
                if (_overlayWindow != null)
                {
                    _overlayWindow.Show();
                    _isVisible = true;
                    ApplyWindowStyles();
                    FpsMeterService.Instance.Resume();
                    FpsMeterService.Instance.Start();

                    // Persist state
                    var s = OptimizationSettings.Load();
                    if (s != null)
                    {
                        s.IsFpsMeterEnabled = true;
                        s.Save();
                    }
                }
            }));
        }

        public void Hide()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (_overlayWindow != null)
                {
                    _overlayWindow.Hide();
                    _isVisible = false;
                    FpsMeterService.Instance.Pause();
                    FpsMeterService.Instance.Stop();

                    // Persist state
                    var s = OptimizationSettings.Load();
                    if (s != null)
                    {
                        s.IsFpsMeterEnabled = false;
                        s.Save();
                    }
                }
            }));
        }

        public void Toggle()
        {
            lock (_toggleLock)
            {
                if ((DateTime.UtcNow - _lastToggleTime).TotalMilliseconds < 250) return;
                _lastToggleTime = DateTime.UtcNow;
            }
            if (_isVisible) Hide();
            else Show();
        }

        private void OnFpsUpdated(double fps, double frametimeMs, double onePercentLow)
        {
            if (!_isVisible || _overlayWindow == null || Application.Current == null) return;

            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (_fpsText != null) _fpsText.Text = string.Format("{0:0} FPS", fps);
                if (_frameTimeText != null) _frameTimeText.Text = string.Format("{0:0.0}ms", frametimeMs);
                if (_lowText != null) _lowText.Text = string.Format("1% {0:0}", onePercentLow);

                if (_statusDot != null)
                {
                    if (fps >= 60.0)
                    {
                        _statusDot.Fill = BrushEmerald;
                    }
                    else if (fps >= 30.0)
                    {
                        _statusDot.Fill = BrushAmber;
                    }
                    else
                    {
                        _statusDot.Fill = BrushCrimson;
                    }
                }
            }));
        }

        public void SavePosition()
        {
            if (_overlayWindow == null) return;
            try
            {
                if (double.IsNaN(_overlayWindow.Left) || double.IsNaN(_overlayWindow.Top) ||
                    double.IsInfinity(_overlayWindow.Left) || double.IsInfinity(_overlayWindow.Top))
                {
                    return;
                }

                // Clamp to virtual screen boundaries so the badge can never be lost off-screen
                double vLeft = SystemParameters.VirtualScreenLeft;
                double vTop = SystemParameters.VirtualScreenTop;
                double vWidth = SystemParameters.VirtualScreenWidth;
                double vHeight = SystemParameters.VirtualScreenHeight;

                double actualW = _overlayWindow.ActualWidth > 0 ? _overlayWindow.ActualWidth : 205;
                double actualH = _overlayWindow.ActualHeight > 0 ? _overlayWindow.ActualHeight : 32;

                double clampedX = Math.Max(vLeft, Math.Min(_overlayWindow.Left, vLeft + vWidth - actualW));
                double clampedY = Math.Max(vTop, Math.Min(_overlayWindow.Top, vTop + vHeight - actualH));

                if (Math.Abs(_overlayWindow.Left - clampedX) > 1.0) _overlayWindow.Left = clampedX;
                if (Math.Abs(_overlayWindow.Top - clampedY) > 1.0) _overlayWindow.Top = clampedY;

                int x = (int)Math.Round(clampedX);
                int y = (int)Math.Round(clampedY);

                var s = OptimizationSettings.Load();
                if (s != null)
                {
                    s.FpsMeterX = x;
                    s.FpsMeterY = y;
                    s.Save();
                }
            }
            catch { }
        }

        public void Dispose()
        {
            UnregisterHotkeys();
            FpsMeterService.Instance.OnFpsUpdated -= OnFpsUpdated;
            Hide();
        }
    }
}
