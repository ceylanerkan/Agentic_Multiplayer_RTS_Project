using TMPro;
using UnityEngine;

/// <summary>
/// Makes placeholder objects easy to tell apart (UNITY_FEEDBACK 2026-11-18): a team disc under every unit and building
/// (blue = mine, red = enemy) and a floating name label ("Villager 25/25", "House 40%", "Wood 180").
/// Added at run time by NetworkUnit, NetworkBuilding and ResourceNode, so the existing prefabs need no editor work.
/// The disc and label are NOT children of the object: parents have non-uniform scales (buildings are flattened cubes)
/// that would stretch them. They follow the object in LateUpdate and copy its visibility, so fog of war still hides them.
/// </summary>
public class EntityVisuals : MonoBehaviour
{
    static readonly Color Mine = new Color(0.15f, 0.45f, 1f), Enemy = new Color(0.95f, 0.2f, 0.15f), Neutral = new Color(1f, 0.95f, 0.6f);

    NetworkUnit unit; NetworkBuilding building; ResourceNode node;
    Renderer body;            // the object's own renderer; fog of war turns it on and off
    Transform disc; Renderer discRenderer; Material discMat;
    TextMeshPro label;
    float labelHeight, nextText;
    int lastTeam = -1;        // 0 neutral, 1 mine, 2 enemy

    public static void Attach(GameObject go) { if (go.GetComponent<EntityVisuals>() == null) go.AddComponent<EntityVisuals>(); }

    void Start()
    {
        unit = GetComponent<NetworkUnit>(); building = GetComponent<NetworkBuilding>(); node = GetComponent<ResourceNode>();
        body = GetComponent<Renderer>();
        var col = GetComponent<Collider>();
        float halfHeight = col != null ? col.bounds.extents.y : 1f;
        float radius = col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 1f;
        labelHeight = halfHeight * 2f + 1.2f;

        if (node == null)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            d.name = name + "_TeamDisc";
            Destroy(d.GetComponent<Collider>());   // must not catch selection or right-click raycasts
            disc = d.transform;
            disc.localScale = new Vector3(radius * 2.6f, 0.03f, radius * 2.6f);
            discRenderer = d.GetComponent<Renderer>();
            // copy the body's material so the shader (URP Lit) is one that is surely included in builds
            discMat = new Material(body != null ? body.sharedMaterial : discRenderer.sharedMaterial);
            discRenderer.sharedMaterial = discMat;
        }

        var l = new GameObject(name + "_Label");
        label = l.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset; // an unset font gives a NullReferenceException
        label.fontSize = building != null ? 11f : 8f;   // about 25-35 px high at the default camera distance
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(30f, 5f);  // wide enough that names never wrap
        label.outlineWidth = 0.25f; label.outlineColor = new Color32(0, 0, 0, 255);
        UpdateText();
    }

    void LateUpdate()
    {
        if (label == null) return;
        Vector3 p = transform.position;
        bool visible = body == null || body.enabled;

        if (disc != null)
        {
            disc.position = new Vector3(p.x, p.y - (labelHeight - 1.2f) * 0.5f + 0.06f, p.z);   // feet; the selection ring lies just below
            discRenderer.enabled = visible;
        }
        label.transform.position = p + Vector3.up * (labelHeight * 0.5f + 0.4f);
        var cam = Camera.main;
        if (cam != null) label.transform.rotation = cam.transform.rotation;   // billboard: always faces the camera
        label.GetComponent<Renderer>().enabled = visible;

        int team = Team();
        if (team != lastTeam)
        {
            lastTeam = team;
            Color c = team == 1 ? Mine : team == 2 ? Enemy : Neutral;
            if (discMat != null) discMat.color = c;
            label.color = c;
        }
        if (Time.time >= nextText) { nextText = Time.time + 0.25f; UpdateText(); }
    }

    /// <summary>0 = resource or unknown yet, 1 = the local player's, 2 = the opponent's.</summary>
    int Team()
    {
        if (node != null || CommandRelay.Local == null) return 0;
        if ((unit != null && !unit.Object.IsValid) || (building != null && !building.Object.IsValid)) return lastTeam;
        var me = CommandRelay.Local.Object.InputAuthority;
        if (unit != null) return unit.Owner == me ? 1 : 2;
        if (building != null) return building.Owner == me ? 1 : 2;
        return 0;
    }

    void UpdateText()
    {
        var data = GameDataProvider.Get();
        if (unit != null && unit.Object != null && unit.Object.IsValid && data.Units.TryGetValue(unit.TypeKey.ToString(), out var u))
            label.text = Pretty(u.Key) + "\n<size=70%>" + Mathf.CeilToInt(unit.Hp) + "/" + u.Hp + "</size>";
        else if (building != null && building.Object != null && building.Object.IsValid && data.Buildings.TryGetValue(building.TypeKey.ToString(), out var b))
            label.text = Pretty(b.Key) + "\n<size=70%>" + (building.Completed ? Mathf.CeilToInt(building.Hp) + "/" + b.Hp
                                                                          : "building " + Mathf.RoundToInt(building.BuildProgress * 100f) + "%") + "</size>";
        else if (node != null && node.Object != null && node.Object.IsValid)
            label.text = Pretty(node.Resource) + "\n<size=70%>" + Mathf.CeilToInt(node.Amount) + "</size>";
    }

    /// <summary>"raider_boat" -> "Raider Boat". Also used by SelectionPanel.</summary>
    public static string Pretty(string key)
    {
        var parts = key.Split('_');
        for (int i = 0; i < parts.Length; i++)
            if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
        return string.Join(" ", parts);
    }

    void OnDestroy()
    {
        if (disc != null) Destroy(disc.gameObject);
        if (label != null) Destroy(label.gameObject);
        if (discMat != null) Destroy(discMat);
    }
}
