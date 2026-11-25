using System.Collections.Generic;
using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Right click with units selected:
///  - on an own building under construction: Villagers help build it
///  - on a resource node: Villagers gather (Fishing Boats: fish), other units just move there
///  - on an enemy unit or building: all selected units attack it
///  - on the ground: move, with a grid formation around the clicked point
/// Everything goes through CommandRelay (RPC to the host), the same path the AI commander uses later.
/// </summary>
public class UnitCommandInput : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] float formationSpacing = 1.8f;

    static UnitCommandInput instance;

    void Awake() { if (cam == null) cam = Camera.main; instance = this; }

    /// <summary>A right click on the mini map.</summary>
    public static void MoveSelected(Vector3 point)
    {
        var sel = SelectionManager.Instance; var relay = CommandRelay.Local;
        if (instance == null || sel == null || relay == null || sel.Units.Count == 0 || BuildPlacement.OwnsClick) return;
        instance.MoveInFormation(sel.Units, point, relay);
    }

    void Update()
    {
        var mouse = Mouse.current;
        var sel = SelectionManager.Instance; var relay = CommandRelay.Local;
        if (mouse == null || cam == null || sel == null || relay == null) return;
        if (!mouse.rightButton.wasPressedThisFrame || sel.Units.Count == 0) return;
        if (BuildPlacement.OwnsClick) return; // right-click while placing only cancels the placement
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (!Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out var hit, 500f)) return;

        var node = hit.collider.GetComponentInParent<ResourceNode>();
        if (node != null) { GatherOrMove(sel.Units, node, relay); return; }

        // Own building under construction: Villagers help build it, other units just walk there.
        var site = hit.collider.GetComponentInParent<NetworkBuilding>();
        if (site != null && site.Owner == relay.Object.InputAuthority && !site.Completed)
        {
            foreach (var u in sel.Units)
            {
                if (u.CanBuild) relay.SendHelpBuild(u.Object.Id, site.Object.Id);
                else relay.SendMove(u.Object.Id, site.transform.position);
            }
            return;
        }

        var enemyUnit = hit.collider.GetComponentInParent<NetworkUnit>();
        if (enemyUnit != null && enemyUnit.Owner != relay.Object.InputAuthority && Seen(enemyUnit))
        { foreach (var u in sel.Units) relay.SendAttack(u.Object.Id, enemyUnit.Object.Id); return; }

        var enemyBuilding = hit.collider.GetComponentInParent<NetworkBuilding>();
        if (enemyBuilding != null && enemyBuilding.Owner != relay.Object.InputAuthority && Seen(enemyBuilding))
        { foreach (var u in sel.Units) relay.SendAttack(u.Object.Id, enemyBuilding.Object.Id); return; }

        MoveInFormation(sel.Units, new Vector3(hit.point.x, 0f, hit.point.z), relay); // ground height (the hit may be on a hidden enemy's top)
    }

    // FogOfWar only switches renderers off; the collider stays. Without this check a right-click on an empty-looking spot
    // would attack the hidden enemy there and reveal it, so a hidden enemy counts as ground.
    static bool Seen(Component target)
    {
        var r = target.GetComponentInChildren<Renderer>(); // a Wall has none on the object itself
        return r == null || r.enabled;
    }

    void GatherOrMove(List<NetworkUnit> units, ResourceNode node, CommandRelay relay)
    {
        foreach (var u in units)
        {
            if (u.GetComponent<VillagerGatherer>() != null && u.IsShip == node.InWater) relay.SendGather(u.Object.Id, node.Object.Id);
            else relay.SendMove(u.Object.Id, node.transform.position);
        }
    }

    void MoveInFormation(List<NetworkUnit> units, Vector3 point, CommandRelay relay)
    {
        var offsets = FormationLayout.Offsets(units.Count, formationSpacing);
        for (int i = 0; i < units.Count; i++)
            relay.SendMove(units[i].Object.Id, point + new Vector3(offsets[i][0], 0, offsets[i][1]));
    }
}
