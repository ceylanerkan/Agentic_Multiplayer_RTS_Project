import copy
import json
import sys
from pathlib import Path

E = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(E))
import plan_checks as pc  # noqa: E402


def state(name="early_economy"):
    return json.loads((E / "fixtures" / f"{name}.json").read_text())


def test_valid_and_invalid_training():
    s = state()
    assert pc.validate_action({"action_type": "TRAIN_UNIT", "unit_type": "villager", "count": 1}, state("cavalry_rush")) is None
    assert pc.validate_action({"action_type": "TRAIN_UNIT", "unit_type": "villager", "count": 21}, s) == "count must be 1-20"
    assert "population" in pc.validate_action({"action_type": "TRAIN_UNIT", "unit_type": "villager", "count": 1}, s)  # 5/5
    assert pc.validate_action({"action_type": "TRAIN_UNIT", "unit_type": "dragon", "count": 1}, s) == "unknown unit_type"


def test_prerequisites_and_resources():
    s = state("cavalry_rush")
    assert pc.validate_action({"action_type": "TRAIN_UNIT", "unit_type": "spearman", "count": 2}, s) is None
    assert pc.validate_action({"action_type": "TRAIN_UNIT", "unit_type": "cavalry", "count": 1}, s) == "requires stable"
    assert pc.validate_action({"action_type": "RESEARCH", "tech_type": "age_2"}, s) == "not enough food"
    assert pc.validate_action({"action_type": "RESEARCH", "tech_type": "fletching"}, s) == "requires blacksmith"


def test_resources_are_reserved_across_actions():
    s = state("cavalry_rush")  # 250 food: 5 villagers affordable, not 6
    plan = {"actions": [{"action_type": "TRAIN_UNIT", "unit_type": "villager", "count": 3},
                        {"action_type": "TRAIN_UNIT", "unit_type": "villager", "count": 3}]}
    assert pc.validate_plan(plan, s) == [None, "not enough food"]


def test_domain_rules_mirror_csharp_tests():
    s = state("late_game")
    s["resources"]["wood"] = 500
    house = {"action_type": "BUILD", "building_type": "house", "target_zone": "P2Base"}
    assert pc.validate_action(house, s) == "cannot build in the enemy base"
    assert pc.validate_action(dict(house, target_zone="P1Harbor"), s) == "buildings need a land target_zone"
    dock = {"action_type": "BUILD", "building_type": "dock", "target_zone": "Center"}
    assert "coastal" in pc.validate_action(dock, s)
    assert pc.validate_action(dict(dock, target_zone="P1Base"), s) is None
    assert pc.validate_action({"action_type": "MOVE_GROUP", "unit_group": "archer", "target_zone": "OpenSea"}, s)
    assert pc.validate_action({"action_type": "MOVE_GROUP", "unit_group": "warship", "target_zone": "Center"}, s)
    assert pc.validate_action({"action_type": "MOVE_GROUP", "unit_group": "warship", "target_zone": "OpenSea"}, s) is None


def test_python_and_csharp_validators_give_the_same_reasons():
    # plan_checks.py is a copy of GameActionValidator.cs for the evaluation; every fixed rejection reason
    # in the copy must exist in the C# original, so the two cannot drift apart unnoticed.
    import re
    from pathlib import Path
    root = Path(__file__).resolve().parents[2]
    py = (root / "evaluation/plan_checks.py").read_text(encoding="utf-8")
    cs = (root / "UnityRTS/Assets/Scripts/Core/Rules/GameActionValidator.cs").read_text(encoding="utf-8")
    reasons = re.findall(r'return "([^"]+)"', py)
    assert len(reasons) >= 10
    missing = [r for r in reasons if f'"{r}"' not in cs]
    assert missing == [], f"reasons only in plan_checks.py: {missing}"


def test_move_group_needs_a_known_group():
    ok = {"action_type": "MOVE_GROUP", "unit_group": "spearman", "target_zone": "Center", "reason": "x"}
    assert pc.validate_action(ok, state()) is None
    for bad in (None, "dragons", "villager"):  # villagers move through "idle_villagers" only
        assert pc.validate_action(dict(ok, unit_group=bad), state()) == "unknown unit_group"
