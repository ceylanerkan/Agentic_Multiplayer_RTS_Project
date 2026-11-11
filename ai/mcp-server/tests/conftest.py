import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
for p in (ROOT / "mcp-server", ROOT, ROOT / "rag"):
    sys.path.insert(0, str(p))
