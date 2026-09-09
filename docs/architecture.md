# Architecture

Gameplay lives under Assets/StrategyGame with separate runtime, editor, Edit Mode and Play Mode assemblies.

- StrategySettings owns economy, combat, enemy multipliers and wave composition tuning. Wallet, HealthModel, ProductionQueue, MineralStock, WaveComposition and WaveState are independent rule models.
- StrategyMatch owns match state, spawning, placement and spending. Its pending enemy queue retains enemy kinds and participates in wave-completion checks.
- StrategyEntity executes explicit movement, attack, attack-move and gather orders through NavMeshAgent. Barracks retain an optional rally position. Public orders are gated on a living entity and running match.
- StrategyCommander handles selection, control groups, targeting, camera and contextual orders. EventSystem raycasts determine UI interception instead of fixed screen bands.
- StrategyUI builds scalable uGUI canvases; StrategyHud and StrategyMenu bind runtime buttons. Generated scene canvases preview the layout in the editor and are recreated at runtime to bind listeners. Health bars are non-interactive UI. StrategyFeedback owns selection/rally rings, flashes, short effects and cached procedural sound cues.
- StrategyProjectBuilder generates geometric prefabs, navigation and both scenes. Decorations do not contribute to the navigation bake; structures carve runtime footprints. Existing settings and materials are preserved.

## Generation and verification
Rebuild Prototype explicitly regenerates scenes and prefabs after offering to save editor work. Build Windows Development only builds existing scenes to Builds/Windows/OutpostStrategy.exe. Preserve all existing asset GUIDs.

Run Edit Mode and Play Mode through Unity Test Runner. CLI uses -batchmode -projectPath <project> -runTests -testPlatform EditMode -testResults <file> -logFile <file>; use PlayMode for integration tests and omit -quit for tests.

The development-only --outpost-smoke replay mines, constructs defenses, trains and rallies soldiers through five waves. It writes rendered screenshots and results to Builds/Windows/Smoke. Add --outpost-normal-speed for a real-time replay writing to SmokeNormal. Captures cover menu, pause and HUD at 1280x720, 1920x1080 and 2560x1080. It never runs without the flag and is excluded from release builds. Upgrade validation reports live under ignored Builds/UpgradeValidation.
