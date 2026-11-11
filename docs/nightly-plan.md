# Nightly plan

The autopilot agent works through this plan one night at a time. Each night is about one hour of agent work. The
date is the UTC date of the run; the run starts at about 03:00 Istanbul time on the same date.

How the agent uses this plan (the full rules are in [autopilot.md](autopilot.md)):
- It always starts with the **earliest night that still has an unchecked item**, even if that night's date has
  passed.
- When tonight's items are done and time is left, it continues with the next night's items. It never stops early.
- It marks finished items `[x]` with the run date, for example `- [x] ... (done 2026-11-12)`.
- "You" lines are the student's daytime work after merging the pull request. The agent does not tick them. It turns
  them into exact **[You]** steps in `MORNING.md` and `TODO.md`.
- `TODO.md` and `docs/design-brief.md` have the details and the numbers for each item.

Done before this plan started (2026-11-11):
- M4 Python: RAG retriever, the 7-tool MCP server, the session store, the gateway with its agent loop, and logs.
- M6: the evaluation runner in mock mode.
- Engine-free Unity rules (`GameData`, `DamageCalculator`, `GameActionValidator`, `AiDtos`, `AiGatewayClient`) and
  EditMode tests. None of this C# has been compiled yet.

Small decision already taken: gameplay and networking scripts go under `Assets/Scripts/<folder>/` **without** an
asmdef. They compile into `Assembly-CSharp`, which is already in Fusion's `AssembliesToWeave` list. Only the
engine-free `RTS.Core` keeps its asmdef.

## Night 1 – 2026-11-12: check last night's C#, load the game data, start a match
- [ ] Install the .NET SDK (autopilot.md section 3). Compile `Assets/Scripts/Core` and run `RulesTests` with NUnit
      in a throwaway project outside the repository, and fix every error.
- [ ] `GameDataProvider`: load `ai/rag/knowledge/*.json` into `GameData` at runtime. In the editor it reads the
      repository folder. For builds, an `IPreprocessBuildWithReport` step copies the files to `StreamingAssets`.
- [ ] `GameLauncher` and the menu UI script: Host or Join with a session name, `NetworkRunner.StartGame` with
      `GameMode.Host` or `GameMode.Client`, loading the Game scene, P1/P2 assignment. When the other player
      disconnects, the match ends and both players return to the menu.
- You: email the instructor about the one-person team if you have not done it yet. Delete the URP sample content,
  create the `Menu` and `Game` scenes, add them to Build Settings, and set up the launcher in `Menu`.

## Night 2 – 2026-11-13: networked state and commands
- [ ] `PlayerState`: host-owned `[Networked]` resources, population used and capacity, age, researched technologies.
- [ ] `NetworkUnit` and `NetworkBuilding`: owner, type key, `[Networked]` HP, and position through
      `NetworkTransform`. Spawn each player's Town Center and 3 Villagers when the player joins.
- [ ] `CommandRelay` RPCs (InputAuthority to StateAuthority) for move, attack, gather, build, train and research.
      Before acting, the host checks ownership, cost, population and prerequisites with `RTS.Core`.
- You: placeholder prefabs (capsules for units, cubes for buildings) with `NetworkObject`, then a two-instance
  smoke test with ParrelSync.

## Night 3 – 2026-11-14: map and economy
- [ ] `ZoneMap`: maps a world position to one of the 10 zone names, so that every point belongs to a zone.
      `BuildSlot` per zone, with a coastal flag.
- [ ] `ResourceNode` (finite, networked amount, despawned when empty) and the Villager gather loop on the host.
- [ ] Population capacity from Houses and the Town Center, the hard cap, and training blocked at the cap.
- You: build the map (ground, 7 land and 3 water zone volumes, resource nodes, build slots) and bake the NavMesh.

## Night 4 – 2026-11-15: player control
- [ ] Selection: click, drag box, shift to add, and a highlight.
- [ ] Right-click commands (move, gather, attack, build), sent through `CommandRelay`.
- [ ] RTS camera (pan and zoom) with the Input System, and group movement with formation offsets.

## Night 5 – 2026-11-16: buildings and production
- [ ] Building placement ghost (valid or invalid on build slots) and Villager construction with build time.
- [ ] Production queue with train time, and the research queue. The Town Center cannot train while it researches
      Age II.
- [ ] HUD: resources, population, age.

## Night 6 – 2026-11-17: UI panels and combat
- [ ] Selection panel (info, HP), build menu, train and research buttons with cost and disabled state, queue
      progress.
- [ ] Combat on the host with `DamageCalculator`: cooldown, range, counters, death and despawn. Targets: explicit
      attack, auto-attack in range while idle, and buildings.
- You: two-instance test of gather, build, train and fight. Record a short clip as M3 evidence.

## Night 7 – 2026-11-18: technologies, Scout, win condition
- [ ] Technology effects: Age II unlocks, Iron Weapons, Fletching, Horse Collar, for existing and future units.
- [ ] Scout unit.
- [ ] Win condition: when a Town Center is destroyed, the match ends over the network and both players see a
      Victory or Defeat screen.

