using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ZenithOptimizer.Services.Sniper
{
    /// <summary>
    /// 3-tier GDI/DWM optical screen sampling engine.
    /// Safely handles Windows 11 DirectX 11 DirectFlip, DWM overlays, and desktop composition.
    /// </summary>
    public static class OpticalSamplingEngine
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private const uint PW_RENDERFULLCONTENT = 0x00000002;
        private const uint SRCCOPY = 0x00CC0020;

        public static IntPtr FindRobloxClientHwnd()
        {
            IntPtr fg = GetForegroundWindow();
            if (fg != IntPtr.Zero && IsRobloxClientWindow(fg))
            {
                return fg;
            }

            IntPtr found = FindWindow("WINDOWSCLIENT", null);
            if (found != IntPtr.Zero && IsRobloxClientWindow(found)) return found;

            IntPtr bestHwnd = IntPtr.Zero;
            long bestArea = 0;
            try
            {
                EnumWindows((h, lp) =>
                {
                    if (IsRobloxClientWindow(h))
                    {
                        RECT rc;
                        if (GetClientRect(h, out rc))
                        {
                            long area = (long)rc.Right * rc.Bottom;
                            if (area > bestArea)
                            {
                                bestArea = area;
                                bestHwnd = h;
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            return bestHwnd;
        }

        public static bool IsRobloxClientWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !IsWindowVisible(hWnd) || IsIconic(hWnd)) return false;

            RECT rc;
            if (!GetClientRect(hWnd, out rc)) return false;
            if (rc.Right < 320 || rc.Bottom < 240) return false;

            StringBuilder sbClass = new StringBuilder(256);
            GetClassName(hWnd, sbClass, 256);
            string clsName = sbClass.ToString();
            if (clsName.IndexOf("WINDOWSCLIENT", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            StringBuilder sbTitle = new StringBuilder(256);
            GetWindowText(hWnd, sbTitle, 256);
            string title = sbTitle.ToString();
            if (title.IndexOf("Roblox", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid != 0)
            {
                try
                {
                    string pName = Process.GetProcessById((int)pid).ProcessName;
                    if (pName.IndexOf("RobloxPlayer", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        pName.IndexOf("Crash", StringComparison.OrdinalIgnoreCase) < 0 &&
                        pName.IndexOf("Installer", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return true;
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }
            return false;
        }

        public static uint SamplePixelWithFallback(int screenX, int screenY, out string captureSource, string mode = "Auto", IntPtr rbxHwnd = default(IntPtr))
        {
            captureSource = "Unknown";
            IntPtr robloxHwnd = rbxHwnd != IntPtr.Zero ? rbxHwnd : FindRobloxClientHwnd();
            if (string.IsNullOrEmpty(mode)) mode = "Auto";

            // Tier 1: Roblox Client Window DC
            if (mode == "Auto" || mode.Equals("RobloxWindowDc", StringComparison.OrdinalIgnoreCase))
            {
                if (robloxHwnd != IntPtr.Zero)
                {
                    POINT pt = new POINT { X = screenX, Y = screenY };
                    if (ScreenToClient(robloxHwnd, ref pt))
                    {
                        RECT rc;
                        if (GetClientRect(robloxHwnd, out rc) && pt.X >= 0 && pt.Y >= 0 && pt.X < rc.Right && pt.Y < rc.Bottom)
                        {
                            IntPtr hdcRoblox = GetDC(robloxHwnd);
                            if (hdcRoblox != IntPtr.Zero)
                            {
                                try
                                {
                                    uint px = GetPixel(hdcRoblox, pt.X, pt.Y);
                                    if (px != 0xFFFFFFFF && px != 0x00000000)
                                    {
                                        captureSource = "Roblox Window DC";
                                        return px;
                                    }
                                    if (px != 0xFFFFFFFF && mode.Equals("RobloxWindowDc", StringComparison.OrdinalIgnoreCase))
                                    {
                                        captureSource = "Roblox Window DC (Black)";
                                        return px;
                                    }
                                }
                                finally
                                {
                                    ReleaseDC(robloxHwnd, hdcRoblox);
                                }
                            }
                        }
                    }
                }
            }

            // Tier 2: Desktop DC
            if (mode == "Auto" || mode.Equals("DesktopDc", StringComparison.OrdinalIgnoreCase))
            {
                IntPtr hdcScreen = GetDC(IntPtr.Zero);
                if (hdcScreen != IntPtr.Zero)
                {
                    try
                    {
                        uint px = GetPixel(hdcScreen, screenX, screenY);
                        if (px != 0xFFFFFFFF && px != 0x00000000)
                        {
                            captureSource = "Desktop DC";
                            return px;
                        }
                        if (px != 0xFFFFFFFF && mode.Equals("DesktopDc", StringComparison.OrdinalIgnoreCase))
                        {
                            captureSource = "Desktop DC";
                            return px;
                        }
                    }
                    finally
                    {
                        ReleaseDC(IntPtr.Zero, hdcScreen);
                    }
                }
            }

            // Tier 3: BitBlt fallback
            if (mode == "Auto" || mode.Equals("BitBltFallback", StringComparison.OrdinalIgnoreCase))
            {
                uint bltPx = SamplePixelBitBlt(screenX, screenY, robloxHwnd);
                if (bltPx != 0xFFFFFFFF && bltPx != 0x00000000)
                {
                    captureSource = "BitBlt Fallback";
                    return bltPx;
                }
            }

            captureSource = robloxHwnd != IntPtr.Zero ? "Roblox Window DC" : "Desktop DC";
            return 0x00000000;
        }

        public static uint SamplePixelBitBlt(int screenX, int screenY, IntPtr rbxHwnd)
        {
            IntPtr srcWnd = (rbxHwnd != IntPtr.Zero) ? rbxHwnd : IntPtr.Zero;
            IntPtr srcHdc = GetDC(srcWnd);
            if (srcHdc == IntPtr.Zero)
            {
                srcWnd = IntPtr.Zero;
                srcHdc = GetDC(IntPtr.Zero);
            }
            if (srcHdc == IntPtr.Zero) return 0xFFFFFFFF;

            IntPtr memDc = IntPtr.Zero;
            IntPtr hBmp = IntPtr.Zero;
            IntPtr oldBmp = IntPtr.Zero;
            try
            {
                memDc = CreateCompatibleDC(srcHdc);
                if (memDc == IntPtr.Zero) return 0xFFFFFFFF;
                hBmp = CreateCompatibleBitmap(srcHdc, 1, 1);
                if (hBmp == IntPtr.Zero) return 0xFFFFFFFF;
                oldBmp = SelectObject(memDc, hBmp);

                int srcX = screenX;
                int srcY = screenY;
                if (srcWnd != IntPtr.Zero)
                {
                    POINT pt = new POINT { X = screenX, Y = screenY };
                    ScreenToClient(srcWnd, ref pt);
                    srcX = pt.X;
                    srcY = pt.Y;
                }

                if (BitBlt(memDc, 0, 0, 1, 1, srcHdc, srcX, srcY, SRCCOPY))
                {
                    return GetPixel(memDc, 0, 0);
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            finally
            {
                if (oldBmp != IntPtr.Zero) SelectObject(memDc, oldBmp);
                if (hBmp != IntPtr.Zero) DeleteObject(hBmp);
                if (memDc != IntPtr.Zero) DeleteDC(memDc);
                if (srcHdc != IntPtr.Zero) ReleaseDC(srcWnd, srcHdc);
            }
            return 0xFFFFFFFF;
        }

        public static uint SamplePixelPrintWindow(int screenX, int screenY, IntPtr rbxHwnd)
        {
            if (rbxHwnd == IntPtr.Zero) return 0xFFFFFFFF;
            RECT rc;
            if (!GetClientRect(rbxHwnd, out rc) || rc.Right <= 0 || rc.Bottom <= 0) return 0xFFFFFFFF;

            POINT pt = new POINT { X = screenX, Y = screenY };
            ScreenToClient(rbxHwnd, ref pt);
            if (pt.X < 0 || pt.Y < 0 || pt.X >= rc.Right || pt.Y >= rc.Bottom) return 0xFFFFFFFF;

            IntPtr wndDc = GetDC(rbxHwnd);
            if (wndDc == IntPtr.Zero) return 0xFFFFFFFF;

            IntPtr memDc = IntPtr.Zero;
            IntPtr hBmp = IntPtr.Zero;
            IntPtr oldBmp = IntPtr.Zero;
            try
            {
                memDc = CreateCompatibleDC(wndDc);
                if (memDc == IntPtr.Zero) return 0xFFFFFFFF;
                hBmp = CreateCompatibleBitmap(wndDc, rc.Right, rc.Bottom);
                if (hBmp == IntPtr.Zero) return 0xFFFFFFFF;
                oldBmp = SelectObject(memDc, hBmp);

                if (PrintWindow(rbxHwnd, memDc, PW_RENDERFULLCONTENT))
                {
                    return GetPixel(memDc, pt.X, pt.Y);
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            finally
            {
                if (oldBmp != IntPtr.Zero) SelectObject(memDc, oldBmp);
                if (hBmp != IntPtr.Zero) DeleteObject(hBmp);
                if (memDc != IntPtr.Zero) DeleteDC(memDc);
                if (wndDc != IntPtr.Zero) ReleaseDC(rbxHwnd, wndDc);
            }
            return 0xFFFFFFFF;
        }
    }
}
