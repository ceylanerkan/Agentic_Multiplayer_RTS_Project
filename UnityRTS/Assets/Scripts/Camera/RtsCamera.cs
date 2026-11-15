using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// RTS camera on the Main Camera: WASD / arrow keys pan on the ground plane, mouse wheel zooms by moving the camera
/// along its own forward direction (so the viewing angle you set in the editor stays the same).
/// Uses the Input System devices directly: the project has "Active Input Handling = Input System".
/// </summary>
public class RtsCamera : MonoBehaviour
{
    [SerializeField] float panSpeed = 30f;
    [SerializeField] float zoomSpeed = 4f;     // units per mouse-wheel "notch"
    [SerializeField] float minHeight = 8f, maxHeight = 60f;
    [SerializeField] Vector2 mapMin = new Vector2(-80, -60), mapMax = new Vector2(80, 70); // clamp on x/z

    bool focused;

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
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.z += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.z -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
        // Pan faster when zoomed out, like most RTS games.
        float scale = Mathf.Max(0.5f, transform.position.y / 20f);
        Vector3 pos = transform.position + move.normalized * panSpeed * scale * Time.unscaledDeltaTime;

        if (mouse != null)
        {
            float wheel = mouse.scroll.ReadValue().y / 120f; // 120 = one notch
            if (Mathf.Abs(wheel) > 0.01f)
            {
                Vector3 zoomed = pos + transform.forward * wheel * zoomSpeed;
                if (zoomed.y >= minHeight && zoomed.y <= maxHeight) pos = zoomed;
            }
        }
        pos.x = Mathf.Clamp(pos.x, mapMin.x, mapMax.x);
        pos.z = Mathf.Clamp(pos.z, mapMin.y, mapMax.y);
        transform.position = pos;
    }
}
