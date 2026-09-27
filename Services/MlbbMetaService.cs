using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services
{
    public class MlbbMetaService
    {
        private static readonly MlbbMetaService _instance = new MlbbMetaService();

        public static MlbbMetaService Instance
        {
            get { return _instance; }
        }

        private readonly List<MlbbHero> _heroes;
        private readonly object _lock = new object();
        private string _metaFilePath;

        public event Action OnMetaUpdated;

        public string Version { get; private set; }
        public string Season { get; private set; }
        public string LastUpdated { get; private set; }

        public IReadOnlyList<MlbbHero> Heroes
        {
            get
            {
                lock (_lock)
                {
                    return _heroes.ToList();
                }
            }
        }

        public MlbbMetaService()
        {
            _heroes = new List<MlbbHero>();
            Version = "1.2.0";
            Season = "MPL S14/S15 Meta";
            LastUpdated = DateTime.Now.ToString("yyyy-MM-dd");

            LocateAndLoadMetaFile();
        }

        private void LocateAndLoadMetaFile()
        {
            string[] possiblePaths = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mlbb_meta.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Data", "mlbb_meta.json"),
                Path.Combine(Environment.CurrentDirectory, "Data", "mlbb_meta.json"),
                @"C:\SIDEPROJECTS\zenith-game-optimizer\Data\mlbb_meta.json"
            };

            foreach (var path in possiblePaths)
            {
                var full = Path.GetFullPath(path);
                if (File.Exists(full))
                {
                    _metaFilePath = full;
                    break;
                }
            }

            if (string.IsNullOrEmpty(_metaFilePath))
            {
                _metaFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mlbb_meta.json");
            }

            LoadFromDisk();
        }

        public bool LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_metaFilePath))
                    return false;

                string json = File.ReadAllText(_metaFilePath);
                return ParseJsonContent(json);
            }
            catch
            {
                return false;
            }
        }

        public bool ParseJsonContent(string json)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                var root = serializer.Deserialize<Dictionary<string, object>>(json);

                if (root == null || !root.ContainsKey("heroes"))
                    return false;

                if (root.ContainsKey("version") && root["version"] != null) Version = root["version"].ToString();
                if (root.ContainsKey("season") && root["season"] != null) Season = root["season"].ToString();
                if (root.ContainsKey("lastUpdated") && root["lastUpdated"] != null) LastUpdated = root["lastUpdated"].ToString();

                var heroListObj = root["heroes"] as System.Collections.ArrayList;
                if (heroListObj == null) return false;

                var parsedHeroes = new List<MlbbHero>();

                foreach (Dictionary<string, object> h in heroListObj)
                {
                    var hero = new MlbbHero
                    {
                        Id = (h.ContainsKey("id") && h["id"] != null) ? h["id"].ToString() : "",
                        Name = (h.ContainsKey("name") && h["name"] != null) ? h["name"].ToString() : "",
                        PrimaryRole = (h.ContainsKey("primaryRole") && h["primaryRole"] != null) ? h["primaryRole"].ToString() : "Fighter",
                        SecondaryRole = (h.ContainsKey("secondaryRole") && h["secondaryRole"] != null) ? h["secondaryRole"].ToString() : "None",
                        Lane = (h.ContainsKey("lane") && h["lane"] != null) ? h["lane"].ToString() : "Exp",
                        BaseWinRate = h.ContainsKey("baseWinRate") ? Convert.ToDouble(h["baseWinRate"]) : 50.0,
                        BanRate = h.ContainsKey("banRate") ? Convert.ToDouble(h["banRate"]) : 0.0,
                        PickRate = h.ContainsKey("pickRate") ? Convert.ToDouble(h["pickRate"]) : 0.0,
                        MplTier = (h.ContainsKey("mplTier") && h["mplTier"] != null) ? h["mplTier"].ToString() : "A",
                        MplPresence = h.ContainsKey("mplPresence") ? Convert.ToDouble(h["mplPresence"]) : 0.0
                    };

                    if (h.ContainsKey("counters"))
                    {
                        var countersList = h["counters"] as System.Collections.ArrayList;
                        if (countersList != null)
                        {
                            foreach (Dictionary<string, object> c in countersList)
                            {
                                hero.Counters.Add(new HeroCounterMatchup
                                {
                                    HeroName = (c.ContainsKey("heroName") && c["heroName"] != null) ? c["heroName"].ToString() : "",
                                    AdvantageDelta = c.ContainsKey("advantageDelta") ? Convert.ToDouble(c["advantageDelta"]) : 0.0,
                                    Reason = (c.ContainsKey("reason") && c["reason"] != null) ? c["reason"].ToString() : ""
                                });
                            }
                        }
                    }

                    if (h.ContainsKey("counteredBy"))
                    {
                        var counteredByList = h["counteredBy"] as System.Collections.ArrayList;
                        if (counteredByList != null)
                        {
                            foreach (Dictionary<string, object> cb in counteredByList)
                            {
                                hero.CounteredBy.Add(new HeroCounterMatchup
                                {
                                    HeroName = (cb.ContainsKey("heroName") && cb["heroName"] != null) ? cb["heroName"].ToString() : "",
                                    AdvantageDelta = cb.ContainsKey("advantageDelta") ? Convert.ToDouble(cb["advantageDelta"]) : 0.0,
                                    Reason = (cb.ContainsKey("reason") && cb["reason"] != null) ? cb["reason"].ToString() : ""
                                });
                            }
                        }
                    }

                    if (h.ContainsKey("synergies"))
                    {
                        var synList = h["synergies"] as System.Collections.ArrayList;
                        if (synList != null)
                        {
                            foreach (Dictionary<string, object> s in synList)
                            {
                                hero.Synergies.Add(new HeroSynergyMatchup
                                {
                                    HeroName = (s.ContainsKey("heroName") && s["heroName"] != null) ? s["heroName"].ToString() : "",
                                    SynergyScore = s.ContainsKey("synergyScore") ? Convert.ToDouble(s["synergyScore"]) : 0.0,
                                    Reason = (s.ContainsKey("reason") && s["reason"] != null) ? s["reason"].ToString() : ""
                                });
                            }
                        }
                    }

                    parsedHeroes.Add(hero);
                }

                lock (_lock)
                {
                    _heroes.Clear();
                    _heroes.AddRange(parsedHeroes);
                }

                if (OnMetaUpdated != null)
                {
                    OnMetaUpdated();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public MlbbHero FindHero(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            var q = query.Trim().ToLowerInvariant();

            lock (_lock)
            {
                // Exact match
                var exact = _heroes.FirstOrDefault(h => h.Name.ToLowerInvariant() == q || h.Id.ToLowerInvariant() == q);
                if (exact != null) return exact;

                // Starts with
                var starts = _heroes.FirstOrDefault(h => h.Name.ToLowerInvariant().StartsWith(q));
                if (starts != null) return starts;

                // Contains
                return _heroes.FirstOrDefault(h => h.Name.ToLowerInvariant().Contains(q));
            }
        }

        public List<string> GetAllHeroNames()
        {
            lock (_lock)
            {
                return _heroes.Select(h => h.Name).OrderBy(n => n).ToList();
            }
        }

        public async Task<bool> SyncMetaOnlineAsync(string remoteEndpoint = null)
        {
            try
            {
                string endpoint = string.IsNullOrEmpty(remoteEndpoint)
                    ? "https://raw.githubusercontent.com/YashjitPal/BetterGravity/main/data/mlbb_meta.json"
                    : remoteEndpoint;

                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "ZenithOptimizer/1.2 MLBB-Assistant";
                    string freshJson = await client.DownloadStringTaskAsync(new Uri(endpoint));

                    if (ParseJsonContent(freshJson))
                    {
                        var dir = Path.GetDirectoryName(_metaFilePath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.WriteAllText(_metaFilePath, freshJson);
                        return true;
                    }
                }
            }
            catch
            {
                // Graceful fallback: local meta remains intact
            }
            return false;
        }
    }
}
