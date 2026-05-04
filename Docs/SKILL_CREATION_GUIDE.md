# Skill Creation Guide

This document describes the complete process for adding a new skill to the game — from design through code to Unity editor steps. It also describes how to extend the system when a new skill requires new behaviour that does not exist yet.

---

## Overview: How Skills Work

A **skill** is an ordered sequence of `ActionDefinition` assets assigned to a unit's action slot in the `TerrainBattleManager` Inspector. When a unit decides to use a skill, this happens:

```
PickBestSkill()
  → SkillSystem.ResolveSkill(slot, caster)
      → ComboLibrary.TryMatch(actionIds)           ← checks for a combo recipe
          match  → ResolvedTechnique (isCombo=true, type=TechniqueType.X)
          no match → each action fires its standalone effect via ExecuteIndividualActions()
  → UpdateDecide() picks CastType (Melee / Mobile / Rooted) → transitions state machine
  → FireDamage()
      → if isCombo → BattleCombatResolver.ResolveSkillAttack()
      → else       → BattleCombatResolver.ExecuteIndividualActions()
```

**Key files:**

| File | Role |
|---|---|
| `Assets/Scripts/DataModels/Enums.cs` | All enums: `ActionType`, `TechniqueType`, `ElementType`, `CastType` |
| `Assets/Scripts/DataModels/ActionDefinition.cs` | ScriptableObject — one asset per action input (Punch, Hand Sign A, etc.) |
| `Assets/Scripts/DataModels/ComboLibrary.cs` | Hardcoded fallback combo recipe list |
| `Assets/Scripts/DataModels/ComboRecipeDefinition.cs` | ScriptableObject — one asset per combo recipe |
| `Assets/Scripts/DataModels/ComboLibraryAsset.cs` | ScriptableObject container holding all recipes |
| `Assets/Scripts/Systems/SkillSystem.cs` | Resolves a slot into a `ResolvedTechnique` |
| `Assets/Scripts/ThirdPerson/BattleCombatResolver.cs` | Executes the resolved technique (damage, buff, orb spawn, etc.) |
| `Assets/Scripts/Editor/SkillDataCreator.cs` | Editor tool — generates all action and combo assets |

---

## Part 1: Adding a Standard Skill (no new behaviour)

A "standard" skill uses only existing `ActionType` and `TechniqueType` values (Physical, Elemental, Support, Attack, Buff, etc.). No new C# code is needed.

### Step 1 — Design the skill

Decide:
- **Input sequence** — which action IDs the unit must have in their slot, in order (e.g. `handsign_a`, `punch`)
- **Result name** — shown in logs (e.g. `"Earth Fist"`)
- **TechniqueType** — what the resolver does with it (`Attack`, `Buff`, `Heal`, `Summon`)
- **ElementType** — dominant element (`Earth`, `Lightning`, `Water`, `Fire`, `Wind`, `None`)
- **Power multiplier** — scales raw action `basePower` sum (e.g. `1.4` = 40% stronger than base)
- **CastType** — movement behaviour during cast (`Melee`, `Mobile`, `Rooted`)

### Step 2 — Add the recipe to `SkillDataCreator.cs`

Open `Assets/Scripts/Editor/SkillDataCreator.cs`. Find `CreateComboLibrary()`. Add a new `MakeRecipe` call. Longer sequences must be listed **above** shorter ones.

```csharp
// Example: A > A > Punch (3-action combo)
recipeAssets.Add(MakeRecipe("Combo_MyNewSkill",
    "My New Skill", "Short description of what this does.",
    new[] { "handsign_a", "handsign_a", "punch" },
    TechniqueType.Attack, ElementType.Earth, 1.8f, CastType.Melee));
```

Also add it to the hardcoded fallback list in `Assets/Scripts/DataModels/ComboLibrary.cs` inside the `Recipes` list at the correct position (longer sequences first):

```csharp
new ComboRecipe(
    new[] { "handsign_a", "handsign_a", "punch" },
    "My New Skill", TechniqueType.Attack, ElementType.Earth, 1.8f, CastType.Melee),
```

### Step 3 — Regenerate assets in Unity

In Unity's top menu:
1. **TacticalRPG → Create Combo Library**

This rebuilds `Assets/Data/Combos/ComboLibrary.asset` and all individual recipe assets.

### Step 4 — Assign the skill to a hero

1. Select **TerrainBattleManager** in the Hierarchy.
2. Find the hero under **Player Heroes**.
3. Set their **Actions** list to the input sequence (e.g. `HandSignA`, `HandSignA`, `Punch`).

The unit will now use this skill whenever it can afford the total energy cost.

---

## Part 2: Adding a New Action Input

If the skill needs an entirely new action input that doesn't exist yet (e.g. a new hand sign, a new physical move):

### Step 1 — Decide what the action does standalone

