using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// In the Game scene. When a player joins, the host spawns that player's NetworkPlayerState/CommandRelay object,
/// a Town Center and 3 Villagers. Runs on every peer (SimulationBehaviour) but only the host spawns.
/// </summary>
public class GameSpawner : SimulationBehaviour, IPlayerJoined
{
    public static GameSpawner Instance { get; private set; }

    [Serializable] public class Entry { public string key; public NetworkObject prefab; }

    [SerializeField] NetworkObject playerPrefab;      // NetworkObject + NetworkPlayerState + CommandRelay
    [SerializeField] Entry[] unitPrefabs;             // key "villager", ...
    [SerializeField] Entry[] buildingPrefabs;         // key "town_center", ...
    [SerializeField] Transform p1Spawn, p2Spawn;      // base centres

    readonly Dictionary<PlayerRef, NetworkPlayerState> players = new Dictionary<PlayerRef, NetworkPlayerState>();

    void Awake() { Instance = this; GameEvents.Local.Clear(); } // new match, new event log

    // Works on host and clients: NetworkPlayerState registers itself when it spawns (input authority = the player).
    public NetworkPlayerState StateOf(PlayerRef p)
    {
        NetworkPlayerState known;
        if (players.TryGetValue(p, out known) && known != null) return known; // host, right after spawning (Spawned may not have run yet)
        foreach (var s in NetworkPlayerState.All) if (s.Object.InputAuthority == p) return s;
        return null;
    }

    public NetworkPlayerState StateBySlot(int slot)
    {
        foreach (var s in NetworkPlayerState.All) if (s.Slot == slot) return s;
        return null;
    }

    // The Game scene loads after the players joined, so a PlayerJoined callback may have been missed: the host checks every frame
    // that each active player has a base, and spawns it for those that do not (PlayerJoined ignores players that already have one).
    void Update()
    {
        if (Runner == null || !Runner.IsRunning || !Runner.IsServer) return;
        foreach (var p in Runner.ActivePlayers) if (!players.ContainsKey(p)) PlayerJoined(p);
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer || players.ContainsKey(player)) return; // already has a base (also called from Update)
        MatchEnd.Reset();
        int slot = GameLauncher.Instance != null ? GameLauncher.Instance.SlotOf(player) : players.Count + 1;
        if (slot == 0) { players[player] = null; return; } // third player: ignored (remembered, so Update does not retry)
        Transform baseCentre = slot == 1 ? p1Spawn : p2Spawn;

        var po = Runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        var state = po.GetComponent<NetworkPlayerState>();
        state.InitHost(slot);
        players[player] = state;

        SpawnBuilding(player, "town_center", baseCentre.position, true);
        for (int i = 0; i < RTS.Core.Rules.GameConstants.StartVillagers; i++)
            SpawnUnit(player, "villager", baseCentre.position + new Vector3(3 + i, 0, -3));
        state.PopUsed = RTS.Core.Rules.GameConstants.StartVillagers;
        state.RecalculateCapacity();
    }

    public NetworkUnit SpawnUnit(PlayerRef owner, string key, Vector3 pos)
    {
        var obj = Runner.Spawn(Find(unitPrefabs, key), pos, Quaternion.identity, owner);
        StandOnGround(obj);
        var u = obj.GetComponent<NetworkUnit>();
        u.InitHost(owner, key);
        return u;
    }

    public NetworkBuilding SpawnBuilding(PlayerRef owner, string key, Vector3 pos, bool completed)
    {
        var obj = Runner.Spawn(Find(buildingPrefabs, key), pos, Quaternion.identity, owner);
        StandOnGround(obj);
        var b = obj.GetComponent<NetworkBuilding>();
        b.InitHost(owner, key, completed);
        if (completed && players.TryGetValue(owner, out var st)) st.RecalculateCapacity();
        return b;
    }

    /// <summary>
    /// Put a freshly spawned object on the ground: cast a ray down from above its position and lift it by its collider's half height,
    /// so capsules and cubes do not sink into the floor. Ships land on the sea surface because the ray hits the sea object. Host only, before the first tick.
    /// </summary>
    static void StandOnGround(NetworkObject obj)
    {
        var col = obj.GetComponentInChildren<Collider>();
        Vector3 p = obj.transform.position;
        float ground = 0f;
        if (Physics.Raycast(p + Vector3.up * 100f, Vector3.down, out var hit, 300f)) ground = hit.point.y;
        obj.transform.position = new Vector3(p.x, ground + (col != null ? col.bounds.extents.y : 0f), p.z);
    }

    static NetworkObject Find(Entry[] list, string key)
    {
        foreach (var e in list) if (e.key == key) return e.prefab;
        throw new InvalidOperationException("No prefab with key '" + key + "'. Fill the GameSpawner lists.");
    }
}
