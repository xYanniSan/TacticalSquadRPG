# DATA_MODELS.md

## Purpose

This document defines the **core data structures and shared terminology** used across all systems. These models are the foundation for communication between systems and the source of truth for data representation.

All systems should reference these models to maintain consistency.

> **Implementation note:** Models 11–14 (`GridPosition`, `GridMap`, `GridTile`, `BattleState`, `UnitIntent`) belong to the hex/grid prototype (`HexBattleScene`). The active 3D auto-battle system uses `TerrainBattleUnit` and `TerrainBattleManager` directly. See [IMPLEMENTATION_OVERVIEW.md](IMPLEMENTATION_OVERVIEW.md) for the live system.

---

## Design Principles

### Static vs Runtime Separation

- **Static Data (Definitions)** - Immutable templates defined in ScriptableObjects or JSON
  - Examples: Hero base stats, action definitions, skill templates
  - Never modified at runtime
  - Shared across multiple instances

- **Runtime Data (State)** - Mutable state during battle
  - Examples: Current HP, position, active buffs
  - Unique per instance
  - Destroyed after battle ends

### Data-Driven Design

- Gameplay data should live in data files, not hardcoded in logic
- Systems should operate on data structures, not specific hero IDs
- New content should be addable without code changes

---

## Core Data Models

---

### 1. UnitDefinition (Static)

**Purpose:** Immutable template for a hero or enemy unit.

**Storage:** ScriptableObject

**Fields:**

```csharp
public class UnitDefinition : ScriptableObject
{
    // Identity
    public string unitId;              // Unique identifier (e.g., "hero_kai")
    public string displayName;         // Human-readable name (e.g., "Kai")
    public Sprite portrait;            // UI portrait
    public GameObject visualPrefab;    // Battle visualization prefab

    // Base Stats
    public StatBlock baseStats;        // HP, Attack, Defense, Speed

    // Proficiencies
    public ProficiencySet proficiencies; // Elemental/technique bonuses

    // Default Loadout (optional)
    public BehaviorType defaultBehavior;
    public List<SkillSlotDefinition> defaultSkills;

    // Progression (future)
    // public ProgressionTree progressionTree;
    // public List<PassiveDefinition> unlockedPassives;
}
```

**Notes:**
- Represents the "blueprint" of a hero or enemy
- Multiple UnitRuntime instances can reference the same UnitDefinition
- Never modified during battle

---

### 2. UnitRuntime (Runtime)

**Purpose:** Live instance of a unit during battle.

**Storage:** In-memory object (not ScriptableObject)

**Fields:**

```csharp
public class UnitRuntime
{
    // Reference to static definition
    public UnitDefinition definition;

    // Battle Identity
    public int runtimeId;              // Unique ID for this battle instance
    public UnitTeam team;              // Player or Enemy

    // Current State
    public int currentHP;
    public int maxHP;                  // May differ from baseStats due to buffs
    public GridPosition position;
    public bool isDead;

    // Current Stats (modified by buffs/items)
    public StatBlock currentStats;     // Runtime-modified copy of base stats

    // Loadout (configured pre-battle)
    public BehaviorLoadout behavior;
    public List<SkillSlot> equippedSkills;

    // Active Effects
    public List<StatusEffect> activeEffects;

    // Combat State
    public UnitIntent currentIntent;   // What the unit is trying to do
    public UnitRuntime currentTarget;  // Who they're targeting

    // Visual Reference (non-gameplay)
    public GameObject visualInstance;
}
```

**Notes:**
- Created at battle start, destroyed at battle end
- This is the "live" unit in combat
- Gameplay truth lives here, not in ScriptableObjects

---

### 3. BattleState (Runtime)

**Purpose:** Source of truth for the active battle.

**Storage:** In-memory singleton or manager-owned object

**Fields:**

```csharp
public class BattleState
{
    // Battle Identity
    public string battleId;
    public BattlePhase currentPhase;

    // Units
    public List<UnitRuntime> playerUnits;
    public List<UnitRuntime> enemyUnits;

    // Grid
    public GridMap grid;

    // Timing
    public float battleTime;           // Elapsed time since battle start
    public int currentTick;            // If using tick-based system

    // Victory State
    public bool isBattleOver;
    public BattleOutcome outcome;      // Victory, Defeat, or None

    // Combat Context (active resolution)
    public CombatContext activeCombat; // Current attack/skill being resolved
}
```

