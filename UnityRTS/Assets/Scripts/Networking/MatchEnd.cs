using RTS.Core.Rules;

/// <summary>
/// Host only. When a Town Center is destroyed and its owner has no other one, the match ends:
/// the winner's NetworkPlayerState.Result becomes 1 and the loser's 2. Both clients read their own Result
/// (a [Networked] value), so both see Victory or Defeat. Units stop fighting because the UI shows a result screen
/// and the host ignores further results.
/// </summary>
public static class MatchEnd
{
    public static bool Finished { get; private set; }

    public static void Reset() { Finished = false; }

    /// <param name="destroyed">The building that just died; it is still in NetworkBuilding.All until despawn runs.</param>
    public static void Check(NetworkBuilding destroyed)
    {
        if (Finished) return;
        int[] tc = new int[3];
        foreach (var b in NetworkBuilding.All)
        {
            if (b == destroyed || !b.Completed || b.TypeKey.ToString() != "town_center") continue;
            var st = GameSpawner.Instance.StateOf(b.Owner);
            if (st != null) tc[st.Slot]++;
        }
        int loser = WinCondition.LosingSlot(tc[1], tc[2]);
        if (loser == 0) return;
        Finished = true;
        foreach (var slot in new[] { 1, 2 })
        {
            var st = GameSpawner.Instance.StateBySlot(slot);
            if (st != null) st.Result = slot == loser ? 2 : 1;
        }
    }
}
