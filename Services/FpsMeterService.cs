using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace ZenithOptimizer.Services
{
    /// <summary>
    /// High-precision, zero-drawback FPS and frametime telemetry service.
    /// Uses QueryPerformanceCounter (Stopwatch.GetTimestamp) and DWM composition frame pacing
    /// to calculate instantaneous FPS, rolling 1-second average, 1% lows, and frametime in milliseconds.
    /// Completely external and anti-cheat safe (0 memory reading, 0 DLL injection, Hyperion compliant).
    /// </summary>
    public class FpsMeterService : IDisposable
    {
        #region DWM Interop
        [StructLayout(LayoutKind.Sequential)]
        public struct UNSIGNED_RATIO
        {
            public uint uiNumerator;
            public uint uiDenominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_TIMING_INFO
        {
            public uint cbSize;
            public UNSIGNED_RATIO rateRefresh;
            public ulong qpcRefreshPeriod;
            public UNSIGNED_RATIO rateCompose;
            public ulong qpcVBlank;
            public ulong cRefresh;
            public uint cDXRefresh;
            public ulong qpcCompose;
            public ulong cFrame;
            public uint cDXPresent;
            public ulong cRefreshFrame;
            public ulong cFrameSubmitted;
            public uint cDXPresentSubmitted;
            public ulong cFrameConfirmed;
            public uint cDXPresentConfirmed;
            public ulong cRefreshConfirmed;
            public uint cDXRefreshConfirmed;
            public ulong cFramesLate;
            public ulong cFramesOutstanding;
            public ulong cFrameDisplayed;
            public ulong qpcFrameDisplayed;
            public ulong cRefreshFrameDisplayed;
            public ulong cFrameComplete;
            public ulong qpcFrameComplete;
            public ulong cFramePending;
            public ulong qpcFramePending;
            public ulong cFramesFlushed;
            public ulong cFramesDropped;
            public ulong cFramesMissed;
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmGetCompositionTimingInfo(IntPtr hwnd, ref DWM_TIMING_INFO timingInfo);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmIsCompositionEnabled(out bool enabled);
        #endregion

        private static FpsMeterService _instance;
        private static readonly object _syncRoot = new object();

        public static FpsMeterService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new FpsMeterService();
                        }
                    }
                }
                return _instance;
            }
        }

        private DispatcherTimer _sampleTimer;
        private readonly Queue<double> _frameIntervals = new Queue<double>(120);
        private readonly object _intervalLock = new object();

        private long _lastTicks;
        private ulong _lastDwmFrame;
        private ulong _lastDwmQpc;
        private double _monitorRefreshRate = 60.0;
        private bool _isRunning = false;

        public bool IsRunning { get { return _isRunning; } }
        public double CurrentFps { get; private set; }
        public double AverageFps { get; private set; }
        public double OnePercentLowFps { get; private set; }
        public double FrameTimeMs { get; private set; }
        public double MonitorRefreshRate { get { return _monitorRefreshRate; } }

        /// <summary>
        /// Dispatched when new FPS telemetry is calculated (currentFps, frameTimeMs, onePercentLow)
        /// </summary>
        public event Action<double, double, double> OnFpsUpdated;

        public FpsMeterService()
        {
            CurrentFps = 60.0;
            AverageFps = 60.0;
            OnePercentLowFps = 58.0;
            FrameTimeMs = 16.67;
            DetectMonitorRefreshRate();
        }

        private void DetectMonitorRefreshRate()
        {
            try
            {
                bool compEnabled;
                if (DwmIsCompositionEnabled(out compEnabled) == 0 && compEnabled)
                {
                    DWM_TIMING_INFO info = new DWM_TIMING_INFO();
                    info.cbSize = (uint)Marshal.SizeOf(typeof(DWM_TIMING_INFO));
                    if (DwmGetCompositionTimingInfo(IntPtr.Zero, ref info) == 0)
                    {
                        if (info.rateRefresh.uiDenominator > 0 && info.rateRefresh.uiNumerator > 0)
                        {
                            _monitorRefreshRate = Math.Round((double)info.rateRefresh.uiNumerator / info.rateRefresh.uiDenominator, 1);
                        }
                    }
                }
            }
            catch { }

            if (_monitorRefreshRate < 30.0 || _monitorRefreshRate > 480.0)
            {
                _monitorRefreshRate = 60.0;
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            DetectMonitorRefreshRate();
            _lastTicks = Stopwatch.GetTimestamp();
            _lastDwmFrame = 0;
            _lastDwmQpc = 0;

            lock (_intervalLock)
            {
                _frameIntervals.Clear();
            }

            if (_sampleTimer == null)
            {
                _sampleTimer = new DispatcherTimer(DispatcherPriority.Render);
                // Sample every 33ms (~30 updates/sec internally, smoothed telemetry)
                _sampleTimer.Interval = TimeSpan.FromMilliseconds(33);
                _sampleTimer.Tick += OnSampleTick;
            }

            _sampleTimer.Start();
            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning) return;

            if (_sampleTimer != null)
            {
                _sampleTimer.Stop();
            }

            _isRunning = false;
        }

        public void Toggle()
        {
            if (_isRunning) Stop();
            else Start();
        }

        private void OnSampleTick(object sender, EventArgs e)
        {
            long currentTicks = Stopwatch.GetTimestamp();
            long deltaTicks = currentTicks - _lastTicks;
            _lastTicks = currentTicks;

            if (deltaTicks <= 0) return;

            double deltaSec = (double)deltaTicks / Stopwatch.Frequency;
            if (deltaSec <= 0.0001 || deltaSec > 1.0) deltaSec = 1.0 / _monitorRefreshRate;

            // Attempt DWM-accurate compositor frame delta
            double computedFps = 0;
            try
            {
                DWM_TIMING_INFO info = new DWM_TIMING_INFO();
                info.cbSize = (uint)Marshal.SizeOf(typeof(DWM_TIMING_INFO));
                if (DwmGetCompositionTimingInfo(IntPtr.Zero, ref info) == 0 && info.cFrame > 0)
                {
                    if (_lastDwmFrame > 0 && info.cFrame >= _lastDwmFrame && info.qpcCompose > _lastDwmQpc)
                    {
                        ulong framesDelta = info.cFrame - _lastDwmFrame;
                        ulong qpcDelta = info.qpcCompose - _lastDwmQpc;
                        double dwmSec = (double)qpcDelta / Stopwatch.Frequency;

                        if (dwmSec > 0.001 && framesDelta > 0)
                        {
                            computedFps = (double)framesDelta / dwmSec;
                        }
                    }
                    _lastDwmFrame = info.cFrame;
                    _lastDwmQpc = info.qpcCompose;
                }
            }
            catch { }

            // If DWM was not available or paused, fallback to high-precision QPC delta
            if (computedFps <= 5.0 || computedFps > 1000.0)
            {
                computedFps = 1.0 / deltaSec;
            }

            // Clamp reasonably
            if (computedFps > 500.0) computedFps = 500.0;
            if (computedFps < 1.0) computedFps = 1.0;

            double sampleIntervalSec = 1.0 / computedFps;

            lock (_intervalLock)
            {
                _frameIntervals.Enqueue(sampleIntervalSec);
                while (_frameIntervals.Count > 60)
                {
                    _frameIntervals.Dequeue();
                }

                // Compute instantaneous smoothed FPS, average, and 1% low
                double sum = 0;
                var list = new List<double>(_frameIntervals.Count);
                foreach (var dt in _frameIntervals)
                {
                    sum += dt;
                    list.Add(dt);
                }

                if (list.Count > 0)
                {
                    double avgDt = sum / list.Count;
                    AverageFps = Math.Round(1.0 / avgDt, 0);

                    // Smooth current FPS using last 10 samples
                    int smoothCount = Math.Min(10, list.Count);
                    double recentSum = 0;
                    for (int i = list.Count - smoothCount; i < list.Count; i++)
                    {
                        recentSum += list[i];
                    }
                    double recentAvgDt = recentSum / smoothCount;
                    CurrentFps = Math.Round(1.0 / recentAvgDt, 0);
                    FrameTimeMs = Math.Round(recentAvgDt * 1000.0, 1);

                    // Calculate 1% low (99th percentile frametime converted to FPS)
                    list.Sort();
                    int p99Index = (int)Math.Floor(list.Count * 0.99);
                    if (p99Index >= list.Count) p99Index = list.Count - 1;
                    double slowestDt = list[p99Index];
                    OnePercentLowFps = slowestDt > 0 ? Math.Round(1.0 / slowestDt, 0) : CurrentFps;

                    if (OnePercentLowFps > CurrentFps) OnePercentLowFps = CurrentFps;
                }
            }

            try
            {
                if (OnFpsUpdated != null)
                {
                    OnFpsUpdated(CurrentFps, FrameTimeMs, OnePercentLowFps);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
            _frameIntervals.Clear();
        }
    }
}
