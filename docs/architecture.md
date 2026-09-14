# Architecture

Gameplay lives under Assets/StrategyGame with separate runtime, editor, Edit Mode and Play Mode assemblies.

- StrategySettings owns economy, supply, combat, enemy multipliers and wave composition tuning, plus two authored tables: UnitProfile rows for every trainable unit and ArmorProfile rows for everything that fights. Wallet, HealthModel, ProductionQueue, MineralStock, SupplyModel, UnitProfile, ArmorProfile, WaveComposition and WaveState are independent rule models.
- StrategyMatch owns match state, spawning, placement and spending. Its pending enemy queue retains enemy kinds and participates in wave-completion checks.
- StrategyEntity executes explicit movement, attack, attack-move and gather orders through NavMeshAgent. Barracks retain an optional rally position. Public orders are gated on a living entity and running match.
- StrategyCommander handles selection, control groups, targeting and contextual orders. EventSystem raycasts determine UI interception instead of fixed screen bands.
- StrategyUI builds scalable uGUI canvases; StrategyHud and StrategyMenu bind runtime buttons. Generated scene canvases preview the layout in the editor and are recreated at runtime to bind listeners. Health bars are non-interactive UI. StrategyFeedback owns selection/rally rings, flashes, short effects and cached procedural sound cues.
- StrategyProjectBuilder generates geometric prefabs, navigation and both scenes. Decorations do not contribute to the navigation bake; structures carve runtime footprints. Existing settings and materials are preserved.

## Unit roster tables

Two serialized tables replace the per-kind switch chains, each with one job and each fully populated. `UnitProfile` holds a trainable unit's stat line (producer, cost, supply, health, damage, range, speed, training time); `ArmorProfile` holds an armor class and the three outgoing counter multipliers for anything that attacks or can be attacked. `Cost`, `Health`, `Supply`, `Damage`, `Range` and `Speed` consult the profile first and fall back to the remaining building and enemy arms; `Armor` is an instance lookup defaulting to Structure; `DamageScale` collapses to the attacker's row scaled against the target's class, which covers hostiles with no extra branch.

Both tables are hand-authored YAML in DefaultStrategy, and Unity drops mistyped keys without complaint, so `EveryTrainableKindHasACompleteProfile` loads the shipped asset through AssetDatabase and asserts every expected row exists and is non-degenerate. That test failing means the asset is wrong, not the code.

Producers are discovered from the table rather than named: `IsProducer` asks whether any profile lists that kind as its producer, so training gates, rally points and the production tick all widen automatically when a roster entry is added.

## Supply and combat counters

SupplyModel is a pure model holding only arithmetic: a used count, a cap clamped to a hard limit, and a Fits test. It performs no bookkeeping of its own. StrategyMatch.RecountSupply derives both numbers from the living entity list every frame and folds each queued job's own kind into the used count, so a barracks holding a soldier and two defenders reserves what it will actually cost. Cancelled production, destroyed relays and battlefield losses correct themselves without event plumbing, and a full queue can never overshoot the cap. Train recounts before gating so repeated calls within one frame stay correct.

ProductionQueue stores (kind, seconds) pairs rather than bare durations. Before, the trained kind was re-derived from the producer's own kind in three separate places, which made more than one unit per building impossible; now the queue is the single source and exposes Next and Queued for the spawn tick, supply reservation and HUD.

Armor scales damage in both directions, structures excepted: a Defender's Heavy class is what makes it a screen rather than a large health bar. StrategyMatch keeps the single-argument CombatDamage as the research-modified base and adds a two-argument overload that applies the counter, so the two effects compose multiplicatively without either owning the other.

Medics and engineers share one UpdateSupport branch parameterised by whether they mend units or structures, so the two can never compete for the same target. HealthModel.Heal clamps to maximum and refuses to revive the dead. Engineer repair spends minerals through a fractional debt accumulator, so a partial second of repair still bills correctly and repair stops when the wallet empties. Both reuse the existing StrategyEffects.Emit pool for their beams; no new presentation plumbing was added.

StrategyMatch.PriorityTarget resolves a hostile's long-range objective by kind through NearestFriendly; runners select on Light armor rather than a hard-coded Worker check, so the roster's soft units are harassment targets automatically. StrategyEntity consults it only when it has no target, leaving the existing short-range NearestOpponent sweep authoritative for anything within reach.

