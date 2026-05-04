# Battle System Design

## Overview

Battles are fully automatic 3D engagements between two squads on a terrain map.  
Each unit runs an autonomous AI state machine. Players influence outcomes through pre-battle hero configuration (skill slot loadouts, action selection).

---

## Unit Combat State Machine

```
Backline --> Engage --> Decide --> [Melee | CastMobile | CastRooted] --> Execute --> Recover --> (loop to Decide)
                                                                                       ^
                                                                              Dodging (interrupts)
                                                                                       |
                                                                                     Dead
```

### States

| State | Behaviour |
|---|---|
| Backline | Waits for a frontline slot to open. Moves toward back of own side. Regen bonus: +10 energy/sec. |
| Engage | Acquires nearest enemy target. Immediately transitions to Decide (no range gate). |
| Decide | Picks best affordable skill. Resolves technique via SkillSystem. Checks summon guard. Determines CastType. |
| Melee | Chases target to attack range. Executes on arrival. |
| CastMobile | Moves toward target while charging. Executes at mid-range. |
| CastRooted | Roots in place, charges for 1 second, then executes. |
| Execute | Fires the resolved technique at target. |
| Recover | 1-second cooldown before next Decide cycle. |
| Dodging | Jumps back 3.5m in a parabolic arc (0.3s move + 0.5s pause). Returns to Decide. |
| Dead | Removed from frontline. Triggers backline promotion for the team. |

---

## Skill & Combo System

### Actions (Building Blocks)

Each unit has SkillSlots. Each SkillSlot holds an ordered sequence of ActionDefinitions.  
On use, the sequence is sent to SkillSystem.ResolveSkill().

| Action | Type | Element | Energy Cost | Effect |
|---|---|---|---|---|
| Punch | Physical | None | 0 | Basic melee attack |
| Kick | Physical | None | 0 | Slightly stronger melee |
| Hand Sign A | Elemental | Earth | 10 | +20 Earth dmg/hit self-buff, 10 charges |
| Hand Sign B | Elemental | Lightning | 10 | +20 Lightning dmg/hit self-buff, 10 charges |
| Hand Sign C | Elemental | Water | 10 | +20 Water dmg/hit self-buff, 10 charges |
| Focus | Support | None | 15 | +20% damage to next skill |

### Standalone Action Effects (no combo match)

- **Physical action** — resolves as a basic melee attack immediately.
- **Elemental action** — applies a charge-based self-buff (`ActiveBuff`). Adds flat elemental damage per hit for N charges.
- **Support action** — stores a `pendingPowerBoost` on `UnitRuntime` (+20%). Applied once to the next technique, then cleared.
- **OrbSummon action** — calls `OrbBuffHandler.Spawn()`. Spawns N orbiting `OrbProjectile` instances around the caster. Each subsequent punch (not kick) fires one orb at the enemy.

### Combo Resolution

SkillSystem.ResolveSkill() calls ComboLibrary.TryMatch(actionIds) first.  
If the exact sequence matches a recipe, the result is a named combo (isCombo = true) with its own:
- Technique name and description
- Power multiplier
- Element
- CastType (Melee / Mobile / Rooted)

If no match: each action fires its standalone effect sequentially.

### Combo Library (17 recipes)

#### 5-Action Combos
| Name | Sequence | Element | Power | Cast | Effect |
|---|---|---|---|---|---|
| Orb Strike | A > A > A > B > B | None | 1.0x | Rooted | Spawns 3 orbiting orbs — each punch fires one at the enemy |

#### 4-Action Combos
| Name | Sequence | Element | Power | Cast |
|---|---|---|---|---|
| Summoning | A > B > C > Focus | None | 3.0x | Rooted |
| Elemental Fist | A > B > C > Punch | Earth | 2.5x | Melee |
| Elemental Storm | A > B > C > Kick | Lightning | 2.5x | Melee |

#### 3-Action Combos
| Name | Sequence | Element | Power | Cast |
|---|---|---|---|---|
| Triple Sign | A > B > C | Fire | 2.0x | Rooted |

#### 2-Action Elemental Combos
| Name | Sequence | Element | Power | Cast |
|---|---|---|---|---|
| Geomagnetic | A > B | Earth | 1.4x | Mobile |
| Thunderstorm | B > C | Lightning | 1.4x | Mobile |
| Mudslide | A > C | Water | 1.4x | Mobile |

#### 2-Action Physical Combos
| Name | Sequence | Element | Power | Cast |
|---|---|---|---|---|
| Combo Strike | Punch > Kick | None | 1.3x | Melee |
| Power Strike | Focus > Punch | None | 1.5x | Melee |
| Crescent Kick | Focus > Kick | None | 2.0x | Melee |

#### Sign + Physical Combos
| Name | Sequence | Element | Power | Cast |
|---|---|---|---|---|
| Earth Fist | A > Punch | Earth | 1.2x | Melee |
| Thunder Fist | B > Punch | Lightning | 1.2x | Melee |
| Water Fist | C > Punch | Water | 1.2x | Melee |
| Tremor Kick | A > Kick | Earth | 1.3x | Melee |
| Thunder Sweep | B > Kick | Lightning | 1.3x | Melee |
| Tidal Sweep | C > Kick | Water | 1.3x | Melee |

See `Docs/SKILL_REFERENCE.md` for estimated damage values per hero tier.

---

## Energy System

