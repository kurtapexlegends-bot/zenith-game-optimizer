using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services
{
    /// <summary>
    /// Optical Sweet-Spot & Auction Bidding Engine for Roblox Bid Battles.
    /// Operates 100% externally via desktop pixel sampling and OS mouse input.
    /// Zero DLL injection, zero memory hooks, 100% anti-cheat (Hyperion/Byfron) compliant.
    /// </summary>
    public class RobloxBidSniperService : IDisposable
    {
        #region Win32 API
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
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_V = 0x56;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        #endregion

        private static RobloxBidSniperService _instance;
        private static readonly object _syncRoot = new object();

        public static RobloxBidSniperService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new RobloxBidSniperService();
                        }
                    }
                }
                return _instance;
            }
        }

        private Thread _workerThread;
        private volatile bool _isRunning = false;
        private volatile bool _isArmed = true;
        private volatile bool _globalMode = true; // Enables testing & usage across any window layout
        private POINT _sensorPos;
        private POINT? _targetClickPos = null;
        private DateTime _lastSnipeTime = DateTime.MinValue;
        private DateTime _lastTurboClickTime = DateTime.MinValue;
        private int _successfulSnipes = 0;

        // Advanced Color Science & Trigger State
        private bool _useCalibratedColor = false;
        private string _colorPreset = "Green"; // "Green", "Gold", "Cyan", "Purple", "Custom"
        private int _calibratedR = 46;
        private int _calibratedG = 204;
        private int _calibratedB = 113;
        private int _colorTolerance = 45;
        private string _triggerMode = "Auto"; // "Auto" (smart detect), "Disruption" (needle hits green), "Edge" (entry), "Continuous"
        private int _samplingRadius = 4;
        private bool _wasInSweetSpot = false;
        private volatile bool _needsPriming = true;
        private volatile bool _autoBaselineWasMatch = false;
        private DateTime _armCooldownUntil = DateTime.MinValue;

        // Dynamic Screen Tracking Corridor & State
        private bool _dynamicTrackingEnabled = true;
        private int _trackCorridorWidth = 260;
        private int _trackCorridorHeight = 32;
        private int _trackScanStep = 4;
        private POINT? _currentTrackedPos = null;
        private bool _hasTrackLock = false;
        private DateTime _lastTrackLockTime = DateTime.MinValue;

        public event Action<string> OnLog;
        public event Action<bool> OnArmedChanged;
        public event Action<int, int, int, bool> OnColorSampled;
        public event Action<int, int, int> OnSnipeFired;
        public event Action<int, int> OnSensorPositionChanged;
        public event Action<int?, int?> OnClickTargetChanged;
        public event Action<int, int, int, bool> OnTargetColorChanged;
        public event Action<int, int, bool> OnTrackPositionChanged;
        public event Action<bool> OnDynamicTrackingToggled;

        public bool IsRunning { get { return _isRunning; } }
        public bool IsArmed
        {
            get { return _isArmed; }
            set
            {
                _isArmed = value;
                _needsPriming = true;
                _armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120);
                if (OnArmedChanged != null)
                {
                    try { OnArmedChanged(_isArmed); } catch { }
                }
            }
        }
        public bool GlobalMode { get { return _globalMode; } set { _globalMode = value; } }
        public int SuccessfulSnipes { get { return _successfulSnipes; } }
        public POINT SensorPosition { get { return _sensorPos; } }
        public POINT? CalibratedClickPosition { get { return _targetClickPos; } }

        public bool DynamicTrackingEnabled
        {
            get { return _dynamicTrackingEnabled; }
            set
            {
                _dynamicTrackingEnabled = value;
                SaveCurrentSettings();
                if (OnDynamicTrackingToggled != null)
                {
                    try { OnDynamicTrackingToggled(_dynamicTrackingEnabled); } catch { }
                }
            }
        }
        public int TrackCorridorWidth { get { return _trackCorridorWidth; } set { _trackCorridorWidth = Math.Max(50, Math.Min(value, 800)); SaveCurrentSettings(); } }
        public int TrackCorridorHeight { get { return _trackCorridorHeight; } set { _trackCorridorHeight = Math.Max(10, Math.Min(value, 200)); SaveCurrentSettings(); } }
        public int TrackScanStep { get { return _trackScanStep; } set { _trackScanStep = Math.Max(1, Math.Min(value, 16)); SaveCurrentSettings(); } }
        public POINT? CurrentTrackedPosition { get { return _currentTrackedPos; } }
        public bool HasTrackLock { get { return _hasTrackLock; } }

        public string ColorPreset
        {
            get { return _colorPreset; }
            set { SetColorPreset(value); }
        }

        public bool UseCalibratedColor { get { return _useCalibratedColor; } set { _useCalibratedColor = value; SaveCurrentSettings(); } }
        public int CalibratedR { get { return _calibratedR; } }
        public int CalibratedG { get { return _calibratedG; } }
        public int CalibratedB { get { return _calibratedB; } }
        public int ColorTolerance { get { return _colorTolerance; } set { _colorTolerance = Math.Max(10, Math.Min(value, 150)); SaveCurrentSettings(); } }
        public string TriggerMode
        {
            get { return _triggerMode; }
            set
            {
                _triggerMode = string.IsNullOrEmpty(value) ? "Auto" : value;
                _needsPriming = true;
                SaveCurrentSettings();
            }
        }
        public int SamplingRadius { get { return _samplingRadius; } set { _samplingRadius = Math.Max(1, Math.Min(value, 12)); SaveCurrentSettings(); } }

        public RobloxBidSniperService()
        {
            int screenW = 1920;
            int screenH = 1080;
            try
            {
                screenW = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                screenH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
            }
            catch { }
            _sensorPos = new POINT { X = screenW / 2, Y = (int)(screenH * 0.65) };
            try
            {
                LoadFromSettings(OptimizationSettings.Load());
            }
            catch { }
        }

        public void SaveCurrentSettings()
        {
            try
            {
                var settings = OptimizationSettings.Load();
                SaveToSettings(settings);
            }
            catch { }
        }

        public void LoadFromSettings(OptimizationSettings settings)
        {
            if (settings == null) return;
            try
            {
                if (settings.SniperSensorX > 0 && settings.SniperSensorY > 0)
                {
                    _sensorPos = new POINT { X = settings.SniperSensorX, Y = settings.SniperSensorY };
                }
                if (settings.SniperHasClickTarget && settings.SniperClickX > 0 && settings.SniperClickY > 0)
                {
                    _targetClickPos = new POINT { X = settings.SniperClickX, Y = settings.SniperClickY };
                }
                else
                {
                    _targetClickPos = null;
                }

                _useCalibratedColor = settings.SniperUseCalibratedColor;
                if (settings.SniperTargetColorG > 0)
                {
                    _calibratedR = settings.SniperTargetColorR;
                    _calibratedG = settings.SniperTargetColorG;
                    _calibratedB = settings.SniperTargetColorB;
                }
                if (settings.SniperColorTolerance >= 10)
                {
                    _colorTolerance = settings.SniperColorTolerance;
                }
                if (!string.IsNullOrEmpty(settings.SniperTriggerMode))
                {
                    _triggerMode = settings.SniperTriggerMode;
                }
                if (!string.IsNullOrEmpty(settings.SniperColorPreset))
                {
                    _colorPreset = settings.SniperColorPreset;
                }
                if (settings.SniperSamplingRadius >= 1)
                {
                    _samplingRadius = settings.SniperSamplingRadius;
                }
                _dynamicTrackingEnabled = settings.SniperDynamicTrackingEnabled;
                if (settings.SniperTrackCorridorWidth > 0)
                {
                    _trackCorridorWidth = settings.SniperTrackCorridorWidth;
                }
                if (settings.SniperTrackCorridorHeight > 0)
                {
                    _trackCorridorHeight = settings.SniperTrackCorridorHeight;
                }
                if (settings.SniperTrackScanStep > 0)
                {
                    _trackScanStep = settings.SniperTrackScanStep;
                }
            }
            catch { }
        }

        public void SaveToSettings(OptimizationSettings settings)
        {
            if (settings == null) return;
            try
            {
                settings.SniperSensorX = _sensorPos.X;
                settings.SniperSensorY = _sensorPos.Y;
                settings.SniperHasClickTarget = _targetClickPos.HasValue;
                if (_targetClickPos.HasValue)
                {
                    settings.SniperClickX = _targetClickPos.Value.X;
                    settings.SniperClickY = _targetClickPos.Value.Y;
                }
                settings.SniperUseCalibratedColor = _useCalibratedColor;
                settings.SniperTargetColorR = _calibratedR;
                settings.SniperTargetColorG = _calibratedG;
                settings.SniperTargetColorB = _calibratedB;
                settings.SniperColorTolerance = _colorTolerance;
                settings.SniperTriggerMode = _triggerMode;
                settings.SniperSamplingRadius = _samplingRadius;
                settings.SniperColorPreset = _colorPreset;
                settings.SniperDynamicTrackingEnabled = _dynamicTrackingEnabled;
                settings.SniperTrackCorridorWidth = _trackCorridorWidth;
                settings.SniperTrackCorridorHeight = _trackCorridorHeight;
                settings.SniperTrackScanStep = _trackScanStep;
                settings.Save();
            }
            catch { }
        }

        public bool Start(out string message)
        {
            lock (_syncRoot)
            {
                if (_isRunning)
                {
                    message = "Roblox Auction Sweet-Spot Sniper is already running.";
                    return true;
                }

                _isRunning = true;
                _isArmed = true;
                _wasInSweetSpot = false;
                _needsPriming = true;
                _armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120);

                try { TimeBeginPeriod(1); } catch { }

                _workerThread = new Thread(WorkerLoop)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                    Name = "ZenithRobloxBidSniperThread"
                };
                _workerThread.Start();

                message = "Roblox Auction Sweet-Spot Sniper ENGAGED. [F7] Arm/Disarm • [F8] Sensor Spot • [Ctrl+F8] Bid Button • [Alt+F8] Eyedropper • [F9] HUD.";
                Log(message);
                return true;
            }
        }

        public bool Stop(out string message)
        {
            lock (_syncRoot)
            {
                if (!_isRunning)
                {
                    message = "Roblox Auction Sweet-Spot Sniper is already stopped.";
                    return true;
                }

                _isRunning = false;
                try
                {
                    if (_workerThread != null && _workerThread.IsAlive)
                    {
                        _workerThread.Join(400);
                    }
                }
                catch { }
                _workerThread = null;

                try { TimeEndPeriod(1); } catch { }

                message = string.Format("Roblox Auction Sweet-Spot Sniper disengaged. Total snipes executed: {0}", _successfulSnipes);
                Log(message);
                return true;
            }
        }

        public bool Toggle(out bool isRunning, out string message)
        {
            if (_isRunning)
            {
                bool ok = Stop(out message);
                isRunning = false;
                return ok;
            }
            else
            {
                bool ok = Start(out message);
                isRunning = true;
                return ok;
            }
        }

        public void SetSensorPosition(int x, int y)
        {
            _sensorPos = new POINT { X = x, Y = y };
            _needsPriming = true;
            SaveCurrentSettings();
            if (OnSensorPositionChanged != null)
            {
                try { OnSensorPositionChanged(x, y); } catch { }
            }
            Log(string.Format("Sensor target spot set to screen ({0}, {1})", x, y));
        }

        public bool SetSensorToCurrentCursor(out int x, out int y)
        {
            POINT cur;
            if (GetCursorPos(out cur))
            {
                x = cur.X;
                y = cur.Y;
                SetSensorPosition(x, y);
                return true;
            }
            x = _sensorPos.X;
            y = _sensorPos.Y;
            return false;
        }

        public void CalibrateTarget(int x, int y)
        {
            _targetClickPos = new POINT { X = x, Y = y };
            SaveCurrentSettings();
            if (OnClickTargetChanged != null)
            {
                try { OnClickTargetChanged(x, y); } catch { }
            }
            Log(string.Format("Calibrated Bid Click target locked at screen ({0}, {1})", x, y));
        }

        public bool SetClickTargetToCurrentCursor(out int x, out int y)
        {
            POINT cur;
            if (GetCursorPos(out cur))
            {
                x = cur.X;
                y = cur.Y;
                CalibrateTarget(x, y);
                return true;
            }
            x = _targetClickPos.HasValue ? _targetClickPos.Value.X : 0;
            y = _targetClickPos.HasValue ? _targetClickPos.Value.Y : 0;
            return false;
        }

        public void ResetCalibration()
        {
            _targetClickPos = null;
            SaveCurrentSettings();
            if (OnClickTargetChanged != null)
            {
                try { OnClickTargetChanged(null, null); } catch { }
            }
            Log("Bid Click target reset to active mouse cursor position.");
        }

        public void CalibrateTargetColor(int r, int g, int b, int tolerance = 45)
        {
            _calibratedR = r;
            _calibratedG = g;
            _calibratedB = b;
            _colorTolerance = tolerance;
            _useCalibratedColor = true;
            _needsPriming = true;
            SaveCurrentSettings();
            if (OnTargetColorChanged != null)
            {
                try { OnTargetColorChanged(r, g, b, true); } catch { }
            }
            Log(string.Format("Target color locked to RGB({0}, {1}, {2}) with tolerance ±{3}", r, g, b, tolerance));
        }

        public void ResetTargetColor()
        {
            _useCalibratedColor = false;
            _colorPreset = "Green";
            _calibratedR = 46;
            _calibratedG = 204;
            _calibratedB = 113;
            _needsPriming = true;
            SaveCurrentSettings();
            if (OnTargetColorChanged != null)
            {
                try { OnTargetColorChanged(46, 204, 113, false); } catch { }
            }
            Log("Target color reset to Dynamic HSV Green Sweet-Spot mode.");
        }

        public void SetColorPreset(string preset)
        {
            if (string.IsNullOrEmpty(preset)) preset = "Green";
            _colorPreset = preset;
            if (preset.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            {
                _useCalibratedColor = true;
            }
            else
            {
                _useCalibratedColor = false;
                if (preset.Equals("Gold", StringComparison.OrdinalIgnoreCase) || preset.Equals("Yellow", StringComparison.OrdinalIgnoreCase))
                {
                    _calibratedR = 250;
                    _calibratedG = 204;
                    _calibratedB = 21;
                }
                else if (preset.Equals("Cyan", StringComparison.OrdinalIgnoreCase) || preset.Equals("Blue", StringComparison.OrdinalIgnoreCase))
                {
                    _calibratedR = 56;
                    _calibratedG = 189;
                    _calibratedB = 248;
                }
                else if (preset.Equals("Purple", StringComparison.OrdinalIgnoreCase) || preset.Equals("Violet", StringComparison.OrdinalIgnoreCase))
                {
                    _calibratedR = 168;
                    _calibratedG = 85;
                    _calibratedB = 247;
                }
                else if (preset.Equals("Orange", StringComparison.OrdinalIgnoreCase) || preset.Equals("Red", StringComparison.OrdinalIgnoreCase) || preset.Equals("Frenzy", StringComparison.OrdinalIgnoreCase))
                {
                    _calibratedR = 249;
                    _calibratedG = 115;
                    _calibratedB = 22;
                }
                else
                {
                    _calibratedR = 46;
                    _calibratedG = 204;
                    _calibratedB = 113;
                }
            }
            _needsPriming = true;
            SaveCurrentSettings();
            if (OnTargetColorChanged != null)
            {
                try { OnTargetColorChanged(_calibratedR, _calibratedG, _calibratedB, _useCalibratedColor); } catch { }
            }
            Log(string.Format("Storage Hunters color preset switched to: {0}", preset));
        }

        public void ToggleDynamicTracking()
        {
            _dynamicTrackingEnabled = !_dynamicTrackingEnabled;
            SaveCurrentSettings();
            if (OnDynamicTrackingToggled != null)
            {
                try { OnDynamicTrackingToggled(_dynamicTrackingEnabled); } catch { }
            }
            Log(string.Format("Dynamic Screen Tracking {0}", _dynamicTrackingEnabled ? "ENABLED" : "DISABLED"));
        }

        /// <summary>
        /// Multi-point scanning across an optical corridor around (centerX, centerY).
        /// High-speed grid sampling detects dynamically shifting sweet spots along the auction meter.
        /// Calculates the centroid (center of mass) of all matching points within the corridor.
        /// </summary>
        public bool ScanCorridorForTarget(Func<int, int, uint> pixelSampler, int centerX, int centerY, int corridorWidth, int corridorHeight, int step, out int targetX, out int targetY)
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

                        if (IsTargetMatch(r, g, b))
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

        public bool SampleTargetColorAtCursor(out int r, out int g, out int b)
        {
            POINT cur;
            if (GetCursorPos(out cur))
            {
                return SampleColorAtPoint(cur.X, cur.Y, out r, out g, out b);
            }
            r = g = b = 0;
            return false;
        }

        public bool SampleColorAtPoint(int x, int y, out int r, out int g, out int b)
        {
            IntPtr hdc = IntPtr.Zero;
            try
            {
                hdc = GetDC(IntPtr.Zero);
                uint p = GetPixel(hdc, x, y);
                if (p != 0xFFFFFFFF)
                {
                    r = (int)(p & 0x000000FF);
                    g = (int)((p & 0x0000FF00) >> 8);
                    b = (int)((p & 0x00FF0000) >> 16);
                    CalibrateTargetColor(r, g, b, _colorTolerance);
                    return true;
                }
            }
            catch { }
            finally
            {
                if (hdc != IntPtr.Zero)
                {
                    try { ReleaseDC(IntPtr.Zero, hdc); } catch { }
                }
            }
            r = g = b = 0;
            return false;
        }

        /// <summary>
        /// Pure optical color detection for the auction slider green sweet-spot.
        /// Sweet spot in Bid Battles and Roblox auction meters is saturated emerald/lime green.
        /// Evaluates both RGB ratio dominance and HSV color space (hue 65°–165°).
        /// </summary>
        public static bool IsGreenSweetSpot(int r, int g, int b)
        {
            // 1. High saturation emerald / lime green ratio check (guarantees backward compatibility)
            if (g >= 100 && g > (int)(r * 1.15) && g > (int)(b * 1.15))
            {
                return true;
            }

            // 2. HSV color-space evaluation for varied game lighting & shaders
            double h, s, v;
            RgbToHsv(r, g, b, out h, out s, out v);

            // Green hue arc in HSV: 65° (yellow-green / lime) to 165° (emerald / spring green / mint)
            // S >= 0.22 (filters out gray/white/fog), V >= 0.28 (filters out near-black), Green dominance
            return (h >= 65.0 && h <= 165.0) && (s >= 0.22) && (v >= 0.28) && (g >= 70) && (g > (int)(r * 1.05)) && (g > (int)(b * 1.05));
        }

        /// <summary>
        /// Storage Hunters Gold / Yellow sweet-spot detection (High-tier / Legendary auction lots).
        /// High red and green values, lower blue, and warm gold/yellow hue (35°–64°).
        /// </summary>
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

        /// <summary>
        /// Storage Hunters Cyan / Sky Blue sweet-spot detection (Uncommon / Special auction lots).
        /// High green and blue, lower red, and cyan hue (170°–215°).
        /// </summary>
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

        /// <summary>
        /// Storage Hunters Purple / Violet sweet-spot detection (Epic tier lots).
        /// High red and blue, low green, and violet hue (260°–315°).
        /// </summary>
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

        /// <summary>
        /// Storage Hunters Orange / Red sweet-spot detection (Frenzy / Speed auction rounds).
        /// High red, lower green & blue, and warm orange/red hue (10°–35° or 345°–10°).
        /// </summary>
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

        public bool IsCalibratedColorMatch(int r, int g, int b)
        {
            int dr = r - _calibratedR;
            int dg = g - _calibratedG;
            int db = b - _calibratedB;
            double distance = Math.Sqrt((dr * dr) + (dg * dg) + (db * db));
            return distance <= _colorTolerance;
        }

        public bool IsTargetMatch(int r, int g, int b)
        {
            if (_useCalibratedColor)
            {
                return IsCalibratedColorMatch(r, g, b);
            }

            string preset = (_colorPreset ?? "Green").ToLowerInvariant();
            if (preset == "gold" || preset == "yellow")
            {
                return IsGoldSweetSpot(r, g, b);
            }
            if (preset == "cyan" || preset == "blue")
            {
                return IsCyanSweetSpot(r, g, b);
            }
            if (preset == "purple" || preset == "violet")
            {
                return IsPurpleSweetSpot(r, g, b);
            }
            if (preset == "orange" || preset == "red" || preset == "frenzy")
            {
                return IsOrangeRedSweetSpot(r, g, b);
            }
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

        /// <summary>
        /// Dispatches a single test snipe click to deterministically verify the feedback loop.
        /// </summary>
        public void TriggerTestClick()
        {
            ExecuteSnipeClick(_sensorPos.X, _sensorPos.Y, "TEST");
        }

        /// <summary>
        /// Dispatches a rapid burst of turbo bid clicks to verify multi-click input pacing.
        /// </summary>
        public void TriggerTurboBurst(int count = 3)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    ExecuteSnipeClick(_sensorPos.X, _sensorPos.Y, "TURBO");
                    Thread.Sleep(60);
                }
            });
        }

        private void WorkerLoop()
        {
            IntPtr hdcScreen = IntPtr.Zero;
            try
            {
                hdcScreen = GetDC(IntPtr.Zero);

                bool f6WasDown = false;
                bool f7WasDown = false;
                bool f8WasDown = false;
                bool f9WasDown = false;
                DateTime lastUiTelemetryTime = DateTime.MinValue;

                while (_isRunning)
                {
                    // 1. Hotkey checks
                    // [F6]: Toggle Overlay Draggable vs Locked Pass-Through
                    bool f6Down = (GetAsyncKeyState(VK_F6) & 0x8000) != 0;
                    if (f6Down && !f6WasDown)
                    {
                        try
                        {
                            ZenithOptimizer.UI.RobloxSniperOverlay.Instance.ToggleMarkersLock();
                            PlayBeep(1200, 70);
                        }
                        catch { }
                    }
                    f6WasDown = f6Down;

                    // [F7] combinations:
                    // - Normal [F7]: Toggle Arm / Disarm
                    // - [Ctrl + F7]: Toggle Dynamic Screen Tracking
                    bool ctrlDown = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                    bool altDown = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                    bool f7Down = (GetAsyncKeyState(VK_F7) & 0x8000) != 0;
                    if (f7Down && !f7WasDown)
                    {
                        if (ctrlDown)
                        {
                            _dynamicTrackingEnabled = !_dynamicTrackingEnabled;
                            SaveCurrentSettings();
                            PlayBeep(_dynamicTrackingEnabled ? 1500 : 700, 70);
                            Log(string.Format("Sniper Dynamic Screen Tracking {0} via [Ctrl+F7]", _dynamicTrackingEnabled ? "ENABLED" : "DISABLED"));
                            if (OnDynamicTrackingToggled != null)
                            {
                                try { OnDynamicTrackingToggled(_dynamicTrackingEnabled); } catch { }
                            }
                        }
                        else
                        {
                            _isArmed = !_isArmed;
                            _needsPriming = true;
                            _armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120);
                            PlayBeep(_isArmed ? 1100 : 550, 70);
                            Log(string.Format("Sniper {0} via [F7]", _isArmed ? "ARMED (Auto-click ready)" : "DISARMED (Standby)"));
                            if (OnArmedChanged != null)
                            {
                                try { OnArmedChanged(_isArmed); } catch { }
                            }
                        }
                    }
                    f7WasDown = f7Down;

                    // [F8] combinations:
                    // - Normal [F8]: Lock sensor spot to cursor
                    // - [Ctrl + F8]: Lock Bid Button click target to cursor
                    // - [Alt + F8]: Sample Target Color (Eyedropper) at cursor
                    bool f8Down = (GetAsyncKeyState(VK_F8) & 0x8000) != 0;
                    if (f8Down && !f8WasDown)
                    {
                        if (ctrlDown)
                        {
                            int cx, cy;
                            if (SetClickTargetToCurrentCursor(out cx, out cy))
                            {
                                PlayBeep(1600, 90);
                            }
                        }
                        else if (altDown)
                        {
                            int cr, cg, cb;
                            if (SampleTargetColorAtCursor(out cr, out cg, out cb))
                            {
                                PlayBeep(1800, 90);
                            }
                        }
                        else
                        {
                            POINT cur;
                            if (GetCursorPos(out cur))
                            {
                                SetSensorPosition(cur.X, cur.Y);
                                PlayBeep(1400, 80);
                            }
                        }
                    }
                    f8WasDown = f8Down;

                    // [F9]: Toggle Gaming HUD Overlay visibility
                    bool f9Down = (GetAsyncKeyState(VK_F9) & 0x8000) != 0;
                    if (f9Down && !f9WasDown)
                    {
                        try
                        {
                            ZenithOptimizer.UI.RobloxSniperOverlay.Instance.Toggle();
                            PlayBeep(900, 60);
                        }
                        catch { }
                    }
                    f9WasDown = f9Down;

                    // 2. Window Filtering: Check if Roblox is active OR global mode is enabled
                    bool isTargetWindowActive = _globalMode;
                    if (!isTargetWindowActive)
                    {
                        IntPtr fgWnd = GetForegroundWindow();
                        if (fgWnd != IntPtr.Zero)
                        {
                            StringBuilder sbTitle = new StringBuilder(256);
                            GetWindowText(fgWnd, sbTitle, 256);
                            string title = sbTitle.ToString();

                            StringBuilder sbClass = new StringBuilder(256);
                            GetClassName(fgWnd, sbClass, 256);
                            string clsName = sbClass.ToString();

                            uint pid;
                            GetWindowThreadProcessId(fgWnd, out pid);
                            string procName = "";
                            try { procName = Process.GetProcessById((int)pid).ProcessName; } catch { }

                            if (title.IndexOf("Roblox", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                procName.IndexOf("Roblox", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                clsName.IndexOf("WINDOWSCLIENT", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                isTargetWindowActive = true;
                            }
                        }
                    }

                    if (isTargetWindowActive)
                    {
                        int sx = _sensorPos.X;
                        int sy = _sensorPos.Y;
                        int activeX = sx;
                        int activeY = sy;

                        // Dynamic Screen Tracking Scan:
                        // Scans horizontal/grid search band across the meter to lock onto shifted sweet-spots
                        if (_dynamicTrackingEnabled)
                        {
                            bool lockMaintained = false;
                            // Fast-path: If already locked onto sweet-spot, check if current lock position is still valid
                            if (_hasTrackLock && _currentTrackedPos.HasValue)
                            {
                                uint lockPixel = GetPixel(hdcScreen, _currentTrackedPos.Value.X, _currentTrackedPos.Value.Y);
                                if (lockPixel != 0xFFFFFFFF)
                                {
                                    int lr = (int)(lockPixel & 0x000000FF);
                                    int lg = (int)((lockPixel & 0x0000FF00) >> 8);
                                    int lb = (int)((lockPixel & 0x00FF0000) >> 16);
                                    if (IsTargetMatch(lr, lg, lb))
                                    {
                                        lockMaintained = true;
                                        activeX = _currentTrackedPos.Value.X;
                                        activeY = _currentTrackedPos.Value.Y;
                                        _lastTrackLockTime = DateTime.UtcNow;
                                    }
                                }
                            }

                            if (!lockMaintained)
                            {
                                int foundTrackX, foundTrackY;
                                bool trackFound = ScanCorridorForTarget(
                                    (px, py) => GetPixel(hdcScreen, px, py),
                                    sx, sy, _trackCorridorWidth, _trackCorridorHeight, _trackScanStep,
                                    out foundTrackX, out foundTrackY);

                                if (trackFound)
                                {
                                    _currentTrackedPos = new POINT { X = foundTrackX, Y = foundTrackY };
                                    _hasTrackLock = true;
                                    _lastTrackLockTime = DateTime.UtcNow;
                                    activeX = foundTrackX;
                                    activeY = foundTrackY;
                                }
                                else if ((DateTime.UtcNow - _lastTrackLockTime).TotalMilliseconds < 150 && _currentTrackedPos.HasValue)
                                {
                                    // Grace period holds lock during momentary transition frames
                                    activeX = _currentTrackedPos.Value.X;
                                    activeY = _currentTrackedPos.Value.Y;
                                }
                                else
                                {
                                    _hasTrackLock = false;
                                    activeX = sx;
                                    activeY = sy;
                                }
                            }
                        }
                        else
                        {
                            _hasTrackLock = false;
                        }

                        // Primary focal center pixel at active tracking coordinate
                        uint pixel = GetPixel(hdcScreen, activeX, activeY);
                        if (pixel == 0xFFFFFFFF)
                        {
                            // Reset DC if GDI handle became invalid
                            try { ReleaseDC(IntPtr.Zero, hdcScreen); } catch { }
                            hdcScreen = GetDC(IntPtr.Zero);
                            pixel = GetPixel(hdcScreen, activeX, activeY);
                        }

                        int r = (int)(pixel & 0x000000FF);
                        int g = (int)((pixel & 0x0000FF00) >> 8);
                        int b = (int)((pixel & 0x00FF0000) >> 16);

                        bool isMatch;
                        string firingMode;
                        bool shouldFire = EvaluateTriggerFrame(r, g, b, out isMatch, out firingMode);

                        // 4. Turbo Bidding Mode: Hold [V] to rapid-bid
                        // Guarded: only turbo-bids when sensor IS CONFIRMED ON THE GREEN!
                        bool vDown = (GetAsyncKeyState(VK_V) & 0x8000) != 0;
                        if (vDown && isMatch)
                        {
                            if ((DateTime.UtcNow - _lastTurboClickTime).TotalMilliseconds >= 80)
                            {
                                _lastTurboClickTime = DateTime.UtcNow;
                                ExecuteSnipeClick(activeX, activeY, "TURBO-HOLD");
                            }
                        }

                        // Notify HUD overlay of telemetry at smooth ~40 FPS (every 25ms) to prevent WPF Dispatcher queue saturation
                        if ((DateTime.UtcNow - lastUiTelemetryTime).TotalMilliseconds >= 25)
                        {
                            lastUiTelemetryTime = DateTime.UtcNow;

                            if (_dynamicTrackingEnabled && OnTrackPositionChanged != null)
                            {
                                try { OnTrackPositionChanged(activeX, activeY, _hasTrackLock); } catch { }
                            }

                            if (OnColorSampled != null)
                            {
                                try { OnColorSampled(r, g, b, isMatch); } catch { }
                            }
                        }

                        // 5. Fire Snipe based on confirmed trigger
                        if (shouldFire)
                        {
                            _lastSnipeTime = DateTime.UtcNow;
                            ExecuteSnipeClick(activeX, activeY, firingMode);
                        }
                    }

                    // High precision 1ms sampling loop
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Log("Roblox Bid Sniper worker exception: " + ex.Message);
            }
            finally
            {
                if (hdcScreen != IntPtr.Zero)
                {
                    try { ReleaseDC(IntPtr.Zero, hdcScreen); } catch { }
                }
            }
        }

        /// <summary>
        /// Pure state-machine evaluation for optical sweet-spot detection.
        /// Deterministic and testable without active screen DC or threads.
        /// Guaranteed: will NEVER fire if the sensor is not on the green (unless explicitly in Disruption mode).
        /// </summary>
        public bool EvaluateTriggerFrame(int r, int g, int b, out bool isMatch, out string firingMode, bool ignoreCooldown = false)
        {
            firingMode = _triggerMode ?? "Auto";
            bool focalMatch = IsTargetMatch(r, g, b);
            isMatch = focalMatch;

            if (_needsPriming)
            {
                _wasInSweetSpot = focalMatch;
                _autoBaselineWasMatch = focalMatch;
                _needsPriming = false;
                _armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120);
                return false;
            }

            if (!_isArmed)
            {
                _wasInSweetSpot = focalMatch;
                return false;
            }

            if (!ignoreCooldown)
            {
                if (DateTime.UtcNow < _armCooldownUntil)
                {
                    _wasInSweetSpot = focalMatch;
                    return false;
                }

                bool canTriggerTime = (DateTime.UtcNow - _lastSnipeTime).TotalMilliseconds >= 180;
                if (!canTriggerTime)
                {
                    _wasInSweetSpot = focalMatch;
                    return false;
                }
            }

            bool shouldFire = false;
            string mode = _triggerMode ?? "Auto";

            if (mode.Equals("Continuous", StringComparison.OrdinalIgnoreCase))
            {
                // Rapid bids while actively sustained inside the green sweet spot
                if (focalMatch)
                {
                    shouldFire = true;
                    firingMode = "CONTINUOUS";
                }
            }
            else if (mode.Equals("Disruption", StringComparison.OrdinalIgnoreCase))
            {
                // Explicit needle disruption mode (only when user explicitly selects this mode)
                // Requires the sensor to have been confirmed green in prior frame
                if (_wasInSweetSpot && !focalMatch)
                {
                    shouldFire = true;
                    firingMode = "DISRUPTION";
                }
            }
            else // "Auto" (Smart-Sense Green Entry) & "Edge" (Entry Trigger)
            {
                // Positively detects when target transitions onto the green sweet spot
                // Pre-condition: was NOT on green
                // Current condition: IS ON GREEN (confirmed green!)
                if (!_wasInSweetSpot && focalMatch)
                {
                    shouldFire = true;
                    firingMode = mode.ToUpperInvariant();
                }
            }

            // CRITICAL POSITIVE DETECTION GUARD:
            // In Auto, Edge, and Continuous modes: NEVER allow firing if focalMatch is false!
            if (!mode.Equals("Disruption", StringComparison.OrdinalIgnoreCase) && !focalMatch)
            {
                shouldFire = false;
            }

            _wasInSweetSpot = focalMatch;
            return shouldFire;
        }

        private void ExecuteSnipeClick(int hitX, int hitY, string mode)
        {
            // Optical pre-bid verification guard:
            // Confirm the target is still on green immediately before sending mouse clicks
            if (mode != "TEST" && mode != "DISRUPTION")
            {
                IntPtr hdc = IntPtr.Zero;
                try
                {
                    hdc = GetDC(IntPtr.Zero);
                    uint p = GetPixel(hdc, hitX, hitY);
                    if (p != 0xFFFFFFFF)
                    {
                        int r = (int)(p & 0x000000FF);
                        int g = (int)((p & 0x0000FF00) >> 8);
                        int b = (int)((p & 0x00FF0000) >> 16);
                        if (!IsTargetMatch(r, g, b))
                        {
                            Log(string.Format("[{0}] Snipe aborted: Target moved off green before click execution.", mode));
                            return;
                        }
                    }
                }
                catch { }
                finally
                {
                    if (hdc != IntPtr.Zero)
                    {
                        try { ReleaseDC(IntPtr.Zero, hdc); } catch { }
                    }
                }
            }

            POINT origPos;
            GetCursorPos(out origPos);

            if (_targetClickPos.HasValue)
            {
                SetCursorPos(_targetClickPos.Value.X, _targetClickPos.Value.Y);
                // 25ms mouse down dwell time guarantees Roblox UI event loop registers the click
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(25);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                SetCursorPos(origPos.X, origPos.Y);
            }
            else
            {
                // Click at current cursor location (where player is aiming their mouse on the BID button)
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(25);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            }

            _successfulSnipes++;

            if (OnSnipeFired != null)
            {
                try { OnSnipeFired(hitX, hitY, _successfulSnipes); } catch { }
            }

            Log(string.Format("[{0}] SNIPE #{1} FIRED at ({2}, {3})", mode, _successfulSnipes, _targetClickPos.HasValue ? _targetClickPos.Value.X : origPos.X, _targetClickPos.HasValue ? _targetClickPos.Value.Y : origPos.Y));
        }

        private static void PlayBeep(int freq, int duration)
        {
            try
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try { Console.Beep(freq, duration); } catch { }
                });
            }
            catch { }
        }

        private void Log(string msg)
        {
            if (OnLog != null)
            {
                try { OnLog(msg); } catch { }
            }
        }

        public void Dispose()
        {
            string msg;
            Stop(out msg);
        }
    }
}
