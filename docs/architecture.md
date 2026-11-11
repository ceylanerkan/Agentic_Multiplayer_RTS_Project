# Architecture (implemented parts)

```
Unity (host/client, Fusion 2)           Python (ai/)
 VisibilityService.FilterFor  ──► AiSafeSnapshot ──HTTP POST /ask──► gateway (FastAPI)
 AiCommanderPanel ◄── AiAnswer ◄───────────────────────────────────  │ validates snapshot schema (unknown fields rejected)
 Approve ► GameActionValidator ► CommandRelay RPC ► host authority     │ saves it in SQLite under the session token
                                                                       ▼
                                                    Agent loop (≤6 steps, 30 s) ◄─► LLM (Gemini / scripted)
                                                                       │ tool calls
                                                                       ▼
                                                    MCP server: 7 read-only tools ─► SQLite snapshot, RAG retriever
```

| Part | Files |
|---|---|
| Knowledge base (single source of numbers) | `ai/rag/knowledge/*.json`, `rules.md` |
| Retriever (BM25 + optional embeddings) | `ai/rag/retriever.py`, `build_index.py` |
| Session store | `ai/common/session_store.py` |
| MCP tools | `ai/mcp-server/server.py`, `tools/game_tools.py`; docs in `docs/mcp-tools.md` |
| Gateway, agent, logs | `ai/gateway/` (`app.py`, `agent.py`, `llm.py`, `logs.py`) |
| Evaluation | `evaluation/` |
| Unity rules (engine-free) | `UnityRTS/Assets/Scripts/Core/Rules/` |

Security boundaries: see [design-brief.md §4](design-brief.md). The session token is held only by Unity and the
gateway; the MCP server receives its session id from the environment of the gateway, and no tool takes a player id.
Plans are validated by JSON Schema in the gateway (`jsonschema`, one repair retry), by `GameActionValidator` in Unity
at approval time, and by the host in the RPC handler.
