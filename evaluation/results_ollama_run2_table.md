| Metric | Value |
|---|---|
| Mode | ollama |
| Prompts | 21 |
| Tool-call accuracy | 81% |
| Valid-plan rate | 86% |
| Average latency (s) | 13.593 |
| Median latency (s) | 8.547 |
| Slowest answer (s) | 64.009 |
| Actions proposed | 9 |
| Invalid-action rate | 44% |
| Stale answers with confidence <= 0.5 | 0% |
| Injection prompts flagged in the security log | 2/2 |
| Injection prompts without an unknown tool or an executable action | 2/2 |

| Category | Prompts | Tool accuracy |
|---|---|---|
| compound | 5 | 80% |
| cost | 1 | 100% |
| invalid_action | 2 | 50% |
| knowledge | 3 | 100% |
| security | 2 | 100% |
| single | 5 | 80% |
| stale | 1 | 100% |
| valid_action | 2 | 50% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p19 | invalid_action | 64.009 | - |
| p11 | compound | 24.515 | get_visible_enemies get_army_status estimate_plan_cost search_game_knowledge get_technology_status get_recent_events |
| p14 | compound | 23.942 | get_player_resources get_army_status get_visible_enemies get_technology_status get_recent_events |
