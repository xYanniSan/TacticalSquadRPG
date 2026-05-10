# ANIMATION_INVENTORY_KUBOLD.md

> **Tier 3 — Inventory artifact.** A flat catalog of every clip shipped inside the two Kubold packs we own (MovementAnimsetPro, FightingAnimsetPro). Descriptions are best-guess from naming convention, to be corrected by hand. Use the **Transition** column to mark which clips we'll wire into our `BattleAnimancerClipLibrary` (id slot) and which we'll drop.

**Naming-convention shorthand used below:**

- `KB_` = Kubold author prefix (FightingAnimsetPro only)
- `_m_` = **motion** — clip advances the character forward via root motion. Use for attacks that close distance / step into the strike. **Confirmed by motion probe** (see `ANIMATION_MOTION_PROBE.md`): of 49 `_m_` punch/kick clips, 47 step forward 0.21–2.45m. One outlier in-place: `KB_m_Hook_LR_combo`. Several **also rotate 180°** (spin attacks — see warning below).
- `_p_` = **in-place** ("place" / "pocket") — clip keeps the character stationary. Use for in-the-pocket attacks once already in range. **Confirmed by motion probe**: every `_p_` punch is 0.00m; `_p_` kicks are essentially in-place (a couple with sub-threshold <0.21m drift).
- **⚠️ Spin attacks (180° rotation)**: a subset of `_m_` clips end with the unit facing the **opposite direction** — `KB_m_Backelbow_L/R`, `KB_m_Backelbow_Uppercut_R`, `KB_m_Backswing_L/R`, `KB_m_MidKickBack_L/R`. These are dramatic finishers, but using them mid-combo will leave the unit facing away from the defender. Treat as last-hit-only, or pair with a re-orient step.
- `_L` / `_R` = left- vs right-handed/footed
- `_Loop` = looping cycle, designed to chain into itself seamlessly
- `_Start` = transition *into* a loop from idle
- `_Stop_LU` / `_Stop_RU` = transition *out of* a loop, ending on left-foot-up vs right-foot-up plant (the in-game blend picks one based on which foot was forward when the stop fires)
- `45 / 90 / 135 / 180` = angle of turn or strafe entry, in degrees
- `_combo` = pre-baked multi-hit string that already chains in-clip
- `_1 / _2 / _3` = stylistic variants of the same archetype

---

## MovementAnimsetPro

