"""The design brief's unit, building and technology tables must say the same as the knowledge JSON (the source of truth).

The brief is what the oral defense and the report quote, so a number that drifted there would be presented as wrong.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BRIEF = (ROOT / "docs/design-brief.md").read_text(encoding="utf-8")
KB = ROOT / "ai/rag/knowledge"
UNITS = {u["name"]: u for u in json.loads((KB / "units.json").read_text(encoding="utf-8"))}
BUILDINGS = {b["name"]: b for b in json.loads((KB / "buildings.json").read_text(encoding="utf-8"))}
TECHS = {t["name"]: t for t in json.loads((KB / "technologies.json").read_text(encoding="utf-8"))}
LETTER = {"W": "wood", "F": "food", "G": "gold"}


def rows(first_header):
    # rows of the markdown table whose header starts with `| <first_header> |`
    lines = BRIEF.splitlines()
    start = next(i for i, l in enumerate(lines) if l.startswith(f"| {first_header} |"))
    out = []
    for l in lines[start + 2:]:
        if not l.startswith("|"):
            break
        out.append([c.strip() for c in l.strip("|").split("|")])
    return out


def cost(text):
    return {LETTER[m[1]]: int(m[0]) for m in re.findall(r"(\d+)([WFG])", text)}


def test_unit_table_matches_units_json():
    table = rows("Unit")
    assert {r[0] for r in table} == set(UNITS)
    for name, domain, c, hp, atk, armor, rng, speed, *_ in table:
        u, s = UNITS[name], UNITS[name]["stats"]
        assert (domain, cost(c)) == (u["domain"], u["cost"]), name
        assert [float(hp), float(atk), float(armor), float(rng), float(speed)] == \
               [s["hp"], s["attack"], s["armor"], s["range"], s["speed"]], name


def test_building_table_matches_buildings_json():
    table = rows("Building")
    assert {r[0] for r in table} == set(BUILDINGS)
    for name, c, hp, *_ in table:
        b = BUILDINGS[name]
        assert float(hp) == b["hp"], name
        if c != "start only":
            assert cost(c) == b["cost"], name


def test_technology_table_matches_technologies_json():
    table = rows("Technology")
    assert {r[0] for r in table} == set(TECHS)
    for name, c, *_ in table:
        assert cost(c) == TECHS[name]["cost"], name
