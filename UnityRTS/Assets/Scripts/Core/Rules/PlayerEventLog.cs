using System.Collections.Generic;

namespace RTS.Core.Rules
{
    public class GameEvent
    {
        public string Type;
        public string Zone;
        public string Detail;
        public float Time; // game time in seconds when it happened
    }

    /// <summary>
    /// The recent events one player may know about. Each player has their own log. The host only adds events about
    /// that player's own objects, plus ENEMY_SIGHTED which is derived from VisibilityService, so nothing hidden leaks.
    /// The snapshot takes the newest 20 (schema: maxItems 20) with seconds_ago relative to now.
    /// </summary>
    public class PlayerEventLog
    {
        public static readonly HashSet<string> ValidTypes = new HashSet<string>
        {
            "UNDER_ATTACK", "UNIT_LOST", "UNIT_TRAINED", "BUILDING_COMPLETED", "BUILDING_LOST",
            "TECH_RESEARCHED", "ENEMY_SIGHTED", "RESOURCE_DEPLETED"
        };
        public const int MaxStored = 50, MaxInSnapshot = 20, UnderAttackCooldown = 10, DetailMax = 80;

        readonly List<GameEvent> events = new List<GameEvent>();
        float lastUnderAttack = -1000f;

        public void Clear() { events.Clear(); lastUnderAttack = -1000f; }

        public int Count { get { return events.Count; } }

        public bool Add(string type, string zone, string detail, float now)
        {
            if (!ValidTypes.Contains(type)) return false;
            // A fight would produce an UNDER_ATTACK every hit; one per 10 s is enough information.
            if (type == "UNDER_ATTACK")
            {
                if (now - lastUnderAttack < UnderAttackCooldown) return false;
                lastUnderAttack = now;
            }
            if (detail != null && detail.Length > DetailMax) detail = detail.Substring(0, DetailMax);
            events.Add(new GameEvent { Type = type, Zone = zone, Detail = detail, Time = now });
            if (events.Count > MaxStored) events.RemoveAt(0);
            return true;
        }

        /// <summary>Newest events first, at most MaxInSnapshot, with their age in seconds.</summary>
        public List<KeyValuePair<GameEvent, float>> Recent(float now)
        {
            var result = new List<KeyValuePair<GameEvent, float>>();
            for (int i = events.Count - 1; i >= 0 && result.Count < MaxInSnapshot; i--)
                result.Add(new KeyValuePair<GameEvent, float>(events[i], now - events[i].Time < 0 ? 0 : now - events[i].Time));
            return result;
        }
    }
}
