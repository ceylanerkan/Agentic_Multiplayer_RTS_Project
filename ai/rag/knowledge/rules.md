# Game Rules and Strategy Notes

Each `##` section is one retrieval chunk. The id in brackets is its `source_id`.

## [rule_starting_state_v1] Starting state
Each player starts with 1 Town Center, 3 Villagers, 200 wood, 200 food and 100 gold. Starting population is 3 out of a capacity of 5 (provided by the Town Center).

## [rule_population_v1] Population
Every unit costs 1 population. Population capacity is the sum of 5 from the Town Center and 5 from each completed House, up to a hard maximum of 50. A unit cannot be queued for training if it would exceed the current capacity.

## [rule_resources_v1] Resources and gathering
There are three resources: wood (from trees), food (from berry bushes and farms) and gold (from gold mines). A Villager gathers 0.6 resource per second, carries at most 10, and must walk back to a Town Center to deposit. Resources are only added to the player's stockpile when deposited. Resource nodes are finite and disappear when empty.

## [rule_building_prereqs_v1] Building and training prerequisites
Barracks and Blacksmith have no prerequisites. Stable requires Age II. Spearman and Archer are trained at the Barracks. Cavalry is trained at the Stable. Villagers are trained at the Town Center. Age II requires a completed Barracks. Fletching requires a Blacksmith and Age II. A building must be fully constructed before it can train or research.

## [rule_combat_counters_v1] Combat and counters
Damage per hit = max(1, attack x multiplier - target armor). The multiplier is 1.5 when the attacker counters the target and 0.75 when the target counters the attacker; otherwise 1.0. The counter triangle is: Spearman beats Cavalry, Cavalry beats Archer, Archer beats Spearman. Cavalry also deal 1.5x damage to Villagers. Units automatically attack enemies that come into range while idle.

## [rule_win_condition_v1] Win condition
A player wins when the opponent's Town Center is destroyed. The Town Center cannot be rebuilt. There is no other victory condition.

## [rule_fog_of_war_v1] Fog of war
Each player only sees the area within the line of sight of their own units and buildings. Enemy units and buildings outside that area are hidden. The AI Strategic Commander only receives what the player can currently see; it cannot know the opponent's resources, hidden army or unexplored base.

## [rule_map_zones_v1] Map zones
The map is divided into named zones used for commands and reports: P1Base, P2Base, NorthGate, SouthField, EastForest, WestMine and Center. NorthGate and SouthField are the main paths between the two bases. EastForest has most of the wood; WestMine has most of the gold; SouthField has berry bushes.

## [strategy_early_economy_v1] Early economy strategy
In the first minutes, keep the Town Center training Villagers and build a House before reaching the population cap. A balanced opening puts about half of the Villagers on food, a third on wood and the rest on gold. Build a Barracks early; it is needed both for defence and for Age II.

## [strategy_defend_cavalry_v1] Defending against cavalry
Spearmen are the most cost-efficient answer to Cavalry and cost no gold, so training them does not delay gold-based upgrades such as Iron Weapons or Age II. Keep Archers behind Spearmen when Cavalry attack. Move Villagers away from the raided zone if the army is not yet in position.

## [strategy_defend_archers_v1] Defending against archers
Archers beat Spearmen. Answer massed Archers with Cavalry (requires Age II and a Stable) or with your own Archers. Avoid sending Spearmen alone against Archers.

## [strategy_tech_timing_v1] When to research technology
Research Iron Weapons when you have, or are about to have, at least 6 military units; with a tiny army it is poor value. Advance to Age II when your economy can spare 400 food and 100 gold without leaving you defenceless. Horse Collar is worth researching early if 4 or more Villagers gather food.
