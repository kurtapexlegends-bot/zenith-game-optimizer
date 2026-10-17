using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services.Sniper;

namespace ZenithOptimizer.Services
{
    public class RobloxBidSniperService : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_F10 = 0x79;
        public const int VK_V = 0x56;

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
                        if (_instance == null) _instance = new RobloxBidSniperService();
                    }
                }
                return _instance;
            }
        }

        private Thread _workerThread;
        private volatile bool _isRunning = false, _isArmed = true, _globalMode = true, _needsPriming = true, _autoBaselineWasMatch = false;
        private POINT _sensorPos;
        private POINT? _targetClickPos = null, _currentTrackedPos = null;
        private DateTime _lastSnipeTime = DateTime.MinValue, _lastTurboClickTime = DateTime.MinValue, _armCooldownUntil = DateTime.MinValue, _lastTrackLockTime = DateTime.MinValue;
        private int _successfulSnipes = 0, _calibratedR = 46, _calibratedG = 204, _calibratedB = 113, _colorTolerance = 45, _samplingRadius = 4;
        private int _trackCorridorWidth = 260, _trackCorridorHeight = 32, _trackScanStep = 4, _auctionCountdownSeconds = 5;
        private bool _useCalibratedColor = false, _wasInSweetSpot = false, _dynamicTrackingEnabled = true, _hasTrackLock = false, _restoreCursorAfterClick = false;
        private string _colorPreset = "Green", _triggerMode = "Auto", _captureMethod = "Auto", _clickMode = "Dual", _clickTargetMode = "FixedTarget", _lastActiveCaptureSource = "Desktop DC";
        private IntPtr _lastRobloxHwnd = IntPtr.Zero;
        private CancellationTokenSource _timerSnipeCts;

        public event Action<string> OnLog;
        public event Action<bool> OnArmedChanged, OnDynamicTrackingToggled;
        public event Action<int, int, int, bool> OnColorSampled, OnTargetColorChanged;
        public event Action<int, int, int, bool, string> OnColorSampledEx;
        public event Action<int, int, int> OnSnipeFired;
        public event Action<int, int> OnSensorPositionChanged;
        public event Action<int?, int?> OnClickTargetChanged;
        public event Action<int, int, bool> OnTrackPositionChanged;
        public event Action<string> OnCaptureSourceChanged, OnClickTargetModeChanged;

        public string CaptureMethod { get { return _captureMethod; } set { _captureMethod = string.IsNullOrEmpty(value) ? "Auto" : value; SaveCurrentSettings(); if (OnCaptureSourceChanged != null) try { OnCaptureSourceChanged(_captureMethod); } catch { } } }
        public string ClickMode { get { return _clickMode; } set { _clickMode = string.IsNullOrEmpty(value) ? "Dual" : value; SaveCurrentSettings(); } }
        public int AuctionCountdownSeconds { get { return _auctionCountdownSeconds; } set { _auctionCountdownSeconds = Math.Max(1, Math.Min(value, 60)); SaveCurrentSettings(); } }
        public string ClickTargetMode { get { return _clickTargetMode; } set { _clickTargetMode = string.IsNullOrEmpty(value) ? "FixedTarget" : value; SaveCurrentSettings(); if (OnClickTargetModeChanged != null) try { OnClickTargetModeChanged(_clickTargetMode); } catch { } } }
        public bool RestoreCursorAfterClick { get { return _restoreCursorAfterClick; } set { _restoreCursorAfterClick = value; SaveCurrentSettings(); } }
        public string LastActiveCaptureSource { get { return _lastActiveCaptureSource; } }
        public IntPtr LastRobloxHwnd { get { return _lastRobloxHwnd; } }
        public bool IsRunning { get { return _isRunning; } }
        public bool IsArmed { get { return _isArmed; } set { _isArmed = value; _needsPriming = true; _armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120); if (OnArmedChanged != null) try { OnArmedChanged(_isArmed); } catch { } } }
        public bool GlobalMode { get { return _globalMode; } set { _globalMode = value; } }
        public int SuccessfulSnipes { get { return _successfulSnipes; } }
        public POINT SensorPosition { get { return _sensorPos; } }
        public POINT? CalibratedClickPosition { get { return _targetClickPos; } }
        public bool DynamicTrackingEnabled { get { return _dynamicTrackingEnabled; } set { _dynamicTrackingEnabled = value; SaveCurrentSettings(); if (OnDynamicTrackingToggled != null) try { OnDynamicTrackingToggled(_dynamicTrackingEnabled); } catch { } } }
        public int TrackCorridorWidth { get { return _trackCorridorWidth; } set { _trackCorridorWidth = Math.Max(50, Math.Min(value, 800)); SaveCurrentSettings(); } }
        public int TrackCorridorHeight { get { return _trackCorridorHeight; } set { _trackCorridorHeight = Math.Max(10, Math.Min(value, 200)); SaveCurrentSettings(); } }
        public int TrackScanStep { get { return _trackScanStep; } set { _trackScanStep = Math.Max(1, Math.Min(value, 16)); SaveCurrentSettings(); } }
        public POINT? CurrentTrackedPosition { get { return _currentTrackedPos; } }
        public bool HasTrackLock { get { return _hasTrackLock; } }
        public string ColorPreset { get { return _colorPreset; } set { SetColorPreset(value); } }
        public bool UseCalibratedColor { get { return _useCalibratedColor; } set { _useCalibratedColor = value; SaveCurrentSettings(); } }
        public int CalibratedR { get { return _calibratedR; } }
        public int CalibratedG { get { return _calibratedG; } }
        public int CalibratedB { get { return _calibratedB; } }
        public int ColorTolerance { get { return _colorTolerance; } set { _colorTolerance = Math.Max(10, Math.Min(value, 150)); SaveCurrentSettings(); } }
        public string TriggerMode { get { return _triggerMode; } set { _triggerMode = string.IsNullOrEmpty(value) ? "Auto" : value; _needsPriming = true; SaveCurrentSettings(); } }
        public int SamplingRadius { get { return _samplingRadius; } set { _samplingRadius = Math.Max(1, Math.Min(value, 12)); SaveCurrentSettings(); } }

        public RobloxBidSniperService()
        {
            int screenW = 1920, screenH = 1080;
            try { screenW = (int)System.Windows.SystemParameters.PrimaryScreenWidth; screenH = (int)System.Windows.SystemParameters.PrimaryScreenHeight; } catch { }
            _sensorPos = new POINT { X = screenW / 2, Y = (int)(screenH * 0.65) };
            _targetClickPos = new POINT { X = screenW / 2, Y = (int)(screenH * 0.78) };
            try { LoadFromSettings(OptimizationSettings.Load()); } catch { }
        }

        public void SaveCurrentSettings() { try { var s = OptimizationSettings.Load(); SaveToSettings(s); } catch { } }
        public void LoadFromSettings(OptimizationSettings s)
        {
            if (s == null) return;
            try
            {
                if (s.SniperSensorX > 0 && s.SniperSensorY > 0) _sensorPos = new POINT { X = s.SniperSensorX, Y = s.SniperSensorY };
                if (s.SniperHasClickTarget && s.SniperClickX > 0 && s.SniperClickY > 0) _targetClickPos = new POINT { X = s.SniperClickX, Y = s.SniperClickY };
                else _targetClickPos = new POINT { X = _sensorPos.X, Y = (int)(_sensorPos.Y + 120) };
                _useCalibratedColor = s.SniperUseCalibratedColor;
                if (s.SniperTargetColorG > 0) { _calibratedR = s.SniperTargetColorR; _calibratedG = s.SniperTargetColorG; _calibratedB = s.SniperTargetColorB; }
                if (s.SniperColorTolerance >= 10) _colorTolerance = s.SniperColorTolerance;
                if (!string.IsNullOrEmpty(s.SniperTriggerMode)) _triggerMode = s.SniperTriggerMode;
                if (!string.IsNullOrEmpty(s.SniperColorPreset)) _colorPreset = s.SniperColorPreset;
                if (s.SniperSamplingRadius >= 1) _samplingRadius = s.SniperSamplingRadius;
                _dynamicTrackingEnabled = s.SniperDynamicTrackingEnabled;
                if (s.SniperTrackCorridorWidth > 0) _trackCorridorWidth = s.SniperTrackCorridorWidth;
                if (s.SniperTrackCorridorHeight > 0) _trackCorridorHeight = s.SniperTrackCorridorHeight;
                if (s.SniperTrackScanStep > 0) _trackScanStep = s.SniperTrackScanStep;
                if (!string.IsNullOrEmpty(s.SniperCaptureMethod)) _captureMethod = s.SniperCaptureMethod;
                if (!string.IsNullOrEmpty(s.SniperClickMode)) _clickMode = s.SniperClickMode;
                if (s.SniperAuctionCountdownSeconds > 0) _auctionCountdownSeconds = s.SniperAuctionCountdownSeconds;
                if (!string.IsNullOrEmpty(s.SniperClickTargetMode)) _clickTargetMode = s.SniperClickTargetMode;
                _restoreCursorAfterClick = s.SniperRestoreCursor;
            }
            catch { }
        }

        public void SaveToSettings(OptimizationSettings s)
        {
            if (s == null) return;
            try
            {
                s.SniperSensorX = _sensorPos.X; s.SniperSensorY = _sensorPos.Y;
                s.SniperHasClickTarget = _targetClickPos.HasValue;
                if (_targetClickPos.HasValue) { s.SniperClickX = _targetClickPos.Value.X; s.SniperClickY = _targetClickPos.Value.Y; }
                s.SniperUseCalibratedColor = _useCalibratedColor;
                s.SniperTargetColorR = _calibratedR; s.SniperTargetColorG = _calibratedG; s.SniperTargetColorB = _calibratedB;
                s.SniperColorTolerance = _colorTolerance; s.SniperTriggerMode = _triggerMode; s.SniperSamplingRadius = _samplingRadius;
                s.SniperColorPreset = _colorPreset; s.SniperDynamicTrackingEnabled = _dynamicTrackingEnabled;
                s.SniperTrackCorridorWidth = _trackCorridorWidth; s.SniperTrackCorridorHeight = _trackCorridorHeight;
                s.SniperTrackScanStep = _trackScanStep; s.SniperCaptureMethod = _captureMethod; s.SniperClickMode = _clickMode;
                s.SniperAuctionCountdownSeconds = _auctionCountdownSeconds; s.SniperClickTargetMode = _clickTargetMode;
                s.SniperRestoreCursor = _restoreCursorAfterClick; s.Save();
            }
            catch { }
        }

        public bool Start(out string msg)
        {
            lock (_syncRoot)
            {
                if (_isRunning) { msg = "Roblox Auction Sweet-Spot Sniper is already running."; return true; }
                _isRunning = true; _isArmed = true; _wasInSweetSpot = false; _needsPriming = true;
                _armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120);
                try { TimeBeginPeriod(1); } catch { }
                _workerThread = new Thread(WorkerLoop) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "ZenithRobloxBidSniperThread" };
                _workerThread.Start();
                msg = "Roblox Auction Sweet-Spot Sniper ENGAGED. [F7] Arm/Disarm • [F8] Sensor Spot • [Ctrl+F8] Bid Button • [Alt+F8] Eyedropper • [F9] HUD.";
                Log(msg); return true;
            }
        }

        public bool Stop(out string msg)
        {
            lock (_syncRoot)
            {
                if (!_isRunning) { msg = "Roblox Auction Sweet-Spot Sniper is already stopped."; return true; }
                _isRunning = false;
                try { if (_workerThread != null && _workerThread.IsAlive) _workerThread.Join(400); } catch { }
                _workerThread = null;
                try { TimeEndPeriod(1); } catch { }
                msg = string.Format("Roblox Auction Sweet-Spot Sniper disengaged. Total snipes executed: {0}", _successfulSnipes);
                Log(msg); return true;
            }
        }

        public bool Toggle(out bool isRunning, out string msg) { if (_isRunning) { bool ok = Stop(out msg); isRunning = false; return ok; } else { bool ok = Start(out msg); isRunning = true; return ok; } }
        public void SetSensorPosition(int x, int y) { _sensorPos = new POINT { X = x, Y = y }; _needsPriming = true; SaveCurrentSettings(); if (OnSensorPositionChanged != null) try { OnSensorPositionChanged(x, y); } catch { } Log(string.Format("Sensor target spot set to screen ({0}, {1})", x, y)); }
        public bool SetSensorToCurrentCursor(out int x, out int y) { POINT cur; if (GetCursorPos(out cur)) { x = cur.X; y = cur.Y; SetSensorPosition(x, y); return true; } x = _sensorPos.X; y = _sensorPos.Y; return false; }
        public bool SetSensorPositionToCursor() { int x, y; return SetSensorToCurrentCursor(out x, out y); }
        public void CalibrateTarget(int x, int y) { _targetClickPos = new POINT { X = x, Y = y }; SaveCurrentSettings(); if (OnClickTargetChanged != null) try { OnClickTargetChanged(x, y); } catch { } Log(string.Format("Calibrated Bid Click target locked at screen ({0}, {1})", x, y)); }
        public bool SetClickTargetToCurrentCursor(out int x, out int y) { POINT cur; if (GetCursorPos(out cur)) { x = cur.X; y = cur.Y; CalibrateTarget(x, y); return true; } x = _targetClickPos.HasValue ? _targetClickPos.Value.X : 0; y = _targetClickPos.HasValue ? _targetClickPos.Value.Y : 0; return false; }
        public void ResetCalibration() { _targetClickPos = null; SaveCurrentSettings(); if (OnClickTargetChanged != null) try { OnClickTargetChanged(null, null); } catch { } Log("Bid Click target reset to active mouse cursor position."); }
        public void CalibrateTargetColor(int r, int g, int b, int tolerance = 45) { _calibratedR = r; _calibratedG = g; _calibratedB = b; _colorTolerance = tolerance; _useCalibratedColor = true; _needsPriming = true; SaveCurrentSettings(); if (OnTargetColorChanged != null) try { OnTargetColorChanged(r, g, b, true); } catch { } Log(string.Format("Target color locked to RGB({0}, {1}, {2}) with tolerance ±{3}", r, g, b, tolerance)); }
        public void ResetTargetColor() { _useCalibratedColor = false; _colorPreset = "Green"; _calibratedR = 46; _calibratedG = 204; _calibratedB = 113; _needsPriming = true; SaveCurrentSettings(); if (OnTargetColorChanged != null) try { OnTargetColorChanged(46, 204, 113, false); } catch { } Log("Target color reset to Dynamic HSV Green Sweet-Spot mode."); }
        public void SetColorPreset(string preset)
        {
            if (string.IsNullOrEmpty(preset)) preset = "Green";
            _colorPreset = preset;
            if (preset.Equals("Custom", StringComparison.OrdinalIgnoreCase)) _useCalibratedColor = true;
            else
            {
                _useCalibratedColor = false;
                if (preset.Equals("Gold", StringComparison.OrdinalIgnoreCase) || preset.Equals("Yellow", StringComparison.OrdinalIgnoreCase)) { _calibratedR = 250; _calibratedG = 204; _calibratedB = 21; }
                else if (preset.Equals("Cyan", StringComparison.OrdinalIgnoreCase) || preset.Equals("Blue", StringComparison.OrdinalIgnoreCase)) { _calibratedR = 56; _calibratedG = 189; _calibratedB = 248; }
                else if (preset.Equals("Purple", StringComparison.OrdinalIgnoreCase) || preset.Equals("Violet", StringComparison.OrdinalIgnoreCase)) { _calibratedR = 168; _calibratedG = 85; _calibratedB = 247; }
                else if (preset.Equals("Orange", StringComparison.OrdinalIgnoreCase) || preset.Equals("Red", StringComparison.OrdinalIgnoreCase) || preset.Equals("Frenzy", StringComparison.OrdinalIgnoreCase)) { _calibratedR = 249; _calibratedG = 115; _calibratedB = 22; }
                else { _calibratedR = 46; _calibratedG = 204; _calibratedB = 113; }
            }
            _needsPriming = true; SaveCurrentSettings(); if (OnTargetColorChanged != null) try { OnTargetColorChanged(_calibratedR, _calibratedG, _calibratedB, _useCalibratedColor); } catch { } Log(string.Format("Storage Hunters color preset switched to: {0}", preset));
        }
        public void ToggleDynamicTracking() { _dynamicTrackingEnabled = !_dynamicTrackingEnabled; SaveCurrentSettings(); if (OnDynamicTrackingToggled != null) try { OnDynamicTrackingToggled(_dynamicTrackingEnabled); } catch { } Log(string.Format("Dynamic Screen Tracking {0}", _dynamicTrackingEnabled ? "ENABLED" : "DISABLED")); }
        public bool ScanCorridorForTarget(Func<int, int, uint> sampler, int cx, int cy, int cw, int ch, int step, out int tx, out int ty)
        {
            return TrackCorridorScanner.ScanCorridorForTarget(sampler, cx, cy, cw, ch, step, IsTargetMatch, out tx, out ty);
        }
        public bool SampleTargetColorAtCursor(out int r, out int g, out int b) { POINT cur; if (GetCursorPos(out cur)) return SampleColorAtPoint(cur.X, cur.Y, out r, out g, out b); r = g = b = 0; return false; }

        public static IntPtr FindRobloxClientHwnd() { return OpticalSamplingEngine.FindRobloxClientHwnd(); }
        public static bool IsRobloxClientWindow(IntPtr hWnd) { return OpticalSamplingEngine.IsRobloxClientWindow(hWnd); }
        public uint SamplePixelWithFallback(int screenX, int screenY, out string captureSource)
        {
            uint px = OpticalSamplingEngine.SamplePixelWithFallback(screenX, screenY, out captureSource, _captureMethod, _lastRobloxHwnd);
            _lastActiveCaptureSource = captureSource;
            return px;
        }
        public uint SamplePixelBitBlt(int screenX, int screenY, IntPtr rbxHwnd) { return OpticalSamplingEngine.SamplePixelBitBlt(screenX, screenY, rbxHwnd); }
        public uint SamplePixelPrintWindow(int screenX, int screenY, IntPtr rbxHwnd) { return OpticalSamplingEngine.SamplePixelPrintWindow(screenX, screenY, rbxHwnd); }
        public bool SampleColorAtPoint(int x, int y, out int r, out int g, out int b)
        {
            string src; uint p = SamplePixelWithFallback(x, y, out src);
            if (p != 0xFFFFFFFF) { r = (int)(p & 0xFF); g = (int)((p >> 8) & 0xFF); b = (int)((p >> 16) & 0xFF); CalibrateTargetColor(r, g, b, _colorTolerance); Log(string.Format("Sampled color RGB({0}, {1}, {2}) from {3}", r, g, b, src)); return true; }
            r = g = b = 0; return false;
        }

        public static bool IsGreenSweetSpot(int r, int g, int b) { return ColorClassifier.IsGreenSweetSpot(r, g, b); }
        public static bool IsGoldSweetSpot(int r, int g, int b) { return ColorClassifier.IsGoldSweetSpot(r, g, b); }
        public static bool IsCyanSweetSpot(int r, int g, int b) { return ColorClassifier.IsCyanSweetSpot(r, g, b); }
        public static bool IsPurpleSweetSpot(int r, int g, int b) { return ColorClassifier.IsPurpleSweetSpot(r, g, b); }
        public static bool IsOrangeRedSweetSpot(int r, int g, int b) { return ColorClassifier.IsOrangeRedSweetSpot(r, g, b); }
        public bool IsCalibratedColorMatch(int r, int g, int b) { return ColorClassifier.IsCalibratedColorMatch(r, g, b, _calibratedR, _calibratedG, _calibratedB, _colorTolerance); }
        public bool IsTargetMatch(int r, int g, int b) { return ColorClassifier.IsTargetMatch(r, g, b, _colorPreset, _useCalibratedColor, _calibratedR, _calibratedG, _calibratedB, _colorTolerance); }
        public static void RgbToHsv(int r, int g, int b, out double h, out double s, out double v) { ColorClassifier.RgbToHsv(r, g, b, out h, out s, out v); }

        public void TriggerTestClick()
        {
            int clickX = _targetClickPos.HasValue ? _targetClickPos.Value.X : _sensorPos.X;
            int clickY = _targetClickPos.HasValue ? _targetClickPos.Value.Y : (int)(_sensorPos.Y + 120);
            ExecuteSnipeClick(clickX, clickY, "TEST");
            ClickDispatchEngine.PlayBeep(1800, 90);
        }
        public void TriggerTurboBurst(int count = 3) { System.Threading.Tasks.Task.Run(() => { for (int i = 0; i < count; i++) { ExecuteSnipeClick(_sensorPos.X, _sensorPos.Y, "TURBO"); Thread.Sleep(60); } }); }
        public void TurboBurstClick(int count = 5) { TriggerTurboBurst(count); }

        private void WorkerLoop()
        {
            try
            {
                bool f6WasDown = false, f7WasDown = false, f8WasDown = false, f9WasDown = false, f10WasDown = false;
                DateTime lastUiTelemetryTime = DateTime.MinValue;
                while (_isRunning)
                {
                    bool ctrlDown = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0, altDown = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                    bool f6Down = (GetAsyncKeyState(VK_F6) & 0x8000) != 0;
                    if (f6Down && !f6WasDown)
                    {
                        try { if (ctrlDown) { UI.RobloxSniperOverlay.Instance.ToggleMarkersVisibility(); ClickDispatchEngine.PlayBeep(1350, 70); } else { UI.RobloxSniperOverlay.Instance.ToggleMarkersLock(); ClickDispatchEngine.PlayBeep(1200, 70); } } catch { }
                    }
                    f6WasDown = f6Down;

                    bool f7Down = (GetAsyncKeyState(VK_F7) & 0x8000) != 0;
                    if (f7Down && !f7WasDown)
                    {
                        if (ctrlDown) { _dynamicTrackingEnabled = !_dynamicTrackingEnabled; SaveCurrentSettings(); ClickDispatchEngine.PlayBeep(_dynamicTrackingEnabled ? 1500 : 700, 70); Log(string.Format("Sniper Dynamic Screen Tracking {0} via [Ctrl+F7]", _dynamicTrackingEnabled ? "ENABLED" : "DISABLED")); if (OnDynamicTrackingToggled != null) try { OnDynamicTrackingToggled(_dynamicTrackingEnabled); } catch { } }
                        else { _isArmed = !_isArmed; _needsPriming = true; _armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120); ClickDispatchEngine.PlayBeep(_isArmed ? 1100 : 550, 70); Log(string.Format("Sniper {0} via [F7]", _isArmed ? "ARMED (Auto-click ready)" : "DISARMED (Standby)")); if (OnArmedChanged != null) try { OnArmedChanged(_isArmed); } catch { } }
                    }
                    f7WasDown = f7Down;

                    bool f8Down = (GetAsyncKeyState(VK_F8) & 0x8000) != 0;
                    if (f8Down && !f8WasDown)
                    {
                        if (ctrlDown) { int cx, cy; if (SetClickTargetToCurrentCursor(out cx, out cy)) ClickDispatchEngine.PlayBeep(1600, 90); }
                        else if (altDown) { int cr, cg, cb; if (SampleTargetColorAtCursor(out cr, out cg, out cb)) ClickDispatchEngine.PlayBeep(1800, 90); }
                        else { POINT cur; if (GetCursorPos(out cur)) { SetSensorPosition(cur.X, cur.Y); ClickDispatchEngine.PlayBeep(1400, 80); } }
                    }
                    f8WasDown = f8Down;

                    bool f9Down = (GetAsyncKeyState(VK_F9) & 0x8000) != 0;
                    if (f9Down && !f9WasDown)
                    {
                        try { if (ctrlDown) { UI.RobloxSniperOverlay.Instance.ToggleCompactMode(); ClickDispatchEngine.PlayBeep(1100, 60); } else { UI.RobloxSniperOverlay.Instance.Toggle(); ClickDispatchEngine.PlayBeep(900, 60); } } catch { }
                    }
                    f9WasDown = f9Down;

                    bool f10Down = (GetAsyncKeyState(VK_F10) & 0x8000) != 0;
                    if (f10Down && !f10WasDown)
                    {
                        try { if (ctrlDown) { UI.FpsMeterOverlay.Instance.ToggleLock(); ClickDispatchEngine.PlayBeep(UI.FpsMeterOverlay.Instance.IsClickThrough ? 800 : 1600, 60); Log(string.Format("FPS Meter Overlay {0} via [Ctrl+F10]", UI.FpsMeterOverlay.Instance.IsClickThrough ? "LOCKED (Click-Through)" : "UNLOCKED (Draggable)")); } else { UI.FpsMeterOverlay.Instance.Toggle(); ClickDispatchEngine.PlayBeep(1300, 60); Log(string.Format("On-Screen FPS Meter {0} via [F10]", UI.FpsMeterOverlay.Instance.IsVisible ? "ENABLED" : "DISABLED")); } } catch { }
                    }
                    f10WasDown = f10Down;

                    bool isTargetWindowActive = _globalMode;
                    if (!isTargetWindowActive)
                    {
                        IntPtr fgWnd = GetForegroundWindow();
                        if (fgWnd != IntPtr.Zero && (IsRobloxClientWindow(fgWnd) || FindRobloxClientHwnd() != IntPtr.Zero)) isTargetWindowActive = true;
                    }

                    if (isTargetWindowActive)
                    {
                        int sx = _sensorPos.X, sy = _sensorPos.Y, activeX = sx, activeY = sy;
                        if (_dynamicTrackingEnabled)
                        {
                            bool lockMaintained = false;
                            if (_hasTrackLock && _currentTrackedPos.HasValue)
                            {
                                string lockSrc; uint lockPixel = SamplePixelWithFallback(_currentTrackedPos.Value.X, _currentTrackedPos.Value.Y, out lockSrc);
                                if (lockPixel != 0xFFFFFFFF && IsTargetMatch((int)(lockPixel & 0xFF), (int)((lockPixel >> 8) & 0xFF), (int)((lockPixel >> 16) & 0xFF)))
                                {
                                    lockMaintained = true; activeX = _currentTrackedPos.Value.X; activeY = _currentTrackedPos.Value.Y; _lastTrackLockTime = DateTime.UtcNow;
                                }
                            }
                            if (!lockMaintained)
                            {
                                int foundX, foundY;
                                if (ScanCorridorForTarget((px, py) => { string s; return SamplePixelWithFallback(px, py, out s); }, sx, sy, _trackCorridorWidth, _trackCorridorHeight, _trackScanStep, out foundX, out foundY))
                                {
                                    _currentTrackedPos = new POINT { X = foundX, Y = foundY }; _hasTrackLock = true; _lastTrackLockTime = DateTime.UtcNow; activeX = foundX; activeY = foundY;
                                }
                                else if ((DateTime.UtcNow - _lastTrackLockTime).TotalMilliseconds < 150 && _currentTrackedPos.HasValue) { activeX = _currentTrackedPos.Value.X; activeY = _currentTrackedPos.Value.Y; }
                                else { _hasTrackLock = false; activeX = sx; activeY = sy; }
                            }
                        }
                        else _hasTrackLock = false;

                        string frameSource; uint pixel = SamplePixelWithFallback(activeX, activeY, out frameSource);
                        int r = (int)(pixel & 0xFF), g = (int)((pixel >> 8) & 0xFF), b = (int)((pixel >> 16) & 0xFF);
                        bool isMatch; string firingMode;
                        bool shouldFire = EvaluateTriggerFrame(r, g, b, out isMatch, out firingMode);

                        if ((GetAsyncKeyState(VK_V) & 0x8000) != 0 && isMatch && (DateTime.UtcNow - _lastTurboClickTime).TotalMilliseconds >= 75)
                        {
                            _lastTurboClickTime = DateTime.UtcNow; ExecuteSnipeClick(activeX, activeY, "TURBO-HOLD");
                        }

                        if ((DateTime.UtcNow - lastUiTelemetryTime).TotalMilliseconds >= 25)
                        {
                            lastUiTelemetryTime = DateTime.UtcNow;
                            if (_dynamicTrackingEnabled && OnTrackPositionChanged != null) try { OnTrackPositionChanged(activeX, activeY, _hasTrackLock); } catch { }
                            if (OnColorSampledEx != null) try { OnColorSampledEx(r, g, b, isMatch, frameSource); } catch { }
                            if (OnColorSampled != null) try { OnColorSampled(r, g, b, isMatch); } catch { }
                        }

                        if (shouldFire) { _lastSnipeTime = DateTime.UtcNow; ExecuteSnipeClick(activeX, activeY, firingMode); }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex) { Log("Roblox Bid Sniper worker exception: " + ex.Message); }
        }

        public bool EvaluateTriggerFrame(int r, int g, int b, out bool isMatch, out string firingMode, bool ignoreCooldown = false)
        {
            firingMode = _triggerMode ?? "Auto";
            bool focalMatch = IsTargetMatch(r, g, b);
            isMatch = focalMatch;

            if (_needsPriming) { _wasInSweetSpot = focalMatch; _autoBaselineWasMatch = focalMatch; _needsPriming = false; _armCooldownUntil = DateTime.UtcNow.AddMilliseconds(120); return false; }
            if (!_isArmed) { _wasInSweetSpot = focalMatch; return false; }
            if (!ignoreCooldown && (DateTime.UtcNow < _armCooldownUntil || (DateTime.UtcNow - _lastSnipeTime).TotalMilliseconds < 180)) { _wasInSweetSpot = focalMatch; return false; }

            bool shouldFire = false;
            string mode = _triggerMode ?? "Auto";
            if (mode.Equals("Turbo", StringComparison.OrdinalIgnoreCase)) { if (focalMatch && ((DateTime.UtcNow - _lastSnipeTime).TotalMilliseconds >= 75 || ignoreCooldown)) { shouldFire = true; firingMode = "TURBO"; } }
            else if (mode.Equals("TimerSnipe", StringComparison.OrdinalIgnoreCase)) { if (focalMatch) { shouldFire = true; firingMode = "TIMER"; } }
            else if (mode.Equals("Continuous", StringComparison.OrdinalIgnoreCase)) { if (focalMatch) { shouldFire = true; firingMode = "CONTINUOUS"; } }
            else if (mode.Equals("Disruption", StringComparison.OrdinalIgnoreCase)) { if (_wasInSweetSpot && !focalMatch) { shouldFire = true; firingMode = "DISRUPTION"; } }
            else if (!_wasInSweetSpot && focalMatch) { shouldFire = true; firingMode = mode.ToUpperInvariant(); }

            if (!mode.Equals("Disruption", StringComparison.OrdinalIgnoreCase) && !focalMatch) shouldFire = false;
            _wasInSweetSpot = focalMatch;
            return shouldFire;
        }

        public void DispatchMouseClick(int targetX, int targetY, string mode)
        {
            ClickDispatchEngine.POINT? pt = _targetClickPos.HasValue ? new ClickDispatchEngine.POINT { X = _targetClickPos.Value.X, Y = _targetClickPos.Value.Y } : (ClickDispatchEngine.POINT?)null;
            ClickDispatchEngine.DispatchMouseClick(targetX, targetY, mode, _clickMode, _clickTargetMode, _restoreCursorAfterClick, pt, FindRobloxClientHwnd());
        }

        private void ExecuteSnipeClick(int hitX, int hitY, string mode)
        {
            int clickX, clickY;
            if (_clickTargetMode == "Hover" && mode != "TEST") { POINT cur; GetCursorPos(out cur); clickX = cur.X; clickY = cur.Y; }
            else if (_targetClickPos.HasValue) { clickX = _targetClickPos.Value.X; clickY = _targetClickPos.Value.Y; }
            else { clickX = hitX; clickY = hitY + 120; }
            DispatchMouseClick(clickX, clickY, mode);
            _successfulSnipes++;
            if (OnSnipeFired != null) try { OnSnipeFired(clickX, clickY, _successfulSnipes); } catch { }
            Log(string.Format("[{0}] SNIPE #{1} FIRED at ({2}, {3}) via {4} ({5})", mode, _successfulSnipes, clickX, clickY, _clickMode, _clickTargetMode));
        }

        public void TriggerAuctionTimerSnipe(double countdownSeconds = 5.0, int burstCount = 4)
        {
            if (_timerSnipeCts != null) { try { _timerSnipeCts.Cancel(); _timerSnipeCts.Dispose(); } catch { } }
            _timerSnipeCts = new CancellationTokenSource();
            var token = _timerSnipeCts.Token;
            System.Threading.Tasks.Task.Run(async () =>
            {
                int delayMs = Math.Max(0, (int)((countdownSeconds - 0.25) * 1000));
                Log(string.Format("Auction Timer Snipe armed for {0:F1}s countdown (firing at -250ms latency margin)", countdownSeconds));
                try { await System.Threading.Tasks.Task.Delay(delayMs, token); } catch { return; }
                if (token.IsCancellationRequested) return;
                for (int i = 0; i < burstCount; i++) { ExecuteSnipeClick(_sensorPos.X, _sensorPos.Y, "TIMER-SNIPE"); await System.Threading.Tasks.Task.Delay(55, token); }
            }, token);
        }

        private void Log(string msg) { if (OnLog != null) try { OnLog(msg); } catch { } }
        public void Dispose() { string msg; Stop(out msg); }
    }
}
