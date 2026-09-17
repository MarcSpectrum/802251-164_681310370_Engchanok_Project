# Milestone tracking

## Focused upgrade implemented
- Scalable Canvas menu/HUD, contextual actions, controls panel, pause/results and session mute.
- Attack-move with route resumption, explicit attack target retention, barracks rally points and unit control groups.
- Runner and Brute variants with configurable multipliers and five mixed waves; blocked spawns retain their types.
- Distinct geometric silhouettes, selection/rally/destination indicators, health bars, combat flashes and procedural cues.
- Generated scenes/prefabs updated without resetting existing tuning or asset identity.
- Starting camera frames HQ and all four mineral deposits above the command panel.

## Validation
- Edit Mode: 9/9 passed, including composition and legacy settings compatibility.
- Play Mode: 9/9 passed after camera/text polish, plus the focused targeting-cancellation/HUD isolation test passed (10 scenarios total).
- Repeated generation preserved the settings JSON and GUID and produced every entity prefab.
- Windows development build succeeded. Accelerated and normal-speed standalone replays both won all five waves with default balance (45 and 75 minerals remaining respectively). The delivery build replay also reached victory; its corrected result label and teal selection ring were visually verified.
- Rendered menu, HUD and pause captures inspected at 1280x720, 1920x1080 and 2560x1080. Fixed a controls-text encoding issue and improved starting camera framing after inspection.
- Reports and logs: ignored Builds/UpgradeValidation. Replay captures: Builds/Windows/Smoke and SmokeNormal.

## Remaining evaluation
- Normal-speed validation uses an automated commander. Human playtesting with several players remains useful for difficulty and pacing feedback.
- Multiplayer, save/load and additional maps remain outside this version.

## Research and onboarding milestone
- Three configurable, timed, per-match research investments: carrying capacity, soldier damage, turret damage. Existing and future entities use effective stats; base settings are preserved.
- Orders / Build / Research tabs with visible Move/Gather targeting, cost and availability labels, research countdowns, upcoming wave composition and contextual guidance.
- First-time guided practice, replay via Learn to Play, six action-based steps, control/target highlights and a fresh mission on completion or skip.
- Generated menu and HUD previews updated; existing scenes, settings identity and tuning survived repeated generation.
- Edit Mode: 10/10 passed. Final Play Mode: 12/12 passed, including tutorial progression/reset, research pause/benefits and synthetic Move/Gather targeting/UI isolation.
- Windows development build succeeded; final visual review found no text overflow at 1280x720, 1920x1080 and 2560x1080.
- Both normal-speed standalone replays won all five waves with 1600/1600 HQ health. Baseline: 60 minerals and 31 entities. Research: all three upgrades completed, 35 minerals and 35 entities.
- Reports, logs and captures: Builds/ResearchValidation. Human first-time-player feedback remains a separate, unperformed evaluation.
- Final delivery executable replay also won all five waves with full HQ health; the corrected victory screen was visually verified.

