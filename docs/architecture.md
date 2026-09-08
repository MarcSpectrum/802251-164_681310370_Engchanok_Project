# Architecture

Project code and assets live under Assets/StrategyGame with separate runtime, editor, Edit Mode test and Play Mode test assemblies.

- Core: StrategySettings stores tuning. HealthModel, Wallet, MineralStock, ProductionQueue and WaveState hold testable rules. StrategyMatch owns entities, spending, spawning, placement, pause, results and scene flow.
- Units/economy: StrategyEntity executes movement, mining, production and combat. MineralDeposit owns finite stock. NavMeshAgent handles travel; buildings carve NavMesh obstacles. Pending wave spawns retry until space is available.
- Input: StrategyCommander polls the installed Input System, manages selection and contextual orders, camera bounds and placement preview. UI bands exclude world commands.
- UI: StrategyHud and StrategyMenu use Unity immediate-mode GUI for a dependency-free prototype HUD. Match state gates commands; time scale pauses agents, production, combat and waves.
- Editor: StrategyProjectBuilder generates prefabs, materials, static navigation data and two scenes. The static floor is baked; buildings carve runtime footprints. Lane reservations prevent players sealing enemy approaches.

## Generated content
Strategy Game > Rebuild Prototype regenerates Scenes and Prefabs, preserving existing settings and materials. Save editor changes first. Generated assets and their .meta files are tracked. Strategy Game > Build Windows Development builds existing scenes only, to ignored Builds/Windows/OutpostStrategy.exe.

The old shooter is removed from the active project and recoverable through Git history. Do not restore its startup hook or build scene entries.

## Verification
Use Unity Test Runner for Edit Mode and Play Mode suites. CLI: Unity.exe -batchmode -projectPath <project> -runTests -testPlatform EditMode -testResults <file> -logFile <file> (replace platform with PlayMode for integration tests; omit -quit for the test runner).

The development player accepts `--outpost-smoke` for an opt-in accelerated replay using default balance. It mines, builds and trains through all five waves, saves screenshots and a result under `Builds/Windows/Smoke`, then exits with code 0 on victory or 1 otherwise. This harness is excluded from non-development player builds and never runs without the flag.
