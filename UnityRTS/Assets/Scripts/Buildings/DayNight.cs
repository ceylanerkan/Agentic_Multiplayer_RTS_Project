using UnityEngine;

/// <summary>
/// Day and night (only to look at: sight, speed and all other rules stay the same). The time comes from the session clock, so both
/// players see the same hour. It turns the directional light (sun, at night the moon), sets its colour and the ambient light and gives
/// the shaders _RtsNight (0 day .. 1 night). GroundMesh adds it to the ground when the match starts.
/// </summary>
public class DayNight : MonoBehaviour
{
    public const float CycleSeconds = 420f;  // one day and one night
    const float DayPart = 0.68f, Start01 = 0.14f; // the match begins in the morning

    static DayNight instance;
    float time01 = Start01;
    Light sun;

    /// <summary>0 = day, 1 = night, between them dusk and dawn.</summary>
    public static float Night01 { get { return instance != null ? instance.night : 0f; } }
    float night;

    /// <summary>The hour of the day, 0..24 (sunrise at 6).</summary>
    public static float Hour
    {
        get
        {
            float t = instance != null ? instance.time01 : Start01;
            return t < DayPart ? 6f + 12f * t / DayPart : (18f + 12f * (t - DayPart) / (1f - DayPart)) % 24f;
        }
    }

    void Awake()
    {
        instance = this;
        sun = RenderSettings.sun;
        if (sun == null)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    }

    void OnDestroy() { if (instance == this) instance = null; Shader.SetGlobalFloat("_RtsNight", 0f); }

    void Update()
    {
        time01 = Mathf.Repeat(MatchSeconds() / CycleSeconds + Start01, 1f);
        Apply();
    }

    /// <summary>Seconds since the match began, the same for both players (player 1's base is the start).</summary>
    public static float MatchSeconds()
    {
        foreach (var s in NetworkPlayerState.All)
            if (s != null && s.Object != null && s.Object.IsValid && s.Slot == 1) return Mathf.Max(0f, s.Runner.SimulationTime - s.StartTime);
        return 0f;
    }

    void Apply()
    {
        bool day = time01 < DayPart;
        float u = day ? time01 / DayPart : (time01 - DayPart) / (1f - DayPart);            // 0..1 through the day or through the night
        float edge = Mathf.Min(u, 1f - u) * (day ? DayPart : 1f - DayPart) * CycleSeconds; // seconds to the nearest change
        float up = Mathf.SmoothStep(0f, 1f, edge / 16f);                                   // sun and moon fade out before they change places
        float low = 1f - Mathf.SmoothStep(0f, 1f, edge / 60f);                             // warm light near sunrise and sunset
        night = day ? 0.5f * (1f - Mathf.SmoothStep(0f, 1f, edge / 34f)) : 0.5f + 0.5f * Mathf.SmoothStep(0f, 1f, edge / 34f);

        if (sun != null)
        {
            if (day)
            {
                sun.transform.rotation = Quaternion.Euler(24f + 34f * Mathf.Sin(u * Mathf.PI), Mathf.Lerp(-92f, 38f, u), 0f);
                sun.color = Color.Lerp(new Color(1f, 0.957f, 0.839f), new Color(1f, 0.62f, 0.36f), low);
                sun.intensity = 1.05f * up;
            }
            else
            {
                sun.transform.rotation = Quaternion.Euler(40f + 12f * Mathf.Sin(u * Mathf.PI), Mathf.Lerp(-70f, 10f, u), 0f);
                sun.color = new Color(0.58f, 0.70f, 1f);
                sun.intensity = 0.42f * up;
            }
        }
        // never black: the players must still see their units
        Color sky = Color.Lerp(new Color(0.50f, 0.56f, 0.65f), new Color(0.26f, 0.32f, 0.50f), night), side = Color.Lerp(new Color(0.42f, 0.43f, 0.44f), new Color(0.20f, 0.24f, 0.38f), night),
              ground = Color.Lerp(new Color(0.24f, 0.22f, 0.19f), new Color(0.10f, 0.12f, 0.18f), night), glow = new Color(0.62f, 0.40f, 0.34f);
        float dusk = 0.35f * low * (day ? 1f : 0.4f);
        RenderSettings.ambientSkyColor = Color.Lerp(sky, glow, dusk); RenderSettings.ambientEquatorColor = Color.Lerp(side, glow, dusk); RenderSettings.ambientGroundColor = ground;
        Shader.SetGlobalFloat("_RtsNight", night);
    }
}
