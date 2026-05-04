# SYSTEM_MOVEMENT.md

## Purpose

This document defines the **Movement System**, which is responsible for **executing unit movement** on the battle grid. The movement system takes movement intents from the BehaviorSystem and translates them into actual position changes.

---

## Responsibilities

The Movement System is responsible for:

- Executing unit movement from one grid position to another
- Updating grid occupancy when units move
- Validating movement destinations
- Handling simple pathfinding (or direct movement for MVP)
- Managing movement timing/speed
- Updating unit position in BattleState

---

## What Movement System Does NOT Do

- **Does not decide where to move** - That's BehaviorSystem
- **Does not handle combat** - That's CombatResolutionSystem
- **Does not generate paths** - That's GridSystem (or simple direct for MVP)
- **Does not handle movement visuals** - That's presentation layer

**Movement executes "go here", not "decide where".**

---

## Core Concept: Intent → Execution

### Flow

```
1. BehaviorSystem generates UnitIntent with type = Move
2. BattleManager calls MovementSystem.MoveUnit()
3. MovementSystem validates target position
4. MovementSystem updates unit position
5. MovementSystem updates grid occupancy
6. Movement complete
```

---

## Movement Models

Choose one for MVP:

### Option 1: Instant Movement (Simplest)

Unit teleports to target position instantly (one tile per tick).

**Pros:**
- Extremely simple
- No animation concerns
- Easy to implement and test

**Cons:**
- Visually jarring without animation

**For MVP:** This is fine if you add simple animation tweening in presentation layer.

### Option 2: Gradual Movement (Smoother)

Unit moves gradually over time (e.g., 0.5 seconds per tile).

**Pros:**
- Smoother visually
- More natural feel

**Cons:**
- Slightly more complex
- Need to handle mid-movement state

**For MVP:** Option 1 is recommended. Add gradual movement in presentation layer.

---

## Core Method: MoveUnit

```csharp
public class MovementSystem
{
    private GridSystem gridSystem;

    public bool MoveUnit(UnitRuntime unit, GridPosition targetPosition, BattleState state)
    {
        // Validate target position
        if (!IsValidMoveDestination(targetPosition, state.grid))
        {
            Debug.LogWarning($"Invalid move destination: {targetPosition}");
            return false;
        }

        // Clear old position
        state.grid.ClearOccupied(unit.position);

        // Update unit position
        GridPosition oldPosition = unit.position;
        unit.position = targetPosition;

        // Occupy new position
        state.grid.SetOccupied(targetPosition, unit);

        // Optional: Fire event for presentation layer
        OnUnitMoved?.Invoke(unit, oldPosition, targetPosition);

        return true;
    }

    private bool IsValidMoveDestination(GridPosition pos, GridMap grid)
    {
        // Must be in bounds
        if (!grid.IsInBounds(pos))
            return false;

        // Must be walkable
        if (!grid.IsWalkable(pos))
            return false;

        // Must be unoccupied
        if (grid.IsOccupied(pos))
            return false;

        return true;
    }

    public event Action<UnitRuntime, GridPosition, GridPosition> OnUnitMoved;
}
```

---

## Movement Distance

### MVP: One Tile Per Tick

For MVP, units move **one tile per tick** (or per action).

```csharp
// BehaviorSystem generates intent
var nextPosition = GetAdjacentPositionTowardTarget(unit.position, target.position);

var intent = new UnitIntent
{
    actor = unit,
    type = IntentType.Move,
    targetPosition = nextPosition // Only one tile away
};

// MovementSystem executes
movementSystem.MoveUnit(unit, intent.targetPosition, state);
```

### Future: Variable Movement Speed

Later, units can have different movement speeds:

```csharp
public class UnitRuntime
{
    public float movementPoints; // Tiles per tick
}

// Fast unit: 2 tiles per tick
// Normal: 1 tile per tick
// Slow: 0.5 tiles per tick (moves every 2 ticks)
```

---

## Pathfinding

