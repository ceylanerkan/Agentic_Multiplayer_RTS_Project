"""Python copy of the gameplay checks in GameActionValidator.cs (Assets/Scripts/Core/Rules), used to measure the
invalid-action rate of AI plans without Unity. Numbers come from ai/rag/knowledge/*.json, same as in Unity.
Keep it in sync with the C# class: the tests in evaluation/tests/test_plan_checks.py mirror the C# EditMode tests.
"""
import json
from pathlib import Path

KNOW = Path(__file__).resolve().parents[1] / "ai" / "rag" / "knowledge"
GROUPS = {"all_military", "all_ships", "idle_villagers"}  # plus every unit type except villager (schema)
LAND = {"P1Base", "P2Base", "NorthGate", "SouthField", "EastForest", "WestMine", "Center"}
WATER = {"P1Harbor", "P2Harbor", "OpenSea"}


def load_data():
    data = {}
    for kind, name in (("units", "units"), ("buildings", "buildings"), ("techs", "technologies")):
        data[kind] = {r["key"]: r for r in json.loads((KNOW / f"{name}.json").read_text(encoding="utf-8"))}
    return data


DATA = load_data()


def _requirement(requires, age, techs, buildings):
    for r in requires:
        if r == "age_2":
            if age < 2:
                return "requires Age II"
        elif buildings.get(r, 0) < 1 and r not in techs:
            return f"requires {r}"
    return None


def _pay(cost, count, resources, reserved):
    for res, amount in (cost or {}).items():
        if resources.get(res, 0) - reserved.get(res, 0) < amount * count:
            return f"not enough {res}"
    for res, amount in (cost or {}).items():
        reserved[res] = reserved.get(res, 0) + amount * count
    return None


def validate_action(a, state, reserved=None, slot=1, coastal_zones=("P1Base",)):
    """Returns None if the action is valid, otherwise the reason. `state` is a safe snapshot dict."""
    reserved = {} if reserved is None else reserved
    res = state["resources"]
    age, techs = state["age"], set(state["researched_tech"])
    buildings = {b["type"]: b["count"] for b in state["own_buildings"]}
    count = a.get("count", 1)
    if not isinstance(count, int) or count < 1 or count > 20:
        return "count must be 1-20"
    kind = a.get("action_type")
    if kind == "TRAIN_UNIT":
        u = DATA["units"].get(a.get("unit_type"))
        if not u:
            return "unknown unit_type"
        if state["population"]["used"] + u["population"] * count > state["population"]["capacity"]:
            return "not enough population capacity"
        return _requirement(u["requires"], age, techs, buildings) or _pay(u["cost"], count, res, reserved)
    if kind == "BUILD":
        b = DATA["buildings"].get(a.get("building_type"))
        if not b or b.get("buildable") is False:
            return "unknown or unbuildable building_type"
        zone = a.get("target_zone")
        if zone not in LAND:
            return "buildings need a land target_zone"
        if zone == ("P2Base" if slot == 1 else "P1Base"):
            return "cannot build in the enemy base"
        if a["building_type"] == "dock" and zone not in coastal_zones:
            return "dock needs a free coastal slot in your own base"
        return _requirement(b["requires"], age, techs, buildings) or _pay(b["cost"], count, res, reserved)
    if kind == "RESEARCH":
        t = DATA["techs"].get(a.get("tech_type"))
        if not t:
            return "unknown tech_type"
        if a["tech_type"] in techs:
            return "already researched"
        return _requirement(t["requires"], age, techs, buildings) or _pay(t["cost"], 1, res, reserved)
    if kind == "MOVE_GROUP":
        zone = a.get("target_zone")
        if zone not in LAND and zone not in WATER:
            return "unknown target_zone"
        group = a.get("unit_group")
        if group not in GROUPS and (group not in DATA["units"] or group == "villager"):
            return "unknown unit_group"
        ships = a.get("unit_group") in ("all_ships", "raider_boat", "warship")
        if ships and zone not in WATER:
            return "ships can only move to water zones"
        if not ships and zone in WATER:
            return "land units cannot enter water zones"
        return None
    return "unknown action_type"


def validate_plan(plan, state, **kw):
    """Checks every action in order with resource reservation (like ApprovalEvaluator). Returns a list of reasons or None."""
    reserved = {}
    return [validate_action(a, state, reserved, **kw) for a in plan.get("actions", [])]
