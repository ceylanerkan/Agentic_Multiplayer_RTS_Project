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
    [HideInInspector] public bool reserved;   // host: set at once when a BUILD command picks this slot

    /// <summary>
    /// True when the slot is reserved or a building stands on it. The building check uses the networked buildings, so it is
    /// also correct on clients (the build ghost and the Dock button use it); setting it writes the host-side reservation.
    /// </summary>
    public bool occupied
    {
        get
        {
            if (reserved) return true;
            foreach (var b in NetworkBuilding.All)
            {
                Vector3 d = b.transform.position - transform.position; d.y = 0;
                if (d.sqrMagnitude < 1f) return true;
            }
            return false;
        }
        set { reserved = value; }
    }

    public string Zone { get { return ZoneMap.Instance != null ? ZoneMap.Instance.ZoneAt(transform.position) : null; } }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    /// <summary>
    /// The free slot in the zone that is nearest to pos, or null. A Dock needs a coastal slot; every other building gets
    /// one only when no inland slot is free, otherwise a Barracks takes the place of the Dock.
    /// </summary>
    public static BuildSlot FindNear(string zone, bool needCoastal, Vector3 pos)
    {
        BuildSlot best = null; float bestD = float.MaxValue;
        foreach (var s in All)
        {
            if (s.occupied || s.Zone != zone || (needCoastal && !s.coastal)) continue;
            float d = (s.transform.position - pos).sqrMagnitude + (s.coastal && !needCoastal ? 1e6f : 0f);
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
