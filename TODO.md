# TODO: remaining work

This file lists only the work that is still open. Milestone overview and status: [README.md](README.md#status).
All code for nights 1-12 is written (updated 2026-11-20); what is left is editor work, play testing, live runs and
the final deliverables. Scripts are in `UnityRTS/Assets/Scripts/`; rule code is compiled and tested with the .NET SDK
(54 C# tests), the Unity-specific scripts are only checked against stubs (`tools/compile-check`), so expect small
compile errors the first time you open them: paste them into UNITY_FEEDBACK.md.

## Open decisions
- [ ] Q-1120-2 (MORNING.md): `weaknesses` in units.json that have no matching `strengths`; recommended: align them (no gameplay change).
- [ ] Q-1120-1 (MORNING.md): resource layout favours each side unevenly (wood only near P2, gold only near P1); recommended: a small forest in each base.
- [ ] Get written instructor approval for the one-person team (assignment asks for 2 students). Mention it in the report.

## [You] 1. Rebuild the generated scenes and prefabs (fixes from UNITY_FEEDBACK 2026-11-18 and 2026-11-19)
- [ ] Throw away the duplicated `Game.unity` (it was not committed): `git checkout -- UnityRTS/Assets/Scenes/Game.unity` or simply
      run the rebuild below. Run **only** the Rebuild command, not 1/2/3 on top of an existing scene (2 and 3 now refuse that with a dialog).
- [ ] Menu `Tools > RTS > Rebuild generated scenes and prefabs` > Rebuild. Console: no red errors. Commit `Assets/Scenes/*.unity` and
      `Assets/Prefabs` to main. Open `Game.unity`: exactly one `MapAndSystems` and one `GameCanvas` in the Hierarchy.
- [ ] Host alone (no second player): Play > session name > Host. Console shows `GameSpawner registered with the NetworkRunner (host).`;
      your TC + 3 Villagers appear; the AI panel no longer says "Match not started yet" (if something is still missing it now names the step).
- [ ] Two players: ParrelSync clone (A = Host, B = Join, same session name), or if the clone keeps closing, Windows build = Host and
      editor = Join (steps in `docs/setup.md` section 3). Both sides: own TC + 3 Villagers (blue) standing on the ground, enemy red
      once seen; host's top bar shows P1, the client's P2, bases at opposite ends. Send 3 Villagers to one bush until it is empty: no error.

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
- [ ] Economy: box-select Villagers (green rings at the feet) and move them; right-click a tree: its label counts down and Wood
      rises in the HUD after each trip to the TC.
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
- [ ] Windows build (IL2CPP): first `Tools > RTS > Set build window (1280x720, windowed)`, then build; test two instances side by side
      on the demo machine.
- [ ] Record the backup video following `docs/demo-script.md`.
- [ ] Slides from `docs/slides-outline.md`.
- [ ] Read `docs/report/report.md` once: fix author, instructor approval, and every `[TODO live]`; export to PDF (IEEE A4).
- [ ] Read `docs/defense-notes.md` and practise the five defense topics.

## Agent work still open
- [ ] Night 16-18 of `docs/nightly-plan.md`: fixes from the next UNITY_FEEDBACK.md entries (four code reviews and the
      2026-11-18/19 feedback are done), the final consistency pass after the last Unity test, and the submission checklist on the last run.
- [ ] Q-1120-1 answer: if B, a small forest in each base (`RtsSetupTools`, design brief, `rules.md`, map EditMode test).
- [ ] Known gaps to decide after the play test: unit pathing around obstacles (straight lines now), ships on a NavMesh agent type (zone rule is used instead), gather/attack animations, sound.
      Graphics stay placeholders on purpose (team discs + labels give the readability); say so in the demo.

## Submission checklist (deadline 2026-11-30)
- [ ] Repository: `git status` clean, no `.env`, no `PhotonAppSettings.asset`, no API keys (`git grep -i "AIza"` is empty).
- [ ] `pytest ai evaluation` passes; EditMode tests pass in Unity.
- [ ] Report PDF: author, instructor approval, all `[TODO live]` filled, figures added.
- [ ] Slides, backup video, Windows build tested on the demo machine, Gemini key and quota checked the day before.
- [ ] Demo rehearsal with `docs/demo-script.md` (all 9 scenarios) and `docs/defense-notes.md`.
