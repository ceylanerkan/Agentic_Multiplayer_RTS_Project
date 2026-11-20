"""Consistency: numbers hard-coded in Unity's GameConstants.cs must match the knowledge base and design brief."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CS = (ROOT / "UnityRTS/Assets/Scripts/Core/Rules/GameConstants.cs").read_text(encoding="utf-8")
RULES = (ROOT / "ai/rag/knowledge/rules.md").read_text(encoding="utf-8")


def const(name):
    return float(re.search(rf"\b{name}\s*=\s*([0-9.]+)f?", CS).group(1))


def test_starting_state_matches_rules():
    m = re.search(r"(\d+) wood,? (\d+) food,? and (\d+) gold|(\d+) wood, (\d+) food, (\d+) gold", RULES)
    assert m, "starting resources not found in rules.md"
    nums = [int(x) for x in m.groups() if x]
    assert nums == [const("StartWood"), const("StartFood"), const("StartGold")]


def test_population_numbers_match_rules_and_brief():
    assert const("MaxPopulation") == 50 and const("TownCenterCapacity") == 5 and const("HouseCapacity") == 5
    assert "maximum of 50" in RULES and "5 from the Town Center and 5 from each completed House" in RULES


def test_gather_numbers_match_rules():
    gs = (ROOT / "UnityRTS/Assets/Scripts/Core/Rules/GatherState.cs").read_text(encoding="utf-8")
    assert "RatePerSecond = 0.6f" in gs and "CarryMax = 10" in gs
    assert "0.6 resource per second" in RULES and "at most 10" in RULES


def test_every_unit_and_building_has_a_generated_prefab():
    # GameSpawner throws "No prefab with key" when a key from the JSON has no prefab row in RtsSetupTools.cs
    import json
    tools = (ROOT / "UnityRTS/Assets/Scripts/Editor/RtsSetupTools.cs").read_text(encoding="utf-8")
    rows = set(re.findall(r'\{ "([a-z_0-9]+)", "[A-Za-z]+", "[0-9.,]+", "[0-9.]+" \}', tools))
    for name in ("units", "buildings"):
        keys = {r["key"] for r in json.loads((ROOT / f"ai/rag/knowledge/{name}.json").read_text(encoding="utf-8"))}
        assert keys <= rows, f"{name} without a prefab row in RtsSetupTools.cs: {sorted(keys - rows)}"


def test_scene_simulation_behaviours_register_with_the_runner():
    # Fusion only auto-registers a SimulationBehaviour on the runner's own GameObject. One in a scene must call
    # AddGlobal itself, or its Runner stays null (UNITY_FEEDBACK 2026-11-19: GameSpawner never spawned any base).
    scripts = ROOT / "UnityRTS/Assets/Scripts"
    found = []
    for path in scripts.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        if re.search(r"class \w+\s*:\s*SimulationBehaviour\b", text):
            found.append(path.name)
            assert "AddGlobal(this)" in text, f"{path.name} is a SimulationBehaviour but never calls Runner.AddGlobal(this)"
    assert "GameSpawner.cs" in found


def test_tech_effect_constants_match_technologies_json():
    # TechEffects.cs hard-codes the effect numbers; they must say the same as the "effect" texts the AI reads.
    techs = {t["key"]: t["effect"] for t in json.loads((ROOT / "ai/rag/knowledge/technologies.json").read_text(encoding="utf-8"))}
    cs = (ROOT / "UnityRTS/Assets/Scripts/Core/Rules/TechEffects.cs").read_text(encoding="utf-8")
    num = lambda name: float(re.search(rf"\b{name}\s*=\s*([0-9.]+)f", cs).group(1))
    assert f"+{num('IronWeaponsAttack'):g} attack for Spearman, Archer and Cavalry" in techs["iron_weapons"]
    assert '"spearman", "archer", "cavalry"' in cs
    assert f"+{num('FletchingRange'):g} range and +{num('FletchingLos'):g} line of sight for Archers" in techs["fletching"]
    assert f"+{round((num('HorseCollarFood') - 1) * 100)}% food" in techs["horse_collar"]


def test_damage_multipliers_match_rules():
    cs = (ROOT / "UnityRTS/Assets/Scripts/Core/Rules/DamageCalculator.cs").read_text(encoding="utf-8")
    num = lambda name: float(re.search(rf"\b{name}\s*=\s*([0-9.]+)f", cs).group(1))
    assert f"multiplier is {num('CounterBonus'):g} when the attacker counters the target and {num('CounterPenalty'):g} when" in RULES
    assert f"Catapults deal {num('CatapultVsBuilding'):g}x damage to buildings" in RULES
