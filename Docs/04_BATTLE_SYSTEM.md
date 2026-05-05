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
3. **Battle.** `battleStarted = true`. Each unit ticks its own `UnitCombatState` machine independently. Subsystems execute combat rules (damage, knockback, hit-stop, etc.).
4. **End.** When all units on one side reach `Dead`, `TerrainBattleManager` displays the victory/defeat message and stops the loop.

There is **no central tick.** Combat is real-time, frame-driven via Unity's `Update`. Each unit makes its own decisions; subsystems coordinate.

---

## The per-unit state machine

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
| `Decide` | Calls `SkillSystem.PickBestSkill` (highest-cost affordable skill). Resolves the chosen `SkillSlot` via `SkillSystem.ResolveSkill`. Checks summon guard (see Summon System below). Reads the `ResolvedTechnique.castType` to determine the next state. |
| `Melee` | Chases target until within `attackRange` (~2.5u), then transitions to `Execute`. |
| `CastMobile` | Moves toward target while charging (charge time = action count × 0.3s). Transitions to `Execute` at mid-range. |
| `CastRooted` | Stops moving, faces target, charges for ~1 second, then transitions to `Execute`. |
| `Execute` | Fires the resolved technique through `BattleCombatResolver`. |
| `Recover` | ~1-second cooldown. Returns to `Decide`. |
| `Dodging` | Parabolic jump-back arc, 3.5m distance (0.3s movement + 0.5s pause). Interrupts any other state. Returns to `Decide`. |
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
| Chance | `SPD * 5%` (e.g. SPD 10 = 50% chance) |
| Energy cost | 10 |
| Cooldown | 2 seconds between dodges |
| Animation | Parabolic jump-back arc, 3.5m distance |
| Result | Attack misses entirely; no damage |

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

### `BattleMeleeTokenSystem`

Tracks how many units are currently pressing each target. Caps at 3. Excess units are told to orbit (move around the target at distance) rather than stack at the same melee point.

### `BattleEngagementManager`

Owns frontline slot availability for both sides (3 per side). Promotes backline units when a slot opens via `OnUnitDied`. Blocks slot requests until `battleStarted`.

### `BattleTargetFinder`

Nearest-enemy acquisition. Used by `Engage` state. Currently: simple distance check on alive enemy units. Future targeting priorities (lowest HP, backline-first, etc.) belong here, gated by `BehaviorType`.

### `BattleHitStopSystem`

Short freeze frames on hit for combat feel. Three tiers:

| Tier | Duration | Used for |
|---|---|---|
| `Light` | ~0.033s (2 frames) | Basic attacks |
| `Medium` | ~0.066s (4 frames) | Skill attacks |
| `Heavy` | ~0.133s (8 frames) | High damage / ultimates |

Implementation: brief `animator.speed = 0` on both attacker and defender, restored after the duration.

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
