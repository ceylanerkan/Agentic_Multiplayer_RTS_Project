using System.Collections.Generic;
using RTS.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The top of the screen: resources, population and idle Villagers (left), age and clock (middle), player and menu (right), the hint
/// while placing a building and the latest events. It also builds the mini map. Reads the [Networked] values of the local
/// NetworkPlayerState; every client sees only its own numbers. Built in code on the GameCanvas.
/// </summary>
public class HudUI : MonoBehaviour
{
    TMP_Text wood, food, gold, population, idle, age, clock, player, note, hint;
    RawImage sky;
    RectTransform hintBox, menu;
    Image playerDot;
    Button idleButton;
    readonly List<TMP_Text> feed = new List<TMP_Text>();
    NetworkPlayerState state;
    float nextSlow;
    int idleIndex;

    void Awake()
    {
        var old = transform.Find("HudBar"); // scenes made before the interface was built in code
        if (old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
        var canvas = (RectTransform)transform;

        var left = UiKit.Frame(canvas, "Resources"); UiKit.At((RectTransform)left.parent, 0, 1, 12, -12, 640, 52);
        wood = Chip(left, 0, "wood", UiKit.WoodColour, "Wood: from trees. For buildings, archers and ships.");
        food = Chip(left, 1, "food", UiKit.FoodColour, "Food: from berry bushes and fish. For Villagers, soldiers and research.");
        gold = Chip(left, 2, "gold", UiKit.GoldColour, "Gold: from gold mines. For strong units and research.");
        population = Chip(left, 3, "population", UiKit.Paper, "Population: used / room. Every House gives room for 5 more, 50 at most.");
        idleButton = UiKit.Button(left, "IdleVillagers", UiKit.Field, NextIdleVillager); UiKit.At((RectTransform)idleButton.transform, 0, 0.5f, 522, 0, 106, 38);
        var ii = UiKit.Icon(idleButton.transform, "idle", UiKit.Dim); UiKit.At(ii.rectTransform, 0, 0.5f, 12, 0, 26, 26);
        idle = UiKit.Text(idleButton.transform, "0", 22, UiKit.Paper, TextAlignmentOptions.Midline, FontStyles.Bold); UiKit.Stretch(idle.rectTransform, 0, 0, 1, 1, 40, 0, -8, 0);
        UiHover.Add(idleButton, () => "Villagers without work. Click to jump to the next one.");

        var mid = UiKit.Frame(canvas, "Age"); UiKit.At((RectTransform)mid.parent, 0.5f, 1, 0, -12, 280, 52);
        var ai = UiKit.Icon(mid, "age_2", UiKit.Gold); UiKit.At(ai.rectTransform, 0, 0.5f, 16, 0, 30, 30);
        age = UiKit.Text(mid, "AGE I", 26, UiKit.Gold, TextAlignmentOptions.MidlineLeft, FontStyles.Bold); age.characterSpacing = 4; UiKit.Stretch(age.rectTransform, 0, 0, 1, 1, 58, 0, -90, 0);
        clock = UiKit.Text(mid, "0:00", 22, UiKit.Dim, TextAlignmentOptions.MidlineRight); UiKit.Stretch(clock.rectTransform, 0, 0, 1, 1, 0, 0, -18, 0);
        sky = UiKit.Icon(mid, "sun", UiKit.Gold); UiKit.At(sky.rectTransform, 1, 0.5f, -92, 0, 28, 28);

        var right = UiKit.Frame(canvas, "Player"); UiKit.At((RectTransform)right.parent, 1, 1, -12, -12, 430, 52);
        var dot = UiKit.Box(right, "Dot", UiKit.Blue, true); UiKit.At(dot, 0, 0.5f, 16, 0, 20, 20); playerDot = dot.GetComponent<Image>();
        player = UiKit.Text(right, "", 22, UiKit.Paper, TextAlignmentOptions.MidlineLeft, FontStyles.Bold); UiKit.Stretch(player.rectTransform, 0, 0, 1, 1, 48, 0, -250, 0);
        note = UiKit.Fit(UiKit.Text(right, "", 19, UiKit.Dim, TextAlignmentOptions.MidlineRight), 12); UiKit.Stretch(note.rectTransform, 0, 0, 1, 1, 160, 0, -70, 0);
        var mb = UiKit.Button(right, "Menu", UiKit.Field, () => menu.gameObject.SetActive(!menu.gameObject.activeSelf)); UiKit.At((RectTransform)mb.transform, 1, 0.5f, -8, 0, 46, 38);
        var mi = UiKit.Icon(mb.transform, "menu", UiKit.Paper); UiKit.At(mi.rectTransform, 0.5f, 0.5f, 0, 0, 24, 24);

        hintBox = UiKit.Box(canvas, "Hint", new Color(0.05f, 0.07f, 0.1f, 0.85f), true); UiKit.At(hintBox, 0.5f, 1, 0, -74, 720, 40);
        hint = UiKit.Fit(UiKit.Text(hintBox, "", 20, UiKit.Gold, TextAlignmentOptions.Center), 12); UiKit.Stretch(hint.rectTransform, 0, 0, 1, 1, 14, 2, -14, -2);
        hintBox.gameObject.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            var t = UiKit.Text(canvas, "", 20, UiKit.Paper); UiKit.At(t.rectTransform, 0, 1, 24, -76 - i * 28, 520, 28);
            t.outlineWidth = 0.22f; t.outlineColor = new Color32(0, 0, 0, 220);
            feed.Add(t);
        }

        var map = UiKit.Frame(canvas, "MiniMap"); UiKit.At((RectTransform)map.parent, 0, 0, 12, 12, 4 + 212 * MiniMap.Aspect, 216);
        var mapImage = new GameObject("Map", typeof(RectTransform), typeof(RawImage)); mapImage.transform.SetParent(map, false);
        UiKit.Stretch((RectTransform)mapImage.transform, 0, 0, 1, 1, 0, 0, 0, 0);
        mapImage.AddComponent<MiniMap>();

        BuildMenu(canvas);
    }

