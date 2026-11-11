using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RTS.Core.Rules
{
    /// <summary>One unit, building or technology from ai/rag/knowledge/*.json (the single source of numbers).</summary>
    public class DefRecord
    {
        public string Key;
        public string Kind; // "unit" | "building" | "technology"
        public string Domain = "land"; // units only: "land" | "water"
        public Dictionary<string, int> Cost = new Dictionary<string, int>();
        public int Population;
        public float Hp, Attack, Armor, Range;
        public List<string> Requires = new List<string>();
        public List<string> Strengths = new List<string>();
        public bool Buildable = true; // false for the Town Center
    }

    /// <summary>Loads the knowledge JSON text, so Unity and the AI always use the same numbers.</summary>
    public class GameData
    {
        public readonly Dictionary<string, DefRecord> Units = new Dictionary<string, DefRecord>();
        public readonly Dictionary<string, DefRecord> Buildings = new Dictionary<string, DefRecord>();
        public readonly Dictionary<string, DefRecord> Techs = new Dictionary<string, DefRecord>();

        public static GameData FromJson(string unitsJson, string buildingsJson, string techsJson)
        {
            var d = new GameData();
            foreach (var r in JArray.Parse(unitsJson)) d.Units[(string)r["key"]] = Parse(r, "unit");
            foreach (var r in JArray.Parse(buildingsJson)) d.Buildings[(string)r["key"]] = Parse(r, "building");
            foreach (var r in JArray.Parse(techsJson)) d.Techs[(string)r["key"]] = Parse(r, "technology");
            return d;
        }

        static DefRecord Parse(JToken r, string kind)
        {
            var rec = new DefRecord { Key = (string)r["key"], Kind = kind };
            if (r["domain"] != null) rec.Domain = (string)r["domain"];
            if (r["cost"] != null && r["cost"].Type == JTokenType.Object)
                foreach (var p in ((JObject)r["cost"]).Properties()) rec.Cost[p.Name] = (int)p.Value;
            if (r["population"] != null) rec.Population = (int)r["population"];
            var stats = r["stats"];
            if (stats != null)
            {
                rec.Attack = (float)stats["attack"]; rec.Armor = (float)stats["armor"];
                rec.Range = (float)stats["range"]; rec.Hp = (float)stats["hp"];
            }
            if (r["requires"] != null) foreach (var x in r["requires"]) rec.Requires.Add((string)x);
            if (r["strengths"] != null) foreach (var x in r["strengths"]) rec.Strengths.Add((string)x);
            if (r["buildable"] != null) rec.Buildable = (bool)r["buildable"];
            return rec;
        }
    }
}
