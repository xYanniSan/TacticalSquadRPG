# ARCHITECTURE_OVERVIEW.md

## Purpose

This document defines the **overall code architecture** for the project. The architecture is designed to be modular, system-based, data-driven, and extensible to support 3-4 years of development without requiring catastrophic rewrites.

---

## Core Architectural Principles

### 1. Modular, System-Based Design

The project is built around **independent systems** with clear responsibilities.

**Key Ideas:**
- Each system owns a specific domain (grid, targeting, movement, combat, etc.)
- Systems communicate through **shared data models** (see DATA_MODELS.md)
- Systems do NOT directly depend on each other's internals
- Systems expose clean interfaces and accept data as input

**Anti-Pattern:**
- One giant "GameManager" or "BattleManager" that owns all logic
- Systems calling each other's private methods
- Tightly coupled code where changing one system breaks five others

---

### 2. Data-Driven Design

Gameplay content and configuration should live in **data files**, not hardcoded in logic.

**Static Data:**
- Hero stats, proficiencies, base info → ScriptableObjects
- Action definitions → ScriptableObjects
- Skill templates → ScriptableObjects
- Enemy templates → ScriptableObjects

**Runtime Data:**
- Current HP, position, active buffs → In-memory runtime objects
- Battle state → In-memory state machine
- Unit instances → Created at battle start, destroyed at battle end

**Benefits:**
- New heroes/enemies/actions can be added without code changes
- Designers can tweak values without touching code
- Easier to test and balance
- AI agents can generate content files more easily

---

### 3. Separation of Concerns

The architecture separates:

- **Static Data** (templates) vs **Runtime State** (live instances)
- **Combat Logic** (damage, targeting) vs **Presentation** (animation, VFX)
- **Behavior Decisions** (what to do) vs **Action Execution** (doing it)
- **System Logic** (rules) vs **Battle Manager** (coordination)

**Example:**
- `UnitDefinition` (static) defines base stats
- `UnitRuntime` (runtime) tracks current HP
- `BehaviorSystem` decides what to do
- `CombatResolutionSystem` executes damage
- `BattleManager` coordinates timing and phases

---

### 4. Battle Manager as Coordinator, Not Owner

The `BattleManager` should **orchestrate** systems, not contain all combat logic.

**Battle Manager Responsibilities:**
- Initialize battle state
- Run battle loop (tick or frame-based)
- Coordinate system execution order
- Check win/loss conditions
- Transition between battle phases
- Clean up after battle ends

**Battle Manager Should NOT:**
- Contain all damage formulas
- Hardcode item bonuses
- Hardcode hero-specific exceptions
- Own targeting logic
- Own movement logic
- Own skill resolution logic

**Why:**
- Prevents god class
- Easier to test individual systems
- Easier to extend with items, passives, modifiers
- Clearer code ownership

---

### 5. Independent, Replaceable Systems

Systems should be **loosely coupled** and **replaceable** where possible.

**Design Goals:**
- Changing `MovementSystem` implementation shouldn't break `TargetingSystem`
- Replacing grid with hexagonal grid should be localized to `GridSystem`
- Adding new targeting priorities shouldn't require rewriting `BehaviorSystem`

**How:**
- Systems communicate through shared data models
- Systems expose interfaces or public APIs
- Systems don't access each other's private state
- Systems operate on `BattleState` as shared truth

---

### 6. Build for Future Expansion

The architecture must allow future addition of:

- Items and equipment bonuses
- Passive abilities
- Skill trees
- Conditional modifiers
- Advanced status effects
- More complex targeting and behavior

**Without requiring:**
- Rewriting core battle flow
- Breaking existing systems
- Massive refactors

**How:**
- Use **modifier pipelines** for damage/healing calculations
- Use **extensible enums** for actions, elements, techniques
- Use **data-driven effects** instead of hardcoded bonuses
- Use **event hooks** where systems need to react to changes

---

