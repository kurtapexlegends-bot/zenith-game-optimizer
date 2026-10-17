using System;
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