**Notes:**
- Single source of truth for entire battle
- Battle Manager owns this, other systems read/modify it
- Destroyed when battle ends

---

### 4. StatBlock (Data Structure)

**Purpose:** Container for unit stats.

**Fields:**

```csharp
public struct StatBlock
{
    public int maxHP;
    public int attack;
    public int defense;
    public float moveSpeed;            // Tiles per second or similar

    // Future stats
    // public int magicPower;
    // public int resistance;
    // public float critChance;
}
```

**Notes:**
- Used in both UnitDefinition (base) and UnitRuntime (current)
- Immutable struct for base stats, mutable copy for runtime

---

### 5. ProficiencySet (Data Structure)

**Purpose:** Defines what a hero is naturally good at.

**Fields:**

```csharp
public class ProficiencySet
{
    public Dictionary<ActionType, float> actionProficiencies;   // e.g., Punch: 1.2x
    public Dictionary<ElementType, float> elementProficiencies; // e.g., Fire: 1.3x
    public Dictionary<TechniqueType, float> techniqueProficiencies;

    public float GetProficiencyBonus(ActionType action);
    public float GetProficiencyBonus(ElementType element);
    public float GetProficiencyBonus(TechniqueType technique);
}
```

**Notes:**
- Influences technique effectiveness
- Does NOT lock heroes into roles, just guides strength
- Values > 1.0 = bonus, < 1.0 = penalty, 1.0 = neutral

---

### 6. BehaviorLoadout (Data Structure)

**Purpose:** Defines how a unit makes decisions in combat.

**Fields:**

```csharp
public class BehaviorLoadout
{
    public BehaviorType behaviorType;  // Aggressive, Defensive, Balanced, etc.

    // Future: More granular behavior settings
    // public TargetPriority targetPriority;
    // public MovementStyle movementStyle;
    // public float aggressionLevel;
}
```

**Notes:**
- Assigned pre-battle, not changed during combat
- Interpreted by BehaviorSystem to generate UnitIntent
- Behavior is **combat logic only**, not hero identity

---

### 7. SkillSlot (Data Structure)

**Purpose:** One of up to 5 skill slots a hero can equip.

**Fields:**

```csharp
public class SkillSlot
{
    public int slotIndex;              // 0-4 (for 5 slots)
    public List<ActionSlot> actionSequence; // 5 sub-slots containing actions

    public ResolvedTechnique ResolveSequence(UnitRuntime caster);
}
```

**Notes:**
- Each hero can have up to 5 SkillSlots (MVP: 1-2)
- Each SkillSlot contains 5 ActionSlots (MVP: 3-5)
- This is the core of the action-chain system

---

### 8. ActionSlot (Data Structure)

**Purpose:** One sub-slot within a SkillSlot, containing a single action.

**Fields:**

```csharp
public struct ActionSlot
{
    public int subSlotIndex;           // 0-4 (for 5 sub-slots)
    public ActionDefinition action;    // Reference to static action data
}
```

**Notes:**
- Simple container for one action in the sequence
- The combination of 5 ActionSlots forms an action-chain

---

### 9. ActionDefinition (Static)

**Purpose:** Defines a single action (punch, kick, sign, etc.).

**Storage:** ScriptableObject

**Fields:**

```csharp
public class ActionDefinition : ScriptableObject
{
    public string actionId;            // Unique ID (e.g., "action_punch")
    public string displayName;         // Human-readable (e.g., "Punch")
    public ActionType actionType;      // Physical, Elemental, Support, etc.
    public Sprite icon;                // UI icon

    // MVP: Simple properties
    public float basePower;            // Contribution to technique power
    public ElementType element;        // None, Fire, Earth, Lightning, etc.

    // Future: Animation, VFX, sound
    // public AnimationClip animation;
    // public GameObject vfxPrefab;
}
```

**Notes:**
- Represents a single building block of techniques
- Actions don't do damage directly, they contribute to resolved techniques

---

### 10. ResolvedTechnique (Runtime)

**Purpose:** The actual combat ability resolved from an action-chain.

**Fields:**

