using System;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services.Sniper
{
    public static class SniperSettingsSync
    {
        public static void Load(RobloxBidSniperService service, OptimizationSettings s,
            Action<int, int> setSensorPos,
            Action<int, int> setClickPos,
            Action<bool> setUseCalibratedColor,
            Action<int, int, int> setCalibratedColor,
            Action<int> setColorTolerance,
            Action<string> setTriggerMode,
            Action<string> setColorPreset,
            Action<int> setSamplingRadius,
            Action<bool> setDynamicTracking,
            Action<int> setCorridorWidth,
            Action<int> setCorridorHeight,
            Action<int> setScanStep,
            Action<string> setCaptureMethod,
            Action<string> setClickMode,
            Action<int> setCountdownSeconds,
            Action<string> setClickTargetMode,
            Action<bool> setRestoreCursor)
        {
            if (s == null) return;
            try
            {
                if (s.SniperSensorX > 0 && s.SniperSensorY > 0) setSensorPos(s.SniperSensorX, s.SniperSensorY);
                if (s.SniperHasClickTarget && s.SniperClickX > 0 && s.SniperClickY > 0) setClickPos(s.SniperClickX, s.SniperClickY);
                setUseCalibratedColor(s.SniperUseCalibratedColor);
                if (s.SniperTargetColorG > 0) setCalibratedColor(s.SniperTargetColorR, s.SniperTargetColorG, s.SniperTargetColorB);
                if (s.SniperColorTolerance >= 10) setColorTolerance(s.SniperColorTolerance);
                if (!string.IsNullOrEmpty(s.SniperTriggerMode)) setTriggerMode(s.SniperTriggerMode);
                if (!string.IsNullOrEmpty(s.SniperColorPreset)) setColorPreset(s.SniperColorPreset);
                if (s.SniperSamplingRadius >= 1) setSamplingRadius(s.SniperSamplingRadius);
                setDynamicTracking(s.SniperDynamicTrackingEnabled);
                if (s.SniperTrackCorridorWidth > 0) setCorridorWidth(s.SniperTrackCorridorWidth);
                if (s.SniperTrackCorridorHeight > 0) setCorridorHeight(s.SniperTrackCorridorHeight);
                if (s.SniperTrackScanStep > 0) setScanStep(s.SniperTrackScanStep);
                if (!string.IsNullOrEmpty(s.SniperCaptureMethod)) setCaptureMethod(s.SniperCaptureMethod);
                if (!string.IsNullOrEmpty(s.SniperClickMode)) setClickMode(s.SniperClickMode);
                if (s.SniperAuctionCountdownSeconds > 0) setCountdownSeconds(s.SniperAuctionCountdownSeconds);
                if (!string.IsNullOrEmpty(s.SniperClickTargetMode)) setClickTargetMode(s.SniperClickTargetMode);
                setRestoreCursor(s.SniperRestoreCursor);
            }
            catch { }
        }

        public static void Save(OptimizationSettings s,
            RobloxBidSniperService.POINT sensorPos,
            RobloxBidSniperService.POINT? clickPos,
            bool useCalibratedColor,
            int calR, int calG, int calB,
            int colorTolerance,
            string triggerMode,
            int samplingRadius,
            string colorPreset,
            bool dynamicTracking,
            int corridorWidth,
            int corridorHeight,
            int scanStep,
            string captureMethod,
            string clickMode,
            int countdownSeconds,
            string clickTargetMode,
            bool restoreCursor)
        {
            if (s == null) return;
            try
            {
                s.SniperSensorX = sensorPos.X;
                s.SniperSensorY = sensorPos.Y;
                s.SniperHasClickTarget = clickPos.HasValue;
                if (clickPos.HasValue) { s.SniperClickX = clickPos.Value.X; s.SniperClickY = clickPos.Value.Y; }
                s.SniperUseCalibratedColor = useCalibratedColor;
                s.SniperTargetColorR = calR; s.SniperTargetColorG = calG; s.SniperTargetColorB = calB;
                s.SniperColorTolerance = colorTolerance;
                s.SniperTriggerMode = triggerMode;
                s.SniperSamplingRadius = samplingRadius;
                s.SniperColorPreset = colorPreset;
                s.SniperDynamicTrackingEnabled = dynamicTracking;
                s.SniperTrackCorridorWidth = corridorWidth;
                s.SniperTrackCorridorHeight = corridorHeight;
                s.SniperTrackScanStep = scanStep;
                s.SniperCaptureMethod = captureMethod;
                s.SniperClickMode = clickMode;
                s.SniperAuctionCountdownSeconds = countdownSeconds;
                s.SniperClickTargetMode = clickTargetMode;
                s.SniperRestoreCursor = restoreCursor;
                s.Save();
            }
            catch { }
        }
    }
}
