using System.Collections.Generic;
using Newtonsoft.Json;

namespace RTS.Core.Rules
{
    // DTOs mirror ai/schemas/safe_snapshot.schema.json and action_plan.schema.json. There is deliberately no field for
    // hidden data (opponent resources, hidden units): the gateway rejects unknown fields.
    public class SnapResources { [JsonProperty("wood")] public int Wood; [JsonProperty("food")] public int Food; [JsonProperty("gold")] public int Gold; }
    public class SnapPopulation { [JsonProperty("used")] public int Used; [JsonProperty("capacity")] public int Capacity; }
    public class SnapGroup
    {
        [JsonProperty("type")] public string Type;
        [JsonProperty("count")] public int Count;
        [JsonProperty("zone", NullValueHandling = NullValueHandling.Ignore)] public string Zone;
        [JsonProperty("idle", NullValueHandling = NullValueHandling.Ignore)] public int? Idle;
        [JsonProperty("avg_hp_pct", NullValueHandling = NullValueHandling.Ignore)] public float? AvgHpPct;
    }
    public class SnapBuilding
    {
        [JsonProperty("type")] public string Type;
        [JsonProperty("count")] public int Count;
        [JsonProperty("under_construction", NullValueHandling = NullValueHandling.Ignore)] public int? UnderConstruction;
        [JsonProperty("busy", NullValueHandling = NullValueHandling.Ignore)] public int? Busy;
    }
    public class SnapEvent
    {
        [JsonProperty("type")] public string Type;
        [JsonProperty("zone", NullValueHandling = NullValueHandling.Ignore)] public string Zone;
        [JsonProperty("detail", NullValueHandling = NullValueHandling.Ignore)] public string Detail;
        [JsonProperty("seconds_ago")] public float SecondsAgo;
    }

    public class AiSafeSnapshot
    {
        [JsonProperty("match_id")] public string MatchId;
        [JsonProperty("game_time_s")] public float GameTimeS;
        [JsonProperty("age")] public int Age = 1;
        [JsonProperty("resources")] public SnapResources Resources = new SnapResources();
        [JsonProperty("population")] public SnapPopulation Population = new SnapPopulation();
        [JsonProperty("own_army")] public List<SnapGroup> OwnArmy = new List<SnapGroup>();
        [JsonProperty("own_buildings")] public List<SnapBuilding> OwnBuildings = new List<SnapBuilding>();
        [JsonProperty("visible_enemies")] public List<SnapGroup> VisibleEnemies = new List<SnapGroup>(); // from VisibilityService.FilterFor
        [JsonProperty("researched_tech")] public List<string> ResearchedTech = new List<string>();
        [JsonProperty("researching", NullValueHandling = NullValueHandling.Ignore)] public List<string> Researching;
        [JsonProperty("recent_events")] public List<SnapEvent> RecentEvents = new List<SnapEvent>();

        public string ToJson() { return JsonConvert.SerializeObject(this); }
    }

    public class PlanActionDto
    {
        [JsonProperty("action_type")] public string ActionType;
        [JsonProperty("unit_type")] public string UnitType;
        [JsonProperty("building_type")] public string BuildingType;
        [JsonProperty("tech_type")] public string TechType;
        [JsonProperty("unit_group")] public string UnitGroup;
        [JsonProperty("count")] public int? Count;
        [JsonProperty("target_zone")] public string TargetZone;
        [JsonProperty("reason")] public string Reason;

        public PlanAction ToPlanAction()
        {
            return new PlanAction { ActionType = ActionType, UnitType = UnitType, BuildingType = BuildingType, TechType = TechType,
                UnitGroup = UnitGroup, TargetZone = TargetZone, Count = Count ?? 1 };
        }
    }

    public class AiPlan
    {
        [JsonProperty("summary")] public string Summary;
        [JsonProperty("confidence")] public float Confidence;
        [JsonProperty("evidence")] public List<string> Evidence = new List<string>();
        [JsonProperty("actions")] public List<PlanActionDto> Actions = new List<PlanActionDto>();
    }

    /// <summary>Gateway response of /ask and /why.</summary>
    public class AiAnswer
    {
        [JsonProperty("plan")] public AiPlan Plan;
        [JsonProperty("tools_used")] public List<string> ToolsUsed = new List<string>();
        [JsonProperty("needs_approval")] public bool NeedsApproval;

        public static AiAnswer Parse(string json) { return JsonConvert.DeserializeObject<AiAnswer>(json); }
    }
}
