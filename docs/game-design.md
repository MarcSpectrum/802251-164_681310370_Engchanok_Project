# Outpost: game design

Single-player sci-fi RTS survival for keyboard and mouse. One geometric-art map and five waves.

Start with headquarters, three workers and 250 minerals. Workers gather 20 minerals per load, taking 2 seconds to mine, then return automatically. HQ trains workers; three production buildings train the combat roster below; turrets (100) defend automatically; supply relays (75) raise the army cap. Construction is instant inside the HQ perimeter and cannot obstruct reserved approach lanes. Production queues hold five jobs and wait for a free exit. Destroyed producers lose their queues without refunds.

The four deposits hold 1400 minerals each. That is roughly one mission's spending, so deposits run dry and every purchase carries an opportunity cost.

## Roster

Six trainable units across three production buildings. Barracks (150) trains soldiers and defenders; ranger post (140) trains rangers; support bay (175) trains medics and engineers. Each production building provides 4 supply and accepts a rally point, and brutes will besiege all of them.

| Unit | From | Minerals | Supply | Health | Armor | Range | Damage | Speed | Training |
| --- | --- | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: |
| Worker | Headquarters | 50 | 1 | 70 | Light | - | - | 5 | 6s |
| Soldier | Barracks | 75 | 2 | 120 | Medium | 7 | 15 | 5 | 8s |
| Defender | Barracks | 115 | 3 | 300 | Heavy | 2.5 | 12 | 3.8 | 11s |
| Ranger | Ranger post | 90 | 2 | 80 | Light | 13 | 13 | 5.5 | 9s |
| Medic | Support bay | 100 | 2 | 90 | Light | 8 | - | 5 | 10s |
| Engineer | Support bay | 85 | 2 | 90 | Light | 4 | 6 | 5 | 9s |

Medics mend wounded units for free. Engineers repair damaged structures and pay minerals for every point restored, so holding a turret together competes with building the next one; with nothing left to mend an engineer fights, badly. Both accept attack-move so they advance with the army. A production queue holds five jobs of any mix, and each queued job reserves its own supply the moment it is ordered.

## Supply

Every trained unit occupies supply. Headquarters provides 10, each production building 4, each supply relay 8, up to a hard limit of 60. Training is refused at the cap, and queued jobs reserve their supply the moment they are enqueued, so a full queue can never overshoot it. Supply is recounted from the living entity list every frame, so destroyed producers and relays lower the cap immediately.

This is the mission's central tension: every worker trained is a soldier that cannot be, and every relay is minerals not spent on defence.

## Armor and counters

Each entity has an armor class: Light (workers, rangers, medics, engineers, runners, lancers, wardens), Medium (soldiers, standard hostiles, breakers), Heavy (defenders, brutes, juggernauts) or Structure. Armor both counters and defends: damage is scaled by the attacker's row against the target's class. Structures are exempt in both directions, so headquarters and turret balance stay governed by their own numbers.

| Attacker | vs Light | vs Medium | vs Heavy |
| --- | ---: | ---: | ---: |
| Soldier | 135% | 115% | 55% |
| Ranger | 175% | 100% | 40% |
| Defender | 60% | 110% | 165% |
| Turret | 50% | 100% | 160% |
| Engineer | 100% | 100% | 100% |
| Runner | 130% | 95% | 45% |
| Standard hostile | 100% | 100% | 80% |
| Brute | 150% | 110% | 70% |
| Lancer | 100% | 115% | 90% |
| Breaker | 70% | 95% | 160% |
| Juggernaut | 120% | 120% | 85% |

Wardens deal no damage at all and so have no row.

No single answer covers a mixed wave. Rangers shred runners but are Light themselves, and runners hit Light hardest, so rangers need a defender screen. Defenders and turrets crush brutes, and brutes only manage 70% against Heavy, so a screen genuinely holds. Brutes flatten anything Light left exposed. The preparation preview names each group's armor class, so the next wave is intelligence the player is expected to act on. Research bonuses and armor scaling compose multiplicatively.

Armor defended nothing two milestones ago, when the player had one unit type and there was nothing for it to express. With a defender in the roster it has to work in both directions, or its Heavy class is decoration.

The hostile roster then exposed the other half of the problem: every enemy was weak against Heavy — brutes 70%, runners 45%, standard hostiles 80% — so a defender screen was the universally correct answer and no wave ever punished it. The **breaker** exists to close that hole. At 160% against Heavy it is the only hostile that beats a screen, and at 70% against Light it cannot also answer everything else: soldiers, at 115% against its Medium armor, are the intended response. Defenders stop being a default and become one choice among several.

