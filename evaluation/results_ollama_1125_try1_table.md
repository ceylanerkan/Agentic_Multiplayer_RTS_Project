| Metric | Value |
|---|---|
| Mode | ollama |
| Prompts | 21 |
| Tool-call accuracy | 90% |
| Valid-plan rate | 100% |
| Average latency (s) | 11.505 |
| Median latency (s) | 4.248 |
| Slowest answer (s) | 115.191 |
| Actions proposed | 7 |
| Invalid-action rate | 29% |
| Stale answers with confidence <= 0.5 | 100% |
| The same before the gateway's cap (the model's own) | 0% |
| Injection prompts flagged in the security log | 2/2 |
| Injection prompts without an unknown tool or an executable action | 2/2 |

| Category | Prompts | Tool accuracy |
|---|---|---|
| compound | 5 | 80% |
| cost | 1 | 0% |
| invalid_action | 2 | 100% |
| knowledge | 3 | 100% |
| security | 2 | 100% |
| single | 5 | 100% |
| stale | 1 | 100% |
| valid_action | 2 | 100% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p17 | stale | 115.191 | get_player_resources get_army_status get_visible_enemies get_technology_status estimate_plan_cost estimate_plan_cost search_game_knowledge search_game_knowledge search_game_knowledge search_game_knowledge |
| p11 | compound | 20.6 | get_army_status get_visible_enemies search_game_knowledge estimate_plan_cost estimate_plan_cost estimate_plan_cost estimate_plan_cost get_player_resources search_game_knowledge search_game_knowledge |
| p05 | compound | 18.652 | get_army_status get_visible_enemies search_game_knowledge estimate_plan_cost estimate_plan_cost estimate_plan_cost estimate_plan_cost estimate_plan_cost estimate_plan_cost estimate_plan_cost |
