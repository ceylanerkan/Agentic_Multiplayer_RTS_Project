using System.Collections.Generic;
using System.IO;
using Fusion;
using UnityEditor;
using TMPro;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Saves editor work: creates the placeholder prefabs (capsules, cubes) and fills the open Game scene with the map markers,
/// resource nodes, GameSpawner, the gameplay "Systems" and the UI (HUD, selection panel, game over, AI panel).
///   Tools > RTS > 0 Create Menu and Game scenes     (new scenes, Build Settings, then runs 1-3 for you)
///   Tools > RTS > 1 Create placeholder prefabs
///   Tools > RTS > 2 Setup open Game scene          (open Assets/Scenes/Game.unity first)
///   Tools > RTS > 3 Create Game UI                 (same scene; the panels build themselves, see UiKit)
/// Map layout (about x -105..105, z -75..80, mountains around it): land up to z = 45, sea north of it; bases at x = -65 / +65.
/// Everything here is a starting point: move the objects in the scene as you like.
/// </summary>
public static class RtsSetupTools
{
    const string PrefabDir = "Assets/Prefabs";

    static readonly string[,] Units =   // key, prefab name, colour (r,g,b), scale
    {
        { "villager", "Villager", "0.95,0.85,0.55", "1.2" }, { "spearman", "Spearman", "0.75,0.75,0.8", "1.4" },
        { "archer", "Archer", "0.35,0.8,0.35", "1.3" }, { "cavalry", "Cavalry", "0.65,0.4,0.2", "1.8" },
        { "scout", "Scout", "1,0.6,0.1", "1.2" }, { "catapult", "Catapult", "0.45,0.3,0.25", "2.0" },
        { "raider_boat", "RaiderBoat", "0.3,0.85,0.95", "1.8" }, { "warship", "Warship", "0.15,0.25,0.5", "2.4" },
        { "fishing_boat", "FishingBoat", "0.9,0.9,0.8", "1.5" },
    };
    static readonly string[,] Buildings =
    {
        { "town_center", "TownCenter", "0.8,0.8,0.9", "6" }, { "house", "House", "0.8,0.6,0.5", "3" },
        { "barracks", "Barracks", "0.6,0.3,0.3", "4" }, { "stable", "Stable", "0.5,0.4,0.2", "4" },
        { "blacksmith", "Blacksmith", "0.3,0.3,0.3", "4" }, { "dock", "Dock", "0.3,0.4,0.6", "4" },
        { "wall", "Wall", "0.45,0.32,0.2", "2" }, { "gate", "Gate", "0.45,0.32,0.2", "2" },
    };

