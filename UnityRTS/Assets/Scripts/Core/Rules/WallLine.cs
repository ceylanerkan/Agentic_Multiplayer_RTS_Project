using System;
using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>
    /// The pieces of a wall between two points: on a 2 m grid, the next piece is always a direct or diagonal neighbour. Two diagonal
    /// neighbours touch at a corner, which no unit can slip through (PathGrid does not cut corners).
    /// </summary>
    public static class WallLine
    {
        public const float Step = 2f;
        public const int MaxPieces = 40;

        public static float Snap(float v) { return (float)Math.Floor(v / Step + 0.5f) * Step; }

        public static List<float[]> Points(float ax, float az, float bx, float bz)
        {
            int x = (int)Math.Floor(ax / Step + 0.5f), z = (int)Math.Floor(az / Step + 0.5f), x1 = (int)Math.Floor(bx / Step + 0.5f), z1 = (int)Math.Floor(bz / Step + 0.5f);
            int dx = Math.Abs(x1 - x), dz = Math.Abs(z1 - z), sx = x < x1 ? 1 : -1, sz = z < z1 ? 1 : -1, error = dx - dz;
            var list = new List<float[]>();
            while (list.Count < MaxPieces)
            {
                list.Add(new[] { x * Step, z * Step });
                if (x == x1 && z == z1) break;
                int twice = 2 * error;
                if (twice > -dz) { error -= dz; x += sx; }
                if (twice < dx) { error += dx; z += sz; }
            }
            return list;
        }
    }
}
