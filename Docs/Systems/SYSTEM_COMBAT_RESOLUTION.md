# SYSTEM_COMBAT_RESOLUTION.md

## Purpose

This document defines the **Combat Resolution System**, which executes attacks, techniques, damage calculations, healing, and death handling. This system is where combat mechanics actually happen.

---

## Responsibilities

The Combat Resolution System is responsible for:

- Resolving basic attacks (unit attacks unit)
- Resolving technique execution (skills dealing damage/healing/effects)
- Calculating damage (attack - defense, with modifiers)
- Calculating healing
- Applying damage to units
- Handling unit death
- Managing combat context for extensibility
- Applying modifiers from buffs, items, passives (future)

---

## What Combat Resolution System Does NOT Do

- **Does not decide who to attack** - That's TargetingSystem
- **Does not decide when to attack** - That's BehaviorSystem
- **Does not resolve action-chains** - That's SkillSystem
- **Does not handle visuals** - That's presentation layer

**Combat Resolution executes "deal damage", not "decide target".**

---

## Core Data Model

### CombatContext

```csharp
public class CombatContext
{
    public UnitRuntime attacker;
    public UnitRuntime defender;
    public ResolvedTechnique technique; // null if basic attack

    public int baseDamage;
    public int finalDamage;            // After modifiers
    public List<Modifier> appliedModifiers;

    // Future
    public bool isCritical;
    public bool isEvaded;
    public bool isBlocked;
}
```

**Purpose:** Tracks an active attack/technique resolution, allowing modifiers to hook in.

---

## Basic Attack Resolution

### Simple Damage Formula (MVP)

```csharp
public void ResolveBasicAttack(UnitRuntime attacker, UnitRuntime defender)
{
    // Create combat context
    var context = new CombatContext
    {
        attacker = attacker,
        defender = defender,
        technique = null, // Basic attack
        baseDamage = attacker.currentStats.attack - defender.currentStats.defense,
        appliedModifiers = new List<Modifier>()
    };

    // Ensure minimum damage
    if (context.baseDamage < 1)
        context.baseDamage = 1;

    // Apply modifiers (future items, passives, buffs)
    ApplyModifiers(context);

    // Calculate final damage
    context.finalDamage = CalculateFinalDamage(context);

    // Apply damage
    DealDamage(defender, context.finalDamage);

    // Check for death
    CheckDeath(defender);

    // Fire event for presentation
    OnDamageDealt?.Invoke(attacker, defender, context.finalDamage);
}
```

### Minimum Damage

Attacks should always deal at least 1 damage (unless evaded/blocked in future).

```csharp
if (context.baseDamage < 1)
    context.baseDamage = 1;
```

---

## Technique Resolution

### Resolving a Skill

```csharp
public void ResolveTechnique(UnitRuntime caster, ResolvedTechnique technique, UnitRuntime target)
{
    switch (technique.type)
    {
        case TechniqueType.Attack:
            ResolveDamageTechnique(caster, technique, target);
            break;

        case TechniqueType.Heal:
            ResolveHealTechnique(caster, technique, target);
            break;

        case TechniqueType.Buff:
            ResolveBuffTechnique(caster, technique, target);
            break;

        case TechniqueType.Debuff:
            ResolveDebuffTechnique(caster, technique, target);
            break;

        default:
            Debug.LogWarning($"Unknown technique type: {technique.type}");
            break;
    }
}
```

### Damage Technique

```csharp
private void ResolveDamageTechnique(UnitRuntime caster, ResolvedTechnique technique, UnitRuntime target)
{
    // Create combat context
    var context = new CombatContext
    {
        attacker = caster,
        defender = target,
        technique = technique,
        baseDamage = technique.power, // Use technique power
        appliedModifiers = new List<Modifier>()
    };

    // Apply modifiers
    ApplyModifiers(context);

    // Calculate final damage
    context.finalDamage = CalculateFinalDamage(context);

    // Apply damage
    DealDamage(target, context.finalDamage);

    // Check for death
    CheckDeath(target);

    // Fire event
    OnTechniqueUsed?.Invoke(caster, target, technique, context.finalDamage);
}
```

