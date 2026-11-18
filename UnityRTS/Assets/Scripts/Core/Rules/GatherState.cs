namespace RTS.Core.Rules
{
    /// <summary>
    /// What one Villager carries while gathering (rule_resources_v1): 0.6 resource per second, carry at most 10,
    /// deposit at a Town Center. Pure logic so it can be unit-tested; the host drives it with Tick(dt).
    /// </summary>
    public class GatherState
    {
        public const float RatePerSecond = 0.6f;
        public const int CarryMax = 10;
        public const float HorseCollarFoodBonus = 1.2f; // +20% food gathering

        public string Resource;      // what is carried right now
        public float Carried;        // fractional while gathering
        public bool Full { get { return Carried >= CarryMax; } }
        public int CarriedWhole { get { return (int)Carried; } }

        /// <summary>
        /// Gather for dt seconds from a node holding nodeAmount. Returns how much was taken from the node
        /// (never more than it holds, never more than fits in the bag).
        /// </summary>
        public float Tick(float dt, string resource, float nodeAmount, bool horseCollar)
        {
            if (Resource != resource) { Resource = resource; Carried = 0; }
            float rate = RatePerSecond * (resource == "food" && horseCollar ? HorseCollarFoodBonus : 1f);
            float take = rate * dt;
            if (take > CarryMax - Carried) take = CarryMax - Carried;
            if (take > nodeAmount) take = nodeAmount;
            if (take < 0) take = 0;
            Carried += take;
            return take;
        }

        /// <summary>Empty the bag at a Town Center; returns the whole amount to add to the stockpile.</summary>
        public int Deposit()
        {
            int whole = CarriedWhole;
            Carried -= whole; // keep the fraction so nothing is lost over many trips
            return whole;
        }
    }
}
