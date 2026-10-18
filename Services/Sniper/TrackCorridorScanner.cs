using System;

namespace ZenithOptimizer.Services.Sniper
{
    /// <summary>
    /// Optical corridor scanning engine for horizontal auction meter tracking.
    /// Dual-element tracking: dynamically identifies shifting sweet-spot zones (Green, Gold, Cyan, etc.)
    /// and tracks oscillating vertical needles via Luminance Edge Contrast (dL/dx) and RAM buffer scanning.
    /// </summary>
    public static class TrackCorridorScanner
    {
        public static bool IsWhiteNeedlePixel(int r, int g, int b)
        {
            return r >= 210 && g >= 210 && b >= 210 &&
                   Math.Abs(r - g) <= 35 && Math.Abs(r - b) <= 35 && Math.Abs(g - b) <= 35;
        }

        public static int ComputeLuminance(uint pixel)
        {
            int r = (int)(pixel & 0xFF), g = (int)((pixel >> 8) & 0xFF), b = (int)((pixel >> 16) & 0xFF);
            return (int)(0.299 * r + 0.587 * g + 0.114 * b + 0.5);
        }

        public static bool DetectNeedleLuminanceGradient(uint[] buffer, int width, int height, out int needleX)
        {
            needleX = width / 2;
            if (buffer == null || width < 7 || height < 1 || buffer.Length < width * height) return false;

            int centerY = height / 2;
            int[] yOffsets = new int[] { 0, -3, 3 };

            int[] avgL = new int[width];
            for (int x = 0; x < width; x++)
            {
                int sum = 0, count = 0;
                for (int i = 0; i < yOffsets.Length; i++)
                {
                    int y = centerY + yOffsets[i];
                    if (y >= 0 && y < height)
                    {
                        uint p = buffer[y * width + x];
                        if (p != 0xFFFFFFFF) { sum += ComputeLuminance(p); count++; }
                    }
                }
                avgL[x] = count > 0 ? (sum / count) : 0;
            }

            int maxContrast = 0, bestX = -1;
            for (int x = 3; x < width - 3; x++)
            {
                int c2 = 2 * avgL[x] - avgL[x - 2] - avgL[x + 2];
                int c3 = 2 * avgL[x] - avgL[x - 3] - avgL[x + 3];
                int contrast = Math.Max(c2, c3);
                if (contrast > maxContrast) { maxContrast = contrast; bestX = x; }
            }

            if (maxContrast >= 22 && bestX >= 0)
            {
                long weightSum = 0, weightedPos = 0;
                for (int i = -1; i <= 1; i++)
                {
                    int cx = bestX + i;
                    if (cx >= 3 && cx < width - 3)
                    {
                        int c = Math.Max(2 * avgL[cx] - avgL[cx - 2] - avgL[cx + 2], 2 * avgL[cx] - avgL[cx - 3] - avgL[cx + 3]);
                        if (c > 0) { weightSum += c; weightedPos += (long)cx * c; }
                    }
                }
                needleX = weightSum > 0 ? (int)(weightedPos / weightSum) : bestX;
                return true;
            }

            long whiteSumX = 0;
            int whiteCount = 0;
            for (int x = 0; x < width; x++)
            {
                for (int i = 0; i < yOffsets.Length; i++)
                {
                    int y = centerY + yOffsets[i];
                    if (y >= 0 && y < height)
                    {
                        uint p = buffer[y * width + x];
                        if (IsWhiteNeedlePixel((int)(p & 0xFF), (int)((p >> 8) & 0xFF), (int)((p >> 16) & 0xFF)))
                        {
                            whiteSumX += x;
                            whiteCount++;
                        }
                    }
                }
            }
            if (whiteCount > 0) { needleX = (int)(whiteSumX / whiteCount); return true; }
            return false;
        }

        public static bool ScanCorridorBuffer(
            uint[] buffer, int width, int height, Func<int, int, int, bool> isMatch,
            out int zoneLeft, out int zoneRight, out int needleX)
        {
            zoneLeft = width / 2;
            zoneRight = width / 2;
            needleX = width / 2;

            if (buffer == null || width <= 0 || height <= 0 || buffer.Length < width * height) return false;

            int centerY = height / 2, halfH = height / 2;
            int minX = int.MaxValue, maxX = int.MinValue, matchCount = 0;
            int[] yOffsets = new int[] { 0, -4, 4, -8, 8 };

            for (int yi = 0; yi < yOffsets.Length; yi++)
            {
                int currY = centerY + yOffsets[yi];
                if (currY < centerY - halfH || currY > centerY + halfH || currY < 0 || currY >= height) continue;

                int rowOffset = currY * width;
                for (int currX = 0; currX < width; currX += 2)
                {
                    uint pixel = buffer[rowOffset + currX];
                    if (pixel == 0xFFFFFFFF) continue;

                    int r = (int)(pixel & 0xFF), g = (int)((pixel >> 8) & 0xFF), b = (int)((pixel >> 16) & 0xFF);
                    if (isMatch != null && isMatch(r, g, b))
                    {
                        if (currX < minX) minX = currX;
                        if (currX > maxX) maxX = currX;
                        matchCount++;
                    }
                }
                if (matchCount >= 4 && yi == 0) break;
            }

            bool hasZone = (matchCount > 0 && minX <= maxX);
            if (hasZone) { zoneLeft = minX; zoneRight = maxX; }

            bool hasNeedle = DetectNeedleLuminanceGradient(buffer, width, height, out needleX);
            if (!hasNeedle && hasZone) needleX = (zoneLeft + zoneRight) / 2;

            return hasZone || hasNeedle;
        }

