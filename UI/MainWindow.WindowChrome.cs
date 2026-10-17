using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace ZenithOptimizer
{
    public partial class MainWindow : Window
    {
        private void BtnWinMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnWinMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void BtnWinClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ToggleMaximize()
        {
            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) ToggleMaximize();
        }

        #region Win32 Interop & Glassmorphism
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);
        private const uint MSGFLT_ADD = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_MICA_EFFECT = 1029;
        private const int DWMSBT_MAINWINDOW = 2;       // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic (Frosted Glass)

        private const uint WM_SETICON = 0x0080;
        private static readonly IntPtr ICON_SMALL = new IntPtr(0);
        private static readonly IntPtr ICON_BIG = new IntPtr(1);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        private const int SM_CXSMICON = 49;
        private const int SM_CYSMICON = 50;

        private static System.Drawing.Icon _persistentIconBig;
        private static System.Drawing.Icon _persistentIconSmall;

        private static System.Drawing.Icon LoadBestAppIcon(int width, int height)
        {
            try
            {
                string[] candidates = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\app.ico")
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate)) return new System.Drawing.Icon(candidate, width, height);
                }

                try
                {
                    Uri packUri = new Uri("pack://application:,,,/ZenithOptimizer;component/app.ico", UriKind.Absolute);
                    var sri = Application.GetResourceStream(packUri) ?? Application.GetResourceStream(new Uri("pack://application:,,,/app.ico", UriKind.RelativeOrAbsolute));
                    if (sri != null && sri.Stream != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            sri.Stream.CopyTo(ms);
                            ms.Position = 0;
                            return new System.Drawing.Icon(ms, width, height);
                        }
                    }
                }
                catch (Exception ex) { Debug.WriteLine("[MainWindow.Chrome] Pack icon load error: " + ex.Message); }

                try
                {
                    using (var exeIcon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location))
                    {
                        if (exeIcon != null) return new System.Drawing.Icon(exeIcon, width, height);
                    }
                }
                catch (Exception ex) { Debug.WriteLine("[MainWindow.Chrome] Associated icon load error: " + ex.Message); }
            }
            catch (Exception ex) { Debug.WriteLine("[MainWindow.Chrome] LoadBestAppIcon error: " + ex.Message); }
            return null;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    int smWidth = Math.Max(16, GetSystemMetrics(SM_CXSMICON));
                    int smHeight = Math.Max(16, GetSystemMetrics(SM_CYSMICON));

                    _persistentIconBig = LoadBestAppIcon(64, 64) ?? LoadBestAppIcon(32, 32);
                    _persistentIconSmall = LoadBestAppIcon(smWidth, smHeight) ?? LoadBestAppIcon(16, 16);

                    if (_persistentIconBig != null) SendMessage(hwnd, WM_SETICON, ICON_BIG, _persistentIconBig.Handle);
                    if (_persistentIconSmall != null) SendMessage(hwnd, WM_SETICON, ICON_SMALL, _persistentIconSmall.Handle);

                    ApplyGlassmorphism(hwnd);
                }
            }
            catch (Exception ex) { Debug.WriteLine("[MainWindow.Chrome] OnSourceInitialized error: " + ex.Message); }
        }

        private void ApplyGlassmorphism(IntPtr hwnd)
        {
            try
            {
                var source = HwndSource.FromHwnd(hwnd);
                if (source != null && source.CompositionTarget != null)
                {
                    source.CompositionTarget.BackgroundColor = Colors.Transparent;
                }

                if (source != null)
                {
                    try
                    {
                        uint showMsg = App.RegisterWindowMessage(App.ShowWindowMessageName);
                        ChangeWindowMessageFilter(showMsg, MSGFLT_ADD);
                        source.AddHook((IntPtr h, int m, IntPtr w, IntPtr l, ref bool handled) =>
                        {
                            if ((uint)m == showMsg)
                            {
                                Dispatcher.BeginInvoke((Action)(() => RestoreFromTray()));
                                handled = true;
                            }
                            return IntPtr.Zero;
                        });
                    }
                    catch (Exception ex) { Debug.WriteLine("[MainWindow.Chrome] Message filter hook error: " + ex.Message); }
                }

                MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref margins);

                int backdrop = DWMSBT_TRANSIENTWINDOW;
                int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                if (hr != 0)
                {
                    backdrop = DWMSBT_MAINWINDOW;
                    hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                    if (hr != 0)
                    {
                        int mica = 1;
                        DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref mica, sizeof(int));
                    }
                }

                UpdateDwmDarkMode(hwnd, _isDarkMode);
            }
            catch (Exception ex) { Debug.WriteLine("[MainWindow.Chrome] ApplyGlassmorphism error: " + ex.Message); }
        }

        public void UpdateDwmDarkMode(IntPtr hwnd, bool isDark)
        {
            try
            {
                if (hwnd != IntPtr.Zero)
                {
                    int darkMode = isDark ? 1 : 0;
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                }
            }
            catch (Exception ex) { Debug.WriteLine("[MainWindow.Chrome] UpdateDwmDarkMode error: " + ex.Message); }
        }

        private System.Drawing.Icon GenerateTrayIcon()
        {
            try
            {
                int smWidth = Math.Max(16, GetSystemMetrics(SM_CXSMICON));
                int smHeight = Math.Max(16, GetSystemMetrics(SM_CYSMICON));

                var icon = LoadBestAppIcon(smWidth, smHeight) ?? LoadBestAppIcon(16, 16);
                if (icon != null) return icon;

                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(32, 32))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.Clear(System.Drawing.Color.Transparent);

                        using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                        {
                            int r = 6, d = r * 2;
                            path.AddArc(1, 1, d, d, 180, 90);
                            path.AddArc(31 - d, 1, d, d, 270, 90);
                            path.AddArc(31 - d, 31 - d, d, d, 0, 90);
                            path.AddArc(1, 31 - d, d, d, 90, 90);
                            path.CloseFigure();

                            using (System.Drawing.Brush bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(14, 19, 31))) g.FillPath(bgBrush, path);
                            using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(240, 240, 240), 1.2f)) g.DrawPath(pen, path);
                        }

                        using (System.Drawing.Font font = new System.Drawing.Font("Segoe UI", 15f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                        using (System.Drawing.Brush textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                        {
                            System.Drawing.StringFormat sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
                            g.DrawString("Z", font, textBrush, new System.Drawing.RectangleF(0, 0, 32, 32), sf);
                        }
                    }
                    IntPtr hIcon = bmp.GetHicon();
                    try
                    {
                        using (var tempIcon = System.Drawing.Icon.FromHandle(hIcon)) return (System.Drawing.Icon)tempIcon.Clone();
                    }
                    finally { DestroyIcon(hIcon); }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MainWindow.Chrome] GenerateTrayIcon error: " + ex.Message);
                return System.Drawing.SystemIcons.Application;
            }
        }
        #endregion
    }
}
