# Automatic two-player match (2026-11-25 15:59)

Two instances of the Windows test build (Fusion host and client through Photon, no window). The client plays with a virtual mouse through the normal scripts. `Testing/AutoMatch.cs` is not in the game build.

**35 passed, 0 failed.**

Same state on both screens: 101 client samples with 1143 object values (position within 1.5 m, HP, existence): **0 differences**, largest distance 0.38 m; 101 samples with 202 player values (resources, population, age, result): **0 differences**.

| Side | Section | Check | Result | Detail |
|---|---|---|---|---|
| host (P1) | start | host is player 1 with a Town Center and 3 Villagers | ok |  |
| host (P1) | start | the second player joined | ok |  |
| host (P1) | start | player 2 has slot 2 and its own base | ok |  |
| host (P1) | play | the host builds a House | ok | (-85,0, 8,0) |
| host (P1) | play | the House of player 2 is finished on the host too | ok |  |
| host (P1) | play | the host sees the attack on its Town Center | ok | 1500 -> 1494 |
| host (P1) | end | the host sees DEFEAT | ok | DEFEAT |
| host (P1) | end | the other player left: back in the menu | ok | Rakip oyundan ayrildi, mac bitti. |
| client (P2) | start | joined as player 2 with a Town Center and 3 Villagers | ok |  |
| client (P2) | start | the camera starts at the own base | ok | (65,0, 8,2) |
| client (P2) | start | the enemy base is hidden by the fog | ok |  |
| client (P2) | selection | a click selects one Villager | ok | 1 selected, screen 1280x720, mouse virtual (640.00, 411.72) |
| client (P2) | selection | the panel shows the building buttons | ok |  |
| client (P2) | selection | dragging a box selects all three | ok | 3 selected |
| client (P2) | placement | the button starts the ghost | ok |  |
| client (P2) | placement | free ground: green, under the mouse | ok | (52,0, 10,0) |
| client (P2) | placement | on the Town Center: red | ok |  |
| client (P2) | placement | on the sea: red | ok |  |
| client (P2) | placement | in the enemy base: red | ok |  |
| client (P2) | placement | at the edge of the Town Center: moves beside it | ok | mouse 5.5 m beside the centre, ghost 5,9 m |
| client (P2) | placement | a click places the House there | ok | (52,0, 10,0) |
| client (P2) | placement | the Villagers build it, capacity 10 | ok | capacity 10 |
| client (P2) | placement | a right click cancels the ghost, nobody walks off | ok |  |
| client (P2) | placement | Dock inland: red | ok |  |
| client (P2) | placement | Dock near the shore: green, straight to the shore | ok | (52,0, 42,5) |
| client (P2) | placement | a click places the Dock on the shore | ok | (52,0, 42,5) |
| client (P2) | commands | a right click moves a Villager | ok | (45,8, 2,5) |
| client (P2) | commands | a click selects the Town Center | ok | town_center |
| client (P2) | commands | the button trains a Villager | ok | queue 1 |
| client (P2) | commands | the Villager appears | ok |  |
| client (P2) | commands | a right click on a tree: wood arrives | ok | 2050 -> 2060 |
| client (P2) | attack | the hidden enemy Town Center is only ground | ok |  |
| client (P2) | attack | it appears when the Villagers come near | ok |  |
| client (P2) | attack | a right click attacks it | ok | 1500 -> 1494 |
| client (P2) | end | the client sees VICTORY | ok | VICTORY |

Errors in the logs: host 0, client 0
