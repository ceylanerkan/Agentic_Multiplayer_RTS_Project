using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Builds the ground from TerrainShape when the scene loads: mesh, collider and, in play mode, the ground and water materials
/// (photo textures and shaders in Resources/Ground; a painted control texture says what lies where). Nothing is saved: change
/// TerrainShape or Weights here and the ground follows. In the editor and without the shaders a simple painted ground is shown.
/// </summary>
[ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class GroundMesh : MonoBehaviour
{
    const float MinX = -150f, MaxX = 150f, MinZ = -120f, MaxZ = 70f, Step = 1.25f;
    const float FarStep = 3f; const int FarColumns = 50; // coarser mountains beside the part above, out to x = 300 (no collider)
    const float FloorY = -3f, FloorX = 320f, FloorFar = 320f; // sea floor far out: the water needs something under it
    const int Pixels = 960;

    float[] heights; int gridW, gridH; // of the mesh, also for the slopes
    Mesh mesh, solid; Texture2D texture; Material groundMaterial, waterMaterial;

    void OnEnable()
    {
        BuildMesh();
        if (!Application.isPlaying || !UseMaterials()) ShowPainted();
        if (Application.isPlaying) { AddRiver(); if (GetComponent<DayNight>() == null) { gameObject.AddComponent<DayNight>(); gameObject.AddComponent<Scenery>(); } }
    }

    void OnDisable()
    {
        foreach (var o in new Object[] { mesh, solid, texture, groundMaterial, waterMaterial }) if (o != null) DestroyImmediate(o);
    }

    void BuildMesh()
    {
        int near = Mathf.RoundToInt((MaxX - MinX) / Step) + 1, w = near + 2 * FarColumns, h = Mathf.RoundToInt((MaxZ - MinZ) / Step) + 1, n = w * h;
        heights = new float[n]; gridW = w; gridH = h;
        var v = new Vector3[n + 4]; var uv = new Vector2[n + 4]; var tri = new int[(w - 1) * (h - 1) * 6 + 6]; var solidTriangles = new int[(near - 1) * (h - 1) * 6];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                int c = i - FarColumns; // column of the near part
                float x = c < 0 ? MinX + c * FarStep : c >= near ? MaxX + (c - near + 1) * FarStep : MinX + c * Step, z = MinZ + j * Step;
                v[j * w + i] = new Vector3(x, heights[j * w + i] = TerrainShape.Height(x, z), z);
                uv[j * w + i] = new Vector2(Mathf.Clamp01((x - MinX) / (MaxX - MinX)), j / (h - 1f));
            }
        int k = 0, s = 0;
        for (int j = 0; j < h - 1; j++)
            for (int i = 0; i < w - 1; i++)
            {
                int a = j * w + i;
                tri[k++] = a; tri[k++] = a + w; tri[k++] = a + 1; tri[k++] = a + 1; tri[k++] = a + w; tri[k++] = a + w + 1;
                if (i >= FarColumns && i < FarColumns + near - 1) { System.Array.Copy(tri, k - 6, solidTriangles, s, 6); s += 6; }
            }
        v[n] = new Vector3(-FloorX, FloorY, 35); v[n + 1] = new Vector3(-FloorX, FloorY, FloorFar); v[n + 2] = new Vector3(FloorX, FloorY, 35); v[n + 3] = new Vector3(FloorX, FloorY, FloorFar);
        for (int i = 0; i < 4; i++) uv[n + i] = new Vector2(0.5f, 1f);
        tri[k++] = n; tri[k++] = n + 1; tri[k++] = n + 2; tri[k++] = n + 2; tri[k++] = n + 1; tri[k++] = n + 3;
        mesh = new Mesh { name = "Ground", hideFlags = HideFlags.DontSave, vertices = v, uv = uv, colors32 = Shade(v), triangles = tri };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        // without the far mountains and without the far sea floor, whose huge triangles are bad for physics
        solid = new Mesh { name = "GroundCollider", hideFlags = HideFlags.DontSave, vertices = v, triangles = solidTriangles };
        GetComponent<MeshCollider>().sharedMesh = solid;
    }

    /// <summary>For the shader: r = how open the place is (dark gullies, bright crests), g = how much it belongs to the mountains, b = the rocks at their foot.</summary>
    Color32[] Shade(Vector3[] v)
    {
        var c = new Color32[v.Length];
        for (int j = 0; j < gridH; j++)
            for (int i = 0; i < gridW; i++)
            {
                int a = j * gridW + i; float open = 1f, o = TerrainShape.Foot(v[a].x, v[a].z), mountain = 0f, scree = 0f;
                if (o > -2f)
                {
                    int dx = i < FarColumns || i >= gridW - FarColumns ? 2 : 5; // about 6 m
                    int l = Mathf.Max(i - dx, 0), r = Mathf.Min(i + dx, gridW - 1), b = Mathf.Max(j - 5, 0) * gridW, f = Mathf.Min(j + 5, gridH - 1) * gridW;
                    float higher = (heights[j * gridW + l] + heights[j * gridW + r] + heights[b + i] + heights[f + i] + heights[b + l] + heights[b + r] + heights[f + l] + heights[f + r]) / 8f - heights[a];
                    open = Mathf.Clamp(1f - 0.075f * Mathf.Max(higher, 0f) + 0.03f * Mathf.Clamp(-higher, 0f, 6f), 0.35f, 1.15f);
                    mountain = Mathf.SmoothStep(0f, 1f, (o + 1.2f) / 3f);
                    scree = Mathf.SmoothStep(0f, 1f, (o + 0.5f) / 2f) * (1f - Mathf.SmoothStep(0f, 1f, (o - 5f) / 7f));
                }
                c[a] = new Color32((byte)(open * 200f), (byte)(mountain * 255f), (byte)(scree * 255f), 255);
            }
        for (int a = gridW * gridH; a < c.Length; a++) c[a] = new Color32(200, 0, 0, 255);
        return c;
    }

    /// <summary>The real materials; false when a shader or texture is missing.</summary>
    bool UseMaterials()
    {
        var shader = UnityEngine.Resources.Load<Shader>("Ground/Ground");
        if (shader == null || !shader.isSupported) { Debug.LogWarning("GroundMesh: shader Ground/Ground is missing or does not work here, using the simple ground."); return false; }
        groundMaterial = new Material(shader);
        foreach (var name in new[] { "Grass", "Dry", "Sand", "Earth", "Rock" })
            foreach (var kind in new[] { "", "N" })
            {
                var t = UnityEngine.Resources.Load<Texture2D>("Ground/" + name.ToLowerInvariant() + (kind == "N" ? "_n" : ""));
                if (t == null) { Debug.LogWarning("GroundMesh: texture Ground/" + name + kind + " is missing."); return false; }
                t.anisoLevel = 8; t.filterMode = FilterMode.Trilinear;
                groundMaterial.SetTexture("_" + name + kind, t);
            }
        texture = Paint(true);
        groundMaterial.SetTexture("_Control", texture);
        groundMaterial.SetFloat("_SeaLevel", TerrainShape.SeaLevel);
        var r = GetComponent<MeshRenderer>();
        r.SetPropertyBlock(null);
        r.material = groundMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // gentle hills; the mountains are shaded by Shade

        var sea = transform.parent != null ? transform.parent.Find("Sea") : null;
        var water = UnityEngine.Resources.Load<Shader>("Ground/Water");
        if (sea != null && water != null && water.isSupported)
        {
            waterMaterial = new Material(water);
            var s = sea.GetComponent<Renderer>();
            s.material = waterMaterial; sea.localScale = new Vector3(2f * FloorX, sea.localScale.y, sea.localScale.z); // up to the far mountains
            s.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return true;
    }

    /// <summary>The surface of the river (the sea object ends at z = 20; the land around lies higher than the water).</summary>
    void AddRiver()
    {
        var sea = transform.parent != null ? transform.parent.Find("Sea") : null;
        if (sea == null) return;
        var river = GameObject.CreatePrimitive(PrimitiveType.Cube);
        river.name = "RiverWater";
        Destroy(river.GetComponent<Collider>()); // clicks reach the river bed
        river.transform.SetParent(transform, false);
        river.transform.position = new Vector3(0, TerrainShape.SeaLevel - 0.5f, -55f); river.transform.localScale = new Vector3(60f, 1f, 150f);
        var r = river.GetComponent<Renderer>();
        r.sharedMaterial = waterMaterial != null ? waterMaterial : sea.GetComponent<Renderer>().sharedMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void ShowPainted()
    {
        texture = Paint(false);
        var block = new MaterialPropertyBlock();
        block.SetTexture("_BaseMap", texture); block.SetColor("_BaseColor", Color.white); // the material asset stays as it is
        GetComponent<MeshRenderer>().SetPropertyBlock(block);
    }

    /// <summary>The control texture (weights as colour channels) or the picture for the simple ground.</summary>
    Texture2D Paint(bool control)
    {
        int w = Pixels, h = Mathf.RoundToInt(Pixels * (MaxZ - MinZ) / (MaxX - MinX));
        var px = new Color32[w * h];
        var coast = new float[w];
        for (int i = 0; i < w; i++) coast[i] = TerrainShape.CoastZ(MinX + (i + 0.5f) / w * (MaxX - MinX));
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = MinX + (i + 0.5f) / w * (MaxX - MinX), z = MinZ + (j + 0.5f) / h * (MaxZ - MinZ);
                Color c = Weights(x, z, coast[i]);
                if (!control)
                {
                    float grass = Mathf.Clamp01(1f - c.r - c.g - c.b - c.a);
                    c = new Color(0.36f, 0.45f, 0.21f) * grass + new Color(0.53f, 0.51f, 0.28f) * c.r + new Color(0.81f, 0.72f, 0.55f) * c.g + new Color(0.48f, 0.38f, 0.25f) * c.b + new Color(0.48f, 0.46f, 0.42f) * c.a;
                    if (heights[GridIndex(x, z)] > 13f) c = new Color(0.9f, 0.93f, 0.97f);
                }
                px[j * w + i] = c;
            }
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true, control) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    int GridIndex(float x, float z)
    {
        int gx = FarColumns + Mathf.Clamp(Mathf.RoundToInt((x - MinX) / Step), 0, gridW - 2 * FarColumns - 1), gz = Mathf.Clamp(Mathf.RoundToInt((z - MinZ) / Step), 1, gridH - 2);
        return gz * gridW + gx;
    }

    /// <summary>What lies at this place: r dry grass, g sand, b earth, a rock (the rest is grass).</summary>
    Color Weights(float x, float z, float coastZ)
    {
        float coarse = Mathf.PerlinNoise(x * 0.028f + 40f, z * 0.028f + 70f), mid = Mathf.PerlinNoise(x * 0.10f + 90f, z * 0.10f + 30f), fine = Mathf.PerlinNoise(x * 0.42f + 10f, z * 0.42f + 5f);
        float dry = Mathf.Clamp01((coarse * 0.7f + mid * 0.4f - 0.52f) * 4f);
        // trodden earth around the two Town Centers and in a few patches
        float dx = Mathf.Abs(x) - 65f, dz = z - 25f, d = Mathf.Sqrt(dx * dx + dz * dz) + (mid - 0.5f) * 9f + (fine - 0.5f) * 3f;
        float earth = Mathf.Max(0.85f * Mathf.Clamp01((11f - d) / 5f), Mathf.Clamp01((Mathf.PerlinNoise(x * 0.06f + 200f, z * 0.06f + 130f) + fine * 0.15f - 0.80f) * 6f));
        // stones on steep ground (the mountains outside the map are coloured by the shader)
        int g = GridIndex(x, z);
        float slope = new Vector2(heights[g + 1] - heights[g - 1], heights[g + gridW] - heights[g - gridW]).magnitude / (2f * Step);
        float rock = Mathf.Clamp01((slope + (fine - 0.5f) * 0.12f - 0.24f) * 7f);
        // beach, its inner edge is uneven
        float inland = coastZ - z;
        float sand = Mathf.Clamp01((6.2f + (mid - 0.5f) * 7f + (fine - 0.5f) * 2.5f - inland) / 2.2f);
        if (Mathf.Abs(x) < 30f && inland > 0f) // sandy river banks, stones in the bed
        {
            float bank = TerrainShape.RiverBank(x, z) + (fine - 0.5f) * 1.6f;
            sand = Mathf.Max(sand, Mathf.Clamp01((3.4f + (mid - 0.5f) * 3f - bank) / 1.6f));
            if (bank < 0f) { float bed = Mathf.Clamp01(-bank / 2.5f); sand *= 1f - 0.7f * bed; return new Color(0f, sand, 0f, (1f - sand) * 0.9f); }
        }
        rock *= 1f - sand; earth *= (1f - sand) * (1f - rock); dry *= (1f - sand) * (1f - rock) * (1f - earth);
        return new Color(dry, sand, earth, rock);
    }
}
