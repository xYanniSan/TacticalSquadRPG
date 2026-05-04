# SYSTEM_STATUS_EFFECTS.md

## Purpose

This document defines the **Status Effect System**, which manages buffs, debuffs, and temporary modifiers on units during battle. Status effects add tactical depth by allowing temporary stat changes, damage over time, and special conditions.

---

## MVP Scope

**Status effects are OPTIONAL for MVP.** They can be added after core combat is validated.

**Recommended MVP approach:**
- Skip status effects entirely, OR
- Implement 1-2 simple effects as proof-of-concept (e.g., "Attack Buff")

**This document describes the full system for future implementation.**

---

## Responsibilities

The Status Effect System is responsible for:

- Applying status effects to units
- Tracking effect duration (turns/ticks)
- Ticking effects each turn (applying damage/healing, decrementing duration)
- Removing expired effects
- Applying stat modifiers from effects
- Handling effect stacking
- Supporting future complex effects

---

## What Status Effect System Does NOT Do

- **Does not decide when to apply effects** - That's CombatResolutionSystem or SkillSystem
- **Does not calculate damage directly** - That's CombatResolutionSystem
- **Does not handle visuals** - That's presentation layer

**Status Effect System manages effect lifecycle, not application logic.**

---

## Core Data Models

See DATA_MODELS.md for full definitions.

### StatusEffect (Runtime)

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

### StatusEffectDefinition (Static)

```csharp
[CreateAssetMenu(menuName = "Game/StatusEffects/Effect")]
public class StatusEffectDefinition : ScriptableObject
{
    [Header("Identity")]
    public string effectId;
    public string displayName;         // "Attack Buff", "Poison"
    public Sprite icon;
    public EffectType type;            // Buff, Debuff, Neutral

    [Header("Duration")]
    public int baseDuration;           // Ticks/turns
    public bool isStackable;
    public int maxStacks;

    [Header("Stat Modifiers")]
    public StatModifier statModifier;

    // Future: DOT, HOT, special behaviors
    // public int damagePerTick;
    // public EffectBehavior behavior;
}
```

### EffectType Enum

```csharp
public enum EffectType
{
    Buff,          // Beneficial
    Debuff,        // Harmful
    Neutral        // Neither (e.g., mark)
}
```

---

## Effect Types

### Buffs (Beneficial)

- **Attack Buff** - Increase attack stat
- **Defense Buff** - Increase defense stat
- **Speed Buff** - Increase movement speed
- **Regeneration** - Heal over time

### Debuffs (Harmful)

- **Attack Debuff** - Decrease attack stat
- **Defense Debuff** - Decrease defense stat
- **Poison** - Damage over time
- **Slow** - Decrease movement speed
- **Stun** - Cannot act (future)

### Neutral

- **Mark** - Target marked for bonus damage (future)
- **Shield** - Absorbs damage (future)

---

## Stat Modifiers

### StatModifier Structure

```csharp
public class StatModifier
{
    public StatType affectedStat;
    public ModifierType modifierType;  // Additive or Multiplicative
    public float value;
}

public enum StatType
{
    Attack,
    Defense,
    MaxHP,
    MoveSpeed,
    // Future: MagicPower, Resistance, etc.
}
```

### Example: Attack Buff

```csharp
// "Attack Buff" ScriptableObject
effectId = "buff_attack";
displayName = "Attack Buff";
type = EffectType.Buff;
baseDuration = 3; // Lasts 3 turns
statModifier = new StatModifier
{
    affectedStat = StatType.Attack,
    modifierType = ModifierType.Additive,
    value = 10 // +10 attack
};
```

---

## Effect Lifecycle

### 1. Application

