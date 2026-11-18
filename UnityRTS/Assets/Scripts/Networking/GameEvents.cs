using Fusion;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Recent-event log of the LOCAL player (what the AI snapshot may contain). The host decides which events a player may know
/// (only things about their own objects, plus enemies they can see) and sends each one to that player's CommandRelay.
/// Every peer therefore holds only its own player's log.
/// </summary>
public static class GameEvents
{
    public static readonly PlayerEventLog Local = new PlayerEventLog();

    /// <summary>Host only. Tell player 'owner' about an event; zone is looked up from the world position.</summary>
    static readonly System.Collections.Generic.Dictionary<PlayerRef, float> lastUnderAttack = new System.Collections.Generic.Dictionary<PlayerRef, float>();

    public static void Report(PlayerRef owner, string type, Vector3 where, string detail)
    {
        if (type == "UNDER_ATTACK")   // every hit would otherwise send an RPC: at most one per owner every 5 s
        {
            float last;
            if (lastUnderAttack.TryGetValue(owner, out last) && Time.time - last < 5f) return;
            lastUnderAttack[owner] = Time.time;
        }
        var state = GameSpawner.Instance != null ? GameSpawner.Instance.StateOf(owner) : null;
        if (state == null) return;
        string zone = ZoneMap.Instance != null ? ZoneMap.Instance.ZoneAt(where) : null;
        var relay = state.GetComponent<CommandRelay>();
        if (relay.Object.HasInputAuthority) Local.Add(type, zone, detail, Time.time); // the host's own player
        else relay.SendEventToOwner(type, zone ?? "", detail ?? "");
    }
}
