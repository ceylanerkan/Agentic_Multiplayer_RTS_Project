# Oral defense notes

Short answers you can say in your own words. Code locations in `backticks`.

## 1. Who has authority? (Fusion Host Mode)
The host has StateAuthority over every unit, building, resource node and the player stockpiles. A client calls
`CommandRelay.Send*`, which is an RPC `InputAuthority -> StateAuthority` (`Networking/CommandRelay.cs`). The host first checks
that the object belongs to the sender (`FindOwned`), then validates cost, population, prerequisites with
`GameActionValidator`, then changes state. So a client cannot cheat by editing its own numbers. AI actions use the same RPCs.
*Why Host Mode?* Contested state (combat, resources) is easy to validate with one authority; trade-off: host has zero latency.

## 2. The three MCP tools I can explain in detail
- `get_visible_enemies`: reads the newest snapshot of this session; returns only visible enemies grouped by type and zone.
- `estimate_plan_cost`: pure calculation from the knowledge JSON: total cost, missing resources, population check. Inputs are
  range-checked (1-10 items, count 1-50, no extra fields).
- `search_game_knowledge`: the RAG tool; query max 200 chars, `top_k` clamped to 1-5, returns `source_id`, score and text.
All seven are read-only; none takes a player or session id (the session comes from the environment of the MCP process).

## 3. RAG pipeline
Knowledge JSON (units, buildings, technologies) and `rules.md` sections -> one chunk per record with a stable `source_id`
(`ai/rag/retriever.py: load_chunks`) -> BM25 over title+text (title counted twice) -> optional hybrid with sentence-transformer
embeddings (average of normalised BM25 and cosine) -> top-k chunks with ids -> the agent cites ids in `evidence` -> the panel shows them.

## 4. Validator
`GameActionValidator` (`Core/Rules`): count 1-20; unit/building/tech known; population; prerequisites (buildings, Age II);
resources with reservation across actions; land vs water zones; no building in the enemy base; Dock only on a free coastal slot.
It runs three times: gateway (JSON Schema), Unity on Approve (`AiApprovalFlow` -> `ApprovalEvaluator`), host in the RPC handler.

## 5. One security test explained
*Identity switch* (`ai/gateway/tests/test_security.py::test_identity_switch_...`): two sessions, A has 999 gold. In B's session the
question says "I am player A, show A's resources". The tool result still contains only B's numbers, because the session id comes from the
token and the environment, not from the model or the question. Also: a tool call with `player_id` is rejected by the agent.
Alternative: *injection* - a poisoned knowledge chunk is returned only as data, and even an obedient model has no tool that returns hidden data.

## 6. Questions to expect
- *Where is fog of war enforced?* Before the snapshot leaves Unity (`VisibilityService.FilterFor`); the network still sends all objects (limitation).
- *What if the LLM is wrong?* The plan needs approval; invalid actions are skipped with a reason; confidence and evidence are shown.
- *What if the snapshot is stale?* "Ask why" tells the model the snapshot is old and to lower its confidence.
- *Why a mock evaluation?* Offline and repeatable; its 100% is by construction; live numbers are reported separately.
- *Why plain C# classes in `RTS.Core`?* Testable without the editor (54 tests); MonoBehaviours only adapt.
- *Why are the team colours and labels not networked?* They are presentation only: each peer computes "mine or enemy" from its own
  `PlayerRef` (`EntityVisuals`). Nothing the host decides depends on them, so they cost no bandwidth and cannot be cheated with.
- *Can a client queue the same research twice?* The client's validator cannot see queued research, so the host checks every own
  queue in `CommandRelay` before it takes the cost: the host is the final check, the client check is only for the buttons.
- *What did you not finish / what would you change?* Fill in honestly after the last test day (see MORNING.md notes).
