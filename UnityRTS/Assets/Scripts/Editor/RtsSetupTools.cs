using System.Collections.Generic;
using System.IO;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Saves editor work: creates the placeholder prefabs (capsules, cubes) and fills the open Game scene with the map markers,
/// build slots, resource nodes, GameSpawner and the gameplay "Systems". UI canvases are still made by hand (see TODO.md).
///   Tools > RTS > 1 Create placeholder prefabs
///   Tools > RTS > 2 Setup open Game scene          (open Assets/Scenes/Game.unity first)
/// Map layout (x -100..100, z -70..80): land up to z = 45, sea north of it; bases at x = -65 / +65.
/// Everything here is a starting point: move the objects in the scene as you like.
/// </summary>
public static class RtsSetupTools
{
    const string PrefabDir = "Assets/Prefabs";

    static readonly string[,] Units =   // key, prefab name, colour (r,g,b), scale
    {
        { "villager", "Villager", "0.9,0.8,0.5", "0.8" }, { "spearman", "Spearman", "0.7,0.7,0.7", "1.0" },
        { "archer", "Archer", "0.3,0.7,0.3", "0.9" }, { "cavalry", "Cavalry", "0.6,0.4,0.2", "1.3" },
        { "scout", "Scout", "0.9,0.9,0.2", "0.8" }, { "catapult", "Catapult", "0.4,0.3,0.3", "1.5" },
        { "raider_boat", "RaiderBoat", "0.2,0.5,0.9", "1.2" }, { "warship", "Warship", "0.1,0.3,0.7", "1.8" },
    };
    static readonly string[,] Buildings =
    {
        { "town_center", "TownCenter", "0.8,0.8,0.9", "6" }, { "house", "House", "0.8,0.6,0.5", "3" },
        { "barracks", "Barracks", "0.6,0.3,0.3", "4" }, { "stable", "Stable", "0.5,0.4,0.2", "4" },
        { "blacksmith", "Blacksmith", "0.3,0.3,0.3", "4" }, { "dock", "Dock", "0.3,0.4,0.6", "4" },
    };

    [MenuItem("Tools/RTS/1 Create placeholder prefabs")]
    static void CreatePrefabs()
    {
        EnsureFolder("Assets", "Prefabs"); EnsureFolder(PrefabDir, "Materials");

        var player = new GameObject("Player");
        player.AddComponent<NetworkObject>(); player.AddComponent<NetworkPlayerState>(); player.AddComponent<CommandRelay>();
        Save(player, "Player");

        for (int i = 0; i < Units.GetLength(0); i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = Units[i, 1]; Colorize(go, Units[i, 1], Units[i, 2]);
            float s = float.Parse(Units[i, 3], System.Globalization.CultureInfo.InvariantCulture); go.transform.localScale = new Vector3(s, s, s);
            go.AddComponent<NetworkObject>(); go.AddComponent<NetworkTransform>(); go.AddComponent<NetworkUnit>(); go.AddComponent<UnitCombat>();
            if (Units[i, 0] == "villager") go.AddComponent<VillagerGatherer>();
            Save(go, Units[i, 1]);
        }
        for (int i = 0; i < Buildings.GetLength(0); i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = Buildings[i, 1]; Colorize(go, Buildings[i, 1], Buildings[i, 2]);
            float s = float.Parse(Buildings[i, 3], System.Globalization.CultureInfo.InvariantCulture); go.transform.localScale = new Vector3(s, s * 0.6f, s);
            go.AddComponent<NetworkObject>(); go.AddComponent<NetworkTransform>(); go.AddComponent<NetworkBuilding>();
            Save(go, Buildings[i, 1]);
        }
        MakeNode("Tree", "wood", PrimitiveType.Cylinder, "0.1,0.5,0.1", new Vector3(1f, 2f, 1f));
        MakeNode("BerryBush", "food", PrimitiveType.Sphere, "0.7,0.1,0.3", new Vector3(1.4f, 1f, 1.4f));
        MakeNode("GoldMine", "gold", PrimitiveType.Cube, "1,0.85,0.1", new Vector3(2f, 1.5f, 2f));
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("RTS: placeholder prefabs created in " + PrefabDir + ". Check Fusion's Network Prefab Table if a prefab is missing there.");
    }

