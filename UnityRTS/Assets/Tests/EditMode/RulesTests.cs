using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RTS.Core.Rules;

namespace RTS.Tests
{
    public class RulesTests
    {
        GameData data;
        GameActionValidator validator;

        [SetUp]
        public void Load()
        {
            // The same JSON files the AI uses: Unity and the AI cannot disagree on numbers.
            string dir = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../ai/rag/knowledge"));
            data = GameData.FromJson(File.ReadAllText(Path.Combine(dir, "units.json")),
                File.ReadAllText(Path.Combine(dir, "buildings.json")), File.ReadAllText(Path.Combine(dir, "technologies.json")));
            validator = new GameActionValidator(data);
        }

        static PlayerState Fresh()
        {
            var s = new PlayerState { PopulationUsed = 3, PopulationCapacity = 5 };
            s.Resources["wood"] = 200; s.Resources["food"] = 200; s.Resources["gold"] = 100;
            s.CompletedBuildings["town_center"] = 1;
            return s;
        }

        [Test] public void SpearmanVsCavalry_IsCounter()
        {
            // 5 x 1.5 - 2 = 5.5
            Assert.AreEqual(5.5f, DamageCalculator.Damage(data.Units["spearman"], data.Units["cavalry"], false), 0.001f);
        }
        [Test] public void CavalryVsSpearman_IsPenalised()
        {
            // 8 x 0.75 - 1 = 5
            Assert.AreEqual(5f, DamageCalculator.Damage(data.Units["cavalry"], data.Units["spearman"], false), 0.001f);
        }
        [Test] public void Damage_IsAtLeastOne()
        {
            Assert.AreEqual(1f, DamageCalculator.Damage(data.Units["villager"], data.Units["cavalry"], false), 0.001f);
        }
        [Test] public void IronWeapons_AddsFlatAttack()
        {
            Assert.AreEqual(7f, DamageCalculator.Damage(data.Units["archer"], data.Units["villager"], false, 2f), 0.001f); // (5+2) x 1 - 0
        }
        [Test] public void Catapult_TriplesVsBuildings()
        {
            Assert.AreEqual(36f - data.Buildings["house"].Armor, DamageCalculator.Damage(data.Units["catapult"], data.Buildings["house"], true), 0.001f);
        }

        [Test] public void Train_NeedsBarracks()
        {
            var r = validator.Validate(new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "spearman", Count = 1 }, Fresh());
            Assert.IsFalse(r.Ok);
        }
        [Test] public void Train_PopulationCap()
        {
            var s = Fresh(); s.CompletedBuildings["barracks"] = 1;
            Assert.IsTrue(validator.Validate(new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "spearman", Count = 2 }, s).Ok);
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "spearman", Count = 3 }, s).Ok);
        }
        [Test] public void Plan_CannotSpendSameGoldTwice()
        {
            var s = Fresh(); s.CompletedBuildings["blacksmith"] = 1; s.Resources["gold"] = 60; // Iron Weapons costs 50 gold
            var reserved = new Dictionary<string, int>();
            var a = new PlanAction { ActionType = "RESEARCH", TechType = "iron_weapons" };
            Assert.IsTrue(validator.Validate(a, s, reserved).Ok);
            Assert.IsFalse(validator.Validate(a, s, reserved).Ok); // the first action already reserved 50 of the 60 gold
        }
        [Test] public void Dock_NeedsCoastalSlot()
        {
            var a = new PlanAction { ActionType = "BUILD", BuildingType = "dock", TargetZone = "P1Base" };
            Assert.IsFalse(validator.Validate(a, Fresh()).Ok);
            var s = Fresh(); s.CoastalSlotZones.Add("P1Base");
            Assert.IsTrue(validator.Validate(a, s).Ok);
        }
        [Test] public void Move_DomainRules()
        {
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "MOVE_GROUP", UnitGroup = "all_ships", TargetZone = "Center" }, Fresh()).Ok);
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "MOVE_GROUP", UnitGroup = "all_military", TargetZone = "OpenSea" }, Fresh()).Ok);
            Assert.IsTrue(validator.Validate(new PlanAction { ActionType = "MOVE_GROUP", UnitGroup = "all_military", TargetZone = "NorthGate" }, Fresh()).Ok);
        }
    }
}
