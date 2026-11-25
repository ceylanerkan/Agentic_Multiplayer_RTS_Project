import os
import sys
from pathlib import Path

os.environ["TOOL_ROUTER"] = "off"   # wins over the developer's .env: the tests must not load Laya
AI = Path(__file__).resolve().parents[2]
for p in (AI, AI / "mcp-server", AI / "rag"):
    sys.path.insert(0, str(p))
