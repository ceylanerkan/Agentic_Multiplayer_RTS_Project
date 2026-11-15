# Progress log (newest first)

## 2026-11-15 (run 3, night 3)
- UNITY_FEEDBACK.md headings handled: "2026-11-14" (the `Start()` rename was already fixed on main; no other
  Unity-message method names with parameters in the scripts; compile and RulesTests 13/13 confirmed by the student).
- Night 3 chunk: `ZoneLookup` + `GatherState` (core, tested), `ZoneVolume`, `ZoneMap`, `BuildSlot`, `ResourceNode`,
  `VillagerGatherer`, `CommandRelay.SendGather`, build slot choice on the host, coastal slots feed the Dock rule.
- Tests: throwaway net8.0 project (dotnet from apt): 19/19 pass (6 new: zones, gather, capacity, pop cap).
- Night 4 chunk: `FormationLayout` (core, tested), `RtsCamera`, `SelectionManager`, `UnitCommandInput`,
  `CommandRelay.Local` + `SendAttack` (stores `NetworkUnit.AttackTarget`, combat uses it on night 6). Tests: 20/20 pass.
- Night 5 chunk: `ProductionQueue` + `ConstructionState` (core, tested), `NetworkBuilding` now has construction progress
  and a queue (units and research), `CommandRelay` train/research/build RPCs use them, Villagers build
  (`VillagerGatherer.StartBuild`), `BuildPlacement` ghost, `HudUI`. Fixed bug: building HP was read as 0 (hp is
  top-level in buildings.json); unit speed now comes from units.json. Tests: 25/25 pass.
- Night 6 chunk: `UnitCombat` (explicit/auto targets, cooldown, damage via `DamageCalculator`), `TechBonus` (Iron Weapons),
  `TakeDamage` on units/buildings (death frees pop/slot/capacity), `SelectionPanel` (info + buttons made from game data,
  disabled with the validator's reason). Small decisions: attack cooldown 1.0 s, reach = range + 1.2 (unit) / +3 (building).
- Night 7 chunk: `TechEffects` + `WinCondition` (core, tested), `TechBonus` adapter, Fletching range in `UnitCombat`,
  `MatchEnd` (host sets `NetworkPlayerState.Result` 1/2 when the last Town Center dies), `GameOverUI`. Scout needs only a
  prefab (key `scout`, uses `UnitCombat`); `DefRecord.LineOfSight` added for fog. Tests: 29/29 pass.
- Night 8 chunk: `VisibilityService` (+ `MapObject`) and `PlayerEventLog` (core, 3 new tests, 32/32 pass), `VisibilityAdapter`,
  `FogOfWar` (hides enemy renderers, dark overlay texture, ENEMY_SIGHTED), `GameEvents` (host decides, RPC `RPC_Event`
  delivers to the owner so each peer holds only its own player's log), hooks for the other 7 event types.
- Night 9/10 chunk: `SnapshotAssembler` (core), `AiSnapshotBuilder` + export menu, `AiCommanderPanel`, `ActionCosts`,
  `ApprovalEvaluator` + `DecisionLog` (core), `AiApprovalFlow` (re-validate, send via CommandRelay, log to
  decision-log.jsonl), validator rule "no building in the enemy base" (`PlayerState.Slot`) + domain tests.
  C#: 41/41 pass. Python: `pytest ai evaluation` 38 passed (new `test_snapshot_contract.py`, sample exported by the C# test).
  Small decision: Approve executes the valid actions and skips the invalid ones, reporting why.
- Risks: all new MonoBehaviour/NetworkBehaviour code is uncompiled (needs Unity).

## 2026-11-12 (run 2, night 1)
- UNITY_FEEDBACK.md headings handled: "11/11/2026" (build_index + pytest passed, nothing to fix).
- Compiled `Assets/Scripts/Core` + `RulesTests` in a throwaway net8.0 project (dotnet from apt; dot.net is blocked): no
  errors. Tests: 12/12 pass (10 old + 2 new PlayerSlots tests). `AiGatewayClient.cs` still uncompiled (UnityWebRequest).
- Added `GameDataProvider`, `KnowledgeBuildCopy` (editor build step), `PlayerSlots` (core), `GameLauncher`, `MenuUI`.
- Night 2 code (uncompiled, Unity needed): `GameConstants` (+test, 13/13 pass), `NetworkPlayerState`, `NetworkUnit`, `NetworkBuilding`, `CommandRelay` (validator-checked RPCs), `GameSpawner`.
- Risks: Fusion callbacks/`StartGameArgs` written from the repo's FusionBootstrap and are unverified in Unity.

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
