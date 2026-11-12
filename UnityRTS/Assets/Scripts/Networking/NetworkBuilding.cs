using System.Collections.Generic;
using Fusion;

/// <summary>A building. Host-owned state: owner, type, HP, and whether construction is finished.</summary>
public class NetworkBuilding : NetworkBehaviour
{
    public static readonly List<NetworkBuilding> All = new List<NetworkBuilding>();

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public NetworkString<_32> TypeKey { get; set; }
    [Networked] public float Hp { get; set; }
    [Networked] public NetworkBool Completed { get; set; }

    public override void Spawned() { All.Add(this); }
    public override void Despawned(NetworkRunner runner, bool hasState) { All.Remove(this); }

    /// <summary>Host only, right after Runner.Spawn.</summary>
    public void InitHost(PlayerRef owner, string typeKey, bool completed)
    {
        Owner = owner; TypeKey = typeKey; Completed = completed;
        Hp = GameDataProvider.Get().Buildings[typeKey].Hp;
    }
}
