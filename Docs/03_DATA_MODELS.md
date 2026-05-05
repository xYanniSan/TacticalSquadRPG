# 03_DATA_MODELS.md

> **Tier 1 — Living.** Update this doc in the same task as any change to live data structures.

---

## Purpose

This is the catalog of data structures used by the active 3D battle system. It is the canonical reference for what fields exist on what types and what role each plays.

If a structure described here doesn't match the code, **the code is right and this doc is stale** — fix the doc as part of the next relevant task.

Anything in `archive/SYSTEM_BATTLE_STATE.md` or `archive/DATA_MODELS.md` referencing `GridPosition`, `GridMap`, `BattleState` (as a class), `UnitIntent`, `IntentType` belongs to the legacy grid prototype and is **not used at runtime.** Don't reference it.

---

## Static data (ScriptableObjects)

Static data is loaded from `Assets/Data/` at battle start, read by systems, and **never modified during a battle.**

### `ActionDefinition`

**Path:** `Assets/Scripts/DataModels/ActionDefinition.cs`
**Asset location:** `Assets/Data/Actions/`
**Created via:** `TacticalRPG → Recreate Action Definitions (force)` editor menu

Represents one action input — Punch, Kick, Hand Sign A, Focus, etc. The atomic building block of skill chains.

| Field | Type | Purpose |
|---|---|---|
| `actionId` | `string` | Unique lowercase identifier used for combo matching (e.g., `handsign_a`). Must be unique across all actions. |
| `displayName` | `string` | Human-readable name for UI and logs |
| `actionType` | `ActionType` enum | Category — drives standalone effect when no combo matches |
| `basePower` | `float` | Raw power contribution before multipliers |
| `element` | `ElementType` enum | Elemental affinity |
| `energyCost` | `float` | Energy cost when this action is part of a fired skill (`0` for free) |
| `selfBuffDamage` | `int` | (Elemental) Flat bonus damage per buff charge |
| `selfBuffCharges` | `int` | (Elemental) Number of subsequent hits the buff lasts |
| `powerBoostPercent` | `float` | (Support) Fraction added to caster's `pendingPowerBoost` |
| `orbCount` | `int` | (OrbSummon) How many orbs to spawn |
| `orbDamage` | `int` | (OrbSummon) Flat damage each orb deals on impact |

### `ComboRecipeDefinition`

**Path:** `Assets/Scripts/DataModels/ComboRecipeDefinition.cs`
**Asset location:** `Assets/Data/Combos/`
**Created via:** `TacticalRPG → Create Combo Library` editor menu

A named combo recipe. The skill system checks the **first three actions** of a chain against these recipes (see `05_SKILL_SYSTEM.md`).

| Field | Type | Purpose |
|---|---|---|
| `recipeId` | `string` | Internal identifier |
| `displayName` | `string` | Skill name shown in logs and UI ("Earth Fist") |
| `description` | `string` | Short description |
| `actionIds` | `string[]` | The action ID sequence that triggers this combo |
| `techniqueType` | `TechniqueType` enum | What kind of effect the combo produces |
| `element` | `ElementType` enum | Element of the resulting skill |
| `powerMultiplier` | `float` | Multiplier applied to summed `basePower` of the trigger actions |
| `castType` | `CastType` enum | Movement behavior during cast |

### `ComboLibraryAsset`

**Path:** `Assets/Scripts/DataModels/ComboLibraryAsset.cs`
**Asset location:** `Assets/Data/Combos/ComboLibrary.asset`

Container holding all `ComboRecipeDefinition` assets. Assigned to `TerrainBattleManager` in the Inspector. If left empty at runtime, the hardcoded fallback list in `ComboLibrary.cs` is used instead.

### `AttackProfile`

**Path:** `Assets/Scripts/DataModels/AttackProfile.cs`
**Asset location:** `Assets/Data/Profiles/` (created on demand per skill)
**Created via:** `Assets → Create → TacticalRPG → Attack Profile`