```csharp
public void ApplyEffect(UnitRuntime target, StatusEffectDefinition effectDef)
{
    // Check if effect already exists
    var existing = target.activeEffects.FirstOrDefault(e => e.definition == effectDef);

    if (existing != null && effectDef.isStackable)
    {
        // Stack effect
        if (existing.stackCount < effectDef.maxStacks)
        {
            existing.stackCount++;
            existing.remainingDuration = effectDef.baseDuration; // Refresh duration
        }
    }
    else if (existing != null)
    {
        // Non-stackable, refresh duration
        existing.remainingDuration = effectDef.baseDuration;
    }
    else
    {
        // New effect
        var effect = new StatusEffect
        {
            definition = effectDef,
            target = target,
            remainingDuration = effectDef.baseDuration,
            stackCount = 1
        };

        target.activeEffects.Add(effect);
        effect.OnApply();
    }
}
```

### 2. OnApply (Immediate Effect)

```csharp
public void OnApply()
{
    // Apply stat modifier
    if (definition.statModifier != null)
    {
        ApplyStatModifier(target, definition.statModifier, stackCount);
    }

    // Fire event
    OnEffectApplied?.Invoke(target, this);
}

private void ApplyStatModifier(UnitRuntime unit, StatModifier modifier, int stacks)
{
    int totalValue = (int)(modifier.value * stacks);

    switch (modifier.affectedStat)
    {
        case StatType.Attack:
            unit.currentStats.attack += totalValue;
            break;

        case StatType.Defense:
            unit.currentStats.defense += totalValue;
            break;

        // Add more stats as needed
    }
}
```

### 3. OnTick (Per-Turn Effect)

```csharp
public void OnTick()
{
    // Apply damage/healing over time (DOT/HOT)
    if (definition.damagePerTick != 0)
    {
        int damage = definition.damagePerTick * stackCount;

        if (damage > 0)
        {
            // Deal damage
            target.currentHP -= damage;
            OnDOTDamage?.Invoke(target, damage);
        }
        else
        {
            // Heal
            int healing = -damage;
            target.currentHP += healing;
            if (target.currentHP > target.maxHP)
                target.currentHP = target.maxHP;

            OnHOTHealing?.Invoke(target, healing);
        }
    }

    // Decrement duration
    remainingDuration--;
}
```

### 4. OnRemove (Cleanup)

```csharp
public void OnRemove()
{
    // Remove stat modifier
    if (definition.statModifier != null)
    {
        RemoveStatModifier(target, definition.statModifier, stackCount);
    }

    // Fire event
    OnEffectRemoved?.Invoke(target, this);
}

private void RemoveStatModifier(UnitRuntime unit, StatModifier modifier, int stacks)
{
    int totalValue = (int)(modifier.value * stacks);

    switch (modifier.affectedStat)
    {
        case StatType.Attack:
            unit.currentStats.attack -= totalValue;
            break;

        case StatType.Defense:
            unit.currentStats.defense -= totalValue;
            break;

        // Add more stats as needed
    }
}
```

---

## System Methods

### Tick All Effects

```csharp
public class StatusEffectSystem
{
    public void TickAllEffects(BattleState state)
    {
        var allUnits = state.GetAllUnits();

        foreach (var unit in allUnits)
        {
            TickUnitEffects(unit);
        }
    }

    private void TickUnitEffects(UnitRuntime unit)
    {
        var expiredEffects = new List<StatusEffect>();

        foreach (var effect in unit.activeEffects)
        {
            effect.OnTick();

            if (effect.remainingDuration <= 0)
            {
                expiredEffects.Add(effect);
            }
        }

        // Remove expired effects
        foreach (var effect in expiredEffects)
        {
            RemoveEffect(unit, effect);
        }
    }

    private void RemoveEffect(UnitRuntime unit, StatusEffect effect)
    {
        effect.OnRemove();
        unit.activeEffects.Remove(effect);
    }
}
```

---

## Stacking Rules

### Stackable Effects

Some effects can stack (e.g., poison stacks, multiple attack buffs):

```csharp
// "Poison" ScriptableObject
isStackable = true;
maxStacks = 5; // Max 5 stacks
damagePerTick = 2; // 2 damage per tick per stack

// At 3 stacks: 6 damage per tick
```

### Non-Stackable Effects

Some effects don't stack, just refresh duration:

```csharp
// "Invulnerability" ScriptableObject
isStackable = false;

// If applied again, just refreshes duration
```

---

## Duration Types

