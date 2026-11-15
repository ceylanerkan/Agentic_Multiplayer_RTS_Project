using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>One queued job: a unit to train or a technology to research.</summary>
    public class ProductionItem
    {
        public string Key;
        public bool IsResearch;
        public float Duration;
        public float Elapsed;
    }

    /// <summary>
    /// The queue of one building. Jobs run one after another. Pure logic: the host calls Tick(dt) and gets back
    /// the finished item (or null), then spawns the unit or grants the technology.
    /// Rule: the Town Center cannot train while it researches Age II (rule in nightly plan, night 5).
    /// </summary>
    public class ProductionQueue
    {
        public const int MaxLength = 5;
        public const string BlockingResearch = "age_2";

        readonly List<ProductionItem> items = new List<ProductionItem>();

        public int Count { get { return items.Count; } }
        public ProductionItem Current { get { return items.Count > 0 ? items[0] : null; } }
        public float CurrentProgress { get { return Current == null || Current.Duration <= 0 ? 0f : Current.Elapsed / Current.Duration; } }

        public bool IsResearching(string techKey)
        {
            foreach (var i in items) if (i.IsResearch && i.Key == techKey) return true;
            return false;
        }

        /// <summary>Reason the item cannot be queued, or null when it can.</summary>
        public string WhyNot(string key, bool isResearch)
        {
            if (items.Count >= MaxLength) return "queue is full";
            if (!isResearch && IsResearching(BlockingResearch)) return "cannot train while researching Age II";
            if (isResearch && key == BlockingResearch && HasUnits()) return "finish training before researching Age II";
            if (isResearch && IsResearching(key)) return "already being researched";
            return null;
        }

        bool HasUnits() { foreach (var i in items) if (!i.IsResearch) return true; return false; }

        public bool Enqueue(string key, bool isResearch, float duration)
        {
            if (WhyNot(key, isResearch) != null) return false;
            items.Add(new ProductionItem { Key = key, IsResearch = isResearch, Duration = duration });
            return true;
        }

        /// <summary>Advance the first item. Returns it when it finishes, otherwise null.</summary>
        public ProductionItem Tick(float dt)
        {
            var cur = Current;
            if (cur == null) return null;
            cur.Elapsed += dt;
            if (cur.Elapsed < cur.Duration) return null;
            items.RemoveAt(0);
            return cur;
        }
    }

    /// <summary>Construction progress: Villagers add work, the building is done when work reaches the build time.</summary>
    public class ConstructionState
    {
        public readonly float BuildTime;
        public float Work;

        public ConstructionState(float buildTime, float work = 0f) { BuildTime = buildTime; Work = work; }

        public float Progress { get { return BuildTime <= 0 ? 1f : (Work >= BuildTime ? 1f : Work / BuildTime); } }
        public bool Done { get { return Work >= BuildTime; } }

        /// <summary>One Villager working for dt seconds adds dt seconds of work; two Villagers build twice as fast.</summary>
        public void AddWork(float seconds) { if (seconds > 0) Work += seconds; }
    }
}
