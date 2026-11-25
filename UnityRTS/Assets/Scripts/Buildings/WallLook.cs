using System.Collections.Generic;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// The look of a Wall or Gate (local, no network state): a post of sharpened logs in the middle and, towards every neighbouring piece
/// of the same owner, a piece of fence (model "fence"; without it a row of logs made here). A Gate stands across the line and opens its doors when one of its owner's units is near.
/// All parts exist from the start (switched on and off), so the fog of war can hide them with the rest of the building.
/// </summary>
[RequireComponent(typeof(NetworkBuilding))]
public class WallLook : MonoBehaviour
{
    [SerializeField] Material wood, dark;
    [SerializeField] bool gate;

    const float Height = 1.9f, OpenDistance = 3.4f;
    static readonly Vector2Int[] Directions = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, -1) };
    static Mesh postMesh, gatePostMesh, beamMesh; // shared by all pieces
    static readonly Dictionary<int, Mesh> rows = new Dictionary<int, Mesh>(); // length in cm -> row of logs

    NetworkBuilding building;
    Transform look, frame, doorA, doorB;
    readonly MeshFilter[] links = new MeshFilter[4];
    bool fences; // the links are models
    int version = -1;
    float nextCheck, open, wantedOpen, progress = -1f;

    void Awake()
    {
        building = GetComponent<NetworkBuilding>();
        look = new GameObject("Look").transform;
        look.SetParent(transform, false);
        look.localPosition = Vector3.down * 0.5f; // the foot of the collider
        Vector3 s = transform.localScale;
        look.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
        if (postMesh == null)
        {
            postMesh = Logs(new[] { new Vector4(0, 0, 0.30f, Height + 0.35f), new Vector4(0.26f, 0.2f, 0.2f, Height), new Vector4(-0.25f, -0.2f, 0.2f, Height + 0.1f), new Vector4(-0.2f, 0.26f, 0.19f, Height - 0.1f), new Vector4(0.22f, -0.25f, 0.19f, Height - 0.05f) });
            gatePostMesh = Logs(new[] { new Vector4(0, 0, 0.27f, 2.9f), new Vector4(0.02f, 0.3f, 0.2f, 2.3f), new Vector4(-0.02f, -0.3f, 0.2f, 2.3f) });
            beamMesh = Box();
        }
        if (gate)
        {
            frame = new GameObject("Frame").transform; frame.SetParent(look, false);
            if (FenceGate()) { Links(); return; }
            Part(frame, gatePostMesh, wood, new Vector3(-0.95f, 0, 0), Vector3.one);
            Part(frame, gatePostMesh, wood, new Vector3(0.95f, 0, 0), Vector3.one);
            Part(frame, beamMesh, dark, new Vector3(0, 2.35f, 0), new Vector3(2.5f, 0.22f, 0.34f));
            Part(frame, beamMesh, dark, new Vector3(0, 2.72f, 0), new Vector3(2.1f, 0.14f, 0.5f));
            doorA = Door(-0.78f, 1f); doorB = Door(0.78f, -1f);
        }
        else Part(look, postMesh, wood, Vector3.zero, Vector3.one);
        Links();
    }

    void Links()
    {
        for (int i = 0; i < 4; i++)
        {
            links[i] = Part(look, beamMesh, wood, Vector3.zero, Vector3.one).GetComponent<MeshFilter>();
            var model = GlbModel.Spawn("fence", links[i].transform);
            if (model != null) { fences = true; DestroyImmediate(links[i].GetComponent<MeshRenderer>()); model.transform.localPosition = new Vector3(0.5f, 0, 0); model.transform.localScale = new Vector3(0.5f, 1, 1); } // one metre long: scaled to the link
            links[i].gameObject.SetActive(false);
        }
    }

    Transform fenceDoor;

    bool FenceGate()
    {
        var model = GlbModel.Spawn("fencegate", frame); var door = GlbModel.Spawn("fencedoor", frame);
        if (model == null || door == null) return false;
        var f = door.GetComponentInChildren<MeshFilter>();
        float edge = f != null ? f.sharedMesh.bounds.min.x : -0.28f;
        fenceDoor = new GameObject("Hinge").transform; fenceDoor.SetParent(frame, false); fenceDoor.localPosition = new Vector3(edge, 0, 0);
        door.transform.SetParent(fenceDoor, false); door.transform.localPosition = new Vector3(-edge, 0, 0);
        return true;
    }

    Transform Door(float x, float side)
    {
        var hinge = new GameObject("Door").transform; hinge.SetParent(frame, false); hinge.localPosition = new Vector3(x, 0, 0);
        for (int i = 0; i < 3; i++) Part(hinge, beamMesh, wood, new Vector3(side * (0.14f + i * 0.25f), 1.05f, 0), new Vector3(0.23f, 1.9f, 0.1f));
        Part(hinge, beamMesh, dark, new Vector3(side * 0.39f, 0.6f, 0), new Vector3(0.74f, 0.14f, 0.16f));
        Part(hinge, beamMesh, dark, new Vector3(side * 0.39f, 1.55f, 0), new Vector3(0.74f, 0.14f, 0.16f));
        return hinge;
    }

    void LateUpdate()
    {
        if (building.Object == null || !building.Object.IsValid) return;
        if (version != BuildPlaces.Version) { version = BuildPlaces.Version; Connect(); }
        float p = building.Completed ? 1f : building.BuildProgress;
        if (p != progress) { progress = p; look.localScale = new Vector3(look.localScale.x, Mathf.Lerp(0.2f, 1f, p) / transform.localScale.y, look.localScale.z); } // it grows out of the ground
        if (!gate) return;
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + 0.2f; wantedOpen = 0f;
            foreach (var u in NetworkUnit.All)
            {
                if (u.Object == null || !u.Object.IsValid || u.Owner != building.Owner || u.IsShip) continue;
                Vector3 d = u.transform.position - transform.position; d.y = 0;
                if (d.magnitude < OpenDistance) { wantedOpen = building.Completed ? 1f : 0f; break; }
            }
        }
        open = Mathf.MoveTowards(open, wantedOpen, Time.deltaTime * 2.5f);
        if (fenceDoor != null) { fenceDoor.localRotation = Quaternion.Euler(0, -98f * open, 0); return; }
        doorA.localRotation = Quaternion.Euler(0, -98f * open, 0); doorB.localRotation = Quaternion.Euler(0, 98f * open, 0);
    }

    void Connect()
    {
        var here = new Dictionary<Vector2Int, bool>(); // own neighbours: offset in pieces -> is it a Gate
        foreach (var b in NetworkBuilding.All)
        {
            if (b == building || b.Object == null || !b.Object.IsValid || b.Owner != building.Owner) continue;
            string key = b.TypeKey.ToString();
            if (!BuildSite.IsWall(key)) continue;
            Vector3 d = (b.transform.position - transform.position) / WallLine.Step;
            var o = new Vector2Int(Mathf.RoundToInt(d.x), Mathf.RoundToInt(d.z));
            if (Mathf.Abs(o.x) <= 1 && Mathf.Abs(o.y) <= 1) here[o] = key == "gate";
        }
        if (gate) frame.localRotation = Quaternion.Euler(0, GateTurn(here), 0);
        for (int i = 0; i < 4; i++)
        {
            var d = Directions[i];
            bool on = here.TryGetValue(d, out bool otherGate) && !(gate && otherGate);
            // a diagonal row only when the two are not already connected over a corner
            if (on && d.x != 0 && d.y != 0 && (here.ContainsKey(new Vector2Int(d.x, 0)) || here.ContainsKey(new Vector2Int(0, d.y)))) on = false;
            links[i].gameObject.SetActive(on);
            if (!on) continue;
            float length = WallLine.Step * Mathf.Sqrt(d.x * d.x + d.y * d.y);
            bool half = gate || otherGate; // beside a Gate only up to its post
            var t = links[i].transform;
            if (fences) t.localScale = new Vector3(half ? length - 0.9f : length, 1f, 1f); else links[i].sharedMesh = Row(half ? length - 0.9f : length);
            t.localPosition = gate ? new Vector3(d.x, 0, d.y) * WallLine.Step : Vector3.zero;
            t.localRotation = Quaternion.LookRotation((gate ? -1f : 1f) * new Vector3(d.x, 0, d.y)) * Quaternion.Euler(0, -90f, 0); // the row lies along x
        }
    }

    static float GateTurn(Dictionary<Vector2Int, bool> here)
    {
        bool x = here.ContainsKey(new Vector2Int(1, 0)) || here.ContainsKey(new Vector2Int(-1, 0)), z = here.ContainsKey(new Vector2Int(0, 1)) || here.ContainsKey(new Vector2Int(0, -1));
        if (x || !z && here.Count == 0) return 0f;
        if (z) return 90f;
        return here.ContainsKey(new Vector2Int(1, 1)) || here.ContainsKey(new Vector2Int(-1, -1)) ? -45f : 45f;
    }

    // ---- meshes ----
    static GameObject Part(Transform parent, Mesh mesh, Material material, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    static Mesh Row(float length)
    {
        int key = Mathf.RoundToInt(length * 100f);
        if (rows.TryGetValue(key, out var m) && m != null) return m;
        var logs = new List<Vector4>();
        int n = Mathf.Max(1, Mathf.RoundToInt((length - 0.5f) / 0.36f));
        for (int i = 0; i < n; i++)
            logs.Add(new Vector4(0.25f + (length - 0.5f) * (i + 0.5f) / n, 0.04f * Mathf.Sin(i * 5.1f + key), 0.19f, Height - 0.12f + 0.16f * Mathf.Sin(i * 2.7f + key * 0.1f)));
        m = Logs(logs.ToArray(), length);
        rows[key] = m;
        return m;
    }

    /// <summary>Sharpened logs (x, z, radius, height), reaching a little into the ground; with a length also a cross beam along x.</summary>
    static Mesh Logs(Vector4[] logs, float beam = 0f)
    {
        const int sides = 7; const float depth = 0.9f;
        var v = new List<Vector3>(); var t = new List<int>();
        foreach (var l in logs)
        {
            float tip = l.z * 1.7f;
            int foot = v.Count;
            for (int i = 0; i < sides; i++) { float a = i * 2f * Mathf.PI / sides; v.Add(new Vector3(l.x + Mathf.Cos(a) * l.z, -depth, l.y + Mathf.Sin(a) * l.z)); }
            for (int i = 0; i < sides; i++) { float a = i * 2f * Mathf.PI / sides; v.Add(new Vector3(l.x + Mathf.Cos(a) * l.z, l.w - tip, l.y + Mathf.Sin(a) * l.z)); }
            int rim = v.Count; // again for the tip: a sharp edge between side and tip
            for (int i = 0; i < sides; i++) v.Add(v[foot + sides + i]);
            int top = v.Count; v.Add(new Vector3(l.x, l.w, l.y));
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                t.AddRange(new[] { foot + i, foot + sides + i, foot + j, foot + j, foot + sides + i, foot + sides + j, rim + i, top, rim + j });
            }
        }
        if (beam > 0f)
            foreach (float y in new[] { 0.55f, 1.25f })
                AddBox(v, t, new Vector3(beam * 0.5f, y, -0.2f), new Vector3(beam, 0.14f, 0.12f));
        return Finish(v, t);
    }

    static Mesh Box() { var v = new List<Vector3>(); var t = new List<int>(); AddBox(v, t, Vector3.zero, Vector3.one); return Finish(v, t); }

    static void AddBox(List<Vector3> v, List<int> t, Vector3 c, Vector3 size)
    {
        Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var n in normals)
        {
            Vector3 a = new Vector3(n.y, n.z, n.x), b = Vector3.Cross(n, a); int k = v.Count;
            foreach (var corner in new[] { n - a - b, n - a + b, n + a + b, n + a - b }) v.Add(c + Vector3.Scale(corner, size) * 0.5f);
            t.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
        }
    }

    static Mesh Finish(List<Vector3> v, List<int> t)
    {
        var m = new Mesh { name = "Wall", hideFlags = HideFlags.DontSave };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}
