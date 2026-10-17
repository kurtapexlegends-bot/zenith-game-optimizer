using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ZenithOptimizer.Services;

namespace ZenithOptimizer.UI.Sniper
{
    public class SniperReticleWindow
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
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private Window _window;
        private bool _isLocked = true;
        private Ellipse _reticleRing;
        private Border _trackLockIndicator;

        public Window Window { get { return _window; } }

        public void Initialize(bool isLocked)
        {
            _isLocked = isLocked;
            var sniper = RobloxBidSniperService.Instance;

            _window = new Window
            {
                Title = "Zenith Sensor Target Reticle",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Width = 48,
                Height = 48,
                Cursor = Cursors.SizeAll
            };

            double scaleX = 1.0, scaleY = 1.0;
            _window.Loaded += (s, e) =>
            {
                GetDpi(_window, out scaleX, out scaleY);
                _window.Left = (sniper.SensorPosition.X / scaleX) - 24;
                _window.Top = (sniper.SensorPosition.Y / scaleY) - 24;
                try
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(_window);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        SetWindowDisplayAffinity(helper.Handle, WDA_EXCLUDEFROMCAPTURE);
                        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                        if (_isLocked) SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
                        else SetWindowLong(helper.Handle, GWL_EXSTYLE, (exStyle & ~WS_EX_TRANSPARENT & ~WS_EX_NOACTIVATE));
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            };

            Grid reticleGrid = new Grid
            {
                Width = 48,
                Height = 48,
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0))
            };

            _reticleRing = new Ellipse
            {
                Width = 32,
                Height = 32,
                Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                StrokeThickness = 1.5,
                Fill = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            reticleGrid.Children.Add(_reticleRing);

            // Crosshair ticks with open central optical corridor (Anti-Self-Blinding)
            reticleGrid.Children.Add(new Line { X1 = 4, Y1 = 24, X2 = 8, Y2 = 24, Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), StrokeThickness = 1.5, IsHitTestVisible = false });
            reticleGrid.Children.Add(new Line { X1 = 40, Y1 = 24, X2 = 44, Y2 = 24, Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), StrokeThickness = 1.5, IsHitTestVisible = false });
            reticleGrid.Children.Add(new Line { X1 = 24, Y1 = 4, X2 = 24, Y2 = 8, Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), StrokeThickness = 1.5, IsHitTestVisible = false });
            reticleGrid.Children.Add(new Line { X1 = 24, Y1 = 40, X2 = 24, Y2 = 44, Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), StrokeThickness = 1.5, IsHitTestVisible = false });

            _trackLockIndicator = new Border
            {
                Width = 14,
                Height = 14,
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(7),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            reticleGrid.Children.Add(_trackLockIndicator);

            _window.LocationChanged += (s, e) =>
            {
                if (!_isLocked && _window != null && _window.IsLoaded)
                {
                    try
                    {
                        Point center = _window.PointToScreen(new Point(24, 24));
                        RobloxBidSniperService.Instance.SetSensorPosition((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                }
            };

            reticleGrid.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && !_isLocked)
                {
                    e.Handled = true;
                    try
                    {
                        _window.DragMove();
                        Point center = _window.PointToScreen(new Point(24, 24));
                        RobloxBidSniperService.Instance.SetSensorPosition((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                }
            };

            _window.Content = reticleGrid;
        }

        public void SetLockState(bool isLocked)
        {
            _isLocked = isLocked;
            if (_window == null) return;
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(_window);
                if (helper.Handle != IntPtr.Zero)
                {
                    int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                    if (_isLocked) SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
                    else SetWindowLong(helper.Handle, GWL_EXSTYLE, (exStyle & ~WS_EX_TRANSPARENT & ~WS_EX_NOACTIVATE));
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
        }

        public void SetReticleColor(Color c)
        {
            if (_reticleRing != null)
            {
                _reticleRing.Stroke = new SolidColorBrush(c);
            }
        }

        public void UpdatePosition(int x, int y)
        {
            if (_window == null) return;
            double scaleX, scaleY;
            GetDpi(_window, out scaleX, out scaleY);
            _window.Left = (x / scaleX) - 24;
            _window.Top = (y / scaleY) - 24;
        }

        public void UpdateCorridorState(int trackX, int trackY, bool hasLock)
        {
            if (_trackLockIndicator == null) return;
            var sniper = RobloxBidSniperService.Instance;
            if (hasLock && sniper.DynamicTrackingEnabled)
            {
                _trackLockIndicator.Visibility = Visibility.Visible;
                int offsetX = Math.Max(-20, Math.Min(20, trackX - sniper.SensorPosition.X));
                _trackLockIndicator.Margin = new Thickness(offsetX, 0, 0, 0);
            }
            else
            {
                _trackLockIndicator.Visibility = Visibility.Collapsed;
            }
        }

        public void Show() { if (_window != null) _window.Show(); }
        public void Hide() { if (_window != null) _window.Hide(); }
        public void Close() { if (_window != null) { try { _window.Close(); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); } _window = null; } }

        private static void GetDpi(Visual v, out double scaleX, out double scaleY)
        {
            scaleX = 1.0; scaleY = 1.0;
            var source = PresentationSource.FromVisual(v);
            if (source != null && source.CompositionTarget != null)
            {
                scaleX = source.CompositionTarget.TransformToDevice.M11;
                scaleY = source.CompositionTarget.TransformToDevice.M22;
            }
        }
    }
}