```csharp
public class ResolvedTechnique
{
    public string techniqueName;       // e.g., "Earth Fist", "Orb Strike"
    public TechniqueType type;         // Attack, Heal, Buff, Summon, OrbSummon, etc.
    public ElementType element;        // Elemental type (if applicable)

    public float power;                // Damage or healing amount (after multiplier)
    public CastType castType;          // Melee, Mobile, or Rooted
    public bool isCombo;               // true = matched a ComboRecipe, false = standalone actions

    public List<ActionDefinition> sourceActions; // Actions that formed this technique
}
```

**Notes:**
- Created by `SkillSystem.ResolveSkill()`
- `isCombo = true` → `BattleCombatResolver.ResolveSkillAttack()` handles it
- `isCombo = false` → `BattleCombatResolver.ExecuteIndividualActions()` fires each action's standalone effect
- Proficiencies modify the power of resolved techniques

---

### 11. GridPosition (Data Structure)

**Purpose:** Represents a position on the battle grid.

**Fields:**

```csharp
public struct GridPosition
{
    public int x;
    public int y;

    public GridPosition(int x, int y);

    public float DistanceTo(GridPosition other);
    public bool IsAdjacent(GridPosition other);
    public bool Equals(GridPosition other);

    public static GridPosition Zero => new GridPosition(0, 0);
}
```

**Notes:**
- Simple coordinate system
- Distance methods handle Manhattan or Euclidean as needed

---

### 12. GridMap (Runtime)

**Purpose:** Represents the battle grid.

**Fields:**

```csharp
public class GridMap
{
    public int width;
    public int height;
    private GridTile[,] tiles;

    public GridTile GetTile(GridPosition pos);
    public bool IsWalkable(GridPosition pos);
    public bool IsOccupied(GridPosition pos);
    public UnitRuntime GetUnitAt(GridPosition pos);
    public List<GridPosition> GetNeighbors(GridPosition pos);
}
```

**Notes:**
- Created at battle start
- Queried by movement, targeting, and skill systems

---

### 13. GridTile (Data Structure)

**Purpose:** Represents one tile on the grid.

**Fields:**

```csharp
public class GridTile
{
    public GridPosition position;
    public bool isWalkable;            // Can units move here?
    public UnitRuntime occupyingUnit;  // null if empty

    // Future: Terrain modifiers, hazards
    // public TerrainType terrain;
    // public List<GroundEffect> effects;
}
```

---

### 14. UnitIntent (Runtime)

**Purpose:** Represents what a unit is *trying* to do this frame/tick.

**Fields:**

```csharp
public class UnitIntent
{
    public UnitRuntime actor;          // Who is acting
    public IntentType type;            // Move, Attack, UseSkill, Wait
    public UnitRuntime target;         // Target unit (if applicable)
    public GridPosition targetPosition; // Target position (if applicable)
    public SkillSlot skillToUse;       // Which skill to execute (if applicable)
}
```

**Notes:**
- Generated by BehaviorSystem
- Consumed by Movement/Combat systems
- Intent ≠ execution (intent may fail due to range, death, etc.)

---

### 15. StatusEffect (Runtime)

**Purpose:** Buff, debuff, or temporary modifier on a unit.

**Fields:**

```csharp
public class StatusEffect
{
    public StatusEffectDefinition definition; // Reference to static data
    public UnitRuntime target;         // Who has this effect
    public int remainingDuration;      // Turns/ticks left
    public int stackCount;             // If stackable

    public void OnApply();
    public void OnTick();
    public void OnRemove();
}
```

**Notes:**
- MVP may not include status effects
- Future: Buffs, debuffs, DoTs, shields, etc.

---

### 16. StatusEffectDefinition (Static)

**Purpose:** Template for a status effect.

**Storage:** ScriptableObject

**Fields:**

```csharp
public class StatusEffectDefinition : ScriptableObject
{
    public string effectId;
    public string displayName;
    public Sprite icon;
    public EffectType type;            // Buff, Debuff, Neutral

    public int baseDuration;
    public bool isStackable;
    public int maxStacks;

    public StatModifier statModifier;  // e.g., +20% Attack

    // Future: More complex effects
    // public EffectBehavior behavior;
}
```

---

### 17. CombatContext (Runtime)

**Purpose:** Tracks an active attack/skill resolution.

**Fields:**

```csharp
public class CombatContext
{
    public UnitRuntime attacker;
    public UnitRuntime defender;
    public ResolvedTechnique technique; // null if basic attack

    public int baseDamage;
    public int finalDamage;            // After modifiers
    public List<Modifier> appliedModifiers;

    public bool isCritical;            // Future
    public bool isEvaded;              // Future
}
```

