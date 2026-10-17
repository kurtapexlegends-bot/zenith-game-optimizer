using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services.Launcher;

namespace ZenithOptimizer.Services
{
    public static class GameLauncherService
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        public static bool BringWindowToForeground(Process process)
        {
            if (process == null) return false;
            try
            {
                IntPtr handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    ShowWindow(handle, SW_RESTORE);
                    return SetForegroundWindow(handle);
                }
            }
            catch { }
            return false;
        }

        public static bool IsGameRunning(GameProfile profile, out Process activeProcess)
        {
            activeProcess = null;
            if (profile == null || profile.ProcessNames == null) return false;

            foreach (string procName in profile.ProcessNames)
            {
                try
                {
                    Process[] procs = Process.GetProcessesByName(procName);
                    if (procs != null && procs.Length > 0)
                    {
                        activeProcess = procs[0];
                        for (int i = 1; i < procs.Length; i++)
                        {
                            try { procs[i].Dispose(); } catch { }
                        }
                        return true;
                    }
                    else if (procs != null)
                    {
                        for (int i = 0; i < procs.Length; i++)
                        {
                            try { procs[i].Dispose(); } catch { }
                        }
                    }
                }
                catch { }
            }
            return false;
        }

        public static string FindGameShortcut(params string[] patterns)
        {
            return StandaloneGameDetector.FindShortcut(patterns);
        }

        public static bool IsSteamClientInstalled()
        {
            return SteamGameDetector.IsSteamClientInstalled();
        }

        public static string ResolveExecutablePath(string detectedPath)
        {
            return IconExtractor.ResolveExecutablePath(detectedPath);
        }

        public static void ClearIconCache()
        {
            IconExtractor.ClearCache();
        }

        public static ImageSource ExtractGameIcon(string targetPath)
        {
            return IconExtractor.Extract(targetPath);
        }

        public static void ScanAllLocalInstalledGames(IEnumerable<GameProfile> profiles)
        {
            if (profiles == null) return;

            foreach (var profile in profiles)
            {
                if (profile == null) continue;
                try
                {
                    string detectedPath;
                    bool installed = IsGameInstalled(profile, out detectedPath);
                    profile.IsInstalled = installed;
                    if (installed && !string.IsNullOrEmpty(detectedPath))
                    {
                        string resolved = ResolveExecutablePath(detectedPath);
                        profile.DetectedExecutablePath = !string.IsNullOrEmpty(resolved) ? resolved : detectedPath;
                        if (profile.IconImage == null)
                        {
                            profile.IconImage = ExtractGameIcon(profile.DetectedExecutablePath);
                        }
                    }
                    else
                    {
                        profile.DetectedExecutablePath = null;
                    }
                }
                catch { }
            }
        }

        public static bool IsGameInstalled(GameProfile profile, out string detectedPath)
        {
            detectedPath = null;
            if (profile == null) return false;

            string id = (profile.Id ?? "").ToLowerInvariant();

            if (id.Contains("bluestacks")) return BlueStacksDetector.DetectBlueStacks(out detectedPath);
            if (id.Equals("valo", StringComparison.OrdinalIgnoreCase)) return RiotGameDetector.DetectValorant(out detectedPath);
            if (id.Equals("lol", StringComparison.OrdinalIgnoreCase)) return RiotGameDetector.DetectLeague(out detectedPath);
            if (id.Equals("roblox", StringComparison.OrdinalIgnoreCase)) return StandaloneGameDetector.DetectRoblox(out detectedPath);
            if (id.Equals("tekken7", StringComparison.OrdinalIgnoreCase) || id.Equals("tekken8", StringComparison.OrdinalIgnoreCase))
                return StandaloneGameDetector.DetectTekken(id, out detectedPath);
            if (id.Equals("acu", StringComparison.OrdinalIgnoreCase)) return StandaloneGameDetector.DetectAssassinCreedUnity(out detectedPath);

            int steamAppId = 0;
            if (id.Equals("cs2", StringComparison.OrdinalIgnoreCase)) steamAppId = 730;
            else if (id.Equals("apex", StringComparison.OrdinalIgnoreCase)) steamAppId = 1172470;
            else if (id.Equals("dota2", StringComparison.OrdinalIgnoreCase)) steamAppId = 570;

            if (steamAppId > 0)
            {
                string sc = FindGameShortcut(profile.Name, id);
                if (!string.IsNullOrEmpty(sc))
                {
                    detectedPath = sc;
                    return true;
                }

                if (SteamGameDetector.DetectSteamGame(steamAppId, out detectedPath)) return true;

                if (steamAppId == 1172470)
                {
                    string eaApex = @"C:\Program Files\EA Games\Apex\r5apex.exe";
                    if (File.Exists(eaApex))
                    {
                        detectedPath = eaApex;
                        return true;
                    }
                }

                return false;
            }

            if (id.Equals("fortnite", StringComparison.OrdinalIgnoreCase)) return StandaloneGameDetector.DetectFortnite(out detectedPath);
            if (id.Equals("genshin", StringComparison.OrdinalIgnoreCase)) return StandaloneGameDetector.DetectGenshin(out detectedPath);
            if (id.Equals("minecraft", StringComparison.OrdinalIgnoreCase)) return StandaloneGameDetector.DetectMinecraft(out detectedPath);
            if (profile.IsCustom) return StandaloneGameDetector.DetectCustomGame(profile, out detectedPath);

            return false;
        }

        public static bool TryLaunchGame(GameProfile profile, out string statusMessage)
        {
            statusMessage = "";
            if (profile == null)
            {
                statusMessage = "Profile is null.";
                return false;
            }

            Process runningProc;
            if (IsGameRunning(profile, out runningProc))
            {
                BringWindowToForeground(runningProc);
                statusMessage = string.Format("✓ {0} is already active! Focused window and engaged 0.500ms low-latency mode.", profile.Name);
                try { runningProc.Dispose(); } catch { }
                return true;
            }

            string detectedPath;
            bool isInstalled = IsGameInstalled(profile, out detectedPath);
            if (!isInstalled)
            {
                statusMessage = string.Format("✓ Pre-armed 0.500ms timer & High Performance mode, but {0} is not installed on this PC.", profile.Name);
                return false;
            }

            return GameProcessLauncher.LaunchGameProcess(profile, detectedPath, out statusMessage);
        }
    }
}
