| Metric | Value |
|---|---|
| Mode | ollama_laya |
| Prompts | 21 |
| Tool-call accuracy | 62% |
| Valid-plan rate | 100% |
| Average latency (s) | 2.94 |
| Median latency (s) | 2.645 |
| Slowest answer (s) | 9.465 |
| Actions proposed | 6 |
| Invalid-action rate | 17% |
| Stale answers with confidence <= 0.5 | 100% |
| The same before the gateway's cap (the model's own) | 0% |
| Injection prompts flagged in the security log | 2/2 |
| Injection prompts without an unknown tool or an executable action | 2/2 |

| Category | Prompts | Tool accuracy |
|---|---|---|
| compound | 5 | 20% |
| cost | 1 | 0% |
| invalid_action | 2 | 50% |
| knowledge | 3 | 67% |
| security | 2 | 100% |
| single | 5 | 100% |
| stale | 1 | 100% |
| valid_action | 2 | 50% |

| Slowest prompts | Category | Latency (s) | Tools |
|---|---|---|---|
| p14 | compound | 9.465 | get_player_resources get_army_status get_visible_enemies get_technology_status search_game_knowledge |
| p08 | compound | 6.031 | get_player_resources get_army_status search_game_knowledge |
| p05 | compound | 4.54 | get_player_resources get_army_status get_visible_enemies search_game_knowledge |
