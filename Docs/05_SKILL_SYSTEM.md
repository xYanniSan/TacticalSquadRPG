# 05_SKILL_SYSTEM.md

> **Tier 1 — Living.** Update this doc in the same task as any change to actions, combo recipes, or the resolution pipeline.

---

## Purpose

The skill system is the game's **most identity-defining mechanic**. This doc covers the action-chain skill model, the resolution pipeline, all action definitions, all combo recipes, and the workflow for adding new skills.

For data structure details see `03_DATA_MODELS.md`. For how skills are fired during combat see `04_BATTLE_SYSTEM.md`.

---

## The skill model

### Concept

Heroes don't have flat pre-defined abilities. Instead, players configure **action chains** in skill slots, and the chain resolves into a technique at runtime.

- Each hero has up to **5 skill slots**.
- Each skill slot holds up to **5 actions** in order.
- The first **3 actions** form a **combo trigger** that may match a recipe to fire a named skill.
- The remaining **2 actions** are **tail actions** — they don't change which skill fires; they apply their own standalone bonuses.

### Target design vs current implementation

> **⚠️ Important:** the model below is the **target design.** The current implementation matches the **full action sequence** against combo recipes (so a 5-action chain like `A → A → A → B → B` is itself a 5-action recipe). The 3+2 split has not yet been implemented.
>
> **When extending skill code, build toward the 3+2 model.** When adding combo data right now, follow the existing 2/3/4/5-action recipe pattern — but be aware that recipes will eventually be normalized to 3-action triggers with the tail interpreted separately.
>
> Migration scope (when scheduled):
> - `ComboLibrary.TryMatch` should match the first 3 action IDs of the chain only
> - `SkillSystem.ResolveSkill` should resolve the combo from the first 3, then layer standalone effects from actions 4 and 5
> - Existing 4-action and 5-action recipes need to be re-expressed as 3-action triggers + tail bonuses
> - The 2-action recipes need a design decision: keep them as a special short-chain case, or require padding to 3
>
> Document this migration in the same PR that performs it. Update this section to reflect the new behavior.

### Why this model

- **Player creativity.** A player who configures Earth Fist (`A → Punch`) with two extra Hand Sign A tail actions is layering elemental buffs *and* a power skill. That's expressive without requiring new combo recipes.
- **Authoring scalability.** Combo recipes are bounded — only 3-action triggers need to be designed. Tail variations multiply expressiveness without multiplying recipes.
- **Discoverability.** Players learn 3-action shapes (sign + sign + physical, three signs, etc.) and tail actions are intuitive add-ons.

---

## Resolution pipeline

When a unit's `Decide` state picks a skill slot, this is the flow:

```
unit.equippedSkills[i]                       (the skill slot the unit picked)
   │
   ▼
SkillSystem.PickBestSkill(unit)              (highest-cost affordable slot)
   │
   ▼
SkillSystem.ResolveSkill(slot, caster)
   │
   ├─► ComboLibrary.TryMatch(actionIds[0..2])  (target: first 3 only)
   │      ├─ match    → ResolvedTechnique with isCombo=true,
   │      │             techniqueName from recipe,
   │      │             type/element/power/cast from recipe
   │      └─ no match → standalone resolution (each action's own effect)
   │
   ├─► Apply caster proficiencies to power
   │
   ├─► (Target design) Layer standalone effects from tail actions [3..4]
   │
   └─► return ResolvedTechnique
   │
   ▼
TerrainBattleUnit reads ResolvedTechnique.castType → state transition
   │
   ▼
Execute state → BattleCombatResolver fires the technique
```

### Combo matching

`ComboLibrary.TryMatch(string[] actionIds)` checks the action ID array against all known recipes and returns the first match.

In the current implementation, recipes have varying lengths (2, 3, 4, 5 actions) and are matched with **longer recipes checked first** so a 5-action chain doesn't accidentally match a shorter prefix.

In the target design, all combos resolve from a 3-action trigger; longer chains add tail bonuses.

### Standalone action effects

When no combo matches (or when processing tail actions in the target design), each action fires its own standalone effect:

| Action type | Standalone effect | Implementation |
|---|---|---|
| `Physical` | Basic melee attack | `BattleCombatResolver.ResolveBasicAttack` |
| `Elemental` | Apply `ActiveBuff` (flat bonus damage per hit, N charges) | `ExecuteIndividualActions` Elemental branch |
| `Support` | Add to caster's `pendingPowerBoost` (% bonus to next technique) | `ExecuteIndividualActions` Support branch |
| `OrbSummon` | Spawn orbs around caster | `OrbBuffHandler.Spawn` |
| `Movement` | Reserved — no standalone effect yet | (future dash/teleport) |

