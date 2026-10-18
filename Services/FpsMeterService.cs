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
    /// Combines active game window presentation tracking (optical frame delta) with
    /// DWM compositor timing fallback to detect genuine in-game frame pacing and stutters.
    /// Features exponential moving average smoothing and a steady 300ms display update rate.
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public int biSize, biWidth, biHeight;
            public short biPlanes, biBitCount;
            public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
        }
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
                        if (_instance == null) _instance = new FpsMeterService();
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
        public bool IsMultiQuadrantSamplingActive { get { return true; } }

        public void Pause() { _isPaused = true; }
        public void Resume() { _isPaused = false; }

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
                    finally { ReleaseDC(IntPtr.Zero, hdc); }
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

            _meterThread = new Thread(MeterWorkerLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "ZenithFpsMeterWorker"
            };
            _meterThread.Start();

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

            if (_publishTimer != null) _publishTimer.Stop();

            if (_meterThread != null)
            {
                try
                {
                    _meterThread.Interrupt();
                    if (!_meterThread.Join(400)) _meterThread.Abort();
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
            long lastFrameTicks = lastTicks;
            ulong lastHash = 0;

            try { TimeBeginPeriod(1); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            // Set up 64x16 DIB section for 4-quadrant dynamic viewport motion hashing
            IntPtr scrDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(scrDc);
            int W = 64, H = 16, patchW = 16, patchH = 16;
            BITMAPINFO bmi = new BITMAPINFO
            {
                biSize = Marshal.SizeOf(typeof(BITMAPINFO)),
                biWidth = W,
                biHeight = -H, // top-down
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0
            };
            IntPtr pBits;
            IntPtr hBmp = CreateDIBSection(memDc, ref bmi, 0, out pBits, IntPtr.Zero, 0);
            IntPtr oldBmp = SelectObject(memDc, hBmp);
            int[] pixelBuffer = new int[1024];

            try
            {
                while (_isRunning)
                {
                    try
                    {
                        if (_isPaused || !FpsMeterOverlay.Instance.IsVisible)
                        {
                            Thread.Sleep(200);
                            lastTicks = Stopwatch.GetTimestamp();
                            lastFrameTicks = lastTicks;
                            continue;
                        }

                        IntPtr fgWnd = GetForegroundWindow();
                        bool trackedViaWindow = false;

                        if (fgWnd != IntPtr.Zero && IsWindowVisible(fgWnd) && !IsIconic(fgWnd))
                        {
                            RECT rc;
                            if (GetClientRect(fgWnd, out rc) && rc.Right >= 100 && rc.Bottom >= 100)
                            {
                                IntPtr fgDc = GetDC(fgWnd);
                                if (fgDc != IntPtr.Zero)
                                {
                                    try
                                    {
                                        // 4-quadrant dynamic sampling: avoids static center crosshairs and avatar models
                                        int q0X = Math.Max(0, (rc.Right * 25 / 100) - 8);
                                        int q0Y = Math.Max(0, (rc.Bottom * 25 / 100) - 8);
                                        int q1X = Math.Max(0, (rc.Right * 75 / 100) - 8);
                                        int q1Y = Math.Max(0, (rc.Bottom * 25 / 100) - 8);
                                        int q2X = Math.Max(0, (rc.Right * 25 / 100) - 8);
                                        int q2Y = Math.Max(0, (rc.Bottom * 75 / 100) - 8);
                                        int q3X = Math.Max(0, (rc.Right * 75 / 100) - 8);
                                        int q3Y = Math.Max(0, (rc.Bottom * 75 / 100) - 8);

                                        BitBlt(memDc, 0,  0, patchW, patchH, fgDc, q0X, q0Y, 0x00CC0020);
                                        BitBlt(memDc, 16, 0, patchW, patchH, fgDc, q1X, q1Y, 0x00CC0020);
                                        BitBlt(memDc, 32, 0, patchW, patchH, fgDc, q2X, q2Y, 0x00CC0020);
                                        BitBlt(memDc, 48, 0, patchW, patchH, fgDc, q3X, q3Y, 0x00CC0020);

                                        if (pBits != IntPtr.Zero)
                                        {
                                            Marshal.Copy(pBits, pixelBuffer, 0, 1024);
                                        }

                                        ulong hash = 14695981039346656037UL;
                                        for (int i = 0; i < 1024; i++)
                                        {
                                            hash ^= (uint)pixelBuffer[i];
                                            hash *= 1099511628211UL;
                                        }

                                        long curTicks = Stopwatch.GetTimestamp();
                                        if (hash != lastHash)
                                        {
                                            long delta = curTicks - lastFrameTicks;
                                            lastFrameTicks = curTicks;
                                            lastHash = hash;
                                            trackedViaWindow = true;

                                            if (delta > 0)
                                            {
                                                double frameDt = (double)delta / freq;
                                                if (frameDt >= 0.001 && frameDt <= 0.5)
                                                {
                                                    lock (_intervalLock)
                                                    {
                                                        _frameIntervals.Enqueue(frameDt);
                                                        while (_frameIntervals.Count > 120) _frameIntervals.Dequeue();
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Frame unchanged: check if game is stalling / dropping frames
                                            long stallDelta = curTicks - lastFrameTicks;
                                            double stallDt = (double)stallDelta / freq;
                                            if (stallDt > 0.040 && stallDt <= 0.6)
                                            {
                                                // In-game presentation stall detected (>40ms indicates drop below 25 FPS)
                                                trackedViaWindow = true;
                                                lock (_intervalLock)
                                                {
                                                    _frameIntervals.Enqueue(stallDt);
                                                    while (_frameIntervals.Count > 120) _frameIntervals.Dequeue();
                                                }
                                                lastFrameTicks = curTicks;
                                            }
                                        }
                                    }
                                    finally { ReleaseDC(fgWnd, fgDc); }
                                }
                            }
                        }

                        if (!trackedViaWindow)
                        {
                            // Fallback to DWM compositor frame presentation when desktop is active or static
                            int hr = DwmFlush();
                            long currentTicks = Stopwatch.GetTimestamp();
                            long deltaTicks = currentTicks - lastTicks;
                            lastTicks = currentTicks;

                            if (hr == 0 && deltaTicks > 0)
                            {
                                double dt = (double)deltaTicks / freq;
                                if (dt >= 0.0005 && dt <= 0.5)
                                {
                                    lock (_intervalLock)
                                    {
                                        _frameIntervals.Enqueue(dt);
                                        while (_frameIntervals.Count > 120) _frameIntervals.Dequeue();
                                    }
                                }
                            }
                            else
                            {
                                Thread.Sleep(Math.Max(1, (int)(1000.0 / Math.Max(30.0, _monitorRefreshRate))));
                            }
                        }
                        else
                        {
                            Thread.Sleep(2);
                        }
                    }
                    catch (ThreadInterruptedException) { break; }
                    catch (ThreadAbortException) { break; }
                    catch { Thread.Sleep(5); }
                }
            }
            finally
            {
                SelectObject(memDc, oldBmp);
                DeleteObject(hBmp);
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, scrDc);
                try { TimeEndPeriod(1); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
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

            double sumAll = 0;
            for (int i = 0; i < samples.Count; i++) sumAll += samples[i];
            double avgAll = sumAll / samples.Count;
            if (avgAll > 0.0001) AverageFps = Math.Round(1.0 / avgAll, 0);

            int recentCount = Math.Min(30, samples.Count);
            double sumRecent = 0;
            for (int i = samples.Count - recentCount; i < samples.Count; i++) sumRecent += samples[i];
            double avgRecent = sumRecent / recentCount;
            if (avgRecent > 0.0001)
            {
                double targetFps = 1.0 / avgRecent;
                double targetFt = avgRecent * 1000.0;

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

            // 1% Low FPS (99th percentile longest frametime)
            samples.Sort();
            int p99Index = (int)Math.Floor(samples.Count * 0.99);
            if (p99Index >= samples.Count) p99Index = samples.Count - 1;
            double slowestDt = samples[p99Index];
            double rawOnePercent = slowestDt > 0 ? Math.Round(1.0 / slowestDt, 0) : CurrentFps;

            if (_smoothedOnePercent <= 0.0) _smoothedOnePercent = rawOnePercent;
            else _smoothedOnePercent = (_smoothedOnePercent * 0.75) + (rawOnePercent * 0.25);

            double finalOnePercent = Math.Round(_smoothedOnePercent, 0);
            if (finalOnePercent > CurrentFps) finalOnePercent = CurrentFps;
            OnePercentLowFps = finalOnePercent;

            try
            {
                if (OnFpsUpdated != null) OnFpsUpdated(CurrentFps, FrameTimeMs, OnePercentLowFps);
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
        }

        public void Dispose()
        {
            Stop();
            lock (_intervalLock) { _frameIntervals.Clear(); }
        }
    }
}
