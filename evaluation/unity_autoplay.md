# Automatic play test (Unity 6000.4.9f1, 2026-11-25 14:06)

`AutoPlayTest.Run` (editor only): host session, real commands, checked on the host; player 2 is a stand-in. Mouse input and the look of the UI are not tested.

**66 passed, 0 failed.**

| Section | Check | Result | Detail |
|---|---|---|---|
| Start | host session, Town Center and 3 Villagers | ok | 200w 200f 100g, pop 3/5 |
| Start | local player is slot 1 | ok | slot 1 |
| Start | stand-in for player 2 | ok |  |
| Town Center | refused: Age II without a Barracks | ok |  |
| Town Center | refused: Stable before Age II | ok |  |
| Town Center | refused: Spearman at the Town Center | ok |  |
| Town Center | refused: building in the enemy base | ok |  |
| Town Center | refused: moving a unit of player 2 | ok |  |
| Town Center | train villager | ok | 12 s, zone P1Base |
| Town Center | train villager | ok | 12 s, zone P1Base |
| Town Center | refused: Villager above the population limit (5200w 5100f 5100g, pop 5/5) | ok |  |
| Houses | build house | ok | 4 s with 5 Villagers at z=13 |
| Houses | build house | ok | 6 s with 5 Villagers at z=13 |
| Houses | build house | ok | 5 s with 5 Villagers at z=25 |
| Houses | 3 Houses give 15 population | ok | 5110w 5100f 5100g, pop 5/20 |
| Barracks | build barracks | ok | 12 s with 5 Villagers at z=25 |
| Barracks | train scout | ok | 10 s, zone P1Base |
| Barracks | train spearman | ok | 14 s, zone P1Base |
| Barracks | train archer | ok | 16 s, zone P1Base |
| Barracks | queue holds 5, the sixth is refused | ok | queue 5, queue is full, 4725w 4750f 5065g, pop 13/20 |
| Barracks | queue is worked off, nobody stands inside another | ok | 6 Spearmen, closest pair 1,6 m |
| Age II | the button says why Age II does not start | ok | finish training before researching Age II |
| Age II | refused: Age II while the Town Center trains | ok |  |
| Age II | no training while Age II is researched | ok | cannot train while researching Age II |
| Age II | refused: Villager during the Age II research | ok |  |
| Age II | refused: Age II a second time | ok |  |
| Age II | Age II reached | ok | age 2 |
| Age II | research horse_collar | ok | 25 s |
| Blacksmith | build blacksmith | ok | 6 s with 6 Villagers at z=13 |
| Blacksmith | research iron_weapons | ok | 30 s |
| Blacksmith | Iron Weapons: Spearman +2 attack, Scout none | ok |  |
| Blacksmith | refused: Iron Weapons a second time | ok |  |
| Blacksmith | research fletching | ok | 25 s |
| Blacksmith | Fletching: Archer +1 range | ok |  |
| Blacksmith | train catapult | ok | 30 s, zone P1Base |
| Stable | build stable | ok | 11 s with 6 Villagers at z=43 |
| Stable | train cavalry | ok | 20 s, zone P1Base |
| Combat | an enemy next to us is visible | ok |  |
| Combat | Cavalry hits an Archer (counter x1.5, Iron Weapons) | ok | first damage seen 15,0, one Cavalry hit 15,0 (others may hit too) |
| Combat | the Archer dies | ok |  |
| Combat | a distant enemy is hidden | ok |  |
| Catapult | Catapult hits a building (x3) | ok | damage 36,0, expected 36,0 |
| Catapult | refused: building a House on the enemy's order | ok |  |
| Dock | a coastal slot is still free for the Dock | ok | 1 free |
| Dock | build dock | ok | 8 s with 6 Villagers at z=43 |
| Dock | train raider_boat | ok | 15 s, zone P1Harbor |
| Dock | train warship | ok | 30 s, zone P1Harbor |
| Dock | train raider_boat | ok | 15 s, zone P1Harbor |
| Dock | ships appear side by side in the own harbor | ok | closest pair 4,0 m |
| Ships | a ship stops at the shore | ok | P1Harbor z=45 |
| Ships | a land unit stops at the shore | ok | P1Base z=45 |
| Ships | Raider Boat attacks an enemy Dock from the sea | ok | damage 6,0 |
| Ships | Warship shoots a land unit on the shore | ok |  |
| Ships | sunk ships free their population | ok | 19 -> 16 |
| Gathering | gather wood | ok | +10 after 27 s, node 22 m from the Town Center |
| Gathering | gather gold | ok | +10 after 46 s, node 55 m from the Town Center |
| Gathering | gather food | ok | +10 after 59 s, node 85 m from the Town Center |
| AI approval | approve: 2 Spearmen are queued | ok | Approved plan-001: 1 action(s) sent. |
| AI approval | approve: 20 Archers are refused | ok | Approved plan-002: 0 sent, 1 skipped (not enough population capacity). |
| AI approval | approve: researched technology is refused | ok | Approved plan-003: 0 sent, 1 skipped (already researched). |
| AI approval | approve: Archers to the OpenSea are refused | ok | Approved plan-004: 0 sent, 1 skipped (land units cannot enter water zones). |
| AI approval | approve: building in the enemy base is refused | ok | Approved plan-005: 0 sent, 1 skipped (cannot build in the enemy base). |
| AI approval | approve: no free slot is said, not 'sent' | ok | Approved plan-006: 0 sent, 1 skipped (no free build slot in P1Base). |
| AI approval | reject changes nothing | ok | Rejected plan-007. Nothing was changed. |
| AI approval | approve: Cavalry moves to NorthGate | ok | Approved plan-008: 1 action(s) sent. |
| Victory | destroying the enemy Town Center wins the match | ok | result 1 |

Errors in the Unity log during the run: 0

