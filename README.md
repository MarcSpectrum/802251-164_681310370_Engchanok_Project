# Outpost — Strategy Survival

A single-player sci-fi RTS prototype built with Unity 6000.3.22f1 and URP. Mine minerals, construct barracks and turrets, command soldiers, and defend headquarters through five waves.

## Play
1. Open this folder through Unity Hub using Unity 6000.3.22f1.
2. Wait for import and compilation. Press Play to start from MainMenu.
3. Choose **Deploy to Outpost**. Select workers, then right-click the teal mineral deposits.
4. Use HUD buttons to place structures. Select headquarters to train workers, or a barracks to train soldiers.

Destroy all five enemy waves to win. Losing headquarters ends the match.

## Controls
| Input | Action |
| --- | --- |
| Left click / drag | Select an entity / multiple units |
| Shift + selection | Add to selection |
| Right click | Move, attack with soldiers, or gather with workers |
| WASD / arrows | Pan camera |
| Mouse wheel | Zoom |
| F then click | Attack-move soldiers |
| Barracks + right click | Set rally point |
| Ctrl+1?9 / 1?9 | Store / recall unit group |
| Escape | Cancel targeting/placement, otherwise pause/resume |
| Right click during placement | Cancel placement |

Build previews turn green on valid ground. Buildings must fit inside the headquarters perimeter and leave marked approach lanes clear. Production queues hold up to five units.

## Generate, test, build
- **Strategy Game → Rebuild Prototype** regenerates scenes and prefabs. Save your work first; existing tuning and materials are preserved.
- **Window → General → Test Runner** runs Edit Mode rule tests and Play Mode integration tests.
- **Strategy Game → Build Windows Development** builds the existing scenes to `Builds/Windows/OutpostStrategy.exe`. It does not regenerate content.
- Tune the game through `Assets/StrategyGame/Data/DefaultStrategy.asset`.

Build scenes: `Assets/StrategyGame/Scenes/MainMenu.unity` and `Assets/StrategyGame/Scenes/Survival.unity`. SampleScene remains unused.

## Project information
- [Agent guidance](AGENTS.md)
- [Game design and balance](docs/game-design.md)
- [Architecture and verification](docs/architecture.md)
- [Milestone status](docs/tasks.md)

The focused upgrade adds a scalable Canvas HUD, attack-move, rally points, control groups, Runner/Brute enemies, selection indicators and combat effects with procedural sound. Use the HUD controls panel for help.

The former shooter prototype is recoverable from Git history. Multiplayer, save/load, fog of war, research and custom asset packs are outside this milestone.
