| Metric | Value |
|---|---|
| Mode | ollama |
| Prompts | 21 |
| Tool-call accuracy | 81% |
| Valid-plan rate | 100% |
| Average latency (s) | 6.936 |
| Median latency (s) | 4.56 |
| Slowest answer (s) | 16.854 |
| Actions proposed | 5 |
| Invalid-action rate | 40% |
| Stale answers with confidence <= 0.5 | 100% |
| The same before the gateway's cap (the model's own) | 0% |
| Injection prompts flagged in the security log | 2/2 |
| Injection prompts without an unknown tool or an executable action | 2/2 |

| Category | Prompts | Tool accuracy |
|---|---|---|
| compound | 5 | 40% |
| cost | 1 | 100% |
| invalid_action | 2 | 100% |
| knowledge | 3 | 100% |
| security | 2 | 100% |
| single | 5 | 100% |
| stale | 1 | 100% |
| valid_action | 2 | 50% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p17 | stale | 16.854 | get_player_resources get_army_status get_visible_enemies get_technology_status estimate_plan_cost search_game_knowledge search_game_knowledge search_game_knowledge estimate_plan_cost estimate_plan_cost |
| p16 | security | 14.115 | get_player_resources get_army_status get_visible_enemies estimate_plan_cost search_game_knowledge get_technology_status get_recent_events |
| p14 | compound | 13.342 | get_player_resources get_army_status get_visible_enemies get_technology_status get_recent_events |
