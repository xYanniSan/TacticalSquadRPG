# LOCOMOTION_CHEATSHEET.md

> Distilled from `ANIMATION_MOTION_PROBE.md`. Picks out only the locomotion clips, computes m/s for loops, and proposes a keep/skip list. Use this to drive the locomotion-driver wiring.

---

## Speed ladder (loop clips)

m/s = `forward_displacement / clip_length`. This is the in-game speed each loop will produce when played back at 1× speed with root motion enabled.

| Speed band | Clip | m/s | Notes |
|---|---|---:|---|
| **Combat walk fwd** | `KB_WalkFwd1` | **0.60** | Combat-stance forward walk, slow boxer pace. |
| Combat walk fwd (alt) | `KB_WalkFwd2` | 0.88 | Slightly faster combat walk variant. |
| **Combat walk bwd** | `KB_WalkBwd` | **0.73** | Combat-stance retreat. |
| Standing walk fwd | `WalkFwdLoop` | 1.57 | Non-combat walk. |
| Standing walk bwd | `WalkBwdLoop` | 1.57 | Non-combat retreat. |
| Standing strafe L | `StrafeLeftLoop` | 1.57 | |
| Standing strafe R | `StrafeRightLoop` | 1.57 | |
| **Run fwd** | `RunFwdLoop` | **3.39** | Non-combat run. |
| Run bwd | `RunBwdLoop` | 2.10 | |
| Run strafe L | `RunLtLoop` | 2.05 | |
| Run strafe R | `RunRtLoop` | 2.14 | |
| Run strafe 45° L | `RunStrafeLeft45Loop` | fwd 2.43 / lat -2.43 | Diagonal forward-left run. |
| Run strafe 45° R | `RunStrafeRight45Loop` | fwd 2.43 / lat +2.43 | Diagonal forward-right run. |
| **Sprint** | `SprintFwdLoop` (SprintFixed) | **6.00** | Use the SprintFixed version, not the original (5.86). |
| Combat sidestep L | `KB_Sidestep_L` | 0.94 | One-shot, ~0.77s. |
| Combat sidestep R | `KB_Sidestep_R` | 0.86 | One-shot. |

So the **gameplay speed bands** map cleanly to:

- **Combat idle ↔ combat walk** (0.6–0.9 m/s) — engaged-state movement.
- **Combat walk ↔ standing walk** (0.9–1.6 m/s) — approach / disengage.
- **Standing walk ↔ run** (1.6–3.4 m/s) — traversal.
- **Run ↔ sprint** (3.4–6.0 m/s) — long-distance reposition.

For our H2H system, the relevant ones are the **combat-stance band** (KB_WalkFwd1, KB_WalkBwd, KB_Sidestep_L/R) for engaged movement and **run** (RunFwdLoop) for approach. Sprint is overkill for current map sizes; reserve for later "ninja-dash" abilities.

---

## Start / Stop cheatsheet

For each loop we want a `Start` (idle → loop) and a `Stop_LU` / `Stop_RU` (loop → idle on left-/right-foot plant). The number is the **footplant offset** — how far the character travels during the blend.

### Standing forward (`WalkFwdLoop` / `RunFwdLoop`)

| Clip | Length | Forward (m) | Use |
|---|---:|---:|---|
| `WalkFwdStart` | 0.77 | +0.66 | Idle → walk |
| `WalkFwdStop_LU` | 1.33 | +0.63 | Walk → idle, plant L |
| `WalkFwdStop_RU` | 1.53 | +0.68 | Walk → idle, plant R |
| `RunFwdStart` | 0.77 | +1.27 | Idle → run |
| `RunFwdStop_LU` | 1.27 | +1.44 | Run → idle, plant L |
| `RunFwdStop_RU` | 1.50 | +1.86 | Run → idle, plant R |

### Standing backward (`WalkBwdLoop`)

| Clip | Length | Forward (m) | Use |
|---|---:|---:|---|
| `WalkBwdStart` | 0.83 | -0.71 | Idle → walk-bwd |
| `WalkBwdStop_LU` | 1.10 | -0.57 | Walk-bwd → idle |
| `WalkBwdStop_RU` | 1.20 | -0.33 | Walk-bwd → idle |