### MVP: Direct Movement (No Pathfinding)

For MVP with small grids and no obstacles:

```csharp
public GridPosition GetNextStepToward(GridPosition from, GridPosition to, GridMap grid)
{
    var neighbors = grid.GetOrthogonalNeighbors(from);
    var validNeighbors = neighbors.Where(p => grid.IsValidMovePosition(p)).ToList();

    if (validNeighbors.Count == 0)
    {
        return from; // Can't move, stay put
    }

    // Find neighbor closest to target (greedy)
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

This is simple greedy movement: always move toward target.

**Works when:**
- No obstacles
- Small grid
- Simple straight-line movement

### Future: A* Pathfinding

When obstacles are added, use A* pathfinding:

```csharp
public List<GridPosition> FindPath(GridPosition start, GridPosition goal, GridMap grid)
{
    // A* implementation
    // Returns list of positions from start to goal
    // ...
}

public GridPosition GetNextStepOnPath(UnitRuntime unit, GridPosition goal, GridMap grid)
{
    // Get full path
    var path = FindPath(unit.position, goal, grid);

    if (path == null || path.Count < 2)
        return unit.position;

    // Return next step (first position after current)
    return path[1];
}
```

For MVP, **defer A*** unless obstacles are added.

---

## Movement Validation

### Blocked Movement

If target position is invalid, unit stays in place:

```csharp
public bool MoveUnit(UnitRuntime unit, GridPosition targetPosition, BattleState state)
{
    if (!IsValidMoveDestination(targetPosition, state.grid))
    {
        // Can't move, stay in place
        return false;
    }

    // Proceed with movement
    // ...
    return true;
}
```

### Unit Blocking

Units cannot move through each other:

```csharp
if (grid.IsOccupied(targetPosition))
{
    // Tile occupied by another unit
    return false;
}
```

**Exception (Future):** Some skills might allow teleportation or swapping.

---

## Movement Range

### Maximum Movement Per Tick

For MVP, units move **one tile per tick**.

### Range Checks

Some movement might have range limits:

```csharp
public bool CanReachInOneTick(GridPosition from, GridPosition to)
{
    float distance = CalculateDistance(from, to);
    return distance <= 1.0f; // One tile
}
```

---

## Repositioning vs Advancing

### Advancing (Moving Toward Enemy)

```csharp
var target = targetingSystem.SelectTarget(unit, enemies);
var nextPosition = GetNextStepToward(unit.position, target.position, grid);

movementSystem.MoveUnit(unit, nextPosition, state);
```

### Retreating (Moving Away)

Future behavior might retreat:

```csharp
public GridPosition GetPositionAwayFrom(GridPosition from, GridPosition threat, GridMap grid)
{
    var neighbors = grid.GetOrthogonalNeighbors(from);
    var validNeighbors = neighbors.Where(p => grid.IsValidMovePosition(p)).ToList();

    if (validNeighbors.Count == 0)
        return from;

    // Find neighbor farthest from threat
    GridPosition best = validNeighbors[0];
    float bestDistance = grid.GetDistance(best, threat);

    foreach (var neighbor in validNeighbors)
    {
        float distance = grid.GetDistance(neighbor, threat);
        if (distance > bestDistance)
        {
            best = neighbor;
            bestDistance = distance;
        }
    }

    return best;
}
```

---

## Movement and Timing

### Tick-Based Movement

For MVP using tick-based system:

```csharp
void ProcessTick()
{
    foreach (var unit in battleState.GetAliveUnits())
    {
        var intent = behaviorSystem.GenerateIntent(unit, battleState);

        if (intent.type == IntentType.Move)
        {
            movementSystem.MoveUnit(unit, intent.targetPosition, battleState);
        }
    }
}
```

### Real-Time Movement (Future)

For continuous real-time movement:

```csharp
void Update(float deltaTime)
{
    foreach (var unit in battleState.GetAliveUnits())
    {
        if (unit.isMoving)
        {
            movementSystem.UpdateMovement(unit, deltaTime);
        }
    }
}

