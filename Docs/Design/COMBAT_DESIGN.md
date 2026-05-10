# COMBAT_DESIGN.md

> **Tier 2 — Stable.** Canonical spec for the combat system. Read **before any combat work**. The Tier 1 docs (`04_BATTLE_SYSTEM.md` etc.) reflect what is actually built; this doc is the destination.

---

## Purpose

This document defines the **target combat system** — the move-based, frame-data-driven, reactive simulation that produces continuous anime-flavored fights. It is the north star: every combat-related code change should move toward this model, not away from it.

For data structures see `03_DATA_MODELS.md`. For the subsystem architecture pattern see `02_ARCHITECTURE.md`. For the action-chain skill resolution see `05_SKILL_SYSTEM.md`. For animation pipeline rules see `07_PRESENTATION.md`. For the master list of all move animation handles see `MOVES_CATALOG.md` (in this folder).

---

## The vision in one sentence

A **real-time, reactive, frame-data combat engine** that produces continuous anime fights through the interplay of two units' moves overlapping in time, with paced cinematic moments emerging naturally from the simulation.

Not turn-based. Not exchange-locked. Not state-machine-discrete. **Continuous and reactive.**

---

## Design philosophy

Combat must feel like a *boxer dancing in the ring before striking* — not a JRPG turn order and not a shoot-em-up DPS race. The texture comes from three interlocked principles:

**Movement is meaningful.** Units don't just walk to the target and attack. They circle, retreat, re-engage, dash, weave. Movement generates resource that powers offense. A unit that stops moving becomes a worse fighter.

**Aggression is a commitment.** Big offensive output costs something real. Spending speed on a five-hit combo leaves the unit briefly vulnerable. The fighter who throws everything at once must rebuild before they can throw again. **Risk and recovery are the rhythm.**

**The brain reads, doesn't just decide.** Each unit's brain runs at engine rate (20Hz). It picks defensive moves *in reaction to* incoming attacks within a real reaction window. It cancels its own combos based on what landed, what was blocked, what was parried. Combat is a conversation between two move sequences, not two independent loops.

These three principles drive every system below.

---

## Combat engine — move-based, frame-data driven

### Tick rate

**50ms (20Hz).** Fast enough to feel real-time, slow enough to be deterministic and analyzable. Aligned with Unity's `FixedUpdate` default. Everything that follows operates on this tick.

### The atomic unit: the Move

Every unit, every frame, is **executing a move**. There is no separate "Idle state" — Idle is a move. Walking is a move. Recovering from a hit is a move. Combat is what happens when two units' moves overlap in time.

A `MoveDefinition` is a ScriptableObject:

```
MoveDefinition {
  id                      "attack_punch_jab"
  animationName           "anim_attack_punch_jab"     // string handle, see MOVES_CATALOG.md
  category                MoveCategory.LightAttack
  
  // Frame timeline (in 50ms ticks)
  startupFrames           2     //  100ms
  activeFrames            1     //   50ms — hit window
  recoveryFrames          4     //  200ms
  totalFrames             7     //  350ms total
  
  // Hit properties
  damage                  8
  hitType                 Light
  hitArchetype            Light
  range                   1.8                // units
  angleDegrees            45                 // hit cone half-angle
  
  // Cancel & chain windows
  cancelIntoOnHit         [punch_hook, power_strike]   // confirmed-hit chains
  cancelIntoOnWhiff       []                            // whiff cancels are rarer
  cancelWindowFrames      3                             // last 3 frames of recovery accept cancel
  
  // Defensive properties
  iFrameStart             0
  iFrameEnd               0
  superArmorFrames        0
  
  // Resource
  speedCost               0
  energyCost              0
  speedScaling            0
  speedGate               0
  
  // Movement during move
  movementCurve           AnimationCurve     // optional, integrated over frames
  facing                  FaceTarget         // FaceTarget / Lock / Free
  
  // Reaction this move triggers in the defender (paired-reaction lookup)
  reactionTag             ReactionTag.LightHit
}
```

### Hit resolution

Each tick, for each unit in *active frames* of an attack move:

1. Sphere-check against opponent's collider within `range` and `angleDegrees`
2. Read opponent's current move state (Idle / Block-active / Dodge-active / attack-active / recovery / stunned / airborne)
3. Resolve via state-pair table:
   - opponent in **Block-active** → block formula, opponent gains energy
   - opponent in **Dodge-active with i-frames** → miss
   - opponent in **Parry-active window** → parry triggers, attacker enters punish-window
   - opponent in **own attack startup** → trade (both hits land — clash)
   - opponent in **own attack active** → both hits land simultaneously
   - opponent in **recovery / idle / stunned** → full hit
4. Apply damage, knockback, CC per the resolved interaction

### Move state per unit

```
UnitMoveState {
  currentMove         MoveDefinition reference
  framesElapsed       int
  phase               Startup | Active | Recovery | Cancellable
  queuedNext          MoveDefinition?       // brain pre-queued next move
  airborne            bool
  iFrameCounter       int
  superArmor          bool
  facing              Vector3
}
```

The previous `UnitCombatState` enum (Backline / Engage / Decide / Melee / CastMobile / CastRooted / Execute / Recover / Stagger / Dodging / Stunned / Repositioning / Airborne / Dead) **is deprecated as the primary state**. Most of these become `MoveState.phase` properties of specific moves: "stunned" is the `react_stunned` move with active=duration; "dodge" is `defend_dodge_*` with i-frames during active; "airborne" is `react_launch_airborne` with mid-flight active frames. The state machine collapses into the move catalog.

A small handful — Backline, Dead, Engage — survive as *meta-states* outside the move loop because they describe whether the unit is in the move loop at all.

---

## The brain layer

### Reactive, not deliberate

The brain runs at engine rate (20Hz) but only **decides on transitions and pre-empts**:

- **Move complete** → pick next move from current move's `cancelInto` list (combo chain) OR from the stance's neutral pool
- **Opponent entered attack startup** → check reaction window; if within reach and stance permits, queue a defensive move
- **Got hit** → forced into hit-react move based on hit weight; brain regains control on recovery
- **Resource ceiling crossed** → opportunity to upgrade pick (e.g. just hit Primed band, big combo unlocked)

No more 1-second Decide cycles. Every tick is a chance to read the world and react if something is about to happen.

### Asymmetric stance brains

`IStanceBrain` interface. Each stance has its own implementation:

```csharp
interface IStanceBrain {
  // Called when unit's current move ends and a next move must be picked.
  MoveDefinition PickNeutral(BrainContext ctx);
  
  // Called when opponent enters attack startup AND impact is imminent
  // within a single frame. The reflex hook. Return null to eat the hit.
  MoveDefinition? PickReaction(MoveDefinition incoming, BrainContext ctx);
  
  // Called every tick in neutral. Reads the perception-gated threat list,
  // ally states, and nearby world entities. Lets the brain *prepare* —
  // cast Earth Wall, interpose for an ally, set a trap, interrupt a
  // long opponent cast. Distinct from PickReaction (reactive single-frame)
  // — PickPreparation runs ahead of time. Return null if no preparation
  // is warranted; brain stays in PickNeutral selection.
  MoveDefinition? PickPreparation(BrainContext ctx);
  
  // Called when current move enters its cancel window. Optional cancel-into.
  MoveDefinition? PickCancel(MoveDefinition current, BrainContext ctx);
}
```

Each stance is a different *fighter personality*. The seven canonical stances:

| Stance | Neutral pick | Reaction pick | Cancel behaviour |
|---|---|---|---|
| **Onslaught** | Walk forward + jab/hook | Light dodge or block, never disengage | Always cancel into next combo on hit |
| **Tempest** | Charge in via dash, then heavy combo | Dodge through to maintain pressure | Cancels into Launch finisher when speed permits |
| **Stalwart** | Idle near ally / target | BraceBlock — never moves | Never cancels, follows-through committed moves only |
| **Tactician** | Orbit at mid distance, bait | Parry first, block on parry-fail, dodge on Sharp+ | Cancels into counter after successful parry |
| **Wraith** | Orbit + observe | `defend_fade_out` at Primed, lateral dodge otherwise | Rarely cancels, prefers to disengage between hits |
| **Sentinel** | Idle in place | BraceBlock-only — never moves | Never cancels |
| **Conduit** | Stays Far, casts signs | Defensive teleport to escape | Chains sign casts; rarely closes |

Stances produce **visibly different fights** even with identical loadouts because the *brain itself* is different, not just numerical thresholds.

---

## Resources

| Resource | Built by | Spent on | Gate effect |
|---|---|---|---|
| **HP** | Healing skills | Damage | Death at 0 |
| **Energy** | Time in combat, frontline bonus, blocking | Sign casts, dodges | Move locked if insufficient |
| **Speed** | Movement (intent-shaped), specific kinetic skills | Heavy moves, dashes, dodges | Move locked / damage scaled / band visuals unlocked |
| **Stamina** *(planned)* | Slow regen | Consecutive blocks (each costs 5) | Block becomes unavailable when drained — forces dodge or eat |
| **Concentration** *(planned)* | Time + successful reads | Parry windows, counter punishes | Without concentration, parry window is shorter |

Stamina prevents infinite blocking. Concentration rewards correct prediction (successful parry → easier next parry). HP/Energy/Speed are first-class today; Stamina/Concentration are forward-looking and tracked here so we don't drift away from them.

### Speed pool & caps

| Property | Value |
|---|---|
| Minimum speed | 0 |
| Soft cap | 70 |
| Hard cap | 100 |
| Starting speed | 30 |
| Drain rate (idle) | -5/sec |
| Drain rate (blocking) | -2/sec |
| Drain rate (knocked down / staggered) | -10/sec |

Above the soft cap, gain rate is multiplied by 0.4 — the 70-100 band is a *primed* state, expensive to maintain but powerful to spend. Most fights live in 20-65.

### Speed-band as visual budget

Speed isn't just a stat modifier — it's the **legible currency for which moves are available**:

| Band | Range | Unlocks |
|---|---|---|
| **Sluggish** | 0-20 | walk, stand, basic block |
| **Engaged** | 20-50 | run, strafe, basic dodge, orbit |
| **Sharp** | 50-70 | dash, sidestep, bob-weave |
| **Primed** | 70-100 | teleport, ghost-trail, fade-attack, launch finishers |

A unit that can't afford the band literally can't perform the visual. The viewer learns: orbit → bar fills → primitive unlocks → fade-attack lands. That sequence is the design loop.

---

## Spatial system

### Range bands

Distance becomes a per-frame influence on move selection. For brain logic and camera framing:

| Band | Distance | Allowed moves |
|---|---|---|
| **Far** | > 8u | Cast Rooted, observe, orbit |
| **Mid** | 3-8u | Closing, circling, ranged casts, courtship |
| **Close** | 1-3u | Strike sequences, most attacks |
| **Locked** | < 1u | Mutual contact, parry-counter window, pose-attacks |

### Movement is continuous

**No teleports unless the move *is* a teleport.** Dashes are smooth integrations of the move's `movementCurve` over its active frames. Backsteps are smooth. **Units don't snap.** This is the "no warping" rule baked in.

A 200ms `defend_dodge_back` moves the unit 3u smoothly across 4 frames, not in one snap.

`mobility_teleport_flank` (Wraith only) IS a teleport — but it's a move with a specific teleport frame, with `GhostTrail` ghosts spawned in between. It's the explicit exception, not the default movement model.

---

## Reaction system — paired moves

The reaction table is a **move-pair lookup**. Brain consumes it inside `IStanceBrain.PickReaction`:

```
attacker move                     +  defender state                  →  defender's reaction move
─────────────────────────────────────────────────────────────────────────────────────────────────
attack_punch_jab                  +  Idle, Sharp+ band                 →  defend_bob_weave
attack_punch_hook                 +  Idle, blocking-stamina>0          →  defend_block_react
attack_kick_crescent (Launch)     +  any (forced)                      →  react_launch_airborne
attack_power_strike (Heavy)       +  Sentinel stance                   →  defend_static_anchor
attack_power_strike (Heavy)       +  Wraith Primed                     →  defend_fade_out
cast_triple_sign (ranged)         +  any                               →  defend_dodge_side or eat
attacker move (any startup)       +  Tactician + parry-window          →  defend_parry → counter-attack
```

**The reaction is itself a move.** It plays out over its frames. Hit resolution checks active frames against attack frames just like any other move-pair.

Reactions emerge from the move data, not a hardcoded switch. Adding a new reaction = adding a row to the lookup table.

---

## Choreography emerges from simulation

No more "force a 0.4s beat lock" coordinator hack. Pacing emerges from move durations:

- An exchange of light attacks: 350ms each, 200ms recovery — feels rapid
- A heavy attack: 600ms recovery — natural pause
- A blocked combo: `react_recoil_blocked` pushes attacker back, creates space
- Each move's `recoveryFrames` IS the beat; we don't force one externally

**Slow-mo is reserved for moments**, not beats:
- Killing blow lands → 500ms slow-mo
- Successful parry → 300ms slow-mo (the "I read you" moment)
- Crescent Kick connects → 400ms slow-mo at impact
- Knockdown lands → 250ms slow-mo

**Camera framing changes per moment**:
- Default: third-person follow, smooth-damped anchor
- Heavy hit lands: camera shake (impulse / direct transform offset, ~150ms)
- Round-end / killing blow: slow-mo + held angle
- Cinematic finisher (Launch combos, special moves): camera switches to side-on / low-angle for the duration
- Airborne: camera follows the launched unit, framed below

---

## Status / CC as moves

CC effects collapse into the move catalog:

- **Stun** = forced into `react_stunned` for N frames
- **Slow** = locomotion moves play with reduced velocity multiplier; `executionTime` formula divides by slow factor
- **Knockdown** = forced into `react_knockdown_*` (3 phases: launch / prone / rise) — total ~50 frames
- **Knockback** = forced into `react_recoil_*` with positional curve
- **Interrupt** = current cast move is *cancelled* (cancel window forced open), unit enters `react_dazed` for ~10 frames

`BattleStatusEffectSystem` becomes a "force-feeder" — when CC is applied, it forces the unit's move state to the appropriate reaction move regardless of brain decisions.

### CC catalog (for reference)

| Effect | Mechanical behavior | Typical duration |
|---|---|---|
| **Knockback** | Target forced backward along strike vector | Instant (movement ~0.4s) |
| **Stun** | Cannot act, cannot move, cannot dodge or block | 1.0–2.5s |
| **Interrupt** | Cancels target's current cast | Instant; refunds 50% of cost |
| **Knockdown / Ragdoll** | Launched, falls, must stand up | 1.5–3.0s |
| **Slow** | Movement & speed-gain reduced | 3–8s |

Secondary CC (Root, Silence, Daze, Mark, Fear) tracked in `08_ROADMAP.md` and added as new skills demand.

---

## Perception, world entities, and predictive reactions

The engine described above is purely *unit vs unit* — two move states overlapping in time. Real combat needs a third axis: **the world**, and units' awareness of it. This section adds the layer.

### The motivating example

Hero A casts **Triple Sign** — a 1-second rooted sign cast that fires a homing fireball. Hero B has Earth Wall available. Hero B's brain *sees* A entering the cast, *predicts* the fireball will arrive in N frames, *decides* to cast Earth Wall now (4-6 frames startup) so the wall stands between A and B before impact. The fireball fires, hits the wall, the wall takes damage, B is unscathed.

