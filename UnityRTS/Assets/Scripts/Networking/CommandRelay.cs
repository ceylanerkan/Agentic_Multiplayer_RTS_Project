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

    public override void Spawned()
    {
        state = GetComponent<NetworkPlayerState>();
        validator = new GameActionValidator(GameDataProvider.Get());
    }

    // ---- local API (called by the UI / AI approval flow) ----
    public void SendMove(NetworkId unit, Vector3 pos) { RPC_Move(unit, pos); }
    public void SendBuild(string buildingKey, Vector3 pos, string zone) { RPC_Build(buildingKey, pos, zone); }
    public void SendTrain(string unitKey, NetworkId building) { RPC_Train(unitKey, building); }
    public void SendResearch(string techKey) { RPC_Research(techKey); }

    // ---- host side ----
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Move(NetworkId unitId, Vector3 pos)
    {
        var unit = FindOwned<NetworkUnit>(unitId);
        if (unit != null) unit.MoveTo(pos);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_Build(NetworkString<_32> buildingKey, Vector3 pos, NetworkString<_32> zone)
    {
        var a = new PlanAction { ActionType = "BUILD", BuildingType = buildingKey.ToString(), TargetZone = zone.ToString() };
        if (!validator.Validate(a, state.ToCore()).Ok) return;
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
