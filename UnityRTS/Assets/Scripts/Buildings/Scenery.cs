using System.Collections.Generic;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Stones and bushes on the map, only to look at: no colliders, nobody is blocked, the same on both screens (fixed seed) and mirrored.
/// Most lie at the foot of the mountains and along the river. One that stands where a building is placed disappears.
/// </summary>
public class Scenery : MonoBehaviour
{
    static readonly string[] Stones = { "rockA", "rockB", "rockC", "rockD", "rockE", "rockF", "rockG" };

    class Piece { public GameObject Go; public Vector3 At; }
    readonly List<Piece> pieces = new List<Piece>();
    int version = -1;

    void Start()
    {
        var random = new System.Random(4711);
        var nodes = MapLayout.Build();
        float R(float a, float b) { return a + (float)random.NextDouble() * (b - a); }
        for (int i = 0, placed = 0; i < 3000 && placed < 85; i++)
        {
            float x = -R(1f, 106f), z = R(-78f, 41f);
            float foot = TerrainShape.Foot(x, z), bank = Mathf.Abs(x) < 30f ? TerrainShape.RiverBank(x, z) : 99f;
            bool atFoot = foot > -3f && foot < 0.4f, atRiver = bank > 1.2f && bank < 4f;
            if (foot > 0.4f || bank < 1.2f || TerrainShape.CoastZ(x) - z < 5f) continue;
            if (!atFoot && !atRiver && random.NextDouble() > 0.10) continue; // the open ground stays mostly free
            float dx = x + 65f, dz = z - 25f;
            if (dx * dx + dz * dz < 15f * 15f || Taken(nodes, x, z)) continue;
            bool bush = !atFoot && random.NextDouble() < 0.45;
            string key = bush ? "bush" : Stones[random.Next(Stones.Length)];
            float size = bush ? R(0.8f, 1.5f) : atFoot ? R(1.2f, 2.6f) : R(0.9f, 1.8f), turn = R(0f, 360f);
            Put(key, x, z, turn, size); Put(key, -x, z, -turn, size);
            placed++;
        }
    }

    static bool Taken(List<MapNode> nodes, float x, float z)
    {
        foreach (var n in nodes) if ((n.X - x) * (n.X - x) + (n.Z - z) * (n.Z - z) < 2.6f * 2.6f) return true;
        return false;
    }

    void Put(string key, float x, float z, float turn, float size)
    {
        var go = GlbModel.Spawn(key, transform);
        if (go == null) return;
        go.transform.position = new Vector3(x, TerrainShape.Height(x, z) - 0.05f, z);
        go.transform.rotation = Quaternion.Euler(0, turn, 0);
        go.transform.localScale = Vector3.one * size;
        pieces.Add(new Piece { Go = go, At = go.transform.position });
    }

    void Update()
    {
        if (version == BuildPlaces.Version) return;
        version = BuildPlaces.Version;
        foreach (var p in pieces)
        {
            bool free = true;
            foreach (var b in NetworkBuilding.All)
            {
                var col = b != null ? b.GetComponent<Collider>() : null;
                if (col == null) continue;
                Vector3 e = col.bounds.extents, d = b.transform.position - p.At;
                if (Mathf.Abs(d.x) < e.x + 0.8f && Mathf.Abs(d.z) < e.z + 0.8f) { free = false; break; }
            }
            if (p.Go != null && p.Go.activeSelf != free) p.Go.SetActive(free);
        }
    }
}
