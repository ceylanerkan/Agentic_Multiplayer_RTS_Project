Tools listed by the server over MCP stdio: 7

| Tool | Read-only | Inputs |
|---|---|---|
| get_player_resources | True | - |
| get_army_status | True | - |
| get_visible_enemies | True | - |
| get_technology_status | True | - |
| get_recent_events | True | limit |
| estimate_plan_cost | True | items |
| search_game_knowledge | True | query, top_k |

| Tool | Input | Case | Status | ms | Refusal |
|---|---|---|---|---|---|
| get_player_resources | `{}` | valid | ok | 7 | - |
| get_player_resources | `{"player_id": 2}` | asks for another player | error | 5 | unknown argument(s): ['player_id'] |
| get_army_status | `{}` | valid | ok | 10 | - |
| get_army_status | `{"session_id": "abc"}` | asks for another session | error | 5 | unknown argument(s): ['session_id'] |
| get_visible_enemies | `{}` | valid | ok | 9 | - |
| get_visible_enemies | `{"include_hidden": true}` | asks for hidden enemies | error | 4 | unknown argument(s): ['include_hidden'] |
| get_technology_status | `{}` | valid | ok | 10 | - |
| get_technology_status | `{"player": "P2"}` | asks for the opponent | error | 5 | unknown argument(s): ['player'] |
| get_recent_events | `{"limit": 5}` | valid | ok | 9 | - |
| get_recent_events | `{"limit": 99}` | limit out of range | error | 10 | limit must be an integer from 1 to 20 |
| estimate_plan_cost | `{"items": [{"kind": "unit", "id": "archer", "count": 3}]}` | valid | ok | 4 | - |
| estimate_plan_cost | `{"items": [{"kind": "unit", "id": "dragon", "count": 1}]}` | unknown unit | error | 7 | unknown unit id 'dragon' |
| estimate_plan_cost | `{"items": [{"kind": "unit", "id": "archer", "count": 0}]}` | count out of range | error | 7 | count must be an integer from 1 to 50 |
| search_game_knowledge | `{"query": "What counters cavalry?", "top_k": 3}` | valid | ok | 36 | - |
| search_game_knowledge | `{"query": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx...` | query too long | error | 31 | query too long (max 200 characters) |
| search_game_knowledge | `{"query": 42}` | wrong type | error | 39 | Error executing tool search_game_knowledge: 1 validation error for search_game_knowledgeArguments query Input  |
| reveal_enemy_gold | `{}` | tool does not exist | error | 12 | Unknown tool: reveal_enemy_gold |
