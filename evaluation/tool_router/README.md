# Tool router (Laya)

`TOOL_ROUTER=laya` puts a small decision model in front of the LLM. [Laya](https://huggingface.co/convaiinnovations/laya) is an encoder that
reads a typed question with its options and writes no text. For every player question it reads "which information is needed?" with one
option per MCP tool, once, on the CPU (about 0.25 s). A small classifier per tool turns that reading into "needed or not". The gateway calls
the chosen tools through MCP, and the local LLM (qwen2.5:7b in Ollama) gets the question together with the results.

```
question --> Laya (CPU): which of the 7 tools? --> MCP calls --> qwen: more tools if needed --> RTSActionPlan
```

- Laya never touches the game. The plan is written by qwen, checked by the schema, approved by the player and re-validated in Unity,
  exactly as without Laya.
- The tool order is not fixed: qwen still sees all 7 tools and calls more when a fact is missing. `estimate_plan_cost` is never called in
  advance, because qwen has to choose the items; when Laya chose it, qwen is told to call it before it answers.
- A question that the injection check flags gets no data in advance, and "Ask why" does not use the router.
- If Laya is not installed or fails, the error is logged (`ai/logs/router.jsonl`) and the question takes the normal path.

Code: [ai/gateway/tool_router.py](../../ai/gateway/tool_router.py). Log of every choice with its probabilities: `ai/logs/router.jsonl`.

## What was tried (25 November)

| Way | Needed tools found | Extra tools per question | Time |
|---|---|---|---|
| Untrained, one yes/no question per tool (`measure_base_yesno_test.md`) | 9/27 test | 0.29 | 2.2 s per question (seven passes) |
| Untrained, one multiple-choice question | 13/27 test | 0.81 | 0.43 s per question |
| Fine-tuned decision layer of Laya, 20 epochs (`measure_finetuned_dev.md`) | 30/40 held out | 1.43 | 34 minutes of wall time on a quiet laptop CPU (the laptop slept for about 14 of them), 842 MB result |
| **Laya unchanged + small classifiers** (`train_report.md`, `measure_test.md`) | **25/27 test**, 89% cross-validated | 1.48 test, 0.99 cross-validated | **1 minute, 86 KB**, 0.25 s per question |

With qwen behind it, 25 November (`results_ollama_1125_1304.*` without, `results_ollama_laya_1125_1310.*` with Laya; one after the other,
same power state):

| 21 prompts | Without Laya | With Laya |
|---|---|---|
| Average / median / slowest answer (s) | 5.23 / 3.52 / 14.75 | **3.75 / 2.95 / 9.86** |
| Tool-call accuracy | 100% | 86% |
| Valid plans | 100% | 100% |
| Injection prompts safe | 2/2 | 2/2 |
| Rejected actions | 2/7 | 2/7 |

What gets worse: qwen mostly stops calling tools of its own, so a cost calculation that Laya misses is missing (p05, p11), and one knowledge
question got an extra tool (p02). p08 answered "archers counter cavalry" although the rule was in the results. The instruction texts behind
the prefetch were adjusted while looking at these 21 prompts, so the numbers are optimistic for new questions.

With qwen behind it, 26 November, after the prompt changed for free building placement (`results_ollama.*` without, `results_ollama_laya.*`
with Laya; one after the other, charger, silent mode, default GPU split of Ollama: 25 of 29 layers on the card). The run without Laya went first:
its first question took 31.9 s because the long prompt was not yet in Ollama's cache (the model itself had been loaded before), so it is also
given without p01. Times are only comparable inside this table, not with 25 November.

| 21 prompts | Without Laya | With Laya |
|---|---|---|
| Average / median / slowest answer (s) | 8.91 / 7.99 / 31.91 | **4.83 / 4.15 / 11.32** |
| Average without p01 (s) | 7.76 | **4.96** |
| Tool-call accuracy | 95% (p11 no cost) | 86% (p02, p05, p11, as before) |
| Valid plans | 100% | 100% |
| Injection prompts safe | 2/2 | 2/2 |
| Rejected actions | 1/6 | 2/4 |

With Laya, p21 ("Research fletching for my archers.") says it can be done but proposes no action (on 25 November too); without Laya it does.
p19 ("Send my archers to the OpenSea.") proposes the move in both runs and the validator rejects it ("land units cannot enter water zones").


## Training data and what it may be used for
`questions.jsonl` holds 190 hand-labelled questions in ten groups (one tool, several tools, orders, knowledge only, no tool or attack).
The 21 prompts of `evaluation/prompts.jsonl` are the test set and are **not** in it: `router_data.py` refuses duplicates and questions that
share 75% of their words with a test prompt. The threshold (0.4) was chosen by cross-validation on the training questions only.

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
python evaluation/tool_router/router_data.py             check the data (no model)
python evaluation/tool_router/train_router.py            fit the classifiers (about 1 minute, CPU, 4 threads, low priority)
python evaluation/tool_router/measure_router.py          the 21 prompts, Laya alone
python evaluation/run_eval.py --real                     21 prompts without Laya  (results_ollama.*)
python evaluation/run_eval.py --real --router laya       21 prompts with Laya     (results_ollama_laya.*)
```
