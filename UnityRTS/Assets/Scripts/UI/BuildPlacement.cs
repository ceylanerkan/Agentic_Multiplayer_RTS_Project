using System.Collections.Generic;
using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Building placement ghost. A UI button calls BeginPlacement("house"). The model on a coloured plate follows the mouse; a building can
/// stand anywhere on land outside the enemy base where the ground is free, a Dock only on the shore (the ghost snaps to it).
/// Green = valid and affordable, red = invalid. Left click sends the BUILD command with the selected Villager;
/// right click or Esc cancels; with Shift the next one can be placed at once. A Wall is drawn: press, move, release; all selected
/// Villagers work on it. The host checks everything again, so the ghost is only a convenience.
/// </summary>
public class BuildPlacement : MonoBehaviour
{
    public static BuildPlacement Instance { get; private set; }

    [SerializeField] Camera cam;
    const float SnapRadius = 2f, ShoreSnapRadius = 8f; // how far the ghost may move from the mouse to a free place (Dock: to the shore)

    string buildingKey;
    GameObject ghost;
    Renderer ghostRenderer;
    Material ghostMaterial;
    Vector3 target;
    readonly List<GameObject> pieces = new List<GameObject>(); // ghosts of a wall
    Material badMaterial;
    bool drawing; Vector3 drawStart;
    float ghostHeight = 2f;
    readonly List<Material> ghostMaterials = new List<Material>(); readonly List<Color> ghostColours = new List<Color>(); // of the model, tinted green or red

    /// <summary>A line for the HUD while placing ("Wall: 12 pieces, 60 wood"), or null.</summary>
    public static string Hint { get; private set; }

    public bool Active { get { return buildingKey != null; } }
    /// <summary>Frame in which placement used a mouse click (place or right-click cancel), so SelectionManager and
    /// UnitCommandInput do not also treat that click as "deselect" or "move".</summary>
    public int UsedClickFrame { get; private set; } = -1;

    /// <summary>True while placing, and in the frame a click ended placement.</summary>
    public static bool OwnsClick { get { return Instance != null && (Instance.Active || Instance.UsedClickFrame == Time.frameCount); } }

    void Awake() { Instance = this; if (cam == null) cam = Camera.main; }

