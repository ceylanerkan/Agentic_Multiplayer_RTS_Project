using System.Collections.Generic;
using Newtonsoft.Json;

namespace RTS.Core.Rules
{
    /// <summary>One line of the decision log: which plan, what the player decided, and what the validator said per action.</summary>
    public class DecisionEntry
    {
        [JsonProperty("plan_id")] public string PlanId;
        [JsonProperty("decision")] public string Decision;       // "approved" | "rejected"
        [JsonProperty("game_time_s")] public float GameTime;
        [JsonProperty("actions")] public List<string> Actions = new List<string>();   // description of each action
        [JsonProperty("validator")] public List<string> Validator = new List<string>(); // "OK" or the reason, per action
    }

    public class DecisionLog
    {
        public readonly List<DecisionEntry> Entries = new List<DecisionEntry>();
        int counter;

        public string NextPlanId() { counter++; return "plan-" + counter.ToString("D3"); }

        public DecisionEntry Add(DecisionEntry e) { Entries.Add(e); return e; }

        public string ToJsonLine(DecisionEntry e) { return JsonConvert.SerializeObject(e); }
    }

    /// <summary>
    /// Re-checks a plan against the CURRENT state when the player presses Approve (the state may have changed since the AI
    /// looked at it). Resources are reserved action by action, so two actions cannot spend the same gold.
    /// </summary>
    public static class ApprovalEvaluator
    {
        public static List<ValidationResult> Evaluate(IList<PlanAction> actions, PlayerState state, GameActionValidator validator)
        {
            var reserved = new Dictionary<string, int>();
            var results = new List<ValidationResult>();
            foreach (var a in actions) results.Add(validator.Validate(a, state, reserved));
            return results;
        }
    }
}
