# SYSTEM_TARGETING.md

## Purpose

This document defines the **Targeting System**, which is responsible for **selecting which enemy a unit should attack**. The targeting system works in coordination with the BehaviorSystem to complete combat decision-making.

---

## Responsibilities

The Targeting System is responsible for:

- Selecting valid targets for attacks and skills
- Implementing targeting priorities (nearest, lowest HP, etc.)
- Validating targets (alive, in range, etc.)
- Handling target reacquisition when current target dies
- Supporting different targeting rules for different behaviors

---

## What Targeting System Does NOT Do

- **Does not decide when to attack** - That's BehaviorSystem
- **Does not execute attacks** - That's CombatResolutionSystem
- **Does not move units** - That's MovementSystem
- **Does not handle targeting visuals** - That's presentation layer

**Targeting selects WHO to attack. Behavior decides WHEN. Combat executes HOW.**

---

## Core Concept: Separation from Behavior

### Why Targeting is Separate

**BehaviorSystem** decides:
- "I want to attack"
- "I want to use a skill"

**TargetingSystem** decides:
- "Attack THIS enemy"
- "Use skill on THIS target"

This separation allows:
- Reusable targeting logic across behaviors
- Different targeting priorities for same behavior
- Future targeting customization (items, passives, player config)

---

## Targeting Priority Types

### MVP: Simple Nearest Enemy

For MVP, use the simplest targeting: **nearest enemy**.

```csharp
public enum TargetPriority
{
    Nearest,      // MVP: Closest enemy by distance

    // Future priorities:
    // LowestHP,     // Target enemy with least HP
    // HighestHP,    // Target tank/beefy enemy
    // Backline,     // Target farthest enemy
    // Random,       // Random valid enemy
    // MostDangerous // Target highest threat
}
```

---

## Core Methods

### Main Targeting Method

```csharp
public class TargetingSystem
{
    public UnitRuntime SelectTarget(UnitRuntime actor, List<UnitRuntime> potentialTargets)
    {
        if (potentialTargets == null || potentialTargets.Count == 0)
        {
            return null;
        }

        // Filter valid targets
        var validTargets = potentialTargets.Where(t => IsValidTarget(actor, t)).ToList();

        if (validTargets.Count == 0)
        {
            return null;
        }

        // Apply targeting priority
        return ApplyTargetingPriority(actor, validTargets, TargetPriority.Nearest);
    }

    private bool IsValidTarget(UnitRuntime actor, UnitRuntime target)
    {
        // Must be alive
        if (target.isDead)
            return false;

        // Must be on opposing team
        if (target.team == actor.team)
            return false;

        // Additional validation can go here (e.g., line of sight)

        return true;
    }

    private UnitRuntime ApplyTargetingPriority(
        UnitRuntime actor,
        List<UnitRuntime> validTargets,
        TargetPriority priority)
    {
        switch (priority)
        {
            case TargetPriority.Nearest:
                return SelectNearestTarget(actor, validTargets);

            // Future priorities:
            // case TargetPriority.LowestHP:
            //     return SelectLowestHPTarget(validTargets);

            default:
                return validTargets[0]; // Fallback
        }
    }
}
```

---

## Nearest Target Selection (MVP)

```csharp
private UnitRuntime SelectNearestTarget(UnitRuntime actor, List<UnitRuntime> targets)
{
    UnitRuntime nearest = targets[0];
    float nearestDistance = CalculateDistance(actor.position, nearest.position);

    foreach (var target in targets)
    {
        float distance = CalculateDistance(actor.position, target.position);
        if (distance < nearestDistance)
        {
            nearest = target;
            nearestDistance = distance;
        }
    }

    return nearest;
}

private float CalculateDistance(GridPosition a, GridPosition b)
{
    // Use grid's distance calculation (Manhattan for MVP)
    return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}
```

---

## Target Validation

### Valid Target Requirements

A target is valid if:
1. **Alive** - `!target.isDead`
2. **Enemy team** - `target.team != actor.team`
3. **In range** (optional, depends on use case)
4. **Line of sight** (future, if needed)

### Range Validation

For ranged attacks or skills, validate range:

