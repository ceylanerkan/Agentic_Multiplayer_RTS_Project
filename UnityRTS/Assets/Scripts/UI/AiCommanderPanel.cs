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
    [SerializeField] TMP_InputField questionInput;
    [SerializeField] Button askButton, approveButton, rejectButton, whyButton;
    [SerializeField] TMP_Text answerText;      // summary + confidence + evidence + actions
    [SerializeField] TMP_Text statusText;      // loading / errors / approval result
    [SerializeField] string gatewayUrl = "http://127.0.0.1:8765";

    AiGatewayClient client;
    AiAnswer current;
    bool busy;

    void Start()
    {
        client = new AiGatewayClient(gatewayUrl);
        // long LLM answers shrink to fit the box instead of running out of the panel
        foreach (var t in new[] { answerText, statusText })
            if (t != null) { t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = t.fontSize; }
        askButton.onClick.AddListener(() => _ = Ask());
        whyButton.onClick.AddListener(() => _ = Why());
        approveButton.onClick.AddListener(Approve);
        rejectButton.onClick.AddListener(Reject);
        ShowAnswer(null);
    }

    // Says which step is missing, so a failed test points to the cause (before: always "Match not started yet.").
    static string WhyNoSnapshot()
    {
        var launcher = GameLauncher.Instance;
        if (launcher == null || launcher.Runner == null) return "Not connected: start from the Menu scene and press Host or Join.";
        if (!launcher.Runner.IsRunning) return "Still connecting to the session...";
        if (GameSpawner.Instance == null) return "No GameSpawner in the Game scene (Tools > RTS > Rebuild).";
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
            SetBusy(false, error ?? "");
            if (answer != null) ShowAnswer(answer);
        }
        catch (Exception e)   // "_ = Ask()" would swallow it and leave the panel busy for the rest of the match
        {
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
            SetBusy(false, error ?? "");
            if (answer != null) statusText.text = "Why: " + answer.Plan.Summary; // read-only explanation, no actions to approve
        }
        catch (Exception e)
        {
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
            sb.AppendLine(a.Plan.Summary);
            sb.AppendLine("Confidence: " + Mathf.RoundToInt(a.Plan.Confidence * 100) + "%");
            if (a.Plan.Evidence != null && a.Plan.Evidence.Count > 0) sb.AppendLine("Evidence: " + string.Join(", ", a.Plan.Evidence));
            if (a.ToolsUsed != null && a.ToolsUsed.Count > 0) sb.AppendLine("Tools: " + string.Join(", ", a.ToolsUsed));
            if (hasActions)
            {
                sb.AppendLine("Proposed actions:");
                foreach (var d in a.Plan.Actions) sb.AppendLine(" - " + ActionCosts.Describe(d.ToPlanAction(), data) + (string.IsNullOrEmpty(d.Reason) ? "" : "  (" + d.Reason + ")"));
            }
            else sb.AppendLine("(Information only: nothing to approve.)");
        }
        answerText.text = sb.ToString();
        bool needs = hasActions && a.NeedsApproval;
        approveButton.gameObject.SetActive(needs); rejectButton.gameObject.SetActive(needs);
        approveButton.interactable = needs; rejectButton.interactable = needs;
        whyButton.interactable = a != null && !busy;
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