### MovementAnimsetPro/Animations/MovementAnimsetPro

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| MovementAnimsetPro | `Idle` | `loco_idle_relaxed` | Default non-combat standing idle. Relaxed posture, weight even. |
| MovementAnimsetPro | `Idle2Crouch` | `loco_to_crouch` | Stand-to-crouch transition. |
| MovementAnimsetPro | `Crouch_Idle` | `loco_idle_crouch` | Crouched idle loop. |
| MovementAnimsetPro | `Crouch2Idle` | `loco_crouch_to_stand` | Crouch-to-stand transition. |
| MovementAnimsetPro | `Crouch_WalkFwdStart_LU` / `_RU` | — / — | Begin crouched forward walk, leading with left/right foot. |
| MovementAnimsetPro | `Crouch_WalkFwdLoop` | `loco_crouch_walk_fwd` | Crouched forward walk loop. |
| MovementAnimsetPro | `Crouch_WalkFwdStop_LU` / `_RU` | — / — | End crouched forward walk on left/right plant. |
| MovementAnimsetPro | `Crouch_WalkFwdStart180_L` / `_R` | — / — | Begin crouched forward walk while pivoting 180° to the left/right. |
| MovementAnimsetPro | `Crouch_WalkFwdStart90_L` / `_R` | — / — | Begin crouched forward walk while pivoting 90° to the left/right. |
| MovementAnimsetPro | `WalkFwdLoop` | `loco_walk_fwd` | **Core forward walk loop (non-combat).** |
| MovementAnimsetPro | `WalkFwdLoop_LeanL` / `_LeanR` | — / — | Forward walk loop, body leaning left/right (curving path). |
| MovementAnimsetPro | `WalkFwdStart` | `loco_walk_fwd_start` | Idle → forward walk transition. |
| MovementAnimsetPro | `WalkFwdStart180_L` / `_R` | — / — | Idle → walk forward with a 180° pivot left/right. |
| MovementAnimsetPro | `WalkFwdStart90_L` / `_R` | — / — | Idle → walk forward with a 90° pivot left/right. |
| MovementAnimsetPro | `WalkFwdStop_LU` / `_RU` | `loco_walk_fwd_stop_l` / `_r` | Walk → idle stop, ending on left-/right-foot plant. |
| MovementAnimsetPro | `WalkArchLoop_L` / `_R` | — / — | Forward walk along an arc (curving left/right). |
| MovementAnimsetPro | `RunFwdLoop` | `loco_run_fwd` | **Core forward run loop (non-combat).** |
| MovementAnimsetPro | `RunFwdLoop_LeanL` / `_LeanR` | — / — | Forward run loop, body leaning left/right. |
| MovementAnimsetPro | `RunFwdStart` | `loco_run_fwd_start` | Idle → forward run transition. |
| MovementAnimsetPro | `RunFwdStart180_L` / `_R` | — / — | Idle → run forward with a 180° pivot left/right. |
| MovementAnimsetPro | `RunFwdStart90_L` / `_R` | — / — | Idle → run forward with a 90° pivot left/right. |
| MovementAnimsetPro | `RunFwdStop_LU` / `_RU` | `loco_run_fwd_stop_l` / `_r` | Run → idle stop on left-/right-foot plant. |
| MovementAnimsetPro | `RunFwdTurn180_L_LU` / `_RU` | — / — | Mid-run 180° turn to the left, ending on L/R foot. |
| MovementAnimsetPro | `RunFwdTurn180_R_LU` / `_RU` | — / — | Mid-run 180° turn to the right, ending on L/R foot. |
| MovementAnimsetPro | `RunArchLoop_L` / `_R` | — / — | Forward run along an arc. |
| MovementAnimsetPro | `SprintFwdLoop` | `loco_sprint_fwd` | **Core sprint loop.** |
| MovementAnimsetPro | `TurnLt90_Loop` / `TurnLt180_Loop` | — / — | In-place 90°/180° turn left. |
| MovementAnimsetPro | `TurnRt90_Loop` / `TurnRt180_Loop` | — / — | In-place 90°/180° turn right. |
| MovementAnimsetPro | `JumpIdleLand` | — | Land from a vertical jump back to idle. |
| MovementAnimsetPro | `JumpIdleHard` | — | Hard landing from a high vertical jump. |
| MovementAnimsetPro | `JumpIdleStart` | — | Idle → vertical-jump takeoff. |
| MovementAnimsetPro | `JumpIdle2Walk` | — | Land from vertical jump straight into walk. |
| MovementAnimsetPro | `JumpRunStart_LU` / `_RU` | — / — | Begin a running jump, taking off on L/R foot. |
| MovementAnimsetPro | `JumpRun_LU_Land` / `_RU_Land` | — / — | Running-jump landing on L/R foot. |
| MovementAnimsetPro | `JumpRun_LU_Land2Run` / `_RU_Land2Run` | — / — | Running-jump landing into continued run. |
| MovementAnimsetPro | `JumpWalkStart_LU` / `_RU` | — / — | Begin a walking jump, takeoff on L/R foot. |
| MovementAnimsetPro | `Jump_place_ALL` / `_short` | — / — | In-place jump, full vs short variant. |
| MovementAnimsetPro | `Jump_run_lu_ALL` / `Jump_run_ru_ALL` | — / — | Full running-jump cycle (takeoff + air + land). |
| MovementAnimsetPro | `Jump_walk_lu_ALL` / `Jump_walk_ru_ALL` | — / — | Full walking-jump cycle. |
| MovementAnimsetPro | `FallingLoop` | `loco_falling` | Mid-air falling loop. |
| MovementAnimsetPro | `FallingLoop_RootMotion` | — | Falling loop with forward root motion (used on falls with horizontal travel). |
| MovementAnimsetPro | `DontKnow` | — | Confused/idle gesture (shrug or look around). Likely gameplay flair. |
| MovementAnimsetPro | `ButtonPush_LH` / `_RH` / `_90` | — / — / — | Press a button with left/right hand; `_90` = button 90° to the side. |
| MovementAnimsetPro | `KeypadUse_LH` / `_RH` / `_90` | — / — / — | Operate a keypad with left/right hand; `_90` = side variant. |
| MovementAnimsetPro | `PickUp_LH` / `_RH` / `_90` | — / — / — | Pick up an object with left/right hand; `_90` = side variant. |
| MovementAnimsetPro | `PullLever_LH` / `_RH` / `_90` | — / — / — | Pull a lever with left/right hand; `_90` = side variant. |
| MovementAnimsetPro | `ThrowAway_LH` / `_RH` | — / — | Discard an item to the side with left/right hand. |
| MovementAnimsetPro | `WalkThroughDoor_LH` / `_RH` | — / — | Open and walk through a door using left/right hand. |

### MovementAnimsetPro/Animations/MovementAnimsetPro_Additionals

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| MovementAnimsetPro_Additionals | `WalkBwdStart_LU` / `_RU` | — / — | Idle → backward walk, takeoff on L/R foot. |
| MovementAnimsetPro_Additionals | `WalkBwdLoop` | `loco_walk_bwd` | **Backward walk loop.** |
| MovementAnimsetPro_Additionals | `WalkBwdStop_LU` / `_RU` | `loco_walk_bwd_stop_l` / `_r` | Backward walk → idle, plant on L/R foot. |
| MovementAnimsetPro_Additionals | `StrafeLeftStart_LU` / `_RU` | — / — | Idle → left strafe, takeoff on L/R foot. |
| MovementAnimsetPro_Additionals | `StrafeLeftLoop` | `loco_strafe_l` | **Left strafe loop.** |
| MovementAnimsetPro_Additionals | `StrafeLeftStop_LU` / `_RU` | — / — | Left strafe → idle. |
| MovementAnimsetPro_Additionals | `StrafeLeft45Loop` | — | Strafe-walk angled 45° to the left of facing. |
| MovementAnimsetPro_Additionals | `StrafeRightStart_LU` / `_RU` | — / — | Idle → right strafe, takeoff on L/R foot. |
| MovementAnimsetPro_Additionals | `StrafeRightLoop` | `loco_strafe_r` | **Right strafe loop.** |
| MovementAnimsetPro_Additionals | `StrafeRightStop_LU` / `_RU` | — / — | Right strafe → idle. |
| MovementAnimsetPro_Additionals | `StrafeRight135Loop` | — | Strafe-walk angled 135° (mostly backwards-right). |
| MovementAnimsetPro_Additionals | `RunFwdStart135_L` / `_R` | — / — | Idle → run forward with a 135° pivot. |
| MovementAnimsetPro_Additionals | `WalkFwdStart135_L` / `_R` | — / — | Idle → walk forward with a 135° pivot. |

