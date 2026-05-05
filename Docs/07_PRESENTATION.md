# 07_PRESENTATION.md

> **Tier 2 — Stable.** Describes the animation pipeline and visual feel goals. Do not modify without explicit user instruction.

---

## Purpose

This is the spec for how animation, visual effects, and combat presentation are structured in the project. It exists to enforce a single rule that protects everything else:

> **Animation reflects combat state. It does not define it.**

This is the most important rule in the entire codebase. Every animation problem the project will face — sliding attacks, hits that don't connect, skills that miss their windup, ragdolls misbehaving — traces back to violations of this rule. Read it carefully.

For combat logic, see `04_BATTLE_SYSTEM.md`. For the architectural rationale, see `02_ARCHITECTURE.md`.

---

## Core principle

### Combat owns truth. Animation owns presentation.

When a hit lands, when a skill is valid, when a unit moves — those are **gameplay decisions made in code.** The animator plays clips that *show* those decisions happening, but the animator never *causes* them.

If the animator decides damage timing, the gameplay can desync from the visuals. If gameplay decides damage timing, the visuals can be tuned freely without breaking combat. The latter is what this project requires.

### What this means concretely

The animator must **not** decide:

- When an ability is allowed to start
- Whether range or angle is valid
- Where the attacker should stand
- When the target gets hit
- Whether movement is locked
- Whether the unit should rotate
- Whether a hit lands

All of these are owned by combat systems. The animator is told what to play and when to apply visual effects. It does not author rules.

### Animation Events: notifications, never authority

`UnitAnimationEventRelay` forwards Animation Events from the animator child to `TerrainBattleUnit`. These events are **notifications** — "the animation reached frame X" — not commands.

The combat system uses these notifications to time visual coupling (impact VFX, hit-stop, etc.). It does not depend on them for correctness. If the animation is replaced or its timing changes, combat still works; the visuals just look different.

### Root motion: opt-in, not default

Root motion (animation-driven movement) is **off by default.** Locomotion is code-driven through `CharacterController`. This is non-negotiable for general movement.

Specific actions can opt into controlled root-motion windows when justified — typically for special-movement attacks like a flying kick or a leap strike — but those windows must be explicit, time-bounded, and documented in the action's profile (see Attack Profile section below).

---

## The combat action phase model

Every melee or skill action passes through these phases. Each phase is owned by a different layer.

| Phase | Owner | What happens |
|---|---|---|
| **Intent** | AI / behavior | Unit's `Decide` state picks an action and target |
| **Validate** | Combat | Range, cooldown, energy, target alive |
| **Reserve slot** | `BattleEngagementManager` | Claim a position around the target |
| **Move to start** | Movement code | Unit walks/runs to attack range |
| **Pre-align** | Combat | Face target, optional snap, stabilize |
| **Commit** | Combat | Animation triggered. Movement locked. Target switching disabled. |
| **Impact** | Combat (timed by animation event) | Damage applies, VFX triggers, hit reaction starts |
| **Recovery** | Combat | Recoil/follow-through animation finishes |
| **Exit** | Combat | Slot released, movement restored, AI resumes |

Animation enters the picture at **Commit** (start the clip), provides a notification at **Impact** (the event that triggers damage), and finishes during **Recovery**. It never owns a phase boundary.

---

## Animation categories

Animations are grouped by usage. Each category has different rules.

### Locomotion

Examples: idle, run, combat idle, turn in place.

- Loopable
- Code-driven movement (no root motion)
- Used outside committed attack states
- Transitions handled by code via `Animator.CrossFade`

### Basic combat actions

Examples: punch, kick, jab, short slash.

- Usually in-place or with a small lunge
- Short recovery
- Suitable for repeated combat usage
- No root motion

### Special skill actions

Examples: flying kick, leap strike, hand-sign cast, summoning ritual.

- Require an explicit attack profile (see below)
- May use scripted travel or a controlled root-motion window
- Have well-defined impact timing
- Are not interchangeable with generic attacks

### Reactions

Examples: hit react (left/right/heavy), block react, stagger, knockback, death.

- Used to sell impact and consequence
- **Must not be used to correct bad spacing as a primary fix** — fix the spacing problem in combat code, then the reaction looks good

---

## Attack profiles (target architecture)