## Stylized sci-fi polish milestone
- Recorded the stylized sci-fi art direction in game design: navy industrial surfaces, cyan allies, amber workers, teal resources, and coral enemies, with shape-based recognition and restrained lighting.
- Added armor, cargo cells, mining tools, hangar details, command-core details, and an exposed turret barrel. Added collider-free deck plates, approach edge markers, perimeter scenery, and a render-only menu diorama.
- Separated minerals, HQ integrity, and wave status. Added HQ, training, and research progress bars plus consistent action edges and button states across menu, tutorial, HUD, pause, and results.
- Added a bounded, scene-owned pool for tracers, impacts, expanding command markers, construction pulses, and destruction effects. Added construction/research cues and corrected audio clip cache keys to include duration.
- Edit Mode: 10/10 passed. Play Mode: 13/13 passed, including pooling, pause, restart cleanup, and existing HUD input isolation. The Windows development build succeeded after correcting the turret silhouette.
- Repeated generation preserved settings content and all pre-existing StrategyGame .meta files. The accelerated standalone replay won all five waves with 1600/1600 HQ health.
- Final Play Mode rerun: 13/13 passed against the regenerated scenes. Both normal-speed delivery replays exited successfully with victory and 1600/1600 HQ health. Baseline finished with 25 minerals and 34 entities; research finished with all three upgrades, 40 minerals, and 40 entities.
- Reviewed menu, HUD, tutorial, research, placement, combat, pause, victory, and defeat captures across 1280x720, 1920x1080, and 2560x1080. No layout-overflow warnings were produced; all capture dimensions matched their targets.
- Diagnostic process samples: before polish, 3.59 CPU-seconds per wall second and 648 MB working set; final concurrent replays, 4.70-4.74 CPU-seconds per wall second and 718-723 MB peak sampled working set. These indicate additional presentation cost, but differing concurrent workloads prevent a controlled FPS/regression conclusion. Human playtesting and a controlled frame-time benchmark remain useful follow-up evaluations.
- Reports, logs, before/after captures, identity checks, and performance samples are under ignored Builds/PolishValidation. Delivery executable: Builds/Windows/OutpostStrategy.exe.
## Camera and inspection milestone
- Added smooth tactical panning/zooming, C to frame selection, and Home to restore the HQ overview.
- Added I-then-click inspection for living entities and mineral deposits, plus a single-selection Inspect button. Left-drag orbits, wheel zooms, and Escape/I/Exit returns to the preserved tactical pose.
- Inspection has an independent pause flag, blocks gameplay commands, preserves selection, cancels pending interactions, and shows only camera hints and Exit. Target loss, restart, disable, and match completion release inspection safely.
- Generated Survival scene includes the dedicated camera controller and updated HUD preview; existing tuning and asset GUIDs are preserved.
- Validation reports and captures: Builds/CameraValidation and Builds/Windows/SmokeCamera.
- Final validation: Edit Mode 10/10 and Play Mode 19/19 passed (six camera scenarios). Repeat generation preserved tuning and existing metadata; the Windows development build succeeded.
- Reviewed worker/HQ/mineral close-ups and tactical/returned views across 1280x720, 1920x1080, and 2560x1080. Fixed Inspect-label clipping and inspection wave-hint visibility; the final replay had no layout warnings and restored the selection and running match.

## Live object popup milestone
- Replaced fixed Orders / Build / Research tabs with object-following popups. HQ owns construction, worker training, and research; workers/soldiers expose their commands; barracks exposes training and rally targeting. Enemies, turrets, and deposits show live information.
- Left-click combines inspection with selection, keeps the tactical camera, and leaves simulation running. Removed the separate close-up/I-key mode and inspection pause ownership.
- Added mixed-group actions, scrollable HQ research, live costs/queues/status, target-loss handling, and immediate popup hiding for destination/placement targeting. Manual pause and match-end command guards remain intact.
- Updated tutorial instructions/highlights, generated Survival HUD preview, controls documentation, and development replays.
- Edit Mode: 10/10 passed. Play Mode: 19/19 passed, including the popup coverage and existing economy/combat/input regressions. Windows development build succeeded.
- Inspected HQ, research, worker, enemy, deposit, and group captures across 1280x720, 1920x1080, and 2560x1080. Popup replay finished with simulation running and selection preserved; no text-overflow warnings were generated.
- Existing tuning and metadata hashes were preserved. Unrelated prefab/menu regeneration and test-setting changes were removed.
- Reports: Builds/PopupValidation. Popup captures: Builds/Windows/SmokeCamera.
- Final delivery replay cleared all five waves with 1600/1600 HQ health. Updated construction/research tutorial captures were reviewed at the supported resolutions, with no text-overflow warnings. Temporary editor validation code was archived under Builds/PopupValidation/EditorHarness, outside Assets.

