using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>
    /// Maps a ground position (x, z) to a zone name. Each zone has a centre; a point belongs to the nearest centre.
    /// Why nearest-centre: every point on the map then belongs to exactly one zone, and the AI schemas accept
    /// only the 10 zone names, so there must never be a "no zone" answer.
    /// </summary>
    public class ZoneLookup
    {
        public static readonly string[] AllZones =
        {
            "P1Base", "P2Base", "NorthGate", "SouthField", "EastForest", "WestMine", "Center",
            "P1Harbor", "OpenSea", "P2Harbor"
        };
        static readonly HashSet<string> Water = new HashSet<string> { "P1Harbor", "OpenSea", "P2Harbor" };

        readonly List<string> names = new List<string>();
        readonly List<float> xs = new List<float>();
        readonly List<float> zs = new List<float>();

        public static bool IsWater(string zone) { return Water.Contains(zone); }

        public void Add(string zone, float x, float z) { names.Add(zone); xs.Add(x); zs.Add(z); }

        public int Count { get { return names.Count; } }

        /// <summary>True when all 10 zone names have a centre. The Unity adapter checks this at start-up.</summary>
        public bool IsComplete()
        {
            foreach (var z in AllZones) if (!names.Contains(z)) return false;
            return true;
        }

        /// <summary>Zone name of the nearest centre, or null if no zone was added.</summary>
        public string ZoneAt(float x, float z)
        {
            string best = null; float bestD = float.MaxValue;
            for (int i = 0; i < names.Count; i++)
            {
                float dx = x - xs[i], dz = z - zs[i];
                float d = dx * dx + dz * dz;
                if (d < bestD) { bestD = d; best = names[i]; }
            }
            return best;
        }
    }
}
