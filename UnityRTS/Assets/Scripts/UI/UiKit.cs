using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The look of the in-game interface in one place: colours, framed panels, texts, icons (Resources/Ui), buttons, bars and the tooltip.
/// HudUI, SelectionPanel, MiniMap, AiCommanderPanel and GameOverUI build themselves with it when the scene starts.
/// </summary>
public static class UiKit
{
    public static readonly Color Gold = new Color(0.93f, 0.76f, 0.36f), Edge = new Color(0.47f, 0.39f, 0.22f, 0.95f), Paper = new Color(0.93f, 0.92f, 0.88f),
                                 Dim = new Color(0.66f, 0.69f, 0.74f), Panel = new Color(0.055f, 0.07f, 0.095f, 0.95f), Field = new Color(0.12f, 0.15f, 0.20f, 1f),
                                 Ink = new Color(0.08f, 0.09f, 0.12f), Blue = new Color(0.25f, 0.52f, 1f), Red = new Color(0.95f, 0.35f, 0.29f), Green = new Color(0.36f, 0.78f, 0.38f),
                                 WoodColour = new Color(0.82f, 0.62f, 0.38f), FoodColour = new Color(0.95f, 0.50f, 0.42f), GoldColour = new Color(1f, 0.83f, 0.32f);

    // ---- pieces ----
    /// <summary>A panel with a thin golden edge; returns the inside, its parent is the panel itself.</summary>
    public static RectTransform Frame(Transform parent, string name)
    {
        var outer = Box(parent, name, Edge, true, true);
        var inner = Box(outer, "Inside", Panel, true);
        Stretch(inner, 0, 0, 1, 1, 2, 2, -2, -2);
        return inner;
    }

    /// <summary>A coloured rectangle; solid ones catch the mouse, so a click does not reach the map below.</summary>
    public static RectTransform Box(Transform parent, string name, Color colour, bool rounded = false, bool solid = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        Paint(go.GetComponent<Image>(), colour, rounded, solid);
        return (RectTransform)go.transform;
    }

    public static void Paint(Image image, Color colour, bool rounded, bool solid)
    {
        image.color = colour; image.raycastTarget = solid;
        if (rounded) { image.sprite = Round; image.type = Image.Type.Sliced; }
    }

