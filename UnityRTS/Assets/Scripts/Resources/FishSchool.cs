using UnityEngine;

/// <summary>Fish of a shoal swim in a circle (only a picture, no network state).</summary>
public class FishSchool : MonoBehaviour
{
    [SerializeField] float degreesPerSecond = 40f;

    void Update() { transform.Rotate(0, -degreesPerSecond * Time.deltaTime, 0); }
}
