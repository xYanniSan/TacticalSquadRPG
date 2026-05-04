# ENGINEERING_RULES.md

## Purpose

This document defines **coding standards, architectural best practices, and development rules** for the project. These rules ensure maintainability, extensibility, and consistency across 3-4 years of development.

---

## Core Principles

1. **Clean separation of responsibilities** - Each class/system has one clear job
2. **Avoid god classes** - No single class should own everything
3. **Data-driven over hardcoded** - Prefer configuration data over code switches
4. **Extensible by default** - Build for future items, passives, modifiers
5. **Gameplay truth in code, not visuals** - Logic determines outcomes, presentation follows
6. **Static data stays static** - Never modify ScriptableObjects at runtime
7. **Contained refactoring over brittle optimization** - Clarity first, optimize when needed

---

## 1. Separation of Responsibilities

### Rule: One Class, One Job

Each class should have a **single, clear responsibility**.

❌ **Bad:**
```csharp
public class BattleManager : MonoBehaviour
{
    void Update()
    {
        CheckWinConditions();
        MoveAllUnits();
        ResolveAllCombat();
        UpdateUI();
        PlaySounds();
        // Too many responsibilities
    }
}
```

✅ **Good:**
```csharp
public class BattleManager : MonoBehaviour
{
    private WinConditionSystem winSystem;
    private MovementSystem movementSystem;
    private CombatResolutionSystem combatSystem;

    void Update()
    {
        // Coordinate systems, don't do everything
        winSystem.CheckConditions(battleState);
        movementSystem.UpdateMovement(battleState, Time.deltaTime);
        combatSystem.ProcessCombat(battleState);
    }
}
```

---

### Rule: Separate Static Data, Runtime State, Logic, and Presentation

**Static Data** (ScriptableObjects):
- Hero base stats
- Action definitions
- Skill templates
- Never modified at runtime

