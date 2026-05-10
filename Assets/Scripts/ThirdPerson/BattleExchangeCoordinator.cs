using UnityEngine;
using TacticalRPG.DataModels;
using System.Collections.Generic;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Watches units that are in Melee range of each other and assigns Attacker / Defender
    /// roles before any animation plays. This prevents both units firing attack triggers
    /// on the same frame and cancelling each other's animations.
    ///
    /// One exchange = Attacker swings → both Recover → roles cleared → next exchange decided.
    /// </summary>
    public class BattleExchangeCoordinator : MonoBehaviour
    {
        // Active exchanges keyed by the attacker
        private readonly Dictionary<TerrainBattleUnit, ExchangeRecord> _exchanges
            = new Dictionary<TerrainBattleUnit, ExchangeRecord>();

        // Tracks which unit attacked last per pair, so roles swap each exchange
        private readonly Dictionary<int, TerrainBattleUnit> _lastAttacker
            = new Dictionary<int, TerrainBattleUnit>();

        // Phase 8 — beat-locks per pair. After an exchange resolves, a brief
        // non-skippable pause prevents a new exchange from starting between
        // the same units. Map keyed by PairKey, value is unscaled-time when
        // the lock expires.
        private readonly Dictionary<int, float> _beatLockExpiry
            = new Dictionary<int, float>();

        [Header("Exchange Beat (Phase 8)")]
        [Tooltip("Non-skippable pause after an exchange resolves. Both units' brains can tick, but no new exchange will start until the beat expires.")]
        [SerializeField] private float beatDurationSeconds = 0.4f;

        [Header("Speed economy (Phase 8 / spec lines 412-419)")]
        [SerializeField] private float speedRefundPerLandedStrike = 3f;
        [SerializeField] private float dodgeSpeedCost              = 5f;

        // Produces a stable pair key regardless of argument order
        private static int PairKey(TerrainBattleUnit a, TerrainBattleUnit b)
        {
            int x = a.GetHashCode(), y = b.GetHashCode();
            return x < y ? x * 100003 + y : y * 100003 + x;
        }

        private class ExchangeRecord
        {
            public TerrainBattleUnit attacker;
            public TerrainBattleUnit defender;
            public ExchangePhase    phase;
        }

        public ExchangePhase GetExchangePhase(TerrainBattleUnit attacker)
        {
            if (attacker == null) return ExchangePhase.None;
            return _exchanges.TryGetValue(attacker, out var rec) ? rec.phase : ExchangePhase.None;
        }

        private void Update()
        {
            var toRemove = new List<TerrainBattleUnit>();
            foreach (var kv in _exchanges)
            {
                var rec = kv.Value;
                // Only clean up on death — not on role changes, those are handled explicitly
                bool expired = rec.attacker == null || rec.attacker.IsDead
                            || rec.defender == null || rec.defender.IsDead;
                if (expired) toRemove.Add(kv.Key);
            }
            foreach (var key in toRemove)
            {
                if (_exchanges.TryGetValue(key, out var rec))
                {
                    if (rec.attacker != null && rec.attacker.CombatRole != CombatRole.Free)
                        rec.attacker.SetCombatRole(CombatRole.Free);
                    if (rec.defender != null && rec.defender.CombatRole != CombatRole.Free)
                        rec.defender.SetCombatRole(CombatRole.Free);
                }
                _exchanges.Remove(key);
            }
        }

        // How much initiative one attack costs the attacker
        private const float AttackInitiativeCost = 1f;

        // When the initiative gap between a unit and its target drops to this threshold,
        // the target earns the right to become the next attacker.
        private const float InitiativeFlipThreshold = -2f;

        /// <summary>
        /// Called by TerrainBattleUnit when it's in melee range and ready to attack.
        /// Returns the role this unit should play in the next exchange.
        /// If the unit is already assigned a role, that role is returned unchanged.
        ///
        /// Role selection is driven by initiative:
        ///   - Whoever has strictly MORE initiative attacks.
        ///   - If equal, the unit that didn't attack last attacks now.
        ///   - A unit may never attack while IsAnimating (mid-clip).
        /// </summary>
        public CombatRole RequestRole(TerrainBattleUnit requester)
        {
            // Already locked into an exchange — don't change role mid-animation
            if (requester.CombatRole != CombatRole.Free)
                return requester.CombatRole;

            TerrainBattleUnit target = requester.CurrentTarget;
            if (target == null || target.IsDead)
                return CombatRole.Free;

            // Block new exchanges while either unit is mid-animation
            if (requester.IsAnimating) return CombatRole.Free;
            if (target.IsAnimating)   return CombatRole.Free;

            // Phase 8 — respect the beat lock between this pair. Even with both
            // units free and intent to engage, the recent post-exchange pause
            // suppresses a new exchange until the beat expires.
            int pairKeyEarly = PairKey(requester, target);
            if (_beatLockExpiry.TryGetValue(pairKeyEarly, out float expiry)
                && Time.unscaledTime < expiry)
            {
                return CombatRole.Free;
            }

            // Both units free and idle — decide attacker by initiative
            if (target.CombatRole == CombatRole.Free)
            {
                TerrainBattleUnit attacker;
                TerrainBattleUnit defender;

                float diff = requester.Initiative - target.Initiative;

                if (Mathf.Abs(diff) < 0.01f)
                {
                    // Equal initiative — fall back to last-attacker swap
                    int pairKey = PairKey(requester, target);
                    if (_lastAttacker.TryGetValue(pairKey, out var last))
                    {
                        attacker = (last == target) ? requester : target;
                    }
                    else
                    {
                        attacker = requester.GetHashCode() > target.GetHashCode() ? requester : target;
                    }
                }
                else
                {
                    // Higher initiative attacks
                    attacker = diff > 0f ? requester : target;
                }

                defender = (attacker == requester) ? target : requester;

                attacker.SetCombatRole(CombatRole.Attacker);
                defender.SetCombatRole(CombatRole.Defender);

                _exchanges[attacker] = new ExchangeRecord
                {
                    attacker = attacker,
                    defender = defender,
                    phase    = ExchangePhase.Initiation
                };
                _lastAttacker[PairKey(attacker, defender)] = attacker;

                CombatLogger.Instance?.Log(CombatLogger.CAT_EXCHANGE,
                    attacker.Unit?.DisplayName ?? "?",
                    $"ATTACKER (ini={attacker.Initiative:F1}) vs {defender.Unit?.DisplayName ?? "?"} DEFENDER (ini={defender.Initiative:F1})");

                // Sanity check — both units must be idle when assigned
                if (attacker.IsAnimating)
                    CombatLogger.Instance?.Warn(attacker.Unit?.DisplayName ?? "?", "Assigned Attacker role while IsAnimating!");
                if (defender.IsAnimating)
                    CombatLogger.Instance?.Warn(defender.Unit?.DisplayName ?? "?", "Assigned Defender role while IsAnimating!");

                return requester == attacker ? CombatRole.Attacker : CombatRole.Defender;
            }

            return CombatRole.Free;
        }

        /// <summary>Phase 8 — advance phase, called by brain/resolver as the exchange progresses.</summary>
        public void AdvancePhase(TerrainBattleUnit attacker, ExchangePhase newPhase)
        {
            if (attacker == null) return;
            if (!_exchanges.TryGetValue(attacker, out var rec)) return;
            if (rec.phase == newPhase) return;
            rec.phase = newPhase;
            CombatLogger.Instance?.Log(CombatLogger.CAT_EXCHANGE,
                attacker.Unit?.DisplayName ?? "?",
                $"phase → {newPhase}");
        }

        /// <summary>
        /// Phase 8 — refund attacker speed for a strike that landed (per spec).
        /// Bleed defender speed for a successful dodge.
        /// </summary>
        public void OnStrikeResolved(TerrainBattleUnit attacker, TerrainBattleUnit defender, DefenderResponse response)
        {
            var speedSys = TerrainBattleManager.Instance?.Speed;
            if (speedSys == null) return;

            if (response == DefenderResponse.Eat || response == DefenderResponse.Block)
                speedSys.GainSpeed(attacker, speedRefundPerLandedStrike);
            else if (response == DefenderResponse.Dodge && defender != null)
                speedSys.Shatter(defender, dodgeSpeedCost);
        }

        /// <summary>
        /// Called when an attacker finishes their Execute+Recover cycle.
        /// Spends initiative for the attack and frees both units.
        /// The unit with higher initiative will become attacker again on the next RequestRole call.
        /// </summary>
        public void OnAttackerRecoveryComplete(TerrainBattleUnit attacker)
        {
            if (!_exchanges.TryGetValue(attacker, out var rec)) return;

            TerrainBattleUnit oldDefender = rec.defender;
            _exchanges.Remove(attacker);

            if (attacker.IsDead || oldDefender.IsDead)
            {
                attacker.SetCombatRole(CombatRole.Free);
                if (oldDefender != null) oldDefender.SetCombatRole(CombatRole.Free);
                return;
            }

            // Charge the attacker for the attack they just completed
            attacker.SpendInitiative(AttackInitiativeCost);

            // Free both — next frame RequestRole will re-evaluate initiative and assign fresh roles
            attacker.SetCombatRole(CombatRole.Free);
            oldDefender.SetCombatRole(CombatRole.Free);

            // Phase 8 — set the beat lock so neither unit can re-engage this
            // pair until the non-skippable pause expires. Other pairs are
            // unaffected (per-pair locking, not battlefield-wide).
            if (beatDurationSeconds > 0f)
            {
                int pairKey = PairKey(attacker, oldDefender);
                _beatLockExpiry[pairKey] = Time.unscaledTime + beatDurationSeconds;
            }

            // Phase 14 — force separation. Both units physically backstep
            // away from each other so the next exchange has visible space
            // to dash through. Distance + duration tuned so the brain's
            // chase doesn't catch up before the backstep finishes (brain
            // yields movement while a primitive is active).
            var choreo = TerrainBattleManager.Instance?.Choreography;
            if (choreo != null)
            {
                choreo.BackstepAway(attacker, oldDefender, distance: 1.0f, durationSec: 0.45f);
                choreo.BackstepAway(oldDefender, attacker, distance: 1.0f, durationSec: 0.45f);
            }

            CombatLogger.Instance?.Log(CombatLogger.CAT_EXCHANGE,
                attacker.Unit?.DisplayName ?? "?",
                $"RECOVERY COMPLETE  atk-ini={attacker.Initiative:F1}  def-ini={oldDefender.Initiative:F1}  → both Free  beat={beatDurationSeconds:F2}s");

            // Warn only if the defender is still mid-execute when freed (genuinely unexpected)
            if (oldDefender.IsAnimating && oldDefender.CombatState == UnitCombatState.Execute)
                CombatLogger.Instance?.Warn(oldDefender.Unit?.DisplayName ?? "?",
                    $"Freed from Defender while still in Execute! state={oldDefender.CombatState}");
        }
    }
}
