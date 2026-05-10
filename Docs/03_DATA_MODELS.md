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
| `speedCost` | `float` | (Phase 4) Speed required to use standalone. `0` = free. |
| `speedGain` | `float` | (Phase 4) Speed granted on use (kinetic skills). `0` = none. |
| `speedScaling` | `float` | (Phase 4) Damage multiplier per 100 speed. `0` = no scaling. |
| `speedGate` | `float` | (Phase 4) Minimum current speed required to use. `0` = no gate. |
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
| `speedCost` | `float` | (Phase 4) Speed required to fire. Recipe value supersedes summed action costs. |
| `speedGain` | `float` | (Phase 4) Speed granted on fire. |
| `speedScaling` | `float` | (Phase 4) Damage multiplier per 100 speed. |
| `speedGate` | `float` | (Phase 4) Minimum caster speed required. |
| `ccType` | `CCEffectType` enum | (Phase 10) CC effect applied on landed strike. `None` = no CC. |
| `ccDuration` | `float` | (Phase 10) CC duration in seconds. |
| `ccChance` | `float` (0-1) | (Phase 10) Chance the CC applies on a landed strike. |
| `ccMagnitude` | `float` | (Phase 10) Magnitude (Slow uses this as the slow-strength factor; binary effects ignore). |
| `targetSpeedShatter` | `float` | (Phase 12) Flat speed drained from target on landed strike. |
| `targetSoftCapOverride` | `float` | (Phase 12) Target's soft cap is set to this value temporarily. |
| `casterSoftCapOverride` | `float` | (Phase 12) Caster's soft cap is set to this value temporarily. |
| `speedCapModifierDuration` | `float` | (Phase 12) Duration in seconds of any cap-modifier effects on this combo. |

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

### `StanceDefinition`

**Path:** `Assets/Scripts/DataModels/StanceDefinition.cs`
**Asset location:** `Assets/Data/Stances/` (created by `TacticalRPG → Create Default Stances (force)`)

(Phase 7) A combat preset bundling decision biases — speed-spend threshold, target priority, dodge willingness, movement preference. Seven default stances ship with the game; custom stances may be added later.

| Field | Type | Purpose |
|---|---|---|
| `id` | `StanceId` enum | One of seven canonical stances |
| `displayName` / `description` | `string` | UI surface |
| `behaviorBias` | `BehaviorType` enum | Aggressive / Balanced / Defensive — modulates `BattleAIBrain.ParamsFor` |
| `speedThresholdBigCombo` | `float` | Speed value above which the brain commits a Big Combo |
| `hpThresholdDisengage` | `float` | HP fraction below which the unit prefers disengage |
| `speedReserveFloor` | `float` | Speed below which the unit refuses to commit a combo (Sentinel-style hoarding) |
| `targetPriority` | `TargetPriority` enum | Overrides default nearest-enemy targeting |
| `preferredIntent` | `MovementIntent` enum | Default movement intent for this stance |
| `dodgeWillingnessAtLowSpeed` | `float` (0-1) | How readily the unit dodges at low speed |
| `engagementDelaySeconds` | `float` | Sentinel-style delay before advancing from `Backline` |
| `separationChanceModifier` | `float` | (H2H) Added to base separation chance after each exchange. Aggressive ≈ -0.2, Defensive ≈ +0.25 |
| `initiativeBonus` | `int` | (H2H) Added to the unit's initiative when entering an exchange. Aggressive +20, Defensive -20 |
| `pursuitAggression` | `float` | (H2H) Multiplier on chase distance willingness during Approach |

### `MoveDefinition`

**Path:** `Assets/Scripts/DataModels/MoveDefinition.cs`
**Asset location:** `Assets/Resources/Moves/` (loaded automatically at engine startup via `Resources.LoadAll`; the `Resources/` folder is required so the catalog can discover them)
**Created via:** `TacticalRPG → Combat → Create Tier 1 Move Catalog` editor menu (Tier 1 essentials); additional moves are authored as needed

The atomic unit of the move-based combat engine. Every unit, every tick, is executing one of these. One frame == 50ms (20Hz tick). See `Docs/Design/COMBAT_DESIGN.md` "Combat engine — move-based, frame-data driven" and `Docs/Design/MOVES_CATALOG.md` for the canonical move list.

