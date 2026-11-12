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

    void Awake() { Instance = this; }

    public NetworkPlayerState StateOf(PlayerRef p) { NetworkPlayerState s; players.TryGetValue(p, out s); return s; }

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer) return;
        int slot = GameLauncher.Instance != null ? GameLauncher.Instance.SlotOf(player) : players.Count + 1;
        if (slot == 0) return; // third player: ignored
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
        var u = obj.GetComponent<NetworkUnit>();
        u.InitHost(owner, key);
        return u;
    }

    public NetworkBuilding SpawnBuilding(PlayerRef owner, string key, Vector3 pos, bool completed)
    {
        var obj = Runner.Spawn(Find(buildingPrefabs, key), pos, Quaternion.identity, owner);
        var b = obj.GetComponent<NetworkBuilding>();
        b.InitHost(owner, key, completed);
        if (completed && players.TryGetValue(owner, out var st)) st.RecalculateCapacity();
        return b;
    }

    static NetworkObject Find(Entry[] list, string key)
    {
        foreach (var e in list) if (e.key == key) return e.prefab;
        throw new InvalidOperationException("No prefab with key '" + key + "'. Fill the GameSpawner lists.");
    }
}
