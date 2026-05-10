using System.Collections.Generic;
using UnityEngine;
using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Runtime registry of MoveDefinition assets. Built once at engine
    /// startup by scanning Resources/Moves/ (for SOs created via the
    /// editor menu) plus any Inspector-assigned moves on the engine
    /// component. Keyed by id and animationName.
    ///
    /// Brains and the engine resolve move references by id rather than
    /// holding direct asset pointers — this is important for the
    /// long-term plan: moves can be hot-swapped, stance pools come from
    /// data, and serialized save/replay strings reference ids.
    ///
    /// See MOVES_CATALOG.md "How code references these".
    /// </summary>
    public class MoveCatalog
    {
        private readonly Dictionary<string, MoveDefinition> _byId =
            new Dictionary<string, MoveDefinition>();

        private readonly HashSet<string> _missingLoggedOnce = new HashSet<string>();

        public int Count => _byId.Count;

        public MoveCatalog() { }

        /// <summary>
        /// Construct from a flat list (e.g. Inspector-assigned moves on
        /// BattleCombatEngine). Skips nulls and duplicates (later entries
        /// win). Also loads from Resources/Moves/ for assets created via
        /// the menu.
        /// </summary>
        public MoveCatalog(IEnumerable<MoveDefinition> assigned, bool loadFromResources = true)
        {
            if (loadFromResources)
                LoadFromResources();
            if (assigned != null)
            {
                foreach (var m in assigned) Register(m);
            }
        }

        public void LoadFromResources()
        {
            MoveDefinition[] loaded = Resources.LoadAll<MoveDefinition>("Moves");
            if (loaded == null) return;
            foreach (var m in loaded) Register(m);
        }

        public void Register(MoveDefinition move)
        {
            if (move == null || string.IsNullOrEmpty(move.id)) return;
            _byId[move.id] = move;
            // Also register by animationName so PlayMove("anim_xxx") lookups work.
            if (!string.IsNullOrEmpty(move.animationName) && move.animationName != move.id)
                _byId[move.animationName] = move;
        }

        public MoveDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId.TryGetValue(id, out var m)) return m;
            if (!_missingLoggedOnce.Contains(id))
            {
                _missingLoggedOnce.Add(id);
                Debug.Log($"[MoveCatalog] Missing move '{id}' — combat continues without it. " +
                          $"(Add a MoveDefinition asset under Resources/Moves/ or assign it via the engine.)");
            }
            return null;
        }

        /// <summary>True iff a move with this id has been registered.</summary>
        public bool Has(string id) => !string.IsNullOrEmpty(id) && _byId.ContainsKey(id);

        /// <summary>
        /// Diagnostic: dump all registered ids. Used by the editor menu
        /// "Dump Move Catalog" to verify what loaded.
        /// </summary>
        public IEnumerable<string> AllIds() => _byId.Keys;
    }

    /// <summary>
    /// Canonical move id constants. Code references these instead of raw
    /// strings so renames cause compile errors instead of silent misses.
    /// Names match MOVES_CATALOG.md exactly.
    /// </summary>
    public static class MoveIds
    {
        // Idle / locomotion
        public const string Idle              = "idle";
        public const string WalkForward       = "locomotion_walk_forward";
        public const string WalkBackward      = "locomotion_walk_backward";
        public const string Run               = "locomotion_run";
        public const string Backstep          = "locomotion_backstep";
        public const string OrbitClockwise    = "locomotion_orbit_clockwise";
        public const string OrbitCounter      = "locomotion_orbit_counterclockwise";
        public const string DashForward       = "locomotion_dash_forward";

        // Light attacks
        public const string PunchJab          = "attack_punch_jab";
        public const string PunchHook         = "attack_punch_hook";
        public const string PunchUppercut     = "attack_punch_uppercut";
        public const string KickLow           = "attack_kick_low";

        // Heavy attacks
        public const string PowerStrike       = "attack_power_strike";
        public const string KickAxe           = "attack_kick_axe";
        public const string KickCrescent      = "attack_kick_crescent";
        public const string SlamGround        = "attack_slam_ground";

        // Defensive
        public const string BlockIdle         = "defend_block_idle";
        public const string BlockReact        = "defend_block_react";
        public const string DodgeBack         = "defend_dodge_back";
        public const string DodgeSideLeft     = "defend_dodge_side_left";
        public const string DodgeSideRight    = "defend_dodge_side_right";
        public const string BobWeave          = "defend_bob_weave";
        public const string Parry             = "defend_parry";
        public const string StaticAnchor      = "defend_static_anchor";
        public const string FadeOut           = "defend_fade_out";

        // Reactions
        public const string HitLight          = "react_hit_light";
        public const string HitHeavy          = "react_hit_heavy";
        public const string HitSweep          = "react_hit_sweep";
        public const string LaunchAirborne    = "react_launch_airborne";
        public const string KnockdownBack     = "react_knockdown_back";
        public const string Stunned           = "react_stunned";
        public const string Dazed             = "react_dazed";
        public const string RecoilBlocked     = "react_recoil_blocked";

        // Casts
        public const string HandsignA         = "cast_handsign_a";
        public const string HandsignB         = "cast_handsign_b";
        public const string HandsignC         = "cast_handsign_c";
        public const string TripleSign        = "cast_triple_sign";

        // Death
        public const string DeathCollapse     = "death_collapse";
    }
}
