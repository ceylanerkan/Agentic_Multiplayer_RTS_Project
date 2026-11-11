using System;

namespace RTS.Core.Rules
{
    /// <summary>damage = max(1, attack x multiplier - armor). Rules: rule_combat_counters_v1.</summary>
    public static class DamageCalculator
    {
        public const float CounterBonus = 1.5f;  // attacker counters target
        public const float CounterPenalty = 0.75f; // target counters attacker
        public const float CatapultVsBuilding = 3f;

        public static float Multiplier(DefRecord attacker, DefRecord target, bool targetIsBuilding)
        {
            if (targetIsBuilding) return attacker.Key == "catapult" ? CatapultVsBuilding : 1f;
            if (attacker.Strengths.Contains(target.Key)) return CounterBonus;
            if (target.Strengths.Contains(attacker.Key)) return CounterPenalty;
            return 1f;
        }

        /// <param name="attackBonus">Flat bonus from technologies, e.g. +2 from Iron Weapons.</param>
        public static float Damage(DefRecord attacker, DefRecord target, bool targetIsBuilding, float attackBonus = 0f)
        {
            float raw = (attacker.Attack + attackBonus) * Multiplier(attacker, target, targetIsBuilding);
            return Math.Max(1f, raw - target.Armor);
        }
    }
}