| Field | Type | Purpose |
|---|---|---|
| `id` | `string` | Stable id; matches an entry in `MOVES_CATALOG.md` |
| `animationName` | `string` | Handle the engine passes to `UnitAnimationDriver.PlayMove` (defaults to id) |
| `category` | `MoveCategory` enum | Idle / Locomotion / LightAttack / HeavyAttack / Cast / Block / Dodge / HitReact / Death / etc. |
| `startupFrames` / `activeFrames` / `recoveryFrames` | `int` | Frame timeline; total frames = sum, total seconds = sum × 0.05 |
| `cancelWindowFrames` | `int` | Trailing frames of recovery during which a chained move can replace this one |
| `damage` | `int` | Damage on full hit; 0 for non-attacks |
| `range` | `float` | Reach in meters (cone tip at chest) |
| `angleDegrees` | `float` | Half-angle of the hit cone |
| `archetype` | `AttackArchetype` enum | Light / Heavy / Launch / Flurry / Sweep / Sign / GuardBreak |
| `reactionTag` | `ReactionTag` enum | What forced reaction this attack pairs with (drives `MoveReactionTable`) |
| `isAttack` | `bool` | True for moves whose active frames are a hitbox |
| `speedDamageScaling` | `float` | Damage scalar with current speed |
| `iFrameStart` / `iFrameEnd` | `int` | Within active phase, frames during which incoming hits whiff (dodges) |
| `superArmorFrames` | `int` | Total frames during which this unit ignores incoming hits |
| `isBlock` / `isParry` | `bool` | Defender resolution flags |
| `incomingDamageMultiplier` | `float` | Damage multiplier for hits taken while this move is in active phase (block reduction etc.) |
| `speedCost` | `float` | Speed debited on `StartMove` |
| `energyCost` | `float` | Energy debited on `StartMove` |
| `speedGate` | `float` | Brain may not pick this move below this current-speed value |
| `cancelIntoOnHit` | `List<MoveDefinition>` | Combo continuations on confirmed hit |
| `cancelIntoOnWhiff` | `List<MoveDefinition>` | Combo continuations on whiff |
| `forwardSpeedMetersPerSecond` | `float` | Per-second forward translation while the move plays (negative = backstep / knockback) |
| `lateralSpeedMetersPerSecond` | `float` | Per-second lateral translation |
| `forwardDisplacementCurve` / `verticalDisplacementCurve` | `AnimationCurve` | Optional curves; when non-empty, override forward speed for dashes / arcs |
| `facing` | `FacingPolicy` enum | FaceTarget / Lock / Free |
| `spawnsEntityPrefab` *(reserved)* | `GameObject` | Reserved slot for Earth Wall / Fire Zone etc. — see `Docs/Design/COMBAT_DESIGN.md` "Perception, world entities" |

Phase computation: `MoveDefinition.PhaseAtFrame(int)` returns `Startup / Active / Recovery / CancelWindow / Done` based on the timeline. The engine reads this each tick.

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
| `defaultStance` | `StanceDefinition` | Default H2H stance |
| `spottingRangeMeters` | `float` | (H2H) Detection range that triggers Spotting phase. Default 8 |
| `engagementRangeMeters` | `float` | (H2H) Range at which Engagement begins. Default 2 |
| `strikeRangeMeters` | `float` | (H2H) Range at which Exchange may begin. Default 1.5 |
| `separationDistanceMeters` | `float` | (H2H) Distance the unit must reach during Separation before re-engaging. Default 3 |
| `spottingMinTime` / `spottingMaxTime` | `float` | (H2H) Spotting delay range. Default 0.3-0.7 |
| `decisionLagMin` / `decisionLagMax` | `float` | (H2H) Decision lag during Engagement. Default 0.2-0.5 |
| `combatMovementSpeed` | `float` | (H2H) Speed during Engagement. Default 1.5 |
| `traversalSpeed` | `float` | (H2H) Speed during Approach. Default 6 |
| `disengageSpeed` | `float` | (H2H) Speed during Separation. Default 3.5 |
| `separationMinDuration` / `separationMaxDuration` | `float` | (H2H) Separation phase duration. Default 1-1.5 |

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

> **Per-unit state that intentionally does NOT live on `UnitRuntime`:** current Speed (owned by `BattleSpeedSystem`), active CC / status effects (owned by `BattleStatusEffectSystem`), Animancer playback ownership (owned by `BattleAnimancerDriver`'s registry), current move execution (owned by `BattleCombatEngine`'s `UnitMoveExecution` registry). Adding fields here for those concerns is the "five subsystems writing to UnitRuntime" anti-pattern flagged in `02_ARCHITECTURE.md`.

