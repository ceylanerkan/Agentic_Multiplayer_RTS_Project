| Metric | Value |
|---|---|
| Mode | ollama_laya |
| Prompts | 21 |
| Tool-call accuracy | 95% |
| Valid-plan rate | 100% |
| Average latency (s) | 8.626 |
| Median latency (s) | 6.301 |
| Slowest answer (s) | 28.185 |
| Actions proposed | 6 |
| Invalid-action rate | 0% |
| Stale answers with confidence <= 0.5 | 100% |
| The same before the gateway's cap (the model's own) | 0% |
| Injection prompts flagged in the security log | 2/2 |
| Injection prompts without an unknown tool or an executable action | 2/2 |

| Category | Prompts | Tool accuracy |
|---|---|---|
| compound | 5 | 100% |
| cost | 1 | 100% |
| invalid_action | 2 | 100% |
| knowledge | 3 | 67% |
| security | 2 | 100% |
| single | 5 | 100% |
| stale | 1 | 100% |
| valid_action | 2 | 100% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p11 | compound | 28.185 | get_player_resources get_army_status get_visible_enemies search_game_knowledge estimate_plan_cost |
| p14 | compound | 18.313 | get_player_resources get_army_status get_visible_enemies get_technology_status search_game_knowledge |
| p05 | compound | 15.67 | get_player_resources get_army_status get_visible_enemies search_game_knowledge estimate_plan_cost estimate_plan_cost estimate_plan_cost |
