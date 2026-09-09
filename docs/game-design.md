# Outpost: game design

Single-player sci-fi RTS survival for keyboard and mouse. One geometric-art map and five waves.

Start with headquarters, three workers and 250 minerals. Workers gather 20 minerals per load, taking 2 seconds to mine, then return automatically. HQ trains workers (50 / 6 seconds); barracks (150) train soldiers (75 / 8 seconds); turrets (100) defend automatically. Construction is instant inside the HQ perimeter and cannot obstruct reserved approach lanes. Production queues hold five jobs and wait for a free exit. Destroyed producers lose their queues without refunds.

## Commands
- Left-click / drag selects; Shift adds to selection.
- Right-click moves units, orders soldiers to attack a specific enemy, or assigns workers to minerals.
- F then terrain click orders selected soldiers to attack-move: engage nearby enemies and resume the route after combat.
- Select one barracks and right-click reachable ground to set its rally point. Newly produced soldiers attack-move there.
- Ctrl+1–9 stores friendly units; 1–9 recalls surviving members. Empty groups do nothing. Groups reset on restart.
- WASD / arrows pan; wheel zooms. Escape / right-click cancels targeting or placement. Escape otherwise pauses.
- HUD includes contextual training, construction, attack-move, controls, pause and a session sound toggle.
- Paused and finished matches reject world orders.

## Waves
Preparation is 65 seconds; cleared-wave breaks are 40 seconds. Default standard / Runner / Brute counts are 5/0/0, 6/3/0, 8/4/1, 9/6/2 and 10/8/3. Pending spawns count as hostiles and retain their type when blocked.

Runners have 60% standard health, 160% speed and 70% damage. Brutes have 250% health, 65% speed and 160% damage. All enemies use melee attacks and advance toward HQ, engaging nearby defenders. Destroy all five waves to win; losing HQ immediately ends the mission.

Tune values and compositions in DefaultStrategy. Settings without compositions retain their original count formula. No research, multiplayer, additional maps or save/load in this version.
