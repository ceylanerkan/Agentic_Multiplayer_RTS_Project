import sys
from pathlib import Path

AI = Path(__file__).resolve().parents[2]
for p in (AI, AI / "mcp-server", AI / "rag"):
    sys.path.insert(0, str(p))
