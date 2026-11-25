"""Evidence for the report: every MCP tool with a valid and an invalid input, over the real MCP protocol.

Starts ai/mcp-server/server.py as a subprocess (stdio, like MCP_TRANSPORT=stdio) with the late_game fixture and calls the
tools the way the agent does, so the log has the same fields as ai/logs/tool_calls.jsonl. No LLM is involved.
Run: python evaluation/mcp_calls.py        Outputs: mcp_calls.jsonl and mcp_calls_table.md next to this file
"""
import asyncio
import json
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
for p in (ROOT / "ai", ROOT / "ai" / "mcp-server"):
    sys.path.insert(0, str(p))

from common.session_store import SessionStore  # noqa: E402
from gateway.agent import Agent  # noqa: E402
from gateway.logs import JsonlLog  # noqa: E402
from gateway.mcp_stdio import StdioMcp  # noqa: E402

CALLS = [   # (tool, arguments, what is wrong with them; "" = valid)
    ("get_player_resources", {}, ""),
    ("get_player_resources", {"player_id": 2}, "asks for another player"),
    ("get_army_status", {}, ""),
    ("get_army_status", {"session_id": "abc"}, "asks for another session"),
    ("get_visible_enemies", {}, ""),
    ("get_visible_enemies", {"include_hidden": True}, "asks for hidden enemies"),
    ("get_technology_status", {}, ""),
    ("get_technology_status", {"player": "P2"}, "asks for the opponent"),
    ("get_recent_events", {"limit": 5}, ""),
    ("get_recent_events", {"limit": 99}, "limit out of range"),
    ("estimate_plan_cost", {"items": [{"kind": "unit", "id": "archer", "count": 3}]}, ""),
    ("estimate_plan_cost", {"items": [{"kind": "unit", "id": "dragon", "count": 1}]}, "unknown unit"),
    ("estimate_plan_cost", {"items": [{"kind": "unit", "id": "archer", "count": 0}]}, "count out of range"),
    ("search_game_knowledge", {"query": "What counters cavalry?", "top_k": 3}, ""),
    ("search_game_knowledge", {"query": "x" * 300}, "query too long"),
    ("search_game_knowledge", {"query": 42}, "wrong type"),
    ("reveal_enemy_gold", {}, "tool does not exist"),
]


def main():
    log_path = HERE / "mcp_calls.jsonl"
    log_path.unlink(missing_ok=True)   # one run per file
    rows, unexpected = [], 0
    with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as tmp:
        db = Path(tmp) / "sessions.sqlite3"
        store = SessionStore(db)
        sid = store.create_session()
        store.save_snapshot(sid, json.loads((HERE / "fixtures" / "late_game.json").read_text(encoding="utf-8")))
        with StdioMcp(sid, db) as mcp:
            asyncio.run(mcp.call_tool("search_game_knowledge", {"query": "warm up"}))   # not logged: loads the embedding model
            agent = Agent(None, mcp, JsonlLog(log_path))
            for tool, args, wrong in CALLS:
                agent._call_tool("mcp_calls", tool, args)
                entry = json.loads(log_path.read_text(encoding="utf-8").splitlines()[-1])
                if (entry["status"] == "error") != bool(wrong):
                    unexpected += 1
                rows.append((tool, args, wrong, entry))
    lines = ["| Tool | Input | Case | Status | ms | Refusal |", "|---|---|---|---|---|---|"]
    for tool, args, wrong, e in rows:
        text = json.dumps(args)
        text = text if len(text) <= 70 else text[:67] + "..."
        refusal = " ".join((e["validation_error"] or "-").split())[:110].replace("|", "/")
        lines.append(f"| {tool} | `{text}` | {wrong or 'valid'} | {e['status']} | {e['duration_ms']} | {refusal} |")
    (HERE / "mcp_calls_table.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    print(f"{len(rows)} calls, {unexpected} unexpected result(s)")


if __name__ == "__main__":
    main()
