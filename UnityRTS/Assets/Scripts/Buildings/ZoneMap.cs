using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Scene singleton: world position -> zone name (used by snapshots, events and MOVE_GROUP) and the reverse
/// (zone name -> centre position). Reads all ZoneVolume markers once at start-up.
/// </summary>
public class ZoneMap : MonoBehaviour
{
    public static ZoneMap Instance { get; private set; }

    ZoneLookup lookup;
    readonly System.Collections.Generic.Dictionary<string, Vector3> centres = new System.Collections.Generic.Dictionary<string, Vector3>();

    void Awake()
    {
        Instance = this;
        lookup = new ZoneLookup();
        foreach (var v in FindObjectsByType<ZoneVolume>())
        {
            Vector3 p = v.transform.position;
            lookup.Add(v.ZoneName, p.x, p.z);
            centres[v.ZoneName] = p;
        }
        if (!lookup.IsComplete())
            Debug.LogError("ZoneMap: the scene needs one ZoneVolume for each of the 10 zones (" + string.Join(", ", ZoneLookup.AllZones) + ").");
    }

    public string ZoneAt(Vector3 pos) { return lookup.ZoneAt(pos.x, pos.z); }

    public bool TryGetCentre(string zone, out Vector3 centre) { return centres.TryGetValue(zone, out centre); }
}
