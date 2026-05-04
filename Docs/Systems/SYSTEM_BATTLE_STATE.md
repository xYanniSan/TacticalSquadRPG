# SYSTEM_BATTLE_STATE.md

## Purpose

This document defines the **Battle State System**, which acts as the **single source of truth** for the active battle. BattleState contains all runtime combat data and is the central data structure that other systems read and modify.

---

## Responsibilities

The Battle State System is responsible for:

- Maintaining the source of truth for battle data
- Tracking all active units (player and enemy)
- Managing battle phase transitions
- Tracking battle timing (ticks, elapsed time)
- Storing victory/defeat state
- Holding reference to grid and active combat context
- Providing centralized access to battle data

---

## What Battle State Does NOT Do

- **Does not execute combat logic** - That's CombatResolutionSystem
- **Does not move units** - That's MovementSystem
- **Does not generate unit intents** - That's BehaviorSystem
- **Does not control battle flow** - That's BattleManager (coordinator)

**BattleState is data, not logic.**

---

## Core Data Model

See DATA_MODELS.md for full definition.

### BattleState Structure

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

---

## Battle Phases

### BattlePhase Enum

```csharp
public enum BattlePhase
{
    NotStarted,   // Battle hasn't begun yet
    Placement,    // Player is placing units (pre-battle setup)
    Combat,       // Auto-resolving combat
    Victory,      // Player won
    Defeat        // Player lost
}
```

### Phase Transitions

```
NotStarted
    ↓ (Player enters battle)
Placement
    ↓ (Player confirms placement and starts battle)
Combat
    ↓ (All enemies defeated)
Victory
    OR
    ↓ (All player units defeated)
Defeat
```

### Phase Logic

**NotStarted:**
- Initial state
- No battle data loaded yet

**Placement:**
- Player assigns hero positions on grid
- Player configures final loadouts (if needed)
- Battle data is initialized but combat hasn't started

**Combat:**
- Auto-resolving combat active
- Units move, attack, use skills
- Systems are actively updating

**Victory:**
- All enemy units defeated
- Battle is over, combat stops
- Display rewards/results

**Defeat:**
- All player units defeated
- Battle is over, combat stops
- Display failure screen

---

## Unit Lists

### Player Units

`List<UnitRuntime> playerUnits`

- Contains all player-controlled heroes in this battle
- MVP: 2 player units
- Future: Up to 5 player units

**Operations:**
- Add unit when battle initializes
- Remove unit when it dies (or keep in list with `isDead = true`)
- Query alive units: `playerUnits.Where(u => !u.isDead)`

### Enemy Units

`List<UnitRuntime> enemyUnits`

- Contains all enemy units in this battle
- MVP: 2 enemy units
- Future: Variable count based on mission

**Operations:**
- Add units when battle initializes
- Remove unit when it dies (or keep in list with `isDead = true`)
- Query alive units: `enemyUnits.Where(u => !u.isDead)`

### Querying Units

```csharp
public List<UnitRuntime> GetAlivePlayerUnits()
{
    return playerUnits.Where(u => !u.isDead).ToList();
}

public List<UnitRuntime> GetAliveEnemyUnits()
{
    return enemyUnits.Where(u => !u.isDead).ToList();
}

public List<UnitRuntime> GetAllUnits()
{
    return playerUnits.Concat(enemyUnits).ToList();
}

public List<UnitRuntime> GetAliveUnits()
{
    return GetAllUnits().Where(u => !u.isDead).ToList();
}
```

---

## Grid Reference

### GridMap Storage

`GridMap grid`

- Reference to the battle grid
- Created at battle start by GridSystem
- Shared by all systems

Systems query the grid through BattleState:

```csharp
var distance = battleState.grid.GetDistance(unit1.position, unit2.position);
```

---

## Timing Models

BattleState supports multiple timing models. Choose one for MVP:

### Option 1: Real-Time (Continuous)

```csharp
public float battleTime; // Elapsed seconds since battle start

void Update(float deltaTime)
{
    battleTime += deltaTime;
    // Systems update continuously
}
```

