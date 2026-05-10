#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Animancer;
using TacticalRPG.Systems.Combat;
using UnityEditor;
using UnityEngine;

namespace TacticalRPG.EditorTools
{
    /// <summary>
    /// One-shot bootstrapper for the full Kubold clip library: creates a
    /// `TransitionAsset` per logical id under
    /// `Assets/Data/AnimancerClips/Kubold/` and writes them into
    /// `Kubold_TestClipLibrary.asset`.
    ///
    /// Library ids match the move catalog (`Resources/Moves/`) where one
    /// exists — `idle`, `attack_punch_jab`, `react_hit_light`, etc. — so the
    /// move engine can later resolve `MoveDefinition.animationName` against
    /// this library directly. Extras (`walk_back`, `kick_roundhouse`,
    /// `getup_face`, `ko_top`) are descriptive ids for clips not yet in the
    /// catalog but useful for movement testing and the ragdoll recovery
    /// path.
    ///
    /// All clips come from MovementAnimsetPro / FightingAnimsetPro only.
    /// Re-runnable: existing `TransitionAsset`s are refreshed in place,
    /// existing library entries are re-pointed.
    /// </summary>
    public static class KuboldClipLibrarySetup
    {
        private const string OutputDir   = "Assets/Data/AnimancerClips/Kubold";
        private const string LibraryPath = "Assets/Data/AnimancerClips/Kubold/Kubold_TestClipLibrary.asset";

        private const string MovementFbx       = "Assets/Art/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx";
        private const string IdlesFbx          = "Assets/Art/MovementAnimsetPro/Animations/MovementAnimsetPro_Idles.fbx";
        private const string AdditionalsFbx    = "Assets/Art/MovementAnimsetPro/Animations/MovementAnimsetPro_Additionals.fbx";
        private const string RunStrafeFbx      = "Assets/Art/MovementAnimsetPro/Animations/MovementAnimsetPro_RunStrafeUpdate.fbx";
        private const string SlideClimbFbx     = "Assets/Art/MovementAnimsetPro/Animations/MovementAnimsetPro_SlideClimb.fbx";
        private const string SprintFixedFbx    = "Assets/Art/MovementAnimsetPro/Animations/MovementAnimsetPro_SprintFixed.fbx";
        private const string FightDir          = "Assets/Art/FightingAnimsetPro/Animations";

        private struct Spec
        {
            public string libraryId;
            public string fbx;
            public string clipName;
            public Spec(string id, string fbx, string clipName)
            { this.libraryId = id; this.fbx = fbx; this.clipName = clipName; }
        }

