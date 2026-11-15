"""Night 11: the agent loop through a REAL stdio MCP client with the server as a subprocess, and a scripted LLM."""
import json

from common.session_store import SessionStore
from gateway.agent import Agent
from gateway.llm import LLMReply, ScriptedLLM
from gateway.logs import JsonlLog
from gateway.mcp_stdio import StdioMcp

from test_gateway import PLAN, SNAP


def run_agent(tmp_path, replies):
    db = tmp_path / "sessions.sqlite3"
    store = SessionStore(db)
    sid = store.create_session()
    store.save_snapshot(sid, SNAP)
    log = JsonlLog(tmp_path / "tool_calls.jsonl")
    with StdioMcp(sid, db) as mcp:
        agent = Agent(ScriptedLLM(replies), mcp, log, max_steps=6, timeout_s=60)
        plan, used = agent.run("s1", "question")
    lines = [json.loads(x) for x in (tmp_path / "tool_calls.jsonl").read_text().splitlines()]
    return plan, used, lines


def test_compound_question_calls_at_least_three_tools_over_stdio(tmp_path):
    replies = [
        LLMReply(tool_calls=[{"name": "get_player_resources", "args": {}}, {"name": "get_visible_enemies", "args": {}},
                             {"name": "search_game_knowledge", "args": {"query": "counter cavalry"}}]),
        LLMReply(tool_calls=[{"name": "estimate_plan_cost", "args": {"items": [{"kind": "unit", "id": "spearman", "count": 2}]}}]),
        LLMReply(text=json.dumps(PLAN)),
    ]
    plan, used, log = run_agent(tmp_path, replies)
    assert len(set(used)) >= 3 and len(used) == 4
    assert all(x["status"] == "ok" for x in log)
    assert plan["actions"][0]["unit_type"] == "spearman"


def test_knowledge_only_question_calls_only_search_over_stdio(tmp_path):
    plan = {"summary": "Iron Weapons gives +2 attack.", "confidence": 0.9, "evidence": ["tech_iron_weapons_v1"], "actions": []}
    replies = [LLMReply(tool_calls=[{"name": "search_game_knowledge", "args": {"query": "What does Iron Weapons do?"}}]),
               LLMReply(text=json.dumps(plan))]
    _, used, log = run_agent(tmp_path, replies)
    assert used == ["search_game_knowledge"] and log[0]["result_count"] >= 1


def test_server_lists_exactly_seven_read_only_tools_over_stdio(tmp_path):
    import asyncio
    db = tmp_path / "s.sqlite3"
    store = SessionStore(db)
    sid = store.create_session()
    with StdioMcp(sid, db) as mcp:
        tools = asyncio.run(mcp.list_tools())
    assert len(tools) == 7
    assert all(t.annotations and t.annotations.read_only_hint for t in tools)


def test_gateway_stdio_transport_end_to_end(tmp_path, monkeypatch):
    from fastapi.testclient import TestClient
    from gateway.app import create_app
    db = tmp_path / "gw.sqlite3"
    monkeypatch.setenv("MCP_TRANSPORT", "stdio")
    monkeypatch.setenv("SESSION_DB_PATH", str(db))
    llm = ScriptedLLM([LLMReply(tool_calls=[{"name": "get_army_status", "args": {}}]), LLMReply(text=json.dumps(PLAN))])
    client = TestClient(create_app(SessionStore(db), llm, tmp_path))
    h = {"X-Session-Token": client.post("/session").json()["token"]}
    r = client.post("/ask", json={"snapshot": SNAP, "question": "q"}, headers=h)
    assert r.status_code == 200 and r.json()["tools_used"] == ["get_army_status"]
