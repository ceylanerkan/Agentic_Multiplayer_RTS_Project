using System.Collections.Generic;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Puts the model on a unit, building or resource (local, only to look at: colliders, speed and all rules stay the same). Units turn
/// to where they go or fight and play their animations; buildings grow out of the ground while they are built. The placeholder shape
/// of the prefab is removed. Added by EntityVisuals.
/// </summary>
public class ModelLook : MonoBehaviour
{
    class Def
    {
        public string File, Rider;       // a file of GlbModel, or a character of Resources/Models/Units
        public float Height, Turn, Scale = 1f;
        public bool Person, Spear;
        public string Attack = "Attack", Work = "PickUp", Idle = "Idle";
    }

    static readonly Dictionary<string, Def> Units = new Dictionary<string, Def>
    {
        { "villager", new Def { File = "Monk", Person = true, Height = 1.9f } },
        { "spearman", new Def { File = "Warrior", Person = true, Height = 2.15f, Spear = true, Attack = "Sword_Attack", Idle = "Idle_Weapon" } },
        { "archer", new Def { File = "Ranger", Person = true, Height = 2.05f, Attack = "Bow_Attack_Shoot", Idle = "Idle_Weapon" } },
        { "cavalry", new Def { File = "horse", Scale = 0.52f, Rider = "spearman" } },
        { "scout", new Def { File = "horse", Scale = 0.45f, Rider = "archer" } },
        { "catapult", new Def { File = "catapult", Turn = 180f } },
        { "fishing_boat", new Def { File = "fishing_boat" } }, { "raider_boat", new Def { File = "raider_boat" } }, { "warship", new Def { File = "warship" } },
    };
    static readonly string[] Trees = { "tree1", "tree4", "tree2", "tree6", "tree3", "tree4", "tree5" };

    /// <summary>Height of the model over the foot, for labels and bars; 0 = no model.</summary>
    public float Height { get; private set; }
    /// <summary>Wide ones (riders, ships, catapults) get a wider ring.</summary>
    public bool Wide { get { return def != null && !def.Person; } }
    public bool Ship { get { return ship; } }

    NetworkUnit unit; NetworkBuilding building;
    Transform pivot, model;
    Animation anim; Def def;
    Animation riderAnim; Def riderDef;
    readonly List<Renderer> renderers = new List<Renderer>();
    Vector3 last; float speed, yaw, yawGoal, progress = -1f, nextLook, fightUntil;
    bool ship;

    // ---- attaching ----
    public static ModelLook Attach(GameObject go, NetworkUnit unit, NetworkBuilding building, ResourceNode node)
    {
        string key = unit != null ? unit.TypeKey.ToString() : building != null ? building.TypeKey.ToString() : null;
        Def d = null; string file = null;
        if (unit != null && Units.TryGetValue(key, out d)) file = d.File;
        else if (building != null && !BuildSite.IsWall(key)) file = key;
        else if (node != null) file = node.InWater ? null : node.Resource == "gold" ? "gold" : node.Resource == "food" ? "berry" : Trees[Mathf.Abs(Mathf.RoundToInt(go.transform.position.x * 13f + go.transform.position.z * 7f)) % Trees.Length];
        if (file == null) { if (node != null && node.InWater) Fish(go); return null; }

        var pivot = new GameObject("Model").transform;
        pivot.SetParent(go.transform, false);
        var m = d != null && d.Person ? Person(file, pivot) : GlbModel.Spawn(file, pivot);
        if (m == null) { Destroy(pivot.gameObject); return null; } // no file: the placeholder stays

        // at the foot of the collider, in metres whatever the scale of the prefab
        var col = go.GetComponent<Collider>();
        Vector3 s = go.transform.localScale;
        float foot = col != null ? -col.bounds.extents.y : 0f; // not bounds.min: physics may still know the old place of a new object
        if (building != null) foot += 0.2f;                   // GameSpawner sinks buildings a little, the models have a deep foundation instead
        if (node != null) pivot.localPosition = new Vector3(0, Local(foot, s.y), 0); // trees and stones keep the size of their place
        else { pivot.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z); pivot.localPosition = new Vector3(0, Local(foot, s.y), 0); }

        // the placeholder goes
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            if (r != null && !r.transform.IsChildOf(pivot)) { if (r.gameObject == go) { var f = go.GetComponent<MeshFilter>(); DestroyImmediate(r); if (f != null) Destroy(f); } else DestroyImmediate(r.gameObject); }
        FogOfWar.Forget(go);

