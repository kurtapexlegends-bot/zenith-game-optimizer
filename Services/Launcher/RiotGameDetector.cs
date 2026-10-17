using System;
using System.IO;

namespace ZenithOptimizer.Services.Launcher
{
    public static class RiotGameDetector
    {
        public static bool DetectValorant(out string detectedPath)
        {
            detectedPath = null;
            string sc = StandaloneGameDetector.FindShortcut("VALORANT");
            if (!string.IsNullOrEmpty(sc))
            {
                detectedPath = sc;
                return true;
            }

            string riotClient = @"C:\Riot Games\Riot Client\RiotClientServices.exe";
            string valoDir = @"C:\Riot Games\VALORANT";
            if (File.Exists(riotClient) && Directory.Exists(valoDir))
            {
                detectedPath = riotClient;
                return true;
            }

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
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }

            if (File.Exists(@"C:\Riot Games\VALORANT\live\VALORANT.exe"))
            {
                detectedPath = @"C:\Riot Games\VALORANT\live\VALORANT.exe";
                return true;
            }

            return false;
        }

        public static bool DetectLeague(out string detectedPath)
        {
            detectedPath = null;
            string sc = StandaloneGameDetector.FindShortcut("League of Legends");
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
                catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
            }

            return false;
        }
    }
}