Profile-driven configuration for a single skill animation. Source of truth for ranges, timing, root-motion policy, and the Animancer transition that plays the clip. Combat still owns damage, hit-stop tier, and knockback — the profile only describes presentation and positioning.

See `07_PRESENTATION.md` "Animation runtime (Animancer Pro)" for the integration rules.

| Field | Type | Purpose |
|---|---|---|
| `techniqueName` | `string` | Resolved technique name this profile services (e.g. `"Earth Fist"`). Matched against `ResolvedTechnique.techniqueName` for dispatch. |
| `transition` | `Animancer.TransitionAsset` | Animancer transition that plays the skill clip. Required for Animancer playback. |
| `impactEventName` | `Animancer.StringAsset` | Animancer named event fired at the impact frame. If null, the ability falls back to legacy clip `AnimationEvent`s forwarded by `UnitAnimationEventRelay`. |
| `minStartRange` / `idealStartRange` / `maxStartRange` | `float` | Range gating for the action. |
| `desiredImpactDistance` | `float` | Spacing the engagement-slot system should target. |
| `allowedAngleDegrees` | `float` | Max angle off-target the action accepts before re-aligning. |
| `requiresPreAlign` / `requiresEngagementSlot` / `canUseIfTooClose` | `bool` | Setup flags consulted before commit. |
| `movementMode` | `ActionMovementMode` enum | `InPlace`, `ShortLunge`, `ScriptedTravel`, `Dash`, or `Leap`. |
| `useRootMotion` / `lockMovementDuringCommit` / `lockRotationDuringImpact` | `bool` | Movement lock policy during the action. |
| `scriptedTravelDistance` | `float` | Distance for `ScriptedTravel`/`Dash`/`Leap` modes. |
| `causesKnockback` / `knockbackDistance` | `bool` / `float` | Outcome knockback the resolver applies on impact. |

### `UnitDefinition`

**Path:** `Assets/Scripts/DataModels/UnitDefinition.cs`
**Asset location:** `Assets/Data/Units/`

Static template for a hero or enemy.

| Field | Type | Purpose |
|---|---|---|
| `unitId` | `string` | Unique identifier (e.g., `hero_kai`) |
| `displayName` | `string` | Human-readable name |
| `portrait` | `Sprite` | UI portrait |
| `visualPrefab` | `GameObject` | Battle visualization prefab |
| `baseStats` | `StatBlock` | Base stats (see below) |
| `proficiencies` | `ProficiencySet` | Per-element / per-action proficiency multipliers |
| `defaultBehavior` | `BehaviorType` enum | Default AI archetype |

---

## Runtime data (plain C# classes)

Runtime data is created at battle start, modified during battle, and discarded at battle end. Never lives in a `ScriptableObject`.

### `UnitRuntime`

**Path:** `Assets/Scripts/DataModels/UnitRuntime.cs`

Live state for one unit during a battle. Holds the mutable data that `UnitDefinition` (static) cannot.

| Field | Type | Purpose |
|---|---|---|
| `definition` | `UnitDefinition` | Reference to the static template |
| `runtimeId` | `int` | Unique ID for this battle instance |
| `team` | `UnitTeam` enum | Player or Enemy |
| `currentHP` | `int` | Current health |
| `maxHP` | `int` | Max health (may differ from `definition.baseStats.maxHP` due to buffs) |
| `currentEnergy` | `float` | Current energy for skill casting |
| `currentStats` | `StatBlock` | Runtime-modified copy of base stats |
| `behavior` | `BehaviorLoadout` | Assigned AI archetype |
| `equippedSkills` | `List<SkillSlot>` | Configured skill slots |
| `activeBuffs` | `List<ActiveBuff>` | Charge-based buffs currently active |
| `pendingPowerBoost` | `float` | Stored Support-action bonus, applied to next technique |
| `isDead` | `bool` | Death flag |
| `visualInstance` | `GameObject` | Reference to the spawned `TerrainBattleUnit` GameObject |

### `SkillSlot`

A configured skill — an ordered chain of up to 5 actions (see `05_SKILL_SYSTEM.md` for the model).

