using System;
using System.Runtime.InteropServices;

namespace ZenithOptimizer.Services
{
    public class TimerResolutionEngine : IDisposable
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryTimerResolution(out uint minResolution, out uint maxResolution, out uint currentResolution);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetTimerResolution(uint desiredResolution, bool setResolution, out uint currentResolution);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        private bool _isHighResolutionActive = false;

        public bool IsHighResolutionActive
        {
            get { return _isHighResolutionActive; }
        }

        public double GetCurrentResolutionMs()
        {
            try
            {
                uint minRes, maxRes, currRes;
                int status = NtQueryTimerResolution(out minRes, out maxRes, out currRes);
                if (status == 0 && currRes > 0)
                {
                    return Math.Round(currRes / 10000.0, 3);
                }
            }
            catch
            {
            }
            return _isHighResolutionActive ? 0.500 : 15.625;
        }

        public bool EnableHighResolution()
        {
            try
            {
                if (_isHighResolutionActive) return true;

                // Request 1ms via winmm
                TimeBeginPeriod(1);

                // Request 0.5ms (5000 in 100ns units) via NtSetTimerResolution
                uint currentRes;
                int status = NtSetTimerResolution(5000, true, out currentRes);
                _isHighResolutionActive = (status == 0 || currentRes <= 10000);
                return _isHighResolutionActive;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RestoreResolution()
        {
            try
            {
                if (!_isHighResolutionActive) return true;

                TimeEndPeriod(1);
                uint currentRes;
                NtSetTimerResolution(5000, false, out currentRes);
                _isHighResolutionActive = false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            RestoreResolution();
        }
    }
}
