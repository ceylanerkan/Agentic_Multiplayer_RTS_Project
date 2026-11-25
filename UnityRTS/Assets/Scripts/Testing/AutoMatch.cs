#if RTS_AUTOTEST   // only in the test build (AutoBuild.TestBuild), never in the game
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using LogType = UnityEngine.LogType;

/// <summary>
/// Automatic two-player match: the test build runs twice (tools/run_auto_match.ps1), once as host and once as client. The client
/// plays with a virtual mouse through the normal scripts (selection, buttons, placement ghost, right clicks); both sides write what
/// they see, evaluation/two_players_report.py compares it.
/// Arguments: -rtsauto host|client -rtssession NAME -rtsout FILE
/// </summary>
public class AutoMatch : MonoBehaviour
{
    const float TimeLimit = 480f;
    static string role, session, outPath;

    readonly StringBuilder file = new StringBuilder();
    bool ok, done;
    string section = "start";
    float nextSample;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        role = Arg("-rtsauto"); session = Arg("-rtssession"); outPath = Arg("-rtsout");
        if (role == null || outPath == null) return;
        var go = new GameObject("AutoMatch"); DontDestroyOnLoad(go); go.AddComponent<AutoMatch>();
    }

    static string Arg(string name)
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
    }

    void Start()
    {
        Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0; Application.runInBackground = true;
        Application.logMessageReceived += OnLog;
        StartCoroutine(role == "host" ? Host() : Client());
    }

    void OnLog(string text, string trace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        file.AppendLine("E;" + section + ";" + type + ": " + text.Split('\n')[0].Replace(';', ','));
        if (type == LogType.Exception && trace != null && trace.Contains("AutoMatch")) { Check("test driver", false, text.Split('\n')[0] + " @ " + trace.Split('\n')[0]); Finish(); }
    }

    void Update()
    {
        if (!done && Time.realtimeSinceStartup > TimeLimit) { Check("finished in time", false, "stopped after " + TimeLimit + " s in section " + section); Finish(); return; }
        if (!done && Time.realtimeSinceStartup >= nextSample) { nextSample = Time.realtimeSinceStartup + (role == "host" ? 0.2f : 1f); Sample(); }
    }

    void Finish()
    {
        if (done) return;
        done = true;
        File.WriteAllText(outPath, file.ToString());
        Application.Quit();
    }

    void Check(string what, bool good, string detail = "")
    {
        file.AppendLine("ROW;" + section + ";" + what.Replace(';', ',') + ";" + (good ? "ok" : "FAIL") + ";" + detail.Replace(';', ',').Replace("\n", " "));
        Debug.Log("AUTOMATCH " + role + " " + (good ? "ok   " : "FAIL ") + section + ": " + what + " " + detail);
    }

    /// <summary>What this process sees now; the tick makes the two files comparable.</summary>
    void Sample()
    {
        var launcher = GameLauncher.Instance;
        if (launcher == null || launcher.Runner == null || !launcher.Runner.IsRunning || CommandRelay.Local == null) return;
        var r = launcher.Runner;
        int tick = r.IsServer ? r.Tick.Raw : r.LatestServerTick.Raw;
        string F(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);
        try
        {
            foreach (var s in NetworkPlayerState.All) file.AppendLine("P;" + tick + ";" + s.Slot + ";" + s.Wood + ";" + s.Food + ";" + s.Gold + ";" + s.PopUsed + ";" + s.PopCapacity + ";" + s.Age + ";" + s.Result);
            foreach (var u in NetworkUnit.All) file.AppendLine("S;" + tick + ";" + u.Object.Id.Raw + ";" + u.TypeKey + ";" + F(u.transform.position.x) + ";" + F(u.transform.position.z) + ";" + F(u.Hp));
            foreach (var b in NetworkBuilding.All) file.AppendLine("S;" + tick + ";" + b.Object.Id.Raw + ";" + b.TypeKey + ";" + F(b.transform.position.x) + ";" + F(b.transform.position.z) + ";" + F(b.Hp));
        }
        catch (Exception) { } // the session is closing
    }

    // ---- helpers ----
    static CommandRelay Relay => CommandRelay.Local;
    static NetworkPlayerState Me => Relay.GetComponent<NetworkPlayerState>();
    static PlayerRef MyRef => Relay.Object.InputAuthority;
    static bool Alive(NetworkBehaviour n) => n != null && n.Object != null && n.Object.IsValid;
    static List<NetworkUnit> Units(string type) => NetworkUnit.All.Where(u => Alive(u) && u.Owner == MyRef && u.TypeKey.ToString() == type).ToList();
    static List<NetworkBuilding> Buildings(string type, bool mine = true) => NetworkBuilding.All.Where(b => Alive(b) && (b.Owner == MyRef) == mine && b.TypeKey.ToString() == type).ToList();
    static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }
    static string At(Vector3 p) => "(" + p.x.ToString("0.0") + ", " + p.z.ToString("0.0") + ")";
    static bool Seen(Component c) => c.GetComponent<Renderer>().enabled;

    IEnumerator For(Func<bool> cond, float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds; ok = false;
        while (Time.realtimeSinceStartup < end)
        {
            bool hit = false;
            try { hit = cond(); } catch (Exception) { }
            if (hit) { ok = true; yield break; }
            yield return null;
        }
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static string GameOverText()
    {
        var ui = FindAnyObjectByType<GameOverUI>();
        if (ui == null) return "";
        var t = typeof(GameOverUI).GetField("resultText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as TMP_Text;
        return t != null && t.gameObject.activeInHierarchy ? t.text : "";
    }

    // ---- host (player 1) ----
    IEnumerator Host()
    {
        yield return For(() => GameLauncher.Instance != null, 30);
        GameLauncher.Instance.StartHost(session);
        yield return For(() => Relay != null && Units("villager").Count == 3, 90);
        Check("host is player 1 with a Town Center and 3 Villagers", ok && Me.Slot == 1 && Buildings("town_center").Count == 1, GameLauncher.LastMessage);
        if (!ok) { Finish(); yield break; }
        yield return For(() => NetworkPlayerState.All.Count == 2, 120);
        Check("the second player joined", ok);
        if (!ok) { Finish(); yield break; }
        yield return new WaitForSecondsRealtime(3f);
        Check("player 2 has slot 2 and its own base", NetworkPlayerState.All.Any(s => s.Slot == 2) && NetworkBuilding.All.Count(b => b.TypeKey.ToString() == "town_center") == 2);
        foreach (var s in NetworkPlayerState.All) { s.Wood += 2000; s.Food += 2000; s.Gold += 2000; }   // test resources

        section = "play";
        var v = Units("villager");
        Relay.SendMove(v[0].Object.Id, new Vector3(-40, 0, 5));
        Relay.SendBuild("house", new Vector3(-85, 0, 8), "P1Base", v[1].Object.Id);
        yield return For(() => Buildings("house").Any(b => b.Completed), 60);
        Check("the host builds a House", ok, ok ? At(Buildings("house")[0].transform.position) : "");
        yield return For(() => Buildings("house", false).Any(b => b.Completed), 150);
        Check("the House of player 2 is finished on the host too", ok);
        var tc = Buildings("town_center")[0]; float full = tc.Hp;
        yield return For(() => Alive(tc) && tc.Hp < full, 240);
        Check("the host sees the attack on its Town Center", ok, ok ? full + " -> " + tc.Hp : "");
        yield return new WaitForSecondsRealtime(6f);
        if (ok && Alive(tc)) tc.TakeDamage(tc.Hp - 8f);   // shortens the siege: the last hits come from player 2

        section = "end";
        yield return For(() => Me.Result == 2, 60);
        yield return For(() => GameOverText() == "DEFEAT", 5);
        Check("the host sees DEFEAT", ok, GameOverText());
        yield return For(() => SceneManager.GetActiveScene().name == "Menu", 60);
        Check("the other player left: back in the menu", ok, GameLauncher.LastMessage);
        Finish();
    }

    // ---- client (player 2), with a virtual mouse ----
    Mouse mouse; Vector2 cursor;

    void Send(bool left = false, bool right = false)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = cursor }.WithButton(MouseButton.Left, left).WithButton(MouseButton.Right, right));
    }

    IEnumerator MoveMouse(Vector2 p) { cursor = p; Send(); yield return Frames(4); }

    IEnumerator Click(Vector2 p, bool right = false)
    {
        yield return MoveMouse(p);
        Send(!right, right); yield return Frames(4);
        Send(); yield return Frames(4);
    }

    IEnumerator Drag(Vector2 a, Vector2 b)
    {
        yield return MoveMouse(a);
        Send(true); yield return Frames(4);
        cursor = b; Send(true); yield return Frames(4);
        Send(); yield return Frames(4);
    }

    static Camera Cam => Camera.main;
    static Vector2 OnScreen(Vector3 p) => Cam.WorldToScreenPoint(p);

    /// <summary>Scrolls the map so that the point lies in the free middle of the screen.</summary>
    static IEnumerator Look(Vector3 ground)
    {
        var ray = Cam.ViewportPointToRay(new Vector3(0.5f, 0.55f, 0));
        Vector3 hit = ray.origin + ray.direction * (ray.origin.y / -ray.direction.y);
        Cam.transform.position += new Vector3(ground.x - hit.x, 0, ground.z - hit.z);
        yield return Frames(3);
    }

    IEnumerator Hover(Vector3 ground) { yield return Look(ground); yield return MoveMouse(OnScreen(ground)); yield return Frames(2); }
    IEnumerator ClickOn(Vector3 p, bool right = false) { yield return Look(new Vector3(p.x, 0, p.z)); yield return Click(OnScreen(p), right); }

    IEnumerator ClickButton(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) { Check("button " + name, false, "not shown"); yield break; }
        var rt = (RectTransform)go.transform;
        yield return new WaitForSecondsRealtime(0.4f);   // the panel refreshes its buttons five times a second
        yield return Click(RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center)));
    }

    static T Placement<T>(string field) where T : class
    {
        return typeof(BuildPlacement).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(BuildPlacement.Instance) as T;
    }
    static bool Green() { var c = Placement<Material>("ghostMaterial").color; return c.g > c.r; }
    static Vector3 Ghost() => Placement<GameObject>("ghost").transform.position;

    IEnumerator Client()
    {
        yield return For(() => GameLauncher.Instance != null, 30);
        for (int i = 0; i < 8; i++)   // the host needs some seconds to open the session
        {
            GameLauncher.Instance.StartClient(session);
            yield return For(() => Relay != null && Units("villager").Count == 3 && Buildings("town_center").Count == 1, 25);
            if (ok) break;
            yield return new WaitForSecondsRealtime(3f);
        }
        Check("joined as player 2 with a Town Center and 3 Villagers", ok && Me.Slot == 2, GameLauncher.LastMessage);
        if (!ok) { Finish(); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        var tc = Buildings("town_center")[0]; var enemyTc = Buildings("town_center", false)[0];
        Check("the camera starts at the own base", Flat(Cam.transform.position - tc.transform.position) < 30f, At(Cam.transform.position));
        Check("the enemy base is hidden by the fog", !Seen(enemyTc));
        yield return For(() => Me.Wood > 2000, 30);

        mouse = InputSystem.AddDevice<Mouse>("AutoMouse"); mouse.MakeCurrent();
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var sel = SelectionManager.Instance;

        section = "selection";
        var v = Units("villager");
        yield return ClickOn(v[0].transform.position);
        Check("a click selects one Villager", sel.Units.Count == 1 && sel.Units[0] == v[0], sel.Units.Count + " selected, screen " + Screen.width + "x" + Screen.height + ", mouse " + (Mouse.current == mouse ? "virtual " : "other ") + cursor);
        yield return Frames(10);
        var buttons = new[] { "Build House", "Build Barracks", "Build Blacksmith", "Build Stable", "Build Dock" };
        Check("the panel shows the building buttons", buttons.All(b => GameObject.Find(b) != null), string.Join(", ", buttons.Where(b => GameObject.Find(b) == null)));
        Vector3 mid = (v[0].transform.position + v[1].transform.position + v[2].transform.position) / 3f;
        yield return Look(new Vector3(mid.x, 0, mid.z));
        var pts = v.Select(u => OnScreen(u.transform.position)).ToList();
        yield return Drag(new Vector2(pts.Min(p => p.x) - 40, pts.Min(p => p.y) - 40), new Vector2(pts.Max(p => p.x) + 40, pts.Max(p => p.y) + 40));
        Check("dragging a box selects all three", sel.Units.Count == 3, sel.Units.Count + " selected");
        Picture("selection");

        section = "placement";
        yield return ClickButton("Build House");
        Check("the button starts the ghost", BuildPlacement.Instance.Active);
        var place = new Vector3(52, 0, 10);
        yield return Hover(place);
        Check("free ground: green, under the mouse", Green() && Flat(Ghost() - place) < 0.6f, At(Ghost()));
        Picture("ghost_green");
        yield return Hover(tc.transform.position);
        Check("on the Town Center: red", !Green());
        yield return Hover(new Vector3(60, 0, 52));
        Check("on the sea: red", !Green());
        yield return Hover(new Vector3(-65, 0, 8));
        Check("in the enemy base: red", !Green());
        yield return Hover(tc.transform.position + new Vector3(5.5f, 0, 0));
        Check("at the edge of the Town Center: moves beside it", Green(), "mouse 5.5 m beside the centre, ghost " + (Ghost().x - tc.transform.position.x).ToString("0.0") + " m");
        yield return Hover(place);
        yield return Click(OnScreen(place));
        yield return For(() => Buildings("house").Count == 1, 5);
        var house = Buildings("house").FirstOrDefault();
        Check("a click places the House there", ok && Flat(house.transform.position - place) < 0.6f, ok ? At(house.transform.position) : "no site");
        yield return ClickOn(house != null ? house.transform.position : place, true);   // all three help
        yield return For(() => Buildings("house").Any(b => b.Completed), 60);
        Check("the Villagers build it, capacity 10", ok && Me.PopCapacity == 10, "capacity " + Me.PopCapacity);
        yield return ClickButton("Build Barracks");
        yield return Hover(new Vector3(40, 0, 10));
        bool green = Green(); int count = NetworkBuilding.All.Count;
        yield return Click(OnScreen(new Vector3(40, 0, 10)), true);
        Check("a right click cancels the ghost, nobody walks off", green && !BuildPlacement.Instance.Active && NetworkBuilding.All.Count == count && sel.Units.All(u => !u.HasMoveTarget));
        yield return ClickButton("Build Dock");
        yield return Hover(new Vector3(52, 0, 25));
        Check("Dock inland: red", !Green());
        yield return Hover(new Vector3(52, 0, 38));
        Check("Dock near the shore: green, straight to the shore", Green() && Ghost().z >= 42.4f && Mathf.Abs(Ghost().x - 52f) < 1f, At(Ghost()));
        Picture("ghost_dock");
        yield return Click(OnScreen(new Vector3(52, 0, 38)));
        yield return For(() => Buildings("dock").Count == 1, 5);
        Check("a click places the Dock on the shore", ok && Buildings("dock")[0].transform.position.z >= 42.4f, ok ? At(Buildings("dock")[0].transform.position) : "no site");

        section = "commands";
        yield return ClickOn(v[1].transform.position);
        var goal = new Vector3(45, 0, 2);
        yield return ClickOn(goal, true);
        yield return For(() => Flat(v[1].transform.position - goal) < 1f, 30);
        Check("a right click moves a Villager", ok, At(v[1].transform.position));
        yield return ClickOn(v[2].transform.position);
        var tree = ResourceNode.Nearest(tc.transform.position, "wood", 60f);
        int wood = Me.Wood;
        yield return ClickOn(tree.transform.position, true);
        yield return ClickOn(tc.transform.position);
        Check("a click selects the Town Center", sel.Building == tc, sel.Building != null ? sel.Building.TypeKey.ToString() : sel.Units.Count + " units");
        yield return ClickButton("Train Villager");
        yield return For(() => tc.QueueCount == 1, 3);
        Check("the button trains a Villager", ok, "queue " + tc.QueueCount);
        yield return For(() => Units("villager").Count == 4, 30);
        Check("the Villager appears", ok);
        yield return For(() => Me.Wood > wood, 60);
        Check("a right click on a tree: wood arrives", ok, wood + " -> " + Me.Wood);

        section = "attack";
        sel.Clear(); sel.Units.AddRange(Units("villager"));
        yield return ClickOn(enemyTc.transform.position + Vector3.up, true);
        yield return new WaitForSecondsRealtime(1f);
        Check("the hidden enemy Town Center is only ground", Units("villager").All(u => !u.AttackTarget.IsValid) && !Seen(enemyTc));
        yield return For(() => Seen(enemyTc), 90);
        Check("it appears when the Villagers come near", ok);
        yield return For(() => Units("villager").Count(u => Flat(u.transform.position - enemyTc.transform.position) < 12f) >= 3, 40);
        float hp = enemyTc.Hp;
        yield return ClickOn(enemyTc.transform.position + Vector3.up, true);
        yield return For(() => enemyTc.Hp < hp, 20);
        Check("a right click attacks it", ok, hp + " -> " + (Alive(enemyTc) ? enemyTc.Hp.ToString("0") : "destroyed"));
        Picture("attack");

        section = "end";
        yield return For(() => Me.Result == 1, 60);
        yield return For(() => GameOverText() == "VICTORY", 5);
        Check("the client sees VICTORY", ok, GameOverText());
        Picture("victory");
        yield return new WaitForSecondsRealtime(3f);
        Finish();
    }

    /// <summary>Picture of the screen with the UI (the canvases are drawn by the camera for one frame: there is no window).</summary>
    void Picture(string name)
    {
        try
        {
            var canvases = FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
            var rt = new RenderTexture(Mathf.Max(Screen.width, 640), Mathf.Max(Screen.height, 360), 24);
            var old = Cam.targetTexture; Cam.targetTexture = rt;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = Cam; c.planeDistance = 1f; }
            Canvas.ForceUpdateCanvases();
            Cam.Render();
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Cam.targetTexture = old;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(outPath), "match_" + name + ".png"), tex.EncodeToPNG());
            Destroy(rt); Destroy(tex);
        }
        catch (Exception e) { Debug.LogWarning("AUTOMATCH no picture: " + e.Message); }
    }
}
#endif
