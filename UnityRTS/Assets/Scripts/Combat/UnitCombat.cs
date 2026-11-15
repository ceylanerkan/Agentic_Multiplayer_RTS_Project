using Fusion;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Combat, host only. Put it on every unit prefab. Three kinds of targets:
///  1. the explicit target from the player's attack command (NetworkUnit.AttackTarget): walk to it, then hit it,
///  2. while idle, any enemy unit or building within reach (military units only, Villagers never start fights),
///  3. buildings are valid targets in both cases.
/// Damage comes from DamageCalculator (max(1, attack x counter multiplier - armor)); one hit per cooldown.
/// </summary>
[RequireComponent(typeof(NetworkUnit))]
public class UnitCombat : NetworkBehaviour
{
    NetworkUnit unit;
    VillagerGatherer gatherer;      // null for non-Villagers
    float cooldownLeft;
    NetworkUnit autoUnit;           // current auto-acquired targets (not networked: host only)
    NetworkBuilding autoBuilding;

    public override void Spawned()
    {
        unit = GetComponent<NetworkUnit>();
        gatherer = GetComponent<VillagerGatherer>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || unit.Hp <= 0) return;
        if (cooldownLeft > 0) cooldownLeft -= Runner.DeltaTime;

        var data = GameDataProvider.Get();
        var me = data.Units[unit.TypeKey.ToString()];

        // 1. explicit target
        NetworkUnit tu = null; NetworkBuilding tb = null;
        bool explicitTarget = unit.AttackTarget.IsValid;
        if (explicitTarget)
        {
            if (Runner.TryFindObject(unit.AttackTarget, out var obj)) { tu = obj.GetComponent<NetworkUnit>(); tb = obj.GetComponent<NetworkBuilding>(); }
            if (tu == null && tb == null) { unit.AttackTarget = default; explicitTarget = false; } // target is gone
        }

        // 2. idle military units pick the nearest enemy in reach
        if (!explicitTarget)
        {
            bool busy = unit.HasMoveTarget || (gatherer != null && gatherer.Busy);
            if (busy || unit.TypeKey.ToString() == "villager") return;
            FindAuto(me.Range, out tu, out tb);
            if (tu == null && tb == null) return;
        }

        Vector3 targetPos = tu != null ? tu.transform.position : tb.transform.position;
        float reach = me.Range + (tu != null ? GameConstants.UnitReach : GameConstants.BuildingReach);
        Vector3 d = targetPos - transform.position; d.y = 0;

        if (d.magnitude > reach)
        {
            if (explicitTarget) unit.MoveTo(targetPos); // chase; auto targets are only hit when already in reach
            return;
        }
        if (explicitTarget) unit.StopMoving();
        if (cooldownLeft > 0) return;
        cooldownLeft = GameConstants.AttackCooldown;
        Hit(me, tu, tb);
    }

    void Hit(DefRecord me, NetworkUnit tu, NetworkBuilding tb)
    {
        var data = GameDataProvider.Get();
        var owner = GameSpawner.Instance.StateOf(unit.Owner);
        float bonus = owner != null ? TechBonus.AttackBonus(me, owner) : 0f;
        if (tu != null)
        {
            tu.TakeDamage(DamageCalculator.Damage(me, data.Units[tu.TypeKey.ToString()], false, bonus));
            if (tu == null || tu.Hp <= 0) unit.AttackTarget = default;
        }
        else
        {
            tb.TakeDamage(DamageCalculator.Damage(me, data.Buildings[tb.TypeKey.ToString()], true, bonus));
            if (tb == null || tb.Hp <= 0) unit.AttackTarget = default;
        }
    }

    void FindAuto(float range, out NetworkUnit bestU, out NetworkBuilding bestB)
    {
        bestU = null; bestB = null; float bestD = float.MaxValue;
        Vector3 me = transform.position;
        foreach (var u in NetworkUnit.All)
        {
            if (u.Owner == unit.Owner || u.Hp <= 0) continue;
            float dist = Flat(u.transform.position - me);
            if (dist <= range + GameConstants.UnitReach && dist < bestD) { bestD = dist; bestU = u; bestB = null; }
        }
        if (bestU != null) return; // units are preferred over buildings
        foreach (var b in NetworkBuilding.All)
        {
            if (b.Owner == unit.Owner || b.Hp <= 0) continue;
            float dist = Flat(b.transform.position - me);
            if (dist <= range + GameConstants.BuildingReach && dist < bestD) { bestD = dist; bestB = b; }
        }
    }

    static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }
}
