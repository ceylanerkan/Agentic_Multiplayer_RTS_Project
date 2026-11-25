using System.Collections.Generic;
using Fusion;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Unity side of BuildSite: collects the ground taken by buildings and resources and answers where a building fits.
/// The placement ghost, the AI approval and the host use the same test; the host decides.
/// </summary>
public static class BuildPlaces
{
    /// <summary>
    /// The free place nearest to pos (pos itself when it is free). zone = null: any zone where the player may build.
    /// viewer = the player who looks for a place: enemy buildings hidden by the fog are ignored, so a red ghost does not give
    /// them away. The host passes null and sees everything.
    /// </summary>
    public static bool TryFind(string key, string zone, Vector3 pos, int slot, float radius, float gap, PlayerRef? viewer, out Vector3 place)
    {
        place = pos;
        if (GameSpawner.Instance == null || ZoneMap.Instance == null) return false;
        Vector3 half = GameSpawner.Instance.BuildingSize(key) * 0.5f;
        var blocks = Blocks(viewer);
        bool found = BuildSite.FindNear(pos.x, pos.z, radius,
            (x, z) => (zone == null || ZoneAt(x, z) == zone) && BuildSite.WhyNot(key, x, z, half.x, half.z, slot, ZoneAt, blocks, gap) == null,
            out float fx, out float fz);
        if (found) place = new Vector3(fx, pos.y, fz);
        return found;
    }

    /// <summary>Can a unit of that domain appear here: inside the map, on its own ground, not inside a building or a resource.</summary>
    public static bool CanStand(DefRecord unit, Vector3 p)
    {
        if (Mathf.Abs(p.x) > BuildSite.MapHalfX || p.z < BuildSite.MapMinZ || p.z > BuildSite.MapMaxZ) return false;
        if (TerrainShape.InRiver(p.x, p.z) || TerrainShape.Rock(p.x, p.z)) return false; // nobody appears in the river or on a mountain
        if (ZoneMap.Instance != null && !DomainRules.CanStandIn(unit, ZoneLookup.IsWater(ZoneAt(p.x, p.z)))) return false;
        foreach (var b in Blocks(null))
            if (Mathf.Abs(p.x - b.X) < b.HalfX + 0.5f && Mathf.Abs(p.z - b.Z) < b.HalfZ + 0.5f) return false;
        return true;
    }

    // ---- pathfinding (host only) ----
    static PathGrid grid; static ZoneMap gridMap; static bool dirty;

    /// <summary>Counts the changes of buildings and resources: a unit on its way checks its path again.</summary>
    public static int Version { get; private set; }

    public static void Changed() { dirty = true; Version++; }

    public static PathGrid Grid
    {
        get
        {
            if (ZoneMap.Instance == null) return null;
            if (grid == null || gridMap != ZoneMap.Instance) // a new match
            {
                gridMap = ZoneMap.Instance; dirty = true;
                grid = new PathGrid(-BuildSite.MapHalfX, BuildSite.MapMinZ, BuildSite.MapHalfX, BuildSite.MapMaxZ, (x, z) => ZoneLookup.IsWater(ZoneAt(x, z)), TerrainShape.InRiver, TerrainShape.Rock);
            }
            if (dirty) { grid.SetBlocks(Blocks(null)); dirty = false; }
            return grid;
        }
    }

    /// <summary>A move order into a building or resource ends beside it.</summary>
    public static Vector3 BesideBuildings(Vector3 p, Vector3 from)
    {
        if (Grid == null) return p;
        foreach (var b in Blocks(null))
        {
            if (Mathf.Abs(p.x - b.X) >= b.HalfX || Mathf.Abs(p.z - b.Z) >= b.HalfZ) continue;
            bool water = ZoneLookup.IsWater(ZoneAt(p.x, p.z));
            return Grid.NearestFree(p.x, p.z, water, from.x, from.z, out float x, out float z) ? new Vector3(x, p.y, z) : p;
        }
        return p;
    }

    static string ZoneAt(float x, float z) { return ZoneMap.Instance.ZoneAt(new Vector3(x, 0, z)); }

    static List<Block> Blocks(PlayerRef? viewer)
    {
        var list = new List<Block>();
        foreach (var b in NetworkBuilding.All)
            if (viewer == null || b.Owner == viewer.Value || Visible(b)) list.Add(ToBlock(b));
        foreach (var n in ResourceNode.All) if (!n.InWater) list.Add(ToBlock(n)); // ships pass over fish
        return list;
    }

    static bool Visible(Component c) { var r = c.GetComponentInChildren<Renderer>(); return r == null || r.enabled; } // FogOfWar switches the renderers

    static Block ToBlock(Component c)
    {
        var col = c.GetComponentInChildren<Collider>();
        Vector3 half = col != null ? col.bounds.extents : Vector3.one, p = c.transform.position;
        return new Block(p.x, p.z, half.x, half.z);
    }
}