        var look = go.AddComponent<ModelLook>();
        look.Set(unit, building, pivot, m.transform, d);
        return look;
    }

    // the six shapes of a shoal become fish; the circle they swim in stays
    static void Fish(GameObject go)
    {
        var school = go.GetComponentInChildren<FishSchool>();
        if (school == null) return;
        var old = new List<Transform>();
        foreach (Transform c in school.transform) if (c.GetComponent<Renderer>() != null) old.Add(c);
        foreach (var c in old)
        {
            var fish = GlbModel.Spawn("fish", school.transform);
            if (fish == null) return;
            fish.transform.localPosition = c.localPosition + Vector3.down * 0.12f; fish.transform.localRotation = Quaternion.Euler(0, c.localEulerAngles.y + 180f, 0);
            fish.transform.localScale = Vector3.one * (0.8f + 0.1f * (old.IndexOf(c) % 4));
            DestroyImmediate(c.gameObject);
        }
        foreach (Transform c in go.transform)
        {
            var r = c.GetComponent<Renderer>();
            if (r == null) continue;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) break;
            if (shoal == null) shoal = new Material(shader) { mainTexture = EntityVisuals.GlowTexture(), color = new Color(0.01f, 0.06f, 0.10f, 0.55f), renderQueue = 2950 };
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false); quad.transform.localPosition = new Vector3(0, -0.02f, 0); quad.transform.localRotation = Quaternion.Euler(90, 0, 0); quad.transform.localScale = Vector3.one * 3.6f;
            quad.GetComponent<Renderer>().sharedMaterial = shoal;
            DestroyImmediate(c.gameObject);
            break;
        }
        FogOfWar.Forget(go);
    }

    static Material shoal;

    static float Local(float metres, float scale) { return metres / Mathf.Max(0.01f, scale); }

    /// <summary>A character: FBX from Resources/Models/Units, set to its height, with our materials.</summary>
    static GameObject Person(string file, Transform parent)
    {
        var prefab = UnityEngine.Resources.Load<GameObject>("Models/Units/" + file);
        if (prefab == null) { Debug.LogWarning("Models/Units/" + file + " is not imported yet."); return null; }
        var go = Instantiate(prefab, parent, false);
        go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                string n = mats[i] != null ? mats[i].name : "";
                mats[i] = n.Contains("Sword") ? GlbModel.Material("Warrior_Sword_Texture", Color.white) : n.Contains("Bow") ? GlbModel.Material("Bow_Texture", Color.white) : GlbModel.Material(file + "_Blue", Color.white);
            }
            r.sharedMaterials = mats;
            if (r is SkinnedMeshRenderer sk) sk.quality = SkinQuality.Bone2;
        }
        return go;
    }

    void Set(NetworkUnit u, NetworkBuilding b, Transform pivot, Transform model, Def d)
    {
        unit = u; building = b; this.pivot = pivot; this.model = model; def = d;
        if (d != null)
        {
            ship = u != null && u.IsShip;
            if (d.Person) Fit(model, d.Height);
            else model.localScale = Vector3.one * d.Scale;
            model.localRotation = Quaternion.Euler(0, d.Turn, 0);
            anim = Prepare(model, d);
            if (d.Spear) GiveSpear(model);
            if (d.Rider != null) AddRider(d);
        }
        // sheep's clothing for the height: the renderers know it after the first pose
        Bounds box = new Bounds(transform.position, Vector3.zero); bool any = false;
        foreach (var r in pivot.GetComponentsInChildren<Renderer>(true)) { renderers.Add(r); if (!any) { box = r.bounds; any = true; } else box.Encapsulate(r.bounds); }
        Height = any ? Mathf.Clamp(box.max.y - pivot.position.y, 0.5f, 9f) : 0f;
        if (d != null && d.Person) Height = d.Height;
        if (d != null && d.Rider != null) Height = 3.1f;
        last = transform.position;
        yaw = yawGoal = u != null ? (transform.position.x > 0 ? 200f : 160f) : 0f; // towards the camera
        if (u != null) Turn();
    }

    static Animation Prepare(Transform model, Def d)
    {
        var a = model.GetComponentInChildren<Animation>();
        if (a == null) return null;
        a.playAutomatically = false; a.cullingType = AnimationCullingType.BasedOnRenderers;
        foreach (AnimationState s in a) s.wrapMode = s.name.EndsWith("Death") ? WrapMode.ClampForever : WrapMode.Loop;
        Play(a, d.Idle);
        foreach (AnimationState s in a) s.time = Random.value * s.length; // not all in step
        return a;
    }

    /// <summary>Scales a character to its height (the length from the feet to the top of the head bone).</summary>
    static void Fit(Transform model, float height)
    {
        Transform head = Find(model, "Head_end") ?? Find(model, "Head");
        float h = 0f;
        if (head != null) { model.localScale = Vector3.one; h = (model.parent.InverseTransformPoint(head.position) - model.localPosition).y; }
        if (h > 0.01f) model.localScale = Vector3.one * (height * 0.9f / h);
    }

    static Transform Find(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var f = Find(c, name); if (f != null) return f; }
        return null;
    }

    static void Hide(Transform model, string part) { foreach (var r in model.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains(part)) r.enabled = false; }

    // the warrior's sword becomes a spear: a shaft along the same direction in the hand
    static void GiveSpear(Transform model)
    {
        Renderer sword = null;
        foreach (var r in model.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Sword")) sword = r;
        Transform hand = Find(model, "Weapon.R");
        if (sword == null || hand == null) return;
        Mesh mesh = sword is SkinnedMeshRenderer sk ? sk.sharedMesh : sword.GetComponent<MeshFilter>() != null ? sword.GetComponent<MeshFilter>().sharedMesh : null;
        if (mesh == null) return;
        // the long axis of the sword in the space of the hand bone
        Matrix4x4 toHand = hand.worldToLocalMatrix * sword.transform.localToWorldMatrix;
        if (sword is SkinnedMeshRenderer s2)
            for (int i = 0; i < s2.bones.Length; i++) if (s2.bones[i] == hand) toHand = mesh.bindposes[i];
        Vector3 e = mesh.bounds.extents, axis = e.x > e.y && e.x > e.z ? Vector3.right : e.y > e.z ? Vector3.up : Vector3.forward;
        Vector3 a = toHand.MultiplyPoint3x4(mesh.bounds.center - Vector3.Scale(axis, e)), b = toHand.MultiplyPoint3x4(mesh.bounds.center + Vector3.Scale(axis, e));
        if (a.magnitude > b.magnitude) { var t = a; a = b; b = t; } // a = hilt
        Vector3 along = b - a;
        sword.gameObject.SetActive(false);
        var spear = new GameObject("Spear").transform;
        spear.SetParent(hand, false);
        spear.localPosition = a + along * 0.45f;
        spear.localRotation = Quaternion.FromToRotation(Vector3.up, along.normalized);
        float length = along.magnitude * 1.9f;
        Stick(spear, GlbModel.Material("C_Shaft", new Color(0.42f, 0.28f, 0.16f)), Vector3.zero, new Vector3(length * 0.028f, length * 0.5f, length * 0.028f), PrimitiveType.Cylinder);
        Stick(spear, GlbModel.Material("C_Tip", new Color(0.75f, 0.77f, 0.8f)), Vector3.up * length * 0.53f, new Vector3(length * 0.06f, length * 0.07f, length * 0.025f), PrimitiveType.Sphere);
    }

    static void Stick(Transform parent, Material m, Vector3 at, Vector3 size, PrimitiveType shape)
    {
        var go = GameObject.CreatePrimitive(shape);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    void AddRider(Def d)
    {
        riderDef = Units[d.Rider];
        var seat = Find(model, "Torso2") ?? model;
        var holder = new GameObject("Rider").transform;
        holder.SetParent(pivot, false);
        holder.position = new Vector3(seat.position.x, seat.position.y, seat.position.z);
        // legs inside the horse: the rider is set low and small
        var rider = Person(riderDef.File, holder);
        if (rider == null) return;
        Fit(rider.transform, riderDef.Height * 0.86f);
        rider.transform.localPosition = new Vector3(0, -0.42f, -0.12f);
        riderAnim = Prepare(rider.transform, riderDef);
        if (riderDef.Spear) GiveSpear(rider.transform);
        holder.SetParent(seat, true); // moves with the back of the horse
    }

    // ---- team ----
    public void SetTeam(bool enemy)
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = GlbModel.ForTeam(mats[i], enemy);
            r.sharedMaterials = mats;
        }
    }

    // ---- every frame ----
    static void Play(Animation a, string name, float speed = 1f, bool fade = false)
    {
        if (a == null) return;
        AnimationState found = null;
        foreach (AnimationState s in a) if (s.name == name || s.name.EndsWith("|" + name)) { found = s; break; }
        if (found == null) return;
        found.speed = speed;
        if (a.IsPlaying(found.name)) return;
        if (fade) a.CrossFade(found.name, 0.18f); else a.Play(found.name);
    }

    void LateUpdate()
    {
        if (building != null) { Grow(); return; }
        if (unit == null || unit.Object == null || !unit.Object.IsValid) return;
        Vector3 p = transform.position, d = p - last; d.y = 0; last = p;
        float now = d.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
        speed = Mathf.Lerp(speed, now > 25f ? 0f : now, 1f - Mathf.Exp(-12f * Time.deltaTime));
        bool moving = speed > 0.25f;
        if (moving && d.sqrMagnitude > 1e-7f) yawGoal = Quaternion.LookRotation(d).eulerAngles.y;
        if (Time.time >= nextLook) { nextLook = Time.time + 0.25f; LookForFight(moving); }
        bool fighting = !moving && Time.time < fightUntil;
        yaw = Mathf.LerpAngle(yaw, yawGoal, 1f - Mathf.Exp(-(ship ? 2.5f : 9f) * Time.deltaTime));
        Turn();
        if (anim != null)
        {
            if (moving) Play(anim, speed > 2.2f ? "Run" : "Walk", Mathf.Clamp(speed / (def.Rider != null ? 5.5f : 3.6f), 0.7f, 1.6f), true);
            else if (fighting) Play(anim, def.Attack, 1f, true);
            else if (Working()) Play(anim, def.Work, 0.9f, true);
            else Play(anim, def.Idle, 1f, true);
        }
        if (riderAnim != null) Play(riderAnim, fighting ? riderDef.Attack : riderDef.Idle, 1f, true);
        if (ship) pivot.localRotation = pivot.localRotation * Quaternion.Euler(1.2f * Mathf.Sin(Time.time * 0.9f + p.x), 0, 2.2f * Mathf.Sin(Time.time * 0.7f + p.z));
    }

    void Turn() { pivot.localRotation = Quaternion.Euler(0, yaw, 0); }

    bool Working() { var g = GetComponent<VillagerGatherer>(); return g != null && g.Busy && !unit.HasMoveTarget; }

    /// <summary>Does it fight now? Seen from what stands within reach; the host alone knows the automatic targets.</summary>
    void LookForFight(bool moving)
    {
        if (moving || !GameDataProvider.Get().Units.TryGetValue(unit.TypeKey.ToString(), out var me)) return;
        bool ordered = unit.AttackTarget.IsValid;
        if (!ordered && unit.GetComponent<VillagerGatherer>() != null) return; // Villagers never start fights
        Vector3 p = transform.position; float best = float.MaxValue; Vector3 at = Vector3.zero;
        foreach (var u in NetworkUnit.All)
        {
            if (u.Object == null || !u.Object.IsValid || u.Owner == unit.Owner) continue;
            Vector3 d = u.transform.position - p; d.y = 0;
            if (d.magnitude < me.Range + GameConstants.UnitReach + 1.2f && d.magnitude < best) { best = d.magnitude; at = u.transform.position; }
        }
        if (best == float.MaxValue)
            foreach (var b in NetworkBuilding.All)
            {
                if (b.Object == null || !b.Object.IsValid || b.Owner == unit.Owner) continue;
                Vector3 d = b.transform.position - p; d.y = 0;
                if (d.magnitude < me.Range + GameConstants.BuildingReach + 0.6f && d.magnitude < best) { best = d.magnitude; at = b.transform.position; }
            }
        if (best == float.MaxValue) return;
        fightUntil = Time.time + 0.6f;
        Vector3 to = at - p; to.y = 0;
        if (to.sqrMagnitude > 0.01f) yawGoal = Quaternion.LookRotation(to).eulerAngles.y;
    }

    void Grow()
    {
        if (building.Object == null || !building.Object.IsValid) return;
        float p = building.Completed ? 1f : building.BuildProgress;
        if (p == progress) return;
        progress = p;
        model.localScale = new Vector3(1f, Mathf.Lerp(0.12f, 1f, p), 1f);
    }

    /// <summary>A dying unit leaves its model behind for a moment, falling.</summary>
    public void Fall()
    {
        if (model == null || pivot == null || renderers.Count == 0 || renderers[0] == null || !renderers[0].enabled) return; // not seen: nothing to show
        pivot.SetParent(null, true);
        if (anim != null) Play(anim, "Death", 1.2f, true);
        Destroy(pivot.gameObject, anim != null ? 2.4f : 0.2f);
        pivot = null;
    }
}
