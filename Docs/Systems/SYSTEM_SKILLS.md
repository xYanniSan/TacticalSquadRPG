# SYSTEM_SKILLS.md

## Purpose

This document defines the **Skill System**, which implements the game's **signature modular action-chain mechanic**. This is one of the core identity pillars of the entire project.

**Core Concept:** Heroes don't just have flat skills. Instead, they configure **action sequences** (chains of 5 actions) in **skill slots**, which the combat system resolves into **techniques** (actual abilities).

---

## Critical Identity Pillar

This system is **NOT optional** or **secondary**. It is **central to the game's unique identity**.

**What makes this game different from other tactical auto-battlers:**
- TFT = Champion abilities are pre-defined, fixed
- **This game** = Players configure action-chains that resolve into techniques

**The 5x5 structure (5 skill slots × 5 action sub-slots) is a defining feature.**

---

## Responsibilities

The Skill System is responsible for:

- Managing skill slot structure (up to 5 slots per hero)
- Managing action sub-slot structure (5 actions per skill)
- Resolving action-chains into executable techniques
- Applying proficiency bonuses to techniques
- Validating skill availability and usage
- Supporting pre-battle skill configuration

---

## What Skill System Does NOT Do

- **Does not decide when to use skills** - That's BehaviorSystem
- **Does not execute damage/healing** - That's CombatResolutionSystem
- **Does not select targets** - That's TargetingSystem
- **Does not handle skill visuals** - That's presentation layer

**Skills resolve "what technique", not "when" or "how much damage".**

---

## Skill Slot Structure

### Full System (Future)

Each hero can equip **up to 5 skill slots**.

Each skill slot contains **5 action sub-slots**.

**Total:** 5 × 5 = 25 action slots per hero

### MVP (Simplified)

For MVP, use **1-2 skill slots** with **3-5 actions** each.

**Recommended MVP:** 2 skill slots, 5 actions each = 10 action slots per hero

This proves the concept without overwhelming the player.

---

## Data Structures

See DATA_MODELS.md for full definitions.

### SkillSlot

```csharp
public class SkillSlot
{
    public int slotIndex;              // 0-4 (for 5 slots)
    public List<ActionSlot> actionSequence; // 5 sub-slots containing actions

    // Optional metadata
    public string slotName;            // Player-assigned name (e.g., "Fire Combo")
    public KeyCode hotkey;             // Keyboard shortcut (future, if manual control)

    public ResolvedTechnique ResolveSequence(UnitRuntime caster);
}
```

### ActionSlot

```csharp
public struct ActionSlot
{
    public int subSlotIndex;           // 0-4 (for 5 sub-slots)
    public ActionDefinition action;    // Reference to static action data
}
```

### ActionDefinition (Static)

```csharp
[CreateAssetMenu(menuName = "Game/Actions/Action")]
public class ActionDefinition : ScriptableObject
{
    [Header("Identity")]
    public string actionId;            // "action_punch", "action_fire_sign_a"
    public string displayName;         // "Punch", "Fire Sign: Ashes"
    public Sprite icon;                // UI icon

    [Header("Properties")]
    public ActionType actionType;      // Physical, Elemental, Support
    public ElementType element;        // None, Fire, Water, Earth, Lightning, Wind
    public float basePower;            // Contribution to technique power

    // Future: Animation, VFX, sound
}
```

### ResolvedTechnique (Runtime)

```csharp
public class ResolvedTechnique
{
    public string techniqueName;       // "Fire Breath", "Triple Strike"
    public TechniqueType type;         // Attack, Heal, Buff, Debuff
    public ElementType element;

    public int power;                  // Damage or healing amount
    public TargetPattern targetPattern; // Single, AOE, Line, etc.
    public List<EffectDefinition> effects; // Status effects to apply

    // Metadata
    public List<ActionDefinition> sourceActions; // What actions created this
}
```

---

## Action Types

### ActionType Enum