## Commands
- Left-click / drag selects; Shift adds to selection.
- Right-click moves units, orders soldiers to attack a specific enemy, or assigns workers to minerals.
- F then terrain click orders selected soldiers to attack-move: engage nearby enemies and resume the route after combat.
- Select one barracks and right-click reachable ground to set its rally point. Newly produced soldiers attack-move there.
- Ctrl+1�9 stores friendly units; 1�9 recalls surviving members. Empty groups do nothing. Groups reset on restart.
- WASD / arrows pan; wheel zooms. Escape / right-click cancels targeting or placement. Escape otherwise pauses.
- Object popups include contextual training, construction, attack-move, and rally targeting; the HUD retains controls, pause and a session sound toggle.
- Q, E, R, T, G, Z, X, V and B trigger the open popup's visible actions from top to bottom, with each key shown on its button. A hotkey is exactly a click, so every existing check applies.
- The bottom-left minimap: left-click or drag moves the camera, right-click orders the selection (or sets a lone producer's rally point), and a click completes Move, Gather, Attack-move and rally targeting. Placement ignores it.
- Friendly damage raises a throttled attack alert (a notice and a pulsing minimap ring); Space jumps the camera to the latest one.
- I, or the Idle workers button, selects the next worker with nothing to mine, carry or walk to, and looks at it.
- F1-F4, or the power bar, aim a commander power; a click on the ground or the minimap calls it in. Escape, right-click or the same key again cancels at no cost.
- Paused and finished matches reject world orders.

## Commander powers
Four powers are called onto a point rather than issued to a unit, so they need no selection. Each is bought with minerals and then recharges; the price is the point, because every power competes with the next turret or relay in the same finite mineral budget.

| Power | Key | Minerals | Recharge | Lands after | Area | Effect |
| --- | --- | ---: | ---: | ---: | --- | --- |
| Airstrike | F1 | 125 | 60s | 1.5-1.9s | 12-unit line | Five bombs along the jet's path, 70 damage each within 3 units |
| Artillery barrage | F2 | 100 | 45s | 1-5s | radius 8 | Ten shells at random points, 45 damage each within 2.2 units |
| Cryo field | F3 | 75 | 40s | 0.5s | radius 7 | Hostiles inside move, attack and mend at 35% pace for 6s |
| Repair field | F4 | 100 | 50s | 0.5s | radius 7 | Friendly units and structures regain 20 health per second for 8s |

Strikes hurt hostiles only and ignore armor, so they are a flat answer rather than another counter row; there is no friendly fire, so calling one on top of your own defender screen is always safe. The delay is the skill: an airstrike flies in from the headquarters side and lays its stick south to north, so it rewards leading a column on an approach lane, and the barrage rewards a clump that stays put. Cryo buys time rather than damage, which is what a runner pack or a juggernaut needs. Repair field is the only heal that touches structures for free, but it costs as much as a turret. A warden, unarmed, fragile and mending the rest of its wave, is the canonical airstrike target.

The aiming preview and the landed zone share one outline: a stadium along the airstrike's flight line, a circle for the others, coral for anything that explodes, ice for cryo and green for repair. Powers pause with the mission, stop and clear when it ends, and work in practice mode. The values above are a first pass and live in DefaultStrategy's `powers` table.

## Mission report
Victory and defeat show a grade beside the mission's numbers: time, waves cleared, headquarters integrity, minerals mined and spent, units trained and lost, structures built and lost, and hostiles defeated. Only trained units count as trained, so the starting workers do not inflate the army. Defeat is always D. A victory is S when headquarters holds 90% or more and no more than a quarter of the trained army died, A at 70%, B at 40%, otherwise C. Headquarters integrity carries most of the grade because it is the mission's own objective. The thresholds are a first pass for playtesting, like the wave counts.

## Waves
Preparation is 55 seconds; cleared-wave breaks are 32 seconds. Pending spawns count as hostiles and retain their type when blocked.

Each wave is a list of (kind, count) groups, so a wave can hold any mix of the seven hostile kinds. Defaults:

| Wave | Standard | Runner | Brute | Lancer | Breaker | Warden | Juggernaut |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 6 | - | - | - | - | - | - |
| 2 | 7 | 4 | - | 2 | - | - | - |
| 3 | 8 | 6 | 2 | 3 | 2 | - | - |
| 4 | 9 | 8 | 3 | 4 | 3 | 1 | - |
| 5 | 10 | 10 | 4 | 5 | 4 | 2 | 1 |

Each wave introduces exactly one new kind, so the player meets a threat before facing it in numbers.

## The hostile roster

Hostile stats are multipliers on the standard hostile's base numbers, so retuning the base moves the whole roster together.

| Hostile | Health | Damage | Speed | Range | Armor | Hunts |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| Standard | 100% | 100% | 100% | 2 | Medium | Headquarters |
| Runner | 60% | 70% | 160% | 2 | Light | Light-armored units |
| Brute | 250% | 160% | 65% | 2 | Heavy | Structures |
| Lancer | 80% | 75% | 85% | **9** | Light | Structures |
| Breaker | 130% | 110% | 100% | 2 | Medium | **Heavy-armored units** |
| Warden | 100% | none | 90% | 8 heal | Light | Headquarters |
| Juggernaut | 800% | 220% | 55% | 2.5 | Heavy | Structures |

Hostiles engage any defender within their own aggro radius, never less than their weapon range, and beyond that pursue a priority target so defending headquarters alone is never sufficient:

- **Runners** hunt the nearest Light-armored friendly - workers, rangers, medics, engineers - falling back to structures and then headquarters. Mining lines and support units must be covered.
- **Breakers** hunt the nearest Heavy-armored friendly, falling back to structures and then headquarters. They are the answer to a player who solves every wave with defenders.
- **Brutes, lancers and juggernauts** siege the nearest turret, barracks, ranger post, support bay or supply relay, falling back to headquarters. Outlying structures need support.
- **Standard hostiles and wardens** march on headquarters.

**Lancers** are the only hostile that shoots. At range 9 they outrange a defender screen and hit structures without walking into it, but a turret reaches 12 and still wins the duel, so turrets answer them. **Wardens** carry no weapon and mend wounded hostiles within 8 units, so a wave stops dying until the warden does; they are Light and unarmed, so focusing them is always possible. A warden with nobody to mend keeps marching on headquarters and stops at its mending range; earlier builds left it standing at its spawn point, which both kept it out of the fight and held its wave open forever. The **juggernaut** anchors the final wave: eight times a standard hostile's health, a larger footprint, and slow. It has no special ability - defenders and turrets, at 165% and 160% against Heavy, remain the answer.

Headquarters holds 1200 health. Destroy all five waves to win; losing HQ immediately ends the mission. The mission is tuned so that an undefended economy loses.

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

The presentation uses refined geometric models and industrial details designed to remain readable from the tactical camera. Dark navy metal supports cyan allied soldiers and structures, amber workers, teal mineral deposits, and coral hostiles. Silhouettes distinguish cargo-carrying workers, armored rifle soldiers, low-profile runners, heavy brutes, the command center, hangar, turret, the squat relay pylon with its paired storage drums, the ranger post's watch platform, and the support bay's lit aid cross. Rangers carry a long barrel and optic, defenders a tower shield and bulwark plating, medics a glowing aid cross, engineers a welder and part rack. Color reinforces these shapes rather than providing the only distinction.

Warm directional light and cool ambient light reveal form; luminous accents remain restrained. Ground plates, approach markers, and perimeter machinery establish an operational outpost. Scenery has no collision and does not alter navigation, gathering access, or construction footprints.

The interface uses consistent navy panels, cyan rules, clear action states, separate minerals/supply/HQ/wave status, and training/research progress bars. The supply readout turns amber at the cap. The menu presents a cosmetic outpost diorama. Short tracers, impacts, expanding command rings, construction pulses, and destruction bursts communicate actions without obscuring targets. Sound cues distinguish orders, construction, research completion, and incoming waves; the sound toggle mutes them.

This polish milestone preserves the existing map, balance, five waves, research, tutorial, and controls. Effects follow the match clock, freeze on pause, and are cleared when a match ends or restarts. No camera shake, asset packs, or new dependencies are introduced.

## Camera and live inspection

Left-click selects and inspects an object in a contextual popup without pausing or moving the tactical camera. HQ offers worker training, instant construction (barracks, ranger post, support bay, turret, supply relay), and research; workers offer Move/Gather; troops offer Move/Attack-move; each production building offers its own roster and rally targeting. Turrets, enemies, relays, and deposits show live information only. Every popup names the subject's armor class; soldiers and turrets list their effectiveness against each class, and a hostile lists how well each defence answers it. Groups show commands applicable to their members.

The popup tracks its target, clamps within the screen, and remains stationary under the pointer. Scroll the HQ action list to reach research. Relevant unavailable actions explain their requirements. Empty-ground clicks clear selection; X dismisses the popup while retaining friendly selection. Destroyed targets close their popup (or fall back to surviving group members). Targeted actions hide the popup until completion/cancellation. UI clicks never issue world orders, with one deliberate exception: the minimap, which is the world drawn small and passes the same pause and mission-end gates. The top status strip and tutorial remain visible.

WASD/arrows pan; the wheel zooms outside UI. C frames friendly selection; Home restores the HQ overview. Escape/right-click cancels targeting; Escape otherwise pauses. Manual pause and mission completion still gate commands. There is no separate close-up inspection mode; I now selects idle workers.
