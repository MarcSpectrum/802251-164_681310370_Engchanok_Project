# Hero Shooter Prototype

A reusable third-person, movement-focused shooter foundation built with Unity 6 and URP. The current project is a deliberately sparse solo sandbox: a baseplate, a configurable player controller, a semi-automatic hitscan rifle, and respawning target dummies.

## Open and generate the project

1. Open this repository folder from Unity Hub with Unity `6000.3.22f1`.
2. Wait for scripts and packages to finish importing.
3. Choose **Hero Shooter → Rebuild Starter Project** if the generated scenes or prefabs need to be restored.
4. Press Play from any scene. The Editor always starts Play Mode from MainMenu.

The rebuild command is the source of truth for generated materials, data assets, prefabs, scenes, startup configuration, and Build Settings. It replaces the generated starter prefabs and scenes when explicitly run; gameplay source files are not rewritten.

## Controls

- `WASD` / arrow keys — move
- Mouse — orbit camera
- `Left Shift` — sprint
- `Space` — jump
- `Q` — directional dash
- Hold right mouse — shoulder aim
- Click left mouse — fire one shot
- `R` — reload
- `Escape` — pause or resume

The crosshair is white and open normally. It tightens, turns red, and gains a center dot when the center aim ray is over a live damageable target.

## Project structure

- `Assets/HeroShooter/Core` — scene flow and framework-independent state models
- `Assets/HeroShooter/Player` — input, movement, and shoulder camera
- `Assets/HeroShooter/Combat` — weapon configuration, hitscan firing, and damage contract
- `Assets/HeroShooter/Targets` — reusable target dummy
- `Assets/HeroShooter/UI` — crosshair and HUD presentation
- `Assets/HeroShooter/Editor` — repeatable project, prefab, and scene builder
- `Assets/HeroShooter/Tests/EditMode` — health, magazine, and cooldown tests

## Scenes and builds

- `Assets/HeroShooter/Scenes/MainMenu.unity` — build index 0
- `Assets/HeroShooter/Scenes/HeroSandbox.unity` — build index 1
- `Assets/Scenes/SampleScene.unity` — retained as an unused reference and excluded from builds

Use **Hero Shooter → Build Windows Development** to regenerate the starter content and build `Builds/Windows/HeroShooterPrototype.exe`. The build output is ignored by Git.

Run Edit Mode tests from **Window → General → Test Runner**. The gameplay tuning assets generated under `Assets/HeroShooter/Data` can be adjusted without changing code.
