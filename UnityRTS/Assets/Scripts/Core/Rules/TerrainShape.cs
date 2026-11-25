using System;

namespace RTS.Core.Rules
{
    /// <summary>
    /// The shape of the ground: a winding coast, gentle hills, flat ground around the Town Centers and higher land outside the map.
    /// A function of the position (no file), the same on every peer and mirrored between the two halves, so no player has an advantage.
    /// Hills are only seen: speed, range and sight do not depend on the height.
    /// </summary>
    public static class TerrainShape
    {
        public const float SeaLevel = -0.35f;

        /// <summary>North edge of the land at this x (the sea lies behind it).</summary>
        public static float CoastZ(float x) { return 45f + CoastOffset(x); }

        static float CoastOffset(float x)
        {
            float a = (float)Math.Sqrt(x * x + 16f); // like |x|, but smooth in the middle
            return 2.9f * Sin(a * 0.105f + 0.4f) + 1.5f * Sin(a * 0.27f + 2.2f) + 0.6f * Sin(a * 0.62f + 1.1f);
        }

        /// <summary>The z that the zone lookup must use: it knows only the straight coast at z = 45.</summary>
        public static float ZoneZ(float x, float z)
        {
            float near = Smooth(22f, 37f, z) * (1f - Smooth(53f, 70f, z)); // only around the coast, the other zone borders stay
            return near <= 0f ? z : z - CoastOffset(x) * near;
        }

        /// <summary>Height of the land (below SeaLevel: sea floor).</summary>
        public static float Height(float x, float z)
        {
            float a = (float)Math.Sqrt(x * x + 16f);
            float inland = CoastZ(x) - z; // metres from the shore, positive on land
            if (inland <= 0f) return SeaLevel - Math.Min(-inland, 16f) * 0.16f;

            float h = 1.0f + 0.50f * Sin(a * 0.062f + 1.3f) * Cos(z * 0.071f + 0.7f) + 0.34f * Sin(a * 0.115f + z * 0.094f + 2f) + 0.18f * Sin(a * 0.23f - z * 0.19f + 0.5f);
            h += Hill(a, z, 37, -13, 13, 3.2f) + Hill(a, z, 25, -31, 11, 2.8f) + Hill(a, z, 4, 9, 10, 2.0f) + Hill(a, z, 91, -15, 11, 3.0f)
               + Hill(a, z, 52, -53, 13, 3.0f) + Hill(a, z, 17, -60, 11, 2.4f) + Hill(a, z, 39, 19, 9, 1.6f);
            // flat ground for building around the Town Center
            float dx = a - 65f, dz = z - 25f;
            h *= 0.2f + 0.8f * Smooth(13f, 30f, (float)Math.Sqrt(dx * dx + dz * dz));
            // higher land around the map hides its edge
            float outside = Math.Max(a - 99f, -69f - z);
            if (outside > 0f) h += Smooth(0f, 34f, outside) * 13f + 0.8f * Sin(x * 0.31f + z * 0.27f) * Smooth(0f, 12f, outside);
            return SeaLevel + (h - SeaLevel) * Smooth(-0.5f, 10f, inland); // the beach
        }

        /// <summary>Where something stands: on the land or, over the sea, on the water.</summary>
        public static float Surface(float x, float z) { return Math.Max(SeaLevel, Height(x, z)); }

        static float Hill(float a, float z, float centreA, float centreZ, float radius, float height)
        {
            float d = ((a - centreA) * (a - centreA) + (z - centreZ) * (z - centreZ)) / (radius * radius);
            return d > 6f ? 0f : height * (float)Math.Exp(-d * 1.4f);
        }

        static float Smooth(float from, float to, float v)
        {
            float t = Math.Max(0f, Math.Min(1f, (v - from) / (to - from)));
            return t * t * (3f - 2f * t);
        }

        static float Sin(float v) { return (float)Math.Sin(v); }
        static float Cos(float v) { return (float)Math.Cos(v); }
    }
}
