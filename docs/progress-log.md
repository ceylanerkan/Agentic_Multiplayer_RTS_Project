# Progress log (newest first)

## 2026-11-19 (run 4, night 16 fix day)
- UNITY_FEEDBACK.md headings handled: "2026-11-18" (3 obsolete-API warnings, TMP NullReferenceException, Test 4: Play starts in Game,
  unreadable visuals and text, no target resolution).
- Fixes: `ZoneMap` uses `FindObjectsByType<T>()`, `RtsSetupTools` uses `FindAnyObjectByType`. Cause of the NRE: every generated TMP text
  had `m_fontAsset: {fileID: 0}` (checked in Game.unity); `RtsSetupTools.Label` now assigns LiberationSans SDF before the text.
  New `Editor/PlayFromMenu.cs` (`EditorSceneManager.playModeStartScene` = Menu, toggle `Tools > RTS > Play from Menu scene`).
  New command `Tools > RTS > Rebuild generated scenes and prefabs` (confirm dialog). New `UI/EntityVisuals.cs`, attached at run time by
  `NetworkUnit`/`NetworkBuilding`/`ResourceNode.Spawned`: team disc (blue mine / red enemy) + billboard label with name and HP / build % /
  amount, hidden together with the body by fog of war. Bigger units, distinct resource shapes, opaque UI panels, bigger fonts,
  `CanvasScaler.matchWidthOrHeight = 0.5`. Target resolution: 1920x1080 (16:9), 1280x720 for two windows.
- Review chunk: the selection ring was placed at the object's pivot (its centre, lifted by `StandOnGround`), i.e. hidden inside the
  capsule; now at the feet and sized from the collider, just below the team disc. A building-placement click also deselected the
  Villagers, and a right-click that cancels placement also moved the units: `BuildPlacement.OwnsClick` now guards
  `SelectionManager` and `UnitCommandInput`. Gather loop and numbers re-checked against `rules.md` (no change needed).
- Readability: `SelectionPanel` buttons get an explicit font, 18 px text, display names ("Raider Boat" instead of `raider_boat`) in
  buttons and the info text; HUD shows "waiting for player 2..." while the host is alone. C# rule tests (throwaway net8.0 + NUnit): 47/47 pass; later 48/48 with the new `DisplayNames` test (core helper used by labels and buttons).
- `RtsCamera`: WASD no longer pans while a TMP input field is focused (typing a question moved the camera); mouse wheel accepts both
  120-per-notch and normalised (~1) scroll values (with /120 only, a normalised value is below the 0.01 threshold and zoom is dead).
  `AiCommanderPanel`: answer and status texts auto-size (min 12) instead of overflowing the panel.
- Resource nodes are lifted by half their mesh height (fixed y = 0.5 buried most of a tree); placement ghost has the real building's size
  (`GameSpawner.BuildingSize`). `docs/setup.md`: Play from Menu, resolution, rebuild command.
- Independent review (read-only subagent), 8 findings; fixed after checking each against the code:
  (1) unfinished buildings were left out of `VisibilityAdapter.Collect`, so fog of war hid the player's own building sites (and
  enemy sites were never visible): now listed with Los 0 (+ EditMode test, 49/49); (2) `VillagerGatherer.Busy` was host-only, so
  P2's AI snapshot showed gathering Villagers as idle: `[Networked] BusyNet`; (3) builders read a despawned site in the same frame:
  `SiteAlive()` checks `Object.IsValid`; (4) the same technology could be queued and paid in two buildings: host checks every own
  queue; (6) ENEMY_SIGHTED re-armed instantly at the edge of sight and flooded the event log: re-armed after 10 s out of sight;
  (7) start Villagers / trained units spawned on the Town Center's corner (maybe its roof): offset by the collider half size + 1.5.
  Also: Menu session field had the AI panel's placeholder; report "Lessons learned" gained the editor play-test lesson.
  (5) a unit ordered to attack across water (or an archer vs. a ship out of range) froze re-ordering the blocked chase and stopped
  auto-attacking: `NetworkUnit.BlockedByShore` (host only, reset by `MoveTo` / new `Attack`) makes `UnitCombat` drop the order;
  (8) `TechBonus.TechsOf` caches one set per tech mask instead of a new HashSet per unit per tick. Not changed: FogOfWar's
  `GetComponentsInChildren` every 0.25 s (small) and the no-BuildSlot fallback in `CommandRelay` (the generated scene always has slots).
