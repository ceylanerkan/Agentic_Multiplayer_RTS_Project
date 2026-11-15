# An Agentic Multiplayer RTS: Photon Fusion 2, an MCP Tool Server and Retrieval-Augmented Strategy Advice

**CSE-377 Game Programming, final project report (IEEE structure).**
Author: *[student name and number]*. Team size: one student (written instructor approval: *[date / mention]*).

> Draft status: sections marked **[TODO live]** need numbers from the live Gemini run or screenshots from the
> Unity editor. Everything else describes code that exists in this repository.

## Abstract

We built a small two-player real-time strategy game in Unity 6 with Photon Fusion 2 in Host Mode. Players gather
wood, food and gold, build a base, train a counter-based army and fleet, advance one age and win by destroying the
opponent's Town Center. On top of the game sits an AI Strategic Commander. The player asks a question; Unity sends a
visibility-filtered snapshot to a Python gateway; a Gemini agent decides which of seven read-only MCP tools to call,
retrieves game knowledge with a local RAG index, and returns a schema-validated action plan. Nothing happens until the
player approves the plan, and an approved plan travels through the same authoritative command path as manual play.
We describe the design, the multiplayer authority model, the MCP and RAG components, the security boundaries around
hidden information, and an evaluation with a fixed prompt set.

## I. Introduction

Large language models can give useful strategy advice, but a game is an adversarial setting: the model must not know
what the player may not know, it must not act without consent, and its plans must respect the rules of the game.
The goal of this project is to show one complete, explainable solution to these three problems in a playable
multiplayer game:

1. **Multiplayer:** two Unity instances share one match with a single authority (the host).
2. **Tool use:** the agent chooses tools at run time through the Model Context Protocol (MCP) instead of receiving
   a fixed prompt with all data.
3. **Safety:** fog of war is applied before data leaves the game, the player approves every plan, and every plan is
   validated three times.

The report follows the structure of the code: game design (Section II), system architecture (III), the multiplayer
method (IV), the MCP and agent design (V) and the RAG method (VI). Sections VII to XII (human control, security,
evaluation, lessons, conclusion, appendix) are in part 2.

## II. Game Design

**Setting.** One symmetric map with seven land zones (P1Base, P2Base, NorthGate, SouthField, EastForest, WestMine,
Center) and three water zones along the north edge (P1Harbor, OpenSea, P2Harbor). Every point of the map belongs to
exactly one of the ten zones; the zones are the vocabulary shared by the game, the snapshot and the AI plans.

**Economy.** Three finite resources: wood (trees), food (berry bushes), gold (mines). A Villager gathers 0.6 units per
second, carries 10 and must walk back to a Town Center to deposit. Each player starts with one Town Center, three
Villagers and 200 wood, 200 food, 100 gold. Population is capped at 50: the Town Center and every House give +5.

**Units and counters.** Eight unit types (Villager, Spearman, Archer, Cavalry, Scout, Catapult, Raider Boat,
Warship). Damage is `max(1, attack x multiplier - armor)` with multiplier 1.5 for a counter and 0.75 when countered.
The land loop is Spearman > Cavalry > Archer > Spearman; the naval loop is Raider Boat > Warship > Archer > Raider
Boat; Catapults deal 3x damage to buildings and beat Warships. Land units stay on land and ships on water; melee
units attack only their own domain, ranged units shoot across the shoreline.

**Buildings and technology.** Town Center, House, Barracks, Stable, Blacksmith, Dock. Four technologies: Age II
(unlocks Stable, Cavalry, Catapult, Warship, Fletching), Iron Weapons (+2 attack for military units), Fletching
(+1 range and line of sight for Archers), Horse Collar (+20% food gathering). The Town Center cannot train while it
researches Age II.

**Single source of numbers.** All values live in `ai/rag/knowledge/*.json`. Unity reads the same files at run time
(`GameDataProvider`), the AI retrieves them as knowledge, and the evaluation reads them for its own rule checks, so
the game and the AI cannot disagree about a cost or a statistic.

## III. System Architecture

The system has two halves connected by a local HTTP call (Fig. 1).

*Fig. 1. Architecture (see also `docs/architecture.md` and `docs/design-brief.md` for the Mermaid diagram).*

```
Unity (host or client, Fusion 2)                      Python (ai/)
 VisibilityService.FilterFor -> AiSafeSnapshot --POST /ask--> Gateway (FastAPI)
 AiCommanderPanel <-- AiAnswer <------------------------------  validates snapshot, saves it (SQLite)
 Approve -> GameActionValidator -> CommandRelay RPC                  |
          -> host authority                                          v
                                                       Agent loop <-> Gemini (function calling)
                                                          | MCP over stdio
                                                          v
                                                       MCP server: 7 read-only tools -> snapshot, RAG
```

**Engine-free core.** The game rules that must be correct and explainable (costs, population, prerequisites, damage,
domains, visibility, production queue, win condition, approval checks) are plain C# classes in the assembly
`RTS.Core` without any `UnityEngine` reference. They are compiled and covered by 45 NUnit tests outside Unity. The
MonoBehaviours and NetworkBehaviours are thin adapters that read the networked objects and call the core.

**Python side.** `ai/gateway` (FastAPI, agent loop, logs), `ai/mcp-server` (seven tools), `ai/rag` (knowledge index
and retriever) and `evaluation/` (fixtures, prompts, runner). Python tests: 53 pytest cases.

**Data flow of one question.** (1) The player types a question. (2) `AiSnapshotBuilder` collects own units and
buildings and the enemies returned by `VisibilityService.FilterFor`. (3) The gateway validates the snapshot against
`safe_snapshot.schema.json` (unknown fields are rejected) and stores it under the session token. (4) The agent loop
asks the model, executes the tool calls it requests against the MCP server, and finally parses an `RTSActionPlan`.
(5) Unity shows summary, confidence, evidence, tools and the proposed actions with costs.

