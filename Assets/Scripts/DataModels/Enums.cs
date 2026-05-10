namespace TacticalRPG.DataModels
{
    public enum UnitTeam
    {
        Player,
        Enemy
    }

    public enum BattlePhase
    {
        NotStarted,
        Placement,
        Combat,
        Victory,
        Defeat
    }

    public enum BattleOutcome
    {
        None,
        Victory,
        Defeat
    }

    public enum BehaviorType
    {
        Aggressive,
        Defensive,
        Balanced
    }

    public enum IntentType
    {
        Wait,
        Move,
        BasicAttack,
        UseSkill,
        Retreat
    }

    public enum ActionType
    {
        Physical,
        Elemental,
        Support,
        Movement,
        OrbSummon
    }

    public enum ElementType
    {
        None,
        Fire,
        Water,
        Earth,
        Lightning,
        Wind
    }

    public enum TechniqueType
    {
        Attack,
        Heal,
        Buff,
        Debuff,
        Utility,
        Summon,
        OrbSummon,
        OrbRay,
        LaunchCombo  // Reference 3 — Naruto: launch → aerial flurry → far knockback
    }

    public enum TargetPattern
    {
        Single,
        AOE,
        Line,
        Self,
        AllAllies,
        AllEnemies
    }

    public enum EffectType
    {
        Buff,
        Debuff,
        Neutral
    }

    public enum ModifierSource
    {
        BaseStats,
        StatusEffect,
        Item,
        Passive,
        SkillTree,
        Behavior
    }

    public enum ModifierType
    {
        Additive,
        Multiplicative,
        Override
    }

    public enum GamePhase
    {
        PreBattle,
        Combat,
        Result
    }

    public enum CastType
    {
        Melee,
        Mobile,
        Rooted
    }

    public enum ActionMovementMode
    {
        InPlace,
        ShortLunge,
        ScriptedTravel,
        Dash,
        Leap
    }

    public enum CombatRole
    {
        Free,       // no exchange in progress — unit attacks freely
        Attacker,   // this unit swings first in the current exchange
        Defender    // this unit waits and braces while the attacker swings
    }

    public enum UnitCombatState
    {
        Backline,
        Engage,
        Decide,
        Melee,
        CastMobile,
        CastRooted,
        AttackDash,     // closing the last gap before a hit lands
        Execute,
        Recover,
        Stagger,        // briefly stunned after significant knockback
        Dodging,
        Stunned,        // CC: action suppressed, returns to Decide on expiry
        Repositioning,  // (Phase 5) deliberate movement to rebuild speed
        Airborne,       // launched into the air; can't act, takes 1.5x damage
        Dead
    }

    public enum SpeedBand
    {
        Sluggish,    //   0 - 20
        Engaged,     //  20 - 50
        Sharp,       //  50 - 70
        Primed       //  70 - 100
    }

    public enum MovementIntent
    {
        Hold,
        Close,
        Circle,
        Disengage,
        Dash
    }

    public enum CCEffectType
    {
        None,        // no CC (default for non-CC techniques)
        Stun,        // suppress all action
        Slow,        // multiplier on movement / execution
        Interrupt,   // cancel current cast (Phase 10+)
        Knockdown,   // ragdoll prone (Phase 10+, needs physics)
        Knockback    // formalized via BattleKnockbackSystem
    }

    public enum StanceId
    {
        Onslaught,    // Aggressive — spend liberally, low commit threshold, nearest enemy
        Tempest,      // Aggressive — build aggressively, big bursts, backline-first
        Stalwart,     // Defensive — hoard speed, commit only when primed, protect allies
        Tactician,    // Balanced — adaptive, lowest-HP target
        Wraith,       // Aggressive — hit-and-run, dodge-heavy, backline-first
        Sentinel,     // Defensive — build slowly, never spend below 30, marked target
        Conduit       // Caster-leaning — speed as dodge fund only, furthest enemy
    }

    public enum TargetPriority
    {
        Nearest,
        LowestHP,
        BacklineFirst,
        Furthest,
        AttackerOfAlly,
        Marked          // reserved — needs marking system
    }

    public enum ExchangePhase
    {
        None,           // no active exchange
        Initiation,     // roles assigned; attacker committing
        WindUp,         // brief moment before first hit; defender choosing reaction
        StrikeSequence, // combo playing out
        Resolution,     // last hit landed; hit-stop fired
        Beat,           // non-skippable pause (~0.3-0.5s)
        ReEvaluation    // brains tick; next exchange may begin
    }

    public enum DefenderResponse
    {
        Eat,            // no avoidance; takes the hit
        Dodge,          // dodge animation; no damage
        Block,          // 50% damage reduction; +5 energy
        Counter         // (Phase 8+ extension) take attacker role after strike
    }

    /// <summary>
    /// Attack identity tag — pairs with `ReactionType` via DefenderReactionTable.
    /// Each combo recipe declares one. Drives what the defender sees and how
    /// they react. See COMBAT_DESIGN "Spatial combat choreography → Per-phase
    /// exchange choreography" and the new "Paired reactions" addition.
    /// </summary>
    public enum AttackArchetype
    {
        Light,        // basic punch / quick combo — most attacks default here
        Heavy,        // single high-power hit (Power Strike, Tremor Kick etc)
        Launch,       // upper / leap-strike that lifts the defender (Crescent Kick)
        Flurry,       // multi-hit pressure (Earth Fist, Combo Strike — already multi-strike)
        Sweep,        // low kick — Tidal Sweep / Tremor / Thunder Sweep
        Sign,         // ranged elemental cast (Triple Sign, Geomagnetic, etc.)
        GuardBreak    // breaks blocking stance — reserved for Phase 12 skills
    }

    /// <summary>
    /// Defender reaction kind — paired with `AttackArchetype`. Returned by
    /// `DefenderReactionTable.Lookup` and orchestrated by the resolver.
    /// </summary>
    public enum ReactionType
    {
        Eat,            // default — take full damage with hit-react animation
        Block,          // existing block — 50% damage reduction
        Dodge,          // existing dodge ability — no damage, parabolic backflip
        BobWeave,       // in-place narrow window evasion (Sharp+ band)
        BraceBlock,     // Sentinel/Stalwart anchored 25%-damage absorb
        Airborne,       // get launched — defender forced to Airborne state, takes 1.5x dmg
        FadeOut,        // Wraith Primed — defender vanishes, reappears at Mid range
        Parry,          // brief mutual freeze + counter window
        Counter,        // defender takes attacker role for next strike (post-Parry)
        Recoil          // defender shielded against ranged signs — chip damage only
    }

    /// <summary>
    /// Distance bands to a unit's current target. See COMBAT_DESIGN
    /// "Spatial combat choreography → Range bands". Brain reads the band
    /// the same way it reads speed posture.
    /// </summary>
    public enum RangeBand
    {
        Far,        // > 8u — observe, cast Rooted, orbit
        Mid,        // 3-8u — closing/circling/courtship; exchanges initiate from here
        Close,      // 1-3u — strike sequences live here
        Locked      // < 1u — mutual contact / parry window; brief
    }

    // ─────────────────────────────────────────────────────────────────
    // Move-based engine enums (see COMBAT_DESIGN "Combat engine —
    // move-based, frame-data driven" and MOVES_CATALOG).
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Top-level kind a MoveDefinition belongs to. Matches the section
    /// headers in MOVES_CATALOG.md. Used by stance brains to filter the
    /// move pool ("pick a LightAttack from this stance's neutrals").
    /// </summary>
    public enum MoveCategory
    {
        Idle,
        Locomotion,
        LightAttack,
        HeavyAttack,
        Cast,           // sign / focus / mobile element cast
        BigCast,        // Triple Sign, Summoning — long startup, rooted
        EntitySpawn,    // walls, fire zones, traps, summons (reserved)
        Block,
        Dodge,
        Parry,
        HitReact,       // forced reaction — no startup
        Knockdown,
        Stun,
        Finisher,       // multi-segment cinematic move
        MobilityAbility,
        Death
    }

    /// <summary>
    /// The frame-window phase of a currently-executing move. Computed by
    /// the engine each tick from framesElapsed vs the move's frame data.
    /// Hit resolution and brain reaction reads use this.
    /// </summary>
    public enum MovePhase
    {
        Startup,        // committed to the move; no hit yet
        Active,         // hitbox / i-frames live
        Recovery,       // can't act; cancel-window may overlap the tail
        CancelWindow,   // last cancelWindowFrames of recovery — chained move accepted
        Done            // move finished; engine picks next this tick
    }

    /// <summary>
    /// What kind of reaction this attack invites in the defender. The
    /// `MoveReactionTable` maps (ReactionTag × stance × band) → defender
    /// move id. A move's tag is part of its move data, not its archetype.
    /// AttackArchetype is for skill resolution / damage; ReactionTag is
    /// for the paired-reaction lookup.
    /// </summary>
    public enum ReactionTag
    {
        None,           // not an attack (locomotion, idle, etc.)
        LightHit,       // jab / hook — short stagger pairs
        Heavy,          // power strike / heavy kick — heavy stagger
        Sweep,          // low kick — react_hit_sweep
        Launch,         // upper / crescent — forces airborne
        BigSign,        // Triple Sign / ranged cast — dodge-or-eat
        Knockdown,      // forces react_knockdown_*
        GuardBreak      // breaks block — reserved for later
    }

    /// <summary>
    /// How a move handles the unit's facing while playing.
    /// </summary>
    public enum FacingPolicy
    {
        FaceTarget,     // continuously rotate toward the current target
        Lock,           // freeze facing for the duration of the move
        Free            // unit can rotate freely (rarely used)
    }

    /// <summary>
    /// Outcome categories from one tick's hit-resolution check. Drives
    /// what reaction (if any) the defender enters and what feedback the
    /// engine logs.
    /// </summary>
    public enum HitResolution
    {
        OutOfRange,     // attacker's active frame swung but defender wasn't in cone
        Whiff,          // in range but defender was iframed/parried/superarmored
        Blocked,        // defender in active block — reduced damage
        FullHit,        // defender ate it — full damage, react move forced
        Trade           // both units' active frames overlap — both hits land
    }

    /// <summary>
    /// Phase a unit is in within the hand-to-hand combat cycle. See
    /// `Docs/Design/HAND_TO_HAND_COMBAT.md` "Combat phases — the cycle".
    /// Each phase has its own movement speed band, AI behavior, and
    /// allowed transitions; the orchestrator coordinates pairs of units
    /// when both reach Exchange together.
    /// </summary>
    public enum H2HPhase
    {
        NotEngaged,     // out of combat — idle / patrol
        Spotting,       // hostile detected, brief alert delay before pursuit
        Approaching,    // closing the gap at traversal speed
        Engaged,        // in engagement range, circling / posturing at combat speed
        Exchange,       // active strike / defense moment — orchestrator owns both units
        Separating      // breaking off after exchange, creating distance
    }
}