**Pros:**
- Smooth, real-time feel
- Units can have different action speeds

**Cons:**
- Harder to predict/debug
- Timing precision issues

### Option 2: Tick-Based (Discrete)

```csharp
public int currentTick; // Discrete turn/tick counter

void Tick()
{
    currentTick++;
    // All units act once per tick
}
```

**Pros:**
- Easier to debug and predict
- Deterministic behavior
- Easier to implement action queues

**Cons:**
- Less smooth visually (but can hide with animations)

### MVP Recommendation: Tick-Based

For MVP, use **tick-based** for simplicity and clarity.

**Tick duration:** 0.5 seconds to 1.0 second (configurable)

---

## Victory and Defeat

### Win/Loss Tracking

```csharp
public bool isBattleOver;
public BattleOutcome outcome;

public enum BattleOutcome
{
    None,     // Battle still ongoing
    Victory,  // Player won
    Defeat    // Player lost
}
```

### Setting Outcome

Only the WinConditionSystem should set these values:

```csharp
public void SetVictory()
{
    isBattleOver = true;
    outcome = BattleOutcome.Victory;
    currentPhase = BattlePhase.Victory;
}

public void SetDefeat()
{
    isBattleOver = true;
    outcome = BattleOutcome.Defeat;
    currentPhase = BattlePhase.Defeat;
}
```

---

## Combat Context

### Active Combat Resolution

`CombatContext activeCombat`

- Tracks the currently resolving attack or skill
- Used by CombatResolutionSystem to apply modifiers
- Allows items, passives, buffs to hook into damage calculation

See SYSTEM_COMBAT_RESOLUTION.md for details.

**Example:**

```csharp
// CombatResolutionSystem creates context
battleState.activeCombat = new CombatContext
{
    attacker = unit1,
    defender = unit2,
    baseDamage = 10,
    appliedModifiers = new List<Modifier>()
};

// Modifier systems can inspect and modify
// (Future: ItemSystem, PassiveSystem, etc.)

// Apply final damage
ApplyDamage(battleState.activeCombat);

// Clear context
battleState.activeCombat = null;
```

---

## Initialization

### Creating BattleState

At battle start, BattleManager creates BattleState:

```csharp
public BattleState InitializeBattle(
    List<UnitDefinition> playerDefs,
    List<UnitDefinition> enemyDefs,
    int gridWidth,
    int gridHeight)
{
    var battleState = new BattleState
    {
        battleId = System.Guid.NewGuid().ToString(),
        currentPhase = BattlePhase.Placement,
        playerUnits = new List<UnitRuntime>(),
        enemyUnits = new List<UnitRuntime>(),
        grid = gridSystem.Initialize(gridWidth, gridHeight),
        battleTime = 0f,
        currentTick = 0,
        isBattleOver = false,
        outcome = BattleOutcome.None
    };

    // Create player units
    foreach (var def in playerDefs)
    {
        var unit = CreateUnitRuntime(def, UnitTeam.Player);
        battleState.playerUnits.Add(unit);
    }

    // Create enemy units
    foreach (var def in enemyDefs)
    {
        var unit = CreateUnitRuntime(def, UnitTeam.Enemy);
        battleState.enemyUnits.Add(unit);
    }

    return battleState;
}
```

### Creating UnitRuntime from Definition

```csharp
private UnitRuntime CreateUnitRuntime(UnitDefinition definition, UnitTeam team)
{
    return new UnitRuntime
    {
        definition = definition,
        runtimeId = GenerateRuntimeId(),
        team = team,
        currentHP = definition.baseStats.maxHP,
        maxHP = definition.baseStats.maxHP,
        position = GridPosition.Zero, // Set during placement
        isDead = false,
        currentStats = definition.baseStats, // Copy base stats
        behavior = new BehaviorLoadout { behaviorType = definition.defaultBehavior },
        equippedSkills = new List<SkillSlot>(), // Configured pre-battle
        activeEffects = new List<StatusEffect>(),
        currentIntent = null,
        currentTarget = null
    };
}
```

