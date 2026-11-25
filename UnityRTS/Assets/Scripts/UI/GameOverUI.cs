using TMPro;
using UnityEngine;

/// <summary>
/// Victory / Defeat screen, built in code on the GameCanvas. Shows when the local player's [Networked] Result changes from 0.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    RectTransform panel;
    TMP_Text resultText, detail;
    NetworkPlayerState state;

    void Awake()
    {
        var old = transform.Find("GameOverPanel"); // scenes made before the interface was built in code
        if (old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
        panel = UiKit.Box(transform, "GameOver", new Color(0.02f, 0.03f, 0.05f, 0.72f), false, true); UiKit.Stretch(panel, 0, 0, 1, 1, 0, 0, 0, 0);
        var box = UiKit.Frame(panel, "Box"); UiKit.At((RectTransform)box.parent, 0.5f, 0.5f, 0, 40, 760, 360);
        resultText = UiKit.Text(box, "", 96, UiKit.Gold, TextAlignmentOptions.Center, FontStyles.Bold); resultText.characterSpacing = 8; UiKit.At(resultText.rectTransform, 0.5f, 1, 0, -36, 720, 120);
        detail = UiKit.Text(box, "", 26, UiKit.Dim, TextAlignmentOptions.Center); UiKit.At(detail.rectTransform, 0.5f, 1, 0, -160, 700, 60);
        var back = UiKit.TextButton(box, "Back to menu", UiKit.Gold, UiKit.Ink, 28, OnMenuClicked); UiKit.At((RectTransform)back.transform, 0.5f, 0, 0, 40, 340, 68);
        panel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (state == null)
        {
            if (CommandRelay.Local == null) return;
            state = CommandRelay.Local.GetComponent<NetworkPlayerState>();
            if (state == null) return;
        }
        // the session is shutting down: the object still exists for a frame, but its [Networked] values can no longer be read
        if (state.Object == null || !state.Object.IsValid) return;
        if (state.Result == 0 || panel.gameObject.activeSelf) return;
        panel.gameObject.SetActive(true); panel.SetAsLastSibling();
        bool won = state.Result == 1;
        resultText.text = won ? "VICTORY" : "DEFEAT"; resultText.color = won ? UiKit.Gold : UiKit.Red;
        int s = (int)DayNight.MatchSeconds();
        detail.text = (won ? "The enemy Town Center has fallen" : "Your Town Center has fallen") + " after " + s / 60 + ":" + (s % 60).ToString("00") + " minutes.";
    }

    /// <summary>The "Back to menu" button.</summary>
    public void OnMenuClicked()
    {
        if (GameLauncher.Instance != null) GameLauncher.Instance.ReturnToMenu("");
    }
}
