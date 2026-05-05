# 02_ARCHITECTURE.md

> **Tier 2 — Stable.** Describes how the code is organized and the rules for keeping it that way. Do not modify without explicit user instruction.

---

## Architectural philosophy

This project is built around **modular subsystems with single responsibilities**, coordinated by a thin orchestrator. The goal is to keep the codebase healthy across multi-year development without the project collapsing into a god class or becoming impossible to extend.

The five non-negotiable principles:

1. **Single responsibility.** Each class owns one concern. If a class has two reasons to change, split it.
2. **Coordinator, not god.** `TerrainBattleManager` orchestrates; it does not decide.
3. **Static data is static.** ScriptableObjects are never modified at runtime.
4. **Data-driven over hardcoded.** Combos, balance, and behavior live in data assets, not in `if (heroId == "kai")` branches.
5. **Animation reflects combat state, never defines it.** Combat owns truth; animation owns presentation.

These are repeated as guardrails in `CLAUDE.md` because they're the rules most likely to be broken under pressure.

---

## High-level structure

The runtime is organized into four layers:

```
┌────────────────────────────────────────────────────────────┐
│  PRESENTATION                                              │
│  Unity-specific MonoBehaviours, animation, VFX, UI,       │
│  CharacterController-driven movement, camera               │
│  Lives in: TacticalRPG.ThirdPerson                         │
└────────────────────────────────────────────────────────────┘
                           ▲
                           │ (consumes runtime state, raises events)
                           │
┌────────────────────────────────────────────────────────────┐
│  COORDINATION                                              │
│  TerrainBattleManager — spawns units, wires subsystems,   │
│  manages battle flow and win condition                     │
│  TerrainBattleUnit — per-unit combat state machine         │
│  Lives in: TacticalRPG.ThirdPerson                         │
└────────────────────────────────────────────────────────────┘
                           ▲
                           │ (delegates decisions to)
                           │
┌────────────────────────────────────────────────────────────┐
│  SUBSYSTEMS                                                │
│  Specialized MonoBehaviours attached to TerrainBattle-     │
│  Manager: BattleCombatResolver, BattleTargetFinder,        │
│  BattleEngagementManager, BattleHitStopSystem, etc.        │
│  Plain-C# resolvers: SkillSystem, ComboLibrary             │
│  Lives in: TacticalRPG.ThirdPerson and TacticalRPG.Systems │
└────────────────────────────────────────────────────────────┘
                           ▲
                           │ (operates on)
                           │
┌────────────────────────────────────────────────────────────┐
│  DATA                                                      │
│  Static: ActionDefinition, ComboRecipeDefinition,          │
│  ComboLibraryAsset, UnitDefinition (ScriptableObjects)     │
│  Runtime: UnitRuntime, ActiveBuff, ResolvedTechnique       │
│  Lives in: TacticalRPG.DataModels                          │
└────────────────────────────────────────────────────────────┘
```

A subsystem may depend on data and on other subsystems, but it must not depend on `TerrainBattleManager` for its decisions — the manager wires references in, the subsystem makes its own calls.

---

## The subsystem pattern

This is the project's answer to "how do we add new combat behavior without growing god classes." It is **how new features should be built.**

### Anatomy of a subsystem

A subsystem is a `MonoBehaviour` that:

1. Lives as a component on the `TerrainBattleManager` GameObject (or as a child).
2. Owns exactly one combat concern (hit-stop, knockback, target acquisition, engagement slots, etc.).
3. Exposes a small public API for other subsystems and `TerrainBattleUnit` to call.
4. Is wired into `TerrainBattleManager.Start()` once at battle init and never reassigned.
5. Holds its own runtime state — it does not stuff state into `UnitRuntime` or `TerrainBattleUnit`.

### Existing subsystems

| Subsystem | Concern |
|---|---|
| `BattleCombatResolver` | Executes resolved techniques (damage, buffs, orb spawn, summons) |
| `BattleExchangeCoordinator` | Initiative-based attacker/defender role assignment between unit pairs; `IsAnimating` lock |
| `BattleMeleeTokenSystem` | Limits how many units can press one target (max 3); excess units orbit |
| `BattleEngagementManager` | Manages frontline slot availability and backline promotion |
| `BattleTargetFinder` | Nearest-enemy target acquisition |
| `BattleHitStopSystem` | Short freeze frames on hit (`Light` / `Medium` / `Heavy`) |
| `BattleKnockbackSystem` | Directional knockback and stagger; suppressed when target blocks |
| `BattleSummonManager` | Spawns and tracks guardian units summoned by combos |
| `OrbBuffHandler` (per-unit) | Tracks orbiting `OrbProjectile` instances; fires one per punch |
| `CombatLogger` | In-memory timestamped combat diagnostic log |
| `HealthSystem` | HP tracking and floating-bar sync |