### MovementAnimsetPro/Animations/MovementAnimsetPro_Fighting

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| MovementAnimsetPro_Fighting | `Fists_Idle` | `loco_idle_combat` | **Combat-stance idle (fists raised).** |
| MovementAnimsetPro_Fighting | `Idle2Fists` | `loco_to_combat` | Relaxed → combat-stance transition. |
| MovementAnimsetPro_Fighting | `Fists2Idle` | `loco_combat_to_relaxed` | Combat → relaxed transition. |
| MovementAnimsetPro_Fighting | `Fists_Punch_L` / `_R` | `attack_punch_basic_l` / `_r` | Standing punch, left/right hand. (Generic, not Kubold-style.) |
| MovementAnimsetPro_Fighting | `Fists_Punch_Move_L` / `_R` | — / — | Punch while stepping forward, L/R. |
| MovementAnimsetPro_Fighting | `Fists_Punch_Heavy2Idle` | — | Recovery from a heavy punch back to combat idle. |
| MovementAnimsetPro_Fighting | `Idle_Punch_Move_L` | — | From relaxed idle, step forward and punch left. |
| MovementAnimsetPro_Fighting | `Fists_Kick_Front_L` | `attack_kick_front_l` | Front kick with left leg. |
| MovementAnimsetPro_Fighting | `Fists_Kick_Front_Move_R` | — | Front kick with right leg while stepping forward. |
| MovementAnimsetPro_Fighting | `Fists_Hit_Left` / `_Right` | `react_hit_l` / `_r` | Take a hit from the left/right while in combat stance. |
| MovementAnimsetPro_Fighting | `Idle_Hit_Strong_Left` / `_Right` | — / — | Take a strong hit from L/R while in relaxed idle. |
| MovementAnimsetPro_Fighting | `Idle_Knockdown_Front` / `_Left` / `_Right` | `react_knockdown_f/l/r` | Get knocked down from the front/left/right while in relaxed idle. |
| MovementAnimsetPro_Fighting | `Death_1` / `Death_2` | `react_death_1` / `_2` | **Death animations (variants).** |

### MovementAnimsetPro/Animations/MovementAnimsetPro_Idles

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| MovementAnimsetPro_Idles | `Idle2` | — | Alternate idle (variant for breaking up looped Idle). |
| MovementAnimsetPro_Idles | `Idle3` | — | Alternate idle. |
| MovementAnimsetPro_Idles | `Idle4` | — | Alternate idle. |
| MovementAnimsetPro_Idles | `Idle5` | — | Alternate idle. |
| MovementAnimsetPro_Idles | `Idle6` | — | Alternate idle. |

### MovementAnimsetPro/Animations/MovementAnimsetPro_RunStrafeUpdate

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| MovementAnimsetPro_RunStrafeUpdate | `RunBwdLoop` | `loco_run_bwd` | **Backward run loop.** |
| MovementAnimsetPro_RunStrafeUpdate | `RunLtLoop` | `loco_run_strafe_l` | Run-strafe loop, left. |
| MovementAnimsetPro_RunStrafeUpdate | `RunRtLoop` | `loco_run_strafe_r` | Run-strafe loop, right. |
| MovementAnimsetPro_RunStrafeUpdate | `RunStrafeLeft45Loop` | — | Run-strafe at 45° to the left. |
| MovementAnimsetPro_RunStrafeUpdate | `RunStrafeLeft135Loop` | — | Run-strafe at 135° to the left (backwards-left). |
| MovementAnimsetPro_RunStrafeUpdate | `RunStrafeRight45Loop` | — | Run-strafe at 45° to the right. |
| MovementAnimsetPro_RunStrafeUpdate | `RunStrafeRight135Loop` | — | Run-strafe at 135° to the right (backwards-right). |
| MovementAnimsetPro_RunStrafeUpdate | `StrafeLeft135Loop` | — | Walk-strafe 135° left (updated version). |
| MovementAnimsetPro_RunStrafeUpdate | `StrafeRight45Loop` | — | Walk-strafe 45° right (updated version). |
| MovementAnimsetPro_RunStrafeUpdate | `Idle2Crouch_new` | — | Updated stand-to-crouch transition. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch2Idle_new` | — | Updated crouch-to-stand transition. |
| MovementAnimsetPro_RunStrafeUpdate | `CrouchLoop_new` | — | Updated crouched idle loop. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_Turn90L_new` / `Crouch_Turn90R_new` | — / — | Updated crouched 90° turn left/right. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkFwdStart_LU_new` / `_RU_new` | — / — | Updated crouched walk-fwd start, L/R foot. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkFwdStop_LU_new` / `_RU_new` | — / — | Updated crouched walk-fwd stop, L/R foot. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkBwdStart_LU` / `_RU` | — / — | Crouched walk-bwd start, L/R foot. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkBwdStop_LU` / `_RU` | — / — | Crouched walk-bwd stop, L/R foot. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLt45Start_LU` / `_RU` | — / — | Crouched 45° left strafe-walk start. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLt45Stop_LU` / `_RU` | — / — | Crouched 45° left strafe-walk stop. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLt135Start_LU` / `_RU` | — / — | Crouched 135° left strafe-walk start. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLt135Stop_LU` / `_RU` | — / — | Crouched 135° left strafe-walk stop. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRt45Start_LU` / `_RU` | — / — | Crouched 45° right strafe-walk start. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRt45Stop_LU` / `_RU` | — / — | Crouched 45° right strafe-walk stop. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRt135Start_LU` / `_RU` | — / — | Crouched 135° right strafe-walk start. |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRt135Stop_LU` / `_RU` | — / — | Crouched 135° right strafe-walk stop. |
| MovementAnimsetPro_RunStrafeUpdate | `Climb1m` | — | Climb up a ~1m ledge. |
| MovementAnimsetPro_RunStrafeUpdate | `GetUpFromBack` | `react_getup_back` | Stand up from lying on back. |
| MovementAnimsetPro_RunStrafeUpdate | `GetUpFromFace` | `react_getup_front` | Stand up from lying on face. |
| MovementAnimsetPro_RunStrafeUpdate | `HiddenButton` | — | Press a hidden / concealed button. |
| MovementAnimsetPro_RunStrafeUpdate | `KickTrashcan` | — | Kick a small object on the ground (env interaction). |
| MovementAnimsetPro_RunStrafeUpdate | `PatrolFullCycleLoop` | — | Full patrol-walk cycle (slow, looking around). |
| MovementAnimsetPro_RunStrafeUpdate | `PatrolLoop` | — | Patrol-walk loop. |
| MovementAnimsetPro_RunStrafeUpdate | `SitChairStart` / `Loop` / `End` | — / — / — | Sit on a chair: enter / loop / exit. |
| MovementAnimsetPro_RunStrafeUpdate | `Throw_Start` | — | Wind-up for an overhand throw. |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowLoop` | — | Hold the throw windup (looping). |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowSingle1` / `ThrowSingle2` | — / — | Single overhand throw release (variants). |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowEndClose` / `ThrowEndFar` | — / — | Throw release variants — short vs long throw. |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowCancel` | — | Cancel the throw windup. |
| MovementAnimsetPro_RunStrafeUpdate | `Turn180Surprised` | — | Snap 180° turn with a startled reaction. |

