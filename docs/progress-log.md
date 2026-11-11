# Progress log (newest first)

## 2026-11-11 (run 1, one-off)
- UNITY_FEEDBACK.md headings handled: none (no entries yet).
- Done (M4 part 1, redone after the lost push): `ai/requirements.txt`, `.env.example` additions, RAG retriever
  (BM25 + optional embeddings) and `build_index.py`, SQLite session store, 7-tool read-only MCP server
  (`ai/mcp-server/`), `docs/mcp-tools.md`, pytest suite.
- Tests: `python -m pytest -q ai` → 24 passed (mock-free; no LLM involved).
- Open questions: none. Known risks: embeddings/hybrid mode and the real MCP stdio run were not exercised here;
  `mcp` 2.x API (`MCPServer`) is used, so requirements pin `mcp>=2.3,<3`.
- Chunk 2: gateway (`ai/gateway/`): FastAPI `/session`, `/ask`, `/why`; snapshot schema validation (unknown fields
  rejected); agent loop (max 6 steps, 30 s, one repair retry) calling the MCP server in-process; provider-neutral
  `LLM` interface with `ScriptedLLM` (tests) and `GeminiLLM` (untested); JSONL tool log + security log; stale flag.
  Tests: `python -m pytest -q ai` → 33 passed.
- Chunk 3: Unity pure-rules code: script folders under `Assets/Scripts/`, `RTS.Core.asmdef`, `GameData` (JSON loader),
  `DamageCalculator`, `GameActionValidator`, EditMode tests `RulesTests.cs` + test asmdef. NOT compiled (no dotnet here).
- Chunk 4: M6 evaluation: 3 fixtures, 16 prompts, `evaluation/run_eval.py` (mock + `--real`), `security-tests.md`, tests. `pytest ai evaluation` → 35 passed. Mock accuracy 100% is by construction.
- Chunk 5: `AiDtos.cs`, `AiGatewayClient.cs` (uncompiled), `docs/architecture.md`.
- Risks: C# code never compiled; asmdef reference name `Unity.Nuget.Newtonsoft-Json` is from memory. `GeminiLLM` is untested against the real API; agent uses the MCP server object in-process, not a stdio client.
- Not started: Unity work, M3, M5, M6.
