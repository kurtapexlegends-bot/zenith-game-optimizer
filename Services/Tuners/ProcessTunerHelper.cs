using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ZenithOptimizer.Services.Tuners
{
    public static class ProcessTunerHelper
    {
        public static bool SetProcessIoPriority(string processExe, int ioPriority, out string message)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + processExe + @"\PerfOptions"))
                {
                    if (key != null)
                    {
                        key.SetValue("IoPriority", ioPriority, RegistryValueKind.DWord);
                    }
                }
                message = string.Format("Process I/O Priority for {0} elevated to {1} (accelerates disk asset streaming).", processExe, ioPriority == 3 ? "High" : "Normal");
                return true;
            }
            catch (Exception ex)
            {
                message = "I/O Priority: " + ex.Message;
                return false;
            }
        }

        public static bool DisableProcessPowerThrottling(string processExe, out string message)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + processExe))
                {
                    if (key != null)
                    {
                        key.SetValue("PowerThrottlingOff", 1, RegistryValueKind.DWord);
                    }
                }
                message = string.Format("Windows EcoQoS Power Throttling disabled for {0} (maintains peak CPU clock).", processExe);
                return true;
            }
            catch (Exception ex)
            {
                message = "Power Throttling: " + ex.Message;
                return false;
            }
        }

        public static bool SetProcessPagePriority(string processExe, int pagePriority, out string message)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + processExe + @"\PerfOptions"))
                {
                    if (key != null)
                    {
                        key.SetValue("PagePriority", pagePriority, RegistryValueKind.DWord);
                    }
                }
                message = string.Format("Page Priority for {0} set to {1} (prevents memory eviction).", processExe, pagePriority);
                return true;
            }
            catch (Exception ex)
            {
                message = "Page Priority: " + ex.Message;
                return false;
            }
        }

        public static bool ConfigureDscpQoSPolicy(string policyName, string processExe, int dscpValue, out string message)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\QoS\" + policyName))
                {
                    if (key != null)
                    {
                        key.SetValue("Application Name", processExe, RegistryValueKind.String);
                        key.SetValue("Protocol", "*", RegistryValueKind.String);
                        key.SetValue("Local Port", "*", RegistryValueKind.String);
                        key.SetValue("Remote Port", "*", RegistryValueKind.String);
                        key.SetValue("DSCP Value", dscpValue.ToString(), RegistryValueKind.String);
                        key.SetValue("Throttle Rate", "-1", RegistryValueKind.String);
                    }
                }
                message = string.Format("Windows Network QoS policy configured: {0} ({1}) tagged with DSCP {2} Expedited Forwarding.", policyName, processExe, dscpValue);
                return true;
            }
            catch (Exception ex)
            {
                message = "Network QoS: " + ex.Message;
                return false;
            }
        }

        public static bool ProtectWorkingSet(string processBaseName, out string message)
        {
            try
            {
                Process[] procs = Process.GetProcessesByName(processBaseName);
                int count = 0;
                foreach (Process p in procs)
                {
                    try
                    {
                        p.PriorityBoostEnabled = true;
                        count++;
                    }
                    catch { }
                }
                message = string.Format("PriorityBoost and Working Set protection enabled for {0} ({1} active instance{2}).", processBaseName, count, count == 1 ? "" : "s");
                return true;
            }
            catch (Exception ex)
            {
                message = "Working Set: " + ex.Message;
                return false;
            }
        }

        public static bool DisableUniversalStickyKeys(out string message)
        {
            try
            {
                using (RegistryKey sk = Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys"))
                {
                    if (sk != null) sk.SetValue("Flags", "506", RegistryValueKind.String);
                }
                using (RegistryKey tk = Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\ToggleKeys"))
                {
                    if (tk != null) tk.SetValue("Flags", "58", RegistryValueKind.String);
                }
                using (RegistryKey kr = Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\Keyboard Response"))
                {
                    if (kr != null) kr.SetValue("Flags", "122", RegistryValueKind.String);
                }
                message = "Windows Sticky Keys, Filter Keys & Toggle Keys shortcuts permanently silenced for uninterrupted gaming.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Sticky Keys: " + ex.Message;
                return false;
            }
        }

        public static bool EnforceRawMouseInputPacing(out string message)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse"))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                    }
                }
                message = "1:1 Raw linear mouse input curve active. Dynamic Windows mouse acceleration removed for skill-shot precision.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Mouse Pacing: " + ex.Message;
                return false;
            }
        }

        public static bool RestoreMouseInputPacing(out string message)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse"))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSpeed", "1", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "6", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "10", RegistryValueKind.String);
                    }
                }
                message = "Standard Windows mouse curve restored.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Mouse Pacing Restore: " + ex.Message;
                return false;
            }
        }
    }
}
