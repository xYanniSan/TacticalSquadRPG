# SYSTEM_UNITS.md

## Purpose

This document defines the **Unit System**, which manages the difference between **static hero/enemy definitions** (templates) and **live runtime instances** (active units in battle). This system is critical for maintaining proper separation of static data and mutable battle state.

---

## Core Concept: Definition vs Runtime

### Two-Part Structure

Every unit exists in two forms:

1. **UnitDefinition (Static)** - Immutable template (ScriptableObject)
   - Hero base identity
   - Base stats
   - Proficiencies
   - Visual prefabs
   - Never modified at runtime

2. **UnitRuntime (Runtime)** - Live battle instance
   - Current HP, position, active buffs
   - Equipped skills and behavior
   - Combat state (intent, target)
   - Created at battle start, destroyed at battle end

---

## Responsibilities

The Unit System is responsible for:

- Defining the structure of static hero/enemy templates
- Defining the structure of live runtime unit instances
- Creating runtime units from static definitions
- Managing unit lifecycle (creation, battle, destruction)
- Tracking unit stats (base vs current)
- Managing hero proficiencies and role flexibility

---

## What Unit System Does NOT Do

- **Does not handle combat logic** - That's CombatResolutionSystem
- **Does not move units** - That's MovementSystem
- **Does not decide actions** - That's BehaviorSystem
- **Does not manage progression** - That's ProgressionSystem (future)

---

## Core Data Models

See DATA_MODELS.md for full definitions.

### UnitDefinition (Static)

```csharp
[CreateAssetMenu(menuName = "Game/Units/Hero")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public string unitId;              // "hero_kai", "enemy_grunt"
    public string displayName;         // "Kai", "Forest Bandit"
    public Sprite portrait;
    public GameObject visualPrefab;

    [Header("Base Stats")]
    public StatBlock baseStats;        // HP, Attack, Defense, Speed

    [Header("Proficiencies")]
    public ProficiencySet proficiencies;

    [Header("Default Loadout")]
    public BehaviorType defaultBehavior;
    public List<SkillSlotDefinition> defaultSkills;

    // Future: Progression, passives, lore, etc.
}
```

### UnitRuntime (Runtime)

```csharp
public class UnitRuntime
{
    // Reference to static template
    public UnitDefinition definition;

    // Battle Identity
    public int runtimeId;              // Unique ID per battle instance
    public UnitTeam team;              // Player or Enemy

    // Current State
    public int currentHP;
    public int maxHP;                  // May differ from base due to buffs
    public GridPosition position;
    public bool isDead;

    // Current Stats (modified by buffs/items)
    public StatBlock currentStats;

    // Loadout (configured pre-battle)
    public BehaviorLoadout behavior;
    public List<SkillSlot> equippedSkills;

    // Active Effects
    public List<StatusEffect> activeEffects;

    // Combat State
    public UnitIntent currentIntent;
    public UnitRuntime currentTarget;

    // Visual Reference (non-gameplay)
    public GameObject visualInstance;
}
```

---

## StatBlock Structure

### Base vs Current Stats

**Base Stats (in UnitDefinition):**
- Defined in ScriptableObject
- Never modified at runtime
- Represents hero's natural stats at current level

**Current Stats (in UnitRuntime):**
- Modified copy of base stats
- Affected by buffs, debuffs, items, passives
- Used for actual combat calculations

```csharp
public struct StatBlock
{
    public int maxHP;
    public int attack;
    public int defense;
    public float moveSpeed;

    // Future stats
    // public int magicPower;
    // public int resistance;
    // public float critChance;
    // public float attackSpeed;
}
```

### Applying Stat Modifiers

When a buff is applied:

```csharp
public void ApplyStatModifier(UnitRuntime unit, StatModifier modifier)
{
    // Modify current stats, not base stats
    if (modifier.affectsStat == StatType.Attack)
    {
        unit.currentStats.attack += modifier.value;
    }
}

public void RemoveStatModifier(UnitRuntime unit, StatModifier modifier)
{
    // Restore stats
    if (modifier.affectsStat == StatType.Attack)
    {
        unit.currentStats.attack -= modifier.value;
    }
}
```

**Base stats remain unchanged.**

