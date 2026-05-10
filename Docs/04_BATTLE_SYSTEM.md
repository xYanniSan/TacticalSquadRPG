# 04_BATTLE_SYSTEM.md

> **Tier 1 — Living.** Update this doc in the same task as any change to the combat loop, subsystems, state machine, or win conditions.

---

## Purpose

This is the canonical description of how combat actually works. It covers the per-unit state machine, all combat subsystems, the energy system, dodge/block mechanics, movement during combat, and win conditions.

For data structures referenced here, see `03_DATA_MODELS.md`. For the skill resolution pipeline (combos, technique types, action effects), see `05_SKILL_SYSTEM.md`.

---

## High-level loop

A battle plays out in `TerrainBattleScene` with these phases:

1. **Spawn.** `TerrainBattleManager.Start()` instantiates units from `HeroLoadoutData`, wires subsystems, and loads the assigned `ComboLibraryAsset`.
2. **Countdown.** A 3-second OnGUI countdown displays. `battleStarted` is `false`. Units cannot request frontline slots yet.
3. **Battle.** `battleStarted = true`. Each unit's legacy state machine handles `Backline → Engage → Decide`, then **hands off to the move-based combat engine** which drives all combat at 20Hz from that point.
4. **End.** When all units on one side reach `Dead`, `TerrainBattleManager` displays the victory/defeat message; the engine stops ticking.

---

## Move-based combat engine

The 20Hz move engine — `BattleCombatEngine` MonoBehaviour subsystem on `TerrainBattleManager` — is the **active combat loop**. The legacy state machine (Engage / Melee / Cast / Execute / Recover / etc., described later in this doc) is preserved for **backline + engagement promotion only**; the moment a unit transitions to `Decide` for the first time, control hands off to the engine.

### What the engine owns