### Heal Technique

```csharp
private void ResolveHealTechnique(UnitRuntime caster, ResolvedTechnique technique, UnitRuntime target)
{
    int healAmount = technique.power;

    // Apply modifiers (future: healing bonuses from items/passives)
    // ...

    // Apply healing
    ApplyHealing(target, healAmount);

    // Fire event
    OnHealingApplied?.Invoke(caster, target, healAmount);
}
```

---

## Damage Calculation

### MVP: Simple Subtraction

```csharp
public int CalculateFinalDamage(CombatContext context)
{
    int damage = context.baseDamage;

    // Apply all modifiers
    foreach (var modifier in context.appliedModifiers)
    {
        damage = modifier.Apply(damage);
    }

    // Ensure minimum damage
    if (damage < 1)
        damage = 1;

    return damage;
}
```

### Future: Advanced Calculation

```csharp
public int CalculateFinalDamage(CombatContext context)
{
    int damage = context.baseDamage;

    // 1. Apply additive modifiers
    foreach (var modifier in context.appliedModifiers.Where(m => m.type == ModifierType.Additive))
    {
        damage += modifier.value;
    }

    // 2. Apply multiplicative modifiers
    foreach (var modifier in context.appliedModifiers.Where(m => m.type == ModifierType.Multiplicative))
    {
        damage = (int)(damage * modifier.value);
    }

    // 3. Apply defense reduction
    damage -= context.defender.currentStats.defense;

    // 4. Check for critical
    if (context.isCritical)
    {
        damage = (int)(damage * 2.0f);
    }

    // 5. Ensure minimum
    if (damage < 1)
        damage = 1;

    return damage;
}
```

---

## Applying Damage

```csharp
private void DealDamage(UnitRuntime target, int damage)
{
    target.currentHP -= damage;

    if (target.currentHP < 0)
        target.currentHP = 0;
}
```

### Applying Healing

```csharp
private void ApplyHealing(UnitRuntime target, int healAmount)
{
    target.currentHP += healAmount;

    // Cap at max HP
    if (target.currentHP > target.maxHP)
        target.currentHP = target.maxHP;
}
```

---

## Death Handling

```csharp
private void CheckDeath(UnitRuntime unit)
{
    if (unit.currentHP <= 0 && !unit.isDead)
    {
        unit.isDead = true;
        unit.currentHP = 0;

        // Clear grid occupancy
        grid.ClearOccupied(unit.position);

        // Fire death event
        OnUnitDied?.Invoke(unit);
    }
}
```

### Death Effects (Future)

Later, death can trigger effects:
- Drop items
- Trigger passive abilities
- AOE explosion
- Grant buffs to allies

---

## Modifier System (Future Extensibility)

### Why Modifiers Matter

The modifier system allows **items, passives, buffs, and skill trees** to hook into damage calculation **without hardcoding**.

### Modifier Structure

```csharp
public class Modifier
{
    public ModifierSource source;      // Item, Passive, Buff, etc.
    public ModifierType type;          // Additive, Multiplicative
    public float value;

    public int Apply(int baseValue)
    {
        switch (type)
        {
            case ModifierType.Additive:
                return baseValue + (int)value;

            case ModifierType.Multiplicative:
                return (int)(baseValue * value);

            case ModifierType.Override:
                return (int)value;

            default:
                return baseValue;
        }
    }
}

public enum ModifierType
{
    Additive,        // +10 damage
    Multiplicative,  // *1.2 damage
    Override         // Set to exact value
}

public enum ModifierSource
{
    BaseStats,
    StatusEffect,
    Item,
    Passive,
    SkillTree,
    Behavior
}
```

### Applying Modifiers