**Notes:**
- Created when damage/healing is resolved
- Allows modifiers from items/passives to hook in
- Prevents hardcoding all bonuses in one place

---

### 18. Modifier (Data Structure)

**Purpose:** Represents a single modifier to combat calculations.

**Fields:**

```csharp
public class Modifier
{
    public ModifierSource source;      // Item, Passive, StatusEffect, etc.
    public ModifierType type;          // Additive, Multiplicative, Override
    public float value;

    public int Apply(int baseValue);
}
```

**Notes:**
- Future-proofing for items, passives, skill trees
- Allows extensible combat resolution without hardcoding

---

## Shared Enums

---

### UnitTeam

```csharp
public enum UnitTeam
{
    Player,
    Enemy
}
```

---

### BattlePhase

```csharp
public enum BattlePhase
{
    NotStarted,
    Placement,     // Player placing units (pre-battle)
    Combat,        // Auto-resolving combat
    Victory,
    Defeat
}
```

---

### BattleOutcome

```csharp
public enum BattleOutcome
{
    None,
    Victory,
    Defeat
}
```

---

### BehaviorType

```csharp
public enum BehaviorType
{
    Aggressive,    // Advance and attack
    Defensive,     // Hold position, defend
    Balanced,      // Mixed approach
    // Future: Assassin, Skirmisher, Support, etc.
}
```

---

### IntentType

```csharp
public enum IntentType
{
    Wait,          // Do nothing
    Move,          // Reposition
    BasicAttack,   // Attack with basic attack
    UseSkill,      // Execute a skill slot
    Retreat        // Future
}
```

---

### ActionType

```csharp
public enum ActionType
{
    Physical,      // Punch, Kick — basic melee attack standalone
    Elemental,     // Hand signs — applies charge-based self-buff standalone
    Support,       // Focus — adds pending power boost standalone
    Movement,      // Step, Dash (future)
    OrbSummon,     // Combo trigger — spawns orbiting OrbProjectiles standalone
}
```

---

### ElementType

```csharp
public enum ElementType
{
    None,
    Fire,
    Water,
    Earth,
    Lightning,
    Wind
    // Future: Ice, Light, Dark, etc.
}
```

---

### TechniqueType

```csharp
public enum TechniqueType
{
    Attack,        // Deals damage — default combo resolution path
    Heal,          // Restores HP to caster
    Buff,          // Applies ActiveBuff (charge-based bonus damage)
    Debuff,        // Applies harmful effect (future)
    Utility,       // Other (movement, control, etc.) (future)
    Summon,        // Spawns a guardian unit via BattleSummonManager
    OrbSummon,     // Spawns orbiting OrbProjectiles via OrbBuffHandler
}
```

---

### TargetPattern

```csharp
public enum TargetPattern
{
    Single,        // One target
    AOE,           // Area of effect
    Line,          // Line of targets
    Self,          // Caster only
    AllAllies,
    AllEnemies
}
```

---

### EffectType

```csharp
public enum EffectType
{
    Buff,
    Debuff,
    Neutral        // Neither good nor bad
}
```

---

### ModifierSource

```csharp
public enum ModifierSource
{
    BaseStats,
    StatusEffect,
    Item,          // Future
    Passive,       // Future
    SkillTree,     // Future
    Behavior
}
```

---

### ModifierType

```csharp
public enum ModifierType
{
    Additive,      // +10 damage
    Multiplicative, // *1.2 damage
    Override       // Set to exact value
}
```

---

### CastType

```csharp
public enum CastType
{
    Melee,    // Unit chases to attack range, then executes instantly
    Mobile,   // Unit casts while moving toward target
    Rooted,   // Unit stops in place and channels for cast duration
}
```

---

---

### 19. ActiveBuff (Runtime)

**Purpose:** A charge-based temporary buff on a unit, applied by Elemental standalone actions.

**Fields:**

```csharp
public class ActiveBuff
{
    public int bonusDamage;    // Flat bonus damage added per hit
    public int charges;        // Decremented on each hit; buff removed when 0
    public ElementType element; // Element of the bonus damage
}
```

**Notes:**
- Stored in `UnitRuntime.activeBuffs` (list)
- Applied by `BattleCombatResolver.ExecuteIndividualActions()` for Elemental actions
- Consumed one charge per punch/kick hit in `ResolveBasicAttack()`