For this scene to work, three things must exist that don't today:

1. **Earth Wall is a thing in the world** — a destructible object with HP and collision, not a status effect on B
2. **B's brain can read A's *intent*** — knows A is in the startup of a blockable ranged attack, not just "A is doing something"
3. **The fireball checks for the wall** before reaching B — geometric hit resolution

Earth Wall is one example. The same infrastructure covers fire zones, ice floors, summons that body-block, marked-target effects, mines / traps, environmental destructibles, and any future "thing in the world that affects combat."

### World entities

A new entity type alongside units. Lives in `BattleEntityRegistry` (new subsystem on `TerrainBattleManager`):

```
abstract class CombatEntity : MonoBehaviour {
  int currentHP, maxHP;
  UnitRuntime owner;                // the caster (null for environment-spawned)
  EntityCategory category;
  
  bool blocksProjectiles;
  bool blocksMelee;
  bool blocksMovement;
  
  Bounds collisionBounds;
  float lifetimeSeconds;            // auto-despawn after; HP=0 also despawns
}

enum EntityCategory {
  Wall,         // Earth Wall, Ice Wall, Stone Pillar
  Hazard,       // Fire Zone, Lightning Field, Poison Cloud
  Summon,       // Guardian, orb, familiar (orbs already exist — fold into this)
  Projectile,   // Triple Sign fireball (becomes an entity, not a one-shot resolution)
  Trap,         // Mine, Rune, ground-marker
  Marker        // JJK-style flagged target, no collision
}
```

`BattleEntityRegistry` is the central list. Subsystems iterate it for hit resolution, brain perception, despawn ticking. Replaces the ad-hoc `OrbBuffHandler` / `BattleSummonManager` patterns long-term — they fold into this.

### Hit resolution with entities

Currently hit resolution is "is opponent in cone." Extended:

1. Attack move enters active frames
2. Cast a ray / sphere along the attack's reach toward target
3. **First check**: any `CombatEntity` with `blocksMelee` (or `blocksProjectiles` for ranged moves) intersecting the path?
4. If blocked → entity takes damage. If entity HP reaches 0 → entity destroyed. Attack does not reach target.
5. If no entity intercepts → existing resolution against opponent unit

Movement also respects `blocksMovement` entities — units path around them rather than through them. This handles walls naturally without special-casing.

### Perception stat

Add to `StatBlock`:

```
StatBlock {
  maxHP, attack, defense, moveSpeed   // existing
  perception                           // NEW
}
```

`perception` controls **how many frames in advance the brain can read opponent intent**. The mechanic:

- Engine each tick scans nearby units. For each opponent in startup of a move, computes "frames until impact."
- Filters by visibility: a unit's brain only "sees" a threat where `framesUntilImpact <= perception`.
- High-perception units see threats early, can prepare. Low-perception units see threats only at the last moment, can only react.

| Perception value | Visibility window | Effect |
|---|---|---|
| 1-3 | 50-150ms | Reflex-only. Can dodge but can't prepare. |
| 5-8 | 250-400ms | Standard. Can react to most attacks; can prepare for slow casts. |
| 10-15 | 500-750ms | Sharp. Can interrupt mid-cast, set up defensive walls preemptively. |
| 16-20 | 800ms-1s | Master. Reads almost any move at startup; nearly precognitive. |

Heroes start at perception 5-8 by default; this is one of the stats that progresses with mastery.

### BrainContext extensions

```
BrainContext {
  // existing fields ...
  
  // World awareness:
  List<CombatEntity> nearbyEntities;        // walls, hazards, summons in range
  List<UnitMoveState> alliedStates;
  List<UnitMoveState> enemyStates;
  
  // Perception-gated predictions:
  List<PredictedThreat> threats;
}

struct PredictedThreat {
  TerrainBattleUnit source;
  MoveDefinition incomingMove;
  int framesUntilImpact;
  bool isBlockable;          // can a wall stop it? (ranged signs: yes; melee: no)
  bool isAvoidable;           // can dodge clear it? (most yes; AOE no)
  AttackArchetype archetype;
  Vector3 expectedImpactPosition;
}
```

The threat list is rebuilt each tick by the engine. Visibility is gated by the unit's perception stat. **Without sufficient perception, the threat doesn't appear in the list.**

### `IStanceBrain` extends with `PickPreparation`

```csharp
interface IStanceBrain {
  MoveDefinition PickNeutral(BrainContext ctx);
  MoveDefinition? PickReaction(MoveDefinition incoming, BrainContext ctx);
  MoveDefinition? PickCancel(MoveDefinition current, BrainContext ctx);
  
  // NEW: every tick in neutral, brain may pick a *preparatory* move based on
  // its perceived threat list. Distinct from PickReaction (which fires when
  // a hit is imminent within a single frame). PickPreparation runs ahead of
  // time — "I see this coming, I should get ready."
  MoveDefinition? PickPreparation(BrainContext ctx);
}
```

Per-stance behaviour:

| Stance | Preparation tendency |
|---|---|
| **Conduit** | High — casts walls, fire zones, defensive signs preemptively |
| **Tactician** | High — reads opponent, picks interrupt-strike or counter-trap |
| **Sentinel** | Medium — interposes for ally if ally threatened; else stays put |
| **Stalwart** | Medium — same as Sentinel; protective preparation |
| **Wraith** | Low — prefers post-startup teleport flank to preemptive defense |
| **Onslaught** | Low — only acts on direct, immediate threats to self |
| **Tempest** | Low — committed to offense; rarely prepares |

### Skills that spawn entities

`MoveDefinition` extends with optional spawn fields:

```
MoveDefinition {
  // existing fields ...
  
  // Optional entity spawn — null for normal moves
  CombatEntityPrefab spawnsEntity;
  Vector3 spawnOffsetLocal;          // where, relative to caster
  float spawnRotationDegrees;        // facing — 0 = caster forward
  EntitySpawnPolicy spawnPolicy;     // facing target / facing forward / at target
}
```

Move triggers entity spawn on its `active` frame. Examples:

- `cast_earth_wall` spawns an `EarthWallEntity` 1.5u in front of caster, facing target
- `cast_fire_zone` spawns a `FireZoneEntity` at target's feet
- `cast_mark_target` spawns a `MarkEntity` attached to target (no collision, just data)
- `cast_summoning` spawns a `GuardianSummonEntity` next to caster

Same data shape covers all. New entity types are class extensions of `CombatEntity` — adding one is the same shape as adding a new unit type.

### Generalization beyond Earth Wall

The same infrastructure handles a broad family of features that would otherwise each be one-off code:

| Feature | Mechanism |
|---|---|
| **Earth Wall blocks projectile** | `EarthWallEntity` with `blocksProjectiles = true`; geometric hit resolution |
| **Fire zone damages over time** | `FireZoneEntity` ticks damage to units within bounds |
| **Mark target for ally bonus** | `MarkEntity` attached to target; ally moves with `consumeMark = true` get bonus |
| **Trap detonates on enemy** | `TrapEntity` proximity-triggers on enemy entry |
| **Summon body-blocks** | `GuardianSummonEntity` with collision; existing `BattleSummonManager` folds into entity registry |
| **Brain detects ally about to die** | `PickPreparation` reads `alliedStates`; chooses `cast_heal` or `interpose` |
| **Brain interrupts mid-cast** | `PickPreparation` reads `threats`; if opponent in long startup, picks `attack_interrupt_strike` |
| **Brain dodges out of AOE** | `PickReaction` (or `PickPreparation` for big AOEs) reads `expectedImpactPosition`, picks dodge in safe direction |
| **Brain takes cover behind ally summon** | `PickPreparation` reads `nearbyEntities`, repositions to put a friendly summon between self and threat |
| **High-perception hero counters everything** | Larger visibility window → more preparations available |
| **Low-perception hero gets surprised** | Threats visible only at last frame → forced into reactive (often bad) moves |

This is a real layer of depth. Combat goes from "two stat machines fighting" to "two minds reading each other and the world."

### Implementation cost

- `BattleEntityRegistry` MonoBehaviour subsystem (~150 LOC)
- `CombatEntity` base class + 1-2 concrete entity types initially (`EarthWallEntity`, `FireZoneEntity`) (~200 LOC)
- Hit resolution extension to check entities first (~30 LOC change in resolver)
- `BrainContext.threats` list build (~50 LOC, perception-gated scan each tick)
- `IStanceBrain.PickPreparation` hook + per-stance implementations (incremental — add one at a time)
- `perception` field on `StatBlock` (1 line)
- `spawnsEntity` field on `MoveDefinition` (3 fields) + spawn handling on active frame (~20 LOC)

Total ~500-700 LOC for the foundation. Adding new entity types or new perception-driven reactions thereafter is data + small extension code.

### When to land it

Not in the first slice of the engine rewrite. Build the move engine + brain + reaction table first; once those are stable, add this layer. The architecture is reserved (move definition has the optional `spawnsEntity` slot from day 1; `BrainContext` has the `threats` list field with empty default; perception field exists with default 8). The slot exists, the implementations land later.

---

## Animation contract

`UnitAnimationDriver.Play(string moveAnimationName)`:

