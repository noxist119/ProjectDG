# Defense Game Quick Start

## What is included
- 30 starter heroes with grade, role, stats, and skills
- 30 starter monsters plus 5 boss monsters
- summon, merge, round, boss-round, mana, crit, attack-speed systems
- runtime bootstrap that can build a playable test stage in an empty scene

## Fastest way to run
1. Open `Assets/Scenes/DG.unity`
2. Create an empty GameObject named `GameBootstrap`
3. Add `DefenseGame.RuntimeSceneBootstrap`
4. Press Play

The bootstrap builds:
- board slots
- spawn points
- goal point
- default defender/monster templates
- camera and light
- runtime HUD and buttons

## Test controls
- `Space`: start round
- `S`: summon hero
- `1`: merge Normal
- `2`: merge Rare
- `3`: merge Epic
- `4`: merge Legendary
- `C`: add 5 heroes to database
- `M`: add 3 monsters to database

## Main scripts
- `Assets/Scripts/DefenseGame/RuntimeSceneBootstrap.cs`
- `Assets/Scripts/DefenseGame/DefenseGameController.cs`
- `Assets/Scripts/DefenseGame/CharacterDatabase.cs`
- `Assets/Scripts/DefenseGame/MonsterDatabase.cs`
- `Assets/Scripts/DefenseGame/DefenderUnit.cs`
- `Assets/Scripts/DefenseGame/MonsterUnit.cs`

## Notes
- Boss monsters appear every 10 rounds
- Rare, Epic, Legendary units use 2 skills
- Mythic units use 3 skills
- Boss monsters use 2 skills
- If you want custom art/prefabs later, assign prefabs in character and monster definitions or replace the runtime templates
