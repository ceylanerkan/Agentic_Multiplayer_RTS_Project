using TMPro;
using UnityEngine;

/// <summary>
/// Victory / Defeat screen. Put the panel (inactive at start) in the Game canvas and assign it with its TMP_Text.
/// Shows when the local player's [Networked] Result changes from 0. The button calls OnMenuClicked.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text resultText;

    NetworkPlayerState state;

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
        if (state.Result == 0 || panel == null || panel.activeSelf) return;
        panel.SetActive(true);
        if (resultText != null) resultText.text = state.Result == 1 ? "VICTORY" : "DEFEAT";
    }

    /// <summary>Hooked to the "Back to menu" button.</summary>
    public void OnMenuClicked()
    {
        if (GameLauncher.Instance != null) GameLauncher.Instance.ReturnToMenu("Mac bitti.");
    }
}
