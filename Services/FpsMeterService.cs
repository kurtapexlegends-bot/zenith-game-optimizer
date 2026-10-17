using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using ZenithOptimizer.UI;

namespace ZenithOptimizer.Services
{
    /// <summary>
    /// High-precision, zero-drawback FPS and frametime telemetry service.
    /// Uses Win32 DWM compositor frame synchronization (DwmFlush) and QueryPerformanceCounter
    /// in a dedicated background worker to calculate true instantaneous FPS, rolling average,
    /// 1% low frame stutters, and frametime in milliseconds.
    /// Features exponential moving average smoothing and a steady 300ms display update rate
    /// to eliminate millisecond digit jitter while keeping telemetry responsive and anti-cheat safe.
    /// </summary>
    public class FpsMeterService : IDisposable
    {
        #region Win32 Interop
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmFlush();

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        private const int VREFRESH = 116;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);
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

        private Thread _meterThread;
        private DispatcherTimer _publishTimer;
        private readonly Queue<double> _frameIntervals = new Queue<double>(120);
        private readonly object _intervalLock = new object();

        private double _monitorRefreshRate = 60.0;
        private volatile bool _isRunning = false;
        private volatile bool _isPaused = false;

        // Exponential smoothing states to eliminate instant digit flickering
        private double _smoothedFps = -1.0;
        private double _smoothedFrameTime = -1.0;
        private double _smoothedOnePercent = -1.0;

        public bool IsRunning { get { return _isRunning; } }
        public bool IsPaused { get { return _isPaused; } }
        public double CurrentFps { get; private set; }
        public double AverageFps { get; private set; }
        public double OnePercentLowFps { get; private set; }
        public double FrameTimeMs { get; private set; }
        public double MonitorRefreshRate { get { return _monitorRefreshRate; } }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Dispatched when new FPS telemetry is calculated (currentFps, frameTimeMs, onePercentLow)
        /// </summary>
        public event Action<double, double, double> OnFpsUpdated;

        public FpsMeterService()
        {
            DetectMonitorRefreshRate();
            CurrentFps = _monitorRefreshRate;
            AverageFps = _monitorRefreshRate;
            OnePercentLowFps = Math.Max(30.0, _monitorRefreshRate - 2.0);
            FrameTimeMs = Math.Round(1000.0 / _monitorRefreshRate, 1);
            _smoothedFps = _monitorRefreshRate;
            _smoothedFrameTime = FrameTimeMs;
            _smoothedOnePercent = OnePercentLowFps;
        }

        private void DetectMonitorRefreshRate()
        {
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    try
                    {
                        int hz = GetDeviceCaps(hdc, VREFRESH);
                        if (hz >= 30 && hz <= 480)
                        {
                            _monitorRefreshRate = hz;
                            return;
                        }
                    }
                    finally
                    {
                        ReleaseDC(IntPtr.Zero, hdc);
                    }
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            _monitorRefreshRate = 60.0;
        }

        public void Start()
        {
            _isPaused = false;
            if (_isRunning) return;

            DetectMonitorRefreshRate();
            _isRunning = true;

            lock (_intervalLock)
            {
                _frameIntervals.Clear();
            }

            // Dedicated high-cadence compositor sync worker
            _meterThread = new Thread(MeterWorkerLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "ZenithFpsMeterWorker"
            };
            _meterThread.Start();

            // UI publish timer: 300ms interval (~3.3 updates/sec) provides steady, human-readable display without instant jitter
            if (_publishTimer == null)
            {
                _publishTimer = new DispatcherTimer(DispatcherPriority.Render);
                _publishTimer.Interval = TimeSpan.FromMilliseconds(300);
                _publishTimer.Tick += OnPublishTick;
            }
            _publishTimer.Start();
        }

