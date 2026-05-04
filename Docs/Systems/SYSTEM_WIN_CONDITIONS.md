# SYSTEM_WIN_CONDITIONS.md

## Purpose

This document defines the **Win Condition System**, which determines when a battle ends and who won. This system checks battle outcomes and triggers battle-end flow.

---

## Responsibilities

The Win Condition System is responsible for:

- Checking if battle is over
- Determining victory or defeat
- Supporting different mission objectives (future)
- Triggering battle-end transitions

---

## What Win Condition System Does NOT Do

- **Does not control battle flow** - That's BattleManager
- **Does not handle rewards** - That's future ProgressionSystem or RewardsSystem
- **Does not show UI** - That's presentation layer

**Win Condition checks "is battle over?" and "who won?".**

---

## MVP Win Condition

### Simple: Defeat All Enemies

For MVP, the only win condition is:

**Victory:** All enemy units are dead
**Defeat:** All player units are dead

---

## Core Method

```csharp
public class WinConditionSystem
{
    public BattleOutcome CheckOutcome(BattleState state)
    {
        // Check if all enemies are dead
        int aliveEnemies = state.GetAliveEnemyUnits().Count;
        if (aliveEnemies == 0)
        {
            return BattleOutcome.Victory;
        }

        // Check if all players are dead
        int alivePlayers = state.GetAlivePlayerUnits().Count;
        if (alivePlayers == 0)
        {
            return BattleOutcome.Defeat;
        }

        // Battle still ongoing
        return BattleOutcome.None;
    }
}
```

---

## BattleOutcome Enum

```csharp
public enum BattleOutcome
{
    None,      // Battle still ongoing
    Victory,   // Player won
    Defeat     // Player lost
}
```

---

## Integration with BattleManager

```csharp
public class BattleManager : MonoBehaviour
{
    private WinConditionSystem winConditionSystem;

    void ProcessTick()
    {
        // Check win/loss FIRST before processing units
        var outcome = winConditionSystem.CheckOutcome(battleState);

        if (outcome != BattleOutcome.None)
        {
            EndBattle(outcome);
            return;
        }

        // Continue combat
        ProcessUnits();
    }

    void EndBattle(BattleOutcome outcome)
    {
        // Set battle state
        if (outcome == BattleOutcome.Victory)
        {
            battleState.SetVictory();
        }
        else
        {
            battleState.SetDefeat();
        }

        // Trigger end-of-battle flow
        OnBattleEnd?.Invoke(outcome);
    }
}
```

---

## Future Win Conditions

### Mission-Specific Objectives

Later, missions can have custom win conditions:

#### 1. Survive for N Turns

```csharp
public class SurvivalObjective
{
    public int targetTurns;

    public BattleOutcome Check(BattleState state)
    {
        if (state.currentTick >= targetTurns)
        {
            return BattleOutcome.Victory; // Survived!
        }

        if (state.GetAlivePlayerUnits().Count == 0)
        {
            return BattleOutcome.Defeat; // All died
        }

        return BattleOutcome.None;
    }
}
```

#### 2. Protect Target

```csharp
public class ProtectObjective
{
    public UnitRuntime targetToProtect;

    public BattleOutcome Check(BattleState state)
    {
        if (targetToProtect.isDead)
        {
            return BattleOutcome.Defeat; // Failed to protect
        }

        if (state.GetAliveEnemyUnits().Count == 0)
        {
            return BattleOutcome.Victory; // All enemies defeated
        }

        return BattleOutcome.None;
    }
}
```

#### 3. Defeat Boss Only

```csharp
public class BossObjective
{
    public UnitRuntime bossUnit;

    public BattleOutcome Check(BattleState state)
    {
        if (bossUnit.isDead)
        {
            return BattleOutcome.Victory; // Boss defeated!
        }

        if (state.GetAlivePlayerUnits().Count == 0)
        {
            return BattleOutcome.Defeat;
        }

        return BattleOutcome.None;
    }
}
```

#### 4. Time Limit

```csharp
public class TimeLimitObjective
{
    public float timeLimit; // Seconds

    public BattleOutcome Check(BattleState state)
    {
        if (state.battleTime >= timeLimit)
        {
            return BattleOutcome.Defeat; // Time ran out
        }

        if (state.GetAliveEnemyUnits().Count == 0)
        {
            return BattleOutcome.Victory;
        }

        return BattleOutcome.None;
    }
}
```

---

## Extensible Objective System (Future)

### Abstract Objective Interface

```csharp
public interface IBattleObjective
{
    BattleOutcome CheckOutcome(BattleState state);
    string GetObjectiveText(); // For UI display
    float GetProgress(BattleState state); // For progress bar
}

public class DefeatAllEnemiesObjective : IBattleObjective
{
    public BattleOutcome CheckOutcome(BattleState state)
    {
        if (state.GetAliveEnemyUnits().Count == 0)
            return BattleOutcome.Victory;

        if (state.GetAlivePlayerUnits().Count == 0)
            return BattleOutcome.Defeat;

        return BattleOutcome.None;
    }

    public string GetObjectiveText()
    {
        return "Defeat all enemies";
    }

    public float GetProgress(BattleState state)
    {
        int totalEnemies = state.enemyUnits.Count;
        int deadEnemies = state.enemyUnits.Count(u => u.isDead);
        return (float)deadEnemies / totalEnemies;
    }
}
```