## Hard choices milestone
- Diagnosis: the mission contained no decisions. Four deposits of 6000 held roughly seven times a full build's cost, nothing capped the army, all damage was a single number regardless of target, and every hostile walked at headquarters. The previous milestone's delivery replay cleared all five waves at 1600/1600 HQ health, taking no damage at all.
- Added supply: workers cost 1, soldiers 2; headquarters provides 10, barracks 4, and the new supply relay 8, to a hard limit of 60. SupplyModel holds the arithmetic; StrategyMatch recounts from the living entity list each frame and reserves supply for queued jobs so a full queue cannot overshoot the cap.
- Added the SupplyRelay entity kind, appended to the end of EntityKind so serialized prefab indices are unchanged. Spawn now reports a missing prefab instead of throwing when a scene predates a new kind.
- Added armor classes and a counter table. Soldiers deal 135/115/55% and turrets 50/100/160% against Light/Medium/Heavy; structures are unscaled, and hostile damage stays governed by the existing Runner and Brute multipliers. Research bonuses and armor scaling compose multiplicatively through a two-argument CombatDamage overload.
- Added hostile target priorities: runners hunt the nearest worker, brutes siege the nearest structure, standard hostiles push headquarters. The short-range NearestOpponent sweep is unchanged, so only the long march is affected.
- Retuned for a real threat: deposits 6000 to 1400, headquarters health 1600 to 1200, enemy damage 10 to 12, preparation 65s to 55s, wave breaks 40s to 32s, and wave counts raised to 6/0/0, 7/4/0, 9/6/2, 11/9/3 and 13/12/5. Wave count stays at five.
- HUD: supply readout in the top strip (amber at the cap), a Build supply relay action on headquarters, armor class and per-class damage percentages in object popups, armor classes in the wave preview, and an expanded field manual.
- The development replay now builds a supply relay before further turrets whenever free supply drops below 4, so it cannot stall holding unspendable minerals.
- Added Edit Mode coverage for supply arithmetic and queue reservation, the armor counter table, and the per-kind supply/cost/radius lookups. Added Play Mode coverage for supply-gated training with relay recovery and for hostile target priorities with armor counters. Updated the headquarters action-set assertion for the new build action.
- All four assemblies compile with no errors or warnings.

## Hard choices validation
- All four assemblies compile with no errors and no warnings.
- Rebuild Prototype completed in batch mode (STRATEGY_REBUILD_COMPLETE). It generated SupplyRelay.prefab, grew the Survival scene's prefab array to nine entries, and preserved the retuned DefaultStrategy values and existing asset GUIDs.
- Edit Mode: 13/13 passed, including the three new supply arithmetic, armor counter and per-kind lookup scenarios.
- Play Mode: 21/21 passed in 32.9s, including the two new supply-gating and hostile-priority scenarios. FiveWavesCombatVictoryAndRestart still completes inside its deadline despite the larger waves and the turret's reduced damage against Light armor, so its boost factor was left unchanged.
- Remaining: human playtesting to confirm the retune. If a competent run never drops below roughly 80% HQ health, the difficulty has not gone far enough. The automated replay is a floor, not a substitute for a player.

## Combat roster milestone
- Added four units and two production buildings: Ranger (ranger post), Defender (barracks), Medic and Engineer (support bay). Six new EntityKind values appended to the end of the enum, keeping serialized prefab indices stable.
- Replaced the per-kind stat switches with two authored tables. UnitProfile holds a trainable unit's producer, cost, supply, health, damage, range, speed and training time; ArmorProfile holds an armor class plus the three outgoing counter multipliers. Deleted the flat worker/soldier/turret-counter fields those tables absorbed.
- ProductionQueue now stores (kind, seconds) pairs. Previously the trained kind was re-derived from the producer's kind in three separate places, which made more than one unit per building impossible. The queue is now the single source for the spawn tick, supply reservation and the HUD, so a mixed queue reserves what it will actually cost.
- Armor now defends as well as counters. The previous milestone deliberately left hostile damage unscaled; with one player unit type there was nothing for defensive armor to express, but a Defender's Heavy class would otherwise be decoration. Structures remain exempt in both directions, so headquarters and turret balance are unchanged.
- Medics mend units, engineers mend structures, through one shared UpdateSupport branch so the two never compete for a target. HealthModel.Heal clamps to maximum and refuses to revive the dead. Engineer repair bills minerals through a fractional debt accumulator and stops when the wallet empties.
- Producers are discovered from the roster table rather than named, so training gates, rally points and the production tick widened automatically. Attack-move opened to every non-worker unit; explicit attack to the damage dealers. Runners now hunt the nearest Light-armored friendly instead of a hard-coded Worker check.
- HUD: one train button per roster entry shown only on its own producer, Build ranger post and Build support bay on headquarters, and the popup detail line rebuilt as a switch instead of a ternary chain.
- The development replay stands up the ranger post and support bay before stacking turrets and trains a mix from every producer.
- Tests: added EveryTrainableKindHasACompleteProfile (loads the shipped asset and validates both tables), CounterTableAnswersEachArmorClassAndCoversHostiles, HealingClampsToMaximumAndNeverResurrects, QueueKeepsEachJobsKindInOrder, MixedQueueSpawnsTheRightKindsAndReservesTheirSupply and MedicHealsWoundedAlliesAndEngineerRepairsBuildings. Migrated the tests that read the deleted flat fields onto profile lookups, and updated the popup action-name assertions for the per-unit train buttons.
- All four assemblies compile with no errors or warnings.