**Runtime State** (Plain C# classes):
- Current HP, position, active buffs
- Created per battle instance
- Destroyed when battle ends

**Systems (Logic)**:
- Combat rules
- Behavior decisions
- Targeting logic
- No Unity MonoBehaviour unless needed

**Presentation** (MonoBehaviour, UI):
- Visual feedback
- Animations
- VFX
- Sound
- Reacts to logic, doesn't drive it

❌ **Bad:**
```csharp
// Storing runtime state in ScriptableObject
[CreateAssetMenu]
public class HeroData : ScriptableObject
{
    public int currentHP; // WRONG! ScriptableObjects are shared assets
}
```

✅ **Good:**
```csharp
// Static definition
[CreateAssetMenu]
public class UnitDefinition : ScriptableObject
{
    public int maxHP; // Static template data
}

// Runtime instance
public class UnitRuntime
{
    public UnitDefinition definition;
    public int currentHP; // Runtime state
}
```

---

## 2. Avoid God Classes

### Rule: No Single Class Should Own Everything

A "god class" is a class that knows too much or does too much.

**Signs of a god class:**
- 1000+ lines
- Handles movement, combat, UI, progression, inventory
- Other classes can't function without it
- Hard to test

❌ **Bad:**
```csharp
public class GameManager : MonoBehaviour
{
    public void HandleEverything()
    {
        // Movement
        // Combat
        // UI
        // Saving
        // Audio
        // Everything
    }
}
```

✅ **Good:**
```csharp
// Multiple focused managers/systems
public class BattleManager { } // Coordinates battle flow
public class GridSystem { }     // Manages grid
public class SaveManager { }    // Handles persistence
public class AudioManager { }   // Plays sounds
```

---

### Rule: Battle Manager Is a Coordinator, Not an Owner

The `BattleManager` should **orchestrate** systems, not contain all combat logic.

**Battle Manager should:**
- Initialize battle state
- Run battle loop
- Coordinate system execution
- Check win/loss
- Transition phases

**Battle Manager should NOT:**
- Calculate damage formulas
- Resolve targeting logic
- Execute movement
- Resolve skill combos
- Hardcode item bonuses

❌ **Bad:**
```csharp
public class BattleManager
{
    void ResolveAttack(Unit attacker, Unit defender)
    {
        int damage = attacker.attack - defender.defense;

        // Hardcoded item bonuses
        if (attacker.hasFireSword)
            damage += 10;
        if (attacker.hasCritRing && Random.value < 0.2f)
            damage *= 2;

        // Hardcoded hero exceptions
        if (attacker.name == "Kai")
            damage += 5;

        defender.currentHP -= damage;
    }
}
```

✅ **Good:**
```csharp
public class BattleManager
{
    private CombatResolutionSystem combatSystem;

    void ResolveAttack(UnitRuntime attacker, UnitRuntime defender)
    {
        // Delegate to combat system
        combatSystem.ResolveBasicAttack(attacker, defender);
    }
}

public class CombatResolutionSystem
{
    public void ResolveBasicAttack(UnitRuntime attacker, UnitRuntime defender)
    {
        var context = new CombatContext
        {
            attacker = attacker,
            defender = defender,
            baseDamage = attacker.currentStats.attack - defender.currentStats.defense
        };

        // Extensible modifier system
        ApplyModifiers(context);

        int finalDamage = CalculateFinalDamage(context);
        DealDamage(defender, finalDamage);
    }

    private void ApplyModifiers(CombatContext context)
    {
        // Items, passives, buffs can hook in here
        // No hardcoding
    }
}
```

---

## 3. No Hardcoding Hero-Specific or Item-Specific Logic

### Rule: Use Data-Driven Systems, Not Code Switches

New heroes, items, and abilities should be **addable as data**, not code changes.

❌ **Bad:**
```csharp
public int CalculateDamage(Unit attacker, Unit defender)
{
    int damage = attacker.attack - defender.defense;

    // Hardcoded hero exceptions
    if (attacker.heroId == "hero_kai")
        damage += 10;
    if (attacker.heroId == "hero_rin")
        damage = (int)(damage * 1.2f);

    // Hardcoded item exceptions
    if (attacker.hasItem("fire_sword"))
        damage += 15;

    return damage;
}
```

**Problems:**
- Every new hero/item requires code changes
- Hundreds of if-statements over time
- Impossible to maintain
- Can't be tested easily

✅ **Good:**
```csharp
public int CalculateDamage(CombatContext context)
{
    int damage = context.baseDamage;

    // Apply all modifiers generically
    foreach (var modifier in context.appliedModifiers)
    {
        damage = modifier.Apply(damage);
    }

    return damage;
}

// Items/passives/buffs contribute modifiers
public class ItemSystem
{
    public void ApplyItemModifiers(CombatContext context)
    {
        foreach (var item in context.attacker.equippedItems)
        {
            var modifier = item.GetModifier(context);
            if (modifier != null)
                context.appliedModifiers.Add(modifier);
        }
    }
}
```

**Benefits:**
- New items are just data (ScriptableObjects)
- No code changes needed
- Easy to balance and test

---

### Rule: Use Proficiencies, Not Hard-Locks

Heroes should be **guided by proficiencies**, not hard-locked to roles.

❌ **Bad:**
```csharp
public bool CanUseSkill(Unit unit, Skill skill)
{
    // Hard-locked by hero ID
    if (skill.name == "Fire Breath" && unit.heroId != "hero_kai")
        return false;

    return true;
}
```

✅ **Good:**
```csharp
public float GetSkillEffectiveness(UnitRuntime unit, ResolvedTechnique technique)
{
    float effectiveness = 1.0f;

    // Apply proficiency bonuses
    if (technique.element != ElementType.None)
    {
        effectiveness *= unit.definition.proficiencies.GetProficiencyBonus(technique.element);
    }

    return effectiveness;
}
```

**Kai** might have:
- Fire proficiency: 1.3x
- Lightning proficiency: 0.8x

He's **better** at fire techniques, but can still use lightning if the player configures it.

---

## 4. Gameplay Truth Must Not Live in Animation Events

### Rule: Logic Determines Outcomes, Presentation Follows

**Gameplay truth** (damage, death, state changes) must happen in **code**, not animation callbacks.

❌ **Bad:**
```csharp
// Animation event callback
public void OnPunchAnimationHit()
{
    // Damage happens in animation!
    target.TakeDamage(attackPower);
}
```

**Problems:**
- Gameplay is tied to animation timing
- Can't test without animations
- Breaks if animation is replaced
- Can't simulate battles without visuals

✅ **Good:**
```csharp
// Combat system resolves damage
public void ResolveAttack(UnitRuntime attacker, UnitRuntime defender)
{
    int damage = CalculateDamage(attacker, defender);
    DealDamage(defender, damage);

    // THEN trigger visual feedback
    OnDamageDealt?.Invoke(defender, damage);
}

// Presentation reacts to event
void OnDamageDealtHandler(UnitRuntime unit, int damage)
{
    // Play hit animation
    // Spawn VFX
    // Update UI
}
```

**Benefits:**
- Combat can be simulated without visuals
- Easy to test
- Easy to replace/disable animations
- Clear separation of logic and presentation

---

### Rule: Animations Are Visual Feedback, Not Triggers

Animations should **react** to game state, not drive it.

✅ **Good flow:**
1. Combat system calculates damage
2. Combat system applies damage to UnitRuntime
3. Combat system fires event: `OnUnitAttacked`
4. Animation controller listens for event
5. Animation controller plays attack animation

---

## 5. Systems Should Communicate Through Clear Contracts

### Rule: Use Shared Data Models, Not Direct Dependencies

Systems should communicate through **shared data structures** (see DATA_MODELS.md), not by calling each other's internals.

❌ **Bad:**
```csharp
public class MovementSystem
{
    public void MoveUnit(Unit unit, GridPosition target)
    {
        // Direct dependency on TargetingSystem
        var targetingSystem = FindObjectOfType<TargetingSystem>();
        targetingSystem.InvalidateTarget(unit);

        // Move logic
    }
}
```

✅ **Good:**
```csharp
public class MovementSystem
{
    public void MoveUnit(UnitRuntime unit, GridPosition target)
    {
        // Update shared state
        unit.position = target;

        // Emit event if needed
        OnUnitMoved?.Invoke(unit, target);
    }
}

// TargetingSystem listens for event if it needs to react
void OnUnitMovedHandler(UnitRuntime unit, GridPosition newPos)
{
    // Revalidate target range
}
```

---

### Rule: Systems Expose Interfaces, Not Internals

Systems should have **clean public APIs**.

❌ **Bad:**
```csharp
public class GridSystem
{
    public GridTile[,] tiles; // Exposed internal state!

    // Anyone can modify tiles directly
}
```

✅ **Good:**
```csharp
public class GridSystem
{
    private GridTile[,] tiles; // Private

    // Controlled access through methods
    public GridTile GetTile(GridPosition pos);
    public bool IsWalkable(GridPosition pos);
    public void SetOccupied(GridPosition pos, UnitRuntime unit);
}
```

---

## 6. Runtime State Should Not Be Stored in ScriptableObjects

### Rule: ScriptableObjects Are Static Templates, Not Runtime Instances

ScriptableObjects are **shared assets**. If you modify them at runtime, **all instances share the change**.

❌ **Bad:**
```csharp
[CreateAssetMenu]
public class HeroData : ScriptableObject
{
    public int currentHP; // WRONG!
    public GridPosition position; // WRONG!
}

// In battle:
heroData.currentHP -= 10; // Modifies the asset file!
```

**Problems:**
- Changes persist across play sessions
- Multiple units can't have different HP
- Asset file gets corrupted

✅ **Good:**
```csharp
// ScriptableObject: Static definition
[CreateAssetMenu]
public class UnitDefinition : ScriptableObject
{
    public int maxHP; // Static template
    public int baseAttack;
}

// Plain class: Runtime instance
public class UnitRuntime
{
    public UnitDefinition definition; // Reference to template
    public int currentHP; // Runtime state
    public GridPosition position;
}
```

---

### Rule: Create Runtime Instances from Static Definitions

At battle start, create runtime instances from ScriptableObject templates.

✅ **Good:**
```csharp
public UnitRuntime CreateUnit(UnitDefinition definition, GridPosition startPos)
{
    return new UnitRuntime
    {
        definition = definition,
        currentHP = definition.baseStats.maxHP, // Copy static data
        maxHP = definition.baseStats.maxHP,
        position = startPos,
        // etc.
    };
}
```

---

## 7. Build Extensible Systems for Future Content

### Rule: Design for Items, Passives, Modifiers from Day 1

Even if MVP doesn't include items or passives, the **architecture** should support them.

✅ **Good:**
```csharp
// CombatContext allows future modifiers
public class CombatContext
{
    public UnitRuntime attacker;
    public UnitRuntime defender;
    public int baseDamage;
    public List<Modifier> appliedModifiers; // Future items/passives hook here
}

public int CalculateFinalDamage(CombatContext context)
{
    int damage = context.baseDamage;

    // Apply modifiers (none in MVP, but system is ready)
    foreach (var modifier in context.appliedModifiers)
    {
        damage = modifier.Apply(damage);
    }

    return damage;
}
```

**Later, when adding items:**
```csharp
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
```

No changes to `CalculateFinalDamage` needed!

---

### Rule: Use Extensible Enums

Use enums that can grow over time.

✅ **Good:**
```csharp
public enum ActionType
{
    Physical,
    Elemental,
    Support,
    Movement // Added later
}

public enum ElementType
{
    None,
    Fire,
    Water,
    Earth,
    Lightning,
    Wind,
    Ice,    // Added later
    Light,  // Added later
    Dark    // Added later
}
```

---

## 8. Prefer Contained Refactoring Over Brittle Early Optimization

### Rule: Optimize for Clarity First, Performance Later

For MVP:
- Prioritize **readable, correct code**
- Profile before optimizing
- 2v2 battles won't stress performance

❌ **Premature optimization:**
```csharp
// Overly complex caching for 4 units
private Dictionary<int, Dictionary<int, List<GridPosition>>> cachedPaths;
```

✅ **Good for MVP:**
```csharp
// Simple, clear, correct
public List<GridPosition> GetPath(GridPosition from, GridPosition to)
{
    // Simple A* or direct path
    return CalculatePath(from, to);
}
```

**When to optimize:**
- After profiling shows a bottleneck
- When scaling to larger battles
- Not before MVP validation

---

### Rule: Refactor in Contained Scopes

When refactoring:
- Change one system at a time
- Verify other systems still work
- Don't rewrite everything at once

✅ **Good refactoring:**
1. Refactor `MovementSystem` to use new pathfinding
2. Test that movement still works
3. Verify `BehaviorSystem` and `TargetingSystem` aren't broken
4. Move to next refactor

❌ **Bad refactoring:**
1. Rewrite `MovementSystem`, `GridSystem`, `TargetingSystem`, and `BehaviorSystem` all at once
2. Hope everything works
3. Debug for days

---

## 9. Code Organization

### Rule: Follow Consistent Folder Structure

See ARCHITECTURE_OVERVIEW.md for folder structure.

**Key points:**
- Static data in `Assets/Data/`
- Systems in `Assets/Scripts/Systems/`
- Data models in `Assets/Scripts/DataModels/`
- UI in `Assets/Scripts/UI/`

---

### Rule: Naming Conventions

**Classes:**
- PascalCase: `UnitDefinition`, `BattleManager`

**Methods:**
- PascalCase: `CalculateDamage()`, `MoveUnit()`

**Variables:**
- camelCase: `currentHP`, `targetPosition`

**Private fields:**
- camelCase with underscore: `_battleState` or camelCase: `battleState` (pick one, be consistent)

**Constants:**
- PascalCase or UPPER_CASE: `MaxUnits` or `MAX_UNITS`

**ScriptableObject menu names:**
- `[CreateAssetMenu(menuName = "Game/Units/Hero")]`

---

### Rule: Comments

**When to comment:**
- Complex algorithms
- Non-obvious design decisions
- TODOs for future work

**When NOT to comment:**
```csharp
// Bad: Obvious comment
// Increment HP by 10
currentHP += 10;
```

✅ **Good:**
```csharp
// Apply proficiency bonus to technique power
// Proficiency values are defined in UnitDefinition.proficiencies
float bonus = caster.definition.proficiencies.GetProficiencyBonus(technique.element);
int finalPower = (int)(technique.basePower * bonus);
```

---

## 10. Testing Philosophy

### Rule: Write Testable Code

Code should be testable without Unity runtime.

✅ **Testable:**
```csharp
public class TargetingSystem
{
    // Pure logic, no Unity dependencies
    public UnitRuntime SelectTarget(UnitRuntime actor, List<UnitRuntime> enemies)
    {
        // Logic here
    }
}

// Easy to test
[Test]
public void SelectTarget_ReturnsNearestEnemy()
{
    var actor = CreateTestUnit(new GridPosition(0, 0));
    var enemy1 = CreateTestUnit(new GridPosition(5, 5));
    var enemy2 = CreateTestUnit(new GridPosition(2, 2));

    var targeting = new TargetingSystem();
    var target = targeting.SelectTarget(actor, new List<UnitRuntime> { enemy1, enemy2 });

    Assert.AreEqual(enemy2, target);
}
```

---

### Rule: Test System Interfaces, Not Internals

Test **what** a system does, not **how** it does it.

✅ **Good:**
```csharp
[Test]
public void CombatSystem_DealsDamageCorrectly()
{
    var attacker = CreateUnitWithAttack(10);
    var defender = CreateUnitWithDefense(5);

    combatSystem.ResolveBasicAttack(attacker, defender);

    Assert.AreEqual(95, defender.currentHP); // 100 - (10 - 5) = 95
}
```

❌ **Bad:**
```csharp
[Test]
public void CombatSystem_UsesCorrectPrivateMethod()
{
    // Testing implementation details, not behavior
}
```

---

## 11. Version Control

### Rule: Commit Often, Commit Meaningfully

**Good commit messages:**
- `Add basic TargetingSystem`
- `Fix bug where dead units could be targeted`
- `Implement action-chain resolution for SkillSystem`

**Bad commit messages:**
- `Update`
- `Fixed stuff`
- `WIP`

---

### Rule: Don't Commit Generated or Binary Files

Add to `.gitignore`:
- `Library/`
- `Temp/`
- `Obj/`
- `*.meta` (if not needed)

---

## 12. Documentation

### Rule: Keep Design Docs Updated

When making architectural changes:
1. Update relevant `.md` file
2. Ensure AI agent has latest context
3. Document breaking changes

---

### Rule: Code Should Be Self-Documenting Where Possible

Use clear names over comments.

❌ **Bad:**
```csharp
// Calculate d
int d = a - b;
```

✅ **Good:**
```csharp
int damage = attackPower - defense;
```

---

## 13. Performance Guidelines (For Later)

### For MVP: Don't Worry About Performance

Focus on correctness and clarity.

### For Later Optimization:

**Object Pooling:**
- Pool units, projectiles, VFX

**Spatial Partitioning:**
- Use grids or quadtrees for large battlefields

**Batch Processing:**
- Process status effects in batches
- Update all units in one loop

**LOD:**
- Reduce visual complexity for distant units

---

## 14. Unity-Specific Rules

### Rule: Minimize MonoBehaviour Use

Only use `MonoBehaviour` when you need Unity lifecycle hooks.

**Needs MonoBehaviour:**
- BattleManager (Update loop)
- Unit visuals (Transform, Animator)
- UI elements

**Doesn't need MonoBehaviour:**
- GridSystem (pure logic)
- TargetingSystem (pure logic)
- CombatResolutionSystem (pure logic)

✅ **Good:**
```csharp
// Plain C# class
public class GridSystem
{
    private GridMap grid;

    public GridSystem(int width, int height)
    {
        grid = new GridMap(width, height);
    }
}
```

---

### Rule: Use ScriptableObjects for Static Data Only

ScriptableObjects are perfect for:
- Hero definitions
- Action definitions
- Status effect definitions

ScriptableObjects are NOT for:
- Runtime state
- Save data (use JSON/binary serialization)

---

## 15. AI Agent Guidelines

### Rule: Provide Clear Context

When asking AI to generate code:
- Reference relevant `.md` docs
- Specify which system you're working on
- Provide existing code for context

### Rule: Validate AI Output

AI-generated code should:
- Follow these engineering rules
- Match architectural patterns
- Be tested before committing

---

## Summary of Key Rules

1. ✅ **One class, one responsibility**
2. ✅ **No god classes**
3. ✅ **Data-driven, not hardcoded**
4. ✅ **Gameplay truth in code, not animations**
5. ✅ **ScriptableObjects are static, never runtime**
6. ✅ **Build extensible systems (items, passives, modifiers)**
7. ✅ **Systems communicate through data, not dependencies**
8. ✅ **Clarity first, optimize later**
9. ✅ **Write testable code**
10. ✅ **Keep documentation updated**

---

## Common Mistakes to Avoid

❌ Storing runtime state in ScriptableObjects
❌ Hardcoding hero/item-specific logic
❌ Putting gameplay logic in animation events
❌ Creating god classes
❌ Tight coupling between systems
❌ Premature optimization
❌ Ignoring separation of concerns

---

## Checklist for New Features

Before implementing a new feature, ask:

- [ ] Does this fit into an existing system, or need a new one?
- [ ] Am I following separation of static/runtime data?
- [ ] Am I hardcoding hero/item-specific logic? (If yes, refactor)
- [ ] Is this testable without Unity runtime?
- [ ] Does this communicate through shared data models?
- [ ] Is this extensible for future content?
- [ ] Have I updated relevant documentation?

---

## Version

**Version 1.0** - Initial engineering rules for MVP
