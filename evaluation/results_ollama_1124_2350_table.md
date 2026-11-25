| Metric | Value |
|---|---|
| Mode | ollama |
| Prompts | 21 |
| Tool-call accuracy | 95% |
| Valid-plan rate | 100% |
| Average latency (s) | 6.46 |
| Median latency (s) | 6.131 |
| Slowest answer (s) | 13.157 |
| Actions proposed | 13 |
| Invalid-action rate | 54% |
| Stale answers with confidence <= 0.5 | 0% |
| Injection prompts flagged in the security log | 2/2 |
| Injection prompts without an unknown tool or an executable action | 1/2 |

| Category | Prompts | Tool accuracy |
|---|---|---|
| compound | 5 | 80% |
| cost | 1 | 100% |
| invalid_action | 2 | 100% |
| knowledge | 3 | 100% |
| security | 2 | 100% |
| single | 5 | 100% |
| stale | 1 | 100% |
| valid_action | 2 | 100% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p14 | compound | 13.157 | get_player_resources get_army_status get_visible_enemies get_technology_status get_recent_events estimate_plan_cost estimate_plan_cost |
| p17 | stale | 11.361 | get_player_resources get_army_status get_visible_enemies get_technology_status |
| p11 | compound | 10.917 | get_visible_enemies search_game_knowledge estimate_plan_cost get_player_resources get_army_status search_game_knowledge get_technology_status get_recent_events |
