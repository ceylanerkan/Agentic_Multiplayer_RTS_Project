using System.Collections.Generic;
using Fusion;
using RTS.Core.Rules;

/// <summary>
/// One per player, spawned by the host (GameSpawner). Only the host writes the [Networked] values;
/// clients just read them for the HUD. Also converts itself to the engine-free PlayerState used by the validator.
/// </summary>
public class NetworkPlayerState : NetworkBehaviour
{
    [Networked] public int Slot { get; set; }            // 1 or 2
    [Networked] public int Wood { get; set; }
    [Networked] public int Food { get; set; }
    [Networked] public int Gold { get; set; }
    [Networked] public int PopUsed { get; set; }
    [Networked] public int PopCapacity { get; set; }
    [Networked] public int Age { get; set; }
    [Networked] public int TechMask { get; set; }        // bit i = i-th technology of GameData.Techs

    public PlayerRef Owner => Object.InputAuthority;

    /// <summary>Host only: give the starting state from the rules.</summary>
    public void InitHost(int slot)
    {
        Slot = slot; Age = 1;
        Wood = GameConstants.StartWood; Food = GameConstants.StartFood; Gold = GameConstants.StartGold;
        PopUsed = 0; PopCapacity = GameConstants.TownCenterCapacity; TechMask = 0;
    }

    public int Get(string resource)
    {
        switch (resource) { case "wood": return Wood; case "food": return Food; case "gold": return Gold; default: return 0; }
    }

    public void Add(string resource, int amount)
    {
        switch (resource) { case "wood": Wood += amount; break; case "food": Food += amount; break; case "gold": Gold += amount; break; }
    }

    /// <summary>Host only: pay a cost that the validator already approved.</summary>
    public void Spend(Dictionary<string, int> cost, int count)
    {
        foreach (var kv in cost) Add(kv.Key, -kv.Value * count);
    }

    public bool HasTech(string key) { return (TechMask & (1 << TechIndex(key))) != 0; }
    public void GrantTech(string key) { TechMask |= 1 << TechIndex(key); }

    static int TechIndex(string key)
    {
        int i = 0;
        foreach (var k in GameDataProvider.Get().Techs.Keys) { if (k == key) return i; i++; }
        return 0;
    }

    /// <summary>Snapshot for GameActionValidator. Completed buildings come from the NetworkBuilding registry.</summary>
    public PlayerState ToCore()
    {
        var s = new PlayerState { PopulationUsed = PopUsed, PopulationCapacity = PopCapacity, Age = Age };
        s.Resources["wood"] = Wood; s.Resources["food"] = Food; s.Resources["gold"] = Gold;
        foreach (var k in GameDataProvider.Get().Techs.Keys) if (HasTech(k)) s.Techs.Add(k);
        foreach (var b in NetworkBuilding.All)
        {
            if (b.Owner != Owner || !b.Completed) continue;
            string key = b.TypeKey.ToString();
            int n; s.CompletedBuildings.TryGetValue(key, out n); s.CompletedBuildings[key] = n + 1;
        }
        return s;
    }

    /// <summary>Host only: recompute capacity from completed Town Centers and Houses.</summary>
    public void RecalculateCapacity()
    {
        int tc = 0, houses = 0;
        foreach (var b in NetworkBuilding.All)
        {
            if (b.Owner != Owner || !b.Completed) continue;
            if (b.TypeKey.ToString() == "town_center") tc++; else if (b.TypeKey.ToString() == "house") houses++;
        }
        PopCapacity = GameConstants.Capacity(tc, houses);
    }
}
