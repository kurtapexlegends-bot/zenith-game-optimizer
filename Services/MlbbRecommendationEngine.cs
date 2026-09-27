using System;
using System.Collections.Generic;
using System.Linq;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.Services
{
    public class MlbbRecommendationEngine
    {
        private readonly MlbbMetaService _metaService;

        public MlbbRecommendationEngine(MlbbMetaService metaService = null)
        {
            _metaService = metaService ?? MlbbMetaService.Instance;
        }

        public void EvaluateDraft(MlbbDraftState draft)
        {
            if (draft == null) return;

            var allHeroes = _metaService.Heroes;
            if (allHeroes == null || allHeroes.Count == 0) return;

            // 1. Resolve ally and enemy hero models
            var allyHeroObjects = draft.AllyPicks
                .Select(p => _metaService.FindHero(p))
                .Where(h => h != null)
                .ToList();

            var enemyHeroObjects = draft.EnemyPicks
                .Select(p => _metaService.FindHero(p))
                .Where(h => h != null)
                .ToList();

            var bannedNamesLower = new HashSet<string>(
                draft.BannedHeroes.Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.Trim().ToLowerInvariant())
            );

            var pickedNamesLower = new HashSet<string>(
                draft.AllyPicks.Concat(draft.EnemyPicks).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim().ToLowerInvariant())
            );

            // 2. Analyze Missing Roles and Lanes in Ally Composition
            draft.MissingAllyRoles.Clear();

            bool hasRoam = allyHeroObjects.Any(h => h.Lane == "Roam" || h.PrimaryRole == "Tank" || h.PrimaryRole == "Support");
            bool hasJungle = allyHeroObjects.Any(h => h.Lane == "Jungle" || h.PrimaryRole == "Assassin");
            bool hasGold = allyHeroObjects.Any(h => h.Lane == "Gold" || h.PrimaryRole == "Marksman");
            bool hasMid = allyHeroObjects.Any(h => h.Lane == "Mid" || h.PrimaryRole == "Mage");
            bool hasExp = allyHeroObjects.Any(h => h.Lane == "Exp" || h.PrimaryRole == "Fighter");

            if (!hasRoam) draft.MissingAllyRoles.Add("Roamer / Tank");
            if (!hasJungle) draft.MissingAllyRoles.Add("Jungler");
            if (!hasMid) draft.MissingAllyRoles.Add("Mid / Mage");
            if (!hasGold) draft.MissingAllyRoles.Add("Gold / Marksman");
            if (!hasExp) draft.MissingAllyRoles.Add("Exp / Fighter");

            // Check damage distribution (Magic vs Physical)
            bool hasMagicDamage = allyHeroObjects.Any(h => h.PrimaryRole == "Mage" || h.SecondaryRole == "Mage" || h.Name == "Harith" || h.Name == "Esmeralda" || h.Name == "Kadita" || h.Name == "Zhuxin");
            if (allyHeroObjects.Count >= 3 && !hasMagicDamage)
            {
                draft.MissingAllyRoles.Add("Magic Damage Source");
            }

            // 3. Identify Enemy Key Threats and calculate 5v5 Win Probability
            draft.EnemyKeyThreats.Clear();
            foreach (var enemy in enemyHeroObjects)
            {
                if (enemy.MplTier == "S+" || enemy.BanRate > 50.0 || enemy.PrimaryRole == "Assassin")
                {
                    draft.EnemyKeyThreats.Add(string.Format("{0} ({1} Threat)", enemy.Name, enemy.PrimaryRole));
                }
            }

            double allyNetAdvantage = 0.0;
            foreach (var ally in allyHeroObjects)
            {
                foreach (var enemy in enemyHeroObjects)
                {
                    var directC = ally.Counters.FirstOrDefault(c => c.HeroName.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase));
                    if (directC != null) allyNetAdvantage += directC.AdvantageDelta;

                    var getC = ally.CounteredBy.FirstOrDefault(c => c.HeroName.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase));
                    if (getC != null) allyNetAdvantage += getC.AdvantageDelta;
                }
            }

            double baseChance = 50.0 + (allyNetAdvantage * 1.2);
            draft.AllyWinChance = Math.Round(Math.Max(15.0, Math.Min(85.0, baseChance)), 1);
            draft.EnemyWinChance = Math.Round(100.0 - draft.AllyWinChance, 1);

            // 4. Calculate Scores for candidate heroes
            var candidateRecommendations = new List<HeroRecommendation>();

            foreach (var candidate in allHeroes)
            {
                var nameLower = candidate.Name.ToLowerInvariant();
                // Exclude already picked or banned heroes
                if (bannedNamesLower.Contains(nameLower) || pickedNamesLower.Contains(nameLower))
                    continue;

                double baseWin = candidate.BaseWinRate;
                double counterAdvantage = 0.0;
                double synergyBonus = 0.0;
                double roleBonus = 0.0;
                double mplBonus = 0.0;
                var highlights = new List<string>();

                // A. Counter matchups vs Enemy Picks
                foreach (var enemy in enemyHeroObjects)
                {
                    // Does candidate counter this enemy?
                    var directCounter = candidate.Counters.FirstOrDefault(c => c.HeroName.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase));
                    if (directCounter != null)
                    {
                        counterAdvantage += directCounter.AdvantageDelta;
                        highlights.Add(string.Format("Hard counters {0} (+{1:F1}%): {2}", enemy.Name, directCounter.AdvantageDelta, directCounter.Reason));
                    }

                    // Does candidate get countered by this enemy?
                    var getsCountered = candidate.CounteredBy.FirstOrDefault(c => c.HeroName.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase));
                    if (getsCountered != null)
                    {
                        counterAdvantage += getsCountered.AdvantageDelta;
                        highlights.Add(string.Format("Vulnerable to {0} ({1:F1}%): {2}", enemy.Name, getsCountered.AdvantageDelta, getsCountered.Reason));
                    }
                }

                // B. Ally Synergy Matchups
                foreach (var ally in allyHeroObjects)
                {
                    var syn = candidate.Synergies.FirstOrDefault(s => s.HeroName.Equals(ally.Name, StringComparison.OrdinalIgnoreCase));
                    if (syn != null)
                    {
                        synergyBonus += syn.SynergyScore;
                        highlights.Add(string.Format("Synergy with {0} (+{1:F1}%): {2}", ally.Name, syn.SynergyScore, syn.Reason));
                    }
                }

                // C. User Assigned Role ("My Role") & Lane fulfillment bonus
                if (!string.IsNullOrEmpty(draft.MyRole) && draft.MyRole != "All")
                {
                    bool matchesUserRole = (candidate.Lane.Equals(draft.MyRole, StringComparison.OrdinalIgnoreCase) ||
                                            candidate.PrimaryRole.Equals(draft.MyRole, StringComparison.OrdinalIgnoreCase));
                    if (matchesUserRole)
                    {
                        roleBonus += 8.0; // Strongly prioritize the user's selected position
                        highlights.Insert(0, string.Format("Direct match for your role: {0} ({1})", draft.MyRole, candidate.Lane));
                    }
                    else
                    {
                        roleBonus -= 5.0; // Deprioritize heroes outside user's selected role
                    }
                }
                else
                {
                    // General lane fulfillment bonus
                    bool fillsMissingLane = false;
                    if (!hasRoam && (candidate.Lane == "Roam" || candidate.PrimaryRole == "Tank" || candidate.PrimaryRole == "Support")) fillsMissingLane = true;
                    if (!hasJungle && (candidate.Lane == "Jungle" || candidate.PrimaryRole == "Assassin")) fillsMissingLane = true;
                    if (!hasMid && (candidate.Lane == "Mid" || candidate.PrimaryRole == "Mage")) fillsMissingLane = true;
                    if (!hasGold && (candidate.Lane == "Gold" || candidate.PrimaryRole == "Marksman")) fillsMissingLane = true;
                    if (!hasExp && (candidate.Lane == "Exp" || candidate.PrimaryRole == "Fighter")) fillsMissingLane = true;

                    if (fillsMissingLane)
                    {
                        roleBonus += 2.5;
                        highlights.Add(string.Format("Fills missing team role ({0} - {1})", candidate.PrimaryRole, candidate.Lane));
                    }
                    else if (allyHeroObjects.Count > 0)
                    {
                        int sameRoleCount = allyHeroObjects.Count(a => a.Lane == candidate.Lane || a.PrimaryRole == candidate.PrimaryRole);
                        if (sameRoleCount >= 1 && (candidate.Lane == "Jungle" || candidate.Lane == "Gold"))
                        {
                            roleBonus -= 4.0;
                        }
                    }
                }

                // D. MPL Tournament Meta Priority Bonus & First Pick Premium
                switch (candidate.MplTier)
                {
                    case "S+": mplBonus = 3.5; break;
                    case "S": mplBonus = 2.0; break;
                    case "A": mplBonus = 0.8; break;
                    case "B": mplBonus = -0.5; break;
                    default: mplBonus = 0.0; break;
                }

                // High tournament ban rate boost (contested picks in >40% bans)
                if (candidate.BanRate > 40.0)
                {
                    mplBonus += (candidate.BanRate - 40.0) * 0.06;
                }

                // First Pick Priority: If Ally has First Pick and zero picks made, boost high-priority contest tier picks
                if (draft.FirstPickTeam == "Ally" && draft.AllyPicks.Count == 0 && (candidate.MplTier == "S+" || candidate.BanRate >= 60.0))
                {
                    mplBonus += 2.0;
                    if (counterAdvantage <= 0.0)
                    {
                        highlights.Insert(0, string.Format("Priority 1st-Pick Contest ({0:F0}% Ban, {1:F0}% Contest)", candidate.BanRate, candidate.MplPresence));
                    }
                }
                else if (candidate.MplTier == "S+" && counterAdvantage <= 0.0)
                {
                    highlights.Insert(0, string.Format("S+ Tournament Power Pick ({0:F0}% Ban, {1:F0}% Contest)", candidate.BanRate, candidate.MplPresence));
                }

                double finalScore = baseWin + counterAdvantage + synergyBonus + roleBonus + mplBonus;
                finalScore = Math.Round(Math.Max(30.0, Math.Min(98.0, finalScore)), 1);

                string reasonText = highlights.Count > 0
                    ? highlights.First()
                    : string.Format("High Tier {0} pick with {1:F1}% base win rate", candidate.MplTier, candidate.BaseWinRate);

                candidateRecommendations.Add(new HeroRecommendation
                {
                    Hero = candidate,
                    CompositeScore = finalScore,
                    BaseWinRate = baseWin,
                    CounterAdvantage = counterAdvantage,
                    SynergyBonus = synergyBonus,
                    MplTierBonus = mplBonus,
                    RoleBadge = string.Format("{0} • {1}", candidate.PrimaryRole, candidate.Lane),
                    RecommendedReason = reasonText,
                    SpecificMatchups = highlights
                });
            }

            // Order by highest score
            draft.Recommendations = candidateRecommendations
                .OrderByDescending(r => r.CompositeScore)
                .Take(8)
                .ToList();
        }
    }
}
