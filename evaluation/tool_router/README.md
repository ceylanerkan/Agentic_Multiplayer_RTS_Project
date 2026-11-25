# Tool router (Laya)

`TOOL_ROUTER=laya` puts a small decision model in front of the LLM. For every question [Laya](https://huggingface.co/convaiinnovations/laya)
answers one yes/no question per MCP tool in one forward pass on the CPU. The gateway calls the chosen tools through MCP, and the local LLM
(qwen2.5:7b in Ollama) gets the question together with the results.

```
question ─► Laya (CPU): which of the 7 tools? ─► MCP calls ─► qwen: more tools if needed ─► RTSActionPlan
```

- Laya writes no text and never touches the game. The plan is written by qwen, checked by the schema, approved by the player and
  re-validated in Unity, exactly as without Laya.
- The tool order is not fixed: qwen still sees all 7 tools and calls more when a fact is missing. `estimate_plan_cost` is never called in
  advance, because qwen has to choose the items.
- A question that the injection check flags gets no data in advance, and "Ask why" does not use the router.
- If Laya is not installed or fails, the error is logged (`ai/logs/router.jsonl`) and the question takes the normal path.

Code: [ai/gateway/tool_router.py](../../ai/gateway/tool_router.py). Log of every choice with its probabilities: `ai/logs/router.jsonl`.

## Training data and what it may be used for
`questions.jsonl` holds hand-labelled questions in ten groups (one tool, several tools, orders, knowledge only, no tool or attack).
The 21 prompts of `evaluation/prompts.jsonl` are the test set and are **not** in it: `router_data.py` refuses duplicates and questions that
share 75% of their words with a test prompt. Every 6th question of a group is held out as the dev part; thresholds are chosen on the dev part only.

How a label is decided:

| Tool | Needed when the question is about |
|---|---|
| `get_player_resources` | own wood, food, gold, population, age; buying anything now; general advice |
| `get_army_status` | own units or buildings; orders to own units; training or building (is the building there?); advice |
| `get_visible_enemies` | enemies in sight, threats, attack and defence |
| `get_technology_status` | what is researched or running; starting a research |
| `get_recent_events` | what happened in the last minutes |
| `estimate_plan_cost` | a price, "can I afford", an order or a plan that buys something |
| `search_game_knowledge` | a rule, a fact, a counter, strategy advice |

Small talk, orders outside the game and requests for hidden data need no tool.

## Commands
With the Python of the Laya environment ([docs/setup.md](../../docs/setup.md), section 4), from the repository root:

```
python evaluation/tool_router/router_data.py                             check the data (no model)
python evaluation/tool_router/measure_router.py --model base             the published model on the 21 prompts
python evaluation/tool_router/train_router.py                            train the decision layer (CPU, 4 threads, low priority)
python evaluation/tool_router/measure_router.py --model tuned --data dev thresholds on the held-out part
python evaluation/tool_router/measure_router.py --model tuned            the trained model on the 21 prompts
python evaluation/run_eval.py --real                                     21 prompts without Laya  (results_ollama.*)
python evaluation/run_eval.py --real --router laya                       21 prompts with Laya     (results_ollama_laya.*)
```
