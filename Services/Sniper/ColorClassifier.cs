using System;

namespace ZenithOptimizer.Services.Sniper
{
    /// <summary>
    /// Pure optical color classifier for auction sweet-spot identification.
    /// Provides RGB euclidean distance calculations and HSV hue angle arc matching.
    /// </summary>
    public static class ColorClassifier
    {
        public static bool IsGreenSweetSpot(int r, int g, int b)
        {
            if (g >= 100 && g > (int)(r * 1.15) && g > (int)(b * 1.15))
            {
                return true;
            }

            double h, s, v;
            RgbToHsv(r, g, b, out h, out s, out v);
            return (h >= 65.0 && h <= 165.0) && (s >= 0.22) && (v >= 0.28) && (g >= 70) && (g > (int)(r * 1.05)) && (g > (int)(b * 1.05));
        }

        public static bool IsGoldSweetSpot(int r, int g, int b)
        {
            if (r >= 165 && g >= 135 && b <= 125 && (r + g) > (b * 2.8))
            {
                return true;
            }
            double h, s, v;
            RgbToHsv(r, g, b, out h, out s, out v);
            return (h >= 35.0 && h <= 64.0) && (s >= 0.30) && (v >= 0.40) && (r >= 135) && (g >= 110) && (b < Math.Max(r, g) * 0.75);
        }

        public static bool IsCyanSweetSpot(int r, int g, int b)
        {
            if (b >= 155 && g >= 135 && r <= 135 && (g + b) > (r * 2.1))
            {
                return true;
            }
            double h, s, v;
            RgbToHsv(r, g, b, out h, out s, out v);
            return (h >= 170.0 && h <= 215.0) && (s >= 0.30) && (v >= 0.40) && (b >= 115) && (g >= 105) && (r < Math.Max(b, g) * 0.85);
        }

        public static bool IsPurpleSweetSpot(int r, int g, int b)
        {
            if (r >= 125 && b >= 155 && g <= 115 && (r + b) > (g * 2.1))
            {
                return true;
            }
            double h, s, v;
            RgbToHsv(r, g, b, out h, out s, out v);
            return (h >= 260.0 && h <= 315.0) && (s >= 0.30) && (v >= 0.40) && (b >= 105) && (r >= 95) && (g < Math.Max(r, b) * 0.75);
        }

        public static bool IsOrangeRedSweetSpot(int r, int g, int b)
        {
            if (r >= 170 && r > (int)(g * 1.2) && r > (int)(b * 1.35) && (g + b) < (r * 1.4))
            {
                return true;
            }
            double h, s, v;
            RgbToHsv(r, g, b, out h, out s, out v);
            return ((h >= 10.0 && h <= 35.0) || (h >= 345.0 || h <= 10.0)) && (s >= 0.35) && (v >= 0.35) && (r >= 135) && (r > (int)(g * 1.1)) && (r > (int)(b * 1.25));
        }

        public static bool IsCalibratedColorMatch(int r, int g, int b, int calR, int calG, int calB, int tolerance)
        {
            int dr = r - calR;
            int dg = g - calG;
            int db = b - calB;
            double distance = Math.Sqrt((dr * dr) + (dg * dg) + (db * db));
            return distance <= tolerance;
        }

        public static bool IsTargetMatch(int r, int g, int b, string preset, bool useCalibrated, int calR, int calG, int calB, int tolerance)
        {
            if (useCalibrated)
            {
                return IsCalibratedColorMatch(r, g, b, calR, calG, calB, tolerance);
            }

            string p = (preset ?? "Green").ToLowerInvariant();
            if (p == "gold" || p == "yellow") return IsGoldSweetSpot(r, g, b);
            if (p == "cyan" || p == "blue") return IsCyanSweetSpot(r, g, b);
            if (p == "purple" || p == "violet") return IsPurpleSweetSpot(r, g, b);
            if (p == "orange" || p == "red" || p == "frenzy") return IsOrangeRedSweetSpot(r, g, b);
            return IsGreenSweetSpot(r, g, b);
        }

        public static void RgbToHsv(int r, int g, int b, out double h, out double s, out double v)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            v = max;
            s = max == 0 ? 0 : delta / max;

            if (delta == 0)
            {
                h = 0;
            }
            else if (max == rd)
            {
                h = 60.0 * (((gd - bd) / delta) % 6);
            }
            else if (max == gd)
            {
                h = 60.0 * (((bd - rd) / delta) + 2);
            }
            else
            {
                h = 60.0 * (((rd - gd) / delta) + 4);
            }

            if (h < 0) h += 360.0;
        }
    }
}
