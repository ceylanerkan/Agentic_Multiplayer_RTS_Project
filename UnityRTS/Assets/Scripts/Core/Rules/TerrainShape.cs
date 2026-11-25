using System;
using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>
    /// The shape of the ground: a winding coast, gentle hills, flat ground around the Town Centers and mountains outside the map.
    /// A function of the position (no file), the same on every peer and, inside the map, mirrored between the two halves, so no player
    /// has an advantage (the mountains differ, nobody can go there).
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

        // ---- the river: from the sea through the middle of the map to the south; ships sail on it, land units wade through it ----
        /// <summary>Middle of the river at this z.</summary>
        public static float RiverX(float z) { return 4.5f * Sin(z * 0.062f + 0.6f) + 1.4f * Sin(z * 0.15f + 1f); }

        /// <summary>Metres from the river bank, negative in the river (only meaningful south of the coast).</summary>
        public static float RiverBank(float x, float z)
        {
            float half = 4.4f + 0.6f * Sin(z * 0.11f) + 4.5f * Smooth(28f, 46f, z); // wider at the mouth
            return Math.Abs(x - RiverX(z)) - half;
        }

        /// <summary>In the river: land units and ships can both be here, nothing can be built.</summary>
        public static bool InRiver(float x, float z) { return Math.Abs(x) < 30f && RiverBank(x, z) < 0.3f && z < CoastZ(x); }

        /// <summary>Height of the land (below SeaLevel: sea floor or river bed).</summary>
        public static float Height(float x, float z)
        {
            float h = Land(x, z);
            if (Math.Abs(x) > 30f) return h;
            float t = Smooth(-1.5f, 5.5f, RiverBank(x, z)), bed = Math.Min(SeaLevel - 0.6f, h); // also before the coast: no sand bar at the mouth
            return t >= 1f ? h : bed * (1f - t) + h * t;
        }

        static float Land(float x, float z)
        {
            float a = (float)Math.Sqrt(x * x + 16f);
            float inland = CoastZ(x) - z; // metres from the shore, positive on land
            if (inland <= 0f) return SeaLevel - Math.Min(-inland, 16f) * 0.16f;

            float h = 1.0f + 0.50f * Sin(a * 0.062f + 1.3f) * Cos(z * 0.071f + 0.7f) + 0.34f * Sin(a * 0.115f + z * 0.094f + 2f) + 0.18f * Sin(a * 0.23f - z * 0.19f + 0.5f);
            h += Hill(a, z, 37, -13, 13, 3.2f) + Hill(a, z, 25, -31, 11, 2.8f) + Hill(a, z, 91, -15, 11, 3.0f)
               + Hill(a, z, 52, -53, 13, 3.0f) + Hill(a, z, 24, -60, 11, 2.4f) + Hill(a, z, 39, 19, 9, 1.6f);
            // flat ground for building around the Town Center
            float dx = a - 65f, dz = z - 25f;
            h *= 0.2f + 0.8f * Smooth(13f, 30f, (float)Math.Sqrt(dx * dx + dz * dz));
            h += Mountains(x, z, inland);
            return SeaLevel + (h - SeaLevel) * Smooth(-0.5f, 10f, inland); // the beach
        }

        /// <summary>Where something stands: on the land or, over the sea, on the water.</summary>
        public static float Surface(float x, float z) { return Math.Max(SeaLevel, Height(x, z)); }

        // ---- the mountains around the map, only scenery ----
        /// <summary>Metres outside the map on the land sides, 0 inside.</summary>
        public static float Outside(float x, float z)
        {
            float ox = Math.Max(Math.Abs(x) - 98f, 0f), oz = Math.Max(-68f - z, 0f);
            return (float)Math.Sqrt(ox * ox + oz * oz);
        }

        static float Mountains(float x, float z, float inland)
        {
            float o = Outside(x, z);
            if (o <= 0f) return 0f;
            // the place is shifted by noise: crests and gullies instead of round cones
            float wx = x + 9f * Noise(x / 31f, z / 31f, 11) + 3.2f * Noise(x / 10f, z / 10f, 13), wz = z + 9f * Noise(x / 31f, z / 31f, 12) + 3.2f * Noise(x / 10f, z / 10f, 14);
            float p = 0f;
            foreach (var m in Peaks)
            {
                float dx = wx - m[0], dz = wz - m[1], d2 = dx * dx + dz * dz;
                float q = d2 >= m[3] * m[3] ? 0f : m[2] * (float)Math.Pow(1.0 - Math.Sqrt(d2) / m[3], 1.22);
                float e = Math.Max(5f - Math.Abs(p - q), 0f) / 5f, low = Math.Min(p, q);
                p = Math.Max(p, q) + e * e * 1.25f * Math.Min(1f, low / 3f); // saddles between the peaks
            }
            float h = p * (0.70f + 0.66f * Ridges(wx / 27f, wz / 27f)) + Smooth(3f, 30f, o) * (2.2f + 3f * Noise(x / 23f, z / 23f, 5));
            float g = 1f - Math.Abs(Noise(wx / 10f + 0.3f, wz / 10f, 45)), fine = 1f - Math.Abs(Noise(wx / 4.7f, wz / 4.7f, 46));
            h += Smooth(4f, 15f, p) * (3.4f * (g * g - 0.55f) + 1.1f * (fine * fine - 0.55f)); // gullies
            h *= Smooth(0f, 30f, o + 5f * Noise(x / 19f, z / 19f, 7) * Smooth(0f, 10f, o));    // foothills, with an uneven edge
            if (Math.Abs(x) < 60f) h *= Smooth(1f, 22f, RiverBank(x, z));                      // the valley the river comes from
            return Math.Max(h * Smooth(3f, 42f, inland), -0.6f);                               // lower towards the sea
        }

        // x, z, height, radius: a row south of the map, rows behind each other beside it
        static readonly float[][] Peaks = MakePeaks();

        static float[][] MakePeaks()
        {
            var rows = new List<float[]>();
            for (int s = -1; s <= 1; s += 2)
            {
                foreach (var v in new[] { new[] { 37f, 25f }, new[] { 70f, 22f }, new[] { 103f, 27f } }) rows.Add(new[] { s * v[0], -104f, v[1], 36f });
                foreach (var v in new[] { new[] { -68f, 27f }, new[] { -34f, 24f }, new[] { -1f, 28f } }) rows.Add(new[] { s * 137f, v[0], v[1], 37f });
                rows.Add(new[] { s * 140f, -108f, 30f, 40f });
                foreach (var v in new[] { new[] { -86f, 36f }, new[] { -42f, 40f }, new[] { 4f, 34f } }) rows.Add(new[] { s * 178f, v[0], v[1], 48f });
                foreach (var v in new[] { new[] { -100f, 42f }, new[] { -48f, 46f }, new[] { 0f, 40f } }) rows.Add(new[] { s * 228f, v[0], v[1], 60f });
                foreach (var v in new[] { new[] { -90f, 50f }, new[] { -30f, 46f } }) rows.Add(new[] { s * 280f, v[0], v[1], 66f });
            }
            for (int i = 0; i < rows.Count; i++) // no two alike
            {
                var m = rows[i];
                m[0] += (Chance(i, 1) - 0.5f) * 9f; m[1] += (Chance(i, 2) - 0.5f) * 9f; m[2] *= 0.86f + 0.28f * Chance(i, 3); m[3] *= 0.92f + 0.2f * Chance(i, 4);
            }
            return rows.ToArray();
        }

        static float Chance(int i, int k) { return (Hash(i, k, 0) & 0xffff) / 65535f; }

        static uint Hash(int x, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + z * 668265263 + seed * 974634777);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        static readonly float[] DirX = Directions(0), DirZ = Directions(1);

        static float[] Directions(int part)
        {
            var d = new float[16];
            for (int k = 0; k < 16; k++) d[k] = (float)(part == 0 ? Math.Cos(k * Math.PI / 8) : Math.Sin(k * Math.PI / 8));
            return d;
        }

        /// <summary>Gradient noise, about -1..1.</summary>
        static float Noise(float x, float z, int seed)
        {
            int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z);
            float fx = x - ix, fz = z - iz, u = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f), v = fz * fz * fz * (fz * (fz * 6f - 15f) + 10f);
            float a = Corner(ix, iz, fx, fz, seed), b = Corner(ix + 1, iz, fx - 1f, fz, seed), c = Corner(ix, iz + 1, fx, fz - 1f, seed), d = Corner(ix + 1, iz + 1, fx - 1f, fz - 1f, seed);
            float low = a + (b - a) * u, high = c + (d - c) * u;
            return (low + (high - low) * v) * 1.5f;
        }

        static float Corner(int ix, int iz, float dx, float dz, int seed) { uint k = Hash(ix, iz, seed) & 15; return DirX[k] * dx + DirZ[k] * dz; }

        /// <summary>Sharp crests, 0..1.</summary>
        static float Ridges(float x, float z)
        {
            float n = 0f, amp = 1f, w = 1f, total = 0f;
            for (int k = 0; k < 6; k++)
            {
                float r = Math.Max(1f - Math.Abs(Noise(x, z, 21 + k)), 0f);
                r = r * r * w; w = Math.Min(1f, r * 1.7f);
                n += r * amp; total += amp; amp *= 0.48f;
                float nx = x * 1.62f - z * 1.28f; z = x * 1.28f + z * 1.62f; x = nx;
            }
            return n / total;
        }

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
