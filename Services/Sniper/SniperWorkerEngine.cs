using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ZenithOptimizer.Services.Sniper
{
    public static class SniperWorkerEngine
    {
        public static bool EvaluateTriggerFrame(
            int r, int g, int b,
            string triggerMode,
            bool isArmed,
            ref bool needsPriming,
            ref bool wasInSweetSpot,
            ref bool autoBaselineWasMatch,
            ref DateTime armCooldownUntil,
            DateTime lastSnipeTime,
            Func<int, int, int, bool> isTargetMatch,
            out bool isMatch,
            out string firingMode,
            bool ignoreCooldown = false)
        {
            firingMode = triggerMode ?? "Auto";
            bool focalMatch = isTargetMatch(r, g, b);
            isMatch = focalMatch;

            if (needsPriming)
            {
                wasInSweetSpot = focalMatch;
                autoBaselineWasMatch = focalMatch;
                needsPriming = false;
                armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120);
                return false;
            }

            if (!isArmed)
            {
                wasInSweetSpot = focalMatch;
                return false;
            }

            if (!ignoreCooldown && (DateTime.UtcNow < armCooldownUntil || (DateTime.UtcNow - lastSnipeTime).TotalMilliseconds < 180))
            {
                wasInSweetSpot = focalMatch;
                return false;
            }

            bool shouldFire = false;
            string mode = triggerMode ?? "Auto";
            if (mode.Equals("Turbo", StringComparison.OrdinalIgnoreCase))
            {
                if (focalMatch && ((DateTime.UtcNow - lastSnipeTime).TotalMilliseconds >= 75 || ignoreCooldown)) { shouldFire = true; firingMode = "TURBO"; }
            }
            else if (mode.Equals("TimerSnipe", StringComparison.OrdinalIgnoreCase))
            {
                if (focalMatch) { shouldFire = true; firingMode = "TIMER"; }
            }
            else if (mode.Equals("Continuous", StringComparison.OrdinalIgnoreCase))
            {
                if (focalMatch) { shouldFire = true; firingMode = "CONTINUOUS"; }
            }
            else if (mode.Equals("Disruption", StringComparison.OrdinalIgnoreCase))
            {
                if (wasInSweetSpot && !focalMatch) { shouldFire = true; firingMode = "DISRUPTION"; }
            }
            else if (!wasInSweetSpot && focalMatch)
            {
                shouldFire = true;
                firingMode = mode.ToUpperInvariant();
            }

            if (!mode.Equals("Disruption", StringComparison.OrdinalIgnoreCase) && !focalMatch) shouldFire = false;
            wasInSweetSpot = focalMatch;
            return shouldFire;
        }

        public static void ProcessSniperScan(
            int sx, int sy,
            bool dynamicTrackingEnabled,
            int corridorWidth, int corridorHeight, int scanStep,
            Func<int, int, uint> pixelSampler,
            Func<int, int, int, bool> isTargetMatch,
            ref bool hasTrackLock,
            ref DateTime lastTrackLockTime,
            ref RobloxBidSniperService.POINT? currentTrackedPos,
            out int activeX, out int activeY,
            out bool isInsideSweetSpot,
            out int r, out int g, out int b,
            out string captureSource,
            uint[] dibBuffer = null)
        {
            int zoneLeft = sx, zoneRight = sx, needleX = sx;
            isInsideSweetSpot = false;

            if (dynamicTrackingEnabled)
            {
                if (dibBuffer != null && dibBuffer.Length >= corridorWidth * corridorHeight)
                {
                    TrackCorridorScanner.ScanStorageHuntersMeter(
                        dibBuffer, sx, sy, corridorWidth, corridorHeight,
                        isTargetMatch,
                        out zoneLeft, out zoneRight, out needleX, out isInsideSweetSpot);
                }
                else
                {
                    TrackCorridorScanner.ScanStorageHuntersMeter(
                        pixelSampler, sx, sy, corridorWidth, corridorHeight, scanStep,
                        isTargetMatch,
                        out zoneLeft, out zoneRight, out needleX, out isInsideSweetSpot);
                }

                hasTrackLock = (zoneRight > zoneLeft);
                if (hasTrackLock)
                {
                    currentTrackedPos = new RobloxBidSniperService.POINT { X = (zoneLeft + zoneRight) / 2, Y = sy };
                    lastTrackLockTime = DateTime.UtcNow;
                }
            }
            else
            {
                hasTrackLock = false;
            }

            activeX = hasTrackLock ? needleX : sx;
            activeY = sy;
            uint pix = pixelSampler != null ? pixelSampler(activeX, activeY) : 0xFFFFFFFF;
            captureSource = dibBuffer != null ? "Roblox DIB" : "Auto DC";
            r = (int)(pix & 0xFF);
            g = (int)((pix >> 8) & 0xFF);
            b = (int)((pix >> 16) & 0xFF);
        }

        public static CancellationTokenSource TriggerAuctionTimerSnipe(
            CancellationTokenSource existingCts,
            double countdownSeconds,
            int burstCount,
            Action<string> log,
            Action executeBurst)
        {
            if (existingCts != null) { try { existingCts.Cancel(); existingCts.Dispose(); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); } }
            var newCts = new CancellationTokenSource();
            var token = newCts.Token;
            Task.Run(async () =>
            {
                int delayMs = Math.Max(0, (int)((countdownSeconds - 0.25) * 1000));
                if (log != null) log(string.Format("Auction Timer Snipe armed for {0:F1}s countdown (firing at -250ms latency margin)", countdownSeconds));
                try { await Task.Delay(delayMs, token); } catch { return; }
                if (token.IsCancellationRequested) return;
                for (int i = 0; i < burstCount; i++)
                {
                    executeBurst();
                    await Task.Delay(55, token);
                }
            }, token);
            return newCts;
        }
    }
}
