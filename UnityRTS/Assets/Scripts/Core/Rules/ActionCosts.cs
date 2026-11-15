using System.Collections.Generic;
using System.Text;

namespace RTS.Core.Rules
{
    /// <summary>Total resource cost and a one-line description of an AI action, for the commander panel.</summary>
    public static class ActionCosts
    {
        public static Dictionary<string, int> CostOf(PlanAction a, GameData data)
        {
            var total = new Dictionary<string, int>();
            DefRecord def = null;
            int times = 1;
            if (a.ActionType == "TRAIN_UNIT" && a.UnitType != null) { data.Units.TryGetValue(a.UnitType, out def); times = a.Count; }
            else if (a.ActionType == "BUILD" && a.BuildingType != null) data.Buildings.TryGetValue(a.BuildingType, out def);
            else if (a.ActionType == "RESEARCH" && a.TechType != null) data.Techs.TryGetValue(a.TechType, out def);
            if (def == null) return total;
            foreach (var kv in def.Cost) total[kv.Key] = kv.Value * times;
            return total;
        }

        public static string CostText(Dictionary<string, int> cost)
        {
            if (cost.Count == 0) return "free";
            var sb = new StringBuilder();
            foreach (var kv in cost) { if (sb.Length > 0) sb.Append(' '); sb.Append(kv.Value).Append(kv.Key[0] == 'w' ? "W" : kv.Key[0] == 'f' ? "F" : "G"); }
            return sb.ToString();
        }

        public static string Describe(PlanAction a, GameData data)
        {
            string what = a.UnitType ?? a.BuildingType ?? a.TechType ?? a.UnitGroup ?? "";
            string where = a.TargetZone != null ? " -> " + a.TargetZone : "";
            string n = a.ActionType == "TRAIN_UNIT" || a.ActionType == "MOVE_GROUP" ? " x" + a.Count : "";
            return a.ActionType + " " + what + n + where + "  [" + CostText(CostOf(a, data)) + "]";
        }
    }
}