    public void BeginPlacement(string key)
    {
        Cancel();
        if (!GameDataProvider.Get().Buildings.ContainsKey(key) || !GameDataProvider.Get().Buildings[key].Buildable) return;
        buildingKey = key;
        ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(ghost.GetComponent<Collider>());
        // same footprint as the real building, so the player sees what will fit
        ghost.transform.localScale = GameSpawner.Instance != null ? GameSpawner.Instance.BuildingSize(key) : new Vector3(3f, 2f, 3f);
        ghostRenderer = ghost.GetComponent<Renderer>();
        if (ghostMaterial == null)   // one material for all placements (a new one each time leaked)
        {
            // Sprites/Default is transparent without keywords (like the fog overlay); a URP Unlit material switched to
            // Transparent at run time may lack that shader variant in a build, so it is only the fallback.
            Shader sh = Shader.Find("Sprites/Default");
            if (sh != null) ghostMaterial = new Material(sh);
            else { ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); MakeTransparent(ghostMaterial); }
        }
        ghostMaterial.renderQueue = 3200; // after the fog of war: the colour must stay clear
        ghostRenderer.sharedMaterial = ghostMaterial;
        if (key != "wall")
        {
            // the plate shows the ground it takes, the model what it will be (without a model: the cube)
            var model = GlbModel.Spawn(key == "gate" ? "gateframe" : key, null);
            ghostHeight = ghost.transform.localScale.y;
            if (model == null) return;
            ghostHeight = 0.1f;
            Vector3 size = ghost.transform.localScale; ghost.transform.localScale = new Vector3(size.x * 1.3f, ghostHeight, size.z * 1.3f);
            model.transform.SetParent(ghost.transform, false);
            model.transform.localScale = new Vector3(1f / (size.x * 1.3f), 1f / ghostHeight, 1f / (size.z * 1.3f));
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) { mats[i] = new Material(GlbModel.ForTeam(mats[i], false)); ghostMaterials.Add(mats[i]); ghostColours.Add(mats[i].color); }
                r.sharedMaterials = mats; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return;
        }
        ghost.SetActive(false);
        if (badMaterial == null) badMaterial = new Material(ghostMaterial);
        badMaterial.renderQueue = 3200; badMaterial.color = Bad; ghostMaterial.color = Good;
    }

    static readonly Color Good = new Color(0.2f, 1f, 0.3f, 0.5f), Bad = new Color(1f, 0.2f, 0.2f, 0.5f);

    public void Cancel()
    {
        buildingKey = null; drawing = false; Hint = null;
        if (ghost != null) Destroy(ghost);
        foreach (var m in ghostMaterials) Destroy(m);
        ghostMaterials.Clear(); ghostColours.Clear();
        foreach (var g in pieces) if (g != null) Destroy(g);
        pieces.Clear();
    }

    void Update()
    {
        if (!Active) return;
        var mouse = Mouse.current; var kb = Keyboard.current; var relay = CommandRelay.Local;
        if (mouse == null || cam == null || relay == null) return;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) { Cancel(); return; }
        if (mouse.rightButton.wasPressedThisFrame) { UsedClickFrame = Time.frameCount; Cancel(); return; }

        if (!GroundUnder(cam.ScreenPointToRay(mouse.position.ReadValue()), out Vector3 point)) return;

        if (buildingKey == "wall") { DrawWall(point, relay); return; }
        string name = EntityVisuals.Pretty(buildingKey);
        Hint = name + ":  left click to place,  Shift + click to place several,  right click to cancel";

        bool free = BuildPlaces.TryFind(buildingKey, null, point, relay.GetComponent<NetworkPlayerState>().Slot,
                                        buildingKey == "dock" ? ShoreSnapRadius : SnapRadius, BuildSite.Gap, relay.Object.InputAuthority, out target);
        bool ok = free && CanAfford(relay);
        Vector3 at = free ? target : BuildSite.IsWall(buildingKey) ? new Vector3(WallLine.Snap(point.x), 0, WallLine.Snap(point.z)) : point;
        ghost.transform.position = new Vector3(at.x, ghostHeight < 0.2f ? ModelLook.BaseHeight(buildingKey, at.x, at.z, ghost.transform.localScale.x * 0.38f, ghost.transform.localScale.z * 0.38f) + 0.06f : TerrainShape.Surface(at.x, at.z) + ghostHeight * 0.5f - 0.25f, at.z);
        ghostMaterial.color = ok ? Good : Bad;
        for (int i = 0; i < ghostMaterials.Count; i++) ghostMaterials[i].color = ghostColours[i] * (ok ? new Color(0.75f, 1.15f, 0.75f) : new Color(1.2f, 0.42f, 0.40f));

        if (ok && mouse.leftButton.wasPressedThisFrame && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            NetworkUnit builder = FirstSelectedVillager();
            if (builder == null) return; // a Villager must be selected to build
            relay.SendBuild(buildingKey, target, ZoneMap.Instance.ZoneAt(target), builder.Object.Id);
            UsedClickFrame = Time.frameCount;
            if (kb == null || !kb.shiftKey.isPressed) Cancel();
        }
    }

    void DrawWall(Vector3 point, CommandRelay relay)
    {
        var mouse = Mouse.current;
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!drawing && mouse.leftButton.wasPressedThisFrame && !overUi) { drawing = true; drawStart = point; UsedClickFrame = Time.frameCount; }

        var points = drawing ? WallLine.Points(drawStart.x, drawStart.z, point.x, point.z) : WallLine.Points(point.x, point.z, point.x, point.z);
        var state = relay.GetComponent<NetworkPlayerState>();
        var fits = BuildPlaces.WallFits(points, state.Slot, relay.Object.InputAuthority);
        int wood = GameDataProvider.Get().Buildings["wall"].Cost["wood"], count = 0;
        for (int i = 0; i < points.Count; i++)
        {
            bool ok = fits[i] && (count + 1) * wood <= state.Wood;
            if (ok) count++;
            if (pieces.Count <= i) pieces.Add(Piece());
            float x = points[i][0], z = points[i][1];
            pieces[i].SetActive(true);
            pieces[i].transform.position = new Vector3(x, TerrainShape.Surface(x, z) + 1.1f, z);
            pieces[i].GetComponent<Renderer>().sharedMaterial = ok ? ghostMaterial : badMaterial;
        }
        for (int i = points.Count; i < pieces.Count; i++) pieces[i].SetActive(false);
        Hint = drawing ? "Wall:  " + count + " pieces,  " + count * wood + " wood.  Release to build" : "Wall:  press, draw the line and release.  Right click to cancel";

        if (!drawing || !mouse.leftButton.wasReleasedThisFrame) return;
        UsedClickFrame = Time.frameCount;
        NetworkUnit builder = FirstSelectedVillager();
        if (builder != null && count > 0)
        {
            relay.SendWall(drawStart, point, builder.Object.Id);
            foreach (var u in SelectionManager.Instance.Units) if (u.CanBuild && u != builder) relay.SendWallWork(u.Object.Id, u.transform.position);
        }
        drawing = false;
        var kb = Keyboard.current;
        if (kb == null || !kb.shiftKey.isPressed) Cancel();
    }

    static GameObject Piece()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.name = "WallGhost"; go.transform.localScale = new Vector3(1.9f, 2.4f, 1.9f);
        return go;
    }

    /// <summary>Where the ray meets the hills (a few steps: meet the height of the last point again).</summary>
    static bool GroundUnder(Ray ray, out Vector3 point)
    {
        point = Vector3.zero;
        float y = 0f;
        for (int i = 0; i < 6; i++)
        {
            if (!new Plane(Vector3.up, new Vector3(0, y, 0)).Raycast(ray, out float d)) return false;
            point = ray.GetPoint(d);
            y = TerrainShape.Surface(point.x, point.z);
        }
        return true;
    }

    // URP Unlit is opaque by default, so the 0.5 alpha of the ghost colour was ignored and the ghost hid the ground.
    // These are the settings the URP material inspector writes for Surface Type = Transparent (harmless on Sprites/Default).
    static void MakeTransparent(Material m)
    {
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    bool CanAfford(CommandRelay relay)
    {
        var st = relay.GetComponent<NetworkPlayerState>();
        foreach (var kv in GameDataProvider.Get().Buildings[buildingKey].Cost) if (st.Get(kv.Key) < kv.Value) return false;
        return true;
    }

    static NetworkUnit FirstSelectedVillager()
    {
        var sel = SelectionManager.Instance;
        if (sel == null) return null;
        foreach (var u in sel.Units) if (u.CanBuild) return u;
        return null;
    }

    void OnDestroy() { Cancel(); if (ghostMaterial != null) Destroy(ghostMaterial); if (badMaterial != null) Destroy(badMaterial); }
}