Every action has a standalone effect if the unit fires it without a combo match:
- `Physical` → basic melee hit
- `Elemental` → applies a charge-based self-buff (bonus damage per hit)
- `Support` → stores a pending power boost (+X% to next technique)
- `OrbSummon` → spawns orbiting orbs (see Part 3)

### Step 2 — Add to `SkillDataCreator.cs`

Find `CreateActions()` and add a new `CreateAction` call:

```csharp
CreateAction("HandSignD", new ActionData
{
    actionId         = "handsign_d",    // must be lowercase, unique, used in combo recipes
    displayName      = "Hand Sign D",
    actionType       = ActionType.Elemental,
    basePower        = 12f,
    element          = ElementType.Wind,
    energyCost       = 10f,
    selfBuffDamage   = 20,
    selfBuffCharges  = 10,
    powerBoostPct    = 0f
}, forceRecreate);
```

### Step 3 — Regenerate in Unity

**TacticalRPG → Recreate Action Definitions (force)**

This rebuilds `Assets/Data/Actions/` with the new asset included.

---

## Part 3: Adding a Skill That Needs New Behaviour

When a skill does something that no existing `TechniqueType` handles — like spawning orbiting projectiles, applying a shield, rooting the enemy, etc. — you need to extend the code before creating the asset.

This is a 5-step process. The Orb Strike skill (`A A A B B`) is the reference example.

---

### 3.1 — Add a new `ActionType` (if the action input itself is a new kind of thing)

Open `Assets/Scripts/DataModels/Enums.cs`:

```csharp
public enum ActionType
{
    Physical,
    Elemental,
    Support,
    Movement,
    OrbSummon,      // ← added for orb skill
    YourNewType     // ← add here
}
```

Only needed if the action itself has unique standalone behaviour (not just a combo trigger).

---

### 3.2 — Add a new `TechniqueType` (what the combo resolves to)

In the same file:

```csharp
public enum TechniqueType
{
    Attack,
    Heal,
    Buff,
    Debuff,
    Utility,
    Summon,
    OrbSummon,      // ← added for orb skill
    YourNewType     // ← add here
}
```

---

### 3.3 — Tell `SkillSystem` how to detect the new type

Open `Assets/Scripts/Systems/SkillSystem.cs`, find `GetTechniqueType()`. Add detection logic **before** the existing checks:

```csharp
private TechniqueType GetTechniqueType(ActionType dominant, List<ActionDefinition> actions)
{
    // Detect OrbSummon: any action in the chain is OrbSummon type
    foreach (var action in actions)
        if (action.actionType == ActionType.OrbSummon)
            return TechniqueType.OrbSummon;

    // Add your new detection logic here, e.g.:
    // foreach (var action in actions)
    //     if (action.actionType == ActionType.YourNewType)
    //         return TechniqueType.YourNewType;

    // ... rest of existing logic
```

---

### 3.4 — Add the resolver branch in `BattleCombatResolver`

Open `Assets/Scripts/ThirdPerson/BattleCombatResolver.cs`, find `ResolveSkillAttack()`. Add a new early-return branch for your new type:

```csharp
if (tech.type == TechniqueType.OrbSummon)
{
    ApplyOrbSummon(attacker, tech);
    return;
}

// Add yours:
// if (tech.type == TechniqueType.YourNewType)
// {
//     ApplyYourNewSkill(attacker, tech);
//     return;
// }
```

Then write the private method that implements the actual behaviour:

```csharp
private void ApplyYourNewSkill(TerrainBattleUnit caster, ResolvedTechnique tech)
{
    // implement here
}
```

---

### 3.5 — Add any new data fields to `ActionDefinition`

If the new skill needs configuration parameters (like orb count or orb damage), add them to `ActionDefinition.cs` under a new header:

```csharp
[Header("Your New Skill")]
public int yourNewField = 0;
```

And expose them in `SkillDataCreator.cs` via the `ActionData` struct:

```csharp
private struct ActionData
{
    // ... existing fields ...
    public int yourNewField;
}
```

And write the value when creating the asset:

```csharp
def.yourNewField = data.yourNewField;
```

---

### 3.6 — Add the recipe to both `SkillDataCreator` and `ComboLibrary`

Follow the same steps as Part 1, Step 2 — but use your new `TechniqueType`:

```csharp
recipeAssets.Add(MakeRecipe("Combo_YourSkill",
    "Your Skill Name", "Description.",
    new[] { "handsign_a", "handsign_a", "your_action_id" },
    TechniqueType.YourNewType, ElementType.None, 1.0f, CastType.Rooted));
```

---

### 3.7 — Regenerate in Unity

1. **TacticalRPG → Recreate Action Definitions (force)** — rebuilds action assets
2. **TacticalRPG → Create Combo Library** — rebuilds combo assets

---

## Part 4: Keeping This Document Up to Date

