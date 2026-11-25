using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// What floats around an object: a team disc under every unit and building (blue = mine, red = enemy), an HP bar (when selected, hurt
/// or under construction) and a name label while the mouse is near ("Villager 25/25", "Wood 180").
/// Added at run time by NetworkUnit, NetworkBuilding and ResourceNode, so the existing prefabs need no editor work.
/// The disc and label are NOT children of the object: parents have non-uniform scales (buildings are flattened cubes)
/// that would stretch them. They follow the object in LateUpdate and copy its visibility, so fog of war still hides them.
/// </summary>
public class EntityVisuals : MonoBehaviour
{
    static readonly Color Mine = new Color(0.15f, 0.45f, 1f), Enemy = new Color(0.95f, 0.2f, 0.15f), Neutral = new Color(1f, 0.95f, 0.6f);

    NetworkUnit unit; NetworkBuilding building; ResourceNode node;
    Renderer body;            // the object's own renderer; fog of war turns it on and off
    Transform disc; Renderer discRenderer; Material discMat;
    TextMeshPro label;
    Transform glow; Renderer glowRenderer; Material glowMat;   // light of a building at night
    Transform bar, barFill; Renderer barBack, barFront; Material barMat;
    float labelHeight, nextText, barWidth, hover = 2.5f;
    bool wall;
    ModelLook look; float labelUp;
    int lastTeam = -1;        // 0 neutral, 1 mine, 2 enemy

    /// <summary>TMP's default font, or null. TMP_Settings itself throws when TMP Essential Resources were never imported.</summary>
    public static TMP_FontAsset DefaultFont()
    {
        return UnityEngine.Resources.Load<TMP_Settings>("TMP Settings") != null ? TMP_Settings.defaultFontAsset : null;
    }

    public static void Attach(GameObject go) { if (go.GetComponent<EntityVisuals>() == null) go.AddComponent<EntityVisuals>(); }

    void Start()
    {
        unit = GetComponent<NetworkUnit>(); building = GetComponent<NetworkBuilding>(); node = GetComponent<ResourceNode>();
        look = ModelLook.Attach(gameObject, unit, building, node);
        body = GetComponentInChildren<Renderer>(); // a model, Wall or tree has it on a child
        wall = building != null && building.Object != null && building.Object.IsValid && RTS.Core.Rules.BuildSite.IsWall(building.TypeKey.ToString());
        var col = GetComponent<Collider>();
        float halfHeight = col != null ? col.bounds.extents.y : 1f;
        float radius = col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 1f;
        labelHeight = halfHeight * 2f + 1.2f;
        labelUp = look != null && look.Height > 0f ? look.Height - halfHeight + 0.7f : labelHeight * 0.5f + 0.4f;

        hover = Mathf.Max(1.2f, radius * 1.25f);
        if (node == null) MakeBar(radius);
        if (node == null && !wall && !(look != null && (building != null || unit != null && unit.IsShip))) // roofs and sails show the team
        {
            var d = new GameObject(name + "_TeamRing", typeof(MeshFilter), typeof(MeshRenderer));
            d.GetComponent<MeshFilter>().sharedMesh = Ring();
            disc = d.transform;
            float size = look == null ? radius * 2.6f : look.Wide ? radius * 2.1f : radius * 1.5f;
            disc.localScale = new Vector3(size, 1f, size);
            discRenderer = d.GetComponent<Renderer>(); discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // copy the body's material so the shader (URP Lit) is one that is surely included in builds
            discMat = new Material(look != null ? GlbModel.Material("C_Disc", Color.white) : body != null ? body.sharedMaterial : discRenderer.sharedMaterial);
            discRenderer.sharedMaterial = discMat;
        }

        if (building != null && !wall) MakeGlow(radius);

        var l = new GameObject(name + "_Label");
        label = l.AddComponent<TextMeshPro>();
        var font = DefaultFont(); if (font != null) label.font = font; // an unset font gives a NullReferenceException
        label.fontSize = 9f;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(30f, 5f);  // wide enough that names never wrap
        label.outlineWidth = 0.25f; label.outlineColor = new Color32(0, 0, 0, 255);
        UpdateText();
    }

