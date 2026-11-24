using UnityEditor;
using UnityEngine;

/// <summary>
/// Play-test helper, editor only (not in builds): gives every player 1000 of each resource, so late-game features
/// (Age II, Stable, Catapult, ships) can be tested without half an hour of gathering. Works when the editor is the host.
/// </summary>
public static class TestResourcesMenu
{
    [MenuItem("Tools/RTS/Test: +1000 resources (host, play mode)")]
    static void Give()
    {
        int players = 0;
        foreach (var s in NetworkPlayerState.All)
        {
            if (s.Object == null || !s.Object.IsValid || !s.HasStateAuthority) continue;
            s.Wood += 1000; s.Food += 1000; s.Gold += 1000;
            players++;
        }
        Debug.Log(players > 0 ? "RTS: +1000 wood, food and gold for " + players + " player(s)."
                              : "RTS: no match running with this editor as the host.");
    }
}
