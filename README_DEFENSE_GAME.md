# Defense Game Current State

## Overview
This project is currently a playable 3D defense prototype built around:
- summon
- merge
- round progression
- boss waves
- auto-combat
- runtime scene bootstrap
- custom prefab replacement
- per-unit combat tuning

The project is structured so art, UI, VFX, unit prefabs, monster prefabs, and tuning data can be replaced without rewriting the core game loop.

## Current Core Loop
1. Start a round.
2. A countdown runs before enemies spawn.
3. Monsters spawn over time and move from top to bottom.
4. Defenders placed on the bottom board auto-attack.
5. Defenders gain mana over time, when hit, and when attacking.
6. When mana is full, the unit uses a skill.
7. Three units of the same grade can be merged into the next grade.
8. Every 10 rounds, a boss monster appears.

## Included Systems

### Characters
- 30 starter character definitions
- 5 grades: `Normal`, `Rare`, `Epic`, `Legendary`, `Mythic`
- stat fields:
  - health
  - attack power
  - critical chance
  - critical damage
  - attack speed
  - mana
  - attack range
- role-based stat variation
- skill counts by grade
- merge to next grade
- drag and drop board movement
- swap positions when dropping onto another occupied slot

### Monsters
- 30 starter monster definitions
- 5 boss definitions
- role-based monster behaviors
- boss round support spawns
- boss phase change under 50% HP
- per-monster movement speed tuning
- per-monster attack range tuning
- optional splash / extra hit tuning

### Combat
- auto targeting
- projectile attacks
- splash damage on basic attacks
- additional hit / pseudo-pierce support
- floating damage text
- HP / mana world-space bars
- hit flash feedback
- monster death effect

### Animation
- support for:
  - `spawn`
  - `idle`
  - `walk`
  - `attack`
  - `skill`
  - `win`
- animation event proxy for missing event handlers
- action states intended to complete before returning to loop states

### Round / HUD / UX
- pre-round countdown
- round clear banner
- merge success popup
- basic HUD
- test buttons
- keyboard debug controls

### Presentation / Art Replacement
- replaceable:
  - character prefabs
  - monster prefabs
  - projectile prefab
  - background prefab
  - death effect prefab
  - stage decor prefabs
  - UI font and colors

## Fastest Way To Run
1. Open `Assets/Scenes/DG.unity`
2. Select or create `GameBootstrap`
3. Add `DefenseGame.RuntimeSceneBootstrap` if missing
4. Ensure `Presentation Config` is linked if you want custom art
5. Press Play

## Runtime Bootstrap Builds
- board slots
- lane markers
- spawn points
- goal point
- templates for defenders / monsters / projectiles
- camera
- light
- HUD
- buttons

## Main Data Assets
- `Assets/Data/DefenseGamePresentationConfig.asset`
  - prefab replacement
  - UI colors / font
  - stage visuals
  - monster death effect

- `Assets/Data/CharacterCombatTuningConfig.asset`
  - per-character attack range
  - splash radius
  - splash damage ratio
  - extra hit count

- `Assets/Data/MonsterCombatTuningConfig.asset`
  - per-monster attack range
  - per-monster move speed
  - splash radius
  - splash damage ratio
  - extra hit count

## Main Scripts
- `Assets/Scripts/DefenseGame/RuntimeSceneBootstrap.cs`
  - builds the playable runtime test stage
- `Assets/Scripts/DefenseGame/DefenseGameController.cs`
  - game flow, summon, merge, economy, round hooks
- `Assets/Scripts/DefenseGame/DefenseBoardManager.cs`
  - board slots, spawning, merging, dragging, swapping
- `Assets/Scripts/DefenseGame/CharacterDatabase.cs`
  - character definitions and generation
- `Assets/Scripts/DefenseGame/MonsterDatabase.cs`
  - monster definitions and generation
- `Assets/Scripts/DefenseGame/DefenderUnit.cs`
  - defender combat logic
- `Assets/Scripts/DefenseGame/MonsterUnit.cs`
  - monster combat and movement logic
- `Assets/Scripts/DefenseGame/UnitAnimationDriver.cs`
  - animation state driving
- `Assets/Scripts/DefenseGame/SimpleGameHUD.cs`
  - HUD and merge / round banners

## Input
- `Space`: start round
- `S`: summon hero
- `1`: merge Normal
- `2`: merge Rare
- `3`: merge Epic
- `4`: merge Legendary
- `C`: add 5 heroes to database
- `M`: add 3 monsters to database

## Important Notes
- Boss monsters appear every 10 rounds.
- Rare / Epic / Legendary characters use 2 skills.
- Mythic characters use 3 skills.
- Boss monsters use 2 skills.
- If a character ID has no direct prefab mapping, the presentation config can fall back to a random custom character prefab.
- Monster movement speed currently also uses a global multiplier inside `MonsterUnit.cs`.

## Recommended Editing Points

### To change character visuals
- edit `Assets/Data/DefenseGamePresentationConfig.asset`
- use `characterOverrides`

### To change monster visuals
- edit `Assets/Data/DefenseGamePresentationConfig.asset`
- use `monsterOverrides`

### To change character balance
- edit `Assets/Data/CharacterCombatTuningConfig.asset`

### To change monster balance
- edit `Assets/Data/MonsterCombatTuningConfig.asset`

### To change round pacing
- edit `Assets/Scripts/DefenseGame/RoundManager.cs`

### To change movement / combat feel
- edit `Assets/Scripts/DefenseGame/DefenderUnit.cs`
- edit `Assets/Scripts/DefenseGame/MonsterUnit.cs`
- edit `Assets/Scripts/DefenseGame/UnitAnimationDriver.cs`

## Current Rough Edges To Watch
- animation controller transition settings can still override code-driven loop intent depending on the imported controller
- some art packs may require per-prefab axis / animation event cleanup
- Unity-side playtest / compile should always be checked after big prefab or controller edits
