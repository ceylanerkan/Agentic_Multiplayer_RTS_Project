# Architecture (implemented parts)

```
Unity (host/client, Fusion 2)           Python (ai/)
 VisibilityService.FilterFor  ──► AiSafeSnapshot ──HTTP POST /ask──► gateway (FastAPI)
 AiCommanderPanel ◄── AiAnswer ◄───────────────────────────────────  │ validates snapshot schema (unknown fields rejected)
 Approve ► GameActionValidator ► CommandRelay RPC ► host authority     │ saves it in SQLite under the session token
                                                                       ▼
                                                    Agent loop (≤6 steps, 30 s) ◄─► LLM (Ollama local / scripted)
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
| Tool pre-selection with Laya (optional, `TOOL_ROUTER=laya`) | `ai/gateway/tool_router.py`, `laya_tool_weights.npz`; training and measurement in `evaluation/tool_router/` |
| Evaluation | `evaluation/` |
| Unity rules (engine-free) | `UnityRTS/Assets/Scripts/Core/Rules/` |

Security boundaries: see [design-brief.md §4](design-brief.md). The session token is held only by Unity and the
gateway; the MCP server receives its session id from the environment of the gateway, and no tool takes a player id.
Plans are validated by JSON Schema in the gateway (`jsonschema`, one repair retry), by `GameActionValidator` in Unity
at approval time, and by the host in the RPC handler.

## Networking (Photon Fusion 2, Host Mode)

```
 Client (InputAuthority)                       Host (StateAuthority)
 SelectionManager / UnitCommandInput  ──RPC──► CommandRelay (one per player)
 SelectionPanel / BuildPlacement      ──RPC──►   │ FindOwned: the object must belong to the sender
 AiApprovalFlow (approved plan)       ──RPC──►   │ GameActionValidator: cost, population, prerequisites, zone rules
                                                 ▼
                                       NetworkPlayerState  [Networked] wood, food, gold, pop, age, techs, result
                                       NetworkUnit         [Networked] owner, type, HP, move target, attack target
                                       NetworkBuilding     [Networked] owner, type, HP, build progress, queue summary
                                       ResourceNode        [Networked] remaining amount
                                       VillagerGatherer / UnitCombat / production queue run only on the host
 NetworkTransform ◄── position of every unit ◄──┘
