"""Consistency: numbers hard-coded in Unity's GameConstants.cs must match the knowledge base and design brief."""
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
