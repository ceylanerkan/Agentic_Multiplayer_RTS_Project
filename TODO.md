# TODO: remaining work

This file lists only the work that is still open. Milestone overview and status: [README.md](README.md#status).
All code for nights 1-12 is written (updated 2026-11-19); what is left is editor work, play testing, live runs and
the final deliverables. Scripts are in `UnityRTS/Assets/Scripts/`; rule code is compiled and tested with the .NET SDK
(51 C# tests), the Unity-specific scripts are only checked against stubs (`tools/compile-check`), so expect small
compile errors the first time you open them: paste them into UNITY_FEEDBACK.md.

## Open decisions
- [ ] Get written instructor approval for the one-person team (assignment asks for 2 students). Mention it in the report.

## [You] 1. Rebuild the generated scenes and prefabs (fixes from UNITY_FEEDBACK 2026-11-18)
- [ ] Delete `Assets/TutorialInfo` and `Assets/Readme.asset` if they are still there, and `Assets/Scenes/SampleScene.unity` (not used).
- [ ] Menu `Tools > RTS > Rebuild generated scenes and prefabs` > Rebuild. It deletes `Assets/Scenes/Menu.unity`, `Game.unity` and
      `Assets/Prefabs` and runs command 0 again: every TMP text now has the LiberationSans SDF font (the NullReferenceException came
      from texts without a font), bigger units, distinct resource shapes, more opaque UI panels. Console: no red errors expected.
- [ ] Check `Tools > RTS > Play from Menu scene` is ticked (default). Press Play with the Game scene open: the Menu must appear.
- [ ] Game view resolution: `Full HD (1920x1080)` (16:9). For two windows on one screen use `1280x720`; the UI scales with the window.
- [ ] Test: Play with a ParrelSync clone: A = Host, B = Join, same session name; both load `Game`. Every unit and building has a coloured
      disc and a name label (blue = mine, red = enemy); trees, berry bushes and gold mines show their remaining amount.
      Closing the clone returns the other to `Menu` with a message.

### What the commands do (reference only; the rebuild runs all of them)
- `Tools > RTS > 1 Create placeholder prefabs`: makes `Assets/Prefabs` with `Player`, 8 unit prefabs (capsules), 6 building prefabs
      (cubes) and `Tree` / `BerryBush` / `GoldMine` (all with `NetworkObject` and the right scripts). If Fusion's Network Prefab Table does not
      list them, rebuild it from the Fusion menu.
- `Tools > RTS > 2 Setup open Game scene`: `MapAndSystems` with ground and sea, the 10 `ZoneVolume` markers + `ZoneMap`, `BuildSlot`s (2 coastal per
      base), resource nodes, `Spawner` (`GameSpawner`, fully filled), `Systems` (`SelectionManager`, `UnitCommandInput`, `BuildPlacement`,
      `FogOfWar`) and `RtsCamera` on the Main Camera. Layout: land z < 45, sea north of it, bases at x = -65 and +65. Move anything you like.
- `Tools > RTS > 3 Create Game UI`: `GameCanvas` with the HUD bar, `SelectionPanel` (info + generated buttons), the AI commander panel
      (question field, Ask / Approve / Reject / Ask why, answer and status text) and the inactive game over screen with "Back to menu", all fields
      assigned (plain layout; restyle as you like). If the gateway is not at `http://127.0.0.1:8765`, change the URL on `AiCommanderPanel`.
- Manual fallback: prefab components are `NetworkObject` + `NetworkTransform` + `NetworkUnit` + `UnitCombat` (+ `VillagerGatherer` for the Villager);
  buildings `NetworkObject` + `NetworkTransform` + `NetworkBuilding`; nodes `NetworkObject` + `ResourceNode`; `Player` = `NetworkObject` +
  `NetworkPlayerState` + `CommandRelay`.

## [You] 2. Play tests (report results in UNITY_FEEDBACK.md)
- [ ] Two instances: each side sees its TC and 3 Villagers (blue disc) in front of the TC; box-select (green rings at the feet) and move;
      right-click a tree: its label counts down and Wood rises in the HUD after each trip to the TC.
- [ ] Build a House (Villager selected > `Build House` > click a slot; the ghost turns green near a free slot): the site shows
      "building N%", the Villager stays selected; when done, Pop shows x/10. Select more Villagers and right-click the
      unfinished building: they help and it finishes faster. Train a Villager at the TC.
- [ ] Build a Barracks, train Spearmen, attack the enemy: HP drops on both screens; destroy the enemy TC: Victory / Defeat on both.
- [ ] Fog: enemy units outside your sight are hidden (with their disc and label), dark overlay on unseen ground; your own building sites
      stay visible; Scout sees farthest.
- [ ] AI panel (needs the gateway running): ask, approve, reject, "Ask why". Menu Tools > RTS > Export AI snapshot, then `pytest ai`.
- [ ] Record a short clip of a two-instance fight as M3 evidence.

## [You] 3. Python and live runs
- [ ] `pip install -r ai/requirements.txt`, `python ai/rag/build_index.py` (downloads the embedding model), `pytest ai evaluation`.
- [ ] Live Gemini: `.env` with `GEMINI_API_KEY`, `uvicorn --factory ai.gateway.app:create_app --port 8765` from the repo root; try
      `MCP_TRANSPORT=stdio` with a file `SESSION_DB_PATH` too.
- [ ] `GEMINI_API_KEY=... python evaluation/run_eval.py --real` (waits 4 s between prompts for the free-tier rate limit; `--delay N` changes it): keep `results.json`, `results.csv`, `results_table.md`; fill the `[TODO live]`
      cells in `docs/report/report.md` and the "Live (Gemini)" column of `evaluation/security-tests.md`.
- [ ] MCP Inspector screenshots of the 7 tools with valid and invalid input: `python ai/mcp-server/make_session.py late_game` prints the exact
      `npx @modelcontextprotocol/inspector ...` command for a ready session.

## [You] 4. Deliverables
- [ ] Windows build (IL2CPP); test two instances on the demo machine.
- [ ] Record the backup video following `docs/demo-script.md`.
- [ ] Slides from `docs/slides-outline.md`.
- [ ] Read `docs/report/report.md` once: fix author, instructor approval, and every `[TODO live]`; export to PDF (IEEE A4).
- [ ] Read `docs/defense-notes.md` and practise the five defense topics.

## Agent work still open
- [ ] Night 16-18 of `docs/nightly-plan.md` (fix day, consistency pass, last run) and fixes from UNITY_FEEDBACK.md.
- [ ] Known gaps to decide after the play test: unit pathing around obstacles (straight lines now), ships on a NavMesh agent type (zone rule is used instead), gather/attack animations, sound.

## Submission checklist (deadline 2026-11-30)
- [ ] Repository: `git status` clean, no `.env`, no `PhotonAppSettings.asset`, no API keys (`git grep -i "AIza"` is empty).
- [ ] `pytest ai evaluation` passes; EditMode tests pass in Unity.
- [ ] Report PDF: author, instructor approval, all `[TODO live]` filled, figures added.
- [ ] Slides, backup video, Windows build tested on the demo machine, Gemini key and quota checked the day before.
- [ ] Demo rehearsal with `docs/demo-script.md` (all 9 scenarios) and `docs/defense-notes.md`.