### `UnitMoveExecution`

**Path:** `Assets/Scripts/Systems/Combat/UnitMoveExecution.cs`

Runtime per-unit state owned by `BattleCombatEngine`. One instance per registered (engaged) unit. Created on register, mutated each tick, discarded on unregister. Plain C# class, not a `MonoBehaviour`.

| Field | Type | Purpose |
|---|---|---|
| `currentMove` | `MoveDefinition` | The move currently executing |
| `framesElapsed` | `int` | Tick count since `currentMove` started |
| `phase` | `MovePhase` enum | Recomputed each tick from currentMove + framesElapsed |
| `queuedNext` | `MoveDefinition` | Optional brain-pre-queued next move |
| `airborne` | `bool` | True while in launch/airborne reaction |
| `superArmorActive` | `bool` | True during a move's super-armor frames |
| `remainingIFrames` | `int` | Counted down each tick during active i-frame window |
| `lastActiveHitConfirmed` | `bool` | Set when this move's most-recent active hit landed; drives onHit-cancel chain |
| `cancelDecisionMade` | `bool` | Per-move latch so cancel-pick runs once inside the cancel window |
| `activeHitResolved` | `bool` | Per-move latch so multi-frame active windows don't multi-hit |
| `lockedFacing` | `Vector3` | World-space facing snapshot for `FacingPolicy.Lock` moves |

### `CombatEntity` *(planned — see `Docs/Design/COMBAT_DESIGN.md` "Perception, world entities, predictive reactions")*

Non-unit world objects that participate in combat: walls, hazards, summons, projectiles, traps, markers. Lives in a registry on `TerrainBattleManager` (planned `BattleEntityRegistry` subsystem). Has HP, position, collision properties, lifetime; takes damage; despawns when HP reaches 0 or lifetime expires.

Concrete subclasses (planned): `EarthWallEntity`, `IceWallEntity`, `FireZoneEntity`, `IceFloorEntity`, `ThunderPillarEntity`, `PoisonCloudEntity`, `GuardianSummonEntity`, `OrbSummonEntity`, `MarkEntity`, `TrapEntity`. Spawned by moves with `MoveDefinition.spawnsEntity` set (see `MOVES_CATALOG.md`).

| Field | Type | Purpose |
|---|---|---|
| `currentHP` / `maxHP` | `int` | Health, despawns at 0 |
| `owner` | `UnitRuntime` | Caster (null for environment-spawned) |
| `category` | `EntityCategory` enum | Wall / Hazard / Summon / Projectile / Trap / Marker |
| `blocksProjectiles` / `blocksMelee` / `blocksMovement` | `bool` | Geometric hit-resolution flags |
| `collisionBounds` | `Bounds` | World-space collision volume |
| `lifetimeSeconds` | `float` | Auto-despawn timer |

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
| `executionTime` | `float` | (Phase 11) Final execution time in seconds. Computed from base time × (10/SPD) × (1/proficiency) × speed-band modifier. `0` = use ability default. |
| `speedCost` | `float` | (Phase 4) Speed required to fire (recipe value or summed action costs) |
| `speedGain` | `float` | (Phase 4) Speed granted on fire |
| `speedScaling` | `float` | (Phase 4) Damage multiplier per 100 speed |
| `speedGate` | `float` | (Phase 4) Minimum caster speed required |
| `ccType` | `CCEffectType` enum | (Phase 10) CC applied on landed strike |
| `ccDuration` | `float` | (Phase 10) CC duration |
| `ccChance` | `float` | (Phase 10) Application chance (0-1) |
| `ccMagnitude` | `float` | (Phase 10) Effect magnitude (e.g. slow strength) |
| `targetSpeedShatter` | `float` | (Phase 12) Flat speed drained from target on landed strike |
| `targetSoftCapOverride` | `float` | (Phase 12) Temporary soft-cap drop on target (Destabilize) |
| `casterSoftCapOverride` | `float` | (Phase 12) Temporary soft-cap raise on caster (Flow State) |
| `speedCapModifierDuration` | `float` | (Phase 12) Duration of cap modifiers in seconds |

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
| `perception` *(planned)* | `int` | Frames-of-warning the brain reads for opponent intent. Higher = sees threats earlier, can prepare via `PickPreparation`. Default ~8. See `Docs/Design/COMBAT_DESIGN.md` "Perception, world entities, and predictive reactions". |

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

