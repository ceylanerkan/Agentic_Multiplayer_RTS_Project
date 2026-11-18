using System.Collections.Generic;
using RTS.Core.Rules;

/// <summary>Builds the engine-free MapObject list from the networked units and buildings, for VisibilityService.</summary>
public static class VisibilityAdapter
{
    public static List<MapObject> Collect()
    {
        var data = GameDataProvider.Get();
        var list = new List<MapObject>();
        foreach (var u in NetworkUnit.All)
        {
            var st = GameSpawner.Instance.StateOf(u.Owner);
            if (st == null) continue;
            string key = u.TypeKey.ToString();
            float los = data.Units[key].LineOfSight + TechEffects.LosBonus(key, TechBonus.TechsOf(st)); // Fletching: Archer +1
            list.Add(new MapObject { Id = (int)u.Object.Id.Raw, Owner = st.Slot, Type = key, IsBuilding = false, X = u.transform.position.x, Z = u.transform.position.z, Los = los });
        }
        foreach (var b in NetworkBuilding.All)
        {
            var st = GameSpawner.Instance.StateOf(b.Owner);
            if (st == null || !b.Completed) continue; // an unfinished building does not see anything
            string key = b.TypeKey.ToString();
            list.Add(new MapObject { Id = (int)b.Object.Id.Raw, Owner = st.Slot, Type = key, IsBuilding = true, X = b.transform.position.x, Z = b.transform.position.z, Los = data.Buildings[key].LineOfSight });
        }
        return list;
    }
}