1. Look up name in a registered `Dictionary<string, AnimancerTransition>` (Animancer path) or Animator state (legacy path)
2. If found, play it
3. If not found, fall back to nearest similar (`attack_punch_*` → fallback to a generic `anim_punch`)
4. If no fallback exists, no-op (simulation continues — combat doesn't break on missing animations)

Animation authoring is **decoupled** from gameplay logic. You can ship a fight system with placeholder Mixamo punches; later you author distinctive clips and bind them by name. **The names list in `MOVES_CATALOG.md` is the contract** — code references those names; clips authored under matching names automatically wire in.

---

## Determinism for replay

The 50ms tick + brain decisions + RNG seed = **fully reproducible fight**. A fight can be serialized as `(seed, list of brain decisions per tick)` and replayed bit-perfect. This enables async snapshot multiplayer in the long-term vision (`LONG_TERM_VISION.md`).

Implications for the engine:
- All RNG goes through a per-battle seeded random
- Brain decisions are deterministic given context
- No `Random.value` calls outside the seeded path during combat
- `Time.unscaledTime` and wall-clock are not used in combat math

---

## Stances — combat presets

---

## Design philosophy

Combat must feel like a *boxer dancing in the ring before striking* — not a JRPG turn order and not a shoot-em-up DPS race. The texture comes from three interlocked principles:

**Movement is meaningful.** Units don't just walk to the target and attack. They circle, retreat, re-engage, dash, weave. Movement generates resource that powers offense. A unit that stops moving becomes a worse fighter.

**Aggression is a commitment.** Big offensive output costs something real. Spending speed on a five-hit combo leaves the unit briefly vulnerable. The fighter who throws everything at once must rebuild before they can throw again. **Risk and recovery are the rhythm.**

**The AI thinks tactically, not just reactively.** Each unit's brain weighs current speed, current energy, current HP, target's state, and behavior type when picking what to do next. Different behaviors produce different rhythms. The combat is *legible* — the player can read why a unit chose to disengage or commit.

These three principles drive every system below.

---

## The resource model

Combat runs on three resources, each with a distinct role and rhythm:

| Resource | Built by | Spent on | Visible state |
|---|---|---|---|
| **HP** | Healing skills, slow regen out of combat | Damage taken | Floating health bar |
| **Energy** | Time in combat, frontline bonus, blocking | Skill costs (Hand Signs, Focus, etc.) | Below HP bar |
| **Speed** | Movement, specific kinetic skills | Combos, multi-hit attacks, dodging, dashing | Speed indicator (ring around the unit, glow, or animated effect) |

HP and Energy already exist in the current implementation. **Speed is the new layer.**

The three resources interact: a unit with high speed dodges better and lands harder hits, but spending speed on a combo leaves them sluggish; a unit out of energy can't cast elemental skills regardless of speed; a unit at low HP may behaviorally hoard speed for escape rather than spend it on offense.

---

## Stances — combat presets

A **stance** is a player-assignable preset that bundles the hero's combat disposition into a single switchable choice. Stances do not change the hero's identity, equipped skills, or attributes — they change *how* the hero fights. The same hero in Aggressive Stance versus Defensive Stance produces visibly different combat behavior with the same skill loadout.

### What a stance controls

A stance is a tuple of decision biases:

| Stance dimension | What it adjusts |
|---|---|
| **Behavior type** | Aggressive / Balanced / Defensive — the existing `BehaviorType` enum |
| **Speed-strategy preference** | How willing the hero is to spend speed (commit threshold) |
| **Target priority** | Default targeting policy (nearest / lowest HP / backline / marked) |
| **Dodge willingness** | How readily the hero spends speed on dodges |
| **Engagement willingness** | How quickly the hero advances from backline to frontline |
| **Disengage threshold** | At what HP / speed the hero retreats to rebuild |

These are six knobs that previously didn't exist as a unified concept. The stance bundles them so the player isn't tweaking six sliders per hero per mission — they pick one preset.

### Default stances

The game ships with a small number of canonical stances. Players don't *create* stances; they *assign* them. (Custom stances may come as an endgame unlock — tracked in `08_ROADMAP.md`.)

| Stance | Behavior bias | Speed posture | Target priority | Description |
|---|---|---|---|---|
| **Onslaught** | Aggressive | Spend liberally, low commit threshold | Nearest enemy | Constant pressure, high risk, looks for kills |
| **Tempest** | Aggressive | Build aggressively, dump in big bursts | Backline-first | Burst skirmisher; closes fast, commits hard |
| **Stalwart** | Defensive | Hoard speed, commit only when primed | Whoever attacks ally | Protector role; holds position, supports allies |
| **Tactician** | Balanced | Adaptive — reads situation | Lowest-HP enemy | Smart finisher; picks fights they can win |
| **Wraith** | Aggressive | High mobility, dodge-heavy | Backline-first | Hit-and-run assassin; never commits to standing fights |
| **Sentinel** | Defensive | Build slowly, never spend below 30 | Marked target only | Static defender; chooses one target and stays on it |
| **Conduit** | Balanced (caster-leaning) | Speed only as emergency dodge fund | Furthest enemy | Caster role; energy-focused, range-focused |

Stances are configured in the Hero Config menu (per `06_HERO_CONFIG.md`) — one stance assignment per hero per mission. Stances can be changed between missions but not during.

### Why stances matter

Three reasons stances earn their complexity:

1. **They give the player tactical agency without scripting.** The same hero, same loadout, fights very differently across stances. This rewards mission-specific preparation without requiring rebuilding skill chains.

2. **They give the AI strong personality differentials.** Two units in the same Aggressive behavior type but different stances still fight visibly differently. The brain has more dimensions to work with.

3. **They support the *roster of specialists* design from `LONG_TERM_VISION.md`.** A hero can have a "natural" stance that fits their build, and the player can experiment with off-stances for unusual missions. This is what makes a 20-hero roster feel deep instead of redundant.

### How stances integrate with the AI brain

The `BattleAIBrain` (defined in detail later in this doc) reads the hero's assigned stance during the Decide tick. The stance modifies:

- The threshold table for archetype selection (a Stalwart unit needs Speed > 80 to commit a big combo; a Wraith needs only Speed > 40)
- The movement intent bias (Stalwart prefers Hold; Wraith prefers Circle)
- Target priority (overrides the default `BattleTargetFinder` nearest-enemy logic when the stance specifies otherwise)

Stances do *not* override hard requirements (energy costs, speed gates, HP-critical survival logic). They shape preferences, not break rules.

---

## Speed — the kinetic resource

### Core concept

Speed represents the unit's *kinetic momentum* — how primed they are to strike. A unit with high speed is dancing on the balls of their feet, ready to explode into action. A unit with zero speed is standing flat-footed, vulnerable.

Speed is **earned through real movement** and **spent on real action**. It cannot be regenerated by standing still. There is no passive speed regeneration — only kinetic activity grants it.

### Speed pool and caps

| Property | Value |
|---|---|
| Minimum speed | 0 |
| Soft cap | 70 |
| Hard cap | 100 |
| Starting speed | 30 (combat begins with units already in motion from approach) |
| Drain rate (idle) | -5/sec while completely stationary in combat |
| Drain rate (blocking) | -2/sec while holding block |
| Drain rate (knocked down) | -10/sec while staggered |

**The soft cap matters.** Building speed past 70 costs progressively more — you have to actively maintain motion at the soft cap to push higher. This means the 70-100 range represents a *primed* state, expensive to maintain but powerful to spend. Most of the fight lives in 20-65.

### How movement builds speed

Movement type determines the build rate. The more aggressive and committed the movement, the faster speed builds — but the more it exposes the unit.

| Movement type | Speed gain rate | Risk profile |
|---|---|---|
| Standing still | -5/sec | Safe but bleeds speed |
| Walking | +0/sec | Neutral |
| Walking with block raised | -2/sec | Defensive but bleeds |
| Backing away from target | +3/sec | Defensive build, slower |
| Circling target at mid-range | +6/sec | Tactical build, balanced risk |
| Closing distance to target | +8/sec | Aggressive build, exposes self |
| Sprinting (any direction) | +10/sec | Fast build, animation-locked briefly |
| Dashing (skill-driven) | +15 instant burst | Burst build, costs energy |

**Direction matters.** Circling builds at +6/sec because it represents skilled, evasive footwork — the unit is mobile but not committed. Closing distance builds faster (+8/sec) because the unit is *committing forward* — they're paying for speed with positional risk. Backing away builds slowest (+3/sec) because there's no real risk involved.

### How skills manipulate speed

Skills can interact with speed in five ways:

**1. Skills that cost speed (offensive output).** Most multi-hit combos and aggressive techniques consume speed proportional to their power. The cost is paid on cast.

**2. Skills that grant speed (kinetic skills).** Some skills simulate kinetic burst — a Wind-element dash, a sprint-skill, a dodge-roll cast. These are *alternatives* to running for speed — slower per-second but useful when terrain or AI doesn't permit running.

**3. Skills that scale with speed.** Damage on certain skills increases proportionally to the unit's current speed. A flying kick from a high-speed unit hits much harder than from a sluggish one.

**4. Skills that gate on speed.** Some advanced combos cannot fire unless the unit's speed is above a threshold. This represents techniques that require kinetic prerequisite — you can't throw a flying kick from a standstill.

**5. Skills that affect enemy speed.** A few specific skills *steal* or *shatter* enemy speed. A heavy stagger blow drops the target's speed by 30. A "destabilize" skill caps the target's speed for 5 seconds. These are tactical denial tools.

### Speed cost matrix (target values)

These are starting values — to be tuned. The rule is: power scales with cost.

| Action | Speed cost |
|---|---|
| Punch (basic) | 0 |
| Kick (basic) | 5 |
| Hand Sign A/B/C (cast) | 0 (sign is internal, not kinetic) |
| Focus (cast) | 0 |
| Combo Strike (Punch → Kick) | 10 |
| Earth Fist / Thunder Fist / Water Fist | 15 |
| Power Strike / Crescent Kick | 20 |
| Geomagnetic / Thunderstorm / Mudslide | 10 (these are mobile, partially compensate) |
| Triple Sign | 0 (rooted, no kinetic component) |
| Elemental Fist / Elemental Storm | 35 (heavy commitment) |
| Summoning | 0 (rooted, ritual) |
| Orb Strike (combo trigger) | 0 (the orbs are the offensive output, not the trigger) |
| Each orb fired (per punch) | 5 (the punch that fires it spends speed) |
| Dodge (parabolic backflip) | 25 |
| Skill-driven dash | -15 (grants speed) |
| Wind Burst (future) | -25 (grants speed) |

### Speed's effects on combat performance

Current speed continuously modifies the unit's combat performance in three ways:

**Dodge chance.** Existing dodge formula is `SPD stat × 5%`. Updated formula: `(SPD stat × 5%) × speed_modifier`, where `speed_modifier = 0.5 + (current_speed / 100)`. So a unit at 0 current speed dodges at half their stat-based chance; a unit at 100 current speed dodges at 1.5× their stat-based chance.

**Damage modifier on certain attacks.** Speed-scaling skills (flying kicks, sprint-charged strikes) deal `base_damage × (1 + current_speed / 200)`. A flying kick at 0 speed = base damage. At 100 speed = 1.5× damage.

**Animation speed.** Visually, attack animations play 10-20% faster at high speed. This is a *presentation* effect, not a gameplay-truth effect — combat math doesn't change, but the unit *looks* faster. This sells the kinetic state visibly.

### Visualizing speed

Critical for readability per `07_PRESENTATION.md`. The player must understand at a glance whether a unit is primed or sluggish.

**Visual indicators by speed band:**

| Band | Visual cue |
|---|---|
| 0-20 (sluggish) | Heavy footsteps, slow blink, dust at feet, animations slightly weighty |
| 20-50 (engaged) | Default, no special effects |
| 50-70 (sharp) | Subtle motion blur or trail when moving |
| 70-100 (primed) | Strong trail effect, faint glow, eyes light up — clear "ready to strike" telegraphing |

These cues let the player read the combat at a glance: "Kai is winding up, he's about to commit something big" / "Mira is sluggish, she needs to disengage and rebuild."

---

## Spatial combat choreography

This section is **load-bearing for combat feel**. The systems above (speed, exchanges, stances, CC) describe *what* combat decides; this section describes *how units move while it's deciding*. Anime combat reads as combat because units **occupy and contest space** — not because their swings animate well. Animation decorates a positional pattern that's already legible.

Anything authored as a combat behavior must be expressed in terms of the primitives below. New skills compose primitives; new stances pick different default primitives; the visible difference between an Onslaught fighter and a Wraith fighter is *which primitives their brain reaches for*, not which clips play.

### Reference moments

The choreography is reverse-engineered from these specific anime moments. New primitives must trace back to one of these (or a clearly similar reference):

| # | Reference | The visible moment | Primitive(s) |
|---|---|---|---|
| 1 | Rock Lee fade-strikes around Gaara | Lee blink-strikes from 4 different angles in succession; Gaara's body doesn't track him | `TeleportFlank`, `GhostTrail`, defender = `StaticDefense` |
| 2 | Lee orbits Gaara, opening gates | Sustained circle at mid-distance; Gaara stands centered; speed visibly building | `OrbitTarget(distance, dir, durationSec)` as the canonical speed-build primitive |
| 3 | Naruto uppercut → aerial combo → flying kick on Sasuke | Upper launches, attacker follows up with N strikes mid-air, final kick flings target far | `LaunchAttack`, `AerialFollowUp(strikes, interval)`, `KnockbackFar` (3-segment combo) |
| 4 | Naoya kick sends Maki flying (JJK) | Single high-impact finisher; defender goes Close → Far in one beat | `KnockbackFar` (cinematic-scale, distinct from existing knockback) |
| 5 | Zuko's fire-punch frozen pose (Avatar) | Attacker freezes in pose at impact, effect emanates, *the moment* is held | `PoseAttack(holdMs, vfx)` — hit-stop given dramatic form |

Future primitives must pass the test: **can I name an anime moment this is reverse-engineered from?** If not, it's noise.

### Range bands

A unit's distance to its current target falls into one of four bands. Each band has rules for what's allowed and what's typical. The brain reads the band the same way it reads speed posture.

| Band | Distance | Allowed primitives | Default behaviour |
|---|---|---|---|
| **Far** | > 8u | Cast Rooted, observe, OrbitTarget(8) | Conduit casts; Wraith readies fade; pre-engagement courtship |
| **Mid** | 3–8u | Closing, circling, ranged casts, stance-courtship | Most exchanges initiate from here; Beat snaps back to here |
| **Close** | 1–3u | Strike sequences, basic exchange, lateral dodge | Onslaught/Tempest live here |
| **Locked** | < 1u | Mutual contact, parry-counter window, pose-attacks | Brief — Beat forces back to Mid |

Range is real tactical state. Pre-engagement at Far (two units reading each other while circling) is the *most* anime moment and is currently absent. Post-exchange separation (Beat back to Mid) is what creates rhythm.

### Movement primitives library

These are the verbs the brain composes. Speed band gates which primitives are *unlocked* — the same gating rule as resource gates: a unit that can't afford the speed cost literally can't perform the visual.

```
Always available (Sluggish+):
  WalkTo(target, speed)
  StandStill()
  FaceTarget(target)
  StaticDefense()                         // Gaara-style anchored block

Engaged band (≥20 speed):
  RunToward(target, distance)
  Strafe(direction, distance)
  DisengageBackstep(distance)
  OrbitTarget(distance, direction, durationSec)   // Reference 2
  ReengageFrom(angle, distance)

Sharp band (≥50 speed):
  DashTo(position, durationMs)            // 0.15-0.30s straight-line
  SidestepDodge(direction, distance)      // lateral evade in-place
  BobWeave(magnitude)                     // narrow-window in-place dodge
  BaitAndPunish()                         // Aang-style: invite commit, sidestep, counter

Primed band (≥70 speed):
  TeleportFlank(targetRelative, fadeMs)   // Reference 1: Lee fade-strike
  GhostTrail(fromPos, toPos, ghostCount)  // Reference 1: visual fade trail
  AerialClash(altitude, durationSec)      // brief mutual launch
  Domain(radius, durationSec)             // JJK locked-mode exchange

Cast-driven (skill specifies):
  LaunchAttack(target, launchHeight)      // Reference 3: Naruto upper
  AerialFollowUp(strikeCount, interval)   // Reference 3: mid-air combo
  KnockbackFar(direction, distance)       // References 3, 4: cinematic knockback
  PoseAttack(holdMs, vfx)                 // Reference 5: Zuko frozen pose
  PassThroughStrike(target)               // attacker ends behind defender
```

Every primitive is a coroutine-shaped operation: it has a duration, locks the unit's state machine for that duration, and reports completion. The brain queues primitives the same way it queues abilities.

### Per-phase exchange choreography

The 6-phase exchange (Initiation → WindUp → StrikeSequence → Resolution → Beat → ReEvaluation) is currently mechanical only — every phase looks like "stand and swing." It must be *positional*:

| Phase | Duration | Positional rule |
|---|---|---|
| **Initiation** | 0.3–0.5s | Attacker moves Mid → Close via `RunToward` (Engaged) / `DashTo` (Sharp) / `TeleportFlank` (Primed). Defender role-locks. |
| **WindUp** | 0.2s | Both units brief stillness. Attacker plays a tell pose (`Ctx.Anim.PlayWindUp`); defender's brain picks reaction. **The viewer reads the commit.** |
| **StrikeSequence** | 0.5–2s | Multi-strike combo. Attacker may stay Locked OR `TeleportFlank` between strikes (Primed) OR full sequence is one `GhostTrail` pass-through. Defender reaction per strike. |
| **Resolution** | 0.2–0.5s | Last hit lands with `PoseAttack` if heavy. `KnockbackFar` if cinematic finisher. |
| **Beat** | 0.4s | Both units forced to Mid via brief `DisengageBackstep` — **non-skippable**, this is the manga panel-break. |
| **ReEvaluation** | — | Brains tick. New exchange may begin from Mid; defender may convert into attacker if speed/posture shifted. |

The Beat is the most important and most often missed phase. Without it, exchanges blur into a continuous slap-fight. The Beat is the visual quiet that lets the *next* exchange land.

### Defender response variants

Brain picks one per strike, not per exchange. Stance gates which are available. **A multi-hit combo can have a different response per strike** — block hit 1, dodge hit 2, eat hit 3.

| Response | Mechanical | Visual | Stance permission |
|---|---|---|---|
| Eat | Take full damage | Hit react animation | All |
| Block | -50 % dmg, +5 energy | Block stance | All except Wraith |
| Static-backstep | Avoid + 3.5m back | Existing parabolic dodge | All except Sentinel/Stalwart |
| Lateral sidestep | Avoid + sidestep, stay in range | `SidestepDodge` | Engaged+ |
| Bob-weave | Avoid + in-place narrow window | `BobWeave` | Sharp+ |
| Parry | Avoid + 0.5s counter window | Mutual brief freeze | Tactician/Wraith only |
| Counter | Take attacker role for next strike | Defender swings | Tactician (with stance permission) |
| Defensive teleport | Avoid + relocate to Mid | Fade-out, fade-in elsewhere | Wraith only, Primed band |
| StaticDefense (Gaara) | -75 % dmg, no movement | Anchored block, no body motion | Sentinel only |

Brain selection priority: stance → speed band → cooldowns → posture. A Wraith in Primed band defaulting to Defensive Teleport is what makes Wraith *visibly different*.

### Per-stance choreography signature

Every stance has a default movement signature — the visible identity that survives even when the stance has the same `BehaviorType` as another. This is what makes Tempest and Onslaught (both Aggressive) read as different fighters.

| Stance | Pre-engagement | Strike sequence | Post-strike | Identifying primitive |
|---|---|---|---|---|
| Onslaught | RunToward, Close fast | Sustained pressure, all Close | Stay Close, re-engage | Constant `RunToward(Close)` |
| Tempest | DashTo from angle | Big single combo + pose | DisengageBackstep | Pre-strike `DashTo` from Mid arc |
| Stalwart | StandStill at Mid | Static counter only on parry | StandStill | Never moves first |
| Tactician | OrbitTarget at Mid | BaitAndPunish + Counter | DisengageBackstep | `BaitAndPunish` + `Counter` |
| Wraith | OrbitTarget at Far | TeleportFlank chain (3-5) | Defensive teleport | `TeleportFlank` + `GhostTrail` |
| Sentinel | StandStill at Mid | PoseAttack only when struck | StandStill | `StaticDefense`, no chase |
| Conduit | OrbitTarget at Far | Cast Rooted only | DisengageBackstep | Stays at Far, never closes |

A test fight should be runnable for any 1v1 of stances and produce **visibly distinct combat** based on signatures alone, even before unique animation clips exist.

### Speed-band as visual-cheating budget

Speed isn't just a damage modifier — it's the **legible currency for which moves are available**. The viewer learns:

- Sluggish (0–20): walk, stand, basic block
- Engaged (20–50): run, strafe, basic dodge, orbit
- Sharp (50–70): dash, sidestep, bob-weave
- Primed (70–100): teleport, ghost-trail, fade-attack

A Lee in Sluggish band literally cannot fade — the primitive is gated. The player watches the speed bar climb during the orbit, then watches him *vanish* mid-swing. That sequence — orbit → bar fills → primitive unlocks → fade-attack lands — is the design loop. It collapses the gap between resource state and visible capability.

This rule produces the Rock Lee/Gaara opening-gates scene mechanically: Lee cannot fade until he's earned the band, the orbit is what earns it, the fade is what spends it.

### Defender mismatch reads as combat texture

Reference 1's payoff is that *Lee fades while Gaara stands still.* The mismatch is the read. Stalwart and Sentinel stances **do not chase** — they `StaticDefense`. Watching a Wraith fade around a Sentinel produces the right visual without either side authoring complex behaviour. The fight is one animation primitive (`TeleportFlank`) on the attacker and one (`StaticDefense`) on the defender, composing into the iconic moment.

This is why per-stance signatures matter: they create natural mismatches that the viewer recognizes as combat texture.

---

## The unit AI brain — Decide-tick logic

This is where speed integrates with everything else. Every time a unit enters the `Decide` state, the brain runs through this decision tree:

### Step 1 — Survival check

If the unit is at critical HP (< 25%) and has retreat options:
- **Defensive/Balanced behaviors:** strong bias toward disengage and rebuild speed at distance
- **Aggressive behaviors:** weaker bias — may still commit if a kill is on the table

If the unit is at critical HP and has no retreat options, fall through to the regular logic with a panic modifier (more likely to commit aggressively).

### Step 2 — Resource posture

The brain reads three flags:

- **Speed posture:** Sluggish (< 30), Engaged (30-65), Primed (> 65)
- **Energy posture:** Drained (< 20), Mid (20-70), Loaded (> 70)
- **HP posture:** Wounded (< 50%), Healthy (≥ 50%)

The combination drives the next step.

### Step 3 — Action archetype selection

Based on the resource posture and behavior type, the brain selects an action archetype:

| Posture combination | Aggressive AI picks | Balanced AI picks | Defensive AI picks |
|---|---|---|---|
| Primed + Loaded + Healthy | Big combo (Elemental Fist tier) | Mid combo + setup | Mid combo, hold reserves |
| Primed + Loaded + Wounded | Mid combo (kill commit) | Mid combo or disengage | Disengage and rebuild |
| Primed + Drained + Healthy | Physical combo | Physical combo | Wait/circle |
| Primed + Drained + Wounded | Desperate big spend | Disengage | Disengage |
| Engaged + Loaded + Healthy | Build phase (sign casts) | Build phase | Build phase |
| Engaged + Loaded + Wounded | Cautious commit or build | Build phase or disengage | Disengage |
| Engaged + Drained + any | Basic punches while building | Reposition | Reposition or block |
| Sluggish + any + any | **Build speed** (running, circling) | **Build speed** | **Build speed** at safe distance |

The archetype is then translated into a specific skill choice (from the unit's equipped skills) and a specific movement pattern.

### Step 4 — Skill resolution

Once the archetype is chosen, the existing `SkillSystem.PickBestSkill` runs with archetype as a hint:

- "Big combo" → highest-power affordable skill
- "Mid combo" → moderate-power skill, prefer ones with lower risk profile
- "Physical combo" → physical-only chains (no element setup needed)
- "Build phase" → elemental sign casts (set up future combos)
- "Build speed" → no skill cast; transition to movement state instead

### Step 5 — Movement intent

Independently of skill, the brain sets a movement intent for the next state cycle:

- **Close** — drive toward target (high speed gain, exposes self)
- **Circle** — maintain distance while moving (mid speed gain, evasive)
- **Disengage** — increase distance from target (low speed gain, safe)
- **Hold** — maintain current position
- **Dash** — burst movement (consumes energy, grants speed)

The state machine then enters the appropriate state (Melee for close, CastMobile for skill-while-moving, etc.) with the movement intent layered on top.

### Behavior personality differentials

Behavior types differ on these brain-level parameters:

| Parameter | Aggressive | Balanced | Defensive |
|---|---|---|---|
| Threshold to commit big combo | Speed > 50 | Speed > 65 | Speed > 80 |
| Threshold to disengage when wounded | HP < 30% | HP < 50% | HP < 60% |
| Tolerance for staying sluggish | 2 sec | 1 sec | 0.5 sec (immediately rebuilds) |
| Movement style preference | Closing | Circling | Backing away |
| Dodge willingness when low speed | High (gambles) | Medium | Low (saves dodge for full speed) |
| Target switching frequency | Low (commits to a target) | Medium | High (avoids being pinned) |

These create *visibly different* combat rhythms across behaviors. An Aggressive unit cycles fast, commits often, dies dramatically. A Defensive unit waits patiently, builds patiently, strikes precisely.

### Implicit resource focus — reading the hero's loadout

The brain doesn't weigh all resources equally for every hero. A Rock-Lee-style speed-focused hero needs to think about speed *constantly*; a healer-caster barely thinks about speed at all unless their HP is critical and they need to dodge. Hard-coding "this hero is a caster" in their `UnitDefinition` is brittle and breaks the moment a player puts a different loadout on them.

Instead, the brain **reads the hero's equipped skills at battle start** and infers their resource focus. This makes the AI feel like it understands the hero rather than reading a config file — and it adapts automatically when the player rebuilds a loadout.

#### The inference

When a unit enters the battle, `BattleAIBrain` analyzes the unit's `equippedSkills` and computes a focus profile:

| Profile axis | How it's computed |
|---|---|
| **Speed weight** | Total speed cost across all equipped skill chains, normalized. High = speed-focused. |
| **Energy weight** | Total energy cost across all equipped skill chains, normalized. High = caster. |
| **Speed-gating dependence** | Number of skills with `speedGate > 0`. High = needs primed state to function. |
| **Element diversity** | Number of distinct elements across the loadout. High = sign-caster. Low = physical specialist. |
| **Cast type ratio** | Ratio of Rooted / Mobile / Melee skills. Heavy on Rooted = caster; heavy on Melee = brawler. |

These axes combine into one of these inferred archetypes (which are *brain-level*, not player-facing):

| Inferred archetype | Profile signature | Brain weighting |
|---|---|---|
| **Brawler** | High speed weight, low energy weight, melee-heavy | Prioritize speed posture; energy is bonus |
| **Burst Striker** | High speed-gating dependence, high speed weight | Speed posture dominant; refuses to commit when sluggish |
| **Caster** | Low speed weight, high energy weight, sign-heavy, rooted-heavy | Energy posture dominant; speed only as emergency dodge fund |
| **Hybrid** | Moderate everything | Balanced weighting; adapts to fight tempo |
| **Healer** | Low speed weight, high `Heal` technique-type count | Energy + ally HP dominant; speed only when self-threatened |

The brain stores this archetype on the unit at battle start and uses it to weight decisions. **The player never sees this.** It's emergent intelligence — the hero just *fights smarter for who they are*.

#### Concrete behavioral differences

Same Decide tick, same resource posture (Engaged speed, Loaded energy, Healthy HP), three different inferred archetypes:

- **Brawler:** "I have the energy I need. Build speed and commit a physical combo."
- **Burst Striker:** "Speed is only Engaged. I can't fire my flying kick yet. Sprint and circle until I'm Primed."
- **Caster:** "Energy is loaded — that's all that matters. Cast Triple Sign now from current position."

This is what makes a roster of 20 heroes feel like 20 distinct fighters. A speed-focused hero with five speed-gated skills *constantly works to build speed*. A caster with five sign-heavy skills *barely moves*. The same brain, weighted by the hero's loadout.

#### Re-inference on loadout change

If the player edits a hero's skill loadout (between missions), the inference runs again at next battle start. There's no manual config to update.

#### Stance × inferred archetype

Stance and inferred archetype interact, but the inferred archetype usually wins for fundamental decisions. A Caster in Onslaught Stance still won't try to commit to melee combos — they don't *have* the skills for it. But Onslaught Stance pushes them to cast more aggressively, accept more risk, prioritize damage targets. Stances modulate; archetype constrains.

---

## Exchanges — the structured back-and-forth

Anime combat reads as *fights* rather than *simultaneous combat actions* because attackers don't just trade blows in parallel — they take **turns within structured exchanges**. Hero A throws a flurry. Hero B blocks, dodges, eats hits. There's a beat. Hero B counters. Hero A reacts. The fight has a *rhythm*, like a conversation.

The current implementation has a fragment of this — `BattleExchangeCoordinator` uses an `IsAnimating` lock to prevent role-swap conflicts. That's the *foundation* of an exchange system. This section defines the full design.

### The exchange concept

An **exchange** is a brief structured interaction between two units, with explicit attacker/defender roles and a defined sequence of beats. During an exchange:

- One unit is the **active attacker**, committed to a combo or skill
- One unit is the **active defender**, locked into reactive options (dodge, block, take damage)
- Other units in combat continue their own state machines independently — exchanges are **per-pair**, not battlefield-wide
- The exchange ends when the attacker's combo finishes, the defender successfully creates distance, or either unit is interrupted (knocked back, killed, dodges away)

After an exchange ends, roles can swap immediately if the defender chose to counter, or both units can disengage and the AI brain re-evaluates.

### Why exchanges matter

Without them, combat looks like *two units running their own loops in proximity to each other*. Both attack, both defend, hits trade randomly, and the player can't tell where one fighter's "turn" ends and another's begins. With exchanges, combat reads as *intentional back-and-forth*. Two units circling, one commits, the other reacts, the moment resolves, beat, then the other commits.

This is what gives anime combat its narrative legibility. A 90-second fight is **a series of exchanges**, not a continuous DPS race.

### The exchange lifecycle

An exchange has six phases. Each one is short — most of the exchange runs in 1-3 seconds.

```
1. Initiation       — Attacker commits; defender locked into reactive role
2. Wind-up          — Brief moment before first hit; defender chooses reaction
3. Strike sequence  — Attacker's combo plays out; defender absorbs/dodges/blocks
4. Resolution       — Last hit lands; hit-stop fires; knockback applied if any
5. Beat             — Brief pause where neither unit acts (~0.3-0.5s)
6. Re-evaluation    — Both units' brains tick; new exchange may begin or both disengage
```

Phase 5 — **the beat** — is the most important and most often overlooked. It's the visual moment where the player sees the result of the exchange before the next thing happens. Without it, exchanges blur together and combat becomes unreadable. The beat is *non-skippable*; even Aggressive units respect it.

### When does an exchange initiate?

An exchange begins when **all** of these conditions are met:

1. An attacker enters `Execute` state with a melee or close-range skill
2. The target is within engagement range
3. The target is not already locked in a different exchange
4. Neither unit is in a hard-interrupt state (knocked down, dead, mid-dodge)

If conditions are met, `BattleExchangeStager` (new subsystem, defined below) registers the pair as locked into an exchange and assigns roles.

If the target *is* already in another exchange, the new attacker either:
- Queues to attack after the current exchange ends (if patient — Defensive/Tactical stances)
- Targets a different enemy instead (if impatient — Aggressive stances)
- Joins the exchange as a "third party" (rare; only if `BattleMeleeTokenSystem` permits — most units don't)

### What the defender does

When a unit becomes the defender in an exchange, their brain doesn't run normal Decide logic. Instead, a streamlined **defender response** kicks in:

| Response | Trigger conditions | Effect |
|---|---|---|
| **Dodge** | Speed ≥ 25, dodge cooldown ready, dodge chance roll succeeds | Avoids the strike entirely, exits exchange via mobility |
| **Block** | Block roll succeeds (DEF × 2%) | Reduces damage 50%, gains 5 energy, stays in exchange |
| **Counter** | Speed ≥ 50, defender's brain has a fast-cast counter skill, stance permits | After the strike resolves, defender takes attacker role; exchange continues with roles swapped |
| **Eat the hit** | None of the above succeed | Takes full damage, may be knocked back, exchange ends |

The defender's response is decided per-strike, not per-exchange — a multi-hit combo gives the defender multiple chances to escape or react.

### What the attacker does

The attacker doesn't choose individual strikes; they're committed to the combo they entered with. But the attacker's brain monitors the exchange:

- If the defender successfully dodges *the first strike*, the attacker can **abort** the rest of the combo (recovers ~70% of speed cost)
- If the defender successfully **counters**, the attacker enters defender role with no transition delay
- If the defender is **defeated mid-combo**, the attacker exits the exchange immediately and re-enters Decide

This makes long combos a real commitment — they pay off if they connect, but the attacker is exposed to counter risk throughout.

### Exchange and the speed system

Speed and exchanges interact in concrete ways:

- The attacker pays speed cost on initiation (the combo cost)
- Each strike that lands gives the attacker a small speed refund (+3 per landed strike) — successful aggression rewards momentum
- Each strike that's dodged costs the defender 5 speed (the dodge keeps them safe but bleeds momentum)
- A successful counter costs the original defender 30 speed (counters are expensive but devastating)
- After the beat, both units' speed values are visible to each other's brains — the next decision is based on the post-exchange resource state

### Exchanges and group combat (3v3, 5v5)

In larger fights, exchanges happen *concurrently* across the battlefield. A 3v3 might have:

- Hero A vs Enemy A in an exchange (Hero A attacking)
- Hero B and Enemy B circling, neither committed
- Hero C in Repositioning, building speed
- Enemy C casting Triple Sign at range

The battlefield is busy but legible because each unit pair has clear state. The camera framing (presentation concern, see `07_PRESENTATION.md`) emphasizes the most active exchange at any moment.

### Avoiding the "everyone freezes" failure mode

The naive implementation of exchanges produces a bug: when one unit initiates, everything else stops. **Exchanges must remain local.** Other units in combat continue independently. The `IsAnimating` lock from the existing `BattleExchangeCoordinator` only applies to the two units in the exchange — it does not affect anyone else.

Tested in playtesting: if combat ever feels "stiff" or "everything pauses," the exchange system has been over-globalized. Roll back the lock scope.

---

## Crowd control and status effects

Combat depth comes from skills that don't just deal damage but *change what the target can do*. Stuns, interrupts, knockdowns, slows — these are the mechanics that turn straightforward damage trades into tactical exchanges where positioning, timing, and skill selection matter beyond raw DPS.

Crowd control (CC) is implemented as a specialized layer of the broader **status effect system** described in `08_ROADMAP.md`. CC effects are status effects that specifically modify combat capability rather than just dealing damage over time or modifying stats.

### Primary CC effects (build these first)

The combat system commits to these five effects as primary. New skills should design around this set; new CC types are added rarely and deliberately.

| Effect | Mechanical behavior | Typical duration |
|---|---|---|
| **Knockback** | Target is forcibly moved away from attacker along the strike vector | Instant (movement plays out over ~0.4s) |
| **Stun** | Target cannot act, cannot move, cannot dodge or block | 1.0-2.5s |
| **Interrupt** | Cancels target's current cast or active skill execution | Instant; refunds 50% of action cost |
| **Knockdown / Ragdoll** | Target is launched, falls, and must stand up | 1.5-3.0s total (launch + ground time + recovery) |
| **Slow** | Target's movement and speed-gain rates are reduced | 3-8s |

Note that Knockback already exists in the current implementation (`BattleKnockbackSystem`). Formalizing it as part of the CC catalog ensures consistent integration with the new effects.

### Secondary CC effects (future additions, lower priority)

These are tracked in `08_ROADMAP.md` as future systems. They expand the CC catalog without changing the primary architecture:

- **Root** — cannot move but can still cast (caster-counter to interrupts)
- **Silence** — cannot cast Elemental or Support actions (anti-caster denial)
- **Daze** — temporary SPD attribute reduction (slows execution timing without canceling action)
- **Mark** — flagged as priority target; next ally attack against this target gets bonus
- **Fear** — forces target to disengage and back away (anti-aggression tool)

These are designed as new skills demand them, not pre-built.

### How each primary effect integrates with combat systems

#### Knockback

Already implemented. Integration with the new design:

- Triggered by `BattleCombatResolver` on landed strike (some skills always knock back, some have a chance, some never)
- Suppressed when the target is blocking (existing rule)
- New: triggers terrain interaction if the target collides with environment features (see `ENVIRONMENT_DESIGN.md`)
- New: causes target to lose 5-15 speed depending on knockback severity
- Does **not** cancel the target's current state — the target slides backward, then resumes whatever they were doing

Knockback is the *least disruptive* CC. It's a positional reset, not a tactical lockout. This is intentional — it's the right "default" CC to use generously.

#### Stun

The most disruptive CC. Hard interrupt, no actions allowed. Implementation:

- Applied via a `StatusEffect` of type Stun on the target's `UnitRuntime`
- `BattleStatusEffectSystem` (new subsystem) ticks the duration each frame
- During Stun:
  - State machine forces the target into a `Stunned` state (new state — see State machine extensions below)
  - All input to the target's brain is ignored
  - Speed drains at 2× normal rate (8/sec instead of 4/sec)
  - Target cannot dodge or block
  - Target visible state: stunned animation loop (per Animancer integration)
- On expiration:
  - Target re-enters `Decide` from a fresh evaluation
  - 0.5s post-stun grace period before they can be re-stunned (anti-stun-lock)

Stuns interrupt exchanges. If a stunned unit was the active attacker mid-combo, the combo aborts with 70% speed refund. If a stunned unit was the active defender, the exchange resolves with the stunned unit eating remaining strikes at default damage.

#### Interrupt

A surgical version of Stun. Specifically targets cast actions:

- Triggered when a target in `CastMobile` or `CastRooted` state takes damage above an interrupt threshold (typically 15% of max HP per hit, tunable per skill)
- The cast aborts immediately
- Energy cost is 50% refunded
- Target enters a brief `Stagger` micro-state (~0.4s, can't act, but can still receive subsequent CC)
- Target then re-enters `Decide`

Interrupts are what make caster heroes *vulnerable*. A Triple Sign cast (1.0s rooted) is a 1-second window where the hero is exposed. Without interrupt, casters can fire freely from the back. With interrupt, casters need protection — either tank teammates absorbing aggression, or stances that hoard speed for emergency dodges.

Some skills have an explicit `causesInterrupt` flag — they always interrupt regardless of damage threshold. These are designed as anti-caster tools.

#### Knockdown / Ragdoll

The dramatic CC. Knockdown and Ragdoll are *the same mechanic with two visual presentations*:

- **Knockdown**: target is launched into a low arc, lands, prone for ~1.5s, stands up over ~0.5s
- **Ragdoll**: target physics-flops briefly (~0.6s), then transitions to prone, stands up over ~0.5s

Mechanically identical from the combat resolver's perspective. The visual choice is per-skill — `causesRagdoll: true` on heavy combat strikes, `causesKnockdown: true` on launch attacks.

Implementation:

- Triggered by skills with `causesKnockdown` or `causesRagdoll` flags
- Target enters `KnockedDown` state (new state)
- Speed = 0 while knocked down
- Cannot dodge or block; cannot be stunned (already prone)
- *Can* take damage with a 1.5× multiplier — knocked-down units are vulnerable, this is the payoff for landing a knockdown
- On stand-up, target is briefly in Stagger (~0.4s) before re-entering Decide
- 2.0s post-knockdown grace period (anti-perma-knockdown)

Knockdown is the *premium* CC. It's expensive to apply (high speed cost on the originating skill), it's powerful when it lands (vulnerability window for follow-up), and it's the most cinematic moment in any fight. The bonus damage on knocked-down targets is what makes setup-and-payoff combo design work — Hero A knocks down, Hero B finishes.

This is the moment the player records and shares.

#### Slow

The least disruptive CC, the most chess-like. No interruption, no lockout — just degradation:

- Applied as a `StatusEffect` of type Slow on the target
- Movement speed reduced by 30-50% (per skill specification)
- Speed-gain rates from movement reduced by 50%
- Animation playback speed reduced by 0.85× (visual cue per Animancer driver)
- Duration: 3-8s
- Stacks: each new application *refreshes* the duration; does not stack the magnitude

Slow is a *positional* CC. A slowed unit can still fight, but they can't reposition effectively. Used well, slow lets a faster squad isolate enemies one at a time. Used poorly, it does nothing — a slowed caster casts just as effectively.

This is what makes Slow tactically interesting: it punishes mobile builds and is wasted on stationary ones. Skill designers must consider what archetype each Slow-applying skill is meant to counter.

### CC and the speed system

CC effects interact with the speed resource in deliberate ways:

| CC | Speed effect on target |
|---|---|
| Knockback | -5 to -15 (severity dependent) |
| Stun | Drains at 2× normal rate during; +0 on expire |
| Interrupt | -10 instant; partial cast refund only |
| Knockdown/Ragdoll | Set to 0; standing up rebuilds normally |
| Slow | Speed-gain rate × 0.5 |

This means CC is not just a temporal lockout — it's also a *resource attack*. A unit who survives a stun emerges sluggish and vulnerable. The defender pays multiple costs.

### CC and stances

Stances modify how the brain reacts to CC:

- **Aggressive stances** (Onslaught, Tempest, Wraith) prioritize *applying* CC. They pick CC-causing skills more readily.
- **Defensive stances** (Stalwart, Sentinel) prioritize *avoiding* CC. They keep more speed reserved for emergency dodges; they target enemies preparing CC casts (high-priority interrupt).
- **Tactician** reads enemy resource state and applies CC opportunistically — interrupting low-HP casters, knocking down low-speed brawlers.
- **Conduit** (caster) is *vulnerable* to CC and stances responses are limited — a conduit's main defense is teammates and positioning.

This is what makes CC counterplay legible. The same stunned hero in different stances responds differently to having been stunned (Aggressive: rage, immediately re-engage; Defensive: retreat to rebuild; Tactician: assess and adapt).

### Resistance and immunity

Some heroes will be more resistant to CC than others. Mechanism:

- Each hero has implicit `ccResistance` derived from attributes — high HP and Defense reduce CC durations; high SPD reduces Stun duration; high Wisdom reduces Silence duration
- Future passives and items contribute additive `ccResistance` modifiers
- Resistance is **multiplicative on duration**, not a hard "resist or not" check — a hero with 30% CC resistance suffers a 1s stun as a 0.7s stun
- A few high-tier passives or boss mechanics may grant **temporary CC immunity** windows. These are rare and deliberately powerful.

CC resistance never reaches 100% on heroes — there's always *some* duration. This prevents builds that completely no-sell CC.

### CC and the AI brain

The brain treats CC as a high-priority signal. When a unit is in a CC state, the brain doesn't tick — but for unaffected allied or enemy units, the brain reads CC state of others to make decisions:

- An ally being stunned is a *threat assessment trigger* — defensive units may move to interpose
- An enemy being knocked down is an *opportunity trigger* — aggressive units pivot to commit a follow-up attack on the prone target
- An enemy preparing a CC cast (visible windup) is a *priority interrupt target*

This is what makes CC feel *interactive* rather than just timer-based.

### Skill design implications

Adding CC to the combat catalog means new fields on `ActionDefinition` and `ComboRecipeDefinition`:

| Field | Type | Purpose |
|---|---|---|
| `appliesCC` | `CCEffectType` enum | Which CC effect this skill applies (or None) |
| `ccDuration` | `float` | Base duration before resistance |
| `ccChance` | `float` | Probability of CC application (0-1; 1 = guaranteed) |
| `causesInterrupt` | `bool` | Always interrupts casts regardless of damage |
| `causesKnockdown` | `bool` | Triggers knockdown/ragdoll on landing |
| `causesRagdoll` | `bool` | Visual flag — uses ragdoll presentation instead of knockdown |

The `CCEffectType` enum:

```csharp
public enum CCEffectType
{
    None,
    Stun,
    Slow,
    Knockback,
    Knockdown
    // Secondary effects added as designed
}
```

Most skills will have `appliesCC = None`. CC-causing skills are designed deliberately, not sprinkled across the catalog. **Roughly 15-25% of skills should apply some form of CC.** Higher than that and combat becomes a CC-chain meta; lower than that and CC feels rare.

### CC visualization

Per `07_PRESENTATION.md` rules, CC needs to read clearly. Each CC effect has a distinct visual:

| CC | Visual cue |
|---|---|
| Knockback | Target slides back with motion blur trail |
| Stun | Stars, sweat drops, or other anime-style icon above head; idle stagger animation loops |
| Interrupt | Cast effect dissipates with disrupt particles; brief "fizzle" sound |
| Knockdown | Target launched in arc, dust cloud on landing, prone visible |
| Ragdoll | Brief physics flop with particle debris |
| Slow | Subtle frame-skip on animations; faint slowing trail |

These are part of `BattleAnimancerDriver`'s responsibilities — when a CC state begins, the driver plays the appropriate visual.

### CC subsystem

`BattleStatusEffectSystem` (new MonoBehaviour subsystem) manages the lifecycle of all status effects, including CC. Responsibilities:

- Maintains a list of active `StatusEffect` instances per unit
- Ticks duration each frame
- Applies effect modifiers to the unit (speed multipliers, attribute changes, state machine constraints)
- Notifies the AI brain on application and expiration
- Handles stacking rules (refresh, stack, ignore)

Public API:

- `ApplyEffect(unit, effectType, duration, source) → StatusEffectInstance`
- `RemoveEffect(unit, effectId)`
- `HasEffect(unit, effectType) → bool`
- `GetEffects(unit) → List<StatusEffectInstance>`

Other subsystems (`BattleCombatResolver`, `BattleAIBrain`, `BattleAnimancerDriver`) query and modify status through this system. **No subsystem stores CC state locally.**

---

## State machine extensions

The current state machine (`Backline → Engage → Decide → [Melee | CastMobile | CastRooted] → Execute → Recover → loop`, with `Dodging` as interrupt) needs minimal additions to support speed. The big change is:

### New state: `Repositioning`

When the brain selects a "build speed" archetype, the unit enters `Repositioning` instead of `Melee` or a Cast state. Behavior:

- Movement intent is read (Circle, Close, Disengage, Hold, Dash)
- Unit moves accordingly for 1-3 seconds (duration tunable, depends on how much speed needs to be built)
- Speed accumulates per the movement table above
- During this state, the unit can still be attacked, can still dodge, can still block
- Returns to `Decide` when target speed reached or duration expires

### `Decide` state — speed cost gating

Before transitioning to `Execute`, the unit verifies it has enough speed for the chosen skill. If not, the skill is skipped and the brain re-evaluates. This prevents committing to a combo the unit can't afford.

### `Execute` state — speed spend

Speed is deducted on Execute entry, alongside the existing Energy spend. Both must succeed; if either fails, the action aborts and the unit re-enters Decide.

### `Recover` state — speed depletion impact

If the unit ended Execute with very low speed (< 15), Recover duration is extended by 0.5s. This represents the *winded* state after spending heavy momentum on a combo — the unit needs an extra beat to gather themselves.

### `Dodging` — speed cost

Dodging now costs both energy and speed (25 each). This naturally limits dodge-spam — a sluggish unit *cannot* dodge until they rebuild. This adds tactical depth without being punishing (running for a few seconds restores dodge capability).

### `Backline` — no speed change

Backline units do not build speed. Speed is a frontline-combat resource. When they get promoted to frontline (via `BattleEngagementManager`), they spawn into combat with starting speed (30).

### New state: `Stunned`

When a CC effect of type Stun is applied, the unit transitions to `Stunned`:

- Cannot tick the brain; ignores all decision input
- Cannot move (movement intent forced to Hold)
- Cannot dodge or block
- Speed drains at 2× normal rate
- On expiration, transitions to a brief Stagger state (~0.4s) before re-entering Decide
- 0.5s post-stun grace period prevents immediate re-stunning

If the unit was the active attacker in an exchange when stunned, the exchange aborts with partial cost refund. If the active defender, the exchange resolves with the stunned unit eating remaining strikes.

### New state: `KnockedDown`

Triggered by skills with `causesKnockdown` or `causesRagdoll` flags. Two-phase state:

1. **Launch / fall** (~0.6-1.0s): unit is forced through the launch arc (knockdown) or physics flop (ragdoll). Cannot act, cannot dodge, cannot block.
2. **Prone** (~1.0-1.5s): unit lies on the ground. Still cannot act. Takes 1.5× damage from any attack.
3. **Recovery / Stagger** (~0.5s): unit rises and is briefly staggered before re-entering Decide.

Speed is set to 0 on knockdown entry and rebuilds normally during recovery. 2.0s post-knockdown grace period prevents immediate re-knockdown.

This state's vulnerability window is what makes knockdown the *premium* CC — landing it gives allies a payoff opportunity. AI brains for allies recognize this opportunity and prioritize follow-up commits on prone enemies.

### Mid-state CC interrupts

CC application doesn't always create a new state — sometimes it just interrupts the current one:

- A unit in `CastRooted` taking heavy damage (or skill with `causesInterrupt`) cancels the cast and enters Stagger
- A unit in `Melee` (chasing target) hit by Slow has its movement immediately reduced
- A unit in `Repositioning` hit by Knockback has its repositioning interrupted; the brain re-evaluates after the slide

The pattern: CC effects modify state behavior or force state transitions, but they're applied through the status effect system rather than as one-off special cases in the state machine. The state machine *queries* status; it doesn't *own* status logic.

### Exchange-aware Execute and Recover

The `Execute` state behaves slightly differently when entered as part of an exchange:

- The attacker plays out the full combo without re-entering Decide between strikes
- Each strike checks the defender's response (dodge / block / counter / eat) before applying damage
- If the exchange is interrupted (defender counters, attacker dies), `Execute` exits early and the unit transitions appropriately

Outside of exchanges (e.g., for ranged casts that don't engage a target up close), `Execute` works as before — single resolution of the technique.

The `Recover` state always includes the exchange beat (the ~0.3-0.5s pause) when exiting an exchange. This is enforced regardless of behavior or stance. The beat is for the player, not the AI.

---

## Subsystems needed

Following the architecture rules in `02_ARCHITECTURE.md`, all new combat behavior goes into dedicated subsystems. **Don't add speed logic to `TerrainBattleManager` or `TerrainBattleUnit` directly.**

### `BattleSpeedSystem` (new)

A MonoBehaviour subsystem on `TerrainBattleManager`. Owns:

- Per-unit current speed values (a dictionary keyed by `UnitRuntime`)
- Movement-type → speed-gain rate table (configurable as ScriptableObject)
- Skill speed cost lookup (read from `ActionDefinition` / `ComboRecipeDefinition`)
- Speed cap enforcement (soft and hard)
- Drain logic (idle drain, block drain, stagger drain)
- Public API: `GainSpeed(unit, amount)`, `SpendSpeed(unit, amount) → bool`, `GetSpeed(unit) → float`, `GetSpeedBand(unit) → SpeedBand`

Other subsystems (`BattleCombatResolver`, AI brain, `TerrainBattleUnit`) query and modify speed through this system. **No subsystem stores a unit's speed locally.**

### `BattleAIBrain` (new)

A MonoBehaviour subsystem implementing the Decide-tick decision tree above. Replaces the current ad-hoc logic in `TerrainBattleUnit.UpdateDecide`. Owns:

- The decision tree (Step 1 through Step 5 above)
- Behavior personality parameter table (per `BehaviorType`)
- Public API: `MakeDecision(unit, target) → DecisionResult`

`DecisionResult` is a struct containing the chosen skill (or null if "build speed"), the movement intent, and any flags.

`TerrainBattleUnit.UpdateDecide` calls `BattleAIBrain.MakeDecision` and acts on the result. The unit doesn't make decisions itself; it just executes them.

### `BattleMovementSystem` (new — extracts existing logic)

Currently movement is coupled into `TerrainBattleUnit` per-state. As the speed system depends on tracking *what kind* of movement is happening (circling vs closing vs disengaging), this needs to be extracted.

Owns:

- Per-unit current movement intent (Close, Circle, Disengage, Hold, Dash)
- Movement execution logic (CharacterController-driven)
- Coordination with `BattleSpeedSystem` to report movement type per-frame
- Engagement slot integration (preserves existing `BattleEngagementManager` and `BattleMeleeTokenSystem` behavior)

This refactor is **necessary for speed to work** — without it, you can't measure "is this unit currently circling or closing?"

### `BattleExchangeStager` (new — extends existing `BattleExchangeCoordinator`)

The current `BattleExchangeCoordinator` handles initiative-based role assignment with an `IsAnimating` lock. This is the seed of the exchange system but doesn't yet implement the full exchange lifecycle.

The Stager extends it with:

- Active exchange tracking (which pairs of units are currently in an exchange)
- Phase tracking per active exchange (Initiation / Wind-up / Strike sequence / Resolution / Beat / Re-evaluation)
- Defender response decision (per-strike dodge / block / counter / eat)
- Beat enforcement (the non-skippable pause)
- Notification API: subscribers get notified when exchanges initiate, transition phases, and end

Public API:
- `TryInitiateExchange(attacker, defender, technique) → ExchangeHandle | null`
- `GetExchange(unit) → ExchangeHandle | null` (returns the active exchange a unit is in, if any)
- `ResolveDefenderResponse(handle, strike) → DefenderResponse`
- `EndExchange(handle, reason)`

Other subsystems query the Stager when they need to know whether a unit is locked in an exchange (e.g., the AI brain skips Decide for units actively defending).

### `BattleAnimancerDriver` (new — translates combat state to animation)

A small but critical adapter sitting between combat subsystems and the Animancer playback layer. Its job is to ensure that **animation is always downstream of combat truth** (per `07_PRESENTATION.md`).

Owns:
- Per-unit reference to the `AnimancerComponent`
- Mapping from combat events (state transitions, exchange phases, hit-stop triggers) to Animancer transition assets
- Speed-band visual modulation (animation playback speed multiplier based on current speed)
- Coordination with hit-stop (animator speed = 0 during hit-stop windows)

Public API:
- `PlayState(unit, animatorState)` — plays a locomotion or idle state
- `PlayAttack(unit, attackProfile)` — plays an attack profile's clip with proper transition
- `OnHitStop(unit, duration)` — pauses animation for hit-stop
- `SetSpeedBandModifier(unit, band)` — adjusts playback speed visually

Notably, this subsystem does **not** decide *when* things should play — it only *plays* what other systems instruct. The combat decisions live in the brain, the speed system, the resolver, etc. The driver is a translator.

See the Animancer integration section below for full detail.

### `BattleStatusEffectSystem` (new)

Manages the lifecycle of all status effects, including CC and any future buffs/debuffs/DOTs from `08_ROADMAP.md`. This is the central registry for "things that are temporarily true about a unit."

Owns:

- Per-unit list of active `StatusEffectInstance` objects
- Duration ticking each frame
- Effect application logic (modifies unit state, queues state machine transitions when CC requires)
- Stacking rules (refresh / stack-up-to-N / replace / ignore — per effect type)
- Resistance calculation (combines hero attributes with item/passive contributions)
- Notifications to the AI brain on effect application and expiration

Public API:

- `ApplyEffect(unit, effectType, duration, magnitude, source) → StatusEffectInstance`
- `RemoveEffect(unit, effectId)`
- `HasEffect(unit, effectType) → bool`
- `GetEffects(unit) → List<StatusEffectInstance>`
- `GetEffectiveDuration(unit, baseDuration, effectType) → float` (applies resistance)

`BattleCombatResolver` calls `ApplyEffect` when a CC-bearing skill lands. The state machine queries `HasEffect` to gate transitions. The brain queries `GetEffects` when making decisions about ally support and enemy targeting.

This subsystem is the foundation for status effects in general — not just CC. When the broader status system (DOTs, buffs, shields) comes online per `08_ROADMAP.md`, it extends this same subsystem rather than creating a parallel one.

### Updates to existing subsystems

**`BattleCombatResolver`:**
- Reads speed cost from `ResolvedTechnique`, calls `BattleSpeedSystem.SpendSpeed` before executing
- Applies speed-scaling damage modifier where applicable
- Triggers speed effects on the target for stagger / momentum-shatter skills

**`BattleTargetFinder`:**
- Receives optional `SpeedAware` flag — high-perception units may target sluggish enemies preferentially (a future enhancement, not day-one)

**`SkillSystem`:**
- `PickBestSkill` accepts an archetype hint from `BattleAIBrain` and biases selection accordingly
- `ResolveSkill` populates the speed cost on the `ResolvedTechnique`

---

## Animancer integration

The combat backend depends on Animancer Pro for clip playback. This section defines how Animancer integrates *specifically with the combat systems* — for general animation rules, see `07_PRESENTATION.md`.

The principle is unchanged: **animation reflects combat state, never defines it.** Animancer is a tool that *enables* this principle in code; it does not replace the principle.

### Why Animancer fits the combat design

Three reasons Animancer specifically helps the systems above:

1. **Code-driven playback matches the data-driven design.** Combos, skills, and attack profiles are ScriptableObjects. With Animancer, playing the right clip for a skill is a one-line call referencing the clip stored on the profile — no Animator Controller graph editing per skill.

2. **Speed-band animation modulation is trivial.** Animancer exposes per-state playback speed. Setting `state.Speed = 1.2f` for a high-speed unit and `state.Speed = 0.85f` for a sluggish one is a single property write — no graph parameter wiring.

3. **Clip events map naturally to combat phases.** Animancer's event system lets you register callbacks at named or normalized times in any clip. The "impact" event of an attack profile becomes `clip.Events.Add(impactTime, OnImpact)` — the combat resolver wires the callback once and forgets it.

### Attack profiles as Animancer assets

The `AttackProfile` ScriptableObject defined in `07_PRESENTATION.md` carries an `AnimationClip` reference. With Animancer, this profile becomes the **single source of truth** for an attack's animation, and the combat code reads it directly:

```csharp
// In BattleCombatResolver, simplified
public void ExecuteSkill(TerrainBattleUnit attacker, ResolvedTechnique tech)
{
    var profile = tech.attackProfile;
    var animState = attacker.AnimancerDriver.PlayAttack(profile);

    // Register impact callback
    animState.Events.Add(profile.impactTimeNormalized, () =>
    {
        ApplyDamage(attacker, defender, tech);
        hitStopSystem.TriggerHitStop(profile.hitStopTier);
    });

    // Register recovery callback
    animState.Events.OnEnd = () => attacker.OnExecuteEnd();
}
```

The clip plays. The impact callback fires at the right frame. Damage applies. Hit-stop fires. The attacker exits Execute when the clip ends. **None of these decisions live in animation; the clip is just the visual delivery mechanism.**

### Speed-band visual modulation

The brain's speed band (Sluggish / Engaged / Sharp / Primed) maps to a playback speed multiplier in `BattleAnimancerDriver`:

| Speed band | Playback multiplier | Visual effect |
|---|---|---|
| Sluggish (0-20) | 0.85× | Heavy, weighted, slightly behind beat |
| Engaged (20-50) | 1.0× | Default reference timing |
| Sharp (50-70) | 1.1× | Crisp, snappy |
| Primed (70-100) | 1.2× | Sharp, almost unfair |

This is a **visual-only** modifier — the gameplay-truth math (when damage applies, when energy spends, when an exchange resolves) uses the canonical normalized impact times from the profile. Animancer runs the clip faster or slower; combat events fire at the same normalized progress regardless.

The driver applies this when state changes:

```csharp
public void SetSpeedBandModifier(TerrainBattleUnit unit, SpeedBand band)
{
    var multiplier = bandMultiplierLookup[band];
    unit.AnimancerComponent.Layers[0].Speed = multiplier;
}
```

### Hit-stop integration

Hit-stop is implemented by setting Animancer playback speed to 0 for the duration:

```csharp
public IEnumerator HitStopRoutine(TerrainBattleUnit unit, float duration)
{
    var prev = unit.AnimancerComponent.Layers[0].Speed;
    unit.AnimancerComponent.Layers[0].Speed = 0;
    yield return new WaitForSeconds(duration);
    unit.AnimancerComponent.Layers[0].Speed = prev;
}
```

Speed-band modifier is *restored* after hit-stop, not overwritten. This avoids the bug where a hit-stop "resets" a sluggish unit's animation back to default speed.

### Exchange phase mapping

Exchanges have phases (Initiation, Wind-up, Strike sequence, Resolution, Beat). These map to Animancer state transitions:

| Exchange phase | Animation handling |
|---|---|
| Initiation | Attack clip starts playing via `PlayAttack(profile)` |
| Wind-up | Plays through clip's pre-impact frames |
| Strike sequence | For multi-hit combos, multiple clips chain via Animancer transitions |
| Resolution | Hit-stop triggers (Animancer speed = 0); landed strike's impact event has fired |
| Beat | Hit-stop ends; playback resumes for follow-through |
| Re-evaluation | Final clip ends; Animancer returns to locomotion or idle state |

Multi-hit combos use Animancer's transition system to chain clips smoothly without returning to idle between hits — visually crucial for the "fluid combo" feel.

### Animation events as notifications, never authority

Animancer's clip event system is used to **notify combat of timing**, not to **command combat to do things**. The pattern:

✅ **Correct:** "When the clip reaches normalized time 0.4, call `OnImpact()` so combat can apply damage."

❌ **Wrong:** "When the clip reaches normalized time 0.4, the clip itself applies damage to the target."

The combat resolver registers the callback. The callback is the *bridge*. The clip just plays.

This rule is what allows the same combat to look correct even if you swap the clip for a better one later. Replace the punch animation; the impact still happens at normalized 0.4; the visuals change but combat doesn't.

### Locomotion and idle states

Outside of attacks, units use a locomotion controller built on Animancer's blend system:

- Idle (combat ready stance)
- Walk (any direction)
- Run / Sprint
- Strafe (when circling, plays a sideways gait)
- Backstep (when retreating)

These are exposed by `BattleAnimancerDriver.PlayState(unit, locomotionState)`. The driver picks the right blend based on movement intent (Close / Circle / Disengage / Hold / Dash) from `BattleMovementSystem`.

When the brain transitions a unit out of a non-locomotion state, the driver selects the appropriate locomotion or idle transition automatically.

### Custom behavior flags

A few attack profiles need behaviors Animancer doesn't directly express:

- **Animation cancel into dodge.** Successful counter responses interrupt the attacker's clip mid-play. The driver supports `CancelAttack(unit)` which crossfades back to combat idle.
- **Brief clip slow-mo.** For the moment a killing blow lands, the driver supports a one-shot "slow burst" — playback speed drops to 0.4× for 0.5s, then resumes. This is a *presentation* feature, gated on combat events (kill confirmed).
- **Frozen clip pose for ultimates.** For dramatic moments (rare combos, finishers), the driver can hold a specific pose mid-clip. Used sparingly per `07_PRESENTATION.md`'s restraint principle.

### What this section explicitly does not cover

The following are presentation concerns covered in `07_PRESENTATION.md`, not here:

- The full attack profile schema (this doc references it; that doc defines it)
- Visual aesthetic choices (anime feel, particles, trails)
- Camera framing and shake
- VFX that play alongside animations

This section is purely about *how Animancer hooks into the combat backend*. Visual design lives elsewhere.

---

## Action execution speed — SPD stat and proficiency

Beyond the kinetic speed *resource*, the **SPD attribute** affects how fast the hero physically executes actions. This is distinct from current speed (the resource), and from animation playback speed (the visual modifier).

### The three speeds

To avoid confusion, here's the full taxonomy:

| Concept | What it is | Set by |
|---|---|---|
| **Current speed** | Kinetic resource (0-100) used by combat | Movement, skills |
| **SPD attribute** | Permanent stat on the hero | Hero progression, leveling |
| **Action execution timing** | How fast a specific action plays out for this hero | SPD attribute × proficiency × current speed band |
| **Animation playback speed** | Visual playback rate of the clip | Speed band only (cosmetic) |

The fourth row is what `BattleAnimancerDriver.SetSpeedBandModifier` controls. The third row — **action execution timing** — is what affects gameplay-truth combat math.

### How SPD and proficiency combine

The execution time of an action is:

```
final_execution_time =
    base_action_time
    × (10 / SPD_stat)
    × (1.0 / proficiency_modifier)
    × speed_band_modifier
```

Where:
- `base_action_time` is the action's intrinsic timing (defined on the profile)
- `SPD_stat` is the hero's SPD attribute (10 = neutral)
- `proficiency_modifier` is the hero's proficiency in the relevant action type or technique type (1.0 = neutral, up to ~1.5 at mastery)
- `speed_band_modifier` matches the animation playback table (0.85× to 1.2×)

A novice hero (SPD 8, Punch proficiency 1.0, sluggish state) executing a punch:
`base × (10/8) × (1.0/1.0) × (1/0.85)` = `base × 1.47` (47% slower than reference)

A master hero (SPD 14, Punch proficiency 1.4, primed state) executing the same punch:
`base × (10/14) × (1.0/1.4) × (1/1.2)` = `base × 0.43` (57% faster than reference)

**The same action plays out 3.4× faster on the master than on the novice.** This is a real, perceptible difference, and it makes leveling a hero feel meaningful — your old veterans *visibly fight better* than your rookies.

### What "execution time" means concretely

For a punch:
- Wind-up time (from Execute entry to impact event)
- Recovery time (from impact event to Execute exit)
- Both scale together — the entire animation runs faster

For a multi-hit combo:
- The full combo plays out faster
- Time-between-strikes shortens
- Recovery shortens

For a charged cast (CastRooted):
- Cast time shortens (a Triple Sign that takes 1s reference may take 0.6s for a master)
- Recovery shortens

This is what makes mastery progression feel *visible* even when raw damage doesn't change much. The hero isn't just hitting harder — they're hitting *faster*.

### Implementation note

Both `BattleCombatResolver` and `BattleAnimancerDriver` need to know the final execution time. The resolver needs it for damage timing; the driver needs it for clip playback speed. The calculation happens once in the resolver when the technique resolves and is passed to the driver:

```csharp
var finalTime = ComputeExecutionTime(unit, technique);
animancerDriver.PlayAttack(unit, technique.attackProfile, finalTime);
```

The driver's `PlayAttack` accepts an explicit duration override, scaling the clip to fit.

### Why this is in COMBAT_DESIGN, not 07_PRESENTATION

The animation playback speed is presentation. The execution time is gameplay-truth — damage applies at the right moment, energy spends at the right moment, the unit exits Execute at the right moment. Per the architecture rule, gameplay-truth lives in combat. This section belongs here.

---

## Data model additions

### `ActionDefinition` — new fields

| Field | Type | Purpose |
|---|---|---|
| `speedCost` | `float` | Speed required to use this action (default 0) |
| `speedGain` | `float` | Speed granted on use (for kinetic skills, default 0) |
| `speedScaling` | `float` | Damage multiplier per 100 speed (default 0) |
| `speedGate` | `float` | Minimum speed required to use (default 0) |

### `ComboRecipeDefinition` — new fields

Same as above. Combo recipes inherit speed properties from the recipe, not from the constituent actions.

### `ResolvedTechnique` — new fields

| Field | Type | Purpose |
|---|---|---|
| `speedCost` | `float` | Final speed cost after modifiers |
| `speedScaling` | `float` | Final damage multiplier per 100 speed |
| `speedGate` | `float` | Final minimum speed required |

### `UnitRuntime` — no new fields

Speed is **not** stored on `UnitRuntime`. It's stored in `BattleSpeedSystem`'s internal map. This keeps `UnitRuntime` clean and prevents the "five subsystems writing to UnitRuntime" anti-pattern flagged in `02_ARCHITECTURE.md`.

### New enum: `MovementIntent`

```csharp
public enum MovementIntent
{
    Hold,
    Close,
    Circle,
    Disengage,
    Dash
}
```

### New enum: `SpeedBand`

```csharp
public enum SpeedBand
{
    Sluggish,   // 0-20
    Engaged,    // 20-50
    Sharp,      // 50-70
    Primed      // 70-100
}
```

---

## Combat math — updated formulas

### Damage formula

```
final_damage =
    sum(basePower)
    × powerMultiplier
    × (ATK / 10)
    × proficiency
    × (1 + (current_speed / 200) × speed_scaling_coefficient)
```

Where `speed_scaling_coefficient` is the technique's `speedScaling` value (0 for non-scaling skills).

### Dodge chance

```
dodge_chance =
    (SPD_stat × 0.05)
    × (0.5 + current_speed / 100)
    × (energy_check ? 1 : 0)
    × (cooldown_ready ? 1 : 0)
```

### Block chance

Unchanged from current: `DEF_stat × 0.02`. Block does not interact with speed because block represents *passive defense* rather than active reaction. (This is a design choice — keeps block as the "low-speed defensive option.")

### Speed gain per frame

```
speed_gain_this_frame = movement_type_rate * Time.deltaTime
```

Capped at hard cap (100). Above soft cap (70), the rate is multiplied by 0.4 — slow grind to push past 70.

---

## Skill catalog — speed properties

These are starting values for existing skills. To be tuned during integration.

| Skill | Speed cost | Speed scaling | Speed gate |
|---|---|---|---|
| Punch (standalone) | 0 | 0.0 | 0 |
| Kick (standalone) | 5 | 0.0 | 0 |
| Hand Sign A/B/C (standalone) | 0 | 0.0 | 0 |
| Focus (standalone) | 0 | 0.0 | 0 |
| Combo Strike | 10 | 0.0 | 0 |
| Power Strike | 15 | 0.5 | 0 |
| Crescent Kick | 25 | 0.7 | 30 |
| Earth/Thunder/Water Fist | 15 | 0.3 | 0 |
| Tremor/Thunder/Tidal Sweep | 20 | 0.3 | 0 |
| Geomagnetic/Thunderstorm/Mudslide | 10 | 0.0 | 0 |
| Triple Sign | 0 | 0.0 | 0 |
| Elemental Fist | 35 | 0.5 | 40 |
| Elemental Storm | 35 | 0.7 | 40 |
| Summoning | 0 | 0.0 | 0 |
| Orb Strike (trigger) | 0 | 0.0 | 0 |
| Per-orb fire (during orb state) | 5 | 0.0 | 0 |

### Future kinetic skills (suggested)

These don't exist yet but should be among the first new skills designed for the speed system:

| Skill | Trigger | Speed effect | Notes |
|---|---|---|---|
| Wind Burst | Wind-element combo | +30 instant | Alternative to running for sluggish units |
| Step | Single action | +20 instant, costs 5 energy | Quick speed top-up |
| Sprint Cast | Modifier on movement | +50% movement-based speed gain for 3 sec | Active opt-in |
| Stagger Strike | Heavy attack | Target loses 30 speed | Tactical denial |
| Destabilize | Sign-based debuff | Target's speed cap drops to 40 for 5 sec | Tactical denial |
| Flow State | Self-buff | Speed soft cap raised to 90 for 5 sec | Endgame ultimate |

---

## Implementation roadmap

This is the order to build the combat system. Each phase is a coherent commit, builds on the previous, and produces a testable improvement to combat. **Don't try to build the whole system in one PR.**

The roadmap weaves together speed-system phases with the other deep-combat systems (Animancer integration, Exchanges, Stances, SPD execution timing). They are sequenced so each phase produces a usable, testable result on its own.

### Phase 1 — Animancer driver foundation (1 week) — **shipped 2026-05-06**

- [x] `BattleAnimancerDriver` subsystem on `TerrainBattleManager` (registry-based; per-unit `AnimancerComponent`s register on `TerrainBattleUnit.Initialize`)
- [x] Earth Fist (`Hand Sign A → Punch`) plays through the driver via `EarthFistAbility` + `AttackProfile_EarthFist.asset`. Other skills remain on the legacy Animator Controller path; `EarthFistAbility` falls back to legacy when the profile is unconfigured
- [x] Per-instance event registration via `AnimancerState.Events(this).SetCallback`; impact still fires through Mixamo's legacy `AnimationEvent` forwarded by `UnitAnimationEventRelay` (named `[EventNames]` Animancer events authored per-skill from Phase 2 onward)
- [x] Per-unit hit-stop via `AnimancerComponent.Graph.Speed = 0` (composes additively with the global `Time.timeScale` freeze in `BattleHitStopSystem`)
- [x] `07_PRESENTATION.md` updated with the centralized driver pattern, public surface, and PoC notes

Deliverable shipped: Earth Fist plays through the driver with attacker-side per-unit pause on impact. Foundation in place for Phase 2 catalog migration.

### Phase 2 — Migrate remaining skills to Animancer (1-2 weeks) — **plumbing shipped 2026-05-06**

- [x] Generalized: `EarthFistAbility` → `AnimancerMeleeAbility` — single ability handles every Animancer-routed melee technique
- [x] Data-driven dispatch: `UnitBrainAI` consults a `List<TechniqueProfileBinding>` (technique name → `AttackProfile`); matching techniques play through the central driver, others fall through to legacy random punch / kick / dash
- [ ] **Content task — defer:** authoring per-skill `TransitionAsset` + `AttackProfile` for every existing combo. The system accepts new bindings as data; the bottleneck is sourcing or recording new clips per skill.
- [ ] **Content task — defer:** locomotion blend (idle/walk/run/strafe/backstep) — uses existing Mixamo packs already in `Assets/Art/`, but needs blend-tree authoring on a `LinearMixerTransition` asset.
- [ ] Old Animator Controller graph deprecation — kept as fallback until every skill has an `AttackProfile` binding.

Deliverable: the *system* is in place; future skills are added by data, not code. Removing the legacy graph is gated on content authoring, not engineering.

### Phase 3 — Core speed pool (1-2 weeks) — **shipped 2026-05-06**

- [x] `BattleSpeedSystem` subsystem on `TerrainBattleManager` with per-unit registry
- [x] Idle drain (-5/sec), block drain (-2/sec while `Recover` + `Defender`), stagger drain (-10/sec)
- [x] Movement-velocity gain — linear in `UnitMovementController.CurrentMoveSpeed`, soft cap at 70 with ×0.4 multiplier above
- [x] `SpeedBarUI` auto-attached per unit; band-keyed color and soft-cap tick
- [x] `OnSpeedChanged` event for UI / future visual modulation

Deliverable shipped: speed builds and drains visibly. Per-`MovementIntent` shaping deferred to Phase 5.

### Phase 4 — Speed costs and gates on skills (1 week) — **shipped 2026-05-06**

- [x] `speedCost`, `speedGain`, `speedScaling`, `speedGate` on `ActionDefinition` and `ComboRecipeDefinition`
- [x] `ResolvedTechnique` carries the same fields after `SkillSystem.ResolveSkill` (recipe-defined for combos, summed for unmatched chains)
- [x] `BattleCombatResolver.ResolveSkillAttack` spends speed before damage and credits gain on cast
- [x] `UnitBrainAI.PickBestSkill` filters skills by speed gate AND speed cost so unaffordable / gated skills aren't selected
- [ ] **Content task — defer:** populate balanced `speedCost` / `speedScaling` / `speedGate` on every recipe (current values default to 0). Spec catalog at lines 1190-1199 has starting values to drop in.

Deliverable: combos cost speed when configured. The catalog tuning pass is a content task that doesn't block downstream phases.

### Phase 5 — Movement-aware speed gain (1-2 weeks)

### Phase 3 — Core speed pool (1-2 weeks)

- Create `BattleSpeedSystem` with the basic API
- Add `currentSpeed` tracking per unit (in the system, not on `UnitRuntime`)
- Implement idle drain (stand still → lose speed)
- Implement basic movement-based gain (movement velocity = speed gain rate)
- Add speed indicator UI (simple bar below energy)
- **No skill costs yet.** Speed builds and drains; doesn't gate anything.

Deliverable: a speed bar fills as units move and drains when they stand still. Players can see it. No combat impact yet.

### Phase 4 — Speed costs and gates on skills (1 week)

- Add `speedCost`, `speedGain`, `speedScaling`, `speedGate` fields to `ActionDefinition` and `ComboRecipeDefinition`
- Update `SkillDataCreator` to populate these
- `BattleCombatResolver` calls `BattleSpeedSystem.SpendSpeed` on Execute
- Existing AI logic gets a temporary speed-cost guard (skip skill if can't afford)
- Tune values per the catalog above

Deliverable: combos cost speed. Units run out of speed and have to rebuild before doing big attacks.

### Phase 5 — Movement-aware speed gain (1-2 weeks) — **shipped 2026-05-06**

- [x] `BattleMovementSystem` subsystem on `TerrainBattleManager` with per-unit intent registry
- [x] `MovementIntent` enum and per-intent speed-gain rates (Hold=0, Disengage=3, Circle=6, Close=8, Dash=10) — matches spec table
- [x] `UnitBrainAI.TransitionTo` pushes intent on every state transition; `UpdateMelee` overrides to `Circle` when orbiting (token denied)
- [x] `BattleSpeedSystem.ComputeDelta` reads intent-driven rate; static units drain even with aggressive intent set
- [x] `Repositioning` state added to `UnitCombatState` enum (reserved — used as a future explicit "rebuild speed" state; AI brain currently routes the same intent through `Melee` with the `BuildSpeed` archetype)
- [ ] **Refactor task — defer:** moving `UnitMovementController` translation logic into a centralized "movement system." The current setup keeps `UnitMovementController` as the per-unit translator and `BattleMovementSystem` as the policy layer — clean separation, no destructive refactor needed.

Deliverable shipped: closing vs circling vs disengaging produce distinct speed-gain rates. Combat texture starts to emerge once stance values are populated.

### Phase 6 — AI brain core (2-3 weeks) — **scaffolding shipped 2026-05-06**

- [x] `BattleAIBrain` static decision service in `TacticalRPG.Systems` with the full Decide-tick tree (Survival → Resource posture → Action archetype → Movement intent)
- [x] `BehaviorParams` differentials per `BehaviorType` (Aggressive: speed=50, Defensive: speed=80, Balanced: speed=65 thresholds for big-combo commit; HP-disengage thresholds 30/60/50; dodge willingness 0.7/0.2/0.45)
- [x] Implicit resource-focus inference: `BattleAIBrain.InferArchetype` reads loadout at init, returns `Brawler` / `BurstStriker` / `Caster` / `Hybrid` / `Healer`. Cached on `UnitBrainAI` and passed into `Decide` for archetype-specific archetype routing (e.g. Caster + loaded energy → BuildPhase preferring Circle)
- [x] `UnitBrainAI.UpdateDecide` invokes `BattleAIBrain.Decide`, sets movement intent, short-circuits skill cast for `BuildSpeed` / `Disengage` / `Wait` archetypes
- [ ] **Tuning task — defer:** the `BehaviorParams` numbers are seeded from the spec but the *visible* personality differentials need playtesting. Aggressive vs Defensive currently differ in thresholds but the second-order behaviors (Aggressive pursuing fleeing enemies, Defensive holding choke points) belong to Phase 13 polish.

Deliverable shipped: units now make resource-aware decisions and movement intent flows from posture × behavior × loadout archetype. Tuning is iterative.

### Phase 7 — Stances (1 week) — **shipped 2026-05-06**

- [x] `StanceDefinition` ScriptableObject with all spec-listed knobs (behavior bias, speed thresholds, target priority, preferred intent, dodge willingness, engagement delay)
- [x] `StanceId` enum (Onslaught, Tempest, Stalwart, Tactician, Wraith, Sentinel, Conduit) and `TargetPriority` enum (Nearest, LowestHP, BacklineFirst, Furthest, AttackerOfAlly, Marked)
- [x] `Editor → TacticalRPG/Create Default Stances (force)` generates the seven canonical stance assets matching the spec table at `Assets/Data/Stances/`
- [x] `BattleAIBrain.ApplyStance` modulates behavior params from the assigned stance; reserve floor enforced (Sentinel-style hoarding)
- [x] `BattleTargetFinder.GetTarget(unit, priority)` consults stance; brain calls it before falling back to `GetNearestEnemy`
- [x] `[SerializeField] StanceDefinition stance` on `UnitBrainAI` for per-unit assignment
- [ ] **Hero Config integration — defer:** UI for assigning stance per hero is not yet wired into `HeroConfigScene`. Per-hero stance is currently set on the prefab/Inspector. UI will follow `06_HERO_CONFIG.md` patterns when authored.

Deliverable shipped: stances modulate decisions and targeting. Same hero in Onslaught vs Sentinel produces visibly different combat (once values are populated and bound).

### Phase 8 — Exchange system (2-3 weeks) — **lifecycle shipped 2026-05-06**

- [x] `ExchangePhase` enum (None/Initiation/WindUp/StrikeSequence/Resolution/Beat/ReEvaluation) tracked per pair on `BattleExchangeCoordinator.ExchangeRecord`
- [x] `AdvancePhase(attacker, phase)` public hook for the resolver/brain
- [x] Beat lock — per-pair `Time.unscaledTime` expiry after `OnAttackerRecoveryComplete`. New exchange between same pair is suppressed until the beat (default 0.4s) clears. Per-pair, not battlefield-wide — other pairs continue.
- [x] `DefenderResponse` enum (Eat/Dodge/Block/Counter) and `OnStrikeResolved(attacker, defender, response)` running the spec's speed economy (+3 for landed, -5 for dodged)
- [x] Resolver calls `OnStrikeResolved` and `AdvancePhase(Resolution)` after each landed strike
- [ ] **Reserved — defer:** `Counter` response (defender → attacker role flip on a fast-cast counter skill). Needs stance-driven counter selection logic and per-skill "counter eligibility" data. Stance permission infrastructure is in place; the flip itself isn't wired.
- [ ] **Refactor task — defer:** renaming the coordinator to `BattleExchangeStager` per spec. Lifecycle behavior is correct under the existing class name; the rename is cosmetic.

Deliverable shipped: exchanges have explicit phase tracking, the beat fires after every Recovery, and speed economy follows the spec. Counters and the formal stager rename are post-MVP.


### Phase 9 — Speed effects on dodge and damage (1 week) — **shipped 2026-05-06**

- [x] Dodge formula updated: `SPD × 5% × (0.5 + currentSpeed / 100)`, clamped to 75 %
- [x] Speed-scaling damage modifier in `BattleCombatResolver.ResolveSkillAttack`: `dmg × (1 + (currentSpeed / 200) × tech.speedScaling)`
- [x] Speed-band visual indicators (per-unit `SpeedBarUI` with band-keyed color)
- [ ] **Polish task — defer:** Animancer playback-speed modulation by speed band (cosmetic-only, separate from gameplay-truth `executionTime`). Hooks exist in `BattleAnimancerDriver`; visual tuning passes through `CombatTimingFormula.BandModifier`.

Deliverable shipped: high-speed units dodge more often and (with `speedScaling > 0` skills) hit harder. Numbers will feel right after Phase 4 catalog tuning.

### Phase 10 — Status effect foundation and core CC (2-3 weeks) — **foundation shipped 2026-05-06**

- [x] `BattleStatusEffectSystem` central registry with longest-wins-on-refresh stacking
- [x] `CCEffectType` enum: `None`, `Stun`, `Slow`, `Interrupt`, `Knockdown`, `Knockback` (last is informational — owned by `BattleKnockbackSystem`)
- [x] `Stunned` state added to `UnitCombatState`; `EnterStun` / `ExitStun` on `UnitBrainAI`; dodge suppressed while stunned
- [x] `Slow` consumed by `UnitBrainAI.ChaseTarget` (movement scaler) and by `LaunchExecuteAbility` (`executionTime` stretch)
- [x] CC fields on `ComboRecipeDefinition`, `ComboRecipe`, `ResolvedTechnique`
- [x] `BattleCombatResolver.ResolveSkillAttack` rolls and applies CC on landed strikes
- [ ] **Content task — defer:** populate `ccType` / `ccDuration` / `ccChance` on recipes that should CC (current recipes default to none)
- [ ] **Phase 6 dependency:** AI awareness of CC (ally protection, enemy follow-up). Brain currently only reacts to Stun via state machine; tactical CC reasoning lands when `BattleAIBrain` does
- [ ] **Phase 9 dependency:** CC visuals through `BattleAnimancerDriver` (visual freeze on stun, slowed playback on slow). Hooks ready
- [ ] **Future:** `KnockedDown` state + ragdoll (needs physics work); `Interrupt` (needs cast-cancel pipeline); CC resistance from attributes

Deliverable shipped: the system, the data model, the apply pipeline, and Stun/Slow end-to-end. Designers can author CC-enabled combos by editing `ComboRecipeDefinition` assets.

### Phase 11 — SPD attribute and proficiency execution timing (1-2 weeks) — **shipped 2026-05-06**

- [x] `CombatTimingFormula.ComputeExecutionTime` implements the `final_execution_time` formula (SPD × proficiency × speed-band)
- [x] SPD attribute (`UnitRuntime.currentStats.moveSpeed`) wired into the calculation
- [x] Proficiency multiplier (per-element from `ProficiencySet`) wired in
- [x] `UnitBrainAI.LaunchExecuteAbility` populates `ResolvedTechnique.executionTime` at Execute entry; `AnimancerMeleeAbility` reads it as its hold duration. Slow CC stretches the result multiplicatively.
- [ ] **Polish task — defer:** override `BattleAnimancerDriver.PlayAttack(unit, profile, durationOverride)` so the *clip* itself plays back at the computed rate (currently the hold gates the abstract ability while the clip plays at default rate). Cosmetic only — gameplay-truth time is already correct.

Deliverable shipped: high-SPD / high-proficiency / primed-band casters now run their abilities faster than novices. Numbers visible via `CombatLogger`'s `STATE` and `ANIM` categories.

### Phase 12 — Tactical denial skills (2 weeks) — **mechanisms shipped 2026-05-06**

- [x] Stagger-strike speed shatter: `targetSpeedShatter` field on `ComboRecipeDefinition` / `ComboRecipe` / `ResolvedTechnique`. `BattleSpeedSystem.Shatter(unit, amount)` drains target speed on landed strike.
- [x] Speed cap modifiers (Destabilize/Flow State): `targetSoftCapOverride` / `casterSoftCapOverride` / `speedCapModifierDuration` data fields. `BattleSpeedSystem.SetSoftCapOverride(unit, capValue, duration)` applies a temporary per-unit soft cap that decays over time. `GetSoftCap(unit)` returns the override when active.
- [x] Resolver wires shatter and cap modifiers on cast (caster) and on landed strike (target).
- [ ] **Content task — defer:** authoring the actual Wind Burst / Step / Sprint Cast combo recipes. Recipes need new action IDs (currently the action set is Punch/Kick/Hand Sign A/B/C/Focus/OrbSummon — no kinetic-burst action exists). New action definitions + recipe authoring is content work.
- [ ] **Content task — defer:** populating `targetSpeedShatter` on existing heavy combos (Power Strike, Crescent Kick, Elemental Fist) once tuning starts.

Deliverable shipped: the *mechanisms* — drain target speed, raise/lower per-unit soft cap on a duration — are wired end-to-end. Designers can author tactical-denial combos by editing data.

### Phase 13 — Polish and balance pass (ongoing)

- Tune all numbers
- Add second-order behaviors (Aggressive units pursuing fleeing enemies, Defensive units holding choke points)
- Iterate on visual indicators
- Add finisher cinematics (slow-burst on killing blows)
- Refine exchange beat duration based on playtesting
- Add secondary CC effects (Root, Silence, Mark, Daze, Fear) as new skills demand them

This phase is permanent. Combat tuning never finishes.

### Estimated total

Roughly 18-27 weeks of focused work across 13 phases. With Claude Code as a multiplier and disciplined sequential commits, achievable in a 5-7 month dedicated push if combat is the primary focus during that period.

**Don't compress this.** The ordering matters — each phase depends on the previous. Skipping ahead produces broken combat that takes longer to fix than to build right.

---

## What changes in the existing docs

When implementing each phase, the corresponding Tier 1 docs need updates. Here's what to expect:

**`03_DATA_MODELS.md`:**
- Add `speedCost`, `speedGain`, `speedScaling`, `speedGate` fields to `ActionDefinition` and `ComboRecipeDefinition` tables
- Add `appliesCC`, `ccDuration`, `ccChance`, `causesInterrupt`, `causesKnockdown`, `causesRagdoll` fields to skill data tables
- Add `MovementIntent`, `SpeedBand`, `Stance`, `DefenderResponse`, `CCEffectType` enums to the enum section
- Add `StanceDefinition` and `StatusEffectDefinition` ScriptableObjects to the static data section
- Add `StatusEffectInstance` runtime class
- Note that current speed lives in `BattleSpeedSystem`, not `UnitRuntime`
- Note that exchange state lives in `BattleExchangeStager`, not `UnitRuntime`
- Note that status/CC state lives in `BattleStatusEffectSystem`, not `UnitRuntime`

**`04_BATTLE_SYSTEM.md`:**
- Add `Repositioning`, `Stunned`, `KnockedDown` states to the state machine section
- Add the full resource model section (HP, Energy, Speed)
- Add new subsystems to the subsystem list: `BattleSpeedSystem`, `BattleAIBrain`, `BattleMovementSystem`, `BattleExchangeStager`, `BattleAnimancerDriver`, `BattleStatusEffectSystem`
- Add the exchange lifecycle to the combat flow description
- Add the CC catalog and how each effect integrates with the state machine
- Update dodge / block formulas with speed modifier
- Update the combat subsystem flow diagram
- Add a section on how stances integrate with the AI brain
- Add a section on how implicit resource focus works
- Note the action execution time formula and how SPD / proficiency interact

**`05_SKILL_SYSTEM.md`:**
- Add the speed catalog table
- Update skill resolution pipeline to mention speed cost / gate / scaling
- Note new fields in `ActionDefinition` and `ComboRecipeDefinition`
- Add execution timing notes (how SPD and proficiency affect skill timing)

**`06_HERO_CONFIG.md`:**
- Add stance assignment UI (one stance per hero)
- Note that stance preview should display in the hero panel

**`07_PRESENTATION.md`:**
- Add the speed-band visual indicator section
- Note the animation playback speed modifier as a presentation-only effect distinct from execution time
- Reference `BattleAnimancerDriver` as the integration layer
- Update attack profile schema if any new fields are needed

**`CLAUDE.md`:**
- Update the "what good looks like" example to include a speed-aware task
- Add `BattleAnimancerDriver`, `BattleExchangeStager`, `BattleSpeedSystem`, `BattleAIBrain`, `BattleMovementSystem` to the list of "valid places combat behavior can live" (vs. `TerrainBattleManager` / `TerrainBattleUnit` which remain forbidden)

---

## Tuning notes

These are starting values. Combat balance is iterative and will require many cycles of playtesting. Some heuristics:

**If fights end too fast:** speed costs are too low (units always have full speed for combos). Raise costs.

**If fights drag:** speed costs are too high (units spend more time building than spending). Lower costs.

**If sluggish state never matters:** drain rate is too low or movement gains are too high. Reduce gains, increase drains.

**If speed feels invisible:** visual indicators aren't strong enough. Make speed-band transitions more dramatic.

**If AI feels brain-dead:** archetype selection isn't differentiating behaviors enough. Tune personality parameters more aggressively.

**If certain combos feel mandatory:** speed scaling is too dominant. Reduce scaling coefficients.

A weekly tuning ritual during development: record one battle, watch it back, ask "did anything feel wrong?" — then change one number. One per week, not five.

---

## Risks and watch-points

A few things that could go wrong with this design, flagged for future-you to watch:

**Risk: Speed becomes a treadmill.** If the player perceives "I'm always rebuilding speed and never spending it," the system feels like work. Mitigation: keep skill costs lower than they intuitively want to be. Combat should feel *spendy* — fights end with units having spent everything, not having hoarded.

**Risk: AI looks erratic.** Decision trees that flip rapidly between archetypes will look like AI indecision. Mitigation: add a small commit window — once the brain picks an archetype, it sticks to it for at least 2 seconds before re-evaluating.

**Risk: Behavior types blur together.** If Aggressive and Defensive end up looking similar in combat, the personality parameters are too close. Mitigation: exaggerate the differentials initially, scale them back if they feel parodic.

**Risk: Speed hides behind other factors.** If combat is dominated by elemental advantage, equipment quality, or HP totals, speed becomes flavor. Mitigation: ensure speed scaling on damage is *meaningful* — at full speed vs zero speed, the same skill should feel obviously different.

**Risk: Sluggish state feels punishing rather than tactical.** A player whose hero is at 0 speed and getting hit might feel like the system is kicking them while down. Mitigation: ensure rebuild paths are always available — backing away always builds speed at +3/sec, no exceptions.

**Risk: Exchanges produce the "everyone freezes" bug.** A naive exchange implementation locks unrelated units when one pair commits. Mitigation: aggressively scope the `IsAnimating` lock to *only* the two units in the exchange. If combat ever feels stiff in playtesting, the lock has been over-globalized.

**Risk: Exchange beats feel slow at high tempo.** The 0.3-0.5s beat between exchange phases reads correctly at default speed but may feel sluggish when many exchanges happen in rapid succession. Mitigation: do not skip the beat for "speed" reasons. Trust the beat. If combat reads as too slow, *shorten* the beat slightly (to 0.2-0.3s minimum), but never eliminate it. The beat is the readability anchor.

**Risk: Stances become noise.** If players don't perceive different stances producing different outcomes, the system is pure UI complexity for nothing. Mitigation: tune the seven default stances to produce *visibly* different combat — the same fight in Onslaught vs Stalwart should read as a different style. If they don't, exaggerate stance differentials.

**Risk: Implicit resource focus mis-classifies a hybrid hero.** A hero with mixed loadouts may be inferred as a Hybrid archetype that lacks strong AI conviction. Mitigation: ensure Hybrid archetype has a real personality (adaptive — read fight tempo and mirror it). Don't treat Hybrid as "default that does nothing"; treat it as a deliberate strategy.

**Risk: Animation playback speed and execution timing become confused.** These are two different things that look similar. Mitigation: keep them named clearly in code (`AnimationPlaybackMultiplier` for cosmetic, `ExecutionTimeScale` for gameplay-truth). Keep `BattleCombatResolver`'s damage timing tied to normalized clip progress, never to wall-clock time. If a clip plays faster, the impact event still fires at normalized 0.4 — combat math is consistent.

**Risk: Animancer migration leaves dead Animator graphs.** As skills migrate to Animancer, old Animator Controller states should be removed. Half-migrated state is the worst state. Mitigation: track migration progress explicitly. When Phase 2 of the roadmap completes, the old graph should be empty or deleted.

**Risk: CC stacks into permanent lockout.** Without grace periods, a unit could be perma-stunned by a coordinated squad. Mitigation: enforce post-CC grace periods (0.5s after stun, 2.0s after knockdown). These are non-negotiable. Players must always have a window to act.

**Risk: CC density makes combat feel unfair.** If 30%+ of skills apply CC, players feel like the game is removing their agency. Mitigation: target ~15-25% of skills as CC-applying. The rest are clean damage skills. CC should be a *highlight*, not a constant.

**Risk: Knockdown-as-combo-finisher trivializes encounters.** If allies always pile on a knocked-down enemy for 1.5× damage, fights become "land knockdown, win." Mitigation: knockdown application is *expensive* (high speed cost on origin skill, gated to specific high-tier combos). It should feel like an *earned* moment. Boss enemies have higher knockdown resistance to prevent trivializing them.

**Risk: Caster heroes become unplayable due to interrupts.** If interrupts are too easy to apply, caster archetypes fail because they never finish a cast. Mitigation: interrupts should require *deliberate* setup (a skill specifically designed as a caster-counter, or sustained damage during the cast window). Random hits during a cast should *not* automatically interrupt.

---

## Final note

This system is the deepest gameplay layer in the game. It's also the one most likely to produce *the moments players remember* — the dramatic last-second speed burst before a kill, the desperate disengage to rebuild, the tactical stagger that breaks an opponent's combo wind-up, the breathtaking counter that swaps an exchange mid-flight.

Build it carefully. Iterate often. Don't ship until it *feels* right.

When this system is working, combat won't just look like anime. It'll *play* like it.
