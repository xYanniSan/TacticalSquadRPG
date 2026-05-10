# ENGAGEMENT_ANIMATIONS.md

> **Tier 3 — Working inventory.** Lists animations needed for combat engagement (first contact) and re-engagement (returning to combat after separation). For each, identifies whether Kubold provides it directly, whether it can be assembled from existing clips, or whether custom animation is required.

---

## Purpose

The "engagement" moments — when units transition into combat or return to it after a beat — are visually critical and currently weak. Kubold's combat walk-forward is robotic at slow speed and doesn't capture the energy of a fight initiating. This doc catalogs what's needed and where each piece comes from.

The list is split into two main sections:

1. **First engagement** — the unit goes from non-combat state to combat-ready state for the first time
2. **Re-engagement** — the unit was in combat, separated, and is now closing distance again

Plus a third smaller section:

3. **Engagement variants** — different *types* of engagement (cautious, aggressive, ambush, etc.)

---

## Notation

Each entry uses this format:

- **Name:** proposed animation name following naming convention
- **Description:** what the animation looks like
- **Phase:** which combat phase it plays in (Spotting, Approaching, Engaged, Separating, etc.)
- **Source:** [Kubold direct] / [Assembled from Kubold] / [Custom needed] / [Mixamo possible]
- **Notes:** assembly technique, layering needs, gotchas

---

## 1. First engagement (entering combat)

This sequence covers the unit's transition from "not in combat" to "engaged with target." It's a multi-stage sequence with several distinct moments.

### 1.1 Pre-combat idle

- **Name:** `Loco_Idle_Casual`
- **Description:** Relaxed standing pose, weight on one foot, arms at sides, looking around naturally. Reads as "not currently fighting."
- **Phase:** NotEngaged
- **Source:** Kubold direct (Movement Animset Pro has casual idles)
- **Notes:** This is the baseline for all non-combat scenes. Different from combat idle (which has weight forward and arms ready).

### 1.2 Spotting reaction

- **Name:** `React_Spotted_Subtle`
- **Description:** Brief moment of recognition. Head turns toward threat, body slightly tenses, weight shifts to balanced stance. ~0.3s duration.
- **Phase:** Spotting (start of)
- **Source:** Custom needed, OR assembled from Kubold's "look around" + brief torso turn
- **Notes:** This is a small but important moment. Without it, units transition from casual to combat-ready jarringly. A Kubold "head turn" or "alert" pose can substitute. Kevin Iglesias' free human animations may have something usable.

### 1.3 Spotting alert hold

- **Name:** `Loco_Idle_Alert`
- **Description:** Bridge pose between casual and combat-ready. Body squared toward threat, hands raised slightly but not full guard, eyes locked on target. Plays for 0.3-0.7s during Spotting phase.
- **Phase:** Spotting
- **Source:** Assembled from Kubold (combat idle with reduced posture intensity, or hold a frame from a transition)
- **Notes:** Could also use Kubold's pre-combat ready stance if available. The key is it reads as "I see you, I'm getting ready" — not yet full combat mode.

### 1.4 Combat-ready transition

- **Name:** `Stance_EnterCombat`
- **Description:** Full transition from alert idle to combat-ready stance. Hands rise to guard, weight shifts forward, knees bend slightly, body squares to opponent. ~0.4s.
- **Phase:** End of Spotting → start of Approaching
- **Source:** Custom needed, OR assembled from Kubold by chaining alert idle into combat idle with crossfade
- **Notes:** This is one of the missing pieces you mentioned. Kubold's combat idle is the *result* of this transition; the transition itself isn't directly provided. A short custom animation here would have outsized impact on engagement feel.

### 1.5 Initial approach burst

