using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Legacy-Animator-driven melee ability with multi-strike support.
    /// Plays a punch/kick clip via the existing Animator Controller and
    /// orchestrates a combo as N strikes spaced by `strikeInterval`, each
    /// going through the resolver's per-strike pipeline.
    ///
    /// Mirrors `AnimancerMeleeAbility`'s shape so combos flurry the same
    /// way regardless of which playback path runs. Single-strike combos
    /// (strikeCount = 1) behave identically to the old single-swing.
    ///
    /// Per spec line 406: a defender dodging mid-combo aborts the remaining
    /// strikes (the attacker exits Execute early).
    /// </summary>
    public class MeleeStrikeAbility : ActiveAbility
    {
        private const float DefaultHoldDuration = 1.5f;
        private const float WindUpFraction      = 0.25f;
        private const float TailFraction        = 0.20f;

        private readonly bool _useKick;

        private int   _strikeCount;
        private float _strikeInterval;
        private int   _strikesFired;
        private float _phaseTimer;
        private bool  _aborted;
        private bool  _castBegan;

        public MeleeStrikeAbility(bool useKick) { _useKick = useKick; }

        public override void OnStart()
        {
            // Multi-strike from technique data; default 1 strike if technique
            // is null (e.g. basic attack with no combo).
            _strikeCount    = Ctx.Technique != null ? Mathf.Max(1, Ctx.Technique.strikeCount) : 1;
            _strikeInterval = Ctx.Technique != null && Ctx.Technique.strikeInterval > 0f
                ? Ctx.Technique.strikeInterval : 0.25f;

            _strikesFired = 0;
            _aborted      = false;
            _castBegan    = false;

            float hold = Ctx.Technique != null && Ctx.Technique.executionTime > 0f
                ? Ctx.Technique.executionTime : DefaultHoldDuration;
            _phaseTimer = hold * WindUpFraction;

            // Pay cast costs for skill techniques. Basic attacks (no Skill)
            // skip BeginCast — they're free.
            if (Ctx.Skill != null && Ctx.Technique != null)
            {
                var resolver = TerrainBattleManager.Instance?.CombatResolver;
                if (resolver != null)
                {
                    _castBegan = resolver.BeginCast(Ctx.Unit, Ctx.Technique);
                    if (!_castBegan) { _aborted = true; return; }
                }
            }

            // Play the legacy Animator clip.
            if (_useKick) Ctx.Anim.PlayKick(out _);
            else          Ctx.Anim.PlayAttack();

            TerrainBattleManager.Instance?.ExchangeCoordinator?
                .AdvancePhase(Ctx.Unit, ExchangePhase.WindUp);
        }

        public override bool OnTick(float dt)
        {
            if (_aborted) return true;

            _phaseTimer -= dt;
            if (_phaseTimer > 0f) return false;

            if (_strikesFired < _strikeCount)
            {
                FireOneStrike(_strikesFired);
                _strikesFired++;

                if (_aborted) return true;
                if (_strikesFired < _strikeCount)
                {
                    _phaseTimer = _strikeInterval;

                    // Wraith stance + Primed band — teleport to a new flank
                    // angle between strikes (Reference 1: Lee around Gaara).
                    // The visible loop: strike → fade → flank → strike again.
                    var stance = Ctx.Unit.GetComponent<UnitBrainAI>()?.Stance;
                    var speedSys = TerrainBattleManager.Instance?.Speed;
                    bool wraithFade = stance != null && stance.id == StanceId.Wraith
                        && speedSys != null && speedSys.GetSpeedBand(Ctx.Unit) == SpeedBand.Primed;
                    if (wraithFade && Ctx.Target != null)
                    {
                        var choreo = TerrainBattleManager.Instance?.Choreography;
                        if (choreo != null)
                        {
                            // 90° offsets per strike — front, right, back, left, front…
                            float angle = (_strikesFired * 90f) % 360f;
                            choreo.TeleportFlank(Ctx.Unit, Ctx.Target,
                                flankAngleDegrees: angle, orbitDistance: 1.8f, ghostCount: 4);
                        }
                    }

                    // Replay only on the first follow-up strike — keeps the
                    // visible cadence without resetting the clip every beat.
                    if (_strikesFired == 1)
                    {
                        if (_useKick) Ctx.Anim.PlayKick(out _);
                        else          Ctx.Anim.PlayAttack();
                    }
                    return false;
                }

                float hold = Ctx.Technique != null && Ctx.Technique.executionTime > 0f
                    ? Ctx.Technique.executionTime : DefaultHoldDuration;
                _phaseTimer = hold * TailFraction;
                return false;
            }

            return true;
        }

        public override void OnAnimationEvent(string eventName)
        {
            // Multi-strike combos derive cadence from strikeInterval, not
            // animation events. Events are intentionally ignored here.
        }

        private void FireOneStrike(int strikeIndex)
        {
            if (Ctx.Target == null || Ctx.Target.IsDead) { _aborted = true; return; }

            var mgr      = TerrainBattleManager.Instance;
            var resolver = mgr?.CombatResolver;
            if (mgr == null || resolver == null) { _aborted = true; return; }

            // Basic attack (no Skill) — single hit, no multi-strike.
            if (Ctx.Skill == null)
            {
                mgr.ResolveBasicAttack(Ctx.Unit, Ctx.Target);
                _aborted = true;
                return;
            }

            // Skill picked but no combo matched (Individual technique) — defer
            // to the existing chain runner. Cadence happens inside that
            // method (per below).
            if (Ctx.Technique == null || !Ctx.Technique.isCombo)
            {
                AbilityDamageHelper.Fire(Ctx);
                _aborted = true;
                return;
            }

            // Combo — multi-strike through the resolver's per-strike entry.
            bool dodged = resolver.ResolveStrike(Ctx.Unit, Ctx.Target,
                Ctx.Technique, strikeIndex, _strikeCount);
            if (dodged) _aborted = true;
        }
    }
}
