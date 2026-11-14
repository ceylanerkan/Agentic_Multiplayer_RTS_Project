using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Starts and stops the Photon Fusion session (Host Mode). Lives in the Menu scene and survives scene loads.
/// Host: StartGame(GameMode.Host) and loads the Game scene for everyone. Client: StartGame(GameMode.Client).
/// If the other player leaves, the match ends and this player goes back to the menu.
/// </summary>
public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static GameLauncher Instance { get; private set; }

    [SerializeField] string menuSceneName = "Menu";
    [SerializeField] string gameSceneName = "Game";

    NetworkRunner runner;
    bool matchRunning;        // true once two players are in the Game scene
    bool returningToMenu;     // avoids handling OnShutdown twice

    /// <summary>Shown by MenuUI after we came back from a match or a failed connection.</summary>
    public static string LastMessage = "";

    public NetworkRunner Runner => runner;
    public bool IsHost => runner != null && runner.IsServer;

    /// <summary>1 or 2 for the local player, 0 if not in a match. Same answer on host and client.</summary>
    public int LocalSlot => runner == null ? 0 : SlotOf(runner.LocalPlayer);

    public int SlotOf(PlayerRef player)
    {
        if (runner == null) return 0;
        return PlayerSlots.SlotOf(runner.ActivePlayers.Select(p => p.RawEncoded), player.RawEncoded);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void StartHost(string sessionName) { await StartSession(GameMode.Host, sessionName); }
    public async void StartClient(string sessionName) { await StartSession(GameMode.Client, sessionName); }

    // Not called "Start": Unity reserves Start() for its own parameterless startup message.
    async System.Threading.Tasks.Task StartSession(GameMode mode, string sessionName)
    {
        if (runner != null) return; // already connecting or running
        sessionName = string.IsNullOrWhiteSpace(sessionName) ? "room1" : sessionName.Trim();
        returningToMenu = false; matchRunning = false; LastMessage = "";

        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var args = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = PlayerSlots.MaxPlayers,
            SceneManager = sceneManager,
        };
        // Only the host picks the scene; clients load whatever the host loaded.
        if (mode == GameMode.Host) args.Scene = SceneRef.FromIndex(BuildIndexOf(gameSceneName));

        StartGameResult result = await runner.StartGame(args);
        if (!result.Ok)
        {
            LastMessage = "Baglanti kurulamadi: " + result.ShutdownReason + " " + result.ErrorMessage;
            Debug.LogWarning(LastMessage);
            await CleanUp();
            if (MenuUI.Instance != null) MenuUI.Instance.ShowMessage(LastMessage);
        }
        else if (mode == GameMode.Host)
        {
            Debug.Log("Hosting session '" + sessionName + "'; waiting for the second player.");
        }
    }

    static int BuildIndexOf(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            if (Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)) == sceneName) return i;
        throw new InvalidOperationException("Scene '" + sceneName + "' is not in Build Settings.");
    }

    /// <summary>Leave the match on purpose (Quit button) or because the opponent left.</summary>
    public async void ReturnToMenu(string message)
    {
        if (returningToMenu) return;
        returningToMenu = true;
        LastMessage = message ?? "";
        await CleanUp();
        SceneManager.LoadScene(menuSceneName);
    }

    async System.Threading.Tasks.Task CleanUp()
    {
        matchRunning = false;
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
            var r = runner; runner = null;
            await r.Shutdown();
            Destroy(r);
            var sm = GetComponent<NetworkSceneManagerDefault>();
            if (sm != null) Destroy(sm);
        }
    }

    // ---- INetworkRunnerCallbacks ----

    public void OnPlayerJoined(NetworkRunner r, PlayerRef player)
    {
        if (r.IsServer && r.ActivePlayers.Count() >= PlayerSlots.MaxPlayers) matchRunning = true;
    }

    public void OnPlayerLeft(NetworkRunner r, PlayerRef player)
    {
        // Host side: the opponent left, so the match is over.
        if (r.IsServer && matchRunning) ReturnToMenu("Rakip oyundan ayrildi, mac bitti.");
    }

    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason)
    {
        // Client side: the host is gone.
        ReturnToMenu("Host ile baglanti koptu (" + reason + ").");
    }

    public void OnConnectFailed(NetworkRunner r, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        ReturnToMenu("Baglanti basarisiz (" + reason + ").");
    }

    public void OnShutdown(NetworkRunner r, ShutdownReason reason)
    {
        if (!returningToMenu && runner == r) ReturnToMenu("Oturum kapandi (" + reason + ").");
    }

    // Unused callbacks (required by the interface). Input is added later with the unit commands.
    public void OnInput(NetworkRunner r, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
}
