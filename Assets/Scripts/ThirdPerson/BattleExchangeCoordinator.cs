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
            if (target.IsAnimating)
            {
                // Target is busy — wait as defender so we don't spin in Melee
                requester.SetCombatRole(CombatRole.Defender);
                return CombatRole.Defender;
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

                _exchanges[attacker] = new ExchangeRecord { attacker = attacker, defender = defender };
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

            CombatLogger.Instance?.Log(CombatLogger.CAT_EXCHANGE,
                attacker.Unit?.DisplayName ?? "?",
                $"RECOVERY COMPLETE  atk-ini={attacker.Initiative:F1}  def-ini={oldDefender.Initiative:F1}  → both Free");

            // Warn if the defender is still mid-animation when freed
            if (oldDefender.IsAnimating)
                CombatLogger.Instance?.Warn(oldDefender.Unit?.DisplayName ?? "?",
                    $"Freed from Defender while still IsAnimating! state={oldDefender.CombatState}");
        }
    }
}