### Proficiency application

After base power is computed, the caster's `ProficiencySet` multipliers apply:

- Element proficiency (per `ElementType`)
- Action type proficiency (per `ActionType`)
- Technique type proficiency (per `TechniqueType`)

Proficiencies default to 1.0 (neutral). Higher values mean the hero is naturally better with that element/category.

### `pendingPowerBoost`

When a Support action (Focus) fires standalone, it stores a percentage on `UnitRuntime.pendingPowerBoost`. The next technique the unit fires consumes this boost as a multiplier on its power, then clears the field.

This is what makes `Focus → Punch` (Power Strike) and `Focus → Kick` (Crescent Kick) interact correctly with the next action.

---

## Action catalog

These are the action inputs currently defined as `ActionDefinition` assets in `Assets/Data/Actions/`.

| ID | Display | Type | Element | Base Power | Energy | Standalone effect |
|---|---|---|---|---|---|---|
| `punch` | Punch | Physical | None | 10 | 0 | Basic melee attack |
| `kick` | Kick | Physical | None | 15 | 0 | Basic melee attack (×1.5 damage) |
| `handsign_a` | Hand Sign A | Elemental | Earth | 12 | 10 | Buff: +20 Earth dmg for 10 hits |
| `handsign_b` | Hand Sign B | Elemental | Lightning | 12 | 10 | Buff: +20 Lightning dmg for 10 hits |
| `handsign_c` | Hand Sign C | Elemental | Water | 12 | 10 | Buff: +20 Water dmg for 10 hits |
| `focus` | Focus | Support | None | 8 | 15 | +20% power to next technique |
| `orb_summon_b` | Orb Summon | OrbSummon | None | — | 0 | Combo trigger only — see Orb Strike |

Standalone behavior fires when no combo matched (or in tail position under the target design).

---

## Combo catalog

Damage estimates assume **ATK = 10**, proficiency = **1.0**.
**Formula:** `final = sum(basePower) × powerMultiplier × (ATK / 10) × proficiency`

### 5-action combos

| Sequence | Skill | Element | Mult | Cast | Notes |
|---|---|---|---|---|---|
| A → A → A → B → B | **Orb Strike** | None | 1.0× | Rooted | Spawns 3 orbiting orbs. Each subsequent **punch** (not kick) fires one at the target. Orb damage configured on prefab. |
| A → A → A → A → B | **Orb Ray** | None | 1.0× | Rooted | Spawns 3 orbs that **immediately** fire instant rays at the nearest enemy (unlimited range). If the caster is within ~3u of the target at cast time, teleports ~20m in a random horizontal direction first. Damage per ray currently fixed at 15. Handled by `BattleOrbRaySystem`. |

### 4-action combos

| Sequence | Skill | Element | Mult | Cast |
|---|---|---|---|---|
| A → B → C → Focus | **Summoning** | None | 3.0× | Rooted |
| A → B → C → Punch | **Elemental Fist** | Earth | 2.5× | Melee |
| A → B → C → Kick | **Elemental Storm** | Lightning | 2.5× | Melee |

> **Summoning** spawns a guardian unit (HP = power × 2, ATK = power / 2). Cannot re-cast while the guardian is alive (see `04_BATTLE_SYSTEM.md` Summon System).

### 3-action combos

| Sequence | Skill | Element | Mult | Cast |
|---|---|---|---|---|
| A → B → C | **Triple Sign** | Fire | 2.0× | Rooted |

### 2-action combos — pure elemental

| Sequence | Skill | Element | Mult | Cast |
|---|---|---|---|---|
| A → B | **Geomagnetic** | Earth | 1.4× | Mobile |
| B → C | **Thunderstorm** | Lightning | 1.4× | Mobile |
| A → C | **Mudslide** | Water | 1.4× | Mobile |

### 2-action combos — sign + physical

| Sequence | Skill | Element | Mult | Cast |
|---|---|---|---|---|
| A → Punch | **Earth Fist** | Earth | 1.2× | Melee |
| B → Punch | **Thunder Fist** | Lightning | 1.2× | Melee |
| C → Punch | **Water Fist** | Water | 1.2× | Melee |
| A → Kick | **Tremor Kick** | Earth | 1.3× | Melee |
| B → Kick | **Thunder Sweep** | Lightning | 1.3× | Melee |
| C → Kick | **Tidal Sweep** | Water | 1.3× | Melee |