- Tests: `tools/compile-check` (stubs extended for the new APIs): 0 errors, 0 warnings.
- Open questions: none.
- Risks: `EntityVisuals` and `PlayFromMenu` are uncompiled in real Unity; the rebuild deletes hand edits in the generated scenes/prefabs.

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
- Night 10 security: `test_security.py` (S10-S14), agent now rejects undeclared tool arguments (`player_id`, `session_id`),
  `evaluation/security-tests.md` updated (S10-S16). `pytest ai evaluation` -> 43 passed.
- Night 11 chunk: `ai/gateway/mcp_stdio.py` (`StdioMcp`: server as subprocess, real MCP over stdio, same two calls as the in-process
  server); gateway option `MCP_TRANSPORT=stdio`; `test_mcp_stdio.py` (compound question calls 4 tools / 3 distinct,
  knowledge-only calls only `search_game_knowledge`, 7 read-only tools, gateway end to end). `evaluation/plan_checks.py`
  (Python copy of `GameActionValidator`) + tests, 5 new prompts (stale, invalid and valid actions), `run_eval.py` now reports
  invalid-action rate and stale-confidence rate and writes `results.csv` and `results_table.md`.
  `pytest ai evaluation` -> 53 passed. Mock numbers are by construction; the live Gemini run is a [You] task.
- Night 12 chunk: `DomainRules` (core) + 4 tests (45/45 pass): melee stays in its domain, ranged shoots across, Dock hit from both;
  catapult cooldown 3.0 s (`GameConstants.CooldownFor`, small decision); `UnitCombat` uses domain rules and cooldown;
  `NetworkUnit` movement stops at the shore (zone map decides land/water; simple replacement for per-agent-type NavMesh);
  ships spawn in the own harbor zone. Fog of war already works on the sea (circle sight, no terrain).
- Nights 13-15: `docs/report/report.md` (all sections, `[TODO live]` placeholders), networking section in `docs/architecture.md`,
  `docs/demo-script.md`, `docs/slides-outline.md`, `docs/defense-notes.md`.
- Review: `tools/compile-check` compiles the Unity scripts against stub Unity/Fusion/TMP types (0 errors; found `units[i].Id`).
  Bug fixed: `GameSpawner.StateOf` only worked on the host, so fog of war and the AI snapshot on a client would have seen no objects;
  `NetworkPlayerState.All` registry added. `TODO.md` and README status rewritten.
- Editor automation: `Editor/RtsSetupTools.cs` (menu: create placeholder prefabs; set up the open Game scene with map, zones, slots, resources,
  spawner, systems, camera; create the Game UI with all fields assigned; command 0 creates the Menu and Game scenes, fills the Build Settings and runs the rest). Compiles against the stubs. `.env` is now loaded by the gateway and `run_eval.py` (it was not before).
- `GameSpawner.StandOnGround`: spawned objects are lifted by their collider half height (capsules no longer half in the floor).
- Fix: `RPC_Event` sent three `NetworkString`s (> 512 byte RPC limit); now sends type and zone as indices (`PlayerEventLog.TypeList`) and a 31 character detail. C# tests: 46/46.
- Gateway: LLM or network errors now give HTTP 502 with only the exception type (no key leak); right-click help-build; null guards in the AI panel.
  `docs/design-brief.md`: one sentence now says Unity reads the JSON at run time (ScriptableObjects were replaced by `GameDataProvider`). `pytest ai evaluation` -> 56 passed.
- System prompt now lists the allowed action types, unit/building/tech names, groups and zones (generated from the schema) plus an example plan, so the live model is less likely to invent values. `pytest ai evaluation` -> 57 passed.
- `RtsCamera` focuses once on the local player's Town Center (player 2 would otherwise start looking at player 1's base).
- Independent review (read-only subagent) found and I fixed: `Shutdown()` destroyed the DontDestroyOnLoad launcher (now `destroyGameObject: false`),
  reading `[Networked]` values of just-despawned objects after a kill / depleted node (`TakeDamage` and `Take` now return a bool), destroyed
  buildings never freeing their slot (y offset), queued units keeping reserved population, stale static event log / `CommandRelay.Local`,
  `TechIndex` returning 0 for unknown keys, UNDER_ATTACK RPC spam (5 s throttle). Possible missed `PlayerJoined` (scene loads after joining): `GameSpawner.Update` now spawns a base for any active player without one.
- `BuildSlot.occupied` now also looks at the networked buildings, so the ghost and the Dock button are correct on clients (review finding 9).
- Added an opening-build-order scenario test (47/47 C# tests). Fusion member names verified against `Fusion.Runtime.xml` in the repo.
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