---

## Battle Loop Integration

The BattleManager uses BattleState to coordinate systems:

### Example Tick-Based Loop

```csharp
public class BattleManager : MonoBehaviour
{
    private BattleState battleState;
    private float tickTimer;
    private float tickDuration = 1.0f;

    void Update()
    {
        if (battleState.currentPhase != BattlePhase.Combat)
            return;

        if (battleState.isBattleOver)
            return;

        tickTimer += Time.deltaTime;

        if (tickTimer >= tickDuration)
        {
            tickTimer -= tickDuration;
            ProcessTick();
        }
    }

    void ProcessTick()
    {
        battleState.currentTick++;

        // Check win/loss first
        var outcome = winConditionSystem.CheckOutcome(battleState);
        if (outcome != BattleOutcome.None)
        {
            EndBattle(outcome);
            return;
        }

        // Process all alive units
        var aliveUnits = battleState.GetAliveUnits();

        foreach (var unit in aliveUnits)
        {
            // Generate intent
            var intent = behaviorSystem.GenerateIntent(unit, battleState);

            // Execute intent
            ExecuteIntent(unit, intent);
        }

        // Tick status effects
        statusEffectSystem.TickAllEffects(battleState);
    }
}
```

---

## State Queries

BattleState should expose helper methods for common queries:

### Unit Queries

```csharp
public UnitRuntime GetUnitById(int runtimeId)
{
    return GetAllUnits().FirstOrDefault(u => u.runtimeId == runtimeId);
}

public List<UnitRuntime> GetUnitsOnTeam(UnitTeam team)
{
    return team == UnitTeam.Player ? playerUnits : enemyUnits;
}

public List<UnitRuntime> GetAliveUnitsOnTeam(UnitTeam team)
{
    return GetUnitsOnTeam(team).Where(u => !u.isDead).ToList();
}

public int GetAlivePlayerCount()
{
    return playerUnits.Count(u => !u.isDead);
}

public int GetAliveEnemyCount()
{
    return enemyUnits.Count(u => !u.isDead);
}
```

### Phase Queries

```csharp
public bool IsInCombat()
{
    return currentPhase == BattlePhase.Combat;
}

public bool IsPlacementPhase()
{
    return currentPhase == BattlePhase.Placement;
}

public bool IsBattleOver()
{
    return isBattleOver;
}
```

---

## State Modifications

### Adding Units (Rare, usually only at init)

```csharp
public void AddPlayerUnit(UnitRuntime unit)
{
    unit.team = UnitTeam.Player;
    playerUnits.Add(unit);
}

public void AddEnemyUnit(UnitRuntime unit)
{
    unit.team = UnitTeam.Enemy;
    enemyUnits.Add(unit);
}
```

### Removing Units

When a unit dies, mark it as dead (or remove from list):

**Option 1: Mark as dead (keep in list)**
```csharp
public void KillUnit(UnitRuntime unit)
{
    unit.isDead = true;
    grid.ClearOccupied(unit.position);
}
```

**Option 2: Remove from list**
```csharp
public void RemoveUnit(UnitRuntime unit)
{
    if (unit.team == UnitTeam.Player)
        playerUnits.Remove(unit);
    else
        enemyUnits.Remove(unit);

    grid.ClearOccupied(unit.position);
}
```

**MVP Recommendation:** Mark as dead (keeps references valid, easier to debug).

---

## Persistence and Cleanup

### Battle End Cleanup

When battle ends, destroy runtime data:

```csharp
public void CleanupBattle()
{
    // Clear all runtime data
    playerUnits.Clear();
    enemyUnits.Clear();
    grid = null;
    activeCombat = null;

    // Runtime data is garbage collected
}
```

### Future: Battle Replay Data

Later, BattleState could record actions for replay:

```csharp
public class BattleState
{
    public List<BattleEvent> eventLog; // Record of all actions

    public void RecordEvent(BattleEvent evt)
    {
        eventLog.Add(evt);
    }
}
```

This allows:
- Replay system
- Analytics
- Debugging

---