- Max: 100 | Start: 50
- Regen: +15/sec while in frontline combat
- Backline bonus: +10/sec additional while in Backline state
- Costs: Physical = 0, Elemental signs = 10, Focus = 15
- SpendEnergy() called before Execute. If insufficient energy, skill is skipped and unit idles.
- PickBestSkill() selects the highest-cost skill the unit can currently afford.

---

## Engagement System

- Maximum 3 frontline units per side at any time.
- Additional units stay in Backline state.
- Each frame, Backline units call RequestFrontlineSlot() — blocked until battleStarted flag is true.
- When a frontline unit dies, OnUnitDied() fires and promotes the next backline unit.

---

## Dodge & Block

### Dodge
- Trigger: incoming attack, before damage
- Chance: SPD stat × 5% (e.g. SPD 10 = 50% chance)
- Cost: 10 energy
- Cooldown: 2 seconds between dodges
- Animation: parabolic jump-back arc, 3.5m distance (0.3s move + 0.5s pause)
- Result: attack misses entirely

### Block
- Trigger: incoming attack, checked if dodge fails
- Chance: DEF stat × 2% (e.g. DEF 15 = 30% chance)
- Effect: 50% damage reduction, +5 energy gained
- No cooldown, no movement

Both are checked in `BattleCombatResolver.ResolveBasicAttack()` and `ResolveSkillAttack()`.

---

## Summon System

- The Summoning combo (A > B > C > Focus, Rooted, 3.0x) deploys a guardian unit.
- Summons are pre-cast at the start of combat — Engage immediately transitions to Decide.
- HasActiveSummon(casterId) guard in UpdateDecide() prevents re-cast loop while a summon is alive.
- When no summon is alive, the unit returns to normal Decide cycling.

---

## Combat Subsystems

All subsystems are components on the `TerrainBattleManager` GameObject.

| Subsystem | Description |
|---|---|
| `BattleCombatResolver` | Executes resolved techniques — damage, buffs, orb spawning, and summons |
| `BattleExchangeCoordinator` | Assigns attacker/defender roles between unit pairs using initiative. `IsAnimating` lock prevents role swaps mid-animation. |
| `BattleMeleeTokenSystem` | Limits how many units can press one target (max 3). Excess units orbit. |
| `BattleEngagementManager` | Manages frontline slot availability and promotes backline units when a slot opens. |
| `BattleTargetFinder` | Nearest-enemy acquisition used by units entering the Engage state. |
| `BattleHitStopSystem` | Short freeze frames on hit — `Light` (basic), `Medium` (skill), `Heavy` (high damage). |
| `BattleKnockbackSystem` | Directional knockback and stagger. Suppressed when the target is blocking. |
| `BattleSummonManager` | Spawns and tracks guardian units for the Summoning combo. |
| `CombatLogger` | In-memory timestamped log. Press **L** at runtime to dump to console. Categories: `STATE`, `ROLE`, `ANIM`, `DMG`, `INIT`, `EXCHANGE`, `WARN`. |

---

## Orb System

- Triggered by the **Orb Strike** combo (A > A > A > B > B, Rooted, 1.0x).
- `BattleCombatResolver.ApplyOrbSummon()` calls `OrbBuffHandler.Spawn(caster, orbPrefab, count, damage)`.
- `OrbBuffHandler` instantiates N `OrbProjectile` GameObjects that orbit the caster at a fixed radius and height.
- Each time the caster lands a **punch** (not a kick), `OrbBuffHandler.TryConsumeOrb(target)` fires the next available orb.
- The orb flies to the target in a parabolic arc and applies flat damage on arrival via `OrbProjectile.OnArrival()`.
- When all orbs are consumed, `OrbBuffHandler` removes itself from the unit.
- Orb prefab must be assigned on the `BattleCombatResolver` component in the Inspector. The prefab must have an `OrbProjectile` component.

---

## Battle Flow

1. TerrainBattleManager.Start() — spawn units, call ComboLibrary.SetLibrary(comboLibraryAsset)
2. 3-second countdown displayed on screen (large OnGUI text)
3. battleStarted = true — units begin requesting frontline slots
4. State machine cycles: Engage → Decide → Cast/Melee → Execute → Recover → repeat
5. Win condition: all units on one side reach Dead state
6. Victory/defeat message shown

---

## Data Setup (Unity Editor)

Run these once (or after any change to action/combo data):

1. **TacticalRPG > Recreate Action Definitions (force)** — rebuilds `Assets/Data/Actions/` with all 7 actions
2. **TacticalRPG > Create Combo Library** — rebuilds `Assets/Data/Combos/` with 17 recipes + `ComboLibrary.asset`
3. In `TerrainBattleScene`, ensure **ComboLibrary.asset** is assigned to the **Combo Library** field on `TerrainBattleManager` — it is loaded automatically in `Start()` via `ComboLibrary.SetLibrary()`. If the field is empty the hardcoded fallback list is used instead.
4. Assign the **Orb Prefab** on the `BattleCombatResolver` component (must have an `OrbProjectile` component).

For full instructions on adding or modifying skills see `Docs/SKILL_CREATION_GUIDE.md`.

---

## CastType Reference

| CastType | Behaviour |
|---|---|
| Melee | Unit chases to melee range before executing |
| Mobile | Unit moves toward target while casting, executes at mid-range |
| Rooted | Unit stops moving, 1s charge, then executes from current position |
