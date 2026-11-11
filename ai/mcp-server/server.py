"""MCP server: 7 read-only tools. Run: RTS_SESSION_ID=<id> python ai/mcp-server/server.py (stdio).

The session id comes from the environment (set by the gateway), never from tool arguments.
"""
import os
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "rag"))

from mcp.server.mcpserver import MCPServer  # noqa: E402
from mcp.types import ToolAnnotations  # noqa: E402

from common.session_store import SessionStore  # noqa: E402
from retriever import Retriever  # noqa: E402
from tools import game_tools as t  # noqa: E402

READ_ONLY = ToolAnnotations(readOnlyHint=True, destructiveHint=False, idempotentHint=True, openWorldHint=False)


def build_server(store, session_id, retriever=None):
    retriever = retriever or Retriever()
    mcp = MCPServer("rts-commander-tools")

    def ctx():
        row = store.get_snapshot(session_id)
        return t.ToolContext(*(row if row else (None, 0)), retriever)

    @mcp.tool(annotations=READ_ONLY)
    def get_player_resources() -> dict:
        """Current wood/food/gold, population used/capacity and age of the player."""
        return t.get_player_resources(ctx())

    @mcp.tool(annotations=READ_ONLY)
    def get_army_status() -> dict:
        """The player's own units (by type and zone) and buildings."""
        return t.get_army_status(ctx())

    @mcp.tool(annotations=READ_ONLY)
    def get_visible_enemies() -> dict:
        """Enemy units/buildings the player can currently see (fog of war applies)."""
        return t.get_visible_enemies(ctx())

    @mcp.tool(annotations=READ_ONLY)
    def get_technology_status() -> dict:
        """Researched and in-progress technologies."""
        return t.get_technology_status(ctx())

    @mcp.tool(annotations=READ_ONLY)
    def get_recent_events(limit: int = 10) -> dict:
        """Most recent game events the player knows about (limit 1-20)."""
        return t.get_recent_events(ctx(), limit)

    @mcp.tool(annotations=READ_ONLY)
    def estimate_plan_cost(items: list[dict]) -> dict:
        """Total cost of items [{kind: unit|building|technology, id: key, count: 1-50}] vs current resources."""
        return t.estimate_plan_cost(ctx(), items)

    @mcp.tool(annotations=READ_ONLY)
    def search_game_knowledge(query: str, top_k: int = 3) -> dict:
        """Search unit/building/tech/rule knowledge. Returns top_k (1-5) chunks with source_id."""
        return t.search_game_knowledge(ctx(), query, top_k)

    return mcp


if __name__ == "__main__":
    sid = os.environ.get("RTS_SESSION_ID")
    if not sid:
        sys.exit("RTS_SESSION_ID is not set")
    build_server(SessionStore(os.environ.get("SESSION_DB_PATH", "ai/data/sessions.sqlite3")), sid).run()