    [MenuItem("Tools/RTS/2 Setup open Game scene")]
    static void SetupScene()
    {
        if (!File.Exists(PrefabDir + "/Player.prefab")) CreatePrefabs();
        var root = new GameObject("MapAndSystems");

        // ground and sea (visual only; zones are the logical map)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Ground";
        ground.transform.SetParent(root.transform); ground.transform.position = new Vector3(0, -0.5f, -12.5f); ground.transform.localScale = new Vector3(200, 1, 115);
        ground.GetComponent<Renderer>().sharedMaterial = Mat("GroundMat", "0.35,0.55,0.3");
        var sea = GameObject.CreatePrimitive(PrimitiveType.Cube); sea.name = "Sea";
        sea.transform.SetParent(root.transform); sea.transform.position = new Vector3(0, -0.6f, 62.5f); sea.transform.localScale = new Vector3(200, 1, 35);
        sea.GetComponent<Renderer>().sharedMaterial = Mat("SeaMat", "0.1,0.3,0.7");

        // zone centres: nearest centre wins, so the coast lies halfway between a land and a water centre (z = 45)
        var zones = new Dictionary<ZoneVolume.Zone, Vector3>
        {
            { ZoneVolume.Zone.P1Base, new Vector3(-65, 0, 25) }, { ZoneVolume.Zone.P2Base, new Vector3(65, 0, 25) },
            { ZoneVolume.Zone.Center, new Vector3(0, 0, 0) }, { ZoneVolume.Zone.NorthGate, new Vector3(0, 0, 30) },
            { ZoneVolume.Zone.WestMine, new Vector3(-65, 0, -30) }, { ZoneVolume.Zone.SouthField, new Vector3(0, 0, -35) },
            { ZoneVolume.Zone.EastForest, new Vector3(65, 0, -30) },
            { ZoneVolume.Zone.P1Harbor, new Vector3(-65, 0, 65) }, { ZoneVolume.Zone.OpenSea, new Vector3(0, 0, 60) },
            { ZoneVolume.Zone.P2Harbor, new Vector3(65, 0, 65) },
        };
        var zoneRoot = new GameObject("Zones"); zoneRoot.transform.SetParent(root.transform);
        foreach (var kv in zones)
        {
            var z = new GameObject("Zone_" + kv.Key); z.transform.SetParent(zoneRoot.transform); z.transform.position = kv.Value;
            z.AddComponent<ZoneVolume>().zone = kv.Key;
        }
        zoneRoot.AddComponent<ZoneMap>();

        // build slots around each base; the two slots toward the sea are coastal
        var slotRoot = new GameObject("BuildSlots"); slotRoot.transform.SetParent(root.transform);
        foreach (int side in new[] { -1, 1 })
        {
            Vector3 b = new Vector3(65 * side, 0, 25);
            for (int i = 0; i < 6; i++)
            {
                var s = new GameObject("Slot_" + (side < 0 ? "P1_" : "P2_") + i); s.transform.SetParent(slotRoot.transform);
                s.transform.position = b + new Vector3(-14 + (i % 3) * 14, 0, i < 3 ? -12 : 0);
                s.AddComponent<BuildSlot>();
            }
            for (int i = 0; i < 2; i++)
            {
                var s = new GameObject("Slot_" + (side < 0 ? "P1" : "P2") + "_Coast" + i); s.transform.SetParent(slotRoot.transform);
                s.transform.position = b + new Vector3(-8 + i * 16, 0, 15);
                s.AddComponent<BuildSlot>().coastal = true;
            }
        }

        // resource nodes (scene objects with a NetworkObject: Fusion spawns them when the scene loads through the runner)
        var nodeRoot = new GameObject("Resources"); nodeRoot.transform.SetParent(root.transform);
        PlaceNodes(nodeRoot.transform, "Tree", new Vector3(65, 0, -30), 8, 6f);
        PlaceNodes(nodeRoot.transform, "BerryBush", new Vector3(0, 0, -35), 6, 5f);
        PlaceNodes(nodeRoot.transform, "GoldMine", new Vector3(-65, 0, -30), 5, 7f);

        // spawner
        var spawn = new GameObject("Spawner"); spawn.transform.SetParent(root.transform);
        var p1 = new GameObject("P1Spawn"); p1.transform.SetParent(spawn.transform); p1.transform.position = new Vector3(-65, 0, 25);
        var p2 = new GameObject("P2Spawn"); p2.transform.SetParent(spawn.transform); p2.transform.position = new Vector3(65, 0, 25);
        var gs = spawn.AddComponent<GameSpawner>();
        var so = new SerializedObject(gs);
        so.FindProperty("playerPrefab").objectReferenceValue = Prefab("Player").GetComponent<NetworkObject>();
        so.FindProperty("p1Spawn").objectReferenceValue = p1.transform; so.FindProperty("p2Spawn").objectReferenceValue = p2.transform;
        FillEntries(so.FindProperty("unitPrefabs"), Units); FillEntries(so.FindProperty("buildingPrefabs"), Buildings);
        so.ApplyModifiedPropertiesWithoutUndo();

        // gameplay systems and camera
        var sys = new GameObject("Systems"); sys.transform.SetParent(root.transform);
        sys.AddComponent<SelectionManager>(); sys.AddComponent<UnitCommandInput>(); sys.AddComponent<BuildPlacement>();
        var fog = sys.AddComponent<FogOfWar>();
        var fso = new SerializedObject(fog);
        fso.FindProperty("mapCentre").vector2Value = new Vector2(0, 5); fso.FindProperty("mapSize").vector2Value = new Vector2(210, 160);
        fso.ApplyModifiedPropertiesWithoutUndo();
        var cam = Camera.main;
        if (cam != null)
        {
            if (cam.GetComponent<RtsCamera>() == null) cam.gameObject.AddComponent<RtsCamera>();
            cam.transform.position = new Vector3(-65, 28, 5); cam.transform.rotation = Quaternion.Euler(55, 0, 0);
        }
        else Debug.LogWarning("RTS: no Main Camera (tag MainCamera) in the scene: add one and an RtsCamera component.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("RTS: Game scene set up under 'MapAndSystems'. Save the scene (Ctrl+S). UI canvases are still manual (TODO.md section 4).");
    }

    // ---------- helpers ----------
    static void MakeNode(string name, string resource, PrimitiveType shape, string colour, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(shape); go.name = name; go.transform.localScale = scale; Colorize(go, name, colour);
        go.AddComponent<NetworkObject>();
        var node = go.AddComponent<ResourceNode>();
        var so = new SerializedObject(node);
        so.FindProperty("resource").stringValue = resource; so.FindProperty("startAmount").floatValue = 200f;
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(go, name);
    }

    static void PlaceNodes(Transform parent, string prefabName, Vector3 centre, int count, float spacing)
    {
        var prefab = Prefab(prefabName);
        for (int i = 0; i < count; i++)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = centre + new Vector3((i % 3 - 1) * spacing, 0.5f, (i / 3 - 1) * spacing);
        }
    }

    static void FillEntries(SerializedProperty list, string[,] table)
    {
        list.arraySize = table.GetLength(0);
        for (int i = 0; i < table.GetLength(0); i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("key").stringValue = table[i, 0];
            e.FindPropertyRelative("prefab").objectReferenceValue = Prefab(table[i, 1]).GetComponent<NetworkObject>();
        }
    }

    static GameObject Prefab(string name) { return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + name + ".prefab"); }

    static void Save(GameObject go, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
    }

    static void Colorize(GameObject go, string name, string rgb) { go.GetComponent<Renderer>().sharedMaterial = Mat(name + "Mat", rgb); }

    static Material Mat(string name, string rgb)
    {
        EnsureFolder("Assets", "Prefabs"); EnsureFolder(PrefabDir, "Materials");
        string path = PrefabDir + "/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var p = rgb.Split(',');
        var c = new Color(float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture));
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { color = c };
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }
}
