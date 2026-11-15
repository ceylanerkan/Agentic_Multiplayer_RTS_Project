using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A predefined place where a building can stand. A BUILD action only names a zone, so the host picks a free
/// slot in that zone. Coastal slots (next to a harbor) are the only valid places for a Dock.
/// Occupied is host-side bookkeeping: clients never place buildings themselves.
/// </summary>
public class BuildSlot : MonoBehaviour
{
    public static readonly List<BuildSlot> All = new List<BuildSlot>();

    public bool coastal;
    [HideInInspector] public bool occupied;

    public string Zone { get { return ZoneMap.Instance != null ? ZoneMap.Instance.ZoneAt(transform.position) : null; } }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    /// <summary>First free slot in the zone (for a Dock: a free coastal one), or null.</summary>
    public static BuildSlot FindFree(string zone, bool needCoastal)
    {
        foreach (var s in All)
            if (!s.occupied && s.Zone == zone && (!needCoastal || s.coastal)) return s;
        return null;
    }

    /// <summary>The free slot in the zone (coastal for a Dock) that is nearest to pos, or null.</summary>
    public static BuildSlot FindNear(string zone, bool needCoastal, Vector3 pos)
    {
        BuildSlot best = null; float bestD = float.MaxValue;
        foreach (var s in All)
        {
            if (s.occupied || s.Zone != zone || (needCoastal && !s.coastal)) continue;
            float d = (s.transform.position - pos).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }
        return best;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = occupied ? Color.red : (coastal ? Color.cyan : Color.yellow);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.1f, new Vector3(3, 0.2f, 3));
    }
}
