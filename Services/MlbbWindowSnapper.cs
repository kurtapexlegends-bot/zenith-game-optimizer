using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace ZenithOptimizer.Services
{
    public enum DockEdge
    {
        Right,
        Left
    }

    public class MlbbWindowSnapper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width
            {
                get { return Right - Left; }
            }

            public int Height
            {
                get { return Bottom - Top; }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        private readonly Window _overlayWindow;
        private readonly DispatcherTimer _snapTimer;
        private IntPtr _blueStacksHwnd;
        private RECT _lastTargetRect;

        public DockEdge CurrentDock { get; set; }
        public bool IsCollapsed { get; set; }

        public IntPtr BlueStacksHwnd
        {
            get { return _blueStacksHwnd; }
        }

        public bool IsAttached
        {
            get { return _blueStacksHwnd != IntPtr.Zero && IsWindow(_blueStacksHwnd); }
        }

        public bool AutoSnapEnabled { get; set; }

        public event Action<bool> OnAttachmentChanged;

        public MlbbWindowSnapper(Window overlayWindow)
        {
            if (overlayWindow == null) throw new ArgumentNullException("overlayWindow");
            _overlayWindow = overlayWindow;

            _blueStacksHwnd = IntPtr.Zero;
            CurrentDock = DockEdge.Right;
            AutoSnapEnabled = true;
            IsCollapsed = false;

            _snapTimer = new DispatcherTimer();
            _snapTimer.Interval = TimeSpan.FromMilliseconds(200);
            _snapTimer.Tick += SnapTimer_Tick;
        }

        public void Start()
        {
            _snapTimer.Start();
        }

        public void Stop()
        {
            _snapTimer.Stop();
        }

        private void SnapTimer_Tick(object sender, EventArgs e)
        {
            if (!AutoSnapEnabled) return;

            // Validate cached handle or search for HD-Player
            if (!IsAttached)
            {
                IntPtr found = FindBlueStacksWindow();
                if (found != _blueStacksHwnd)
                {
                    _blueStacksHwnd = found;
                    if (OnAttachmentChanged != null)
                    {
                        OnAttachmentChanged(IsAttached);
                    }
                }
            }

            if (!IsAttached)
            {
                return;
            }

            // Do NOT collapse or vanish on Alt-Tab!
            // Only update coordinates when BlueStacks is visible and active
            RECT rect;
            if (GetWindowRect(_blueStacksHwnd, out rect))
            {
                if (rect.Width > 200 && rect.Height > 200)
                {
                    if (rect.Left != _lastTargetRect.Left ||
                        rect.Top != _lastTargetRect.Top ||
                        rect.Right != _lastTargetRect.Right ||
                        rect.Bottom != _lastTargetRect.Bottom)
                    {
                        _lastTargetRect = rect;
                        AlignToBlueStacks(rect);
                    }
                }
            }
        }

        public void ForceAlign()
        {
            if (IsAttached)
            {
                RECT rect;
                if (GetWindowRect(_blueStacksHwnd, out rect))
                {
                    _lastTargetRect = rect;
                    AlignToBlueStacks(rect);
                }
            }
        }

        private void AlignToBlueStacks(RECT rect)
        {
            double overlayWidth = _overlayWindow.Width;
            if (double.IsNaN(overlayWidth) || overlayWidth <= 0)
            {
                overlayWidth = IsCollapsed ? 36 : 340;
            }

            double targetX;
            if (CurrentDock == DockEdge.Right)
            {
                targetX = rect.Right;
                var screenRight = SystemParameters.VirtualScreenWidth;
                if (targetX + overlayWidth > screenRight)
                {
                    targetX = Math.Max(0, rect.Left - overlayWidth);
                }
            }
            else
            {
                targetX = rect.Left - overlayWidth;
                if (targetX < 0)
                {
                    targetX = rect.Right;
                }
            }

            _overlayWindow.Left = targetX;
            _overlayWindow.Top = Math.Max(0, rect.Top);

            if (!IsCollapsed)
            {
                _overlayWindow.Height = Math.Max(420, Math.Min(rect.Height, 540));
            }
        }

        public IntPtr FindBlueStacksWindow()
        {
            try
            {
                var processes = Process.GetProcessesByName("HD-Player");
                foreach (var proc in processes)
                {
                    if (proc.MainWindowHandle != IntPtr.Zero && IsWindow(proc.MainWindowHandle))
                    {
                        return proc.MainWindowHandle;
                    }
                }
            }
            catch { }
            return IntPtr.Zero;
        }
    }
}
