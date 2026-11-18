namespace RTS.Core.Rules
{
    /// <summary>
    /// Land / water rules (rule_naval_v1):
    ///  - land units stay on land, ships stay on water,
    ///  - melee units (range 1 or less) can only attack targets of their own domain,
    ///  - ranged units (Archer, Catapult, Warship) can shoot across the shoreline at any target within range,
    ///  - a Dock stands on the coast and can be attacked from both land and water.
    /// </summary>
    public static class DomainRules
    {
        public const string Land = "land", Water = "water", Both = "both";

        public static string DomainOfUnit(DefRecord unit) { return unit.Domain == Water ? Water : Land; }

        public static string DomainOfBuilding(string buildingKey) { return buildingKey == "dock" ? Both : Land; }

        public static bool CanAttack(DefRecord attacker, string targetDomain)
        {
            if (attacker.Range > 1.0f) return true;                  // ranged: any domain
            if (targetDomain == Both) return true;                   // Dock
            return DomainOfUnit(attacker) == targetDomain;           // melee: own domain only
        }

        /// <summary>May a unit of this domain stand in a zone of that kind? (true = zone is water)</summary>
        public static bool CanStandIn(DefRecord unit, bool zoneIsWater)
        {
            return DomainOfUnit(unit) == Water ? zoneIsWater : !zoneIsWater;
        }
    }
}
