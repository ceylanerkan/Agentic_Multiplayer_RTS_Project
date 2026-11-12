using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>
    /// Decides who is Player 1 and Player 2. The rule is deterministic, so host and client agree without any message:
    /// the player with the lowest id is P1 (the host joins first), the next one is P2.
    /// </summary>
    public static class PlayerSlots
    {
        public const int MaxPlayers = 2;

        /// <summary>Returns 1 or 2, or 0 when the id is not an active player or the match is full.</summary>
        public static int SlotOf(IEnumerable<int> activePlayerIds, int playerId)
        {
            var sorted = new List<int>(activePlayerIds);
            sorted.Sort();
            int index = sorted.IndexOf(playerId);
            return index >= 0 && index < MaxPlayers ? index + 1 : 0;
        }
    }
}
