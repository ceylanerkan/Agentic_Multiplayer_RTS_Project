using System.Collections.Generic;
using RTS.Core.Rules;

/// <summary>Adapter from a player's networked tech mask to the pure TechEffects rules.</summary>
public static class TechBonus
{
    public static ICollection<string> TechsOf(NetworkPlayerState owner)
    {
        var set = new HashSet<string>();
        foreach (var k in GameDataProvider.Get().Techs.Keys) if (owner.HasTech(k)) set.Add(k);
        return set;
    }

    public static float AttackBonus(DefRecord unit, NetworkPlayerState owner) { return TechEffects.AttackBonus(unit.Key, TechsOf(owner)); }
    public static float RangeBonus(DefRecord unit, NetworkPlayerState owner) { return TechEffects.RangeBonus(unit.Key, TechsOf(owner)); }
}
