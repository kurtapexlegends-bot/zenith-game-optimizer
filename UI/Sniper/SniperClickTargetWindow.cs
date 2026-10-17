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
    public class SniperClickTargetWindow
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

        public Window Window { get { return _window; } }

        public void Initialize(bool isLocked)
        {
            _isLocked = isLocked;
            var sniper = RobloxBidSniperService.Instance;

            _window = new Window
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
            _window.Loaded += (s, e) =>
            {
                GetDpi(_window, out scaleX, out scaleY);
                int targetX = sniper.CalibratedClickPosition.HasValue ? sniper.CalibratedClickPosition.Value.X : (int)(SystemParameters.PrimaryScreenWidth / 2);
                int targetY = sniper.CalibratedClickPosition.HasValue ? sniper.CalibratedClickPosition.Value.Y : (int)(SystemParameters.PrimaryScreenHeight * 0.78);
                _window.Left = (targetX / scaleX) - 18;
                _window.Top = (targetY / scaleY) - 18;
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
                Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
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

            _window.LocationChanged += (s, e) =>
            {
                if (!_isLocked && _window != null && _window.IsLoaded)
                {
                    try
                    {
                        Point center = _window.PointToScreen(new Point(18, 18));
                        RobloxBidSniperService.Instance.CalibrateTarget((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch { }
                }
            };

            clickGrid.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && !_isLocked)
                {
                    e.Handled = true;
                    try
                    {
                        _window.DragMove();
                        Point center = _window.PointToScreen(new Point(18, 18));
                        RobloxBidSniperService.Instance.CalibrateTarget((int)Math.Round(center.X), (int)Math.Round(center.Y));
                    }
                    catch { }
                }
            };

            _window.Content = clickGrid;
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

        public void UpdatePosition(int? targetX, int? targetY)
        {
            if (_window == null) return;
            double scaleX, scaleY;
            GetDpi(_window, out scaleX, out scaleY);
            if (targetX.HasValue && targetY.HasValue)
            {
                _window.Left = (targetX.Value / scaleX) - 18;
                _window.Top = (targetY.Value / scaleY) - 18;
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
