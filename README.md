# Outpost - Strategy Survival

A single-player sci-fi RTS made with Unity 6000.3.22f1 and URP. Protect headquarters and defeat five enemy waves by mining minerals, building defenses, training soldiers, and investing in research.

## Start playing
1. Open this folder in Unity Hub with Unity 6000.3.22f1, wait for compilation, and press Play from MainMenu.
2. Choose **Play**. First-time players enter guided practice with six short steps and no enemy waves. **Learn to Play** replays practice anytime.
3. Complete or skip practice to start a fresh survival mission with headquarters, three workers, and 250 minerals. Practice resources and upgrades do not carry over.
4. Select workers and use **Gather**, then click teal mineral deposits. Workers automatically deliver minerals to headquarters.
5. Use **Build** for barracks and turrets; select headquarters or barracks and open **Orders** to train units. Use **Research** to improve your economy or weapons.

The Windows development build is `Builds/Windows/OutpostStrategy.exe`.

## Controls
| Input | Action |
| --- | --- |
| Left click / drag | Select a building or units |
| Shift + selection | Add to selection |
| Right click | Move, attack enemies with soldiers, or gather with workers |
| Move / Gather buttons | Choose a command, then click its world target |
| F then click / Attack-move button | Engage enemies along the selected soldiers' route |
| Barracks + right click | Set a rally point for new soldiers |
| Ctrl+1-9 / 1-9 | Store / recall a unit group |
| WASD / arrows | Pan camera |
| Mouse wheel | Zoom |
| I then click / Inspect button | Inspect an object; pauses the battle |
| Left-drag / wheel during inspection | Orbit / zoom |
| Escape / I during inspection | Return to tactical view and resume |
| C / Home | Focus selected / return to HQ overview |
| Escape / right click while targeting | Cancel command targeting or construction |
| Escape otherwise | Pause / resume |

The **Orders**, **Build**, and **Research** tabs group actions. Labels show requirements and availability; training and research show remaining time. The top strip shows minerals, headquarters health, and wave status. Preparation previews the next enemy composition.

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

## Camera and inspection

WASD/arrows smoothly pan; the wheel zooms. C frames the selected units or building; Home restores the starting HQ overview. Press I, then click any living unit, building, enemy, or mineral deposit to inspect it. The Inspect button opens a close-up of one selected friendly. Escape/right-click cancels target picking.

Inspection pauses the battle and hides gameplay information. Left-drag orbits, the wheel zooms, and Escape, I, or Exit restores the previous tactical view before resuming. Selection is preserved; pending orders and construction targeting are canceled. Inspection cannot start from the pause menu or after the mission ends.
