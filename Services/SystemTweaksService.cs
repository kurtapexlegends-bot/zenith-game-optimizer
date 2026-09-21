using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ZenithOptimizer.Services
{
    public class SystemTweaksService
    {
        private const string GameConfigStoreKey = @"System\GameConfigStore";
        private const string GameDvrKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";
        private const string MmcssGamesKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
        private const string GameBarKey = @"Software\Microsoft\GameBar";
        private const string CpuRegistryKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

        private string _cachedCpuName = null;
        private string _cachedGpuName = null;
        private bool? _cachedGameDvrDisabled = null;
        private bool? _cachedGameModeEnabled = null;
        private string _cachedHagsStatus = null;

        public void InvalidateCache()
        {
            _cachedGameDvrDisabled = null;
            _cachedGameModeEnabled = null;
            _cachedHagsStatus = null;
        }

        public string GetCpuName()
        {
            if (_cachedCpuName != null) return _cachedCpuName;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(CpuRegistryKey))
                {
                    if (key != null)
                    {
                        object name = key.GetValue("ProcessorNameString");
                        if (name != null && !string.IsNullOrEmpty(name.ToString()))
                        {
                            _cachedCpuName = name.ToString().Trim();
                            return _cachedCpuName;
                        }
                    }
                }
            }
            catch { }

            _cachedCpuName = "Intel(R) Core(TM) Processor";
            return _cachedCpuName;
        }

        public string GetGpuName()
        {
            if (_cachedGpuName != null) return _cachedGpuName;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"))
                {
                    if (key != null)
                    {
                        object desc = key.GetValue("DriverDesc");
                        if (desc != null && !string.IsNullOrEmpty(desc.ToString()))
                        {
                            _cachedGpuName = desc.ToString().Trim();
                            return _cachedGpuName;
                        }
                    }
                }
            }
            catch { }

            _cachedGpuName = "Intel(R) Graphics";
            return _cachedGpuName;
        }

        public bool IsGameDvrDisabled()
        {
            if (_cachedGameDvrDisabled.HasValue) return _cachedGameDvrDisabled.Value;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(GameConfigStoreKey))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("GameDVR_Enabled");
                        if (val is int && (int)val == 0)
                        {
                            _cachedGameDvrDisabled = true;
                            return true;
                        }
                    }
                }
            }
            catch { }
            _cachedGameDvrDisabled = false;
            return false;
        }

        public bool SetGameDvrDisabled(bool disable, out string message)
        {
            try
            {
                int targetVal = disable ? 0 : 1;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(GameConfigStoreKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue("GameDVR_Enabled", targetVal, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(GameDvrKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue("AppCaptureEnabled", targetVal, RegistryValueKind.DWord);
                    }
                }

                _cachedGameDvrDisabled = disable;
                message = disable 
                    ? "Windows GameDVR background recording disabled (saves GPU encoder cycles)."
                    : "Windows GameDVR restored to standard defaults.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to update GameDVR settings: " + ex.Message;
                return false;
            }
        }

        public bool IsWindowsGameModeEnabled()
        {
            if (_cachedGameModeEnabled.HasValue) return _cachedGameModeEnabled.Value;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(GameBarKey))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AutoGameModeEnabled");
                        if (val is int && (int)val == 1)
                        {
                            _cachedGameModeEnabled = true;
                            return true;
                        }
                    }
                }
            }
            catch { }
            _cachedGameModeEnabled = true;
            return true; // Windows default is enabled
        }

        public bool SetWindowsGameMode(bool enable, out string message)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(GameBarKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue("AutoGameModeEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
                        _cachedGameModeEnabled = enable;
                        message = enable ? "Windows Game Mode priority enabled." : "Windows Game Mode disabled.";
                        return true;
                    }
                }
                message = "Unable to access GameBar registry key.";
                return false;
            }
            catch (Exception ex)
            {
                message = "Failed to update Game Mode: " + ex.Message;
                return false;
            }
        }

        public bool IsMmcssOptimized()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(MmcssGamesKey))
                {
                    if (key != null)
                    {
                        object sched = key.GetValue("Scheduling Category");
                        if (sched != null && sched.ToString().Equals("High", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public bool SetMmcssGamingPriority(bool enable, out string message)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(MmcssGamesKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            key.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                            key.SetValue("Priority", 6, RegistryValueKind.DWord);
                            key.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                            key.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                            message = "DirectX & GPU queue priority elevated to High (MMCSS Gaming).";
                        }
                        else
                        {
                            key.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                            key.SetValue("Priority", 2, RegistryValueKind.DWord);
                            key.SetValue("Scheduling Category", "Medium", RegistryValueKind.String);
                            key.SetValue("SFIO Priority", "Normal", RegistryValueKind.String);
                            message = "MMCSS Gaming priority restored to standard defaults.";
                        }
                        return true;
                    }
                }
                message = "Administrator privileges required to update system multimedia profile.";
                return false;
            }
            catch (Exception ex)
            {
                message = "Failed to update GPU scheduling priority: " + ex.Message;
                return false;
            }
        }

        public void StabilizeAudioEngine()
        {
            try
            {
                // audiodg.exe is the Windows Audio Device Graph.
                // In teamfights with multiple sound effects, audio buffer underruns cause frame stutters.
                Process[] audioProcs = Process.GetProcessesByName("audiodg");
                if (audioProcs != null)
                {
                    foreach (Process p in audioProcs)
                    {
                        try
                        {
                            p.PriorityBoostEnabled = true;
                            if (p.PriorityClass == ProcessPriorityClass.Normal)
                            {
                                p.PriorityClass = ProcessPriorityClass.AboveNormal;
                            }
                        }
                        catch { }
                        finally
                        {
                            p.Dispose();
                        }
                    }
                }
            }
            catch { }
        }

        public string GetHagsStatus()
        {
            if (_cachedHagsStatus != null) return _cachedHagsStatus;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("HwSchMode");
                        if (val is int && (int)val == 2)
                        {
                            _cachedHagsStatus = "Active (Hardware-Accelerated GPU Scheduling)";
                            return _cachedHagsStatus;
                        }
                    }
                }
            }
            catch { }
            _cachedHagsStatus = "Standard / Balanced";
            return _cachedHagsStatus;
        }
    }
}
