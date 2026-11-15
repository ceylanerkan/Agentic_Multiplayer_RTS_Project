using Fusion;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// The Villager gather loop, run only on the host: walk to the node, gather (GatherState: 0.6/s, carry 10),
/// walk to the nearest own Town Center, deposit, and walk back. Movement goes through NetworkUnit.MoveTo.
/// </summary>
[RequireComponent(typeof(NetworkUnit))]
public class VillagerGatherer : NetworkBehaviour
{
    enum Phase { Idle, ToNode, Gathering, ToDeposit }

    const float NodeReach = 2.5f;      // how close to a node/Town Center counts as "there"
    const float DepositReach = 4.5f;   // Town Center is a big cube, so a larger reach
    const float FindNewNodeRadius = 30f;

    NetworkUnit unit;
    readonly GatherState bag = new GatherState();
    Phase phase = Phase.Idle;
    ResourceNode node;
    string resource;

    public override void Spawned() { unit = GetComponent<NetworkUnit>(); }

    /// <summary>Host only (called from CommandRelay after the ownership check).</summary>
    public void StartGather(ResourceNode target)
    {
        if (!HasStateAuthority || target == null) return;
        if (target.Resource != resource) bag.Deposit(); // switching resource: the old bag is lost, like in most RTS games
        node = target; resource = target.Resource;
        phase = Phase.ToNode;
        unit.MoveTo(target.transform.position);
    }

    /// <summary>Host only. A move command cancels gathering.</summary>
    public void Cancel() { phase = Phase.Idle; node = null; }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || phase == Phase.Idle) return;
        Vector3 pos = transform.position;

        switch (phase)
        {
            case Phase.ToNode:
                if (node == null) { FindOtherNodeOrStop(); break; }
                if (Flat(node.transform.position - pos) <= NodeReach) { unit.StopMoving(); phase = Phase.Gathering; }
                break;

            case Phase.Gathering:
                if (node == null) { FindOtherNodeOrStop(); break; }
                bool horseCollar = Owner() != null && Owner().HasTech("horse_collar");
                float taken = bag.Tick(Runner.DeltaTime, resource, node.Amount, horseCollar);
                node.Take(taken); // may despawn the node; the reference then becomes null on the next tick
                if (bag.Full || taken <= 0f) GoDeposit();
                break;

            case Phase.ToDeposit:
                var tc = NearestTownCenter(pos);
                if (tc == null) { phase = Phase.Idle; break; }
                if (Flat(tc.transform.position - pos) <= DepositReach)
                {
                    var st = Owner();
                    if (st != null) st.Add(resource, bag.Deposit());
                    if (node != null) { phase = Phase.ToNode; unit.MoveTo(node.transform.position); }
                    else FindOtherNodeOrStop();
                }
                break;
        }
    }

    void GoDeposit()
    {
        var tc = NearestTownCenter(transform.position);
        if (tc == null) { phase = Phase.Idle; return; }
        phase = Phase.ToDeposit;
        unit.MoveTo(tc.transform.position);
    }

    void FindOtherNodeOrStop()
    {
        if (bag.CarriedWhole > 0) { GoDeposit(); node = null; return; }
        node = ResourceNode.Nearest(transform.position, resource, FindNewNodeRadius);
        if (node == null) { phase = Phase.Idle; unit.StopMoving(); return; }
        phase = Phase.ToNode; unit.MoveTo(node.transform.position);
    }

    NetworkPlayerState Owner() { return GameSpawner.Instance != null ? GameSpawner.Instance.StateOf(unit.Owner) : null; }

    NetworkBuilding NearestTownCenter(Vector3 from)
    {
        NetworkBuilding best = null; float bestD = float.MaxValue;
        foreach (var b in NetworkBuilding.All)
        {
            if (b.Owner != unit.Owner || !b.Completed || b.TypeKey.ToString() != "town_center") continue;
            float d = (b.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = b; }
        }
        return best;
    }

    static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }
}
