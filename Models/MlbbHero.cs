using System;
using System.Collections.Generic;

namespace ZenithOptimizer.Models
{
    public class MlbbHero
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PrimaryRole { get; set; } // Tank, Fighter, Assassin, Mage, Marksman, Support
        public string SecondaryRole { get; set; }
        public string Lane { get; set; } // Roam, Exp, Mid, Gold, Jungle
        public double BaseWinRate { get; set; } // e.g. 51.4
        public double BanRate { get; set; } // e.g. 35.2
        public double PickRate { get; set; } // e.g. 12.8
        public string MplTier { get; set; } // S+, S, A, B, Situational
        public double MplPresence { get; set; } // Pro tournament presence %

        public List<HeroCounterMatchup> Counters { get; set; }
        public List<HeroCounterMatchup> CounteredBy { get; set; }
        public List<HeroSynergyMatchup> Synergies { get; set; }

        public MlbbHero()
        {
            Counters = new List<HeroCounterMatchup>();
            CounteredBy = new List<HeroCounterMatchup>();
            Synergies = new List<HeroSynergyMatchup>();
        }

        public override string ToString()
        {
            return string.Format("{0} ({1} - {2})", Name, PrimaryRole, Lane);
        }
    }

    public class HeroCounterMatchup
    {
        public string HeroName { get; set; }
        public double AdvantageDelta { get; set; } // e.g. +4.5% win rate delta
        public string Reason { get; set; } // e.g. "Hard CC interrupts mobility/cables"
    }

    public class HeroSynergyMatchup
    {
        public string HeroName { get; set; }
        public double SynergyScore { get; set; } // e.g. +3.0% combo synergy
        public string Reason { get; set; } // e.g. "AoE grouping combo with lethal burst"
    }
}
