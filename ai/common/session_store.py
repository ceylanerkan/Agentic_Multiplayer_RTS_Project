"""SQLite session store. The gateway writes snapshots; the MCP server only reads them.

Session ids are random tokens created by the gateway. The MCP server gets its session id from the
environment (RTS_SESSION_ID), never from a tool argument, so the LLM cannot pick another player's data.
"""
import json
import secrets
import sqlite3
import time
from pathlib import Path


class SessionStore:
    def __init__(self, path):
        if str(path) != ":memory:":
            Path(path).parent.mkdir(parents=True, exist_ok=True)
        self.db = sqlite3.connect(str(path), check_same_thread=False)
        self.db.execute("CREATE TABLE IF NOT EXISTS sessions (id TEXT PRIMARY KEY, created REAL)")
        self.db.execute("CREATE TABLE IF NOT EXISTS snapshots (session_id TEXT PRIMARY KEY, "
                        "received REAL, data TEXT)")

    def create_session(self):
        sid = secrets.token_urlsafe(24)
        self.db.execute("INSERT INTO sessions VALUES (?, ?)", (sid, time.time()))
        self.db.commit()
        return sid

    def session_exists(self, sid):
        return self.db.execute("SELECT 1 FROM sessions WHERE id=?", (sid,)).fetchone() is not None

    def save_snapshot(self, sid, snapshot):
        if not self.session_exists(sid):
            raise KeyError("unknown session")
        self.db.execute("INSERT OR REPLACE INTO snapshots VALUES (?, ?, ?)",
                        (sid, time.time(), json.dumps(snapshot)))
        self.db.commit()

    def get_snapshot(self, sid):
        """Returns (snapshot, age_seconds) or None."""
        row = self.db.execute("SELECT received, data FROM snapshots WHERE session_id=?", (sid,)).fetchone()
        return (json.loads(row[1]), time.time() - row[0]) if row else None
