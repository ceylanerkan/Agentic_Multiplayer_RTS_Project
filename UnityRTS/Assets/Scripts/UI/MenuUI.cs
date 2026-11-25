using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// The main menu, built in code on the (otherwise empty) MenuCanvas: start page, waiting room of the host, connecting page and the
/// list of controls. All session logic is in GameLauncher; this class shows its state every frame.
/// </summary>
public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance { get; private set; }

    static readonly Color Gold = new Color(0.93f, 0.76f, 0.36f), Ink = new Color(0.08f, 0.09f, 0.12f), Paper = new Color(0.93f, 0.92f, 0.88f),
                          Dim = new Color(0.68f, 0.71f, 0.76f), Dark = new Color(0.05f, 0.07f, 0.10f, 0.88f), Field = new Color(0.12f, 0.15f, 0.20f, 1f),
                          Blue = new Color(0.25f, 0.52f, 1f), Red = new Color(0.95f, 0.32f, 0.26f);
    const float Width = 520f, Left = 56f;

    RectTransform startPage, lobbyPage, connectPage, helpPage;
    TMP_InputField sessionField;
    TMP_Text message, lobbyName, lobbyHint, connectText, secondPlayer;
    Image secondDot;
    GameObject messageBox;
    Sprite round;
    bool help;

    void Awake() { Instance = this; Build(); MenuLandscape.Build(); }

    void Start()
    {
        string last = GameLauncher.LastMessage; // e.g. "your opponent left" after a match
        ShowMessage(last);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ---- state ----
    void Update()
    {
        var g = GameLauncher.Instance;
        var phase = g != null ? g.Phase : GameLauncher.State.Idle;
        Show(startPage, phase == GameLauncher.State.Idle && !help);
        Show(helpPage, phase == GameLauncher.State.Idle && help);
        Show(lobbyPage, phase == GameLauncher.State.Waiting);
        Show(connectPage, phase == GameLauncher.State.Connecting || phase == GameLauncher.State.Starting);
        if (g == null) return;
        string dots = new string('.', 1 + (int)(Time.unscaledTime * 2f) % 3);
        if (phase == GameLauncher.State.Waiting)
        {
            lobbyName.text = g.SessionName;
            lobbyHint.text = "The match starts by itself when the second player has joined" + dots;
            float pulse = 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f);
            secondDot.color = new Color(Red.r, Red.g, Red.b, pulse);
        }
        else if (phase == GameLauncher.State.Connecting) connectText.text = (g.Hosting ? "Opening the session" : "Joining \"" + g.SessionName + "\"") + dots;
        else if (phase == GameLauncher.State.Starting) connectText.text = "Both players are here. Loading the map" + dots;
    }

    static void Show(RectTransform page, bool on) { if (page.gameObject.activeSelf != on) page.gameObject.SetActive(on); }

    /// <summary>Shows a line under the buttons (why a match ended or a connection failed); empty hides it.</summary>
    public void ShowMessage(string text)
    {
        messageBox.SetActive(!string.IsNullOrEmpty(text));
        message.text = text ?? "";
    }

    void Begin(Action<GameLauncher, string> start)
    {
        var g = GameLauncher.Instance;
        if (g == null) { ShowMessage("GameLauncher is missing in the Menu scene."); return; }
        ShowMessage("");
        start(g, sessionField.text);
    }

    static void Quit()
    {
        Application.Quit();
    }

    // ---- building ----
    void Build()
    {
        round = RoundedSprite();
        var canvas = (RectTransform)transform;

        // dark band on the left with a gold edge; the landscape stays visible on the right
        var band = Box(canvas, "Band", Dark); Stretch(band, 0, 0, 0, 1, 0, 0, Width + 2 * Left, 0);
        var edge = Box(band, "Edge", Gold); Stretch(edge, 1, 0, 1, 1, -3, 0, 0, 0);

        var title = Text(band, "AGENTIC RTS", 74, Gold, FontStyles.Bold); title.characterSpacing = 3; Put(title.rectTransform, 64, 90);
        title.textWrappingMode = TextWrappingModes.NoWrap; title.enableAutoSizing = true; title.fontSizeMin = 40; title.fontSizeMax = 74; title.alignment = TextAlignmentOptions.MidlineLeft;
        var sub = Text(band, "Two players, one island and an AI commander that asks before it acts.", 23, Dim); Put(sub.rectTransform, 168, 64);

        startPage = Page(band, "Start");
        Put(Text(startPage, "Session name (the same on both computers)", 20, Dim).rectTransform, 0, 30);
        sessionField = Input(startPage, "room1", 34, 60);
        AddButton(startPage, "Host a match", 114, true, () => Begin((g, n) => g.StartHost(n)));
        AddButton(startPage, "Join a match", 190, false, () => Begin((g, n) => g.StartClient(n)));
        AddButton(startPage, "Practice alone", 266, false, () => Begin((g, n) => g.StartHost(n, true)));
        AddButton(startPage, "Controls", 366, false, () => help = true, 0.48f, 0f);
        AddButton(startPage, "Quit", 366, false, Quit, 0.48f, 0.52f);

        lobbyPage = Page(band, "Lobby");
        Put(Text(lobbyPage, "Waiting room", 40, Paper, FontStyles.Bold).rectTransform, 0, 50);
        Put(Text(lobbyPage, "Session", 20, Dim).rectTransform, 66, 28);
        lobbyName = Text(lobbyPage, "", 38, Gold, FontStyles.Bold); Put(lobbyName.rectTransform, 94, 50);
        Player(lobbyPage, 164, "Player 1", "you, the host", Blue, out _, out _);
        Player(lobbyPage, 244, "Player 2", "not here yet", Red, out secondDot, out secondPlayer);
        lobbyHint = Text(lobbyPage, "", 22, Dim); Put(lobbyHint.rectTransform, 330, 64);
        AddButton(lobbyPage, "Cancel", 412, false, () => GameLauncher.Instance.Cancel());

        connectPage = Page(band, "Connecting");
        connectText = Text(connectPage, "", 34, Paper, FontStyles.Bold); Put(connectText.rectTransform, 0, 100);
        AddButton(connectPage, "Cancel", 120, false, () => GameLauncher.Instance.Cancel());

        helpPage = Page(band, "Help");
        Put(Text(helpPage, "Controls", 40, Paper, FontStyles.Bold).rectTransform, 0, 50);
        string[,] rows =
        {
            { "WASD / arrows", "move the camera" }, { "Mouse wheel", "zoom" }, { "Left click, drag", "select (Shift adds)" },
            { "Right click", "move, gather, attack, help build" }, { "Villager + button", "place a building on free ground" }, { "Town Center, or + on top", "train Villagers" },
            { "Esc / right click", "cancel placing" }, { "AI Commander", "ask, then approve or reject the plan" },
        };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            var a = Text(helpPage, rows[i, 0], 21, Gold); Put(a.rectTransform, 62 + i * 37, 34, 0f, 0.40f);
            var b = Text(helpPage, rows[i, 1], 21, Paper); Put(b.rectTransform, 62 + i * 37, 34, 0.42f, 1f);
        }
        AddButton(helpPage, "Back", 372, false, () => help = false);

        // message under the page
        var box = Box(band, "Message", new Color(0.42f, 0.23f, 0.10f, 0.95f), true); messageBox = box.gameObject;
        box.anchorMin = new Vector2(0, 0); box.anchorMax = new Vector2(0, 0); box.pivot = new Vector2(0, 0);
        box.anchoredPosition = new Vector2(Left, 44); box.sizeDelta = new Vector2(Width, 92);
        message = Text(box, "", 22, Paper); message.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(message.rectTransform, 0, 0, 1, 1, 18, 8, -18, -8);
        message.enableAutoSizing = true; message.fontSizeMin = 15; message.fontSizeMax = 22;
        foreach (var p in new[] { lobbyPage, connectPage, helpPage }) p.gameObject.SetActive(false);
    }

    RectTransform Page(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(Left, -290); rt.sizeDelta = new Vector2(Width, 520);
        return rt;
    }

    void Player(RectTransform parent, float top, string name, string note, Color colour, out Image dot, out TMP_Text noteText)
    {
        var card = Box(parent, name, Field, true); Put(card, top, 68);
        var d = Box(card, "Dot", colour, true); d.anchorMin = d.anchorMax = new Vector2(0, 0.5f); d.anchoredPosition = new Vector2(34, 0); d.sizeDelta = new Vector2(26, 26);
        dot = d.GetComponent<Image>();
        var a = Text(card, name, 25, Paper, FontStyles.Bold); a.alignment = TextAlignmentOptions.MidlineLeft; Stretch(a.rectTransform, 0, 0, 0.5f, 1, 66, 0, 0, 0);
        noteText = Text(card, note, 21, Dim); noteText.alignment = TextAlignmentOptions.MidlineRight; Stretch(noteText.rectTransform, 0.5f, 0, 1, 1, 0, 0, -22, 0);
    }

    void AddButton(RectTransform parent, string label, float top, bool primary, UnityAction onClick, float width = 1f, float left = 0f)
    {
        var rt = Box(parent, label, Color.white, true); Put(rt, top, 62, left, left + width);
        var image = rt.GetComponent<Image>(); image.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        var c = button.colors;
        c.normalColor = primary ? Gold : new Color(0.17f, 0.21f, 0.28f, 1f);
        c.highlightedColor = primary ? new Color(1f, 0.86f, 0.5f) : new Color(0.25f, 0.31f, 0.40f, 1f);
        c.pressedColor = primary ? new Color(0.8f, 0.62f, 0.25f) : new Color(0.12f, 0.15f, 0.20f, 1f);
        c.selectedColor = c.normalColor; c.fadeDuration = 0.08f;
        button.colors = c;
        button.onClick.AddListener(onClick);
        var t = Text(rt, label, 27, primary ? Ink : Paper, FontStyles.Bold); t.alignment = TextAlignmentOptions.Center;
        Stretch(t.rectTransform, 0, 0, 1, 1, 0, 0, 0, 0);
    }

    TMP_InputField Input(RectTransform parent, string value, float top, float height)
    {
        var rt = Box(parent, "SessionName", Field, true); Put(rt, top, height); rt.GetComponent<Image>().raycastTarget = true;
        var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D)); area.transform.SetParent(rt, false);
        Stretch((RectTransform)area.transform, 0, 0, 1, 1, 18, 6, -18, -6);
        var hint = Text((RectTransform)area.transform, "room1", 27, new Color(0.45f, 0.48f, 0.53f)); hint.alignment = TextAlignmentOptions.MidlineLeft; Stretch(hint.rectTransform, 0, 0, 1, 1, 0, 0, 0, 0);
        var text = Text((RectTransform)area.transform, "", 27, Paper); text.alignment = TextAlignmentOptions.MidlineLeft; Stretch(text.rectTransform, 0, 0, 1, 1, 0, 0, 0, 0);
        text.richText = false;
        var f = rt.gameObject.AddComponent<TMP_InputField>();
        f.textViewport = (RectTransform)area.transform; f.textComponent = text; f.placeholder = hint; f.characterLimit = 24;
        f.customCaretColor = true; f.caretColor = Gold; f.selectionColor = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
        f.text = value;
        return f;
    }

    static TMP_Text Text(Transform parent, string text, float size, Color colour, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        var font = EntityVisuals.DefaultFont(); if (font != null) t.font = font; // font first: a TMP text without one throws in layout
        t.text = text; t.fontSize = size; t.color = colour; t.fontStyle = style; t.raycastTarget = false; t.alignment = TextAlignmentOptions.TopLeft;
        return t;
    }

    RectTransform Box(Transform parent, string name, Color colour, bool rounded = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.color = colour; image.raycastTarget = false;
        if (rounded) { image.sprite = round; image.type = Image.Type.Sliced; }
        return (RectTransform)go.transform;
    }

    /// <summary>A line of the page: distance from the top, height, and which part of the width it takes.</summary>
    static void Put(RectTransform rt, float top, float height, float from = 0f, float to = 1f)
    {
        bool onBand = rt.parent != null && rt.parent.name == "Band";
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2((onBand ? Left : 0) + from * Width, -top); rt.sizeDelta = new Vector2((to - from) * Width, height);
    }

    static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1, float left, float bottom, float right, float top)
    {
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1); rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(right, top);
    }

    // white rounded square, cut into nine parts: no image file needed
    static Sprite RoundedSprite()
    {
        const int n = 32; const float r = 10f;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(r - (x + 0.5f), (x + 0.5f) - (n - r), 0), dy = Mathf.Max(r - (y + 0.5f), (y + 0.5f) - (n - r), 0);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
    }
}
