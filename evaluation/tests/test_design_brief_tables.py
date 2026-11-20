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


def key(name):
    return name.strip().lower().replace(" ", "_").replace("age_ii", "age_2")


def test_where_and_requirements_match():
    # "Trained at" is e.g. "Stable (Age II)": the building, plus Age II when the unit needs it
    for name, *_rest in rows("Unit"):
        trained = _rest[7]
        u = UNITS[name]
        assert key(trained.split("(")[0]) == u["trained_at"], name
        assert ("Age II" in trained) == ("age_2" in u["requires"]), name
    for name, _c, _hp, _f, req in rows("Building"):
        assert ([] if req == "–" else [key(r) for r in req.split(",")]) == BUILDINGS[name]["requires"], name
    for name, _c, where, req, _e in rows("Technology"):
        t = TECHS[name]
        assert key(where) == t["researched_at"], name
        expected = [] if req == "–" else [key(r) for r in req.split(",")]
        assert set(expected) <= set(t["requires"]), name   # the brief omits the obvious "own a Blacksmith"


def test_zone_names_agree_everywhere():
    # brief, both schemas and Unity's ZoneLookup must list the same 10 zones, with the same 3 water zones
    m = re.search(r"\*\*Land zones:\*\*\s*(.*?)\.\s*\*\*Water zones:\*\*\s*(.*?)\.", BRIEF, re.S)
    land = {z.strip() for z in m.group(1).replace("\n", " ").split(",")}
    water = {z.strip() for z in m.group(2).split(",")}
    for schema in ("action_plan.schema.json", "safe_snapshot.schema.json"):
        enums = re.findall(r'"enum":\s*\[("P1Base"[^\]]*)\]', (ROOT / "ai/schemas" / schema).read_text(encoding="utf-8"))
        assert enums and all(set(re.findall(r'"(\w+)"', e)) == land | water for e in enums), schema
    cs = (ROOT / "UnityRTS/Assets/Scripts/Core/Rules/ZoneLookup.cs").read_text(encoding="utf-8")
    cs_water = set(re.findall(r'"(\w+)"', re.search(r"Water = new HashSet<string> \{([^}]*)\}", cs).group(1)))
    assert cs_water == water
    assert land | water <= set(re.findall(r'"(\w+)"', cs))


def test_unit_groups_agree_with_units_and_validator():
    # schema enum = the 3 named groups + every unit type except villager (GameActionValidator accepts exactly that)
    schema = json.loads((ROOT / "ai/schemas/action_plan.schema.json").read_text(encoding="utf-8"))
    enum = set(schema["$defs"]["unit_group"]["enum"])
    cs = (ROOT / "UnityRTS/Assets/Scripts/Core/Rules/GameActionValidator.cs").read_text(encoding="utf-8")
    named = set(re.findall(r'"(\w+)"', re.search(r"Groups = new HashSet<string> \{([^}]*)\}", cs).group(1)))
    unit_keys = {u["key"] for u in UNITS.values()}
    assert named == {"all_military", "all_ships", "idle_villagers"}
    assert enum == named | (unit_keys - {"villager"})


def test_every_action_type_is_validated_and_executed():
    schema = json.loads((ROOT / "ai/schemas/action_plan.schema.json").read_text(encoding="utf-8"))
    types = schema["$defs"]["action"]["properties"]["action_type"]["enum"]
    assert types
    scripts = ROOT / "UnityRTS/Assets/Scripts"
    validator = (scripts / "Core/Rules/GameActionValidator.cs").read_text(encoding="utf-8")
    executor = (scripts / "AI/AiApprovalFlow.cs").read_text(encoding="utf-8")
    for t in types:
        assert f'case "{t}":' in validator and f'case "{t}":' in executor, t
    # the count limit is written in three places: schema, C# validator, Python mirror
    n = schema["$defs"]["action"]["properties"]["count"]["maximum"]
    assert f"a.Count > {n})" in validator
    assert f"count > {n}:" in (ROOT / "evaluation/plan_checks.py").read_text(encoding="utf-8")


def test_event_types_agree_between_unity_and_snapshot_schema():
    cs = (ROOT / "UnityRTS/Assets/Scripts/Core/Rules/PlayerEventLog.cs").read_text(encoding="utf-8")
    unity = re.findall(r'"([A-Z_]+)"', re.search(r"TypeList\s*=\s*\{([^}]*)\}", cs).group(1))
    schema = (ROOT / "ai/schemas/safe_snapshot.schema.json").read_text(encoding="utf-8")
    enum = re.findall(r'"([A-Z_]+)"', re.search(r'"type":\s*\{"enum":\s*\[("UNDER_ATTACK"[^\]]*)\]', schema).group(1))
    assert len(unity) == 8 and set(unity) == set(enum)


def test_report_tool_table_lists_exactly_the_server_tools():
    server = (ROOT / "ai/mcp-server/server.py").read_text(encoding="utf-8")
    tools = set(re.findall(r"@mcp\.tool\([^)]*\)\s*\n\s*def (\w+)\(", server))
    report = (ROOT / "docs/report/report.md").read_text(encoding="utf-8")
    listed = set(re.findall(r"^\| `(\w+)` \|", report, re.M))
    assert len(tools) == 7 and listed == tools


def test_snapshot_limits_agree_between_unity_and_schema():
    # Unity cuts the snapshot to these sizes before sending; larger and the gateway would reject it with 422
    schema = json.loads((ROOT / "ai/schemas/safe_snapshot.schema.json").read_text(encoding="utf-8"))
    rules = ROOT / "UnityRTS/Assets/Scripts/Core/Rules"
    events = int(re.search(r"MaxInSnapshot = (\d+)", (rules / "PlayerEventLog.cs").read_text(encoding="utf-8")).group(1))
    groups = int(re.search(r"MaxGroups = (\d+)", (rules / "SnapshotAssembler.cs").read_text(encoding="utf-8")).group(1))
    assert schema["properties"]["recent_events"]["maxItems"] == events
    assert schema["properties"]["own_army"]["maxItems"] == groups