### Turn-Based Duration

Effects last a certain number of turns:

```csharp
baseDuration = 3; // Lasts 3 turns
```

### Permanent (Until Dispelled)

```csharp
baseDuration = -1; // Infinite duration

// Must be removed manually (dispel magic, etc.)
```

---

## Damage Over Time (DOT)

### Poison Example

```csharp
// "Poison" ScriptableObject
effectId = "debuff_poison";
displayName = "Poison";
type = EffectType.Debuff;
baseDuration = 5; // 5 ticks
damagePerTick = 5; // 5 damage per tick
isStackable = true;
maxStacks = 3;

// At 3 stacks: 15 damage per tick for 5 ticks = 75 total damage
```

---

## Healing Over Time (HOT)

### Regeneration Example

```csharp
// "Regeneration" ScriptableObject
effectId = "buff_regen";
displayName = "Regeneration";
type = EffectType.Buff;
baseDuration = 5;
damagePerTick = -3; // Negative = healing
```

---

## Special Effect Behaviors (Future)

### Stun

```csharp
public class StatusEffect
{
    public bool preventsAction;

    public void OnApply()
    {
        if (definition.effectId == "debuff_stun")
        {
            preventsAction = true;
        }
    }
}

// In BehaviorSystem
public UnitIntent GenerateIntent(UnitRuntime unit, BattleState state)
{
    // Check if stunned
    if (unit.activeEffects.Any(e => e.preventsAction))
    {
        return new UnitIntent { actor = unit, type = IntentType.Wait };
    }

    // Normal behavior
    // ...
}
```

### Shield (Damage Absorption)

```csharp
public class StatusEffect
{
    public int shieldAmount;

    public void OnApply()
    {
        if (definition.effectId == "buff_shield")
        {
            shieldAmount = definition.shieldPower;
        }
    }
}

// In CombatResolutionSystem
private void DealDamage(UnitRuntime target, int damage)
{
    // Check for shield
    var shield = target.activeEffects.FirstOrDefault(e => e.shieldAmount > 0);

    if (shield != null)
    {
        int absorbed = Mathf.Min(damage, shield.shieldAmount);
        shield.shieldAmount -= absorbed;
        damage -= absorbed;

        if (shield.shieldAmount <= 0)
        {
            RemoveEffect(target, shield);
        }
    }

    // Apply remaining damage
    target.currentHP -= damage;
}
```

---

## Integration with Combat

### Applying Effects from Techniques

```csharp
// In CombatResolutionSystem
public void ResolveTechnique(UnitRuntime caster, ResolvedTechnique technique, UnitRuntime target)
{
    // Deal damage
    ResolveDamageTechnique(caster, technique, target);

    // Apply effects
    if (technique.effects != null)
    {
        foreach (var effectDef in technique.effects)
        {
            statusEffectSystem.ApplyEffect(target, effectDef);
        }
    }
}
```

### Example Technique with Effect

```csharp
// "Poison Strike" technique
var technique = new ResolvedTechnique
{
    techniqueName = "Poison Strike",
    type = TechniqueType.Attack,
    power = 30,
    effects = new List<StatusEffectDefinition>
    {
        poisonEffect // Apply poison on hit
    }
};
```

---

## Effect Modifiers in Damage Calculation

Status effects can modify damage via the modifier system:

```csharp
// "Attack Buff" effect modifies damage
private void ApplyStatusEffectModifiers(CombatContext context)
{
    foreach (var effect in context.attacker.activeEffects)
    {
        if (effect.definition.statModifier != null &&
            effect.definition.statModifier.affectedStat == StatType.Attack)
        {
            var modifier = new Modifier
            {
                source = ModifierSource.StatusEffect,
                type = ModifierType.Additive,
                value = effect.definition.statModifier.value * effect.stackCount
            };

            context.appliedModifiers.Add(modifier);
        }
    }
}
```

---

## Visual Indicators (Presentation)

### Effect Icons

Display active effects on units:

```csharp
// Subscribe to effect events
statusEffectSystem.OnEffectApplied += (unit, effect) =>
{
    // Show effect icon above unit
    ShowEffectIcon(unit.visualInstance, effect.definition.icon);
};

statusEffectSystem.OnEffectRemoved += (unit, effect) =>
{
    // Hide effect icon
    HideEffectIcon(unit.visualInstance, effect.definition.icon);
};
```

### DOT/HOT Numbers

```csharp
statusEffectSystem.OnDOTDamage += (unit, damage) =>
{
    // Show damage number (different color for DOT)
    ShowDamageNumber(unit.visualInstance, damage, Color.green);
};

statusEffectSystem.OnHOTHealing += (unit, healing) =>
{
    // Show healing number
    ShowHealingNumber(unit.visualInstance, healing);
};
```

---

## Testing

### Unit Tests

```csharp
[Test]
public void ApplyEffect_AddsEffectToUnit()
{
    var unit = CreateTestUnit();
    var effectDef = CreateAttackBuffEffect();

    statusEffectSystem.ApplyEffect(unit, effectDef);

    Assert.AreEqual(1, unit.activeEffects.Count);
}

[Test]
public void ApplyEffect_ModifiesStats()
{
    var unit = CreateTestUnit();
    unit.currentStats.attack = 10;
    var effectDef = CreateAttackBuffEffect(bonus: 5);

    statusEffectSystem.ApplyEffect(unit, effectDef);

    Assert.AreEqual(15, unit.currentStats.attack); // 10 + 5
}

[Test]
public void TickEffects_DecrementsDuration()
{
    var unit = CreateTestUnit();
    var effectDef = CreateAttackBuffEffect(duration: 3);
    statusEffectSystem.ApplyEffect(unit, effectDef);

    statusEffectSystem.TickUnitEffects(unit);

    Assert.AreEqual(2, unit.activeEffects[0].remainingDuration);
}

[Test]
public void TickEffects_RemovesExpiredEffects()
{
    var unit = CreateTestUnit();
    var effectDef = CreateAttackBuffEffect(duration: 1);
    statusEffectSystem.ApplyEffect(unit, effectDef);

    statusEffectSystem.TickUnitEffects(unit); // Duration becomes 0

    Assert.AreEqual(0, unit.activeEffects.Count); // Effect removed
}

[Test]
public void RemoveEffect_RestoresStats()
{
    var unit = CreateTestUnit();
    unit.currentStats.attack = 10;
    var effectDef = CreateAttackBuffEffect(bonus: 5, duration: 1);
    statusEffectSystem.ApplyEffect(unit, effectDef);

    Assert.AreEqual(15, unit.currentStats.attack);

    statusEffectSystem.TickUnitEffects(unit); // Removes effect

    Assert.AreEqual(10, unit.currentStats.attack); // Restored
}

[Test]
public void StackableEffect_Stacks()
{
    var unit = CreateTestUnit();
    var effectDef = CreatePoisonEffect(stackable: true, maxStacks: 5);

    statusEffectSystem.ApplyEffect(unit, effectDef);
    statusEffectSystem.ApplyEffect(unit, effectDef);

    Assert.AreEqual(1, unit.activeEffects.Count); // Only one effect
    Assert.AreEqual(2, unit.activeEffects[0].stackCount); // 2 stacks
}
```

---

## Summary

The Status Effect System manages **temporary modifiers** on units:

- ✅ Applies buffs and debuffs
- ✅ Tracks effect duration
- ✅ Ticks effects each turn (DOT/HOT, stat modifiers)
- ✅ Removes expired effects
- ✅ Supports stacking
- ✅ Integrates with combat via modifiers
- ✅ **Optional for MVP** - Can be added later

**Status effects add tactical depth without complicating core combat.**

---

## Related Documentation

- **DATA_MODELS.md** - StatusEffect, StatusEffectDefinition
- **SYSTEM_COMBAT_RESOLUTION.md** - How effects modify damage
- **SYSTEM_SKILLS.md** - Techniques can apply effects
- **MVP_SCOPE.md** - Status effects are optional for MVP

---

## Version

**Version 1.0** - Status effect system (future implementation)
