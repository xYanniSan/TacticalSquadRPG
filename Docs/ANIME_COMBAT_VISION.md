# Anime Combat Vision — Feature Roadmap

A complete feature list for achieving fluid, cinematic anime-style battles.
Inspired by: Naruto Storm, Dragon Ball FighterZ, Demon Slayer, Jujutsu Kaisen, Black Clover.

Each feature is tagged with:
- **Complexity**: Low / Medium / High / Very High
- **Impact**: how much it contributes to the "anime feel" visually
- **Unity Tools**: what Unity systems are involved

---

## Tier 1 — Foundation (Make It Feel Alive)
*These are the baseline. Without these, everything else looks wrong.*

---

### 1.1 Contextual Hit Reactions
**Complexity:** Medium | **Impact:** ★★★★★

Characters react differently based on what hit them — not a single generic flinch.

```
Punch left  → head snaps right, stagger right
Punch right → head snaps left, stagger left
Kick low    → body bends forward, stumble
Kick high   → full knockback, airborne
Heavy hit   → launched backward, ragdoll
Elemental   → full body blast, blown back with particle burst
```

**Unity Tools:** Animator layers, Animator.CrossFade, root motion on reaction clips
**Mixamo clips needed:** Hit reaction left, hit reaction right, stagger backward, knockback, stun

---

### 1.2 Hit Stop (Frame Freeze)
**Complexity:** Low | **Impact:** ★★★★★

Freeze both attacker and defender for 2–6 frames on every landed hit.
Cheap to implement, massive feel improvement — every fighting game uses this.

```
Light hit   → 2 frames (~0.033s)
Heavy hit   → 4 frames (~0.066s)
Ultimate    → 8 frames (~0.133s)
```

**Unity Tools:** `Time.timeScale` briefly, or per-animator speed via `animator.speed = 0`

---

### 1.3 Camera Shake
**Complexity:** Low | **Impact:** ★★★★☆

Shake the camera on heavy hits, landings, explosions.

```
Light hit     → subtle shake (0.1 intensity, 0.15s)
Heavy hit     → medium shake (0.3 intensity, 0.25s)
Ultimate/AOE  → full shake (0.6 intensity, 0.5s)
Terrain break → directional shove toward impact
```

**Unity Tools:** Cinemachine Impulse (built-in package) — single line call per hit

---

### 1.4 Directional Knockback
**Complexity:** Medium | **Impact:** ★★★★★

Hits physically push characters in the direction of the attack.
Currently units take damage but don't move from it.

```
Punch    → small push (1–2m)
Kick     → medium push (3–4m)
Heavy    → large push (5–8m), may knock into terrain
Ultimate → massive launch (10m+), airborne arc
```

