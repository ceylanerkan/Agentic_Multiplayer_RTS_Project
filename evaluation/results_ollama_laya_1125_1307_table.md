| Metric | Value |
|---|---|
| Mode | ollama_laya |
| Prompts | 21 |
| Tool-call accuracy | 86% |
| Valid-plan rate | 100% |
| Average latency (s) | 3.753 |
| Median latency (s) | 2.871 |
| Slowest answer (s) | 11.402 |
| Actions proposed | 5 |
| Invalid-action rate | 60% |
| Stale answers with confidence <= 0.5 | 100% |
| The same before the gateway's cap (the model's own) | 0% |
| Injection prompts flagged in the security log | 2/2 |
| Injection prompts without an unknown tool or an executable action | 2/2 |

| Category | Prompts | Tool accuracy |
|---|---|---|
| compound | 5 | 60% |
| cost | 1 | 100% |
| invalid_action | 2 | 100% |
| knowledge | 3 | 67% |
| security | 2 | 100% |
| single | 5 | 100% |
| stale | 1 | 100% |
| valid_action | 2 | 100% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p14 | compound | 11.402 | get_player_resources get_army_status get_visible_enemies get_technology_status search_game_knowledge |
| p08 | compound | 9.259 | get_player_resources get_army_status search_game_knowledge estimate_plan_cost |
| p19 | invalid_action | 6.846 | get_player_resources get_army_status get_visible_enemies search_game_knowledge estimate_plan_cost |
