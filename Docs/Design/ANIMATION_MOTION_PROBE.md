# ANIMATION_MOTION_PROBE.md

> **Auto-generated.** Run `TacticalRPG → H2H → Probe Clip Motion` in the Unity Editor to refresh. Reports the effective root-motion delta of every Kubold AnimationClip under the current FBX import settings. Re-run after changing any import option.

**Threshold:** ≥ 0.10m forward/lateral, ≥ 0.15m vertical, or ≥ 5.0° rotation = **moves**. Otherwise **in-place**.

**Totals:** 459 clips probed → 174 in-place, 277 ground-motion, 8 airborne.

| FBX | Clip | Length (s) | Forward (m) | Lateral (m) | Vertical (m) | Rotation (°) | Verdict |
|---|---|---:|---:|---:|---:|---:|---|
| KB_Blocks | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_Block_End` | 0.42 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_Block_Loop` | 0.93 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_Block_Single` | 0.90 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_Block_Start` | 0.25 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_MidBlock_L_Single` | 0.98 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_MidBlock_R_Single` | 0.98 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_crouch_Block_End` | 0.50 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_crouch_Block_Loop` | 0.85 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_crouch_Block_Single` | 0.82 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_crouch_Block_Start` | 0.25 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `KB_m_Duck` | 0.98 | -0.42 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Blocks | `KB_m_Duck_L` | 0.82 | -0.56 | -0.30 | 0.00 | 0.0 | moves bwd |
| KB_Blocks | `KB_m_Duck_R` | 0.93 | -0.46 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Blocks | `KB_p_Duck` | 0.82 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Blocks | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Block_End` | 0.50 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Block_Loop` | 0.85 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Block_Single` | 0.82 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Block_Start` | 0.25 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_End` | 0.50 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Hit_p_MidFront_Weak` | 0.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Hit_p_MidLeft_Weak` | 0.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Hit_p_MidRight_Weak` | 0.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Idle` | 2.50 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_Sidestep_L` | 0.77 | 0.00 | -0.37 | 0.00 | 0.0 | moves left |
| KB_Crouched | `KB_crouch_Sidestep_R` | 0.77 | 0.00 | +0.37 | 0.00 | 0.0 | moves right |
| KB_Crouched | `KB_crouch_Start` | 0.45 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_TurnL_90` | 1.00 | 0.00 | 0.00 | 0.00 | -90.0 | rotates left |
| KB_Crouched | `KB_crouch_TurnR_90` | 1.00 | 0.00 | 0.00 | 0.00 | +90.0 | rotates right |
| KB_Crouched | `KB_crouch_WalkBwd` | 0.77 | -0.37 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Crouched | `KB_crouch_WalkFwd` | 0.77 | +0.37 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Crouched | `KB_crouch_WalkLeft135` | 0.77 | -0.29 | -0.29 | 0.00 | 0.0 | moves left |
| KB_Crouched | `KB_crouch_WalkLeft45` | 0.77 | +0.29 | -0.29 | 0.00 | 0.0 | moves fwd |
| KB_Crouched | `KB_crouch_WalkRight135` | 0.77 | -0.29 | +0.29 | 0.00 | 0.0 | moves bwd |
| KB_Crouched | `KB_crouch_WalkRight45` | 0.77 | +0.29 | +0.29 | 0.00 | 0.0 | moves right |
| KB_Crouched | `KB_crouch_m_LowKickRound_R` | 1.47 | +0.42 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Crouched | `KB_crouch_m_Uppercut_R` | 1.93 | +1.83 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Crouched | `KB_crouch_m_Uppercut_R_2` | 1.90 | +1.96 | +0.21 | 0.00 | 0.0 | moves fwd |
| KB_Crouched | `KB_crouch_p_Jab_L` | 0.62 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_p_Jab_R` | 0.77 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_p_LowKickRound_R` | 1.33 | +0.34 | -0.31 | 0.00 | 0.0 | moves fwd |
| KB_Crouched | `KB_crouch_p_LowKick_L` | 1.18 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_p_Uppercut_L` | 1.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `KB_crouch_p_Uppercut_R` | 1.12 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Crouched | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_HitOnGroundBack` | 0.92 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_HitOnGroundFront` | 0.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_m_HighBack_Stagger` | 2.00 | +1.54 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Hits | `KB_Hit_m_HighBack_Weak` | 1.17 | +0.44 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Hits | `KB_Hit_m_HighFront_Med` | 1.50 | -0.99 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_HighFront_Stagger` | 2.67 | -1.27 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_HighFront_Weak` | 1.00 | -0.34 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_HighLeft_Med` | 1.50 | -0.76 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_HighLeft_Weak` | 1.00 | -0.32 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_HighRight_Med` | 1.52 | -1.01 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_HighRight_Weak` | 1.03 | -0.39 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_LowLeft_Med` | 1.50 | -0.76 | -0.28 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_LowLeft_Weak` | 1.00 | -0.37 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_LowRight_Med` | 1.50 | -0.81 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_LowRight_Weak` | 1.00 | -0.56 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidBack_Med` | 1.50 | +0.93 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Hits | `KB_Hit_m_MidFront_Med` | 1.50 | -1.03 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidFront_Stagger` | 2.67 | -1.16 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidFront_Weak` | 1.17 | -0.37 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidLeft_Med` | 1.50 | -0.83 | -0.21 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidLeft_Stagger` | 2.67 | -1.07 | -0.28 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidLeft_Weak` | 1.17 | -0.35 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidRight_Med` | 1.50 | -0.87 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidRight_Stagger` | 2.67 | -0.99 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidRight_Weak` | 1.17 | -0.33 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_m_MidTop_Med` | 1.50 | -1.03 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Hits | `KB_Hit_p_HighFront_Weak` | 0.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_p_HighLeft_Weak` | 0.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_p_HighRight_Weak` | 0.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_p_HighUpper_Weak` | 1.20 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_p_LowLeft_Weak` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_p_LowRight_Weak` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_p_MidFront_Weak` | 0.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_p_MidLeft_Weak` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `KB_Hit_p_MidRight_Weak` | 0.93 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Hits | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_JumpFwd_Arcade_RootJump` | 1.58 | +2.11 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Jumping | `KB_JumpFwd_Realistic` | 1.43 | +1.09 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Jumping | `KB_JumpFwd_Realistic_RootJump` | 1.43 | +1.09 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Jumping | `KB_JumpKick_2_Jump_Loop` | 0.27 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_JumpKick_Loop` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_JumpPunch` | 0.62 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_Jump_Arcade_RootJump` | 1.40 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_Jump_Loop` | 0.75 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_Jump_Loop2Kick` | 0.20 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_Jump_Realistic` | 1.25 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_Jump_Realistic_RootJump` | 1.25 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_Jump_Start` | 0.47 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_Land` | 0.62 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `KB_LandPrepare` | 0.20 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Jumping | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_KOs | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_KOs | `KB_GetUpBack` | 1.67 | +0.25 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_KOs | `KB_GetUpBack180` | 1.67 | +0.17 | +0.07 | 0.00 | -180.0 | moves fwd+rotates |
| KB_KOs | `KB_GetUpFace` | 1.67 | -0.12 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_KOs | `KB_GetUpFace180` | 1.67 | -0.33 | -0.29 | 0.00 | +180.0 | moves bwd+rotates |
| KB_KOs | `KB_HighKO_Air` | 1.80 | -1.90 | -0.09 | 0.00 | 0.0 | moves bwd |
| KB_KOs | `KB_HighKO_L` | 1.95 | -1.33 | 0.00 | 0.00 | +6.0 | moves bwd+rotates |
| KB_KOs | `KB_HighKO_Powerful` | 1.88 | -4.23 | -0.52 | 0.00 | -9.0 | moves bwd+rotates |
| KB_KOs | `KB_HighKO_R` | 2.22 | -1.20 | -0.37 | 0.00 | +33.5 | moves bwd+rotates |
| KB_KOs | `KB_KO_Head` | 3.25 | -1.32 | -0.34 | 0.00 | +12.2 | moves bwd+rotates |
| KB_KOs | `KB_LayBack_Roll` | 1.73 | -1.28 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_KOs | `KB_LowKO_L` | 2.03 | +0.22 | +0.37 | 0.00 | +111.1 | moves right+rotates |
| KB_KOs | `KB_LowKO_R` | 1.93 | -0.63 | -0.69 | 0.00 | +104.8 | moves left+rotates |
| KB_KOs | `KB_MidKO` | 2.07 | +0.64 | +0.07 | 0.00 | +10.6 | moves fwd+rotates |
| KB_KOs | `KB_MidKO_Back` | 1.57 | +1.56 | +0.07 | 0.00 | +3.1 | moves fwd |
| KB_KOs | `KB_MidKO_Powerful` | 1.80 | -4.23 | -0.52 | 0.00 | -9.0 | moves bwd+rotates |
| KB_KOs | `KB_TopKO` | 1.95 | -1.54 | -0.03 | 0.00 | -2.4 | moves bwd |
| KB_KOs | `KB_UpperKO` | 2.20 | -1.35 | +0.01 | 0.00 | +1.3 | moves bwd |
| KB_KOs | `KB_UpperKO_2` | 1.95 | -1.54 | -0.03 | 0.00 | -2.4 | moves bwd |
| KB_KOs | `KB_UpperKO_Flip` | 1.93 | -1.90 | -0.08 | 0.00 | +0.8 | moves bwd |
| KB_KOs | `KB_crouch_MidKO` | 1.90 | -1.32 | -0.45 | 0.00 | -9.0 | moves bwd+rotates |
| KB_KOs | `KB_crouch_MidKO_Back` | 1.72 | +1.33 | +0.04 | 0.00 | +21.4 | moves fwd+rotates |
| KB_KOs | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `KB_AxeKick` | 2.00 | +1.31 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_MidKickStraightLong` | 2.08 | +2.43 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_BackKick_R` | 1.55 | +0.89 | +0.24 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_HighKickRound_L_1` | 2.37 | +1.29 | +0.45 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_HighKickRound_R_1` | 2.18 | +0.58 | -0.50 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_HighKick_R` | 1.47 | +0.36 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_HighKick_R_2` | 1.88 | +0.83 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_Jab_RLhookRMidKick_combo` | 1.57 | +0.07 | -0.28 | 0.00 | +20.9 | moves left+rotates |
| KB_Kicks | `KB_m_KickUppercut_R` | 1.63 | +1.24 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_KneeLeft` | 1.63 | +1.19 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_KneeRight` | 1.65 | +1.53 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_LowKickL_Special` | 2.27 | +0.82 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_LowKickRound_R` | 2.35 | +0.64 | -0.61 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidKickBack_L` | 1.70 | -0.34 | -0.07 | 0.00 | -180.0 | moves bwd+rotates |
| KB_Kicks | `KB_m_MidKickBack_R` | 1.42 | +0.30 | +0.11 | 0.00 | +180.0 | moves fwd+rotates |
| KB_Kicks | `KB_m_MidKickRoud_L_1` | 2.42 | +0.95 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidKickRoud_R_1` | 1.80 | +0.81 | -0.48 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidKickStraight_R` | 1.58 | +1.00 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidKick_L` | 2.28 | +0.34 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidKick_LL_2_combo` | 1.83 | -0.11 | -0.11 | 0.00 | +0.3 | moves left |
| KB_Kicks | `KB_m_MidKick_L_2` | 1.43 | +1.01 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidKick_R` | 1.50 | +0.43 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidKick_R_2` | 2.42 | +0.35 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidRabbitKick_R` | 1.53 | +0.55 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_MidRabbitKick_R_combo` | 1.58 | -0.16 | -0.06 | 0.00 | 0.0 | moves bwd |
| KB_Kicks | `KB_m_RoundhouseKickRight` | 1.60 | +0.87 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_m_SideKickLeft` | 1.57 | +1.05 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_p_HighKickStraight_R` | 1.40 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `KB_p_HighKick_R_1` | 1.57 | +0.18 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_p_LowKick_L_1` | 1.22 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `KB_p_LowKick_R_1` | 1.60 | +0.21 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `KB_p_LowKick_R_2` | 1.30 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `KB_p_MidKickFront_L` | 1.27 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `KB_p_MidKickFront_R` | 1.42 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `KB_p_MidKickStraight_R` | 1.50 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `KB_p_MidKick_L_1` | 2.28 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Kicks | `KB_p_MidKick_R_1` | 1.50 | +0.17 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Kicks | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Lay | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Lay | `LayOnBack_Loop` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Lay | `LayOnFront_Loop` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Lay | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Movement | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Movement | `KB_Dodge_L` | 1.32 | 0.00 | -1.63 | 0.00 | 0.0 | moves left |
| KB_Movement | `KB_Dodge_R` | 1.22 | 0.00 | +2.15 | 0.00 | 0.0 | moves right |
| KB_Movement | `KB_Idle_1` | 2.50 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Movement | `KB_Idle_2` | 1.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Movement | `KB_Idle_3` | 1.78 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Movement | `KB_Idle_4` | 3.68 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Movement | `KB_Idle_5` | 2.27 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Movement | `KB_Idle_6` | 2.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Movement | `KB_Sidestep_L` | 0.77 | 0.00 | -0.72 | 0.00 | 0.0 | moves left |
| KB_Movement | `KB_Sidestep_R` | 0.77 | 0.00 | +0.66 | 0.00 | 0.0 | moves right |
| KB_Movement | `KB_SkipBwd_1` | 1.60 | -2.02 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Movement | `KB_SkipBwd_2` | 1.63 | -2.21 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Movement | `KB_SkipFwd_1` | 1.62 | +2.15 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Movement | `KB_SkipFwd_2` | 1.73 | +1.90 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Movement | `KB_TurnL_180` | 1.17 | 0.00 | 0.00 | 0.00 | -179.8 | rotates left |
| KB_Movement | `KB_TurnL_90` | 1.00 | 0.00 | 0.00 | 0.00 | -90.0 | rotates left |
| KB_Movement | `KB_TurnR_180` | 1.17 | 0.00 | 0.00 | 0.00 | +180.0 | rotates right |
| KB_Movement | `KB_TurnR_90` | 1.00 | 0.00 | 0.00 | 0.00 | +90.0 | rotates right |
| KB_Movement | `KB_WalkBwd` | 0.77 | -0.56 | 0.00 | 0.00 | 0.0 | moves bwd |
| KB_Movement | `KB_WalkFwd1` | 0.77 | +0.46 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Movement | `KB_WalkFwd2` | 0.77 | +0.68 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Movement | `KB_WalkLeft135` | 0.77 | -0.46 | -0.46 | 0.00 | 0.0 | moves bwd |
| KB_Movement | `KB_WalkLeft45` | 0.77 | +0.46 | -0.46 | 0.00 | 0.0 | moves fwd |
| KB_Movement | `KB_WalkRight135` | 0.77 | -0.46 | +0.46 | 0.00 | 0.0 | moves bwd |
| KB_Movement | `KB_WalkRight45` | 0.77 | +0.46 | +0.46 | 0.00 | 0.0 | moves right |
| KB_Movement | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_m_Backelbow_L` | 1.53 | -0.03 | -0.55 | 0.00 | -180.0 | moves left+rotates |
| KB_Punches | `KB_m_Backelbow_R` | 1.17 | +0.21 | +0.47 | 0.00 | +180.0 | moves right+rotates |
| KB_Punches | `KB_m_Backelbow_Uppercut_R` | 1.42 | +0.33 | +0.43 | 0.00 | +180.0 | moves right+rotates |
| KB_Punches | `KB_m_BackfistRoundFar_R` | 2.23 | +2.45 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_BackfistRound_L` | 2.18 | +1.49 | +0.32 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_BackfistRound_L2` | 2.45 | +1.11 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_BackfistRound_R` | 1.45 | +1.49 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Backswing_L` | 1.85 | +0.34 | -0.19 | 0.00 | -180.0 | moves fwd+rotates |
| KB_Punches | `KB_m_Backswing_R` | 1.33 | +0.41 | +0.23 | 0.00 | +180.0 | moves fwd+rotates |
| KB_Punches | `KB_m_ElbowRound_L` | 1.77 | +1.60 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_ElbowRound_L2` | 1.77 | +0.80 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_ElbowRound_R` | 1.82 | +1.60 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Hook_L` | 1.27 | +0.84 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Hook_LR_combo` | 1.03 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_m_Hook_R` | 1.03 | +0.82 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Jab_L` | 0.90 | +0.74 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Jab_LR_combo` | 0.77 | +0.62 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Jab_R` | 0.93 | +0.73 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Jab_RLhook_combo` | 1.42 | +0.30 | -0.73 | 0.00 | +39.6 | moves left+rotates |
| KB_Punches | `KB_m_OneTwo` | 1.13 | +1.08 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Overhand_L` | 1.02 | +0.31 | -0.13 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Overhand_R` | 1.60 | +0.75 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Uppercut_L` | 1.17 | +0.65 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_m_Uppercut_R` | 1.20 | +0.65 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Punches | `KB_p_DoubleHooks` | 1.27 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_DoubleJab` | 1.15 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Elbow_L` | 0.92 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Elbow_R` | 0.95 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Elbow_Top_L` | 1.35 | +0.08 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Elbow_Top_R` | 1.35 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Hook_L` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Hook_R` | 1.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Jab_LL_combo` | 0.98 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Jab_LRL_combo` | 0.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Jab_LR_combo` | 0.70 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Jab_L_1` | 0.95 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Jab_R_1` | 0.77 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_MidHook_L` | 1.20 | +0.06 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_MidHook_R` | 1.40 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_MidJab_L` | 0.92 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_MidJab_R` | 0.88 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_MidUppercut_L` | 1.15 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_MidUppercut_R` | 1.12 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_OneTwo` | 0.85 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_OneTwoThree` | 1.13 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_RabbitPunch_End` | 0.90 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_RabbitPunch_Loop` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Uppercut_L` | 0.97 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `KB_p_Uppercut_R` | 1.10 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Punches | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_AxeKick` | 2.00 | +1.30 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `KB_EyeLasers` | 2.80 | +1.39 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `KB_FootBazooka` | 2.23 | +4.89 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `KB_FootShotgun` | 2.22 | +2.06 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `KB_Grenade` | 2.27 | +0.46 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `KB_GroundAttack` | 2.52 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_Gun` | 2.10 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_KnifeThrow` | 1.72 | +0.07 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_MidKickStraightLong` | 2.08 | +2.40 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `KB_Mine` | 1.93 | +0.22 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `KB_Projectile_1` | 1.33 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_Projectile_2` | 1.40 | +0.07 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_Projectile_3` | 1.83 | +0.65 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `KB_Projectile_4` | 1.47 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_Projectile_5` | 1.48 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_Projectile_Up` | 1.60 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Specials | `KB_Superpunch` | 3.65 | +1.77 | 0.00 | 0.00 | 0.0 | moves fwd |
| KB_Specials | `tpose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Stretch | `BindPose` | 0.02 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| KB_Stretch | `KB_Stretch` | 23.32 | +0.32 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `BindPose` | 0.03 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `ButtonPush_LH` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `ButtonPush_LH_90` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `ButtonPush_RH` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `ButtonPush_RH_90` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `Crouch2Idle` | 1.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `Crouch_Idle` | 6.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `Crouch_WalkFwdLoop` | 1.00 | +1.57 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `Crouch_WalkFwdStart` | 0.73 | +0.77 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `Crouch_WalkFwdStart180_L` | 0.87 | +0.27 | -0.36 | 0.00 | -179.8 | moves left+rotates |
| MovementAnimsetPro | `Crouch_WalkFwdStart180_R` | 1.57 | +0.89 | +0.29 | 0.00 | +180.0 | moves fwd+rotates |
| MovementAnimsetPro | `Crouch_WalkFwdStart90_L` | 0.77 | +0.49 | -0.18 | 0.00 | -90.0 | moves fwd+rotates |
| MovementAnimsetPro | `Crouch_WalkFwdStart90_R` | 1.23 | +1.17 | +0.18 | 0.00 | +90.0 | moves fwd+rotates |
| MovementAnimsetPro | `Crouch_WalkFwdStop_LU` | 0.73 | +0.29 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `Crouch_WalkFwdStop_RU` | 0.67 | +0.26 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `DontKnow` | 1.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `FallingLoop` | 3.33 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `FallingLoop_RootMotion` | 3.33 | 0.00 | 0.00 | -59.03 | 0.0 | airborne |
| MovementAnimsetPro | `Idle` | 6.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `Idle2Crouch` | 0.57 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `JumpIdleLand` | 1.03 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `JumpIdleLand2Walk` | 1.30 | +2.17 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `JumpIdleLandHard` | 1.57 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `JumpIdleStart` | 3.53 | 0.00 | 0.00 | -50.19 | 0.0 | airborne |
| MovementAnimsetPro | `JumpRunStart_LU` | 4.00 | +6.90 | 0.00 | -47.89 | 0.0 | airborne+moves |
| MovementAnimsetPro | `JumpRunStart_RU` | 4.00 | +9.23 | 0.00 | -62.38 | 0.0 | airborne+moves |
| MovementAnimsetPro | `JumpRun_LU_Land` | 1.27 | +0.75 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `JumpRun_LU_Land2Run` | 0.90 | +2.69 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `JumpRun_RU_Land` | 1.40 | +0.71 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `JumpRun_RU_Land2Run` | 0.43 | +1.12 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `JumpWalkStart_LU` | 4.00 | +4.62 | 0.00 | -74.92 | 0.0 | airborne+moves |
| MovementAnimsetPro | `JumpWalkStart_RU` | 4.00 | +5.39 | 0.00 | -59.73 | 0.0 | airborne+moves |
| MovementAnimsetPro | `JumpWalk_LU_Land` | 1.27 | +0.72 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `JumpWalk_LU_Land2Walk` | 0.90 | +1.81 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `JumpWalk_RU_Land` | 1.40 | +0.78 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `JumpWalk_RU_Land2Walk` | 0.57 | +1.14 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `Jump_place_ALL` | 2.27 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `Jump_place_ALL_short` | 1.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `Jump_run_lu_ALL` | 1.97 | +3.04 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `Jump_run_ru_ALL` | 1.97 | +3.00 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `Jump_walk_lu_ALL` | 1.50 | +3.63 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `Jump_walk_ru_ALL` | 1.17 | +2.96 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `KeypadUse_LH` | 2.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `KeypadUse_LH_90` | 2.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `KeypadUse_RH` | 2.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `KeypadUse_RH_90` | 2.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `PickUp_LH` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `PickUp_LH_90` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `PickUp_RH` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `PickUp_RH_90` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `PullLever_LH` | 2.33 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `PullLever_LH_90` | 2.33 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `PullLever_RH` | 2.33 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `PullLever_RH_90` | 2.33 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `RunArchLoop_L` | 0.77 | +2.17 | -0.31 | 0.00 | -71.1 | moves fwd+rotates |
| MovementAnimsetPro | `RunArchLoop_R` | 0.77 | +2.01 | +0.28 | 0.00 | +88.5 | moves fwd+rotates |
| MovementAnimsetPro | `RunFwdLoop` | 0.77 | +2.61 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `RunFwdLoop_LeanL` | 0.77 | +2.61 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `RunFwdLoop_LeanR` | 0.77 | +2.61 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `RunFwdStart` | 0.77 | +1.27 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `RunFwdStart180_L` | 0.87 | +0.88 | -0.26 | 0.00 | -179.8 | moves fwd+rotates |
| MovementAnimsetPro | `RunFwdStart180_R` | 1.13 | +1.82 | +0.28 | 0.00 | +180.0 | moves fwd+rotates |
| MovementAnimsetPro | `RunFwdStart90_L` | 0.67 | +0.81 | -0.18 | 0.00 | -90.0 | moves fwd+rotates |
| MovementAnimsetPro | `RunFwdStart90_R` | 1.00 | +1.88 | +0.20 | 0.00 | +90.0 | moves fwd+rotates |
| MovementAnimsetPro | `RunFwdStop_LU` | 1.27 | +1.44 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `RunFwdStop_RU` | 1.50 | +1.86 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `RunFwdTurn180_L_LU` | 0.97 | +1.53 | +0.35 | 0.00 | -179.8 | moves fwd+rotates |
| MovementAnimsetPro | `RunFwdTurn180_L_RU` | 1.07 | +1.33 | +0.14 | 0.00 | -179.8 | moves fwd+rotates |
| MovementAnimsetPro | `RunFwdTurn180_R_LU` | 1.43 | +2.63 | -0.04 | 0.00 | +180.0 | moves fwd+rotates |
| MovementAnimsetPro | `RunFwdTurn180_R_RU` | 1.53 | +3.05 | -0.24 | 0.00 | +180.0 | moves fwd+rotates |
| MovementAnimsetPro | `SprintFwdLoop` | 0.63 | +3.69 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `ThrowAway_LH` | 1.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `ThrowAway_RH` | 1.67 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro | `TurnLt180` | 1.67 | 0.00 | 0.00 | 0.00 | -179.8 | rotates left |
| MovementAnimsetPro | `TurnLt90_Loop` | 1.30 | 0.00 | 0.00 | 0.00 | -90.0 | rotates left |
| MovementAnimsetPro | `TurnRt180` | 1.67 | 0.00 | 0.00 | 0.00 | +180.0 | rotates right |
| MovementAnimsetPro | `TurnRt90_Loop` | 1.30 | 0.00 | 0.00 | 0.00 | +90.0 | rotates right |
| MovementAnimsetPro | `WalkArchLoop_L` | 1.00 | +1.07 | -0.09 | 0.00 | -180.0 | moves fwd+rotates |
| MovementAnimsetPro | `WalkArchLoop_R` | 1.00 | +1.09 | +0.09 | 0.00 | +180.0 | moves fwd+rotates |
| MovementAnimsetPro | `WalkFwdLoop` | 1.00 | +1.57 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `WalkFwdLoop_LeanL` | 1.00 | +1.57 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `WalkFwdLoop_LeanR` | 1.00 | +1.57 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `WalkFwdStart` | 0.77 | +0.66 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `WalkFwdStart180_L` | 0.90 | +0.47 | -0.15 | 0.00 | -179.8 | moves fwd+rotates |
| MovementAnimsetPro | `WalkFwdStart180_R` | 1.40 | +1.11 | +0.27 | 0.00 | +180.0 | moves fwd+rotates |
| MovementAnimsetPro | `WalkFwdStart90_L` | 0.83 | +0.62 | -0.15 | 0.00 | -90.0 | moves fwd+rotates |
| MovementAnimsetPro | `WalkFwdStart90_R` | 1.27 | +1.07 | +0.70 | 0.00 | +90.0 | moves fwd+rotates |
| MovementAnimsetPro | `WalkFwdStop_LU` | 1.33 | +0.63 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `WalkFwdStop_RU` | 1.53 | +0.68 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `WalkThroughDoor_LH` | 2.87 | +1.94 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro | `WalkThroughDoor_RH` | 2.87 | +1.86 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_Additionals | `RunFwdStart135_L` | 0.87 | +0.97 | -0.11 | 0.00 | -134.8 | moves fwd+rotates |
| MovementAnimsetPro_Additionals | `RunFwdStart135_R` | 1.13 | +1.90 | +0.06 | 0.00 | +135.0 | moves fwd+rotates |
| MovementAnimsetPro_Additionals | `StrafeLeft45Loop` | 1.00 | +1.11 | -1.11 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_Additionals | `StrafeLeftLoop` | 1.00 | 0.00 | -1.57 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_Additionals | `StrafeLeftStart` | 0.90 | 0.00 | -0.68 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_Additionals | `StrafeLeftStop_LU` | 1.30 | 0.00 | -0.28 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_Additionals | `StrafeLeftStop_RU` | 1.60 | 0.00 | -0.72 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_Additionals | `StrafeRight135Loop` | 1.00 | -1.11 | +1.11 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Additionals | `StrafeRightLoop` | 1.00 | 0.00 | +1.57 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_Additionals | `StrafeRightStart` | 1.20 | 0.00 | +1.18 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_Additionals | `StrafeRightStop_LU` | 1.17 | 0.00 | +0.89 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_Additionals | `StrafeRightStop_RU` | 1.00 | 0.00 | +0.59 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_Additionals | `WalkBwdLoop` | 1.00 | -1.57 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Additionals | `WalkBwdStart` | 0.83 | -0.71 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Additionals | `WalkBwdStop_LU` | 1.10 | -0.57 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Additionals | `WalkBwdStop_RU` | 1.20 | -0.33 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Additionals | `WalkFwdStart135_L` | 0.90 | +0.51 | -0.04 | 0.00 | -134.8 | moves fwd+rotates |
| MovementAnimsetPro_Additionals | `WalkFwdStart135_R` | 1.40 | +1.20 | +0.23 | 0.00 | +135.0 | moves fwd+rotates |
| MovementAnimsetPro_Fighting | `BindPose` | 0.03 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Fighting | `Death_1` | 2.47 | +0.91 | -0.41 | 0.00 | -51.8 | moves fwd+rotates |
| MovementAnimsetPro_Fighting | `Death_2` | 3.43 | -1.16 | +0.02 | 0.00 | +18.3 | moves bwd+rotates |
| MovementAnimsetPro_Fighting | `Fists2Idle` | 1.67 | -0.14 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Fighting | `Fists_Hit_Left` | 1.17 | -1.02 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Fighting | `Fists_Hit_Right` | 1.17 | -1.09 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Fighting | `Fists_Idle` | 3.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Fighting | `Fists_Kick_Front_L` | 0.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Fighting | `Fists_Kick_Front_Move_R` | 1.50 | +1.48 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_Fighting | `Fists_Punch_Heavy2Idle` | 2.67 | +1.82 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_Fighting | `Fists_Punch_L` | 0.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Fighting | `Fists_Punch_Move_L` | 0.83 | +0.56 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_Fighting | `Fists_Punch_Move_R` | 0.83 | +0.64 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_Fighting | `Fists_Punch_R` | 0.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Fighting | `Idle2Fists` | 0.67 | +0.14 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_Fighting | `Idle_Hit_Strong_Left` | 4.10 | -1.15 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Fighting | `Idle_Hit_Strong_Right` | 3.17 | -1.50 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_Fighting | `Idle_Knockdown_Front` | 2.17 | -1.31 | +0.08 | 0.00 | +18.6 | moves bwd+rotates |
| MovementAnimsetPro_Fighting | `Idle_Knockdown_Left` | 2.83 | -0.28 | -1.32 | 0.00 | -15.0 | moves left+rotates |
| MovementAnimsetPro_Fighting | `Idle_Knockdown_Right` | 1.60 | -0.09 | +0.92 | 0.00 | -22.6 | moves right+rotates |
| MovementAnimsetPro_Fighting | `Idle_Punch_Move_L` | 1.33 | +0.65 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_Idles | `BindPose` | 0.03 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Idles | `Idle2` | 7.37 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Idles | `Idle3` | 7.13 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Idles | `Idle4` | 12.40 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Idles | `Idle5` | 15.80 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_Idles | `Idle6` | 10.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `Climb1m` | 2.17 | +1.30 | 0.00 | +0.97 | 0.0 | airborne+moves |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch2Idle_new` | 1.00 | -0.12 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `CrouchLoop_new` | 5.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_Turn90L_new` | 1.00 | 0.00 | 0.00 | 0.00 | -90.3 | rotates left |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_Turn90R_new` | 1.00 | 0.00 | 0.00 | 0.00 | +90.6 | rotates right |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkBwdStart_new` | 1.03 | -1.07 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkBwdStop_LU_new` | 0.93 | -0.25 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkBwdStop_RU_new` | 1.07 | -0.24 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkBwd_new` | 0.80 | -1.39 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkFwdStart_new` | 0.60 | +0.30 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkFwdStop_LU_new` | 0.93 | +0.53 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkFwdStop_RU_new` | 1.07 | +0.06 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkFwd_new` | 0.90 | +1.39 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLt135_new` | 0.80 | -0.99 | -0.99 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLt45_new` | 0.90 | +0.99 | -0.99 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLtStart_new` | 0.93 | 0.00 | -0.99 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLtStop_LU_new` | 0.93 | 0.00 | -0.10 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLtStop_RU_new` | 1.07 | 0.00 | -0.05 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkLt_new` | 0.70 | 0.00 | -1.39 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRt135_new` | 0.80 | -0.99 | +0.99 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRt45_new` | 0.90 | +0.99 | +0.99 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRtStart_new` | 1.20 | 0.00 | +1.78 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRtStop_LU_new` | 0.93 | 0.00 | +0.31 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRtStop_RU_new` | 1.07 | 0.00 | +0.17 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_RunStrafeUpdate | `Crouch_WalkRt_new` | 0.73 | 0.00 | +1.39 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_RunStrafeUpdate | `GetUpFromBack` | 3.43 | +0.37 | -0.06 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `GetUpFromFace` | 2.40 | +0.36 | +0.06 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `HiddenButton` | 7.50 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `Idle2Crouch_new` | 0.67 | +0.12 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `KickTrashcan` | 3.07 | +0.34 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `PatrolFullCycleLoop` | 13.57 | +7.00 | +0.01 | 0.00 | +360.0 | moves fwd+rotates |
| MovementAnimsetPro_RunStrafeUpdate | `PatrolLoop` | 1.87 | +1.15 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `RunBwdLoop` | 0.77 | -1.62 | 0.00 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `RunLtLoop` | 0.77 | 0.00 | -1.58 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_RunStrafeUpdate | `RunRtLoop` | 0.77 | 0.00 | +1.65 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_RunStrafeUpdate | `RunStrafeLeft135Loop` | 0.77 | -1.14 | -1.14 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `RunStrafeLeft45Loop` | 0.77 | +1.87 | -1.87 | 0.00 | 0.0 | moves left |
| MovementAnimsetPro_RunStrafeUpdate | `RunStrafeRight135Loop` | 0.77 | -1.14 | +1.14 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `RunStrafeRight45Loop` | 0.77 | +1.87 | +1.87 | 0.00 | 0.0 | moves right |
| MovementAnimsetPro_RunStrafeUpdate | `SitChairEnd` | 2.33 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `SitChairLoop` | 5.07 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `SitChairStart` | 1.90 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `StrafeLeft135Loop` | 1.00 | -1.12 | -1.12 | 0.00 | 0.0 | moves bwd |
| MovementAnimsetPro_RunStrafeUpdate | `StrafeRight45Loop` | 1.00 | +1.12 | +1.12 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowCancel` | 2.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowEndClose` | 2.40 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowEndFar` | 2.90 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowLoop` | 0.83 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowSingle1` | 3.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `ThrowSingle2` | 3.00 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `Throw_Start` | 0.90 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_RunStrafeUpdate | `Turn180Surprised` | 2.57 | +0.12 | +0.47 | 0.00 | -180.0 | moves right+rotates |
| MovementAnimsetPro_SlideClimb | `BindPose` | 0.03 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_SlideClimb | `Climb2m` | 3.60 | +0.63 | 0.00 | +1.88 | 0.0 | airborne+moves |
| MovementAnimsetPro_SlideClimb | `Slide` | 2.40 | +6.20 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_SlideClimb | `Vault1m` | 2.90 | +3.10 | 0.00 | 0.00 | 0.0 | moves fwd |
| MovementAnimsetPro_SprintFixed | `BindPose` | 0.03 | 0.00 | 0.00 | 0.00 | 0.0 | in-place |
| MovementAnimsetPro_SprintFixed | `SprintFwdLoop` | 0.63 | +3.78 | 0.00 | 0.00 | 0.0 | moves fwd |