### Standing strafe (`StrafeLeftLoop` / `StrafeRightLoop`)

| Clip | Length | Lateral (m) | Use |
|---|---:|---:|---|
| `StrafeLeftStart` | 0.90 | -0.68 | |
| `StrafeLeftStop_LU` | 1.30 | -0.28 | |
| `StrafeLeftStop_RU` | 1.60 | -0.72 | |
| `StrafeRightStart` | 1.20 | +1.18 | |
| `StrafeRightStop_LU` | 1.17 | +0.89 | |
| `StrafeRightStop_RU` | 1.00 | +0.59 | |

### Pivot-into-walk/run (mid-turn starts)

| Clip | Length | Forward (m) | Lateral (m) | Rotation | Use |
|---|---:|---:|---:|---:|---|
| `WalkFwdStart90_L` | 0.83 | +0.62 | -0.15 | -90° | 90° pivot into walk |
| `WalkFwdStart90_R` | 1.27 | +1.07 | +0.70 | +90° | 90° pivot into walk |
| `WalkFwdStart135_L` | 0.90 | +0.51 | -0.04 | -135° | 135° pivot into walk |
| `WalkFwdStart135_R` | 1.40 | +1.20 | +0.23 | +135° | 135° pivot into walk |
| `WalkFwdStart180_L` | 0.90 | +0.47 | -0.15 | -180° | 180° pivot into walk |
| `WalkFwdStart180_R` | 1.40 | +1.11 | +0.27 | +180° | 180° pivot into walk |
| `RunFwdStart90_L` | 0.67 | +0.81 | -0.18 | -90° | 90° pivot into run |
| `RunFwdStart90_R` | 1.00 | +1.88 | +0.20 | +90° | 90° pivot into run |
| `RunFwdStart135_L` | 0.87 | +0.97 | -0.11 | -135° | 135° pivot into run |
| `RunFwdStart135_R` | 1.13 | +1.90 | +0.06 | +135° | 135° pivot into run |
| `RunFwdStart180_L` | 0.87 | +0.88 | -0.26 | -180° | 180° pivot into run |
| `RunFwdStart180_R` | 1.13 | +1.82 | +0.28 | +180° | 180° pivot into run |
| `RunFwdTurn180_L_LU` / `_RU` | ~1.0 | +1.4 | +0.2 | -180° | Mid-run 180° turn (L/R) |
| `RunFwdTurn180_R_LU` / `_RU` | ~1.5 | +2.8 | -0.1 | +180° | Mid-run 180° turn (L/R) |

### In-place turns (idle → idle)

| Clip | Length | Rotation | Use |
|---|---:|---:|---|
| `TurnLt90_Loop` | 1.30 | -90° | Standing 90° L turn |
| `TurnRt90_Loop` | 1.30 | +90° | Standing 90° R turn |
| `TurnLt180` | 1.67 | -180° | Standing 180° L turn |
| `TurnRt180` | 1.67 | +180° | Standing 180° R turn |
| `KB_TurnL_90` | 1.00 | -90° | Combat-stance 90° L turn |
| `KB_TurnR_90` | 1.00 | +90° | Combat-stance 90° R turn |
| `KB_TurnL_180` | 1.17 | -180° | Combat-stance 180° L turn |
| `KB_TurnR_180` | 1.17 | +180° | Combat-stance 180° R turn |

### Combat-stance equivalents (KB_)

