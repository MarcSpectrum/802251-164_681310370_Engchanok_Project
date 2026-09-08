# Outpost: game design

Single-player sci-fi RTS survival for keyboard and mouse. One primitive-art map, five waves, roughly 5–10 minutes.

## Loop
Start with headquarters, three workers and 250 minerals. Select workers and right-click teal deposits to mine and return loads automatically. Headquarters trains workers (50 minerals / 6 seconds). Build a barracks (150) to train soldiers (75 / 8 seconds), or turrets (100) for automatic defense.

Buildings complete instantly inside the headquarters perimeter. Placement cannot overlap entities, deposits, or the three marked approach lanes. Green previews are valid; red previews explain rejection. Failed placement never spends resources. Production queues hold five jobs and wait if their exits are occupied. Destroyed producers lose their queue without a refund.

Soldiers accept move and attack orders, and engage enemies in range when idle. Workers carry 20 minerals per trip and take 2 seconds to mine. Deposits contain 6000 minerals each. Moving a carrying worker preserves its load; it returns the load after completing the move. Enemy waves target headquarters and engage nearby defenders.

First preparation: 65 seconds. Break after each cleared wave: 40 seconds. Enemy counts: 5, 9, 13, 17, 21. Survive all five waves to win; headquarters destruction ends the match immediately.

## Controls
- Left click: select a friendly entity; drag: select units.
- Shift: add to selection.
- Right click: move, attack an enemy (soldiers), or gather (workers).
- WASD / arrows: pan; mouse wheel: zoom.
- HUD: train units, choose barracks/turret placement.
- Escape / right click: cancel placement. Escape otherwise pauses.

## Deferred
Multiplayer, save/load, fog of war, upgrades, technology trees, additional maps, custom art and audio. Balance remains editable in the default StrategySettings asset.

