# HAND_TO_HAND_COMBAT.md

> **Tier 2 — Stable.** Prescriptive design spec for the hand-to-hand combat layer. Lives in `Docs/design/` alongside `COMBAT_DESIGN.md`.
>
> This document specifies the structural rules, state machine, and animation requirements of close-range melee combat. Tuning numbers given as starting values; tune via training scene playtesting.
>
> For the broader combat architecture (resource model, AI brain, exchanges, status effects), see `COMBAT_DESIGN.md`. This doc is the deep dive on one specific layer of that architecture.
>
> **Scope: 1v1 close-range melee combat.** Multi-unit engagements are deferred to a future revision.

---

## 1. Purpose and design philosophy

Hand-to-hand combat is the most common interaction in the game. Punches, kicks, blocks, hit reactions — these happen constantly. The way they cycle, breathe, and resolve defines what combat *feels* like more than any other system.

The design philosophy is **rhythmic combat**: real fights are not continuous trading; they cycle between close exchanges and brief separations. Two boxers don't stand toe-to-toe trading punches forever — they exchange, break, reset, re-engage. Anime combat exaggerates this rhythm but doesn't break it. The game's combat must reflect this rhythm.

Three principles drive every rule below:

**Combat breathes.** Exchanges are short bursts (1-3 seconds of close trading), followed by separations (1-1.5 seconds of distance), followed by re-engagements. The cycle is rhythmic and visible.

**The orchestrator coordinates pairs.** During an exchange, two units are coordinated by a higher-level system that decides who attacks and who defends. Individual unit AI does not unilaterally decide both sides of an exchange.

**Reactions take time.** Units don't react instantly to combat events. There's a "spotting" delay when first detecting an enemy, and a "decision lag" between exchanges. These delays are configurable and produce natural-feeling pacing.

---

## 2. Combat phases — the cycle

Hand-to-hand combat at the macro level cycles through five phases. Each phase has clear entry and exit conditions, specific AI behaviors, and required animations.

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│   ┌────────────┐    ┌────────────┐    ┌─────────────┐               │
│   │  Spotting  │ →  │  Approach  │ →  │  Engagement │               │
│   └────────────┘    └────────────┘    └──────┬──────┘               │
│                                              │                      │
│                                              ▼                      │
│                                       ┌─────────────┐               │
│                          ┌─────────── │  Exchange   │               │
│                          │            └──────┬──────┘               │
│                          │                   │                      │
│                          │                   ▼                      │
│                          │            ┌─────────────┐               │
│                          └─────────── │  Separation │ ──────────┐   │
│                                       └─────────────┘           │   │
│                                                                 │   │
│                                       (re-engage cycle) ────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.1 Spotting

**Purpose:** The brief delay when a unit first detects a hostile within combat range. This is the "I see you" beat before pursuit begins.

**Entry conditions:**
- A hostile unit enters the spotting range (default: 8m) of this unit
- This unit is in non-combat state (idle or patrol)

**Behavior during phase:**
- Unit plays an "alert" idle variant or a brief reaction animation
- Unit faces the detected hostile
- No movement during this phase
- Random delay between **0.3s** (spot fast) and **0.7s** (spot slow), modified by Perception attribute

**Exit conditions:**
- Spotting timer expires → transition to **Approach**

**Modifiers:**
- High Perception (>14) reduces minimum spotting time to 0.2s
- Low Perception (<8) extends maximum spotting time to 1.0s
- "Surprised" state (attacked from behind) doubles spotting time

**Animation requirements:**
- `Loco_Idle_Alert` — alert idle variant (different posture, head turned toward threat)
- `React_Spotted` — optional brief reaction (small flinch or "guard up" motion). Can fall back to alert idle.

### 2.2 Approach

**Purpose:** Movement from spotting distance into engagement range. Where the unit closes the gap.

**Entry conditions:**
- Coming from Spotting, OR
- Coming from Separation with intent to re-engage

**Behavior during phase:**
- Unit moves toward target at **traversal speed** (sprint or run)
- Faces target throughout movement
- No combat actions during pure approach (skills, attacks suppressed)

**Exit conditions:**
- Distance to target ≤ engagement range (default: 2.0m) → transition to **Engagement**
- Target moves away faster than this unit can close → unit may abort approach (return to non-combat behavior) or continue chasing (stance-dependent)

**Animation requirements:**
- `Loco_Run_Combat` — running with combat-ready posture (different from casual run)
- `Loco_Sprint_Combat` — full-tilt sprint with combat awareness
- `Loco_Stop_Sharp` — sharp stop animation when arriving at engagement range

### 2.3 Engagement

**Purpose:** The "in combat range, deciding what to do" state. Units circle, posture, prepare. This is the *neutral* state of close combat.

**Entry conditions:**
- Coming from Approach (just arrived in engagement range), OR
- Coming from Exchange (exchange just resolved, neither unit separated)

**Behavior during phase:**
- Both units move at **combat movement speed** (much slower than traversal — see section 4)
- Units circle, hold position, or shuffle in micro-distances
- AI continuously evaluates whether to initiate an exchange or break off
- Decision lag (random 0.2-0.5s) between consecutive decisions to prevent jittery behavior

