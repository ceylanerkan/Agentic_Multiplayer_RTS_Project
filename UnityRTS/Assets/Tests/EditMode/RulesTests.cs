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
    
        [Test] public void PlayerSlots_LowestIdIsP1()
        {
            Assert.AreEqual(1, PlayerSlots.SlotOf(new[] { 3, 2 }, 2));
            Assert.AreEqual(2, PlayerSlots.SlotOf(new[] { 3, 2 }, 3));
        }
        [Test] public void PlayerSlots_ThirdPlayerAndUnknownGetZero()
        {
            Assert.AreEqual(0, PlayerSlots.SlotOf(new[] { 1, 2, 3 }, 3));
            Assert.AreEqual(0, PlayerSlots.SlotOf(new[] { 1, 2 }, 9));
        }

        [Test] public void Capacity_TownCenterPlusHouses_CappedAt50()
        {
            Assert.AreEqual(5, GameConstants.Capacity(1, 0));
            Assert.AreEqual(15, GameConstants.Capacity(1, 2));
            Assert.AreEqual(50, GameConstants.Capacity(1, 20));
        }

        // ---- Night 3 ----
        static ZoneLookup TenZones()
        {
            var z = new ZoneLookup();
            z.Add("P1Base", -40, 0); z.Add("P2Base", 40, 0); z.Add("Center", 0, 0);
            z.Add("NorthGate", 0, 20); z.Add("SouthField", 0, -25); z.Add("EastForest", 30, -25); z.Add("WestMine", -30, -25);
            z.Add("P1Harbor", -40, 50); z.Add("OpenSea", 0, 50); z.Add("P2Harbor", 40, 50);
            return z;
        }

        [Test]
        public void ZoneLookup_EveryPointHasAZone()
        {
            var z = TenZones();
            Assert.IsTrue(z.IsComplete());
            for (float x = -100; x <= 100; x += 7) for (float y = -100; y <= 100; y += 7)
                Assert.IsNotNull(z.ZoneAt(x, y));
            Assert.AreEqual("P1Base", z.ZoneAt(-38, 2));
            Assert.AreEqual("OpenSea", z.ZoneAt(3, 60));
            Assert.IsTrue(ZoneLookup.IsWater("OpenSea"));
            Assert.IsFalse(ZoneLookup.IsWater("Center"));
        }

        [Test]
        public void ZoneLookup_IncompleteWithoutAllNames()
        {
            var z = new ZoneLookup(); z.Add("Center", 0, 0);
            Assert.IsFalse(z.IsComplete());
        }

        [Test]
        public void Gather_FillsAtRateAndCapsAtTen()
        {
            var g = new GatherState();
            float taken = g.Tick(5f, "wood", 100, false);
            Assert.AreEqual(3f, taken, 0.001f);
            g.Tick(20f, "wood", 100, false);
            Assert.AreEqual(10, g.CarriedWhole);
            Assert.IsTrue(g.Full);
            Assert.AreEqual(10, g.Deposit());
            Assert.AreEqual(0, g.CarriedWhole);
        }

        [Test]
        public void Gather_NodeRunsOutAndHorseCollarHelpsFoodOnly()
        {
            var g = new GatherState();
            Assert.AreEqual(2f, g.Tick(10f, "gold", 2f, false), 0.001f);
            var f = new GatherState();
            Assert.AreEqual(0.72f, f.Tick(1f, "food", 50, true), 0.001f);
            var w = new GatherState();
            Assert.AreEqual(0.6f, w.Tick(1f, "wood", 50, true), 0.001f);
        }

        [Test]
        public void Capacity_ComesFromHousesAndStopsAtFifty()
        {
            Assert.AreEqual(5, GameConstants.Capacity(1, 0));
            Assert.AreEqual(15, GameConstants.Capacity(1, 2));
            Assert.AreEqual(50, GameConstants.Capacity(1, 20));
        }

        [Test]
        public void Training_BlockedAtPopulationCap()
        {
            var s = Fresh(); s.PopulationUsed = 5; s.PopulationCapacity = 5;
            var r = validator.Validate(new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "villager", Count = 1 }, s);
            Assert.IsFalse(r.Ok);
        }
    }
}
