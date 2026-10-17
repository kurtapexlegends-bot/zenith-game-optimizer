using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services.Launcher
{
    public static class StandaloneGameDetector
    {
        public static string FindShortcut(params string[] patterns)
        {
            if (patterns == null || patterns.Length == 0) return null;

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);

            var searchDirs = new List<string>()
            {
                Path.Combine(userProfile, @"OneDrive\Desktop\Games"),
                Path.Combine(userProfile, @"OneDrive\Desktop"),
                Path.Combine(desktop, "Games"),
                desktop,
                Path.Combine(commonDesktop, "Games"),
                commonDesktop,
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Riot Games",
                @"C:\Games",
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
            };

            foreach (string dir in searchDirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                try
                {
                    string[] files = Directory.GetFiles(dir, "*.lnk", SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        string fname = Path.GetFileName(file);
                        foreach (string pat in patterns)
                        {
                            if (fname.IndexOf(pat, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return file;
                            }
                        }
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }
            return null;
        }

        public static bool DetectRoblox(out string detectedPath)
        {
            detectedPath = null;
            string sc = FindShortcut("Roblox Player", "Roblox");
            if (!string.IsNullOrEmpty(sc))
            {
                detectedPath = sc;
                return true;
            }

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\roblox-player\shell\open\command"))
                {
                    if (key != null)
                    {
                        object cmdVal = key.GetValue(null);
                        if (cmdVal != null)
                        {
                            string cmd = cmdVal.ToString();
                            int firstQuote = cmd.IndexOf('"');
                            if (firstQuote >= 0)
                            {
                                int secondQuote = cmd.IndexOf('"', firstQuote + 1);
                                if (secondQuote > firstQuote)
                                {
                                    string exe = cmd.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                                    if (File.Exists(exe))
                                    {
                                        detectedPath = exe;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            try
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string versionsDir = Path.Combine(localApp, @"Roblox\Versions");
                if (Directory.Exists(versionsDir))
                {
                    string[] exes = Directory.GetFiles(versionsDir, "RobloxPlayerBeta.exe", SearchOption.AllDirectories);
                    if (exes != null && exes.Length > 0)
                    {
                        detectedPath = exes[0];
                        return true;
                    }
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            return false;
        }

        public static bool DetectTekken(string id, out string detectedPath)
        {
            detectedPath = null;
            string sc = FindShortcut("TEKKEN 7", "Tekken 7", "TEKKEN 8", "Tekken 8", "Tekken");
            if (!string.IsNullOrEmpty(sc))
            {
                detectedPath = sc;
                return true;
            }

            string[] tekkenPaths = new string[]
            {
                @"C:\Games\TEKKEN 7\TekkenGame\Binaries\Win64\TekkenGame-Win64-Shipping.exe",
                @"C:\Games\TEKKEN 7\Tekken7.exe",
                @"D:\Games\TEKKEN 7\TekkenGame\Binaries\Win64\TekkenGame-Win64-Shipping.exe",
                @"D:\Games\TEKKEN 7\Tekken7.exe",
                @"C:\Games\TEKKEN 8\Polaris\Binaries\Win64\Polaris-Win64-Shipping.exe"
            };

            foreach (string p in tekkenPaths)
            {
                if (File.Exists(p))
                {
                    detectedPath = p;
                    return true;
                }
            }

            int sId = id.Equals("tekken8", StringComparison.OrdinalIgnoreCase) ? 1778820 : 389730;
            return SteamGameDetector.DetectSteamGame(sId, out detectedPath);
        }

        public static bool DetectAssassinCreedUnity(out string detectedPath)
        {
            detectedPath = null;
            string sc = FindShortcut("Assassin's Creed Unity", "ACU", "AC Unity");
            if (!string.IsNullOrEmpty(sc))
            {
                detectedPath = sc;
                return true;
            }

            string[] acuPaths = new string[]
            {
                @"C:\Games\Assassin's Creed Unity\ACU.exe",
                @"D:\Games\Assassin's Creed Unity\ACU.exe",
                @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games\Assassin's Creed Unity\ACU.exe",
                @"D:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games\Assassin's Creed Unity\ACU.exe"
            };

            foreach (string p in acuPaths)
            {
                if (File.Exists(p))
                {
                    detectedPath = p;
                    return true;
                }
            }

            return SteamGameDetector.DetectSteamGame(289650, out detectedPath);
        }

        public static bool DetectFortnite(out string detectedPath)
        {
            detectedPath = null;
            string fnExe = @"C:\Program Files\Epic Games\Fortnite\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe";
            if (File.Exists(fnExe))
            {
                detectedPath = fnExe;
                return true;
            }

            string epicManifests = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";
            if (Directory.Exists(epicManifests))
            {
                try
                {
                    string[] items = Directory.GetFiles(epicManifests, "*.item");
                    foreach (string item in items)
                    {
                        string text = File.ReadAllText(item);
                        if (text.IndexOf("Fortnite", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            detectedPath = item;
                            return true;
                        }
                    }
                }
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }

            return false;
        }

        public static bool DetectGenshin(out string detectedPath)
        {
            detectedPath = null;
            string[] hoyoPaths = new string[]
            {
                @"C:\Program Files\HoYoPlay\launcher.exe",
                @"C:\Program Files\Genshin Impact\launcher.exe",
                @"C:\Program Files\Genshin Impact\Genshin Impact Game\GenshinImpact.exe",
                @"C:\Program Files\Star Rail\Games\StarRail.exe"
            };

            foreach (string hp in hoyoPaths)
            {
                if (File.Exists(hp))
                {
                    detectedPath = hp;
                    return true;
                }
            }

            return false;
        }

        public static bool DetectMinecraft(out string detectedPath)
        {
            detectedPath = null;
            string mcLauncher = @"C:\Program Files (x86)\Minecraft Launcher\MinecraftLauncher.exe";
            if (File.Exists(mcLauncher))
            {
                detectedPath = mcLauncher;
                return true;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dotMc = Path.Combine(appData, ".minecraft");
            if (Directory.Exists(dotMc))
            {
                detectedPath = dotMc;
                return true;
            }

            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey("minecraft"))
                {
                    if (key != null)
                    {
                        detectedPath = "minecraft:// protocol";
                        return true;
                    }
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            return false;
        }

        public static bool DetectCustomGame(GameProfile profile, out string detectedPath)
        {
            detectedPath = null;
            if (profile == null || !profile.IsCustom) return false;

            Process active;
            if (GameLauncherService.IsGameRunning(profile, out active))
            {
                detectedPath = "Active Process: " + active.ProcessName;
                active.Dispose();
                return true;
            }

            return false;
        }
    }
}