These together implement the combat behavior described in `04_BATTLE_SYSTEM.md`. New combat behavior should follow this same pattern.

### When to create a new subsystem

Create one when adding a feature that:

- Has its own state lifecycle (something starts, ticks, ends)
- Could be turned off independently of other behavior
- Has logic that doesn't belong to a specific unit but to combat as a whole
- Would otherwise need to be threaded through five different existing classes

Examples that warrant a new subsystem:
- Stamina / breath / mana resources beyond the existing energy system
- Environmental effects (terrain elemental zones, hazards)
- Block-counter mechanic
- Cooldown manager beyond the existing per-skill energy gate
- Crit chance and crit modifier pipeline

Examples that do **not** warrant a new subsystem (extend existing instead):
- Adding a new combo recipe — goes into combo data
- Adding a new action input — goes into action data + the resolver branch for its `TechniqueType`
- Tweaking dodge chance formula — modify `BattleCombatResolver.ResolveBasicAttack` and document the change

### Subsystem-to-subsystem communication

Two acceptable patterns:

**Direct reference (preferred for tight coupling).** `TerrainBattleManager` wires the dependency in `Start()`:

```csharp
combatResolver.knockbackSystem = knockbackSystem;
combatResolver.hitStopSystem = hitStopSystem;
```

**Events (preferred for loose coupling and presentation hooks).** Subsystems raise events; presentation, UI, and future systems subscribe:

```csharp
public event Action<TerrainBattleUnit, int> OnDamageDealt;
```

Avoid `FindObjectOfType` and singletons for runtime data. They make tests brittle and hide dependencies.

---

## What `TerrainBattleManager` is allowed to do

`TerrainBattleManager` is a coordinator. Its allowed responsibilities are tightly bounded:

- Spawning and despawning units from `UnitDefinition` assets at battle start
- Holding references to all subsystems (assigned in Inspector or `Start()`)
- Wiring subsystem dependencies in `Start()`
- Running the battle countdown, setting `battleStarted`, ending the battle
- Win condition checks (delegating actual logic if it grows beyond a few lines)
- Owning the squad lists (player units, enemy units)

What it must **not** do:

