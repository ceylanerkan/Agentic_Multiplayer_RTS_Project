using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Building placement ghost. A UI button calls BeginPlacement("house"). A translucent cube follows the mouse; a building can
/// stand anywhere on land outside the enemy base where the ground is free, a Dock only on the shore (the ghost snaps to it).
/// Green = valid and affordable, red = invalid. Left click sends the BUILD command with the selected Villager;
/// right click or Esc cancels. The host checks everything again, so the ghost is only a convenience.
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
    }

    public void Cancel()
    {
        buildingKey = null;
        if (ghost != null) Destroy(ghost);
    }

    void Update()
    {
        if (!Active) return;
        var mouse = Mouse.current; var kb = Keyboard.current; var relay = CommandRelay.Local;
        if (mouse == null || cam == null || relay == null) return;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) { Cancel(); return; }
        if (mouse.rightButton.wasPressedThisFrame) { UsedClickFrame = Time.frameCount; Cancel(); return; }

        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!plane.Raycast(ray, out float dist)) return;
        Vector3 point = ray.GetPoint(dist);

        bool free = BuildPlaces.TryFind(buildingKey, null, point, relay.GetComponent<NetworkPlayerState>().Slot,
                                        buildingKey == "dock" ? ShoreSnapRadius : SnapRadius, BuildSite.Gap, relay.Object.InputAuthority, out target);
        bool ok = free && CanAfford(relay);
        ghost.transform.position = (free ? target : point) + Vector3.up * (ghost.transform.localScale.y * 0.5f);
        ghostMaterial.color = ok ? new Color(0.2f, 1f, 0.3f, 0.5f) : new Color(1f, 0.2f, 0.2f, 0.5f);

        if (ok && mouse.leftButton.wasPressedThisFrame && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            NetworkUnit builder = FirstSelectedVillager();
            if (builder == null) return; // a Villager must be selected to build
            relay.SendBuild(buildingKey, target, ZoneMap.Instance.ZoneAt(target), builder.Object.Id);
            UsedClickFrame = Time.frameCount;
            Cancel();
        }
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

    void OnDestroy() { Cancel(); if (ghostMaterial != null) Destroy(ghostMaterial); }
}