- **Tick.** Fixed 50ms (20Hz) tick via accumulator in `Update`. Each tick, every engaged unit advances one frame of its current move.
- **Per-unit state.** `UnitMoveExecution` runtime object: current `MoveDefinition`, frame counter, phase (Startup / Active / Recovery / CancelWindow / Done), queued next move, super-armor, i-frame counter, locked facing.
- **Move catalog.** `MoveCatalog` (built from `Resources/Moves/` plus Inspector-assigned `MoveDefinition` lists). Brains and reactions resolve moves by string id (`"attack_punch_jab"`, `"react_hit_light"`, etc., per `Docs/Design/MOVES_CATALOG.md`).
- **Hit resolution.** During a move's active frames, the engine cone-checks against the target, reads the defender's `UnitMoveExecution.phase`, and produces one of `OutOfRange / Whiff / Blocked / FullHit / Trade`. On full hit, damage applies and the defender is force-fed the paired-reaction move (`MoveReactionTable.PickForcedReaction`).
- **Movement.** Each tick the engine integrates `forwardSpeedMetersPerSecond × tickSeconds` along the **target-aware basis** (FaceTarget = direction-toward-target so chase doesn't curve into orbit, Lock = saved facing, Free = unit forward). It also pushes a `MovementIntent` to `BattleMovementSystem` so `BattleSpeedSystem` can credit speed gain.
- **Stop-short on locomotion.** When a forward locomotion move closes inside attack reach (~1.6u), the engine ends it that tick so the brain re-picks at Locked range.
- **Animation.** On move start, `UnitAnimationDriver.PlayMove(animationName)` fires the matching animator trigger or no-ops when missing — combat continues without animations. Repeat-locomotion calls are suppressed so the locomotion blend tree isn't reset every 200ms.
- **Battle-end gate.** When `TerrainBattleManager.IsBattleOver` is true, `Update` early-exits — no further ticks, no log spam.

### What the brain layer owns

`IStanceBrain` (one stateless instance per stance, registered in `StanceBrainRegistry`):

| Hook | When called | Returns |
|---|---|---|
| `PickNeutral(BrainContext)` | Current move ended; need a fresh pick | A locomotion / attack / cast move from the stance's pool |
| `PickReaction(MoveDefinition incoming, BrainContext)` | Opponent's attack will reach contact within `reactionLookahead` frames | A defensive move (block / dodge / parry / fade), or null to "eat the hit" |
| `PickPreparation(BrainContext)` | Every tick in neutral | A preemptive move (Earth Wall, interpose, interrupt) — reserved for the perception layer; default returns null |
| `PickCancel(MoveDefinition current, BrainContext)` | Current move enters its cancel window | A chained move from `cancelIntoOnHit` (if hit confirmed) or `cancelIntoOnWhiff`, or null |

Seven stance brains are implemented: Onslaught, Tempest, Stalwart, Tactician, Wraith, Sentinel, Conduit. They differ in neutral / cancel / reaction picks, producing visibly distinct fights with the same loadout.

### How the legacy state machine and the engine cohabit

- The legacy `UnitBrainAI.Tick` continues to handle energy regen, target acquisition (used for stance-priority resolution), and `Backline → Engage → Decide`.
- At the first `Decide` after engagement, `UnitBrainAI.HandoffOrDecide` sets `EngineControlled = true` on the brain and registers the unit with `BattleCombatEngine`.
- From that point: `UnitBrainAI.Tick` early-exits past the energy regen — no state-machine update, no chase code, no Execute / Recover / Stagger handlers run for that unit.
- The legacy `CombatState` field stays at `Decide` for engine-controlled units. UI overlays / external systems that read `CombatState` will see `Decide` indefinitely; treat that as "engine controls this unit."
- The toggle `TerrainBattleManager.useMoveBasedEngine` (default `true`) gates whether handoff happens. With it off, the legacy state machine runs as it did before the engine landed.

### Resource integration

- **Speed.** Each move's `speedCost` is debited via `BattleSpeedSystem.SpendSpeed` on `StartMove`. Movement intent is set per-tick from the move's `forwardSpeedMetersPerSecond` (`Close / Disengage / Circle / Hold / Dash`), so `BattleSpeedSystem` ticks gain rates appropriately.
- **Energy.** Move's `energyCost` is debited via `UnitRuntime.SpendEnergy` on `StartMove`. Backline regen bonus still flows through the legacy brain.
- **HP.** Unchanged — `TerrainBattleUnit.ApplyDamage` is still the single sink for damage.

### Determinism note

Per `Docs/Design/COMBAT_DESIGN.md` "Determinism for replay", the engine's hooks read state and return a `MoveDefinition`. Any randomness in brain decisions must route through a per-battle seeded RNG (not yet wired — current brains use frame-parity and unit IDs as a deterministic substitute for true randomness).

---

## The legacy per-unit state machine (engagement only)

> When `useMoveBasedEngine` is on (default), this state machine runs only up to the first `Decide` — at that point control hands off to the move engine and the rest of the diagram below stops driving the unit. The diagram is preserved for the engagement-promotion / countdown path that's still active, and as a fallback when the engine is toggled off. See "Move-based combat engine" above for what runs after handoff.

Every `TerrainBattleUnit` runs an autonomous state machine. The states are defined in `UnitCombatState` (see `03_DATA_MODELS.md`).

```
                    ┌──────────────┐
                    │   Backline   │  Waiting for frontline slot
                    └──────┬───────┘
                           │ slot opens
                           ▼
                    ┌──────────────┐
                    │   Engage     │  Acquire nearest enemy
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
        ┌──────────►│   Decide     │  Pick best affordable skill,
        │           │              │  resolve technique, choose CastType
        │           └──┬─────┬─────┘
        │              │     │
        │      ┌───────┘     └───────┐
        │      ▼         ▼           ▼
        │ ┌────────┐ ┌─────────┐ ┌─────────────┐
        │ │ Melee  │ │CastMobile│ │ CastRooted  │
        │ └───┬────┘ └────┬────┘ └──────┬──────┘
        │     │           │             │
        │     └───────────┼─────────────┘
        │                 ▼
        │          ┌──────────────┐
        │          │   Execute    │  Fire the technique
        │          └──────┬───────┘
        │                 │
        │                 ▼
        │          ┌──────────────┐
        └──────────│   Recover    │  ~1s cooldown
                   └──────────────┘

                   ┌──────────────┐    ◄── interrupts any state
                   │   Dodging    │        (returns to Decide on completion)
                   └──────────────┘

                   ┌──────────────┐    ◄── terminal
                   │     Dead     │        (triggers backline promotion)
                   └──────────────┘
```

### State responsibilities

| State | Behavior |
|---|---|
| `Backline` | Holds back of own team's side. Regen bonus: +10 energy/sec on top of base regen. Calls `BattleEngagementManager.RequestFrontlineSlot` each frame; blocked until `battleStarted == true`. |
| `Engage` | Acquires nearest enemy via `BattleTargetFinder`. No range gate — immediately transitions to `Decide`. |
| `Decide` | Calls `SkillSystem.PickBestSkill` (highest-cost affordable skill, gated by speed cost / speed gate per Phase 4). Resolves the chosen `SkillSlot` via `SkillSystem.ResolveSkill`. Checks summon guard (see Summon System below). Reads the `ResolvedTechnique.castType` to determine the next state. |
| `Melee` | Chases target until within `attackRange` (~2.5u), then transitions to `Execute`. |
| `CastMobile` | Moves toward target while charging (charge time = action count × 0.3s). Transitions to `Execute` at mid-range. |
| `CastRooted` | Stops moving, faces target, charges for ~1 second, then transitions to `Execute`. |
| `Execute` | Fires the resolved technique through `BattleCombatResolver`. Brain populates `ResolvedTechnique.executionTime` from the SPD/proficiency/speed-band formula at Execute entry (Phase 11). |
| `Recover` | ~1-second cooldown. Returns to `Decide`. |
| `Dodging` | Parabolic jump-back arc, 3.5m distance (0.3s movement + 0.5s pause). Interrupts any other state. Returns to `Decide`. |
| `Stunned` | (Phase 10) CC: action suppressed, executor cancelled on entry, returns to `Decide` when the Stun effect expires in `BattleStatusEffectSystem`. |
| `Repositioning` | (Phase 5) Reserved enum for explicit speed-rebuilding movement. Brain can transition here when it decides "build speed before next exchange"; currently the AI brain encodes the same intent via `BuildSpeed` archetype routing back through `Melee`. |
| `Dead` | Terminal. Triggers `BattleEngagementManager.OnUnitDied`, which promotes a backline unit. |

### Transition rules — important details

- **Decide always re-resolves.** Each loop through the state machine, the unit picks a skill fresh based on current energy and active buffs. Skills are not committed in advance.
- **Dodging interrupts Execute.** Even mid-cast, a successful dodge check interrupts. The cast is wasted (energy was spent in `Execute` entry, not at `Decide`).
- **Death is terminal except for revival mechanics.** Revival is not currently implemented; if added, `Dead → Decide` is the transition path.

---

## Movement

Movement in this game is **real-time, code-driven, and not grid-based.** This needs to be stated explicitly because the `archive/` folder describes a grid-based movement system that no longer applies.

### Locomotion

- Each `TerrainBattleUnit` has a Unity `CharacterController`.
- Movement is computed in `TerrainBattleUnit` per state — there is no central pathfinding system at runtime.
- During `Melee` and `CastMobile`, the unit moves directly toward its target's transform.
- During `Backline`, the unit moves toward a back-of-team position.
- Animation does not drive movement (no root motion). The `CharacterController` moves the body; the animator is told to play locomotion clips.

### Dodge movement

- `Dodging` state plays a scripted parabolic arc: 3.5m horizontal distance, lifted off the ground briefly, 0.3s in motion + 0.5s landed pause.
- This is hardcoded in `TerrainBattleUnit` currently. Worth extracting to data when touched.

### Engagement and crowding

Movement does not include collision avoidance with allies in the general case, but two subsystems prevent crowding:

- **`BattleEngagementManager`** caps frontline units at 3 per side. Excess units stay in `Backline` and only advance when a slot opens.
- **`BattleMeleeTokenSystem`** caps the number of units pressing a single target at 3. Units beyond that orbit the target rather than stack on it.

### Range

- `attackRange` is approximately 2.5 units, used by `Melee` to determine when to stop chasing and execute.
- `CastMobile` mid-range threshold is implementation-specific in `TerrainBattleUnit` — check the code if precise value matters for a task.

### Known coupling

Movement is currently coupled into `TerrainBattleUnit` itself (per-state movement code in update methods). If movement complexity grows (zones, pushed-back states, knockup landing), extract to a `BattleMovementSystem` rather than expanding `TerrainBattleUnit`. See `02_ARCHITECTURE.md` for the subsystem pattern.

---

## Energy system

Energy gates skill use. Without enough energy for the picked skill, the unit idles.

| Property | Value |
|---|---|
| Maximum energy | 100 |
| Starting energy | 50 |
| Base regen | +15/sec while in frontline combat |
| Backline regen bonus | +10/sec additional while in `Backline` |
| Block reward | +5 energy per successful block |

### Action costs

| Action | Cost |
|---|---|
| Physical (Punch, Kick) | 0 |
| Elemental (Hand Sign A/B/C) | 10 each |
| Support (Focus) | 15 |
| OrbSummon trigger | 0 (combo cost) |

When a skill fires, total cost = sum of `energyCost` for all actions in the chain. `SpendEnergy` is called before `Execute`. If energy is insufficient, the skill is skipped and the unit re-enters `Decide`.

`SkillSystem.PickBestSkill` selects the **highest-cost** skill the unit can currently afford — this is the default "spend energy on biggest combo available" behavior. Future behavior types may pick differently.

---

## Engagement system

`BattleEngagementManager` controls who is allowed to be in the frontline.

| Rule | Value |
|---|---|
| Max frontline units per side | 3 |
| Backline units | All others on the team |
| Slot allocation | First-come, first-served via `RequestFrontlineSlot` |
| Promotion trigger | `OnUnitDied` fires from `Dead` state, which calls promotion |

Each frame, units in `Backline` call `RequestFrontlineSlot`. Calls are blocked until `battleStarted == true`. When granted, the unit transitions to `Engage`.

---

## Dodge and block

Both are checked in `BattleCombatResolver.ResolveBasicAttack` and `BattleCombatResolver.ResolveSkillAttack`, in this order: dodge first, then block (only if dodge failed).

### Dodge

| Property | Value |
|---|---|
| Trigger | Incoming attack, before damage |
| Chance | `SPD × 5% × (0.5 + currentSpeed / 100)`, clamped to 75 % (Phase 9 — speed-modulated). At `currentSpeed = 50` this matches the legacy formula. |
| Energy cost | 10 |
| Cooldown | 2 seconds between dodges |
| Animation | Parabolic jump-back arc, 3.5m distance |
| Result | Attack misses entirely; no damage |
| Suppressed | While `Stunned` (Phase 10 CC) |

If the dodging unit doesn't have 10 energy or is on cooldown, dodge is not attempted. **Block check then happens.**

### Block

| Property | Value |
|---|---|
| Trigger | Incoming attack, only if dodge fails or wasn't attempted |
| Chance | `DEF * 2%` (e.g. DEF 15 = 30% chance) |
| Effect | 50% damage reduction |
| Reward | +5 energy on successful block |
| Cooldown | None |
| Movement | None |

When a unit is blocking successfully, knockback is suppressed for that hit (`BattleKnockbackSystem` is told to skip).

---

## Win condition

Currently a single condition: **all units on one side reach `Dead`.**

```
For each tick of the battle loop:
  if all player units are Dead → Defeat
  if all enemy units are Dead → Victory
  else → continue
```

This is checked in `TerrainBattleManager` on a low-frequency tick (not every frame). When the condition triggers, the battle stops processing and the result is displayed.

### Future win condition extensions

Mission types planned (see `08_ROADMAP.md`):

- Survive for N seconds
- Protect target unit
- Defeat boss only
- Time limit failure
- Custom objectives

When implementing, follow the extensible pattern: each objective is a separate object that implements a `CheckOutcome(squad state) → Outcome` method. `TerrainBattleManager` queries the active objective rather than hardcoding the all-dead check.

---

## Combat subsystems

All subsystems are MonoBehaviours attached to the `TerrainBattleManager` GameObject (or as children where appropriate). They are wired together in `TerrainBattleManager.Start()`.

For the architectural rationale of the subsystem pattern, see `02_ARCHITECTURE.md`.

### `BattleCombatResolver`

The single point where techniques actually fire and damage actually happens.

**Responsibilities:**
- `ResolveBasicAttack(attacker, defender)` — basic melee hit (Physical action standalone)
- `ResolveSkillAttack(attacker, technique, target)` — combo attack with damage formula
- `ApplyBuff(unit, buff)` — apply `ActiveBuff` from Elemental action
- `ApplyOrbSummon(caster, technique)` — spawn orbs via `OrbBuffHandler`
- Dispatch `TechniqueType.OrbRay` to `BattleOrbRaySystem.FireOrbRay`
- `ExecuteIndividualActions(caster, target, actions)` — fallback when no combo matched
- Dispatch on `TechniqueType` for the appropriate execution branch

**Damage formula (current):**

```
final = sum(basePower) × powerMultiplier × (ATK / 10) × proficiency
```

Then dodge/block checks reduce or eliminate. `ActiveBuff` damage is added per-hit on top.

When extending damage logic, prefer building toward the **modifier pipeline** described in `02_ARCHITECTURE.md` rather than adding new conditional branches.

### `BattleExchangeCoordinator`

Assigns attacker/defender roles between unit pairs using initiative. The `IsAnimating` lock prevents role swaps mid-animation, which is what stops two units from both trying to attack each other in the same frame.

(Phase 8) Now also tracks the six-phase exchange lifecycle (`Initiation` → `WindUp` → `StrikeSequence` → `Resolution` → `Beat` → `ReEvaluation`). After `OnAttackerRecoveryComplete`, a per-pair beat lock (`beatDurationSeconds`, default `0.4s`) prevents the same pair from re-engaging until the non-skippable pause expires — other pairs are unaffected. `AdvancePhase(attacker, phase)` is the public hook for advancing the phase from the resolver/brain. `OnStrikeResolved(attacker, defender, response)` runs the spec's speed economy: landed strike refunds the attacker `+3` speed, dodged strike costs the defender `5` speed.

`DefenderResponse.Counter` is reserved — defender → attacker role flip on a successful counter is design-tuning work and not yet wired.

### `BattleMeleeTokenSystem`

Tracks how many units are currently pressing each target. Caps at 3. Excess units are told to orbit (move around the target at distance) rather than stack at the same melee point.

### `BattleEngagementManager`

Owns frontline slot availability for both sides (3 per side). Promotes backline units when a slot opens via `OnUnitDied`. Blocks slot requests until `battleStarted`.

### `BattleTargetFinder`

Nearest-enemy acquisition. Used by `Engage` state. Currently: simple distance check on alive enemy units. Future targeting priorities (lowest HP, backline-first, etc.) belong here, gated by `BehaviorType`.

### `BattleHitStopSystem`

Short global freeze frames on hit for combat feel. Three tiers:

| Tier | Duration | Used for |
|---|---|---|
| `Light` | ~0.033s (2 frames) | Basic attacks |
| `Medium` | ~0.066s (4 frames) | Skill attacks |
| `Heavy` | ~0.133s (8 frames) | High damage / ultimates |

Implementation: drops `Time.timeScale` to 0 for the configured duration, then lerps it back to 1. World-wide freeze, not per-unit. Per-unit pause is owned by `BattleAnimancerDriver.ApplyHitStop` and composes additively with this.

### `BattleMovementSystem`

(Phase 5) Per-unit `MovementIntent` registry. Owns the policy ("how does this unit gain speed"); does not own the actual movement (that stays on `UnitMovementController`).

| Method | Purpose |
|---|---|
| `SetIntent(unit, intent)` | Set the unit's current intent. Pushed by `UnitBrainAI.TransitionTo` per state, and overridden inside states (e.g. orbit → Circle). |
| `GetIntent(unit)` | Current intent, defaults to `Hold`. |
| `GetGainRate(unit)` | Per-second speed gain for the current intent: `Hold=0`, `Disengage=3`, `Circle=6`, `Close=8`, `Dash=10`. Consumed by `BattleSpeedSystem.ComputeDelta`. |
| `Clear(unit)` | Cleanup hook from `TerrainBattleUnit.OnDestroy`. |

### `BattleSpeedSystem`

Owns the **Speed kinetic resource** (Phase 3). Per-unit pool tracked via a `Dictionary<TerrainBattleUnit, float>` — Speed lives here, not on `UnitRuntime`, so future visual modulation, AI thresholds, and CC stack composition all read from one source.

| Property | Value |
|---|---|
| Starting speed | 30 |
| Soft cap | 70 (gain ×0.4 above this) |
| Hard cap | 100 |
| Idle drain | -5/sec |
| Block drain | -2/sec (in `Recover` + `Defender` role) |
| Stagger drain | -10/sec (in `Stagger` state) |
| Gain at full velocity | +8/sec (linear in `UnitMovementController.CurrentMoveSpeed` for now; per-`MovementIntent` shaping arrives in Phase 5) |

Public API: `RegisterUnit`, `UnregisterUnit`, `GetSpeed(unit)`, `CanAfford`, `SpendSpeed → bool`, `GainSpeed`, `GetSpeedBand → SpeedBand`. Fires `OnSpeedChanged(unit, newValue)` for UI / future CC visuals.

Visual: `SpeedBarUI` is auto-attached to each unit on init and draws a small screen-space bar below the HP bar with a soft-cap tick mark and a band-keyed color.

### `BattleStatusEffectSystem`

Owns active CC and status effects per unit (Phase 10 foundation). Effects are stacked by type with **longest-wins-on-refresh** semantics — re-applying the same type doesn't add up but does refresh duration if the new one is longer.

| Effect | Wired |
|---|---|
| `Stun` | Drives `UnitBrainAI.EnterStun` / `ExitStun`; suppresses dodge; transitions the unit to `Stunned` state and back via `Decide` on expiry |
| `Slow` | Reduces movement speed in `UnitBrainAI.ChaseTarget` and stretches `executionTime` (multiplicative) |
| `Knockback` | Reserved enum value — formal effect still owned by `BattleKnockbackSystem` |
| `Interrupt`, `Knockdown` | Reserved — not wired (Knockdown needs ragdoll physics) |

Public API: `Apply(unit, type, duration, magnitude)`, `Has`, `GetMagnitude`, `RemoveAll`, `GetSlowFactor`. Brain reads slow factor for movement; `LaunchExecuteAbility` reads it for execution-time stretch.

### `BattleAnimancerDriver`

Centralized Animancer Pro playback subsystem. Per-unit `AnimancerComponent`s register on `TerrainBattleUnit.Initialize`; the driver is the single entry point for `AttackProfile`-driven playback and per-unit hit-stop.

Public surface:

| Method | Purpose |
|---|---|
| `RegisterUnit(unit, animancer)` | Called by `TerrainBattleUnit.Initialize` to publish the unit's Animancer reference. |
| `UnregisterUnit(unit)` | Called from `TerrainBattleUnit.OnDestroy`. |
| `IsAvailable(unit)` | True if the unit has a registered Animancer. Abilities check this before electing the Animancer path. |
| `PlayAttack(unit, profile)` | Plays `profile.transition` on the unit's Animancer; routes the named impact event to `unit.OnHitFrame()` and the state's `OnEnd` to `unit.OnAttackEnd()`. Returns `false` if the unit isn't registered or the profile has no transition — caller falls back to legacy Animator Controller path. |
| `ApplyHitStop(unit, duration)` | Zeroes `AnimancerComponent.Graph.Speed` for `duration` real seconds. Independent of `Time.timeScale`. |
| `ApplyHitStop(attacker, defender, duration)` | Pair convenience overload. |

The driver auto-fires a 50ms attacker-only Animancer pause on every named-impact event, which is the Phase 1 demonstration of per-unit hit-stop. When `BattleSpeedSystem` and CC visuals come online, additional effects fan out from the same impact callback.

Replaces the previous per-unit `UnitAnimancerDriver` MonoBehaviour. See `07_PRESENTATION.md` "Animation runtime (Animancer Pro)" for the rationale and migration pattern.

### `BattleKnockbackSystem`

Directional knockback and stagger on heavy hits. Suppressed when the target is blocking. Drives `CharacterController.Move` over a short curve (0.3–0.5s).

### `BattleSummonManager`

Spawns and tracks guardian units summoned by the Summoning combo. Maintains an active-summon list per caster, queried by `HasActiveSummon(casterId)` to prevent re-summon while a guardian is alive. Summon stats: HP = power × 2, ATK = power / 2.

### `BattleOrbRaySystem`

Owns the **Orb Ray** skill mechanic. On `FireOrbRay(caster, technique, orbPrefab)`:

1. Acquires the nearest enemy via `BattleTargetFinder` (re-resolves at fire time so a stale `Decide` target doesn't cause a miss).
2. If the caster is within `meleeProximityRadius` (~3u) of that target, teleports the caster `teleportDistance` (~20u) in a random horizontal direction. Ground-snapped via downward raycast against `groundLayerMask`.
3. Spawns `defaultOrbCount` (or the source `OrbSummon` action's `orbCount`) instances of the configured `orbPrefab` in a ring around the caster, then calls `OrbProjectile.FireRay(target, damage)` on each — instant ray, instant damage, brief LineRenderer beam, despawn. Optional `perOrbFireDelay` staggers them for staccato feel.

Currently uses fixed defaults (3 orbs × 15 damage). When the modifier pipeline lands, ray damage should flow through it the same as basic skill damage. The orb prefab reference is held by `BattleCombatResolver`; the resolver passes it to `FireOrbRay` so the two subsystems share one Inspector field.

### `OrbBuffHandler` (per-unit)

Attached to a caster when the Orb Strike combo fires. Spawns N `OrbProjectile` instances orbiting the caster at fixed radius/height. Each subsequent **punch** (not kick) calls `TryConsumeOrb(target)`, firing the next available orb. Removes itself when all orbs are consumed.

### `OrbProjectile`

Orbits its caster until fired. Once fired, flies in a parabolic arc to the target and applies flat damage on arrival via `OnArrival`. Damage is configured on the prefab.

### `HealthSystem`

Tracks HP and syncs the floating health bar above each unit. Receives damage events from `BattleCombatResolver` and zeroes-out triggers death transition on the unit.

### `CombatLogger`

In-memory timestamped log for combat diagnostics. Press **L** at runtime to dump to the console. Categories: `STATE`, `ROLE`, `ANIM`, `DMG`, `INIT`, `EXCHANGE`, `WARN`. Use this when debugging combat issues — the logger captures sequencing that's hard to see live.

### `UnitAnimationEventRelay`

Forwards Animation Events from the Animator child to `TerrainBattleUnit`. Animation Events **notify** code that a frame was reached; the code decides what to do. Animation does not own combat decisions (see `07_PRESENTATION.md`).

### `BattleH2HPhaseSystem`

Owns the per-unit `H2HPhase` state and dispatches `OnPhaseEnter` / `OnPhaseExit` events. Phase transitions go through this subsystem, never written directly. Phase-specific timers (spotting delay, decision lag, separation duration) are stamped per transition by reading `UnitDefinition` fields, with optional inspector overrides used by the H2H training UI for testing.

Used by:
- `KuboldLocomotionDriver` — picks combat-context vs traversal locomotion clip
- `TrainingPlayerController` — clamps WASD speed to the unit's phase max
- `H2HUnitBrain` — gates AI decisions; reads spotting / separation expirations

| API | Purpose |
|---|---|
| `Register(unit, initial=NotEngaged)` | Add a unit to phase tracking. The training scene's `H2HTrainingDirector` registers everything in `_units` on Awake. |
| `TransitionPhase(unit, newPhase, reason)` | Move a unit to a new phase. Stamps timers, fires events. Idempotent for same-phase calls. |
| `GetPhase(unit)` / `SecondsInPhase(unit)` | Query helpers used by brains and locomotion. |
| `CanDecide(unit)` / `NoteDecision(unit)` | Decision-lag gate for Engagement-phase brains. |
| `OverrideSpottingTime` / `OverrideDecisionLag` | Test-scene fields ≥ 0 force the timer in place of the unit's def values. |

The unit type is `MonoBehaviour` rather than `TerrainBattleUnit` so the H2H layer can run in the TrainingDummy scene without a `TerrainBattleManager`. Implementations expose unit config via `IH2HConfigured.Definition`.

### `BattleH2HOrchestrator`

Pair coordinator for H2H Exchange. When two units enter Exchange phase together, the orchestrator:
1. Verifies both units are in `Engaged` (rejects `RegisterPair` otherwise)
2. Picks active attacker by initiative score (`speed + stance.initiativeBonus + just-defended bias + RNG`)
3. Asks the attacker for a `Combo` via `IH2HExchangeAgent.PickCombo`
4. Pre-positions the attacker via a smoothstep coroutine to land at the combo's `desiredImpactDistance` over `positionAdjustDuration`
5. Schedules per-hit impact frames (first hit at `positionAdjust + impactNormalized × clipLength`, subsequent hits spaced by `interHitGap`)
6. Dispatches `OnExchangeImpactAttacker(handle, hitIndex)` and `OnExchangeImpactDefender(handle, hitIndex)` once per hit (single-fire latch)
7. Computes post-exchange transitions: separation roll per unit (modulated by stance, HP, current speed), counter-swap if the defender returned `true` from impact

| API | Purpose |
|---|---|
| `RegisterPair(a, b)` | Begin an exchange. Returns null on phase eligibility / busy / no-combo failure. Attacker's combo determines hit count, timing, pre-position adjust. |
| `GetActiveExchange(unit)` | Query whether a unit is currently inside an exchange. |
| `CancelExchange(handle, reason)` | Emergency exit (death mid-exchange, etc.). |
| `OnExchangeStarted` / `OnExchangeImpact(handle, hitIndex)` / `OnExchangeResolved` | Events for UI / logging / hit-stop hooks. |

Counter only honored on the first hit of a combo; after that, the chain proceeds and the defender re-decides per impact (block one, dodge one, eat one is a normal sequence).

### `H2HUnit` (per-unit MonoBehaviour)

The H2H equivalent of a `TerrainBattleUnit` for the training scene. Implements both `IH2HConfigured` (so `BattleH2HPhaseSystem` can read tuning fields) and `IH2HExchangeAgent` (so `BattleH2HOrchestrator` can drive its strike / reaction playback). Plays library clips through the existing `BattleAnimancerClipLibrary`, falls back to `idle` / `punch` / `dodge` ids when combat-context variants are missing.

Holds:
- The unit's stance and resolved hostility list (explicit hostiles + hostile teams)
- A `Combo[]` library — each combo is a list of `ComboHit` entries with attack id, archetype, normalized impact time, damage, and speed cost. The brain calls `PickCombo()` to pick the highest-tier affordable combo; auto-populated with `BasicJab` / `JabHookUppercut` / `HeavyKick` if left empty
- HP / Speed (0-100, soft-capped at 70) / Energy (0-`maxEnergy`) resource model with per-phase tick rates (Approach +8/sec, Engaged-moving +6/sec, Engaged-idle -5/sec, Separating +4/sec, NotEngaged drain -2/sec; energy regens +2/sec while in combat)
- `IsDead` flag with `OnDeath` event — at HP=0, cancels any active exchange, transitions to `NotEngaged`, suppresses locomotion
- Per-impact FX: per-unit Animancer hit-stop, camera shake (via `H2HCameraShake`), particle burst (programmatic glowing sphere or `_impactBurstPrefab`)
- Per-phase clip overrides (alert idle, combat idle, separation backstep, death)
- An optional `CounterChance` used by the training scene's counter scenario

### `H2HCameraShake`

Lightweight transform-shake added to `Camera.main` on demand by `H2HUnit.FireImpactFX`. Perlin-noise jitter with quartic falloff over `duration` seconds, restored to baseline. Cinemachine-free; swap for `CinemachineImpulseSource` when Cinemachine lands.

### `H2HFootIK`

Per-unit foot IK that snaps the humanoid's left/right foot to ground geometry via raycasts during the Animator's IK pass. Suppresses itself during Exchange phase so attack / hit-react clips aren't pulled to ground. Idle when Animancer's playable graph drives the rig (no Animator Controller present), engaged once a controller is reintroduced.

### `H2HUnitBrain`

Reactive AI driving an `H2HUnit` through the phase cycle. One brain per unit. Per-phase logic (in `Docs/Design/HAND_TO_HAND_COMBAT.md` §8):

- `NotEngaged` — scan for hostiles within `spottingRangeMeters`, transition to Spotting on contact
- `Spotting` — face target via `H2HMovementController.FaceTowards`, wait out the spotting timer, transition to Approaching
- `Approaching` — declare forward intent via `H2HMovementController.SetMoveIntent(fwd, traversalSpeed)`; defensive bias composes a forward+lateral drift into a single intent. Transitions to Engaged when within `engagementRangeMeters`
- `Engaged` — when outside strike range, intent forward at `combatMovementSpeed`; in range, intent lateral via the circling director. On each decision-lag tick, rolls commit / disengage / hold; commit calls `BattleH2HOrchestrator.RegisterPair`
- `Exchange` — orchestrator owns the unit; brain idles. `H2HUnit` locks the controller's facing so the orchestrator's snap-face isn't fought by the smooth lerp
- `Separating` — intent backward at `disengageSpeed` until the timer expires AND distance ≥ `separationDistanceMeters`, then return to Engaged

The brain never calls `CharacterController.Move` directly. All motion goes through `H2HMovementController` (below) which integrates intent into a smoothed continuous velocity. This kills the binary 0/peak velocity blips that earlier versions of the brain produced (one `cc.Move` call per conditional branch each frame), and gives the locomotion driver a stable signal for clip selection.

### `H2HMovementController`

Per-unit movement physics. Owns world-space `Velocity` (smoothed), gravity, and rotation lerp; `cc.Move` is called once per frame from this single component.

API:

```csharp
public Vector3 Velocity        { get; }
public float   Speed           { get; }
public bool    HasIntent       { get; }
public bool    IsStarting      { get; }
public bool    IsStopping      { get; }
public bool    IsAccelerating  { get; }
public bool    IsDecelerating  { get; }
public bool    IsSuppressed    { get; }

public void SetMoveIntent(Vector3 worldDir, float maxSpeed);
public void Stop();
public void FaceTowards(Vector3 worldPos);   // smoothed
public void SnapFace(Vector3 worldPos);      // instant
public void LockFacing();                    // suspend rotation lerp
public void UnlockFacing();
public void SuppressFor(float seconds);      // halt cc.Move during a one-shot strike
public void ClearSuppression();
public void Teleport(Vector3 worldPos);
```

Tunables (`_walkAccel`, `_runAccel`, `_decelOnStop`, `_decelOnReverse`, `_walkRunBoundary`, `_maxSpeedHardCap`, `_rotationLerp`, `_gravity`) are in the Inspector. The accel-rate selector picks `_decelOnReverse` when intent direction reverses against current velocity (sharper brake), `_decelOnStop` when intent is zero, `_walkAccel` when target speed < walk-run boundary, and `_runAccel` otherwise.

`H2HUnit.HandlePhaseEnter` calls `LockFacing()` on entry to Exchange and `UnlockFacing()` on exit. `H2HUnit.PlayLibraryClipOneShot` calls `SuppressFor(clip.length)` so brain-driven motion halts while a strike clip plays — the unit doesn't slide forward through the defender during a 1.6s axe-kick.

### `KuboldLocomotionDriver`

Reads `H2HMovementController.Velocity` (preferred) or `cc.velocity` (fallback) and picks the correct Kubold clip from `BattleAnimancerClipLibrary` per `(engaged-state × speed-band × direction)`. The driver covers four animation pathways:

1. **Loops** — combat-stance gets the full 8-direction matrix at walk speeds; standing locomotion gets 4-cardinal walk + 6-direction run + sprint. Sub-band swap from `combat_walk_fwd_loop` (KB_WalkFwd1 ≈ 0.60 m/s) → `combat_walk_fwd_fast` (KB_WalkFwd2 ≈ 0.88 m/s) above 0.75 m/s.
2. **Turn-in-place** — when speed is below `_idleSpeedThreshold` and `MovementController.PendingTurnAngleDegrees` exceeds `_turnInPlaceThreshold` (30° default), swaps to `*_turn_l90` / `_r90` (or `_l180` / `_r180` past `_turn180Threshold` = 120°). Combat / standing variants picked by phase.
3. **Start / Stop transitions** — detects Idle ↔ Walk/Run band edges, plays the matching `_start` (loco_walk_fwd_start, loco_walk_bwd_start, loco_strafe_l/r_start, loco_run_fwd_start) or `_stop_lu` / `_stop_ru` (alternated), and holds the clip for `_transitionHoldSeconds` (600 ms default) before resuming normal resolution. Standing-locomotion only — Kubold pack has no combat-stance start/stop clips. Toggle via `_useStartStopClips`.
4. **Pivot starts** — at the idle → moving edge, compares `MovementController.IntentDir` against current facing; if angle > 60°, picks `loco_walk_fwd_start_l90/_r90/_l135/_r135/_l180/_r180` (or run equivalent) instead of the straight start. Cleanly handles post-Separation re-engage where the unit needs to turn-and-walk in one animation.

Phase-aware: `Spotting` / `Engaged` / `Exchange` / `Separating` use `combat_*` ids; `NotEngaged` / `Approaching` use `loco_*` ids. Dwell-time hysteresis (`_minDwellTime` = 120 ms default) prevents cone-boundary and band-boundary flicker. Recursive fallback chain so a missing combat clip degrades to standing → legacy alias → idle. Full clip matrix in `Docs/Design/LOCOMOTION_CHEATSHEET.md`. Detail in `Docs/07_PRESENTATION.md` §"Locomotion driver".

### `H2HTrainingDirector`

Single MonoBehaviour that owns `BattleH2HPhaseSystem` + `BattleH2HOrchestrator` and the unit registry for the test bench. The future battle integration will spin equivalents up via `TerrainBattleManager`; for now this lives on a dedicated GameObject in `TrainingDummy.unity`. `Awake` auto-spawns the subsystems if not pre-wired and registers every `H2HUnit` in `_units`.

The scene is built (or rebuilt idempotently) by the `TacticalRPG → H2H → Build Training Scene` editor menu (`H2HTrainingSceneSetup.cs`), which adds H2H components to TestSubject / Dummy and constructs the `H2H_Canvas` UI panel.

---

## Behavior system (current state)

Each unit has a `BehaviorLoadout` with a `BehaviorType` (Aggressive, Defensive, Balanced). In the current implementation, behavior types influence:

- Skill selection bias in `SkillSystem.PickBestSkill`
- Engagement aggression (how quickly to advance from `Backline`)
- Dodge willingness threshold

The behavior types are placeholders compared to the eventual design. The vision (see `01_VISION.md`) is for behaviors to feel like setting doctrine — pick a few clear archetypes, give them distinct on-field behavior, expand carefully. Don't add many fine-grained sliders.

When extending behavior:

- **Don't** add `if (behaviorType == X)` branches scattered across multiple subsystems
- **Do** put behavior-driven decisions in one place (e.g., a new `BattleBehaviorSystem` that all subsystems consult)
- **Don't** make behavior into a programming-language-style scripting system

---

## Summon system

The Summoning combo (`A → B → C → Focus`, Rooted, 3.0× power) deploys a guardian unit on the caster's team.

- Summons are pre-cast at the start of combat — the unit transitions `Engage → Decide` immediately.
- `BattleSummonManager.HasActiveSummon(casterId)` guard in `Decide` prevents the unit from re-casting Summoning while their guardian is alive.
- When the summon dies, the caster returns to normal Decide cycling and may re-cast.

---

## Orb system

Triggered by the **Orb Strike** combo (`A → A → A → B → B`, Rooted, 1.0× power).

The flow:

1. Combo resolves with `TechniqueType.OrbSummon`.
2. `BattleCombatResolver.ApplyOrbSummon(caster, technique)` calls `OrbBuffHandler.Spawn(caster, orbPrefab, count, damage)`.
3. `OrbBuffHandler` instantiates N `OrbProjectile` GameObjects orbiting the caster at fixed radius and height.
4. Each time the caster lands a **punch** (not a kick), `OrbBuffHandler.TryConsumeOrb(target)` fires the next available orb.
5. The orb arcs to the target and applies flat damage on arrival via `OrbProjectile.OnArrival`.
6. When all orbs are consumed, `OrbBuffHandler` removes itself from the unit.

**Required setup:** orb prefab assigned on `BattleCombatResolver` Inspector field; prefab must have an `OrbProjectile` component.

---

## Combat ticks vs frame updates

This system is **frame-based**, not tick-based. Each `TerrainBattleUnit.Update` runs its state machine per frame. Subsystems either also run per frame (movement, target finding) or are event-driven (hit-stop coroutines, knockback curves, win-condition checks at lower frequency).

Don't introduce a global tick. Don't impose a turn structure. The real-time per-unit autonomy is core to the combat feel.

---

## Editor setup checklist

When working in the Unity Editor on the battle scene:

1. **Action Definitions** — `TacticalRPG → Recreate Action Definitions (force)` rebuilds all `Assets/Data/Actions/` assets.
2. **Combo Library** — `TacticalRPG → Create Combo Library` rebuilds all `Assets/Data/Combos/` recipes plus `ComboLibrary.asset`.
3. **TerrainBattleManager** — assign `ComboLibrary.asset` to the `Combo Library` field. If empty, the hardcoded fallback list in `ComboLibrary.cs` is used.
4. **BattleCombatResolver** — assign the orb prefab (must have `OrbProjectile` component).

Run these once when the project is set up, and again after any change to action or combo data.

---

## Adding a new combat behavior

The pattern, in order:

1. **Define the concern.** What state does it own? When does it tick? What does it expose?
2. **Create a subsystem MonoBehaviour.** Name it `Battle*System` or `Battle*Manager`. Place it in `Assets/Scripts/ThirdPerson/`.
3. **Wire it through `TerrainBattleManager`.** Add an Inspector reference. Add `Start()` wiring of dependencies.
4. **Have other subsystems and `TerrainBattleUnit` query it.** Don't make them maintain duplicated state.
5. **Update this doc.** Add a row to the subsystem table.
6. **Add a test if possible.** Extract logic to a plain-C# helper if the MonoBehaviour itself is hard to test.

What **not** to do:

- Add fields directly to `UnitRuntime` for the new concern unless they're truly per-unit data
- Add update methods to `TerrainBattleManager`
- Add update methods to `TerrainBattleUnit` that aren't state-machine logic
