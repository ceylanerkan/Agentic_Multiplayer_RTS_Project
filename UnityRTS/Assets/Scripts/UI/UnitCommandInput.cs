using System.Collections.Generic;
using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Right click with units selected:
///  - on a resource node: Villagers gather, other units just move there
///  - on an enemy unit or building: all selected units attack it
///  - on the ground: move, with a grid formation around the clicked point
/// Everything goes through CommandRelay (RPC to the host), the same path the AI commander uses later.
/// </summary>
public class UnitCommandInput : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] float formationSpacing = 1.8f;

    void Awake() { if (cam == null) cam = Camera.main; }

    void Update()
    {
        var mouse = Mouse.current;
        var sel = SelectionManager.Instance; var relay = CommandRelay.Local;
        if (mouse == null || cam == null || sel == null || relay == null) return;
        if (!mouse.rightButton.wasPressedThisFrame || sel.Units.Count == 0) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (!Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out var hit, 500f)) return;

        var node = hit.collider.GetComponentInParent<ResourceNode>();
        if (node != null) { GatherOrMove(sel.Units, node, relay); return; }

        var enemyUnit = hit.collider.GetComponentInParent<NetworkUnit>();
        if (enemyUnit != null && enemyUnit.Owner != relay.Object.InputAuthority)
        { foreach (var u in sel.Units) relay.SendAttack(u.Object.Id, enemyUnit.Object.Id); return; }

        var enemyBuilding = hit.collider.GetComponentInParent<NetworkBuilding>();
        if (enemyBuilding != null && enemyBuilding.Owner != relay.Object.InputAuthority)
        { foreach (var u in sel.Units) relay.SendAttack(u.Object.Id, enemyBuilding.Object.Id); return; }

        MoveInFormation(sel.Units, hit.point, relay);
    }

    void GatherOrMove(List<NetworkUnit> units, ResourceNode node, CommandRelay relay)
    {
        foreach (var u in units)
        {
            if (u.GetComponent<VillagerGatherer>() != null) relay.SendGather(u.Object.Id, node.Object.Id);
            else relay.SendMove(u.Object.Id, node.transform.position);
        }
    }

    void MoveInFormation(List<NetworkUnit> units, Vector3 point, CommandRelay relay)
    {
        var offsets = FormationLayout.Offsets(units.Count, formationSpacing);
        for (int i = 0; i < units.Count; i++)
            relay.SendMove(units[i].Id, point + new Vector3(offsets[i][0], 0, offsets[i][1]));
    }
}