    void LateUpdate()
    {
        if (label == null) return;
        Vector3 p = transform.position;
        bool visible = body == null || body.enabled;

        if (disc != null)
        {
            disc.position = new Vector3(p.x, RTS.Core.Rules.TerrainShape.Surface(p.x, p.z) + 0.14f, p.z);   // on the ground; the selection ring lies just below
            discRenderer.enabled = visible;
        }
        label.transform.position = p + Vector3.up * labelUp;
        if (glow != null)
        {
            float n = building.Object != null && building.Object.IsValid && building.Completed ? Mathf.Clamp01(DayNight.Night01 * 2f - 0.7f) : 0f;
            glowRenderer.enabled = visible && n > 0.02f;
            glow.position = new Vector3(p.x, RTS.Core.Rules.TerrainShape.Surface(p.x, p.z) + 0.22f, p.z);
            glowMat.color = new Color(1f, 0.66f, 0.28f, n * (0.34f + 0.03f * Mathf.Sin(Time.time * 7f + p.x)));
        }
        var cam = Camera.main;
        if (cam != null) label.transform.rotation = cam.transform.rotation;   // billboard: always faces the camera
        bool near = visible && NearMouse(p, node != null ? HoverRadius : hover);
        label.GetComponent<Renderer>().enabled = near;
        if (bar != null) UpdateBar(p, visible, near, cam);

        int team = Team();
        if (team != lastTeam)
        {
            lastTeam = team;
            Color c = team == 1 ? Mine : team == 2 ? Enemy : Neutral;
            if (discMat != null) discMat.color = c;
            if (look != null && node == null) look.SetTeam(team == 2);
            label.color = Color.Lerp(c, Color.white, 0.55f);
        }
        if (Time.time >= nextText) { nextText = Time.time + 0.25f; UpdateText(); }
    }

    // ---- rings ----
    static Mesh ring;

    /// <summary>A flat ring, one metre across (team colour of units, selection).</summary>
    public static Mesh Ring()
    {
        if (ring != null) return ring;
        const int n = 48; const float inner = 0.41f, outer = 0.5f;
        var v = new Vector3[n * 2]; var normals = new Vector3[n * 2]; var t = new int[n * 6];
        for (int i = 0; i < n; i++)
        {
            float a = i * 2f * Mathf.PI / n; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            v[i * 2] = dir * inner; v[i * 2 + 1] = dir * outer; normals[i * 2] = normals[i * 2 + 1] = Vector3.up;
            int k = i * 2, m = (i + 1) % n * 2;
            t[i * 6] = k; t[i * 6 + 1] = m; t[i * 6 + 2] = k + 1; t[i * 6 + 3] = k + 1; t[i * 6 + 4] = m; t[i * 6 + 5] = m + 1;
        }
        ring = new Mesh { name = "Ring", hideFlags = HideFlags.DontSave, vertices = v, normals = normals, triangles = t };
        ring.RecalculateBounds();
        return ring;
    }

    // ---- light at night ----
    static Texture2D glowTexture;

