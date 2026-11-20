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

    // phase lives only on the host; clients (P2's AI snapshot, idle-villager picks) read this networked copy
    [Networked] public NetworkBool BusyNet { get; set; }   // same public pattern as the other [Networked] properties

    public bool Busy { get { return HasStateAuthority ? phase != Phase.Idle : (bool)BusyNet; } }

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
        if (!HasStateAuthority) return;
        BusyNet = phase != Phase.Idle;   // one tick late at most, fine for the UI and the AI
        if (phase == Phase.Idle) return;
        Vector3 pos = transform.position;

        switch (phase)
        {
            case Phase.ToNode:
                if (!NodeAlive()) { node = null; FindOtherNodeOrStop(); break; }
                if (Flat(node.transform.position - pos) <= NodeReach) { unit.StopMoving(); phase = Phase.Gathering; }
                break;

            case Phase.Gathering:
                if (!NodeAlive()) { node = null; FindOtherNodeOrStop(); break; }
                bool horseCollar = Owner() != null && Owner().HasTech("horse_collar"); // +20% food, see TechEffects
                float taken = bag.Tick(Runner.DeltaTime, resource, node.Amount, horseCollar);
                Vector3 nodePos = node.transform.position;
                bool depleted = node.Take(taken); // despawns the node when empty: do not read it afterwards
                if (depleted) GameEvents.Report(unit.Owner, "RESOURCE_DEPLETED", nodePos, resource);
                if (bag.Full || taken <= 0f || depleted) { if (depleted) node = null; if (bag.CarriedWhole > 0) GoDeposit(); else FindOtherNodeOrStop(); }
                break;

            case Phase.ToBuild:
                if (!SiteAlive() || site.Completed) { phase = Phase.Idle; break; }
                if (Flat(site.transform.position - pos) <= DepositReach) { unit.StopMoving(); phase = Phase.Building; }
                break;

            case Phase.Building:
                if (!SiteAlive() || site.Completed) { phase = Phase.Idle; site = null; break; }
                site.AddBuildWork(Runner.DeltaTime); // each Villager adds real seconds of work
                break;

            case Phase.ToDeposit:
                var tc = NearestTownCenter(pos);
                if (tc == null) { phase = Phase.Idle; break; }
                if (Flat(tc.transform.position - pos) <= DepositReach)
                {
                    var st = Owner();
                    if (st != null) st.Add(resource, bag.Deposit());
                    if (NodeAlive()) { phase = Phase.ToNode; unit.MoveTo(node.transform.position); }
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

    // A destroyed site is despawned at once but only Destroyed by Unity at the end of the frame: until then `site != null`
    // is still true and reading its [Networked] values throws, so also check that its NetworkObject is still valid.
    // Another Villager may have emptied (despawned) the node earlier in this tick; Unity destroys it only at the end of the
    // frame, so "node == null" is still false and reading node.Amount would throw.
    bool NodeAlive() { return node != null && node.Object != null && node.Object.IsValid; }

    bool SiteAlive() { return site != null && site.Object != null && site.Object.IsValid; }

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
