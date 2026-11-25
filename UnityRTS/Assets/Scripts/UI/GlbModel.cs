using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Reads the model files of the game (Resources/Models/*.bytes, GLB without pictures: meshes, colours, bones, animations) and gives out
/// copies. Why not imported assets: the files are made by a script (buildings are put together from parts), and this way they look the
/// same in the editor and in a build without any import setting. Pictures come from Resources/Models/Tex by the material's name.
/// </summary>
public static class GlbModel
{
    static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
    static Transform shelf; // inactive: holds the originals

    /// <summary>A new copy of the model, or null when there is no such file.</summary>
    public static GameObject Spawn(string key, Transform parent)
    {
        var t = Template(key);
        if (t == null) return null;
        var go = UnityEngine.Object.Instantiate(t, parent, false);
        go.name = key;
        return go;
    }

    static GameObject Template(string key)
    {
        if (templates.TryGetValue(key, out var known) && known != null) return known;
        if (templates.ContainsKey(key) && known == null && shelf != null) return null; // looked for before: no file
        var file = UnityEngine.Resources.Load<TextAsset>("Models/" + key);
        GameObject go = null;
        if (file != null)
        {
            if (shelf == null) { var s = new GameObject("ModelShelf"); s.SetActive(false); UnityEngine.Object.DontDestroyOnLoad(s); shelf = s.transform; }
            try { go = new Reader(file.bytes, key).Build(shelf); }
            catch (Exception e) { Debug.LogError("Model " + key + " could not be read: " + e.Message + "\n" + e.StackTrace); }
        }
        else if (shelf == null) return null;
        templates[key] = go;
        return go;
    }

    // ---- materials ----
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static Shader shader;

    /// <summary>The material for a picture name (a file in Resources/Models/Tex) or, without such a file, for a plain colour.</summary>
    public static Material Material(string name, Color colour)
    {
        string key = name + ColorUtility.ToHtmlStringRGB(colour);
        if (materials.TryGetValue(key, out var m) && m != null) return m;
        if (shader == null) shader = UnityEngine.Resources.Load<Shader>("Models/Model");
        if (shader == null) { Debug.LogError("Shader Resources/Models/Model is missing."); return null; }
        m = new Material(shader) { name = name };
        m.SetColor("_BaseColor", colour);
        var t = UnityEngine.Resources.Load<Texture2D>("Models/Tex/" + name);
        if (t != null)
        {
            t.anisoLevel = 4;
            m.SetTexture("_BaseMap", t);
            if (name.Contains("Leaves")) { m.SetFloat("_Cutoff", 0.4f); m.SetFloat("_Cull", 0f); m.SetFloat("_Shine", 0f); m.renderQueue = 2450; }
        }
        if (name == "C_Sail" || name == "C_TeamSail" || name == "C_Fabric" || name == "C_White" || name == "C_Red") m.SetFloat("_Cull", 0f); // sails are seen from both sides
        if (name == "MI_WindowGlass") m.SetFloat("_Shine", 0.6f);
        materials[key] = m;
        return m;
    }

    /// <summary>Own objects blue, the opponent's red: the same material with the picture of that team, when there is one.</summary>
    public static Material ForTeam(Material m, bool enemy)
    {
        if (m == null) return m;
        if (m.name.StartsWith("C_TeamSail") || m.name == "C_Sail" || m.name == "C_Red" || m.name == "C_Fabric")
            return Material("C_TeamSail", enemy ? new Color(0.80f, 0.22f, 0.17f) : new Color(0.24f, 0.42f, 0.82f));
        string name = m.name.Replace(" (Instance)", "").Replace("_Blue", "").Replace("_Red", ""), team = name + (enemy ? "_Red" : "_Blue");
        return UnityEngine.Resources.Load<Texture2D>("Models/Tex/" + team) != null ? Material(team, Color.white) : m;
    }

    // ---- reading ----
    class Reader
    {
        readonly JObject j; readonly byte[] b; readonly int bin; readonly string key;
        Transform[] nodes; GameObject root;

        public Reader(byte[] bytes, string key)
        {
            this.key = key; b = bytes;
            if (BitConverter.ToUInt32(b, 0) != 0x46546C67) throw new Exception("not a GLB file");
            int n = (int)BitConverter.ToUInt32(b, 12);
            j = JObject.Parse(Encoding.UTF8.GetString(b, 20, n));
            bin = 20 + n + 8;
        }

        // glTF is right-handed, Unity left-handed: mirrored in x
        static Vector3 Pos(float x, float y, float z) { return new Vector3(-x, y, z); }
        static Quaternion Rot(float x, float y, float z, float w) { return new Quaternion(x, -y, -z, w); }

        public GameObject Build(Transform parent)
        {
            root = new GameObject(key);
            root.transform.SetParent(parent, false);
            var list = (JArray)j["nodes"];
            nodes = new Transform[list.Count];
            for (int i = 0; i < list.Count; i++) nodes[i] = new GameObject((string)list[i]["name"] ?? "Node" + i).transform;
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i];
                if (n["children"] != null) foreach (var c in n["children"]) nodes[(int)c].SetParent(nodes[i], false);
                var t = n["translation"]; var r = n["rotation"]; var s = n["scale"];
                if (t != null) nodes[i].localPosition = Pos((float)t[0], (float)t[1], (float)t[2]);
                if (r != null) nodes[i].localRotation = Rot((float)r[0], (float)r[1], (float)r[2], (float)r[3]);
                if (s != null) nodes[i].localScale = new Vector3((float)s[0], (float)s[1], (float)s[2]);
            }
            foreach (var r in j["scenes"][0]["nodes"]) nodes[(int)r].SetParent(root.transform, false);
            for (int i = 0; i < list.Count; i++) if (list[i]["mesh"] != null) AddMesh(i, list[i]);
            if (j["animations"] != null) AddAnimations();
            return root;
        }

        void AddMesh(int node, JToken n)
        {
            var v = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var weights = new List<BoneWeight>();
            var triangles = new List<int[]>(); var mats = new List<Material>();
            foreach (var p in j["meshes"][(int)n["mesh"]]["primitives"])
            {
                var a = p["attributes"]; int first = v.Count;
                var pos = Floats((int)a["POSITION"], out _); var nor = Floats((int)a["NORMAL"], out _);
                for (int i = 0; i < pos.Length; i += 3) { v.Add(Pos(pos[i], pos[i + 1], pos[i + 2])); normals.Add(Pos(nor[i], nor[i + 1], nor[i + 2])); }
                var tex = a["TEXCOORD_0"] != null ? Floats((int)a["TEXCOORD_0"], out _) : null;
                for (int i = 0; i < pos.Length / 3; i++) uv.Add(tex != null ? new Vector2(tex[i * 2], 1f - tex[i * 2 + 1]) : Vector2.zero);
                if (a["JOINTS_0"] != null)
                {
                    var jo = Floats((int)a["JOINTS_0"], out _); var we = Floats((int)a["WEIGHTS_0"], out _);
                    for (int i = 0; i < jo.Length; i += 4)
                        weights.Add(new BoneWeight { boneIndex0 = (int)jo[i], boneIndex1 = (int)jo[i + 1], boneIndex2 = (int)jo[i + 2], boneIndex3 = (int)jo[i + 3], weight0 = we[i], weight1 = we[i + 1], weight2 = we[i + 2], weight3 = we[i + 3] });
                }
                var ix = Ints((int)p["indices"]);
                for (int i = 0; i + 2 < ix.Length; i += 3) { int s = ix[i + 1]; ix[i] += first; ix[i + 1] = ix[i + 2] + first; ix[i + 2] = s + first; } // mirrored: turn the triangles round
                triangles.Add(ix);
                var m = j["materials"][(int)p["material"]]; var c = m["pbrMetallicRoughness"] != null ? m["pbrMetallicRoughness"]["baseColorFactor"] : null;
                mats.Add(Material((string)m["name"], c != null ? new Color((float)c[0], (float)c[1], (float)c[2]).gamma : Color.white));
            }
            var mesh = new Mesh { name = key, hideFlags = HideFlags.DontSave };
            if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v); mesh.SetNormals(normals); mesh.SetUVs(0, uv);
            mesh.subMeshCount = triangles.Count;
            for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
            if (n["skin"] == null || weights.Count == 0)
            {
                mesh.RecalculateBounds();
                nodes[node].gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                nodes[node].gameObject.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
                return;
            }
            // bones
            var skin = j["skins"][(int)n["skin"]]; var joints = (JArray)skin["joints"];
            var ibm = Floats((int)skin["inverseBindMatrices"], out _);
            var bones = new Transform[joints.Count]; var bind = new Matrix4x4[joints.Count];
            var box = new Bounds(); bool any = false;
            for (int i = 0; i < joints.Count; i++)
            {
                bones[i] = nodes[(int)joints[i]];
                var m = new Matrix4x4();
                for (int k = 0; k < 16; k++) m[k] = ibm[i * 16 + k] * ((k % 4 == 0) != (k / 4 == 0) ? -1f : 1f); // column by column; mirrored in x
                bind[i] = m;
                Vector3 at = root.transform.InverseTransformPoint(bones[i].position);
                if (!any) { box = new Bounds(at, Vector3.zero); any = true; } else box.Encapsulate(at);
            }
            mesh.boneWeights = weights.ToArray(); mesh.bindposes = bind;
            var holder = new GameObject("Skin"); holder.transform.SetParent(root.transform, false);
            var r = holder.AddComponent<SkinnedMeshRenderer>();
            r.sharedMesh = mesh; r.bones = bones; r.sharedMaterials = mats.ToArray(); r.quality = SkinQuality.Bone2;
            box.Expand(box.size.magnitude * 0.25f);
            r.localBounds = box;
        }

        void AddAnimations()
        {
            var player = root.AddComponent<Animation>();
            player.playAutomatically = false;
            foreach (var a in j["animations"])
            {
                var clip = new AnimationClip { name = (string)a["name"], legacy = true };
                foreach (var ch in a["channels"])
                {
                    if (ch["target"]["node"] == null) continue;
                    var s = a["samplers"][(int)ch["sampler"]];
                    if ((string)s["interpolation"] == "CUBICSPLINE") continue;
                    var time = Floats((int)s["input"], out _); var val = Floats((int)s["output"], out int width);
                    string path = PathOf(nodes[(int)ch["target"]["node"]]), kind = (string)ch["target"]["path"];
                    string property = kind == "translation" ? "localPosition" : kind == "rotation" ? "localRotation" : kind == "scale" ? "localScale" : null;
                    if (property == null) continue;
                    for (int k = 0; k < width; k++)
                    {
                        float sign = kind == "translation" ? (k == 0 ? -1f : 1f) : kind == "rotation" ? (k == 1 || k == 2 ? -1f : 1f) : 1f;
                        clip.SetCurve(path, typeof(Transform), property + "." + "xyzw"[k], Curve(time, val, width, k, sign));
                    }
                }
                clip.EnsureQuaternionContinuity();
                clip.wrapMode = clip.name == "Death" ? WrapMode.ClampForever : WrapMode.Loop;
                player.AddClip(clip, clip.name);
            }
        }

        static AnimationCurve Curve(float[] time, float[] val, int width, int k, float sign)
        {
            int n = time.Length; bool flat = true;
            for (int i = 1; i < n && flat; i++) flat = Mathf.Abs(val[i * width + k] - val[k]) < 1e-5f;
            if (flat) return new AnimationCurve(new Keyframe(time[0], val[k] * sign), new Keyframe(Mathf.Max(time[n - 1], time[0] + 0.01f), val[k] * sign));
            var keys = new Keyframe[n];
            for (int i = 0; i < n; i++)
            {
                float left = i > 0 ? (val[i * width + k] - val[(i - 1) * width + k]) / Mathf.Max(1e-4f, time[i] - time[i - 1]) : 0f;
                float right = i < n - 1 ? (val[(i + 1) * width + k] - val[i * width + k]) / Mathf.Max(1e-4f, time[i + 1] - time[i]) : left;
                keys[i] = new Keyframe(time[i], val[i * width + k] * sign, (i > 0 ? left : right) * sign, right * sign);
            }
            return new AnimationCurve(keys);
        }

        string PathOf(Transform t)
        {
            string p = t.name;
            for (t = t.parent; t != null && t != root.transform; t = t.parent) p = t.name + "/" + p;
            return p;
        }

        // ---- numbers ----
        float[] Floats(int accessor, out int width)
        {
            var a = j["accessors"][accessor]; var v = j["bufferViews"][(int)a["bufferView"]];
            string type = (string)a["type"]; width = type == "SCALAR" ? 1 : type == "VEC2" ? 2 : type == "VEC3" ? 3 : type == "VEC4" ? 4 : 16;
            int kind = (int)a["componentType"], size = kind == 5126 || kind == 5125 ? 4 : kind == 5123 || kind == 5122 ? 2 : 1, count = (int)a["count"];
            int start = bin + ((int?)v["byteOffset"] ?? 0) + ((int?)a["byteOffset"] ?? 0), stride = (int?)v["byteStride"] ?? size * width;
            bool normal = (bool?)a["normalized"] ?? false;
            var f = new float[count * width];
            for (int i = 0; i < count; i++)
                for (int k = 0; k < width; k++)
                {
                    int o = start + i * stride + k * size;
                    f[i * width + k] = kind == 5126 ? BitConverter.ToSingle(b, o) : kind == 5125 ? BitConverter.ToUInt32(b, o) : kind == 5123 ? BitConverter.ToUInt16(b, o) * (normal ? 1f / 65535f : 1f)
                                     : kind == 5122 ? BitConverter.ToInt16(b, o) : b[o] * (normal ? 1f / 255f : 1f);
                }
            return f;
        }

        int[] Ints(int accessor)
        {
            var a = j["accessors"][accessor]; var v = j["bufferViews"][(int)a["bufferView"]];
            int kind = (int)a["componentType"], size = kind == 5125 ? 4 : kind == 5123 ? 2 : 1, count = (int)a["count"], start = bin + ((int?)v["byteOffset"] ?? 0) + ((int?)a["byteOffset"] ?? 0);
            var r = new int[count];
            for (int i = 0; i < count; i++) r[i] = kind == 5125 ? (int)BitConverter.ToUInt32(b, start + i * size) : kind == 5123 ? BitConverter.ToUInt16(b, start + i * size) : b[start + i];
            return r;
        }
    }
}