## Combat roster validation complete
- Rebuild Prototype was run as the prerequisite for the hostile roster milestone. It generated the six missing prefabs (Ranger, Defender, Medic, Engineer, RangerPost, SupportBay) and grew the Survival scene's prefab array from 9 to 15 entries. Until that run the Play Mode suite was not merely unrun but broken: Spawn returned null for those kinds and the tests dereferenced it.
- Baseline on the regenerated project: Edit Mode 16/16 and Play Mode 23/23 passed. The documented counts of 13 and 21 predated the roster milestone.
- Human playtesting is still needed to confirm the counter triangle reads in play, and to check that medic sustain is not strong enough to make an army unkillable. Heal rate, repair rate and the repair price are the tuning dials.

## Hostile roster milestone
- Diagnosis: the player side had grown to six units across three producers while the enemy side stayed at the three kinds it had since the hard-choices milestone. Two holes followed. Every hostile was weak against Heavy - brutes 70%, runners 45%, standard 80% - so a defender screen was the universally correct answer and no wave punished it; and every hostile was melee at range 2, so static defence was never out-ranged.
- Added four hostiles, appended to the end of EntityKind as 15-18 so serialized prefab indices are unchanged. Lancer: the first hostile that shoots, range 9, Light, sieges structures. Breaker: 160% against Heavy and 70% against Light, Medium armor, hunts the defender screen. Warden: unarmed, mends wounded hostiles within 8 units. Juggernaut: the wave-5 capstone at 800% health, Heavy, larger footprint, no special ability.
- Replaced the Runner/Brute ternary chains in EnemySpeed, EnemyDamage, Health and Radius with an authored HostileProfile table, finishing the conversion UnitProfile and ArmorProfile started. Stats stay multipliers on the enemy base, so retuning the base still moves the whole roster. Every lookup consults the table first and falls back to the original arms, so an unauthored table behaves exactly as before.
- WaveComposition gained a WaveGroup array of (kind, count) pairs alongside the three legacy int fields. Groups win when authored; the legacy fields remain the fallback, so the old count formula and the settings written against it are untouched.
- IsHostileKind became a static array lookup mirroring the neighbouring Buildable list. It is load-bearing for IsEnemy, IsUnitKind, supply exclusion, prefab generation and targeting, and cannot read settings because StrategyEntity has none, so one array is now the single place a hostile is declared.
- PriorityTarget switches on the profile's HostilePriority instead of naming kinds, with SoftTargets and ArmoredTargets selecting on Light and Heavy armor so the player's own roster decides who gets hunted. Each rung falls back to the next.
- Two fixes that were harmless while every hostile was melee: the aggro sweep is now the profile's radius clamped to at least the weapon range, and ShowShot draws a hostile tracer when a shot travels more than three units. Melee hostiles look exactly as before.
- NearestWounded matches same-side rather than friendly-side. That single predicate change is the whole of what a hostile warden needed to mend its own wave through the existing medic branch, with no new support code and no mineral billing.
- Waves rebuilt as groups, each introducing one new kind: 6 / 13 / 21 / 28 / 36 hostiles against the previous 6 / 11 / 17 / 23 / 30. Standard and runner counts came down to make room rather than simply stacking bodies.
- HUD: the next-wave preview is built from the wave's own groups, so a newly authored hostile appears with no further edit, and the hostile popup line is generated from the profile's priority. The objective label was deepened for the seven-kind final-wave preview to wrap.
- Tests: added EveryHostileKindHasACompleteProfile (loads the shipped asset, and rejects a wave naming a friendly kind), EmptyHostileTableFallsBackToLegacyVariants, WaveGroupsSupersedeLegacyFieldsAndDropEmptyCounts, BreakerPunishesTheDefenderScreenAndLancerOutrangesIt, NewHostilesHuntTheirOwnObjectives, LancerStrikesFromRangeWithoutClosingToMelee, WardenMendsHostilesOnlyAndNeverSpendsMinerals and WardenHealingCannotOutpaceADefendedOutpost. Extended the Combatants array and the popup coverage loop.
- All four assemblies compile with no errors and no warnings.