- Damage formulas, healing formulas, dodge / block math
- Targeting decisions
- Skill resolution
- Animation control or animation event handling
- Per-unit AI decisions (those live in `TerrainBattleUnit`'s state machine)
- Any logic that takes a `TerrainBattleUnit` and decides what happens to it

If a method in `TerrainBattleManager` is doing one of the forbidden things, it belongs in a subsystem. **Move it.**

---

## What `TerrainBattleUnit` is allowed to do

`TerrainBattleUnit` is a per-unit MonoBehaviour. Its responsibilities are:

- The combat state machine (`UnitCombatState` transitions: Backline, Engage, Decide, Melee, CastMobile, CastRooted, Execute, Recover, Dodging, Dead)
- Animation triggering tied to those state transitions
- Receiving damage events and forwarding to `HealthSystem`
- Holding a reference to the unit's `UnitRuntime`
- Initiative tracking (used by `BattleExchangeCoordinator`)
- Movement via `CharacterController` during chase/cast/dodge states

What it must **not** do:

- Calculate damage (use `BattleCombatResolver`)
- Pick targets (use `BattleTargetFinder`)
- Pick skills (use `SkillSystem.PickBestSkill` / `ResolveSkill`)
- Apply knockback or hit-stop (use the corresponding subsystems)
- Manage engagement slots (use `BattleEngagementManager`)
- Modify another unit's state directly — always go through subsystems

State-machine update methods are typically the longest methods in `TerrainBattleUnit` and that's fine, but each branch should mostly **delegate** rather than compute.

---

## Static vs runtime data

This split is critical and has bitten projects badly when violated.

### Static data — `ScriptableObject` assets

Lives in `Assets/Data/`. Includes:

- `ActionDefinition` — one per action input (Punch, Kick, Hand Sign A, etc.)
- `ComboRecipeDefinition` — one per named combo
- `ComboLibraryAsset` — container holding all recipes
- `UnitDefinition` — base stats, default behavior, visual prefab

These are templates. They are loaded at battle start, read by systems, and **never written to during a battle.** Multiple unit instances can reference the same `UnitDefinition`.

### Runtime data — plain C# classes

Lives only in memory during a battle. Includes:

- `UnitRuntime` — current HP, energy, position, equipped skills, active buffs
- `ActiveBuff` — charge-based buff with bonus damage and remaining hits
- `ResolvedTechnique` — output of `SkillSystem.ResolveSkill`
- `BehaviorLoadout` — assigned AI archetype

Created at battle start, modified during battle, discarded at battle end.

### The rule

If you write to a `ScriptableObject` at runtime, every unit instance using that asset is now corrupted. **This breaks save/load, breaks restarting battles, and produces inexplicable bugs.** Don't do it.

---

## Data-driven design

New content should be addable **without code changes wherever possible.**

### What's already data-driven

- All actions are `ActionDefinition` assets in `Assets/Data/Actions/`
- All combos are `ComboRecipeDefinition` assets in `Assets/Data/Combos/`
- All hero base stats are `UnitDefinition` assets

Adding a new action or combo is a data-only change in the typical case (see `05_SKILL_SYSTEM.md`). Code changes are only required when adding genuinely new *behavior* (a new technique type that does something none of the existing types do).

### What's not yet data-driven but should be

These are areas where hardcoded values currently exist and that need to migrate to data when touched:

- Energy regen rate (currently hardcoded in `TerrainBattleUnit`)
- Dodge chance formula (currently `SPD * 5%`)
- Block chance formula (currently `DEF * 2%`)
- Frontline slot count (currently 3)
- Hit-stop duration tiers

When you change any of these, consider making them data-driven first.

### Hero-specific exceptions are a smell

If you find yourself writing:

```csharp
if (caster.definition.unitId == "hero_kai") {
    damage *= 1.5f;
}
```

Stop. The right answer is a `ProficiencySet` field, an item, a passive, or a status effect — something **data-driven** and reusable. Hero-specific switches in resolvers do not extend.

---

## Modifier pipelines (extensibility for items, passives, status effects)

Damage and healing calculations should run through a modifier pipeline so future systems can hook in without touching combat code.

### Conceptual flow

```
base value
  → additive modifiers (flat bonuses: items, buffs, status)
  → multiplicative modifiers (proficiency, crit, vulnerabilities)
  → final value
```

Modifier sources contribute to a `CombatContext` object that the resolver iterates over. Items and passives become "things that contribute modifiers when their conditions are met," not "things that the resolver special-cases."

This is **how `04_BATTLE_SYSTEM.md`'s combat resolution should evolve** as items and passives come online. The current implementation is partway there — proficiency multipliers are already applied generically, but the additive-then-multiplicative pipeline isn't fully wired. When extending damage logic, prefer extending the pipeline over adding new conditional branches.

---

## Event hooks

For systems that need to react to combat events without being coupled to combat code:

- Damage dealt / damage received
- Unit died
- Skill cast started / completed
- Combo resolved
- Buff applied / expired

These should be C# events on the relevant subsystem. Future presentation, achievements, items-with-on-hit-effects, and analytics can subscribe without modifying the subsystem.

Don't overuse events for *primary* logic flow — they make execution order hard to follow. They're for **notifications**, not for replacing direct calls within the combat path.

---

## Folder structure

```
Assets/
├── Data/
│   ├── Actions/                  # ActionDefinition assets (one per action input)
│   ├── Combos/                   # ComboRecipeDefinition + ComboLibrary.asset
│   └── Units/                    # UnitDefinition assets
│
├── Scenes/
│   ├── HeroConfigScene.unity     # Pre-battle hero configuration (active)
│   ├── TerrainBattleScene.unity  # 3D auto-battle (active)
│   └── HexBattleScene.unity      # Legacy grid prototype (do not extend)
│
└── Scripts/
    ├── DataModels/               # Pure data, NO Unity dependencies
    │   ├── ActionDefinition.cs
    │   ├── ComboRecipeDefinition.cs
    │   ├── ComboLibraryAsset.cs
    │   ├── ComboLibrary.cs       # Hardcoded fallback recipe list
    │   ├── UnitDefinition.cs
    │   ├── UnitRuntime.cs
    │   ├── ActiveBuff.cs
    │   ├── ResolvedTechnique.cs
    │   ├── BehaviorLoadout.cs
    │   ├── ProficiencySet.cs
    │   └── Enums.cs              # ALL enums live here
    │
    ├── Systems/                  # Plain-C# logic, no MonoBehaviour
    │   ├── SkillSystem.cs        # Resolves SkillSlot → ResolvedTechnique
    │   └── (other resolvers as needed)
    │
    ├── ThirdPerson/              # 3D battle MonoBehaviours
    │   ├── TerrainBattleManager.cs
    │   ├── TerrainBattleUnit.cs
    │   ├── BattleCombatResolver.cs
    │   ├── BattleExchangeCoordinator.cs
    │   ├── BattleMeleeTokenSystem.cs
    │   ├── BattleEngagementManager.cs
    │   ├── BattleTargetFinder.cs
    │   ├── BattleHitStopSystem.cs
    │   ├── BattleKnockbackSystem.cs
    │   ├── BattleSummonManager.cs
    │   ├── OrbBuffHandler.cs
    │   ├── OrbProjectile.cs
    │   ├── HealthSystem.cs
    │   ├── CombatLogger.cs
    │   ├── UnitAnimationEventRelay.cs
    │   └── ThirdPersonCamera.cs
    │
    ├── UI/                       # UI controllers
    │   ├── HeroConfigManager.cs
    │   ├── HeroLoadoutData.cs    # Static bridge: config scene → battle scene
    │   ├── ActionPoolUI.cs
    │   ├── ActionSlotUI.cs
    │   └── SkillSlotUI.cs
    │
    └── Editor/                   # Editor tools
        └── SkillDataCreator.cs
```

---

## Coding standards

### One class, one job

If a class file exceeds ~500 lines or has more than one cohesive responsibility, split it. The existing `BattleCombatResolver` is approaching this limit — when adding new technique types, consider whether the new logic should be its own subsystem.

### Composition over inheritance

`UnitRuntime` *holds* a `UnitDefinition`, *holds* a `BehaviorLoadout`, *holds* `equippedSkills`. It does not inherit from a `Unit` base class. New variants of units (summons, projectiles, hazards) get their own classes — they do not extend `TerrainBattleUnit` unless they genuinely share its full lifecycle.

### Dependency injection over service locators

Subsystems receive their dependencies via Inspector references or `Start()` wiring, not via `FindObjectOfType` or static singletons. This is what makes tests possible and prevents hidden coupling.

### Comments

Don't comment what the code says. Do comment **why**, especially:

- Why a state transition is gated on something non-obvious
- Why two subsystems are wired in a particular order
- Why a magic number was chosen (energy regen rate, slot count, attack range)

### Naming

Match the existing convention — see `CLAUDE.md` for the full list. The single most important rule: **`Battle*` for combat subsystem MonoBehaviours, `Xxx*Definition` for ScriptableObjects, `Xxx*Runtime` for live state.**

---

## Performance posture

Combat is currently low-load (squads of ~5 units per side). Don't optimize prematurely.

When optimization becomes necessary, in this order:

1. Profile first — never optimize on intuition
2. Pool objects (orbs, hit-stop coroutines, VFX)
3. Cache `GetComponent` calls in `Start`, not `Update`
4. Spatial partitioning if/when squad sizes grow significantly

---

## Testing strategy

The project has a custom in-engine test runner with ~60 tests. Existing tests cover:

- `SkillSystem.ResolveSkill` (combo matching, technique resolution)
- The legacy grid systems (retained for regression coverage even though grid is not used at runtime)
- Selected combat math

When adding new resolvers or pure-C# systems, add tests in the same pattern. Tests for MonoBehaviour subsystems are harder; favor extracting the testable logic into a plain-C# helper that can be unit-tested independently.

**Do not delete the legacy grid-system tests** without explicit user instruction. They exist intentionally.

---

## Anti-patterns to actively avoid

The patterns this project is set up to prevent:

1. **God classes.** Adding logic to `TerrainBattleManager` or `TerrainBattleUnit` because it's "easier than making a new subsystem." Wrong. Make the subsystem.
2. **Runtime state in ScriptableObjects.** `currentHP` on `UnitDefinition` is a bug waiting to happen.
3. **Hardcoded hero-specific behavior.** `if (heroId == "X")` is a smell.
4. **Animation events as combat truth.** Animation triggers presentation; code triggers gameplay. Use Animation Events to *notify* code, not to *cause* damage.
5. **Tight coupling via cross-references.** A subsystem reaching into another subsystem's private state. Subsystems talk through public APIs.
6. **Premature optimization.** Profile first.
7. **`FindObjectOfType` for runtime dependencies.** Wire dependencies explicitly.

When you find one of these in existing code, flag it. When you're tempted to introduce one, don't.

---

## Summary

The architecture is:
- **Modular** (MonoBehaviour subsystems with single responsibilities)
- **Coordinated, not centralized** (`TerrainBattleManager` wires; subsystems decide)
- **Data-driven** (ScriptableObjects for static, plain C# for runtime)
- **Extensible** (modifier pipelines, event hooks, no hero-specific switches)
- **Testable** (logic in plain-C# systems, tested independently)

These properties are what allow the project to grow over years without collapsing. They are **enforced** in `CLAUDE.md`, not just suggested here.
