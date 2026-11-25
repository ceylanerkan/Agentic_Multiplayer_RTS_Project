using UnityEngine;

/// <summary>
/// The village behind the menu: puts the models of the game in the place of the simple shapes of the Menu scene (RtsSetupTools made
/// them; ground and sea stay). Without the models the shapes stay.
/// </summary>
public static class MenuLandscape
{
    public static void Build()
    {
        var old = GameObject.Find("Landscape");
        if (old == null) return;
        var land = new GameObject("Village").transform;
        land.SetParent(old.transform, false);
        if (ModelLook.Statue("town_center", land, Vector3.zero, 12f, 1.05f, false) == null) { Object.Destroy(land.gameObject); return; }

        // ground and sea keep their shapes, the ground gets its picture
        for (int i = old.transform.childCount - 1; i >= 0; i--)
        {
            var c = old.transform.GetChild(i);
            if (c == land) continue;
            var r = c.GetComponent<Renderer>();
            bool flat = r != null && (c.localScale.x > 30f);
            if (!flat) { c.gameObject.SetActive(false); continue; }
            if (r.sharedMaterial != null && (r.sharedMaterial.name.StartsWith("Ground") || r.sharedMaterial.name.StartsWith("Sand")))
            {
                var picture = UnityEngine.Resources.Load<Texture2D>(r.sharedMaterial.name.StartsWith("Ground") ? "Ground/grass" : "Ground/sand");
                if (picture == null) continue;
                var m = new Material(r.sharedMaterial) { mainTexture = picture, mainTextureScale = new Vector2(9f, 9f), color = Color.white };
                r.sharedMaterial = m;
            }
        }
        Add(land, "house", -9f, 2.5f, 25f, 1f, false); Add(land, "house", -8f, -5f, -15f, 1f, false); Add(land, "barracks", 9.5f, 5f, -10f, 1f, false); Add(land, "blacksmith", -3f, 9f, 8f, 1f, false);
        Add(land, "barracks", 19f, -1f, -20f, 1f, true); Add(land, "house", 14f, -7f, 20f, 1f, true);
        Add(land, "villager", -3.2f, -6.2f, 160f, 1f, false); Add(land, "villager", -1.6f, -7.4f, 200f, 1f, false); Add(land, "spearman", 3.6f, -6f, 150f, 1f, false); Add(land, "spearman", 5.1f, -6.6f, 165f, 1f, false);
        Add(land, "archer", 6.4f, -5.4f, 170f, 1f, false); Add(land, "spearman", 15.5f, -10.5f, 250f, 1f, true); Add(land, "archer", 17f, -9.6f, 250f, 1f, true);
        Add(land, "warship", -20f, 23f, 60f, 1f, false, -0.75f); Add(land, "raider_boat", 12f, 28f, -70f, 1f, true, -0.75f); Add(land, "fishing_boat", 29f, 15f, 120f, 1f, false, -0.75f);
        var random = new System.Random(11);
        string[] trees = { "tree1", "tree4", "tree2", "tree3", "tree4" };
        for (int i = 0; i < 60; i++)
        {
            double a = random.NextDouble() * 2 * System.Math.PI, d = 13.5 + random.NextDouble() * 9;
            float x = (float)(System.Math.Cos(a) * d) * 1.05f, z = (float)(System.Math.Sin(a) * d) * 0.8f;
            if (z < -8 && Mathf.Abs(x) < 11 || x > 9 && z < 4 && z > -12) continue; // keep the view and the red village free
            var t = GlbModel.Spawn(i % 9 == 8 ? "rockC" : trees[random.Next(trees.Length)], land);
            if (t == null) continue;
            t.transform.localPosition = new Vector3(x, 0, z); t.transform.localRotation = Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0);
            t.transform.localScale = Vector3.one * (i % 9 == 8 ? 2.2f : 0.75f + (float)random.NextDouble() * 0.4f);
        }
    }

    static void Add(Transform land, string key, float x, float z, float turn, float size, bool enemy, float y = 0f)
    {
        ModelLook.Statue(key, land, new Vector3(x, y, z), turn, size, enemy);
    }
}
