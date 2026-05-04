# SYSTEM_PROGRESSION.md

## Purpose

This document defines the **Progression System**, which handles hero growth, stat increases, proficiency specialization, and long-term character development. Progression is a major pillar of the long-term game.

---

## MVP Scope

**Progression is OUT OF SCOPE for MVP.**

The MVP focuses on proving combat works. Progression can be added after combat is validated.

**This document describes the future progression system.**

---

## Design Philosophy

Progression should:
- **Expand tactical options**, not just increase numbers
- **Shape hero identity** without hard-locking roles
- **Reward experimentation** and build diversity
- **Feel meaningful** at each level
- **Support long-term engagement** (3-4 year timeline)

---

## Progression Pillars

### 1. Level Growth

Heroes gain levels through combat experience.

```csharp
public class HeroProgressionData
{
    public int level;
    public int currentXP;
    public int xpToNextLevel;

    public void GainXP(int amount)
    {
        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        currentXP -= xpToNextLevel;
        xpToNextLevel = CalculateXPForLevel(level + 1);

        // Apply stat growth
        ApplyStatGrowth();

        // Unlock new options
        CheckUnlocks();
    }
}
```

### 2. Stat Growth

Heroes gain stats as they level up.

```csharp
public class StatGrowthCurve
{
    public int baseMaxHP;
    public int hpPerLevel;
    public int baseAttack;
    public int attackPerLevel;
    public int baseDefense;
    public int defensePerLevel;

    public StatBlock GetStatsAtLevel(int level)
    {
        return new StatBlock
        {
            maxHP = baseMaxHP + (hpPerLevel * level),
            attack = baseAttack + (attackPerLevel * level),
            defense = baseDefense + (defensePerLevel * level),
            moveSpeed = 1.0f // Or grows with level
        };
    }
}
```

**Design goal:** Stats should grow, but not so fast that early content becomes trivial.

### 3. Proficiency Specialization

Heroes can increase proficiencies as they level up.

```csharp
public class ProficiencyGrowth
{
    public Dictionary<ElementType, int> elementLevels;
    public Dictionary<ActionType, int> actionLevels;

    public float GetProficiencyBonus(ElementType element)
    {
        int level = elementLevels.GetValueOrDefault(element, 0);
        return 1.0f + (level * 0.05f); // +5% per level
    }
}
```

**Example:**
- Fire proficiency level 5 = 1.25× (25% bonus)
- Physical proficiency level 3 = 1.15× (15% bonus)

**Unlocking proficiency points:**
- Gain 1 proficiency point per level
- Player assigns points to proficiencies
- Encourages specialization without hard-locking

---

## 4. Passive Abilities

Heroes unlock passive abilities through leveling or skill trees.

```csharp
[CreateAssetMenu(menuName = "Game/Passives/Passive")]
public class PassiveDefinition : ScriptableObject
{
    public string passiveId;
    public string displayName;
    public string description;
    public Sprite icon;

    public PassiveEffect effect;
}

public enum PassiveEffect
{
    IncreasedCritChance,
    BonusDamageToLowHP,
    StartBattleWithBuff,
    ExtraSkillCharge,
    // Many more
}
```

**Examples:**
- **"Berserker"** - Deal 20% more damage when below 50% HP
- **"First Strike"** - Start battle with +10 attack for 3 turns
- **"Combo Master"** - Physical techniques cost 1 less action
- **"Elemental Focus"** - +30% Fire damage, -10% other elements

### 5. Skill Trees

Heroes have skill trees that unlock new abilities, stat boosts, and passive effects.

```csharp
public class SkillTree
{
    public List<SkillTreeNode> nodes;

    public void UnlockNode(SkillTreeNode node)
    {
        if (!CanUnlock(node))
        {
            Debug.LogWarning("Cannot unlock node: requirements not met");
            return;
        }

        node.isUnlocked = true;
        ApplyNodeBenefits(node);
    }

    private bool CanUnlock(SkillTreeNode node)
    {
        // Check if prerequisites are unlocked
        return node.prerequisites.All(pre => pre.isUnlocked);
    }
}

public class SkillTreeNode
{
    public string nodeId;
    public string displayName;
    public string description;

    public List<SkillTreeNode> prerequisites;
    public bool isUnlocked;

    public NodeReward reward; // Stat boost, passive, action unlock
}
```

**Design:**
- Multiple paths (offensive, defensive, utility)
- Encourages different builds
- Can't unlock everything (forces choices)

---

## 6. Action Unlocks

New actions are unlocked through progression.

**Starting actions:** 5-10 basic actions (punch, kick, basic signs)

**Unlocked actions:**
- **Level 5:** Unlock "Charge" action
- **Level 10:** Unlock "Lightning Sign C"
- **Skill tree:** Unlock "Clone Strike" action

This encourages experimentation with new action-chains as heroes grow.

---

## Progression Integration with Combat

### Hero Save Data

