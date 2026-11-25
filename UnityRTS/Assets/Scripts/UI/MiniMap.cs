using System.Collections.Generic;
using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The small map: ground colours from TerrainShape (computed once), the fog, resources, own objects blue, visible enemies red and the
/// outline of what the camera shows. A click or drag moves the camera, a right click sends the selected units there.
/// It shows only what the local player may see: hidden enemies have their renderers switched off by FogOfWar.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class MiniMap : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    const float MinX = -116f, MaxX = 116f, MinZ = -84f, MaxZ = 86f; // like FogOfWar
    const int W = 232, H = 170;
    public const float Aspect = (float)W / H;

    static readonly Color32 Own = new Color32(80, 150, 255, 255), Enemy = new Color32(255, 70, 55, 255), View = new Color32(255, 255, 255, 255),
                            Tree = new Color32(28, 66, 24, 255), Berry = new Color32(214, 70, 120, 255), Ore = new Color32(255, 205, 60, 255), Fish = new Color32(170, 205, 235, 255);

    Texture2D tex;
    Color32[] ground, px;
    int[] fogCell;
    float next;

    void Start()
    {
        tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        ground = new Color32[W * H]; px = new Color32[W * H];
        var heights = new float[W * H];
        for (int k = 0; k < heights.Length; k++) heights[k] = TerrainShape.Height(X(k), Z(k));
        for (int k = 0; k < heights.Length; k++) ground[k] = Ground(X(k), Z(k), heights[k], heights[k % W + 1 < W && k >= W ? k + 1 - W : k]);
        GetComponent<RawImage>().texture = tex;
    }

    void OnDestroy() { if (tex != null) Destroy(tex); }

    static float X(int k) { return MinX + (k % W + 0.5f) * (MaxX - MinX) / W; }
    static float Z(int k) { return MinZ + (k / W + 0.5f) * (MaxZ - MinZ) / H; }

    static Color Ground(float x, float z, float h, float beside)
    {
        float coast = TerrainShape.CoastZ(x);
        if (z > coast) return Color.Lerp(new Color(0.25f, 0.55f, 0.62f), new Color(0.09f, 0.25f, 0.42f), (z - coast) / 14f);
        if (TerrainShape.InRiver(x, z)) return new Color(0.22f, 0.48f, 0.58f);
        float foot = TerrainShape.Foot(x, z);
        if (foot > 0f) return h > 12f ? new Color(0.85f, 0.88f, 0.92f) : Color.Lerp(new Color(0.40f, 0.38f, 0.35f), new Color(0.52f, 0.50f, 0.48f), h / 12f);
        if (coast - z < 5f) return new Color(0.78f, 0.69f, 0.50f);
        float light = 1f + 0.10f * (h - 1f) + 0.18f * (h - beside);
        return new Color(0.36f, 0.46f, 0.21f) * Mathf.Clamp(light, 0.7f, 1.35f);
    }

    void Update()
    {
        if (Time.unscaledTime < next || ground == null) return;
        next = Time.unscaledTime + 0.1f;
        var fog = FogOfWar.Cells;
        if (fog != null && fogCell == null) { fogCell = new int[W * H]; for (int k = 0; k < fogCell.Length; k++) fogCell[k] = FogOfWar.CellAt(X(k), Z(k)); }
        for (int k = 0; k < px.Length; k++)
        {
            Color32 c = ground[k];
            int light = fog == null ? 136 : 256 - (int)(fog[fogCell[k]] * 120f);
            px[k] = new Color32((byte)(c.r * light >> 8), (byte)(c.g * light >> 8), (byte)(c.b * light >> 8), 255);
        }
        foreach (var n in ResourceNode.All)
            if (n.Object != null && n.Object.IsValid) Dot(n.transform.position, 1, n.InWater ? Fish : n.Resource == "wood" ? Tree : n.Resource == "gold" ? Ore : Berry);
        if (CommandRelay.Local != null && CommandRelay.Local.Object != null && CommandRelay.Local.Object.IsValid)
        {
            var me = CommandRelay.Local.Object.InputAuthority;
            foreach (var b in NetworkBuilding.All)
            {
                if (b.Object == null || !b.Object.IsValid || b.Owner != me && !Seen(b)) continue;
                string key = b.TypeKey.ToString();
                Dot(b.transform.position, BuildSite.IsWall(key) ? 1 : key == "town_center" ? 4 : 3, b.Owner == me ? Own : Enemy);
            }
            foreach (var u in NetworkUnit.All)
                if (u.Object != null && u.Object.IsValid && (u.Owner == me || Seen(u))) Dot(u.transform.position, 2, u.Owner == me ? Own : Enemy);
        }
        DrawView();
        tex.SetPixels32(px); tex.Apply(false);
    }

    static bool Seen(Component c) { var r = c.GetComponentInChildren<Renderer>(); return r == null || r.enabled; }

    void Dot(Vector3 p, int size, Color32 c)
    {
        int x = (int)((p.x - MinX) / (MaxX - MinX) * W) - size / 2, y = (int)((p.z - MinZ) / (MaxZ - MinZ) * H) - size / 2;
        for (int j = y; j < y + size; j++) for (int i = x; i < x + size; i++) if (i >= 0 && j >= 0 && i < W && j < H) px[j * W + i] = c;
    }

    void DrawView()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var corner = new Vector2[4];
        for (int k = 0; k < 4; k++)
        {
            var ray = cam.ViewportPointToRay(new Vector3(k == 0 || k == 3 ? 0 : 1, k < 2 ? 0 : 1, 0));
            float d = ray.direction.y < -0.02f ? ray.origin.y / -ray.direction.y : 400f;
            Vector3 p = ray.origin + ray.direction * Mathf.Min(d, 400f);
            corner[k] = new Vector2((p.x - MinX) / (MaxX - MinX) * W, (p.z - MinZ) / (MaxZ - MinZ) * H);
        }
        for (int k = 0; k < 4; k++)
        {
            Vector2 a = corner[k], b = corner[(k + 1) % 4];
            int n = Mathf.Min(600, Mathf.CeilToInt((b - a).magnitude));
            for (int i = 0; i <= n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, n == 0 ? 0 : (float)i / n);
                int x = (int)p.x, y = (int)p.y;
                if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = View;
            }
        }
    }

    public void OnPointerDown(PointerEventData e) { Use(e, true); }
    public void OnDrag(PointerEventData e) { Use(e, false); }

    void Use(PointerEventData e, bool down)
    {
        var rt = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var p)) return;
        var r = rt.rect;
        var at = new Vector3(Mathf.Lerp(MinX, MaxX, (p.x - r.xMin) / r.width), 0, Mathf.Lerp(MinZ, MaxZ, (p.y - r.yMin) / r.height));
        if (e.button == PointerEventData.InputButton.Left) RtsCamera.LookAt(at);
        else if (e.button == PointerEventData.InputButton.Right && down) UnitCommandInput.MoveSelected(at);
    }
}
