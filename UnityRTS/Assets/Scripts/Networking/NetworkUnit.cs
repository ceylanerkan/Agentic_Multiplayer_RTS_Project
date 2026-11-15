using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// A unit. The host owns its state and moves it; position is synchronised by the NetworkTransform on the same prefab.
/// Movement here is a straight line; NavMeshAgent pathing replaces it once the map is baked.
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


    public override void Spawned() { All.Add(this); }
    public override void Despawned(NetworkRunner runner, bool hasState) { All.Remove(this); }

    public void InitHost(PlayerRef owner, string typeKey)
    {
        Owner = owner; TypeKey = typeKey;
        Hp = GameDataProvider.Get().Units[typeKey].Hp;
    }

    public void MoveTo(Vector3 target) { MoveTarget = target; HasMoveTarget = true; }
    /// <summary>Host only. Death frees the population slot and removes the object.</summary>
    public void TakeDamage(float amount)
    {
        if (!HasStateAuthority || Hp <= 0) return;
        Hp -= amount;
        GameEvents.Report(Owner, "UNDER_ATTACK", transform.position, TypeKey.ToString());
        if (Hp > 0) return;
        Hp = 0;
        GameEvents.Report(Owner, "UNIT_LOST", transform.position, TypeKey.ToString());
        var st = GameSpawner.Instance.StateOf(Owner);
        if (st != null && st.PopUsed > 0) st.PopUsed -= 1;
        Runner.Despawn(Object);
    }

    public void StopMoving() { HasMoveTarget = false; }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !HasMoveTarget) return;
        Vector3 pos = transform.position;
        Vector3 to = MoveTarget - pos; to.y = 0;
        float step = GameDataProvider.Get().Units[TypeKey.ToString()].Speed * Runner.DeltaTime; // speed from units.json
        if (to.magnitude <= step) { transform.position = new Vector3(MoveTarget.x, pos.y, MoveTarget.z); HasMoveTarget = false; }
        else transform.position = pos + to.normalized * step;
    }
}
