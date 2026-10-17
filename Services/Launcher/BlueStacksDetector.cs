using System;
using System.IO;
using Microsoft.Win32;

namespace ZenithOptimizer.Services.Launcher
{
    public static class BlueStacksDetector
    {
        public static bool DetectBlueStacks(out string detectedPath)
        {
            detectedPath = null;
            string sc = StandaloneGameDetector.FindShortcut("Mobile Legends Bang Bang", "Mobile Legends", "BlueStacks 5", "BlueStacks");
            if (!string.IsNullOrEmpty(sc))
            {
                detectedPath = sc;
                return true;
            }

            string[] bsCandidates = new string[]
            {
                @"C:\Program Files\BlueStacks_nxt\HD-Player.exe",
                @"C:\Program Files (x86)\BlueStacks_nxt\HD-Player.exe",
                @"C:\Program Files\BlueStacks\HD-Player.exe"
            };

            foreach (string path in bsCandidates)
            {
                if (File.Exists(path))
                {
                    detectedPath = path;
                    return true;
                }
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\BlueStacks_nxt"))
                {
                    if (key != null)
                    {
                        object dirVal = key.GetValue("InstallDir");
                        if (dirVal != null)
                        {
                            string exe = Path.Combine(dirVal.ToString(), "HD-Player.exe");
                            if (File.Exists(exe))
                            {
                                detectedPath = exe;
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
