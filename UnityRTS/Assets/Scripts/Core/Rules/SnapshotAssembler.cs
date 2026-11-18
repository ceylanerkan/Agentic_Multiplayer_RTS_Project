using System;
using System.Collections.Generic;

namespace RTS.Core.Rules
{
    public class OwnUnitInfo { public string Type, Zone; public float HpPct; public bool Idle; }
    public class OwnBuildingInfo { public string Type; public bool Completed, Busy; }
    public class EnemySighting { public string Type, Zone; }

    /// <summary>
    /// Builds the AI snapshot from plain lists (the Unity adapter fills the lists). Grouping and limits follow
    /// safe_snapshot.schema.json: units and enemies are grouped by type and zone, at most 32 groups, events at most 20.
    /// The input for enemies must already come from VisibilityService.FilterFor: this class cannot add anything hidden.
    /// </summary>
    public static class SnapshotAssembler
    {
        public const int MaxGroups = 32;

        public static AiSafeSnapshot Build(string matchId, float gameTime, int age, int wood, int food, int gold,
            int popUsed, int popCap, IList<OwnUnitInfo> units, IList<OwnBuildingInfo> buildings,
            IList<EnemySighting> enemies, ICollection<string> researchedTech, ICollection<string> researching,
            IList<KeyValuePair<GameEvent, float>> events)
        {
            var s = new AiSafeSnapshot
            {
                MatchId = matchId.Length > 64 ? matchId.Substring(0, 64) : matchId,
                GameTimeS = Math.Max(0f, gameTime), Age = age < 2 ? 1 : 2
            };
            s.Resources.Wood = Math.Max(0, wood); s.Resources.Food = Math.Max(0, food); s.Resources.Gold = Math.Max(0, gold);
            s.Population.Used = Math.Max(0, popUsed); s.Population.Capacity = Math.Min(50, Math.Max(0, popCap));

            // own units: group by (type, zone), with idle count and average HP
            var groups = new SortedDictionary<string, SnapGroup>();
            var hpSum = new Dictionary<string, float>();
            foreach (var u in units)
            {
                string zone = u.Zone ?? "Center";
                string key = u.Type + "|" + zone;
                SnapGroup g;
                if (!groups.TryGetValue(key, out g)) { g = new SnapGroup { Type = u.Type, Zone = zone, Idle = 0 }; groups[key] = g; hpSum[key] = 0; }
                g.Count++; if (u.Idle) g.Idle++; hpSum[key] += u.HpPct;
            }
            foreach (var kv in groups) { kv.Value.AvgHpPct = (float)Math.Round(Math.Min(100f, Math.Max(0f, hpSum[kv.Key] / kv.Value.Count)), 1); s.OwnArmy.Add(kv.Value); }
            Trim(s.OwnArmy);

            // own buildings: count completed ones, count the others as under construction
            var bs = new SortedDictionary<string, SnapBuilding>();
            foreach (var b in buildings)
            {
                SnapBuilding sb;
                if (!bs.TryGetValue(b.Type, out sb)) { sb = new SnapBuilding { Type = b.Type, Count = 0, UnderConstruction = 0, Busy = 0 }; bs[b.Type] = sb; }
                if (b.Completed) { sb.Count++; if (b.Busy) sb.Busy++; } else sb.UnderConstruction++;
            }
            foreach (var sb in bs.Values) s.OwnBuildings.Add(sb);

            // visible enemies (already filtered): group by type and zone
            var eg = new SortedDictionary<string, SnapGroup>();
            foreach (var e in enemies)
            {
                string zone = e.Zone ?? "Center";
                string key = e.Type + "|" + zone;
                SnapGroup g;
                if (!eg.TryGetValue(key, out g)) { g = new SnapGroup { Type = e.Type, Zone = zone }; eg[key] = g; }
                g.Count++;
            }
            foreach (var g in eg.Values) s.VisibleEnemies.Add(g);
            Trim(s.VisibleEnemies);

            foreach (var t in researchedTech) s.ResearchedTech.Add(t);
            if (researching != null && researching.Count > 0) { s.Researching = new List<string>(); foreach (var t in researching) s.Researching.Add(t); }

            foreach (var kv in events)
            {
                if (s.RecentEvents.Count >= PlayerEventLog.MaxInSnapshot) break;
                s.RecentEvents.Add(new SnapEvent { Type = kv.Key.Type, Zone = kv.Key.Zone, Detail = kv.Key.Detail, SecondsAgo = (float)Math.Round(kv.Value, 1) });
            }
            return s;
        }

        // The schema allows 32 groups; keep the biggest ones if there are more.
        static void Trim(List<SnapGroup> list)
        {
            if (list.Count <= MaxGroups) return;
            list.Sort((a, b) => b.Count.CompareTo(a.Count));
            list.RemoveRange(MaxGroups, list.Count - MaxGroups);
        }
    }
}
