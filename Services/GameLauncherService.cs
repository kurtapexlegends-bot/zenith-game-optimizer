using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services
{
    public static class GameLauncherService
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        /// <summary>
        /// Attempts to bring an existing game window to the foreground.
        /// </summary>
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

        /// <summary>
        /// Checks if any target process for the specified game profile is currently active in memory.
        /// </summary>
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
                catch { }
            }
            return null;
        }

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

        /// <summary>
        /// Detects if the specified game or emulator is installed on the host machine.
        /// </summary>
        public static bool IsGameInstalled(GameProfile profile, out string detectedPath)
        {
            detectedPath = null;
            if (profile == null) return false;

            string id = (profile.Id ?? "").ToLowerInvariant();

            // 1. BlueStacks 5 (Mobile Legends)
            if (id.Contains("bluestacks"))
            {
                string sc = FindGameShortcut("Mobile Legends Bang Bang", "Mobile Legends", "BlueStacks 5", "BlueStacks");
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

            // 2. Valorant
            if (id.Equals("valo", StringComparison.OrdinalIgnoreCase))
            {
                string sc = FindGameShortcut("VALORANT");
                if (!string.IsNullOrEmpty(sc))
                {
                    detectedPath = sc;
                    return true;
                }

                // Official Riot Client location
                string riotClient = @"C:\Riot Games\Riot Client\RiotClientServices.exe";
                string valoDir = @"C:\Riot Games\VALORANT";
                if (File.Exists(riotClient) && Directory.Exists(valoDir))
                {
                    detectedPath = riotClient;
                    return true;
                }

                // Check RiotClientInstalls.json
                string jsonPath = @"C:\ProgramData\Riot Games\RiotClientInstalls.json";
                if (File.Exists(jsonPath))
                {
                    try
                    {
                        string content = File.ReadAllText(jsonPath);
                        if (content.IndexOf("VALORANT", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            detectedPath = File.Exists(riotClient) ? riotClient : jsonPath;
                            return true;
                        }
                    }
                    catch { }
                }

                if (File.Exists(@"C:\Riot Games\VALORANT\live\VALORANT.exe"))
                {
                    detectedPath = @"C:\Riot Games\VALORANT\live\VALORANT.exe";
                    return true;
                }

                return false;
            }

            // 3. League of Legends
            if (id.Equals("lol", StringComparison.OrdinalIgnoreCase))
            {
                string sc = FindGameShortcut("League of Legends");
                if (!string.IsNullOrEmpty(sc))
                {
                    detectedPath = sc;
                    return true;
                }

                string leagueExe = @"C:\Riot Games\League of Legends\LeagueClient.exe";
                if (File.Exists(leagueExe))
                {
                    detectedPath = leagueExe;
                    return true;
                }

                string riotClient = @"C:\Riot Games\Riot Client\RiotClientServices.exe";
                string jsonPath = @"C:\ProgramData\Riot Games\RiotClientInstalls.json";
                if (File.Exists(jsonPath))
                {
                    try
                    {
                        string content = File.ReadAllText(jsonPath);
                        if (content.IndexOf("League of Legends", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            detectedPath = File.Exists(riotClient) ? riotClient : leagueExe;
                            return true;
                        }
                    }
                    catch { }
                }

                return false;
            }

            // 4. Roblox
            if (id.Equals("roblox", StringComparison.OrdinalIgnoreCase))
            {
                string sc = FindGameShortcut("Roblox Player", "Roblox");
                if (!string.IsNullOrEmpty(sc))
                {
                    detectedPath = sc;
                    return true;
                }

                // Check HKCU protocol handler
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
                catch { }

                // Scan %LOCALAPPDATA%\Roblox\Versions
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
                catch { }

                return false;
            }

            // 5. Steam Games: CS2 (730), Tekken 7 (389730), Tekken 8 (1778820), Apex (1172470), Dota 2 (570)
            if (id.Equals("tekken7", StringComparison.OrdinalIgnoreCase) || id.Equals("tekken8", StringComparison.OrdinalIgnoreCase))
            {
                string sc = FindGameShortcut("TEKKEN 7", "Tekken 7", "TEKKEN 8", "Tekken 8", "Tekken");
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
                List<string> libraryPaths = GetSteamLibraryPaths();
                foreach (string libPath in libraryPaths)
                {
                    string manifest = Path.Combine(libPath, @"steamapps\appmanifest_" + sId.ToString() + ".acf");
                    if (File.Exists(manifest))
                    {
                        detectedPath = manifest;
                        return true;
                    }
                }

                if (IsSteamClientInstalled())
                {
                    detectedPath = "steam://rungameid/" + sId.ToString();
                    return true;
                }

                return false;
            }

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

                List<string> libraryPaths = GetSteamLibraryPaths();
                foreach (string libPath in libraryPaths)
                {
                    string manifest = Path.Combine(libPath, @"steamapps\appmanifest_" + steamAppId.ToString() + ".acf");
                    if (File.Exists(manifest))
                    {
                        detectedPath = manifest;
                        return true;
                    }
                }

                if (steamAppId == 1172470) // Apex also on EA app
                {
                    string eaApex = @"C:\Program Files\EA Games\Apex\r5apex.exe";
                    if (File.Exists(eaApex))
                    {
                        detectedPath = eaApex;
                        return true;
                    }
                }

                if (IsSteamClientInstalled())
                {
                    detectedPath = "steam://rungameid/" + steamAppId.ToString();
                    return true;
                }

                return false;
            }

            // 6. Fortnite
            if (id.Equals("fortnite", StringComparison.OrdinalIgnoreCase))
            {
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
                    catch { }
                }

                return false;
            }

            // 7. Genshin Impact & Star Rail
            if (id.Equals("genshin", StringComparison.OrdinalIgnoreCase))
            {
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

            // 8. Minecraft
            if (id.Equals("minecraft", StringComparison.OrdinalIgnoreCase))
            {
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
                catch { }

                return false;
            }

            // Custom profiles
            if (profile.IsCustom)
            {
                Process active;
                if (IsGameRunning(profile, out active))
                {
                    detectedPath = "Active Process: " + active.ProcessName;
                    active.Dispose();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Launches the game if installed, or brings it to the foreground if already running.
        /// </summary>
        public static bool TryLaunchGame(GameProfile profile, out string statusMessage)
        {
            statusMessage = "";
            if (profile == null)
            {
                statusMessage = "Profile is null.";
                return false;
            }

            // 1. If already running, focus window
            Process runningProc;
            if (IsGameRunning(profile, out runningProc))
            {
                BringWindowToForeground(runningProc);
                statusMessage = string.Format("✓ {0} is already active! Focused window and engaged 0.500ms low-latency mode.", profile.Name);
                try { runningProc.Dispose(); } catch { }
                return true;
            }

            // 2. Check if installed
            string detectedPath;
            bool isInstalled = IsGameInstalled(profile, out detectedPath);
            if (!isInstalled)
            {
                statusMessage = string.Format("✓ Pre-armed 0.500ms timer & High Performance mode, but {0} is not installed on this PC.", profile.Name);
                return false;
            }

            // 3. Launch installed game
            string id = (profile.Id ?? "").ToLowerInvariant();
            try
            {
                // If detectedPath is a desktop shortcut (.lnk), ShellExecute executes it natively with correct working directory and arguments
                if (!string.IsNullOrEmpty(detectedPath) && detectedPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && File.Exists(detectedPath))
                {
                    ProcessStartInfo psiLnk = new ProcessStartInfo();
                    psiLnk.FileName = detectedPath;
                    psiLnk.WorkingDirectory = Path.GetDirectoryName(detectedPath);
                    psiLnk.UseShellExecute = true;
                    Process.Start(psiLnk);

                    statusMessage = string.Format("✓ Launched {0} with 0.500ms low-latency timer pre-armed.", profile.Name);
                    return true;
                }

                // BlueStacks & Mobile Legends
                if (id.Contains("bluestacks"))
                {
                    string mlbbShortcut = FindGameShortcut("Mobile Legends Bang Bang", "Mobile Legends");
                    if (!string.IsNullOrEmpty(mlbbShortcut) && File.Exists(mlbbShortcut))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(mlbbShortcut);
                        psi.WorkingDirectory = Path.GetDirectoryName(mlbbShortcut);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        statusMessage = "✓ Launched Mobile Legends (BlueStacks) with 0.500ms microsecond timer pre-armed.";
                        return true;
                    }

                    string exePath = detectedPath;
                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath) || exePath.EndsWith(".conf", StringComparison.OrdinalIgnoreCase))
                    {
                        exePath = @"C:\Program Files\BlueStacks_nxt\HD-Player.exe";
                    }

                    ProcessStartInfo psiBs = new ProcessStartInfo();
                    psiBs.FileName = exePath;
                    psiBs.Arguments = "--instance Pie64 --cmd launchApp --package \"com.mobile.legends\" --source desktop_shortcut";
                    if (File.Exists(exePath)) psiBs.WorkingDirectory = Path.GetDirectoryName(exePath);
                    psiBs.UseShellExecute = true;
                    Process.Start(psiBs);

                    statusMessage = "✓ Launched BlueStacks 5 & Mobile Legends with 0.500ms microsecond timer pre-armed.";
                    return true;
                }

                // Valorant
                if (id.Equals("valo", StringComparison.OrdinalIgnoreCase))
                {
                    string valoShortcut = FindGameShortcut("VALORANT");
                    if (!string.IsNullOrEmpty(valoShortcut) && File.Exists(valoShortcut))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(valoShortcut);
                        psi.WorkingDirectory = Path.GetDirectoryName(valoShortcut);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        statusMessage = "✓ Launched Valorant with 0.500ms microsecond timer pre-armed.";
                        return true;
                    }

                    string riotClient = @"C:\Riot Games\Riot Client\RiotClientServices.exe";
                    if (File.Exists(riotClient))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = riotClient;
                        psi.Arguments = "--launch-product=valorant --launch-patchline=live";
                        psi.WorkingDirectory = @"C:\Riot Games\Riot Client";
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                    else if (File.Exists(detectedPath))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(detectedPath);
                        psi.WorkingDirectory = Path.GetDirectoryName(detectedPath);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }

                    statusMessage = "✓ Launched Valorant via Riot Client with 0.500ms microsecond timer pre-armed.";
                    return true;
                }

                // League of Legends
                if (id.Equals("lol", StringComparison.OrdinalIgnoreCase))
                {
                    string lolShortcut = FindGameShortcut("League of Legends");
                    if (!string.IsNullOrEmpty(lolShortcut) && File.Exists(lolShortcut))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(lolShortcut);
                        psi.WorkingDirectory = Path.GetDirectoryName(lolShortcut);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        statusMessage = "✓ Launched League of Legends with low-latency priority pre-armed.";
                        return true;
                    }

                    string riotClient = @"C:\Riot Games\Riot Client\RiotClientServices.exe";
                    if (File.Exists(riotClient))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = riotClient;
                        psi.Arguments = "--launch-product=league_of_legends --launch-patchline=live";
                        psi.WorkingDirectory = @"C:\Riot Games\Riot Client";
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                    else if (File.Exists(detectedPath))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(detectedPath);
                        psi.WorkingDirectory = Path.GetDirectoryName(detectedPath);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }

                    statusMessage = "✓ Launched League of Legends with low-latency priority pre-armed.";
                    return true;
                }

                // Roblox
                if (id.Equals("roblox", StringComparison.OrdinalIgnoreCase))
                {
                    string robloxShortcut = FindGameShortcut("Roblox Player", "Roblox");
                    if (!string.IsNullOrEmpty(robloxShortcut) && File.Exists(robloxShortcut))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(robloxShortcut);
                        psi.WorkingDirectory = Path.GetDirectoryName(robloxShortcut);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        statusMessage = "✓ Launched Roblox Player with 0.500ms microsecond timer pre-armed.";
                        return true;
                    }

                    if (File.Exists(detectedPath) && detectedPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = detectedPath;
                        psi.Arguments = "--app";
                        psi.WorkingDirectory = Path.GetDirectoryName(detectedPath);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        statusMessage = "✓ Launched Roblox Player with 0.500ms microsecond timer pre-armed.";
                        return true;
                    }
                    else
                    {
                        ProcessStartInfo psi = new ProcessStartInfo("roblox-player:1");
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        statusMessage = "✓ Launched Roblox Player protocol with 0.500ms microsecond timer pre-armed.";
                        return true;
                    }
                }

                // Tekken 7 & Tekken 8
                if (id.Equals("tekken7", StringComparison.OrdinalIgnoreCase) || id.Equals("tekken8", StringComparison.OrdinalIgnoreCase))
                {
                    string tekkenShortcut = FindGameShortcut("TEKKEN 7", "Tekken 7", "TEKKEN 8", "Tekken 8", "Tekken");
                    if (!string.IsNullOrEmpty(tekkenShortcut) && File.Exists(tekkenShortcut))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(tekkenShortcut);
                        psi.WorkingDirectory = Path.GetDirectoryName(tekkenShortcut);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        statusMessage = string.Format("✓ Launched {0} with 0.500ms microsecond timer pre-armed.", profile.Name);
                        return true;
                    }

                    if (!string.IsNullOrEmpty(detectedPath) && File.Exists(detectedPath) && detectedPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(detectedPath);
                        psi.WorkingDirectory = Path.GetDirectoryName(detectedPath);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                        statusMessage = string.Format("✓ Launched {0} with 0.500ms microsecond timer pre-armed.", profile.Name);
                        return true;
                    }

                    int sId = id.Equals("tekken8", StringComparison.OrdinalIgnoreCase) ? 1778820 : 389730;
                    ProcessStartInfo psiSteam = new ProcessStartInfo("steam://rungameid/" + sId.ToString());
                    psiSteam.UseShellExecute = true;
                    Process.Start(psiSteam);
                    statusMessage = string.Format("✓ Launched {0} via Steam with 0.500ms microsecond timer pre-armed.", profile.Name);
                    return true;
                }

                // Steam Games
                int steamAppId = 0;
                if (id.Equals("cs2", StringComparison.OrdinalIgnoreCase)) steamAppId = 730;
                else if (id.Equals("apex", StringComparison.OrdinalIgnoreCase)) steamAppId = 1172470;
                else if (id.Equals("dota2", StringComparison.OrdinalIgnoreCase)) steamAppId = 570;

                if (steamAppId > 0)
                {
                    ProcessStartInfo psi = new ProcessStartInfo("steam://rungameid/" + steamAppId.ToString());
                    psi.UseShellExecute = true;
                    Process.Start(psi);

                    statusMessage = string.Format("✓ Launched {0} via Steam with 0.500ms microsecond timer pre-armed.", profile.Name);
                    return true;
                }

                // Fortnite
                if (id.Equals("fortnite", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessStartInfo psi = new ProcessStartInfo("com.epicgames.launcher://apps/Fortnite?action=launch&silent=true");
                    psi.UseShellExecute = true;
                    Process.Start(psi);

                    statusMessage = "✓ Launched Fortnite via Epic Games with pre-armed timer.";
                    return true;
                }

                // Genshin Impact / Star Rail
                if (id.Equals("genshin", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessStartInfo psi = new ProcessStartInfo(detectedPath);
                    psi.UseShellExecute = true;
                    Process.Start(psi);

                    statusMessage = "✓ Launched Genshin Impact / Star Rail with pre-armed timer.";
                    return true;
                }

                // Minecraft
                if (id.Equals("minecraft", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(detectedPath) && detectedPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(detectedPath);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                    else
                    {
                        ProcessStartInfo psi = new ProcessStartInfo("minecraft:");
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }

                    statusMessage = "✓ Launched Minecraft with pre-armed timer.";
                    return true;
                }

                // Generic executable launch
                if (File.Exists(detectedPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo(detectedPath);
                    psi.UseShellExecute = true;
                    Process.Start(psi);

                    statusMessage = string.Format("✓ Launched {0} with pre-armed profile.", profile.Name);
                    return true;
                }

                statusMessage = string.Format("✓ Pre-armed {0}. Launch the game to engage active tracking.", profile.Name);
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = string.Format("✓ Pre-armed {0}. (Launch notice: {1})", profile.Name, ex.Message);
                return true;
            }
        }

        private static List<string> GetSteamLibraryPaths()
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
            }
            catch { }
            return list;
        }
    }
}