        public static bool ScanStorageHuntersMeter(
            uint[] buffer, int centerX, int centerY, int corridorWidth, int corridorHeight,
            Func<int, int, int, bool> isTargetMatch,
            out int zoneLeft, out int zoneRight, out int needleX, out bool isInsideSweetSpot)
        {
            int relZL, relZR, relNeedle;
            bool detected = ScanCorridorBuffer(buffer, corridorWidth, corridorHeight, isTargetMatch, out relZL, out relZR, out relNeedle);

            int startX = centerX - corridorWidth / 2;
            zoneLeft = startX + relZL;
            zoneRight = startX + relZR;
            needleX = startX + relNeedle;

            if (relZR > relZL)
            {
                isInsideSweetSpot = (needleX >= zoneLeft && needleX <= zoneRight);
                return true;
            }
            isInsideSweetSpot = false;
            return detected;
        }

        public static bool ScanCorridorForTarget(
            Func<int, int, uint> pixelSampler, int centerX, int centerY, int corridorWidth, int corridorHeight,
            int step, Func<int, int, int, bool> isTargetMatch, out int targetX, out int targetY)
        {
            int zLeft, zRight;
            bool found = ScanCorridorForZone(pixelSampler, centerX, centerY, corridorWidth, corridorHeight, step, isTargetMatch, out zLeft, out zRight, out targetY);
            targetX = found ? (zLeft + zRight) / 2 : centerX;
            return found;
        }

        public static bool ScanCorridorForZone(
            Func<int, int, uint> pixelSampler, int centerX, int centerY, int corridorWidth, int corridorHeight,
            int step, Func<int, int, int, bool> isTargetMatch, out int zoneLeft, out int zoneRight, out int targetY)
        {
            zoneLeft = centerX;
            zoneRight = centerX;
            targetY = centerY;

            if (pixelSampler == null || corridorWidth <= 0 || corridorHeight <= 0) return false;
            if (step < 1) step = 4;

            int halfW = corridorWidth / 2, halfH = corridorHeight / 2;
            int startX = centerX - halfW, endX = centerX + halfW;
            int minX = int.MaxValue, maxX = int.MinValue, matchCount = 0;
            long sumY = 0;
            int[] yOffsets = new int[] { 0, -4, 4, -8, 8 };

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

                        int r = (int)(pixel & 0xFF), g = (int)((pixel >> 8) & 0xFF), b = (int)((pixel >> 16) & 0xFF);
                        if (isTargetMatch(r, g, b))
                        {
                            if (currX < minX) minX = currX;
                            if (currX > maxX) maxX = currX;
                            sumY += currY;
                            matchCount++;
                        }
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                }
                if (matchCount >= 4 && yi == 0) break;
            }

            if (matchCount > 0 && minX <= maxX)
            {
                zoneLeft = minX;
                zoneRight = maxX;
                targetY = (int)(sumY / matchCount);
                return true;
            }
            return false;
        }

        public static bool ScanCorridorForNeedle(
            Func<int, int, uint> pixelSampler, int centerX, int centerY, int corridorWidth, int corridorHeight,
            int step, out int needleX)
        {
            needleX = centerX;
            if (pixelSampler == null || corridorWidth <= 0 || corridorHeight <= 0) return false;
            if (step < 1) step = 2;

            int halfW = corridorWidth / 2;
            int startX = centerX - halfW, endX = centerX + halfW;
            long sumX = 0;
            int matchCount = 0;
            int[] yOffsets = new int[] { 0, -3, 3 };

            for (int yi = 0; yi < yOffsets.Length; yi++)
            {
                int currY = centerY + yOffsets[yi];
                for (int currX = startX; currX <= endX; currX += step)
                {
                    try
                    {
                        uint pixel = pixelSampler(currX, currY);
                        if (pixel == 0xFFFFFFFF) continue;

                        int r = (int)(pixel & 0xFF), g = (int)((pixel >> 8) & 0xFF), b = (int)((pixel >> 16) & 0xFF);
                        if (IsWhiteNeedlePixel(r, g, b))
                        {
                            sumX += currX;
                            matchCount++;
                        }
                    }
                    catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                }
                if (matchCount >= 2 && yi == 0) break;
            }

            if (matchCount > 0)
            {
                needleX = (int)(sumX / matchCount);
                return true;
            }
            return false;
        }

        public static bool ScanStorageHuntersMeter(
            Func<int, int, uint> pixelSampler, int centerX, int centerY, int corridorWidth, int corridorHeight,
            int step, Func<int, int, int, bool> isTargetMatch,
            out int zoneLeft, out int zoneRight, out int needleX, out bool isInsideSweetSpot)
        {
            bool hasZone = ScanCorridorForZone(pixelSampler, centerX, centerY, corridorWidth, corridorHeight, step, isTargetMatch, out zoneLeft, out zoneRight, out centerY);
            bool hasNeedle = ScanCorridorForNeedle(pixelSampler, centerX, centerY, corridorWidth, corridorHeight, Math.Max(1, step / 2), out needleX);

            if (hasZone && hasNeedle)
            {
                isInsideSweetSpot = (needleX >= zoneLeft && needleX <= zoneRight);
                return true;
            }
            isInsideSweetSpot = false;
            return hasZone || hasNeedle;
        }
    }
}
