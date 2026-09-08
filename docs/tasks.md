# Milestone tracking

## Implemented
- RTS selection, navigation, context commands and overhead camera.
- Mineral gathering, production queues and instant building placement.
- Soldier/turret combat, enemy waves, victory/defeat, pause and restart.
- Generated scenes, prefabs, navigation, tuning and Windows build command.
- Root agent guidance, gameplay and architecture documentation.
- Edit Mode rule tests and Play Mode integration scenarios.

## Validation
- Unity 6000.3.22f1 validation ran in an isolated project copy while the original editor stayed open.
- Edit Mode: 7/7 passed.
- Play Mode: 6/6 passed, covering mining/delivery, placement/spending, production/pause, navigation, pointer commands/UI isolation, victory/restart and defeat/menu.
- Repeated scene/prefab/navigation generation passed; the Windows development build succeeded.
- Two standalone replays with unchanged default balance reached victory on wave five after the replay was adjusted to place forward defenses and rally soldiers.
- Reports are in ignored `Builds/Validation`; the playable executable is `Builds/Windows/OutpostStrategy.exe`.
- Visual inspection remains pending: hidden-window captures failed or returned black frames. These captures are not delivered as screenshots. Human playtesting of layout and balance is still useful.

## Later
- Playtest default balance with several players.
- Improve unit animations, selection rings, sound and HUD scaling.
- Add new content only after the base survival loop is evaluated.
