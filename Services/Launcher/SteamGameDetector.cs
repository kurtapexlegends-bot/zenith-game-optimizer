using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ZenithOptimizer.Services.Launcher
{
    public static class SteamGameDetector
    {
        public static bool IsSteamClientInstalled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null && key.GetValue("SteamExe") != null) return true;
                }
                return File.Exists(@"C:\Program Files (x86)\Steam\steam.exe");
            }
            catch { return false; }
        }

        public static List<string> GetSteamLibraryPaths()
        {
            var list = new List<string>();
            try
            {
                string steamPath = null;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("SteamPath");
                        if (val != null) steamPath = val.ToString().Replace('/', '\\');
                    }
                }

                if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
                {
                    steamPath = @"C:\Program Files (x86)\Steam";
                }

                if (Directory.Exists(steamPath))
                {
                    list.Add(steamPath);
                    string vdfPath = Path.Combine(steamPath, @"steamapps\libraryfolders.vdf");
                    if (File.Exists(vdfPath))
                    {
                        string[] lines = File.ReadAllLines(vdfPath);
                        foreach (string line in lines)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                            {
                                int firstQuote = trimmed.IndexOf('"', 6);
                                if (firstQuote >= 0)
                                {
                                    int secondQuote = trimmed.IndexOf('"', firstQuote + 1);
                                    if (secondQuote > firstQuote)
                                    {
                                        string p = trimmed.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Replace(@"\\", @"\");
                                        if (Directory.Exists(p) && !list.Contains(p))
                                        {
                                            list.Add(p);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                        {
                            string p1 = Path.Combine(drive.RootDirectory.FullName, "SteamLibrary");
                            if (Directory.Exists(p1) && !list.Contains(p1)) list.Add(p1);

                            string p2 = Path.Combine(drive.RootDirectory.FullName, "Steam");
                            if (Directory.Exists(p2) && !list.Contains(p2)) list.Add(p2);

                            string p3 = Path.Combine(drive.RootDirectory.FullName, @"Program Files (x86)\Steam");
                            if (Directory.Exists(p3) && !list.Contains(p3)) list.Add(p3);
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return list;
        }

        public static bool DetectSteamGame(int steamAppId, out string detectedPath)
        {
            detectedPath = null;
            if (steamAppId <= 0) return false;

            List<string> libraryPaths = GetSteamLibraryPaths();
            foreach (string libPath in libraryPaths)
            {
                string manifest = Path.Combine(libPath, @"steamapps\appmanifest_" + steamAppId.ToString() + ".acf");
                if (File.Exists(manifest))
                {
                    string resolved = IconExtractor.ResolveExecutablePath(manifest);
                    detectedPath = !string.IsNullOrEmpty(resolved) ? resolved : manifest;
                    return true;
                }
            }

            return false;
        }
    }
}
