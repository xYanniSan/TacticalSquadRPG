# Skill Reference

> All damage values assume **ATK = 10**, proficiency multiplier = **1.0**.  
> Actual damage = `sum(basePower) × powerMultiplier × (ATK / 10) × proficiency`

---

## Actions (Building Blocks)

These are the raw inputs the player assigns to skill slots in the Hero Config menu.

| ID | Display Name | Type | Element | Base Power | Energy Cost | Standalone Effect |
|----|-------------|------|---------|-----------|-------------|-------------------|
| `punch` | Punch | Physical | None | 10 | 0 | Basic attack (melee range) |
| `kick` | Kick | Physical | None | 15 | 0 | Basic attack ×1.5 (melee range) |
| `handsign_a` | Hand Sign A | Elemental | Earth | 12 | 10 | Self-buff: **+20 Earth dmg** for **10 hits** |
| `handsign_b` | Hand Sign B | Elemental | Lightning | 12 | 10 | Self-buff: **+20 Lightning dmg** for **10 hits** |
| `handsign_c` | Hand Sign C | Elemental | Water | 12 | 10 | Self-buff: **+20 Water dmg** for **10 hits** |
| `focus` | Focus | Support | None | 8 | 15 | Self-buff: **+20% damage** on next skill |
| `orb_summon_b` | Orb Summon | OrbSummon | None | — | 0 | Combo trigger only — see Orb Strike |

> **Standalone** = no combo matched in the slot. Each action fires its individual effect instead.

---

## Combo Recipes

A combo fires when the exact action sequence in a skill slot matches a recipe.  
**Order matters.** `A → B` ≠ `B → A`.

### 5-Action Combos

| Slot | Skill Name | Element | ×Mult | Est. Dmg | Cast | Special |
|------|-----------|---------|-------|----------|------|---------|
| Sign A → Sign A → Sign A → Sign B → Sign B | **Orb Strike** | None | ×1.0 | — | **Rooted** | Spawns 3 orbiting orbs. Each punch fires one at the target. |

> **Orb Strike** requires the `orb_summon_b` action to be present in the sequence. Orb prefab must be assigned on `BattleCombatResolver`. Damage per orb is configured on the prefab.

---

### 2-Action Combos

#### Physical

| Slot | Skill Name | Element | ×Mult | Est. Dmg | Cast |
|------|-----------|---------|-------|----------|------|
| Punch → Kick | **Combo Strike** | None | ×1.3 | ~32 | Melee |
| Focus → Punch | **Power Strike** | None | ×1.5 | ~27 | Melee |
| Focus → Kick | **Crescent Kick** | None | ×2.0 | ~46 | Melee |

#### Sign + Physical

| Slot | Skill Name | Element | ×Mult | Est. Dmg | Cast |
|------|-----------|---------|-------|----------|------|
| Sign A → Punch | **Earth Fist** | Earth | ×1.2 | ~26 | Melee |
| Sign B → Punch | **Thunder Fist** | Lightning | ×1.2 | ~26 | Melee |
| Sign C → Punch | **Water Fist** | Water | ×1.2 | ~26 | Melee |
| Sign A → Kick | **Tremor Kick** | Earth | ×1.3 | ~35 | Melee |
| Sign B → Kick | **Thunder Sweep** | Lightning | ×1.3 | ~35 | Melee |
| Sign C → Kick | **Tidal Sweep** | Water | ×1.3 | ~35 | Melee |

#### Pure Elemental (cast while moving)

| Slot | Skill Name | Element | ×Mult | Est. Dmg | Cast |
|------|-----------|---------|-------|----------|------|
| Sign A → Sign B | **Geomagnetic** | Earth | ×1.4 | ~34 | Mobile |
| Sign B → Sign C | **Thunderstorm** | Lightning | ×1.4 | ~34 | Mobile |
| Sign A → Sign C | **Mudslide** | Water | ×1.4 | ~34 | Mobile |

---

### 3-Action Combos

| Slot | Skill Name | Element | ×Mult | Est. Dmg | Cast |
|------|-----------|---------|-------|----------|------|
| Sign A → Sign B → Sign C | **Triple Sign** | Fire | ×2.0 | ~72 | **Rooted** |

---

### 4-Action Combos

| Slot | Skill Name | Element | ×Mult | Est. Dmg | Cast |
|------|-----------|---------|-------|----------|------|
| Sign A → Sign B → Sign C → Punch | **Elemental Fist** | Earth | ×2.5 | ~115 | Melee |
| Sign A → Sign B → Sign C → Kick | **Elemental Storm** | Lightning | ×2.5 | ~127 | Melee |
| Sign A → Sign B → Sign C → Focus | **Summoning** | None | ×3.0 | — | **Rooted** |

> **Summoning** spawns a guardian near the caster. Cannot be re-cast while the summon lives.  
> Summon stats: **HP** = power×2 = **264**, **ATK** = power/2 = **66** (at ATK=10)

---

## Cast Types

| Type | Behaviour |
|------|-----------|
| **Melee** | Unit chases enemy until within `attackRange` (2.5u), then executes |
| **Mobile** | Unit casts while walking toward enemy — cast time = `actions × 0.3s` |
| **Rooted** | Unit stops in place and casts — cast time = `actions × 0.3s` |

---

## How to Configure in Unity

1. Go to **TacticalRPG → Create Combo Library** in the top menu
2. This creates `Assets/Data/Combos/` with one `.asset` per recipe + a `ComboLibrary.asset` container
3. Open any recipe in the Inspector to tweak `powerMultiplier`, `castType`, `element`, etc.
4. Drag **`ComboLibrary.asset`** into the **Combo Library** field on your **TerrainBattleManager** GameObject
5. If the field is left empty, the hardcoded built-in list is used as fallback

---

## Adding a New Skill or Combo

Use the **TacticalRPG** top menu — not manual asset creation:

1. **TacticalRPG → Recreate Action Definitions (force)** — rebuilds all `ActionDefinition` assets in `Assets/Data/Actions/`
2. **TacticalRPG → Create Combo Library** — rebuilds all recipes and `ComboLibrary.asset` in `Assets/Data/Combos/`
3. Assign `ComboLibrary.asset` to the **Combo Library** field on `TerrainBattleManager` (auto-loaded at start; falls back to hardcoded list if empty)

For code-level instructions — adding new action types, new technique behavior, or runtime effect extensions — see **`Docs/SKILL_CREATION_GUIDE.md`**.