        // ── Spec list ────────────────────────────────────────────────
        // Order is logical, not strict — easier to scan + edit. Library ids
        // matching the move catalog are kept first within each section.
        private static readonly Spec[] Picks = new[]
        {
            // ─────────── Idle / locomotion (catalog ids) ───────────
            new Spec("idle",                    FightDir + "/KB_Movement.fbx", "KB_Idle_1"),
            new Spec("locomotion_walk_forward", FightDir + "/KB_Movement.fbx", "KB_WalkFwd1"),
            new Spec("locomotion_run",          MovementFbx,                    "RunFwdLoop"),
            // Older ids kept as aliases so existing prefab wiring still resolves.
            new Spec("walk_forward",            FightDir + "/KB_Movement.fbx", "KB_WalkFwd1"),
            new Spec("run_forward",             MovementFbx,                    "RunFwdLoop"),

            // ─────────── Idle variants ───────────
            new Spec("idle_combat_2",   FightDir + "/KB_Movement.fbx", "KB_Idle_2"),
            new Spec("idle_combat_3",   FightDir + "/KB_Movement.fbx", "KB_Idle_3"),
            new Spec("idle_combat_4",   FightDir + "/KB_Movement.fbx", "KB_Idle_4"),
            new Spec("idle_combat_5",   FightDir + "/KB_Movement.fbx", "KB_Idle_5"),
            new Spec("idle_combat_6",   FightDir + "/KB_Movement.fbx", "KB_Idle_6"),
            new Spec("idle_relaxed",    MovementFbx,                    "Idle"),
            new Spec("idle_relaxed_2",  IdlesFbx,                       "Idle2"),
            new Spec("idle_relaxed_3",  IdlesFbx,                       "Idle3"),
            new Spec("idle_relaxed_4",  IdlesFbx,                       "Idle4"),
            new Spec("idle_relaxed_5",  IdlesFbx,                       "Idle5"),
            new Spec("idle_relaxed_6",  IdlesFbx,                       "Idle6"),

            // ─────────── Directional walking (fight stance) ───────────
            new Spec("walk_back",         FightDir + "/KB_Movement.fbx", "KB_WalkBwd"),
            new Spec("walk_strafe_L",     FightDir + "/KB_Movement.fbx", "KB_WalkLeft45"),
            new Spec("walk_strafe_R",     FightDir + "/KB_Movement.fbx", "KB_WalkRight45"),
            new Spec("walk_back_left",    FightDir + "/KB_Movement.fbx", "KB_WalkLeft135"),
            new Spec("walk_back_right",   FightDir + "/KB_Movement.fbx", "KB_WalkRight135"),
            new Spec("sidestep_L",        FightDir + "/KB_Movement.fbx", "KB_Sidestep_L"),
            new Spec("sidestep_R",        FightDir + "/KB_Movement.fbx", "KB_Sidestep_R"),
            new Spec("skip_forward",      FightDir + "/KB_Movement.fbx", "KB_SkipFwd_1"),
            new Spec("skip_forward_2",    FightDir + "/KB_Movement.fbx", "KB_SkipFwd_2"),
            new Spec("skip_back",         FightDir + "/KB_Movement.fbx", "KB_SkipBwd_1"),
            new Spec("skip_back_2",       FightDir + "/KB_Movement.fbx", "KB_SkipBwd_2"),
            new Spec("turn_R_90",         FightDir + "/KB_Movement.fbx", "KB_TurnR_90"),
            new Spec("turn_L_90",         FightDir + "/KB_Movement.fbx", "KB_TurnL_90"),
            new Spec("turn_R_180",        FightDir + "/KB_Movement.fbx", "KB_TurnR_180"),
            new Spec("turn_L_180",        FightDir + "/KB_Movement.fbx", "KB_TurnL_180"),

            // ─────────── Run / sprint variants ───────────
            new Spec("sprint_forward",    MovementFbx, "SprintFwdLoop"),
            new Spec("run_lean_R",        MovementFbx, "RunFwdLoop_LeanR"),
            new Spec("run_lean_L",        MovementFbx, "RunFwdLoop_LeanL"),
            new Spec("run_arch_R",        MovementFbx, "RunArchLoop_R"),
            new Spec("run_arch_L",        MovementFbx, "RunArchLoop_L"),
            new Spec("walk_lean_R",       MovementFbx, "WalkFwdLoop_LeanR"),
            new Spec("walk_lean_L",       MovementFbx, "WalkFwdLoop_LeanL"),
            new Spec("walk_arch_R",       MovementFbx, "WalkArchLoop_R"),
            new Spec("walk_arch_L",       MovementFbx, "WalkArchLoop_L"),

            // ─────────── Standing locomotion (new — full speed × direction matrix) ───────────
            // Naming convention: loco_{speed}_{direction}_{role}
            //   speed:     walk | run | sprint
            //   direction: fwd | bwd | l | r | l45 | r45 | l135 | r135
            //   role:      loop | start | stop_l | stop_r
            // Combat-stance equivalents use the `combat_` prefix below.

            // Standing walk forward
            new Spec("loco_walk_fwd_loop",    MovementFbx, "WalkFwdLoop"),
            new Spec("loco_walk_fwd_start",   MovementFbx, "WalkFwdStart"),
            new Spec("loco_walk_fwd_stop_l",  MovementFbx, "WalkFwdStop_LU"),
            new Spec("loco_walk_fwd_stop_r",  MovementFbx, "WalkFwdStop_RU"),

            // Standing walk backward
            new Spec("loco_walk_bwd_loop",    AdditionalsFbx, "WalkBwdLoop"),
            new Spec("loco_walk_bwd_start",   AdditionalsFbx, "WalkBwdStart"),
            new Spec("loco_walk_bwd_stop_l",  AdditionalsFbx, "WalkBwdStop_LU"),
            new Spec("loco_walk_bwd_stop_r",  AdditionalsFbx, "WalkBwdStop_RU"),

            // Standing strafe left
            new Spec("loco_strafe_l_loop",    AdditionalsFbx, "StrafeLeftLoop"),
            new Spec("loco_strafe_l_start",   AdditionalsFbx, "StrafeLeftStart"),
            new Spec("loco_strafe_l_stop_l",  AdditionalsFbx, "StrafeLeftStop_LU"),
            new Spec("loco_strafe_l_stop_r",  AdditionalsFbx, "StrafeLeftStop_RU"),
            new Spec("loco_strafe_l45_loop",  AdditionalsFbx, "StrafeLeft45Loop"),
            new Spec("loco_strafe_l135_loop", RunStrafeFbx,   "StrafeLeft135Loop"),
            new Spec("loco_strafe_r45_loop",  RunStrafeFbx,   "StrafeRight45Loop"),

            // Standing strafe right
            new Spec("loco_strafe_r_loop",    AdditionalsFbx, "StrafeRightLoop"),
            new Spec("loco_strafe_r_start",   AdditionalsFbx, "StrafeRightStart"),
            new Spec("loco_strafe_r_stop_l",  AdditionalsFbx, "StrafeRightStop_LU"),
            new Spec("loco_strafe_r_stop_r",  AdditionalsFbx, "StrafeRightStop_RU"),
            new Spec("loco_strafe_r135_loop", AdditionalsFbx, "StrafeRight135Loop"),

            // Standing run (forward / backward / strafe)
            new Spec("loco_run_fwd_loop",        MovementFbx,   "RunFwdLoop"),
            new Spec("loco_run_fwd_start",       MovementFbx,   "RunFwdStart"),
            new Spec("loco_run_fwd_stop_l",      MovementFbx,   "RunFwdStop_LU"),
            new Spec("loco_run_fwd_stop_r",      MovementFbx,   "RunFwdStop_RU"),
            new Spec("loco_run_bwd_loop",        RunStrafeFbx,  "RunBwdLoop"),
            new Spec("loco_run_strafe_l_loop",   RunStrafeFbx,  "RunLtLoop"),
            new Spec("loco_run_strafe_r_loop",   RunStrafeFbx,  "RunRtLoop"),
            new Spec("loco_run_strafe_l45_loop", RunStrafeFbx,  "RunStrafeLeft45Loop"),
            new Spec("loco_run_strafe_r45_loop", RunStrafeFbx,  "RunStrafeRight45Loop"),
            new Spec("loco_run_strafe_l135_loop",RunStrafeFbx,  "RunStrafeLeft135Loop"),
            new Spec("loco_run_strafe_r135_loop",RunStrafeFbx,  "RunStrafeRight135Loop"),

            // Pivot-into-walk / -run starts (idle facing X → walk/run facing Y)
            new Spec("loco_walk_fwd_start_l90",  MovementFbx,    "WalkFwdStart90_L"),
            new Spec("loco_walk_fwd_start_r90",  MovementFbx,    "WalkFwdStart90_R"),
            new Spec("loco_walk_fwd_start_l135", AdditionalsFbx, "WalkFwdStart135_L"),
            new Spec("loco_walk_fwd_start_r135", AdditionalsFbx, "WalkFwdStart135_R"),
            new Spec("loco_walk_fwd_start_l180", MovementFbx,    "WalkFwdStart180_L"),
            new Spec("loco_walk_fwd_start_r180", MovementFbx,    "WalkFwdStart180_R"),
            new Spec("loco_run_fwd_start_l90",   MovementFbx,    "RunFwdStart90_L"),
            new Spec("loco_run_fwd_start_r90",   MovementFbx,    "RunFwdStart90_R"),
            new Spec("loco_run_fwd_start_l135",  AdditionalsFbx, "RunFwdStart135_L"),
            new Spec("loco_run_fwd_start_r135",  AdditionalsFbx, "RunFwdStart135_R"),
            new Spec("loco_run_fwd_start_l180",  MovementFbx,    "RunFwdStart180_L"),
            new Spec("loco_run_fwd_start_r180",  MovementFbx,    "RunFwdStart180_R"),
            new Spec("loco_run_fwd_turn_l180_l", MovementFbx,    "RunFwdTurn180_L_LU"),
            new Spec("loco_run_fwd_turn_l180_r", MovementFbx,    "RunFwdTurn180_L_RU"),
            new Spec("loco_run_fwd_turn_r180_l", MovementFbx,    "RunFwdTurn180_R_LU"),
            new Spec("loco_run_fwd_turn_r180_r", MovementFbx,    "RunFwdTurn180_R_RU"),

            // Standing in-place turns
            new Spec("loco_turn_l90",   MovementFbx, "TurnLt90_Loop"),
            new Spec("loco_turn_r90",   MovementFbx, "TurnRt90_Loop"),
            new Spec("loco_turn_l180",  MovementFbx, "TurnLt180"),
            new Spec("loco_turn_r180",  MovementFbx, "TurnRt180"),

            // Sprint (use the SprintFixed variant — superseded older one)
            new Spec("loco_sprint_loop", SprintFixedFbx, "SprintFwdLoop"),

            // Special: slide (sprint-stop / fast direction change finisher)
            new Spec("loco_slide", SlideClimbFbx, "Slide"),

            // ─────────── Combat-stance locomotion (engaged) ───────────
            // Mirror of the loco_* matrix but in fists-up stance.
            new Spec("combat_idle",            FightDir + "/KB_Movement.fbx", "KB_Idle_1"),
            new Spec("combat_walk_fwd_loop",   FightDir + "/KB_Movement.fbx", "KB_WalkFwd1"),
            new Spec("combat_walk_fwd_fast",   FightDir + "/KB_Movement.fbx", "KB_WalkFwd2"),
            new Spec("combat_walk_bwd_loop",   FightDir + "/KB_Movement.fbx", "KB_WalkBwd"),
            new Spec("combat_walk_l45_loop",   FightDir + "/KB_Movement.fbx", "KB_WalkLeft45"),
            new Spec("combat_walk_r45_loop",   FightDir + "/KB_Movement.fbx", "KB_WalkRight45"),
            new Spec("combat_walk_l135_loop",  FightDir + "/KB_Movement.fbx", "KB_WalkLeft135"),
            new Spec("combat_walk_r135_loop",  FightDir + "/KB_Movement.fbx", "KB_WalkRight135"),
            new Spec("combat_sidestep_l",      FightDir + "/KB_Movement.fbx", "KB_Sidestep_L"),
            new Spec("combat_sidestep_r",      FightDir + "/KB_Movement.fbx", "KB_Sidestep_R"),
            new Spec("combat_skip_fwd",        FightDir + "/KB_Movement.fbx", "KB_SkipFwd_1"),
            new Spec("combat_skip_bwd",        FightDir + "/KB_Movement.fbx", "KB_SkipBwd_1"),
            new Spec("combat_dodge_l",         FightDir + "/KB_Movement.fbx", "KB_Dodge_L"),
            new Spec("combat_dodge_r",         FightDir + "/KB_Movement.fbx", "KB_Dodge_R"),
            new Spec("combat_turn_l90",        FightDir + "/KB_Movement.fbx", "KB_TurnL_90"),
            new Spec("combat_turn_r90",        FightDir + "/KB_Movement.fbx", "KB_TurnR_90"),
            new Spec("combat_turn_l180",       FightDir + "/KB_Movement.fbx", "KB_TurnL_180"),
            new Spec("combat_turn_r180",       FightDir + "/KB_Movement.fbx", "KB_TurnR_180"),

            // ─────────── Punch attacks (catalog + extras) ───────────
            new Spec("attack_punch_jab",      FightDir + "/KB_Punches.fbx", "KB_p_Jab_R_1"),
            new Spec("attack_punch_hook",     FightDir + "/KB_Punches.fbx", "KB_p_Hook_R"),
            new Spec("attack_punch_uppercut", FightDir + "/KB_Punches.fbx", "KB_p_Uppercut_R"),
            new Spec("attack_power_strike",   FightDir + "/KB_Specials.fbx", "KB_Superpunch"),
            // Aliases used by the test scene's button panel.
            new Spec("punch",                 FightDir + "/KB_Punches.fbx", "KB_p_Jab_R_1"),
            // Extras
            new Spec("punch_jab_L",           FightDir + "/KB_Punches.fbx", "KB_p_Jab_L_1"),
            new Spec("punch_hook_L",          FightDir + "/KB_Punches.fbx", "KB_p_Hook_L"),
            new Spec("punch_uppercut_L",      FightDir + "/KB_Punches.fbx", "KB_p_Uppercut_L"),
            new Spec("punch_one_two",         FightDir + "/KB_Punches.fbx", "KB_p_OneTwo"),
            new Spec("punch_one_two_three",   FightDir + "/KB_Punches.fbx", "KB_p_OneTwoThree"),
            new Spec("punch_double_jab",      FightDir + "/KB_Punches.fbx", "KB_p_DoubleJab"),
            new Spec("punch_double_hooks",    FightDir + "/KB_Punches.fbx", "KB_p_DoubleHooks"),
            new Spec("punch_elbow_R",         FightDir + "/KB_Punches.fbx", "KB_p_Elbow_R"),
            new Spec("punch_elbow_L",         FightDir + "/KB_Punches.fbx", "KB_p_Elbow_L"),
            new Spec("punch_overhand_R",      FightDir + "/KB_Punches.fbx", "KB_m_Overhand_R"),
            new Spec("punch_overhand_L",      FightDir + "/KB_Punches.fbx", "KB_m_Overhand_L"),
            new Spec("punch_backfist_R",      FightDir + "/KB_Punches.fbx", "KB_m_BackfistRound_R"),
            new Spec("punch_backfist_L",      FightDir + "/KB_Punches.fbx", "KB_m_BackfistRound_L"),
            new Spec("punch_backswing_R",     FightDir + "/KB_Punches.fbx", "KB_m_Backswing_R"),
            new Spec("punch_rabbit_loop",     FightDir + "/KB_Punches.fbx", "KB_p_RabbitPunch_Loop"),
            new Spec("punch_rabbit_end",      FightDir + "/KB_Punches.fbx", "KB_p_RabbitPunch_End"),

            // ─────────── Kick attacks (catalog + extras) ───────────
            new Spec("attack_kick_low",       FightDir + "/KB_Kicks.fbx", "KB_p_LowKick_R_1"),
            new Spec("attack_kick_crescent",  FightDir + "/KB_Kicks.fbx", "KB_AxeKick"),
            // Alias for the test scene
            new Spec("kick",                  FightDir + "/KB_Kicks.fbx", "KB_p_MidKick_R_1"),
            // Extras
            new Spec("kick_low_L",            FightDir + "/KB_Kicks.fbx", "KB_p_LowKick_L_1"),
            new Spec("kick_mid_R",            FightDir + "/KB_Kicks.fbx", "KB_p_MidKick_R_1"),
            new Spec("kick_mid_L",            FightDir + "/KB_Kicks.fbx", "KB_p_MidKick_L_1"),
            new Spec("kick_high_R",           FightDir + "/KB_Kicks.fbx", "KB_p_HighKick_R_1"),
            new Spec("kick_high_straight",    FightDir + "/KB_Kicks.fbx", "KB_p_HighKickStraight_R"),
            new Spec("kick_roundhouse_R",     FightDir + "/KB_Kicks.fbx", "KB_m_RoundhouseKickRight"),
            new Spec("kick_high_round_R",     FightDir + "/KB_Kicks.fbx", "KB_m_HighKickRound_R_1"),
            new Spec("kick_high_round_L",     FightDir + "/KB_Kicks.fbx", "KB_m_HighKickRound_L_1"),
            new Spec("kick_axe",              FightDir + "/KB_Kicks.fbx", "KB_AxeKick"),
            new Spec("kick_back_R",           FightDir + "/KB_Kicks.fbx", "KB_m_BackKick_R"),
            new Spec("kick_side_L",           FightDir + "/KB_Kicks.fbx", "KB_m_SideKickLeft"),
            new Spec("kick_knee_R",           FightDir + "/KB_Kicks.fbx", "KB_m_KneeRight"),
            new Spec("kick_knee_L",           FightDir + "/KB_Kicks.fbx", "KB_m_KneeLeft"),
            new Spec("kick_uppercut_R",       FightDir + "/KB_Kicks.fbx", "KB_m_KickUppercut_R"),
            new Spec("kick_mid_back_R",       FightDir + "/KB_Kicks.fbx", "KB_m_MidKickBack_R"),
            new Spec("kick_low_round_R",      FightDir + "/KB_Kicks.fbx", "KB_m_LowKickRound_R"),

            // ─────────── Specials ───────────
            new Spec("special_ground_attack", FightDir + "/KB_Specials.fbx", "KB_GroundAttack"),
            new Spec("special_superpunch",    FightDir + "/KB_Specials.fbx", "KB_Superpunch"),

            // ─────────── Defense (catalog + extras) ───────────
            new Spec("defend_block_react",       FightDir + "/KB_Blocks.fbx",   "KB_Block_Single"),
            new Spec("defend_dodge_back",        FightDir + "/KB_Movement.fbx", "KB_SkipBwd_1"),
            new Spec("defend_dodge_side_left",   FightDir + "/KB_Movement.fbx", "KB_Sidestep_L"),
            new Spec("defend_dodge_side_right",  FightDir + "/KB_Movement.fbx", "KB_Sidestep_R"),
            new Spec("defend_bob_weave",         FightDir + "/KB_Blocks.fbx",   "KB_p_Duck"),
            new Spec("defend_parry",             FightDir + "/KB_Blocks.fbx",   "KB_MidBlock_R_Single"),
            new Spec("defend_static_anchor",     FightDir + "/KB_Blocks.fbx",   "KB_Block_Loop"),
            new Spec("defend_fade_out",          FightDir + "/KB_Movement.fbx", "KB_Dodge_L"),
            // Aliases for the test scene
            new Spec("block",                    FightDir + "/KB_Blocks.fbx",   "KB_Block_Single"),
            new Spec("dodge",                    FightDir + "/KB_Movement.fbx", "KB_Dodge_R"),
            // Extras
            new Spec("block_start",     FightDir + "/KB_Blocks.fbx", "KB_Block_Start"),
            new Spec("block_loop",      FightDir + "/KB_Blocks.fbx", "KB_Block_Loop"),
            new Spec("block_end",       FightDir + "/KB_Blocks.fbx", "KB_Block_End"),
            new Spec("block_mid_R",     FightDir + "/KB_Blocks.fbx", "KB_MidBlock_R_Single"),
            new Spec("block_mid_L",     FightDir + "/KB_Blocks.fbx", "KB_MidBlock_L_Single"),
            new Spec("block_crouch_start", FightDir + "/KB_Blocks.fbx", "KB_crouch_Block_Start"),
            new Spec("block_crouch_loop",  FightDir + "/KB_Blocks.fbx", "KB_crouch_Block_Loop"),
            new Spec("block_crouch_end",   FightDir + "/KB_Blocks.fbx", "KB_crouch_Block_End"),
            new Spec("duck_R",          FightDir + "/KB_Blocks.fbx", "KB_m_Duck_R"),
            new Spec("duck_L",          FightDir + "/KB_Blocks.fbx", "KB_m_Duck_L"),
            new Spec("duck_parry",      FightDir + "/KB_Blocks.fbx", "KB_p_Duck"),
            new Spec("dodge_R",         FightDir + "/KB_Movement.fbx", "KB_Dodge_R"),
            new Spec("dodge_L",         FightDir + "/KB_Movement.fbx", "KB_Dodge_L"),

            // ─────────── Hit reactions (catalog + extras) ───────────
            new Spec("react_hit_light",       FightDir + "/KB_Hits.fbx", "KB_Hit_p_HighFront_Weak"),
            new Spec("react_hit_heavy",       FightDir + "/KB_Hits.fbx", "KB_Hit_m_HighFront_Med"),
            new Spec("react_hit_sweep",       FightDir + "/KB_Hits.fbx", "KB_Hit_m_LowLeft_Med"),
            new Spec("react_launch_airborne", FightDir + "/KB_KOs.fbx",  "KB_HighKO_Air"),
            new Spec("react_knockdown_back",  FightDir + "/KB_KOs.fbx",  "KB_HighKO_Powerful"),
            // Alias
            new Spec("hit_react",             FightDir + "/KB_Hits.fbx", "KB_Hit_m_HighFront_Med"),
            // Extras (full hit-react spread for AI debug + variation pools)
            new Spec("hit_high_front_weak",     FightDir + "/KB_Hits.fbx", "KB_Hit_p_HighFront_Weak"),
            new Spec("hit_high_left_weak",      FightDir + "/KB_Hits.fbx", "KB_Hit_p_HighLeft_Weak"),
            new Spec("hit_high_right_weak",     FightDir + "/KB_Hits.fbx", "KB_Hit_p_HighRight_Weak"),
            new Spec("hit_high_upper_weak",     FightDir + "/KB_Hits.fbx", "KB_Hit_p_HighUpper_Weak"),
            new Spec("hit_mid_front_weak",      FightDir + "/KB_Hits.fbx", "KB_Hit_p_MidFront_Weak"),
            new Spec("hit_mid_left_weak",       FightDir + "/KB_Hits.fbx", "KB_Hit_p_MidLeft_Weak"),
            new Spec("hit_mid_right_weak",      FightDir + "/KB_Hits.fbx", "KB_Hit_p_MidRight_Weak"),
            new Spec("hit_low_left_weak",       FightDir + "/KB_Hits.fbx", "KB_Hit_p_LowLeft_Weak"),
            new Spec("hit_low_right_weak",      FightDir + "/KB_Hits.fbx", "KB_Hit_p_LowRight_Weak"),
            new Spec("hit_high_front_med",      FightDir + "/KB_Hits.fbx", "KB_Hit_m_HighFront_Med"),
            new Spec("hit_high_left_med",       FightDir + "/KB_Hits.fbx", "KB_Hit_m_HighLeft_Med"),
            new Spec("hit_high_right_med",      FightDir + "/KB_Hits.fbx", "KB_Hit_m_HighRight_Med"),
            new Spec("hit_mid_front_med",       FightDir + "/KB_Hits.fbx", "KB_Hit_m_MidFront_Med"),
            new Spec("hit_mid_left_med",        FightDir + "/KB_Hits.fbx", "KB_Hit_m_MidLeft_Med"),
            new Spec("hit_mid_right_med",       FightDir + "/KB_Hits.fbx", "KB_Hit_m_MidRight_Med"),
            new Spec("hit_low_left_med",        FightDir + "/KB_Hits.fbx", "KB_Hit_m_LowLeft_Med"),
            new Spec("hit_low_right_med",       FightDir + "/KB_Hits.fbx", "KB_Hit_m_LowRight_Med"),
            new Spec("hit_mid_back_med",        FightDir + "/KB_Hits.fbx", "KB_Hit_m_MidBack_Med"),
            new Spec("hit_high_back_weak",      FightDir + "/KB_Hits.fbx", "KB_Hit_m_HighBack_Weak"),
            new Spec("hit_high_front_stagger",  FightDir + "/KB_Hits.fbx", "KB_Hit_m_HighFront_Stagger"),
            new Spec("hit_high_back_stagger",   FightDir + "/KB_Hits.fbx", "KB_Hit_m_HighBack_Stagger"),
            new Spec("hit_mid_front_stagger",   FightDir + "/KB_Hits.fbx", "KB_Hit_m_MidFront_Stagger"),
            new Spec("hit_mid_left_stagger",    FightDir + "/KB_Hits.fbx", "KB_Hit_m_MidLeft_Stagger"),
            new Spec("hit_mid_right_stagger",   FightDir + "/KB_Hits.fbx", "KB_Hit_m_MidRight_Stagger"),
            new Spec("hit_ground_back",         FightDir + "/KB_Hits.fbx", "KB_HitOnGroundBack"),
            new Spec("hit_ground_front",        FightDir + "/KB_Hits.fbx", "KB_HitOnGroundFront"),

            // ─────────── KO / death (catalog + extras) ───────────
            new Spec("death_collapse",   FightDir + "/KB_KOs.fbx", "KB_MidKO"),
            new Spec("ko_high_R",        FightDir + "/KB_KOs.fbx", "KB_HighKO_R"),
            new Spec("ko_high_L",        FightDir + "/KB_KOs.fbx", "KB_HighKO_L"),
            new Spec("ko_low_R",         FightDir + "/KB_KOs.fbx", "KB_LowKO_R"),
            new Spec("ko_low_L",         FightDir + "/KB_KOs.fbx", "KB_LowKO_L"),
            new Spec("ko_mid",           FightDir + "/KB_KOs.fbx", "KB_MidKO"),
            new Spec("ko_mid_back",      FightDir + "/KB_KOs.fbx", "KB_MidKO_Back"),
            new Spec("ko_mid_powerful",  FightDir + "/KB_KOs.fbx", "KB_MidKO_Powerful"),
            new Spec("ko_high_powerful", FightDir + "/KB_KOs.fbx", "KB_HighKO_Powerful"),
            new Spec("ko_high_air",      FightDir + "/KB_KOs.fbx", "KB_HighKO_Air"),
            new Spec("ko_head",          FightDir + "/KB_KOs.fbx", "KB_KO_Head"),
            new Spec("ko_upper",         FightDir + "/KB_KOs.fbx", "KB_UpperKO"),
            new Spec("ko_upper_2",       FightDir + "/KB_KOs.fbx", "KB_UpperKO_2"),
            new Spec("ko_upper_flip",    FightDir + "/KB_KOs.fbx", "KB_UpperKO_Flip"),
            new Spec("ko_top",           FightDir + "/KB_KOs.fbx", "KB_TopKO"),
            new Spec("ko_crouch_mid",    FightDir + "/KB_KOs.fbx", "KB_crouch_MidKO"),
            new Spec("ko_crouch_mid_back",FightDir + "/KB_KOs.fbx","KB_crouch_MidKO_Back"),

            // ─────────── Get-up (ragdoll recovery path) ───────────
            new Spec("getup_back",      FightDir + "/KB_KOs.fbx", "KB_GetUpBack"),
            new Spec("getup_back_180",  FightDir + "/KB_KOs.fbx", "KB_GetUpBack180"),
            new Spec("getup_face",      FightDir + "/KB_KOs.fbx", "KB_GetUpFace"),
            new Spec("getup_face_180", FightDir + "/KB_KOs.fbx", "KB_GetUpFace180"),
            new Spec("layback_roll",    FightDir + "/KB_KOs.fbx", "KB_LayBack_Roll"),
        };