### 2-action combos — pure physical

| Sequence | Skill | Element | Mult | Cast |
|---|---|---|---|---|
| Punch → Kick | **Combo Strike** | None | 1.3× | Melee |
| Focus → Punch | **Power Strike** | None | 1.5× | Melee |
| Focus → Kick | **Crescent Kick** | None | 2.0× | Melee |

---

## Cast types

| Cast | Movement during cast | Cast time |
|---|---|---|
| `Melee` | Chase target until in attack range, then execute | Immediate on arrival |
| `Mobile` | Move toward target while charging | actions × 0.3s |
| `Rooted` | Stop in place, face target, charge | actions × 0.3s (~1s for typical combos) |

---

## Editor workflow

All skill data is generated by editor menus. Don't manually create assets in `Assets/Data/Actions/` or `Assets/Data/Combos/` — use the menus so the registry stays consistent.

| Menu | Effect |
|---|---|
| `TacticalRPG → Create Action Definitions` | Generates missing `ActionDefinition` assets |
| `TacticalRPG → Recreate Action Definitions (force)` | Deletes and rebuilds all action assets |
| `TacticalRPG → Create Combo Library` | Generates all `ComboRecipeDefinition` assets and rebuilds `ComboLibrary.asset` |

After running these, verify in Unity that `TerrainBattleManager.comboLibrary` references the correct `ComboLibrary.asset`. If empty, the hardcoded fallback list in `Assets/Scripts/DataModels/ComboLibrary.cs` is used.

---

## Adding a new combo (no new code needed)

If the combo uses existing action types and existing technique types, this is a data-only task.

1. **Decide the spec:**
   - Action sequence (must use existing action IDs)
   - Skill name and description
   - `TechniqueType` (existing only — Attack, Heal, Buff, Summon, OrbSummon)
   - `ElementType`
   - Power multiplier
   - `CastType`

2. **Add to `SkillDataCreator.cs`** — find `CreateComboLibrary()` and add a `MakeRecipe(...)` call. **Longer sequences must be listed before shorter ones** so the matcher doesn't bail on a prefix.

   ```csharp
   recipeAssets.Add(MakeRecipe("Combo_MyNewSkill",
       "My New Skill", "Short description.",
       new[] { "handsign_a", "handsign_a", "punch" },
       TechniqueType.Attack, ElementType.Earth, 1.8f, CastType.Melee));
   ```

3. **Add the same recipe to `ComboLibrary.cs`** in the hardcoded `Recipes` list (same order rule):

   ```csharp
   new ComboRecipe(
       new[] { "handsign_a", "handsign_a", "punch" },
       "My New Skill", TechniqueType.Attack, ElementType.Earth, 1.8f, CastType.Melee),
   ```

4. **Run** `TacticalRPG → Create Combo Library` in Unity.

5. **Update this doc** — add the new row to the appropriate combo table above.

---

## Adding a new action input (no new behavior)

If the new action uses an existing `ActionType` (Physical, Elemental, Support, OrbSummon) and an existing standalone effect path, this is also a data-only task.

1. **Add to `SkillDataCreator.cs`** — find `CreateActions()` and add a `CreateAction(...)` call:

   ```csharp
   CreateAction("HandSignD", new ActionData
   {
       actionId         = "handsign_d",
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

2. **Run** `TacticalRPG → Recreate Action Definitions (force)` in Unity.

3. **Update this doc** — add the row to the action catalog table.

---

## Adding a skill that needs new behavior

When a skill does something that no existing `TechniqueType` handles (a new effect category like a shield, an enemy root, an AoE zone), code changes are required.

This pattern is **the same pattern that the Orb Strike system follows** — read `OrbBuffHandler` and `BattleCombatResolver.ApplyOrbSummon` as the reference example.

### Step 1: Decide whether you need a new `ActionType`

Only add a new `ActionType` if the action itself has a **standalone behavior** that doesn't fit existing types. If the new mechanic only happens when a combo fires (no standalone effect), you don't need a new `ActionType`.

Edit `Assets/Scripts/DataModels/Enums.cs`:

```csharp
public enum ActionType
{
    Physical,
    Elemental,
    Support,
    Movement,
    OrbSummon,
    YourNewType
}
```

### Step 2: Add a new `TechniqueType`

This is what the combo will resolve to. Edit the same file:

```csharp
public enum TechniqueType
{
    Attack,
    Heal,
    Buff,
    Debuff,
    Utility,
    Summon,
    OrbSummon,
    YourNewType
}
```

### Step 3: Tell `SkillSystem` how to detect the new type

Open `Assets/Scripts/Systems/SkillSystem.cs`, find `GetTechniqueType()`. Add detection logic before the existing checks:

```csharp
foreach (var action in actions)
    if (action.actionType == ActionType.YourNewType)
        return TechniqueType.YourNewType;
