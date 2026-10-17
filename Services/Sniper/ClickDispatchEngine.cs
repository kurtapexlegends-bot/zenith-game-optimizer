using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ZenithOptimizer.Services.Sniper
{
    /// <summary>
    /// Dual-dispatch input engine for Roblox bid clicks.
    /// Combines direct window messaging (PostMessage WM_LBUTTONDOWN) and hardware mouse_event
    /// with anti-camera whip protection.
    /// </summary>
    public static class ClickDispatchEngine
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        private const uint WM_MOUSEMOVE = 0x0200;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public static void DispatchMouseClick(
            int targetX,
            int targetY,
            string mode,
            string clickMode,
            string clickTargetMode,
            bool restoreCursorAfterClick,
            POINT? targetClickPos,
            IntPtr robloxHwnd)
        {
            POINT origPos;
            GetCursorPos(out origPos);

            int clickScreenX = targetX > 0 ? targetX : (targetClickPos.HasValue ? targetClickPos.Value.X : origPos.X);
            int clickScreenY = targetY > 0 ? targetY : (targetClickPos.HasValue ? targetClickPos.Value.Y : origPos.Y);

            // If test click, ensure Roblox gets foreground focus so the click registers in-game
            if (mode == "TEST" && robloxHwnd != IntPtr.Zero)
            {
                try { SetForegroundWindow(robloxHwnd); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }

            // Direct Window Message dispatch to Roblox HWND (with WM_MOUSEMOVE preamble so GUI hover registers)
            if (robloxHwnd != IntPtr.Zero && (clickMode == "Dual" || clickMode == "Message"))
            {
                try
                {
                    POINT clientPt = new POINT { X = clickScreenX, Y = clickScreenY };
                    if (ScreenToClient(robloxHwnd, ref clientPt))
                    {
                        IntPtr lParam = (IntPtr)(((clientPt.Y & 0xFFFF) << 16) | (clientPt.X & 0xFFFF));
                        PostMessage(robloxHwnd, WM_MOUSEMOVE, (IntPtr)0, lParam);
                        PostMessage(robloxHwnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
                        if (clickMode == "Message")
                        {
                            Thread.Sleep(25);
                            PostMessage(robloxHwnd, WM_LBUTTONUP, (IntPtr)0, lParam);
                        }
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }

            // Hardware OS mouse_event dispatch
            if (clickMode == "Dual" || clickMode == "Hardware")
            {
                try
                {
                    bool isHover = clickTargetMode == "Hover" && mode != "TEST";

                    if (!isHover)
                    {
                        SetCursorPos(clickScreenX, clickScreenY);
                    }

                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(25);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

                    // If dual mode, also complete the message up event
                    if (robloxHwnd != IntPtr.Zero && clickMode == "Dual")
                    {
                        POINT clientPt = new POINT { X = clickScreenX, Y = clickScreenY };
                        if (ScreenToClient(robloxHwnd, ref clientPt))
                        {
                            IntPtr lParam = (IntPtr)(((clientPt.Y & 0xFFFF) << 16) | (clientPt.X & 0xFFFF));
                            PostMessage(robloxHwnd, WM_LBUTTONUP, (IntPtr)0, lParam);
                        }
                    }

                    // Only restore cursor if explicitly enabled or if in TEST mode to prevent cursor jumping away from button
                    if (!isHover && (restoreCursorAfterClick || mode == "TEST"))
                    {
                        Thread.Sleep(5);
                        SetCursorPos(origPos.X, origPos.Y);
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }
        }

        public static void PlayBeep(int freq, int duration)
        {
            try
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try { Console.Beep(freq, duration); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                });
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
        }
    }
}
