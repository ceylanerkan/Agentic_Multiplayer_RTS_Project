using System.Collections.Generic;
using RTS.Core.Rules;

/// <summary>Adapter from a player's networked tech mask to the pure TechEffects rules.</summary>
public static class TechBonus
{
    // Combat and fog of war ask this for every unit every tick: build each set once per tech mask (at most 2^techs sets)
    // instead of a new HashSet per call. The sets are shared, so callers must not change them.
    static readonly Dictionary<int, HashSet<string>> byMask = new Dictionary<int, HashSet<string>>();

    public static ICollection<string> TechsOf(NetworkPlayerState owner)
    {
        int mask = owner.TechMask;
        if (byMask.TryGetValue(mask, out var cached)) return cached;
        var set = new HashSet<string>();
        foreach (var k in GameDataProvider.Get().Techs.Keys) if (owner.HasTech(k)) set.Add(k);
        byMask[mask] = set;
        return set;
    }

    public static float AttackBonus(DefRecord unit, NetworkPlayerState owner) { return TechEffects.AttackBonus(unit.Key, TechsOf(owner)); }
    public static float RangeBonus(DefRecord unit, NetworkPlayerState owner) { return TechEffects.RangeBonus(unit.Key, TechsOf(owner)); }
}