**Unity Tools:** CharacterController.Move() with velocity curve over 0.3–0.5s
**Design note:** Knocked-back unit enters a new state: KNOCKED (can't act, sliding)

---

### 1.5 Airborne State
**Complexity:** Medium | **Impact:** ★★★★☆

High-power attacks launch enemies into the air. Aerial follow-ups possible.

```
Launch hit → unit goes airborne (physics arc, 1–3s)
Mid-air    → unit plays tumble/spin animation
Land       → hard landing animation + dust puff + brief stun
Aerial     → attacker can chase (air combo potential, future)
```

**Unity Tools:** CharacterController + manual vertical velocity curve, Animator airborne state

---

## Tier 2 — Environment Interaction (Make The World React)
*The battlefield should feel destructible and alive.*

---

### 2.1 Terrain Deformation
**Complexity:** High | **Impact:** ★★★★★

Heavy hits crater the terrain. Elemental attacks leave burn/ice/water marks.

```
Earth attacks  → raise terrain spikes, create craters
Fire attacks   → scorch marks, burning ground (damage over time zone)
Water attacks  → puddles, slippery terrain (SPD debuff zone)
Lightning      → scorch lines, paralysis zone
Heavy landing  → impact crater at landing point
```

**Unity Tools:** Unity Terrain `TerrainData.SetHeights()` for deformation, Decal Projectors (URP) for marks
**Note:** Real terrain deformation is expensive — decals + particle craters fake it convincingly at low cost

---

### 2.2 Destructible Props / Environment Objects
**Complexity:** Medium | **Impact:** ★★★★☆

Rocks, pillars, trees shatter when hit or when characters are thrown into them.

```
Character thrown into rock → rock shatters, character bounces off
AOE explosion → nearby props fly outward (debris)
Heavy ground slam → cracks spread across ground surface
```

**Unity Tools:** Pre-fractured meshes (Blender), Rigidbody debris, particle systems
**Tip:** Pre-fracture rocks in Blender, hide the broken version, swap + add physics on impact

---

### 2.3 Terrain Collision for Knockback
**Complexity:** Medium | **Impact:** ★★★★★

When a unit is knocked back and hits terrain/a wall, they smash into it.

```
Knockback trajectory → raycast checks for obstacles
Hit wall/rock        → impact particle burst + camera shake
                     → unit "sticks" briefly (stagger), slides down
                     → wall shows crack decal
No obstacle          → unit slides to stop on ground
```

**Unity Tools:** Raycast along knockback path, OnControllerColliderHit callback

---

### 2.4 Ground Crack / Shockwave on Landing
**Complexity:** Low-Medium | **Impact:** ★★★★☆

When a unit lands from airborne state or performs a ground slam:

```
Landing impact → ring shockwave expands from feet outward
               → crack decals projected on terrain
               → nearby units stagger if within radius
               → dust + debris particles
```

**Unity Tools:** Animated ring mesh scale (cheap), Decal Projectors, OverlapSphere for stagger

---

## Tier 3 — Character Physics (Make Bodies Feel Real)
*Characters should feel like they have weight and momentum.*

---

### 3.1 Ragdoll on Death / Heavy Knockdown
**Complexity:** Medium | **Impact:** ★★★★★

When a unit dies or is hit by an ultimate, they ragdoll.

```
Death          → switch from Animator to Ragdoll physics
               → apply velocity in hit direction for tumble
Heavy knockdown → partial ragdoll (upper body loose, legs grounded)
```

**Unity Tools:** Rigidbody per bone (set up in Prefab), toggle `isKinematic` on/off
**Workflow:** Unity's built-in ragdoll wizard (Component > Physics > Ragdoll)

---

### 3.2 Secondary Motion (Cloth / Hair / Cape)
**Complexity:** Medium | **Impact:** ★★★★☆

Hair, capes, scarves, and loose clothing react to movement physically.

```
Running   → cape/hair trails behind
Stop      → hair/cloth continues briefly then settles
Hit       → cloth flies in hit direction
Casting   → cloth billows with energy buildup
```

**Unity Tools:** Unity Cloth component on mesh, or Bone-based spring simulation (lighter)
**Tip:** Bone spring simulation (no Cloth component) is far cheaper and easier to tune

---

### 3.3 Foot IK / Ground Conforming
**Complexity:** Medium | **Impact:** ★★★☆☆

Feet plant correctly on uneven terrain — no floating feet on slopes.

```
Slope → feet rotate to match terrain angle
Step  → foot lifts over obstacles
```

**Unity Tools:** Animation Rigging package — `TwoBoneIK` + terrain raycast per foot

---

### 3.4 Look-At / Head Tracking
**Complexity:** Low | **Impact:** ★★★☆☆

Characters look at their target during combat — heads turn to track enemies.

```
Combat     → head tracks nearest enemy
Casting    → head looks at hands (focus)
Hit        → head snaps in hit direction
```

**Unity Tools:** Animation Rigging `MultiAimConstraint` on head/spine bone

---

## Tier 4 — Visual Effects (Make It Look Anime)
*This is what makes it LOOK like an anime fight vs a game.*

---

### 4.1 Speed Lines
**Complexity:** Low | **Impact:** ★★★★★

Radial speed lines during dashes, dodges, and high-speed movement.
Signature anime effect — cheap to implement.

```
Dash/dodge  → radial speed lines for 0.2–0.3s
Fast charge → continuous speed lines
Ultimate    → dramatic zoom + speed lines freeze frame
```

**Unity Tools:** Full-screen shader (Shader Graph) or animated UI RawImage with speed line texture

---

### 4.2 Elemental VFX per Technique
**Complexity:** Medium-High | **Impact:** ★★★★★

Every technique has its own particle effect tied to its element.

```
Lightning → electric arcs, yellow glow on hands, lightning bolt trail
Fire      → flame burst, smoke, heat distortion
Water     → water splash, ripple rings, mist
Earth     → rock shards fly up, dust cloud, ground crack
None      → white impact flash, wind burst
```

**Unity Tools:** VFX Graph (Unity's GPU particle system) or Particle System per element

---

### 4.3 Aura / Energy Buildup
**Complexity:** Medium | **Impact:** ★★★★★

During casting, a visible energy aura surrounds the caster.

```
Cast start  → aura appears (color matches element)
Cast build  → aura grows, particles orbit body
Execute     → aura explodes outward as technique fires
Backline    → subtle idle aura glow (they're regenerating energy)
```

**Unity Tools:** Particle System (sphere emit), URP Bloom post-process, shader rim light

---

### 4.4 Impact Flash
**Complexity:** Low | **Impact:** ★★★★☆

On every hit, a brief white/colored flash at the point of impact.

```
Physical hit  → white flash
Fire hit      → orange flash
Lightning hit → yellow flash + arc sparks
Heavy hit     → full screen white flash (0.05s)
```

**Unity Tools:** Particle System sprite burst at impact point, full-screen flash via UI Image alpha

---

### 4.5 Afterimage / Ghost Trail
**Complexity:** Medium | **Impact:** ★★★★★

Fast-moving characters leave ghost copies behind — classic Naruto/Dragon Ball effect.

```
High SPD unit dashing → 3–5 ghost copies fade out behind
Dodge                 → substitution ghost stays at origin point (Naruto style)
Ultimate dash         → full afterimage trail
```

**Unity Tools:** Spawn ghost mesh copies every N frames, fade alpha over 0.3s, use same material with transparency

---

### 4.6 Technique Name Display
**Complexity:** Low | **Impact:** ★★★★☆

When a named combo fires, display the technique name dramatically on screen.

```
"THUNDERSTORM" appears in stylized text
Brief hold (0.5s) then fades out
Font: bold, slight tilt, element color
Optional: brief screen darkening behind text (like Naruto Storm)
```

**Unity Tools:** Unity UI TextMeshPro, DOTween or Animation for entrance/exit

---

### 4.7 Elemental Ground Effects
**Complexity:** Medium | **Impact:** ★★★☆☆

Techniques leave persistent ground effects.

```
Fire     → burning ground (damages units standing on it, 3s)
Water    → wet ground (SPD debuff, visual puddle)
Lightning → charged ground (chance to stun, sparks)
Earth    → rough terrain (slows movement, rubble on ground)
```

**Unity Tools:** Trigger collider zone + timer, Decal Projector for visuals

---

## Tier 5 — Camera System (Direct The Action)
*A static camera kills the anime feel. The camera should be a storyteller.*

---

### 5.1 Dynamic Combat Camera
**Complexity:** Medium | **Impact:** ★★★★★

Camera automatically frames the best action shot.

```
Normal       → orbits around fight center, moderate zoom
2 units close → pushes in, tighter framing
AOE/summon   → pulls back to show full area
Unit death   → brief zoom on killer + death animation
```

**Unity Tools:** Cinemachine with multiple virtual cameras, priority switching

---

### 5.2 Dramatic Zoom on Ultimate
**Complexity:** Low | **Impact:** ★★★★★

When a high-cost technique fires, the camera zooms in on the caster briefly.

```
Ultimate fires → zoom to caster face/hands (0.3s)
               → brief freeze (hit stop)
               → cut to wide shot as technique hits
```

**Unity Tools:** Cinemachine virtual camera blend, triggered from TerrainBattleUnit.Execute state

---

### 5.3 Kill Cam
**Complexity:** Medium | **Impact:** ★★★★☆

When a unit dies from a big hit, brief slow-motion + dramatic camera angle.

```
Kill hit → time scale 0.2x for 0.5s
         → camera swings to side-angle profile shot
         → victim ragdolls in slow motion
         → resume normal speed
```

**Unity Tools:** `Time.timeScale`, Cinemachine dolly camera

---

### 5.4 Tracking Shot on Knockback
**Complexity:** Medium | **Impact:** ★★★☆☆

When a unit is launched, the camera briefly follows the trajectory.

```
Launch → camera tilts/pans to follow the arc
Landing → camera snaps back to fight center
```

**Unity Tools:** Cinemachine Target Group, temporarily add launched unit as high-weight target

---

## Tier 6 — Sound Design (Feel Every Hit)
*Sound sells impact more than visuals. Placeholder until real assets.*

---

### 6.1 Hit Sound Variants
Per hit type: light punch, heavy punch, kick, block, dodge, elemental explosion.

### 6.2 Technique Buildup Sound
Casting sound that builds in intensity matching cast time. Cuts to impact sound on Execute.

### 6.3 Whoosh on Fast Movement
Fast movement (dash, dodge, charge) gets a whoosh/wind audio.

### 6.4 Terrain Break Sound
Crunch/explosion sound when terrain deforms or props shatter.

**Unity Tools:** AudioSource per unit, AudioMixer for layering, spatial 3D audio

---

## Tier 8 — Cinematic Skill System (Paired / Finisher Moves)
*How to implement visually dramatic multi-character moves without a full paired animation engine.*
*This is the "ground slam, aerial grab, wall smash" category — moves that involve two characters acting together.*

---

### 8.1 The Commitment Range Pattern
**Complexity:** Medium | **Impact:** ★★★★★

The core design pattern for ALL cinematic skills in this project.
Simple, robust, looks great, maps cleanly onto the existing state machine.

```
DESIGN RULE:
Every cinematic skill has a "commitment range" (e.g. 2.5m).
The skill can only execute its animation within that range.
If the attacker is outside that range when the skill is selected:
  → Finish current animation
  → Enter DASH_TO_TARGET state (fast, locked, committed)
  → The moment range is met → animation triggers

This creates natural anime choreography:
  "Hero A finishes his combo, sees the opening,
   dashes in hard, GRABS — SLAM — CRATER"
```

**State machine addition:**
```
Existing:  DECIDE → ENGAGE → MELEE
New:       DECIDE → DASH_TO_TARGET (cinematic) → EXECUTE_CINEMATIC

DASH_TO_TARGET:
  - Faster than normal Engage speed (feels like a committed lunge)
  - Locked to one specific skill (cannot change mind mid-dash)
  - Cannot be interrupted by normal hits (committed)
  - Triggers EXECUTE_CINEMATIC the instant range condition is met
  - Afterimage trail during dash (speed visual)
```

**One new state, ~1 day of code.**

---

### 8.2 Cinematic Skill Definition

Each cinematic skill is defined by a small data structure:

```csharp
public class CinematicSkillData
{
    public string skillName;            // "Ground Slam"
    public float commitmentRange;       // Must be within this distance to trigger (e.g. 2.5m)
    public float dashSpeed;             // Speed of approach dash (e.g. 12f, faster than normal)
    public AnimationClip attackerClip;  // What Hero A plays
    public AnimationClip defenderClip;  // What Hero B plays
    public float defenderSnapDistance;  // Max snap distance at animation start (e.g. 0.3m)
    public CameraPreset cameraPreset;   // Which camera cut to use
    public float hitStopFrames;         // Frame freeze on impact (e.g. 8f)
    public float damage;                // Damage multiplier
    public VFXPreset impactVFX;         // Crater, shockwave, etc.
}
```

---

### 8.3 The Snap — How To Hide It

The tiny remaining gap between attacker and defender at animation start
is hidden using three simultaneous tricks:

```
Trick 1 — Camera cut:
  At the moment DASH_TO_TARGET ends and animation starts,
  cut to a dramatic close-up camera angle for 0.2s.
  Player eye is on the new camera, not the snap.

Trick 2 — Flash frame:
  Single white flash frame (0.05s) at the snap moment.
  Completely blinds the player to any position adjustment.

Trick 3 — Keep snap distance small:
  The commitment range (2.5m) + fast dash means by the time
  the animation triggers, the gap is already tiny (< 0.3m).
  At that distance a snap is genuinely invisible.
```

**Result: 99% seamless. Players never notice.**

---

### 8.4 Cinematic Skill Catalogue

Planned cinematic skills using this pattern:

| Skill | Attacker Clip | Defender Clip | Range | Camera |
|-------|--------------|---------------|-------|--------|
| Ground Slam | Overhead smash + land | Grabbed + crater impact | 2.5m | Overhead then wide |
| Wall Smash | Throw toward wall | Hit wall + slide down | 3m | Side profile |
| Aerial Chase | Leap upward + punch | Tumble mid-air | 4m vertical | Pull back wide |
| Neck Grab | Choke lift | Lifted + slam | 1.5m | Close up face |
| Ground Pound | Jump + fist slam | Knocked down + bounce | 2m | Low angle looking up |
| Shoulder Charge | Full body tackle | Full body stumble | 3m | Dynamic follow cam |
| Spinning Kick | 360 kick | Sent flying sideways | 2m | Side angle |

---

### 8.5 Defender Reaction During Dash

While the attacker is in DASH_TO_TARGET, the defender should react:

```
Attacker enters DASH_TO_TARGET toward me
↓
Defender detects incoming dash (within 5m, fast approach)
↓
Defender enters REACT_TO_DASH state:
  - Turns to face attacker
  - Plays a "brace/guard" animation (arms up)
  - Brief window to dodge if SPD check passes (2x normal dodge chance)
  - If dodge fails → animation locks defender in place for the grab

This gives the defender a "oh no" moment before the grab
which reads exactly like anime — the losing character sees it coming
but can't stop it. Dramatically satisfying.
```

---

### 8.6 Energy Cost — Cinematic Skills Are Expensive

```
Cinematic skills should cost significantly more energy than normal skills:

  Ground Slam        → 50 energy
  Aerial Chase       → 40 energy  
  Wall Smash         → 45 energy
  Neck Grab          → 55 energy

Rationale:
  - Rare enough that they feel special when they happen
  - Creates the rhythm: build energy → unleash cinematic → back to basics
  - Defender has time to react/position between cinematic uses
  - Matches anime logic: big moves cost the character something
```

---

### 8.7 Implementation Order for Cinematic Skills

```
Step 1: Add DASH_TO_TARGET state to state machine          (1 day)
        - Fast locked dash toward target
        - Triggers on skill selection when out of range
        - Afterimage trail during dash

Step 2: Add CinematicSkillData ScriptableObject            (1 day)
        - Designer-friendly data definition
        - Wired into ComboLibrary / skill system

Step 3: Implement snap + camera cut + flash frame          (1 day)
        - The three hiding tricks at animation start

Step 4: Wire first cinematic skill end-to-end             (2 days)
        - Ground Slam as the proof of concept
        - Attacker clip + defender clip + crater VFX

Step 5: Add defender reaction (REACT_TO_DASH state)        (1 day)
        - Brace animation + dodge window

Step 6: Add remaining cinematic skills from catalogue      (1-2 days each)
```

**Total for full cinematic system: ~2 weeks**
Most of that is animation clip sourcing and VFX, not code.

---

## Implementation Priority Order

Given current state (capsule units, working combat loop), recommended order:

```
Phase A — Immediate Feel (no art required, pure code/shaders):
  ✦ Hit stop (frame freeze)           — 1 day, massive impact
  ✦ Directional knockback             — 2 days, changes combat feel entirely
  ✦ Camera shake (Cinemachine)        — 1 day
  ✦ Technique name display            — 1 day
  ✦ Impact flash particle             — 1 day

Phase B — Character Reaction (needs Mixamo clips):
  ✦ Contextual hit reactions          — 3 days (clips + wiring)
  ✦ Airborne state + launch           — 3 days
  ✦ Ragdoll on death                  — 2 days
  ✦ Afterimage ghost trail            — 2 days

Phase C — Environment (needs art/design pass):
  ✦ Destructible props                — 3 days (pre-fracture in Blender)
  ✦ Terrain collision for knockback   — 2 days
  ✦ Ground crack decals               — 2 days
  ✦ Elemental ground effects          — 3 days

Phase D — Polish (post-art):
  ✦ Elemental VFX per technique       — 1 week (VFX Graph per element)
  ✦ Aura / energy buildup             — 3 days
  ✦ Speed lines shader                — 2 days
  ✦ Dynamic combat camera             — 3 days
  ✦ Kill cam + dramatic zoom          — 2 days
  ✦ Secondary motion (cloth/hair)     — 3 days
  ✦ Terrain deformation               — 1 week

Phase E — Vertical Combat:
  ✦ Jump state in state machine        — 3 days (unlocks everything else)
  ✦ High ground advantage              — 1 day
  ✦ Dive attack                        — 2 days
  ✦ Ground slam (aerial crater)        — 3 days
  ✦ Aerial chase                       — 3 days
  ✦ Platform destruction               — 1 week
  ✦ Wall run / wall jump               — 1 week
  ✦ Ledge grab / climb                 — 3 days
  ✦ AI vertical decision making        — 1 week
  ✦ Vertical arena design (terrain)    — ongoing

Phase F — Cinematic Skills:
  ✦ DASH_TO_TARGET state               — 1 day
  ✦ CinematicSkillData ScriptableObject — 1 day
  ✦ Snap + camera cut + flash frame    — 1 day
  ✦ Ground Slam (first cinematic)      — 2 days
  ✦ Defender REACT_TO_DASH state       — 1 day
  ✦ Remaining cinematic skills         — 1-2 days each
```

---

## Tier 7 — Vertical Combat (Use The Whole World)
*Anime fights are defined by vertical space. Flat arenas feel like placeholder levels.*
*Think: Naruto vs Pain on cliff faces, Goku vs Vegeta smashing through rock layers, Demon Slayer fights on crumbling structures.*

---

### 7.1 Jump State in Combat State Machine
**Complexity:** Medium | **Impact:** ★★★★★

The most fundamental change — units need a proper jump with arc physics.
Currently the state machine has no jump. This unlocks everything else in this tier.

```
New state: JUMPING
  - Triggered by: leap attack, vertical chase, knockback launch, wall jump
  - Has a peak height and gravity arc (same CharacterController + verticalVelocity)
  - Transitions:
      JUMPING → AIRBORNE (at peak / after launch)
      AIRBORNE → LAND (hits ground)
      AIRBORNE → ENGAGE (chasing enemy mid-air, future)

Jump types:
  Tactical jump   → unit leaps to elevated position to gain high ground
  Chase jump      → unit jumps after a launched enemy (aerial follow-up)
  Dive jump       → unit on high ground leaps DOWN onto enemy below
  Escape jump     → unit breaks melee and leaps back + up to a ledge
```

**Unity Tools:** `CharacterController` + vertical velocity curve, jump height tuned per unit SPD stat
**State machine addition:** `Jumping`, `Airborne`, `Landing` states between Engage and Decide

---

### 7.2 High Ground Advantage
**Complexity:** Low-Medium | **Impact:** ★★★★☆

Units on elevated terrain deal more damage and have extended cast range.
Incentivises actually using the vertical space tactically.

```
Height difference > 3m:
  Attacker above   → +15% damage bonus, +20% cast range
  Attacker below   → -10% damage (fighting uphill is harder)

Height difference > 6m:
  Dive attack bonus → +40% damage on first hit after dropping

Visual cue: subtle golden rim light on unit that holds high ground
```

**Unity Tools:** Compare `transform.position.y` between attacker and defender at damage calculation
**Design note:** Fits directly into `TerrainBattleManager` damage resolution — 3 lines of code

---

### 7.3 Dive Attack
**Complexity:** Medium | **Impact:** ★★★★★

Unit on elevated terrain leaps down onto an enemy below — AOE on landing.
One of the most visually iconic anime moves (Pain's Shinra Tensei, Goku's flying kick).

```
Trigger: unit is in high ground position, enemy is >3m below
AI decides: if high ground + enemy below + energy available → DIVE

Dive sequence:
  1. Unit leaps off ledge toward enemy (arc trajectory, 0.5–1s)
  2. Afterimage trail during descent
  3. Speed lines as they close distance
  4. IMPACT: hit stop + shockwave ring + crater + camera shake
  5. AOE stagger: nearby enemies within 3m also knocked back
  6. Attacker enters RECOVER state (committed, brief window)

Damage: base ATK × 2.0 + height bonus (more height = more damage)
```

**Unity Tools:** Target position arc calculation (`ProjectileMotion` formula), VFX on landing
**Mixamo clip:** Falling attack / drop kick

---

### 7.4 Aerial Chase (Follow-Up on Launched Enemy)
**Complexity:** High | **Impact:** ★★★★★

When a unit launches an enemy airborne, they can chase them into the air for a follow-up hit.
The signature DB FighterZ / Naruto Storm move that defines aerial combat.

```
Trigger: attacker just launched an enemy (enemy enters AIRBORNE state)
         attacker has energy >= 20

Chase sequence:
  1. Attacker leaps upward toward airborne enemy (instant burst of speed)
  2. Both units are mid-air for ~0.5–1s
  3. Attacker executes aerial hit (punch/kick, no cast)
  4. Enemy launched again or slammed downward (see 7.5)
  5. Attacker lands, enters RECOVER

Camera: pull back to show both units in the air (Cinemachine wide shot)
Visual: speed lines on chase leap, impact flash mid-air
```

**Unity Tools:** JUMPING state with target tracking, `Vector3.MoveTowards` to close gap mid-air

---

### 7.5 Ground Slam (Aerial → Crater)
**Complexity:** Medium | **Impact:** ★★★★★

Grab an airborne enemy and smash them straight down into the terrain.
Creates a crater, stuns nearby units, looks absolutely devastating.

```
Trigger: attacker is mid-air after aerial chase, executes slam input

Slam sequence:
  1. Attacker grabs enemy mid-air (brief freeze, 0.1s)
  2. Both units accelerate downward together
  3. IMPACT on terrain:
     → Crater deformation at landing point
     → Shockwave ring expands (3–5m radius)
     → Camera shake (heavy)
     → Hit stop (6–8 frames)
     → Nearby units stumble
  4. Slammed unit: massive damage, stunned (0.5s)
  5. Attacker: lands cleanly, brief RECOVER

Damage multiplier: ATK × 2.5 + height fallen bonus
```

**Unity Tools:** Paired animation (two-unit choreography), crater via terrain raycast + decal
**Note:** The "grab" moment is a great place for a dramatic zoom camera cut

---

### 7.6 Wall Run / Wall Jump
**Complexity:** High | **Impact:** ★★★★☆

Units run up vertical surfaces briefly and launch off them.
Iconic ninja move — Naruto wall-walking is basically the series' signature.

```
Trigger: unit is knocked/running toward a steep wall or cliff face
         unit has SPD >= 3.0 (fast enough to wall run)

Wall run sequence:
  1. Unit hits wall → detects near-vertical surface (raycast angle check)
  2. Rotates to run along wall surface for 0.5–1s
  3. Launches off with a jump (angle = wall normal + upward)
  4. Can execute aerial attack at peak or land on top of cliff

SPD stat bonus: higher SPD = longer wall run duration
```

**Unity Tools:** `OnControllerColliderHit` to detect wall angle, rotate unit to surface normal
**Mixamo clip:** Wall run (Mixamo has this)

---

### 7.7 Ledge Grab / Climb
**Complexity:** Medium | **Impact:** ★★★☆☆

Units knocked near a ledge grab it instead of falling, then pull themselves up.

```
Trigger: unit trajectory passes near ledge edge during knockback/airborne

Sequence:
  1. Detect ledge edge (raycast from unit position outward + downward)
  2. Unit "grabs" ledge — snaps to hang position
  3. Brief pause (0.3s) — vulnerable, can be hit
  4. Pull up animation → lands on top of ledge
  5. Resumes combat from high ground position

Purpose: prevents units from just falling off the map
         creates dramatic "hanging by a thread" moment
```

**Unity Tools:** Edge detection via paired raycasts, IK hand placement on ledge surface

---

### 7.8 Platform Destruction
**Complexity:** High | **Impact:** ★★★★★

The rock, pillar, or platform a unit is standing on gets destroyed — dropping them.

```
Trigger: heavy hit lands on or near a destructible platform
         OR unit standing on platform takes >30% max HP in one hit

Destruction sequence:
  1. Platform fractures (pre-fractured mesh swaps in)
  2. Rigidbody debris flies outward
  3. Unit on platform enters AIRBORNE (falling)
  4. Dust cloud + crack sounds
  5. Debris settles as permanent terrain change

Examples:
  Rock pillar → shatters into boulders
  Cliff ledge → breaks off, falls as one piece
  Tree/log    → splits, falls sideways (knockback for nearby units)
```

**Unity Tools:** Pre-fractured meshes (Blender Fracture modifier), Rigidbody on fragments, OverlapSphere for units on top

---

### 7.9 Vertical Terrain Design — Reference Layout

How the battle arena should be structured to support vertical combat:

```
                         [Peak Spire]          ← Highest point, 1 unit fits
                              |
                    [Upper Cliff L]  [Upper Cliff R]   ← 8-10m height
                         \              /
                    [Mid Ledge L]  [Mid Ledge R]        ← 4-5m height
                              \    /
                    ════════ [ARENA FLOOR] ════════     ← Ground level 0m
                         /              \
                [Sunken Pit L]      [Sunken Pit R]      ← -2m (below grade)

Rules:
  - At least 3 distinct height levels
  - Each level accessible by jump OR by being launched to it
  - Destructible elements on upper levels (pillars, overhangs)
  - Narrow paths at high levels (forces close combat)
  - Open floor (allows formations, backline positioning)
  - No "safe" high ground — all ledges breakable eventually
```

---

### 7.10 AI Vertical Decision Making
**Complexity:** High | **Impact:** ★★★★☆

The AI needs to understand and USE vertical space, not just fight on flat ground.

```
New AI considerations in DECIDE state:

  "Am I on low ground vs an enemy on high ground?"
    → Consider leaping up to match height first
    → Or use ranged skill to close gap

  "Am I on high ground?"
    → Prefer dive attack if enemy is below
    → Prefer CAST_ROOTED (hard to interrupt, safe position)
    → Hold position longer before engaging melee

  "Can I launch this enemy into a wall/off a ledge?"
    → Prefer knockback skills if enemy is near edge
    → Aerial chase if energy allows

  "Is there a platform I can destroy the enemy off of?"
    → Prioritize AoE/heavy skills if enemy is on destructible surface
```

**Design note:** This is essentially adding terrain awareness to the existing
`DECIDE` state — the AI already picks skills by cost/type, it just needs
height/position context added to the evaluation.

---

## Quick Reference — What Makes It "Anime"

| Feature | Games That Do It | Difficulty |
|---------|-----------------|------------|
| Hit stop | Every fighter ever | Low |
| Speed lines | Naruto Storm, DB FighterZ | Low |
| Technique name display | Naruto Storm | Low |
| Directional knockback | DB FighterZ, Naruto Storm | Medium |
| Afterimage trail | DB FighterZ, Naruto | Medium |
| Airborne combos | DB FighterZ | Medium |
| Terrain destruction | Naruto Storm, DB FighterZ | High |
| Aura buildup | DB FighterZ, Black Clover | Medium |
| Ragdoll death | Most modern action games | Medium |
| Kill cam slow-mo | Naruto Storm, Sekiro | Medium |
| Cloth/hair physics | Most modern games | Medium |
| Camera zoom on ultimate | Naruto Storm | Low-Medium |
| Jump state / vertical combat | Naruto Storm, DB FighterZ | Medium |
| High ground advantage | Naruto Storm, Sekiro | Low |
| Dive attack | Naruto Storm, DB FighterZ | Medium |
| Aerial chase | DB FighterZ, Naruto Storm | High |
| Ground slam / crater | DB FighterZ, Naruto Storm | Medium |
| Wall run / wall jump | Naruto Storm, Ninja Gaiden | High |
| Platform destruction | Naruto Storm, DB FighterZ | High |
| Cinematic skills (commitment range) | Naruto Storm, DB FighterZ | Medium |
| Defender react-to-dash | Naruto Storm | Medium |