## Night 8 – 2026-11-19: fog of war and events
- [ ] `VisibilityService.FilterFor(player)`: a pure core in `RTS.Core` with EditMode tests, plus a Unity adapter
      that hides enemy renderers outside line of sight and draws a simple fog overlay.
- [ ] Recent-event log per player (the 8 event types, with zone and time), holding only what the player may know.

## Night 9 – 2026-11-20: AI in Unity
- [ ] `AiSnapshotBuilder`: own units and buildings grouped by type and zone, enemies only from `FilterFor`. Add a
      snapshot export, and a pytest contract test that validates an exported snapshot against the schema.
- [ ] `AiCommanderPanel`: question, loading state, summary, actions with costs, confidence, evidence, and Approve,
      Reject and Ask why buttons. Read-only answers need no approval.

## Night 10 – 2026-11-21: approval flow and security
- [ ] Approval: run `GameActionValidator` again on the current state and send approved actions through
      `CommandRelay`. Reject changes nothing. Decision log: plan id, decision, validator result per action.
- [ ] Domain checks in `GameActionValidator` (land and water zones, Dock only on your own coastal slot), with
      EditMode tests.
- [ ] Security tests, in pytest where possible: hidden extra field, identity switch, oversized `top_k`, injected
      knowledge chunk, invalid plans. Update `evaluation/security-tests.md`.

## Night 11 – 2026-11-22: real MCP client and evaluation numbers
- [ ] Run the agent loop through a real stdio MCP client, with the server as a subprocess. Test it with the
      scripted LLM: a compound question calls at least 3 tools, and a knowledge-only question calls only
      `search_game_knowledge`.
- [ ] `prompts.jsonl`: add a stale-state case and an invalid-action case, and check the category mix.
- [ ] Invalid-action rate through a Python copy of the gameplay checks, plus CSV export and a summary table.
- You: live Gemini run (`run_eval.py --real`), and keep the results.

## Night 12 – 2026-11-23: siege and naval (the first to cut if behind)
- [ ] Catapult: range, slow rate of fire, bonus against buildings.
- [ ] Ships: Dock on a coastal slot, Raider Boat and Warship, a water-only NavMesh agent type, the domain rule in
      combat, and fog of war on the sea.

## Night 13 – 2026-11-24: report, part 1
- [ ] `docs/report/report.md` in IEEE structure: Abstract, Introduction, Game Design, System Architecture,
      Multiplayer Method, MCP and Agent Design, RAG Method. Add the networking part to `docs/architecture.md`.

## Night 14 – 2026-11-25: report, part 2
- [ ] Structured Actions and Human Control, Security (with the client-side fog-of-war limitation), Evaluation
      (tables, with placeholders where live numbers are missing), Challenges and Lessons Learned, Conclusion and
      Future Work, Appendix, and the dependency list.
- You: Windows build (IL2CPP), and two instances on the demo machine.

## Night 15 – 2026-11-26: demo and defense material
- [ ] `docs/demo-script.md`: click-by-click steps for the 9 required scenarios (listed below), with what to say.
- [ ] `docs/slides-outline.md` and `docs/defense-notes.md`: authority model, 3 MCP tools, the RAG pipeline, the
      validator, one security test.
- You: record the backup video while following the demo script.

## Night 16 – 2026-11-27: fix day
- [ ] Fix everything open in `UNITY_FEEDBACK.md`, then review the code for bugs and missing tests.
- You: slides.

## Night 17 – 2026-11-28: final consistency pass
- [ ] `docs/setup.md` works from a clean clone, the repository has no secrets, and the README status, the report
      numbers and `TODO.md` match the code.

## Night 18 – 2026-11-29: last run
- [ ] Only fixes from `UNITY_FEEDBACK.md`. `MORNING.md` ends with a submission checklist.
- You: submit on 2026-11-30.

## When the plan is finished early
Work through these in order until the hour is over:
1. Open code items in `TODO.md` that are not in this plan.
2. Review the existing code for bugs and add missing tests (Python and EditMode).
3. Make the **[You]** steps shorter and more exact.
4. Improve the report draft.

## The 9 required demo scenarios (assignment section 13)
1. Multiplayer RTS: two instances join one match. Create units, move them, and show synchronised combat or another
   shared state change.
2. Resource and production: gather resources, build or use a production building, and train a unit.
3. Dynamic MCP tool calling: one compound strategy question that calls at least 3 MCP tools. Show the tool log.
4. RAG: a game-knowledge question that shows the retrieved source and chunk ids, then a strategy question that
   combines RAG with live state.
5. Structured output: Unity shows a schema-valid ActionPlan it received.
6. Human approval: approve one plan and reject another. The rejection changes nothing.
7. Validation failure: GameActionValidator rejects an invalid plan.
8. Fog-of-war security: try to get hidden opponent information, and show that the AI cannot access it.
9. Evaluation: the metrics table and at least one observed failure or limitation.
