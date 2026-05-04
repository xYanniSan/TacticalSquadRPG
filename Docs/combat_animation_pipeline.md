# Combat Animation Pipeline Specification

## Purpose

This document defines the recommended animation pipeline for the project’s real-time, behavior-driven, auto-resolving combat system in Unity.

The goal is to ensure that:

- combat remains readable from the gameplay camera
- attacks do not jitter, slide, or warp unnaturally
- movement logic and animation logic do not fight each other
- special attacks with travel, such as flying kicks or leap strikes, behave correctly
- the animation system scales to many skills, effects, and hero archetypes
- the architecture remains modular and data-driven

This spec is intended for implementation by the local coding agent.

---

## Design goals

The animation pipeline must support the following project goals:

- real-time combat
- auto-battler style execution
- preconfigured behavior and tactical planning
- multiple heroes and enemies acting automatically
- many skills, VFX, and reactions over time
- readable combat over purely cinematic realism
- modular combat systems that can grow over the project timeline

Animation is a presentation and timing layer inside combat execution. It must not own battle rules.

---

## Core principles

### 1. Animation does not own combat decisions

The Animator must not decide:

- when an ability may start
- whether range is valid
- whether angle is valid
- where the attacker should stand
- when a target should be hit
- whether movement is allowed during the action
- whether the unit should rotate

Those decisions belong to combat systems.

### 2. Combat owns execution, animation owns presentation

The expected flow is:

- AI or behavior chooses an action
- combat validates the action
- engagement logic reserves a usable position
- movement logic places the attacker correctly
- animation plays the correct visual state
- combat applies hit, damage, status effects, and recovery

### 3. Root motion is opt-in, not default

Most actions should not use unrestricted root motion.

Default approach:

- locomotion is code-driven
- basic attacks are in-place or use a very small controlled lunge
- movement-heavy attacks use scripted travel or tightly controlled root motion windows

### 4. Special movement attacks are ability actions

Moves such as:

- flying kick
- leap slash
- dash stab
- charge attack

must not be treated as generic melee clips.

They require explicit setup rules such as:

- valid start distance
- ideal start distance
- reserved attack slot
- travel mode
- impact timing
- post-hit end position

### 5. Readability is more important than raw realism

The game camera is angled and tactical. Combat clarity is more important than full physical realism.

This means:

- limit visual overlap
- use clean attack silhouettes
- keep facing understandable
- avoid chaotic body drift from competing motion sources
- prefer controlled spacing and consistent impact timing

---

## High-level system boundaries

The following systems are recommended.

### Battle or Combat Coordinator

Responsibilities:

- receives chosen actions from AI or behavior
- routes actions into validation and execution
- tracks action state and completion

Must not:

- directly manipulate animator parameters everywhere
- hardcode individual move logic per unit type

### Action Validator

Responsibilities:

- checks whether action can be used
- checks cooldown, cost, target validity, angle, range, and state restrictions
- rejects actions that do not satisfy profile rules

### Engagement Slot System

Responsibilities:

- provides valid attack positions around a target
- reserves a slot before the attacker commits
- prevents overlap and crowding around targets

### Movement Controller

Responsibilities:

- handles normal movement and repositioning
- moves attacker into action start position
- stops movement when commit phase begins

### Ability Executor

Responsibilities:

- runs the action phase flow
- applies movement locks and rotation locks
- starts animation via the animation driver
- processes impact timing and recovery

### Animation Driver

Responsibilities:

- translates gameplay commands into animator calls
- exposes a clean API to the rest of gameplay
- hides animator hashes, triggers, and implementation details

### Hit Resolution

Responsibilities:

- applies damage, effects, crowd control, and knockback
- is triggered from a controlled impact event or time window
- does not rely on raw clip motion for correctness

---

## Combat action phase model

Every melee or skill action should pass through the following phases.

### 1. Intent

A unit’s AI or behavior module selects an action and target.

### 2. Validate

Combat checks:

- target still valid
- action off cooldown
- correct state to use action
- rough range eligibility
- angle restrictions if any

