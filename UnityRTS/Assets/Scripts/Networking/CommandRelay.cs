using Fusion;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// One per player (on the same object as NetworkPlayerState). The client calls the Send* methods; the RPC runs on the
/// host (InputAuthority -> StateAuthority) which checks ownership, cost, population and prerequisites with
/// GameActionValidator before changing anything. A client can therefore never cheat by editing its own state.
/// </summary>
[RequireComponent(typeof(NetworkPlayerState))]
public class CommandRelay : NetworkBehaviour
{
    NetworkPlayerState state;
    GameActionValidator validator;

    /// <summary>The CommandRelay of the local player (the one this peer has input authority over).</summary>
    public static CommandRelay Local { get; private set; }

    public override void Despawned(NetworkRunner runner, bool hasState) { if (Local == this) Local = null; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority) Local = this;
        state = GetComponent<NetworkPlayerState>();
        validator = new GameActionValidator(GameDataProvider.Get());
    }

    // ---- local API (called by the UI / AI approval flow) ----
    public void SendMove(NetworkId unit, Vector3 pos) { RPC_Move(unit, pos); }
    public void SendAttack(NetworkId unit, NetworkId target) { RPC_Attack(unit, target); }
    public void SendGather(NetworkId unit, NetworkId node) { RPC_Gather(unit, node); }
    public void SendBuild(string buildingKey, Vector3 pos, string zone, NetworkId builder) { RPC_Build(buildingKey, pos, zone, builder); }
    public void SendHelpBuild(NetworkId unit, NetworkId building) { RPC_HelpBuild(unit, building); }
    public void SendTrain(string unitKey, NetworkId building) { RPC_Train(unitKey, building); }
    public void SendResearch(string techKey, NetworkId building) { RPC_Research(techKey, building); }

    /// <summary>Host only: deliver an event to the player this relay belongs to (see GameEvents).</summary>
    public void SendEventToOwner(string type, string zone, string detail)
    {
        int t = System.Array.IndexOf(PlayerEventLog.TypeList, type);
        if (t < 0) return;
        if (detail != null && detail.Length > 31) detail = detail.Substring(0, 31); // NetworkString<_32> holds 31 characters
        RPC_Event(t, System.Array.IndexOf(ZoneLookup.AllZones, zone), detail ?? ""); // zone -1 = unknown
    }

    // Sends indices, not texts: a NetworkString<_128> alone would already exceed the 512 byte RPC limit.
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_Event(int typeIndex, int zoneIndex, NetworkString<_32> detail)
    {
        if (typeIndex < 0 || typeIndex >= PlayerEventLog.TypeList.Length) return;
        string zone = zoneIndex >= 0 && zoneIndex < ZoneLookup.AllZones.Length ? ZoneLookup.AllZones[zoneIndex] : null;
        GameEvents.Local.Add(PlayerEventLog.TypeList[typeIndex], zone, detail.ToString(), Time.time);
    }

    // ---- host side ----
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Move(NetworkId unitId, Vector3 pos)
    {
        var unit = FindOwned<NetworkUnit>(unitId);
        if (unit == null) return;
        var g = unit.GetComponent<VillagerGatherer>();
        if (g != null) g.Cancel(); // an explicit move stops gathering
        unit.AttackTarget = default; // and attacking
        unit.MoveTo(pos);
    }

    // Stores the target; UnitCombat walks to it and attacks.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Attack(NetworkId unitId, NetworkId targetId)
    {
        var unit = FindOwned<NetworkUnit>(unitId);
        if (unit == null || !Runner.TryFindObject(targetId, out var target)) return;
        var tu = target.GetComponent<NetworkUnit>(); var tb = target.GetComponent<NetworkBuilding>();
        PlayerRef owner = tu != null ? tu.Owner : (tb != null ? tb.Owner : PlayerRef.None);
        if (owner == PlayerRef.None || owner == Object.InputAuthority) return; // only enemy objects can be attacked
        var g = unit.GetComponent<VillagerGatherer>(); if (g != null) g.Cancel();
        unit.Attack(targetId);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Gather(NetworkId unitId, NetworkId nodeId)
    {
        var unit = FindOwned<NetworkUnit>(unitId);
        var g = unit != null ? unit.GetComponent<VillagerGatherer>() : null; // only Villagers have a gatherer
        if (g == null || !Runner.TryFindObject(nodeId, out var nodeObj)) return;
        unit.AttackTarget = default; // gathering replaces an attack order (UnitCombat would keep chasing the old target)
        g.StartGather(nodeObj.GetComponent<ResourceNode>());
    }

    const float HostSearchRadius = 12f;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Build(NetworkString<_32> buildingKey, Vector3 pos, NetworkString<_32> zone, NetworkId builderId)
    {
        var a = new PlanAction { ActionType = "BUILD", BuildingType = buildingKey.ToString(), TargetZone = zone.ToString() };
        if (!validator.Validate(a, state.ToCore()).Ok) return;
        // The host decides about the place: the requested one when it is free, otherwise (two orders for the same ground, or a
        // building hidden by the fog) the nearest free one in the validated zone, so a client cannot build in water, in the
        // enemy base or on top of something.
        if (!BuildPlaces.TryFind(a.BuildingType, a.TargetZone, pos, state.Slot, HostSearchRadius, BuildSite.Gap, null, out pos)) return;
        state.Spend(GameDataProvider.Get().Buildings[a.BuildingType].Cost, 1);
        var b = GameSpawner.Instance.SpawnBuilding(Object.InputAuthority, a.BuildingType, pos, false); // Villagers finish it
        var builder = FindOwned<NetworkUnit>(builderId);
        var g = builder != null ? builder.GetComponent<VillagerGatherer>() : null;
        if (g != null) { builder.AttackTarget = default; g.StartBuild(b); }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_HelpBuild(NetworkId unitId, NetworkId buildingId)
    {
        var unit = FindOwned<NetworkUnit>(unitId);
        var b = FindOwned<NetworkBuilding>(buildingId);
        var g = unit != null ? unit.GetComponent<VillagerGatherer>() : null;
        if (g != null && b != null && !b.Completed) { unit.AttackTarget = default; g.StartBuild(b); }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Train(NetworkString<_32> unitKey, NetworkId buildingId)
    {
        var b = FindOwned<NetworkBuilding>(buildingId);
        if (b == null || !b.Completed) return;
        var a = new PlanAction { ActionType = "TRAIN_UNIT", UnitType = unitKey.ToString() };
        if (!GameDataProvider.Get().Buildings[b.TypeKey.ToString()].Trains.Contains(a.UnitType)) return; // wrong building
        if (!validator.Validate(a, state.ToCore()).Ok) return;
        if (!b.Enqueue(a.UnitType, false)) return; // queue full or Town Center researching Age II
        state.Spend(GameDataProvider.Get().Units[a.UnitType].Cost, 1);
        state.PopUsed += 1; // reserved now, so a full queue cannot exceed the population cap
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Research(NetworkString<_32> techKey, NetworkId buildingId)
    {
        var b = FindOwned<NetworkBuilding>(buildingId);
        if (b == null || !b.Completed) return;
        var a = new PlanAction { ActionType = "RESEARCH", TechType = techKey.ToString() };
        if (!GameDataProvider.Get().Buildings[b.TypeKey.ToString()].Researches.Contains(a.TechType)) return;
        if (state.HasTech(a.TechType)) return; // already researched
        // the queue only rejects duplicates inside one building: with two Blacksmiths (or an AI plan approved twice)
        // the same technology would be queued and paid twice
        foreach (var other in NetworkBuilding.All)
            if (other.Owner == Object.InputAuthority && other.IsResearching(a.TechType)) return;
        if (!validator.Validate(a, state.ToCore()).Ok) return;
        if (!b.Enqueue(a.TechType, true)) return;
        state.Spend(GameDataProvider.Get().Techs[a.TechType].Cost, 1);
    }

    // The RPC sender is Object.InputAuthority; objects belonging to somebody else are ignored.
    T FindOwned<T>(NetworkId id) where T : NetworkBehaviour
    {
        if (!Runner.TryFindObject(id, out var obj)) return null;
        var t = obj.GetComponent<T>();
        if (t == null) return null;
        PlayerRef owner = t is NetworkUnit u ? u.Owner : ((NetworkBuilding)(object)t).Owner;
        return owner == Object.InputAuthority ? t : null;
    }
}
