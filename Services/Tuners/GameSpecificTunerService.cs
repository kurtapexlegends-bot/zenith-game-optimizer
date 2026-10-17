using System;
using System.Collections.Generic;
using System.IO;
using ZenithOptimizer.Services.Tuners;

namespace ZenithOptimizer.Services
{
    public partial class GameSpecificTunerService
    {
        public static void ProtectUserKeymaps()
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string userCfgDir = Path.Combine(programData, @"BlueStacks_nxt\Engine\UserData\InputMapper\UserFiles");
                string userCfg = Path.Combine(userCfgDir, "com.mobile.legends.cfg");
                string backupDir = Path.Combine(programData, @"Zenith\KeymapBackups");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
                string backupCfg = Path.Combine(backupDir, "com.mobile.legends.cfg");

                if (File.Exists(userCfg) && new FileInfo(userCfg).Length > 1000) File.Copy(userCfg, backupCfg, true);
                else if (!File.Exists(userCfg) && File.Exists(backupCfg))
                {
                    if (!Directory.Exists(userCfgDir)) Directory.CreateDirectory(userCfgDir);
                    File.Copy(backupCfg, userCfg, true);
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
        }

        public static bool FlushSystemDns(out string message) { return TunerHelper.FlushSystemDns(out message); }
        public static bool SetWindowsGpuPreference(string exe, int pref, out string msg) { return TunerHelper.SetWindowsGpuPreference(exe, pref, out msg); }
        public static bool SetWindowsFullscreenOptimizationBypass(string exe, out string msg) { return TunerHelper.SetWindowsFullscreenOptimizationBypass(exe, out msg); }
        public static bool PinProcessToPhysicalPerformanceCores(string proc, out string msg) { return TunerHelper.PinProcessToPhysicalPerformanceCores(proc, out msg); }
        public static bool IsolateAudioDgThread(out string message) { return TunerHelper.IsolateAudioDgThread(out message); }
        public static string UpdateJsonSetting(string content, string key, object value) { return TunerHelper.UpdateJsonSetting(content, key, value); }
        public static string SerializeFormattedJson(Dictionary<string, object> dict) { return TunerHelper.SerializeFormattedJson(dict); }
        public static string RegexReplaceOrInsert(string content, string key, string value) { return TunerHelper.RegexReplaceOrInsert(content, key, value); }
        private static void BackupConfig(string path) { TunerHelper.BackupConfig(path); }
        private static string ReplaceOrInsert(string c, string s, string k, string v) { return TunerHelper.ReplaceOrInsert(c, s, k, v); }
        private static long CleanFolderFiles(string f) { return TunerHelper.CleanFolderFiles(f); }

        private readonly RobloxTuner _roblox;
        private readonly BlueStacksTuner _blueStacks;
        private readonly LeagueTuner _league;
        private readonly ValorantTuner _valorant;
        private readonly TekkenTuner _tekken;
        private readonly OtherGamesTuner _otherGames;

        public RobloxTuner Roblox { get { return _roblox; } }
        public BlueStacksTuner BlueStacks { get { return _blueStacks; } }
        public LeagueTuner League { get { return _league; } }
        public ValorantTuner Valorant { get { return _valorant; } }
        public TekkenTuner Tekken { get { return _tekken; } }
        public OtherGamesTuner OtherGames { get { return _otherGames; } }

        public GameSpecificTunerService() : this(new BlueStacksOptimizer(), new TimerResolutionEngine(), new SystemTweaksService(), new MemoryPurgeService(), new NetworkOptimizer()) { }

        public GameSpecificTunerService(BlueStacksOptimizer bs, TimerResolutionEngine t = null, SystemTweaksService s = null, MemoryPurgeService m = null, NetworkOptimizer n = null)
        {
            _roblox = new RobloxTuner();
            _blueStacks = new BlueStacksTuner(bs);
            _league = new LeagueTuner();
            _valorant = new ValorantTuner();
            _tekken = new TekkenTuner();
            _otherGames = new OtherGamesTuner();
        }

        public IGameTuner GetTuner(string gameId)
        {
            if (string.IsNullOrEmpty(gameId)) return null;
            string id = gameId.ToLowerInvariant();
            if (id == "roblox") return _roblox;
            if (id == "bluestacks" || id == "mlbb") return _blueStacks;
            if (id == "league" || id == "lol") return _league;
            if (id == "valorant") return _valorant;
            if (id == "tekken" || id == "tekken7" || id == "tekken8") return _tekken;
            return _otherGames;
        }
    }
}
