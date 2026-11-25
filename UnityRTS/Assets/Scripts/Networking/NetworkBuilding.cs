using System.Collections.Generic;
using Fusion;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// A building. Host-owned state: owner, type, HP, construction progress and the production queue.
/// The queue itself (ProductionQueue) lives only on the host; clients read the small [Networked] summary
/// (QueueCount, CurrentKey, CurrentProgress) for the UI.
/// </summary>
public class NetworkBuilding : NetworkBehaviour
{
    public static readonly List<NetworkBuilding> All = new List<NetworkBuilding>();

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public NetworkString<_32> TypeKey { get; set; }
    [Networked] public float Hp { get; set; }
    [Networked] public NetworkBool Completed { get; set; }
    [Networked] public float BuildProgress { get; set; }                 // 0..1 while under construction
    [Networked] public int QueueCount { get; set; }
    [Networked] public NetworkString<_32> CurrentKey { get; set; }       // what the queue is working on
    [Networked] public NetworkBool CurrentIsResearch { get; set; }
    [Networked] public float CurrentProgress { get; set; }               // 0..1

    readonly ProductionQueue queue = new ProductionQueue();
    ConstructionState construction;

    public override void Spawned() { All.Add(this); EntityVisuals.Attach(gameObject); BuildPlaces.Changed(); }
    public override void Despawned(NetworkRunner runner, bool hasState) { All.Remove(this); BuildPlaces.Changed(); }

    /// <summary>Half the width of the building on the ground (from its collider), for placing units next to it.</summary>
    public float FootprintHalf()
    {
        var col = GetComponent<Collider>();
        return col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 3f;
    }

    /// <summary>Host only, right after Runner.Spawn.</summary>
    public void InitHost(PlayerRef owner, string typeKey, bool completed)
    {
        Owner = owner; TypeKey = typeKey; Completed = completed;
        var def = GameDataProvider.Get().Buildings[typeKey];
        Hp = def.Hp;
        construction = new ConstructionState(def.TimeSeconds);
        BuildProgress = completed ? 1f : 0f;
    }

    /// <summary>Host only: a Villager calls this every tick while it builds (seconds of work).</summary>
    public void AddBuildWork(float seconds)
    {
        if (!HasStateAuthority || Completed || construction == null) return;
        construction.AddWork(seconds);
        BuildProgress = construction.Progress;
        if (!construction.Done) return;
        Completed = true;
        GameEvents.Report(Owner, "BUILDING_COMPLETED", transform.position, TypeKey.ToString());
        var st = GameSpawner.Instance.StateOf(Owner);
        if (st != null) st.RecalculateCapacity(); // a finished House raises the population capacity
    }

    /// <summary>Host only. A destroyed building takes its population capacity with it.</summary>
    /// <returns>true when this hit destroyed the building</returns>
    public bool TakeDamage(float amount)
    {
        if (!HasStateAuthority || Hp <= 0) return false;
        Hp -= amount;
        GameEvents.Report(Owner, "UNDER_ATTACK", transform.position, TypeKey.ToString());
        if (Hp > 0) return false;
        Hp = 0;
        GameEvents.Report(Owner, "BUILDING_LOST", transform.position, TypeKey.ToString());
        var st = GameSpawner.Instance.StateOf(Owner);
        if (st != null) { st.PopUsed = Mathf.Max(0, st.PopUsed - queue.UnitCount); st.RecalculateCapacity(this); } // queued units never appear: free their reserved population // this building is still in All until Despawned runs
        bool wasTownCenter = TypeKey.ToString() == "town_center";
        Runner.Despawn(Object);
        if (wasTownCenter) MatchEnd.Check(this);
        return true;
    }

    // ---- production queue (host only) ----
    /// <summary>Why this building cannot queue the item, or null. The queue lives on the host; a client judges by the networked
    /// summary, which is enough for the button texts and the AI approval (the host checks again).</summary>
    public string WhyNotQueue(string key, bool isResearch)
    {
        if (HasStateAuthority) return queue.WhyNot(key, isResearch);
        if (QueueCount >= ProductionQueue.MaxLength) return "queue is full";
        if (QueueCount == 0) return null;
        string current = CurrentKey.ToString();
        if (!isResearch && CurrentIsResearch && current == ProductionQueue.BlockingResearch) return "cannot train while researching Age II";
        if (isResearch && key == ProductionQueue.BlockingResearch && !CurrentIsResearch) return "finish training before researching Age II";
        return null;
    }

    /// <summary>Is one of this player's buildings researching the technology now? The host would refuse it (paid twice).</summary>
    public static bool BeingResearched(string techKey, PlayerRef owner)
    {
        foreach (var b in All)
            if (b.Object != null && b.Object.IsValid && b.Owner == owner
                && (b.HasStateAuthority ? b.IsResearching(techKey) : b.CurrentIsResearch && b.CurrentKey.ToString() == techKey)) return true;
        return false;
    }

    /// <summary>Host only: is this technology already in this building's queue?</summary>
    public bool IsResearching(string techKey) { return queue.IsResearching(techKey); }

    public bool Enqueue(string key, bool isResearch)
    {
        var data = GameDataProvider.Get();
        float t = isResearch ? data.Techs[key].TimeSeconds : data.Units[key].TimeSeconds;
        bool ok = queue.Enqueue(key, isResearch, t);
        if (ok) Mirror();
        return ok;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !Completed || queue.Count == 0) return;
        var done = queue.Tick(Runner.DeltaTime);
        if (done != null) Finish(done);
        Mirror();
    }

    void Finish(ProductionItem item)
    {
        var st = GameSpawner.Instance.StateOf(Owner);
        if (item.IsResearch)
        {
            if (st == null) return;
            st.GrantTech(item.Key);
            GameEvents.Report(Owner, "TECH_RESEARCHED", transform.position, item.Key);
            if (item.Key == "age_2") st.Age = 2;
        }
        else
        {
            // Population was reserved when the unit was queued, so the unit just appears next to the building.
            GameSpawner.Instance.SpawnUnit(Owner, item.Key, FreeSpot(GameDataProvider.Get().Units[item.Key]));
            GameEvents.Report(Owner, "UNIT_TRAINED", transform.position, item.Key);
        }
    }

    static readonly Vector2[] Corners = { new Vector2(1, -1), new Vector2(-1, -1), new Vector2(1, 1), new Vector2(-1, 1) };

    /// <summary>
    /// Where a trained unit appears: next to a corner of the building (twelve spots each), on its own ground (a ship in the water
    /// before the Dock), not inside another building or unit.
    /// </summary>
    Vector3 FreeSpot(DefRecord unit)
    {
        bool ship = DomainRules.DomainOfUnit(unit) == DomainRules.Water;
        float clear = FootprintHalf() + 1.5f, step = ship ? 4f : 1.6f;
        foreach (var c in Corners)
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = transform.position + new Vector3(c.x * (clear + i / 4 * step), 0, c.y * (clear + i % 4 * step));
                if (!BuildPlaces.CanStand(unit, p)) continue;
                bool free = true;
                foreach (var u in NetworkUnit.All)
                {
                    Vector3 d = u.transform.position - p; d.y = 0;
                    if (d.magnitude < step * 0.6f) { free = false; break; }
                }
                if (free) return p;
            }
        return transform.position + new Vector3(clear, 0, ship ? clear + 3f : -clear); // everything is full
    }

    void Mirror()
    {
        QueueCount = queue.Count;
        CurrentKey = queue.Current != null ? queue.Current.Key : "";
        CurrentIsResearch = queue.Current != null && queue.Current.IsResearch;
        CurrentProgress = queue.CurrentProgress;
    }
}
