using System.Collections.Generic;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Client-side fog of war for the local player (the network still sends everything: known limitation, see report).
/// Every 0.25 s: ask VisibilityService which objects the local player sees, hide the renderers of the others,
/// and paint a low-resolution dark overlay over the map where nothing is seen.
/// Add it to an empty GameObject in the Game scene and set the map centre and size.
/// </summary>
public class FogOfWar : MonoBehaviour
{
    [SerializeField] Vector2 mapCentre = Vector2.zero;
    [SerializeField] Vector2 mapSize = new Vector2(200, 160);
    [SerializeField] float overlayHeight = 6f;      // above the ground, below the camera
    [SerializeField] int resolution = 96;           // texture pixels along the long side
    [SerializeField] float interval = 0.25f;

    Texture2D tex; Color32[] pixels; int w, h;
    float next;
    GameObject quad;

    void Start()
    {
        GameEvents.Local.Clear(); // new match, new log
        float aspect = mapSize.y / mapSize.x;
        w = resolution; h = Mathf.Max(8, Mathf.RoundToInt(resolution * aspect));
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        pixels = new Color32[w * h];
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<Collider>()); // must not block raycasts
        quad.transform.SetParent(transform, false);
        quad.transform.position = new Vector3(mapCentre.x, overlayHeight, mapCentre.y);
        quad.transform.rotation = Quaternion.Euler(90, 0, 0);
        quad.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1);
        var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex }; // supports alpha without extra setup
        quad.GetComponent<Renderer>().material = mat;
    }

    void Update()
    {
        if (Time.unscaledTime < next || CommandRelay.Local == null) return;
        next = Time.unscaledTime + interval;
        int me = CommandRelay.Local.GetComponent<NetworkPlayerState>().Slot;
        if (me == 0) return;

        var all = VisibilityAdapter.Collect();
        var visibleIds = new HashSet<int>();
        foreach (var o in VisibilityService.FilterFor(me, all)) visibleIds.Add(o.Id);

        // Hide enemy renderers outside line of sight (own objects are always visible).
        foreach (var u in NetworkUnit.All) SetVisible(u.gameObject, visibleIds.Contains((int)u.Object.Id.Raw));
        foreach (var b in NetworkBuilding.All) SetVisible(b.gameObject, visibleIds.Contains((int)b.Object.Id.Raw));

        Paint(me, all);
        ReportSightings(me, all, visibleIds);
    }

    static void SetVisible(GameObject go, bool visible)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.enabled = visible;
    }

    void Paint(int me, List<MapObject> all)
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float wx = mapCentre.x + ((x + 0.5f) / w - 0.5f) * mapSize.x;
                float wz = mapCentre.y + ((y + 0.5f) / h - 0.5f) * mapSize.y;
                bool seen = VisibilityService.PointVisible(me, wx, wz, all);
                pixels[y * w + x] = seen ? new Color32(0, 0, 0, 0) : new Color32(0, 0, 0, 150);
            }
        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    // ENEMY_SIGHTED: when an enemy object comes into view, again only after it was out of sight for RearmSeconds.
    // (Re-arming at once flooded the event log when an enemy stood on the edge of the sight circle.)
    const float RearmSeconds = 10f;
    readonly Dictionary<int, float> lastSeen = new Dictionary<int, float>();

    void ReportSightings(int me, List<MapObject> all, HashSet<int> visibleIds)
    {
        float now = Time.time;
        foreach (var o in all)
        {
            if (o.Owner == me || !visibleIds.Contains(o.Id)) continue;
            bool isNew = !lastSeen.TryGetValue(o.Id, out float seen) || now - seen > RearmSeconds;
            lastSeen[o.Id] = now;
            if (!isNew) continue;
            var pos = new Vector3(o.X, 0, o.Z);
            string zone = ZoneMap.Instance != null ? ZoneMap.Instance.ZoneAt(pos) : null;
            GameEvents.Local.Add("ENEMY_SIGHTED", zone, "enemy " + o.Type, now);
        }
    }
}
