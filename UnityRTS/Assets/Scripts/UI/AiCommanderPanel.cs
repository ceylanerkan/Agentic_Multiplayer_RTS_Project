using System;
using System.Text;
using System.Threading.Tasks;
using RTS.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The AI commander window. The player types a question; the panel builds the visibility-filtered snapshot, asks the
/// gateway and shows: summary, confidence, evidence (tool names / knowledge chunk ids) and the proposed actions with costs.
/// Answers without actions are read-only and need no approval. With actions, three buttons appear:
/// Approve / Reject (see AiApprovalFlow) and "Ask why" (explains the last plan; also read-only).
/// </summary>
public class AiCommanderPanel : MonoBehaviour
{
    TMP_InputField questionInput;
    const int MaxQuestionLength = 500;   // = MAX_QUESTION_LEN of the gateway (a pytest checks both)
    Button askButton, approveButton, rejectButton, whyButton;
    TMP_Text answerText;      // summary + confidence + evidence + actions
    TMP_Text statusText;      // loading / errors / approval result
    [SerializeField] string gatewayUrl = "http://127.0.0.1:8765";

    AiGatewayClient client;
    AiAnswer current;
    bool busy;

    const float Width = 440f, Height = 640f, HeaderHeight = 50f;
    static readonly string[] Suggestions = { "What should I do next?", "Which enemies can I see?" };

    RectTransform body, answerContent;
    TMP_Text placeholder, foldText, confidenceText;
    UiKit.Bar confidenceBar;
    GameObject confidenceRow;
    bool folded;

    void Awake() { Build(); }

    void Start()
    {
        client = new AiGatewayClient(gatewayUrl);
        // the gateway rejects longer questions (MAX_QUESTION_LEN in ai/gateway/app.py); stop the typing here instead
        if (questionInput != null) questionInput.characterLimit = MaxQuestionLength;
        ShowAnswer(null);
    }

