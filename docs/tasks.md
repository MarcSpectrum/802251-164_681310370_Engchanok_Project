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