New EntityKind values must be appended to the end of the enum: StrategyMatch indexes its prefab array by enum value and the scene serializes that array by index. Spawn refuses a kind the array does not cover and reports it rather than throwing, so a scene saved before a rebuild fails visibly instead of crashing.

## Generation and verification
Rebuild Prototype explicitly regenerates scenes and prefabs after offering to save editor work. Build Windows Development only builds existing scenes to Builds/Windows/OutpostStrategy.exe. Preserve all existing asset GUIDs.

Run Edit Mode and Play Mode through Unity Test Runner. CLI uses -batchmode -projectPath <project> -runTests -testPlatform EditMode -testResults <file> -logFile <file>; use PlayMode for integration tests and omit -quit for tests.

The development-only --outpost-smoke replay mines, constructs defenses and supply relays, trains and rallies soldiers through five waves. It builds a relay before further turrets whenever free supply drops below 4, stands up the ranger post and support bay before stacking turrets, and trains a mix from every producer, so the replay exercises the whole roster instead of stalling on minerals it cannot convert into an army. It writes rendered screenshots and results to Builds/Windows/Smoke. Add --outpost-normal-speed for a real-time replay writing to SmokeNormal. Captures cover menu, pause and HUD at 1280x720, 1920x1080 and 2560x1080. It never runs without the flag and is excluded from release builds. Upgrade validation reports live under ignored Builds/UpgradeValidation.

## Research and onboarding architecture
ResearchState is a pure C# model with atomic start/spending, progress and a completed-upgrade set. StrategyMatch validates availability and supplies effective carrying capacity and damage; base settings are never mutated by research. StrategySession carries a one-use practice request across scene loading. StrategyMatch tracks tutorial actions and isolates practice from wave ticking. The local Outpost.TutorialComplete preference controls first-time onboarding only.

The HUD presents object-specific actions, shows research status, and highlights tutorial controls/targets. StrategyCommander supports Move/Gather targeting with the same UI interception and cancellation rules as attack-move.

Development smoke additionally captures tutorial steps, HQ actions/placement and research at three resolutions and records text-overflow warnings. Add --outpost-research to exercise research purchases (SmokeResearch); combine with --outpost-normal-speed for real-time validation.

## Presentation architecture

StrategyEffects is a scene-owned pool on StrategyMatch, limited to 128 reusable line effects. It renders tracers, impacts, expanding ground markers, construction pulses, and destruction bursts without transient collider creation. Effects reject requests while the match is paused/finished, advance on scaled time only while running, clear on match end, and unload with the scene. StrategyFeedback binds hit flashes and procedural audio cues to existing gameplay events; clip caching includes frequency and duration.

The generator adds only cosmetic child meshes to entity prefabs and collider-free ground/perimeter decorations outside the navigation hierarchy. The menu diorama copies render children only, with no entities or navigation agents. Shared uGUI helpers provide decorative rules and non-interactive progress bars; editor previews and runtime HUD use the same construction code. Existing material assets and gameplay tuning remain preserved on regeneration.

## Camera and contextual popup architecture

StrategyCameraController owns tactical smoothing, pan/zoom, selection focus, and HQ reset. Inspection does not own a camera state or pause flag. StrategyMatch.Running depends on manual pause and match result.

StrategyCommander keeps InspectedObject and PopupOpen separate from its friendly Selection list. InspectObject validates entity/deposit targets, clears prior interactions, and never adds enemies or deposits to command selection. Selection APIs update popup context, including groups. BeginRallyPoint uses an explicit producer while choosing reachable terrain and calls the existing SetRallyPoint validation.

StrategyHud builds a reusable object popup with live stats, relevant action buttons, and a scrollable HQ list. Targeting hides it; manual pause/results hide it behind the existing mission overlay. The popup follows the object/group, clamps to usable canvas bounds, avoids tutorial space, and holds position during pointer interaction. EventSystem raycasts intercept popup input. Existing economy, production, research, and world-command validation remain authoritative.

The development-only --outpost-smoke --outpost-camera replay now captures tactical object popups at three resolutions under Builds/Windows/SmokeCamera. CameraTests covers continuing simulation, action availability, read-only objects, groups, target loss, popup click isolation, rally targeting, scrolling, pause/restart, and tactical focus.