    // ---- building ----
    void Build()
    {
        UiKit.Clear(transform);
        var own = GetComponent<Image>(); if (own == null) own = gameObject.AddComponent<Image>();
        UiKit.Paint(own, UiKit.Edge, true, true);
        var rt = (RectTransform)transform;
        var inside = UiKit.Box(rt, "Inside", UiKit.Panel, true); UiKit.Stretch(inside, 0, 0, 1, 1, 2, 2, -2, -2);

        var head = UiKit.Button(inside, "Header", new Color(0.10f, 0.13f, 0.18f), () => { folded = !folded; Fold(); }); UiKit.Stretch((RectTransform)head.transform, 0, 1, 1, 1, 0, -HeaderHeight + 4, 0, 0);
        var icon = UiKit.Icon(head.transform, "ai", UiKit.Gold); UiKit.At(icon.rectTransform, 0, 0.5f, 14, 0, 30, 30);
        var title = UiKit.Text(head.transform, "AI COMMANDER", 22, UiKit.Gold, TextAlignmentOptions.MidlineLeft, FontStyles.Bold); title.characterSpacing = 3; UiKit.Stretch(title.rectTransform, 0, 0, 1, 1, 56, 0, -50, 0);
        foldText = UiKit.Text(head.transform, "", 30, UiKit.Dim, TextAlignmentOptions.Center, FontStyles.Bold); UiKit.At(foldText.rectTransform, 1, 0.5f, -6, 2, 40, 40);

        body = UiKit.Stretch(UiKit.Empty(inside, "Body"), 0, 0, 1, 1, 0, 0, 0, -HeaderHeight);

        // answer: scrolls when it is long
        var view = UiKit.Box(body, "Answer", new Color(0, 0, 0, 0.25f), true, true); UiKit.Stretch(view, 0, 0, 1, 1, 12, 248, -12, -8);
        view.gameObject.AddComponent<RectMask2D>();
        answerContent = UiKit.Empty(view, "Content"); UiKit.Stretch(answerContent, 0, 1, 1, 1, 12, 0, -12, 0); answerContent.pivot = new Vector2(0.5f, 1);
        answerText = answerContent.gameObject.AddComponent<TextMeshProUGUI>();
        var font = EntityVisuals.DefaultFont(); if (font != null) answerText.font = font;
        answerText.fontSize = 20; answerText.color = UiKit.Paper; answerText.alignment = TextAlignmentOptions.TopLeft; answerText.raycastTarget = false; answerText.margin = new Vector4(0, 10, 0, 10);
        var fitter = answerContent.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.content = answerContent; scroll.viewport = view; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30f;
        placeholder = UiKit.Text(view, "Ask about the rules, your economy, what you can see, or for a plan.\n\nNothing happens in the game until you press Approve.", 19, UiKit.Dim, TextAlignmentOptions.Center, FontStyles.Italic);
        UiKit.Stretch(placeholder.rectTransform, 0, 0, 1, 1, 26, 10, -26, -10);

        // confidence
        var row = UiKit.Empty(body, "Confidence"); UiKit.Stretch(row, 0, 0, 1, 0, 14, 222, -14, 244); confidenceRow = row.gameObject;
        confidenceText = UiKit.Text(row, "", 16, UiKit.Dim); UiKit.Stretch(confidenceText.rectTransform, 0, 0, 1, 1, 0, 0, 0, 0);
        confidenceBar = new UiKit.Bar(row, UiKit.Gold); UiKit.Stretch(confidenceBar.Root, 0, 0.5f, 1, 0.5f, 150, -5, 0, 5);

        // decision
        approveButton = UiKit.TextButton(body, "Approve", new Color(0.20f, 0.48f, 0.23f), UiKit.Paper, 20, Approve); UiKit.At((RectTransform)approveButton.transform, 0, 0, 12, 172, 132, 44);
        rejectButton = UiKit.TextButton(body, "Reject", new Color(0.50f, 0.18f, 0.15f), UiKit.Paper, 20, Reject); UiKit.At((RectTransform)rejectButton.transform, 0, 0, 152, 172, 132, 44);
        whyButton = UiKit.TextButton(body, "Ask why", UiKit.Field, UiKit.Paper, 20, () => _ = Why()); UiKit.At((RectTransform)whyButton.transform, 1, 0, -12, 172, 132, 44);

        statusText = UiKit.Fit(UiKit.Text(body, "", 18, UiKit.Gold, TextAlignmentOptions.TopLeft), 11); UiKit.Stretch(statusText.rectTransform, 0, 0, 1, 0, 14, 106, -14, 166);

        // question
        for (int i = 0; i < Suggestions.Length; i++)
        {
            string q = Suggestions[i];
            var b = UiKit.TextButton(body, q, new Color(0.10f, 0.14f, 0.20f), UiKit.Dim, 17, () => { if (!busy) { questionInput.text = q; _ = Ask(); } });
            UiKit.At((RectTransform)b.transform, 0, 0, 12 + i * 209, 66, 203, 34);
        }
        questionInput = UiKit.Input(body, "Question", "Ask the AI commander...", 20); UiKit.Stretch((RectTransform)questionInput.transform, 0, 0, 1, 0, 12, 12, -104, 58);
        questionInput.onSubmit.AddListener(_ => { var ask = Ask(); });
        askButton = UiKit.TextButton(body, "Ask", UiKit.Gold, UiKit.Ink, 22, () => _ = Ask()); UiKit.At((RectTransform)askButton.transform, 1, 0, -12, 12, 86, 46);
        Fold();
    }

    void Fold()
    {
        body.gameObject.SetActive(!folded);
        foldText.text = folded ? "+" : "\u2013";
        UiKit.At((RectTransform)transform, 1, 0, -12, 12, Width, folded ? HeaderHeight : Height);
    }

    // Says which step is missing, so a failed test points to the cause (before: always "Match not started yet.").
    static string WhyNoSnapshot()
    {
        var launcher = GameLauncher.Instance;
        if (launcher == null || launcher.Runner == null) return "Not connected: start from the Menu scene and press Host or Join.";
        if (!launcher.Runner.IsRunning) return "Still connecting to the session...";
        if (GameSpawner.Instance == null) return "No GameSpawner in the Game scene (Tools > RTS > Rebuild).";
        if (GameSpawner.SpawnError != null) return "Base spawn failed: " + GameSpawner.SpawnError + " (see Console).";
        if (launcher.IsHost) return "Your player was not spawned yet; if this stays, check the Console for GameSpawner errors.";
        return "Waiting for the host to spawn your base...";
    }

