# Architecture

Gameplay lives under Assets/StrategyGame with separate runtime, editor, Edit Mode and Play Mode assemblies.

- StrategySettings owns economy, supply, combat and wave composition tuning, plus three authored tables: UnitProfile rows for every trainable unit, ArmorProfile rows for everything that fights, and HostileProfile rows for every enemy kind. Wallet, HealthModel, ProductionQueue, MineralStock, SupplyModel, UnitProfile, ArmorProfile, HostileProfile, WaveGroup, WaveComposition and WaveState are independent rule models.
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

StrategyMatch.PriorityTarget resolves a hostile's long-range objective through NearestFriendly, switching on the HostileProfile's HostilePriority rather than on the kind. Each rung falls back to the next, so no hostile is ever left without a target: SoftTargets and ArmoredTargets select on Light and Heavy armor rather than naming kinds, so the player's own roster decides who gets hunted. StrategyEntity consults it only when it has no target, leaving the short-range NearestOpponent sweep authoritative for anything within reach.

## The hostile table

HostileProfile does for enemies what UnitProfile did for the roster: it replaces the Runner/Brute ternary chains in EnemySpeed, EnemyDamage, Health and Radius with one authored row per kind. Health, damage and speed stay **multipliers on the enemy base numbers**, so retuning enemyHealth still moves the whole roster together and the legacy assertions about brutes scaling from the base keep their meaning.

The table is an override, not a replacement. Every lookup consults HostileOf first and falls through to the original Runner/Brute arms, so a settings asset authored before this table behaves exactly as it did; `EmptyHostileTableFallsBackToLegacyVariants` pins that. `EveryHostileKindHasACompleteProfile` loads the shipped asset and asserts a complete row per hostile, and that no wave names a friendly kind.

`StrategyMatch.IsHostileKind` is a static array lookup rather than an OR chain, mirroring the neighbouring Buildable list, because StrategyEntity.IsEnemy has no settings to consult and the predicate is load-bearing for IsUnitKind, supply exclusion, prefab generation and targeting. A new hostile is declared in that one array.

Ranged hostiles needed two fixes that were harmless while every enemy was melee: the aggro sweep is the profile's radius clamped to at least the weapon range, so a lancer is not blind inside its own reach, and ShowShot draws a hostile tracer when the shot actually travels more than three units, so melee hostiles look exactly as before. NearestWounded matches on same-side rather than friendly-side, which is the whole of what a hostile warden needed to mend its own wave through the existing medic branch.

WaveComposition carries a WaveGroup array of (kind, count) pairs alongside the original standard/runners/brutes fields. Groups win when authored and the three fields remain the fallback, so old settings still describe their waves and the legacy count formula is untouched. The HUD preview is built from Groups(), so a newly authored hostile appears in the next-wave line with no further edit.

New EntityKind values must be appended to the end of the enum: StrategyMatch indexes its prefab array by enum value and the scene serializes that array by index. Spawn refuses a kind the array does not cover and reports it rather than throwing, so a scene saved before a rebuild fails visibly instead of crashing. A new hostile must also be added to StrategyProjectBuilder's humanoid gate, or it silently falls to the building branch and renders as architecture.

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

## Minimap, hotkeys and mission report architecture

StrategyMinimap draws and maps coordinates; it reads no input. StrategyHud builds it before the popup, so a popup opened over the corner draws on top, and hands it to StrategyCommander. The commander routes every minimap click from its own Update: right-click cancels targeting first, then a left press completes targeting, placement ignores the map, an idle left press starts a camera drag, and a right press issues the same command a world right-click would. Keeping all of this in one Update makes the order deterministic; minimap event handlers would race the commander's targeting cancel. `TryWorldPoint` accepts a click only when the minimap is the topmost EventSystem hit, so the popup keeps its own clicks.

Two helpers were extracted from the commander's Update so that world and minimap clicks run the same validation. `CompleteTargeting` finishes rally, Move/Gather and attack-move targeting; `CommandAt` holds the right-click rules. `WorldToMap` and `MapToWorld` are static and clamped so Edit Mode can test them. Blips are pooled Images keyed by entity, cleaned up the same way as health bars. The camera frame is the four viewport corners projected onto the ground, a trapezoid for this tilted camera, clipped to the map with Sutherland-Hodgman (`ClipToMap`, at most eight sides). The first version drew the corners' bounding box, but the far corners land well beyond the map, so that box covered nearly the whole minimap and showed nothing useful.

StrategyCameraController.JumpTo moves the focus immediately, which minimap drags need, and keeps the zoom. StrategyMatch.ReportAttack is called from StrategyEntity.Damage for friendly victims. It remembers every alert site from the last `AlertCooldown` seconds and suppresses a victim within `AlertSeparation` of any of them. `AlertMinimumGap` separates any two alerts. The first version remembered only the latest site, so two distant fights alternated and alerted on nearly every hit. Space jumps to the latest alert position.

Popup hotkeys are positional. `StrategyHud.ActionKeys` maps onto the popup's visible actions in layout order, and a press invokes the button's onClick, so a hotkey is never a second code path with its own rules. Hotkeys use the actions laid out in the previous LateUpdate, which are what the player is looking at. The key hint is added to the label after the per-frame relabel, so button GameObject names, which tests address, are unchanged. StrategyUI.Button now clears the EventSystem selection after every click; otherwise the default Submit and Navigate bindings could re-press the last clicked button from Enter or WASD.

`StrategyEntity.IsIdleWorker` is a worker with no deposit, no cargo and no pending move. The commander cycles those workers in entity-list order, from the last one it picked.

