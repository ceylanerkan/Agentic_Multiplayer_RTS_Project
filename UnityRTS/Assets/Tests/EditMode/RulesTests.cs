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

        [Test]
        public void Formation_OneOffsetPerUnitAndNoOverlap()
        {
            for (int n = 1; n <= 12; n++)
            {
                var o = FormationLayout.Offsets(n, 2f);
                Assert.AreEqual(n, o.Count);
                for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
                    Assert.IsTrue(System.Math.Abs(o[i][0] - o[j][0]) >= 1.99f || System.Math.Abs(o[i][1] - o[j][1]) >= 1.99f);
            }
            var one = FormationLayout.Offsets(1, 2f);
            Assert.AreEqual(0f, one[0][0]); Assert.AreEqual(0f, one[0][1]);
            Assert.AreEqual(0, FormationLayout.Offsets(0, 2f).Count);
        }

        [Test]
        public void GameData_HasBuildingHpAndTimes()
        {
            Assert.AreEqual(1500f, data.Buildings["town_center"].Hp);
            Assert.Greater(data.Buildings["barracks"].TimeSeconds, 0f);
            Assert.AreEqual(12f, data.Units["villager"].TimeSeconds);
            Assert.AreEqual(40f, data.Techs["age_2"].TimeSeconds);
            Assert.Greater(data.Units["villager"].Speed, 0f);
            Assert.Contains("villager", data.Buildings["town_center"].Trains);
        }

        [Test]
        public void Queue_RunsInOrderAndReportsProgress()
        {
            var q = new ProductionQueue();
            Assert.IsTrue(q.Enqueue("villager", false, 10f));
            Assert.IsTrue(q.Enqueue("villager", false, 10f));
            Assert.IsNull(q.Tick(5f));
            Assert.AreEqual(0.5f, q.CurrentProgress, 0.001f);
            Assert.AreEqual("villager", q.Tick(5f).Key);
            Assert.AreEqual(1, q.Count);
        }

        [Test]
        public void Queue_TownCenterCannotTrainWhileResearchingAgeTwo()
        {
            var q = new ProductionQueue();
            Assert.IsTrue(q.Enqueue("age_2", true, 40f));
            Assert.IsFalse(q.Enqueue("villager", false, 12f));
            Assert.IsTrue(q.Enqueue("horse_collar", true, 20f)); // other research may queue behind it
            var q2 = new ProductionQueue(); q2.Enqueue("villager", false, 12f);
            Assert.IsFalse(q2.Enqueue("age_2", true, 40f)); // and the other way round
        }

        [Test]
        public void Queue_HasMaxLength()
        {
            var q = new ProductionQueue();
            for (int i = 0; i < ProductionQueue.MaxLength; i++) Assert.IsTrue(q.Enqueue("villager", false, 1f));
            Assert.IsFalse(q.Enqueue("villager", false, 1f));
        }

        [Test]
        public void Construction_MoreBuildersAreFaster()
        {
            var c = new ConstructionState(30f);
            c.AddWork(10f); c.AddWork(10f); // two builders for 10 s
            Assert.AreEqual(20f / 30f, c.Progress, 0.001f);
            Assert.IsFalse(c.Done);
            c.AddWork(10f);
            Assert.IsTrue(c.Done); Assert.AreEqual(1f, c.Progress);
        }

        [Test]
        public void TechEffects_ApplyOnlyToTheRightUnits()
        {
            var none = new HashSet<string>();
            var all = new HashSet<string> { "iron_weapons", "fletching", "horse_collar", "age_2" };
            Assert.AreEqual(0f, TechEffects.AttackBonus("spearman", none));
            Assert.AreEqual(2f, TechEffects.AttackBonus("spearman", all));
            Assert.AreEqual(0f, TechEffects.AttackBonus("villager", all));
            Assert.AreEqual(1f, TechEffects.RangeBonus("archer", all));
            Assert.AreEqual(0f, TechEffects.RangeBonus("spearman", all));
            Assert.AreEqual(1f, TechEffects.LosBonus("archer", all));
            Assert.AreEqual(1.2f, TechEffects.FoodGatherMultiplier(all), 0.0001f);
            Assert.AreEqual(1f, TechEffects.FoodGatherMultiplier(none));
        }

        [Test]
        public void IronWeapons_RaisesDamage()
        {
            var sp = data.Units["spearman"]; var vil = data.Units["villager"];
            float before = DamageCalculator.Damage(sp, vil, false, 0f);
            float after = DamageCalculator.Damage(sp, vil, false, TechEffects.AttackBonus("spearman", new HashSet<string> { "iron_weapons" }));
            Assert.AreEqual(2f, after - before, 0.001f);
        }

        [Test]
        public void WinCondition_LoserIsThePlayerWithoutTownCenter()
        {
            Assert.AreEqual(0, WinCondition.LosingSlot(1, 1));
            Assert.AreEqual(1, WinCondition.LosingSlot(0, 1));
            Assert.AreEqual(2, WinCondition.LosingSlot(2, 0));
        }

        [Test]
        public void Scout_IsTrainedAtTheBarracksWithLongestSight()
        {
            Assert.Contains("scout", data.Buildings["barracks"].Trains);
            Assert.AreEqual(2f, data.Units["scout"].Attack);
            Assert.AreEqual(12f, data.Units["scout"].LineOfSight);
            Assert.AreEqual(10f, data.Buildings["town_center"].LineOfSight);
        }

        static MapObject Obj(int id, int owner, float x, float z, float los, string type = "spearman")
        {
            return new MapObject { Id = id, Owner = owner, Type = type, X = x, Z = z, Los = los };
        }

        [Test]
        public void Visibility_OwnAlwaysEnemyOnlyInSight()
        {
            var all = new List<MapObject> { Obj(1, 1, 0, 0, 7), Obj(2, 2, 5, 0, 7), Obj(3, 2, 30, 0, 7), Obj(4, 1, -3, 0, 7) };
            var v1 = VisibilityService.FilterFor(1, all);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, v1.ConvertAll(o => o.Id));   // enemy 3 is hidden
            var v2 = VisibilityService.FilterFor(2, all);
            CollectionAssert.AreEquivalent(new[] { 2, 3, 1 }, v2.ConvertAll(o => o.Id));   // P2 sees P1 unit at distance 5
        }

        [Test]
        public void Visibility_SightEdgeAndPoints()
        {
            var all = new List<MapObject> { Obj(1, 1, 0, 0, 7), Obj(2, 2, 7, 0, 5), Obj(3, 2, 7.1f, 0, 5) };
            var v = VisibilityService.FilterFor(1, all);
            Assert.AreEqual(2, v.Count);      // exactly at the edge counts as visible
            Assert.IsTrue(VisibilityService.PointVisible(1, 3, 3, all));
            Assert.IsFalse(VisibilityService.PointVisible(1, 20, 20, all));
        }

        [Test]
        public void EventLog_ValidTypesLimitsAndAge()
        {
            var log = new PlayerEventLog();
            Assert.IsFalse(log.Add("SECRET_ENEMY_POSITION", "Center", "x", 1f));
            Assert.IsTrue(log.Add("UNIT_TRAINED", "P1Base", "villager", 1f));
            Assert.IsTrue(log.Add("UNDER_ATTACK", "NorthGate", null, 5f));
            Assert.IsFalse(log.Add("UNDER_ATTACK", "NorthGate", null, 8f));   // within the 10 s cooldown
            Assert.IsTrue(log.Add("UNDER_ATTACK", "NorthGate", null, 20f));
            for (int i = 0; i < 40; i++) log.Add("UNIT_TRAINED", "P1Base", new string('x', 200), 21f + i);
            var recent = log.Recent(100f);
            Assert.AreEqual(20, recent.Count);
            Assert.AreEqual(80, recent[0].Key.Detail.Length);
            Assert.AreEqual(100f - 60f, recent[0].Value, 0.001f);
            Assert.LessOrEqual(log.Count, PlayerEventLog.MaxStored);
        }

        [Test]
        public void Snapshot_GroupsUnitsAndKeepsOnlyGivenEnemies()
        {
            var units = new List<OwnUnitInfo>
            {
                new OwnUnitInfo { Type = "villager", Zone = "P1Base", HpPct = 100, Idle = true },
                new OwnUnitInfo { Type = "villager", Zone = "P1Base", HpPct = 50, Idle = false },
                new OwnUnitInfo { Type = "spearman", Zone = "Center", HpPct = 80, Idle = true },
            };
            var buildings = new List<OwnBuildingInfo>
            {
                new OwnBuildingInfo { Type = "town_center", Completed = true, Busy = true },
                new OwnBuildingInfo { Type = "house", Completed = false },
            };
            var enemies = new List<EnemySighting> { new EnemySighting { Type = "cavalry", Zone = "NorthGate" }, new EnemySighting { Type = "cavalry", Zone = "NorthGate" } };
            var log = new PlayerEventLog(); log.Add("UNIT_TRAINED", "P1Base", "villager", 5f);
            var snap = SnapshotAssembler.Build("m1", 100f, 1, 200, 150, 50, 3, 5, units, buildings, enemies,
                new List<string>(), new List<string>(), log.Recent(10f));
            Assert.AreEqual(2, snap.OwnArmy.Count);
            var vill = snap.OwnArmy.Find(g => g.Type == "villager");
            Assert.AreEqual(2, vill.Count); Assert.AreEqual(1, vill.Idle); Assert.AreEqual(75f, vill.AvgHpPct, 0.01f);
            Assert.AreEqual(1, snap.OwnBuildings.Find(b => b.Type == "house").UnderConstruction);
            Assert.AreEqual(0, snap.OwnBuildings.Find(b => b.Type == "house").Count);
            Assert.AreEqual(1, snap.VisibleEnemies.Count); Assert.AreEqual(2, snap.VisibleEnemies[0].Count);
            Assert.AreEqual(5f, snap.RecentEvents[0].SecondsAgo, 0.01f);
            StringAssert.DoesNotContain("opponent", snap.ToJson());
        }

        // Writes the sample that the Python contract test (ai/gateway/tests/test_snapshot_contract.py) validates.
        // Only when RTS_SNAPSHOT_OUT is set, so a normal test run never touches the repository.
        [Test]
        public void Snapshot_ExportSampleForPythonContractTest()
        {
            string outPath = System.Environment.GetEnvironmentVariable("RTS_SNAPSHOT_OUT");
            var units = new List<OwnUnitInfo>
            {
                new OwnUnitInfo { Type = "villager", Zone = "P1Base", HpPct = 100, Idle = true },
                new OwnUnitInfo { Type = "archer", Zone = "NorthGate", HpPct = 64.5f, Idle = false },
            };
            var buildings = new List<OwnBuildingInfo> { new OwnBuildingInfo { Type = "town_center", Completed = true, Busy = true }, new OwnBuildingInfo { Type = "barracks", Completed = false } };
            var enemies = new List<EnemySighting> { new EnemySighting { Type = "spearman", Zone = "Center" } };
            var log = new PlayerEventLog(); log.Add("ENEMY_SIGHTED", "Center", "enemy spearman", 40f); log.Add("UNIT_TRAINED", "P1Base", "villager", 50f);
            var snap = SnapshotAssembler.Build("sample-match", 62.5f, 2, 120, 340, 60, 4, 10, units, buildings, enemies,
                new List<string> { "age_2" }, new List<string> { "iron_weapons" }, log.Recent(60f));
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(snap, Newtonsoft.Json.Formatting.Indented);
            if (!string.IsNullOrEmpty(outPath)) File.WriteAllText(outPath, json);
            Assert.IsTrue(json.Contains("\"visible_enemies\""));
        }

        // ---- Night 10: domain rules ----
        [Test]
        public void Domain_DockNeedsOwnCoastalSlot()
        {
            var s = Fresh(); s.Resources["wood"] = 500;
            var dock = new PlanAction { ActionType = "BUILD", BuildingType = "dock", TargetZone = "P1Base" };
            Assert.IsFalse(validator.Validate(dock, s).Ok);                 // no free coastal slot
            s.CoastalSlotZones.Add("P1Base");
            Assert.IsTrue(validator.Validate(dock, s).Ok);
            dock.TargetZone = "Center";                                      // slot exists only in the own base
            Assert.IsFalse(validator.Validate(dock, s).Ok);
            dock.TargetZone = "P1Harbor";                                    // water zone: buildings are land only
            Assert.IsFalse(validator.Validate(dock, s).Ok);
        }

        [Test]
        public void Domain_NoBuildingInTheEnemyBase()
        {
            var s = Fresh(); s.Resources["wood"] = 500;
            var house = new PlanAction { ActionType = "BUILD", BuildingType = "house", TargetZone = "P2Base" };
            Assert.IsFalse(validator.Validate(house, s).Ok);                 // P1 cannot build in P2Base
            s.Slot = 2;
            Assert.IsTrue(validator.Validate(house, s).Ok);
            house.TargetZone = "P1Base";
            Assert.IsFalse(validator.Validate(house, s).Ok);
        }

        [Test]
        public void Domain_LandAndWaterMoves()
        {
            var s = Fresh();
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "MOVE_GROUP", UnitGroup = "archer", TargetZone = "OpenSea" }, s).Ok);
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "MOVE_GROUP", UnitGroup = "warship", TargetZone = "Center" }, s).Ok);
            Assert.IsTrue(validator.Validate(new PlanAction { ActionType = "MOVE_GROUP", UnitGroup = "warship", TargetZone = "OpenSea" }, s).Ok);
            Assert.IsTrue(validator.Validate(new PlanAction { ActionType = "MOVE_GROUP", UnitGroup = "all_military", TargetZone = "NorthGate" }, s).Ok);
        }

        [Test]
        public void Validator_RejectsBadCountsAndUnknownThings()
        {
            var s = Fresh();
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "villager", Count = 21 }, s).Ok);
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "dragon", Count = 1 }, s).Ok);
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "DELETE_ALL", Count = 1 }, s).Ok);
            Assert.IsFalse(validator.Validate(new PlanAction { ActionType = "BUILD", BuildingType = "town_center", TargetZone = "Center" }, s).Ok);
        }

        [Test]
        public void Approval_RechecksOnCurrentStateAndReservesResources()
        {
            var s = Fresh(); s.Resources["wood"] = 200; s.Resources["food"] = 100; // only 2 villagers affordable
            var actions = new List<PlanAction>
            {
                new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "villager", Count = 2 },
                new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "villager", Count = 1 }, // food is already reserved
            };
            s.PopulationCapacity = 10;
            var r = ApprovalEvaluator.Evaluate(actions, s, validator);
            Assert.IsTrue(r[0].Ok); Assert.IsFalse(r[1].Ok);
            Assert.AreEqual(100, s.Resources["food"]); // evaluation never spends anything
        }

        [Test]
        public void DecisionLog_ProducesIdsAndJson()
        {
            var log = new DecisionLog();
            Assert.AreEqual("plan-001", log.NextPlanId()); Assert.AreEqual("plan-002", log.NextPlanId());
            var e = log.Add(new DecisionEntry { PlanId = "plan-001", Decision = "rejected", GameTime = 5f });
            e.Actions.Add("TRAIN_UNIT archer"); e.Validator.Add("not checked");
            string line = log.ToJsonLine(e);
            StringAssert.Contains("\"plan_id\":\"plan-001\"", line);
            StringAssert.Contains("\"decision\":\"rejected\"", line);
            Assert.AreEqual(1, log.Entries.Count);
        }

        [Test]
        public void ActionCosts_TotalsAndDescription()
        {
            var a = new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "villager", Count = 3 };
            Assert.AreEqual(150, ActionCosts.CostOf(a, data)["food"]);
            StringAssert.Contains("150F", ActionCosts.Describe(a, data));
        }

        // ---- Night 12: siege and naval ----
        [Test]
        public void Catapult_FiresSlowlyHasLongRangeAndHitsBuildingsHard()
        {
            Assert.Greater(GameConstants.CooldownFor("catapult"), GameConstants.CooldownFor("spearman"));
            var cat = data.Units["catapult"];
            Assert.Greater(cat.Range, data.Units["archer"].Range);
            var tc = data.Buildings["town_center"];
            float vsBuilding = DamageCalculator.Damage(cat, tc, true);
            float vsUnit = DamageCalculator.Damage(cat, data.Units["spearman"], false);
            Assert.AreEqual(cat.Attack * 3f - tc.Armor, vsBuilding, 0.001f);
            Assert.Greater(vsBuilding, vsUnit);
            Assert.AreEqual(1.5f, DamageCalculator.Multiplier(cat, data.Units["warship"], false)); // counters Warships
        }

        [Test]
        public void Domain_MeleeStaysInOwnDomainRangedShootsAcross()
        {
            var spear = data.Units["spearman"]; var archer = data.Units["archer"];
            var boat = data.Units["raider_boat"]; var ship = data.Units["warship"]; var cat = data.Units["catapult"];
            Assert.IsFalse(DomainRules.CanAttack(spear, DomainRules.Water));   // melee on land cannot hit ships
            Assert.IsTrue(DomainRules.CanAttack(archer, DomainRules.Water));   // ranged can
            Assert.IsTrue(DomainRules.CanAttack(cat, DomainRules.Water));
            Assert.IsTrue(DomainRules.CanAttack(ship, DomainRules.Land));
            Assert.IsFalse(DomainRules.CanAttack(boat, DomainRules.Land));     // melee boat cannot hit land units
            Assert.IsTrue(DomainRules.CanAttack(boat, DomainRules.Water));
            string dock = DomainRules.DomainOfBuilding("dock");
            Assert.IsTrue(DomainRules.CanAttack(spear, dock));                 // a Dock can be attacked from land and water
            Assert.IsTrue(DomainRules.CanAttack(boat, dock));
            Assert.AreEqual(DomainRules.Land, DomainRules.DomainOfBuilding("house"));
        }

        [Test]
        public void Domain_WhereUnitsMayStand()
        {
            Assert.IsTrue(DomainRules.CanStandIn(data.Units["cavalry"], false));
            Assert.IsFalse(DomainRules.CanStandIn(data.Units["cavalry"], true));
            Assert.IsTrue(DomainRules.CanStandIn(data.Units["warship"], true));
            Assert.IsFalse(DomainRules.CanStandIn(data.Units["warship"], false));
        }

        [Test]
        public void Naval_CountersFollowTheLoop()
        {
            float boatVsShip = DamageCalculator.Multiplier(data.Units["raider_boat"], data.Units["warship"], false);
            float shipVsArcher = DamageCalculator.Multiplier(data.Units["warship"], data.Units["archer"], false);
            float archerVsBoat = DamageCalculator.Multiplier(data.Units["archer"], data.Units["raider_boat"], false);
            Assert.AreEqual(1.5f, boatVsShip); Assert.AreEqual(1.5f, shipVsArcher); Assert.AreEqual(1.5f, archerVsBoat);
        }

        [Test]
        public void EventLog_TypeListMatchesValidTypes()
        {
            Assert.AreEqual(PlayerEventLog.ValidTypes.Count, PlayerEventLog.TypeList.Length);
            foreach (var t in PlayerEventLog.TypeList) Assert.IsTrue(PlayerEventLog.ValidTypes.Contains(t));
            Assert.AreEqual(10, ZoneLookup.AllZones.Length);
        }

        // A whole opening with the same rules the host uses (CommandRelay): pay when queued, reserve population, capacity from Houses.
        [Test]
        public void Scenario_Opening_PopulationCapHouseThenMoreVillagers()
        {
            var s = new PlayerState { PopulationUsed = GameConstants.StartVillagers, PopulationCapacity = GameConstants.Capacity(1, 0) };
            s.Resources["wood"] = GameConstants.StartWood; s.Resources["food"] = GameConstants.StartFood; s.Resources["gold"] = GameConstants.StartGold;
            s.CompletedBuildings["town_center"] = 1;
            var train = new PlanAction { ActionType = "TRAIN_UNIT", UnitType = "villager", Count = 1 };
            var queue = new ProductionQueue();

            for (int i = 0; i < 2; i++)   // 3/5 -> 5/5
            {
                Assert.IsTrue(validator.Validate(train, s).Ok);
                Assert.IsTrue(queue.Enqueue("villager", false, data.Units["villager"].TimeSeconds));
                s.Resources["food"] -= data.Units["villager"].Cost["food"]; s.PopulationUsed++;
            }
            Assert.AreEqual(100, s.Resources["food"]);
            Assert.IsFalse(validator.Validate(train, s).Ok);                    // blocked at the cap

            var house = new PlanAction { ActionType = "BUILD", BuildingType = "house", TargetZone = "P1Base" };
            Assert.IsTrue(validator.Validate(house, s).Ok);
            foreach (var kv in data.Buildings["house"].Cost) s.Resources[kv.Key] -= kv.Value;
            var site = new ConstructionState(data.Buildings["house"].TimeSeconds);
            site.AddWork(data.Buildings["house"].TimeSeconds);                  // a Villager works until it is done
            Assert.IsTrue(site.Done);
            s.CompletedBuildings["house"] = 1; s.PopulationCapacity = GameConstants.Capacity(1, 1);
            Assert.AreEqual(10, s.PopulationCapacity);
            Assert.IsTrue(validator.Validate(train, s).Ok);                     // training works again

            float total = 0; ProductionItem done = null;
            while (queue.Count > 0) { total += 1f; done = queue.Tick(1f) ?? done; }
            Assert.AreEqual(2 * data.Units["villager"].TimeSeconds, total, 1.01f);
            Assert.IsNotNull(done);
        }
    }
}