        [MenuItem("TacticalRPG/Kubold/Setup Test Clip Library")]
        public static void Run()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/AnimancerClips");
            EnsureFolder(OutputDir);

            var library = AssetDatabase.LoadAssetAtPath<BattleAnimancerClipLibrary>(LibraryPath);
            bool createdLibrary = false;
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<BattleAnimancerClipLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
                createdLibrary = true;
            }

            int wired = 0, missingClip = 0;
            // Cache TransitionAssets per (fbx,clipName) so two ids pointing at
            // the same clip share one asset on disk.
            var transitionCache = new Dictionary<string, TransitionAsset>();
            var seenIds = new HashSet<string>();

            foreach (var spec in Picks)
            {
                if (!seenIds.Add(spec.libraryId))
                {
                    Debug.LogWarning($"[KuboldClipLibrarySetup] Duplicate library id '{spec.libraryId}' — keeping first definition.");
                    continue;
                }

                string cacheKey = spec.fbx + "::" + spec.clipName;
                if (!transitionCache.TryGetValue(cacheKey, out var transition))
                {
                    var clip = LoadClip(spec.fbx, spec.clipName);
                    if (clip == null)
                    {
                        Debug.LogError($"[KuboldClipLibrarySetup] Could not find clip '{spec.clipName}' in {spec.fbx}");
                        missingClip++;
                        continue;
                    }

                    string transPath = OutputDir + "/Trans_" + Sanitize(spec.clipName) + ".asset";
                    transition = AssetDatabase.LoadAssetAtPath<TransitionAsset>(transPath);
                    if (transition == null)
                    {
                        transition = ScriptableObject.CreateInstance<TransitionAsset>();
                        transition.Transition = new ClipTransition { Clip = clip };
                        AssetDatabase.CreateAsset(transition, transPath);
                    }
                    else
                    {
                        if (transition.Transition is ClipTransition existing)
                            existing.Clip = clip;
                        else
                            transition.Transition = new ClipTransition { Clip = clip };
                        EditorUtility.SetDirty(transition);
                    }
                    transitionCache[cacheKey] = transition;
                }

                library.Set(spec.libraryId, transition);
                wired++;
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[KuboldClipLibrarySetup] {(createdLibrary ? "Created" : "Updated")} {LibraryPath}. " +
                      $"Wired {wired}/{Picks.Length} ids ({transitionCache.Count} unique TransitionAssets)" +
                      (missingClip > 0 ? $", {missingClip} missing clips." : "."));
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName)
        {
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
            foreach (var sub in subAssets)
            {
                if (sub is AnimationClip ac && ac.name == clipName) return ac;
            }
            return null;
        }

        private static string Sanitize(string s)
        {
            // Trim invalid filename characters; collapse spaces.
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            return sb.ToString();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