    TMP_Text Chip(RectTransform bar, int i, string icon, Color colour, string tip)
    {
        var cell = UiKit.Box(bar, icon, new Color(1, 1, 1, 0.001f), false, true); UiKit.At(cell, 0, 0.5f, 8 + i * 128, 0, 126, 46);
        var ic = UiKit.Icon(cell, icon, colour); UiKit.At(ic.rectTransform, 0, 0.5f, 6, 0, 32, 32);
        var t = UiKit.Fit(UiKit.Text(cell, "", 25, UiKit.Paper, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), 14); UiKit.Stretch(t.rectTransform, 0, 0, 1, 1, 46, 0, 0, 0);
        UiHover.Add(cell, () => tip);
        return t;
    }

    void BuildMenu(RectTransform canvas)
    {
        var inside = UiKit.Frame(canvas, "MenuPanel"); menu = (RectTransform)inside.parent; UiKit.At(menu, 1, 1, -12, -72, 430, 400);
        string[,] rows =
        {
            { "WASD / arrows", "move the camera" }, { "Mouse wheel", "zoom" }, { "Left click, drag", "select (Shift adds)" },
            { "Right click", "move, gather, attack, help" }, { "Wall", "press, draw, release" }, { "Esc / right click", "cancel placing" },
        };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            var a = UiKit.Text(inside, rows[i, 0], 19, UiKit.Gold); UiKit.At(a.rectTransform, 0, 1, 20, -16 - i * 34, 170, 32);
            var b = UiKit.Text(inside, rows[i, 1], 19, UiKit.Paper); UiKit.At(b.rectTransform, 0, 1, 190, -16 - i * 34, 226, 32);
        }
        var resume = UiKit.TextButton(inside, "Back to the game", UiKit.Gold, UiKit.Ink, 22, () => menu.gameObject.SetActive(false)); UiKit.At((RectTransform)resume.transform, 0.5f, 0, 0, 84, 386, 50);
        var leave = UiKit.TextButton(inside, "Leave the match", new Color(0.45f, 0.16f, 0.13f), UiKit.Paper, 22, () => { if (GameLauncher.Instance != null) GameLauncher.Instance.ReturnToMenu("You left the match."); });
        UiKit.At((RectTransform)leave.transform, 0.5f, 0, 0, 22, 386, 50);
        menu.gameObject.SetActive(false);
    }

    void Update()
    {
        string h = BuildPlacement.Hint;
        if (hintBox.gameObject.activeSelf != (h != null)) hintBox.gameObject.SetActive(h != null);
        if (h != null) hint.text = h;

        if (state == null)
        {
            if (CommandRelay.Local == null) return;
            state = CommandRelay.Local.GetComponent<NetworkPlayerState>();
            if (state == null) return;
        }
        // the session is shutting down: the object still exists for a frame, but its [Networked] values can no longer be read
        if (state.Object == null || !state.Object.IsValid) return;
        wood.text = state.Wood.ToString(); food.text = state.Food.ToString(); gold.text = state.Gold.ToString();
        population.text = state.PopUsed + "/" + state.PopCapacity;
        population.color = state.PopUsed >= state.PopCapacity ? UiKit.Red : UiKit.Paper;
        age.text = "AGE " + (state.Age == 2 ? "II" : "I");
        int s = (int)DayNight.MatchSeconds();
        clock.text = s / 60 + ":" + (s % 60).ToString("00");
        bool dark = DayNight.Night01 > 0.5f;
        UiKit.SetIcon(sky, dark ? "moon" : "sun"); sky.color = dark ? new Color(0.70f, 0.80f, 1f) : UiKit.Gold;
        // which player this window is, so the two screens are easy to tell apart in the demo
        player.text = state.Slot > 0 ? "Player " + state.Slot : "";
        playerDot.color = UiKit.Blue; // own objects are blue on every screen
        note.text = Practice() ? "practice" : WaitingForOpponent() ? "waiting for player 2" : "";

        if (Time.unscaledTime < nextSlow) return;
        nextSlow = Time.unscaledTime + 0.25f;
        int count = IdleVillagers().Count;
        idle.text = count.ToString();
        idle.color = count > 0 ? UiKit.Gold : UiKit.Dim;
        idleButton.interactable = count > 0;
        ShowEvents();
    }

    static bool Practice() { return GameLauncher.Instance != null && GameLauncher.Instance.Solo; }

    /// <summary>The host is alone in the Game scene until the second player joins; say so, or it looks like nothing started.</summary>
    static bool WaitingForOpponent()
    {
        var launcher = GameLauncher.Instance;
        if (launcher == null || launcher.Runner == null) return false;
        return System.Linq.Enumerable.Count(launcher.Runner.ActivePlayers) < PlayerSlots.MaxPlayers;
    }

    // ---- idle Villagers ----
    static List<NetworkUnit> IdleVillagers()
    {
        var list = new List<NetworkUnit>();
        if (CommandRelay.Local == null || CommandRelay.Local.Object == null || !CommandRelay.Local.Object.IsValid) return list;
        var me = CommandRelay.Local.Object.InputAuthority;
        foreach (var u in NetworkUnit.All)
        {
            if (u.Object == null || !u.Object.IsValid || u.Owner != me || !u.CanBuild || u.HasMoveTarget || u.AttackTarget.IsValid) continue;
            var g = u.GetComponent<VillagerGatherer>();
            if (g != null && !g.Busy) list.Add(u);
        }
        return list;
    }

    void NextIdleVillager()
    {
        var list = IdleVillagers();
        if (list.Count == 0 || SelectionManager.Instance == null) return;
        var u = list[idleIndex++ % list.Count];
        SelectionManager.Instance.Clear();
        SelectionManager.Instance.Units.Add(u);
        RtsCamera.LookAt(u.transform.position);
    }

    // ---- events ----
    const float EventSeconds = 7f;

    void ShowEvents()
    {
        int n = 0;
        foreach (var kv in GameEvents.Local.Recent(Time.time))
        {
            if (n >= feed.Count) break;
            if (kv.Value > EventSeconds) continue;
            feed[n].text = Describe(kv.Key, out Color c);
            c.a = Mathf.Clamp01((EventSeconds - kv.Value) / 1.5f);
            feed[n].color = c;
            n++;
        }
        for (; n < feed.Count; n++) feed[n].text = "";
    }

    static string Describe(GameEvent e, out Color colour)
    {
        string what = DisplayNames.Of(e.Detail ?? ""), where = string.IsNullOrEmpty(e.Zone) ? "" : "  (" + e.Zone + ")";
        colour = UiKit.Paper;
        switch (e.Type)
        {
            case "UNDER_ATTACK": colour = UiKit.Red; return what + " under attack" + where;
            case "UNIT_LOST": colour = UiKit.Red; return what + " lost" + where;
            case "BUILDING_LOST": colour = UiKit.Red; return what + " destroyed" + where;
            case "ENEMY_SIGHTED": colour = new Color(1f, 0.72f, 0.35f); return "Sighted: " + (e.Detail ?? "enemy").Replace('_', ' ') + where;
            case "UNIT_TRAINED": return what + " is ready";
            case "BUILDING_COMPLETED": colour = UiKit.Green; return what + " completed";
            case "TECH_RESEARCHED": colour = UiKit.Gold; return what + " researched";
            case "RESOURCE_DEPLETED": return what + " source is empty" + where;
            default: return e.Type;
        }
    }
}