| Field | Type | Purpose |
|---|---|---|
| `slotIndex` | `int` | 0–4, which slot on the hero |
| `actionSequence` | `List<ActionDefinition>` | Up to 5 actions in order |
| `slotName` | `string` | Optional player-assigned name |

### `ResolvedTechnique`

The output of `SkillSystem.ResolveSkill`. Describes what's actually about to fire.

| Field | Type | Purpose |
|---|---|---|
| `techniqueName` | `string` | Display name (combo name if matched, otherwise generic) |
| `type` | `TechniqueType` enum | What `BattleCombatResolver` does with it |
| `element` | `ElementType` enum | Resolved element |
| `power` | `float` | Calculated power before defense |
| `castType` | `CastType` enum | Movement during cast |
| `isCombo` | `bool` | Whether the chain matched a combo recipe |
| `sourceActions` | `List<ActionDefinition>` | The chain that produced this technique |

### `ActiveBuff`

A charge-based temporary buff. Decrements `remainingCharges` per hit and removes itself when it reaches 0.

| Field | Type | Purpose |
|---|---|---|
| `bonusDamage` | `int` | Flat damage added per hit while active |
| `element` | `ElementType` enum | Element of the bonus damage |
| `remainingCharges` | `int` | Hits left before the buff expires |
| `source` | `ActionDefinition` | The action that applied this buff (for debugging) |

### `BehaviorLoadout`

| Field | Type | Purpose |
|---|---|---|
| `behaviorType` | `BehaviorType` enum | Aggressive, Defensive, Balanced |

Future expansion: target priority, aggression threshold, retreat conditions.

---

## Shared structures

### `StatBlock`

Used in both `UnitDefinition` (base values) and `UnitRuntime` (current values).

| Field | Type | Purpose |
|---|---|---|
| `maxHP` | `int` | Max health |
| `attack` | `int` | Damage modifier |
| `defense` | `int` | Damage reduction modifier; also drives block chance |
| `moveSpeed` | `float` | Locomotion speed; also drives dodge chance |

### `ProficiencySet`

Per-unit multipliers applied during skill resolution.

| Field | Type | Purpose |
|---|---|---|
| `elementProficiencies` | `Dictionary<ElementType, float>` | Per-element damage multiplier (1.0 = neutral) |
| `actionTypeProficiencies` | `Dictionary<ActionType, float>` | Per-action-category multiplier |
| `techniqueTypeProficiencies` | `Dictionary<TechniqueType, float>` | Per-technique-category multiplier |

---

## Enums (all in `Enums.cs`)

All enums live in `Assets/Scripts/DataModels/Enums.cs` for discoverability.

### `ActionType`

Standalone behavior of an action when no combo matches:

| Value | Standalone behavior | Used by |
|---|---|---|
| `Physical` | Basic melee attack via `BattleCombatResolver.ResolveBasicAttack` | Punch, Kick |
| `Elemental` | Applies `ActiveBuff` (flat bonus damage, N charges) | Hand Signs A/B/C |
| `Support` | Adds to `pendingPowerBoost` | Focus |
| `Movement` | Reserved — no standalone behavior yet | (future dash/teleport) |
| `OrbSummon` | Calls `OrbBuffHandler.Spawn` | Orb-trigger actions |

### `TechniqueType`

What kind of effect a resolved combo produces:

| Value | Resolved by | Effect |
|---|---|---|
| `Attack` | `BattleCombatResolver.ResolveSkillAttack` (default path) | Damage scaled by power and proficiency |
| `Heal` | `BattleCombatResolver.ResolveSkillAttack` (Heal branch) | Restores HP to caster |
| `Buff` | `BattleCombatResolver.ApplyBuff` | Adds an `ActiveBuff` |
| `Debuff` | (not yet implemented) | Reserved |
| `Utility` | (not yet implemented) | Reserved |
| `Summon` | `BattleSummonManager.TrySummon` | Spawns a guardian unit |
| `OrbSummon` | `BattleCombatResolver.ApplyOrbSummon` | Spawns N orbiting `OrbProjectile` |
| `OrbRay` | `BattleOrbRaySystem.FireOrbRay` | Spawns N orbs that immediately fire instant rays at the target. Caster teleports if target is in melee range. |

