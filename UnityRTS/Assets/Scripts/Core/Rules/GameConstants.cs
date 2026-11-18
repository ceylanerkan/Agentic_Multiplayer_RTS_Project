namespace RTS.Core.Rules
{
    /// <summary>Numbers from ai/rag/knowledge/rules.md (rule_starting_state_v1 and the population rule).</summary>
    public static class GameConstants
    {
        public const int StartWood = 200, StartFood = 200, StartGold = 100;
        public const int StartVillagers = 3;
        public const float AttackCooldown = 1.0f;   // seconds between attacks; not in the knowledge base yet (small decision)
        public const float CatapultCooldown = 3.0f; // "fires slowly" (units.json description); the exact number is a small decision

        public static float CooldownFor(string unitKey) { return unitKey == "catapult" ? CatapultCooldown : AttackCooldown; }

        public const float UnitReach = 1.2f;        // added to range: units and buildings have a size
        public const float BuildingReach = 3.0f;
        public const int TownCenterCapacity = 5, HouseCapacity = 5, MaxPopulation = 50;

        /// <summary>5 per Town Center plus 5 per completed House, never above 50.</summary>
        public static int Capacity(int townCenters, int houses)
        {
            int cap = townCenters * TownCenterCapacity + houses * HouseCapacity;
            return cap > MaxPopulation ? MaxPopulation : cap;
        }
    }
}
