using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Menu scene UI: a session name field and Host / Join buttons. All logic is in GameLauncher.</summary>
public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance { get; private set; }

    [SerializeField] TMP_InputField sessionNameInput;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_Text statusText;

    void Awake() { Instance = this; }

    void Start()
    {
        if (sessionNameInput != null && string.IsNullOrEmpty(sessionNameInput.text)) sessionNameInput.text = "room1";
        hostButton.onClick.AddListener(() => Begin(true));
        joinButton.onClick.AddListener(() => Begin(false));
        ShowMessage(GameLauncher.LastMessage); // e.g. "opponent left" after a match
    }

    void Begin(bool host)
    {
        if (GameLauncher.Instance == null) { ShowMessage("GameLauncher sahnede yok."); return; }
        SetButtons(false);
        if (statusText != null) statusText.text = host ? "Host baslatiliyor..." : "Baglaniliyor...";
        if (host) GameLauncher.Instance.StartHost(sessionNameInput.text);
        else GameLauncher.Instance.StartClient(sessionNameInput.text);
    }

    /// <summary>Shows a status line and re-enables the buttons (used after a failure or when coming back to the menu).</summary>
    public void ShowMessage(string text)
    {
        if (statusText != null) statusText.text = text ?? "";
        SetButtons(true);
    }

    void SetButtons(bool on)
    {
        if (hostButton != null) hostButton.interactable = on;
        if (joinButton != null) joinButton.interactable = on;
    }
}
