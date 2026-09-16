# Outpost - Strategy Survival

A single-player sci-fi RTS made with Unity 6000.3.22f1 and URP. Protect headquarters and defeat five enemy waves by mining minerals, building defenses, training soldiers, and investing in research.

## Start playing
1. Open this folder in Unity Hub with Unity 6000.3.22f1, wait for compilation, and press Play from MainMenu.
2. Choose **Play**. First-time players enter guided practice with six short steps and no enemy waves. **Learn to Play** replays practice anytime.
3. Complete or skip practice to start a fresh survival mission with headquarters, three workers, and 250 minerals. Practice resources and upgrades do not carry over.
4. Select workers and use **Gather**, then click teal mineral deposits. Workers automatically deliver minerals to headquarters.
5. Click headquarters to train workers, research, and build a **barracks**, **ranger post**, **support bay**, **turret** or **supply relay**. Click a production building to train its units or set its rally point.

The Windows development build is `Builds/Windows/OutpostStrategy.exe`.

## Hard choices

Three rules make every mission a series of tradeoffs rather than a build queue.

**Supply.** Workers cost 1 supply, soldiers 2, defenders 3. Headquarters provides 10, each barracks 4, and each **supply relay** (75 minerals) another 8. Training stops at the cap, and queued jobs reserve their supply immediately. Every worker you train is a soldier you cannot have.

**Armor counters.** Armor both counters and defends, so no single unit answers a mixed wave.

| Attacker | vs Light | vs Medium | vs Heavy |
| --- | ---: | ---: | ---: |
| Soldier | 135% | 115% | 55% |
| Ranger | 175% | 100% | 40% |
| Defender | 60% | 110% | 165% |
| Turret | 50% | 100% | 160% |
| Runner | 130% | 95% | 45% |
| Standard hostile | 100% | 100% | 80% |
| Brute | 150% | 110% | 70% |
| Lancer | 100% | 115% | 90% |
| Breaker | 70% | 95% | 160% |
| Juggernaut | 120% | 120% | 85% |

Light is workers, rangers, medics, engineers, runners, lancers and wardens; Medium is soldiers, standard hostiles and breakers; Heavy is defenders, brutes and juggernauts. Rangers shred runners, but rangers are Light and runners hit Light hardest, so put defenders in front. Defenders and turrets crush brutes, so the screen holds — until a **breaker** arrives, which is built to smash exactly that screen at 160% against Heavy. Answer breakers with soldiers, not more defenders. The preparation preview names each group's armor class, so read it and build the answer before the wave arrives. Structures take unscaled damage in both directions.

**Hostiles pick their targets.** Runners hunt anything Light — workers, rangers, medics, engineers — breakers hunt your Heavy units, brutes, lancers and juggernauts siege your structures, and standard hostiles march on headquarters. Ringing the HQ with turrets no longer wins the mission; your mining line and your support units need cover too.

## What is coming for you

| Hostile | Health | Damage | Speed | Range | Armor | Threat |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| Standard | 90 | 12 | 3.5 | 2 | Medium | Marches on headquarters |
| Runner | 54 | 8 | 5.6 | 2 | Light | Hunts your workers and support |
| Brute | 225 | 19 | 2.3 | 2 | Heavy | Sieges your structures |
| Lancer | 72 | 9 | 3.0 | **9** | Light | Shoots your buildings from outside the screen |
| Breaker | 117 | 13 | 3.5 | 2 | Medium | **Smashes a defender screen** |
| Warden | 90 | none | 3.2 | 8 heal | Light | Mends the wave until you kill it |
| Juggernaut | 720 | 26 | 1.9 | 2.5 | Heavy | Final-wave siege engine |

**Lancers** are the only hostiles that shoot. Range 9 beats a defender screen, but your turret reaches 12 and wins the duel, so turrets are the answer. **Wardens** carry no weapon and heal wounded hostiles — the wave stops dying until the warden does, and they are Light and unarmed, so focus them. The **juggernaut** arrives once, in the final wave: eight times a standard hostile's health and no tricks, so defenders and turrets still answer it.

## Your army

| Unit | Trained at | Minerals | Supply | Health | Armor | Role |
| --- | --- | ---: | ---: | ---: | --- | --- |
| Worker | Headquarters | 50 | 1 | 70 | Light | Mines minerals |
| Soldier | Barracks | 75 | 2 | 120 | Medium | Generalist, range 7 |
| Defender | Barracks | 115 | 3 | 300 | Heavy | Melee screen, beats brutes |
| Ranger | Ranger post (140) | 90 | 2 | 80 | Light | Range 13, shreds runners |
| Medic | Support bay (175) | 100 | 2 | 90 | Light | Heals wounded units |
| Engineer | Support bay (175) | 85 | 2 | 90 | Light | Repairs structures for minerals |