```csharp
public enum ActionType
{
    Physical,      // Punch, Kick, Strike
    Elemental,     // Hand signs, elemental gestures
    Support,       // Meditate, Focus, Charge
    Movement       // Step, Dash (future)
}
```

### Element Types

```csharp
public enum ElementType
{
    None,          // Non-elemental
    Fire,
    Water,
    Earth,
    Lightning,
    Wind

    // Future: Ice, Light, Dark, etc.
}
```

---

## Example Actions (MVP)

Create ScriptableObjects for basic actions:

### Physical Actions

1. **Punch**
   - Type: Physical
   - Element: None
   - Power: 10

2. **Kick**
   - Type: Physical
   - Element: None
   - Power: 12

3. **Strike**
   - Type: Physical
   - Element: None
   - Power: 15

### Elemental Actions

4. **Fire Sign A**
   - Type: Elemental
   - Element: Fire
   - Power: 5

5. **Fire Sign B**
   - Type: Elemental
   - Element: Fire
   - Power: 5

6. **Lightning Sign A**
   - Type: Elemental
   - Element: Lightning
   - Power: 5

### Support Actions

7. **Meditate**
   - Type: Support
   - Element: None
   - Power: 5

8. **Focus**
   - Type: Support
   - Element: None
   - Power: 5

9. **Charge**
   - Type: Support
   - Element: None
   - Power: 10

---

## Action-Chain Resolution

### Core Mechanic

When a skill is used, the system analyzes the **sequence of 5 actions** and resolves them into a **technique**.

### Resolution Logic

**Pattern Matching:** Certain action sequences produce specific techniques.

**Example Patterns:**

1. **Triple Strike** (Physical burst combo)
   - Pattern: 3+ Physical actions
   - Technique: Triple Strike (Attack, high damage)
   - Power: Sum of action powers × 1.5

2. **Fire Breath** (Elemental attack)
   - Pattern: 2+ Fire elemental actions
   - Technique: Fire Breath (Attack, Fire element)
   - Power: Sum of action powers × 2.0

3. **Healing Aura** (Support technique)
   - Pattern: 3+ Support actions
   - Technique: Healing Aura (Heal, self or ally)
   - Power: Sum of action powers × 1.2

4. **Lightning Kick** (Hybrid physical/elemental)
   - Pattern: 2 Physical + 1 Lightning
   - Technique: Lightning Kick (Attack, Lightning element)
   - Power: Sum of action powers × 1.8

---

## Technique Resolution Algorithm

### Simple Pattern Matching (MVP)

```csharp
public ResolvedTechnique ResolveSequence(UnitRuntime caster, SkillSlot skill)
{
    var actions = skill.actionSequence.Select(slot => slot.action).ToList();

    // Count action types
    int physicalCount = actions.Count(a => a.actionType == ActionType.Physical);
    int elementalCount = actions.Count(a => a.actionType == ActionType.Elemental);
    int supportCount = actions.Count(a => a.actionType == ActionType.Support);

    // Count elements
    int fireCount = actions.Count(a => a.element == ElementType.Fire);
    int lightningCount = actions.Count(a => a.element == ElementType.Lightning);

    // Calculate base power
    int basePower = actions.Sum(a => (int)a.basePower);

    // Pattern matching
    ResolvedTechnique technique;

    if (physicalCount >= 3)
    {
        // Triple Strike
        technique = new ResolvedTechnique
        {
            techniqueName = "Triple Strike",
            type = TechniqueType.Attack,
            element = ElementType.None,
            power = (int)(basePower * 1.5f),
            targetPattern = TargetPattern.Single,
            sourceActions = actions
        };
    }
    else if (fireCount >= 2)
    {
        // Fire Breath
        technique = new ResolvedTechnique
        {
            techniqueName = "Fire Breath",
            type = TechniqueType.Attack,
            element = ElementType.Fire,
            power = (int)(basePower * 2.0f),
            targetPattern = TargetPattern.Single,
            sourceActions = actions
        };
    }
    else if (supportCount >= 3)
    {
        // Healing Aura
        technique = new ResolvedTechnique
        {
            techniqueName = "Healing Aura",
            type = TechniqueType.Heal,
            element = ElementType.None,
            power = (int)(basePower * 1.2f),
            targetPattern = TargetPattern.Self,
            sourceActions = actions
        };
    }
    else if (physicalCount >= 2 && lightningCount >= 1)
    {
        // Lightning Kick
        technique = new ResolvedTechnique
        {
            techniqueName = "Lightning Kick",
            type = TechniqueType.Attack,
            element = ElementType.Lightning,
            power = (int)(basePower * 1.8f),
            targetPattern = TargetPattern.Single,
            sourceActions = actions
        };
    }
    else
    {
        // Default: Basic combo
        technique = new ResolvedTechnique
        {
            techniqueName = "Basic Combo",
            type = TechniqueType.Attack,
            element = ElementType.None,
            power = basePower,
            targetPattern = TargetPattern.Single,
            sourceActions = actions
        };
    }

    // Apply proficiency bonuses
    ApplyProficiencyBonuses(caster, technique);

    return technique;
}
```

