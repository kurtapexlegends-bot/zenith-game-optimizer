using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;

namespace ZenithOptimizer.UI
{
    /// <summary>
    /// Sleek, transparent on-screen FPS and frametime meter badge.
    /// Operates as a topmost click-through micro-overlay with zero impact on game performance.
    /// Supports hotkey toggling (F10), repositioning (Ctrl+F10 / double-click), multi-monitor
    /// virtual screen boundary persistence, and smooth color grading based on frame pacing.
    /// </summary>
    public partial class FpsMeterOverlay : IDisposable
    {
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
        private Ellipse _statusDot;
        private TextBlock _fpsText;
        private TextBlock _frameTimeText;
        private TextBlock _lowText;
        private HwndSource _hwndSource;
        private volatile bool _isClickThrough = false;
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
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                FpsMeterService.Instance.OnFpsUpdated += OnFpsUpdated;
            }));
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
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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

        private DispatcherTimer _autoLockTimer;

        /// <summary>
        /// Dispatched when reposition / lock state transitions (true = reposition mode active, false = locked click-through).
        /// </summary>
        public event Action<bool> OnLockStateChanged;

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
                    _cardBorder.BorderBrush = BrushSlateBorder;
                    _cardBorder.Background = BrushBgLocked;
                    _cardBorder.ToolTip = _isClickThrough
                        ? "Zenith FPS Meter • [Ctrl+F10] to unlock drag"
                        : "Zenith FPS Meter • Drag anywhere to position";
                }

                if (_autoLockTimer != null)
                {
                    _autoLockTimer.Stop();
                }

                try
                {
                    if (OnLockStateChanged != null)
                    {
                        OnLockStateChanged(!_isClickThrough);
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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

        public void Dispose()
        {
            UnregisterHotkeys();
            if (_autoLockTimer != null)
            {
                _autoLockTimer.Stop();
                _autoLockTimer = null;
            }
            FpsMeterService.Instance.OnFpsUpdated -= OnFpsUpdated;
            Hide();
        }
    }
}
