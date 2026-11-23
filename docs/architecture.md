# Architecture (implemented parts)

```
Unity (host/client, Fusion 2)           Python (ai/)
 VisibilityService.FilterFor  ──► AiSafeSnapshot ──HTTP POST /ask──► gateway (FastAPI)
 AiCommanderPanel ◄── AiAnswer ◄───────────────────────────────────  │ validates snapshot schema (unknown fields rejected)
 Approve ► GameActionValidator ► CommandRelay RPC ► host authority     │ saves it in SQLite under the session token
                                                                       ▼
                                                    Agent loop (≤6 steps, 30 s) ◄─► LLM (Gemini / Ollama / scripted)
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
| Land and water | Zones come from the nearest `ZoneVolume` centre (`ZoneLookup`, every point has a zone). Land units cannot leave land zones and ships cannot leave water zones (`DomainRules`). |
| Presentation | Purely local, no network state: `EntityVisuals` adds a team disc (own blue, enemy red) and a name/HP label to every unit and building and an amount label to resources; it copies the body's visibility, so fog of war hides it too. `SelectionManager` draws the selection rings. |
| Editor tooling | `RtsSetupTools` generates the Menu and Game scenes, the placeholder prefabs and the UI; `PlayFromMenu` makes play mode start in the Menu scene, because the Game scene only works when the `NetworkRunner` loads it. |

Engine-free rule code (`Assets/Scripts/Core/Rules`, assembly `RTS.Core`) is compiled and tested outside Unity;
the networked MonoBehaviours are thin adapters around it.
