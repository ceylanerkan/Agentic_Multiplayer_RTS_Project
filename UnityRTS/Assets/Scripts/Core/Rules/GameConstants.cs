namespace RTS.Core.Rules
{
    /// <summary>Numbers from ai/rag/knowledge/rules.md (rule_starting_state_v1 and the population rule).</summary>
    public static class GameConstants
    {
        public const int StartWood = 200, StartFood = 200, StartGold = 100;
        public const int StartVillagers = 3;
        public const int TownCenterCapacity = 5, HouseCapacity = 5, MaxPopulation = 50;

        /// <summary>5 per Town Center plus 5 per completed House, never above 50.</summary>
        public static int Capacity(int townCenters, int houses)
        {
            int cap = townCenters * TownCenterCapacity + houses * HouseCapacity;
            return cap > MaxPopulation ? MaxPopulation : cap;
        }
    }
}
