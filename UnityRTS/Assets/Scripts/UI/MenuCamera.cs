using UnityEngine;

/// <summary>Menu scene: the camera circles slowly around the little landscape behind the menu.</summary>
public class MenuCamera : MonoBehaviour
{
    [SerializeField] Vector3 centre = new Vector3(0, 1, 0);
    [SerializeField] float distance = 34f, height = 15f, degreesPerSecond = 2.2f, swing = 28f;

    void Update()
    {
        // swings back and forth instead of a full circle, so the menu always looks from the good side
        float angle = Mathf.Sin(Time.unscaledTime * degreesPerSecond / swing) * swing;
        transform.position = centre + Quaternion.Euler(0, angle, 0) * new Vector3(0, height, -distance);
        transform.LookAt(centre + transform.right * -7f); // the landscape stands right of the menu panel
    }
}