`Backline`, `Engage`, `Decide`, `Melee`, `CastMobile`, `CastRooted`, `AttackDash`, `Execute`, `Recover`, `Stagger`, `Dodging`, `Stunned`, `Repositioning`, `Dead`.

### `BehaviorType`

`Aggressive`, `Defensive`, `Balanced`. Future: `Skirmisher`, `Support`, `Assassin`.

### `SpeedBand`

(Phase 3) Discretized current-speed buckets used for visual modulation, AI thresholds, and execution timing.

| Value | Range |
|---|---|
| `Sluggish` | 0 – 20 |
| `Engaged` | 20 – 50 |
| `Sharp` | 50 – 70 |
| `Primed` | 70 – 100 |

### `MovementIntent`

(Phase 5) `Hold`, `Close`, `Circle`, `Disengage`, `Dash`. Per-intent speed-gain rates owned by `BattleMovementSystem`. The brain pushes intent on every state transition; `BattleSpeedSystem.ComputeDelta` reads the rate when the unit is moving.

### `StanceId`

(Phase 7) `Onslaught`, `Tempest`, `Stalwart`, `Tactician`, `Wraith`, `Sentinel`, `Conduit`. Each maps to a `StanceDefinition` asset under `Assets/Data/Stances/`, generated by `TacticalRPG → Create Default Stances (force)`.

### `TargetPriority`

(Phase 7) `Nearest`, `LowestHP`, `BacklineFirst`, `Furthest`, `AttackerOfAlly`, `Marked`. Set by stance; consumed by `BattleTargetFinder.GetTarget`. `Marked` is reserved (no marking system yet).

### `ExchangePhase`

(Phase 8) `None`, `Initiation`, `WindUp`, `StrikeSequence`, `Resolution`, `Beat`, `ReEvaluation`. Tracked per pair on `BattleExchangeCoordinator.ExchangeRecord`; advanced by `AdvancePhase`.

### `DefenderResponse`

(Phase 8) `Eat`, `Dodge`, `Block`, `Counter`. Resolver passes the per-strike response to `BattleExchangeCoordinator.OnStrikeResolved` for speed economy. `Counter` is reserved (defender → attacker role flip is design-tuning work).

### `CCEffectType`

(Phase 10) `None`, `Stun`, `Slow`, `Interrupt`, `Knockdown`, `Knockback`. The current `BattleStatusEffectSystem` end-to-end-implements `Stun` and `Slow`; `Knockback` is owned by the existing `BattleKnockbackSystem`; `Interrupt` and `Knockdown` are reserved enum values for later phases (Knockdown in particular needs ragdoll physics).

### `EntityCategory` *(planned)*

Tags non-unit world objects (`CombatEntity` instances). Drives hit-resolution and perception filtering. See `Docs/Design/COMBAT_DESIGN.md` "Perception, world entities, predictive reactions".

| Value | Examples |
|---|---|
| `Wall` | Earth Wall, Ice Wall, Stone Pillar — block projectiles / melee |
| `Hazard` | Fire Zone, Ice Floor, Thunder Pillar, Poison Cloud — area effects, tick damage / debuff |
| `Summon` | Guardian, orbs — ally-aligned units that can body-block and attack |
| `Projectile` | Triple Sign fireball, Orb Ray — in-flight ranged attacks (becomes an entity rather than a one-shot resolve) |
| `Trap` | Mines, runes — proximity-triggers on enemy entry |
| `Marker` | JJK-style flagged target — no collision, just data for ally moves to consume |

### `MoveCategory`

Top-level kind a `MoveDefinition` belongs to; matches the section headers in `MOVES_CATALOG.md`. Used by stance brains to filter the move pool.

`Idle`, `Locomotion`, `LightAttack`, `HeavyAttack`, `Cast`, `BigCast`, `EntitySpawn`, `Block`, `Dodge`, `Parry`, `HitReact`, `Knockdown`, `Stun`, `Finisher`, `MobilityAbility`, `Death`.

### `MovePhase`

Frame-window phase of a currently-executing move; computed by `MoveDefinition.PhaseAtFrame`.

`Startup` (committed, no hit yet) → `Active` (hitbox / i-frames / armor live) → `Recovery` (no hit, no defense) → `CancelWindow` (last N recovery frames; chained move accepted) → `Done`.

### `ReactionTag`

What forced reaction an attack invites in the defender. Drives `MoveReactionTable.PickForcedReaction`.

