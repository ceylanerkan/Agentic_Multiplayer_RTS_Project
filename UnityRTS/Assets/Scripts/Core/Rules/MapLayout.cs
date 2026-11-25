using System;
using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>One tree, berry bush, gold mine or fish shoal of the map.</summary>
    public class MapNode
    {
        public string Kind; // "tree" | "berry" | "gold" | "fish"
        public float X, Z, Scale, Turn;
    }

    /// <summary>
    /// Where the resources of the map lie: irregular groups around hand-picked centres on the left half, mirrored to the right half
    /// (both players get the same). The random numbers have a fixed seed, so every build has the same map.
    /// </summary>
    public static class MapLayout
    {
        class Group
        {
            public string Kind; public float X, Z, RadiusX, RadiusZ; public int Count; public float Apart;
            public Group(string kind, float x, float z, float radiusX, float radiusZ, int count, float apart)
            { Kind = kind; X = x; Z = z; RadiusX = radiusX; RadiusZ = radiusZ; Count = count; Apart = apart; }
        }

        // left half (x < 0); the Town Center of player 1 stands at (-65, 25)
        static readonly Group[] Left =
        {
            // own base: a grove on the outer side, a small one on the inner side, berries and two gold mines in front
            new Group("tree", -89, 27, 7, 8, 8, 2.7f), new Group("tree", -38, 31, 3.5f, 6, 4, 2.7f),
            new Group("berry", -74, 5, 6, 3.5f, 5, 2.4f), new Group("gold", -60, 0, 5, 3, 2, 4.6f),
            // outer field: gold and a forest
            new Group("gold", -68, -36, 9, 6, 4, 5f), new Group("tree", -86, -32, 9, 11, 9, 2.7f), new Group("berry", -50, -24, 4, 3, 2, 2.4f),
            // between the players: worth fighting for
            new Group("gold", -16, 12, 2, 2, 1, 5f), new Group("tree", -21, 34, 5, 4, 3, 2.7f),
            new Group("berry", -18, -38, 5, 5, 4, 2.4f), new Group("tree", -30, -54, 10, 8, 6, 2.7f),
            // single trees
            new Group("tree", -34, -36, 2, 2, 1, 3), new Group("tree", -96, -52, 2, 2, 1, 3), new Group("tree", -50, -60, 2, 2, 1, 3),
            new Group("tree", -27, 17, 2, 2, 1, 3), new Group("tree", -19, -63, 2, 2, 1, 3), new Group("tree", -97, -4, 1, 2, 1, 3),
            // fish: before the own harbor, and two shoals in the open sea
            new Group("fish", -70, 61, 14, 7, 3, 7f), new Group("fish", -18, 63, 10, 8, 2, 8f),
        };

        public static List<MapNode> Build()
        {
            var random = new Random(377);
            var left = new List<MapNode>();
            foreach (var g in Left)
            {
                int placed = 0;
                for (int attempt = 0; attempt < 4000 && placed < g.Count; attempt++)
                {
                    // a point in the ellipse, denser in the middle
                    double a = random.NextDouble() * 2 * Math.PI, r = Math.Pow(random.NextDouble(), 0.6);
                    float x = g.X + (float)(Math.Cos(a) * r) * g.RadiusX, z = g.Z + (float)(Math.Sin(a) * r) * g.RadiusZ;
                    bool free = true;
                    foreach (var n in left)
                        if ((n.X - x) * (n.X - x) + (n.Z - z) * (n.Z - z) < g.Apart * g.Apart) { free = false; break; }
                    if (!free) continue;
                    float spread = g.Kind == "tree" ? 0.25f : 0.1f;
                    left.Add(new MapNode { Kind = g.Kind, X = x, Z = z, Scale = 1f + (float)(random.NextDouble() * 2 - 1) * spread, Turn = (float)random.NextDouble() * 360f });
                    placed++;
                }
            }
            var all = new List<MapNode>(left);
            foreach (var n in left) all.Add(new MapNode { Kind = n.Kind, X = -n.X, Z = n.Z, Scale = n.Scale, Turn = -n.Turn });
            return all;
        }
    }
}
