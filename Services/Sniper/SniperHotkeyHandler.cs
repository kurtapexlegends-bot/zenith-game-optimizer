using System;
using System.Runtime.InteropServices;

namespace ZenithOptimizer.Services.Sniper
{
    public class SniperHotkeyHandler
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_F10 = 0x79;
        public const int VK_V = 0x56;

        private bool _f6WasDown = false;
        private bool _f7WasDown = false;
        private bool _f8WasDown = false;
        private bool _f9WasDown = false;
        private bool _f10WasDown = false;

        public static bool IsKeyDown(int vKey)
        {
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }

        public void ProcessHotkeys(RobloxBidSniperService sniper)
        {
            if (sniper == null) return;

            bool ctrlDown = IsKeyDown(VK_CONTROL);
            bool altDown = IsKeyDown(VK_MENU);

            bool f6Down = IsKeyDown(VK_F6);
            if (f6Down && !_f6WasDown)
            {
                try
                {
                    if (ctrlDown) { UI.RobloxSniperOverlay.Instance.ToggleMarkersVisibility(); ClickDispatchEngine.PlayBeep(1350, 70); }
                    else { UI.RobloxSniperOverlay.Instance.ToggleMarkersLock(); ClickDispatchEngine.PlayBeep(1200, 70); }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }
            _f6WasDown = f6Down;

            bool f7Down = IsKeyDown(VK_F7);
            if (f7Down && !_f7WasDown)
            {
                if (ctrlDown)
                {
                    sniper.DynamicTrackingEnabled = !sniper.DynamicTrackingEnabled;
                    ClickDispatchEngine.PlayBeep(sniper.DynamicTrackingEnabled ? 1500 : 700, 70);
                }
                else
                {
                    sniper.IsArmed = !sniper.IsArmed;
                    ClickDispatchEngine.PlayBeep(sniper.IsArmed ? 1100 : 550, 70);
                }
            }
            _f7WasDown = f7Down;

            bool f8Down = IsKeyDown(VK_F8);
            if (f8Down && !_f8WasDown)
            {
                if (ctrlDown)
                {
                    int cx, cy;
                    if (sniper.SetClickTargetToCurrentCursor(out cx, out cy)) ClickDispatchEngine.PlayBeep(1600, 90);
                }
                else if (altDown)
                {
                    int cr, cg, cb;
                    if (sniper.SampleTargetColorAtCursor(out cr, out cg, out cb)) ClickDispatchEngine.PlayBeep(1800, 90);
                }
                else
                {
                    if (sniper.SetSensorPositionToCursor()) ClickDispatchEngine.PlayBeep(1400, 80);
                }
            }
            _f8WasDown = f8Down;

            bool f9Down = IsKeyDown(VK_F9);
            if (f9Down && !_f9WasDown)
            {
                try
                {
                    if (ctrlDown) { UI.RobloxSniperOverlay.Instance.ToggleCompactMode(); ClickDispatchEngine.PlayBeep(1100, 60); }
                    else { UI.RobloxSniperOverlay.Instance.Toggle(); ClickDispatchEngine.PlayBeep(900, 60); }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }
            _f9WasDown = f9Down;

            bool f10Down = IsKeyDown(VK_F10);
            if (f10Down && !_f10WasDown)
            {
                try
                {
                    if (ctrlDown)
                    {
                        UI.FpsMeterOverlay.Instance.ToggleLock();
                        ClickDispatchEngine.PlayBeep(UI.FpsMeterOverlay.Instance.IsClickThrough ? 800 : 1600, 60);
                    }
                    else
                    {
                        UI.FpsMeterOverlay.Instance.Toggle();
                        ClickDispatchEngine.PlayBeep(1300, 60);
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }
            _f10WasDown = f10Down;
        }
    }
}