    async Task Ask()
    {
        string q = questionInput.text.Trim();
        if (busy || q.Length == 0) return;
        var snap = AiSnapshotBuilder.Build();
        if (snap == null) { statusText.text = WhyNoSnapshot(); return; }
        SetBusy(true, "AI is thinking...");
        try
        {
            var (answer, error) = await client.Ask(snap, q);
            if (this == null) return;   // the match ended while the AI was thinking: the panel is gone
            SetBusy(false, error ?? "");
            if (answer != null) ShowAnswer(answer);
        }
        catch (Exception e)   // "_ = Ask()" would swallow it and leave the panel busy for the rest of the match
        {
            if (this == null) return;
            Debug.LogException(e);
            SetBusy(false, "Error: " + e.GetType().Name + " (see Console)");
        }
    }

    async Task Why()
    {
        if (busy || current == null) return;
        SetBusy(true, "Asking why...");
        try
        {
            var (answer, error) = await client.AskWhy();
            if (this == null) return;
            SetBusy(false, error ?? "");
            if (answer != null) statusText.text = "Why: " + answer.Plan.Summary; // read-only explanation, no actions to approve
        }
        catch (Exception e)
        {
            if (this == null) return;
            Debug.LogException(e);
            SetBusy(false, "Error: " + e.GetType().Name + " (see Console)");
        }
    }

    void SetBusy(bool b, string message)
    {
        busy = b; statusText.text = message;
        askButton.interactable = !b; whyButton.interactable = !b && current != null;
        if (b) { approveButton.interactable = false; rejectButton.interactable = false; }
    }

    void ShowAnswer(AiAnswer a)
    {
        current = a;
        var data = GameDataProvider.Get();
        var sb = new StringBuilder();
        bool hasActions = a != null && a.Plan != null && a.Plan.Actions != null && a.Plan.Actions.Count > 0;
        if (a != null && a.Plan != null)
        {
            string P(string s) => UiKit.Plain(s);
            sb.AppendLine(P(a.Plan.Summary)).AppendLine();
            if (hasActions)
            {
                sb.AppendLine("<b><color=#EDC25C>Proposed actions:</color></b>");
                foreach (var d in a.Plan.Actions)
                    sb.AppendLine("<indent=3%>\u2022 <b>" + P(ActionCosts.Describe(d.ToPlanAction(), data)) + "</b>" + (string.IsNullOrEmpty(d.Reason) ? "" : "\n<size=85%><color=#A8B0BC>" + P(d.Reason) + "</color></size>") + "</indent>");
                sb.AppendLine();
            }
            else sb.AppendLine("<i><color=#A8B0BC>(Information only: nothing to approve.)</color></i>").AppendLine();
            sb.Append("<size=80%><color=#8A93A0>Confidence: ").Append(Mathf.RoundToInt(a.Plan.Confidence * 100)).AppendLine("%");
            if (a.Plan.Evidence != null && a.Plan.Evidence.Count > 0) sb.AppendLine("Evidence: " + P(string.Join(", ", a.Plan.Evidence)));
            if (a.ToolsUsed != null && a.ToolsUsed.Count > 0) sb.AppendLine("Tools: " + P(string.Join(", ", a.ToolsUsed)));
            sb.Append("</color></size>");
            confidenceText.text = "Confidence  " + Mathf.RoundToInt(a.Plan.Confidence * 100) + "%";
            confidenceBar.Set(a.Plan.Confidence, a.Plan.Confidence >= 0.7f ? UiKit.Green : a.Plan.Confidence >= 0.4f ? UiKit.Gold : UiKit.Red);
        }
        answerText.text = sb.ToString();
        answerContent.anchoredPosition = Vector2.zero;
        placeholder.gameObject.SetActive(sb.Length == 0);
        confidenceRow.SetActive(sb.Length > 0);
        bool needs = hasActions && a.NeedsApproval;
        approveButton.gameObject.SetActive(needs); rejectButton.gameObject.SetActive(needs);
        approveButton.interactable = needs; rejectButton.interactable = needs;
        whyButton.gameObject.SetActive(a != null); whyButton.interactable = a != null && !busy;
    }

    void Approve()
    {
        if (current == null) return;
        statusText.text = AiApprovalFlow.Approve(current);
        ShowAnswer(null); // one decision per plan
    }

    void Reject()
    {
        if (current == null) return;
        statusText.text = AiApprovalFlow.Reject(current);
        ShowAnswer(null);
    }
}
