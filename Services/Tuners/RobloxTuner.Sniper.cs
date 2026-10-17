using System;

namespace ZenithOptimizer.Services.Tuners
{
    public partial class RobloxTuner
    {
        // --- Sniper Bridge ---
        public bool ToggleRobloxAuctionSniper(out bool isActive, out string message)
        {
            bool ok = RobloxBidSniperService.Instance.Toggle(out isActive, out message);
            try { if (isActive) UI.RobloxSniperOverlay.Instance.Show(); else UI.RobloxSniperOverlay.Instance.Hide(); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            return ok;
        }

        public bool ToggleRobloxSniperOverlay(out bool isVisible, out string message)
        {
            var overlay = UI.RobloxSniperOverlay.Instance;
            overlay.Toggle();
            isVisible = overlay.IsVisible;
            message = isVisible ? "Gaming HUD Overlay & Sensor Reticle visible on screen. Drag reticle over green zone!" : "Gaming HUD Overlay hidden.";
            return true;
        }

        public bool TriggerRobloxSniperTestClick(out string message) { RobloxBidSniperService.Instance.TriggerTestClick(); message = "Dispatched test click (25ms down/up). Input verified!"; return true; }
        public bool TriggerRobloxSniperTurboBurst(out string message) { RobloxBidSniperService.Instance.TriggerTurboBurst(3); message = "Dispatched 3x Turbo Bid burst. High-speed input verified!"; return true; }
        public bool SetRobloxSniperSensorToCursor(out string message)
        {
            int x, y;
            bool ok = RobloxBidSniperService.Instance.SetSensorToCurrentCursor(out x, out y);
            message = ok ? string.Format("Sensor reticle locked to screen coordinates ({0}, {1})", x, y) : "Unable to query current mouse coordinates.";
            return ok;
        }
        public bool IsRobloxSniperOverlayVisible() { return UI.RobloxSniperOverlay.Instance.IsVisible; }
        public bool IsRobloxAuctionSniperActive() { return RobloxBidSniperService.Instance.IsRunning; }
        public string GetRobloxAuctionSniperStatus()
        {
            var s = RobloxBidSniperService.Instance;
            if (!s.IsRunning) return "Inactive (Standby)";
            return string.Format("Active • {0} • Spot: ({1}, {2}) • Snipes: {3}", s.IsArmed ? "Armed [Auto-Click Ready]" : "Disarmed [V to Snipe]", s.SensorPosition.X, s.SensorPosition.Y, s.SuccessfulSnipes);
        }
        public bool SetRobloxSniperClickTargetToCursor(out string message)
        {
            int x, y;
            bool ok = RobloxBidSniperService.Instance.SetClickTargetToCurrentCursor(out x, out y);
            message = ok ? string.Format("Bid Button click target locked to screen coordinates ({0}, {1})", x, y) : "Unable to query current mouse coordinates.";
            return ok;
        }
        public bool ResetRobloxSniperClickTarget(out string message) { RobloxBidSniperService.Instance.ResetCalibration(); message = "Bid Button click target reset to active mouse cursor position."; return true; }
        public bool CalibrateRobloxSniperTargetColorFromCursor(out string message)
        {
            int r, g, b;
            bool ok = RobloxBidSniperService.Instance.SampleTargetColorAtCursor(out r, out g, out b);
            message = ok ? string.Format("Eyedropper sampled color RGB({0}, {1}, {2}) [#{0:X2}{1:X2}{2:X2}] locked as sweet-spot target!", r, g, b) : "Unable to sample color under mouse cursor.";
            return ok;
        }
        public bool ResetRobloxSniperTargetColor(out string message) { RobloxBidSniperService.Instance.ResetTargetColor(); message = "Target color reset to Dynamic HSV Green Sweet-Spot mode."; return true; }
        public bool SetRobloxSniperTriggerMode(string mode, out string message) { RobloxBidSniperService.Instance.TriggerMode = mode; message = string.Format("Roblox Auction Sniper trigger mode set to: {0}", mode); return true; }
    }
}