## High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                       BATTLE MANAGER                        │
│  - Initializes BattleState                                  │
│  - Runs battle loop                                         │
│  - Coordinates system execution                             │
│  - Checks win/loss                                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ├─────────────────────────────┐
                              │                             │
                              ▼                             ▼
┌─────────────────────────────────────┐   ┌─────────────────────────────────┐
│         BATTLE STATE                │   │      SYSTEMS (Independent)      │
│  - Source of truth                  │   │  - GridSystem                   │
│  - Player/enemy units               │   │  - BehaviorSystem               │
│  - Grid reference                   │   │  - TargetingSystem              │
│  - Current phase                    │   │  - MovementSystem               │
│  - Battle time/tick                 │   │  - SkillSystem                  │
│  - Victory/defeat state             │   │  - CombatResolutionSystem       │
└─────────────────────────────────────┘   │  - StatusEffectSystem           │
                                          │  - WinConditionSystem           │
                                          └─────────────────────────────────┘
                                                        │
                                                        ▼
                                          ┌─────────────────────────────────┐
                                          │      DATA MODELS (Shared)       │
                                          │  - UnitDefinition (static)      │
                                          │  - UnitRuntime (runtime)        │
                                          │  - ActionDefinition             │
                                          │  - SkillSlot                    │
                                          │  - GridPosition                 │
                                          │  - UnitIntent                   │
                                          │  - CombatContext                │
                                          └─────────────────────────────────┘
                                                        │
                                                        ▼
                                          ┌─────────────────────────────────┐
                                          │    PRESENTATION (Non-Gameplay)  │
                                          │  - Unit visuals                 │
                                          │  - VFX                          │
                                          │  - Animation                    │
                                          │  - UI                           │
                                          └─────────────────────────────────┘