---

## Proficiency Bonuses

### How Proficiencies Affect Techniques

When a technique is resolved, the caster's **proficiencies** modify its effectiveness.

```csharp
private void ApplyProficiencyBonuses(UnitRuntime caster, ResolvedTechnique technique)
{
    float bonus = 1.0f;

    // Elemental proficiency
    if (technique.element != ElementType.None)
    {
        bonus *= caster.definition.proficiencies.GetProficiencyBonus(technique.element);
    }

    // Technique type proficiency
    bonus *= caster.definition.proficiencies.GetProficiencyBonus(technique.type);

    // Apply bonus to power
    technique.power = (int)(technique.power * bonus);
}
```

### Example

**Hero "Kai"** has:
- Fire proficiency: 1.3× (30% bonus)
- Attack proficiency: 1.0× (neutral)

**Kai uses Fire Breath:**
- Base power: 50 (from action chain)
- Pattern multiplier: 2.0× = 100
- Fire proficiency: 1.3× = 130
- **Final power: 130**

**Hero "Rin"** (no fire proficiency) uses same chain:
- Base power: 50
- Pattern multiplier: 2.0× = 100
- Fire proficiency: 1.0× (neutral) = 100
- **Final power: 100**

**This encourages players to match heroes with techniques they're good at, but doesn't lock them out.**

---

## Pre-Battle Skill Configuration

### Player Configures Skills

Before battle, players assign actions to skill slots:

**UI Flow:**
1. Select hero
2. Open skill configuration screen
3. For each skill slot (0-4):
   - Assign 5 actions to sub-slots
   - Preview resolved technique
   - Save configuration

```csharp
public void ConfigureSkillSlot(UnitRuntime hero, int slotIndex, List<ActionDefinition> actions)
{
    if (hero.equippedSkills.Count <= slotIndex)
    {
        hero.equippedSkills.Add(new SkillSlot { slotIndex = slotIndex });
    }

    var skill = hero.equippedSkills[slotIndex];
    skill.actionSequence.Clear();

    for (int i = 0; i < actions.Count; i++)
    {
        skill.actionSequence.Add(new ActionSlot
        {
            subSlotIndex = i,
            action = actions[i]
        });
    }
}
```

---

## Skill Usage in Combat

### When Skills Are Used

**BehaviorSystem** decides when to use a skill:

```csharp
// Aggressive behavior might use skill when available
if (unit.equippedSkills.Count > 0)
{
    var skill = unit.equippedSkills[0]; // Use first skill
    float skillRange = 2.0f;

    if (distance <= skillRange)
    {
        return new UnitIntent
        {
            actor = unit,
            type = IntentType.UseSkill,
            target = target,
            skillToUse = skill
        };
    }
}
```

