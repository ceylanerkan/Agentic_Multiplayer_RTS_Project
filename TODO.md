# TODO: remaining work

This file lists only the work that is still open. Milestone overview and status: [README.md](README.md#status).
All code for nights 1-12 is written (updated 2026-11-15); what is left is editor work, play testing, live runs and
the final deliverables. Scripts are in `UnityRTS/Assets/Scripts/`; rule code is compiled and tested with the .NET SDK
(45 C# tests), the Unity-specific scripts are only checked against stubs (`tools/compile-check`), so expect small
compile errors the first time you open them: paste them into UNITY_FEEDBACK.md.

## Open decisions
- [ ] Get written instructor approval for the one-person team (assignment asks for 2 students). Mention it in the report.

## [You] 1. One command for scenes, prefabs, map and UI
- [ ] Delete `Assets/TutorialInfo` and `Assets/Readme.asset` (URP sample content).
- [ ] Menu `Tools > RTS > 0 Create Menu and Game scenes` (needs `Assets/Scenes/Menu.unity` and `Game.unity` not to exist yet). It creates:
      - `Menu`: `Launcher` (`GameLauncher`), canvas with session name field, Host, Join, status text and `MenuUI`, all assigned;
      - `Game`: everything of commands 1-3 below;
      - Build Settings: Menu first, Game second.
      Check the Console afterwards; paste errors into UNITY_FEEDBACK.md. If you prefer to do it in steps, use commands 1-3 yourself.
- [ ] Test: open `Menu`, Play with a ParrelSync clone: A = Host, B = Join, same session name; both load `Game`; closing the clone returns the other
      to `Menu` with a message.

### What the commands do (for reference or step by step)
- [ ] `Tools > RTS > 1 Create placeholder prefabs`: makes `Assets/Prefabs` with `Player`, 8 unit prefabs (capsules), 6 building prefabs
      (cubes) and `Tree` / `BerryBush` / `GoldMine` (all with `NetworkObject` and the right scripts). If Fusion's Network Prefab Table does not
      list them, rebuild it from the Fusion menu.
- [ ] `Tools > RTS > 2 Setup open Game scene`: `MapAndSystems` with ground and sea, the 10 `ZoneVolume` markers + `ZoneMap`, `BuildSlot`s (2 coastal per
      base), resource nodes, `Spawner` (`GameSpawner`, fully filled), `Systems` (`SelectionManager`, `UnitCommandInput`, `BuildPlacement`,
      `FogOfWar`) and `RtsCamera` on the Main Camera. Layout: land z < 45, sea north of it, bases at x = -65 and +65. Move anything you like.
- [ ] `Tools > RTS > 3 Create Game UI`: `GameCanvas` with the HUD bar, `SelectionPanel` (info + generated buttons), the AI commander panel
      (question field, Ask / Approve / Reject / Ask why, answer and status text) and the inactive game over screen with "Back to menu", all fields
      assigned (plain layout; restyle as you like). If the gateway is not at `http://127.0.0.1:8765`, change the URL on `AiCommanderPanel`.
- Manual fallback: prefab components are `NetworkObject` + `NetworkTransform` + `NetworkUnit` + `UnitCombat` (+ `VillagerGatherer` for the Villager);
  buildings `NetworkObject` + `NetworkTransform` + `NetworkBuilding`; nodes `NetworkObject` + `ResourceNode`; `Player` = `NetworkObject` +
  `NetworkPlayerState` + `CommandRelay`.

## [You] 2. Play tests (report results in UNITY_FEEDBACK.md)
- [ ] Two instances: each side sees its TC and 3 Villagers; box-select and move; right-click a tree: wood rises in the HUD.
- [ ] Build a House (Villager selected > Build house > click a slot): construction finishes, capacity 5 -> 10. Train a Villager at the TC.
- [ ] Build a Barracks, train Spearmen, attack the enemy: HP drops on both screens; destroy the enemy TC: Victory / Defeat on both.
- [ ] Fog: enemy units outside your sight are hidden, dark overlay on unseen ground; Scout sees farthest.
- [ ] AI panel (needs the gateway running): ask, approve, reject, "Ask why". Menu Tools > RTS > Export AI snapshot, then `pytest ai`.
- [ ] Record a short clip of a two-instance fight as M3 evidence.

## [You] 3. Python and live runs
- [ ] `pip install -r ai/requirements.txt`, `python ai/rag/build_index.py` (downloads the embedding model), `pytest ai evaluation`.
- [ ] Live Gemini: `.env` with `GEMINI_API_KEY`, `uvicorn --factory ai.gateway.app:create_app --port 8765` from the repo root; try
      `MCP_TRANSPORT=stdio` with a file `SESSION_DB_PATH` too.
- [ ] `GEMINI_API_KEY=... python evaluation/run_eval.py --real`: keep `results.json`, `results.csv`, `results_table.md`; fill the `[TODO live]`
      cells in `docs/report/report.md` and the "Live (Gemini)" column of `evaluation/security-tests.md`.
- [ ] MCP Inspector screenshots of the 7 tools with valid and invalid input
      (`RTS_SESSION_ID=<id> npx @modelcontextprotocol/inspector python ai/mcp-server/server.py`).

## [You] 4. Deliverables
- [ ] Windows build (IL2CPP); test two instances on the demo machine.
- [ ] Record the backup video following `docs/demo-script.md`.
- [ ] Slides from `docs/slides-outline.md`.
- [ ] Read `docs/report/report.md` once: fix author, instructor approval, and every `[TODO live]`; export to PDF (IEEE A4).
- [ ] Read `docs/defense-notes.md` and practise the five defense topics.

## Agent work still open
- [ ] Night 16-18 of `docs/nightly-plan.md` (fix day, consistency pass, last run) and fixes from UNITY_FEEDBACK.md.
- [ ] Known gaps to decide after the play test: unit pathing around obstacles (straight lines now), Villager build help by right-click on a
      construction site, ships on a NavMesh agent type (zone rule is used instead), gather/attack animations, sound.