---

## Hero Proficiencies

### ProficiencySet Structure

Proficiencies define what a hero is **naturally good at**.

```csharp
public class ProficiencySet
{
    // Action type proficiencies (Physical, Elemental, Support)
    public Dictionary<ActionType, float> actionProficiencies;

    // Elemental proficiencies (Fire, Water, Earth, Lightning, Wind)
    public Dictionary<ElementType, float> elementProficiencies;

    // Technique type proficiencies (Attack, Heal, Buff, Debuff)
    public Dictionary<TechniqueType, float> techniqueProficiencies;

    public float GetProficiencyBonus(ActionType action)
    {
        return actionProficiencies.TryGetValue(action, out float bonus) ? bonus : 1.0f;
    }

    public float GetProficiencyBonus(ElementType element)
    {
        return elementProficiencies.TryGetValue(element, out float bonus) ? bonus : 1.0f;
    }

    public float GetProficiencyBonus(TechniqueType technique)
    {
        return techniqueProficiencies.TryGetValue(technique, out float bonus) ? bonus : 1.0f;
    }
}
```

### Proficiency Values

- **1.0** = Neutral (no bonus or penalty)
- **> 1.0** = Bonus (e.g., 1.2 = +20% effectiveness)
- **< 1.0** = Penalty (e.g., 0.8 = -20% effectiveness)

### Example: Hero "Kai"

```csharp
// In Kai's UnitDefinition ScriptableObject
proficiencies = new ProficiencySet
{
    elementProficiencies = new Dictionary<ElementType, float>
    {
        { ElementType.Fire, 1.3f },      // +30% fire techniques
        { ElementType.Lightning, 1.1f }, // +10% lightning techniques
        { ElementType.Water, 0.8f }      // -20% water techniques
    },
    actionProficiencies = new Dictionary<ActionType, float>
    {
        { ActionType.Physical, 1.2f },   // +20% physical actions
        { ActionType.Elemental, 1.1f }   // +10% elemental actions
    }
};
```

**Effect:**
- When Kai uses a Fire technique, it deals 30% more damage/healing
- When Kai uses a Water technique, it deals 20% less

**This guides Kai toward fire-based builds, but doesn't lock him out of water builds.**

---

## Hero Role Flexibility

### Design Philosophy

Heroes should have:
- **Natural strengths** (proficiencies, base stats)
- **Flexible roles** (not hard-locked to one archetype)
- **Player influence** (behavior, skills, progression shape the role)

### Three-Part Hero Identity

A hero's combat identity comes from **three sources**:

1. **Base Identity (UnitDefinition)**
   - Base stats
   - Proficiencies
   - Visual appearance

2. **Configured Loadout (Pre-Battle)**
   - Equipped skill slots (action-chains)
   - Assigned behavior (aggressive, defensive, etc.)
   - Equipped items (future)

3. **Progression (Future)**
   - Stat growth
   - Unlocked passives
   - Skill tree choices

**Example:**

Hero "Rin" naturally has:
- High defense, moderate HP
- Earth proficiency
- Tank-like base stats

But the player can configure Rin as:
- **Defensive Tank** - Hold position, protect allies
- **Aggressive Bruiser** - Advance, use earth-based melee combos
- **Support Controller** - Earth walls, defensive buffs

**Role is influenced by proficiencies, not locked by them.**

---

## Unit Lifecycle

### 1. Static Definition Creation (Design Time)

Designers create UnitDefinition ScriptableObjects:

```csharp
// Assets/Data/Units/HeroKai.asset
UnitDefinition {
    unitId = "hero_kai",
    displayName = "Kai",
    baseStats = { maxHP: 100, attack: 15, defense: 8, moveSpeed: 1.0 },
    proficiencies = { Fire: 1.3, Physical: 1.2 },
    defaultBehavior = BehaviorType.Aggressive
}
```

### 2. Runtime Instance Creation (Battle Start)

BattleManager creates UnitRuntime from definition:

```csharp
public UnitRuntime CreateUnitInstance(UnitDefinition definition, UnitTeam team)
{
    var unit = new UnitRuntime
    {
        definition = definition,
        runtimeId = GenerateUniqueId(),
        team = team,

        // Copy base stats to current stats
        currentHP = definition.baseStats.maxHP,
        maxHP = definition.baseStats.maxHP,
        currentStats = definition.baseStats, // Struct copy

        // Initialize state
        position = GridPosition.Zero, // Set during placement
        isDead = false,

        // Copy default loadout (or use player-configured)
        behavior = new BehaviorLoadout { behaviorType = definition.defaultBehavior },
        equippedSkills = new List<SkillSlot>(),

        // Initialize empty runtime state
        activeEffects = new List<StatusEffect>(),
        currentIntent = null,
        currentTarget = null,
        visualInstance = null
    };

    return unit;
}
```

### 3. Battle Placement

Player places unit on grid:

```csharp
unit.position = new GridPosition(2, 3);
grid.SetOccupied(unit.position, unit);
```

### 4. Combat (Active)

During combat, UnitRuntime is modified:
- HP changes
- Position changes
- Stats modified by buffs
- Active effects applied/removed

**UnitDefinition is never modified.**

### 5. Battle End (Cleanup)

UnitRuntime instances are destroyed:

```csharp
public void CleanupBattle()
{
    foreach (var unit in battleState.GetAllUnits())
    {
        if (unit.visualInstance != null)
            Destroy(unit.visualInstance);
    }

    battleState.playerUnits.Clear();
    battleState.enemyUnits.Clear();

    // Runtime instances are garbage collected
}
```

**UnitDefinition assets remain unchanged, ready for next battle.**

---

## Unit Behavior vs Unit Identity

### Critical Distinction

**Behavior (BehaviorLoadout):**
- How the unit acts in battle
- Assigned pre-battle, fixed during combat
- Examples: Aggressive, Defensive, Balanced

**Identity (UnitDefinition + Loadout):**
- What the unit is (tank, assassin, mage, etc.)
- Combination of stats, proficiencies, skills, items
- More permanent than behavior

**Example:**

A hero might be a **Tank** (identity) but use **Aggressive** behavior (rush forward).

A hero might be a **Mage** (identity) but use **Defensive** behavior (stay back, wait for enemies).

**Behavior is a subset of identity, not the whole picture.**

---

## Static Data (ScriptableObjects)

### Creating Hero Definitions

Create ScriptableObjects for each hero:

```csharp
[CreateAssetMenu(menuName = "Game/Units/Hero")]
public class UnitDefinition : ScriptableObject
{
    // Fields defined above
}
```

**Location:** `Assets/Data/Units/Heroes/`

**Example files:**
- `HeroKai.asset`
- `HeroRin.asset`
- `HeroAkira.asset`

### Creating Enemy Definitions

Same structure, different data:

```csharp
[CreateAssetMenu(menuName = "Game/Units/Enemy")]
public class UnitDefinition : ScriptableObject
{
    // Same structure as heroes
}
```

**Location:** `Assets/Data/Units/Enemies/`

**Example files:**
- `EnemyGrunt.asset`
- `EnemyArcher.asset`
- `EnemyBoss.asset`

---

## Default Loadouts

### Default Skills

UnitDefinition can contain default skill loadouts:

```csharp
public class UnitDefinition : ScriptableObject
{
    public List<SkillSlotDefinition> defaultSkills;
}

[System.Serializable]
public class SkillSlotDefinition
{
    public int slotIndex;
    public List<ActionDefinition> actions; // 5 actions
}
```

**Usage:**

Players can customize skills pre-battle, or use defaults.

For MVP, enemies likely use default loadouts.

---

## Hero vs Enemy Differences

### Conceptually Similar

Both heroes and enemies use the same UnitDefinition/UnitRuntime structure.

### Practical Differences

**Heroes:**
- Controlled by player behavior configuration
- Have progression (future)
- Can equip items (future)
- Customizable skill loadouts

**Enemies:**
- Controlled by AI behavior (similar to player behavior, but fixed)
- No progression
- No items (or fixed items)
- Fixed skill loadouts

**For MVP, heroes and enemies can use the exact same data structures.**

---

## Proficiency Application

### How Proficiencies Work

When a technique is resolved, proficiencies modify effectiveness:

