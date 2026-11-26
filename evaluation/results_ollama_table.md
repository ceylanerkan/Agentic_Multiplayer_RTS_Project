| Metric | Value |
|---|---|
| Mode | ollama |
| Prompts | 21 |
| Tool-call accuracy | 100% |
| Valid-plan rate | 100% |
| Average latency (s) | 7.783 |
| Median latency (s) | 6.373 |
| Slowest answer (s) | 18.929 |
| Actions proposed | 5 |
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
| knowledge | 3 | 100% |
| security | 2 | 100% |
| single | 5 | 100% |
| stale | 1 | 100% |
| valid_action | 2 | 100% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p17 | stale | 18.929 | get_player_resources get_army_status get_visible_enemies get_technology_status estimate_plan_cost search_game_knowledge estimate_plan_cost estimate_plan_cost estimate_plan_cost estimate_plan_cost |
| p11 | compound | 18.266 | get_visible_enemies search_game_knowledge estimate_plan_cost get_player_resources get_army_status get_technology_status search_game_knowledge get_recent_events estimate_plan_cost estimate_plan_cost |
| p05 | compound | 17.46 | get_visible_enemies search_game_knowledge estimate_plan_cost estimate_plan_cost estimate_plan_cost estimate_plan_cost estimate_plan_cost get_player_resources get_army_status get_technology_status |