### WinConditionSystem with Custom Objectives

```csharp
public class WinConditionSystem
{
    private IBattleObjective primaryObjective;
    private List<IBattleObjective> secondaryObjectives;

    public void SetObjectives(IBattleObjective primary, List<IBattleObjective> secondary = null)
    {
        primaryObjective = primary;
        secondaryObjectives = secondary ?? new List<IBattleObjective>();
    }

    public BattleOutcome CheckOutcome(BattleState state)
    {
        return primaryObjective.CheckOutcome(state);
    }

    public bool CheckSecondaryObjectives(BattleState state)
    {
        return secondaryObjectives.All(obj => obj.CheckOutcome(state) == BattleOutcome.Victory);
    }
}
```

---

## Draw/Stalemate (Future)

### Mutual Destruction

If all units die simultaneously:

```csharp
public BattleOutcome CheckOutcome(BattleState state)
{
    int alivePlayers = state.GetAlivePlayerUnits().Count;
    int aliveEnemies = state.GetAliveEnemyUnits().Count;

    if (alivePlayers == 0 && aliveEnemies == 0)
    {
        return BattleOutcome.Draw; // Both sides wiped out
    }

    if (aliveEnemies == 0)
        return BattleOutcome.Victory;

    if (alivePlayers == 0)
        return BattleOutcome.Defeat;

    return BattleOutcome.None;
}

public enum BattleOutcome
{
    None,
    Victory,
    Defeat,
    Draw       // Future
}
```

---

## Objective UI Display (Presentation)

### Show Objective Text

```csharp
// In UI
void Start()
{
    string objectiveText = winConditionSystem.GetObjectiveText();
    objectiveLabel.text = objectiveText; // "Defeat all enemies"
}
```

### Show Progress

```csharp
void Update()
{
    float progress = winConditionSystem.GetProgress(battleState);
    progressBar.fillAmount = progress; // 0.0 to 1.0

    string progressText = $"{(int)(progress * 100)}%";
    progressLabel.text = progressText;
}
```

---

## Testing

### Unit Tests

```csharp
[Test]
public void CheckOutcome_Victory_WhenAllEnemiesDead()
{
    var player1 = CreateTestUnit(UnitTeam.Player, alive: true);
    var enemy1 = CreateTestUnit(UnitTeam.Enemy, alive: false);
    var enemy2 = CreateTestUnit(UnitTeam.Enemy, alive: false);

    var state = CreateBattleState(new[] { player1 }, new[] { enemy1, enemy2 });

    var outcome = winConditionSystem.CheckOutcome(state);

    Assert.AreEqual(BattleOutcome.Victory, outcome);
}

[Test]
public void CheckOutcome_Defeat_WhenAllPlayersDead()
{
    var player1 = CreateTestUnit(UnitTeam.Player, alive: false);
    var player2 = CreateTestUnit(UnitTeam.Player, alive: false);
    var enemy1 = CreateTestUnit(UnitTeam.Enemy, alive: true);

    var state = CreateBattleState(new[] { player1, player2 }, new[] { enemy1 });

    var outcome = winConditionSystem.CheckOutcome(state);

    Assert.AreEqual(BattleOutcome.Defeat, outcome);
}

[Test]
public void CheckOutcome_None_WhenBothSidesAlive()
{
    var player1 = CreateTestUnit(UnitTeam.Player, alive: true);
    var enemy1 = CreateTestUnit(UnitTeam.Enemy, alive: true);

    var state = CreateBattleState(new[] { player1 }, new[] { enemy1 });

    var outcome = winConditionSystem.CheckOutcome(state);

    Assert.AreEqual(BattleOutcome.None, outcome);
}

[Test]
public void CheckOutcome_Draw_WhenBothSidesDead()
{
    var player1 = CreateTestUnit(UnitTeam.Player, alive: false);
    var enemy1 = CreateTestUnit(UnitTeam.Enemy, alive: false);

    var state = CreateBattleState(new[] { player1 }, new[] { enemy1 });

    var outcome = winConditionSystem.CheckOutcome(state);

    Assert.AreEqual(BattleOutcome.Draw, outcome);
}
```

---

## Summary

The Win Condition System determines **when battle ends and who won**:

- ✅ Checks for victory (all enemies dead)
- ✅ Checks for defeat (all players dead)
- ✅ Returns battle outcome
- ✅ **MVP: Simple "defeat all enemies"**
- ✅ Future: Custom mission objectives (survive, protect, boss, time limit)
- ✅ Extensible via objective interface

**Win Condition is simple for MVP, expandable for future missions.**

---

## Related Documentation

- **DATA_MODELS.md** - BattleOutcome enum
- **SYSTEM_BATTLE_STATE.md** - BattleState stores outcome
- **MVP_SCOPE.md** - Only "defeat all enemies" for MVP

---

## Version

**Version 1.0** - Initial win condition system for MVP
