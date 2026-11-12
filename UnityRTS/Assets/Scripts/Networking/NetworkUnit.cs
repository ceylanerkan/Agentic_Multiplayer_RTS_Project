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

    const float MoveSpeed = 3.5f; // placeholder until unit speeds are in the knowledge JSON

    public override void Spawned() { All.Add(this); }
    public override void Despawned(NetworkRunner runner, bool hasState) { All.Remove(this); }

    public void InitHost(PlayerRef owner, string typeKey)
    {
        Owner = owner; TypeKey = typeKey;
        Hp = GameDataProvider.Get().Units[typeKey].Hp;
    }

    public void MoveTo(Vector3 target) { MoveTarget = target; HasMoveTarget = true; }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !HasMoveTarget) return;
        Vector3 pos = transform.position;
        Vector3 to = MoveTarget - pos; to.y = 0;
        float step = MoveSpeed * Runner.DeltaTime;
        if (to.magnitude <= step) { transform.position = new Vector3(MoveTarget.x, pos.y, MoveTarget.z); HasMoveTarget = false; }
        else transform.position = pos + to.normalized * step;
    }
}