### Skill Execution Flow

```
1. BehaviorSystem generates intent with type = UseSkill
2. BattleManager calls SkillSystem.ResolveSkill()
3. SkillSystem analyzes action-chain
4. SkillSystem returns ResolvedTechnique
5. CombatResolutionSystem applies technique to target
```

---

## Cooldowns and Charges (Future)

### For MVP: No Cooldowns

Skills can be used every tick if behavior allows.

### Future: Cooldown System

```csharp
public class SkillSlot
{
    public int cooldownTicks;          // Ticks remaining before usable
    public int maxCooldown;            // Cooldown duration

    public bool IsAvailable()
    {
        return cooldownTicks == 0;
    }

    public void Use()
    {
        cooldownTicks = maxCooldown;
    }

    public void TickCooldown()
    {
        if (cooldownTicks > 0)
            cooldownTicks--;
    }
}
```

### Future: Charge System

```csharp
public class SkillSlot
{
    public int currentCharges;
    public int maxCharges;

    public bool CanUse()
    {
        return currentCharges > 0;
    }

    public void Use()
    {
        currentCharges--;
    }
}
```

---

## Advanced Pattern Matching (Future)

### Sequence-Order Matters

Later, the ORDER of actions could matter:

```csharp
// [Fire Sign A, Fire Sign B, Punch, Kick, Focus] = Fire-Empowered Strike
// [Punch, Kick, Fire Sign A, Fire Sign B, Focus] = Different technique
```

### Combo Libraries

Create a library of known techniques:

```csharp
[CreateAssetMenu(menuName = "Game/Techniques/Technique")]
public class TechniqueDefinition : ScriptableObject
{
    public string techniqueName;
    public List<ActionDefinition> requiredSequence; // Exact sequence
    public int power;
    public TechniqueType type;
    public ElementType element;
}

public ResolvedTechnique MatchTechnique(List<ActionDefinition> actions)
{
    // Search technique library for exact or partial matches
    foreach (var techniqueTemplate in techniqueLibrary)
    {
        if (SequenceMatches(actions, techniqueTemplate.requiredSequence))
        {
            return CreateTechniqueFromTemplate(techniqueTemplate);
        }
    }

    // Fallback to generic pattern matching
    return MatchGenericPattern(actions);
}
```

---

## Player Discovery vs Guided Configuration

### MVP: Guided Configuration

For MVP, show players what technique will resolve:

**UI shows:**
- Player selects actions
- Real-time preview: "This will create: Fire Breath (Power: 130)"

### Future: Discovery System

Later, techniques could be discovered:

- Player experiments with action chains
- New technique discovered: "You created: **Blazing Phoenix Strike**!"
- Technique added to codex
- Player can recreate or modify

This adds depth and replayability.

---

## Technique Types

```csharp
public enum TechniqueType
{
    Attack,        // Deals damage
    Heal,          // Restores HP
    Buff,          // Applies beneficial effect
    Debuff,        // Applies harmful effect
    Utility        // Movement, control, etc.
}
```

### MVP Techniques

For MVP, focus on **Attack** and optionally **Heal**.

Buffs, debuffs, and utility can be added later.

---

## Target Patterns

```csharp
public enum TargetPattern
{
    Single,        // One enemy
    AOE,           // Area around target
    Line,          // Line of enemies
    Self,          // Caster only
    AllAllies,     // All allies
    AllEnemies     // All enemies
}
```

### MVP: Single Target

For MVP, all techniques target **Single**.

AOE and multi-target can be added later.

---

## Example Technique Configurations

### Example 1: Physical Striker

**Hero: Melee Fighter**

**Skill Slot 1:** Triple Strike
- Actions: `[Punch, Punch, Kick, Strike, Punch]`
- Resolved: Triple Strike (Power: ~70, single target)

**Skill Slot 2:** Knockback Combo
- Actions: `[Kick, Kick, Strike, Kick, Kick]`
- Resolved: Heavy Kick (Power: ~65, single target)

