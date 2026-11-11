using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>One action of an RTSActionPlan (action_plan.schema.json), already parsed.</summary>
    public class PlanAction
    {
        public string ActionType; // TRAIN_UNIT | BUILD | MOVE_GROUP | RESEARCH
        public string UnitType, BuildingType, TechType, UnitGroup, TargetZone;
        public int Count = 1;
    }

    /// <summary>The player's current authoritative state (not the AI snapshot).</summary>
    public class PlayerState
    {
        public Dictionary<string, int> Resources = new Dictionary<string, int> { { "wood", 0 }, { "food", 0 }, { "gold", 0 } };
        public int PopulationUsed, PopulationCapacity;
        public int Age = 1;
        public HashSet<string> Techs = new HashSet<string>();
        public Dictionary<string, int> CompletedBuildings = new Dictionary<string, int>();
        public HashSet<string> CoastalSlotZones = new HashSet<string>(); // own zones with a free coastal slot
    }

    public class ValidationResult
    {
        public bool Ok;
        public string Reason;
        public static ValidationResult Pass() { return new ValidationResult { Ok = true }; }
        public static ValidationResult Fail(string why) { return new ValidationResult { Ok = false, Reason = why }; }
    }

    /// <summary>
    /// Re-checks an AI action against the CURRENT state at approval time. The host validates again on the RPC.
    /// Costs are summed per plan via the `reserved` argument so two actions cannot spend the same gold twice.
    /// </summary>
    public class GameActionValidator
    {
        static readonly HashSet<string> LandZones = new HashSet<string> { "P1Base", "P2Base", "NorthGate", "SouthField", "EastForest", "WestMine", "Center" };
        static readonly HashSet<string> WaterZones = new HashSet<string> { "P1Harbor", "P2Harbor", "OpenSea" };
        readonly GameData data;

        public GameActionValidator(GameData data) { this.data = data; }

        public ValidationResult Validate(PlanAction a, PlayerState s, Dictionary<string, int> reserved = null)
        {
            if (a == null || a.Count < 1 || a.Count > 20) return ValidationResult.Fail("count must be 1-20");
            switch (a.ActionType)
            {
                case "TRAIN_UNIT": return Train(a, s, reserved);
                case "BUILD": return Build(a, s, reserved);
                case "RESEARCH": return Research(a, s, reserved);
                case "MOVE_GROUP": return Move(a, s);
                default: return ValidationResult.Fail("unknown action_type");
            }
        }

        ValidationResult Train(PlanAction a, PlayerState s, Dictionary<string, int> reserved)
        {
            DefRecord u;
            if (a.UnitType == null || !data.Units.TryGetValue(a.UnitType, out u)) return ValidationResult.Fail("unknown unit_type");
            if (s.PopulationUsed + u.Population * a.Count > s.PopulationCapacity) return ValidationResult.Fail("not enough population capacity");
            var req = Requirement(u.Requires, s);
            if (req != null) return ValidationResult.Fail(req);
            return Pay(u.Cost, a.Count, s, reserved);
        }

        ValidationResult Build(PlanAction a, PlayerState s, Dictionary<string, int> reserved)
        {
            DefRecord b;
            if (a.BuildingType == null || !data.Buildings.TryGetValue(a.BuildingType, out b) || !b.Buildable) return ValidationResult.Fail("unknown or unbuildable building_type");
            if (a.TargetZone == null || !LandZones.Contains(a.TargetZone)) return ValidationResult.Fail("buildings need a land target_zone");
            if (a.BuildingType == "dock" && !s.CoastalSlotZones.Contains(a.TargetZone)) return ValidationResult.Fail("dock needs a free coastal slot in your own base");
            var req = Requirement(b.Requires, s);
            if (req != null) return ValidationResult.Fail(req);
            return Pay(b.Cost, a.Count, s, reserved);
        }

        ValidationResult Research(PlanAction a, PlayerState s, Dictionary<string, int> reserved)
        {
            DefRecord t;
            if (a.TechType == null || !data.Techs.TryGetValue(a.TechType, out t)) return ValidationResult.Fail("unknown tech_type");
            if (s.Techs.Contains(a.TechType)) return ValidationResult.Fail("already researched");
            var req = Requirement(t.Requires, s);
            if (req != null) return ValidationResult.Fail(req);
            return Pay(t.Cost, 1, s, reserved);
        }

        ValidationResult Move(PlanAction a, PlayerState s)
        {
            if (a.TargetZone == null || (!LandZones.Contains(a.TargetZone) && !WaterZones.Contains(a.TargetZone))) return ValidationResult.Fail("unknown target_zone");
            bool ships = a.UnitGroup == "all_ships" || a.UnitGroup == "raider_boat" || a.UnitGroup == "warship";
            if (ships && !WaterZones.Contains(a.TargetZone)) return ValidationResult.Fail("ships can only move to water zones");
            if (!ships && WaterZones.Contains(a.TargetZone)) return ValidationResult.Fail("land units cannot enter water zones");
            return ValidationResult.Pass();
        }

        // Requirements are keys of buildings, "age_2" or other techs (see the knowledge JSON).
        static string Requirement(List<string> requires, PlayerState s)
        {
            foreach (var r in requires)
            {
                if (r == "age_2") { if (s.Age < 2) return "requires Age II"; }
                else
                {
                    int built;
                    s.CompletedBuildings.TryGetValue(r, out built);
                    if (built < 1 && !s.Techs.Contains(r)) return "requires " + r;
                }
            }
            return null;
        }

        static ValidationResult Pay(Dictionary<string, int> cost, int count, PlayerState s, Dictionary<string, int> reserved)
        {
            foreach (var kv in cost)
            {
                int already = 0;
                if (reserved != null) reserved.TryGetValue(kv.Key, out already);
                int have;
                s.Resources.TryGetValue(kv.Key, out have);
                if (have - already < kv.Value * count) return ValidationResult.Fail("not enough " + kv.Key);
            }
            if (reserved != null)
                foreach (var kv in cost)
                {
                    int already;
                    reserved.TryGetValue(kv.Key, out already);
                    reserved[kv.Key] = already + kv.Value * count;
                }
            return ValidationResult.Pass();
        }
    }
}
