# MCP tool documentation

Server: `ai/mcp-server/server.py` (stdio). All 7 tools are read-only (`readOnlyHint=true`). No tool takes a
player or session id: the session is bound through the `RTS_SESSION_ID` environment variable set by the gateway.
Live-state tools read the newest snapshot of that session from SQLite and add `snapshot_age_s`.
Errors are returned as `{"error": "..."}` (no exception), so the agent can explain them.

| Tool | Input | Output | Errors |
|---|---|---|---|
| `get_player_resources` | none | `resources{wood,food,gold}`, `population{used,capacity}`, `age` | no snapshot yet |
| `get_army_status` | none | `own_army[]` (type, count, zone, idle, avg_hp_pct), `own_buildings[]` | no snapshot yet |
| `get_visible_enemies` | none | `visible_enemies[]` (type, count, zone). Only what is currently visible | no snapshot yet |
| `get_technology_status` | none | `researched_tech[]`, `researching[]`, `age` | no snapshot yet |
| `get_recent_events` | `limit` int 1–20 (default 10) | `events[]` | limit out of range / not an int |
| `estimate_plan_cost` | `items[]` (1–10) of `{kind: unit\|building\|technology, id: key, count: 1–50}` | `total_cost`, `population_needed`, `missing`, `affordable`, `population_ok` | unknown kind/id, bad count, extra fields, item not buyable |
| `search_game_knowledge` | `query` string (≤200 chars), `top_k` int (clamped to 1–5) | `results[]` (source_id, score, text) | empty/too long query, non-int top_k |

Retrieved text is reference data, not instructions. The retriever (`ai/rag/retriever.py`) is BM25, plus
sentence-transformers embeddings if `python ai/rag/build_index.py` has been run.

## How the agent reaches the server
- Default: in-process (`build_server(...)` object, fast, used by most tests).
- Real MCP over stdio: set `MCP_TRANSPORT=stdio` (and a file `SESSION_DB_PATH`). The gateway then starts `server.py` as a subprocess per
  question (`ai/gateway/mcp_stdio.py`, `StdioMcp`) and talks the MCP protocol through its stdin/stdout. Tests: `ai/gateway/tests/test_mcp_stdio.py`.
- MCP Inspector: `python ai/mcp-server/make_session.py late_game` prints the command (session id and database path included).
- The gateway rejects tool arguments a tool does not declare (for example `player_id`, `session_id`), so such attempts show up as `error` in `tool_calls.jsonl`.