## Hostile roster validation
- Rebuild Prototype completed (STRATEGY_REBUILD_COMPLETE). It generated Lancer, Breaker, Warden and Juggernaut prefabs, grew the Survival scene's prefab array to 19, and preserved the retuned DefaultStrategy values and existing asset GUIDs.
- All four new prefabs carry a NavMeshAgent and their signature detail meshes, confirming none fell through StrategyProjectBuilder's humanoid gate into the building branch - the one failure mode in this change that produces no error.
- Edit Mode: 20/20 passed, including the four new hostile-table and counter scenarios.
- Play Mode: 28/28 passed, including the five new hostile and layout scenarios. One intermediate failure was the new clearability test's own setup: it built its turret at (-7,0,-12), which placement rejects for deposit clearance. Spawning it there, as FiveWavesCombatVictoryAndRestart already does, was the fix; no gameplay code was involved.
- FiveWavesCombatVictoryAndRestart still completes inside its deadline. The new hostiles are multipliers on enemyHealth, which that test lowers to 10, so the whole roster scales down with its existing boost.
- Windows development build succeeded. Both standalone replays cleared all five waves: accelerated finished with 60 minerals and 23 entities, normal-speed with 65 minerals and 25 entities. The live wave-2 capture shows 13 hostiles, matching the authored 7 standard / 4 runners / 2 lancers, so group compositions drive the real match and not just the model.
- Popup captures for all four new hostiles were reviewed at 1280x720, 1920x1080 and 2560x1080. The juggernaut popup reads "JUGGERNAUT / 720 HP / HEAVY" with the profile-driven role line. No text-overflow warnings were recorded in any replay, and no runtime exceptions.
- Fixed a fault in the development capture loop, not in the game: it spawned all four hostiles up front, and the turret killed the breaker while the lancer's three resolution passes were still running, so InspectObject dereferenced a destroyed entity and the remaining captures never happened. Each hostile is now spawned immediately before its own capture and removed after. DevelopmentSmoke is excluded from release builds, so no shipped code was affected.
- Added FinalWavePreviewFitsItsLabel. The next-wave preview grew from three fixed groups to as many as seven, and no capture lands in the wave-4 break, so the replay's own text-overflow check would never have seen the worst-case string; the test applies that same preferredHeight check directly. The objective label was deepened from 0.065 to 0.135 of canvas height to hold it.
- Remaining: human playtesting. Both replays won with headquarters untouched at 1200/1200, which is the same signal that prompted the hard-choices retune. The automated commander is a floor rather than a player, and its defender line was deepened as part of this change, so this is weak evidence - but the wave counts are a starting point for tuning, not a balance claim. The specific questions are whether the breaker actually stops players defaulting to a defender wall, and whether warden sustain reads as a target-priority decision rather than a damage sponge.