---

### 20. ComboRecipeDefinition (Static)

**Purpose:** ScriptableObject defining one combo recipe — an exact action-ID sequence that resolves to a named technique.

**Storage:** ScriptableObject (`Assets/Data/Combos/`)

**Fields:**

```csharp
public class ComboRecipeDefinition : ScriptableObject
{
    public string recipeId;            // Unique ID (e.g. "Combo_EarthFist")
    public string recipeName;          // Display name (e.g. "Earth Fist")
    public string description;
    public string[] actionIds;         // Exact sequence (e.g. ["handsign_a", "punch"])
    public TechniqueType techType;
    public ElementType element;
    public float powerMultiplier;      // Scales sum of action basePower values
    public CastType castType;
}
```

**Notes:**
- Generated by `SkillDataCreator.CreateComboLibrary()` from the TacticalRPG menu
- **Do not create manually** — use the editor tool to keep assets in sync with hardcoded fallbacks
- Longer sequences must be listed before shorter sub-sequences in the library

---

### 21. ComboLibraryAsset (Static)

**Purpose:** ScriptableObject container holding all `ComboRecipeDefinition` assets. Assigned to `TerrainBattleManager` and loaded at battle start.

**Storage:** ScriptableObject (`Assets/Data/Combos/ComboLibrary.asset`)

**Fields:**

```csharp
public class ComboLibraryAsset : ScriptableObject
{
    public List<ComboRecipeDefinition> recipes;
}
```

**Notes:**
- `TerrainBattleManager.Start()` calls `ComboLibrary.SetLibrary(asset)` to load it
- If the field is empty, `ComboLibrary` falls back to its hardcoded recipe list
- For adding or modifying skills see `Docs/SKILL_CREATION_GUIDE.md`

---

## Common Vocabulary

### Terms

- **Unit** - Any hero or enemy on the battlefield
- **Definition** - Static template data (never changes at runtime)
- **Runtime** - Live instance during battle (mutable)
- **Behavior** - Battle decision-making logic (not hero identity)
- **Role** - Overall combat archetype (tank, DPS, support)
- **Proficiency** - Natural strength with certain actions/elements
- **Action** - Single building block (punch, kick, sign)
- **Action-Chain** - Sequence of 5 actions in a skill slot
- **Technique** - Resolved combat ability from action-chain
- **Skill Slot** - One of 5 equippable action-chain containers
- **Intent** - What a unit wants to do (generated by behavior)
- **Execution** - Actually doing the action (may differ from intent)
- **Static Data** - Lives in ScriptableObjects, never modified
- **Runtime State** - Lives in memory, unique per battle instance
- **Combat Context** - Snapshot of active damage/healing resolution
- **Modifier** - Additive/multiplicative bonus (items, passives, buffs)

---

## Data Flow Example

**Pre-Battle:**
1. Player selects `UnitDefinition` (e.g., "Kai")
2. Player configures `SkillSlot` with 5 `ActionSlot` entries
3. Player assigns `BehaviorType` (e.g., Aggressive)
4. Player places unit on grid

**Battle Start:**
1. `BattleState` is created
2. `UnitRuntime` is created from `UnitDefinition`
3. `UnitRuntime` is placed on `GridMap` at `GridPosition`

**Each Frame/Tick:**
1. `BehaviorSystem` generates `UnitIntent` based on `BehaviorLoadout`
2. `TargetingSystem` selects target from enemy units
3. `MovementSystem` moves unit toward target (if needed)
4. `SkillSystem` resolves action-chain into `ResolvedTechnique`
5. `CombatResolutionSystem` applies damage using `CombatContext`
6. `StatusEffectSystem` ticks active effects
7. `WinConditionSystem` checks if battle is over

**Battle End:**
1. `BattleState.outcome` is set
2. `BattlePhase` changes to Victory or Defeat
3. Runtime data is discarded
4. Player returns to meta/progression layer

---

## Notes for AI Agent

- **Always use these models** - Don't invent new data structures without updating this doc
- **Static vs Runtime is critical** - Never store runtime state in ScriptableObjects
- **Enums should be extensible** - Design for future values
- **Keep models simple for MVP** - Add complexity only when needed
- **Data-driven > Hardcoded** - Prefer data files over code switches
- **Systems communicate through shared models** - Not direct dependencies

---

## Version

**Version 1.0** - Initial data model definitions for MVP
