using System.Collections.Generic;
using System.IO;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Collects what the LOCAL player may know and passes it to SnapshotAssembler.
/// Own units and buildings come from the networked objects; enemies come only from VisibilityService.FilterFor, so the
/// snapshot cannot contain an enemy the player cannot see. Opponent resources do not exist in the snapshot at all.
/// </summary>
public static class AiSnapshotBuilder
{
    public static string MatchId = "local-match";

    public static AiSafeSnapshot Build()
    {
        var relay = CommandRelay.Local;
        if (relay == null) return null;
        var me = relay.GetComponent<NetworkPlayerState>();
        var data = GameDataProvider.Get();
        float now = Time.time;

        var units = new List<OwnUnitInfo>();
        foreach (var u in NetworkUnit.All)
        {
            if (u.Owner != me.Owner) continue;
            var def = data.Units[u.TypeKey.ToString()];
            var gatherer = u.GetComponent<VillagerGatherer>();
            bool idle = !u.HasMoveTarget && !u.AttackTarget.IsValid && (gatherer == null || !gatherer.Busy);
            units.Add(new OwnUnitInfo { Type = u.TypeKey.ToString(), Zone = ZoneMap.Instance?.ZoneAt(u.transform.position), HpPct = 100f * u.Hp / def.Hp, Idle = idle });
        }

        var buildings = new List<OwnBuildingInfo>();
        var researching = new List<string>();
        foreach (var b in NetworkBuilding.All)
        {
            if (b.Owner != me.Owner) continue;
            buildings.Add(new OwnBuildingInfo { Type = b.TypeKey.ToString(), Completed = b.Completed, Busy = b.QueueCount > 0 });
            if (b.CurrentIsResearch && b.QueueCount > 0) researching.Add(b.CurrentKey.ToString());
        }

        // enemies: only through the single visibility query
        var enemies = new List<EnemySighting>();
        foreach (var o in VisibilityService.FilterFor(me.Slot, VisibilityAdapter.Collect()))
            if (o.Owner != me.Slot)
                enemies.Add(new EnemySighting { Type = o.Type, Zone = ZoneMap.Instance?.ZoneAt(new Vector3(o.X, 0, o.Z)) });

        var techs = new List<string>(TechBonus.TechsOf(me));
        return SnapshotAssembler.Build(MatchId, now, me.Age, me.Wood, me.Food, me.Gold, me.PopUsed, me.PopCapacity,
            units, buildings, enemies, techs, researching, GameEvents.Local.Recent(now));
    }

    /// <summary>Writes the current snapshot as JSON, e.g. for the pytest contract test (ai/exports/ is git-ignored).</summary>
    public static string Export(string path)
    {
        var snap = Build();
        if (snap == null) return null;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(snap, Newtonsoft.Json.Formatting.Indented));
        return path;
    }
}