## RTS polish milestone
- Diagnosis: the game had deep rules but lacked the standard RTS controls for seeing and running them. Hostiles hunt workers and outlying structures, but nothing showed where a fight was. Every popup action needed the mouse. Workers whose deposit ran dry stood idle with no indication. The end screen said only "OUTPOST SECURED/LOST", with no measure of how well the mission went.
- Minimap (StrategyMinimap). It shows units, structures, workers, hostiles, deposits (grey when empty), approach lanes and spawn points, a camera frame, and white for selected units. Left-click or drag moves the camera; right-click orders the selection or sets a lone producer's rally point. A click completes Move, Gather, Attack-move and rally targeting; placement ignores it. It accepts a click only when it is the topmost UI, so a popup over it keeps its own clicks. It draws only; StrategyCommander routes all of its input so ordering stays deterministic.
- Extracted `CompleteTargeting` and `CommandAt` from the commander's Update, so world clicks and minimap clicks go through the same validation.
- Attack alerts: friendly damage raises a notice and a pulsing coral minimap ring. Each site alerts at most once per 8 s, a fight more than 12 units from every recent site gets its own alert, and any two alerts are at least 2 s apart. The first version remembered only the latest site, which let two distant fights alert on alternate hits. Space jumps the camera to the latest alert via the new `StrategyCameraController.JumpTo`, which also drives minimap jumps.
- Positional popup hotkeys: Q E R T G Z X V B map to the visible actions from top to bottom, and each button shows its key. A hotkey invokes the button's click, so cost, supply, queue and research checks are unchanged. Hotkeys do nothing while paused, targeting, placing, or with the popup closed. Button GameObject names are unchanged. StrategyUI.Button now clears the EventSystem selection after a click, so Enter and WASD cannot re-press it.
- Idle workers: `StrategyEntity.IsIdleWorker` covers workers with no deposit, no cargo and no pending move. An amber "IDLE WORKERS n [I]" button above the minimap and the I key cycle through them and move the camera.
- Mission report. MatchStats (pure model) records time, units trained and lost, structures built and lost, hostiles defeated, and an S–D grade. `Wallet.Spent` and `Refund` supply minerals spent; `WaveState.Cleared` supplies waves cleared. Training is counted where a production job completes, so starting workers and scripted spawns are excluded. The victory and defeat overlays show the grade and eight counters, with Restart and Main menu moved below them; the pause layout keeps Resume in its slot. No tuning changed.
- Field manual rewritten into the same eight control lines to cover the new keys. The development replay now captures `alert` and `manual` in the fresh defeat mission (the field manual was never captured before), and writes the grade, the counters and the replay frame rate to result.txt.
- Tests. Edit Mode: ClearedWavesExcludeTheWaveThatBrokeHeadquarters, WalletTracksSpendingAndRefunds, MissionGradeFollowsHeadquartersAndLosses, MinimapMappingRoundTripsAndClamps, ActionHotkeysAvoidReservedKeys. The Edit Mode assembly now references Unity.InputSystem for the Key type. Play Mode: MinimapShowsEntitiesAndDrivesCameraAndOrders, MinimapCompletesTargetingButIgnoresPlacement, AttackAlertsAreThrottledAndSpaceJumpsCamera, IdleWorkerFinderCyclesAndSkipsMiners, MissionReportCountsTrainingLossesAndKills, PopupHotkeysTriggerVisibleActionsOnly. FiveWavesCombatVictoryAndRestart also checks the victory report and that Restart resets the stats. Shared synthetic-input helpers live in SurvivalTests.
- Two fixes during validation. First, the camera frame drew the bounding box of the projected viewport corners, and the replay captures showed it covering almost the whole minimap, because the tilted camera's far corners land beyond the map. It now draws the actual footprint clipped to the map, covered by the Edit Mode test CameraFootprintIsClippedToTheMap. Second, the hotkey hint was first added in LateUpdate. A coroutine resuming after `yield return null` runs between Update and LateUpdate, and so do the development captures, so both saw labels without hints. The hint is now added at the end of Update, after the per-frame relabel.

## RTS polish validation
- All four assemblies compile with no errors and no warnings.
- Edit Mode: 26/26 passed (six new). Play Mode: 34/34 passed in 55.7s (six new, plus the extended five-wave victory test).
- Rebuild Prototype ran twice through ValidateUpgrade (STRATEGY_REPEAT_GENERATION_PASSED). DefaultStrategy and navigation data are unchanged, and the Survival scene's prefab array is identical. The regenerated Survival scene carries the minimap in its HUD preview. Regeneration also renumbered internal object IDs in every prefab and in MainMenu without changing their content; those files were restored, since the scene references only the prefab roots and those IDs are stable.
- Windows development build succeeded (STRATEGY_WINDOWS_BUILD_COMPLETE).
- Replay A/B, accelerated. An unmodified HEAD build won 3/3 with 1200/1200 HQ. The first harness, with the new captures before the loop, won 2/6 with three defeats. After moving those captures to the fresh mission, the final build went 2 wins (grades S and A, 1200/1200 HQ) and 1 stall, running at 322-446 fps, so frame rate is not a factor. Gameplay code only gained counters, a notice and a sound, so the extra preparation time the captures used is the explanation the data supports. The samples are small.
- The stall is a pre-existing warden fault, not part of this change. A mending unit returns from StrategyEntity.Update after UpdateSupport, and UpdateSupport only moves toward a wounded ally. A warden whose patients are all dead therefore stands still wherever it is, instead of marching on headquarters as documented. The replay only attack-moves around headquarters, so it never reaches the warden, and the wave never ends (103/104 hostiles defeated after 1000 game seconds, HQ untouched). A player can now find the warden on the minimap. The fix belongs in the warden's idle branch.
- Normal-speed replay on the final build: victory, 1200/1200 HQ, grade A, 8:03 mission time, 104 hostiles defeated, 372 fps.
- No layout-overflow warnings in any replay, and no runtime exceptions. Reviewed captures: alert, field manual, victory report, defeat report, HQ popup hotkey hints and the clipped camera frame, across 1280x720, 1920x1080 and 2560x1080.
- Remaining:
  - Human playtesting of hotkey comfort and minimap size.
  - Whether the grade should weigh lost structures. The replays lose 15-20 of roughly 28 structures and still grade A or S.
