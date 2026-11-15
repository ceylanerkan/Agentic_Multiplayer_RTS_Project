using RTS.Core.Rules;

/// <summary>Attack bonus from researched technologies (Iron Weapons: +2 attack for military units).</summary>
public static class TechBonus
{
    public const float IronWeaponsBonus = 2f;

    public static float AttackBonus(DefRecord unit, NetworkPlayerState owner)
    {
        if (unit.Key == "villager") return 0f; // "military" only
        return owner.HasTech("iron_weapons") ? IronWeaponsBonus : 0f;
    }
}
