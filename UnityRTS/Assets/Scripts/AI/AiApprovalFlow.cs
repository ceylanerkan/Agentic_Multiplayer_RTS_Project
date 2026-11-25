using System.Collections.Generic;
using System.IO;
using RTS.Core.Rules;
using UnityEngine;

/// <summary>
/// Human approval. Nothing the AI proposes changes the game until the player presses Approve.
///  - Approve: every action is validated again on the current state (GameActionValidator); the valid ones are sent through
///    CommandRelay (the same RPCs as manual play, so the host validates once more); invalid ones are skipped and reported.
///  - Reject: nothing is sent, only the decision is logged.
/// Each decision is appended to the decision log (plan id, decision, validator result per action).
/// </summary>
public static class AiApprovalFlow
{
    static readonly DecisionLog Log = new DecisionLog();
    static string LogPath { get { return Path.Combine(Application.persistentDataPath, "decision-log.jsonl"); } }

    public static string Approve(AiAnswer answer)
    {
        var relay = CommandRelay.Local;
        if (relay == null || answer == null || answer.Plan == null) return "No plan to approve.";
        var data = GameDataProvider.Get();
        var validator = new GameActionValidator(data);
        var actions = new List<PlanAction>();
        foreach (var d in answer.Plan.Actions) actions.Add(d.ToPlanAction());

        var results = ApprovalEvaluator.Evaluate(actions, relay.GetComponent<NetworkPlayerState>().ToCore(), validator);
        var entry = new DecisionEntry { PlanId = Log.NextPlanId(), Decision = "approved", GameTime = Time.time };
        int done = 0, skipped = 0; string firstReason = null; note = null; builders.Clear();
        for (int i = 0; i < actions.Count; i++)
        {
            entry.Actions.Add(ActionCosts.Describe(actions[i], data));
            entry.Validator.Add(results[i].Ok ? "OK" : results[i].Reason);
            blocked = null;
            if (results[i].Ok && Execute(actions[i], relay)) done++;
            else { skipped++; if (firstReason == null) firstReason = results[i].Ok ? blocked ?? "no suitable building or unit" : results[i].Reason; }
        }
        Save(entry);
        string result = skipped == 0 ? "Approved " + entry.PlanId + ": " + done + " action(s) sent."
                                     : "Approved " + entry.PlanId + ": " + done + " sent, " + skipped + " skipped (" + firstReason + ").";
        return note == null ? result : result + " " + note;
    }

    public static string Reject(AiAnswer answer)
    {
        if (answer == null || answer.Plan == null) return "No plan.";
        var data = GameDataProvider.Get();
        var entry = new DecisionEntry { PlanId = Log.NextPlanId(), Decision = "rejected", GameTime = Time.time };
        foreach (var d in answer.Plan.Actions) { entry.Actions.Add(ActionCosts.Describe(d.ToPlanAction(), data)); entry.Validator.Add("not checked"); }
        Save(entry);
        return "Rejected " + entry.PlanId + ". Nothing was changed.";
    }

    static void Save(DecisionEntry e)
    {
        Log.Add(e);
        try { File.AppendAllText(LogPath, Log.ToJsonLine(e) + "\n"); }
        catch (System.Exception ex) { Debug.LogWarning("Decision log not written: " + ex.Message); }
    }

    static string note;      // extra remark for the status line (set by Execute)
    static string blocked;   // why the host would refuse an action that passed the validator (set by Execute)
    static readonly HashSet<NetworkUnit> builders = new HashSet<NetworkUnit>(); // Villagers this plan already sent to a building site

