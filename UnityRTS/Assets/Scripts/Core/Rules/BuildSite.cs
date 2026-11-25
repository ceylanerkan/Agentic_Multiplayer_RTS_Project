using System;
using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>Ground that is taken by a building or a resource node (a rectangle around x, z).</summary>
    public struct Block
    {
        public float X, Z, HalfX, HalfZ;
        public Block(float x, float z, float halfX, float halfZ) { X = x; Z = z; HalfX = halfX; HalfZ = halfZ; }
    }

    /// <summary>
    /// Where a building may stand (rule_building_prereqs_v1): anywhere on land outside the enemy base, inside the map and not on top of
    /// another building or a resource. A Dock must stand on the shore.
    /// </summary>
    public static class BuildSite
    {
        public const float MapHalfX = 100f, MapMinZ = -70f, MapMaxZ = 80f; // ground and sea of the generated map (RtsSetupTools)
        public const float Gap = 1f;             // free ground between two buildings
        public const float ShoreDistance = 2.5f; // Dock: water is at most this far from its centre (a Raider Boat reaches 4)

        /// <summary>Why the building cannot stand here, or null.</summary>
        public static string WhyNot(string key, float x, float z, float halfX, float halfZ, int slot,
                                    Func<float, float, string> zoneAt, List<Block> blocks, float gap = Gap)
        {
            if (Math.Abs(x) + halfX > MapHalfX || z - halfZ < MapMinZ) return "outside the map";
            string zone = zoneAt(x, z);
            if (zone == null || ZoneLookup.IsWater(zone)) return "buildings stand on land";
            if (zone == (slot == 1 ? "P2Base" : "P1Base")) return "cannot build in the enemy base";
            if (key == "dock") { if (!NearWater(x, z, zoneAt)) return "a Dock must stand on the shore"; }
            else
                foreach (float sx in Signs)
                    foreach (float sz in Signs)
                        if (ZoneLookup.IsWater(zoneAt(x + sx * halfX, z + sz * halfZ))) return "too close to the water";
            foreach (var b in blocks)
                if (Math.Abs(x - b.X) < halfX + b.HalfX + gap && Math.Abs(z - b.Z) < halfZ + b.HalfZ + gap) return "the place is taken";
            return null;
        }

        static readonly float[] Signs = { -1f, 1f };

        static bool NearWater(float x, float z, Func<float, float, string> zoneAt)
        {
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                if (ZoneLookup.IsWater(zoneAt(x + (float)Math.Cos(a) * ShoreDistance, z + (float)Math.Sin(a) * ShoreDistance))) return true;
            }
            return false;
        }

        /// <summary>The point nearest to (x, z) that passes the test, searched in rings up to the radius.</summary>
        public static bool FindNear(float x, float z, float radius, Func<float, float, bool> test, out float foundX, out float foundZ)
        {
            foundX = x; foundZ = z;
            if (test(x, z)) return true;
            var ring = new List<double>();
            for (float r = 0.5f; r <= radius; r += 0.5f)
            {
                ring.Clear();
                int n = 8 * (int)Math.Ceiling(r);
                for (int i = 0; i < n; i++)
                {
                    double a = i * 2 * Math.PI / n;
                    if (test(x + (float)Math.Cos(a) * r, z + (float)Math.Sin(a) * r)) ring.Add(a);
                }
                if (ring.Count == 0) continue;
                double mid = ring[ring.Count / 2]; // the middle of the free part of the ring: straight to the shore, not diagonally
                foundX = x + (float)Math.Cos(mid) * r; foundZ = z + (float)Math.Sin(mid) * r;
                return true;
            }
            return false;
        }
    }
}
