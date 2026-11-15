using UnityEngine;

/// <summary>
/// Marker for one of the 10 map zones. Put one empty GameObject per zone in the Game scene at the zone's centre.
/// ZoneMap uses the centres: a point belongs to the nearest centre, so every point has a zone.
/// </summary>
public class ZoneVolume : MonoBehaviour
{
    public enum Zone { P1Base, P2Base, NorthGate, SouthField, EastForest, WestMine, Center, P1Harbor, OpenSea, P2Harbor }

    public Zone zone;
    [SerializeField] float gizmoRadius = 12f;

    public string ZoneName { get { return zone.ToString(); } }

    void OnDrawGizmos()
    {
        Gizmos.color = RTS.Core.Rules.ZoneLookup.IsWater(ZoneName) ? new Color(0.2f, 0.5f, 1f, 0.6f) : new Color(0.2f, 0.9f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}