    /// <summary>A soft round spot.</summary>
    public static Texture2D GlowTexture()
    {
        if (glowTexture != null) return glowTexture;
        const int n = 64;
        glowTexture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Clamp01(1f - new Vector2(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f).magnitude / (n * 0.5f));
                glowTexture.SetPixel(x, y, new Color(1, 1, 1, d * d));
            }
        glowTexture.Apply();
        return glowTexture;
    }

    void MakeGlow(float radius)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(go.GetComponent<Collider>());
        glow = go.transform; glow.rotation = Quaternion.Euler(90f, 0, 0); glow.localScale = Vector3.one * (radius * 5f + 3f);
        glowRenderer = go.GetComponent<Renderer>(); glowRenderer.enabled = false;
        glowMat = new Material(shader) { mainTexture = GlowTexture(), renderQueue = 3050 };
        glowRenderer.sharedMaterial = glowMat; glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ---- HP bar ----
    const float BarHeight = 0.17f;
    static Shader barShader; static Material barBackMat;

    void MakeBar(float radius)
    {
        if (barShader == null) barShader = Shader.Find("Sprites/Default");
        if (barShader == null) return;
        bar = new GameObject(name + "_Bar").transform;
        barWidth = wall ? 1.4f : Mathf.Clamp(radius * 1.9f, 1.3f, 4.2f);
        barMat = new Material(barShader) { renderQueue = 3250 }; // after the fog of war
        if (barBackMat == null) barBackMat = new Material(barShader) { color = new Color(0, 0, 0, 0.8f), renderQueue = 3240 }; // shared by all
        barBack = Quad(barBackMat, new Vector3(0, 0, 0.01f), new Vector3(barWidth + 0.08f, BarHeight + 0.08f, 1), out _);
        barFront = Quad(barMat, Vector3.zero, new Vector3(barWidth, BarHeight, 1), out barFill);
    }

    Renderer Quad(Material material, Vector3 at, Vector3 size, out Transform t)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(go.GetComponent<Collider>());
        t = go.transform; t.SetParent(bar, false); t.localPosition = at; t.localScale = size;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return r;
    }

    void UpdateBar(Vector3 p, bool visible, bool near, Camera cam)
    {
        float part = 1f; bool building100 = true, selected = false;
        var sel = SelectionManager.Instance;
        var data = GameDataProvider.Get();
        if (unit != null && unit.Object != null && unit.Object.IsValid && data.Units.TryGetValue(unit.TypeKey.ToString(), out var u))
        { part = unit.Hp / u.Hp; selected = sel != null && sel.Units.Contains(unit); }
        else if (building != null && building.Object != null && building.Object.IsValid && data.Buildings.TryGetValue(building.TypeKey.ToString(), out var b))
        { building100 = building.Completed; part = building100 ? building.Hp / b.Hp : building.BuildProgress; selected = sel != null && sel.Building == building; }
        bool show = visible && (near || selected || part < 0.995f);
        barBack.enabled = barFront.enabled = show;
        if (!show) return;
        bar.position = p + Vector3.up * (labelUp - 0.5f);
        if (cam != null) bar.rotation = cam.transform.rotation;
        part = Mathf.Clamp(part, 0.02f, 1f);
        barFill.localScale = new Vector3(barWidth * part, BarHeight, 1);
        barFill.localPosition = new Vector3(-barWidth * (1f - part) * 0.5f, 0, 0);
        barMat.color = !building100 ? UiKit.Gold : lastTeam == 2 ? UiKit.Red : UiKit.HpColour(part);
    }

    const float HoverRadius = 2.5f;
    static int mouseFrame = -1; static Vector3 mouseGround; static bool mouseValid;

    /// <summary>Is the mouse over the ground near this point? (The ray is computed once per frame for all labels.)</summary>
    static bool NearMouse(Vector3 p, float radius)
    {
        if (mouseFrame != Time.frameCount)
        {
            mouseFrame = Time.frameCount; mouseValid = false;
            var cam = Camera.main; var mouse = Mouse.current;
            if (cam != null && mouse != null)
            {
                var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                mouseValid = new Plane(Vector3.up, new Vector3(0, 1.5f, 0)).Raycast(ray, out float d);
                var events = UnityEngine.EventSystems.EventSystem.current;
                if (events != null && events.IsPointerOverGameObject()) mouseValid = false; // over a panel
                if (mouseValid) mouseGround = ray.GetPoint(d);
            }
        }
        float dx = p.x - mouseGround.x, dz = p.z - mouseGround.z;
        return mouseValid && dx * dx + dz * dz < radius * radius;
    }

    /// <summary>0 = resource or unknown yet, 1 = the local player's, 2 = the opponent's.</summary>
    int Team()
    {
        if (node != null || CommandRelay.Local == null) return 0;
        if ((unit != null && (unit.Object == null || !unit.Object.IsValid)) || (building != null && (building.Object == null || !building.Object.IsValid))) return lastTeam;
        var me = CommandRelay.Local.Object.InputAuthority;
        if (unit != null) return unit.Owner == me ? 1 : 2;
        if (building != null) return building.Owner == me ? 1 : 2;
        return 0;
    }

    void UpdateText()
    {
        var data = GameDataProvider.Get();
        if (unit != null && unit.Object != null && unit.Object.IsValid && data.Units.TryGetValue(unit.TypeKey.ToString(), out var u))
            label.text = Pretty(u.Key) + "\n<size=90%>" + Mathf.CeilToInt(unit.Hp) + "/" + u.Hp + "</size>";
        else if (building != null && building.Object != null && building.Object.IsValid && data.Buildings.TryGetValue(building.TypeKey.ToString(), out var b))
            label.text = Pretty(b.Key) + "\n<size=90%>" + (building.Completed ? Mathf.CeilToInt(building.Hp) + "/" + b.Hp
                                                                          : "building " + Mathf.RoundToInt(building.BuildProgress * 100f) + "%") + "</size>";
        else if (node != null && node.Object != null && node.Object.IsValid)
            label.text = (node.InWater ? "Fish" : Pretty(node.Resource)) + "\n<size=90%>" + Mathf.CeilToInt(node.Amount) + "</size>";
    }

    /// <summary>"raider_boat" -> "Raider Boat" (rule in RTS.Core, tested). Also used by SelectionPanel.</summary>
    public static string Pretty(string key) { return RTS.Core.Rules.DisplayNames.Of(key); }

    void OnDestroy()
    {
        if (disc != null) Destroy(disc.gameObject);
        if (bar != null) Destroy(bar.gameObject);
        if (glow != null) { Destroy(glow.gameObject); Destroy(glowMat); }
        if (barMat != null) Destroy(barMat);
        if (label != null) { Destroy(label.fontMaterial); Destroy(label.gameObject); }   // outlineWidth made a material copy
        if (discMat != null) Destroy(discMat);
    }
}