```

### Step 4: Add the resolver branch

Open `Assets/Scripts/ThirdPerson/BattleCombatResolver.cs`, find `ResolveSkillAttack()`. Add an early-return branch:

```csharp
if (tech.type == TechniqueType.YourNewType)
{
    ApplyYourNewSkill(attacker, tech);
    return;
}
```

Then implement `ApplyYourNewSkill` as a private method.

> **If the new behavior has its own state lifecycle** (something starts, ticks, ends — like the orb system does), **do not** put all that logic in `BattleCombatResolver`. Create a dedicated subsystem MonoBehaviour following the pattern of `OrbBuffHandler`. See `02_ARCHITECTURE.md`.

### Step 5: Add data fields if needed

If the skill needs configuration (orb count, shield duration, AOE radius), add fields to `ActionDefinition.cs` under a new header:

```csharp
[Header("Your New Skill")]
public int yourNewField = 0;
```

Mirror them in the `ActionData` struct in `SkillDataCreator.cs` and write them in `CreateAction()`.

### Step 6: Add the recipe

Follow the standard combo-adding flow above, using your new `TechniqueType`.

### Step 7: Update this doc

- Add the new `ActionType` value, if any, to the standalone-effect table
- Add the new `TechniqueType` value to the technique catalog (in `03_DATA_MODELS.md` and below)
- Add the new combo to the recipe tables above
- Add any new `ActionDefinition` fields to `03_DATA_MODELS.md`

---

## TechniqueType handler reference

| TechniqueType | Handler | Status |
|---|---|---|
| `Attack` | `BattleCombatResolver.ResolveSkillAttack` (default path) | Implemented |
| `Heal` | `BattleCombatResolver.ResolveSkillAttack` (Heal branch) | Implemented |
| `Buff` | `BattleCombatResolver.ApplyBuff` | Implemented |
| `Debuff` | (not yet implemented) | Reserved |
| `Utility` | (not yet implemented) | Reserved |
| `Summon` | `BattleSummonManager.TrySummon` | Implemented |
| `OrbSummon` | `BattleCombatResolver.ApplyOrbSummon` → `OrbBuffHandler.Spawn` | Implemented |
| `OrbRay` | `BattleCombatResolver` → `BattleOrbRaySystem.FireOrbRay` → `OrbProjectile.FireRay` | Implemented |

---

## Quick reference checklists

### New combo (existing types only)

- [ ] Add `MakeRecipe` to `SkillDataCreator.CreateComboLibrary()` (longer first)
- [ ] Add `new ComboRecipe(...)` to `ComboLibrary.Recipes` (same order)
- [ ] Run `TacticalRPG → Create Combo Library`
- [ ] **Update this doc's combo catalog**

### New action input (existing types only)

- [ ] Add `CreateAction` to `SkillDataCreator.CreateActions()`
- [ ] Run `TacticalRPG → Recreate Action Definitions (force)`
- [ ] **Update this doc's action catalog**

### New skill type (new behavior)

- [ ] Add `ActionType` value to `Enums.cs` (only if standalone effect needed)
- [ ] Add `TechniqueType` value to `Enums.cs`
- [ ] Add detection in `SkillSystem.GetTechniqueType`
- [ ] Add resolver branch in `BattleCombatResolver.ResolveSkillAttack`
- [ ] Implement the resolver method (or, if state-driven, a new subsystem MonoBehaviour)
- [ ] Add new `ActionDefinition` fields if needed
- [ ] Mirror in `SkillDataCreator.ActionData` and `CreateAction()`
- [ ] Add new actions and combos via the menus
- [ ] Run `TacticalRPG → Recreate Action Definitions (force)`
- [ ] Run `TacticalRPG → Create Combo Library`
- [ ] **Update this doc's TechniqueType handler table, action catalog, combo catalog**
- [ ] **Update `03_DATA_MODELS.md` for any new enum values or fields**
