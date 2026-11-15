using Fusion;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// The Villager gather loop, run only on the host: walk to the node, gather (GatherState: 0.6/s, carry 10),
/// walk to the nearest own Town Center, deposit, and walk back. It also builds: walk to an unfinished building and
/// add work until it is complete. Movement goes through NetworkUnit.MoveTo.
/// </summary>
[RequireComponent(typeof(NetworkUnit))]
public class VillagerGatherer : NetworkBehaviour
{
    enum Phase { Idle, ToNode, Gathering, ToDeposit, ToBuild, Building }

    const float NodeReach = 2.5f;      // how close to a node/Town Center counts as "there"
    const float DepositReach = 4.5f;   // Town Center is a big cube, so a larger reach
    const float FindNewNodeRadius = 30f;

    NetworkUnit unit;
    readonly GatherState bag = new GatherState();
    Phase phase = Phase.Idle;
    ResourceNode node;
    NetworkBuilding site;   // building under construction
    string resource;

    public bool Busy { get { return phase != Phase.Idle; } }

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

    /// <summary>Host only: walk to an unfinished building and work on it until it is done.</summary>
    public void StartBuild(NetworkBuilding b)
    {
        if (!HasStateAuthority || b == null) return;
        site = b; node = null; phase = Phase.ToBuild;
        unit.MoveTo(b.transform.position);
    }

    /// <summary>Host only. A move command cancels gathering and building.</summary>
    public void Cancel() { phase = Phase.Idle; node = null; site = null; }

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

            case Phase.ToBuild:
                if (site == null || site.Completed) { phase = Phase.Idle; break; }
                if (Flat(site.transform.position - pos) <= DepositReach) { unit.StopMoving(); phase = Phase.Building; }
                break;

            case Phase.Building:
                if (site == null || site.Completed) { phase = Phase.Idle; site = null; break; }
                site.AddBuildWork(Runner.DeltaTime); // each Villager adds real seconds of work
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