    public static RectTransform Empty(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static TMP_Text Text(Transform parent, string text, float size, Color colour, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        var font = EntityVisuals.DefaultFont(); if (font != null) t.font = font; // font first: a TMP text without one throws in layout
        t.text = text; t.fontSize = size; t.color = colour; t.fontStyle = style; t.raycastTarget = false; t.alignment = align;
        return t;
    }

    /// <summary>A text that shrinks instead of running out of its box.</summary>
    public static TMP_Text Fit(TMP_Text t, float smallest) { t.enableAutoSizing = true; t.fontSizeMax = t.fontSize; t.fontSizeMin = smallest; return t; }

    static readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();

    public static RawImage Icon(Transform parent, string key, Color colour)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(RawImage)); go.transform.SetParent(parent, false);
        var i = go.GetComponent<RawImage>(); i.raycastTarget = false; i.color = colour;
        SetIcon(i, key);
        return i;
    }

    public static void SetIcon(RawImage image, string key)
    {
        if (!icons.TryGetValue(key, out var t) || t == null) { t = UnityEngine.Resources.Load<Texture2D>("Ui/" + key); icons[key] = t; }
        image.texture = t; image.enabled = t != null;
    }

    public static Button Button(Transform parent, string name, Color colour, Action onClick)
    {
        var rt = Box(parent, name, Color.white, true, true);
        var b = rt.gameObject.AddComponent<Button>();
        var c = b.colors;
        c.normalColor = colour; c.selectedColor = colour;
        c.highlightedColor = Color.Lerp(colour, Color.white, 0.18f); c.pressedColor = Color.Lerp(colour, Color.black, 0.25f);
        c.disabledColor = new Color(colour.r * 0.55f, colour.g * 0.55f, colour.b * 0.55f, 0.75f); c.fadeDuration = 0.08f;
        b.colors = c;
        b.navigation = new Navigation { mode = Navigation.Mode.None }; // the arrow keys move the camera
        if (onClick != null) b.onClick.AddListener(() => onClick());
        return b;
    }

    public static Button TextButton(Transform parent, string label, Color colour, Color textColour, float size, Action onClick)
    {
        var b = Button(parent, label, colour, onClick);
        var t = Fit(Text(b.transform, label, size, textColour, TextAlignmentOptions.Center, FontStyles.Bold), 10);
        Stretch(t.rectTransform, 0, 0, 1, 1, 6, 2, -6, -2);
        return b;
    }

    public static TMP_InputField Input(Transform parent, string name, string hintText, float size)
    {
        var rt = Box(parent, name, Field, true, true);
        var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D)); area.transform.SetParent(rt, false);
        Stretch((RectTransform)area.transform, 0, 0, 1, 1, 12, 4, -12, -4);
        var hint = Text(area.transform, hintText, size, new Color(0.45f, 0.48f, 0.53f)); Stretch(hint.rectTransform, 0, 0, 1, 1, 0, 0, 0, 0);
        var text = Text(area.transform, "", size, Paper); Stretch(text.rectTransform, 0, 0, 1, 1, 0, 0, 0, 0);
        text.richText = false;
        var f = rt.gameObject.AddComponent<TMP_InputField>();
        f.textViewport = (RectTransform)area.transform; f.textComponent = text; f.placeholder = hint;
        f.customCaretColor = true; f.caretColor = Gold; f.selectionColor = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
        return f;
    }

    /// <summary>A bar that fills from the left.</summary>
    public class Bar
    {
        public readonly RectTransform Root;
        readonly RectTransform fill; readonly Image image;

        public Bar(Transform parent, Color colour)
        {
            Root = Box(parent, "Bar", new Color(0, 0, 0, 0.75f));
            fill = Box(Root, "Fill", colour); image = fill.GetComponent<Image>();
            Set(1f);
        }

        public void Set(float value) { Stretch(fill, 0, 0, Mathf.Clamp01(value), 1, 1, 1, -1, -1); }
        public void Set(float value, Color colour) { Set(value); image.color = colour; }
    }

    public static Color HpColour(float part) { return part > 0.6f ? Green : part > 0.3f ? new Color(0.95f, 0.78f, 0.25f) : Red; }

    // ---- placing ----
    /// <summary>Anchor and pivot at the same corner (0..1), then offset and size.</summary>
    public static RectTransform At(RectTransform rt, float ax, float ay, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ax, ay); rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt, float x0, float y0, float x1, float y1, float left, float bottom, float right, float top)
    {
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1); rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(right, top);
        return rt;
    }

    /// <summary>Removes what an older version of the scene put under this object.</summary>
    public static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) { var c = t.GetChild(i).gameObject; c.SetActive(false); UnityEngine.Object.Destroy(c); }
    }

    // ---- texts ----
    public static string CostText(Dictionary<string, int> cost, bool words)
    {
        var parts = new List<string>();
        foreach (var kv in cost)
            parts.Add("<color=#" + ColorUtility.ToHtmlStringRGB(kv.Key == "wood" ? WoodColour : kv.Key == "food" ? FoodColour : GoldColour) + ">" + kv.Value + (words ? " " + kv.Key : "") + "</color>");
        return string.Join(words ? ",  " : " ", parts);
    }

    /// <summary>Texts of the language model must not be read as rich text tags.</summary>
    public static string Plain(string s) { return (s ?? "").Replace('<', '‹').Replace('>', '›'); }

    // ---- tooltip ----
    static RectTransform tip; static TMP_Text tipText;

    public static void ShowTip(RectTransform near, string text)
    {
        var canvas = near.GetComponentInParent<Canvas>().rootCanvas.transform;
        if (tip == null)
        {
            var inside = Frame(canvas, "Tooltip"); tip = (RectTransform)inside.parent;
            tip.GetComponent<Image>().raycastTarget = false;
            tipText = Text(inside, "", 19, Paper, TextAlignmentOptions.TopLeft); Stretch(tipText.rectTransform, 0, 0, 1, 1, 12, 8, -12, -8);
        }
        tip.gameObject.SetActive(true); tip.SetAsLastSibling();
        tipText.text = text;
        float w = 380f, h = tipText.GetPreferredValues(text, w - 28f, 0f).y + 24f;
        // above the whole panel of the element, so it covers none of the buttons beside it
        var frame = near;
        while (frame.name != "Commands" && frame.parent is RectTransform up && up.GetComponent<Canvas>() == null && up.GetComponent<SelectionPanel>() == null) frame = up;
        var corners = new Vector3[4]; near.GetWorldCorners(corners);
        Vector3 p = canvas.InverseTransformPoint(corners[1]);
        frame.GetWorldCorners(corners);
        Vector3 foot = canvas.InverseTransformPoint(corners[0]); p.y = canvas.InverseTransformPoint(corners[1]).y;
        Rect screen = ((RectTransform)canvas).rect;
        At(tip, 0.5f, 0.5f, 0, 0, w, h);
        tip.localPosition = new Vector3(Mathf.Clamp(p.x + w * 0.5f, screen.xMin + w * 0.5f + 8, screen.xMax - w * 0.5f - 8), p.y + h + 16f > screen.yMax ? foot.y - h * 0.5f - 8f : p.y + h * 0.5f + 8f, 0); // at the top of the screen below it
    }

    public static void HideTip() { if (tip != null) tip.gameObject.SetActive(false); }

    // white rounded square, cut into nine parts: no image file needed
    static Sprite round;
    static Sprite Round
    {
        get
        {
            if (round != null) return round;
            const int n = 32; const float r = 8f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(r - (x + 0.5f), (x + 0.5f) - (n - r), 0), dy = Mathf.Max(r - (y + 0.5f), (y + 0.5f) - (n - r), 0);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f)));
                }
            tex.Apply();
            round = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
            return round;
        }
    }
}

/// <summary>Shows a tooltip while the mouse is over the element.</summary>
public class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Func<string> Tip;
    bool over;

    public static void Add(Component on, Func<string> tip) { on.gameObject.AddComponent<UiHover>().Tip = tip; }

    public void OnPointerEnter(PointerEventData e) { over = true; Show(); }
    public void OnPointerExit(PointerEventData e) { over = false; UiKit.HideTip(); }
    void Show() { string s = Tip != null ? Tip() : null; if (string.IsNullOrEmpty(s)) UiKit.HideTip(); else UiKit.ShowTip((RectTransform)transform, s); }
    void Update() { if (over && Time.frameCount % 15 == 0) Show(); } // costs and reasons change while the mouse rests
    void OnDisable() { if (over) { over = false; UiKit.HideTip(); } }
}