Medics heal units, engineers repair buildings — never the other way round. Engineer repair costs minerals per point restored, so patching a turret competes with building the next one; with nothing to mend an engineer fights, badly. Both follow attack-move so they advance with the army. Any production building can queue five jobs in any mix, and each reserves its own supply when ordered.

Minerals are finite: four deposits of 1400, roughly one mission's spending. Deposits run dry.

## Controls
| Input | Action |
| --- | --- |
| Left click / drag | Inspect an object and show its actions / select a unit group |
| Shift + selection | Add to selection |
| Right click | Move, attack enemies with soldiers, or gather with workers |
| Move / Gather buttons | Choose a command, then click its world target |
| F then click / Attack-move button | Engage enemies along the selected troops' route |
| Production building + right click | Set a rally point for new units |
| Ctrl+1-9 / 1-9 | Store / recall a unit group |
| WASD / arrows | Pan camera |
| Mouse wheel | Zoom |
| C / Home | Focus selected / return to HQ overview |
| Escape / right click while targeting | Cancel command targeting or construction |
| Escape otherwise | Pause / resume |

The object popup shows only relevant actions. Labels show costs, supply and availability; training and research show remaining time. Every popup names its subject's armor class, and soldiers, turrets and hostiles list the matching damage percentages. Scroll the HQ popup to reach all research projects. The top strip shows minerals, supply, headquarters health, and wave status; supply turns amber at the cap. Preparation previews the next enemy composition with its armor classes.

Structures must fit inside the headquarters perimeter, leave mineral deposits accessible, and keep marked approach lanes clear. Production queues hold five units. Orders and progression stop while paused or after the mission ends.

## Research
| Project | Minerals | Time | Benefit |
| --- | ---: | ---: | --- |
| Improved Mining | 125 | 20s | +50% worker carrying capacity |
| Soldier Weapons | 150 | 25s | +25% soldier damage |
| Turret Weapons | 150 | 25s | +25% turret damage |

One project runs at a time, independently of training. Research costs minerals immediately and cannot be canceled. Each project is available once per mission; benefits apply to existing and future entities. Restarting clears all research. Mining benefits apply to the next extraction, preserving cargo already carried.

## Generate, test, build
- **Strategy Game > Rebuild Prototype** regenerates both scenes and prefabs. Save editor work first; existing tuning and asset GUIDs are preserved.
- **Window > General > Test Runner** runs Edit Mode rule tests and Play Mode integration tests.
- **Strategy Game > Build Windows Development** builds existing scenes without regenerating content.
- Tune gameplay and research in `Assets/StrategyGame/Data/DefaultStrategy.asset`.

Build scenes: MainMenu and Survival under `Assets/StrategyGame/Scenes`. SampleScene is unused.

See [game design](docs/game-design.md), [architecture and verification](docs/architecture.md), [milestone status](docs/tasks.md), and [project guidance](AGENTS.md).

The former shooter prototype remains in Git history. Multiplayer, additional maps, match save/load, fog of war, and custom asset packs are outside this milestone.

## Stylized sci-fi presentation

The outpost now has detailed geometric unit silhouettes, industrial structures, deck plates, perimeter lighting, and a menu diorama. The HUD separates minerals, headquarters integrity, and wave status, with training and research progress bars. Pooled tracers, impacts, construction pulses, expanding order markers, and distinct procedural cues provide action feedback. Gameplay and controls remain unchanged. See the art-direction section in the game design for the visual guidelines.

## Camera and live object inspection

Left-click a unit, building, enemy, or mineral deposit to show its information beside it. The tactical camera stays in place and the battle keeps running. The popup follows moving targets, stays within the screen, and holds still while the pointer is over it.

- HQ: train workers, build barracks/ranger post/support bay/turrets/supply relays, and research upgrades.
- Workers: Move and Gather, with cargo and current order.
- Soldiers: Move and Attack-move, with current order and damage per armor class.
- Barracks / ranger post / support bay: train their own units and Set Rally Point.
- Turrets, relays, enemies, and deposits: live information; turrets defend automatically. Hostile popups show how well each of your units answers them.
- Groups: applicable commands affect eligible units only.

Click empty ground to clear selection and the popup. Its X closes it while retaining friendly selection. Destination and construction targeting temporarily hide the popup; Escape/right-click cancels targeting. Manual pause and mission completion still stop gameplay commands.

WASD/arrows pan, the wheel zooms (or scrolls a popup under the pointer), C frames selection, and Home restores the HQ overview.