Every time a new skill type is added, **update this file** with:

1. A row in the **Existing Skill Types** table below
2. A short description of what runtime component or method handles it
3. Any new fields added to `ActionDefinition`

This ensures the next person (or AI) adding a skill knows what already exists.

---

## Existing Skill Types Reference

### ActionType values

| Value | Standalone behaviour | Typical use |
|---|---|---|
| `Physical` | Basic melee attack resolved via `ResolveBasicAttack()` | Punch, Kick |
| `Elemental` | Applies `ActiveBuff` (flat bonus damage, N charges) | Hand Signs A/B/C |
| `Support` | Adds to `pendingPowerBoost` (+X% to next technique power) | Focus |
| `Movement` | Reserved — no standalone behaviour yet | Future dash/teleport |
| `OrbSummon` | Calls `OrbBuffHandler.Spawn()` — 3 orbs orbit the caster | Orb Strike trigger |

### TechniqueType values

| Value | Resolved by | Effect |
|---|---|---|
| `Attack` | `ResolveSkillAttack()` default path | Deals damage scaled by power + proficiency |
| `Heal` | `ResolveSkillAttack()` Heal branch | Restores HP to caster, syncs `HealthSystem` |
| `Buff` | `ApplyBuff()` | Adds `ActiveBuff` with charge-based bonus damage |
| `Debuff` | Not yet implemented | Reserved |
| `Utility` | Not yet implemented | Reserved |
| `Summon` | `BattleSummonManager.TrySummon()` | Spawns a guardian unit on the caster's team |
| `OrbSummon` | `ApplyOrbSummon()` in `BattleCombatResolver` | Spawns N orbiting `OrbProjectile` via `OrbBuffHandler` |

### CastType values

| Value | Unit behaviour during cast |
|---|---|
| `Melee` | Chases target to attack range, then executes |
| `Mobile` | Moves toward target while counting down cast timer |
| `Rooted` | Stops moving, faces target, counts down cast timer |

### Known ActionDefinition fields

| Field | Used by | Purpose |
|---|---|---|
| `actionId` | `ComboLibrary.TryMatch()` | Unique string ID for combo matching (lowercase, underscores) |
| `displayName` | Logs, UI | Human-readable name |
| `actionType` | `SkillSystem.GetTechniqueType()` | Determines technique category |
| `basePower` | `SkillSystem.SumBasePower()` | Raw power contribution before multipliers |
| `element` | `SkillSystem.GetDominantElement()` | Elemental affinity |
| `energyCost` | `PickBestSkill()`, `FireDamage()` | Energy required; 0 = free |
| `selfBuffDamage` | `ExecuteIndividualActions()` Elemental branch | Flat bonus damage per buff charge |
| `selfBuffCharges` | `ExecuteIndividualActions()` Elemental branch | Number of hits the buff lasts |
| `powerBoostPercent` | `ExecuteIndividualActions()` Support branch | Fraction added to `pendingPowerBoost` |
| `orbCount` | `ApplyOrbSummon()` | How many orbs to spawn |
| `orbDamage` | `OrbProjectile.OnArrival()` | Flat damage each orb deals on impact |

---

## Quick Checklist

### Adding a standard new combo
- [ ] Add `MakeRecipe(...)` to `SkillDataCreator.CreateComboLibrary()` (longer sequences first)
- [ ] Add matching `new ComboRecipe(...)` to `ComboLibrary.Recipes` (same order rule)
- [ ] **TacticalRPG → Create Combo Library** in Unity
- [ ] Assign the action sequence to the hero in TerrainBattleManager Inspector

### Adding a new action input
- [ ] Add `CreateAction(...)` to `SkillDataCreator.CreateActions()`
- [ ] **TacticalRPG → Recreate Action Definitions (force)** in Unity

### Adding a new skill type (new behaviour)
- [ ] Add value to `ActionType` in `Enums.cs` (if needed)
- [ ] Add value to `TechniqueType` in `Enums.cs`
- [ ] Add detection in `SkillSystem.GetTechniqueType()`
- [ ] Add resolver branch in `BattleCombatResolver.ResolveSkillAttack()`
- [ ] Write the private resolver method
- [ ] Add any new data fields to `ActionDefinition.cs`
- [ ] Add fields to `ActionData` struct and `CreateAction()` in `SkillDataCreator.cs`
- [ ] Add `CreateAction(...)` for any new action input
- [ ] Add `MakeRecipe(...)` to `SkillDataCreator.CreateComboLibrary()`
- [ ] Add matching `new ComboRecipe(...)` to `ComboLibrary.Recipes`
- [ ] **TacticalRPG → Recreate Action Definitions (force)** in Unity
- [ ] **TacticalRPG → Create Combo Library** in Unity
- [ ] Update **Existing Skill Types Reference** tables in this document
