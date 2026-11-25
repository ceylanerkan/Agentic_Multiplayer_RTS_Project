# Automatic play test (Unity 6000.4.9f1, 2026-11-25 15:55)

`AutoPlayTest.Run` (editor only): host session, real commands, checked on the host; player 2 is a stand-in. Mouse input and the look of the UI are not tested.

**81 passed, 0 failed.**

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
| Houses | build house | ok | 3 s with 5 Villagers at (-65, 18) P1Base |
| Houses | build house | ok | 4 s with 5 Villagers at (-59, 17) P1Base |
| Houses | build house | ok | 4 s with 5 Villagers at (-57, 24) P1Base |
| Houses | 3 Houses give 15 population | ok | 5110w 5100f 5100g, pop 5/20 |
| Barracks | build barracks | ok | 8 s with 5 Villagers at (-52, 17) P1Base |
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
| Blacksmith | build blacksmith | ok | 5 s with 6 Villagers at (-51, 24) P1Base |
| Blacksmith | research iron_weapons | ok | 30 s |
| Blacksmith | Iron Weapons: Spearman +2 attack, Scout none | ok |  |
| Blacksmith | refused: Iron Weapons a second time | ok |  |
| Blacksmith | research fletching | ok | 25 s |
| Blacksmith | Fletching: Archer +1 range | ok |  |
| Blacksmith | train catapult | ok | 30 s, zone P1Base |
| Stable | build stable | ok | 7 s with 6 Villagers at (-45, 17) P1Base |
| Stable | train cavalry | ok | 20 s, zone P1Base |
| Combat | an enemy next to us is visible | ok |  |
| Combat | Cavalry hits an Archer (counter x1.5, Iron Weapons) | ok | first damage seen 15,0, one Cavalry hit 15,0 (others may hit too) |
| Combat | the Archer dies | ok |  |
| Combat | a distant enemy is hidden | ok |  |
| Catapult | Catapult hits a building (x3) | ok | damage 36,0, expected 36,0 |
| Catapult | refused: building a House on the enemy's order | ok |  |
| Dock | build dock | ok | 10 s with 6 Villagers at (-49, 43) P1Base |
| Dock | the Dock stands on the shore | ok | (-49, 43) P1Base |
| Dock | train raider_boat | ok | 15 s, zone P1Harbor |
| Dock | train warship | ok | 30 s, zone P1Harbor |
| Dock | train raider_boat | ok | 15 s, zone P1Harbor |
| Dock | ships appear side by side in the water before the Dock | ok | closest pair 4,0 m |
| Ships | a ship stops at the shore | ok | P1Harbor z=45 |
| Ships | a land unit stops at the shore | ok | P1Base z=45 |
| Ships | Raider Boat attacks an enemy Dock from the sea | ok | damage 6,0 |
| Ships | Warship shoots a land unit on the shore | ok |  |
| Ships | sunk ships free their population | ok | 19 -> 16 |
| Gathering | gather wood | ok | +10 after 31 s, node 22 m from the Town Center |
| Gathering | gather gold | ok | +10 after 46 s, node 55 m from the Town Center |
| Gathering | gather food | ok | +10 after 60 s, node 85 m from the Town Center |
| AI approval | approve: 2 Spearmen are queued | ok | Approved plan-001: 1 action(s) sent. |
| AI approval | approve: 20 Archers are refused | ok | Approved plan-002: 0 sent, 1 skipped (not enough population capacity). |
| AI approval | approve: researched technology is refused | ok | Approved plan-003: 0 sent, 1 skipped (already researched). |
| AI approval | approve: Archers to the OpenSea are refused | ok | Approved plan-004: 0 sent, 1 skipped (land units cannot enter water zones). |
| AI approval | approve: building in the enemy base is refused | ok | Approved plan-005: 0 sent, 1 skipped (cannot build in the enemy base). |
| AI approval | approve: a Dock without a shore is refused | ok | Approved plan-006: 0 sent, 1 skipped (dock needs a shore zone: your own base or NorthGate). |
| AI approval | approve: two Houses in one plan do not overlap | ok | Approved plan-007: 2 action(s) sent. |
| AI approval | reject changes nothing | ok | Rejected plan-008. Nothing was changed. |
| AI approval | approve: Cavalry moves to NorthGate | ok | Approved plan-009: 1 action(s) sent. |
| Placement | house exactly at (-88, 4) P1Base | ok | (-88, 4) P1Base |
| Placement | house exactly at (-41, -12) WestMine | ok | (-41, -12) WestMine |
| Placement | barracks exactly at (3, -2) Center | ok | (3, -2) Center |
| Placement | dock exactly at (10, 43) NorthGate | ok | (10, 43) NorthGate |
| Placement | a second House on the same ground goes beside it | ok | (-92, 4) P1Base |
| Placement | refused: House in the sea | ok |  |
| Placement | refused: House outside the map | ok |  |
| Placement | refused: Dock far from the shore | ok |  |
| Placement | refused: House in the enemy base with a wrong zone name | ok |  |
| Pathfinding | a Villager walks around the Town Center | ok | straight 18 m, walked 19 m in 5 s, ends at (-56, 26) P1Base |
| Pathfinding | a Scout walks around a wall of five Barracks | ok | straight 20 m, walked 32 m in 5 s, ends at (-10, -5) Center |
| Pathfinding | a building that appears on the way is avoided | ok | straight 30 m, walked 31 m in 5 s, ends at (20, -5) Center |
| Pathfinding | a move order into a building ends beside it | ok | (11, -5) Center |
| Pathfinding | the longest search of the match is short | ok | 1,1 ms |
| Victory | destroying the enemy Town Center wins the match | ok | result 1 |

Errors in the Unity log during the run: 0