## IV. Multiplayer Method

**Why Host Mode.** An RTS has a lot of contested shared state (combat, resources, production). With one authority the
host can validate every command deterministically, which also makes the system easy to explain. The cost is that the
host has no latency; for a class prototype this is acceptable. Shared Mode would require resolving conflicting writes
to the same unit.

**Authority.** The host (StateAuthority) owns every unit, building, resource node and player stockpile and runs
pathing, gathering, construction, production, combat and the win check. Each player has a `CommandRelay`. A client
calls `Send*`, which is an RPC from InputAuthority to StateAuthority. On the host the relay (a) checks that the
object belongs to the sender, (b) validates cost, population, prerequisites and zone rules with `GameActionValidator`
and the production queue rules, and only then (c) changes the state. A client therefore cannot spend resources it
does not have or command units it does not own.

**Replicated state.** `NetworkPlayerState` (resources, population, age, researched technologies, match result),
`NetworkUnit` (owner, type, HP, move and attack target; position through `NetworkTransform`), `NetworkBuilding`
(owner, type, HP, construction progress, queue summary) and `ResourceNode` (remaining amount). Clients only read these
values for the HUD and panels.

**Match life cycle.** `GameLauncher` starts the Fusion runner (`GameMode.Host` or `GameMode.Client`) with a session
name, loads the Game scene, and assigns slot 1 to the lowest `PlayerRef` and slot 2 to the next, which every peer
computes identically. If the opponent disconnects the match ends and both return to the menu. When a player loses the
last Town Center the host sets a result for both players and `GameOverUI` shows Victory or Defeat.

**Fog of war.** Visibility is a pure function: a player sees all own objects and every enemy object inside the line
of sight circle (from the knowledge base, plus the Fletching bonus) of at least one own object. The Unity adapter
hides enemy renderers and paints a fog texture; the AI snapshot uses the same function. Because Fusion interest
management is off, every client still receives all networked objects, so a modified client could read hidden
positions. We document this limitation honestly in Section VIII; the AI is not affected because its input is filtered
before it leaves Unity.

**Events.** Each player has an event log (eight event types with zone and time). The host decides which events a
player may know (own losses and training, plus enemy sightings that come from the visibility function) and sends
each one to its owner by RPC, so every peer stores only its own log.

## V. MCP and Agent Design

**Tools.** The MCP server (Python `mcp` SDK) exposes seven tools, all marked read-only (Table I). No tool takes a
player or session identifier: the session is bound through an environment variable set by the gateway, so the model
cannot ask for another player's data. Inputs are typed and range-checked and errors are returned as data so that the
agent can explain them.

*Table I. MCP tools.*

| Tool | Purpose |
|---|---|
| `get_player_resources` | resources, population, age |
| `get_army_status` | own units grouped by type and zone, own buildings |
| `get_visible_enemies` | only enemies currently visible |
| `get_technology_status` | researched and in-progress technologies |
| `get_recent_events` | last 1-20 events |
| `estimate_plan_cost` | total cost, missing resources, population check for up to 10 items |
| `search_game_knowledge` | RAG over game knowledge (top 1-5 chunks with source ids) |

**Agent loop.** The gateway runs a provider-neutral loop (maximum 6 steps, 30 s). The model decides which tools to
call; every call is logged as JSON (tool, arguments, start and end time, status, result count). A compound question
such as "What should I do about the cavalry at NorthGate?" typically calls the enemy tool, the resource tool, the
knowledge tool and the cost tool, while "What does Iron Weapons do?" calls only the knowledge tool. Tests with a
scripted model verify both behaviours, once in-process and once through a real stdio MCP client with the server as a
subprocess (`MCP_TRANSPORT=stdio`). The final answer must be one `RTSActionPlan` JSON object (summary, confidence,
evidence, up to 8 actions of four types: TRAIN_UNIT, BUILD, MOVE_GROUP, RESEARCH). It is validated with JSON Schema;
if invalid, the agent gets one repair retry, otherwise the gateway answers with an error.

**Why MCP.** The tools are the only door through which the model can reach game data. Their number, inputs and
read-only property are easy to audit, and the same server could be used by any MCP client (MCP Inspector, another
model). The cost is an extra process in the demo; the in-process mode keeps unit tests fast.

## VI. RAG Method

**Corpus.** 18 structured records (8 units, 6 buildings, 4 technologies, from the same JSON files Unity reads) and
15 rule or strategy sections from `rules.md`. Each record is one chunk with a stable `source_id` such as
`unit_spearman_v1` or `strategy_defend_cavalry_v1`; structured records are rendered as plain text so that both
retrieval methods can use them.

**Retrieval.** BM25 over title and text (the title is counted twice so that exact names win) is the baseline and runs
without a model download. If `ai/rag/build_index.py` has been run and `sentence-transformers` is installed, the
score becomes the average of the normalised BM25 score and the cosine similarity of the embeddings (hybrid). The tool
returns the top-k chunks with source id, score and text; `top_k` is clamped to 1-5 and the query length is limited.

**Use in answers.** The agent puts retrieved source ids into the plan's `evidence` list, and the panel shows them, so
the player can see which knowledge an answer rests on. Retrieved text is marked as reference data, and the system
prompt tells the model to ignore instructions inside tool results (tested with a poisoned chunk).

**Limits.** The corpus is small (33 chunks); BM25 handles exact names and rule keywords well but can miss paraphrases
when embeddings are not installed. We do not rerank and we do not chunk below record level.

*Part 2 (Sections VII-XII) follows in the next revision.*
