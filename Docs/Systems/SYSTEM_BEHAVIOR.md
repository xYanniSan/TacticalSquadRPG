# SYSTEM_BEHAVIOR.md

## Purpose

This document defines the **Behavior System**, which generates **unit intents** (what units want to do) based on configured battle logic. The behavior system is the AI/decision-making layer for auto-combat.

---

## Critical Distinction: Behavior vs Identity

**Behavior** = Battle decision-making logic (how units act in combat)
**Identity** = Overall hero archetype (tank, DPS, mage, etc.)

**Behavior is a SUBSET of identity, not the whole thing.**

A hero's full identity comes from:
1. Base stats and proficiencies (UnitDefinition)
2. Equipped skills and action-chains (SkillSlots)
3. Behavior configuration (BehaviorLoadout)
4. Items and passives (future)

**Behavior is ONLY about combat decision-making.**

---

## Responsibilities

The Behavior System is responsible for:

- Generating `UnitIntent` for each unit each tick/frame
- Deciding whether to move, attack, use skill, or wait
- Selecting which skill to use (if any)
- Delegating target selection to TargetingSystem
- Implementing behavior types (Aggressive, Defensive, Balanced, etc.)
- Making decisions based on battle state (enemies nearby, HP, etc.)

---

## What Behavior System Does NOT Do

- **Does not execute movement** - That's MovementSystem
- **Does not execute attacks** - That's CombatResolutionSystem
- **Does not resolve skills** - That's SkillSystem
- **Does not select targets** - That's TargetingSystem
- **Does not define hero identity** - That's UnitDefinition + loadout
- **Does not handle visuals** - That's presentation layer

**Behavior generates intent. Other systems execute intent.**

---

## Core Data Models

See DATA_MODELS.md for full definitions.

### BehaviorLoadout

```csharp
public class BehaviorLoadout
{
    public BehaviorType behaviorType;  // Aggressive, Defensive, Balanced

    // Future: More granular settings
    // public TargetPriority targetPriority;
    // public float aggressionLevel;
    // public MovementStyle movementStyle;
}
```

### BehaviorType Enum

```csharp
public enum BehaviorType
{
    Aggressive,   // Advance toward enemies, attack frequently
    Defensive,    // Hold position, wait for enemies
    Balanced      // Mixed approach

    // Future:
    // Skirmisher,  // Hit and run
    // Support,     // Prioritize healing/buffing
    // Assassin,    // Target backline, burst damage
}
```

### UnitIntent

```csharp
public class UnitIntent
{
    public UnitRuntime actor;          // Who is acting
    public IntentType type;            // Move, Attack, UseSkill, Wait
    public UnitRuntime target;         // Target unit (if applicable)
    public GridPosition targetPosition; // Target position (if applicable)
    public SkillSlot skillToUse;       // Which skill to execute
}
```

### IntentType Enum

```csharp
public enum IntentType
{
    Wait,          // Do nothing this tick
    Move,          // Move toward a position
    BasicAttack,   // Use basic attack
    UseSkill,      // Execute a skill slot
    Retreat        // Future: Move away from danger
}
```

---

## Behavior Types (MVP)

For MVP, implement **2-3 simple behavior types**.

### 1. Aggressive

**Philosophy:** Rush toward enemies, attack as soon as possible.

**Decision logic:**
1. If enemy in attack range → BasicAttack
2. If skill available and enemy in range → UseSkill
3. If no enemy in range → Move toward nearest enemy
4. Else → Wait

**Characteristics:**
- Advances toward enemies
- Attacks frequently
- Doesn't retreat
- Simple, predictable

### 2. Defensive

**Philosophy:** Hold position, wait for enemies to come close.

