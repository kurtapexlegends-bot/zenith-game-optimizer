using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZenithOptimizer.Services;
using ZenithOptimizer.Services.Sniper;

namespace ZenithOptimizer.UI.Sniper
{
    /// <summary>
    /// Frictionless 1-click screen target picker for optical sensor, bid button, and eyedropper.
    /// Captures coordinates/colors with live cursor telemetry and clean right-click/Esc cancellation.
    /// </summary>
    public static class SniperScreenPicker
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        public static void PickCoordinate(Action<int, int> onPicked, Action onCancelled = null)
        {
            PickCoordinate("SELECT TARGET SPOT", onPicked, onCancelled);
        }

        public static void PickCoordinate(string prompt, Action<int, int> onPicked, Action onCancelled = null)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window picker = CreatePickerWindow(prompt);
                bool completed = false;

                Action<bool, int, int> finish = (accepted, x, y) =>
                {
                    if (completed) return;
                    completed = true;
                    try { picker.Close(); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                    if (accepted) { if (onPicked != null) onPicked(x, y); }
                    else { if (onCancelled != null) onCancelled(); }
                };

                picker.MouseLeftButtonDown += (s, e) => { e.Handled = true; POINT pt; GetCursorPos(out pt); finish(true, pt.X, pt.Y); };
                picker.MouseRightButtonDown += (s, e) => { e.Handled = true; finish(false, 0, 0); };
                picker.KeyDown += (s, e) => { if (e.Key == Key.Escape) { e.Handled = true; finish(false, 0, 0); } };

                picker.Show();
                try { picker.Activate(); picker.Focus(); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            });
        }

        public static void PickColor(Action<int, int, int> onColorPicked, Action onCancelled = null)
        {
            PickCoordinate("SAMPLE SCREEN COLOR", (x, y) =>
            {
                int r, g, b;
                if (RobloxBidSniperService.Instance.SampleColorAtPoint(x, y, out r, out g, out b))
                {
                    if (onColorPicked != null) onColorPicked(r, g, b);
                }
                else if (onCancelled != null) onCancelled();
            }, onCancelled);
        }

        private static Window CreatePickerWindow(string prompt)
        {
            Canvas canvas = new Canvas();
            Border badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 5, 8, 5),
                IsHitTestVisible = false
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Vertical };
            sp.Children.Add(new TextBlock { Text = prompt ?? "CLICK SCREEN TARGET", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)) });
            TextBlock tbCoords = new TextBlock { Text = "Move cursor to aim...", FontSize = 8.5, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)), Margin = new Thickness(0, 1, 0, 1) };
            sp.Children.Add(tbCoords);
            sp.Children.Add(new TextBlock { Text = "Left: Lock • Right / Esc: Cancel", FontSize = 7.5, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)) });
            badge.Child = sp;
            canvas.Children.Add(badge);

            var win = new Window
            {
                Title = "Zenith Target Picker",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                Topmost = true,
                ShowInTaskbar = false,
                Left = SystemParameters.VirtualScreenLeft,
                Top = SystemParameters.VirtualScreenTop,
                Width = SystemParameters.VirtualScreenWidth,
                Height = SystemParameters.VirtualScreenHeight,
                Cursor = Cursors.Cross,
                Content = canvas
            };

            win.MouseMove += (s, e) =>
            {
                try
                {
                    Point winPos = e.GetPosition(win);
                    POINT scrPt;
                    GetCursorPos(out scrPt);
                    Canvas.SetLeft(badge, Math.Max(10, Math.Min(win.ActualWidth - 220, winPos.X + 16)));
                    Canvas.SetTop(badge, Math.Max(10, Math.Min(win.ActualHeight - 70, winPos.Y + 16)));
                    string src;
                    uint p = OpticalSamplingEngine.SamplePixelWithFallback(scrPt.X, scrPt.Y, out src);
                    int r = (int)(p & 0xFF), g = (int)((p >> 8) & 0xFF), b = (int)((p >> 16) & 0xFF);
                    tbCoords.Text = string.Format("({0}, {1}) • RGB({2},{3},{4})", scrPt.X, scrPt.Y, r, g, b);
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            };

            win.Loaded += (s, e) =>
            {
                try
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(win);
                    if (helper.Handle != IntPtr.Zero) SetWindowDisplayAffinity(helper.Handle, WDA_EXCLUDEFROMCAPTURE);
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            };

            return win;
        }
    }
}