```

| Topic | Decision |
|---|---|
| Authority | The host owns all game state and runs gathering, construction, production, combat, the win check. |
| Commands | A client never changes state. It calls `CommandRelay.Send*`, an RPC `InputAuthority -> StateAuthority`. The host checks ownership (`FindOwned`) and the rules (`GameActionValidator`, `ProductionQueue.WhyNot`) before acting. |
| AI actions | The approved plan uses the same `CommandRelay` RPCs as manual play, so the AI cannot bypass authority. |
| Resources and population | Cost is paid and population is reserved when a unit is queued, so a long queue cannot exceed the cap. |
| Spawning | `GameSpawner` (a `SimulationBehaviour` in the Game scene) registers itself with `Runner.AddGlobal`; the host spawns each player's `Player` object (input authority = that player), Town Center and 3 Villagers. Units and buildings have no input authority. |
| Slots | The host gives each joining player the first free slot (`PlayerSlots.FirstFree`) and stores it in the networked `NetworkPlayerState.Slot`; every peer reads that value. (Not by `PlayerRef` order: Fusion gives the host the last index.) |
| Disconnect | When the other player leaves, the host ends the match and both peers return to the menu (`GameLauncher`). |
| Match end | When a player's last Town Center is destroyed the host sets `NetworkPlayerState.Result` (1 = won, 2 = lost) for both players; `GameOverUI` shows Victory or Defeat. |
| Events | Per-player event log. The host decides what a player may know and sends each event to its owner with an RPC (`StateAuthority -> InputAuthority`); every peer stores only its own log. |
| Fog of war | `VisibilityService.FilterFor` (engine-free) decides what a player sees. Rendering (`FogOfWar`) and the AI snapshot (`AiSnapshotBuilder`) both call it. Interest management is off, so a modified client could read hidden positions: a known limitation, the AI is not affected. |
| Land and water | Zones come from the nearest `ZoneVolume` centre (`ZoneLookup`, every point has a zone). Land units cannot leave land zones and ships cannot leave water zones (`DomainRules`); a unit stopped by the coast moves on along it (`NetworkUnit.SlideAlongShore`). |
| Building places | Free placement (`BuildSite`, engine-free): land, outside the enemy base, inside the map, not on a building or resource; a Dock at most 2.5 m from the water. `BuildPlaces` gives the same test to the ghost, the AI approval (the plan names only a zone) and the host, which decides. The ghost ignores buildings hidden by the fog. |
| Walls | `WallLine` (engine-free): pieces on a 2 m grid, direct or diagonal neighbours; drawn with the mouse, one RPC per line, the host places what fits and can be paid; Villagers go from piece to piece. `PathGrid` stores for every cell who is blocked, so a Gate lets its owner pass and nobody slips between two diagonal pieces; a unit never pushes through a wall on the last straight part. `WallLook` draws the logs towards the neighbours. Not part of AI plans (`player_only`). |
| Pathfinding | Host only: A* on a 1 m grid (`PathGrid`, engine-free): land or water cells, buildings and resources block (0.5 m clearance), no corner cutting, straightened; a straight free way needs no search. The path is checked again when a building appears or disappears and when a chased enemy moves. The last part goes straight to the target until the reach test stops it. Units do not avoid each other. |
| Ground and coast | `TerrainShape` (engine-free): winding coast, hills, flat ground at the Town Centers, mirrored; snow-capped mountains outside the map (scenery, not mirrored; the fog is thin over them and the camera rises above them); `GroundMesh` builds mesh, collider and texture from it at load time, the zones and units use the same functions. Height is only seen, it changes no rule. A river (`TerrainShape.InRiver`) runs from the sea through the middle: `PathGrid` gives its cells to land units and ships, melee units of both can fight there, nothing is built in it. |
| Day and night | `DayNight` (added by `GroundMesh`): a 7 minute cycle from the session clock, so both players see the same hour; turns and colours the light, sets the ambient light and `_RtsNight` for the shaders. Only to look at: no rule depends on it. |
| Map resources | `MapLayout` (engine-free, fixed seed): irregular groups on the left half, mirrored to the right, so both players get the same; fish shoals in the water. `Tools > RTS > Refresh map resources and menu` places them. |
| Start of a match | The host waits in the menu (`GameLauncher.State.Waiting`) and loads the Game scene when the second player has joined; *Practice alone* starts a session for one player at once. |
| Presentation | Purely local, no network state: `EntityVisuals` adds a team disc (own blue, enemy red) and a name/HP label to every unit and building and an amount label to resources; it copies the body's visibility, so fog of war hides it too. The fog is a box with a shader that reads the depth texture (`Resources/FogOfWar.shader`), so it lies on the ground and on objects. `SelectionManager` draws the selection rings. The panels build themselves in code when the scene starts (`UiKit`: colours, frames, icons, tooltip): `HudUI` (resources, idle Villagers, age, events, menu), `MiniMap` (shows only what the fog allows), `SelectionPanel`, `AiCommanderPanel`, `GameOverUI`. |
| Editor tooling | `RtsSetupTools` generates the Menu and Game scenes, the placeholder prefabs and the UI; `PlayFromMenu` makes play mode start in the Menu scene, because the Game scene only works when the `NetworkRunner` loads it. |

Engine-free rule code (`Assets/Scripts/Core/Rules`, assembly `RTS.Core`) is compiled and tested outside Unity;
the networked MonoBehaviours are thin adapters around it.
