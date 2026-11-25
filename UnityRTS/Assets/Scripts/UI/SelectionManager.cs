using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Local player's selection: left click selects one own unit or building, dragging draws a box that selects all
/// own units inside, Shift adds to the selection. Selected objects get a flat ring as highlight.
/// Purely local (no network): only commands are sent to the host.
/// </summary>
[DefaultExecutionOrder(-50)] // prune despawned units before SelectionPanel / UnitCommandInput read the list
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    const float DragThreshold = 6f; // pixels; below this a mouse-up counts as a click

    public readonly List<NetworkUnit> Units = new List<NetworkUnit>();
    public NetworkBuilding Building;  // at most one building, only when no unit is selected

    [SerializeField] Camera cam;
    [SerializeField] Color ringColor = new Color(0.2f, 1f, 0.3f);

    Vector2 dragStart;
    bool dragging;
    readonly Dictionary<GameObject, GameObject> rings = new Dictionary<GameObject, GameObject>();
    Material ringMaterial;

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default"); // fallback if URP shader is not found
        ringMaterial = new Material(sh) { color = ringColor };
    }

    void Update()
    {
        PruneDestroyed();
        var mouse = Mouse.current; var kb = Keyboard.current;
        if (mouse == null || cam == null || CommandRelay.Local == null) return;
        bool shift = kb != null && kb.shiftKey.isPressed;
        Vector2 pos = mouse.position.ReadValue();

        // a left click that places a building belongs to BuildPlacement (it may run before or after this script)
        if (mouse.leftButton.wasPressedThisFrame && !OverUi() && !BuildPlacement.OwnsClick) { dragStart = pos; dragging = true; }

        if (dragging && mouse.leftButton.wasReleasedThisFrame)
        {
            dragging = false;
            if ((pos - dragStart).magnitude < DragThreshold) ClickSelect(pos, shift);
            else BoxSelect(dragStart, pos, shift);
        }

        PruneDestroyed();
        UpdateRings();
    }

    void OnDestroy() { if (ringMaterial != null) Destroy(ringMaterial); }

    bool OverUi() { return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(); }

    PlayerRef Me { get { return CommandRelay.Local.Object.InputAuthority; } }

    void ClickSelect(Vector2 screen, bool add)
    {
        if (!add) Clear();
        if (!Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, 500f)) return;
        var unit = hit.collider.GetComponentInParent<NetworkUnit>();
        if (unit != null && unit.Owner == Me) { if (!Units.Contains(unit)) Units.Add(unit); Building = null; return; }
        var b = hit.collider.GetComponentInParent<NetworkBuilding>();
        if (b != null && b.Owner == Me && Units.Count == 0) Building = b;
    }

    void BoxSelect(Vector2 a, Vector2 b, bool add)
    {
        if (!add) Clear();
        var rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        foreach (var u in NetworkUnit.All)
        {
            if (u.Owner != Me || Units.Contains(u)) continue;
            Vector3 sp = cam.WorldToScreenPoint(u.transform.position);
            if (sp.z > 0 && rect.Contains(sp)) Units.Add(u);
        }
        if (Units.Count > 0) Building = null;
    }

    public void Clear() { Units.Clear(); Building = null; }

    void PruneDestroyed()
    {
        // Fusion despawns at once but Unity destroys at the end of the frame: also drop objects whose NetworkObject is no longer valid
        Units.RemoveAll(u => u == null || u.Object == null || !u.Object.IsValid);
        if (Building == null || Building.Object == null || !Building.Object.IsValid) Building = null;
    }

    // ---- highlight: one flat ring under each selected object ----
    void UpdateRings()
    {
        var wanted = new HashSet<GameObject>();
        foreach (var u in Units) wanted.Add(u.gameObject);
        if (Building != null) wanted.Add(Building.gameObject);

        var remove = new List<GameObject>();
        foreach (var kv in rings) if (kv.Key == null || !wanted.Contains(kv.Key)) remove.Add(kv.Key);
        foreach (var k in remove) { Destroy(rings[k]); rings.Remove(k); }

        foreach (var go in wanted)
        {
            if (!rings.TryGetValue(go, out var ring))
            {
                ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(ring.GetComponent<Collider>()); // a ring must never block raycasts
                ring.GetComponent<Renderer>().sharedMaterial = ringMaterial;
                // a little wider than the team disc of EntityVisuals (2.6 x radius), so a green border shows around it
                var col = go.GetComponent<Collider>();
                float radius = col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 1f;
                ring.transform.localScale = new Vector3(radius * 3.4f, 0.02f, radius * 3.4f);
                rings[go] = ring;
            }
            // at the feet, just below the team disc: the object's pivot is its centre, so a ring there was hidden inside the body
            Vector3 at = go.transform.position;
            ring.transform.position = new Vector3(at.x, RTS.Core.Rules.TerrainShape.Surface(at.x, at.z) + 0.09f, at.z);
        }
    }

    void OnGUI()
    {
        if (!dragging || Mouse.current == null) return;
        Vector2 now = Mouse.current.position.ReadValue();
        if ((now - dragStart).magnitude < DragThreshold) return;
        // GUI y axis points down, screen y axis points up.
        var r = Rect.MinMaxRect(Mathf.Min(dragStart.x, now.x), Screen.height - Mathf.Max(dragStart.y, now.y),
                                Mathf.Max(dragStart.x, now.x), Screen.height - Mathf.Min(dragStart.y, now.y));
        GUI.color = new Color(0.2f, 1f, 0.3f, 0.25f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
