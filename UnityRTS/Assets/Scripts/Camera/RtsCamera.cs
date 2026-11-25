using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// RTS camera on the Main Camera: WASD / arrow keys pan on the ground plane, mouse wheel zooms by moving the camera
/// along its own forward direction (so the viewing angle you set in the editor stays the same); over the mountains it is lifted.
/// Uses the Input System devices directly: the project has "Active Input Handling = Input System".
/// </summary>
public class RtsCamera : MonoBehaviour
{
    [SerializeField] float panSpeed = 30f;
    [SerializeField] float zoomSpeed = 4f;     // units per mouse-wheel "notch"
    [SerializeField] float minHeight = 8f, maxHeight = 60f;
    [SerializeField] Vector2 limitMin = new Vector2(-126, -104), limitMax = new Vector2(126, 70); // clamp on x/z: far enough to see the mountains

    bool focused;
    float height; // set by zooming; over high ground the camera is lifted above it and comes down again afterwards

    void Awake() { height = transform.position.y; }

    /// <summary>The lowest allowed height of the camera here: above the ground under it and before it, more over the mountains.</summary>
    static float Lowest(Vector3 p)
    {
        float ground = 0f;
        for (int dx = -6; dx <= 6; dx += 6)
            for (int dz = 0; dz <= 12; dz += 6) ground = Mathf.Max(ground, TerrainShape.Surface(p.x + dx, p.z + dz));
        return ground + Mathf.SmoothStep(5f, 19f, TerrainShape.Foot(p.x, p.z) / 20f);
    }

    /// <summary>Once, when the local player's Town Center exists: move the camera so that it looks at it (P2 would otherwise start at P1's base).</summary>
    void FocusOnOwnTownCenter()
    {
        if (focused || CommandRelay.Local == null) return;
        foreach (var b in NetworkBuilding.All)
        {
            if (b.Owner != CommandRelay.Local.Object.InputAuthority || b.TypeKey.ToString() != "town_center") continue;
            Vector3 fwd = transform.forward;
            if (fwd.y >= -0.01f) return; // camera does not look down: nothing sensible to compute
            float t = transform.position.y / -fwd.y;                     // distance along the view direction to the ground
            Vector3 lookAt = transform.position + fwd * t;               // where the camera looks now
            Vector3 offset = b.transform.position - lookAt; offset.y = 0;
            transform.position += offset;
            focused = true;
            return;
        }
    }

    void Update()
    {
        FocusOnOwnTownCenter();
        var kb = Keyboard.current; var mouse = Mouse.current;
        if (kb == null) return;

        Vector3 move = Vector3.zero;
        if (!TypingInTextField())   // WASD in the AI question field must not move the camera
        {
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.z += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.z -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
        }
        // Pan faster when zoomed out, like most RTS games.
        float scale = Mathf.Max(0.5f, height / 20f);
        Vector3 pos = transform.position + move.normalized * panSpeed * scale * Time.unscaledDeltaTime;

        if (mouse != null)
        {
            // One notch is 120 on older Input System / Windows setups and about 1 when the Input System normalises scrolling;
            // accept both, otherwise zoom silently does nothing on one of them.
            float raw = mouse.scroll.ReadValue().y;
            float wheel = Mathf.Abs(raw) > 10f ? raw / 120f : raw;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                Vector3 step = transform.forward * wheel * zoomSpeed;
                float lowest = Lowest(pos), wanted = Mathf.Max(height, lowest) + step.y;
                if (wanted >= Mathf.Max(minHeight, lowest) && wanted <= maxHeight) { pos += step; height = wanted; }
            }
        }
        pos.x = Mathf.Clamp(pos.x, limitMin.x, limitMax.x);
        pos.z = Mathf.Clamp(pos.z, limitMin.y, limitMax.y);
        pos.y = Mathf.Lerp(pos.y, Mathf.Max(height, Lowest(pos)), 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
        transform.position = pos;
    }

    static bool TypingInTextField()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null || es.currentSelectedGameObject == null) return false;
        var field = es.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>();
        return field != null && field.isFocused;
    }
}
