using System;
using System.Management;

namespace ZenithOptimizer.Models
{
    public class SystemTelemetry
    {
        private static int _cachedPhysicalCores = 0;
        private static int _cachedLogicalCores = 0;
        private static readonly object _coreLock = new object();

        public double CpuUsagePercent { get; set; }
        public int PhysicalCoreCount { get; set; }
        public int LogicalCoreCount { get; set; }
        public string CpuName { get; set; }
        public string GpuName { get; set; }
        public double TotalRamGb { get; set; }
        public double UsedRamGb { get; set; }
        public double FreeRamGb { get; set; }
        public double RamUsagePercent { get; set; }
        public double CurrentTimerResolutionMs { get; set; }
        public string ActivePowerPlanName { get; set; }
        public string ActiveGameName { get; set; }
        public bool IsGameRunning { get; set; }
        public bool IsHagsActive { get; set; }
        public bool IsGameModeActive { get; set; }
        public bool IsGameDvrDisabled { get; set; }

        public static void GetProcessorTopology(out int physicalCores, out int logicalCores)
        {
            if (_cachedPhysicalCores > 0 && _cachedLogicalCores > 0)
            {
                physicalCores = _cachedPhysicalCores;
                logicalCores = _cachedLogicalCores;
                return;
            }

            lock (_coreLock)
            {
                if (_cachedPhysicalCores > 0 && _cachedLogicalCores > 0)
                {
                    physicalCores = _cachedPhysicalCores;
                    logicalCores = _cachedLogicalCores;
                    return;
                }

                int phys = 0;
                int logi = 0;

                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            object nc = obj["NumberOfCores"];
                            object nlp = obj["NumberOfLogicalProcessors"];
                            if (nc != null) phys += Convert.ToInt32(nc);
                            if (nlp != null) logi += Convert.ToInt32(nlp);
                        }
                    }
                }
                catch { }

                if (phys <= 0)
                {
                    phys = Environment.ProcessorCount > 1 ? Environment.ProcessorCount / 2 : 1;
                }
                if (logi <= 0)
                {
                    logi = Environment.ProcessorCount;
                }

                _cachedPhysicalCores = phys;
                _cachedLogicalCores = logi;

                physicalCores = phys;
                logicalCores = logi;
            }
        }

        public SystemTelemetry()
        {
            int phys, logi;
            GetProcessorTopology(out phys, out logi);
            PhysicalCoreCount = phys;
            LogicalCoreCount = logi;
            CpuName = "Intel Processor";
            GpuName = "Graphics Adapter";
            CurrentTimerResolutionMs = 15.625;
            ActivePowerPlanName = "Balanced";
            ActiveGameName = "None (Monitoring Active)";
            IsGameRunning = false;
            IsHagsActive = false;
            IsGameModeActive = true;
            IsGameDvrDisabled = true;
        }
    }
}