```

---

## System Breakdown

### GridSystem

**Responsibility:** Manage the battle grid.

**Owns:**
- Grid structure (width, height, tiles)
- Tile walkability
- Unit occupancy tracking
- Distance calculations
- Pathfinding (if needed)

**Exposes:**
- `GridMap GetGrid()`
- `bool IsWalkable(GridPosition pos)`
- `bool IsOccupied(GridPosition pos)`
- `UnitRuntime GetUnitAt(GridPosition pos)`
- `List<GridPosition> GetPath(GridPosition from, GridPosition to)`

**Does NOT:**
- Decide where units move (that's BehaviorSystem)
- Execute movement (that's MovementSystem)
- Handle combat logic

---

### BehaviorSystem

**Responsibility:** Generate unit intents based on behavior configuration.

**Owns:**
- Behavior logic (aggressive, defensive, balanced)
- Intent generation
- Decision-making rules

**Exposes:**
- `UnitIntent GenerateIntent(UnitRuntime unit, BattleState state)`

**Does NOT:**
- Execute movement
- Execute attacks
- Modify unit state directly
- Handle targeting details (delegates to TargetingSystem)

**How It Works:**
1. Reads unit's `BehaviorLoadout`
2. Evaluates battle situation (enemies nearby, HP, etc.)
3. Generates `UnitIntent` (Move, Attack, UseSkill, Wait)
4. Returns intent to BattleManager

---

### TargetingSystem

**Responsibility:** Select targets for units.

**Owns:**
- Target selection rules
- Target priority logic
- Range validation

**Exposes:**
- `UnitRuntime SelectTarget(UnitRuntime actor, BattleState state)`
- `bool IsValidTarget(UnitRuntime actor, UnitRuntime target)`

**Does NOT:**
- Decide when to attack (that's BehaviorSystem)
- Execute attacks (that's CombatResolutionSystem)
- Move units (that's MovementSystem)

**How It Works:**
1. Receives actor unit and current battle state
2. Filters valid targets (alive, enemy team, in range if needed)
3. Applies targeting rules (nearest, lowest HP, etc.)
4. Returns selected target

---

### MovementSystem

**Responsibility:** Execute unit movement.

**Owns:**
- Movement execution
- Path following
- Position updates

**Exposes:**
- `void MoveUnit(UnitRuntime unit, GridPosition target, float deltaTime)`
- `bool CanReachPosition(UnitRuntime unit, GridPosition target)`

**Does NOT:**
- Decide where to move (that's BehaviorSystem)
- Generate paths (delegates to GridSystem)
- Handle combat logic

**How It Works:**
1. Receives movement intent from BattleManager
2. Queries GridSystem for path
3. Moves unit along path (or directly if simple MVP)
4. Updates unit's `position` in `UnitRuntime`

---

### SkillSystem

**Responsibility:** Resolve action-chains into executable techniques.

**Owns:**
- Action sequence resolution
- Technique lookup/generation
- Proficiency bonus application

**Exposes:**
- `ResolvedTechnique ResolveSkill(UnitRuntime caster, SkillSlot skill)`
- `bool CanUseSkill(UnitRuntime caster, SkillSlot skill)`

**Does NOT:**
- Decide when to use skills (that's BehaviorSystem)
- Execute damage/healing (that's CombatResolutionSystem)
- Handle targeting (that's TargetingSystem)

**How It Works:**
1. Receives action-chain from `SkillSlot`
2. Analyzes sequence of actions
3. Matches sequence to known technique patterns
4. Applies caster's proficiencies to technique power
5. Returns `ResolvedTechnique`

**Example:**
- Input: `[Punch, Punch, Kick, Focus, Meditate]`
- Analysis: Matches "Triple Strike" pattern (3 physical actions)
- Proficiency: Caster has 1.2x physical proficiency
- Output: `ResolvedTechnique("Triple Strike", power: 60 * 1.2 = 72)`

---

### CombatResolutionSystem

**Responsibility:** Resolve attacks, techniques, damage, and healing.

**Owns:**
- Damage calculation
- Healing calculation
- Death handling
- Modifier application (items, passives, buffs)

**Exposes:**
- `void ResolveBasicAttack(UnitRuntime attacker, UnitRuntime defender)`
- `void ResolveTechnique(UnitRuntime caster, ResolvedTechnique technique, UnitRuntime target)`
- `int CalculateDamage(CombatContext context)`

**Does NOT:**
- Decide who to attack (that's BehaviorSystem + TargetingSystem)
- Resolve action-chains (that's SkillSystem)
- Handle movement

**How It Works:**
1. Creates `CombatContext` for attack/technique
2. Calculates base damage (Attack - Defense, or technique power)
3. Applies modifiers from buffs, items, passives
4. Deals final damage to target
5. Checks for death, triggers death handling if needed

**Modifier Pipeline (Future):**
```
Base Damage → Additive Modifiers → Multiplicative Modifiers → Final Damage
```

This allows items, passives, and buffs to hook into damage without hardcoding.

---

### StatusEffectSystem

**Responsibility:** Manage buffs, debuffs, and temporary modifiers.

**Owns:**
- Status effect application
- Duration tracking
- Effect ticking
- Effect removal

**Exposes:**
- `void ApplyEffect(UnitRuntime target, StatusEffectDefinition effect)`
- `void TickEffects(UnitRuntime unit)`
- `void RemoveExpiredEffects(UnitRuntime unit)`

**Does NOT:**
- Decide when to apply effects (that's CombatResolutionSystem or SkillSystem)
- Handle damage directly (delegates to CombatResolutionSystem)

**How It Works:**
1. Receives effect application request
2. Creates `StatusEffect` instance on target
3. Each tick, calls `OnTick()` on active effects
4. Decrements duration, removes expired effects
5. Applies stat modifiers to `UnitRuntime.currentStats`

---

### WinConditionSystem

**Responsibility:** Check if battle is over.

**Owns:**
- Win condition checks
- Loss condition checks
- Battle outcome determination

**Exposes:**
- `BattleOutcome CheckOutcome(BattleState state)`

**Does NOT:**
- Control battle flow (that's BattleManager)
- Modify unit state

**How It Works:**
1. Checks if all player units are dead → Defeat
2. Checks if all enemy units are dead → Victory
3. (Future) Checks mission-specific objectives
4. Returns `BattleOutcome` (Victory, Defeat, or None)

---

## Battle Flow

### Initialization Phase

1. `BattleManager` creates `BattleState`
2. `BattleManager` creates `GridMap` via `GridSystem`
3. `BattleManager` creates `UnitRuntime` instances from player/enemy definitions
4. `BattleManager` places units on grid
5. `BattleState.currentPhase` = `BattlePhase.Combat`

### Combat Loop (Each Tick/Frame)

```
1. BattleManager checks WinConditionSystem
   ├─ If Victory/Defeat → End battle
   └─ Else → Continue

