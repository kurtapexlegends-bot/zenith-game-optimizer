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
        private bool _isLocked = false;
        private Border _corridorBox;
        private TextBlock _corridorLabel;
        private Border _trackLockIndicator;
        private Ellipse _reticleRing;
        private Line _reticleHLine, _reticleVLine, _tickRight, _tickBottom;

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
                Width = 320,
                Height = 64,
                Cursor = Cursors.SizeAll
            };

            double scaleX = 1.0, scaleY = 1.0;
            _window.Loaded += (s, e) =>
            {
                GetDpi(_window, out scaleX, out scaleY);
                _window.Left = (sniper.SensorPosition.X / scaleX) - 160;
                _window.Top = (sniper.SensorPosition.Y / scaleY) - 32;
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
                catch { }
            };

            Grid reticleGrid = new Grid
            {
                Width = 320,
                Height = 64,
                Background = new SolidColorBrush(Color.FromArgb(2, 16, 185, 129))
            };

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

            _reticleHLine = new Line { X1 = 138, Y1 = 32, X2 = 142, Y2 = 32, Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), StrokeThickness = 1.5, IsHitTestVisible = false };
            reticleGrid.Children.Add(_reticleHLine);

            _tickRight = new Line { X1 = 178, Y1 = 32, X2 = 182, Y2 = 32, Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), StrokeThickness = 1.5, IsHitTestVisible = false };
            reticleGrid.Children.Add(_tickRight);

            _reticleVLine = new Line { X1 = 160, Y1 = 10, X2 = 160, Y2 = 14, Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), StrokeThickness = 1.5, IsHitTestVisible = false };
            reticleGrid.Children.Add(_reticleVLine);

            _tickBottom = new Line { X1 = 160, Y1 = 50, X2 = 160, Y2 = 54, Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), StrokeThickness = 1.5, IsHitTestVisible = false };
            reticleGrid.Children.Add(_tickBottom);

            _window.LocationChanged += (s, e) =>
            {
                if (!_isLocked && _window != null && _window.IsLoaded)
                {
                    try
                    {
                        Point center = _window.PointToScreen(new Point(160, 32));
                        RobloxBidSniperService.Instance.SetSensorPosition((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch { }
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
                        Point center = _window.PointToScreen(new Point(160, 32));
                        RobloxBidSniperService.Instance.SetSensorPosition((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch { }
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
            catch { }
        }

        public void SetReticleColor(Color c)
        {
            if (_reticleRing != null) _reticleRing.Stroke = new SolidColorBrush(c);
        }

        public void UpdatePosition(int x, int y)
        {
            if (_window == null) return;
            double scaleX, scaleY;
            GetDpi(_window, out scaleX, out scaleY);
            _window.Left = (x / scaleX) - 160;
            _window.Top = (y / scaleY) - 32;
        }

        public void UpdateCorridorState(int trackX, int trackY, bool hasLock)
        {
            if (_trackLockIndicator == null || _corridorBox == null) return;
            var sniper = RobloxBidSniperService.Instance;
            _corridorBox.Visibility = sniper.DynamicTrackingEnabled ? Visibility.Visible : Visibility.Collapsed;

            if (hasLock && sniper.DynamicTrackingEnabled)
            {
                _trackLockIndicator.Visibility = Visibility.Visible;
                int offsetX = trackX - sniper.SensorPosition.X;
                _trackLockIndicator.Margin = new Thickness(offsetX, 0, 0, 0);
            }
            else
            {
                _trackLockIndicator.Visibility = Visibility.Collapsed;
            }
        }

        public void Show() { if (_window != null) _window.Show(); }
        public void Hide() { if (_window != null) _window.Hide(); }
        public void Close() { if (_window != null) { try { _window.Close(); } catch { } _window = null; } }

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