### Example 2: Fire Mage

**Hero: Kai (Fire Proficiency 1.3×)**

**Skill Slot 1:** Fire Breath
- Actions: `[Fire Sign A, Fire Sign B, Focus, Fire Sign A, Meditate]`
- Resolved: Fire Breath (Power: ~130 after proficiency, single target)

**Skill Slot 2:** Healing Meditation
- Actions: `[Meditate, Focus, Meditate, Meditate, Focus]`
- Resolved: Healing Aura (Power: ~35, self heal)

### Example 3: Hybrid

**Hero: Lightning Warrior**

**Skill Slot 1:** Lightning Kick
- Actions: `[Punch, Kick, Lightning Sign A, Kick, Focus]`
- Resolved: Lightning Kick (Power: ~80, Lightning element)

**Skill Slot 2:** Defensive Stance
- Actions: `[Meditate, Meditate, Focus, Focus, Charge]`
- Resolved: Shield Aura (Future: buff defense)

---

## Testing

### Unit Tests

```csharp
[Test]
public void ResolveSequence_TripleStrike_WhenThreePhysicalActions()
{
    var actions = new List<ActionDefinition>
    {
        CreateAction(ActionType.Physical, 10),
        CreateAction(ActionType.Physical, 10),
        CreateAction(ActionType.Physical, 10),
        CreateAction(ActionType.Support, 5),
        CreateAction(ActionType.Support, 5)
    };

    var skill = CreateSkillSlot(actions);
    var caster = CreateTestUnit();

    var technique = skillSystem.ResolveSequence(caster, skill);

    Assert.AreEqual("Triple Strike", technique.techniqueName);
    Assert.AreEqual(TechniqueType.Attack, technique.type);
}

[Test]
public void ResolveSequence_FireBreath_WhenTwoFireActions()
{
    var actions = new List<ActionDefinition>
    {
        CreateAction(ActionType.Elemental, 10, ElementType.Fire),
        CreateAction(ActionType.Elemental, 10, ElementType.Fire),
        CreateAction(ActionType.Support, 5),
        CreateAction(ActionType.Support, 5),
        CreateAction(ActionType.Support, 5)
    };

    var skill = CreateSkillSlot(actions);
    var caster = CreateTestUnit();

    var technique = skillSystem.ResolveSequence(caster, skill);

    Assert.AreEqual("Fire Breath", technique.techniqueName);
    Assert.AreEqual(ElementType.Fire, technique.element);
}

[Test]
public void ApplyProficiency_IncreasePower()
{
    var technique = new ResolvedTechnique
    {
        element = ElementType.Fire,
        power = 100
    };

    var caster = CreateTestUnit();
    caster.definition.proficiencies.elementProficiencies[ElementType.Fire] = 1.5f;

    skillSystem.ApplyProficiencyBonuses(caster, technique);

    Assert.AreEqual(150, technique.power); // 100 * 1.5
}
```

---

## Summary

The Skill System is the **signature mechanic** of this game:

- ✅ **5 skill slots × 5 action sub-slots** (MVP: 2 slots × 5 actions)
- ✅ Action-chains resolve into techniques via pattern matching
- ✅ Proficiencies modify technique effectiveness
- ✅ Players configure skills pre-battle
- ✅ Behavior system decides when to use skills
- ✅ Combat system executes technique damage/effects

**This system defines the game's unique identity. It is NOT optional.**

---

## Related Documentation

- **DATA_MODELS.md** - SkillSlot, ActionDefinition, ResolvedTechnique
- **PROJECT_OVERVIEW.md** - Skill system as core identity pillar
- **SYSTEM_BEHAVIOR.md** - When to use skills
- **SYSTEM_COMBAT_RESOLUTION.md** - How techniques deal damage
- **SYSTEM_UNITS.md** - Proficiencies and hero identity

---

## Version

**Version 1.0** - Initial skill/action-chain system for MVP