2. For each alive unit:
   ├─ BehaviorSystem generates UnitIntent
   ├─ If intent is Move:
   │  └─ MovementSystem executes movement
   ├─ If intent is BasicAttack:
   │  ├─ TargetingSystem selects target
   │  └─ CombatResolutionSystem resolves attack
   └─ If intent is UseSkill:
      ├─ SkillSystem resolves action-chain to technique
      ├─ TargetingSystem selects target(s)
      └─ CombatResolutionSystem resolves technique

3. StatusEffectSystem ticks all active effects

4. Increment BattleState.currentTick or battleTime
```

### End Phase

1. `BattleManager` sets `BattleState.isBattleOver = true`
2. `BattleManager` sets `BattleState.outcome`
3. `BattleManager` transitions to Victory/Defeat phase
4. UI displays result
5. Runtime data is discarded
6. Player returns to meta/progression layer

---

## Static vs Runtime Data Flow

### Pre-Battle (Static Data)

```
Player selects UnitDefinition (Kai)
   ↓
Player configures SkillSlot with ActionDefinitions
   ↓
Player assigns BehaviorType
   ↓
Data is ready for battle
```

### Battle Start (Create Runtime Data)

```
BattleManager reads static UnitDefinition
   ↓
BattleManager creates UnitRuntime (copies stats, assigns loadout)
   ↓
UnitRuntime placed on GridMap
   ↓
Runtime state is live
```

### During Battle (Modify Runtime Data)

```
Unit takes damage
   ↓
UnitRuntime.currentHP is modified
   ↓
Unit dies → UnitRuntime.isDead = true
   ↓
Static UnitDefinition is never modified
```

### Battle End (Destroy Runtime Data)

```
Battle over
   ↓
UnitRuntime instances are destroyed
   ↓
BattleState is destroyed
   ↓
Static UnitDefinition remains unchanged
```

**Key Rule:**
- **Static data (ScriptableObjects) is NEVER modified at runtime**
- **Runtime state is created, modified, and destroyed per battle**

---

## System Communication Patterns

### Pattern 1: Shared BattleState

Systems read and modify `BattleState` as the source of truth.

**Example:**
- `BehaviorSystem` reads `BattleState.enemyUnits` to find targets
- `MovementSystem` updates `UnitRuntime.position` in `BattleState`
- `WinConditionSystem` checks `BattleState.playerUnits` for deaths

**Benefits:**
- Single source of truth
- No circular dependencies
- Easy to debug (inspect BattleState)

---

### Pattern 2: Data-In, Data-Out

Systems accept data as input and return results.

**Example:**
```csharp
// BehaviorSystem
public UnitIntent GenerateIntent(UnitRuntime unit, BattleState state)
{
    // Logic here
    return new UnitIntent { type = IntentType.Move, targetPosition = ... };
}
```

**Benefits:**
- Testable (pass mock data, verify output)
- No hidden state
- Clear contracts

---

### Pattern 3: Events (Optional)

For loose coupling, systems can emit events.

**Example:**
```csharp
// CombatResolutionSystem
public event Action<UnitRuntime, int> OnDamageDealt;

public void ResolveAttack(...)
{
    // Deal damage
    OnDamageDealt?.Invoke(defender, finalDamage);
}

