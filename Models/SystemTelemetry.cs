using System;

namespace ZenithOptimizer.Models
{
    public class SystemTelemetry
    {
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

        public SystemTelemetry()
        {
            PhysicalCoreCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount / 2 : 1;
            LogicalCoreCount = Environment.ProcessorCount;
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
