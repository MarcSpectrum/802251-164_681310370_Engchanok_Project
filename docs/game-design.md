# Outpost: game design

Single-player sci-fi RTS survival for keyboard and mouse. One geometric-art map and five waves.

Start with headquarters, three workers and 250 minerals. Workers gather 20 minerals per load, taking 2 seconds to mine, then return automatically. HQ trains workers (50 / 6 seconds); barracks (150) train soldiers (75 / 8 seconds); turrets (100) defend automatically. Construction is instant inside the HQ perimeter and cannot obstruct reserved approach lanes. Production queues hold five jobs and wait for a free exit. Destroyed producers lose their queues without refunds.

## Commands
- Left-click / drag selects; Shift adds to selection.
- Right-click moves units, orders soldiers to attack a specific enemy, or assigns workers to minerals.
- F then terrain click orders selected soldiers to attack-move: engage nearby enemies and resume the route after combat.
- Select one barracks and right-click reachable ground to set its rally point. Newly produced soldiers attack-move there.
- Ctrl+1�9 stores friendly units; 1�9 recalls surviving members. Empty groups do nothing. Groups reset on restart.
- WASD / arrows pan; wheel zooms. Escape / right-click cancels targeting or placement. Escape otherwise pauses.
- Object popups include contextual training, construction, attack-move, and rally targeting; the HUD retains controls, pause and a session sound toggle.
- Paused and finished matches reject world orders.

## Waves
Preparation is 65 seconds; cleared-wave breaks are 40 seconds. Default standard / Runner / Brute counts are 5/0/0, 6/3/0, 8/4/1, 9/6/2 and 10/8/3. Pending spawns count as hostiles and retain their type when blocked.

Runners have 60% standard health, 160% speed and 70% damage. Brutes have 250% health, 65% speed and 160% damage. All enemies use melee attacks and advance toward HQ, engaging nearby defenders. Destroy all five waves to win; losing HQ immediately ends the mission.

Tune values and compositions in DefaultStrategy. Settings without compositions retain their original count formula. No multiplayer, additional maps or save/load in this version.

## Research and learning
Play offers guided practice the first time; Learn to Play always replays it. Practice has 1000 minerals, no enemy waves, and six action-based steps. Skip or finish starts a fresh normal mission. Tutorial completion/skipping is remembered locally; no match progress is saved.

Object popups show relevant commands when clicked. Move and Gather buttons enter click targeting; Escape/right-click cancels. Existing shortcuts remain supported. Contextual labels explain costs, requirements and queue limits; preparation shows the upcoming enemy composition.

Research is offered in the headquarters popup. Only one project runs at a time, independently of unit training; minerals are spent immediately, with no cancellation or queue. Each upgrade can be completed once per mission and benefits existing and future entities.

| Research | Minerals | Seconds | Benefit |
| --- | ---: | ---: | --- |
| Improved Mining | 125 | 20 | +50% worker carrying capacity on the next extraction |
| Soldier Weapons | 150 | 25 | +25% soldier damage |
| Turret Weapons | 150 | 25 | +25% turret damage |

Pause freezes research; ending or restarting a mission stops or resets it. Values live in DefaultStrategy; existing economy and enemy tuning is preserved.

## Art direction: stylized sci-fi

The presentation uses refined geometric models and industrial details designed to remain readable from the tactical camera. Dark navy metal supports cyan allied soldiers and structures, amber workers, teal mineral deposits, and coral hostiles. Silhouettes distinguish cargo-carrying workers, armored rifle soldiers, low-profile runners, heavy brutes, the command center, hangar, and turret. Color reinforces these shapes rather than providing the only distinction.

Warm directional light and cool ambient light reveal form; luminous accents remain restrained. Ground plates, approach markers, and perimeter machinery establish an operational outpost. Scenery has no collision and does not alter navigation, gathering access, or construction footprints.

The interface uses consistent navy panels, cyan rules, clear action states, separate minerals/HQ/wave status, and training/research progress bars. The menu presents a cosmetic outpost diorama. Short tracers, impacts, expanding command rings, construction pulses, and destruction bursts communicate actions without obscuring targets. Sound cues distinguish orders, construction, research completion, and incoming waves; the sound toggle mutes them.

This polish milestone preserves the existing map, balance, five waves, research, tutorial, and controls. Effects follow the match clock, freeze on pause, and are cleared when a match ends or restarts. No camera shake, asset packs, or new dependencies are introduced.

## Camera and live inspection

Left-click selects and inspects an object in a contextual popup without pausing or moving the tactical camera. HQ offers worker training, instant construction, and research; workers offer Move/Gather; soldiers offer Move/Attack-move; barracks offer soldier training and rally targeting. Turrets, enemies, and deposits show live information only. Groups show commands applicable to their members.

The popup tracks its target, clamps within the screen, and remains stationary under the pointer. Scroll the HQ action list to reach research. Relevant unavailable actions explain their requirements. Empty-ground clicks clear selection; X dismisses the popup while retaining friendly selection. Destroyed targets close their popup (or fall back to surviving group members). Targeted actions hide the popup until completion/cancellation. UI clicks never issue world orders. The top status strip and tutorial remain visible.

WASD/arrows pan; the wheel zooms outside UI. C frames friendly selection; Home restores the HQ overview. Escape/right-click cancels targeting; Escape otherwise pauses. Manual pause and mission completion still gate commands. There is no separate close-up or I-key inspection mode.