```csharp
public ResolvedTechnique ResolveSkill(UnitRuntime caster, SkillSlot skill)
{
    var technique = AnalyzeActionSequence(skill.actionSequence);

    // Apply proficiency bonuses
    float proficiencyBonus = 1.0f;

    if (technique.element != ElementType.None)
    {
        proficiencyBonus *= caster.definition.proficiencies.GetProficiencyBonus(technique.element);
    }

    proficiencyBonus *= caster.definition.proficiencies.GetProficiencyBonus(technique.type);

    technique.power = (int)(technique.basePower * proficiencyBonus);

    return technique;
}
```

**Example:**

- Kai uses Fire Breath (Fire element, Attack type)
- Base power: 50
- Fire proficiency: 1.3x
- Attack proficiency: 1.0x (neutral)
- Final power: 50 * 1.3 = 65

---

## Future: Progression Integration

### Stat Growth

Later, UnitDefinition might have progression data:

```csharp
public class UnitDefinition : ScriptableObject
{
    public ProgressionCurve statGrowth;
    public SkillTree skillTree;
    public List<PassiveDefinition> availablePassives;
}
```

### Persistent Hero Data

Player's heroes will have persistent progression:

```csharp
public class HeroSaveData
{
    public string heroId;
    public int level;
    public int experience;
    public List<string> unlockedPassives;
    public Dictionary<string, int> statBoosts;

    // Custom skill loadouts
    public List<SkillSlot> customSkills;
}
```

At battle start, merge save data with UnitDefinition to create UnitRuntime.

---

## Testing

### Unit Tests

```csharp
[Test]
public void CreateUnitRuntime_CopiesBaseStats()
{
    var definition = CreateTestHeroDefinition();
    definition.baseStats = new StatBlock { maxHP = 100, attack = 15 };

    var runtime = CreateUnitInstance(definition, UnitTeam.Player);

    Assert.AreEqual(100, runtime.currentHP);
    Assert.AreEqual(100, runtime.maxHP);
    Assert.AreEqual(15, runtime.currentStats.attack);
}

[Test]
public void ProficiencyBonus_AppliesCorrectly()
{
    var proficiencies = new ProficiencySet();
    proficiencies.elementProficiencies[ElementType.Fire] = 1.3f;

    float bonus = proficiencies.GetProficiencyBonus(ElementType.Fire);

    Assert.AreEqual(1.3f, bonus);
}

[Test]
public void ProficiencyBonus_DefaultsToNeutral()
{
    var proficiencies = new ProficiencySet();

    float bonus = proficiencies.GetProficiencyBonus(ElementType.Water);

    Assert.AreEqual(1.0f, bonus); // No bonus or penalty
}

[Test]
public void ModifyingRuntimeStats_DoesNotAffectDefinition()
{
    var definition = CreateTestHeroDefinition();
    definition.baseStats = new StatBlock { attack = 15 };

    var runtime = CreateUnitInstance(definition, UnitTeam.Player);

    // Modify runtime stats
    runtime.currentStats.attack += 10;

    // Base stats unchanged
    Assert.AreEqual(15, definition.baseStats.attack);
    Assert.AreEqual(25, runtime.currentStats.attack);
}
```

---

## Summary

The Unit System defines the **two-part structure** of units:

- ✅ **UnitDefinition** - Static template (ScriptableObject)
- ✅ **UnitRuntime** - Live battle instance (runtime object)
- ✅ **StatBlock** - Base stats vs current stats
- ✅ **ProficiencySet** - Natural strengths, not hard locks
- ✅ **Hero role flexibility** - Guided, not locked
- ✅ **Proper separation** - Static data never modified at runtime

**Units are created from templates, modified during battle, and destroyed afterward.**

---

## Related Documentation

- **DATA_MODELS.md** - UnitDefinition, UnitRuntime, StatBlock, ProficiencySet
- **ENGINEERING_RULES.md** - Static vs runtime separation rules
- **SYSTEM_PROGRESSION.md** - Future hero growth and progression
- **SYSTEM_SKILLS.md** - How proficiencies affect techniques
- **SYSTEM_BEHAVIOR.md** - Behavior vs identity distinction

---

## Version

**Version 1.0** - Initial unit system for MVP
