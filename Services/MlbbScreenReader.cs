using System;
using System.Collections.Generic;
using System.Linq;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services
{
    public class MlbbScreenReader
    {
        private readonly MlbbMetaService _metaService;
        private readonly MlbbRecommendationEngine _engine;
        private IntPtr _targetHwnd;

        public bool IsAutoScanning { get; private set; }
        public MlbbDraftState CurrentDraft { get; private set; }
        public string LastScanStatus { get; private set; }

        public event Action<MlbbDraftState> OnDraftUpdated;
        public event Action<string> OnScanStatusChanged;

        public MlbbScreenReader(MlbbMetaService metaService = null, MlbbRecommendationEngine engine = null)
        {
            _metaService = metaService ?? MlbbMetaService.Instance;
            _engine = engine ?? new MlbbRecommendationEngine(_metaService);
            _targetHwnd = IntPtr.Zero;

            IsAutoScanning = false;
            CurrentDraft = new MlbbDraftState();
            LastScanStatus = "Interactive Fast-Picker Ready";
        }

        public void SetTargetWindow(IntPtr hwnd)
        {
            _targetHwnd = hwnd;
        }

        public void StartAutoScan()
        {
            IsAutoScanning = true;
        }

        public void StopAutoScan()
        {
            IsAutoScanning = false;
        }

        public void ScanNow()
        {
            // Trigger a clean recalculation and refresh
            Recalculate();
            LastScanStatus = string.Format("Draft State Evaluated: {0} Allies, {1} Enemies, {2} Bans",
                CurrentDraft.AllyPicks.Count, CurrentDraft.EnemyPicks.Count, CurrentDraft.BannedHeroes.Count);

            if (OnScanStatusChanged != null)
            {
                OnScanStatusChanged(LastScanStatus);
            }
        }

        // Strictly Validated Draft Operations (No Duplicates Across Teams)
        public bool AddAllyPick(string heroName)
        {
            if (string.IsNullOrWhiteSpace(heroName)) return false;
            var hero = _metaService.FindHero(heroName);
            string name = hero != null ? hero.Name : heroName.Trim();

            // Cannot pick if already picked by ally, enemy, or banned
            if (CurrentDraft.AllyPicks.Contains(name) ||
                CurrentDraft.EnemyPicks.Contains(name) ||
                CurrentDraft.BannedHeroes.Contains(name) ||
                CurrentDraft.AllyPicks.Count >= 5)
            {
                return false;
            }

            CurrentDraft.AllyPicks.Add(name);
            Recalculate();
            return true;
        }

        public void RemoveAllyPick(string heroName)
        {
            if (CurrentDraft.AllyPicks.Remove(heroName))
            {
                Recalculate();
            }
        }

        public bool AddEnemyPick(string heroName)
        {
            if (string.IsNullOrWhiteSpace(heroName)) return false;
            var hero = _metaService.FindHero(heroName);
            string name = hero != null ? hero.Name : heroName.Trim();

            // Cannot pick if already picked by ally, enemy, or banned
            if (CurrentDraft.AllyPicks.Contains(name) ||
                CurrentDraft.EnemyPicks.Contains(name) ||
                CurrentDraft.BannedHeroes.Contains(name) ||
                CurrentDraft.EnemyPicks.Count >= 5)
            {
                return false;
            }

            CurrentDraft.EnemyPicks.Add(name);
            Recalculate();
            return true;
        }

        public void RemoveEnemyPick(string heroName)
        {
            if (CurrentDraft.EnemyPicks.Remove(heroName))
            {
                Recalculate();
            }
        }

        public bool AddBan(string heroName)
        {
            if (string.IsNullOrWhiteSpace(heroName)) return false;
            var hero = _metaService.FindHero(heroName);
            string name = hero != null ? hero.Name : heroName.Trim();

            if (CurrentDraft.BannedHeroes.Contains(name) ||
                CurrentDraft.AllyPicks.Contains(name) ||
                CurrentDraft.EnemyPicks.Contains(name) ||
                CurrentDraft.BannedHeroes.Count >= 10)
            {
                return false;
            }

            CurrentDraft.BannedHeroes.Add(name);
            Recalculate();
            return true;
        }

        public void RemoveBan(string heroName)
        {
            if (CurrentDraft.BannedHeroes.Remove(heroName))
            {
                Recalculate();
            }
        }

        public void SetMyRole(string role)
        {
            CurrentDraft.MyRole = role;
            Recalculate();
        }

        public void SetFirstPickTeam(string team)
        {
            CurrentDraft.FirstPickTeam = team;
            Recalculate();
        }

        public void ResetDraft()
        {
            CurrentDraft.Clear();
            Recalculate();
        }

        public void Recalculate()
        {
            _engine.EvaluateDraft(CurrentDraft);
            if (OnDraftUpdated != null)
            {
                OnDraftUpdated(CurrentDraft);
            }
        }
    }
}
