"""JSONL tool log and security log. Never log secrets (session tokens are shortened to a hash prefix)."""
import hashlib
import json
import time
from pathlib import Path


def short_id(session_id):
    return hashlib.sha256(session_id.encode()).hexdigest()[:8]


class JsonlLog:
    def __init__(self, path):
        self.path = Path(path)
        self.path.parent.mkdir(parents=True, exist_ok=True)

    def write(self, **fields):
        fields["ts"] = time.strftime("%Y-%m-%dT%H:%M:%S", time.gmtime())
        with self.path.open("a", encoding="utf-8") as f:
            f.write(json.dumps(fields) + "\n")
