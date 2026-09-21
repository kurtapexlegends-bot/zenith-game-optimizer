using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZenithOptimizer.Services
{
    public class PowerPlanService
    {
        [DllImport("Powrprof.dll", SetLastError = true)]
        private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("Powrprof.dll", SetLastError = true)]
        private static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, ref Guid SchemeGuid);

        [DllImport("Powrprof.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint PowerReadFriendlyName(
            IntPtr RootPowerKey,
            ref Guid SchemeGuid,
            IntPtr SubGroupOfPowerSettingsGuid,
            IntPtr PowerSettingGuid,
            IntPtr Buffer,
            ref uint BufferSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        private Guid _originalSchemeGuid = Guid.Empty;

        // Standard Windows Power Scheme GUIDs
        public static readonly Guid HighPerformanceGuid = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        public static readonly Guid UltimatePerformanceGuid = new Guid("e9a42b02-d5df-448d-aa00-03f14749eb61");
        public static readonly Guid BalancedGuid = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");

        public string GetActiveSchemeName()
        {
            try
            {
                Guid current = GetActiveSchemeGuid();
                if (current != Guid.Empty)
                {
                    uint bufferSize = 0;
                    PowerReadFriendlyName(IntPtr.Zero, ref current, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref bufferSize);
                    if (bufferSize > 0)
                    {
                        IntPtr pName = Marshal.AllocHGlobal((int)bufferSize);
                        try
                        {
                            if (PowerReadFriendlyName(IntPtr.Zero, ref current, IntPtr.Zero, IntPtr.Zero, pName, ref bufferSize) == 0)
                            {
                                string name = Marshal.PtrToStringUni(pName);
                                if (!string.IsNullOrEmpty(name))
                                {
                                    return name.Trim();
                                }
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pName);
                        }
                    }
                }
            }
            catch { }

            return "High Performance";
        }

        public Guid GetActiveSchemeGuid()
        {
            try
            {
                IntPtr pGuid;
                if (PowerGetActiveScheme(IntPtr.Zero, out pGuid) == 0 && pGuid != IntPtr.Zero)
                {
                    try
                    {
                        Guid guid = (Guid)Marshal.PtrToStructure(pGuid, typeof(Guid));
                        return guid;
                    }
                    finally
                    {
                        LocalFree(pGuid);
                    }
                }
            }
            catch { }

            return BalancedGuid;
        }

        public bool SetHighPerformanceScheme()
        {
            try
            {
                if (_originalSchemeGuid == Guid.Empty)
                {
                    _originalSchemeGuid = GetActiveSchemeGuid();
                }

                Guid target = HighPerformanceGuid;
                uint result = PowerSetActiveScheme(IntPtr.Zero, ref target);
                if (result == 0) return true;

                // Fallback to command line if needed
                RunPowerCfg("/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RestoreOriginalScheme()
        {
            try
            {
                if (_originalSchemeGuid != Guid.Empty)
                {
                    Guid target = _originalSchemeGuid;
                    uint result = PowerSetActiveScheme(IntPtr.Zero, ref target);
                    _originalSchemeGuid = Guid.Empty;
                    if (result == 0) return true;

                    RunPowerCfg("/setactive " + target.ToString());
                    return true;
                }
            }
            catch { }

            return false;
        }

        private void RunPowerCfg(string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("powercfg", args);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(1000);
                }
            }
            catch { }
        }
    }
}
