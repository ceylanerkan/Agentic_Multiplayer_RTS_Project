import asyncio

import pytest
from common.session_store import SessionStore
from retriever import Retriever
from tools import game_tools as t

SNAP = {"match_id": "m", "game_time_s": 100, "age": 1,
        "resources": {"wood": 100, "food": 200, "gold": 0}, "population": {"used": 3, "capacity": 5},
        "own_army": [{"type": "villager", "count": 3, "zone": "P1Base"}],
        "own_buildings": [{"type": "town_center", "count": 1}],
        "visible_enemies": [{"type": "cavalry", "count": 4, "zone": "NorthGate"}],
        "researched_tech": [], "recent_events": [{"type": "ENEMY_SIGHTED", "seconds_ago": 5}] * 15}


@pytest.fixture(scope="module")
def retriever():
    return Retriever()


@pytest.fixture
def ctx(retriever):
    return t.ToolContext(SNAP, 2.0, retriever)


def test_live_tools(ctx):
    assert t.get_player_resources(ctx)["resources"]["food"] == 200
    assert t.get_army_status(ctx)["own_army"][0]["type"] == "villager"
    assert t.get_visible_enemies(ctx)["visible_enemies"][0]["count"] == 4
    assert t.get_technology_status(ctx)["researched_tech"] == []
    assert len(t.get_recent_events(ctx, 5)["events"]) == 5


def test_no_snapshot(retriever):
    assert "error" in t.get_player_resources(t.ToolContext(None, 0, retriever))


@pytest.mark.parametrize("limit", [0, 21, "5", True, None])
def test_recent_events_bad_limit(ctx, limit):
    assert "error" in t.get_recent_events(ctx, limit)


def test_estimate_cost_ok(ctx):
    r = t.estimate_plan_cost(ctx, [{"kind": "unit", "id": "spearman", "count": 4}])
    assert r["total_cost"] == {"wood": 120, "food": 200, "gold": 0}
    assert r["affordable"] is False and r["missing"]["wood"] == 20
    assert r["population_needed"] == 4 and r["population_ok"] is False


@pytest.mark.parametrize("items", [
    [], "x", [{"kind": "hero", "id": "x"}], [{"kind": "unit", "id": "dragon"}],
    [{"kind": "unit", "id": "spearman", "count": 0}], [{"kind": "unit", "id": "spearman", "count": 51}],
    [{"kind": "unit", "id": "spearman", "player": 2}], [{"kind": "building", "id": "town_center"}],
    [{"kind": "unit", "id": "spearman"}] * 11])
def test_estimate_cost_invalid(ctx, items):
    assert "error" in t.estimate_plan_cost(ctx, items)


def test_search(ctx):
    ids = [r["source_id"] for r in t.search_game_knowledge(ctx, "counter cavalry")["results"]]
    assert {"unit_spearman_v1", "strategy_defend_cavalry_v1"} & set(ids[:3])
    ids = [r["source_id"] for r in t.search_game_knowledge(ctx, "What does Iron Weapons do?", 3)["results"]]
    assert "tech_iron_weapons_v1" in ids
    assert len(t.search_game_knowledge(ctx, "army", 99)["results"]) <= 5  # clamped


@pytest.mark.parametrize("q", ["", "   ", "a" * 201, 5])
def test_search_invalid(ctx, q):
    assert "error" in t.search_game_knowledge(ctx, q)


def test_server_tools_readonly_and_no_player_arg(retriever):
    from server import build_server
    store = SessionStore(":memory:")
    sid = store.create_session()
    store.save_snapshot(sid, SNAP)
    mcp = build_server(store, sid, retriever)
    tools = asyncio.run(mcp.list_tools())
    assert len(tools) == 7
    for tool in tools:
        assert tool.annotations.read_only_hint is True
        assert not {"player", "player_id", "session_id"} & set(tool.input_schema.get("properties", {}))


def test_session_store():
    s = SessionStore(":memory:")
    a, b = s.create_session(), s.create_session()
    s.save_snapshot(a, SNAP)
    assert s.get_snapshot(a)[0]["match_id"] == "m" and s.get_snapshot(b) is None
    with pytest.raises(KeyError):
        s.save_snapshot("nope", SNAP)