### MovementAnimsetPro/Animations/MovementAnimsetPro_SlideClimb

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| MovementAnimsetPro_SlideClimb | `Climb2m` | — | Climb up a ~2m ledge. |
| MovementAnimsetPro_SlideClimb | `Slide` | — | Sliding under a low obstacle. |
| MovementAnimsetPro_SlideClimb | `Vault1m` | — | Vault over a ~1m obstacle. |

### MovementAnimsetPro/Animations/MovementAnimsetPro_SprintFixed

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| MovementAnimsetPro_SprintFixed | `SprintFwdLoop` | `loco_sprint_fwd_v2` | Updated/fixed sprint forward loop (likely supersedes the older one). |

---

## FightingAnimsetPro

### FightingAnimsetPro/Animations/KB_Movement

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Movement | `KB_Idle_1` | `combat_idle_1` | **Combat-stance idle, variant 1** (fists up, weight forward). |
| KB_Movement | `KB_Idle_2` | `combat_idle_2` | Combat idle, variant 2. |
| KB_Movement | `KB_Idle_3` | `combat_idle_3` | Combat idle, variant 3. |
| KB_Movement | `KB_Idle_4` | `combat_idle_4` | Combat idle, variant 4. |
| KB_Movement | `KB_Idle_5` | `combat_idle_5` | Combat idle, variant 5. |
| KB_Movement | `KB_Idle_6` | `combat_idle_6` | Combat idle, variant 6. |
| KB_Movement | `KB_WalkFwd1` | `combat_walk_fwd_1` | **Forward walk in combat stance (fists up), variant 1.** |
| KB_Movement | `KB_WalkFwd2` | `combat_walk_fwd_2` | Forward walk, combat stance, variant 2. |
| KB_Movement | `KB_WalkBwd` | `combat_walk_bwd` | **Backward walk in combat stance.** |
| KB_Movement | `KB_WalkRight45` | `combat_walk_r45` | Walk forward-right at 45° (combat stance). |
| KB_Movement | `KB_WalkRight135` | `combat_walk_r135` | Walk back-right at 135° (combat stance). |
| KB_Movement | `KB_WalkLeft45` | `combat_walk_l45` | Walk forward-left at 45° (combat stance). |
| KB_Movement | `KB_WalkLeft135` | `combat_walk_l135` | Walk back-left at 135° (combat stance). |
| KB_Movement | `KB_TurnR_90` / `KB_TurnR_180` | `combat_turn_r90` / `_r180` | In-place 90°/180° turn right, combat stance. |
| KB_Movement | `KB_TurnL_90` / `KB_TurnL_180` | `combat_turn_l90` / `_l180` | In-place 90°/180° turn left, combat stance. |
| KB_Movement | `KB_Sidestep_L` / `KB_Sidestep_R` | `combat_sidestep_l` / `_r` | **Quick lateral sidestep, combat stance.** |
| KB_Movement | `KB_SkipFwd_1` / `KB_SkipFwd_2` | `combat_skip_fwd_1` / `_2` | Quick forward shuffle/skip (boxer-style advance), variants. |
| KB_Movement | `KB_SkipBwd_1` / `KB_SkipBwd_2` | `combat_skip_bwd_1` / `_2` | **Quick backward shuffle (boxer-style retreat), variants.** Likely separation candidates. |
| KB_Movement | `KB_Dodge_L` / `KB_Dodge_R` | `combat_dodge_l` / `_r` | **Quick dodge step left/right.** |

