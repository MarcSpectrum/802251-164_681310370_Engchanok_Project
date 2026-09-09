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
