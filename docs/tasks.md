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
- Research, multiplayer, save/load and additional maps remain outside this version.
