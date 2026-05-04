# TacticalSquadRPG — Project Overview

A 3D terrain auto-battle tactical RPG built in Unity 6 (6000.5).  
Players configure a squad of heroes before each battle, then watch them fight autonomously on a 3D terrain map using a combo-driven skill system.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity 6 (6000.5), 3D Core, URP |
| Input | New Unity Input System |
| Character movement | CharacterController (code-driven, no root motion) |
| Animations | Mixamo humanoid FBX clips, Animator Controller, Animation Events |
| Data | ScriptableObjects — `ActionDefinition`, `ComboRecipeDefinition`, `ComboLibraryAsset`, `UnitDefinition` |
| Architecture | Namespaces: `TacticalRPG.DataModels`, `TacticalRPG.Systems`, `TacticalRPG.ThirdPerson`, `TacticalRPG.Editor` |
| Tests | Custom in-engine test runner (~60 tests across 7 test files) |

---

## Scenes

| Scene | Purpose | Status |
|---|---|---|
| `TerrainBattleScene` | Primary 3D auto-battle gameplay | **Active** |
| `HeroConfigScene` | Pre-battle hero & skill loadout configuration | **Active** |
| `HexBattleScene` | Abstract hex-grid battle prototype | Legacy / not in active development |

---

## Gameplay Loop

1. **Hero Config** — Player opens `HeroConfigScene`, selects heroes (Kai, Mira), drags `ActionDefinition` assets into skill slots. Data is passed to the battle scene via the static `HeroLoadoutData` bridge.
2. **Battle Start** — `TerrainBattleManager` spawns both teams. A 3-second countdown runs before units engage.
3. **Auto-Battle** — Each unit runs a `UnitCombatState` state machine autonomously. Units chase, cast, melee, dodge, block, and recover without player input.
4. **Win Condition** — Battle ends when all units on one side reach the `Dead` state. A victory/defeat message is shown.

---

## Architecture — System-Data Split

Data models (`UnitRuntime`, `ActionDefinition`, `ResolvedTechnique`, etc.) are pure data with no Unity dependencies. Logic systems (`SkillSystem`, `CombatResolutionSystem`, etc.) are plain C# classes that operate on data. Unity-specific behaviour lives in `ThirdPerson/` MonoBehaviours.

---

## Core Data Models

| Class | Description |
|---|---|
| `UnitDefinition` | ScriptableObject — base stats, default behavior, visual prefab |
| `UnitRuntime` | Live mutable unit state during battle (HP, energy, buffs, skills) |
| `StatBlock` | ATK, DEF, SPD, maxHP |
| `ActionDefinition` | ScriptableObject — one action input (Punch, Hand Sign A, Orb Summon, etc.) |
| `SkillSlot` | Ordered sequence of `ActionDefinition` references |
| `ComboRecipeDefinition` | ScriptableObject — named combo: ID sequence, power multiplier, cast type |
| `ComboLibraryAsset` | ScriptableObject container holding all combo recipes |
| `ResolvedTechnique` | Result of skill resolution: name, power, element, `isCombo`, `castType` |
| `ActiveBuff` | Charge-based temporary buff (bonus damage per hit, expires at 0 charges) |
| `BehaviorLoadout` | AI archetype assigned to a unit (Aggressive, Defensive, Balanced) |
| `ProficiencySet` | Per-unit proficiency bonuses by element, action type, and technique type |

---

## Core Systems (unit-tested)

| System | Description |
|---|---|
| `SkillSystem` | Resolves a `SkillSlot` into a `ResolvedTechnique` via combo matching + proficiency |
| `CombatResolutionSystem` | Calculates final damage — stat scaling, dodge, block, buffs |
| `BehaviorSystem` | Generates `UnitIntent` from `BehaviorType` and grid state |
| `MovementSystem` | Grid-based movement and pathfinding (used in hex/grid prototype) |
| `GridSystem` | Hex-grid tile management |
| `UnitFactory` | Spawns `UnitRuntime` from `UnitDefinition` |
| `BattleManager` | Orchestrates the abstract tick-based grid battle (hex prototype) |

---

## 3D Battle Subsystems (`ThirdPerson/`)

These MonoBehaviours are added as components on the `TerrainBattleManager` GameObject at runtime.

