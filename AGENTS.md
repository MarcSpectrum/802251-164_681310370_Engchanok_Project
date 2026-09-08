# Project guidance

This is a Unity 6000.3.22f1 URP sci-fi RTS survival prototype. Read [game design](docs/game-design.md), [architecture](docs/architecture.md), and [tasks](docs/tasks.md) before changing gameplay.

- Keep project gameplay under `Assets/StrategyGame`, namespace `Engchanok.StrategyGame`.
- Preserve Unity `.meta` files when moving assets. Do not edit Library, Temp, generated solution files, or package caches as source.
- Keep balance in `StrategySettings` assets. Rebuilding must not reset existing tuning.
- `Strategy Game/Rebuild Prototype` replaces generated scenes and prefabs; save editor work first. Building must not rebuild scenes implicitly.
- Keep economy and match rules testable without GameObjects. Input/UI must not issue world commands while paused or after a match ends.
- Run Edit Mode and Play Mode tests through Unity Test Runner; validate the two build scenes and a Windows development build after structural changes.
- Do not add multiplayer, purchases, or dependencies without a gameplay requirement. Keep documentation current with controls and gameplay changes.