## Debugging and Inspection

### Inspector-Friendly Structure

For Unity debugging, BattleState should be easy to inspect:

```csharp
[System.Serializable]
public class BattleState
{
    [Header("Battle Info")]
    public string battleId;
    public BattlePhase currentPhase;

    [Header("Units")]
    public List<UnitRuntime> playerUnits;
    public List<UnitRuntime> enemyUnits;

    [Header("Timing")]
    public float battleTime;
    public int currentTick;

    [Header("Outcome")]
    public bool isBattleOver;
    public BattleOutcome outcome;
}
```

This makes BattleState visible in Unity Inspector during runtime.

---

## Example Implementation

### Minimal BattleState for MVP

```csharp
[System.Serializable]
public class BattleState
{
    // Identity
    public string battleId;
    public BattlePhase currentPhase;

    // Units
    public List<UnitRuntime> playerUnits = new List<UnitRuntime>();
    public List<UnitRuntime> enemyUnits = new List<UnitRuntime>();

    // Grid
    public GridMap grid;

    // Timing (tick-based for MVP)
    public int currentTick;

    // Victory
    public bool isBattleOver;
    public BattleOutcome outcome;

    // Active combat
    public CombatContext activeCombat;

    // Helper Methods
    public List<UnitRuntime> GetAlivePlayerUnits()
    {
        return playerUnits.Where(u => !u.isDead).ToList();
    }

    public List<UnitRuntime> GetAliveEnemyUnits()
    {
        return enemyUnits.Where(u => !u.isDead).ToList();
    }

    public List<UnitRuntime> GetAllUnits()
    {
        return playerUnits.Concat(enemyUnits).ToList();
    }

    public bool IsInCombat()
    {
        return currentPhase == BattlePhase.Combat && !isBattleOver;
    }

    public void SetVictory()
    {
        isBattleOver = true;
        outcome = BattleOutcome.Victory;
        currentPhase = BattlePhase.Victory;
    }

    public void SetDefeat()
    {
        isBattleOver = true;
        outcome = BattleOutcome.Defeat;
        currentPhase = BattlePhase.Defeat;
    }
}
```

---

## Integration with Other Systems

### BattleManager

Owns and initializes BattleState:

```csharp
public class BattleManager : MonoBehaviour
{
    private BattleState battleState;

    public void StartBattle(...)
    {
        battleState = InitializeBattle(...);
    }
}
```

### All Systems

Read and modify BattleState:

```csharp
public class BehaviorSystem
{
    public UnitIntent GenerateIntent(UnitRuntime unit, BattleState state)
    {
        // Read from state
        var enemies = state.GetAliveEnemyUnits();
        // ...
    }
}

public class CombatResolutionSystem
{
    public void ResolveAttack(UnitRuntime attacker, UnitRuntime defender, BattleState state)
    {
        // Create combat context in state
        state.activeCombat = new CombatContext { ... };

        // Apply damage, modify unit HP
        defender.currentHP -= damage;

        // Clear context
        state.activeCombat = null;
    }
}
```

---

## Thread Safety (Future)

For MVP, BattleState is single-threaded.

Later, if multi-threading is needed:
- Use locks or concurrent collections
- Or keep battle logic single-threaded (recommended)

---

## Summary

BattleState is the **central data hub** for combat:

- ✅ Single source of truth for battle data
- ✅ Contains all units, grid, timing, outcome
- ✅ Manages battle phase transitions
- ✅ Provides centralized access for all systems
- ✅ Simple data structure, no complex logic
- ✅ Easy to inspect and debug

**BattleState is data. BattleManager is control flow. Systems are logic.**

---

## Related Documentation

- **DATA_MODELS.md** - BattleState, UnitRuntime, BattlePhase definitions
- **ARCHITECTURE_OVERVIEW.md** - BattleManager as coordinator pattern
- **SYSTEM_WIN_CONDITIONS.md** - Sets victory/defeat in BattleState
- **All other systems** - Read/modify BattleState

---

## Version

**Version 1.0** - Initial battle state system for MVP