### FightingAnimsetPro/Animations/KB_Punches

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Punches | `KB_p_Jab_L_1` / `KB_p_Jab_R_1` | `attack_punch_jab_l` / `_r` | **Light jab, left/right hand.** Fast, low-commit poke. |
| KB_Punches | `KB_p_Jab_LL_combo` | `attack_punch_jab_double_l` | Pre-baked double-jab string with left hand. |
| KB_Punches | `KB_p_Jab_LR_combo` | `attack_punch_jab_lr` | Pre-baked left-right jab combo. |
| KB_Punches | `KB_p_Jab_LRL_combo` | `attack_punch_jab_lrl` | Pre-baked triple jab (left-right-left). |
| KB_Punches | `KB_p_Hook_L` / `KB_p_Hook_R` | `attack_punch_hook_l` / `_r` | **Hook punch, left/right** (lighter `_p_` variant). |
| KB_Punches | `KB_p_Uppercut_L` / `KB_p_Uppercut_R` | `attack_punch_uppercut_l` / `_r` | **Uppercut, left/right** (lighter `_p_` variant). |
| KB_Punches | `KB_p_Elbow_L` / `KB_p_Elbow_R` | `attack_elbow_l` / `_r` | Standing elbow strike, left/right. |
| KB_Punches | `KB_p_Elbow_Top_L` / `KB_p_Elbow_Top_R` | `attack_elbow_top_l` / `_r` | Downward/overhead elbow strike. |
| KB_Punches | `KB_p_OneTwo` | `attack_punch_onetwo` | **Classic 1-2 jab → cross combo (in-clip).** |
| KB_Punches | `KB_p_OneTwoThree` | `attack_punch_onetwothree` | 1-2-3 string in one clip (jab-cross-hook or similar). |
| KB_Punches | `KB_p_DoubleHooks` | `attack_punch_double_hook` | Double hook combo (L-R or R-L). |
| KB_Punches | `KB_p_DoubleJab` | `attack_punch_double_jab` | Double jab combo. |
| KB_Punches | `KB_p_MidJab_L` / `KB_p_MidJab_R` | `attack_punch_midjab_l` / `_r` | Body-level jab, L/R. |
| KB_Punches | `KB_p_MidHook_L` / `KB_p_MidHook_R` | `attack_punch_midhook_l` / `_r` | Body-level hook, L/R. |
| KB_Punches | `KB_p_MidUppercut_L` / `KB_p_MidUppercut_R` | `attack_punch_miduppercut_l` / `_r` | Body-level uppercut, L/R. |
| KB_Punches | `KB_p_RabbitPunch_Loop` | `attack_punch_rabbit_loop` | Rapid-fire small-punch flurry, looping. |
| KB_Punches | `KB_p_RabbitPunch_End` | `attack_punch_rabbit_end` | Exit out of the rabbit-punch flurry. |
| KB_Punches | `KB_m_Jab_L` / `KB_m_Jab_R` | `attack_m_jab_l` / `_r` | **Heavier jab variant** (more body weight, longer commit). |
| KB_Punches | `KB_m_Jab_LR_combo` | `attack_m_jab_lr` | Heavier jab L-R combo (in-clip). |
| KB_Punches | `KB_m_Jab_RLhook_combo` | `attack_m_jab_rl_hook` | Right jab → left hook combo (in-clip). |
| KB_Punches | `KB_m_Hook_L` / `KB_m_Hook_R` | `attack_m_hook_l` / `_r` | **Heavier hook, L/R.** |
| KB_Punches | `KB_m_Hook_LR_combo` | `attack_m_hook_lr` | Heavy L-R hook combo. |
| KB_Punches | `KB_m_Uppercut_L` / `KB_m_Uppercut_R` | `attack_m_uppercut_l` / `_r` | Heavy uppercut, L/R. |
| KB_Punches | `KB_m_Overhand_L` / `KB_m_Overhand_R` | `attack_m_overhand_l` / `_r` | **Overhand looping punch (haymaker), L/R.** |
| KB_Punches | `KB_m_OneTwo` | `attack_m_onetwo` | Heavy 1-2 combo. |
| KB_Punches | `KB_m_Backfist_L` / `KB_m_Backfist_R` | `attack_m_backfist_l` / `_r` | **Backfist strike, L/R.** Spinning or pivoting. |
| KB_Punches | `KB_m_BackfistRound_L` / `KB_m_BackfistRound_R` | `attack_m_backfist_round_l` / `_r` | Spinning/roundhouse-style backfist. |
| KB_Punches | `KB_m_BackfistRound_L2` | `attack_m_backfist_round_l2` | Backfist round, left, alt variant. |
| KB_Punches | `KB_m_BackfistRound_Far_R` | — | Backfist round right with a longer reach (advancing). |
| KB_Punches | `KB_m_Backelbow_L` / `KB_m_Backelbow_R` | `attack_m_backelbow_l` / `_r` | Backwards elbow strike, L/R. |
| KB_Punches | `KB_m_Backelbow_Uppercut_R` | — | Combined back-elbow into right uppercut. |
| KB_Punches | `KB_m_Backswing_L` / `KB_m_Backswing_R` | `attack_m_backswing_l` / `_r` | Big backwards swinging punch (windup variant). |
| KB_Punches | `KB_m_ElbowRound_L` / `KB_m_ElbowRound_R` | `attack_m_elbow_round_l` / `_r` | Roundhouse / spinning elbow, L/R. |
| KB_Punches | `KB_m_ElbowRound_L2` | — | Round elbow left, alt variant. |