public void UpdateMovement(UnitRuntime unit, float deltaTime)
{
    // Move toward target position over time
    float speed = unit.currentStats.moveSpeed; // Tiles per second
    float distance = speed * deltaTime;

    // Interpolate position
    // ...
}
```

For MVP, **tick-based is simpler**.

---

## Movement Priority

### Movement Order

If multiple units move in same tick, order matters if they're moving to same tile.

**Simple solution:** Process units in deterministic order (e.g., by runtimeId).

```csharp
var units = battleState.GetAliveUnits().OrderBy(u => u.runtimeId).ToList();

foreach (var unit in units)
{
    // Process movement
}
```

**Advanced solution (Future):** Use action queue or speed-based turn order.

---

## Integration with Other Systems

### With BehaviorSystem

```csharp
// BehaviorSystem generates movement intent
var intent = new UnitIntent
{
    actor = unit,
    type = IntentType.Move,
    targetPosition = nextPosition
};

return intent;
```

### With BattleManager

```csharp
// BattleManager executes intent
if (intent.type == IntentType.Move)
{
    bool success = movementSystem.MoveUnit(intent.actor, intent.targetPosition, battleState);

    if (!success)
    {
        // Movement failed, unit stays in place
    }
}
```

### With GridSystem

```csharp
// MovementSystem uses GridSystem for validation
if (!gridSystem.IsValidMovePosition(targetPosition))
{
    return false;
}

// Update occupancy
gridSystem.ClearOccupied(oldPosition);
gridSystem.SetOccupied(newPosition, unit);
```

---

## Visual Movement (Presentation Layer)

### Separation of Logic and Visuals

**MovementSystem (logic):**
- Updates `unit.position` instantly
- Updates grid occupancy
- Fires event

**Presentation layer (visuals):**
- Listens for `OnUnitMoved` event
- Animates visual movement smoothly

```csharp
// MovementSystem
movementSystem.OnUnitMoved += (unit, oldPos, newPos) =>
{
    // Presentation layer handles animation
    AnimateUnitMovement(unit.visualInstance, oldPos, newPos);
};

// Presentation
void AnimateUnitMovement(GameObject visual, GridPosition from, GridPosition to)
{
    Vector3 fromWorld = GridToWorld(from);
    Vector3 toWorld = GridToWorld(to);

    // Tween or lerp visual position
    visual.transform.DOMove(toWorld, 0.3f);
}
```

**Gameplay logic is instant. Visuals are smooth.**

---

## Movement Costs (Future)

### Terrain Movement Modifiers

Different terrain can have different movement costs:

```csharp
public class GridTile
{
    public float movementCost; // 1.0 = normal, 2.0 = difficult, 0.5 = road
}

// Movement uses up movement points based on terrain
public bool MoveUnit(UnitRuntime unit, GridPosition targetPosition)
{
    var tile = grid.GetTile(targetPosition);

    if (unit.remainingMovementPoints < tile.movementCost)
    {
        return false; // Not enough movement
    }

    unit.remainingMovementPoints -= tile.movementCost;

    // Move unit
    // ...
}
```

For MVP, **all tiles have equal cost** (or no cost).

---

## Testing

### Unit Tests

```csharp
[Test]
public void MoveUnit_UpdatesPosition()
{
    var grid = CreateTestGrid(10, 10);
    var unit = CreateTestUnit(new GridPosition(0, 0));
    var targetPos = new GridPosition(1, 0);
    var state = CreateBattleState(grid);

    bool success = movementSystem.MoveUnit(unit, targetPos, state);

    Assert.IsTrue(success);
    Assert.AreEqual(targetPos, unit.position);
}