```csharp
public class HeroSaveData
{
    public string heroId;              // Which hero definition
    public int level;
    public int currentXP;

    // Stats
    public StatBlock bonusStats;       // Permanent stat increases

    // Proficiencies
    public Dictionary<ElementType, int> elementProficiencyLevels;
    public Dictionary<ActionType, int> actionProficiencyLevels;

    // Unlocks
    public List<string> unlockedPassives;
    public List<string> unlockedActions;
    public List<string> unlockedSkillTreeNodes;

    // Loadouts (saved skill configurations)
    public List<SkillSlotSaveData> savedSkillSlots;
}
```

### Creating UnitRuntime from Save Data

```csharp
public UnitRuntime CreateProgressedUnit(UnitDefinition definition, HeroSaveData saveData)
{
    var unit = new UnitRuntime
    {
        definition = definition,
        // ...
    };

    // Apply progression stats
    unit.currentStats = definition.baseStats + saveData.bonusStats;
    unit.maxHP = unit.currentStats.maxHP;
    unit.currentHP = unit.maxHP;

    // Apply proficiency growth
    foreach (var element in saveData.elementProficiencyLevels)
    {
        float bonus = 1.0f + (element.Value * 0.05f);
        unit.definition.proficiencies.elementProficiencies[element.Key] = bonus;
    }

    // Apply passives
    foreach (var passiveId in saveData.unlockedPassives)
    {
        var passive = LoadPassiveDefinition(passiveId);
        ApplyPassive(unit, passive);
    }

    return unit;
}
```

---

## XP and Leveling

### XP Sources

- **Winning battles** - Base XP reward
- **Defeating enemies** - XP per enemy killed
- **Mission completion** - Bonus XP for objectives
- **First-time bonuses** - Extra XP for new missions

### XP Curve

```csharp
public int CalculateXPForLevel(int level)
{
    // Example curve: exponential growth
    return 100 * (int)Mathf.Pow(level, 1.5f);

    // Level 1 → 2: 100 XP
    // Level 2 → 3: 282 XP
    // Level 3 → 4: 519 XP
    // Level 10 → 11: ~3162 XP
}
```

**Design goal:** Early levels are fast, later levels take longer but are more rewarding.

---

## Resource Systems (Future)

### Currency

- **Gold** - Buy items, upgrade equipment
- **Skill Points** - Unlock skill tree nodes
- **Essence** - Upgrade proficiencies

### Materials

- **Elemental Shards** - Unlock element-specific skills
- **Training Scrolls** - Boost proficiency XP

---

## Progression UI (Presentation)

### Level Up Screen

```
╔════════════════════════════════════╗
║         LEVEL UP!                  ║
║                                    ║
║    Kai reached Level 5!            ║
║                                    ║
║    +5 HP                           ║
║    +2 Attack                       ║
║    +1 Defense                      ║
║                                    ║
║    Unlocked: Charge (Action)       ║
║                                    ║
║    [Continue]                      ║
╚════════════════════════════════════╝
```

### Skill Tree UI

```
         [Tank Path]
              │
      ┌───────┴───────┐
      │               │
 [+10 HP]      [+5 Defense]
      │               │
 [Taunt]      [Shield Aura]
```

### Proficiency Upgrade UI

```
Fire Proficiency: ★★★☆☆ (Level 3 → 4)
+5% Fire Damage (Total: 20% → 25%)

Cost: 1 Skill Point

[Upgrade]
```

---

## Balance Considerations

### Power Creep

Avoid making high-level heroes too powerful:
- Scale enemy difficulty with player level
- Progression adds options, not just raw power
- Late-game enemies have counters to common builds

### Build Diversity

Encourage multiple viable builds:
- No "must-have" passives
- Different playstyles should be equally viable
- Respec option (reroll skill tree, proficiencies)

---

## Future: Prestige/Ascension

### Endgame Progression

After reaching max level (e.g., 50), heroes can "ascend":

- **Reset level to 1** (keep skill trees)
- **Gain ascension point**
- **Unlock new passive tier**
- **Increased stat scaling**

This provides infinite progression for dedicated players.

---

## Summary

The Progression System handles **long-term hero growth**:

- ✅ Level growth and XP
- ✅ Stat increases
- ✅ Proficiency specialization
- ✅ Passive ability unlocks
- ✅ Skill tree system
- ✅ Action unlocks
- ✅ **Out of scope for MVP** - Add after combat validation
- ✅ Focus on expanding options, not just power

**Progression makes heroes more expressive and diverse over time.**

---

## Related Documentation

- **DATA_MODELS.md** - UnitDefinition, ProficiencySet
- **SYSTEM_UNITS.md** - Hero base identity
- **SYSTEM_SAVE_LOAD.md** - Persistent progression data
- **MVP_SCOPE.md** - Progression excluded from MVP

---

## Version

**Version 1.0** - Progression system design (future implementation)
