"""Create a session with a fixture snapshot, for MCP Inspector screenshots and manual tests.

    python ai/mcp-server/make_session.py [fixture]        # fixture: early_economy (default), cavalry_rush, late_game
prints the command to start the Inspector against the real server (stdio) with that session.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "ai"))

from common.session_store import SessionStore  # noqa: E402

DB = ROOT / "ai" / "data" / "sessions.sqlite3"

if __name__ == "__main__":
    name = sys.argv[1] if len(sys.argv) > 1 else "early_economy"
    snapshot = json.loads((ROOT / "evaluation" / "fixtures" / f"{name}.json").read_text(encoding="utf-8"))
    store = SessionStore(DB)
    sid = store.create_session()
    store.save_snapshot(sid, snapshot)
    print(f"session id: {sid}\nsnapshot: {name} (saved in {DB})\n")
    print("Windows PowerShell:")
    print(f'  $env:RTS_SESSION_ID="{sid}"; $env:SESSION_DB_PATH="{DB}"; npx @modelcontextprotocol/inspector python ai/mcp-server/server.py')
    print("bash:")
    print(f'  RTS_SESSION_ID={sid} SESSION_DB_PATH={DB} npx @modelcontextprotocol/inspector python ai/mcp-server/server.py')