**Exit conditions:**
- Either unit's AI commits to an attack → transition to **Exchange** (both units enter exchange together)
- Either unit's AI decides to disengage → transition to **Separation**
- Distance opens beyond engagement range (one unit drifted out) → return to **Approach** if intent is still hostile

**Animation requirements:**
- `Loco_Idle_Combat` — combat-ready idle (already in animation catalog)
- `Loco_Walk_Combat` — slow combat-context walk
- `Loco_Strafe_Left_Combat` and `_Right_Combat` — combat-context strafing
- `Loco_Backstep_Combat` — small defensive step backward

### 2.4 Exchange

**Purpose:** The active combat moment. One unit attacks, the other reacts. Hits land, blocks fire, reactions play.

**Entry conditions:**
- Either unit's AI committed to an attack from Engagement state
- Both units are within striking range (default: 1.5m or less)

**Behavior during phase:**
- The **orchestrator** (see section 3) decides who is the active attacker and who is the active defender
- Active attacker plays attack animation
- Active defender plays defense response (block, dodge, hit reaction, etc.)
- Synchronized timing: defender's reaction is timed to attacker's impact frame
- During the exchange, neither unit's AI decides further actions — the orchestrator owns both units
- Multi-hit combos are still single exchanges (one continuous attacker commitment), with the defender potentially reacting to each hit

**Exit conditions:**
- Attacker's combo completes → transition to **Engagement** or **Separation** (orchestrator decides)
- Defender successfully counters or interrupts the attacker → transition to **Exchange** with roles swapped
- Either unit reaches 0 HP → transition to **Death** (out of scope here)

**Animation requirements:**
- All attack animations (punches, kicks, combos) — covered by Kubold's Fighting Animset Pro
- All defensive responses (blocks, dodges) — covered by Kubold's Fighting Animset Pro
- All hit reactions (light/heavy, directional) — covered by Kubold's Fighting Animset Pro
- The exchange itself is choreography over these existing clips, not new animations

### 2.5 Separation

**Purpose:** The breathing room after an exchange. Units create distance, reset position, prepare to re-engage or disengage.

**Entry conditions:**
- Coming from Exchange, AND
- Either unit's AI decided to break off (or both did mutually)

**Behavior during phase:**
- The separating unit moves backward at **disengage speed** (faster than combat movement, slower than full sprint)
- Movement uses backstep, jump-back, or backflip animation depending on stance and committed distance
- Random duration between **1.0s** and **1.5s**
- During separation, the separating unit cannot initiate attacks
- The non-separating unit (if only one is separating) may pursue or hold position
- Resource recovery (energy, speed) ticks at slightly elevated rates during separation