    // ---- executing one validated action ----
    static bool Execute(PlanAction a, CommandRelay relay)
    {
        var data = GameDataProvider.Get();
        var me = relay.Object.InputAuthority;
        switch (a.ActionType)
        {
            case "TRAIN_UNIT":
            {
                // A queue holds ProductionQueue.MaxLength items: spread the units over every suitable building, and say so
                // when they do not all fit (the host would silently refuse the rest).
                int left = a.Count, queued = 0;
                foreach (var b in NetworkBuilding.All)
                {
                    if (left == 0) break;
                    if (b.Owner != me || !b.Completed || !data.Buildings[b.TypeKey.ToString()].Trains.Contains(a.UnitType)) continue;
                    string why = b.WhyNotQueue(a.UnitType, false); // e.g. the Town Center is researching Age II
                    if (why != null) { blocked = why; continue; }
                    int space = System.Math.Max(0, System.Math.Min(left, ProductionQueue.MaxLength - b.QueueCount));
                    for (int i = 0; i < space; i++) relay.SendTrain(a.UnitType, b.Object.Id);
                    left -= space; queued += space;
                }
                if (queued == 0) return false;
                if (left > 0) note = "Queued " + queued + " of " + a.Count + " " + a.UnitType + " (queues full).";
                return true;
            }
            case "RESEARCH":
            {
                if (NetworkBuilding.BeingResearched(a.TechType, me)) { blocked = "already being researched"; return false; }
                var b = BestBuilding(me, x => data.Buildings[x.TypeKey.ToString()].Researches.Contains(a.TechType)
                                              && (blocked = x.WhyNotQueue(a.TechType, true)) == null);
                if (b == null) return false;
                relay.SendResearch(a.TechType, b.Object.Id);
                return true;
            }
            case "BUILD":
            {
                var builder = PickVillager(me);
                Vector3 centre;
                if (builder == null || ZoneMap.Instance == null || !ZoneMap.Instance.TryGetCentre(a.TargetZone, out centre)) return false;
                // the plan only names a zone: a place near its centre with room to walk around it, or else any free place
                int slot = relay.GetComponent<NetworkPlayerState>().Slot;
                Vector3 place;
                if (!BuildPlaces.TryFind(a.BuildingType, a.TargetZone, centre, slot, 45f, 4f, me, out place)
                    && !BuildPlaces.TryFind(a.BuildingType, a.TargetZone, centre, slot, 45f, BuildSite.Gap, me, out place))
                    { blocked = "no free place in " + a.TargetZone; return false; }
                builders.Add(builder);
                relay.SendBuild(a.BuildingType, place, a.TargetZone, builder.Object.Id); // the host checks the place again
                return true;
            }
            case "MOVE_GROUP":
            {
                Vector3 centre;
                if (ZoneMap.Instance == null || !ZoneMap.Instance.TryGetCentre(a.TargetZone, out centre)) return false;
                var group = new List<NetworkUnit>();
                foreach (var u in NetworkUnit.All) if (u.Owner == me && InGroup(u, a.UnitGroup)) group.Add(u);
                if (group.Count == 0) return false;
                var offsets = FormationLayout.Offsets(group.Count, 1.8f);
                for (int i = 0; i < group.Count; i++) relay.SendMove(group[i].Object.Id, centre + new Vector3(offsets[i][0], 0, offsets[i][1]));
                return true;
            }
        }
        return false;
    }

    // The finished own building that fits and has the shortest queue.
    static NetworkBuilding BestBuilding(Fusion.PlayerRef me, System.Func<NetworkBuilding, bool> fits)
    {
        NetworkBuilding best = null;
        foreach (var b in NetworkBuilding.All)
            if (b.Owner == me && b.Completed && fits(b) && (best == null || b.QueueCount < best.QueueCount)) best = b;
        return best;
    }

    static NetworkUnit PickVillager(Fusion.PlayerRef me)
    {
        NetworkUnit any = null, busy = null;
        foreach (var u in NetworkUnit.All)
        {
            var g = u.GetComponent<VillagerGatherer>();
            if (u.Owner != me || g == null || !u.CanBuild) continue;
            if (any == null) any = u;
            if (builders.Contains(u)) continue; // a second BUILD of the same plan would pull it off the first site
            if (!g.Busy) return u;              // prefer an idle Villager
            if (busy == null) busy = u;
        }
        return busy != null ? busy : any;
    }

    static bool InGroup(NetworkUnit u, string group)
    {
        string t = u.TypeKey.ToString();
        bool ship = GameDataProvider.Get().Units[t].Domain == "water";
        switch (group)
        {
            case "all_military": return t != "villager" && !ship;
            case "all_ships": return ship && t != "fishing_boat";
            case "idle_villagers": { var g = u.GetComponent<VillagerGatherer>(); return t == "villager" && g != null && !g.Busy && !u.HasMoveTarget; }
            default: return t == group;
        }
    }
}