### 3. Reserve Slot

If the action requires melee or target-relative positioning, reserve an engagement slot around the target.

### 4. Move To Start Position

The attacker moves into a usable launch position defined by the action profile.

### 5. Pre-Align

Before animation starts:

- face the target
- optionally snap a short distance into clean alignment
- stabilize position

### 6. Commit

The action becomes committed.

Usually:

- pathfinding disabled or paused
- steering disabled
- target switching disabled
- attack animation started
- movement permissions restricted according to profile

### 7. Impact

At the defined impact timing:

- hit applies
- VFX triggers
- hit reaction may start
- optional knockback may occur

### 8. Recovery

The attacker finishes the move.

During this phase:

- some movement may still be restricted
- follow-through or recoil animation finishes
- state prepares to return to combat idle or next decision

### 9. Exit

After recovery:

- slot released if appropriate
- movement restored
- AI resumes normal decision loop

---

## Animation categories

The content pipeline should group animations into clear categories.

### A. Locomotion

Examples:

- idle
- move forward
- combat idle
- turn in place
- stop

Rules:

- loopable
- clean transitions
- normally code-driven movement
- used outside committed attack states

### B. Basic Combat Actions

Examples:

- slash
- jab
- punch
- short kick
- quick block

Rules:

- usually in-place or minimal lunge
- short recovery
- suitable for repeated live combat usage

### C. Special Skill Actions

Examples:

- flying kick
- leap strike
- spinning slash
- hand-sign cast
- earth wall summon

Rules:

- require an explicit action profile
- may use scripted travel, controlled root motion, or fixed timing windows
- are not interchangeable with generic attacks

### D. Reactions

Examples:

- light hit react
- heavy hit react
- block react
- stagger
- knockback
- death

Rules:

- used to sell impact and consequence
- must not be used to correct bad spacing as a primary fix

---

## Attack profile data model

Every live-combat attack or skill action must have an associated profile asset.

