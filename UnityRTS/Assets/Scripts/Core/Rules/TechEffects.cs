using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>
    /// What the researched technologies change (numbers from technologies.json descriptions):
    /// Iron Weapons +2 attack for military units, Fletching +1 range and line of sight for Archers,
    /// Horse Collar +20% food gathering. Age II only unlocks things (the validator checks that).
    /// Effects are looked up when needed, so they apply to existing and future units alike.
    /// </summary>
    public static class TechEffects
    {
        public const float IronWeaponsAttack = 2f, FletchingRange = 1f, FletchingLos = 1f, HorseCollarFood = 1.2f;

        static bool IsMilitary(string unitKey) { return unitKey != "villager"; }

        public static float AttackBonus(string unitKey, ICollection<string> techs)
        {
            return IsMilitary(unitKey) && techs.Contains("iron_weapons") ? IronWeaponsAttack : 0f;
        }

        public static float RangeBonus(string unitKey, ICollection<string> techs)
        {
            return unitKey == "archer" && techs.Contains("fletching") ? FletchingRange : 0f;
        }

        public static float LosBonus(string unitKey, ICollection<string> techs)
        {
            return unitKey == "archer" && techs.Contains("fletching") ? FletchingLos : 0f;
        }

        public static float FoodGatherMultiplier(ICollection<string> techs)
        {
            return techs.Contains("horse_collar") ? HorseCollarFood : 1f;
        }
    }

    /// <summary>
    /// Match result: the player whose Town Center count drops to zero loses. Pure, so it is easy to test.
    /// Returns the losing slot (1 or 2), or 0 when the match goes on.
    /// </summary>
    public static class WinCondition
    {
        public static int LosingSlot(int p1TownCenters, int p2TownCenters)
        {
            if (p1TownCenters <= 0 && p2TownCenters <= 0) return 1; // simultaneous: the host (P1) loses the tie, deterministic
            if (p1TownCenters <= 0) return 1;
            if (p2TownCenters <= 0) return 2;
            return 0;
        }
    }
}