| Class | Description |
|---|---|
| `TerrainBattleManager` | Main battle orchestrator — spawns units, wires subsystems, manages win condition |
| `TerrainBattleUnit` | Per-unit AI state machine, animation control, damage events, initiative |
| `BattleCombatResolver` | Executes resolved techniques — damage, buffs, orb spawn, summons |
| `BattleExchangeCoordinator` | Initiative-based attacker/defender role assignment between unit pairs |
| `BattleMeleeTokenSystem` | Limits frontline crowding — max 3 units pressing one target, excess units orbit |
| `BattleEngagementManager` | Manages frontline slot availability and backline promotion |
| `BattleTargetFinder` | Nearest-enemy target acquisition |
| `BattleHitStopSystem` | Short freeze frames on hit (`Light`, `Medium`, `Heavy` strengths) |
| `BattleKnockbackSystem` | Directional knockback and stagger on heavy hits |
| `BattleSummonManager` | Spawns guardian units when a Summon technique fires |
| `UnitAnimationEventRelay` | Forwards Animation Events from the Animator child to `TerrainBattleUnit` |
| `OrbBuffHandler` | Manages orbiting `OrbProjectile` instances on a unit; fires one per punch |
| `OrbProjectile` | Orbits caster until fired, then arcs to the enemy and applies damage on arrival |
| `CombatLogger` | Runtime in-memory timestamped log for combat diagnostics. Press **L** to dump to console. |
| `HealthSystem` | Tracks and syncs HP display above units |
| `ThirdPersonCamera` | Camera follow/aim system |

---

## Hero Config UI

| Class | Description |
|---|---|
| `HeroConfigManager` | Manages 4 hero slots and loadout saving |
| `ActionPoolUI` | Scrollable pool of all available `ActionDefinition` assets |
| `ActionSlotUI` / `SkillSlotUI` | Drag-and-drop skill slot assignment |
| `HeroLoadoutData` | Static bridge — passes hero configs from config scene to battle scene |

---

## Editor Tooling

All under the **TacticalRPG** top menu in Unity:

| Menu Item | Effect |
|---|---|
| Create Action Definitions | Generates missing `ActionDefinition` assets in `Assets/Data/Actions/` |
| Recreate Action Definitions (force) | Deletes and rebuilds all action assets |
| Create Combo Library | Generates all `ComboRecipeDefinition` assets + `ComboLibrary.asset` in `Assets/Data/Combos/` |

See `Docs/SKILL_CREATION_GUIDE.md` for when and how to use these.

---

## Key File Reference

| File | Role |
|---|---|
| `Scripts/ThirdPerson/TerrainBattleUnit.cs` | Unit AI state machine, animation events, combat timing |
| `Scripts/ThirdPerson/TerrainBattleManager.cs` | Battle orchestrator, unit spawning, subsystem wiring |
| `Scripts/ThirdPerson/BattleCombatResolver.cs` | Technique execution — all damage / effect paths |
| `Scripts/ThirdPerson/BattleExchangeCoordinator.cs` | Initiative and attacker/defender role locking |
| `Scripts/DataModels/ComboLibrary.cs` | Hardcoded fallback combo recipe list |
| `Scripts/DataModels/Enums.cs` | All game enums — `ActionType`, `TechniqueType`, `CastType`, `UnitCombatState`, etc. |
| `Scripts/Systems/SkillSystem.cs` | Skill-to-technique resolution pipeline |
| `Scripts/Editor/SkillDataCreator.cs` | Editor tool for generating all data assets |
| `Assets/Data/Actions/` | All `ActionDefinition` ScriptableObject assets |
| `Assets/Data/Combos/` | All `ComboRecipeDefinition` assets + `ComboLibrary.asset` |

---

## Documentation

| File | Contents |
|---|---|
| `Docs/BATTLE_DESIGN.md` | Full combat system design — state machine, energy, dodge/block, combos, subsystems |
| `Docs/SKILL_REFERENCE.md` | All actions and combo recipes with damage estimates |
| `Docs/SKILL_CREATION_GUIDE.md` | Step-by-step guide for adding new skills and new skill behaviour to the codebase |

---

## Known Legacy / Out of Scope

- `HexBattleScene`, `HexBattleManager.cs`, `HexGrid.cs`, `HexMesh.cs` — hex grid prototype, not actively developed
- `GridMap`, `GridSystem`, `MovementSystem`, `BattleManager` — abstract grid systems, retained for unit tests
- `EnemyAI.cs`, `PlayerController.cs` — older prototypes, not used in the active battle scene
- Networked multiplayer, persistent save/load, story/campaign mode — post-MVP