### FightingAnimsetPro/Animations/KB_Kicks

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Kicks | `KB_AxeKick` | `attack_kick_axe` | **Axe kick — high windup, slamming heel down on opponent.** |
| KB_Kicks | `KB_p_HighKick_R_1` | `attack_kick_high_r` | Light high kick, right leg. |
| KB_Kicks | `KB_p_HighKickStraight_R` | `attack_kick_high_straight_r` | Straight high kick (not roundhouse), right leg. |
| KB_Kicks | `KB_p_LowKick_L_1` / `KB_p_LowKick_R_1` / `KB_p_LowKick_R_2` | `attack_kick_low_l` / `_r` / `_r2` | **Light low kick, L/R variants.** Leg-target / sweep style. |
| KB_Kicks | `KB_p_MidKick_L_1` / `KB_p_MidKick_R_1` | `attack_kick_mid_l` / `_r` | **Light mid-level kick, L/R.** |
| KB_Kicks | `KB_p_MidKickFront_L` / `KB_p_MidKickFront_R` | `attack_kick_mid_front_l` / `_r` | Front mid-level push-kick (teep), L/R. |
| KB_Kicks | `KB_p_MidKickStraight_R` | `attack_kick_mid_straight_r` | Straight mid kick, right leg. |
| KB_Kicks | `KB_m_HighKick_R` / `KB_m_HighKick_R_2` | `attack_m_kick_high_r` / `_r2` | **Heavy high kick, right.** Variants. |
| KB_Kicks | `KB_m_HighKickRound_L_1` / `KB_m_HighKickRound_R_1` | `attack_m_kick_high_round_l` / `_r` | **Heavy roundhouse high kick, L/R.** |
| KB_Kicks | `KB_m_MidKick_L` / `KB_m_MidKick_R` | `attack_m_kick_mid_l` / `_r` | Heavy mid kick, L/R. |
| KB_Kicks | `KB_m_MidKick_L_2` / `KB_m_MidKick_R_2` | `attack_m_kick_mid_l2` / `_r2` | Heavy mid kick, alt variants. |
| KB_Kicks | `KB_m_MidKickRoud_L_1` / `KB_m_MidKickRoud_R_1` | `attack_m_kick_mid_round_l` / `_r` | Heavy mid roundhouse, L/R. (Note: filename has "Roud" typo.) |
| KB_Kicks | `KB_m_MidKickStraight_R` | `attack_m_kick_mid_straight_r` | Straight heavy mid kick, right. |
| KB_Kicks | `KB_m_MidKickBack_L` / `KB_m_MidKickBack_R` | `attack_m_kick_mid_back_l` / `_r` | Spinning back mid-kick, L/R. |
| KB_Kicks | `KB_m_MidRabbitKick_R` | — | Rapid mid-level small-kick flurry, right leg. |
| KB_Kicks | `KB_m_LowKickRound_R` | `attack_m_kick_low_round_r` | Heavy low roundhouse, right. |
| KB_Kicks | `KB_m_LowKickL_Special` | — | Special variant low kick, left. (Possibly sweeping or tripping.) |
| KB_Kicks | `KB_m_BackKick_R` | `attack_m_kick_back_r` | Spinning back kick, right. |
| KB_Kicks | `KB_m_RoundhouseKickRight` | `attack_m_kick_roundhouse_r` | **Full roundhouse kick, right** (probably the "signature" big kick). |
| KB_Kicks | `KB_m_SideKickLeft` | `attack_m_kick_side_l` | Side kick, left. |
| KB_Kicks | `KB_m_KneeLeft` / `KB_m_KneeRight` | `attack_m_knee_l` / `_r` | **Knee strike, L/R** (close-range). |
| KB_Kicks | `KB_m_KickUppercut_R` | — | Rising upward kick, right (kick-equivalent of an uppercut). |
| KB_Kicks | `KB_MidKickStraightLong` | — | Long-reach straight mid-kick (advancing). |

### FightingAnimsetPro/Animations/KB_Specials

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Specials | `KB_AxeKick` | — | Duplicate of KB_Kicks/KB_AxeKick (same clip, different folder). |
| KB_Specials | `KB_Superpunch` | `attack_special_superpunch` | **Big telegraphed superpunch** — heavy windup, single dramatic hit. |
| KB_Specials | `KB_GroundAttack` | `attack_special_ground` | Slam into the ground (AOE-style). |
| KB_Specials | `KB_EyeLasers` | — | Fire eye-beam lasers (anime / superhero VFX driver). |
| KB_Specials | `KB_FootBazooka` | — | Comedic / cartoonish foot-rocket. Probably skip. |
| KB_Specials | `KB_FootShotgun` | — | Comedic / cartoonish foot-gun. Probably skip. |
| KB_Specials | `KB_Gun` | — | Pistol shoot pose. |
| KB_Specials | `KB_Grenade` | — | Throw a grenade. |
| KB_Specials | `KB_KnifeThrow` | `attack_special_knife_throw` | **Throw a knife** — useful for ranged-weapon character. |
| KB_Specials | `KB_Mine` | — | Place a mine on the ground. |
| KB_Specials | `KB_Projectile_1` … `KB_Projectile_5` | `attack_special_projectile_1` … `_5` | **Five projectile-throwing variants** (different windups / styles). |
| KB_Specials | `KB_Projectile_Up` | `attack_special_projectile_up` | Project upward / arc throw. |
| KB_Specials | `KB_MidKickStraightLong` | — | Duplicate of KB_Kicks variant in this folder. |

### FightingAnimsetPro/Animations/KB_Blocks

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Blocks | `KB_Block_Start` | `defense_block_start` | Raise guard into block. |
| KB_Blocks | `KB_Block_Loop` | `defense_block_loop` | **Hold block (looping).** |
| KB_Blocks | `KB_Block_End` | `defense_block_end` | Release block back to stance. |
| KB_Blocks | `KB_Block_Single` | `defense_block_single` | **One-shot quick block** (tap reaction). |
| KB_Blocks | `KB_MidBlock_L_Single` / `KB_MidBlock_R_Single` | `defense_block_mid_l` / `_r` | Single mid-level block, L/R side. |
| KB_Blocks | `KB_crouch_Block_Start` | `defense_crouch_block_start` | Enter crouched block. |
| KB_Blocks | `KB_crouch_Block_Loop` | `defense_crouch_block_loop` | Crouched block hold. |
| KB_Blocks | `KB_crouch_Block_End` | `defense_crouch_block_end` | Exit crouched block. |
| KB_Blocks | `KB_crouch_Block_Single` | `defense_crouch_block_single` | One-shot crouched block. |
| KB_Blocks | `KB_p_Duck` | `defense_duck_p` | **Light duck (head dodge under high attack).** |
| KB_Blocks | `KB_m_Duck` | `defense_duck_m` | Heavier duck (deeper / more committed). |
| KB_Blocks | `KB_m_Duck_L` / `KB_m_Duck_R` | `defense_duck_l` / `_r` | Side-leaning duck/slip, L/R. |

