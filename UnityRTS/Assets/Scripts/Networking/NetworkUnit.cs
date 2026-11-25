using System.Collections.Generic;
using Fusion;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// A unit. The host owns its state and moves it; position is synchronised by the NetworkTransform on the same prefab.
/// It walks along the way points of PathGrid (around buildings and resources, land or water only); the last part goes straight to
/// the target, until the reach test of gathering, building or combat stops it.
/// </summary>
public class NetworkUnit : NetworkBehaviour
{
    public static readonly List<NetworkUnit> All = new List<NetworkUnit>();

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public NetworkString<_32> TypeKey { get; set; }
    [Networked] public float Hp { get; set; }
    [Networked] public Vector3 MoveTarget { get; set; }
    [Networked] public NetworkBool HasMoveTarget { get; set; }
    [Networked] public NetworkId AttackTarget { get; set; } // default (0) = none; used by combat on night 6


    public override void Spawned() { All.Add(this); EntityVisuals.Attach(gameObject); }
    public override void Despawned(NetworkRunner runner, bool hasState) { All.Remove(this); }

    public void InitHost(PlayerRef owner, string typeKey)
    {
        Owner = owner; TypeKey = typeKey;
        Hp = GameDataProvider.Get().Units[typeKey].Hp;
    }

    /// <summary>Host only: the last move was stopped by the land/water rule (UnitCombat then drops an unreachable target).</summary>
    public bool BlockedByShore { get; private set; }

    public void MoveTo(Vector3 target)
    {
        if (!HasMoveTarget) planned = false; // a chase calls this every tick: the path of a moving unit is kept
        MoveTarget = target; HasMoveTarget = true; BlockedByShore = false;
    }

    // ---- path (host only) ----
    const float ReplanDistance = 1f, ReplanSeconds = 0.5f;
    readonly List<Vector3> path = new List<Vector3>();
    int pathIndex, pathVersion;
    bool planned;
    float nextPlan;
    Vector3 pathTarget;

    /// <summary>The longest time one search took in this match (milliseconds), for the test report.</summary>
    public static float LongestSearchMs;

    void UpdatePath()
    {
        Vector3 moved = MoveTarget - pathTarget; moved.y = 0;
        bool newTarget = moved.magnitude > ReplanDistance && Runner.SimulationTime >= nextPlan; // an enemy that runs away
        bool newBuilding = pathVersion != BuildPlaces.Version && !PathFree();
        if (planned && !newTarget && !newBuilding) { pathVersion = BuildPlaces.Version; return; }
        var grid = BuildPlaces.Grid;
        planned = true; pathTarget = MoveTarget; pathVersion = BuildPlaces.Version; nextPlan = Runner.SimulationTime + ReplanSeconds;
        path.Clear(); pathIndex = 0;
        if (grid == null) return;
        float start = Time.realtimeSinceStartup;
        Vector3 pos = transform.position;
        foreach (var p in grid.Find(pos.x, pos.z, MoveTarget.x, MoveTarget.z, IsShip)) path.Add(new Vector3(p[0], pos.y, p[1]));
        LongestSearchMs = Mathf.Max(LongestSearchMs, (Time.realtimeSinceStartup - start) * 1000f);
    }

    // after a change of the map: is the rest of the way still free? (the last straight part may end inside a building)
    bool PathFree()
    {
        var grid = BuildPlaces.Grid;
        if (grid == null) return true;
        Vector3 a = transform.position;
        for (int i = pathIndex; i < path.Count; i++) { if (!grid.LineFree(a.x, a.z, path[i].x, path[i].z, IsShip)) return false; a = path[i]; }
        return pathIndex < path.Count || grid.LineFree(a.x, a.z, MoveTarget.x, MoveTarget.z, IsShip);
    }

    /// <summary>Only Villagers build; a Fishing Boat gathers too but never builds.</summary>
    public bool CanBuild { get { return TypeKey.ToString() == "villager"; } }

    public bool IsShip { get { return DomainRules.DomainOfUnit(GameDataProvider.Get().Units[TypeKey.ToString()]) == DomainRules.Water; } }

    /// <summary>Host only: a new attack order; a shore block from an older move must not cancel it.</summary>
    public void Attack(NetworkId target) { AttackTarget = target; BlockedByShore = false; }
    /// <summary>Host only. Death frees the population slot and removes the object.</summary>
    /// <returns>true when this hit killed the unit (do not read its networked values afterwards)</returns>
    public bool TakeDamage(float amount)
    {
        if (!HasStateAuthority || Hp <= 0) return false;
        Hp -= amount;
        GameEvents.Report(Owner, "UNDER_ATTACK", transform.position, TypeKey.ToString());
        if (Hp > 0) return false;
        Hp = 0;
        GameEvents.Report(Owner, "UNIT_LOST", transform.position, TypeKey.ToString());
        var st = GameSpawner.Instance.StateOf(Owner);
        if (st != null && st.PopUsed > 0) st.PopUsed -= 1;
        Runner.Despawn(Object);
        return true;
    }

    public void StopMoving() { HasMoveTarget = false; }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !HasMoveTarget) return;
        UpdatePath();
        Vector3 pos = transform.position;
        bool onPath = pathIndex < path.Count;
        Vector3 to = (onPath ? path[pathIndex] : MoveTarget) - pos; to.y = 0;
        float step = GameDataProvider.Get().Units[TypeKey.ToString()].Speed * Runner.DeltaTime; // speed from units.json
        if (to.magnitude <= step)
        {
            transform.position = pos + to;
            if (onPath) pathIndex++; else HasMoveTarget = false;
        }
        else
        {
            Vector3 next = pos + to.normalized * step;
            // Domain rule: land units never enter water zones, ships never enter land zones (the zone map decides); the path keeps
            // to it, this stops the last straight part at the shore.
            if (!CanStandAt(next) && !SlideAlongShore(pos, to.x, step, out next)) { HasMoveTarget = false; BlockedByShore = true; return; }
            transform.position = next;
        }
    }

    bool CanStandAt(Vector3 p)
    {
        return ZoneMap.Instance == null
            || DomainRules.CanStandIn(GameDataProvider.Get().Units[TypeKey.ToString()], ZoneLookup.IsWater(ZoneMap.Instance.ZoneAt(p)));
    }

    /// <summary>
    /// The shore cuts the straight way: go on parallel to it (the coast runs along x) until the target is straight ahead.
    /// Otherwise a Raider Boat could never reach a Dock, which stands on the land side, from the other end of the sea.
    /// </summary>
    bool SlideAlongShore(Vector3 pos, float dx, float step, out Vector3 next)
    {
        next = pos;
        if (Mathf.Abs(dx) <= step) return false;
        foreach (float away in new[] { 0f, 0.1f, -0.1f }) // a unit exactly on the shoreline may need a small step away from it
        {
            next = pos + new Vector3(Mathf.Sign(dx) * step, 0, away);
            if (CanStandAt(next)) return true;
        }
        return false;
    }
}