```csharp
public bool IsInRange(UnitRuntime actor, UnitRuntime target, float maxRange)
{
    float distance = CalculateDistance(actor.position, target.position);
    return distance <= maxRange;
}

public UnitRuntime SelectTargetInRange(
    UnitRuntime actor,
    List<UnitRuntime> potentialTargets,
    float maxRange)
{
    var validTargets = potentialTargets
        .Where(t => IsValidTarget(actor, t))
        .Where(t => IsInRange(actor, t, maxRange))
        .ToList();

    if (validTargets.Count == 0)
        return null;

    return ApplyTargetingPriority(actor, validTargets, TargetPriority.Nearest);
}
```

---

## Target Reacquisition

### When Current Target Dies

If a unit's current target dies mid-combat, reacquire a new target:

```csharp
public UnitRuntime ReacquireTarget(UnitRuntime actor, BattleState state)
{
    // Get all enemies
    var enemies = actor.team == UnitTeam.Player
        ? state.GetAliveEnemyUnits()
        : state.GetAlivePlayerUnits();

    // Select new target
    return SelectTarget(actor, enemies);
}
```

### Integration with BattleManager

```csharp
void ProcessUnitAction(UnitRuntime unit)
{
    // Check if current target is still valid
    if (unit.currentTarget == null || unit.currentTarget.isDead)
    {
        unit.currentTarget = targetingSystem.ReacquireTarget(unit, battleState);
    }

    if (unit.currentTarget == null)
    {
        // No valid targets, wait
        return;
    }

    // Proceed with action
    // ...
}
```

---

## Future Targeting Priorities

### Lowest HP

Target the enemy with the least HP:

```csharp
private UnitRuntime SelectLowestHPTarget(List<UnitRuntime> targets)
{
    return targets.OrderBy(t => t.currentHP).First();
}
```

**Use case:** Finish off weak enemies

### Highest HP

Target the enemy with the most HP:

```csharp
private UnitRuntime SelectHighestHPTarget(List<UnitRuntime> targets)
{
    return targets.OrderByDescending(t => t.currentHP).First();
}
```

**Use case:** Focus fire on tanks

### Backline (Farthest)

Target the enemy farthest from the actor:

```csharp
private UnitRuntime SelectBacklineTarget(UnitRuntime actor, List<UnitRuntime> targets)
{
    UnitRuntime farthest = targets[0];
    float farthestDistance = CalculateDistance(actor.position, farthest.position);

    foreach (var target in targets)
    {
        float distance = CalculateDistance(actor.position, target.position);
        if (distance > farthestDistance)
        {
            farthest = target;
            farthestDistance = distance;
        }
    }

    return farthest;
}
```

**Use case:** Assassins targeting enemy backline

### Random

Select a random valid target:

```csharp
private UnitRuntime SelectRandomTarget(List<UnitRuntime> targets)
{
    int randomIndex = Random.Range(0, targets.Count);
    return targets[randomIndex];
}
```

**Use case:** Unpredictable behavior, variety

### Most Dangerous (Threat-Based)

Target based on threat level:

```csharp
private UnitRuntime SelectMostDangerousTarget(List<UnitRuntime> targets)
{
    // Calculate threat score (e.g., attack * HP / distance)
    return targets.OrderByDescending(t => CalculateThreat(t)).First();
}

private float CalculateThreat(UnitRuntime target)
{
    // Example threat calculation
    return target.currentStats.attack * (target.currentHP / 100f);
}
```

**Use case:** Tactical prioritization

---

## Configurable Targeting (Future)

### Per-Behavior Targeting

Later, behaviors can have targeting preferences:

```csharp
public class BehaviorLoadout
{
    public BehaviorType behaviorType;
    public TargetPriority targetingPriority; // Customizable

    // Example:
    // Aggressive + Nearest = Rush closest enemy
    // Defensive + LowestHP = Finish weak enemies
    // Assassin + Backline = Target enemy squishy
}
```

### Per-Hero Targeting

Heroes could have targeting preferences:

```csharp
public class UnitDefinition : ScriptableObject
{
    public TargetPriority defaultTargeting;
}
```

---

## Multi-Target Support (Future)

### AOE Skills

Some skills target multiple enemies:

```csharp
public List<UnitRuntime> SelectAOETargets(
    UnitRuntime actor,
    GridPosition center,
    int radius,
    BattleState state)
{
    var allEnemies = actor.team == UnitTeam.Player
        ? state.GetAliveEnemyUnits()
        : state.GetAlivePlayerUnits();

    return allEnemies
        .Where(e => CalculateDistance(center, e.position) <= radius)
        .ToList();
}
```

### All Enemies

```csharp
public List<UnitRuntime> SelectAllEnemies(UnitRuntime actor, BattleState state)
{
    return actor.team == UnitTeam.Player
        ? state.GetAliveEnemyUnits()
        : state.GetAlivePlayerUnits();
}
```

---

## Line of Sight (Future)

### Simple Line of Sight

Check if target is visible (no obstacles blocking):

```csharp
public bool HasLineOfSight(GridPosition from, GridPosition to, GridMap grid)
{
    // Raycast along grid (Bresenham's line algorithm)
    var line = GetLinePositions(from, to);

    foreach (var pos in line)
    {
        if (!grid.IsWalkable(pos))
            return false; // Obstacle blocks LOS
    }

    return true;
}

private List<GridPosition> GetLinePositions(GridPosition from, GridPosition to)
{
    // Bresenham's line algorithm
    // Returns all grid positions along line
    // ...
}
```

**For MVP, LOS is not needed** (no obstacles).

---

## Integration with BehaviorSystem

### Typical Flow

```csharp
// In BehaviorSystem
public UnitIntent GenerateIntent(UnitRuntime unit, BattleState state)
{
    // Get enemies
    var enemies = GetEnemies(unit, state);

    // Select target using TargetingSystem
    var target = targetingSystem.SelectTarget(unit, enemies);

    if (target == null)
    {
        return new UnitIntent { actor = unit, type = IntentType.Wait };
    }

    // Decide action based on behavior
    float distance = state.grid.GetDistance(unit.position, target.position);

    if (distance <= 1.0f) // In melee range
    {
        return new UnitIntent
        {
            actor = unit,
            type = IntentType.BasicAttack,
            target = target
        };
    }
    else
    {
        return new UnitIntent
        {
            actor = unit,
            type = IntentType.Move,
            targetPosition = GetPositionToward(unit.position, target.position)
        };
    }
}
```

---

## Target Persistence

### Storing Current Target

Units can remember their current target:

```csharp
public class UnitRuntime
{
    public UnitRuntime currentTarget;
}
```

**Benefits:**
- Units don't constantly switch targets
- More predictable behavior
- Can track "focus fire"

**When to change target:**
- Current target dies
- New target becomes higher priority
- Behavior switches

### MVP: Reacquire Every Tick

For simplicity, MVP can select a new target every tick:

```csharp
// Every tick, select target fresh
var target = targetingSystem.SelectTarget(unit, enemies);
```

This is simple but may cause units to switch targets frequently.

### Better: Sticky Targets

```csharp
// Keep current target unless it's invalid
if (unit.currentTarget == null || unit.currentTarget.isDead)
{
    unit.currentTarget = targetingSystem.SelectTarget(unit, enemies);
}

var target = unit.currentTarget;
```

This makes units commit to a target until it dies.

---

## Testing

### Unit Tests