Every special skill action should have an associated profile asset (`ScriptableObject`) defining its movement, timing, and animation requirements. This is a **target design** — the current implementation has elements of this baked into combat code; new skills should move toward profile-driven definitions.

### Recommended structure

```csharp
public enum ActionMovementMode
{
    InPlace,
    ShortLunge,
    ScriptedTravel,
    Dash,
    Leap
}

[CreateAssetMenu(menuName = "Combat/Attack Profile")]
public class AttackProfile : ScriptableObject
{
    public string actionId;
    public TransitionAsset transition;
    public StringAsset impactEventName;

    // Range and positioning
    public float minStartRange;
    public float idealStartRange;
    public float maxStartRange;
    public float desiredImpactDistance;
    public float allowedAngleDegrees;

    // Setup behavior
    public bool requiresPreAlign;
    public bool requiresEngagementSlot;
    public bool canUseIfTooClose;

    // Movement during the action
    public ActionMovementMode movementMode;
    public bool useRootMotion;
    public bool lockMovementDuringCommit;
    public bool lockRotationDuringImpact;
    public float scriptedTravelDistance;

    // Timing (normalized 0-1 within the clip)
    public float impactTimeNormalized;
    public float recoveryExitTimeNormalized;

    // Outcome
    public bool causesKnockback;
    public float knockbackDistance;
}
```

### Why profiles matter

Without profiles, every special skill needs hardcoded ranges, hardcoded movement decisions, and hardcoded impact timing scattered across the codebase. With profiles, the rules are data and consistent. New skills are a profile asset plus a resolver branch, not a code archeology expedition.

The flying kick is the canonical example: it's a `Leap` movement-mode action with `requiresEngagementSlot = true`, a specific `idealStartRange`, and a normalized impact time roughly partway through its clip. It is *not* a generic melee attack with a longer reach.

---

## Animation runtime (Animancer Pro)

Animancer Pro is the runtime layer that plays clips, blends them, and dispatches timing events. The rules below describe how Animancer fits **under** the principle that animation reflects combat state and never defines it. Nothing in this section changes the phase model above — it only changes *how* the Commit/Impact/Recovery transitions are expressed in code.

### Default pattern: `TransitionAsset` per skill, `ClipTransition` for one-offs

Animancer offers four playback paths: direct `Play(AnimationClip)`, `ClipTransition` (serialized field), `TransitionAsset` (ScriptableObject), and `StringAsset` (named lookup). The project default is:

- **`TransitionAsset` (ScriptableObject)** for every skill clip referenced by an `AttackProfile`. Skills are shared across units and across loadouts; the asset carries fade duration, start time, end time, speed, and named events as data. This matches the project's data-driven philosophy and lets one asset back many heroes' loadouts.
- **`ClipTransition` (serialized field)** for unit-specific or scene-bound clips that don't warrant their own asset (locomotion idle/run, dodge arc, hit reactions, death). These live as fields on `TerrainBattleUnit` or its presentation children.
- **`StringAsset`** is reserved for lookup-by-name (variant idle pools, "play any heavy hit react"). Don't introduce it ahead of demand.
- **Direct `Play(AnimationClip)`** is not used in combat. It skips fade and event configuration without buying anything back.

`TransitionAsset` is shared, so **per-instance events must be attached to the returned `AnimancerState`, not to the transition asset itself** — otherwise two units playing the same skill would stomp each other's callbacks:

```csharp
var state = animancer.Play(profile.transition);
state.Events(this).SetCallback(ImpactEventName, OnImpact);
state.Events(this).OnEnd ??= OnRecoverComplete;
```

### `AttackProfile` integration

`AttackProfile` remains the source of truth for ranges, movement mode, root-motion policy, and outcome. The animation reference on the profile changes from a raw `AnimationClip` + state name to a `TransitionAsset`:

```csharp
public TransitionAsset transition;       // replaces AnimationClip clip + animatorStateName
public StringAsset impactEventName;       // typically "Impact"; matches a [EventNames] entry on the transition
public float impactTimeNormalized;        // fallback used only if the transition has no named Impact event
public float recoveryExitTimeNormalized;  // mapped to the transition's End Time
```

`TerrainBattleUnit.Execute` plays `profile.transition` and registers an `Impact` callback on the returned state. The callback calls `BattleCombatResolver.ResolveSkillAttack(...)` with the appropriate hit-stop tier and the profile's knockback parameters. The state's `OnEnd` returns the unit to `Recover`. **Combat still owns damage, hit-stop tier, and knockback — the transition only tells us *when*, not *what*.**

