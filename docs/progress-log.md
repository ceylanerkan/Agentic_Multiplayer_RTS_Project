# Progress log (newest first)

## 2026-11-11 (run 1, one-off)
- UNITY_FEEDBACK.md headings handled: none (no entries yet).
- Done (M4 part 1, redone after the lost push): `ai/requirements.txt`, `.env.example` additions, RAG retriever
  (BM25 + optional embeddings) and `build_index.py`, SQLite session store, 7-tool read-only MCP server
  (`ai/mcp-server/`), `docs/mcp-tools.md`, pytest suite.
- Tests: `python -m pytest -q ai` → 24 passed (mock-free; no LLM involved).
- Open questions: none. Known risks: embeddings/hybrid mode and the real MCP stdio run were not exercised here;
  `mcp` 2.x API (`MCPServer`) is used, so requirements pin `mcp>=2.3,<3`.
- Not started: gateway/agent loop, Unity work.