**Exit conditions:**
- Separation duration expires AND distance is sufficient (≥3.0m from opponent) → transition to **Engagement** at distance
- If unit decides to fully disengage from combat → transition to non-combat behavior (out of this doc's scope)

**Animation requirements:**
- `Loco_Backstep_Fast` — quick backward step (~1-1.5m of motion)
- `Dodge_Backflip` — dramatic backflip-style separation (~2-3m of motion). Already in catalog as Dodge_Backflip.
- `Loco_JumpBack_Short` — short jump backward (~1.5m)

These animations have built-in motion. The code should let the animation drive distance for separation, not override with scripted travel. Separation is one of the cases where root motion is preferred.

---

## 3. The orchestrator

The orchestrator is the system that coordinates exchanges. When two units enter Exchange phase together, the orchestrator decides:

- Which unit is the active attacker
- Which unit is the active defender
- What attack the attacker commits to
- Whether the defender's response is dictated or chosen by the defender's brain
- When the exchange ends and what each unit does next

### 3.1 Why the orchestrator exists

Without an orchestrator, both units' AI brains would simultaneously decide to attack, both attacks would fire, both hits would land (or both would miss), and combat would look like simultaneous flailing rather than structured exchange. The orchestrator imposes the turn-taking structure that makes combat *read* as a fight.

### 3.2 Orchestrator decision logic

When both units enter Exchange phase, the orchestrator runs:

**Step 1 — Who attacks first?**

Compare initiative scores. Initiative is computed from:

- Current speed (kinetic resource): higher = more initiative
- Stance: aggressive stances get +20 initiative, defensive stances get -20
- Recent exchange history: a unit who just defended gets +10 (counter-attack opportunity)
- Random component: ±10 initiative for natural variation

Whichever unit has higher initiative becomes the **active attacker** for this exchange. The other becomes the **active defender**.

**Step 2 — What attack does the attacker commit to?**

The attacker's brain selects an attack from its available options based on resources (speed, energy), stance, and current situation. This is normal skill selection logic, not orchestrator-specific.

**Step 3 — What is the defender's response?**

This is the most nuanced decision. Two patterns:

- **Dictated response (rare):** orchestrator forces a specific defender response based on the chosen engagement scenario. Used only for choreographed engagement moves where the response is part of the choreography (e.g., HighSpeedClash from `COMBAT_DESIGN.md` mandates both units commit attacks).
- **Free response (default):** defender's brain independently decides how to respond using the standard `DefenseDecision` flow (dodge, block, cast, take hit). Orchestrator just informs the defender that an attack is coming and provides attack metadata.

For hand-to-hand combat scope (this doc), use **free response by default**. Defender chooses how to react to incoming attacks.

**Step 4 — Resolution timing.**

Both units' animations are synchronized to a shared exchange clock. Attacker's impact frame is the canonical moment; defender's reaction (hit react, block hold, dodge frame) is timed to land at that same moment.

**Step 5 — Post-exchange decision.**

When the attacker's animation completes (or the exchange is interrupted), the orchestrator decides what happens next:

- If the defender successfully countered: roles swap, new exchange begins immediately (no Engagement phase between)
- If the defender survived but is staggered: brief pause, then transition to Engagement
- If either unit chose to separate (random check, stance-modified): transition to Separation
- Otherwise: return to Engagement

### 3.3 Separation likelihood

After each exchange, each unit independently rolls a **separation chance**:

- Aggressive stance: 15% chance to separate
- Balanced stance: 35% chance
- Defensive stance: 60% chance
- Modified by HP: low HP (<40%) increases by 20%
- Modified by speed posture: very low speed (<20) increases by 25%

If either unit's roll succeeds, that unit separates. If both roll succeed, both separate (mutual breakoff). If neither, they remain in Engagement.

These percentages are starting values. Tune based on combat rhythm in playtesting — if combat feels too sticky (units never separate), raise the chances. If combat feels disconnected (units always separate), lower them.

### 3.4 Orchestrator subsystem

`BattleH2HOrchestrator` (new MonoBehaviour subsystem). Responsibilities:

- Detects when two units enter Exchange phase together
- Runs the decision logic above
- Coordinates exchange timing between attacker and defender
- Determines post-exchange transitions
- Notifies the combat state machine of phase changes

Public API:

- `RegisterPair(unitA, unitB) → ExchangeHandle | null` — called when two units enter exchange
- `ResolveExchange(handle, attackResult)` — called after attack resolution to determine next phase
- `GetActiveExchange(unit) → ExchangeHandle | null` — query for unit's current exchange state
- `CancelExchange(handle, reason)` — emergency exit (e.g., one unit dies mid-exchange)

This subsystem extends the `BattleExchangeStager` from `COMBAT_DESIGN.md` with the specific orchestration logic for H2H combat.

---

## 4. Combat movement speeds

A core insight: combat-context movement is fundamentally slower than traversal. Units don't sprint when they're 2 meters from each other; they shuffle, circle, posture.

Three speeds to differentiate:

| Speed name | Value (m/s) | Used during |
|---|---|---|
| **Traversal speed** | 5.0-7.0 (existing sprint/run) | Approach phase, long-distance movement |
| **Combat movement speed** | 1.5-2.0 | Engagement phase, in-combat circling |
| **Disengage speed** | 3.0-4.0 (or animation-driven) | Separation phase, breaking off |

These are starting values. Configure per-unit if heroes vary (e.g., a Wraith-stance hero has higher combat movement speed; a Sentinel has lower).

### 4.1 Speed transitions

When a unit transitions between phases, movement speed shifts smoothly over ~0.2s rather than snapping. The unit doesn't instantly change from sprint to combat shuffle — there's a brief deceleration.

### 4.2 Speed-resource interaction

The kinetic speed *resource* (from `COMBAT_DESIGN.md`) gain rates also vary by combat phase:

- During Approach: speed gain at +8/sec (closing distance is aggressive)
- During Engagement (circling): speed gain at +6/sec (defined in `COMBAT_DESIGN.md`)
- During Engagement (holding still): -5/sec idle drain
- During Separation: speed gain at +4/sec (modest, since unit is moving but not engaging)
- During Exchange: speed varies based on actions taken (already specified in `COMBAT_DESIGN.md`)

---

## 5. Spotting and decision lag — reaction timing

Real units don't react instantly. Two reaction delays produce natural pacing:

### 5.1 Spotting delay (initial detection)

When a hostile first enters detection range:

- Random duration between **0.3s** and **0.7s**
- Configurable as `spottingMinTime` and `spottingMaxTime` on UnitDefinition
- Modified by Perception attribute (formula in section 2.1)
- During this delay, unit plays alert idle but does not move

### 5.2 Decision lag (between exchanges)

Within Engagement phase, when the AI evaluates whether to commit to an attack or break off:

- Random duration between **0.2s** and **0.5s** between consecutive decision evaluations
- Configurable as `decisionLagMin` and `decisionLagMax` on UnitDefinition
- Prevents jittery rapid-fire AI decisions
- During this lag, unit continues current behavior (circling, holding) but is not making fresh decisions

### 5.3 Configurability for testing

Both delay ranges should be exposed in the training scene UI as sliders. Setting both to 0 produces "instant reaction" testing (good for verifying logic). Setting both to high values produces "deliberate" testing (good for visual evaluation). Default to the values above for general play.

---

## 6. Phase transitions — the state machine

The hand-to-hand combat state machine extends the existing `TerrainBattleUnit` state machine. New states:

- `H2H_Spotting`
- `H2H_Approaching`
- `H2H_Engaged` (replaces or extends generic Engagement state)
- `H2H_Exchange` (extends existing Exchange handling)
- `H2H_Separating`

Transition table:

| From | To | Trigger |
|---|---|---|
| `Idle` (non-combat) | `H2H_Spotting` | Hostile detected within spotting range |
| `H2H_Spotting` | `H2H_Approaching` | Spotting timer expires |
| `H2H_Approaching` | `H2H_Engaged` | Distance ≤ engagement range |
| `H2H_Approaching` | `Idle` | Hostile out of range, no chase intent |
| `H2H_Engaged` | `H2H_Exchange` | Either unit's AI commits attack (orchestrator activates) |
| `H2H_Engaged` | `H2H_Separating` | Either unit decides to break off |
| `H2H_Engaged` | `H2H_Approaching` | Distance opens beyond engagement range |
| `H2H_Exchange` | `H2H_Engaged` | Exchange resolves, no separation triggered |
| `H2H_Exchange` | `H2H_Separating` | Exchange resolves, separation roll succeeded |
| `H2H_Exchange` | `H2H_Exchange` | Counter succeeded, roles swap, new exchange begins |
| `H2H_Separating` | `H2H_Engaged` | Separation duration expires, both units intend to re-engage |
| `H2H_Separating` | `Idle` | Unit fully disengaged from combat |

### 6.1 Asymmetric phase states

In a 1v1 fight, the two units may be in different phases at the same time:

- UnitA in `H2H_Engaged`, UnitB in `H2H_Separating` (UnitB jumping back, UnitA following or holding)
- UnitA in `H2H_Approaching`, UnitB in `H2H_Engaged` (UnitB stationary, UnitA closing)
- UnitA in `H2H_Exchange` (attacker), UnitB in `H2H_Exchange` (defender) — synchronized

The state machine must handle this. Each unit has its own state independently. The orchestrator coordinates only during shared exchanges.

---

## 7. Animation requirements summary

This section consolidates animation requirements from sections 2-5 plus a few additions specific to the H2H system. Most animations are already covered by Kubold packs (Movement Animset Pro + Fighting Animset Pro).

### 7.1 Required animations

| Animation | Phase used | Source |
|---|---|---|
| `Loco_Idle_Alert` | Spotting | Kubold (alert idle variant) |
| `React_Spotted` (optional) | Spotting | Kubold or Mixamo |
| `Loco_Run_Combat` | Approach | Kubold Movement Animset |
| `Loco_Sprint_Combat` | Approach (long distance) | Kubold Movement Animset |
| `Loco_Stop_Sharp` | Approach → Engagement | Kubold Movement Animset |
| `Loco_Idle_Combat` | Engagement | Kubold Fighting Animset (combat idle) |
| `Loco_Walk_Combat` | Engagement | Kubold Fighting Animset (combat walk) |
| `Loco_Strafe_Left_Combat` | Engagement (circling) | Kubold Fighting Animset |
| `Loco_Strafe_Right_Combat` | Engagement (circling) | Kubold Fighting Animset |
| `Loco_Backstep_Combat` | Engagement (small backstep) | Kubold Fighting Animset |
| All punch/kick attack clips | Exchange (attacker) | Kubold Fighting Animset |
| All hit reaction clips | Exchange (defender) | Kubold Fighting Animset |
| All block animations | Exchange (defender) | Kubold Fighting Animset |
| `Loco_Backstep_Fast` | Separation (short separation) | Kubold or composite |
| `Dodge_Backflip` | Separation (dramatic) | Already in catalog |
| `Loco_JumpBack_Short` | Separation (medium) | Kubold or Mixamo |

### 7.2 Animations with built-in motion (root motion expected)

The following animations include forward/backward motion and should use the animation's built-in displacement rather than scripted-travel position adjustment:

- `Loco_Run_Combat`, `Loco_Sprint_Combat` — animation drives forward motion
- `Loco_Stop_Sharp` — animation includes deceleration distance
- `Loco_Backstep_Combat`, `Loco_Backstep_Fast` — animation drives backward motion
- `Dodge_Backflip` — animation includes the full backward arc
- `Loco_JumpBack_Short` — animation includes the jump arc and landing

For these, set `useRootMotion: true` on the relevant `AttackProfile` or movement profile. Code does not override position; the animation does.

### 7.3 Animations with scripted travel (code drives motion)

The following require scripted-travel position adjustment (per `COMBAT_DESIGN.md`'s scripted travel pattern):

- All attack animations (punches, kicks, combos) — code positions attacker before strike, animation plays in place
- Approach final closure — code drives the last 0.3s of approach to land exactly at engagement range

For these, set `useRootMotion: false` and specify `desiredImpactDistance` and `positionAdjustDuration` on the `AttackProfile`.

### 7.4 The naming-scheme implication

When the animation naming convention is applied (per `ANIMATION_NAMING.md` once it exists), each animation needs a `motionType` tag in its filename or metadata:

- `_root` suffix indicates root-motion animation
- `_inplace` suffix indicates animation that plays without position change

Example: `Loco_Sprint_Combat_root.fbx` vs `Attack_Punch_Light_inplace_01.fbx`. This makes Claude Code's job easier when wiring animations — the convention tells the system which mode to use.

---

## 8. AI decision logic per phase

### 8.1 Spotting phase AI

No AI decisions during spotting. Unit waits out the spotting timer, plays alert idle, faces target.

### 8.2 Approach phase AI

Minimal AI: pursue target until in engagement range. Stance modifies pursuit aggressiveness:

- Aggressive: pursues regardless of cost; will chase a fleeing target across the arena
- Balanced: pursues if target is reasonably close; gives up if target opens distance significantly
- Defensive: pursues conservatively; happy to engage at range if target is willing

### 8.3 Engagement phase AI

This is where most decisions happen. Each decision tick (gated by decision lag):

**Step 1 — Resource check.**
If speed < 20 (sluggish), strongly prefer to circle/build rather than commit.
If energy < 15 (drained), avoid casting skills.

**Step 2 — Threat assessment.**
Is the opponent in a vulnerable state (winded, low HP, recovering)? → bias toward attack.
Is the opponent primed and aggressive (high speed, attacking stance)? → bias toward defense or separation.

**Step 3 — Stance bias.**
Apply the stance's archetype preferences (per `COMBAT_DESIGN.md`).

**Step 4 — Action selection.**
Choose between:
- Commit attack → triggers Exchange phase
- Hold position → continue Engagement
- Circle → continue Engagement (movement only)
- Separate → triggers Separation phase
- Cast skill → triggers Skill cast (out of H2H scope, see `COMBAT_DESIGN.md`)

### 8.4 Exchange phase AI

The defender's brain runs the standard `DefenseDecision` logic from `COMBAT_DESIGN.md`. The attacker's brain is committed and does not re-decide mid-exchange (with one exception: if defender counters, roles swap and original attacker becomes defender, who now runs `DefenseDecision`).

### 8.5 Separation phase AI

Minimal AI during separation. The separating unit moves backward and ticks down the separation timer. At expiration, the unit decides:

- Re-engage (default) → transition to Engagement phase, re-approach if needed
- Fully disengage (rare, only at very low HP or on retreat order) → exit combat

---

## 9. Subsystem additions

### 9.1 `BattleH2HOrchestrator` (new)

Described in section 3.4. The exchange coordinator for H2H combat.

### 9.2 `BattleH2HPhaseSystem` (new)

Owns the H2H phase states for each unit and handles phase transitions per the state machine in section 6.

Public API:
- `GetPhase(unit) → H2HPhase`
- `TransitionPhase(unit, newPhase, reason)`
- `OnPhaseEnter(unit, phase) → event`
- `OnPhaseExit(unit, phase) → event`

### 9.3 Updates to existing subsystems

**`BattleAIBrain`:**
- Reads current H2H phase to filter relevant decisions (no skill selection during Approach, etc.)
- Uses decision lag timer per section 5.2

**`BattleMovementSystem`:**
- Reads current H2H phase to determine movement speed (traversal vs combat vs disengage)
- Implements smooth speed transitions per section 4.1

**`BattleAnimancerDriver`:**
- Selects appropriate locomotion clip based on current phase (combat-speed walk vs traversal run, etc.)

---

## 10. Data model additions

### 10.1 New enums

```csharp
public enum H2HPhase
{
    NotEngaged,
    Spotting,
    Approaching,
    Engaged,
    Exchange,
    Separating
}
```

### 10.2 New fields on `UnitDefinition`

| Field | Type | Default | Purpose |
|---|---|---|---|
| `spottingRangeMeters` | `float` | 8.0 | Range at which hostiles are detected |
| `engagementRangeMeters` | `float` | 2.0 | Range at which Engagement phase begins |
| `strikeRangeMeters` | `float` | 1.5 | Range at which Exchange phase can begin |
| `spottingMinTime` | `float` | 0.3 | Minimum spotting delay |
| `spottingMaxTime` | `float` | 0.7 | Maximum spotting delay |
| `decisionLagMin` | `float` | 0.2 | Minimum decision lag |
| `decisionLagMax` | `float` | 0.5 | Maximum decision lag |
| `combatMovementSpeed` | `float` | 1.5 | Speed during Engagement |
| `disengageSpeed` | `float` | 3.5 | Speed during Separation (or 0 to use animation root motion) |
| `separationMinDuration` | `float` | 1.0 | Min separation phase duration |
| `separationMaxDuration` | `float` | 1.5 | Max separation phase duration |

### 10.3 New fields on `StanceDefinition`

| Field | Type | Default | Purpose |
|---|---|---|---|
| `separationChanceModifier` | `float` | 0.0 | Added to base separation chance |
| `initiativeBonus` | `int` | 0 | Added to initiative score |
| `pursuitAggression` | `float` | 1.0 | Multiplier on chase distance willingness |

---

## 11. Testing in the training scene

### 11.1 Required test setup controls

The training scene needs:

- Position presets: Adjacent (1m), Mid-range (3m), Long-range (6m), Custom
- Phase forcing: button to put a unit into a specific phase regardless of conditions
- Stance assignment: dropdown per unit to set stance
- Resource overrides: sliders to set unit speed, energy, HP for testing specific scenarios
- Reaction timing: sliders to override spotting/decision lag for testing

### 11.2 Verification scenarios

After implementing the H2H system, the following scenarios must verify correctly:

**Scenario H1 — Basic spotting and approach.**
- Setup: units at Long-range, no combat
- Trigger: enable hostility on Subject
- Expected: Subject enters Spotting phase, plays alert idle for ~0.5s, transitions to Approach, runs toward Dummy
- Log verification: phase transition events fire in correct order

**Scenario H2 — Engagement at distance.**
- Setup: units at Mid-range
- Trigger: Subject approaches Dummy
- Expected: Subject runs in, decelerates at engagement range, transitions to Engaged, locomotion speed drops to combat-speed
- Log verification: speed change happens at engagement range, not at strike range

**Scenario H3 — Single exchange.**
- Setup: units at Adjacent, both in Engaged phase
- Trigger: Subject commits punch
- Expected: orchestrator activates, Subject becomes attacker, Dummy becomes defender, Dummy reacts (block or hit react), exchange resolves, return to Engaged
- Log verification: orchestrator decision logged, both units' animations synchronized

**Scenario H4 — Exchange into separation.**
- Setup: units at Adjacent, both in Engaged, Subject in Defensive stance
- Trigger: Dummy commits punch, Subject's separation roll forced to succeed
- Expected: exchange resolves, Subject transitions to Separating, plays backstep, moves ~1.5m back, resumes Engaged from new position
- Log verification: separation phase entered and exited correctly

**Scenario H5 — Mutual separation and re-engagement.**
- Setup: units at Adjacent, both forced to separate after exchange
- Trigger: exchange completes, both units roll separation success
- Expected: both units back away simultaneously, both reach Mid-range, both decide to re-engage, both run back in
- Log verification: cycle completes cleanly

**Scenario H6 — Counter and role swap.**
- Setup: units at Adjacent, Subject's defense decision forced to "counter"
- Trigger: Dummy attacks Subject, Subject's brain returns "counter" as response
- Expected: Subject's counter fires immediately after Dummy's attack, roles swap, new exchange begins with Subject as attacker
- Log verification: role swap logged, no Engagement phase between the two exchanges

**Scenario H7 — Decision lag behavior.**
- Setup: units in Engaged at Adjacent, decision lag set to 1.5s (artificially high)
- Trigger: enable normal AI
- Expected: visible pause between decisions, units don't rapidly switch behaviors
- Visual verification: combat doesn't feel jittery

**Scenario H8 — Combat speed differentiation.**
- Setup: units at Mid-range, both running toward each other
- Trigger: observe approach
- Expected: at engagement range, both units' visible movement speed drops noticeably
- Visual verification: clear difference between sprint and combat shuffle

These eight scenarios cover the H2H system end-to-end. Each becomes a button in the training scene.

---

## 12. Implementation phases

> **Status (2026-05-08):** Phases H-1 through H-5 shipped in a single end-to-end session. The system is wired into the existing TrainingDummy scene through a new `H2H_Canvas` panel. Phase H-6 (tuning) is ongoing and shipped numbers are starting values per the spec above.

### Phase H-1: Foundation — **shipped**

- ✅ Added `H2HPhase` enum (`Enums.cs`) and `BattleH2HPhaseSystem` subsystem (event dispatch, decision-lag gate, override hooks)
- ✅ Added all H2H fields to `UnitDefinition` (ranges, timing, movement speeds, separation duration)
- ✅ Combat movement speed switches via `KuboldLocomotionDriver.ResolvePhaseMaxSpeed` and `TrainingPlayerController._clampToH2HPhase`
- ✅ Training scene controls — position presets, phase forcing per unit, sliders, status panels — built via `TacticalRPG → H2H → Build Training Scene` menu

### Phase H-2: Spotting and approach — **shipped**

- ✅ Spotting phase with randomized timer (per-unit `spottingMinTime`/`spottingMaxTime`, slider override on phase system)
- ✅ Approach phase: brain calls `CharacterController.Move` at `traversalSpeed`; locomotion driver picks `run_forward` clip
- ✅ Phase transitions wired: `NotEngaged` → `Spotting` → `Approaching` → `Engaged` (and back to `NotEngaged` on target loss)

### Phase H-3: Engagement and exchange orchestration — **shipped**

- ✅ `BattleH2HOrchestrator` subsystem with initiative scoring (speed + stance bonus + just-defended counter bias + RNG)
- ✅ `IH2HExchangeAgent` interface decouples orchestrator timing from unit playback — `H2HUnit` implements it for the test scene; future battle integration adds a `TerrainBattleUnit` adapter
- ✅ Free-response defender (per-stance behavior bias picks Eat / Block / Dodge)
- ✅ Decision-lag gate prevents jittery commits in `Engaged`

### Phase H-4: Separation and re-engagement — **shipped**

- ✅ Separation phase with backstep clip (`dodge_backward` / library fallback), brain drives backward motion at `disengageSpeed`
- ✅ Per-unit separation roll modulated by `StanceDefinition.separationChanceModifier`, HP fraction, and current speed
- ✅ Transitions: `Exchange` → `Separating` → `Engaged` (or `Approaching` if drifted out of engagement range)

### Phase H-5: Counter and role swap — **shipped**

- ✅ Defender's `OnExchangeImpactDefender` returns a counter flag; orchestrator immediately swaps roles and registers a fresh exchange with no `Engaged` phase between
- ✅ `H2HUnit.CounterChance` exposed for scenario testing

### Phase H-6: Tuning and polish — **ongoing**

- Phase numbers in section 4 are starting values; tune via the training scene UI and the eight verification scenarios.
- Decision-lag override slider already exposed in the H2H_Canvas; spotting override too.
- Scenario buttons (1-8) wire one-click reproductions of every spec scenario.
- 03_DATA_MODELS.md and 04_BATTLE_SYSTEM.md updated to document the new enum, fields, and subsystems.

Subsequent work for this layer:
- Adapter wrapping `TerrainBattleUnit` as `IH2HExchangeAgent` for the live battle scene
- Replace the placeholder per-phase resource tick on `H2HUnit.TickResources` with a route through the existing `BattleSpeedSystem` (currently the H2H layer ticks its own speed/energy resources directly, parallel to the speed system used by `TerrainBattleUnit`)
- Replace the placeholder strike resolution in `H2HUnit.OnExchangeImpactDefender` with a route through `BattleCombatResolver` once the move-engine bridge lands

### Subsequent work shipped in the same end-to-end pass

The original §12 implementation phases were paired with a follow-up batch covering the simulation depth + presentation polish:

- **Multi-hit combos.** `H2HUnit.Combo` is a list of `ComboHit` entries with attack id, archetype, normalized impact time, damage, speed cost. The orchestrator schedules N impact frames and the defender re-decides per hit. Default combos: `BasicJab` (1 hit), `JabHookUppercut` (3 hits), `HeavyKick` (1 heavy).
- **AttackProfile-lite pre-position.** Combos carry a `desiredImpactDistance` and `positionAdjustDuration`; the orchestrator smoothsteps the attacker into impact distance before the first strike via a coroutine.
- **Smooth speed transitions.** `KuboldLocomotionDriver.ResolvePhaseMaxSpeed` smoothdamps over `_phaseSpeedSmoothTime` (default 0.2s) instead of snapping at phase change, matching §4.1.
- **Resource model.** `H2HUnit` ticks Speed (0-100, soft cap 70) and Energy (0-`maxEnergy`) per current phase: Approach +8/sec, Engaged-moving +6/sec, Engaged-idle -5/sec, Separating +4/sec, NotEngaged drain. Attack speedCost is debited on commit; block grants +5 energy.
- **Brain reads thresholds.** `H2HUnitBrain.TickEngaged` resource-locks commits below `stance.speedReserveFloor` and below 5 energy, biases disengage when HP fraction < 0.4, and reports the resource state in the BRAIN log line. Big-combo gating uses `stance.speedThresholdBigCombo` to switch from light to multi-hit picks.
- **Defensive-stance approach.** `TickApproaching` reads `stance.behaviorBias`: Aggressive closes at traversal speed; Defensive drifts in at 60% of combat speed with lateral evasion; Balanced eases between traversal and combat as it crosses Mid-range.
- **Hit-stop on impact.** Both attacker and defender pause Animancer's playable graph for `_hitStopSeconds` (default 60ms, real-time) when an impact lands.
- **Camera shake on impact.** `H2HCameraShake` MonoBehaviour added on `Camera.main` on demand; Perlin jitter with quartic falloff.
- **Particle burst.** Programmatic glowing sphere spawned at hit point (or `_impactBurstPrefab` if assigned), 180ms lifetime.
- **Death + victory beat.** HP=0 → `H2HUnit.HandleDeath`: cancel active exchange, transition to `NotEngaged`, suppress locomotion, fire `OnDeath`. The H2HLogger emits a `DEATH` line. The surviving unit's brain naturally returns to NotEngaged once its target stops being hostile (IsDead filter on `ConsidersHostile`).
- **Foot IK.** `H2HFootIK` per-unit (Animator.OnAnimatorIK with downward raycast). Suppresses during Exchange so attack clips aren't ground-clamped. Idle while Animancer drives the rig without an Animator Controller.
- **Time-scale dial.** H2H_Canvas bottom-right TimeScale panel with 5/10/25/50/100% quick-buttons + slider (0.05-2.0). Sets `Time.timeScale` and `Time.fixedDeltaTime` together so physics stays deterministic at low scales.

---

## 13. What this design explicitly does not include

To keep scope clear, the following are out of this doc's scope:

- **Multi-unit engagements (3+ units)** — pure 1v1 only. Multi-unit handling is future work.
- **Skill casting during combat** — covered by `COMBAT_DESIGN.md`. This doc handles the H2H exchange flow; skill casts are a parallel layer.
- **Status effects and CC** — covered by `COMBAT_DESIGN.md`. CC interrupts H2H phases (a stunned unit can't cycle through Engagement→Exchange) but the CC system itself is defined elsewhere.
- **Camera framing during exchanges** — presentation concern, defined in `07_PRESENTATION.md` when written.
- **Death and victory conditions** — out of scope; combat simply ends when one unit reaches 0 HP.
- **Engagement scenarios (HighSpeedClash, etc.)** — these are choreographed exchanges per `COMBAT_DESIGN.md`. They override the default H2H flow when triggered. This doc describes the default; engagement scenarios are exceptions.

---

## 14. First-session implementation prompt

This prompt scopes Phase H-1 (foundation) for the first focused implementation session. It assumes the training scene exists with basic structure.

```
Implement Phase H-1 of the hand-to-hand combat system, per Docs/design/HAND_TO_HAND_COMBAT.md.

This session builds the foundation only. Spotting, Approach, Exchange, and Separation phases are NOT in scope for this session — they come in subsequent phases.

TASKS, IN ORDER:

1. Add the H2HPhase enum and BattleH2HPhaseSystem subsystem.
   - Create H2HPhase enum in Assets/Scripts/DataModels/ with values: NotEngaged, Spotting, Approaching, Engaged, Exchange, Separating
   - Create BattleH2HPhaseSystem MonoBehaviour subsystem in Assets/Scripts/ThirdPerson/
   - Implement public API: GetPhase(unit), TransitionPhase(unit, newPhase, reason), with events OnPhaseEnter/OnPhaseExit
   - Phase data per unit lives in this subsystem, not on UnitRuntime
   - For this session, phase transitions are manually triggered only (no automatic transitions yet)

2. Add UnitDefinition fields for combat movement.
   - combatMovementSpeed (float, default 1.5)
   - disengageSpeed (float, default 3.5)
   - Update existing UnitDefinitions with these fields, default values from spec

3. Implement combat movement speed switching.
   - In BattleMovementSystem (or wherever locomotion speed is determined), read the unit's H2H phase
   - When phase is Engaged: use combatMovementSpeed
   - When phase is Approaching: use existing traversal speed (sprint/run)
   - When phase is Separating: use disengageSpeed
   - Smooth speed transitions over ~0.2s when phase changes (not instant)

4. Add training scene controls for testing.
   - Position presets: Adjacent (1m), Mid-range (3m), Long-range (6m), Custom slider
   - Phase forcing buttons per unit: "Force Subject to Engaged", "Force Subject to Approaching", "Force Subject to Separating", "Force Subject to NotEngaged"
   - Display panel showing each unit's current phase
   - Display panel showing each unit's current movement speed

5. Verification scenarios.
   - Verify Scenario H8 from the design doc: at Long-range, force both units to Approaching, observe sprint-speed locomotion. Then force to Engaged, observe combat-speed locomotion. The visual speed difference must be obvious.
   - Verify position presets work: clicking Adjacent puts units 1m apart facing each other; Mid-range 3m; Long-range 6m.
   - Verify phase forcing works: clicking the buttons immediately changes the unit's reported phase, and (when applicable) movement speed changes accordingly.

DO NOT in this session:
- Implement automatic phase transitions
- Implement spotting timers or decision lag
- Implement the orchestrator
- Implement separation chance rolls
- Implement counter logic
- Implement engagement scenarios

Update Docs/03_DATA_MODELS.md to document the new enum and UnitDefinition fields. Update Docs/04_BATTLE_SYSTEM.md to reference the new BattleH2HPhaseSystem subsystem.

When done, report:
- What was built
- What still needs verification (so I can manually click through the test scenarios)
- Any deviations from the spec, and why
```

---

## 15. Cross-references

- `LONG_TERM_VISION.md` — the game's overall vision, of which this combat is the central pillar
- `COMBAT_DESIGN.md` — the broader combat architecture; this doc is one specific layer of it
- `ENVIRONMENT_DESIGN.md` — environment integration (knockback into terrain affects how Separation plays out near walls)
- `ANIMATION_CATALOG.md` — full list of animations the project will eventually need
- `03_DATA_MODELS.md` — Tier 1, will need updates to document new enums and fields
- `04_BATTLE_SYSTEM.md` — Tier 1, will need updates to document new subsystems

---

## 16. Final note

Hand-to-hand combat is the heartbeat of the game. The cycle of Approach → Engage → Exchange → Separate → Re-engage is what makes combat *read* as a fight rather than as two units stuck together trading hits. Get this rhythm right and combat starts to feel like anime even before the polish layer is complete.

Implementation is staged across six phases, each its own focused session. Don't compress them. Build the foundation first (Phase H-1 above), verify it works, then move to the next phase. The design supports this incremental build because each phase produces a working, testable improvement.

When this system is functioning end-to-end, you'll watch a fight in your training scene and see something genuinely different from where the project is today. That's the goal.