Recommended fields:

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
    public AnimationClip clip;
    public string animatorStateName;

    public float minStartRange;
    public float idealStartRange;
    public float maxStartRange;
    public float desiredImpactDistance;
    public float allowedAngleDegrees;

    public bool requiresPreAlign;
    public bool requiresEngagementSlot;
    public bool canUseIfTooClose;

    public ActionMovementMode movementMode;
    public bool useRootMotion;
    public bool lockMovementDuringCommit;
    public bool lockRotationDuringImpact;

    public float scriptedTravelDistance;
    public float impactTimeNormalized;
    public float recoveryExitTimeNormalized;

    public bool causesKnockback;
    public float knockbackDistance;
}
```

### Notes on important fields

#### `minStartRange`
The action should not begin if the attacker is closer than this, unless the profile explicitly allows it.

#### `idealStartRange`
The desired distance from which the move visually works best.

#### `maxStartRange`
The action should not begin if the attacker is farther than this.

#### `desiredImpactDistance`
The preferred spacing between attacker and target at the moment of impact.

#### `movementMode`
Controls how the action moves through space.

#### `useRootMotion`
Must only be enabled if the move has been explicitly approved to use controlled root motion.

#### `impactTimeNormalized`
Defines the normalized clip time at which the hit should be processed.

---

## Example profiles

### Basic Slash

Recommended behavior:

- min range: 0.7
- ideal range: 1.2
- max range: 1.7
- desired impact distance: 1.0
- movement mode: InPlace
- useRootMotion: false
- requiresPreAlign: true
- requiresEngagementSlot: true
- canUseIfTooClose: true

### Flying Kick

Recommended behavior:

- min range: 1.8
- ideal range: 2.4
- max range: 3.5
- desired impact distance: 0.9
- movement mode: Leap or ScriptedTravel
- useRootMotion: false by default
- requiresPreAlign: true
- requiresEngagementSlot: true
- canUseIfTooClose: false
- causesKnockback: optional

Flying Kick is not a generic melee action. It is a gap-closing burst action.

---

## Engagement slot system

### Purpose

Units should not attack from arbitrary overlapping positions.

The engagement slot system gives targets a controlled set of usable attack positions around them.

### Recommended MVP version

Each target exposes 3 to 5 melee slots:

- front
- front-left
- front-right
- optional left or right

### Rules

- attackers must reserve a free slot before using melee actions
- slot remains reserved during action setup and commit
- slot is released when attack exits or target becomes invalid
- slot positions are recalculated relative to target facing and radius

### Benefits

- reduces overlap
- improves readability
- makes facing cleaner
- gives melee animations space to play correctly
- helps solve attack jitter indirectly by enforcing valid spacing

---

## Movement rules

### Standard locomotion

Normal approach, chasing, and repositioning should be code-driven.

Recommended options:

- NavMeshAgent if appropriate for prototype scale
- custom movement controller if combat requirements grow beyond basic agent behavior

### During action setup

The unit may move normally to reach:

- reserved slot position
- ideal start range
- correct facing window

### During action commit

Depending on the profile:

- disable pathfinding updates
- disable movement steering
- disable distance correction
- disable automatic re-chase logic

### During recovery

Movement can either:

- remain locked until exit
- or be restored gradually if the animation supports it

---

## Root motion policy

### Default policy

Root motion should be disabled for most attacks.

### Allowed use cases

Root motion may be allowed for:

- short finishing bursts
- controlled stylish lunges
- hand-authored cinematic-like skill moves with tight execution windows

### Disallowed use cases by default

Do not use full unrestricted root motion for:

- generic live-combat locomotion
- frequent basic attacks across many units
- situations where AI movement and animation movement may both try to control the transform

### Reason

Unrestricted root motion often causes:

- jitter
- drift
- overlap
- warping
- conflicts with navigation or tracking

For this project, code or executor-driven positioning should remain authoritative.

---

## Practical solution for the flying kick problem

### Problem summary

The flying kick animation contains real forward travel. If it is triggered while units are already point-blank, the animation has no valid space to complete and the character jitters or warps.

### Required solution

Flying Kick must be executed as a profile-based ability action.

### Execution flow

1. Select target
2. Validate Flying Kick profile conditions
3. Reserve front-oriented slot on target
4. Move attacker to launch position near ideal start range
5. Pre-align to target
6. Lock steering and commit
7. Start flying kick animation
8. Apply scripted forward travel or limited controlled root motion
9. Process hit at impact frame
10. Trigger target reaction and optional knockback
11. End attacker at controlled post-impact position
12. Exit and resume AI

### Important restrictions

- do not allow use from point-blank range
- do not rely on the defender being pushed backward as the main fix
- do not let navigation continue modifying the unit during commit

---

## Animator controller structure

Avoid a giant controller with many fragile transitions.

### Recommended structure

#### Base layer

Contains:

- Idle
- Move
- CombatIdle
- HitReact
- Death

#### Action states

Can be in the same controller or a dedicated action layer, depending on implementation style.

Examples:

- BasicAttack_A
- BasicAttack_B
- Skill_FlyingKick
- Skill_FireBreath
- Skill_StoneWall
- Cast_HandSigns
- Block
- Dodge

### Recommendation

Prefer code-driven action entry.

For example:

- gameplay selects action profile
- animation driver enters the named or hashed state
- action timing is monitored by executor

Do not rely on many loosely managed booleans across the project.

---

## Animation driver API

Gameplay should not manipulate the Animator directly from multiple unrelated systems.

Recommended wrapper:

```csharp
public class UnitAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void SetMoveSpeed(float normalizedSpeed) {}
    public void SetCombatMode(bool active) {}
    public void PlayAction(string stateName) {}
    public void PlayHitReaction(int variation) {}
    public void PlayDeath() {}
    public void FaceDirection(Vector3 direction) {}
}
```

### Responsibilities

- centralizes animator access
- hides hashes and triggers from gameplay systems
- makes debugging easier
- reduces animator spaghetti

---

## Impact timing and animation events

### Policy

Animation events may be used as signals, but not as owners of combat rules.

### Good event uses

- notify impact frame
- notify footstep SFX
- notify VFX timing
- enable weapon trail
- disable weapon trail

### Bad event uses

- choose a target
- decide if attack is valid
- change AI logic in hidden clip events
- author critical gameplay solely inside the timeline

### Recommended pattern

Animation event:

```csharp
public void OnImpactEvent()
{
    abilityExecutor.NotifyImpactFrame();
}
```

The executor then decides whether the hit should be applied.

---

## System locks during action commit

Multiple systems must not fight over the character during a committed action.

### Recommended temporary locks

When an action enters commit:

- pause pathfinding updates
- disable steering
- disable target reselection
- disable free auto-rotation if not permitted by profile
- disable range correction movement

### On recovery or exit

- restore movement authority
- restore AI target selection
- release slot as appropriate

---

## Content import and clip review rules

Every imported animation clip should be reviewed before being approved for live combat.

### Review checklist

For each clip, mark:

- category: locomotion, basic attack, special skill, reaction, death
- in-place or travel-heavy
- likely impact frame
- visually readable from gameplay camera
- usable in repeated live combat
- needs start trim or end trim
- requires target alignment
- safe for root motion or not

### Approval categories

- Safe Generic Combat Clip
- Special Movement Ability Clip
- Reaction Clip
- Cinematic Only Clip
- Reject For Live Combat

Not every good-looking animation should be allowed into the real-time combat loop.

---

## Folder and asset structure

Recommended project structure:

```text
Assets/
  Animation/
    Controllers/
    Clips/
      Locomotion/
      BasicAttacks/
      Skills/
      Reactions/
      Death/
    Profiles/
      AttackProfiles/
    Events/

  Combat/
    Runtime/
      Animation/
      AbilityExecution/
      Engagement/
      Targeting/
      Validation/
      Movement/

  Units/
    Heroes/
    Enemies/
