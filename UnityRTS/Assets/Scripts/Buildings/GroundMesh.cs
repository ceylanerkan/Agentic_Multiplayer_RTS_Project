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
    const float FloorY = -3f, FloorX = 300f, FloorFar = 320f; // sea floor far out: the water needs something under it
    const int Pixels = 960;

    float[] heights; int gridW, gridH; // of the mesh, also for the slopes
    Mesh mesh, solid; Texture2D texture; Material groundMaterial, waterMaterial;

    void OnEnable()
    {
        BuildMesh();
        if (!Application.isPlaying || !UseMaterials()) ShowPainted();
        if (Application.isPlaying) AddRiver();
    }

    void OnDisable()
    {
        foreach (var o in new Object[] { mesh, solid, texture, groundMaterial, waterMaterial }) if (o != null) DestroyImmediate(o);
    }

    void BuildMesh()
    {
        int w = Mathf.RoundToInt((MaxX - MinX) / Step) + 1, h = Mathf.RoundToInt((MaxZ - MinZ) / Step) + 1, n = w * h;
        heights = new float[n]; gridW = w; gridH = h;
        var v = new Vector3[n + 4]; var uv = new Vector2[n + 4]; var tri = new int[(w - 1) * (h - 1) * 6 + 6];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = MinX + i * Step, z = MinZ + j * Step;
                v[j * w + i] = new Vector3(x, heights[j * w + i] = TerrainShape.Height(x, z), z);
                uv[j * w + i] = new Vector2(i / (w - 1f), j / (h - 1f));
            }
        int k = 0;
        for (int j = 0; j < h - 1; j++)
            for (int i = 0; i < w - 1; i++)
            {
                int a = j * w + i;
                tri[k++] = a; tri[k++] = a + w; tri[k++] = a + 1; tri[k++] = a + 1; tri[k++] = a + w; tri[k++] = a + w + 1;
            }
        v[n] = new Vector3(-FloorX, FloorY, 35); v[n + 1] = new Vector3(-FloorX, FloorY, FloorFar); v[n + 2] = new Vector3(FloorX, FloorY, 35); v[n + 3] = new Vector3(FloorX, FloorY, FloorFar);
        for (int i = 0; i < 4; i++) uv[n + i] = new Vector2(0.5f, 1f);
        tri[k++] = n; tri[k++] = n + 1; tri[k++] = n + 2; tri[k++] = n + 2; tri[k++] = n + 1; tri[k++] = n + 3;
        mesh = new Mesh { name = "Ground", hideFlags = HideFlags.DontSave, vertices = v, uv = uv, triangles = tri };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        // without the far sea floor: its huge triangles are bad for physics
        var solidTriangles = new int[tri.Length - 6]; System.Array.Copy(tri, solidTriangles, solidTriangles.Length);
        solid = new Mesh { name = "GroundCollider", hideFlags = HideFlags.DontSave, vertices = v, triangles = solidTriangles };
        GetComponent<MeshCollider>().sharedMesh = solid;
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
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // gentle hills, no visible shadow

        var sea = transform.parent != null ? transform.parent.Find("Sea") : null;
        var water = UnityEngine.Resources.Load<Shader>("Ground/Water");
        if (sea != null && water != null && water.isSupported)
        {
            waterMaterial = new Material(water);
            var s = sea.GetComponent<Renderer>();
            s.material = waterMaterial; s.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
                }
                px[j * w + i] = c;
            }
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true, control) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    /// <summary>What lies at this place: r dry grass, g sand, b earth, a rock (the rest is grass).</summary>
    Color Weights(float x, float z, float coastZ)
    {
        float coarse = Mathf.PerlinNoise(x * 0.028f + 40f, z * 0.028f + 70f), mid = Mathf.PerlinNoise(x * 0.10f + 90f, z * 0.10f + 30f), fine = Mathf.PerlinNoise(x * 0.42f + 10f, z * 0.42f + 5f);
        float dry = Mathf.Clamp01((coarse * 0.7f + mid * 0.4f - 0.52f) * 4f);
        // trodden earth around the two Town Centers and in a few patches
        float dx = Mathf.Abs(x) - 65f, dz = z - 25f, d = Mathf.Sqrt(dx * dx + dz * dz) + (mid - 0.5f) * 9f + (fine - 0.5f) * 3f;
        float earth = Mathf.Max(0.85f * Mathf.Clamp01((11f - d) / 5f), Mathf.Clamp01((Mathf.PerlinNoise(x * 0.06f + 200f, z * 0.06f + 130f) + fine * 0.15f - 0.80f) * 6f));
        // stones on steep ground and outside the map
        int gx = Mathf.Clamp(Mathf.RoundToInt((x - MinX) / Step), 1, gridW - 2), gz = Mathf.Clamp(Mathf.RoundToInt((z - MinZ) / Step), 1, gridH - 2), g = gz * gridW + gx;
        float slope = new Vector2(heights[g + 1] - heights[g - 1], heights[g + gridW] - heights[g - gridW]).magnitude / (2f * Step);
        float outside = Mathf.Max(Mathf.Abs(x) - 99f, -69f - z) + (coarse - 0.5f) * 14f;
        float rock = Mathf.Max(Mathf.Clamp01((slope + (fine - 0.5f) * 0.12f - 0.24f) * 7f), 0.9f * Mathf.Clamp01(outside / 14f) * Mathf.Clamp01(0.4f + mid));
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
