using TMPro;
using UnityEngine;

/// <summary>
/// Top bar: wood, food, gold, population (used / capacity) and age of the local player.
/// Reads the [Networked] values of the local NetworkPlayerState; every client sees only its own numbers.
/// </summary>
public class HudUI : MonoBehaviour
{
    [SerializeField] TMP_Text wood, food, gold, population, age;

    NetworkPlayerState state;

    void Start()
    {
        // five equal cells in the top bar: long texts (age + "waiting for P2") shrink instead of overflowing
        foreach (var t in new[] { wood, food, gold, population, age })
            if (t != null) { t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = t.fontSize; }
    }

    void Update()
    {
        if (state == null)
        {
            if (CommandRelay.Local == null) return;
            state = CommandRelay.Local.GetComponent<NetworkPlayerState>();
            if (state == null) return;
        }
        if (wood) wood.text = "Wood " + state.Wood;
        if (food) food.text = "Food " + state.Food;
        if (gold) gold.text = "Gold " + state.Gold;
        if (population) population.text = "Pop " + state.PopUsed + "/" + state.PopCapacity;
        // "P1 | Age I": which player this window is, so the two screens are easy to tell apart in the demo video
        if (age) age.text = (state.Slot > 0 ? "P" + state.Slot + "  |  " : "") + "Age " + (state.Age == 2 ? "II" : "I") + (WaitingForOpponent() ? "  |  waiting for P2" : "");
    }

    /// <summary>The host is alone in the Game scene until the second player joins; say so, or it looks like nothing started.</summary>
    static bool WaitingForOpponent()
    {
        var launcher = GameLauncher.Instance;
        if (launcher == null || launcher.Runner == null) return false;
        return System.Linq.Enumerable.Count(launcher.Runner.ActivePlayers) < RTS.Core.Rules.PlayerSlots.MaxPlayers;
    }
}