```

---

## Recommended runtime flow

```text
Behavior / AI
  -> chooses action and target
Action Validator
  -> validates range, angle, cooldown, state
Engagement Slot System
  -> reserves slot if needed
Movement Controller
  -> moves attacker into valid start position
Ability Executor
  -> applies locks
  -> starts animation via animation driver
Animation / Impact Signal
  -> notifies impact frame
Hit Resolution
  -> applies gameplay consequences
Recovery / Exit
  -> restores movement and AI
```

---

## MVP implementation order

### Phase 1

Implement:

- locomotion
- combat idle
- one basic melee attack
- one hit reaction
- one death state
- simple engagement slot system
- attack profile asset structure
- animation driver wrapper

### Phase 2

Implement:

- short lunge attack
- one scripted movement attack such as Flying Kick
- one cast animation with projectile skill
- one knockback reaction
- commit phase lock logic

### Phase 3

Implement:

- multiple ability categories
- combo or chained actions
- skill-sign or hand-sign style casting flow
- layered reactions
- action variation selection
- richer VFX and SFX timing hooks

---

## Coding guidelines for the local agent

### Required

- keep data separate from runtime state
- use ScriptableObjects for attack profiles or equivalent data assets
- keep the Animator behind a driver wrapper
- keep combat validation outside animation logic
- keep ability execution modular and profile-based
- treat movement attacks as dedicated action types

### Avoid

- giant monolithic combat controllers
- gameplay logic hidden in many animation clips
- unrestricted root motion by default
- direct Animator parameter manipulation from many systems
- hardcoded special cases per hero whenever possible

---

## Final implementation rules

1. No attack may start from arbitrary spacing
2. Every live-combat action needs a profile
3. Special movement attacks are ability actions, not generic attacks
4. Most attacks should be in-place or short-lunge only
5. Root motion is opt-in only
6. Melee attackers should reserve attack slots
7. Gameplay systems own timing and positioning authority
8. Animation events are signals, not rule owners
9. Readability is preferred over raw realism
10. The animation pipeline must scale to many actions and units without fragile special-case code

