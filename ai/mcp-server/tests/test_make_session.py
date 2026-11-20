"""make_session.py (used for the MCP Inspector screenshots) stores the fixture and prints a command with that session."""
import json
import os
import re
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "ai"))
from common.session_store import SessionStore  # noqa: E402


@pytest.mark.parametrize("fixture", ["early_economy", "cavalry_rush", "late_game"])
def test_session_is_created_with_the_fixture(tmp_path, fixture):
    db = tmp_path / "sessions.sqlite3"
    env = dict(os.environ, SESSION_DB_PATH=str(db))
    out = subprocess.run([sys.executable, str(ROOT / "ai/mcp-server/make_session.py"), fixture],
                         capture_output=True, text=True, env=env, cwd=ROOT, check=True).stdout
    sid = re.search(r"session id: (\S+)", out).group(1)
    assert f"RTS_SESSION_ID={sid}" in out and "ai/mcp-server/server.py" in out
    snapshot, age = SessionStore(db).get_snapshot(sid)
    assert snapshot == json.loads((ROOT / f"evaluation/fixtures/{fixture}.json").read_text(encoding="utf-8"))
    assert age < 60