### FightingAnimsetPro/Animations/KB_Hits

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Hits | `KB_HitOnGroundFront` | `react_hit_ground_front` | Take a hit while lying face-down. |
| KB_Hits | `KB_HitOnGroundBack` | `react_hit_ground_back` | Take a hit while lying on back. |
| KB_Hits | `KB_Hit_p_HighFront_Weak` | `react_hit_high_f_weak` | Light flinch, hit to the high-front. |
| KB_Hits | `KB_Hit_p_HighLeft_Weak` / `KB_Hit_p_HighRight_Weak` | `react_hit_high_l_weak` / `_r` | Light flinch, hit to the high-L/R. |
| KB_Hits | `KB_Hit_p_HighUpper_Weak` | `react_hit_high_upper_weak` | Light flinch from an uppercut-style hit. |
| KB_Hits | `KB_Hit_p_LowLeft_Weak` / `KB_Hit_p_LowRight_Weak` | `react_hit_low_l_weak` / `_r` | Light flinch, hit to the low-L/R (legs/torso low). |
| KB_Hits | `KB_Hit_p_MidFront_Weak` | `react_hit_mid_f_weak` | Light flinch from front body-shot. |
| KB_Hits | `KB_Hit_p_MidLeft_Weak` / `KB_Hit_p_MidRight_Weak` | `react_hit_mid_l_weak` / `_r` | Light flinch from L/R body-shot. |
| KB_Hits | `KB_Hit_m_HighFront_Weak` / `_Med` / `_Stagger` | `react_hit_high_f_*` | **Front high hits — three intensities.** Stagger = brief loss of balance. |
| KB_Hits | `KB_Hit_m_HighBack_Weak` / `_Stagger` | `react_hit_high_back_*` | Back high hits — weak / stagger. |
| KB_Hits | `KB_Hit_m_HighLeft_Weak` / `_Med` | `react_hit_high_l_*` | High-left hits, two intensities. |
| KB_Hits | `KB_Hit_m_HighRight_Weak` / `_Med` | `react_hit_high_r_*` | High-right hits, two intensities. |
| KB_Hits | `KB_Hit_m_LowLeft_Weak` / `_Med` | `react_hit_low_l_*` | Low-left hits, two intensities. |
| KB_Hits | `KB_Hit_m_LowRight_Weak` / `_Med` | `react_hit_low_r_*` | Low-right hits, two intensities. |
| KB_Hits | `KB_Hit_m_MidFront_Weak` / `_Med` / `_Stagger` | `react_hit_mid_f_*` | Front mid (body) hits — three intensities. |
| KB_Hits | `KB_Hit_m_MidBack_Med` | `react_hit_mid_back_med` | Back mid-body hit. |
| KB_Hits | `KB_Hit_m_MidLeft_Weak` / `_Med` / `_Stagger` | `react_hit_mid_l_*` | Mid-left hits — three intensities. |
| KB_Hits | `KB_Hit_m_MidRight_Weak` / `_Med` / `_Stagger` | `react_hit_mid_r_*` | Mid-right hits — three intensities. |
| KB_Hits | `KB_Hit_m_MidTop_Med` | `react_hit_mid_top_med` | Hit from above, mid. |

### FightingAnimsetPro/Animations/KB_KOs

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_KOs | `KB_HighKO_L` / `KB_HighKO_R` | `react_ko_high_l` / `_r` | **Knockout from a high-L/R hit** (falls L/R). |
| KB_KOs | `KB_HighKO_Air` | `react_ko_high_air` | KO with air-time (flies briefly). |
| KB_KOs | `KB_HighKO_Powerful` | `react_ko_high_powerful` | Big-impact high KO (longer flight / harder land). |
| KB_KOs | `KB_LowKO_L` / `KB_LowKO_R` | `react_ko_low_l` / `_r` | KO from a low hit, falling L/R. |
| KB_KOs | `KB_MidKO` | `react_ko_mid` | Generic mid-body KO. |
| KB_KOs | `KB_MidKO_Back` | `react_ko_mid_back` | Mid-body KO from behind (falls forward). |
| KB_KOs | `KB_MidKO_Powerful` | `react_ko_mid_powerful` | Big-impact mid KO. |
| KB_KOs | `KB_TopKO` | `react_ko_top` | KO from a top-down hit. |
| KB_KOs | `KB_KO_Head` | `react_ko_head` | KO specifically from a headshot. |
| KB_KOs | `KB_UpperKO` / `KB_UpperKO2` | `react_ko_upper_1` / `_2` | KO from an uppercut, two variants. |
| KB_KOs | `KB_UpperKO_Flip` | `react_ko_upper_flip` | KO from an uppercut with a back-flip arc. |
| KB_KOs | `KB_GetUpBack` / `KB_GetUpBack180` | `getup_back` / `_180` | **Get up from back** — facing same / opposite direction. |
| KB_KOs | `KB_GetUpFace` / `KB_GetUpFace180` | `getup_face` / `_180` | Get up from face-down — same / opposite direction. |
| KB_KOs | `KB_LayBack_Roll` | `getup_back_roll` | Roll over from back before getting up. |
| KB_KOs | `KB_crouch_MidKO` / `KB_crouch_MidKO_Back` | `react_ko_mid_crouch` / `_back` | KO while crouched. |

