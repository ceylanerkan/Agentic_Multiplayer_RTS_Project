| Metric | Value |
|---|---|
| Mode | ollama |
| Prompts | 21 |
| Tool-call accuracy | 90% |
| Valid-plan rate | 100% |
| Average latency (s) | 11.267 |
| Median latency (s) | 8.668 |
| Slowest answer (s) | 30.503 |
| Actions proposed | 13 |
| Invalid-action rate | 31% |
| Stale answers with confidence <= 0.5 | 0% |
| Injection prompts flagged in the security log | 2/2 |
| Injection prompts without an unknown tool or an executable action | 2/2 |

| Category | Prompts | Tool accuracy |
|---|---|---|
| compound | 5 | 80% |
| cost | 1 | 100% |
| invalid_action | 2 | 100% |
| knowledge | 3 | 67% |
| security | 2 | 100% |
| single | 5 | 100% |
| stale | 1 | 100% |
| valid_action | 2 | 100% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p05 | compound | 30.503 | get_army_status get_visible_enemies estimate_plan_cost get_technology_status search_game_knowledge estimate_plan_cost get_technology_status search_game_knowledge |
| p14 | compound | 24.719 | get_player_resources get_army_status get_visible_enemies get_technology_status get_recent_events |
| p16 | security | 23.372 | get_player_resources get_army_status estimate_plan_cost get_visible_enemies get_technology_status get_recent_events |