```csharp
private void ApplyModifiers(CombatContext context)
{
    // 1. Apply status effect modifiers
    ApplyStatusEffectModifiers(context);

    // 2. Apply item modifiers (future)
    // ApplyItemModifiers(context);

    // 3. Apply passive modifiers (future)
    // ApplyPassiveModifiers(context);
}

private void ApplyStatusEffectModifiers(CombatContext context)
{
    // Check attacker's buffs
    foreach (var effect in context.attacker.activeEffects)
    {
        if (effect.definition.statModifier != null)
        {
            var modifier = new Modifier
            {
                source = ModifierSource.StatusEffect,
                type = ModifierType.Multiplicative,
                value = 1.0f + effect.definition.statModifier.attackBonus
            };

            context.appliedModifiers.Add(modifier);
        }
    }

    // Check defender's debuffs
    foreach (var effect in context.defender.activeEffects)
    {
        if (effect.definition.statModifier != null)
        {
            var modifier = new Modifier
            {
                source = ModifierSource.StatusEffect,
                type = ModifierType.Additive,
                value = -effect.definition.statModifier.defenseBonus
            };

            context.appliedModifiers.Add(modifier);
        }
    }
}
```

### Future: Item Modifiers

```csharp
private void ApplyItemModifiers(CombatContext context)
{
    foreach (var item in context.attacker.equippedItems)
    {
        var modifier = item.GetDamageModifier(context);
        if (modifier != null)
        {
            context.appliedModifiers.Add(modifier);
        }
    }
}
```

**Example Item:**
```csharp
// "Fire Sword" ScriptableObject
public Modifier GetDamageModifier(CombatContext context)
{
    // +20 damage if technique is Fire element
    if (context.technique != null && context.technique.element == ElementType.Fire)
    {
        return new Modifier
        {
            source = ModifierSource.Item,
            type = ModifierType.Additive,
            value = 20
        };
    }

    return null;
}
```

**This allows items to be data-driven, not hardcoded.**

---

## Critical Hits (Future)

```csharp
private bool RollCritical(UnitRuntime attacker)
{
    float critChance = attacker.currentStats.critChance; // Future stat
    return Random.value < critChance;
}

// In combat resolution
if (RollCritical(attacker))
{
    context.isCritical = true;
    context.finalDamage *= 2; // Double damage
}
```

---

## Evasion/Miss (Future)

```csharp
private bool RollEvasion(UnitRuntime defender)
{
    float evasionChance = defender.currentStats.evasion; // Future stat
    return Random.value < evasionChance;
}

// In combat resolution
if (RollEvasion(defender))
{
    context.isEvaded = true;
    context.finalDamage = 0; // No damage
}
```

---

## AOE Attacks (Future)

### Multi-Target Damage

```csharp
public void ResolveAOETechnique(UnitRuntime caster, ResolvedTechnique technique, List<UnitRuntime> targets)
{
    foreach (var target in targets)
    {
        ResolveDamageTechnique(caster, technique, target);
    }
}
```

### AOE with Falloff

```csharp
public void ResolveAOETechnique(UnitRuntime caster, ResolvedTechnique technique, GridPosition center, int radius)
{
    var targets = GetUnitsInRadius(center, radius);

    foreach (var target in targets)
    {
        float distance = CalculateDistance(center, target.position);
        float damageFalloff = 1.0f - (distance / radius) * 0.5f; // 50% reduction at edge

        int adjustedPower = (int)(technique.power * damageFalloff);

        var modifiedTechnique = technique;
        modifiedTechnique.power = adjustedPower;

        ResolveDamageTechnique(caster, modifiedTechnique, target);
    }
}
```

---

## Integration with Other Systems

### With BehaviorSystem

```csharp
// BehaviorSystem generates attack intent
var intent = new UnitIntent
{
    type = IntentType.BasicAttack,
    actor = unit,
    target = target
};

// BattleManager executes
if (intent.type == IntentType.BasicAttack)
{
    combatResolutionSystem.ResolveBasicAttack(intent.actor, intent.target);
}
```

### With SkillSystem

```csharp
// SkillSystem resolves action-chain
var technique = skillSystem.ResolveSequence(caster, skill);

// Combat system executes technique
combatResolutionSystem.ResolveTechnique(caster, technique, target);
```

### With StatusEffectSystem

```csharp
// Status effects modify combat context
ApplyStatusEffectModifiers(context);
```

---

## Events for Presentation Layer

### Fire Events, Don't Control Visuals

