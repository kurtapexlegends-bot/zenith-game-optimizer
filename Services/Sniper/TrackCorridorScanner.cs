using System;

namespace ZenithOptimizer.Services.Sniper
{
    /// <summary>
    /// Optical corridor scanning engine for horizontal auction meter tracking.
    /// Computes centroid discovery and target locking for shifting sweet spots.
    /// </summary>
    public static class TrackCorridorScanner
    {
        public static bool ScanCorridorForTarget(
            Func<int, int, uint> pixelSampler,
            int centerX,
            int centerY,
            int corridorWidth,
            int corridorHeight,
            int step,
            Func<int, int, int, bool> isTargetMatch,
            out int targetX,
            out int targetY)
        {
            if (pixelSampler == null || corridorWidth <= 0 || corridorHeight <= 0)
            {
                targetX = centerX;
                targetY = centerY;
                return false;
            }

            if (step < 1) step = 4;

            int halfW = corridorWidth / 2;
            int halfH = corridorHeight / 2;
            int startX = centerX - halfW;
            int endX = centerX + halfW;

            // Multi-row horizontal strip sampling: center row, then top and bottom offsets
            int[] yOffsets = new int[] { 0, -5, 5, -10, 10 };
            long sumX = 0;
            long sumY = 0;
            int matchCount = 0;

            for (int yi = 0; yi < yOffsets.Length; yi++)
            {
                int currY = centerY + yOffsets[yi];
                if (currY < centerY - halfH || currY > centerY + halfH) continue;

                for (int currX = startX; currX <= endX; currX += step)
                {
                    try
                    {
                        uint pixel = pixelSampler(currX, currY);
                        if (pixel == 0xFFFFFFFF) continue;

                        int r = (int)(pixel & 0x000000FF);
                        int g = (int)((pixel & 0x0000FF00) >> 8);
                        int b = (int)((pixel & 0x00FF0000) >> 16);

                        if (isTargetMatch(r, g, b))
                        {
                            sumX += currX;
                            sumY += currY;
                            matchCount++;
                        }
                    }
                    catch { }
                }

                // If center row already yielded solid sweet-spot matches (>= 2 samples),
                // we have sufficient data to center lock without scanning further rows
                if (matchCount >= 2 && yi == 0)
                {
                    break;
                }
            }

            if (matchCount > 0)
            {
                targetX = (int)(sumX / matchCount);
                targetY = (int)(sumY / matchCount);
                return true;
            }

            targetX = centerX;
            targetY = centerY;
            return false;
        }
    }
}
