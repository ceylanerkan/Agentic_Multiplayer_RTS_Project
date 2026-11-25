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
/// Host: StartGame(GameMode.Host), waits in the menu until the second player has joined and then loads the Game scene for both.
/// Client: StartGame(GameMode.Client). Practice: a host session for one player that starts at once.
/// If the other player leaves, the match ends and this player goes back to the menu.
/// </summary>
public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static GameLauncher Instance { get; private set; }

    [SerializeField] string menuSceneName = "Menu";
    [SerializeField] string gameSceneName = "Game";
    // Without a fixed region each process pings and picks its own "best" one; a room in another region is "not found".
    [SerializeField] string photonRegion = "eu";

    public enum State { Idle, Connecting, Waiting, Starting, Playing }

    /// <summary>What the menu shows: nothing running, connecting, host waits for player 2, map loads, match runs.</summary>
    public State Phase { get; private set; }
    public string SessionName { get; private set; } = "";
    public bool Hosting { get; private set; }
    public bool Solo { get; private set; }   // practice without a second player

    NetworkRunner runner;
    bool matchRunning;        // true once two players are in the match
    bool returningToMenu;     // avoids handling OnShutdown twice

    /// <summary>Shown by MenuUI after we came back from a match or a failed connection.</summary>
    public static string LastMessage = "";

    public NetworkRunner Runner => runner;
    public bool IsHost => runner != null && runner.IsServer;

    /// <summary>1 or 2 for the local player, 0 if not in a match. Same answer on host and client.</summary>
    public int LocalSlot => runner == null ? 0 : SlotOf(runner.LocalPlayer);

    /// <summary>The slot the host stored in that player's NetworkPlayerState (0 until it is spawned).</summary>
    public int SlotOf(PlayerRef player)
    {
        if (runner == null) return 0;
        foreach (var s in NetworkPlayerState.All) if (s.Object != null && s.Object.InputAuthority == player) return s.Slot;
        return 0;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void StartHost(string sessionName, bool solo = false) { await StartSession(GameMode.Host, sessionName, solo); }
    public async void StartClient(string sessionName) { await StartSession(GameMode.Client, sessionName, false); }

    /// <summary>Leaves the waiting room or stops connecting; the menu stays.</summary>
    public async void Cancel()
    {
        returningToMenu = true;
        await CleanUp();
        returningToMenu = false;
    }

    void Update()
    {
        // The host starts the match when both players are here (checked here, not in OnPlayerJoined: the second player can
        // join before StartGame has returned).
        if (Phase != State.Waiting || runner == null || !runner.IsRunning || !runner.IsServer) return;
        if (runner.ActivePlayers.Count() < PlayerSlots.MaxPlayers) return;
        matchRunning = true;
        Phase = State.Starting;
        runner.LoadScene(SceneRef.FromIndex(BuildIndexOf(gameSceneName)), LoadSceneMode.Single);
    }

    // Not called "Start": Unity reserves Start() for its own parameterless startup message.
    async System.Threading.Tasks.Task StartSession(GameMode mode, string sessionName, bool solo)
    {
        if (runner != null) return; // already connecting or running
        sessionName = string.IsNullOrWhiteSpace(sessionName) ? "room1" : sessionName.Trim();
        if (solo) sessionName = "practice-" + Guid.NewGuid().ToString("N").Substring(0, 8); // nobody can join it by accident
        returningToMenu = false; matchRunning = false; LastMessage = "";
        Phase = State.Connecting; SessionName = sessionName; Hosting = mode == GameMode.Host; Solo = solo;

        // On its own object: Fusion destroys the runner's GameObject when the session ends (e.g. the host left), and this
        // launcher must survive that to load the menu. The runner makes its object DontDestroyOnLoad itself.
        var runnerObject = new GameObject("NetworkRunner");
        runner = runnerObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);
        var sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();

        var args = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = solo ? 1 : PlayerSlots.MaxPlayers,
            SceneManager = sceneManager,
        };
        // in code, not in PhotonAppSettings.asset: that file holds the App ID and is not in git
        if (!string.IsNullOrEmpty(photonRegion) && Fusion.Photon.Realtime.PhotonAppSettings.TryGetGlobal(out var photon))
        {
            var app = photon.AppSettings.GetCopy();
            app.FixedRegion = photonRegion;
            args.CustomPhotonAppSettings = app;
        }
        // Only the host picks the scene; clients load whatever the host loads. A match for two starts in the menu (waiting room).
        if (solo) args.Scene = SceneRef.FromIndex(BuildIndexOf(gameSceneName));

        var started = runner;
        StartGameResult result = await runner.StartGame(args);
        if (runner != started) return; // cancelled while connecting
        if (!result.Ok)
        {
            LastMessage = Explain(result);
            Debug.LogWarning(LastMessage);
            await CleanUp();
            if (MenuUI.Instance != null) MenuUI.Instance.ShowMessage(LastMessage);
        }
        else if (solo) Phase = State.Playing;
        else if (mode == GameMode.Host)
        {
            Phase = State.Waiting;
            Debug.Log("Hosting session '" + sessionName + "'; waiting for the second player.");
        }
        else Phase = State.Starting; // the host loads the map for both
    }

    static string Explain(StartGameResult result)
    {
        switch (result.ShutdownReason)
        {
            case ShutdownReason.GameNotFound: return "There is no session with this name yet. The other player must host it first.";
            case ShutdownReason.GameIsFull: return "This session already has two players.";
            case ShutdownReason.ServerInRoom:
            case ShutdownReason.GameIdAlreadyExists: return "A session with this name already exists. Join it or choose another name.";
            case ShutdownReason.PhotonCloudTimeout:
            case ShutdownReason.ConnectionTimeout:
            case ShutdownReason.OperationTimeout: return "No connection to the Photon server. Check the internet connection.";
            default: return "Could not connect: " + result.ShutdownReason + " " + result.ErrorMessage;
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
        matchRunning = false; Phase = State.Idle;
        var r = runner; runner = null;
        if (r == null) return; // Fusion already shut it down and destroyed it
        r.RemoveCallbacks(this);
        await r.Shutdown(); // also destroys the runner's own GameObject
    }

    // ---- INetworkRunnerCallbacks ----

    public void OnPlayerJoined(NetworkRunner r, PlayerRef player) { } // see Update

    public void OnPlayerLeft(NetworkRunner r, PlayerRef player)
    {
        // Host side: the opponent left, so the match is over.
        if (r.IsServer && matchRunning) ReturnToMenu("Your opponent left. The match is over.");
    }

    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason)
    {
        // Client side: the host is gone.
        ReturnToMenu("The connection to the host is lost (" + reason + ").");
    }

    public void OnConnectFailed(NetworkRunner r, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        ReturnToMenu("Could not connect to the host (" + reason + ").");
    }

    public void OnShutdown(NetworkRunner r, ShutdownReason reason)
    {
        if (!returningToMenu && runner == r) ReturnToMenu("The session was closed (" + reason + ").");
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
    public void OnSceneLoadDone(NetworkRunner r) { if (Phase == State.Starting) Phase = State.Playing; }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
}