```csharp
public event Action<UnitRuntime, UnitRuntime, int> OnDamageDealt;
public event Action<UnitRuntime, UnitRuntime, ResolvedTechnique, int> OnTechniqueUsed;
public event Action<UnitRuntime, UnitRuntime, int> OnHealingApplied;
public event Action<UnitRuntime> OnUnitDied;

// Presentation layer subscribes
combatSystem.OnDamageDealt += (attacker, defender, damage) =>
{
    // Show damage number
    // Play hit animation
    // Spawn VFX
};

combatSystem.OnUnitDied += (unit) =>
{
    // Play death animation
    // Remove visual
};
```

---

## Testing

### Unit Tests

```csharp
[Test]
public void ResolveBasicAttack_DealsDamage()
{
    var attacker = CreateTestUnit();
    attacker.currentStats.attack = 20;

    var defender = CreateTestUnit();
    defender.currentStats.defense = 8;
    defender.currentHP = 100;

    combatSystem.ResolveBasicAttack(attacker, defender);

    Assert.AreEqual(88, defender.currentHP); // 100 - (20 - 8) = 88
}

[Test]
public void ResolveBasicAttack_MinimumOneDamage()
{
    var attacker = CreateTestUnit();
    attacker.currentStats.attack = 5;

    var defender = CreateTestUnit();
    defender.currentStats.defense = 10; // Defense > Attack
    defender.currentHP = 100;

    combatSystem.ResolveBasicAttack(attacker, defender);

    Assert.AreEqual(99, defender.currentHP); // Minimum 1 damage
}

[Test]
public void ResolveTechnique_DealsTechniquePower()
{
    var caster = CreateTestUnit();
    var target = CreateTestUnit();
    target.currentHP = 100;

    var technique = new ResolvedTechnique
    {
        type = TechniqueType.Attack,
        power = 50
    };

    combatSystem.ResolveTechnique(caster, technique, target);

    Assert.AreEqual(50, target.currentHP); // 100 - 50 = 50
}

[Test]
public void CheckDeath_KillsUnit()
{
    var unit = CreateTestUnit();
    unit.currentHP = 10;

    combatSystem.DealDamage(unit, 15);
    combatSystem.CheckDeath(unit);

    Assert.IsTrue(unit.isDead);
    Assert.AreEqual(0, unit.currentHP);
}

[Test]
public void ApplyHealing_RestoresHP()
{
    var unit = CreateTestUnit();
    unit.currentHP = 50;
    unit.maxHP = 100;

    combatSystem.ApplyHealing(unit, 30);

    Assert.AreEqual(80, unit.currentHP);
}

[Test]
public void ApplyHealing_CapsAtMaxHP()
{
    var unit = CreateTestUnit();
    unit.currentHP = 90;
    unit.maxHP = 100;

    combatSystem.ApplyHealing(unit, 50); // Overheal

    Assert.AreEqual(100, unit.currentHP); // Capped
}
```

---

## Performance Considerations

### For MVP

- Simple calculations
- No complex modifiers yet
- No performance concerns

### For Later

- Cache modifier calculations
- Batch damage events
- Optimize complex formulas

---

## Summary

The Combat Resolution System **executes combat mechanics**:

- ✅ Resolves basic attacks (attack - defense)
- ✅ Resolves techniques (skill damage/healing)
- ✅ Applies damage and healing to units
- ✅ Handles unit death
- ✅ Uses CombatContext for extensibility
- ✅ Supports future modifiers (items, passives, buffs)
- ✅ Fires events for presentation layer

**Combat Resolution executes "deal damage". Other systems decide "who" and "when".**

---

## Related Documentation

- **DATA_MODELS.md** - CombatContext, Modifier, ResolvedTechnique
- **SYSTEM_SKILLS.md** - Technique resolution
- **SYSTEM_BEHAVIOR.md** - When to attack
- **SYSTEM_TARGETING.md** - Who to attack
- **SYSTEM_STATUS_EFFECTS.md** - Buffs/debuffs modify damage

---

## Version

**Version 1.0** - Initial combat resolution system for MVP