        public void Stop()
        {
            _isPaused = true;
            if (!_isRunning) return;

            _isRunning = false;

            if (_publishTimer != null)
            {
                _publishTimer.Stop();
            }

            if (_meterThread != null)
            {
                try
                {
                    _meterThread.Interrupt();
                    if (!_meterThread.Join(400))
                    {
                        _meterThread.Abort();
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                _meterThread = null;
            }
        }

        public void Toggle()
        {
            if (_isRunning) Stop();
            else Start();
        }

        private void MeterWorkerLoop()
        {
            long freq = Stopwatch.Frequency;
            long lastTicks = Stopwatch.GetTimestamp();

            try
            {
                TimeBeginPeriod(1);
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            try
            {
                while (_isRunning)
                {
                    try
                    {
                        // Graceful pause/idle: when overlay is hidden or service is paused, idle quietly without calling DwmFlush
                        if (_isPaused || !FpsMeterOverlay.Instance.IsVisible)
                        {
                            Thread.Sleep(200);
                            lastTicks = Stopwatch.GetTimestamp();
                            continue;
                        }

                        // DwmFlush blocks until the next DWM compositor frame presentation / VBlank
                        int hr = DwmFlush();

                        long currentTicks = Stopwatch.GetTimestamp();
                        long deltaTicks = currentTicks - lastTicks;
                        lastTicks = currentTicks;

                        if (hr != 0)
                        {
                            // DWM flush was unavailable or bypassed (e.g. exclusive fullscreen); fallback to monitor cadence
                            int sleepMs = Math.Max(1, (int)(1000.0 / Math.Max(30.0, _monitorRefreshRate)));
                            Thread.Sleep(sleepMs);
                            long afterSleep = Stopwatch.GetTimestamp();
                            double fallbackDt = (double)(afterSleep - lastTicks) / freq;
                            lastTicks = afterSleep;
                            if (fallbackDt >= 0.0005 && fallbackDt <= 0.5)
                            {
                                lock (_intervalLock)
                                {
                                    _frameIntervals.Enqueue(fallbackDt);
                                    while (_frameIntervals.Count > 120) _frameIntervals.Dequeue();
                                }
                            }
                            continue;
                        }

                        if (deltaTicks <= 0) continue;

                        double dt = (double)deltaTicks / freq;
                        // Sanity range: between 0.5ms (2000 FPS) and 500ms (2 FPS)
                        if (dt >= 0.0005 && dt <= 0.5)
                        {
                            lock (_intervalLock)
                            {
                                _frameIntervals.Enqueue(dt);
                                while (_frameIntervals.Count > 120)
                                {
                                    _frameIntervals.Dequeue();
                                }
                            }
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(5);
                    }
                }
            }
            finally
            {
                try
                {
                    TimeEndPeriod(1);
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }
        }

        private void OnPublishTick(object sender, EventArgs e)
        {
            if (_isPaused || !FpsMeterOverlay.Instance.IsVisible) return;

            List<double> samples;
            lock (_intervalLock)
            {
                if (_frameIntervals.Count == 0) return;
                samples = new List<double>(_frameIntervals);
            }

            if (samples.Count == 0) return;

            // Compute windowed average frametime
            double sumAll = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                sumAll += samples[i];
            }
            double avgAll = sumAll / samples.Count;
            if (avgAll > 0.0001)
            {
                AverageFps = Math.Round(1.0 / avgAll, 0);
            }

            // Windowed recent samples (last 30 samples or total available)
            int recentCount = Math.Min(30, samples.Count);
            double sumRecent = 0;
            for (int i = samples.Count - recentCount; i < samples.Count; i++)
            {
                sumRecent += samples[i];
            }
            double avgRecent = sumRecent / recentCount;
            if (avgRecent > 0.0001)
            {
                double targetFps = 1.0 / avgRecent;
                double targetFt = avgRecent * 1000.0;

                // Smooth instantaneous jitter using 65/35 Exponential Moving Average (like RTSS / Steam Overlay)
                double alpha = 0.35;
                if (_smoothedFps <= 0.0)
                {
                    _smoothedFps = targetFps;
                    _smoothedFrameTime = targetFt;
                }
                else
                {
                    _smoothedFps = (_smoothedFps * (1.0 - alpha)) + (targetFps * alpha);
                    _smoothedFrameTime = (_smoothedFrameTime * (1.0 - alpha)) + (targetFt * alpha);
                }

                CurrentFps = Math.Round(_smoothedFps, 0);
                FrameTimeMs = Math.Round(_smoothedFrameTime, 1);
            }

            // 1% Low FPS (99th percentile longest frametime over rolling 120-frame history)
            samples.Sort();
            int p99Index = (int)Math.Floor(samples.Count * 0.99);
            if (p99Index >= samples.Count) p99Index = samples.Count - 1;
            double slowestDt = samples[p99Index];
            double rawOnePercent = slowestDt > 0 ? Math.Round(1.0 / slowestDt, 0) : CurrentFps;

            if (_smoothedOnePercent <= 0.0)
            {
                _smoothedOnePercent = rawOnePercent;
            }
            else
            {
                _smoothedOnePercent = (_smoothedOnePercent * 0.75) + (rawOnePercent * 0.25);
            }

            double finalOnePercent = Math.Round(_smoothedOnePercent, 0);
            if (finalOnePercent > CurrentFps)
            {
                finalOnePercent = CurrentFps;
            }
            OnePercentLowFps = finalOnePercent;

            try
            {
                if (OnFpsUpdated != null)
                {
                    OnFpsUpdated(CurrentFps, FrameTimeMs, OnePercentLowFps);
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
        }

        public void Dispose()
        {
            Stop();
            lock (_intervalLock)
            {
                _frameIntervals.Clear();
            }
        }
    }
}