    [MenuItem("Tools/RTS/0 Create Menu and Game scenes")]
    static void CreateScenes()
    {
        EnsureFolder("Assets", "Scenes");
        const string menuPath = "Assets/Scenes/Menu.unity", gamePath = "Assets/Scenes/Game.unity";
        if (File.Exists(menuPath) || File.Exists(gamePath)) { Debug.LogError("RTS: Assets/Scenes/Menu.unity or Game.unity already exists; delete it first or use commands 1-3 on your own scene."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Menu: launcher, landscape and an empty canvas (MenuUI builds the pages itself)
        var menu = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        new GameObject("Launcher", typeof(GameLauncher));
        new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MenuUI));
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        FurnishMenu();
        EditorSceneManager.SaveScene(menu, menuPath);

        // Game: map, systems and UI
        var game = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(game, gamePath);
        SetupScene(); CreateUi();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(menuPath, true), new EditorBuildSettingsScene(gamePath, true) };
        PlayFromMenu.Apply();   // Play now starts in Menu even though Game is the open scene
        Debug.Log("RTS: scenes Menu and Game created and added to the Build Settings (Menu first). Open Menu and press Play, or use ParrelSync.");
    }

    /// <summary>
    /// Deletes the generated Menu and Game scenes and Assets/Prefabs, then runs command 0 again. Use it after the agent
    /// changes this file (new look, fixed fonts); anything you changed by hand in those scenes or prefabs is lost.
    /// </summary>
    [MenuItem("Tools/RTS/Rebuild generated scenes and prefabs")]
    static void Rebuild()
    {
        if (!EditorUtility.DisplayDialog("Rebuild RTS scenes and prefabs",
                "Deletes Assets/Scenes/Menu.unity, Assets/Scenes/Game.unity and Assets/Prefabs, then creates them again. Changes you made by hand there are lost.",
                "Rebuild", "Cancel")) return;
        if (EditorApplication.isPlaying) { Debug.LogError("RTS: stop Play mode before rebuilding."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // the open scene may be one we delete
        foreach (var path in new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Game.unity", PrefabDir })
            if (File.Exists(path) || AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.Refresh();
        PlayFromMenu.Apply();   // the Menu scene is gone: do not keep pointing play mode at it if CreateScenes stops early
        CreateScenes();
    }

    /// <summary>
    /// Windows build: a resizable 1280x720 window (16:9), so two instances fit side by side on the demo machine,
    /// and Run In Background, so the window without focus keeps simulating. The UI scales to any window size.
    /// </summary>
    [MenuItem("Tools/RTS/Set build window (1280x720, windowed)")]
    static void SetBuildWindow()
    {
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();
        Debug.Log("RTS: build window set to 1280x720, windowed, resizable, run in background.");
    }

    [MenuItem("Tools/RTS/1 Create placeholder prefabs")]
    static void CreatePrefabsMenu()
    {
        // Overwriting existing prefabs can leave the GameSpawner references in an existing Game scene "Missing".
        if (File.Exists(PrefabDir + "/Player.prefab") &&
            !EditorUtility.DisplayDialog("Prefabs already exist", "Overwrite Assets/Prefabs? References in existing scenes may break; " +
                "'Tools > RTS > Rebuild generated scenes and prefabs' recreates the scenes too.", "Overwrite", "Cancel")) return;
        CreatePrefabs();
    }

    static void CreatePrefabs()
    {
        EnsureFolder("Assets", "Prefabs"); EnsureFolder(PrefabDir, "Materials");

        var player = new GameObject("Player");
        player.AddComponent<NetworkObject>(); player.AddComponent<NetworkPlayerState>(); player.AddComponent<CommandRelay>();
        Save(player, "Player");

        for (int i = 0; i < Units.GetLength(0); i++) MakeUnit(i);
        for (int i = 0; i < Buildings.GetLength(0); i++) MakeBuilding(i);
        MakeNodes();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("RTS: placeholder prefabs created in " + PrefabDir + ". Check Fusion's Network Prefab Table if a prefab is missing there.");
    }

    [MenuItem("Tools/RTS/2 Setup open Game scene")]
    static void SetupScene()
    {
        // Running this twice put two Spawners, two Systems and two Zones into Game.unity (UNITY_FEEDBACK 2026-11-19).
        if (AlreadySetUp(GameObject.Find("MapAndSystems") != null || Object.FindAnyObjectByType<GameSpawner>() != null, "map and systems (MapAndSystems / GameSpawner)")) return;
        if (!File.Exists(PrefabDir + "/Player.prefab")) CreatePrefabs();
        var root = new GameObject("MapAndSystems");

        FurnishGround(root.transform);

        // zone centres: nearest centre wins, so the coast lies halfway between a land and a water centre (z = 45)
        // (RulesTests.GeneratedMapLayout_PutsShoreAndResourcesInTheRightZones repeats these numbers: change both together)
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

        // resource nodes (scene objects with a NetworkObject: Fusion spawns them when the scene loads through the runner)
        PlaceResources(root.transform);

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
        sys.AddComponent<FogOfWar>();
        var cam = Camera.main;
        if (cam != null)
        {
            if (cam.GetComponent<RtsCamera>() == null) cam.gameObject.AddComponent<RtsCamera>();
            cam.transform.position = new Vector3(-65, 24, 8); cam.transform.rotation = Quaternion.Euler(55, 0, 0);
        }
        else Debug.LogWarning("RTS: no Main Camera (tag MainCamera) in the scene: add one and an RtsCamera component.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("RTS: Game scene set up under 'MapAndSystems'. Save the scene (Ctrl+S). UI canvases are still manual (TODO.md section 4).");
    }

    [MenuItem("Tools/RTS/3 Create Game UI")]
    static void CreateUi()
    {
        if (AlreadySetUp(GameObject.Find("GameCanvas") != null || Object.FindAnyObjectByType<AiCommanderPanel>() != null, "game UI (GameCanvas)")) return;
        var canvasGo = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // new Input System UI module

        // the components build their own panels when the scene starts (UiKit)
        canvasGo.AddComponent<HudUI>(); canvasGo.AddComponent<GameOverUI>();
        new GameObject("SelectionPanel", typeof(RectTransform), typeof(SelectionPanel)).transform.SetParent(canvasGo.transform, false);
        new GameObject("AiCommanderPanel", typeof(RectTransform), typeof(AiCommanderPanel)).transform.SetParent(canvasGo.transform, false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("RTS: GameCanvas created (HUD, selection panel, AI panel, game over). Save the scene.");
    }

    // Commands 2 and 3 add to the open scene; on a scene that already has their objects they would duplicate everything.
    static bool AlreadySetUp(bool exists, string what)
    {
        if (!exists) return false;
        EditorUtility.DisplayDialog("Already set up", "The open scene already has the " + what + ". Nothing was added.\n\n" +
            "To start over, use Tools > RTS > Rebuild generated scenes and prefabs.", "OK");
        return true;
    }

    // ---------- helpers ----------
    static void MakeBuilding(int i)
    {
        string key = Buildings[i, 0];
        bool wall = RTS.Core.Rules.BuildSite.IsWall(key);
        var go = wall ? new GameObject() : GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = Buildings[i, 1];
        float s = float.Parse(Buildings[i, 3], System.Globalization.CultureInfo.InvariantCulture); go.transform.localScale = new Vector3(s, wall ? 1.8f : s * 0.6f, s);
        if (wall) go.AddComponent<BoxCollider>(); else Colorize(go, Buildings[i, 1], Buildings[i, 2]);
        go.AddComponent<NetworkObject>(); go.AddComponent<NetworkTransform>(); go.AddComponent<NetworkBuilding>();
        if (wall) // WallLook builds the logs itself
        {
            var so = new SerializedObject(go.AddComponent<WallLook>());
            so.FindProperty("wood").objectReferenceValue = Mat("WallMat", Buildings[i, 2]); so.FindProperty("dark").objectReferenceValue = Mat("TrunkMat", "0.33,0.22,0.13");
            so.FindProperty("gate").boolValue = key == "gate";
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Save(go, Buildings[i, 1]);
    }

    static void MakeUnit(int i)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = Units[i, 1]; Colorize(go, Units[i, 1], Units[i, 2]);
        float s = float.Parse(Units[i, 3], System.Globalization.CultureInfo.InvariantCulture); go.transform.localScale = new Vector3(s, s, s);
        go.AddComponent<NetworkObject>(); go.AddComponent<NetworkTransform>(); go.AddComponent<NetworkUnit>(); go.AddComponent<UnitCombat>();
        if (Units[i, 0] == "villager" || Units[i, 0] == "fishing_boat") go.AddComponent<VillagerGatherer>();
        Save(go, Units[i, 1]);
    }

    // ---- resources: trees with a trunk, bushes with berries, golden rocks, fish in the water ----
    static void MakeNodes()
    {
        var tree = new GameObject("Tree"); var c = tree.AddComponent<CapsuleCollider>(); c.radius = 0.7f; c.height = 3.4f;
        TreeLook(tree.transform, Vector3.down * 1.7f, 1f);
        MakeNode(tree, "wood", 200);

        var bush = new GameObject("BerryBush"); bush.AddComponent<SphereCollider>().radius = 0.8f;
        Part(bush.transform, PrimitiveType.Sphere, "BushMat", "0.16,0.36,0.14", new Vector3(0, -0.25f, 0), new Vector3(1.5f, 1.1f, 1.5f));
        Part(bush.transform, PrimitiveType.Sphere, "BushMat", "0.16,0.36,0.14", new Vector3(0.45f, -0.35f, 0.25f), new Vector3(0.9f, 0.8f, 0.9f));
        foreach (var p in new[] { new Vector3(0.35f, 0.15f, -0.45f), new Vector3(-0.3f, 0.2f, -0.4f), new Vector3(0, 0.3f, 0.1f), new Vector3(-0.6f, -0.1f, 0), new Vector3(0.65f, 0, -0.1f), new Vector3(0.1f, 0f, -0.7f), new Vector3(-0.2f, 0.1f, 0.55f) })
            Part(bush.transform, PrimitiveType.Sphere, "BerryBushMat", "0.9,0.2,0.55", p, Vector3.one * 0.26f);
        MakeNode(bush, "food", 200);

        var gold = new GameObject("GoldMine"); var g = gold.AddComponent<BoxCollider>(); g.size = new Vector3(2.8f, 1.6f, 2.8f);
        Part(gold.transform, PrimitiveType.Cube, "RockMat", "0.36,0.35,0.34", new Vector3(0, -0.45f, 0), new Vector3(2.3f, 1.1f, 2.0f), new Vector3(8, 20, 5));
        Part(gold.transform, PrimitiveType.Cube, "RockMat", "0.36,0.35,0.34", new Vector3(0.5f, -0.1f, 0.4f), new Vector3(1.2f, 1.1f, 1.2f), new Vector3(25, 50, 10));
        foreach (var p in new[] { new Vector3(-0.5f, 0.15f, -0.5f), new Vector3(0.6f, 0.45f, -0.1f), new Vector3(-0.7f, -0.2f, 0.6f), new Vector3(0.9f, -0.35f, -0.8f), new Vector3(0, 0.3f, 0.7f) })
            Part(gold.transform, PrimitiveType.Cube, "GoldMineMat", "1,0.8,0.05", p, Vector3.one * 0.55f, new Vector3(p.x * 70, p.z * 90, 35));
        MakeNode(gold, "gold", 200);

        var fish = new GameObject("Fish"); var f = fish.AddComponent<BoxCollider>(); f.size = new Vector3(3f, 0.6f, 3f);
        var school = new GameObject("School"); school.transform.SetParent(fish.transform, false); school.AddComponent<FishSchool>();
        Part(fish.transform, PrimitiveType.Cylinder, "ShoalMat", "0.22,0.47,0.82", new Vector3(0, -0.22f, 0), new Vector3(3f, 0.01f, 3f));
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f, r = 0.6f + (i % 3) * 0.3f;
            Part(school.transform, PrimitiveType.Sphere, "FishMat", "0.78,0.83,0.88", Quaternion.Euler(0, a, 0) * new Vector3(r, -0.1f, 0), new Vector3(0.18f, 0.16f, 0.6f), new Vector3(0, a, 0));
        }
        MakeNode(fish, "food", 250);
    }

    static void TreeLook(Transform parent, Vector3 foot, float scale)
    {
        Part(parent, PrimitiveType.Cylinder, "TrunkMat", "0.33,0.22,0.13", foot + Vector3.up * 0.7f * scale, new Vector3(0.34f, 0.7f, 0.34f) * scale);
        Part(parent, PrimitiveType.Sphere, "TreeMat", "0.05,0.35,0.08", foot + Vector3.up * 1.9f * scale, new Vector3(1.9f, 1.6f, 1.9f) * scale);
        Part(parent, PrimitiveType.Sphere, "LeafMat", "0.10,0.42,0.12", foot + new Vector3(0.2f, 2.7f, 0.1f) * scale, new Vector3(1.3f, 1.3f, 1.3f) * scale);
    }

    /// <summary>A coloured shape without a collider.</summary>
    static GameObject Part(Transform parent, PrimitiveType shape, string material, string rgb, Vector3 pos, Vector3 scale, Vector3 turn = default)
    {
        var go = GameObject.CreatePrimitive(shape);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = scale; go.transform.localRotation = Quaternion.Euler(turn);
        go.GetComponent<Renderer>().sharedMaterial = Mat(material, rgb);
        return go;
    }

    static void MakeNode(GameObject go, string resource, float amount)
    {
        go.AddComponent<NetworkObject>();
        var so = new SerializedObject(go.AddComponent<ResourceNode>());
        so.FindProperty("resource").stringValue = resource; so.FindProperty("startAmount").floatValue = amount;
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(go, go.name);
    }

    /// <summary>Ground (GroundMesh builds it from TerrainShape) and sea; old ones are replaced.</summary>
    static void FurnishGround(Transform root)
    {
        foreach (var name in new[] { "Ground", "Sea" }) { var old = root.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject); }
        var ground = new GameObject("Ground"); ground.transform.SetParent(root, false); ground.transform.SetAsFirstSibling();
        ground.AddComponent<MeshFilter>(); ground.AddComponent<MeshRenderer>().sharedMaterial = Mat("GroundMat", "0.42,0.5,0.32"); ground.AddComponent<MeshCollider>();
        ground.AddComponent<GroundMesh>();
        var sea = GameObject.CreatePrimitive(PrimitiveType.Cube); sea.name = "Sea";
        sea.transform.SetParent(root, false); sea.transform.position = new Vector3(0, RTS.Core.Rules.TerrainShape.SeaLevel - 0.5f, 110); sea.transform.localScale = new Vector3(500, 1, 180);
        sea.GetComponent<Renderer>().sharedMaterial = Mat("SeaMat", "0.1,0.3,0.7");
    }

    /// <summary>All resources of MapLayout (an old "Resources" object is replaced).</summary>
    static void PlaceResources(Transform root)
    {
        var old = root.Find("Resources"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var parent = new GameObject("Resources").transform; parent.SetParent(root);
        foreach (var n in RTS.Core.Rules.MapLayout.Build())
        {
            string name = n.Kind == "tree" ? "Tree" : n.Kind == "berry" ? "BerryBush" : n.Kind == "gold" ? "GoldMine" : "Fish";
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(name), parent);
            float lift = n.Kind == "tree" ? 1.7f : n.Kind == "berry" ? 0.7f : n.Kind == "gold" ? 0.8f : 0.15f; // pivots are in the middle
            float ground = n.Kind == "fish" ? RTS.Core.Rules.TerrainShape.SeaLevel + 0.1f : RTS.Core.Rules.TerrainShape.Height(n.X, n.Z) - 0.08f;
            inst.transform.position = new Vector3(n.X, ground + lift * n.Scale, n.Z);
            inst.transform.rotation = Quaternion.Euler(0, n.Turn, 0);
            inst.transform.localScale = Vector3.one * n.Scale;
        }
    }

    /// <summary>Menu scene: canvas settings, camera, light and the little landscape behind the menu.</summary>
    static void FurnishMenu()
    {
        var canvasGo = Object.FindAnyObjectByType<MenuUI>().gameObject;
        for (int i = canvasGo.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(canvasGo.transform.GetChild(i).gameObject);
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = canvasGo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;

        var old = GameObject.Find("Landscape"); if (old != null) Object.DestroyImmediate(old);
        var land = new GameObject("Landscape").transform;
        Part(land, PrimitiveType.Cube, "SeaMat", "0.1,0.3,0.7", new Vector3(0, -1.3f, 60), new Vector3(400, 1, 400));
        Part(land, PrimitiveType.Cylinder, "SandMat", "0.72,0.65,0.45", new Vector3(0, -1.0f, 0), new Vector3(56, 0.6f, 44));
        Part(land, PrimitiveType.Cylinder, "GroundMat", "0.42,0.5,0.32", new Vector3(0, -0.55f, 0), new Vector3(52, 0.55f, 40));
        House(land, new Vector3(0, 0, 0), 6f, "TownCenterMat", "0.8,0.8,0.9", "RoofBlueMat", "0.16,0.36,0.78", true);
        House(land, new Vector3(-8, 0, 3), 3f, "HouseMat", "0.8,0.6,0.5", "RoofBlueMat", "0.16,0.36,0.78", false);
        House(land, new Vector3(-7, 0, -4), 3f, "HouseMat", "0.8,0.6,0.5", "RoofBlueMat", "0.16,0.36,0.78", false);
        House(land, new Vector3(8, 0, 4), 4f, "BarracksMat", "0.6,0.3,0.3", "RoofBlueMat", "0.16,0.36,0.78", false);
        House(land, new Vector3(13, 0, -5), 3f, "HouseMat", "0.8,0.6,0.5", "RoofRedMat", "0.72,0.18,0.14", false);
        House(land, new Vector3(18, 0, -1), 4f, "TownCenterMat", "0.8,0.8,0.9", "RoofRedMat", "0.72,0.18,0.14", true);
        var random = new System.Random(11);
        for (int i = 0; i < 46; i++)
        {
            double a = random.NextDouble() * 2 * System.Math.PI, r = 14 + random.NextDouble() * 8;
            var p = new Vector3((float)System.Math.Cos(a) * (float)r * 1.05f, 0, (float)System.Math.Sin(a) * (float)r * 0.8f);
            if (p.z < -8 && Mathf.Abs(p.x) < 11 || p.x > 10 && p.z < 3 && p.z > -9) continue; // keep the view and the red village free
            TreeLook(land, p, 0.8f + (float)random.NextDouble() * 0.7f);
        }
        foreach (var p in new[] { new Vector3(-3, 1, -7), new Vector3(-1.5f, 1, -8), new Vector3(4, 1, -6), new Vector3(5.4f, 1, -6.8f), new Vector3(6.4f, 1, -5.6f) })
            Part(land, PrimitiveType.Capsule, p.x < 0 ? "VillagerMat" : "SpearmanMat", p.x < 0 ? "0.95,0.85,0.55" : "0.75,0.75,0.8", p, Vector3.one * 0.9f);
        Ship(land, new Vector3(-20, -0.6f, 22), 24f); Ship(land, new Vector3(12, -0.6f, 27), -30f); Ship(land, new Vector3(30, -0.6f, 14), 60f);

        var cam = Camera.main;
        if (cam != null)
        {
            if (cam.GetComponent<MenuCamera>() == null) cam.gameObject.AddComponent<MenuCamera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.74f, 0.86f); cam.fieldOfView = 40f;
            cam.transform.position = new Vector3(0, 16, -34); cam.transform.rotation = Quaternion.Euler(22, -10, 0);
        }
        var sun = Object.FindAnyObjectByType<Light>();
        if (sun != null) { sun.transform.rotation = Quaternion.Euler(38, -55, 0); sun.color = new Color(1f, 0.93f, 0.78f); sun.intensity = 1.4f; sun.shadows = LightShadows.Soft; }
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(0.62f, 0.74f, 0.86f);
        RenderSettings.fogStartDistance = 45f; RenderSettings.fogEndDistance = 190f;
    }

    static void House(Transform parent, Vector3 pos, float size, string wall, string wallRgb, string roof, string roofRgb, bool flag)
    {
        var h = new GameObject("House").transform; h.SetParent(parent, false); h.localPosition = pos;
        Part(h, PrimitiveType.Cube, wall, wallRgb, new Vector3(0, size * 0.25f, 0), new Vector3(size, size * 0.5f, size * 0.85f));
        Part(h, PrimitiveType.Cube, roof, roofRgb, new Vector3(0, size * 0.5f, 0), new Vector3(size * 1.04f, size * 0.62f, size * 0.62f), new Vector3(45, 0, 0));
        Part(h, PrimitiveType.Cube, "DoorMat", "0.25,0.17,0.1", new Vector3(0, size * 0.14f, -size * 0.43f), new Vector3(size * 0.2f, size * 0.28f, 0.1f));
        if (!flag) return;
        Part(h, PrimitiveType.Cylinder, "TrunkMat", "0.33,0.22,0.13", new Vector3(size * 0.55f, size * 0.6f, -size * 0.45f), new Vector3(0.12f, size * 0.6f, 0.12f));
        Part(h, PrimitiveType.Cube, roof, roofRgb, new Vector3(size * 0.55f + 0.75f, size * 1.08f, -size * 0.45f), new Vector3(1.4f, 0.8f, 0.06f));
    }

    static void Ship(Transform parent, Vector3 pos, float turn)
    {
        var s = new GameObject("Ship").transform; s.SetParent(parent, false); s.localPosition = pos; s.localRotation = Quaternion.Euler(0, turn, 0);
        Part(s, PrimitiveType.Cube, "HullMat", "0.32,0.2,0.12", new Vector3(0, 0.3f, 0), new Vector3(1.6f, 0.9f, 4.4f));
        Part(s, PrimitiveType.Cylinder, "TrunkMat", "0.33,0.22,0.13", new Vector3(0, 2f, 0), new Vector3(0.14f, 1.8f, 0.14f));
        Part(s, PrimitiveType.Cube, "SailMat", "0.92,0.9,0.82", new Vector3(0, 2.3f, 0.1f), new Vector3(2.4f, 1.9f, 0.08f), new Vector3(8, 0, 0));
    }

    /// <summary>
    /// Brings the existing scenes and prefabs to the current state without deleting them: resource prefabs, missing unit and building prefabs, the
    /// resources of the Game scene, the unit list of GameSpawner, and the Menu scene. Also for batch mode (-executeMethod).
    /// </summary>
    [MenuItem("Tools/RTS/Refresh map resources and menu")]
    public static void Refresh()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("RTS: stop Play mode first."); return; }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        MakeNodes();
        for (int i = 0; i < Units.GetLength(0); i++) if (!File.Exists(PrefabDir + "/" + Units[i, 1] + ".prefab")) MakeUnit(i);
        for (int i = 0; i < Buildings.GetLength(0); i++) if (!File.Exists(PrefabDir + "/" + Buildings[i, 1] + ".prefab")) MakeBuilding(i);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        var menu = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);
        FurnishMenu();
        EditorSceneManager.MarkSceneDirty(menu); EditorSceneManager.SaveScene(menu);

        var game = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        FurnishGround(GameObject.Find("MapAndSystems").transform);
        PlaceResources(GameObject.Find("MapAndSystems").transform);
        var so = new SerializedObject(Object.FindAnyObjectByType<GameSpawner>());
        FillEntries(so.FindProperty("unitPrefabs"), Units); FillEntries(so.FindProperty("buildingPrefabs"), Buildings);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(game); EditorSceneManager.SaveScene(game);
        Debug.Log("RTS: refreshed, " + GameObject.Find("Resources").transform.childCount + " resources on the map.");
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
