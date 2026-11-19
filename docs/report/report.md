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
method (IV), the MCP and agent design (V) and the RAG method (VI). Sections VII to XII cover human control, security,
evaluation, lessons learned, conclusion and the appendix.

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

**Presentation.** The graphics are deliberately simple placeholders, because the course is about the networked and
agentic parts: units are capsules, buildings flat boxes, resources cylinders (trees) and spheres (bushes, gold).
Readability comes from information instead of art: every unit and building has a team disc (own blue, enemy red, from
each player's own point of view) and a label with its name and HP or construction progress; resources show their
remaining amount. The target resolution is 1920x1080 and the UI scales with the window.

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
`RTS.Core` without any `UnityEngine` reference. They are compiled and covered by 47 NUnit tests outside Unity. The
MonoBehaviours and NetworkBehaviours are thin adapters that read the networked objects and call the core.

**Python side.** `ai/gateway` (FastAPI, agent loop, logs), `ai/mcp-server` (seven tools), `ai/rag` (knowledge index
and retriever) and `evaluation/` (fixtures, prompts, runner). Python tests: 64 pytest cases.

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


## VII. Structured Actions and Human Control

**Structured output.** The model may only propose four action types: `TRAIN_UNIT`, `BUILD`, `MOVE_GROUP` and
`RESEARCH`. Each action names unit, building or technology types from closed enumerations, a count of 1-20 and a zone
from the ten zone names, plus a short reason. The JSON Schema (`action_plan.schema.json`) forbids every other field,
so the model cannot express "reveal the enemy base" or "delete a unit" at all.

**Approval.** `AiCommanderPanel` shows the summary, confidence, evidence (tool names and knowledge ids), the tools
used and each action with its resource cost. A plan without actions is an information answer and needs no approval.
A plan with actions shows Approve, Reject and Ask why.

- **Approve:** `AiApprovalFlow` checks every action again against the *current* game state with
  `GameActionValidator` (resources are reserved action by action, so two actions cannot spend the same gold). Valid
  actions are sent through `CommandRelay`, the same RPCs as manual play, where the host validates once more. Invalid
  actions are skipped and the panel tells the player why.
- **Reject:** nothing is sent; only the decision is logged.
- **Ask why:** the gateway explains the last plan; if the stored snapshot is older than a threshold, the prompt tells
  the model to lower its confidence.

**Decision log.** Every decision is appended to `decision-log.jsonl` with plan id, decision, game time, a description
of each action and the validator result per action. This makes the demo scenarios "approve one plan, reject another"
and "validation failure" visible and reproducible.

**Validator rules (examples).** Cost and reserved resources; population capacity; prerequisites (Barracks for Age II,
Age II for Stable and Cavalry); no technology twice; buildings only in land zones and never in the enemy base; a Dock
only on a free coastal slot of the own base; land groups cannot be sent to water zones and ships cannot be sent to
land zones.

## VIII. Security

**Threat model.** The player is trusted to play but not trusted with hidden information; the model is trusted to be
helpful but not to be obedient to text inside its inputs. The assets are (1) the opponent's hidden state, (2) the
integrity of the game state and (3) the player's consent.

**Boundaries.**

1. *Fog of war before the network.* `VisibilityService.FilterFor` is the single visibility query. The snapshot
   builder cannot add an enemy that is not in its output and the schema has no field for opponent resources or exact
   positions; the gateway rejects unknown fields (HTTP 422, logged).
2. *Identity.* The session token is held only by Unity and the gateway. The MCP server learns its session from the
   environment, no tool has a player or session parameter, and the agent rejects undeclared arguments.
3. *Read-only tools.* No tool changes state; there are no filesystem, shell or HTTP tools.
4. *Triple validation of plans.* JSON Schema in the gateway, `GameActionValidator` at approval time, host authority in
   the RPC handler. The host also checks what a client cannot see: a technology already queued in another building is
   refused (it would be paid twice), and a building can only go on a free build slot in the validated zone.
5. *Bounded behaviour.* Maximum 6 agent steps, 30 s, clamped `top_k`, limited query and question length, 20 events.
6. *Prompt injection.* Tool results and retrieved text are marked as data and the system prompt says so; suspicious
   questions are logged. The real protection is structural: even a fully obedient model cannot reach hidden data,
   because no tool can return it.

**Tests.** Security tests S1-S16 are listed in `evaluation/security-tests.md`: injection question, extra hidden field,
missing or wrong token, identity switch across two sessions, tool call with a player id, oversized `top_k` and query,
a poisoned knowledge chunk, invalid plans (five kinds), a snapshot contract test between Unity's export and the
schema, and re-validation after the state changed. **[TODO live]** Rows marked TODO in that file need the live Gemini
run.

**Known limitation: client-side fog of war.** Fusion interest management is off, so each client receives all networked
objects and hides enemies only visually. A modified client can read hidden positions. Fixing this would need
per-player interest management or a server-side visibility replication layer; we left it out of scope. The AI is not
affected because its snapshot never contains hidden data.

## IX. Evaluation

**Method.** Three fixed snapshots (early economy, cavalry rush, late game) and 21 labelled prompts in eight
categories (single tool, compound, knowledge-only, cost, security, stale state, invalid action, valid action). For
each prompt we record the tools called, whether the plan is schema-valid, latency, and for plans with actions the
result of a Python copy of the gameplay checks (`evaluation/plan_checks.py`, mirrored by the C# tests).

**Metrics.** Tool-call accuracy (all expected tools called; knowledge-only questions call only
`search_game_knowledge`), valid-plan rate, average latency, invalid-action rate (share of proposed actions rejected by
the validator on the fixture state), and the share of stale-state answers with confidence <= 0.5.

*Table II. Offline run with the scripted mock model (`python evaluation/run_eval.py`).*

| Metric | Mock | Live Gemini |
|---|---|---|
| Prompts | 21 | 21 |
| Tool-call accuracy | 100% | **[TODO live]** |
| Valid-plan rate | 100% | **[TODO live]** |
| Average latency (s) | 0.003 | **[TODO live]** |
| Actions proposed | 6 | **[TODO live]** |
| Invalid-action rate | 50% | **[TODO live]** |
| Stale answers with confidence <= 0.5 | 100% | **[TODO live]** |

The mock numbers are true by construction (the mock model picks tools from keywords and proposes exactly what the
player asked for); they show that the harness, the plan checks and the table work, not that the AI is good. The two
"invalid action" prompts ("Train 20 archers right now" with 5/5 population, "Send my archers to the OpenSea") are
rejected by the validator, which is the intended behaviour and the observed limitation: **the model will follow a
literal player request even when it is illegal, so the validator is necessary.** **[TODO live]** Add the observed
failures of the real model (for example a missing prerequisite or a wrong zone) and the 2-3 slowest prompts.

*Table III. Security tests.* See `evaluation/security-tests.md` (rows S1-S16; most are automated, the live Gemini column is open).

**Unity side.** 51 EditMode tests cover damage, costs, validator, domains, visibility (including unfinished buildings),
event log, production queue, tech effects, snapshot assembly, approval, the decision log and the contract between the
C# AI types and `action_plan.schema.json`. They were compiled and run with the .NET SDK outside
Unity; the same tests run in the Unity Test Runner. **[TODO live]** Two-instance test results and the demo video.

## X. Challenges and Lessons Learned

- **Compile without the editor.** The agent that helped write the code could not run Unity, so all rule code was put
  into an engine-free assembly and compiled with the .NET SDK. This caught real bugs early (for example buildings read
  with 0 HP because `hp` is a top-level field in `buildings.json`). Lesson: keep game rules free of engine types.
- **Unity message names.** A method `Start(GameMode, string)` was reported by Unity as an invalid message signature
  and had to be renamed. Lesson: never reuse Unity's magic method names.
- **Generated scenes need the same care as code.** The scenes and placeholder prefabs are created by an editor script.
  Its first version created TextMeshPro texts without a font asset, which threw in the UI layout pass and left every
  label invisible, and pressing Play in the open Game scene skipped the network launcher. The fixes were to set the font
  before the text and to force play mode to start in the Menu scene. A first play test also showed that identical
  grey placeholders were hard to read, so each unit and building now has a team disc and a name/HP label.
  Lesson: test the generated content in the editor as early as the code.
- **One source of numbers.** Keeping all values in JSON files read by Unity, the RAG index and the evaluation removed a
  whole class of inconsistencies, but needs a loader at build time (`StreamingAssets` copy).
- **Host authority vs. convenience.** Reserving population and paying cost when a unit is queued (not when it appears)
  keeps the rules simple and makes long queues safe.
- **The model is not the safety layer.** The most important security decision was to remove capabilities (no hidden
  field, no id argument, no write tool) instead of asking the model to behave.
- **Honest evaluation.** A mock model gives 100% by design; the report separates mock numbers from live numbers.

## XI. Conclusion and Future Work

We delivered a two-player networked RTS with an authoritative host, a fog-of-war-aware AI commander that uses seven
read-only MCP tools and a RAG index, structured and approved plans, a decision log and an evaluation harness. Future
work: per-player interest management for server-side fog of war; embeddings and a reranker for retrieval with a
larger corpus; AI plans that can chain (build, then train after the building completes); a tool that explains the
validator's rejection to the model so that it can repair a plan; balance tuning from recorded matches.

## XII. Appendix

**A. Repository layout.** `UnityRTS/` (Unity project; rules in `Assets/Scripts/Core/Rules`), `ai/` (gateway, MCP
server, RAG, schemas), `evaluation/`, `docs/`.

**B. Dependencies.**

| Component | Version |
|---|---|
| Unity | 6000.4 (URP 17.4) |
| Photon Fusion | 2.0.13 (Host Mode) |
| Unity packages | AI Navigation 2.0.12, Input System 1.19.0, Newtonsoft JSON 3.2.2, Test Framework 1.6.0, uGUI 2.0.0, ParrelSync |
| Python | 3.10+ |
| Python packages | `mcp` >= 2.3, `fastapi` >= 0.115, `uvicorn` >= 0.30, `google-genai` >= 1.0, `jsonschema` >= 4.23, `sentence-transformers` >= 3.0 (optional embeddings), `rank-bm25` >= 0.2.2, `python-dotenv` >= 1.0, `pytest` >= 8.0 |
| LLM | Gemini (default `gemini-2.5-flash`, configurable) |

**C. Tests.** Python: `python -m pytest -q ai evaluation` (64 tests, no network, no API key). C#: Unity Test Runner,
EditMode, assembly `RTS.Tests.EditMode` (51 tests). Evaluation: `python evaluation/run_eval.py [--real]`.

**D. Demo scenarios.** See `docs/demo-script.md` (to be written on night 15).