// Elsewhere
combatSystem.OnDamageDealt += (unit, damage) => {
    // Trigger VFX, update UI, etc.
};
```

**Benefits:**
- Presentation layer can react without tight coupling
- Easy to add hooks for items/passives later

**Caution:**
- Don't overuse events for core logic (makes flow hard to follow)
- Use for notifications, not primary data flow

---

## Technology Stack

- **Engine:** Unity (2021 or later)
- **Language:** C#
- **Static Data:** ScriptableObjects
- **Runtime Data:** Plain C# classes/structs
- **Architecture Pattern:** System-based, data-driven
- **Design Pattern:** Composition over inheritance, dependency injection where useful

---

## Folder Structure

```
Assets/
├── Data/                    # ScriptableObject assets
│   ├── Units/               # UnitDefinition assets
│   ├── Actions/             # ActionDefinition assets
│   ├── StatusEffects/       # StatusEffectDefinition assets
│   └── Skills/              # Skill templates (if needed)
│
├── Scripts/
│   ├── Core/                # Core systems and managers
│   │   ├── BattleManager.cs
│   │   ├── BattleState.cs
│   │   └── GameController.cs
│   │
│   ├── DataModels/          # Shared data structures
│   │   ├── UnitDefinition.cs
│   │   ├── UnitRuntime.cs
│   │   ├── SkillSlot.cs
│   │   ├── ActionDefinition.cs
│   │   ├── GridPosition.cs
│   │   └── Enums.cs
│   │
│   ├── Systems/             # Independent systems
│   │   ├── GridSystem.cs
│   │   ├── BehaviorSystem.cs
│   │   ├── TargetingSystem.cs
│   │   ├── MovementSystem.cs
│   │   ├── SkillSystem.cs
│   │   ├── CombatResolutionSystem.cs
│   │   ├── StatusEffectSystem.cs
│   │   └── WinConditionSystem.cs
│   │
│   ├── UI/                  # UI scripts
│   │   ├── PreBattleUI.cs
│   │   ├── BattleUI.cs
│   │   └── ResultsUI.cs
│   │
│   └── Presentation/        # Non-gameplay visuals
│       ├── UnitVisuals.cs
│       ├── VFXController.cs
│       └── AnimationController.cs
│
└── Scenes/
    ├── Battle.unity
    └── MainMenu.unity
```

---

## Design Patterns

### Avoid God Classes

❌ **Bad:**
```csharp
public class BattleManager : MonoBehaviour
{
    public void Update()
    {
        // Movement logic
        // Targeting logic
        // Damage calculation
        // Skill resolution
        // Win condition checks
        // Everything in one class
    }
}
```

✅ **Good:**
```csharp
public class BattleManager : MonoBehaviour
{
    private GridSystem gridSystem;
    private BehaviorSystem behaviorSystem;
    private TargetingSystem targetingSystem;
    private MovementSystem movementSystem;
    private SkillSystem skillSystem;
    private CombatResolutionSystem combatSystem;
    private WinConditionSystem winSystem;

    public void Update()
    {
        // Coordinate systems
        foreach (var unit in battleState.playerUnits)
        {
            var intent = behaviorSystem.GenerateIntent(unit, battleState);
            if (intent.type == IntentType.Move)
                movementSystem.MoveUnit(unit, intent.targetPosition, Time.deltaTime);
            // etc.
        }
    }
}
```

---

### Use Composition Over Inheritance

❌ **Bad:**
```csharp
public class Unit : MonoBehaviour { }
public class Hero : Unit { }
public class Tank : Hero { }
public class FireTank : Tank { }
```

✅ **Good:**
```csharp
public class UnitRuntime
{
    public UnitDefinition definition;  // Composition
    public BehaviorLoadout behavior;   // Composition
    public List<SkillSlot> skills;     // Composition
}
```

---

### Dependency Injection (Where Useful)

Systems should receive dependencies, not find them with `FindObjectOfType`.

✅ **Good:**
```csharp
public class BehaviorSystem
{
    private TargetingSystem targetingSystem;

