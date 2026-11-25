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
    [SerializeField] float interval = 0.25f;

    const int Resolution = 128;        // texture pixels along the long side
    const byte Darkness = 165;         // fog where nothing is seen (0 = clear, 255 = black)
    const float Softness = 1.5f;       // metres over which the edge fades

    Texture2D tex; float[] wanted, shown; byte[] pixels; int w, h;
    float next;

    void Start()
    {
        GameEvents.Local.Clear(); // new match, new log
        w = Resolution; h = Mathf.Max(8, Mathf.RoundToInt(Resolution * mapSize.y / mapSize.x));
        tex = new Texture2D(w, h, TextureFormat.Alpha8, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        wanted = new float[w * h]; shown = new float[w * h]; pixels = new byte[w * h];
        for (int i = 0; i < pixels.Length; i++) { wanted[i] = shown[i] = 1f; pixels[i] = Darkness; } // dark until the local player exists
        tex.SetPixelData(pixels, 0); tex.Apply(false);

        // a box around the map; the shader projects the texture onto everything inside it (Resources: the shader is in every build)
        var shader = UnityEngine.Resources.Load<Shader>("FogOfWar");
        if (shader == null) { Debug.LogError("FogOfWar: shader Resources/FogOfWar not found, no fog is drawn."); return; }
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "FogBox";
        Destroy(box.GetComponent<Collider>()); // must not block raycasts
        box.transform.SetParent(transform, false);
        box.transform.position = new Vector3(mapCentre.x, 4f, mapCentre.y);
        box.transform.localScale = new Vector3(mapSize.x, 16f, mapSize.y);
        var mat = new Material(shader);
        mat.SetTexture("_FogTex", tex);
        mat.SetVector("_FogMap", new Vector4(mapCentre.x - mapSize.x * 0.5f, mapCentre.y - mapSize.y * 0.5f, 1f / mapSize.x, 1f / mapSize.y));
        var r = box.GetComponent<Renderer>();
        r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
    }

    /// <summary>Fades the picture towards what is seen now (it is recomputed only four times a second).</summary>
    void LateUpdate()
    {
        float k = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
        for (int i = 0; i < shown.Length; i++) { shown[i] += (wanted[i] - shown[i]) * k; pixels[i] = (byte)(shown[i] * Darkness); }
        tex.SetPixelData(pixels, 0);
        tex.Apply(false);
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

        if (renderers.Count > 4 * (NetworkUnit.All.Count + NetworkBuilding.All.Count) + 64) PruneDestroyed();
        Paint(me, all);
        ReportSightings(me, all, visibleIds);
    }

    // renderers per object, looked up once (this runs for every unit and building four times a second)
    readonly Dictionary<GameObject, Renderer[]> renderers = new Dictionary<GameObject, Renderer[]>();

    void SetVisible(GameObject go, bool visible)
    {
        if (!renderers.TryGetValue(go, out var rs)) { rs = go.GetComponentsInChildren<Renderer>(); renderers[go] = rs; }
        foreach (var r in rs) if (r != null) r.enabled = visible;
    }

    void PruneDestroyed()
    {
        var dead = new List<GameObject>();
        foreach (var go in renderers.Keys) if (go == null) dead.Add(go);
        foreach (var go in dead) renderers.Remove(go);
    }

    // only the picture (soft edges); what is hidden is decided by VisibilityService above
    void Paint(int me, List<MapObject> all)
    {
        var mine = all.FindAll(o => o.Owner == me && o.Los > 0);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float wx = mapCentre.x + ((x + 0.5f) / w - 0.5f) * mapSize.x, wz = mapCentre.y + ((y + 0.5f) / h - 0.5f) * mapSize.y;
                float inside = -1000f; // metres inside the nearest sight circle (negative: outside)
                foreach (var o in mine)
                {
                    float dx = wx - o.X, dz = wz - o.Z, reach = o.Los + Softness;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < reach * reach) inside = Mathf.Max(inside, o.Los - Mathf.Sqrt(d2));
                }
                wanted[y * w + x] = Mathf.SmoothStep(1f, 0f, (inside + Softness * 0.5f) / Softness);
            }
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
