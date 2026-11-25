using UnityEngine;

/// <summary>
/// The fish of a shoal swim on their own (only a picture): each wanders around, turns to where it swims, beats its tail, and now and
/// then one leaps out of the water. Added by ModelLook.
/// </summary>
public class FishSwim : MonoBehaviour
{
    const float Radius = 1.7f, Leap = 0.55f;

    Transform[] fish; Renderer[] first;
    Vector3[] last; float[] yaw, seed;
    int leaper = -1; float leapStart, nextLeap;
    Transform splash; Material splashMaterial;

    public void Set(Transform[] fish)
    {
        this.fish = fish;
        int n = fish.Length;
        last = new Vector3[n]; yaw = new float[n]; seed = new float[n]; first = new Renderer[n];
        for (int i = 0; i < n; i++) { seed[i] = i * 17.3f + transform.position.x * 0.71f + transform.position.z * 0.37f; last[i] = Place(i, Time.time); first[i] = fish[i].GetComponentInChildren<Renderer>(); }
        nextLeap = Time.time + Random.Range(1f, 6f);
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        splash = new GameObject("Splash", typeof(MeshFilter), typeof(MeshRenderer)).transform;
        splash.SetParent(transform, false);
        splash.GetComponent<MeshFilter>().sharedMesh = EntityVisuals.Ring();
        splashMaterial = new Material(shader) { renderQueue = 2960, color = new Color(1, 1, 1, 0) };
        splash.GetComponent<Renderer>().sharedMaterial = splashMaterial;
        splash.gameObject.SetActive(false);
    }

    // a slow wandering path, different for every fish
    Vector3 Place(int i, float t)
    {
        float s = t * 0.16f;
        float x = Mathf.PerlinNoise(s + seed[i], seed[i] * 1.7f) - 0.5f, z = Mathf.PerlinNoise(seed[i] * 2.3f, s + seed[i] + 40f) - 0.5f;
        return new Vector3(x, 0f, z) * (2.6f * Radius);
    }

    void Update()
    {
        if (fish == null || fish.Length == 0 || first[0] == null || !first[0].enabled) return; // in the fog
        float t = Time.time;
        if (leaper < 0 && t > nextLeap) { leaper = Random.Range(0, fish.Length); leapStart = t; Splash(fish[leaper].localPosition); }
        for (int i = 0; i < fish.Length; i++)
        {
            Vector3 p = Place(i, t), d = p - last[i];
            last[i] = p;
            if (d.sqrMagnitude > 1e-9f) yaw[i] = Mathf.LerpAngle(yaw[i], Quaternion.LookRotation(d).eulerAngles.y, 1f - Mathf.Exp(-5f * Time.deltaTime));
            float beat = Mathf.Sin(t * 9f + seed[i]) * 9f, y = -0.10f + 0.05f * Mathf.Sin(t * 1.3f + seed[i]), nose = 0f;
            if (i == leaper)
            {
                float u = (t - leapStart) / Leap;
                if (u >= 1f) { leaper = -1; nextLeap = t + Random.Range(3f, 9f); Splash(p); }
                else { y += Mathf.Sin(u * Mathf.PI) * 0.85f; nose = Mathf.Lerp(-55f, 55f, u); p += Quaternion.Euler(0, yaw[i], 0) * Vector3.forward * ((u - 0.5f) * 0.8f); }
            }
            fish[i].localPosition = new Vector3(p.x, y, p.z);
            fish[i].localRotation = Quaternion.Euler(nose, yaw[i] + beat, 0);
        }
        if (splash != null && splash.gameObject.activeSelf)
        {
            float size = splash.localScale.x + Time.deltaTime * 1.6f, a = Mathf.Clamp01(1f - size / 1.5f);
            splash.localScale = new Vector3(size, 1f, size);
            splashMaterial.color = new Color(1, 1, 1, a * 0.6f);
            if (a <= 0f) splash.gameObject.SetActive(false);
        }
    }

    void Splash(Vector3 at)
    {
        if (splash == null) return;
        splash.gameObject.SetActive(true);
        splash.localPosition = new Vector3(at.x, 0.02f, at.z); splash.localScale = new Vector3(0.2f, 1f, 0.2f);
    }

    void OnDestroy() { if (splashMaterial != null) Destroy(splashMaterial); }
}
