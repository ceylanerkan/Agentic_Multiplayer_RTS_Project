using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Builds the ground from TerrainShape (mesh, collider and a painted texture) when the scene loads, also in the editor. Nothing is
/// saved: change TerrainShape or the colours here and the ground follows.
/// </summary>
[ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class GroundMesh : MonoBehaviour
{
    const float MinX = -150f, MaxX = 150f, MinZ = -120f, MaxZ = 66f, Step = 1.5f;
    const int Pixels = 800;

    [SerializeField] Color grass = new Color(0.36f, 0.47f, 0.22f), dryGrass = new Color(0.52f, 0.53f, 0.28f), darkGrass = new Color(0.24f, 0.37f, 0.17f);
    [SerializeField] Color sand = new Color(0.80f, 0.71f, 0.49f), wetSand = new Color(0.58f, 0.50f, 0.36f), earth = new Color(0.45f, 0.37f, 0.25f), rock = new Color(0.43f, 0.42f, 0.38f);

    Mesh mesh; Texture2D texture;

    void OnEnable() { Build(); }

    void OnDisable()
    {
        if (mesh != null) DestroyImmediate(mesh);
        if (texture != null) DestroyImmediate(texture);
    }

    void Build()
    {
        int w = Mathf.RoundToInt((MaxX - MinX) / Step) + 1, h = Mathf.RoundToInt((MaxZ - MinZ) / Step) + 1;
        var v = new Vector3[w * h]; var uv = new Vector2[w * h]; var tri = new int[(w - 1) * (h - 1) * 6];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = MinX + i * Step, z = MinZ + j * Step;
                v[j * w + i] = new Vector3(x, TerrainShape.Height(x, z), z);
                uv[j * w + i] = new Vector2(i / (w - 1f), j / (h - 1f));
            }
        int k = 0;
        for (int j = 0; j < h - 1; j++)
            for (int i = 0; i < w - 1; i++)
            {
                int a = j * w + i;
                tri[k++] = a; tri[k++] = a + w; tri[k++] = a + 1; tri[k++] = a + 1; tri[k++] = a + w; tri[k++] = a + w + 1;
            }
        mesh = new Mesh { name = "Ground", hideFlags = HideFlags.DontSave, vertices = v, uv = uv, triangles = tri };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        texture = Paint();
        var block = new MaterialPropertyBlock();
        block.SetTexture("_BaseMap", texture); block.SetColor("_BaseColor", Color.white); // the material asset stays as it is
        GetComponent<MeshRenderer>().SetPropertyBlock(block);
    }

    Texture2D Paint()
    {
        int w = Pixels, h = Mathf.RoundToInt(Pixels * (MaxZ - MinZ) / (MaxX - MinX));
        var px = new Color32[w * h];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = MinX + (i + 0.5f) / w * (MaxX - MinX), z = MinZ + (j + 0.5f) / h * (MaxZ - MinZ);
                px[j * w + i] = ColourAt(x, z);
            }
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    Color ColourAt(float x, float z)
    {
        float coarse = Mathf.PerlinNoise(x * 0.035f + 40f, z * 0.035f + 70f), fine = Mathf.PerlinNoise(x * 0.31f + 10f, z * 0.31f + 5f), dust = Mathf.PerlinNoise(x * 0.09f + 90f, z * 0.09f + 30f);
        Color c = Color.Lerp(darkGrass, grass, Mathf.Clamp01(coarse * 1.5f - 0.1f));
        c = Color.Lerp(c, dryGrass, Mathf.Clamp01((dust - 0.5f) * 2.2f));
        // trodden earth around the two Town Centers
        float dx = Mathf.Abs(x) - 65f, dz = z - 25f, d = Mathf.Sqrt(dx * dx + dz * dz) + (dust - 0.5f) * 8f;
        c = Color.Lerp(c, earth, 0.75f * Mathf.Clamp01((13f - d) / 7f));
        // stony land outside the map
        float outside = Mathf.Max(Mathf.Abs(x) - 99f, -69f - z) + (coarse - 0.5f) * 10f;
        c = Color.Lerp(c, rock, 0.8f * Mathf.Clamp01(outside / 14f));
        // beach
        float inland = TerrainShape.CoastZ(x) - z;
        c = Color.Lerp(c, sand, Mathf.Clamp01((6.5f + (dust - 0.5f) * 6f - inland) / 2.5f));
        c = Color.Lerp(c, wetSand, Mathf.Clamp01((1.6f - inland) / 1.6f));
        return c * (0.86f + 0.28f * fine);
    }
}
