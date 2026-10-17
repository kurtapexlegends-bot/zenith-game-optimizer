using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.UI
{
    public partial class FpsMeterOverlay
    {
        #region Win32 Interop
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
        #endregion

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
                MinWidth = 175,
                MinHeight = 28,
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

            // Seamless repositioning: dragging card moves overlay anywhere by default
            _overlayWindow.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && !_isClickThrough)
                {
                    _isDragging = true;
                    try
                    {
                        _overlayWindow.DragMove();
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                    finally
                    {
                        _isDragging = false;
                        SavePosition();
                    }
                }
            };

            _overlayWindow.MouseRightButtonDown += (s, e) =>
            {
                e.Handled = true;
                ToggleLock();
            };

            _overlayWindow.LocationChanged += (s, e) =>
            {
                if (!_isClickThrough && !_isDragging && _overlayWindow != null)
                {
                    SavePosition();
                }
            };

            _cardBorder = new Border
            {
                Background = BrushBgLocked,
                BorderBrush = BrushSlateBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = _isClickThrough ? Cursors.Arrow : Cursors.SizeAll,
                ToolTip = "Zenith FPS Meter • Drag to reposition",
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
                Fill = BrushEmerald,
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
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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

                // Clamp to virtual screen boundaries
                double vLeft = SystemParameters.VirtualScreenLeft;
                double vTop = SystemParameters.VirtualScreenTop;
                double vWidth = SystemParameters.VirtualScreenWidth;
                double vHeight = SystemParameters.VirtualScreenHeight;

                double actualW = _overlayWindow.ActualWidth > 0 ? _overlayWindow.ActualWidth : 185;
                double actualH = _overlayWindow.ActualHeight > 0 ? _overlayWindow.ActualHeight : 30;

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
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
        }
    }
}