`None`, `LightHit`, `Heavy`, `Sweep`, `Launch`, `BigSign`, `Knockdown`, `GuardBreak`.

### `FacingPolicy`

How a move handles a unit's facing while playing.

`FaceTarget` (continuously rotate toward target — also drives target-aware locomotion direction in the engine), `Lock` (snapshot facing at move start, hold for the duration), `Free` (use unit's current rotation).

### `HitResolution`

Engine-internal categorization of one tick's hit-check outcome.

`OutOfRange`, `Whiff` (i-frames / parry / super-armor), `Blocked`, `FullHit`, `Trade` (both attacks active simultaneously).

### `AttackArchetype`

`Light`, `Heavy`, `Launch`, `Flurry`, `Sweep`, `Sign`, `GuardBreak`. Used by skill resolution and the brain's combo-archetype hint. Distinct from `ReactionTag` — archetype drives damage / scaling rules; tag drives the paired-reaction lookup.

### `H2HPhase`

Phase a unit occupies in the hand-to-hand cycle. Owned by `BattleH2HPhaseSystem`; transitions go through `TransitionPhase`. See `Docs/Design/HAND_TO_HAND_COMBAT.md` §6.

| Value | Meaning |
|---|---|
| `NotEngaged` | Out of combat, idle / patrol. Also where a dead unit is parked (with `H2HUnit.IsDead` true). |
| `Spotting` | Hostile detected; brief alert delay before pursuit |
| `Approaching` | Closing the gap at traversal speed |
| `Engaged` | In engagement range, circling at combat speed |
| `Exchange` | Active strike / defense — `BattleH2HOrchestrator` owns both units |
| `Separating` | Breaking off after exchange, creating distance |

### `H2HUnit.Combo` and `H2HUnit.ComboHit`

Per-unit attack data consumed by `BattleH2HOrchestrator`. A combo is a list of hits sharing one pre-position adjust + per-hit impact timing. See `Docs/Design/HAND_TO_HAND_COMBAT.md` §12 (multi-hit combos shipped row).

`Combo`:

| Field | Type | Purpose |
|---|---|---|
| `name` | `string` | Display name (BasicJab, JabHookUppercut, HeavyKick by default) |
| `hits` | `List<ComboHit>` | Ordered list of strikes |
| `minSpeed` / `minEnergy` | `float` | Resource gates — `H2HUnit.PickCombo` skips combos that fail these |
| `desiredImpactDistance` | `float` | Distance from defender at the FIRST impact frame; the orchestrator pre-positions the attacker to this distance |
| `positionAdjustDuration` | `float` | Seconds spent smoothstepping into impact distance before first strike |
| `interHitGap` | `float` | Time-gap (seconds) between consecutive hit impact frames |

`ComboHit`:

| Field | Type | Purpose |
|---|---|---|
| `attackId` | `string` | Library clip id played for this hit |
| `archetype` | `AttackArchetype` enum | Light / Heavy / etc. — drives defender response bias |
| `impactNormalized` | `float` (0-1) | Where in the clip the impact lands |
| `damage` | `int` | Damage on a clean (non-blocked, non-dodged) hit |
| `speedCost` | `float` | Speed debited from the attacker on commit of this hit |

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
TerrainBattleUnit legacy state machine handles Backline → Engage → Decide
  At first Decide → handoff to BattleCombatEngine (legacy state machine no-ops)

BattleCombatEngine ticks at 20Hz (50ms):
  For each engaged unit's UnitMoveExecution:
    Apply move's per-tick movement (target-aware basis)
    UpdatePhase(currentMove, framesElapsed)
    If Active && IsAttack: cone-check target → resolve hit
      → MoveReactionTable.PickForcedReaction → force defender's move
      → modifies target's UnitRuntime (HP)
    If imminent attacker in reactionLookahead window: ask brain.PickReaction
    If phase == CancelWindow: ask brain.PickCancel (once per move)
    Increment frame; if Done: ask brain.PickPreparation → fallback PickNeutral

Subsystems still active during engine control:
  BattleSpeedSystem ticks speed gain/drain from MovementIntent
  BattleStatusEffectSystem ticks active CC durations
  BattleAnimancerDriver / UnitAnimationDriver play move-name → clip lookup
  BattleEngagementManager / BattleMeleeTokenSystem still gate frontline slots

ScriptableObjects (MoveDefinition, ActionDefinition, etc.) are READ ONLY this entire time
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
