using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// A finite tree, berry bush or gold mine. The host owns the remaining amount; clients only read it.
/// The node despawns itself when empty (rule_resources_v1).
/// </summary>
public class ResourceNode : NetworkBehaviour
{
    public static readonly List<ResourceNode> All = new List<ResourceNode>();

    [SerializeField] string resource = "wood";   // "wood" | "food" | "gold"
    [SerializeField] float startAmount = 200f;

    [Networked] public float Amount { get; set; }

    public string Resource { get { return resource; } }

    public override void Spawned()
    {
        All.Add(this); EntityVisuals.Attach(gameObject); BuildPlaces.Changed();
        if (HasStateAuthority) Amount = startAmount;
    }

    public override void Despawned(NetworkRunner runner, bool hasState) { All.Remove(this); BuildPlaces.Changed(); }

    /// <summary>Host only. Removes up to amount from the node; despawns it when it reaches zero.</summary>
    /// <returns>true when the node is now empty and was despawned (do not read it afterwards)</returns>
    public bool Take(float amount)
    {
        if (Object == null || !Object.IsValid) return true; // already emptied and despawned this tick: never despawn twice
        if (!HasStateAuthority) return false;
        Amount -= amount;
        if (Amount > 0.001f) return false;
        Amount = 0; Runner.Despawn(Object);
        return true;
    }

    public static ResourceNode Nearest(Vector3 from, string resource, float maxDistance)
    {
        ResourceNode best = null; float bestD = maxDistance * maxDistance;
        foreach (var n in All)
        {
            if (n.Resource != resource || n.Amount <= 0) continue;
            float d = (n.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = n; }
        }
        return best;
    }
}