### `CastType`

Movement behavior during a cast:

| Value | Behavior |
|---|---|
| `Melee` | Chase target to attack range, then execute |
| `Mobile` | Move toward target while charging; execute at mid-range |
| `Rooted` | Stop in place, charge for ~1s, then execute |

### `ActionMovementMode`

Movement mode for an `AttackProfile`. Profile-driven; set per skill.

| Value | Behavior |
|---|---|
| `InPlace` | No translation during the action |
| `ShortLunge` | Brief forward translation into the target |
| `ScriptedTravel` | Code-driven curve over a fixed distance |
| `Dash` | Fast straight-line traversal |
| `Leap` | Parabolic arc (e.g. flying kick) |

### `ElementType`

`None`, `Fire`, `Water`, `Earth`, `Lightning`, `Wind`. Reserved for future expansion: `Ice`, `Light`, `Dark`.

### `UnitTeam`

`Player`, `Enemy`.

### `UnitCombatState`

The per-unit AI state machine. See `04_BATTLE_SYSTEM.md` for the transition rules.

`Backline`, `Engage`, `Decide`, `Melee`, `CastMobile`, `CastRooted`, `Execute`, `Recover`, `Dodging`, `Dead`.

### `BehaviorType`

`Aggressive`, `Defensive`, `Balanced`. Future: `Skirmisher`, `Support`, `Assassin`.

---

## How the data fits together

**Pre-battle (configuration):**

```
Player chooses heroes → reads UnitDefinition assets
Player builds skill chains → references ActionDefinition assets
Loadouts saved into HeroLoadoutData (static bridge)
```

**Battle start (UnitRuntime creation):**

```
TerrainBattleManager reads HeroLoadoutData
For each hero:
  Creates UnitRuntime, copies baseStats, assigns SkillSlots
  Spawns visualPrefab as TerrainBattleUnit GameObject
  Wires UnitRuntime ↔ TerrainBattleUnit reference
```

**During battle (runtime mutation):**

```
TerrainBattleUnit state machine ticks
  Decide state → SkillSystem.PickBestSkill(unit)
              → SkillSystem.ResolveSkill(slot, caster) → ResolvedTechnique
  Execute state → BattleCombatResolver fires the technique
                → modifies target's UnitRuntime (HP, buffs)
                → modifies caster's UnitRuntime (energy spent)
ScriptableObjects are READ ONLY this entire time
```

**Battle end (cleanup):**

```
UnitRuntime instances discarded
TerrainBattleUnit GameObjects despawned
ScriptableObjects unchanged, ready for next battle
```

---

## Adding a new data field

When adding a field to a live data structure (`UnitRuntime`, `ActionDefinition`, etc.):

1. Add the field to the C# class
2. If it's on a `ScriptableObject` and used by `SkillDataCreator.cs`, update the `ActionData` struct and the `CreateAction` writer
3. **Update this doc** in the same task — add the row to the relevant table
4. If it's a runtime field on `UnitRuntime`, ensure it's initialized in `UnitRuntime` creation (typically in `TerrainBattleManager` spawn code)
5. If it's part of save data, also update `08_ROADMAP.md` save/load section

---

## Adding a new enum value

1. Add the value to the relevant enum in `Enums.cs`
2. Update the corresponding handler:
   - New `ActionType` → branch in standalone-effect dispatcher (`BattleCombatResolver.ExecuteIndividualActions`)
   - New `TechniqueType` → branch in `BattleCombatResolver.ResolveSkillAttack` and detection in `SkillSystem.GetTechniqueType`
   - New `CastType` → branch in `TerrainBattleUnit` state machine
   - New `UnitCombatState` → transitions in `TerrainBattleUnit` state machine
3. **Update this doc's enum table** in the same task
4. If the new value is reserved/not implemented yet, mark it explicitly in the table