### Animation events: Mixamo today, Animancer events going forward

Unity's built-in Animation Events on imported Mixamo clips continue to fire when played through Animancer, so `UnitAnimationEventRelay` keeps forwarding existing clips as-is — no migration is forced on already-authored events. **New** authoring goes through Animancer's named events:

- Mark frames on the `TransitionAsset` Inspector with `[EventNames]` strings (`Impact`, `OrbConsume`, `FollowThrough`, etc.).
- Combat subscribes by name via `state.Events(this).SetCallback(name, callback)` — never by inspecting clip frames.
- `UnitAnimationEventRelay` is **extended, not replaced**: both legacy Mixamo `AnimationEvent` callbacks and Animancer named events route into the same relay method on `TerrainBattleUnit`. The state machine does not know or care which system fired the event.

This preserves the rule: events *notify*, combat decides. If a clip is swapped and the event name stays the same, combat is unaffected.

### Proof-of-concept migration: Earth Fist

The first skill to move onto Animancer is **Earth Fist** (`Hand Sign A → Punch`, 1.2× Melee). Chosen because:

1. It uses an existing Mixamo punch clip with one impact frame — minimum animation surface.
2. `CastType.Melee` runs the unit through the standard `Engage → Decide → Melee → Execute → Recover` path with no special-cased state code.
3. It exercises the full pipeline (combo match → resolved technique → profile → transition → impact event → resolver → hit-stop → knockback) without pulling in summons, orbs, or rooted charge windows.

Steps:

1. Create `AttackProfile_EarthFist.asset` and `Transition_EarthPunch.asset`. Author the impact frame as a named event `Impact` on the transition.
2. Add a `BattleAnimationDriver` presentation MonoBehaviour as a child of `TerrainBattleUnit` that owns the `AnimancerComponent` reference and exposes `PlayAttack(AttackProfile, Action onImpact, Action onEnd)`. Route the `Execute` state through it. Combat decisions stay in `TerrainBattleUnit` and the resolver — `BattleAnimationDriver` is presentation-only.
3. Leave every other skill on the existing Animator Controller path. `BattleAnimationDriver` falls back to legacy playback when `profile.transition == null`, so the migration is per-skill rather than big-bang.
4. Verify against `CombatLogger`'s `ANIM` and `DMG` categories: the impact event should land at the same logical moment as before, hit-stop and knockback should fire on the resolver call, and `Recover` should re-enter `Decide` at the existing cadence.
5. Once Earth Fist is stable across player and enemy units in `TerrainBattleScene`, schedule the rest of the catalog one cast-type cluster at a time — Melee combos next, then Mobile, then Rooted, with Summoning and Orb Strike last because they touch the presentation surface most.

If a step requires combat to compensate for animation (re-timing damage to match a clip, faking an impact frame in code), **stop** — that's the inverted relationship described above and the migration plan needs to change, not the combat code.

---

## Engagement slot system

This is referenced in `04_BATTLE_SYSTEM.md` and partially implemented as `BattleEngagementManager` and `BattleMeleeTokenSystem`. The presentation-side rationale:

Without slot management, multiple attackers stack on the same melee point, animations clip into each other, facing becomes unreadable, and the combat looks chaotic. With slots:

- Attackers reserve a position around the target
- Spacing is enforced
- Facing is clean
- Animations have room to play
- Combat reads correctly from the camera

The current implementation caps frontline at 3 per side and per-target press at 3. When extending to per-action positioning (a flying kick wants distance, a counter wants in-close), build that into the attack profile and let `BattleEngagementManager` query it.

---

## Movement rules during combat

### During action setup

Move normally to reach the reserved slot position, ideal start range, and correct facing. Pathfinding/steering active.

### During commit (the action is firing)

Depending on the profile:

- Pathfinding disabled
- Steering disabled
- Distance correction disabled
- Re-chase logic disabled
- Target switching disabled

Movement is either locked or follows a scripted curve (for `ScriptedTravel`/`Dash`/`Leap` modes). The unit cannot abandon the action mid-commit unless it dies.

### During recovery

Movement may remain locked until exit, or restored gradually. Profile-driven.

