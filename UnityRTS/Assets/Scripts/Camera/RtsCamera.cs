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

    void Update()
    {
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
