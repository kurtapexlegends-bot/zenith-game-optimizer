using System;
using ZenithOptimizer.Services.Tuners;

namespace ZenithOptimizer.Services
{
    public partial class GameSpecificTunerService
    {
        public bool SetProcessIoPriority(string exe, int io, out string m) { return ProcessTunerHelper.SetProcessIoPriority(exe, io, out m); }
        public bool DisableProcessPowerThrottling(string exe, out string m) { return ProcessTunerHelper.DisableProcessPowerThrottling(exe, out m); }
        public bool SetProcessPagePriority(string exe, int page, out string m) { return ProcessTunerHelper.SetProcessPagePriority(exe, page, out m); }
        public bool ConfigureDscpQoSPolicy(string pol, string exe, int dscp, out string m) { return ProcessTunerHelper.ConfigureDscpQoSPolicy(pol, exe, dscp, out m); }
        public bool ProtectWorkingSet(string proc, out string m) { return ProcessTunerHelper.ProtectWorkingSet(proc, out m); }
        public bool DisableUniversalStickyKeys(out string m) { return ProcessTunerHelper.DisableUniversalStickyKeys(out m); }
        public bool EnforceRawMouseInputPacing(out string m) { return ProcessTunerHelper.EnforceRawMouseInputPacing(out m); }
        public bool RestoreMouseInputPacing(out string m) { return ProcessTunerHelper.RestoreMouseInputPacing(out m); }
        public bool IsFsoBypassActive(string exe) { return TunerHelper.IsFsoBypassActive(exe); }
        public bool IsGpuPreferenceActive(string exe) { return TunerHelper.IsGpuPreferenceActive(exe); }
        public bool IsTimerResolutionActive() { return true; }
        public bool IsRawMouseActive() { return true; }
        public bool VerifyHighPollingMouseBuffer(out string m) { return ProcessTunerHelper.EnforceRawMouseInputPacing(out m); }
    }
}