**Decision logic:**
1. If enemy in attack range → BasicAttack
2. If skill available and enemy in range → UseSkill
3. Else → Wait (don't advance)

**Characteristics:**
- Holds position
- Only attacks enemies that come close
- Never advances
- Conservative

### 3. Balanced (Optional for MVP)

**Philosophy:** Mix of aggressive and defensive.

**Decision logic:**
1. If enemy in attack range → BasicAttack
2. If skill available → UseSkill
3. If enemy is far away → Move closer (but not too far from starting position)
4. Else → Wait

**Characteristics:**
- Moves cautiously
- Maintains medium range
- Balances offense and defense

---

## Behavior Decision Flow

### Each Tick, For Each Unit:

```
1. Check if unit is alive
   └─ If dead → Skip

2. BehaviorSystem.GenerateIntent(unit, battleState)
   ├─ Read unit's BehaviorLoadout
   ├─ Evaluate battle situation
   ├─ Decide action type
   ├─ Select target (via TargetingSystem)
   └─ Return UnitIntent

3. BattleManager executes intent
   ├─ If Move → MovementSystem.MoveUnit()
   ├─ If BasicAttack → CombatResolutionSystem.ResolveBasicAttack()
   ├─ If UseSkill → SkillSystem.ResolveSkill() + CombatResolutionSystem
   └─ If Wait → Do nothing
```

---

## Generating Intent

### Main Method

```csharp
public class BehaviorSystem
{
    private TargetingSystem targetingSystem;

    public UnitIntent GenerateIntent(UnitRuntime unit, BattleState state)
    {
        if (unit.isDead)
        {
            return new UnitIntent { actor = unit, type = IntentType.Wait };
        }

        switch (unit.behavior.behaviorType)
        {
            case BehaviorType.Aggressive:
                return GenerateAggressiveIntent(unit, state);

            case BehaviorType.Defensive:
                return GenerateDefensiveIntent(unit, state);

            case BehaviorType.Balanced:
                return GenerateBalancedIntent(unit, state);

            default:
                return new UnitIntent { actor = unit, type = IntentType.Wait };
        }
    }
}
```

---

## Aggressive Behavior Logic

```csharp
private UnitIntent GenerateAggressiveIntent(UnitRuntime unit, BattleState state)
{
    // Get enemies
    var enemies = GetEnemies(unit, state);
    if (enemies.Count == 0)
    {
        return new UnitIntent { actor = unit, type = IntentType.Wait };
    }

    // Select target
    var target = targetingSystem.SelectTarget(unit, enemies);

    // Check if in attack range
    float distance = state.grid.GetDistance(unit.position, target.position);
    float attackRange = 1.0f; // Melee range (adjacent)

    if (distance <= attackRange)
    {
        // In range, attack
        return new UnitIntent
        {
            actor = unit,
            type = IntentType.BasicAttack,
            target = target
        };
    }
    else
    {
        // Not in range, move closer
        var targetPosition = GetPositionTowardTarget(unit.position, target.position, state.grid);

        return new UnitIntent
        {
            actor = unit,
            type = IntentType.Move,
            targetPosition = targetPosition
        };
    }
}
```

---

## Defensive Behavior Logic

```csharp
private UnitIntent GenerateDefensiveIntent(UnitRuntime unit, BattleState state)
{
    // Get enemies
    var enemies = GetEnemies(unit, state);
    if (enemies.Count == 0)
    {
        return new UnitIntent { actor = unit, type = IntentType.Wait };
    }

    // Select target
    var target = targetingSystem.SelectTarget(unit, enemies);

    // Check if in attack range
    float distance = state.grid.GetDistance(unit.position, target.position);
    float attackRange = 1.0f;

    if (distance <= attackRange)
    {
        // Enemy is close, attack
        return new UnitIntent
        {
            actor = unit,
            type = IntentType.BasicAttack,
            target = target
        };
    }
    else
    {
        // Enemy is far, wait (don't advance)
        return new UnitIntent
        {
            actor = unit,
            type = IntentType.Wait
        };
    }
}
```

---

## Helper Methods

### Get Enemies

```csharp
private List<UnitRuntime> GetEnemies(UnitRuntime unit, BattleState state)
{
    if (unit.team == UnitTeam.Player)
    {
        return state.GetAliveEnemyUnits();
    }
    else
    {
        return state.GetAlivePlayerUnits();
    }
}
```

### Get Position Toward Target

```csharp
private GridPosition GetPositionTowardTarget(GridPosition from, GridPosition to, GridMap grid)
{
    // Get next step toward target (simple greedy)
    var neighbors = grid.GetOrthogonalNeighbors(from);
    var validNeighbors = neighbors.Where(p => grid.IsValidMovePosition(p)).ToList();

    if (validNeighbors.Count == 0)
    {
        return from; // Can't move, stay in place
    }

    // Find neighbor closest to target
    GridPosition best = validNeighbors[0];
    float bestDistance = grid.GetDistance(best, to);

    foreach (var neighbor in validNeighbors)
    {
        float distance = grid.GetDistance(neighbor, to);
        if (distance < bestDistance)
        {
            best = neighbor;
            bestDistance = distance;
        }
    }

    return best;
}
```

---

## Skill Usage (Future MVP Extension)

### Simple Skill Logic

For MVP, skills can be used when available:

```csharp
private UnitIntent GenerateAggressiveIntentWithSkills(UnitRuntime unit, BattleState state)
{
    var enemies = GetEnemies(unit, state);
    if (enemies.Count == 0)
    {
        return new UnitIntent { actor = unit, type = IntentType.Wait };
    }

    var target = targetingSystem.SelectTarget(unit, enemies);

    // Check if we have a skill available
    if (unit.equippedSkills.Count > 0)
    {
        var skill = unit.equippedSkills[0]; // Use first skill
        float skillRange = 2.0f; // Example skill range
        float distance = state.grid.GetDistance(unit.position, target.position);

        if (distance <= skillRange)
        {
            // Use skill
            return new UnitIntent
            {
                actor = unit,
                type = IntentType.UseSkill,
                target = target,
                skillToUse = skill
            };
        }
    }

    // Fallback to basic attack or movement
    // (same logic as before)
    ...
}
```

### Skill Priority (Future)

Later, behavior can have skill usage rules:
- Use skill on cooldown
- Save skill for specific conditions
- Prioritize certain skills over others

For MVP, simple "use skill when available" is enough.

---

## Movement Constraints

### Maximum Movement Per Tick

For MVP, units move **one tile per tick**:

```csharp
public void MoveUnit(UnitRuntime unit, GridPosition targetPosition)
{
    // Move one step toward target
    unit.position = targetPosition;
    grid.MoveUnit(unit, targetPosition);
}
```

### Future: Movement Speed

Later, units can have different movement speeds:

```csharp
public class UnitRuntime
{
    public float movementPoints; // Tiles per tick
}

// Fast unit: 2 tiles per tick
// Slow unit: 1 tile per tick
// Very slow: 0.5 tiles per tick (moves every 2 ticks)
```

---

## Behavior Configuration (Pre-Battle)

### Player Assigns Behavior

Before battle starts, player assigns behavior to heroes:

```csharp
public void AssignBehavior(UnitRuntime hero, BehaviorType behavior)
{
    hero.behavior = new BehaviorLoadout
    {
        behaviorType = behavior
    };
}
```

### Enemy Default Behavior

Enemies use default behavior from their UnitDefinition:

```csharp
enemyUnit.behavior = new BehaviorLoadout
{
    behaviorType = enemyUnit.definition.defaultBehavior
};
```

---

## Advanced Behavior (Future)

### Conditional Logic

Later, behavior can include conditions:

```csharp
public class BehaviorLoadout
{
    public BehaviorType behaviorType;

    // Conditional overrides
    public float retreatHPThreshold;   // Retreat if HP < 30%
    public bool prioritizeBackline;    // Target backline first
    public bool saveSkillForBoss;      // Don't use skill on weak enemies
}
```

### Behavior Switches

Future behavior could change mid-combat:

```csharp
if (unit.currentHP < unit.maxHP * 0.3f)
{
    // Switch to defensive behavior when low HP
    unit.behavior.behaviorType = BehaviorType.Defensive;
}
```

For MVP, behavior is **fixed during combat**.

---

## Behavior vs Targeting

### Separation of Concerns

**BehaviorSystem:**
- Decides WHAT to do (move, attack, skill, wait)
- Decides WHEN to act

**TargetingSystem:**
- Decides WHO to target

**Example:**

```csharp
// BehaviorSystem decides: "I want to attack"
var intent = new UnitIntent { type = IntentType.BasicAttack };

// TargetingSystem decides: "Attack THIS enemy"
intent.target = targetingSystem.SelectTarget(unit, enemies);

return intent;
```

This separation allows:
- Different targeting rules per behavior
- Reusable targeting logic
- Future targeting priority system

---

## Testing

### Unit Tests

```csharp
[Test]
public void AggressiveBehavior_AdvancesTowardEnemy()
{
    var grid = CreateTestGrid(10, 10);
    var unit = CreateTestUnit(BehaviorType.Aggressive, new GridPosition(0, 0));
    var enemy = CreateTestUnit(BehaviorType.Aggressive, new GridPosition(5, 5));
    var state = CreateBattleState(new[] { unit }, new[] { enemy }, grid);

    var intent = behaviorSystem.GenerateIntent(unit, state);

    Assert.AreEqual(IntentType.Move, intent.type);
    Assert.IsTrue(IsCloserToEnemy(intent.targetPosition, unit.position, enemy.position));
}

[Test]
public void AggressiveBehavior_AttacksWhenInRange()
{
    var grid = CreateTestGrid(10, 10);
    var unit = CreateTestUnit(BehaviorType.Aggressive, new GridPosition(0, 0));
    var enemy = CreateTestUnit(BehaviorType.Aggressive, new GridPosition(1, 0)); // Adjacent
    var state = CreateBattleState(new[] { unit }, new[] { enemy }, grid);

    var intent = behaviorSystem.GenerateIntent(unit, state);

    Assert.AreEqual(IntentType.BasicAttack, intent.type);
    Assert.AreEqual(enemy, intent.target);
}

[Test]
public void DefensiveBehavior_WaitsWhenEnemyIsFar()
{
    var grid = CreateTestGrid(10, 10);
    var unit = CreateTestUnit(BehaviorType.Defensive, new GridPosition(0, 0));
    var enemy = CreateTestUnit(BehaviorType.Aggressive, new GridPosition(5, 5));
    var state = CreateBattleState(new[] { unit }, new[] { enemy }, grid);

    var intent = behaviorSystem.GenerateIntent(unit, state);

    Assert.AreEqual(IntentType.Wait, intent.type);
}

[Test]
public void DefensiveBehavior_AttacksWhenEnemyIsClose()
{
    var grid = CreateTestGrid(10, 10);
    var unit = CreateTestUnit(BehaviorType.Defensive, new GridPosition(0, 0));
    var enemy = CreateTestUnit(BehaviorType.Aggressive, new GridPosition(1, 0)); // Adjacent
    var state = CreateBattleState(new[] { unit }, new[] { enemy }, grid);

    var intent = behaviorSystem.GenerateIntent(unit, state);

    Assert.AreEqual(IntentType.BasicAttack, intent.type);
}
```

---

## Performance Considerations

### For MVP

- 2v2 battles → 4 units max
- Simple behavior logic → Very fast
- No performance concerns

### For Later

- If battles get large (10+ units), batch intent generation
- Cache frequently used calculations
- Optimize pathfinding

---

## Summary

The Behavior System is the **AI decision-making layer**:

- ✅ Generates `UnitIntent` based on battle situation
- ✅ Implements behavior types (Aggressive, Defensive, etc.)
- ✅ Delegates target selection to TargetingSystem
- ✅ Separates "what to do" from "how to do it"
- ✅ Simple, readable, testable logic
- ✅ Behavior is combat logic, not full hero identity

**Behavior decides intent. Other systems execute it.**

---

## Related Documentation

- **DATA_MODELS.md** - BehaviorLoadout, UnitIntent, BehaviorType
- **PROJECT_OVERVIEW.md** - Behavior vs role distinction
- **SYSTEM_TARGETING.md** - How targets are selected
- **SYSTEM_MOVEMENT.md** - How movement intent is executed
- **SYSTEM_COMBAT_RESOLUTION.md** - How attack intent is executed

---

## Version

**Version 1.0** - Initial behavior system for MVP