```csharp
[Test]
public void SelectTarget_ReturnsNearestEnemy()
{
    var actor = CreateTestUnit(new GridPosition(0, 0));
    var enemy1 = CreateTestUnit(new GridPosition(5, 5));
    var enemy2 = CreateTestUnit(new GridPosition(2, 2)); // Closer
    var enemies = new List<UnitRuntime> { enemy1, enemy2 };

    var target = targetingSystem.SelectTarget(actor, enemies);

    Assert.AreEqual(enemy2, target);
}

[Test]
public void SelectTarget_IgnoresDeadEnemies()
{
    var actor = CreateTestUnit(new GridPosition(0, 0));
    var enemy1 = CreateTestUnit(new GridPosition(1, 1));
    enemy1.isDead = true;
    var enemy2 = CreateTestUnit(new GridPosition(5, 5));
    var enemies = new List<UnitRuntime> { enemy1, enemy2 };

    var target = targetingSystem.SelectTarget(actor, enemies);

    Assert.AreEqual(enemy2, target); // Ignores dead enemy1
}

[Test]
public void SelectTarget_IgnoresSameTeam()
{
    var actor = CreateTestUnit(new GridPosition(0, 0), UnitTeam.Player);
    var ally = CreateTestUnit(new GridPosition(1, 1), UnitTeam.Player);
    var enemy = CreateTestUnit(new GridPosition(5, 5), UnitTeam.Enemy);
    var units = new List<UnitRuntime> { ally, enemy };

    var target = targetingSystem.SelectTarget(actor, units);

    Assert.AreEqual(enemy, target); // Ignores ally
}

[Test]
public void SelectTarget_ReturnsNull_WhenNoValidTargets()
{
    var actor = CreateTestUnit(new GridPosition(0, 0));
    var enemies = new List<UnitRuntime>();

    var target = targetingSystem.SelectTarget(actor, enemies);

    Assert.IsNull(target);
}

[Test]
public void IsInRange_ReturnsTrueWhenInRange()
{
    var actor = CreateTestUnit(new GridPosition(0, 0));
    var target = CreateTestUnit(new GridPosition(2, 0));

    bool inRange = targetingSystem.IsInRange(actor, target, maxRange: 3.0f);

    Assert.IsTrue(inRange);
}

[Test]
public void IsInRange_ReturnsFalseWhenOutOfRange()
{
    var actor = CreateTestUnit(new GridPosition(0, 0));
    var target = CreateTestUnit(new GridPosition(5, 5));

    bool inRange = targetingSystem.IsInRange(actor, target, maxRange: 3.0f);

    Assert.IsFalse(inRange);
}
```

---

## Performance Considerations

### For MVP

- 2v2 battles → Max 2 potential targets
- Simple distance calculation
- No performance concerns

### For Later

- If battles scale to 10+ units:
  - Cache distance calculations
  - Use spatial partitioning for range queries
  - Optimize priority sorting

---

## Example Implementation

### Minimal TargetingSystem for MVP

```csharp
public class TargetingSystem
{
    public UnitRuntime SelectTarget(UnitRuntime actor, List<UnitRuntime> potentialTargets)
    {
        if (potentialTargets == null || potentialTargets.Count == 0)
            return null;

        // Filter valid targets
        var validTargets = potentialTargets
            .Where(t => !t.isDead)
            .Where(t => t.team != actor.team)
            .ToList();

        if (validTargets.Count == 0)
            return null;

        // Select nearest
        return SelectNearestTarget(actor, validTargets);
    }

    private UnitRuntime SelectNearestTarget(UnitRuntime actor, List<UnitRuntime> targets)
    {
        UnitRuntime nearest = targets[0];
        float nearestDistance = CalculateDistance(actor.position, nearest.position);

        foreach (var target in targets.Skip(1))
        {
            float distance = CalculateDistance(actor.position, target.position);
            if (distance < nearestDistance)
            {
                nearest = target;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private float CalculateDistance(GridPosition a, GridPosition b)
    {
        // Manhattan distance
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public bool IsInRange(UnitRuntime actor, UnitRuntime target, float maxRange)
    {
        float distance = CalculateDistance(actor.position, target.position);
        return distance <= maxRange;
    }
}
```

---

## Summary

The Targeting System is responsible for **selecting WHO to attack**:

- ✅ Selects valid targets from potential enemies
- ✅ Implements targeting priorities (MVP: Nearest)
- ✅ Validates targets (alive, enemy team, range)
- ✅ Supports target reacquisition
- ✅ Separates from BehaviorSystem (WHEN to attack)
- ✅ Extensible for future priorities (lowest HP, backline, etc.)

**Targeting selects WHO. Behavior decides WHEN. Combat executes HOW.**

---

## Related Documentation

- **DATA_MODELS.md** - UnitRuntime, GridPosition
- **SYSTEM_BEHAVIOR.md** - Uses targeting to complete intent generation
- **SYSTEM_GRID.md** - Distance calculations
- **SYSTEM_COMBAT_RESOLUTION.md** - Executes attacks on selected targets

---

## Version

**Version 1.0** - Initial targeting system for MVP