    public BehaviorSystem(TargetingSystem targeting)
    {
        this.targetingSystem = targeting;
    }
}
```

Or use Unity's built-in service locator patterns if preferred.

---

## Extensibility Examples

### Adding Items (Future)

**Without Good Architecture:**
- Hardcode item bonuses in `CombatResolutionSystem`
- Hardcode item bonuses in 10 different places
- Every new item requires code changes

**With Good Architecture:**
```csharp
// Item system hooks into CombatContext modifiers
public class ItemSystem
{
    public void ApplyItemModifiers(CombatContext context)
    {
        foreach (var item in context.attacker.equippedItems)
        {
            context.appliedModifiers.Add(item.GetModifier(context));
        }
    }
}

// CombatResolutionSystem applies all modifiers
public int CalculateDamage(CombatContext context)
{
    int damage = context.baseDamage;
    foreach (var modifier in context.appliedModifiers)
    {
        damage = modifier.Apply(damage);
    }
    return damage;
}
```

New items can be added as data, no code changes needed.

---

### Adding Passives (Future)

Same pattern as items: passives contribute modifiers to `CombatContext`.

---

### Adding New Behavior Types

New behaviors are added as enum values and logic in `BehaviorSystem`.

No changes needed to `MovementSystem`, `TargetingSystem`, etc.

---

## Testing Strategy

### Unit Testing

Each system should be testable in isolation.

**Example:**
```csharp
[Test]
public void TargetingSystem_SelectsNearestEnemy()
{
    var grid = new GridMap(10, 10);
    var actor = CreateTestUnit(new GridPosition(0, 0));
    var enemy1 = CreateTestUnit(new GridPosition(5, 5));
    var enemy2 = CreateTestUnit(new GridPosition(2, 2));

    var targetingSystem = new TargetingSystem();
    var target = targetingSystem.SelectTarget(actor, new List<UnitRuntime> { enemy1, enemy2 });

    Assert.AreEqual(enemy2, target); // Closer enemy
}
```

---

### Integration Testing

Test systems working together.

**Example:**
- Place units on grid
- Generate intent
- Execute movement
- Verify final position

---

### Playtesting

Run full battles and observe:
- Is combat readable?
- Do behaviors work as expected?
- Are techniques resolving correctly?

---

## Performance Considerations

### For MVP

- Prioritize **clarity and correctness** over performance
- Profile before optimizing
- 2v2 battles won't stress performance

### For Later

- Object pooling for units, effects, projectiles
- Spatial partitioning if battlefield gets large
- Batch processing for status effects
- LOD for visuals

---

## Common Pitfalls to Avoid

1. ❌ **Storing runtime state in ScriptableObjects**
   - ScriptableObjects are shared assets, not instance data

2. ❌ **Hardcoding hero/item-specific logic**
   - Use data-driven modifiers and proficiencies

3. ❌ **Gameplay truth in animation events**
   - Animations are presentation, not logic
   - Damage happens in code, animation is visual feedback

4. ❌ **Tight coupling between systems**
   - Systems should communicate through data, not direct calls

5. ❌ **Premature optimization**
   - Build correctly first, optimize later

6. ❌ **Over-engineering for MVP**
   - Keep it simple, expand when validated

---

## Summary

**The architecture is:**
- Modular (independent systems)
- Data-driven (ScriptableObjects for static, runtime for live)
- Extensible (modifiers, hooks, events)
- Testable (data-in, data-out)
- Maintainable (clear boundaries, single responsibilities)

**The battle manager is:**
- A coordinator, not a god class
- Responsible for flow, not all logic

**Systems are:**
- Independent, replaceable modules
- Communicate through BattleState and data models
- Focused on single responsibilities

**This architecture supports:**
- 3-4 years of development
- Future items, passives, skill trees
- Changing requirements without rewrites

---

## Version

**Version 1.0** - Initial architecture for MVP
