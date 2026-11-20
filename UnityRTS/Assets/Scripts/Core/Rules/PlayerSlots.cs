using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>
    /// Decides who is Player 1 and Player 2. The rule is deterministic, so host and client agree without any message:
    /// by id order. Not used for spawning any more (see FirstFree): Fusion gives the host the last id.
    /// </summary>
    public static class PlayerSlots
    {
        public const int MaxPlayers = 2;

        /// <summary>
        /// The slot the host gives a newly joined player: the lowest of 1..MaxPlayers not taken yet, 0 when full.
        /// Used instead of SlotOf when spawning, because Fusion gives the host player the LAST id: a host that starts alone
        /// would be P1 by id order, and a client joining later with a lower id would also compute P1.
        /// </summary>
        public static int FirstFree(IEnumerable<int> takenSlots)
        {
            var taken = new HashSet<int>(takenSlots);
            for (int s = 1; s <= MaxPlayers; s++) if (!taken.Contains(s)) return s;
            return 0;
        }

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
