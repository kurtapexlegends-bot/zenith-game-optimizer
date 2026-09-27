using System;
using System.Collections.Generic;

namespace ZenithOptimizer.Models
{
    public class MlbbDraftState
    {
        public List<string> BannedHeroes { get; set; }
        public List<string> AllyPicks { get; set; }
        public List<string> EnemyPicks { get; set; }

        public List<string> MissingAllyRoles { get; set; }
        public List<string> EnemyKeyThreats { get; set; }

        public double AllyWinChance { get; set; }
        public double EnemyWinChance { get; set; }

        public string MyRole { get; set; } // "All", "Roam", "Exp", "Mid", "Gold", "Jungle"
        public string FirstPickTeam { get; set; } // "Ally", "Enemy"

        public List<HeroRecommendation> Recommendations { get; set; }

        public MlbbDraftState()
        {
            BannedHeroes = new List<string>();
            AllyPicks = new List<string>();
            EnemyPicks = new List<string>();
            MissingAllyRoles = new List<string>();
            EnemyKeyThreats = new List<string>();
            Recommendations = new List<HeroRecommendation>();
            MyRole = "All";
            FirstPickTeam = "Ally";
        }

        public void Clear()
        {
            BannedHeroes.Clear();
            AllyPicks.Clear();
            EnemyPicks.Clear();
            MissingAllyRoles.Clear();
            EnemyKeyThreats.Clear();
            Recommendations.Clear();
        }
    }

    public class HeroRecommendation
    {
        public MlbbHero Hero { get; set; }
        public double CompositeScore { get; set; } // Blended % rating
        public double BaseWinRate { get; set; }
        public double CounterAdvantage { get; set; }
        public double SynergyBonus { get; set; }
        public double MplTierBonus { get; set; }
        public string RoleBadge { get; set; }
        public string RecommendedReason { get; set; }
        public List<string> SpecificMatchups { get; set; }

        public HeroRecommendation()
        {
            SpecificMatchups = new List<string>();
        }

        public string ScoreFormatted
        {
            get { return string.Format("{0:F1}%", CompositeScore); }
        }
    }
}
