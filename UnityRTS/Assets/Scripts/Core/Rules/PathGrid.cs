using System;
using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>
    /// Pathfinding on a 1 m grid (A*, 8 directions, no corner cutting). Land units use land cells, ships water cells; buildings and
    /// resources block the cells they cover. Why not a NavMesh: buildings can stand anywhere, so the free ground changes during the
    /// match, and the zone map already says what is land and what is water.
    /// </summary>
    public class PathGrid
    {
        public const float Cell = 1f;
        public const float Clearance = 0.5f; // a unit's centre keeps this distance from a building
        const int MaxExpanded = 20000;
        const float Diagonal = 1.41421356f;

        readonly int w, h;
        readonly float minX, minZ;
        readonly bool[] water, blocked;
        // search state, reused: an entry counts only when its stamp is the current search
        readonly float[] cost;
        readonly int[] from, seen, closed;
        int search;

        public PathGrid(float minX, float minZ, float maxX, float maxZ, Func<float, float, bool> isWater)
        {
            this.minX = minX; this.minZ = minZ;
            w = (int)Math.Ceiling((maxX - minX) / Cell); h = (int)Math.Ceiling((maxZ - minZ) / Cell);
            water = new bool[w * h]; blocked = new bool[w * h];
            cost = new float[w * h]; from = new int[w * h]; seen = new int[w * h]; closed = new int[w * h];
            for (int i = 0; i < water.Length; i++) water[i] = isWater(X(i), Z(i));
        }

        public void SetBlocks(List<Block> blocks)
        {
            Array.Clear(blocked, 0, blocked.Length);
            foreach (var b in blocks)
            {
                float hx = b.HalfX + Clearance, hz = b.HalfZ + Clearance;
                for (int cz = Math.Max(0, Row(b.Z - hz)); cz <= Math.Min(h - 1, Row(b.Z + hz)); cz++)
                    for (int cx = Math.Max(0, Column(b.X - hx)); cx <= Math.Min(w - 1, Column(b.X + hx)); cx++)
                    {
                        int i = cz * w + cx;
                        if (Math.Abs(X(i) - b.X) < hx && Math.Abs(Z(i) - b.Z) < hz) blocked[i] = true;
                    }
            }
        }

        int Column(float x) { return (int)Math.Floor((x - minX) / Cell); }
        int Row(float z) { return (int)Math.Floor((z - minZ) / Cell); }
        float X(int i) { return minX + (i % w + 0.5f) * Cell; }
        float Z(int i) { return minZ + (i / w + 0.5f) * Cell; }

        int Index(float x, float z)
        {
            int cx = Column(x), cz = Row(z);
            return cx < 0 || cz < 0 || cx >= w || cz >= h ? -1 : cz * w + cx;
        }

        bool Free(int i, bool ship) { return i >= 0 && !blocked[i] && water[i] == ship; }

        public bool Free(float x, float z, bool ship) { return Free(Index(x, z), ship); }

        /// <summary>Is the straight way free (both ends included)?</summary>
        public bool LineFree(float x0, float z0, float x1, float z1, bool ship)
        {
            float dx = x1 - x0, dz = z1 - z0;
            int n = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dz * dz) / (Cell * 0.25f)));
            for (int i = 0; i <= n; i++)
                if (!Free(x0 + dx * i / n, z0 + dz * i / n, ship)) return false;
            return true;
        }

        /// <summary>Centre of the free cell nearest to (x, z), of two similar ones the one nearer to (towardX, towardZ).</summary>
        public bool NearestFree(float x, float z, bool ship, float towardX, float towardZ, out float freeX, out float freeZ)
        {
            int i = NearestFree(x, z, ship, towardX, towardZ);
            freeX = i < 0 ? x : X(i); freeZ = i < 0 ? z : Z(i);
            return i >= 0;
        }

        int NearestFree(float x, float z, bool ship, float towardX, float towardZ)
        {
            int cx = Math.Min(w - 1, Math.Max(0, Column(x))), cz = Math.Min(h - 1, Math.Max(0, Row(z)));
            if (Free(cz * w + cx, ship)) return cz * w + cx;
            int best = -1; float bestValue = float.MaxValue, bestDistance = float.MaxValue;
            for (int r = 1; r < Math.Max(w, h) && (best < 0 || (r - 1) * Cell <= bestDistance); r++)
                for (int dz = -r; dz <= r; dz++)
                    for (int dx = -r; dx <= r; dx += Math.Abs(dz) == r ? 1 : 2 * r) // only the border of the square
                    {
                        int px = cx + dx, pz = cz + dz;
                        if (px < 0 || pz < 0 || px >= w || pz >= h) continue;
                        int i = pz * w + px;
                        if (!Free(i, ship)) continue;
                        float d = Distance(X(i) - x, Z(i) - z), value = d + 0.1f * Distance(X(i) - towardX, Z(i) - towardZ);
                        if (value < bestValue) { bestValue = value; bestDistance = d; best = i; }
                    }
            return best;
        }

        static float Distance(float dx, float dz) { return (float)Math.Sqrt(dx * dx + dz * dz); }

        /// <summary>
        /// Way points (x, z) from a to b without the start; empty when the straight way is free. When b itself is not free (the middle
        /// of a building, the other side of the coast) the last point is the free place next to it; when b cannot be reached, the
        /// reachable place nearest to it.
        /// </summary>
        public List<float[]> Find(float ax, float az, float bx, float bz, bool ship)
        {
            var path = new List<float[]>();
            if (LineFree(ax, az, bx, bz, ship)) return path;
            int start = NearestFree(ax, az, ship, bx, bz), goal = NearestFree(bx, bz, ship, ax, az);
            if (start < 0 || goal < 0) return path;
            int end = Search(start, goal, ship);

            var cells = new List<int>();
            for (int i = end; i >= 0; i = from[i]) cells.Add(i);
            cells.Reverse();
            // pull the string: keep only the corners
            float px = ax, pz = az;
            if (!Free(ax, az, ship)) { px = X(start); pz = Z(start); path.Add(new[] { px, pz }); } // first out of the building
            int k = 0;
            while (k < cells.Count - 1)
            {
                int next = k + 1;
                while (next + 1 < cells.Count && LineFree(px, pz, X(cells[next + 1]), Z(cells[next + 1]), ship)) next++;
                k = next;
                px = X(cells[k]); pz = Z(cells[k]);
                path.Add(new[] { px, pz });
            }
            if (path.Count == 0) path.Add(new[] { X(end), Z(end) });
            // the last cell centre is only a stand-in for a free goal: go to the goal itself
            if (end == goal && Free(bx, bz, ship)) path[path.Count - 1] = new[] { bx, bz };
            return path;
        }

        /// <summary>A* from start to goal; returns the goal or, when it cannot be reached, the visited cell nearest to it.</summary>
        int Search(int start, int goal, bool ship)
        {
            search++;
            heapCount = 0;
            int gx = goal % w, gz = goal / w, best = start, expanded = 0; float bestH = Estimate(start, gx, gz);
            cost[start] = 0; from[start] = -1; seen[start] = search;
            Push(start, bestH);
            while (heapCount > 0 && expanded < MaxExpanded)
            {
                int i = Pop();
                if (closed[i] == search) continue;
                closed[i] = search; expanded++;
                if (i == goal) return goal;
                float e = Estimate(i, gx, gz);
                if (e < bestH) { bestH = e; best = i; }
                int cx = i % w, cz = i / w;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = cx + dx, nz = cz + dz;
                        if (nx < 0 || nz < 0 || nx >= w || nz >= h) continue;
                        int n = nz * w + nx;
                        if (closed[n] == search || !Free(n, ship)) continue;
                        if (dx != 0 && dz != 0 && (!Free(cz * w + nx, ship) || !Free(nz * w + cx, ship))) continue; // no corner cutting
                        float c = cost[i] + (dx != 0 && dz != 0 ? Diagonal : 1f);
                        if (seen[n] == search && c >= cost[n]) continue;
                        seen[n] = search; cost[n] = c; from[n] = i;
                        Push(n, c + Estimate(n, gx, gz));
                    }
            }
            return best;
        }

        float Estimate(int i, int gx, int gz)
        {
            int dx = Math.Abs(i % w - gx), dz = Math.Abs(i / w - gz);
            return Math.Max(dx, dz) + (Diagonal - 1f) * Math.Min(dx, dz);
        }

        // binary heap of (estimated length, cell)
        float[] keys = new float[1024]; int[] items = new int[1024]; int heapCount;

        void Push(int item, float key)
        {
            if (heapCount == keys.Length) { Array.Resize(ref keys, heapCount * 2); Array.Resize(ref items, heapCount * 2); }
            int i = heapCount++;
            while (i > 0 && keys[(i - 1) / 2] > key) { keys[i] = keys[(i - 1) / 2]; items[i] = items[(i - 1) / 2]; i = (i - 1) / 2; }
            keys[i] = key; items[i] = item;
        }

        int Pop()
        {
            int top = items[0];
            float key = keys[--heapCount]; int item = items[heapCount], i = 0;
            while (true)
            {
                int c = 2 * i + 1;
                if (c >= heapCount) break;
                if (c + 1 < heapCount && keys[c + 1] < keys[c]) c++;
                if (keys[c] >= key) break;
                keys[i] = keys[c]; items[i] = items[c]; i = c;
            }
            keys[i] = key; items[i] = item;
            return top;
        }
    }
}