| Clip | Length | Speed | Notes |
|---|---:|---:|---|
| `KB_WalkFwd1` | 0.77 | +0.46m → 0.60 m/s | **Primary engaged-walk-fwd loop.** |
| `KB_WalkFwd2` | 0.77 | +0.68m → 0.88 m/s | Variant (faster). |
| `KB_WalkBwd` | 0.77 | -0.56m → 0.73 m/s | **Engaged-walk-bwd loop.** |
| `KB_WalkLeft45` | 0.77 | fwd +0.46, lat -0.46 | Diagonal fwd-left walk. |
| `KB_WalkRight45` | 0.77 | fwd +0.46, lat +0.46 | Diagonal fwd-right walk. |
| `KB_WalkLeft135` | 0.77 | fwd -0.46, lat -0.46 | Diagonal bwd-left walk. |
| `KB_WalkRight135` | 0.77 | fwd -0.46, lat +0.46 | Diagonal bwd-right walk. |
| `KB_Sidestep_L` | 0.77 | -0.72m lat → 0.94 m/s | One-shot quick lateral. |
| `KB_Sidestep_R` | 0.77 | +0.66m lat → 0.86 m/s | One-shot quick lateral. |
| `KB_SkipFwd_1` | 1.62 | +2.15m → 1.33 m/s | Boxer skip-forward burst. |
| `KB_SkipFwd_2` | 1.73 | +1.90m → 1.10 m/s | Variant. |
| `KB_SkipBwd_1` | 1.60 | -2.02m → 1.26 m/s | **Separation candidate (back-skip).** |
| `KB_SkipBwd_2` | 1.63 | -2.21m → 1.36 m/s | Variant. |
| `KB_Dodge_L` | 1.32 | -1.63m lat → 1.23 m/s | One-shot dodge burst, left. |
| `KB_Dodge_R` | 1.22 | +2.15m lat → 1.76 m/s | One-shot dodge burst, right (faster). |

The KB_ pack has **no Start/Stop transition pairs** for its loops — they're hand-loopable but transition harshly back to idle. We'll either:

- **Option A**: blend KB_WalkFwd1 → standing `WalkFwdStop` clip on transition. Mismatched stance, but a fast cross-fade hides it.
- **Option B**: author a quick custom blend curve in the locomotion driver (no Stop clip, just smooth-damp the playback weight to 0).

Option B is simpler and what the driver already does (`Mathf.SmoothDamp` over `_phaseSpeedSmoothTime` per `KuboldLocomotionDriver.cs`). Stick with B.

---

## Special-purpose clips

| Clip | Length | Travel | Verdict |
|---|---:|---:|---|
| `Slide` | 2.40 | +6.20m fwd | **Keep** — sprint-stop slide, fast turn finisher (per user). 2.6 m/s in clip but the displacement covers ~6m, perfect for "slide-to-stop" out of a sprint or "slide-around-corner" reposition. |
| `Vault1m` | 2.90 | +3.10m fwd, +0m vert | Skip — environmental. |
| `Climb1m` / `Climb2m` | — | — | Skip — environmental. |
| `Turn180Surprised` | 2.57 | +0.12 fwd, +0.47 lat, -180° | Skip — too slow for combat use, has surprised reaction overlay. |
| `JumpIdleStart` / `Land` / `LandHard` | — | airborne | Skip for now — no jump combat yet. |
| `JumpRunStart_LU/RU` | — | +6.9m / +9.2m airborne | Skip — these are running-jumps with full root motion, parkour-flavored. Revisit when we add traversal. |
| `Jump_run_lu_ALL` / `Jump_walk_lu_ALL` | — | +3m | Skip — full jump cycles, environmental. |
| `PatrolFullCycleLoop` | 13.57 | +7m and 360° rotation | Skip — NPC patrol clip, not relevant. |
| `PatrolLoop` | 1.87 | +1.15m | Skip. |

---

## Skip pile (non-combat / non-locomotion clips)

These exist in the FBXes but we **skip entirely** — environmental / adventure-game leftovers from the original Kubold use case:

- `ButtonPush_*`, `KeypadUse_*`, `PullLever_*`, `PickUp_*`, `ThrowAway_*`, `WalkThroughDoor_*`
- `KickTrashcan`, `HiddenButton`
- `SitChairStart` / `Loop` / `End`
- `Throw_Start` / `ThrowLoop` / `ThrowSingle1` / `ThrowSingle2` / `ThrowEndClose` / `ThrowEndFar` / `ThrowCancel`
- `DontKnow` (shrug gesture)
- `JumpRunStart_LU/RU`, `JumpWalkStart_LU/RU`, `JumpIdleStart`, `Jump_*_ALL`, `Jump_walk_*_ALL` (environmental jumping)
- All `_Realistic_RootJump` / `_Arcade_RootJump` variants (pick one when we add jump combat)
- `Crouch_*` family (we won't implement crouching)
- `Idle2Crouch`, `Crouch2Idle`, `Idle2Crouch_new`, `Crouch2Idle_new`
- `Idle2`, `Idle3`, `Idle4`, `Idle5`, `Idle6` — keep maybe one for variety, skip the rest
- `KB_Idle_2`–`KB_Idle_6` — keep `KB_Idle_1` as primary, maybe one variant
- All un-used MovementAnimsetPro_Fighting clips (we use the KB_ versions instead): `Fists_Punch_*`, `Fists_Kick_*`, `Idle_Hit_*`, `Idle_Knockdown_*` (the KB_ pack is richer)
- `BindPose` / `tpose` (Unity scaffolding, not real clips)

---

## Keep list — final shape

**Idle / standing locomotion (10 clips):**
`Idle`, `WalkFwdLoop`, `WalkFwdStart`, `WalkFwdStop_LU`, `WalkFwdStop_RU`, `WalkBwdLoop`, `WalkBwdStart`, `RunFwdLoop`, `RunFwdStart`, `SprintFwdLoop` (use SprintFixed version)

**Standing strafes (8 clips):**
`StrafeLeftLoop`, `StrafeLeftStart`, `StrafeLeftStop_LU`, `StrafeRightLoop`, `StrafeRightStart`, `StrafeRightStop_LU`, `RunLtLoop`, `RunRtLoop`

**Standing turns / pivot-starts (10 clips, optional but useful):**
`TurnLt90_Loop`, `TurnRt90_Loop`, `TurnLt180`, `TurnRt180`, `WalkFwdStart90_L`, `WalkFwdStart90_R`, `WalkFwdStart180_L`, `WalkFwdStart180_R`, `RunFwdTurn180_L_LU`, `RunFwdTurn180_R_LU`

**Combat-stance core (12 clips):**
`KB_Idle_1`, `KB_WalkFwd1`, `KB_WalkBwd`, `KB_WalkLeft45`, `KB_WalkRight45`, `KB_WalkLeft135`, `KB_WalkRight135`, `KB_Sidestep_L`, `KB_Sidestep_R`, `KB_TurnL_90`, `KB_TurnR_90`, `KB_TurnL_180` / `KB_TurnR_180`

**Combat bursts (4 clips):**
`KB_SkipFwd_1`, `KB_SkipBwd_1`, `KB_Dodge_L`, `KB_Dodge_R`

**Special-purpose (1 clip):**
`Slide` — for fast-direction-change finisher / sprint-stop.

**Reactions / state (already in use):**
`GetUpFromBack`, `GetUpFromFace`, `LayOnBack_Loop`, `LayOnFront_Loop`, `Death_1`, `Death_2`, plus the KB_Hit_*, KB_KO_*, KB_GetUp* family (handled separately in the inventory doc).

**Total locomotion-keep: ~45 clips.** That plus the ~50–70 H2H combat clips (jabs/hooks/uppercuts/kicks/blocks/hits) gives a Year-1 wired set of **~100–120 clips**, well within the realistic budget.

---

## Open questions for you

1. Do we need the **standing-walk** band (1.57 m/s) at all, or do we always engage in combat-stance and use only KB_WalkFwd1 + RunFwdLoop? The standing band is what the unit uses when "Not Engaged" — if we always show fists raised, we can cut WalkFwdLoop entirely and save 4 clips.
2. **Sprint** (6.0 m/s) — keep for future or skip until we need it? On a 30m terrain map, sprint reaches the other side in 5 seconds, which feels right for "I see them and I close." Recommend keeping.
3. The **45° / 135° walk variants** (`KB_WalkLeft45`, etc.) are useful if the locomotion driver picks them based on aim-direction-vs-move-direction angle. If the driver currently just uses fwd/bwd/strafe, we can skip the diagonals for now (4 clips saved). Worth checking the driver.
4. **`KB_WalkFwd2`** is the slightly-faster combat walk. Variant we want, or pick one?
