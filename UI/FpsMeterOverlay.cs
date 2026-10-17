using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer.UI
{
    /// <summary>
    /// Sleek, transparent on-screen FPS and frametime meter badge.
    /// Operates as a topmost click-through micro-overlay with zero impact on game performance.
    /// Supports hotkey toggling (F10), smooth color grading based on frame pacing, and custom positioning.
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

        private Window _overlayWindow;
        private Border _cardBorder;
        private Ellipse _statusDot;
        private TextBlock _fpsText;
        private TextBlock _frameTimeText;
        private TextBlock _lowText;
        private bool _isClickThrough = true;
        private bool _isVisible = false;

        public bool IsVisible { get { return _isVisible; } }
        public bool IsClickThrough { get { return _isClickThrough; } }

        public void Initialize()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (_overlayWindow != null) return;
                CreateWindow();
                FpsMeterService.Instance.OnFpsUpdated += OnFpsUpdated;
            }));
        }

        private void CreateWindow()
        {
            var settings = OptimizationSettings.Load();
            double screenW = SystemParameters.PrimaryScreenWidth;
            double left = (settings != null && settings.FpsMeterX >= 0) ? settings.FpsMeterX : Math.Max(20, screenW - 220);
            double top = (settings != null && settings.FpsMeterY >= 0) ? settings.FpsMeterY : 16;

            _overlayWindow = new Window
            {
                Title = "Zenith FPS Meter Overlay",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Width = 195,
                Height = 32,
                Left = left,
                Top = top
            };

            _overlayWindow.Loaded += (s, e) =>
            {
                ApplyWindowStyles();
            };

            _overlayWindow.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && !_isClickThrough)
                {
                    try
                    {
                        _overlayWindow.DragMove();
                        SavePosition();
                    }
                    catch { }
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

            _cardBorder.Child = panel;
            _overlayWindow.Content = _cardBorder;
        }

        private void ApplyWindowStyles()
        {
            if (_overlayWindow == null) return;

            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(_overlayWindow);
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
                    _cardBorder.BorderBrush = _isClickThrough 
                        ? new SolidColorBrush(Color.FromArgb(160, 51, 65, 85))
                        : new SolidColorBrush(Color.FromArgb(220, 250, 204, 21));
                }
            }));
        }

        public void ToggleClickThrough()
        {
            SetClickThrough(!_isClickThrough);
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
                        _statusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald
                    }
                    else if (fps >= 30.0)
                    {
                        _statusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber/Gold
                    }
                    else
                    {
                        _statusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Crimson
                    }
                }
            }));
        }

        private void SavePosition()
        {
            if (_overlayWindow == null) return;
            try
            {
                var s = OptimizationSettings.Load();
                if (s != null)
                {
                    s.FpsMeterX = (int)Math.Round(_overlayWindow.Left);
                    s.FpsMeterY = (int)Math.Round(_overlayWindow.Top);
                    s.Save();
                }
            }
            catch { }
        }

        public void Dispose()
        {
            FpsMeterService.Instance.OnFpsUpdated -= OnFpsUpdated;
            Hide();
        }
    }
}