[Test]
public void MoveUnit_UpdatesGridOccupancy()
{
    var grid = CreateTestGrid(10, 10);
    var unit = CreateTestUnit(new GridPosition(0, 0));
    grid.SetOccupied(unit.position, unit);
    var targetPos = new GridPosition(1, 0);
    var state = CreateBattleState(grid);

    movementSystem.MoveUnit(unit, targetPos, state);

    Assert.IsFalse(grid.IsOccupied(new GridPosition(0, 0))); // Old position clear
    Assert.IsTrue(grid.IsOccupied(targetPos)); // New position occupied
    Assert.AreEqual(unit, grid.GetUnitAt(targetPos));
}

[Test]
public void MoveUnit_FailsWhenPositionOccupied()
{
    var grid = CreateTestGrid(10, 10);
    var unit1 = CreateTestUnit(new GridPosition(0, 0));
    var unit2 = CreateTestUnit(new GridPosition(1, 0));
    grid.SetOccupied(unit2.position, unit2);
    var state = CreateBattleState(grid);

    bool success = movementSystem.MoveUnit(unit1, unit2.position, state);

    Assert.IsFalse(success);
    Assert.AreEqual(new GridPosition(0, 0), unit1.position); // Didn't move
}

[Test]
public void MoveUnit_FailsWhenOutOfBounds()
{
    var grid = CreateTestGrid(10, 10);
    var unit = CreateTestUnit(new GridPosition(0, 0));
    var targetPos = new GridPosition(-1, 0); // Out of bounds
    var state = CreateBattleState(grid);

    bool success = movementSystem.MoveUnit(unit, targetPos, state);

    Assert.IsFalse(success);
}

[Test]
public void GetNextStepToward_ReturnsAdjacentPosition()
{
    var grid = CreateTestGrid(10, 10);
    var from = new GridPosition(0, 0);
    var to = new GridPosition(5, 5);

    var nextStep = movementSystem.GetNextStepToward(from, to, grid);

    // Should move toward target (e.g., (1,0) or (0,1))
    Assert.AreEqual(1, grid.GetDistance(from, nextStep)); // One tile away
    Assert.IsTrue(grid.GetDistance(nextStep, to) < grid.GetDistance(from, to)); // Closer to target
}
```

---

## Performance Considerations

### For MVP

- 2v2 battles → 4 units max
- One tile per tick
- No performance concerns

### For Later

- Pathfinding optimization (A* with heuristics)
- Batch movement updates
- Cache paths for common routes

---

## Example Implementation

### Minimal MovementSystem for MVP

```csharp
public class MovementSystem
{
    public bool MoveUnit(UnitRuntime unit, GridPosition targetPosition, GridMap grid)
    {
        // Validate destination
        if (!grid.IsInBounds(targetPosition))
            return false;

        if (!grid.IsWalkable(targetPosition))
            return false;

        if (grid.IsOccupied(targetPosition))
            return false;

        // Clear old position
        grid.ClearOccupied(unit.position);

        // Update unit
        unit.position = targetPosition;

        // Occupy new position
        grid.SetOccupied(targetPosition, unit);

        return true;
    }

    public GridPosition GetNextStepToward(GridPosition from, GridPosition to, GridMap grid)
    {
        var neighbors = grid.GetOrthogonalNeighbors(from);
        var validNeighbors = neighbors.Where(p => grid.IsValidMovePosition(p)).ToList();

        if (validNeighbors.Count == 0)
            return from;

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
}
```

---

## Summary

The Movement System **executes unit movement** on the grid:

- ✅ Moves units from one position to another
- ✅ Updates grid occupancy
- ✅ Validates movement destinations
- ✅ Supports simple pathfinding (or direct movement for MVP)
- ✅ Separates logic from visual animation
- ✅ Works with BehaviorSystem (intent) and GridSystem (validation)

**Movement executes "go here". Behavior decides "where to go".**

---

## Related Documentation

- **DATA_MODELS.md** - UnitRuntime, GridPosition, GridMap
- **SYSTEM_GRID.md** - Grid validation and pathfinding support
- **SYSTEM_BEHAVIOR.md** - Generates movement intents
- **ARCHITECTURE_OVERVIEW.md** - System separation principles

---

## Version

**Version 1.0** - Initial movement system for MVP