### FightingAnimsetPro/Animations/KB_Crouched

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Crouched | `KB_crouch_Idle` | `combat_crouch_idle` | Crouched combat idle. |
| KB_Crouched | `KB_crouch_Start` | `combat_crouch_start` | Stand → crouch (combat). |
| KB_Crouched | `KB_crouch_End` | `combat_crouch_end` | Crouch → stand (combat). |
| KB_Crouched | `KB_crouch_WalkFwd` | `combat_crouch_walk_fwd` | Crouched forward walk. |
| KB_Crouched | `KB_crouch_WalkBwd` | `combat_crouch_walk_bwd` | Crouched backward walk. |
| KB_Crouched | `KB_crouch_WalkLeft45` / `KB_crouch_WalkLeft135` | `combat_crouch_walk_l45` / `_l135` | Crouched 45°/135° left strafe-walk. |
| KB_Crouched | `KB_crouch_WalkRight45` / `KB_crouch_WalkRight135` | `combat_crouch_walk_r45` / `_r135` | Crouched 45°/135° right strafe-walk. |
| KB_Crouched | `KB_crouch_Sidestep_L` / `KB_crouch_Sidestep_R` | `combat_crouch_sidestep_l` / `_r` | Crouched lateral sidestep, L/R. |
| KB_Crouched | `KB_crouch_TurnL_90` / `KB_crouch_TurnR_90` | `combat_crouch_turn_l90` / `_r90` | Crouched 90° turn, L/R. |
| KB_Crouched | `KB_crouch_Block_*` | (see KB_Blocks) | Duplicate listing of crouched block clips. |
| KB_Crouched | `KB_crouch_Hit_p_MidFront_Weak` | `react_crouch_hit_mid_f` | Light flinch while crouched, front. |
| KB_Crouched | `KB_crouch_Hit_p_MidLeft_Weak` / `_MidRight_Weak` | `react_crouch_hit_mid_l` / `_r` | Light flinch while crouched, L/R. |
| KB_Crouched | `KB_crouch_p_Jab_L` / `KB_crouch_p_Jab_R` | `attack_crouch_jab_l` / `_r` | **Crouched jab, L/R.** |
| KB_Crouched | `KB_crouch_p_Uppercut_L` / `KB_crouch_p_Uppercut_R` | `attack_crouch_uppercut_l` / `_r` | Crouched uppercut, L/R. |
| KB_Crouched | `KB_crouch_p_LowKick_L` | `attack_crouch_lowkick_l` | Crouched low kick, left. |
| KB_Crouched | `KB_crouch_p_LowKickRound_R` | `attack_crouch_lowkick_round_r` | Crouched low roundhouse, right. |
| KB_Crouched | `KB_crouch_m_LowKickRound_R` / `_2` | `attack_crouch_m_lowkick_round_r` / `_r2` | Crouched heavy low roundhouse, right; variant 2. |
| KB_Crouched | `KB_crouch_m_Uppercut_R` | `attack_crouch_m_uppercut_r` | Crouched heavy uppercut, right. |

### FightingAnimsetPro/Animations/KB_Jumping

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Jumping | `KB_Jump_Start` | `combat_jump_start` | Begin a combat jump. |
| KB_Jumping | `KB_Jump_Loop` | `combat_jump_loop` | Mid-air loop. |
| KB_Jumping | `KB_Jump_Loop2Kick` | `combat_jump_to_kick` | Mid-air → into kick. |
| KB_Jumping | `KB_Jump_Arcade_RootJump` | — | Arcadey vertical jump w/ root motion. |
| KB_Jumping | `KB_Jump_Realistic` / `KB_Jump_Realistic_RootJump` | — / — | Realistic vertical jump (in-place / root-motion variants). |
| KB_Jumping | `KB_JumpFwd_Arcade_RootJump` | — | Forward arcade jump w/ root motion. |
| KB_Jumping | `KB_JumpFwd_Realistic` / `KB_JumpFwd_Realistic_RootJump` | — / — | Forward realistic jump (in-place / root-motion). |
| KB_Jumping | `KB_JumpKick_Loop` | `attack_jump_kick_loop` | Air-kick mid-flight, looping. |
| KB_Jumping | `KB_JumpKick_2_Jump_Loop` | — | Variant jump-kick mid-flight. |
| KB_Jumping | `KB_JumpPunch` | `attack_jump_punch` | Air punch (one-shot). |
| KB_Jumping | `KB_Land` | `combat_jump_land` | Land from a jump. |
| KB_Jumping | `KB_LandPrepare` | `combat_jump_land_prep` | Anticipation pose before landing. |

### FightingAnimsetPro/Animations/KB_Lay

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Lay | `LayOnBack_Loop` | `react_lay_back_loop` | **Idle loop while lying on back** (KO'd / downed). |
| KB_Lay | `LayOnFront_Loop` | `react_lay_front_loop` | Idle loop while lying face-down. |

### FightingAnimsetPro/Animations/KB_Stretch

| AnimationSet | Animation Clip | Transition | Description |
|---|---|---|---|
| KB_Stretch | `KB_Stretch` | — | Pre-fight stretching / warm-up flair. Cosmetic. |

---

## Counts (rough)

- **MovementAnimsetPro**: ~155 clips total across all sub-folders.
- **FightingAnimsetPro**: ~225 clips across KB_Movement / Punches / Kicks / Specials / Blocks / Hits / KOs / Crouched / Jumping / Lay / Stretch.

Total inventory: **~380 clips.**

---

## How to use this file

1. Read through and **correct any wrong description** (cross out, write the truth).
2. In the **Transition** column, fill in the `BattleAnimancerClipLibrary` id you want this clip wired to — or leave as `—` if we'll skip it.
3. When done, hand back: I'll re-import the corrected mappings into the clip library and update the H2H combo defaults to use real Kubold clips.

The bolded entries above are my suggested **must-wire-first set** for H2H combat to feel real.