MatchStats is a pure model: time, units trained and lost, structures built and lost, hostiles defeated, and a static Grade. Wallet counts Spent in TrySpend, and Refund reverses a spend rather than counting as income; Build's missing-prefab path uses it. StrategyMatch counts training where a production job completes, not in Spawn, so starting workers and scripted spawns are excluded. It counts deaths from the death branch of StrategyEntity.Damage, and advances the clock only while running outside practice. WaveState.Cleared excludes the wave that was still active when headquarters fell. The HUD shows the report only for a finished mission, and moves Restart and Main menu below it; pause keeps its original layout.

The development replay additionally captures `alert` (after a one-point scratch on a worker) and `manual` (the field manual, which was never captured before, so its overflow was never checked). Both are taken in the fresh mission loaded for the defeat capture, not before the replay loop: captures there run at normal speed and eat into the preparation phase. A first version put them there, and that shift alone turned the automated commander's reliable wins into frequent defeats. result.txt now also records the grade, the report counters and the replay's frames per second.

## Commander powers architecture

The rules and the execution are split the same way research is. `PowerProfile` rows in StrategySettings' `powers` table hold each power's price and shape, looked up by `PowerOf`; `EveryPowerHasACompleteProfile` loads the shipped asset because the table is hand-authored YAML. `PowerState` is a pure model of per-power recharge timers whose `Use` checks readiness, spends and starts the recharge as one atomic step, exactly like `ResearchState.Start`. `PowerPlan.Impacts` is a pure function from a profile and a seed to `(time, x, z)` offsets: the airstrike's stick is evenly spaced along +z with times following `PowerPlan.JetSpeed`, and the barrage draws seeded points uniformly across its disc. Keeping the shapes pure lets Edit Mode pin them without a scene.

StrategyMatch owns the rules. `CanUsePower` mirrors `CanResearch`; `UsePower` gates on it, rejects points off the battlefield before spending, then hands the paid-for power to the executor. `Powers.Tick` sits beside `Research.Tick` in Update, after the `Running` gate, so pause freezes recharge for free. `ApplyBlast` sweeps a copy of the entity list, because `Damage` removes the dead, and damages hostiles only, so there is no friendly fire and no armor scaling. Kills go through the ordinary death branch and so reach the mission report without extra plumbing.

StrategyPowers is the executor, a scene-owned component on the match object found through `StrategyPowers.For`, like StrategyEffects. It advances pending strikes and fields on scaled time only while the match is running, and clears everything, jets and bombs included, as soon as the result is decided. The jet and bombs are built at runtime from primitives with their colliders removed on the Ignore Raycast layer, so no prefab, scene or navigation change was needed and Rebuild Prototype is not required. The jet's position is derived from the strike's age, so it is over each release point exactly `FallSeconds` before that bomb lands. Explosions reuse `StrategyFeedback.Burst` and the effects pool; zone outlines are `StrategyFeedback.Ring` lines reshaped by `StrategyPowers.Shape`, which the commander's aiming preview also uses, so what the player aims is exactly what lands.

Cryo is a status on StrategyEntity rather than a field on the executor. `Chill(factor, seconds)` accepts hostiles only; the field refreshes it every frame for anything inside, so hostiles that walk in are slowed and those that walk out recover shortly after. `Pace` scales the attack cooldown and a warden's mend rate, and the agent speed is written only when the pace changes. StrategyFeedback tints a chilled body ice-blue unless a hit flash is showing.

StrategyCommander treats a power as one more targeting mode: `TargetingPower` joins `Targeting`, so Escape, right-click, popup hiding and HUD hotkey suppression all apply unchanged, and `CompleteTargeting` gained a power branch, so the minimap calls powers in through the same path as orders. F1-F4 live in `StrategyHud.PowerKeys` beside `ActionKeys`, and `ActionHotkeysAvoidReservedKeys` asserts they collide with nothing. The HUD builds the power bar before the popup, like the minimap, so a popup opened over it keeps its clicks, and gives its buttons an opaque disabled colour because, unlike popup buttons, they float over the battlefield.

The warden soft-lock is fixed in the mender branch of StrategyEntity.Update: a hostile mender whose `UpdateSupport` finds nobody to mend now calls `MarchOnObjective`, which walks to its priority target's edge and waits there without firing. Friendly medics keep their old idle behaviour.

## Forest environment authoring
`ForestEnvironment` generates deterministic cosmetic woodland meshes and dedicated Forest materials. It combines scenery by material into saved mesh assets under `Assets/StrategyGame/Environment`; all scenery uses Ignore Raycast and has no colliders. The navigation floor, navigation data, entity prefabs and settings remain unchanged. `Strategy Game/Apply Forest Environment` updates the two existing scenes without rebuilding gameplay assets and prompts to save editor work first. Rebuild Prototype also generates this environment. Existing Forest materials retain artist tuning. Lighting uses warm soft shadows, trilight ambient and distance fog without post-processing dependencies.

## Woodland UI rendering
`ForestPanel` derives from uGUI Image and draws a rounded panel plus optional dashed inset stitching as a UI mesh. It uses the standard UI material, canvas scaling, rectangular hit areas and existing RectMask2D scrolling, with no texture or font dependency. `StrategyUI` owns the cream/sage/ink palette and control styles. `Strategy Game/Refresh Forest UI` regenerates only the saved UI previews in both scenes; runtime continues to bind the same controls through BuildUI. Building never refreshes scenes implicitly. Decorative mission notes and command hints do not intercept world input. World-space power effects retain their original bright cyan independently of the paper UI accent.