---

## Hit reactions and impact feel

The current implementation provides several layers of impact feel through dedicated subsystems:

| System | Effect |
|---|---|
| `BattleHitStopSystem` | Brief frame freeze on hit (Light/Medium/Heavy tiers). Implemented via `animator.speed = 0` for ~2/4/8 frames. |
| `BattleKnockbackSystem` | Directional knockback over a short curve (0.3–0.5s). Suppressed when target blocks. |
| Camera shake | Cinemachine impulse on heavy hits and impacts (when integrated). |

These together produce the "anime hit-feel" that's central to the game's visual goal. They follow the rule strictly: each subsystem owns one concern, none of them are inside `TerrainBattleUnit`.

### Tier mapping (current)

| Hit type | Hit-stop | Knockback | Camera shake |
|---|---|---|---|
| Basic attack | Light (~0.033s) | None or small | Subtle |
| Skill attack | Medium (~0.066s) | Medium | Medium |
| Heavy / ultimate | Heavy (~0.133s) | Large | Full |

When adding new attack types, slot them into the tier system rather than adding a new ad-hoc impact path.

---

## Visual goals (anime feel)

The combat aims for an anime/fighting-game flavor inspired in feel by Naruto Storm, Dragon Ball FighterZ, Demon Slayer, Jujutsu Kaisen. **Inspiration only — original IP, no copying.**

### Currently load-bearing (already implemented or actively in use)

- Hit stop on every landed hit
- Directional knockback that physically displaces targets
- Hit reactions (contextual to attack direction and severity)
- Cinematic camera angles via Cinemachine
- Impact VFX hooked through animation events
- Per-element visual differentiation (Earth, Lightning, Water, Fire signs)

### Aspirational (roadmap, see `08_ROADMAP.md`)

- Airborne states with aerial follow-up potential
- Terrain deformation and elemental ground effects
- Destructible props and environment objects
- Terrain-collision knockback (impact when knocked into a wall)
- Cinematic ultimate cutscenes (camera locks, slow-mo)
- Per-element trail effects on weapons / fists
- Audio layering tied to combat phases

These are tracked in `08_ROADMAP.md`. When implementing any of them, the rule still holds: **animation reflects combat state, never defines it.**

---

## What good looks like

A new skill (e.g., a Wind-element AOE called "Whirlwind Cleave") should:

1. Have an `AttackProfile` asset describing its ranges, movement mode, impact time, and root-motion policy
2. Use a Mixamo or custom clip referenced from the profile
3. Trigger an Animation Event at the impact frame, which `UnitAnimationEventRelay` forwards to combat
4. Have its damage applied by `BattleCombatResolver` — *not* by the animation event
5. Have its hit-stop tier set on the resolver call
6. Have its knockback configured in the profile and applied by `BattleKnockbackSystem`
7. Use the engagement slot system for positioning

What that looks like in practice: combat code runs the action; the animator plays the clip; the animation event tells combat "now is the visual moment of impact"; combat applies damage at that moment, fires hit-stop, fires knockback. **All four of those are independent**. Replacing the clip changes the look but not the rules.

---

## Common animation problems and what they mean

| Symptom | Real cause |
|---|---|
| Attacker slides during attack | Root motion is enabled where it shouldn't be, or movement isn't locked during commit |
| Hit doesn't connect visually | Spacing is wrong (engagement slot system isn't reserving correctly) or impact time is wrong in the profile |
| Two attackers in the same spot | Engagement slot or melee token system isn't enforcing limits |
| Attack jitters near target | Pathfinding/steering is still active during commit |
| Damage applies too early/late | Animation event is at the wrong clip frame, OR combat is applying damage at the wrong phase |
| Hit reaction looks fine but attack still missed | Reaction is fixing visual coverage for a real spacing bug — fix the spacing |

In every case, the fix is to **strengthen the rule**, not to patch the animation. If you find yourself adjusting clip timing to make combat math work, you've inverted the relationship.

---

## Adding new presentation features

When adding a new visual layer (e.g., screen-edge red flash on low HP, slow-motion on the killing blow):

1. Identify which combat event drives it (HP threshold crossed, unit died, combo finisher fired)
2. Subscribe to that event from a presentation-only component
3. Never modify combat logic to "make it easier" for the visual layer

Presentation is downstream of combat. Always.
