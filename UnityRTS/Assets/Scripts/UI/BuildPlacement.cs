using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Building placement ghost. A UI button calls BeginPlacement("house"). A translucent cube follows the mouse and
/// snaps to the nearest free BuildSlot of the right kind in the player's own base (a Dock needs a coastal slot).
/// Green = valid and affordable, red = invalid. Left click sends the BUILD command with the selected Villager;
/// right click or Esc cancels. The host checks everything again, so the ghost is only a convenience.
/// </summary>
public class BuildPlacement : MonoBehaviour
{
    public static BuildPlacement Instance { get; private set; }

    [SerializeField] Camera cam;
    [SerializeField] float snapRadius = 6f; // the mouse must be this close to a free slot for the ghost to be valid

    string buildingKey;
    GameObject ghost;
    Renderer ghostRenderer;
    Material ghostMaterial;
    BuildSlot target;

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
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            ghostMaterial = new Material(sh);
            MakeTransparent(ghostMaterial);
        }
        ghostRenderer.sharedMaterial = ghostMaterial;
    }

    public void Cancel()
    {
        buildingKey = null; target = null;
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

        string zone = relay.GetComponent<NetworkPlayerState>().Slot == 1 ? "P1Base" : "P2Base"; // own base only
        target = BuildSlot.FindNear(zone, buildingKey == "dock", point);
        bool near = target != null && (target.transform.position - point).magnitude <= snapRadius;
        bool ok = near && CanAfford(relay);
        ghost.transform.position = (near ? target.transform.position : point) + Vector3.up * (ghost.transform.localScale.y * 0.5f);
        ghostMaterial.color = ok ? new Color(0.2f, 1f, 0.3f, 0.5f) : new Color(1f, 0.2f, 0.2f, 0.5f);

        if (ok && mouse.leftButton.wasPressedThisFrame && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            NetworkUnit builder = FirstSelectedVillager();
            if (builder == null) return; // a Villager must be selected to build
            relay.SendBuild(buildingKey, target.transform.position, zone, builder.Object.Id);
            UsedClickFrame = Time.frameCount;
            Cancel();
        }
    }

    // URP Unlit is opaque by default, so the 0.5 alpha of the ghost colour was ignored and the ghost hid the slot.
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
        foreach (var u in sel.Units) if (u.GetComponent<VillagerGatherer>() != null) return u;
        return null;
    }

    void OnDestroy() { Cancel(); if (ghostMaterial != null) Destroy(ghostMaterial); }
}
