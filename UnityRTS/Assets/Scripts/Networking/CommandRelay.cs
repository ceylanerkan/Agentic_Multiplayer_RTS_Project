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
    public void SendBuild(string buildingKey, Vector3 pos, string zone) { RPC_Build(buildingKey, pos, zone); }
    public void SendTrain(string unitKey, NetworkId building) { RPC_Train(unitKey, building); }
    public void SendResearch(string techKey) { RPC_Research(techKey); }

    // ---- host side ----
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Move(NetworkId unitId, Vector3 pos)
    {
        var unit = FindOwned<NetworkUnit>(unitId);
        if (unit == null) return;
        var g = unit.GetComponent<VillagerGatherer>();
        if (g != null) g.Cancel(); // an explicit move stops gathering
        unit.MoveTo(pos);
    }

    // Stores the target; the combat loop that uses it comes on night 6.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Attack(NetworkId unitId, NetworkId targetId)
    {
        var unit = FindOwned<NetworkUnit>(unitId);
        if (unit == null || !Runner.TryFindObject(targetId, out var target)) return;
        var tu = target.GetComponent<NetworkUnit>(); var tb = target.GetComponent<NetworkBuilding>();
        PlayerRef owner = tu != null ? tu.Owner : (tb != null ? tb.Owner : PlayerRef.None);
        if (owner == PlayerRef.None || owner == Object.InputAuthority) return; // only enemy objects can be attacked
        var g = unit.GetComponent<VillagerGatherer>(); if (g != null) g.Cancel();
        unit.AttackTarget = targetId;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Gather(NetworkId unitId, NetworkId nodeId)
    {
        var unit = FindOwned<NetworkUnit>(unitId);
        var g = unit != null ? unit.GetComponent<VillagerGatherer>() : null; // only Villagers have a gatherer
        if (g == null || !Runner.TryFindObject(nodeId, out var nodeObj)) return;
        g.StartGather(nodeObj.GetComponent<ResourceNode>());
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Build(NetworkString<_32> buildingKey, Vector3 pos, NetworkString<_32> zone)
    {
        var a = new PlanAction { ActionType = "BUILD", BuildingType = buildingKey.ToString(), TargetZone = zone.ToString() };
        if (!validator.Validate(a, state.ToCore()).Ok) return;
        // The action only names a zone, so the host picks the place: a free BuildSlot in that zone.
        // (Without any slots in the scene the client's position is used, so early smoke tests still work.)
        if (BuildSlot.All.Count > 0)
        {
            var slot = BuildSlot.FindFree(a.TargetZone, a.BuildingType == "dock");
            if (slot == null) return;
            slot.occupied = true;
            pos = slot.transform.position;
        }
        state.Spend(GameDataProvider.Get().Buildings[a.BuildingType].Cost, 1);
        GameSpawner.Instance.SpawnBuilding(Object.InputAuthority, a.BuildingType, pos, false); // Villager construction comes later
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Train(NetworkString<_32> unitKey, NetworkId buildingId)
    {
        var b = FindOwned<NetworkBuilding>(buildingId);
        if (b == null || !b.Completed) return;
        var a = new PlanAction { ActionType = "TRAIN_UNIT", UnitType = unitKey.ToString() };
        if (!validator.Validate(a, state.ToCore()).Ok) return;
        state.Spend(GameDataProvider.Get().Units[a.UnitType].Cost, 1);
        state.PopUsed += 1;
        // Instant for now; the production queue with train time comes on night 5.
        GameSpawner.Instance.SpawnUnit(Object.InputAuthority, a.UnitType, b.transform.position + new Vector3(2, 0, 2));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Research(NetworkString<_32> techKey)
    {
        var a = new PlanAction { ActionType = "RESEARCH", TechType = techKey.ToString() };
        if (!validator.Validate(a, state.ToCore()).Ok) return;
        state.Spend(GameDataProvider.Get().Techs[a.TechType].Cost, 1);
        state.GrantTech(a.TechType); // research time and effects come on night 5/7
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
