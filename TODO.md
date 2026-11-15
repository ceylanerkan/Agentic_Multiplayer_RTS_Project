# TODO: remaining work

This file lists only the work that is still open. Milestone overview and status: [README.md](README.md#status).
All code for nights 1-12 is written (updated 2026-11-15); what is left is editor work, play testing, live runs and
the final deliverables. Scripts are in `UnityRTS/Assets/Scripts/`; rule code is compiled and tested with the .NET SDK
(45 C# tests), the Unity-specific scripts are only checked against stubs (`tools/compile-check`), so expect small
compile errors the first time you open them: paste them into UNITY_FEEDBACK.md.

## Open decisions
- [ ] Get written instructor approval for the one-person team (assignment asks for 2 students). Mention it in the report.

## [You] 1. Project hygiene and scenes
- [ ] Delete `Assets/TutorialInfo` and `Assets/Readme.asset`. Create scenes `Assets/Scenes/Menu.unity` and `Game.unity`, add both to
      File > Build Profiles > Scene List (Menu first).
- [ ] `Menu`: empty object `Launcher` with `GameLauncher`. Canvas with `TMP_InputField` (session name), two Buttons (Host, Join),
      a `TMP_Text` (status); `MenuUI` on the Canvas with the four objects dragged in. Test with ParrelSync: A = Host, B = Join,
      same name; both load `Game`; closing the clone returns the other to `Menu` with a message.
- [ ] Make sure the `Game` scene is loaded through the Fusion runner (GameLauncher does this) so scene objects get networked.

## [You] 2. Prefabs (placeholders are fine: capsules and cubes)
All networked prefabs need `NetworkObject`; register them with the Fusion menu (Network Prefabs) after creating them.
- [ ] `Player`: empty + `NetworkObject`, `NetworkPlayerState`, `CommandRelay`.
- [ ] Units: capsule + `NetworkObject` + `NetworkTransform` + `NetworkUnit` + `UnitCombat` (+ a Collider). `Villager` also needs
      `VillagerGatherer`. Make prefabs `Villager`, `Spearman`, `Archer`, `Cavalry`, `Scout`, `Catapult`, `RaiderBoat`, `Warship`
      (type keys: `villager`, `spearman`, `archer`, `cavalry`, `scout`, `catapult`, `raider_boat`, `warship`).
- [ ] Buildings: cube + `NetworkObject` + `NetworkTransform` + `NetworkBuilding` + Collider: `TownCenter`, `House`, `Barracks`,
      `Stable`, `Blacksmith`, `Dock` (keys `town_center`, `house`, `barracks`, `stable`, `blacksmith`, `dock`).
- [ ] Resource nodes: `Tree` (resource `wood`), `BerryBush` (`food`), `GoldMine` (`gold`): `NetworkObject` + `ResourceNode`
      (set Resource and Start Amount, e.g. 200) + Collider.

## [You] 3. Game scene: map and systems
- [ ] Ground with a Collider (about 200 x 160 units; the sea along the north edge). No NavMesh needed: units walk in straight lines.
- [ ] Ten empty objects with `ZoneVolume` (pick the zone in the dropdown), placed at the zone centres. A point belongs to the nearest
      centre, so put the three water zones (P1Harbor, OpenSea, P2Harbor) in the sea and the seven land zones on land. Add `ZoneMap` to any object.
- [ ] `BuildSlot` objects: several per base zone (and others where you want houses), tick `Coastal` on 1-2 slots of each base next to the harbor.
- [ ] `Spawner` with `GameSpawner`: Player Prefab, Unit Prefabs (key + prefab), Building Prefabs (key + prefab), P1 Spawn and P2 Spawn
      (two empty objects ~40-60 units apart).
- [ ] Resource nodes placed in the scene (trees in EastForest, berries in SouthField, gold in WestMine). If scene NetworkObjects do not
      sync in your setup, tell me in UNITY_FEEDBACK.md and I will change them to spawned objects.
- [ ] Main Camera: add `RtsCamera` (angled about 55 degrees, height 25-30). Empty object `Systems` with `SelectionManager`,
      `UnitCommandInput`, `BuildPlacement`, `FogOfWar` (map centre and size = your ground).

## [You] 4. Game scene: UI
- [ ] HUD: Canvas with five `TMP_Text` (wood, food, gold, population, age) and `HudUI` with them assigned.
- [ ] Bottom panel: `SelectionPanel` with an info `TMP_Text` and an empty `RectTransform` (add a Vertical Layout Group) as button root.
      Buttons are created by the script.
- [ ] `GameOverUI`: inactive panel with a `TMP_Text` and a Button (On Click -> `GameOverUI.OnMenuClicked`); assign the panel and the text.
- [ ] `AiCommanderPanel`: `TMP_InputField` (question), Buttons Ask / Approve / Reject / Why, two `TMP_Text` (answer, status). Assign all.
      Set the gateway URL if it is not `http://127.0.0.1:8765`.

## [You] 5. Play tests (report results in UNITY_FEEDBACK.md)
- [ ] Two instances: each side sees its TC and 3 Villagers; box-select and move; right-click a tree: wood rises in the HUD.
- [ ] Build a House (Villager selected > Build house > click a slot): construction finishes, capacity 5 -> 10. Train a Villager at the TC.
- [ ] Build a Barracks, train Spearmen, attack the enemy: HP drops on both screens; destroy the enemy TC: Victory / Defeat on both.
- [ ] Fog: enemy units outside your sight are hidden, dark overlay on unseen ground; Scout sees farthest.
- [ ] AI panel (needs the gateway running): ask, approve, reject, "Ask why". Menu Tools > RTS > Export AI snapshot, then `pytest ai`.
- [ ] Record a short clip of a two-instance fight as M3 evidence.

## [You] 6. Python and live runs
- [ ] `pip install -r ai/requirements.txt`, `python ai/rag/build_index.py` (downloads the embedding model), `pytest ai evaluation`.
- [ ] Live Gemini: `.env` with `GEMINI_API_KEY`, `uvicorn --factory ai.gateway.app:create_app --port 8765` from the repo root; try
      `MCP_TRANSPORT=stdio` with a file `SESSION_DB_PATH` too.
- [ ] `GEMINI_API_KEY=... python evaluation/run_eval.py --real`: keep `results.json`, `results.csv`, `results_table.md`; fill the `[TODO live]`
      cells in `docs/report/report.md` and the "Live (Gemini)" column of `evaluation/security-tests.md`.
- [ ] MCP Inspector screenshots of the 7 tools with valid and invalid input
      (`RTS_SESSION_ID=<id> npx @modelcontextprotocol/inspector python ai/mcp-server/server.py`).

## [You] 7. Deliverables
- [ ] Windows build (IL2CPP); test two instances on the demo machine.
- [ ] Record the backup video following `docs/demo-script.md`.
- [ ] Slides from `docs/slides-outline.md`.
- [ ] Read `docs/report/report.md` once: fix author, instructor approval, and every `[TODO live]`; export to PDF (IEEE A4).
- [ ] Read `docs/defense-notes.md` and practise the five defense topics.

## Agent work still open
- [ ] Night 16-18 of `docs/nightly-plan.md` (fix day, consistency pass, last run) and fixes from UNITY_FEEDBACK.md.
- [ ] Known gaps to decide after the play test: unit pathing around obstacles (straight lines now), Villager build help by right-click on a
      construction site, ships on a NavMesh agent type (zone rule is used instead), gather/attack animations, sound.