- **Name:** `Loco_Approach_Burst`
- **Description:** First step of running toward target. Weight shifts forward dramatically, body lunges into the run rather than sliding into it. ~0.3s before transitioning to sustained run.
- **Phase:** Approaching (start of)
- **Source:** Assembled from Kubold (start of sprint animation has this energy if you don't blend it from idle)
- **Notes:** Make sure you don't crossfade *too smoothly* from combat idle into sprint — let the first frames of the sprint clip play with their natural launch energy. Crossfade duration ~0.05s rather than 0.15s.

### 1.6 Combat-aware sprint

- **Name:** `Loco_Sprint_Combat`
- **Description:** Running fast toward target but with combat-ready posture — arms in defensive position rather than relaxed swinging, body angled forward, eyes locked on target.
- **Phase:** Approaching (sustained)
- **Source:** Assembled — Kubold sprint on legs + upper-body guard pose layer
- **Notes:** This is where animation layering pays off. Use Kubold's sprint as the base layer (legs, hips, torso). Use a "combat guard" upper-body clip as a masked layer (arms, hands). The result: legs sprint, arms stay in fighting posture. This is what's missing from current locomotion — Kubold's sprint has casual arm swing, which doesn't read as combat.

### 1.7 Approach deceleration

- **Name:** `Loco_Approach_Decelerate`
- **Description:** Sharp slowdown as unit reaches engagement range. Heel plant, weight shifts back to absorb momentum, transitions to combat shuffle. ~0.4s.
- **Phase:** End of Approaching
- **Source:** Kubold direct (Movement Animset Pro has stop animations)
- **Notes:** `Loco_Stop_Sharp` from Movement Animset is the closest match. Combine with a brief "settle into combat stance" follow-through.

### 1.8 First engagement pose

- **Name:** `Stance_FaceOff`
- **Description:** The initial hold once both units are in engagement range. Both squared up, guards raised, brief beat (~0.3s) before either commits to action. Subtle weight shifting, eyes locked.
- **Phase:** Engaged (initial)
- **Source:** Combat idle held briefly, with subtle camera framing doing most of the work
- **Notes:** This isn't really a unique animation — it's combat idle held for a beat with cinematography. The sense of "face-off" comes from the *pause* and the camera, not the body.

---

## 2. Re-engagement (returning to combat after separation)

After a separation, units close back into engagement range. This is similar to first engagement but starts from "already in combat" state, not casual. Different rhythm, different energy.

### 2.1 Separation hold

- **Name:** `Loco_Idle_PostSeparation`
- **Description:** Brief held pose at the end of separation movement. Combat-ready but slightly recovering — breathing visible, weight settling, but already eyes locked on opponent. ~0.5s.
- **Phase:** End of Separating
- **Source:** Assembled — Kubold combat idle with slightly relaxed shoulders, or a "ready idle" variant
- **Notes:** This is the breathing beat between exchanges. Camera framing matters as much as animation here. Both units in this state simultaneously creates the dramatic standoff moment.

### 2.2 Re-engagement decision moment

- **Name:** `React_TacticalAssess`
- **Description:** Brief micro-animation showing the unit deciding to re-engage. A subtle nod, a fist clench, a small step forward. Tells the player "this unit just decided to attack again." ~0.2s.
- **Phase:** Transition from Separating → Approaching (re-engage)
- **Source:** Custom needed, OR substitute with a brief stance shift from Kubold
- **Notes:** Optional but high-value. This is the "I'm coming for you" beat. Without it, re-engagement feels mechanical (separation timer expires, run starts). With it, re-engagement reads as deliberate. Could be skipped if budget is tight.

### 2.3 Re-engagement initial step

- **Name:** `Loco_ReEngage_Step`
- **Description:** First step of returning to combat. Different from initial-approach because the unit is already in combat-ready posture. More controlled, more tactical, less of a "lunge" than first-engagement.
- **Phase:** Approaching (re-engage start)
- **Source:** Assembled — Kubold sprint start, but with shorter crossfade from combat idle (no need for "transitioning into combat" feel)
- **Notes:** The difference from first engagement is energetic. First engagement: wild surge. Re-engagement: focused pursuit. This is mostly conveyed through camera and pacing, not a unique animation.

### 2.4 Re-approach run/sprint

- **Name:** `Loco_Sprint_Combat` (same as 1.6)
- **Description:** Same combat-ready sprint as initial engagement.
- **Phase:** Approaching (re-engage sustained)
- **Source:** Same as 1.6 — assembled with body-part layering
- **Notes:** No need for separate re-engagement run; reuse the same locomotion. The *context* is different (camera framing, surrounding state) but the animation is the same.

### 2.5 Re-engagement stop and re-square

- **Name:** `Loco_ReEngage_Stop`
- **Description:** Sharp stop at engagement range, snapping back into combat stance. Faster and tighter than first-engagement deceleration because the unit is already in combat mindset.
- **Phase:** End of Approaching (re-engage)
- **Source:** Kubold direct + assembled (use Loco_Stop_Sharp at slightly faster playback rate)
- **Notes:** Animancer makes "play this clip 1.2× faster" trivial. Faster playback of the same stop animation reads as "more urgent" — which is what re-engagement should feel like.

### 2.6 Re-engagement face-off

- **Name:** `Stance_ReFaceOff`
- **Description:** Same as `Stance_FaceOff` (1.8) but typically shorter. Both units have already established combat — the second face-off doesn't need as long a beat.
- **Phase:** Engaged (re-engage)
- **Source:** Combat idle, briefly held
- **Notes:** Where first engagement might hold for 0.4s, re-engagement holds for 0.15-0.25s. Tighter pacing.

---

## 3. Engagement variants by intent

Different stances and tactical situations call for different *types* of engagement. These are variations on the basic engagement sequence.

### 3.1 Aggressive engagement (Onslaught/Tempest stance)

- **Name:** `Engage_Aggressive_Charge`
- **Description:** Full sprint with reduced caution. Body angled aggressively forward, arms not in defensive posture but already winding up for an attack. May skip the spotting beat entirely. Reads as "I'm coming for you, no hesitation."
- **Phase:** Spotting → Approaching (compressed timing)
- **Source:** Assembled — Kubold sprint at maximum playback rate, with a wind-up upper body layer
- **Notes:** Skip or minimize spotting phase (reduce timer to 0.1-0.2s). The "wind-up" upper body could be a fist clench or similar — Kubold may have something usable. The visual signature: aggressive units don't pause; they immediately convert detection into commitment.

### 3.2 Cautious engagement (Tactician/Conduit stance)

- **Name:** `Engage_Cautious_Approach`
- **Description:** Slower approach, more like a careful walk-in than a sprint. Eyes scanning, weight balanced, ready to react. Reads as "I'm reading you before I commit."
- **Phase:** Spotting → Approaching (extended timing)
- **Source:** Kubold's combat walk forward (the slow one you mentioned looks robotic — at this slow speed, it actually fits cautious engagement perfectly)
- **Notes:** Counterintuitively, Kubold's "robotic slow walk" is the *right* clip for cautious engagement. The problem is it's currently being used for *all* engagement, including aggressive. Differentiating use-case fixes the perceived problem. Pair with extended spotting timer (0.6-1.0s) for full cautious feel.

### 3.3 Defensive engagement (Stalwart/Sentinel stance)

- **Name:** `Engage_Defensive_Hold`
- **Description:** Unit doesn't approach. Plants feet at current position, raises full guard, waits for opponent to come to them. Body language is "make me move."
- **Phase:** Skips Approaching entirely; goes Spotting → Engaged at distance
- **Source:** Kubold combat idle held + brief "raise guard" transition
- **Notes:** This is structural rather than animation-heavy. The animation is just combat idle with guard up. The *behavior* (don't approach) is what makes it a defensive engagement. Some Kubold idle variants may have a slightly more guarded pose suitable for this.

### 3.4 Wraith-style engagement (Wraith stance)

- **Name:** `Engage_Wraith_Phase`
- **Description:** Vanish-and-reappear approach. Unit blurs out at original position, reappears at engagement range with afterimage. Skips the visible approach entirely.
- **Phase:** Spotting → directly to Engaged (skips Approaching visually)
- **Source:** Custom needed for the vanish/reappear effect, plus VFX
- **Notes:** This is more VFX work than animation work. The "animation" is just the unit's combat idle ending and the same idle starting at a new position, with particle/shader effects in between hiding the teleport. Kubold's `Speed_Burst_Vanish` (catalog) would be the basis if it exists. Currently catalog-only, needs creating.

### 3.5 Ambush engagement (situational)

- **Name:** `Engage_Ambush_Drop`
- **Description:** Unit drops down from above (off a tree, off a ledge, from concealment) into engagement range. Lands in combat stance, immediately ready.
- **Phase:** Pre-combat → Engaged (special)
- **Source:** Kubold has "drop" or "land" animations; chain with combat idle entry
- **Notes:** This is mostly position-driven (unit starts above, gravity arcs them down) with the landing animation being the visible part. Useful for environment-rich scenarios per `ENVIRONMENT_DESIGN.md`. Defer to when stealth / concealment becomes relevant.

### 3.6 Hostile recognition without engagement

- **Name:** `React_Hostile_NoEngage`
- **Description:** Unit notices hostile but doesn't engage (insufficient resources, defensive choice, retreating). Plays brief alert reaction, then continues current behavior (or retreats). ~0.4s.
- **Phase:** NotEngaged → may transition back to NotEngaged
- **Source:** Assembled from Kubold (alert reaction without commitment)
- **Notes:** Useful for scenarios where the unit *should* react to a threat but the AI decides not to engage. Without this, units either ignore threats (look unaware) or commit to engagement (forced into combat). Having "noticed but didn't engage" as a state is valuable for richer combat narratives.

---

## 4. Cross-cutting techniques (used across multiple engagement types)

These aren't engagement-specific animations — they're *techniques* used throughout to make engagement feel right.

### 4.1 Body-part layering for combat-aware locomotion

The single most important technique for fixing "robotic" Kubold movement.

- Base layer: Kubold's locomotion clips (sprint, walk, run, strafe)
- Upper-body layer (masked to arms/hands/head/spine): a "combat guard" pose or subtle arm motion

Even a static guard pose on the upper layer transforms how movement reads. Kubold's casual arm swing during sprint becomes a fixed guard with the legs sprinting. Suddenly the locomotion looks combat-ready.

This is the main fix for the "Kubold movement looks robotic in combat" problem. The legs are fine. The upper body is what's wrong.

### 4.2 Playback speed variation

Same animation, different feels via Animancer's playback speed:

- 1.0× = neutral
- 0.85× = sluggish, cautious, defensive
- 1.15× = sharp, alert, aggressive

Use this generously for engagement variations. Cautious engagement plays the run animation at 0.85× speed. Aggressive engagement plays it at 1.15×. Same clip, three different feels.

Pair with audio: faster playback typically wants more intense breathing/footstep sounds; slower playback wants more measured sounds.

### 4.3 Crossfade timing as expression

Different crossfade durations between clips communicate different things:

- Long crossfade (0.3s+): smooth, deliberate, controlled. Reads as "professional" or "calm."
- Short crossfade (0.05-0.1s): sharp, urgent, reactive. Reads as "alert" or "explosive."
- No crossfade: jarring, broken-feeling. Avoid.

For engagement specifically:
- First engagement (cautious): 0.2s crossfades (smooth transitions)
- First engagement (aggressive): 0.05s crossfades (sharp commitment)
- Re-engagement: 0.1s crossfades (already-in-combat efficiency)

### 4.4 Camera direction during engagement

Engagement is one of the moments where camera most affects perceived quality. Even with the same animations:

- Slow zoom-in during spotting → builds tension
- Tracking shot during approach → conveys momentum
- Quick cut between angles at engagement contact → emphasizes the moment
- Slight pull-back at face-off → frames the standoff

Camera work multiplies animation quality. A perfect approach animation with a static camera reads worse than a mediocre approach animation with dynamic camera.

This is presentation-layer work covered in `07_PRESENTATION.md` (when written), not animation. Mentioning here because engagement specifically benefits enormously.

---

## 5. Priority and effort breakdown

Not all of these are equally important. Here's a triage:

### Critical (build first, biggest impact on engagement feel)

- 1.6 / 4.1 — Combat-aware sprint via body-part layering. **The single highest-impact item.** Fixes the "robotic Kubold movement" problem. Should be week-one work.
- 1.4 — Combat-ready transition (Stance_EnterCombat). The missing bridge between casual and combat-ready posture.
- 1.7 — Approach deceleration into combat stance. Already mostly available from Kubold; needs proper wiring with deceleration tuning.
- 4.2 — Playback speed variation. Free with Animancer; just code variation.

### High value (build after critical items)

- 1.3 — Spotting alert idle. Enables the spotting phase to read as a real moment.
- 2.1 — Post-separation idle. Enables the breath-between-exchanges feel.
- 3.1, 3.2, 3.3 — Stance-variant engagements. Differentiates aggressive/cautious/defensive engagement.

### Medium value (build when polishing)

- 1.2 — Spotting reaction. Subtle but adds to spotting feel.
- 2.2 — Re-engagement decision moment. Optional; helps re-engagement read as deliberate.
- 3.6 — Hostile recognition without engagement. Supports complex tactical scenarios.

### Low priority / specialized

- 3.4 — Wraith-style vanish engagement. Requires VFX work; defer until Wraith stance is being polished.
- 3.5 — Ambush drop engagement. Defer until stealth/environment scenarios are active.

---

## 6. What's actually missing vs. what can be assembled

Counting the entries above:

- **Items requiring custom animation work:** 4
  - 1.2 React_Spotted_Subtle (or skipped)
  - 1.4 Stance_EnterCombat
  - 2.2 React_TacticalAssess (or skipped)
  - 3.4 Engage_Wraith_Phase (or VFX-driven)

- **Items assembled from existing Kubold clips:** ~15
  - Most engagement animations are sequences/blends of clips Kubold already provides

- **Items that are really just camera + timing:** ~6
  - Face-off poses, holding moments, decision beats

So the actual "custom animation needed for engagement" is small — maybe 2-4 short clips. The rest is assembly, layering, and presentation work that doesn't require new animation content.

This is much smaller than it might have felt. The "robotic Kubold movement" problem isn't about missing animations; it's about how the existing animations are being used. Layering combat-ready upper body over Kubold's sprint fixes most of it without any new content.

---

## 7. Recommended next steps

If you tackle this list directly, here's a sensible order:

**Week 1: The body-part layering fix (item 4.1).**
Set up upper-body avatar mask. Configure Animancer with two layers. Apply combat-guard upper body over locomotion. Test in training scene — sprint, walk, strafe — verify combat-ready posture is preserved. This single change visibly fixes the most-noticed problem.

**Week 2: Engagement transition animations.**
Build or assemble Stance_EnterCombat (1.4), Loco_Idle_Alert (1.3), and post-separation idle (2.1). These three together give the engagement and re-engagement phases distinct visual texture.

**Week 3: Stance-variant engagements.**
Wire aggressive (3.1), cautious (3.2), and defensive (3.3) engagement variants. Mostly tuning of timing and clip selection; minimal new animation. Verify in training scene that each stance produces visibly different engagement.

**Later: Polish layer.**
Spotting reactions (1.2, 2.2), Wraith vanish (3.4) and ambush variants (3.5) come when their respective gameplay scenarios are active. Don't pre-build.

Total effort: maybe 2-3 weeks of focused work for engagement-quality combat. Small custom animation budget (~3-4 clips). High-impact result.

---

## 8. Note on Kubold's "robotic combat walk"

You mentioned this specifically: Kubold's combat walk forward looks robotic. A few observations:

**It looks robotic at full slow speed** because that's the nature of slow combat-stance walking — measured, deliberate, no swing. Real combat shuffle is similarly "slow and deliberate," but somehow more grounded. The difference is usually in the upper body and weight distribution, not the legs.

**Solutions in priority order:**

1. Use it for the right contexts only. Cautious engagement, defensive holds, post-stun recovery — these *want* deliberate, measured movement. The "robotic" feel is correct for these moments.
2. Layer combat-guard upper body over it. The slow leg cycle is fine; the upper body is what reads as lifeless. A held guard pose with subtle weight shift transforms it.
3. Increase playback speed slightly (1.05-1.15×) for general engagement use. Same clip, snappier feel.
4. Use it less. For most engagement, use sprint or run, not the slow combat walk. The slow walk is a niche tool for specific moments.

The combination of (1), (2), and (4) probably solves it without any new animation work. Try those before deciding the clip itself is unusable.
