# Animation Catalog — MovementAnimsetPro + FightingAnimsetPro

> **Purpose.** A complete inventory of every clip Kubold ships in the
> two packs we're using, classified into our move-engine taxonomy, and
> mapped from Kubold's source name to a canonical project name. When we
> wire clips into `BattleAnimancerClipLibrary` or the move catalog
> (`Resources/Moves/`), use the **Proposed Name** column as the id.
>
> Updated 2026-05-08.

---

## Naming scheme

The proposed names follow the existing move-catalog convention:

```
<category>_<subcategory>_<descriptor>
```

| Prefix              | Meaning                                                                 |
|---------------------|-------------------------------------------------------------------------|
| `idle_*`            | Standing in place, breathing / shifting                                  |
| `locomotion_*`      | Default-gait travel (walk, run, sprint), in a directional variant        |
| `movement_*`        | Non-default travel (turn, sidestep, skip, slide, climb, vault)           |
| `crouch_*`          | Crouched states — idle / locomotion / attacks / blocks / hits            |
| `jump_*`            | Jumping (in-place, forward, jump-attack variants, landings)              |
| `attack_punch_*`    | Hand strikes (jab/hook/uppercut/elbow/backfist/overhand)                 |
| `attack_kick_*`     | Foot strikes (low/mid/high/round/axe/side/knee/back/sweep)               |
| `attack_special_*`  | Specials (superpunch, projectiles, ground attacks, weapon throws)        |
| `defend_block_*`    | Standing block (single/loop/start/end, mid_L/R, crouch variants)         |
| `defend_duck_*`     | Duck under attacks (L/R, parry-duck)                                     |
| `defend_dodge_*`    | Stepping evasions (L/R/back, sidestep evasions)                          |
| `react_hit_*`       | Pain reactions (height × direction × intensity matrix)                   |
| `react_stagger_*`   | Heavy-hit stagger (separate from `react_hit_*`'s weak/med category)      |
| `react_ko_*`        | Knockout collapse (high/mid/low × L/R, powerful, head, upper, top, etc.) |
| `react_groundhit_*` | Hit while already on the ground                                          |
| `getup_*`           | Recovery from prone (back / face / 180-rotated variants)                 |
| `lay_*`             | Lying / prone idle                                                       |
| `interact_*`        | Environmental interactions (door, button, lever, throw, sit, climb)      |
| `cinematic_*`       | Demo / patrol / scripted poses                                           |
| `pose_*`            | Utility poses (bind, tpose) — skipped from the live library              |

Direction tokens: `_fwd`, `_back`, `_left`, `_right`, `_strafeL`, `_strafeR`, `_45L`, `_135L`, `_45R`, `_135R`.
Side tokens for attacks: `_R` (right side), `_L` (left side).
Phase tokens: `_start`, `_loop`, `_end`, `_single`.

`BindPose` and `tpose` clips inside every Kubold FBX are utility poses and are **excluded from the live library**.

---

## MovementAnimsetPro (relaxed gait, locomotion-focused)

### `MovementAnimsetPro.fbx` — main locomotion set

| Original                        | Category               | Proposed                              | Notes |
|---------------------------------|------------------------|---------------------------------------|-------|
| BindPose                        | pose                   | (skip)                                | Bind utility |
| Idle                            | idle                   | `idle_relaxed_1`                       | Relaxed standing idle, hands at sides |
| TurnRt90_Loop                   | movement_turn          | `movement_turn_R_90_loop`              | In-place 90° right turn loop |
| TurnLt90_Loop                   | movement_turn          | `movement_turn_L_90_loop`              | In-place 90° left turn loop |
| TurnRt180                       | movement_turn          | `movement_turn_R_180`                  | One-shot 180° right turn |
| TurnLt180                       | movement_turn          | `movement_turn_L_180`                  | One-shot 180° left turn |
| WalkFwdLoop                     | locomotion_walk        | `locomotion_walk_fwd`                  | Loop — relaxed forward walk |
| WalkFwdStart                    | locomotion_walk        | `locomotion_walk_fwd_start`            | Idle → walk transition |
| WalkFwdStart180_R               | locomotion_walk        | `locomotion_walk_fwd_start_180R`       | Walk start with 180° right rotation |
| WalkFwdStart180_L               | locomotion_walk        | `locomotion_walk_fwd_start_180L`       | Walk start with 180° left rotation |
| WalkFwdStart90_L                | locomotion_walk        | `locomotion_walk_fwd_start_90L`        | Walk start with 90° left rotation |
| WalkFwdStart90_R                | locomotion_walk        | `locomotion_walk_fwd_start_90R`        | Walk start with 90° right rotation |
| WalkFwdStop_LU                  | locomotion_walk        | `locomotion_walk_fwd_stop_LU`          | Stop on left foot up |
| WalkFwdStop_RU                  | locomotion_walk        | `locomotion_walk_fwd_stop_RU`          | Stop on right foot up |
| WalkFwdLoop_LeanR               | locomotion_walk        | `locomotion_walk_fwd_lean_R`           | Walk loop leaning right (curve) |
| WalkArchLoop_R                  | locomotion_walk        | `locomotion_walk_arch_R`               | Walking around an arc to the right |
| WalkFwdLoop_LeanL               | locomotion_walk        | `locomotion_walk_fwd_lean_L`           | Walk loop leaning left |
| WalkArchLoop_L                  | locomotion_walk        | `locomotion_walk_arch_L`               | Walking around an arc to the left |
| RunFwdLoop                      | locomotion_run         | `locomotion_run_fwd`                   | Loop — relaxed forward run |
| SprintFwdLoop                   | locomotion_sprint      | `locomotion_sprint_fwd`                | Loop — full-tilt sprint |
| RunFwdStart                     | locomotion_run         | `locomotion_run_fwd_start`             | Idle → run transition |
| RunFwdStart180_R                | locomotion_run         | `locomotion_run_fwd_start_180R`        | Run start with 180° right rotation |
| RunFwdStart180_L                | locomotion_run         | `locomotion_run_fwd_start_180L`        | Run start with 180° left rotation |
| RunFwdStart90_R                 | locomotion_run         | `locomotion_run_fwd_start_90R`         | Run start with 90° right rotation |
| RunFwdStart90_L                 | locomotion_run         | `locomotion_run_fwd_start_90L`         | Run start with 90° left rotation |
| RunFwdStop_RU                   | locomotion_run         | `locomotion_run_fwd_stop_RU`           | Run stop on right foot |
| RunFwdStop_LU                   | locomotion_run         | `locomotion_run_fwd_stop_LU`           | Run stop on left foot |
| RunFwdTurn180_R_LU              | locomotion_run         | `locomotion_run_fwd_turn180R_LU`       | Run + 180° right turn, left foot up |
| RunFwdTurn180_R_RU              | locomotion_run         | `locomotion_run_fwd_turn180R_RU`       | Run + 180° right turn, right foot up |
| RunFwdTurn180_L_RU              | locomotion_run         | `locomotion_run_fwd_turn180L_RU`       | Run + 180° left turn, right foot up |
| RunFwdTurn180_L_LU              | locomotion_run         | `locomotion_run_fwd_turn180L_LU`       | Run + 180° left turn, left foot up |
| RunArchLoop_L                   | locomotion_run         | `locomotion_run_arch_L`                | Running around an arc to the left |
| RunFwdLoop_LeanL                | locomotion_run         | `locomotion_run_fwd_lean_L`            | Run loop leaning left |
| RunArchLoop_R                   | locomotion_run         | `locomotion_run_arch_R`                | Running around an arc to the right |
| RunFwdLoop_LeanR                | locomotion_run         | `locomotion_run_fwd_lean_R`            | Run loop leaning right |
| Jump_place_ALL                  | jump                   | `jump_place_full`                      | Full vertical jump in place |
| Jump_place_ALL_short            | jump                   | `jump_place_short`                     | Short vertical jump in place |
| Jump_walk_ru_ALL                | jump                   | `jump_walk_RU`                         | Jump from walk, take-off right foot |
| Jump_walk_lu_ALL                | jump                   | `jump_walk_LU`                         | Jump from walk, take-off left foot |
| Jump_run_ru_ALL                 | jump                   | `jump_run_RU`                          | Jump from run, take-off right foot |
| Jump_run_lu_ALL                 | jump                   | `jump_run_LU`                          | Jump from run, take-off left foot |
| JumpIdleStart                   | jump                   | `jump_idle_start`                      | In-place jump take-off |
| JumpIdleLand                    | jump                   | `jump_idle_land`                       | Idle-jump landing |
| JumpIdleLand2Walk               | jump                   | `jump_idle_land_to_walk`               | Land transitioning into walk |
| JumpIdleLandHard                | jump                   | `jump_idle_land_hard`                  | Heavy idle-jump landing (knees-down) |
| JumpWalkStart_RU                | jump                   | `jump_walk_start_RU`                   | Walk-jump take-off, right foot up |
| JumpWalk_RU_Land                | jump                   | `jump_walk_land_RU`                    | Walk-jump landing, right foot up |
| JumpWalk_RU_Land2Walk           | jump                   | `jump_walk_land_to_walk_RU`            | Land + continue walk, right-foot start |
| JumpWalkStart_LU                | jump                   | `jump_walk_start_LU`                   | Walk-jump take-off, left foot up |
| JumpWalk_LU_Land                | jump                   | `jump_walk_land_LU`                    | Walk-jump landing, left foot up |
| JumpWalk_LU_Land2Walk           | jump                   | `jump_walk_land_to_walk_LU`            | Land + continue walk, left-foot start |
| JumpRunStart_RU                 | jump                   | `jump_run_start_RU`                    | Run-jump take-off, right foot up |
| JumpRun_RU_Land                 | jump                   | `jump_run_land_RU`                     | Run-jump landing, right foot up |
| JumpRun_RU_Land2Run             | jump                   | `jump_run_land_to_run_RU`              | Land + continue run, right-foot start |
| JumpRunStart_LU                 | jump                   | `jump_run_start_LU`                    | Run-jump take-off, left foot up |
| JumpRun_LU_Land                 | jump                   | `jump_run_land_LU`                     | Run-jump landing, left foot up |
| JumpRun_LU_Land2Run             | jump                   | `jump_run_land_to_run_LU`              | Land + continue run, left-foot start |
| FallingLoop                     | jump                   | `jump_falling_loop`                    | Mid-air falling loop |
| FallingLoop_RootMotion          | jump                   | `jump_falling_loop_rm`                 | Falling loop with root motion |
| Idle2Crouch                     | crouch                 | `crouch_idle_in`                       | Stand → crouch transition |
| Crouch_Idle                     | crouch                 | `crouch_idle`                          | Crouched standing idle |
| Crouch2Idle                     | crouch                 | `crouch_idle_out`                      | Crouch → stand transition |
| Crouch_WalkFwdLoop              | crouch                 | `crouch_walk_fwd`                      | Crouch walk forward loop |
| Crouch_WalkFwdStart             | crouch                 | `crouch_walk_fwd_start`                | Crouch idle → walk fwd |
| Crouch_WalkFwdStop_LU           | crouch                 | `crouch_walk_fwd_stop_LU`              | Crouch walk stop, left up |
| Crouch_WalkFwdStop_RU           | crouch                 | `crouch_walk_fwd_stop_RU`              | Crouch walk stop, right up |
| Crouch_WalkFwdStart180_R        | crouch                 | `crouch_walk_fwd_start_180R`           | Crouch walk start, 180° right |
| Crouch_WalkFwdStart180_L        | crouch                 | `crouch_walk_fwd_start_180L`           | Crouch walk start, 180° left |
| Crouch_WalkFwdStart90_R         | crouch                 | `crouch_walk_fwd_start_90R`            | Crouch walk start, 90° right |
| Crouch_WalkFwdStart90_L         | crouch                 | `crouch_walk_fwd_start_90L`            | Crouch walk start, 90° left |
| ButtonPush_RH                   | interact               | `interact_button_R`                    | Press a button with right hand |
| ButtonPush_RH_90                | interact               | `interact_button_R_90`                 | Same but at 90° angle |
| ButtonPush_LH                   | interact               | `interact_button_L`                    | Press a button with left hand |
| ButtonPush_LH_90                | interact               | `interact_button_L_90`                 | Same but at 90° angle |
| KeypadUse_RH                    | interact               | `interact_keypad_R`                    | Type on keypad, right hand |
| KeypadUse_RH_90                 | interact               | `interact_keypad_R_90`                 | At 90° angle |
| KeypadUse_LH                    | interact               | `interact_keypad_L`                    | Type on keypad, left hand |
| KeypadUse_LH_90                 | interact               | `interact_keypad_L_90`                 | At 90° angle |
| PickUp_RH                       | interact               | `interact_pickup_R`                    | Pick up object, right hand |
| PickUp_RH_90                    | interact               | `interact_pickup_R_90`                 | At 90° angle |
| PickUp_LH                       | interact               | `interact_pickup_L`                    | Pick up object, left hand |
| PickUp_LH_90                    | interact               | `interact_pickup_L_90`                 | At 90° angle |
| PullLever_RH                    | interact               | `interact_lever_R`                     | Pull a lever, right hand |
| PullLever_RH_90                 | interact               | `interact_lever_R_90`                  | At 90° angle |
| PullLever_LH                    | interact               | `interact_lever_L`                     | Pull a lever, left hand |
| PullLever_LH_90                 | interact               | `interact_lever_L_90`                  | At 90° angle |
| DontKnow                        | cinematic              | `cinematic_shrug`                      | "I don't know" shrug pose |
| ThrowAway_RH                    | interact               | `interact_throwaway_R`                 | Discard object over shoulder, R hand |
| ThrowAway_LH                    | interact               | `interact_throwaway_L`                 | Discard object, L hand |
| WalkThroughDoor_RH              | interact               | `interact_door_R`                      | Open + walk through door, R hand |
| WalkThroughDoor_LH              | interact               | `interact_door_L`                      | Open + walk through door, L hand |

### `MovementAnimsetPro_Idles.fbx`

| Original    | Category | Proposed             | Notes |
|-------------|----------|----------------------|-------|
| BindPose    | pose     | (skip)               | |
| Idle6       | idle     | `idle_relaxed_6`      | Relaxed idle variant 6 (note: number ≠ ordering) |
| Idle2       | idle     | `idle_relaxed_2`      | Relaxed idle variant 2 |
| Idle3       | idle     | `idle_relaxed_3`      | Relaxed idle variant 3 |
| Idle4       | idle     | `idle_relaxed_4`      | Relaxed idle variant 4 |
| Idle5       | idle     | `idle_relaxed_5`      | Relaxed idle variant 5 |

### `MovementAnimsetPro_Fighting.fbx` — fight-stance variants of locomotion clips

| Original                  | Category          | Proposed                          | Notes |
|---------------------------|-------------------|-----------------------------------|-------|
| BindPose                  | pose              | (skip)                            | |
| Idle2Fists                | idle              | `idle_relaxed_to_fists`           | Relaxed idle → fists guard transition |
| Fists_Idle                | idle              | `idle_fists`                       | Combat-ready fists guard idle |
| Fists2Idle                | idle              | `idle_fists_to_relaxed`           | Fists → relaxed transition |
| Idle_Punch_Move_L         | attack_punch      | `attack_punch_relaxed_L`          | Punch from relaxed idle, left arm |
| Fists_Punch_Move_L        | attack_punch      | `attack_punch_fists_move_L`       | Walking-punch transition, left |
| Fists_Punch_Move_R        | attack_punch      | `attack_punch_fists_move_R`       | Walking-punch transition, right |
| Fists_Punch_L             | attack_punch      | `attack_punch_fists_L`            | Stationary fists punch, left |
| Fists_Punch_R             | attack_punch      | `attack_punch_fists_R`            | Stationary fists punch, right |
| Fists_Kick_Front_Move_R   | attack_kick       | `attack_kick_fists_move_R`        | Walking-kick, front, right |
| Fists_Kick_Front_L        | attack_kick       | `attack_kick_fists_front_L`       | Stationary front kick, left |
| Fists_Punch_Heavy2Idle    | attack_punch      | `attack_punch_heavy_to_idle`      | Heavy punch follow-through into idle |
| Fists_Hit_Left            | react_hit         | `react_hit_fists_left`            | Light hit from left while in fists guard |
| Fists_Hit_Right           | react_hit         | `react_hit_fists_right`           | Light hit from right while in fists guard |
| Idle_Hit_Strong_Left      | react_stagger     | `react_stagger_idle_left`         | Strong hit, idle stance, from left |
| Idle_Hit_Strong_Right     | react_stagger     | `react_stagger_idle_right`        | Strong hit, idle stance, from right |
| Idle_Knockdown_Front      | react_ko          | `react_ko_idle_front`             | Knockdown reaction, front impact |
| Idle_Knockdown_Right      | react_ko          | `react_ko_idle_right`             | Knockdown reaction, right impact |
| Idle_Knockdown_Left       | react_ko          | `react_ko_idle_left`              | Knockdown reaction, left impact |
| Death_1                   | react_ko          | `react_ko_death_1`                | Death animation variant 1 |
| Death_2                   | react_ko          | `react_ko_death_2`                | Death animation variant 2 |

### `MovementAnimsetPro_Additionals.fbx` — extra back-walk and strafe variants

| Original              | Category         | Proposed                          | Notes |
|-----------------------|------------------|-----------------------------------|-------|
| WalkFwdStart135_R     | locomotion_walk  | `locomotion_walk_fwd_start_135R`  | Walk start with 135° right turn |
| WalkFwdStart135_L     | locomotion_walk  | `locomotion_walk_fwd_start_135L`  | Walk start with 135° left turn |
| RunFwdStart135_R      | locomotion_run   | `locomotion_run_fwd_start_135R`   | Run start with 135° right turn |
| RunFwdStart135_L      | locomotion_run   | `locomotion_run_fwd_start_135L`   | Run start with 135° left turn |
| WalkBwdStart          | locomotion_walk  | `locomotion_walk_back_start`      | Idle → backward walk |
| WalkBwdLoop           | locomotion_walk  | `locomotion_walk_back`            | Backward walk loop |
| WalkBwdStop_LU        | locomotion_walk  | `locomotion_walk_back_stop_LU`    | Stop backward walk, left foot up |
| WalkBwdStop_RU        | locomotion_walk  | `locomotion_walk_back_stop_RU`    | Stop backward walk, right foot up |
| StrafeRightStart      | locomotion_walk  | `locomotion_walk_strafeR_start`   | Strafe right start |
| StrafeRightLoop       | locomotion_walk  | `locomotion_walk_strafeR`         | Strafe right loop |
| StrafeRightStop_RU    | locomotion_walk  | `locomotion_walk_strafeR_stop_RU` | Strafe right stop, right foot up |
| StrafeRightStop_LU    | locomotion_walk  | `locomotion_walk_strafeR_stop_LU` | Strafe right stop, left foot up |
| StrafeLeftStart       | locomotion_walk  | `locomotion_walk_strafeL_start`   | Strafe left start |
| StrafeLeftLoop        | locomotion_walk  | `locomotion_walk_strafeL`         | Strafe left loop |
| StrafeLeftStop_LU     | locomotion_walk  | `locomotion_walk_strafeL_stop_LU` | Strafe left stop, left foot up |
| StrafeLeftStop_RU     | locomotion_walk  | `locomotion_walk_strafeL_stop_RU` | Strafe left stop, right foot up |
| StrafeRight135Loop    | locomotion_walk  | `locomotion_walk_strafe_135R`     | Diagonal strafe back-right loop |
| StrafeLeft45Loop      | locomotion_walk  | `locomotion_walk_strafe_45L`      | Diagonal strafe forward-left loop |

### `MovementAnimsetPro_RunStrafeUpdate.fbx` — large grab-bag (strafes, props, getup, sit)

| Original                     | Category               | Proposed                              | Notes |
|------------------------------|------------------------|---------------------------------------|-------|
| Crouch_WalkLt135_new         | crouch                 | `crouch_walk_135L`                    | Crouch diagonal back-left walk |
| Crouch_WalkLt45_new          | crouch                 | `crouch_walk_45L`                     | Crouch diagonal forward-left walk |
| Crouch_WalkRt135_new         | crouch                 | `crouch_walk_135R`                    | Crouch diagonal back-right walk |
| Crouch_WalkRt45_new          | crouch                 | `crouch_walk_45R`                     | Crouch diagonal forward-right walk |
| Idle2Crouch_new              | crouch                 | `crouch_idle_in_v2`                   | Updated stand → crouch |
| Crouch2Idle_new              | crouch                 | `crouch_idle_out_v2`                  | Updated crouch → stand |
| CrouchLoop_new               | crouch                 | `crouch_idle_v2`                      | Updated crouch idle loop |
| Crouch_Turn90R_new           | crouch                 | `crouch_turn_R_90`                    | Crouch in-place 90° right turn |
| Crouch_Turn90L_new           | crouch                 | `crouch_turn_L_90`                    | Crouch in-place 90° left turn |
| Crouch_WalkFwdStart_new      | crouch                 | `crouch_walk_fwd_start_v2`            | Updated crouch fwd walk start |
| Crouch_WalkFwd_new           | crouch                 | `crouch_walk_fwd_v2`                  | Updated crouch fwd walk loop |
| Crouch_WalkFwdStop_LU_new    | crouch                 | `crouch_walk_fwd_stop_LU_v2`          | Updated stop, left up |
| Crouch_WalkFwdStop_RU_new    | crouch                 | `crouch_walk_fwd_stop_RU_v2`          | Updated stop, right up |
| Crouch_WalkBwdStart_new      | crouch                 | `crouch_walk_back_start`              | Crouch back walk start |
| Crouch_WalkBwd_new           | crouch                 | `crouch_walk_back`                    | Crouch back walk loop |
| Crouch_WalkBwdStop_LU_new    | crouch                 | `crouch_walk_back_stop_LU`            | Crouch back stop, left up |
| Crouch_WalkBwdStop_RU_new    | crouch                 | `crouch_walk_back_stop_RU`            | Crouch back stop, right up |
| Crouch_WalkRtStart_new       | crouch                 | `crouch_walk_strafeR_start`           | Crouch strafe right start |
| Crouch_WalkRt_new            | crouch                 | `crouch_walk_strafeR`                 | Crouch strafe right loop |
| Crouch_WalkRtStop_LU_new     | crouch                 | `crouch_walk_strafeR_stop_LU`         | Crouch strafe right stop |
| Crouch_WalkRtStop_RU_new     | crouch                 | `crouch_walk_strafeR_stop_RU`         | Crouch strafe right stop |
| Crouch_WalkLtStart_new       | crouch                 | `crouch_walk_strafeL_start`           | Crouch strafe left start |
| Crouch_WalkLt_new            | crouch                 | `crouch_walk_strafeL`                 | Crouch strafe left loop |
| Crouch_WalkLtStop_LU_new     | crouch                 | `crouch_walk_strafeL_stop_LU`         | Crouch strafe left stop |
| Crouch_WalkLtStop_RU_new     | crouch                 | `crouch_walk_strafeL_stop_RU`         | Crouch strafe left stop |
| RunBwdLoop                   | locomotion_run         | `locomotion_run_back`                 | Backward run loop |
| RunLtLoop                    | locomotion_run         | `locomotion_run_strafeL`              | Run strafe left loop |
| RunRtLoop                    | locomotion_run         | `locomotion_run_strafeR`              | Run strafe right loop |
| RunStrafeLeft45Loop          | locomotion_run         | `locomotion_run_strafe_45L`           | Run diagonal forward-left |
| RunStrafeRight135Loop        | locomotion_run         | `locomotion_run_strafe_135R`          | Run diagonal back-right |
| RunStrafeRight45Loop         | locomotion_run         | `locomotion_run_strafe_45R`           | Run diagonal forward-right |
| RunStrafeLeft135Loop         | locomotion_run         | `locomotion_run_strafe_135L`          | Run diagonal back-left |
| HiddenButton                 | interact               | `interact_button_hidden`              | Press a hidden floor button |
| KickTrashcan                 | cinematic              | `cinematic_kick_trashcan`             | Demo: angry trashcan kick |
| Turn180Surprised             | cinematic              | `cinematic_turn180_surprised`         | Surprised reaction + 180° turn |
| PatrolFullCycleLoop          | cinematic              | `cinematic_patrol_full`               | Long looping patrol cycle |
| PatrolLoop                   | cinematic              | `cinematic_patrol_short`              | Short patrol loop |
| Throw_Start                  | interact               | `interact_throw_start`                | Throwing-object windup |
| ThrowLoop                    | interact               | `interact_throw_hold`                 | Holding throw pose loop |
| ThrowEndFar                  | interact               | `interact_throw_release_far`          | Throw release, long distance |
| ThrowEndClose                | interact               | `interact_throw_release_close`        | Throw release, short distance |
| ThrowCancel                  | interact               | `interact_throw_cancel`               | Cancel a wound-up throw |
| ThrowSingle1                 | interact               | `interact_throw_single_1`             | One-shot throw variant 1 |
| ThrowSingle2                 | interact               | `interact_throw_single_2`             | One-shot throw variant 2 |
| StrafeRight45Loop            | locomotion_walk        | `locomotion_walk_strafe_45R`          | Walk diagonal forward-right |
| StrafeLeft135Loop            | locomotion_walk        | `locomotion_walk_strafe_135L`         | Walk diagonal back-left |
| Climb1m                      | movement_climb         | `movement_climb_1m`                   | Climb 1m ledge |
| SitChairStart                | interact               | `interact_sit_chair_start`            | Sit down on chair |
| SitChairLoop                 | interact               | `interact_sit_chair_loop`             | Sitting idle on chair |
| SitChairEnd                  | interact               | `interact_sit_chair_end`              | Stand up from chair |
| GetUpFromBack                | getup                  | `getup_from_back`                     | Get up from supine (lying on back) |
| GetUpFromFace                | getup                  | `getup_from_face`                     | Get up from prone (lying on face) |

### `MovementAnimsetPro_SlideClimb.fbx`

| Original | Category        | Proposed              | Notes |
|----------|-----------------|-----------------------|-------|
| BindPose | pose            | (skip)                | |
| Slide    | movement_slide  | `movement_slide`       | Combat slide / power-slide |
| Vault1m  | movement_vault  | `movement_vault_1m`    | Vault over 1m obstacle |
| Climb2m  | movement_climb  | `movement_climb_2m`    | Climb 2m ledge / wall |

### `MovementAnimsetPro_SprintFixed.fbx`

| Original       | Category          | Proposed              | Notes |
|----------------|-------------------|-----------------------|-------|
| BindPose       | pose              | (skip)                | |
| SprintFwdLoop  | locomotion_sprint | `locomotion_sprint_fwd_v2` | Updated sprint loop (preferred over the one in main FBX) |

---

## FightingAnimsetPro (combat-stance set)

### `KB_Movement.fbx` — fight-stance locomotion + dodges

| Original         | Category                | Proposed                      | Notes |
|------------------|-------------------------|-------------------------------|-------|
| BindPose         | pose                    | (skip)                        | |
| tpose            | pose                    | (skip)                        | T-pose enforcement target |
| KB_Idle_1        | idle                    | `idle_combat_1`               | Combat-ready boxing idle, primary |
| KB_Idle_2        | idle                    | `idle_combat_2`               | Combat idle variant 2 (subtle motion) |
| KB_Idle_3        | idle                    | `idle_combat_3`               | Combat idle variant 3 |
| KB_Idle_4        | idle                    | `idle_combat_4`               | Combat idle variant 4 |
| KB_Idle_5        | idle                    | `idle_combat_5`               | Combat idle variant 5 |
| KB_Idle_6        | idle                    | `idle_combat_6`               | Combat idle variant 6 |
| KB_WalkFwd1      | locomotion_walk         | `locomotion_walk_fwd_combat`  | Fight-stance forward walk loop, primary |
| KB_WalkFwd2      | locomotion_walk         | `locomotion_walk_fwd_combat_2`| Fight-stance walk variant |
| KB_WalkBwd       | locomotion_walk         | `locomotion_walk_back_combat` | Fight-stance back walk loop |
| KB_WalkRight45   | locomotion_walk         | `locomotion_walk_combat_45R`  | Fight-stance diag forward-right walk |
| KB_WalkRight135  | locomotion_walk         | `locomotion_walk_combat_135R` | Fight-stance diag back-right walk |
| KB_WalkLeft45    | locomotion_walk         | `locomotion_walk_combat_45L`  | Fight-stance diag forward-left walk |
| KB_WalkLeft135   | locomotion_walk         | `locomotion_walk_combat_135L` | Fight-stance diag back-left walk |
| KB_TurnR_90      | movement_turn           | `movement_turn_R_90_combat`   | Combat in-place 90° right turn |
| KB_TurnL_90      | movement_turn           | `movement_turn_L_90_combat`   | Combat in-place 90° left turn |
| KB_TurnR_180     | movement_turn           | `movement_turn_R_180_combat`  | Combat in-place 180° right turn |
| KB_TurnL_180     | movement_turn           | `movement_turn_L_180_combat`  | Combat in-place 180° left turn |
| KB_Sidestep_L    | movement_sidestep       | `movement_sidestep_L`         | Quick combat sidestep left |
| KB_Sidestep_R    | movement_sidestep       | `movement_sidestep_R`         | Quick combat sidestep right |
| KB_SkipBwd_1     | movement_skip           | `movement_skip_back_1`        | Hop backward variant 1 |
| KB_SkipFwd_1     | movement_skip           | `movement_skip_fwd_1`         | Hop forward variant 1 |
| KB_SkipBwd_2     | movement_skip           | `movement_skip_back_2`        | Hop backward variant 2 |
| KB_SkipFwd_2     | movement_skip           | `movement_skip_fwd_2`         | Hop forward variant 2 |
| KB_Dodge_R       | defend_dodge            | `defend_dodge_R`              | Quick combat dodge right (lean / step) |
| KB_Dodge_L       | defend_dodge            | `defend_dodge_L`              | Quick combat dodge left |

### `KB_Punches.fbx` — hand strikes (`p_` = palm/snappy, `m_` = movement-heavy/muay-thai-style)

| Original                       | Category       | Proposed                           | Notes |
|--------------------------------|----------------|------------------------------------|-------|
| BindPose / tpose               | pose           | (skip)                             | |
| KB_p_Jab_L_1                   | attack_punch   | `attack_punch_jab_L`                | Snappy left jab |
| KB_p_Jab_R_1                   | attack_punch   | `attack_punch_jab_R`                | Snappy right jab (currently `attack_punch_jab` in catalog) |
| KB_p_OneTwo                    | attack_punch   | `attack_punch_combo_onetwo`         | Jab → cross combo |
| KB_p_OneTwoThree               | attack_punch   | `attack_punch_combo_onetwothree`    | Jab → cross → hook combo |
| KB_p_DoubleJab                 | attack_punch   | `attack_punch_combo_doublejab`      | Jab × 2 |
| KB_p_Hook_R                    | attack_punch   | `attack_punch_hook_R`               | Right hook (currently `attack_punch_hook`) |
| KB_p_Hook_L                    | attack_punch   | `attack_punch_hook_L`               | Left hook |
| KB_p_DoubleHooks               | attack_punch   | `attack_punch_combo_doublehooks`    | Hook × 2 (L+R) |
| KB_p_RabbitPunch_Loop          | attack_punch   | `attack_punch_rabbit_loop`          | Rabbit-punch flurry loop |
| KB_p_RabbitPunch_End           | attack_punch   | `attack_punch_rabbit_end`           | Flurry recovery |
| KB_p_Uppercut_L                | attack_punch   | `attack_punch_uppercut_L`           | Left uppercut |
| KB_p_Uppercut_R                | attack_punch   | `attack_punch_uppercut_R`           | Right uppercut (currently `attack_punch_uppercut`) |
| KB_m_Overhand_R                | attack_punch   | `attack_punch_overhand_R`           | Right overhand (steps in) |
| KB_m_Overhand_L                | attack_punch   | `attack_punch_overhand_L`           | Left overhand |
| KB_m_Jab_L                     | attack_punch   | `attack_punch_jab_step_L`           | Left jab with step |
| KB_m_Jab_R                     | attack_punch   | `attack_punch_jab_step_R`           | Right jab with step |
| KB_m_OneTwo                    | attack_punch   | `attack_punch_combo_onetwo_step`    | Stepping one-two |
| KB_m_Hook_L                    | attack_punch   | `attack_punch_hook_step_L`          | Stepping left hook |
| KB_m_Hook_R                    | attack_punch   | `attack_punch_hook_step_R`          | Stepping right hook |
| KB_m_Uppercut_L                | attack_punch   | `attack_punch_uppercut_step_L`      | Stepping left uppercut |
| KB_m_Uppercut_R                | attack_punch   | `attack_punch_uppercut_step_R`      | Stepping right uppercut |
| KB_m_Backswing_R               | attack_punch   | `attack_punch_backswing_R`          | Right back-swing punch |
| KB_m_Backswing_L               | attack_punch   | `attack_punch_backswing_L`          | Left back-swing |
| KB_m_Backelbow_R               | attack_punch   | `attack_punch_backelbow_R`          | Spinning back-elbow, right |
| KB_m_Backelbow_L               | attack_punch   | `attack_punch_backelbow_L`          | Spinning back-elbow, left |
| KB_m_Backelbow_Uppercut_R      | attack_punch   | `attack_punch_backelbow_uppercut_R` | Back-elbow into uppercut combo |
| KB_p_Elbow_R                   | attack_punch   | `attack_punch_elbow_R`              | Right elbow strike |
| KB_p_Elbow_L                   | attack_punch   | `attack_punch_elbow_L`              | Left elbow strike |
| KB_p_Elbow_Top_L               | attack_punch   | `attack_punch_elbow_top_L`          | Downward left elbow |
| KB_p_Elbow_Top_R               | attack_punch   | `attack_punch_elbow_top_R`          | Downward right elbow |
| KB_p_MidJab_L                  | attack_punch   | `attack_punch_jab_mid_L`            | Body-shot jab, left |
| KB_p_MidJab_R                  | attack_punch   | `attack_punch_jab_mid_R`            | Body-shot jab, right |
| KB_p_MidUppercut_R             | attack_punch   | `attack_punch_uppercut_mid_R`       | Body-shot uppercut, right |
| KB_p_MidUppercut_L             | attack_punch   | `attack_punch_uppercut_mid_L`       | Body-shot uppercut, left |
| KB_p_MidHook_R                 | attack_punch   | `attack_punch_hook_mid_R`           | Body-shot hook, right |
| KB_p_MidHook_L                 | attack_punch   | `attack_punch_hook_mid_L`           | Body-shot hook, left |
| KB_p_Jab_LR_combo              | attack_punch   | `attack_punch_combo_jab_LR`         | L-jab + R-jab combo |
| KB_p_Jab_LRL_combo             | attack_punch   | `attack_punch_combo_jab_LRL`        | L-R-L jab combo |
| KB_p_Jab_LL_combo              | attack_punch   | `attack_punch_combo_jab_LL`         | Double left jab combo |
| KB_m_Hook_LR_combo             | attack_punch   | `attack_punch_combo_hook_LR_step`   | Stepping L+R hook combo |
| KB_m_Jab_LR_combo              | attack_punch   | `attack_punch_combo_jab_LR_step`    | Stepping L+R jab combo |
| KB_m_ElbowRound_R              | attack_punch   | `attack_punch_elbow_round_R`        | Spinning round-elbow, right |
| KB_m_ElbowRound_L              | attack_punch   | `attack_punch_elbow_round_L`        | Spinning round-elbow, left |
| KB_m_BackfistRound_R           | attack_punch   | `attack_punch_backfist_round_R`     | Round backfist, right |
| KB_m_BackfistRound_L           | attack_punch   | `attack_punch_backfist_round_L`     | Round backfist, left |
| KB_m_BackfistRoundFar_R        | attack_punch   | `attack_punch_backfist_round_far_R` | Long-range round backfist |
| KB_m_ElbowRound_L2             | attack_punch   | `attack_punch_elbow_round_L_v2`     | Round-elbow variant 2, left |
| KB_m_BackfistRound_L2          | attack_punch   | `attack_punch_backfist_round_L_v2`  | Backfist variant 2, left |
| KB_m_Jab_RLhook_combo          | attack_punch   | `attack_punch_combo_jab_RLhook`     | Right jab + left hook combo |

### `KB_Kicks.fbx` — foot strikes

| Original                          | Category      | Proposed                         | Notes |
|-----------------------------------|---------------|----------------------------------|-------|
| BindPose / tpose                  | pose          | (skip)                           | |
| KB_m_MidKickStraight_R            | attack_kick   | `attack_kick_mid_straight_R`      | Stepping mid-front kick, right |
| KB_m_KickUppercut_R               | attack_kick   | `attack_kick_uppercut_R`          | Rising kick like an uppercut |
| KB_p_MidKickStraight_R            | attack_kick   | `attack_kick_mid_straight_p_R`    | Snappy mid-front kick, right |
| KB_p_MidKickFront_R               | attack_kick   | `attack_kick_mid_front_R`         | Front kick mid-height, right |
| KB_p_MidKickFront_L               | attack_kick   | `attack_kick_mid_front_L`         | Front kick mid-height, left |
| KB_p_HighKickStraight_R           | attack_kick   | `attack_kick_high_straight_R`     | High straight kick, right |
| KB_m_HighKickRound_R_1            | attack_kick   | `attack_kick_high_round_R`        | High roundhouse, right |
| KB_p_LowKick_R_1                  | attack_kick   | `attack_kick_low_R`               | Low kick, right (currently `attack_kick_low`) |
| KB_p_LowKick_R_2                  | attack_kick   | `attack_kick_low_R_v2`            | Low kick variant 2 |
| KB_p_MidKick_R_1                  | attack_kick   | `attack_kick_mid_R`               | Mid kick, right (used as `kick` alias) |
| KB_p_LowKick_L_1                  | attack_kick   | `attack_kick_low_L`               | Low kick, left |
| KB_p_MidKick_L_1                  | attack_kick   | `attack_kick_mid_L`               | Mid kick, left |
| KB_p_HighKick_R_1                 | attack_kick   | `attack_kick_high_R`              | High kick, right |
| KB_m_MidKickRoud_R_1              | attack_kick   | `attack_kick_mid_round_R`         | Stepping mid roundhouse, right |
| KB_m_HighKickRound_L_1            | attack_kick   | `attack_kick_high_round_L`        | High roundhouse, left |
| KB_m_MidKickRoud_L_1              | attack_kick   | `attack_kick_mid_round_L`         | Stepping mid roundhouse, left |
| KB_m_RoundhouseKickRight          | attack_kick   | `attack_kick_roundhouse_R`        | Full roundhouse, right |
| KB_m_KneeLeft                     | attack_kick   | `attack_kick_knee_L`              | Knee strike, left |
| KB_m_SideKickLeft                 | attack_kick   | `attack_kick_side_L`              | Side kick, left |
| KB_m_HighKick_R                   | attack_kick   | `attack_kick_high_step_R`         | High kick with step, right |
| KB_m_MidKick_L                    | attack_kick   | `attack_kick_mid_step_L`          | Mid kick with step, left |
| KB_m_MidKick_R                    | attack_kick   | `attack_kick_mid_step_R`          | Mid kick with step, right |
| KB_m_LowKickRound_R               | attack_kick   | `attack_kick_low_round_R`         | Low roundhouse, right |
| KB_m_KneeRight                    | attack_kick   | `attack_kick_knee_R`              | Knee strike, right |
| KB_m_LowKickL_Special             | attack_kick   | `attack_kick_low_special_L`       | Special variant low kick, left |
| KB_m_HighKick_R_2                 | attack_kick   | `attack_kick_high_step_R_v2`      | High kick variant 2 |
| KB_m_MidKick_R_2                  | attack_kick   | `attack_kick_mid_step_R_v2`       | Mid kick variant 2 |
| KB_m_MidRabbitKick_R              | attack_kick   | `attack_kick_rabbit_R`            | Rabbit-fast mid kick, right |
| KB_m_MidRabbitKick_R_combo        | attack_kick   | `attack_kick_combo_rabbit_R`      | Rabbit kick into combo |
| KB_m_MidKick_L_2                  | attack_kick   | `attack_kick_mid_step_L_v2`       | Mid kick variant 2, left |
| KB_m_MidKick_LL_2_combo           | attack_kick   | `attack_kick_combo_mid_LL`        | Double left mid kick combo |
| KB_m_Jab_RLhookRMidKick_combo     | attack_kick   | `attack_kick_combo_jab_RLhook_RMidKick` | Big mixed combo ending in mid kick |
| KB_MidKickStraightLong            | attack_kick   | `attack_kick_mid_straight_long`   | Long-reach mid front kick |
| KB_m_MidKickBack_R                | attack_kick   | `attack_kick_back_mid_R`          | Backward mid kick, right |
| KB_m_MidKickBack_L                | attack_kick   | `attack_kick_back_mid_L`          | Backward mid kick, left |
| KB_m_BackKick_R                   | attack_kick   | `attack_kick_back_R`              | Spinning back kick, right |
| KB_AxeKick                        | attack_kick   | `attack_kick_axe`                 | Axe kick (vertical down) — `attack_kick_crescent` in catalog |

### `KB_Specials.fbx` — projectiles, weapons, gimmick attacks

| Original                | Category          | Proposed                       | Notes |
|-------------------------|-------------------|--------------------------------|-------|
| BindPose / tpose        | pose              | (skip)                         | |
| KB_Projectile_1         | attack_special    | `attack_special_projectile_1`   | Throw projectile variant 1 |
| KB_Projectile_2         | attack_special    | `attack_special_projectile_2`   | Throw projectile variant 2 |
| KB_Projectile_3         | attack_special    | `attack_special_projectile_3`   | Throw projectile variant 3 |
| KB_Projectile_4         | attack_special    | `attack_special_projectile_4`   | Throw projectile variant 4 |
| KB_Projectile_5         | attack_special    | `attack_special_projectile_5`   | Throw projectile variant 5 |
| KB_Projectile_Up        | attack_special    | `attack_special_projectile_up`  | Upward-aimed projectile |
| KB_GroundAttack         | attack_special    | `attack_special_ground_slam`    | Slam ground / shockwave |
| KB_EyeLasers            | attack_special    | `attack_special_eye_lasers`     | Eye-beam emit pose |
| KB_KnifeThrow           | attack_special    | `attack_special_knife_throw`    | Throwing knife |
| KB_Gun                  | attack_special    | `attack_special_gun`            | Pistol fire pose |
| KB_Grenade              | attack_special    | `attack_special_grenade`        | Throw grenade |
| KB_Mine                 | attack_special    | `attack_special_mine`           | Plant a mine |
| KB_Superpunch           | attack_special    | `attack_special_superpunch`     | Telegraphed huge punch (currently `attack_power_strike`) |
| KB_MidKickStraightLong  | attack_kick       | `attack_kick_mid_straight_long_v2` | Same as in Kicks but variant in this FBX |
| KB_FootBazooka          | attack_special    | `attack_special_foot_bazooka`   | Joke clip — bazooka mounted on foot |
| KB_FootShotgun          | attack_special    | `attack_special_foot_shotgun`   | Joke clip — shotgun mounted on foot |
| KB_AxeKick              | attack_kick       | `attack_kick_axe_v2`            | Axe kick variant in this FBX |

### `KB_Blocks.fbx` — guards, parries, ducks

| Original                  | Category       | Proposed                           | Notes |
|---------------------------|----------------|------------------------------------|-------|
| BindPose / tpose          | pose           | (skip)                             | |
| KB_Block_Single           | defend_block   | `defend_block_single`               | Quick block (currently `defend_block_react`) |
| KB_Block_Start            | defend_block   | `defend_block_start`                | Block raise enter |
| KB_Block_Loop             | defend_block   | `defend_block_loop`                 | Held block loop (currently `defend_static_anchor`) |
| KB_Block_End              | defend_block   | `defend_block_end`                  | Block release exit |
| KB_MidBlock_L_Single      | defend_block   | `defend_block_mid_L`                | Mid-height block, left side (currently `defend_parry`) |
| KB_MidBlock_R_Single      | defend_block   | `defend_block_mid_R`                | Mid-height block, right side |
| KB_crouch_Block_Start     | defend_block   | `defend_block_crouch_start`         | Crouched block enter |
| KB_crouch_Block_Loop      | defend_block   | `defend_block_crouch_loop`          | Crouched block loop |
| KB_crouch_Block_End       | defend_block   | `defend_block_crouch_end`           | Crouched block exit |
| KB_crouch_Block_Single    | defend_block   | `defend_block_crouch_single`        | Crouched single block |
| KB_m_Duck_R               | defend_duck    | `defend_duck_R_step`                | Stepping duck right |
| KB_m_Duck_L               | defend_duck    | `defend_duck_L_step`                | Stepping duck left |
| KB_p_Duck                 | defend_duck    | `defend_duck`                       | In-place duck under (currently `defend_bob_weave`) |
| KB_m_Duck                 | defend_duck    | `defend_duck_step`                  | Stepping duck (no L/R) |

### `KB_Hits.fbx` — pain reactions (height × direction × intensity)

`p_` = palm-style (light tap impact), `m_` = movement-heavy (knockback impact). `Weak`, `Med`, `Stagger` ramp up severity.

| Original                       | Category        | Proposed                              | Notes |
|--------------------------------|-----------------|---------------------------------------|-------|
| BindPose / tpose               | pose            | (skip)                                | |
| KB_Hit_p_HighFront_Weak        | react_hit       | `react_hit_high_front_weak`            | Light hit, head, from front (currently `react_hit_light`) |
| KB_Hit_p_HighRight_Weak        | react_hit       | `react_hit_high_right_weak`            | Light hit, head, from right |
| KB_Hit_p_HighLeft_Weak         | react_hit       | `react_hit_high_left_weak`             | Light hit, head, from left |
| KB_Hit_p_HighUpper_Weak        | react_hit       | `react_hit_high_upper_weak`            | Light hit, head, from above |
| KB_Hit_m_HighFront_Weak        | react_hit       | `react_hit_high_front_step_weak`       | Light hit + step back, head, from front |
| KB_Hit_m_HighRight_Weak        | react_hit       | `react_hit_high_right_step_weak`       | Light hit + step, head, right |
| KB_Hit_m_HighLeft_Weak         | react_hit       | `react_hit_high_left_step_weak`        | Light hit + step, head, left |
| KB_Hit_p_MidFront_Weak         | react_hit       | `react_hit_mid_front_weak`             | Light hit, body, front |
| KB_Hit_p_MidLeft_Weak          | react_hit       | `react_hit_mid_left_weak`              | Light hit, body, left |
| KB_Hit_p_MidRight_Weak         | react_hit       | `react_hit_mid_right_weak`             | Light hit, body, right |
| KB_Hit_m_MidFront_Weak         | react_hit       | `react_hit_mid_front_step_weak`        | Light hit + step, body, front |
| KB_Hit_m_MidLeft_Weak          | react_hit       | `react_hit_mid_left_step_weak`         | Light hit + step, body, left |
| KB_Hit_m_MidRight_Weak         | react_hit       | `react_hit_mid_right_step_weak`        | Light hit + step, body, right |
| KB_Hit_p_LowLeft_Weak          | react_hit       | `react_hit_low_left_weak`              | Light hit, leg, left |
| KB_Hit_p_LowRight_Weak         | react_hit       | `react_hit_low_right_weak`             | Light hit, leg, right |
| KB_Hit_m_LowLeft_Weak          | react_hit       | `react_hit_low_left_step_weak`         | Light hit + step, leg, left |
| KB_Hit_m_LowRight_Weak         | react_hit       | `react_hit_low_right_step_weak`        | Light hit + step, leg, right |
| KB_Hit_m_HighFront_Med         | react_hit       | `react_hit_high_front_med`             | Medium hit, head, front (currently `react_hit_heavy`) |
| KB_Hit_m_HighRight_Med         | react_hit       | `react_hit_high_right_med`             | Medium hit, head, right |
| KB_Hit_m_HighLeft_Med          | react_hit       | `react_hit_high_left_med`              | Medium hit, head, left |
| KB_Hit_m_MidFront_Med          | react_hit       | `react_hit_mid_front_med`              | Medium hit, body, front |
| KB_Hit_m_MidLeft_Med           | react_hit       | `react_hit_mid_left_med`               | Medium hit, body, left |
| KB_Hit_m_MidRight_Med          | react_hit       | `react_hit_mid_right_med`              | Medium hit, body, right |
| KB_Hit_m_LowLeft_Med           | react_hit       | `react_hit_low_left_med`               | Medium hit, leg, left (currently `react_hit_sweep`) |
| KB_Hit_m_LowRight_Med          | react_hit       | `react_hit_low_right_med`              | Medium hit, leg, right |
| KB_Hit_m_MidTop_Med            | react_hit       | `react_hit_mid_top_med`                | Medium hit from above |
| KB_Hit_m_HighBack_Weak         | react_hit       | `react_hit_high_back_weak`             | Light hit, head, from behind |
| KB_Hit_m_MidBack_Med           | react_hit       | `react_hit_mid_back_med`               | Medium hit, body, from behind |
| KB_Hit_m_HighBack_Stagger      | react_stagger   | `react_stagger_high_back`              | Heavy stagger, head, from behind |
| KB_Hit_m_HighFront_Stagger     | react_stagger   | `react_stagger_high_front`             | Heavy stagger, head, from front |
| KB_Hit_m_MidFront_Stagger      | react_stagger   | `react_stagger_mid_front`              | Heavy stagger, body, from front |
| KB_Hit_m_MidLeft_Stagger       | react_stagger   | `react_stagger_mid_left`               | Heavy stagger, body, from left |
| KB_Hit_m_MidRight_Stagger      | react_stagger   | `react_stagger_mid_right`              | Heavy stagger, body, from right |
| KB_HitOnGroundBack             | react_groundhit | `react_groundhit_back`                 | Take a hit while lying on back |
| KB_HitOnGroundFront            | react_groundhit | `react_groundhit_front`                | Take a hit while lying face-down |

### `KB_KOs.fbx` — knockouts and recovery

| Original                | Category    | Proposed                | Notes |
|-------------------------|-------------|-------------------------|-------|
| BindPose / tpose        | pose        | (skip)                  | |
| KB_LayBack_Roll         | lay         | `lay_back_roll`          | Roll left-right while lying on back |
| KB_GetUpBack            | getup       | `getup_back`             | Stand up from supine (currently `getup_back`) |
| KB_GetUpBack180         | getup       | `getup_back_180`         | Stand up + 180° rotate (face was wrong way) |
| KB_GetUpFace            | getup       | `getup_face`             | Stand up from prone |
| KB_GetUpFace180         | getup       | `getup_face_180`         | Stand up + 180° rotate from prone |
| KB_HighKO_R             | react_ko    | `react_ko_high_R`        | KO from high right hit, falls right |
| KB_HighKO_L             | react_ko    | `react_ko_high_L`        | KO from high left hit, falls left |
| KB_MidKO_Back           | react_ko    | `react_ko_mid_back`      | KO from body hit, falls backward |
| KB_crouch_MidKO         | react_ko    | `react_ko_crouch_mid`    | KO from crouch, mid hit |
| KB_crouch_MidKO_Back    | react_ko    | `react_ko_crouch_mid_back` | KO from crouch, falls back |
| KB_KO_Head              | react_ko    | `react_ko_head`          | KO from head hit, classic boxing fall |
| KB_UpperKO              | react_ko    | `react_ko_upper`         | KO from uppercut, head snaps back |
| KB_UpperKO_2            | react_ko    | `react_ko_upper_v2`      | Uppercut KO variant 2 |
| KB_LowKO_R              | react_ko    | `react_ko_low_R`         | KO from low hit, falls right |
| KB_LowKO_L              | react_ko    | `react_ko_low_L`         | KO from low hit, falls left |
| KB_HighKO_Air           | react_ko    | `react_ko_high_air`      | Hit launches into air (uppercut launch — currently `react_launch_airborne`) |
| KB_MidKO_Powerful       | react_ko    | `react_ko_mid_powerful`  | KO from huge body hit, big knockback |
| KB_HighKO_Powerful      | react_ko    | `react_ko_high_powerful` | KO from huge head hit, big knockback (currently `react_knockdown_back`) |
| KB_UpperKO_Flip         | react_ko    | `react_ko_upper_flip`    | Uppercut KO with backflip |
| KB_MidKO                | react_ko    | `react_ko_mid`           | Generic body-hit KO (currently `death_collapse`) |
| KB_TopKO                | react_ko    | `react_ko_top`           | KO from top hit (axe-kick / hammer) |

### `KB_Crouched.fbx` — crouched combat (separate from MovementAnimsetPro crouches)

| Original                          | Category      | Proposed                              | Notes |
|-----------------------------------|---------------|---------------------------------------|-------|
| BindPose / tpose                  | pose          | (skip)                                | |
| KB_crouch_Start                   | crouch        | `crouch_combat_in`                     | Stand → crouched combat stance |
| KB_crouch_Idle                    | crouch        | `crouch_combat_idle`                   | Crouched combat idle |
| KB_crouch_End                     | crouch        | `crouch_combat_out`                    | Crouched combat → stand |
| KB_crouch_WalkFwd                 | crouch        | `crouch_combat_walk_fwd`               | Crouched combat fwd walk |
| KB_crouch_WalkBwd                 | crouch        | `crouch_combat_walk_back`              | Crouched combat back walk |
| KB_crouch_Sidestep_L              | crouch        | `crouch_combat_sidestep_L`             | Crouched sidestep left |
| KB_crouch_Sidestep_R              | crouch        | `crouch_combat_sidestep_R`             | Crouched sidestep right |
| KB_crouch_WalkRight45             | crouch        | `crouch_combat_walk_45R`               | Crouched diag walk forward-right |
| KB_crouch_WalkLeft135             | crouch        | `crouch_combat_walk_135L`              | Crouched diag walk back-left |
| KB_crouch_WalkLeft45              | crouch        | `crouch_combat_walk_45L`               | Crouched diag walk forward-left |
| KB_crouch_WalkRight135            | crouch        | `crouch_combat_walk_135R`              | Crouched diag walk back-right |
| KB_crouch_TurnR_90                | crouch        | `crouch_combat_turn_R_90`              | Crouched combat turn right 90° |
| KB_crouch_TurnL_90                | crouch        | `crouch_combat_turn_L_90`              | Crouched combat turn left 90° |
| KB_crouch_p_Jab_L                 | attack_punch  | `attack_punch_jab_crouch_L`            | Crouched left jab |
| KB_crouch_p_Jab_R                 | attack_punch  | `attack_punch_jab_crouch_R`            | Crouched right jab |
| KB_crouch_p_Uppercut_R            | attack_punch  | `attack_punch_uppercut_crouch_R`       | Crouched right uppercut |
| KB_crouch_p_Uppercut_L            | attack_punch  | `attack_punch_uppercut_crouch_L`       | Crouched left uppercut |
| KB_crouch_p_LowKickRound_R        | attack_kick   | `attack_kick_low_round_crouch_R`       | Crouched low roundhouse, right |
| KB_crouch_p_LowKick_L             | attack_kick   | `attack_kick_low_crouch_L`             | Crouched low kick, left |
| KB_crouch_m_LowKickRound_R        | attack_kick   | `attack_kick_low_round_crouch_step_R`  | Crouched stepping low roundhouse, R |
| KB_crouch_m_Uppercut_R            | attack_punch  | `attack_punch_uppercut_crouch_step_R`  | Crouched stepping uppercut, right |
| KB_crouch_m_Uppercut_R_2          | attack_punch  | `attack_punch_uppercut_crouch_step_R_v2` | Crouched stepping uppercut variant 2 |
| KB_crouch_Hit_p_MidFront_Weak     | react_hit     | `react_hit_crouch_mid_front_weak`      | Light body hit while crouched, front |
| KB_crouch_Hit_p_MidLeft_Weak      | react_hit     | `react_hit_crouch_mid_left_weak`       | Light body hit while crouched, left |
| KB_crouch_Hit_p_MidRight_Weak     | react_hit     | `react_hit_crouch_mid_right_weak`      | Light body hit while crouched, right |
| KB_crouch_Block_Start             | defend_block  | `defend_block_crouch_start_v2`         | (Duplicate of one in KB_Blocks; this is the combat-stance variant) |
| KB_crouch_Block_Loop              | defend_block  | `defend_block_crouch_loop_v2`          | |
| KB_crouch_Block_End               | defend_block  | `defend_block_crouch_end_v2`           | |
| KB_crouch_Block_Single            | defend_block  | `defend_block_crouch_single_v2`        | |

### `KB_Jumping.fbx` — combat jumping

| Original                        | Category | Proposed                       | Notes |
|---------------------------------|----------|--------------------------------|-------|
| BindPose / tpose                | pose     | (skip)                         | |
| KB_Jump_Realistic               | jump     | `jump_realistic`                | Realistic in-place jump |
| KB_JumpFwd_Realistic            | jump     | `jump_fwd_realistic`            | Realistic forward jump |
| KB_Jump_Realistic_RootJump      | jump     | `jump_realistic_rm`             | Same with root motion |
| KB_JumpFwd_Realistic_RootJump   | jump     | `jump_fwd_realistic_rm`         | Same with root motion |
| KB_Jump_Arcade_RootJump         | jump     | `jump_arcade_rm`                | Arcade-style high jump (RM) |
| KB_JumpFwd_Arcade_RootJump      | jump     | `jump_fwd_arcade_rm`            | Arcade fwd jump (RM) |
| KB_Jump_Start                   | jump     | `jump_combat_start`             | Combat-stance jump take-off |
| KB_Jump_Loop                    | jump     | `jump_combat_loop`              | Combat-stance airborne loop |
| KB_LandPrepare                  | jump     | `jump_land_prepare`             | Pre-land stance |
| KB_Land                         | jump     | `jump_land`                     | Combat-stance landing |
| KB_Jump_Loop2Kick               | attack_kick | `attack_kick_jump_to_kick`     | Airborne → kick transition |
| KB_JumpKick_Loop                | attack_kick | `attack_kick_jump_loop`        | Airborne kick loop |
| KB_JumpKick_2_Jump_Loop         | attack_kick | `attack_kick_jump_kick_v2`     | Airborne kick variant 2 |
| KB_JumpPunch                    | attack_punch | `attack_punch_jump`            | Airborne punch (drop punch) |

### `KB_Lay.fbx` — prone idle loops

| Original           | Category | Proposed             | Notes |
|--------------------|----------|----------------------|-------|
| BindPose / tpose   | pose     | (skip)               | |
| LayOnFront_Loop    | lay      | `lay_face_loop`       | Lying face-down idle |
| LayOnBack_Loop     | lay      | `lay_back_loop`       | Lying on back idle |

### `KB_Stretch.fbx`

| Original           | Category | Proposed       | Notes |
|--------------------|----------|----------------|-------|
| BindPose / tpose   | pose     | (skip)         | |
| KB_Stretch         | idle     | `idle_stretch`  | Stretching warm-up animation |

---

## Summary by category

| Category              | Approx clip count | Main FBX sources                                                    |
|-----------------------|-------------------|---------------------------------------------------------------------|
| `idle_*`              | 14                | KB_Movement (combat 1-6), MovementAnimsetPro_Idles (relaxed 1-6), KB_Stretch, KB_Movement_Fighting (Fists_Idle, transitions) |
| `locomotion_walk_*`   | ~40               | MovementAnimsetPro (relaxed), MovementAnimsetPro_Additionals (back/strafe), KB_Movement (combat-stance) |
| `locomotion_run_*`    | ~25               | MovementAnimsetPro (relaxed), _RunStrafeUpdate (back/strafe)        |
| `locomotion_sprint_*` | 2                 | MovementAnimsetPro, MovementAnimsetPro_SprintFixed                  |
| `movement_turn_*`     | 8                 | MovementAnimsetPro (relaxed), KB_Movement (combat)                  |
| `movement_sidestep_*` | 2                 | KB_Movement                                                         |
| `movement_skip_*`     | 4                 | KB_Movement                                                         |
| `movement_slide`      | 1                 | MovementAnimsetPro_SlideClimb                                       |
| `movement_climb_*`    | 2                 | MovementAnimsetPro_SlideClimb, _RunStrafeUpdate                     |
| `movement_vault_*`    | 1                 | MovementAnimsetPro_SlideClimb                                       |
| `crouch_*`            | ~50               | MovementAnimsetPro, _RunStrafeUpdate, KB_Crouched                   |
| `jump_*`              | ~30               | MovementAnimsetPro, KB_Jumping                                      |
| `attack_punch_*`      | ~70               | KB_Punches, KB_Crouched (crouched variants), _Fighting (fists), KB_Jumping |
| `attack_kick_*`       | ~45               | KB_Kicks, KB_Crouched, _Fighting, KB_Jumping                        |
| `attack_special_*`    | 14                | KB_Specials                                                         |
| `defend_block_*`      | 14                | KB_Blocks, KB_Crouched                                              |
| `defend_duck_*`       | 4                 | KB_Blocks                                                           |
| `defend_dodge_*`      | 2                 | KB_Movement                                                         |
| `react_hit_*`         | ~35               | KB_Hits, KB_Crouched, _Fighting                                     |
| `react_stagger_*`     | 5                 | KB_Hits, _Fighting                                                  |
| `react_groundhit_*`   | 2                 | KB_Hits                                                             |
| `react_ko_*`          | ~17               | KB_KOs, _Fighting (Knockdown, Death)                                |
| `getup_*`             | 6                 | KB_KOs, _RunStrafeUpdate                                            |
| `lay_*`               | 3                 | KB_Lay, KB_KOs (LayBack_Roll)                                       |
| `interact_*`          | ~30               | MovementAnimsetPro, _RunStrafeUpdate                                |
| `cinematic_*`         | 5                 | MovementAnimsetPro (DontKnow), _RunStrafeUpdate (KickTrashcan, patrols) |
| `pose_*`              | 22 BindPose/tpose | every Kubold FBX (skipped from live library)                        |

**Total live clips ~ 380**, of which ~320 are usable in-game (excluding `interact_*` props, `cinematic_*` demos, and utility `pose_*` poses).

## Notes / open questions

- **Variants vs `_v2` suffix.** When two clips animate the same logical move with subtle differences (e.g. `KB_p_LowKick_R_1` and `KB_p_LowKick_R_2`), I've used `_v2` for the second variant. If we want to support variant pools at runtime we can keep both; otherwise pick the better one and drop the suffix.
- **`p_` vs `m_` Kubold prefix.** Kubold's convention is `p_` for snappy in-place ("p" = pivot/punch?) and `m_` for movement-heavy stepping. I've translated this to `_step` suffix where it changes the move's character. If a move moves substantially, it's `_step`; if it's primarily in place, no suffix.
- **Crouched-but-fight-stance vs crouched-relaxed.** `KB_Crouched.fbx` is the combat-stance crouch (boxing stance, low). `MovementAnimsetPro` crouches are relaxed (sneaking style). I've split them with `crouch_combat_*` vs `crouch_*` to keep both available.
- **Currently-used catalog ids.** The current `Resources/Moves/` move-catalog ids are listed in the Notes column for each clip the catalog points at, so the renaming plan can be applied incrementally without breaking the engine.
- **Joke clips.** `KB_FootBazooka` and `KB_FootShotgun` are obvious gag animations. Kept named for completeness; we won't ship them.
