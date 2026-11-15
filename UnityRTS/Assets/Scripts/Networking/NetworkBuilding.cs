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

    public override void Spawned() { All.Add(this); }
    public override void Despawned(NetworkRunner runner, bool hasState) { All.Remove(this); }

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
        var st = GameSpawner.Instance.StateOf(Owner);
        if (st != null) st.RecalculateCapacity(); // a finished House raises the population capacity
    }

    /// <summary>Host only. A destroyed building frees its slot and population capacity.</summary>
    public void TakeDamage(float amount)
    {
        if (!HasStateAuthority || Hp <= 0) return;
        Hp -= amount;
        if (Hp > 0) return;
        Hp = 0;
        foreach (var slot in BuildSlot.All)
            if ((slot.transform.position - transform.position).sqrMagnitude < 1f) slot.occupied = false;
        var st = GameSpawner.Instance.StateOf(Owner);
        if (st != null) st.RecalculateCapacity(this); // this building is still in All until Despawned runs
        Runner.Despawn(Object);
    }

    // ---- production queue (host only) ----
    public string WhyNotQueue(string key, bool isResearch) { return queue.WhyNot(key, isResearch); }

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
            if (item.Key == "age_2") st.Age = 2;
        }
        else
        {
            // Population was reserved when the unit was queued, so the unit just appears next to the building.
            GameSpawner.Instance.SpawnUnit(Owner, item.Key, transform.position + new Vector3(3f, 0, 3f));
        }
    }

    void Mirror()
    {
        QueueCount = queue.Count;
        CurrentKey = queue.Current != null ? queue.Current.Key : "";
        CurrentIsResearch = queue.Current != null && queue.Current.IsResearch;
        CurrentProgress = queue.CurrentProgress;
    }
}
