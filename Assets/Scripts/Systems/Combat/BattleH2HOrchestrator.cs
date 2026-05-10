using System;
using System.Collections;
using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.ThirdPerson;
using UnityEngine;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Pair coordinator for H2H Exchange. Picks the active attacker by
    /// initiative, schedules the combo's per-hit impact frames,
    /// pre-positions the attacker so the first hit lands at the combo's
    /// `desiredImpactDistance`, dispatches per-hit attacker / defender
    /// callbacks, and computes post-exchange transitions (continue
    /// Engaged, separate, counter-swap roles).
    ///
    /// One exchange = one combo = one or more `ComboHit` entries with
    /// distinct impact frames. The defender re-decides per hit, so a
    /// 3-hit combo with one block + one dodge + one full hit is a normal
    /// outcome.
    ///
    /// Strike resolution / damage / FX are delegated to the agents
    /// (`H2HUnit`); the orchestrator owns roles and timing.
    /// </summary>
    public class BattleH2HOrchestrator : MonoBehaviour
    {
        public class ExchangeHandle
        {
            public IH2HExchangeAgent attacker;
            public IH2HExchangeAgent defender;
            public H2HUnit.Combo combo;
            public AttackArchetype archetype;
            public float startedAt;
            public float[] impactAt;          // wall-clock impact time per hit
            public bool[]  impactDispatched;  // per-hit single-fire latch
            public float endsAt;              // wall-clock exchange end
            public bool   resolved;
            public bool   defenderCountered;
            public Vector3 attackerStartPos;  // for the pre-position smoothstep
            public Vector3 attackerImpactPos;
            public float positionAdjustEndsAt;
        }

        [Header("Tuning")]
        [Tooltip("Default seconds between exchange start and impact frame for single-hit attacks. Used if a combo doesn't supply per-hit impactNormalized.")]
        public float defaultImpactDelay = 0.45f;
        [Tooltip("Recovery tail (seconds) after the LAST hit's impact before the exchange resolves.")]
        public float recoveryAfterLastImpact = 0.3f;
        [Tooltip("Base separation chance after a successful exchange (before stance modifiers).")]
        [Range(0f, 1f)] public float baseSeparationChance = 0.35f;
        [Tooltip("Bonus separation chance applied when the unit's HP fraction is below 0.4.")]
        [Range(0f, 0.5f)] public float lowHpSeparationBonus = 0.2f;
        [Tooltip("Log every orchestrator decision.")]
        public bool debugLog = false;

        [Header("Global toggles (test bench)")]
        [Tooltip("If false, units never roll for Separation after an exchange — they always return to Engaged. Useful while tuning the combat loop.")]
        public bool SeparationEnabled = true;
        [Tooltip("If false, defenders never pick Dodge as a reaction; they Block or Eat instead. Useful while tuning hit reactions.")]
        public bool DodgeEnabled = true;
        [Tooltip("If false, the Speed-pool resource never drains (idle drain, NotEngaged drain) — units stay at their current charge until they spend it explicitly.")]
        public bool ResourceDrainEnabled = false;
        [Tooltip("If false, attack hits are free — committing a combo doesn't subtract speedCost from the attacker's Speed pool.")]
        public bool SkillCostsEnabled = false;

        [Header("References")]
        [SerializeField] private BattleH2HPhaseSystem _phaseSystem;
        public BattleH2HPhaseSystem PhaseSystem
        {
            get => _phaseSystem;
            set => _phaseSystem = value;
        }

        private readonly Dictionary<IH2HExchangeAgent, ExchangeHandle> _byUnit
            = new Dictionary<IH2HExchangeAgent, ExchangeHandle>();
        private readonly List<ExchangeHandle> _active = new List<ExchangeHandle>();

        public event Action<ExchangeHandle> OnExchangeStarted;
        public event Action<ExchangeHandle, int> OnExchangeImpact;   // hitIndex
        public event Action<ExchangeHandle> OnExchangeResolved;

        // ── Entry point ─────────────────────────────────────────────

        /// <summary>
        /// Called when two units are in striking range and one's brain
        /// has committed. Picks attacker by initiative and schedules
        /// the combo. Returns null if eligibility checks fail.
        /// </summary>
        public ExchangeHandle RegisterPair(IH2HExchangeAgent unitA, IH2HExchangeAgent unitB)
        {
            if (unitA == null || unitB == null) return null;
            if (_byUnit.ContainsKey(unitA) || _byUnit.ContainsKey(unitB)) return null;

            // Phase eligibility: both must be Engaged.
            if (_phaseSystem != null)
            {
                var aMb = unitA as MonoBehaviour;
                var bMb = unitB as MonoBehaviour;
                if (aMb != null && _phaseSystem.GetPhase(aMb) != H2HPhase.Engaged) return null;
                if (bMb != null && _phaseSystem.GetPhase(bMb) != H2HPhase.Engaged) return null;
            }

            int initA = ComputeInitiative(unitA);
            int initB = ComputeInitiative(unitB);
            if (initA == initB) initB += UnityEngine.Random.value > 0.5f ? 1 : -1;

            IH2HExchangeAgent attacker = initA >= initB ? unitA : unitB;
            IH2HExchangeAgent defender = attacker == unitA ? unitB : unitA;

            var combo = attacker.PickCombo();
            if (combo == null || combo.hits == null || combo.hits.Count == 0)
            {
                if (debugLog) Debug.Log($"[H2HOrchestrator] {attacker.DisplayName} has no eligible combo.");
                return null;
            }

            float now = Time.time;

            // Per-hit timeline. The first hit's impact is at start +
            // positionAdjustDuration + impactNormalized × clipLen. We don't
            // know the clip's exact length here; ask the agent for a
            // sensible total length and decompose by interHitGap.
            int n = combo.hits.Count;
            var impactTimes = new float[n];
            var dispatched  = new bool[n];

            // First-hit impact: pre-position window + a clip-driven offset
            // estimated from impactNormalized (defaults to 0.45) × an
            // attack-clip estimate.
            float firstClipEst = attacker.GetTotalLengthSeconds(combo.hits[0].attackId, defaultImpactDelay * 2f);
            float firstImpact = now + combo.positionAdjustDuration
                + Mathf.Clamp(combo.hits[0].impactNormalized, 0.05f, 0.95f) * firstClipEst;
            impactTimes[0] = firstImpact;
            for (int i = 1; i < n; i++)
                impactTimes[i] = impactTimes[i - 1] + Mathf.Max(0.1f, combo.interHitGap);

            float endsAt = impactTimes[n - 1] + recoveryAfterLastImpact;

            // Pre-position: attacker walks toward defender along the line
            // between them, ending at the combo's desiredImpactDistance.
            Vector3 atkPos = (attacker as Component)?.transform.position ?? Vector3.zero;
            Vector3 defPos = (defender as Component)?.transform.position ?? Vector3.zero;
            Vector3 toDef = defPos - atkPos; toDef.y = 0f;
            float curDist = toDef.magnitude;
            Vector3 dir = curDist > 0.001f ? toDef / curDist : Vector3.forward;
            float targetDist = Mathf.Max(0.5f, combo.desiredImpactDistance);
            Vector3 impactPos = defPos - dir * targetDist;
            impactPos.y = atkPos.y;

            var handle = new ExchangeHandle
            {
                attacker = attacker,
                defender = defender,
                combo = combo,
                archetype = combo.hits[0].archetype,
                startedAt = now,
                impactAt = impactTimes,
                impactDispatched = dispatched,
                endsAt = endsAt,
                attackerStartPos = atkPos,
                attackerImpactPos = impactPos,
                positionAdjustEndsAt = now + combo.positionAdjustDuration,
            };
            _byUnit[unitA] = handle;
            _byUnit[unitB] = handle;
            _active.Add(handle);

            if (_phaseSystem != null)
            {
                _phaseSystem.TransitionPhase((MonoBehaviour)attacker, H2HPhase.Exchange, "exchange-start-attacker");
                _phaseSystem.TransitionPhase((MonoBehaviour)defender, H2HPhase.Exchange, "exchange-start-defender");
            }

            attacker.OnAssignedAttacker(handle);
            defender.OnAssignedDefender(handle);
            OnExchangeStarted?.Invoke(handle);

            if (debugLog)
                Debug.Log($"[H2HOrchestrator] Exchange begin: ATK={attacker.DisplayName}({initA}) DEF={defender.DisplayName}({initB}) combo={combo.name} hits={n}");

            // Drive the position-adjust on the attacker via a coroutine.
            if (combo.positionAdjustDuration > 0f && attacker is MonoBehaviour atkMb)
                atkMb.StartCoroutine(PrePositionCoroutine(handle));

            return handle;
        }

        public ExchangeHandle GetActiveExchange(IH2HExchangeAgent unit)
        {
            return unit != null && _byUnit.TryGetValue(unit, out var h) ? h : null;
        }

        public bool IsBusy(IH2HExchangeAgent unit) => GetActiveExchange(unit) != null;

        public void CancelExchange(ExchangeHandle handle, string reason)
        {
            if (handle == null) return;
            ResolveExchange(handle, defenderCounter: false, reasonOverride: reason);
        }

        // ── Pre-position smoothstep ────────────────────────────────

        private IEnumerator PrePositionCoroutine(ExchangeHandle h)
        {
            var atkMb = h.attacker as MonoBehaviour;
            if (atkMb == null) yield break;
            var atkUnit = atkMb as H2HUnit;
            CharacterController cc = atkUnit != null ? atkUnit.CC : null;
            float dur = h.combo.positionAdjustDuration;
            float t = 0f;
            Vector3 from = h.attackerStartPos;
            Vector3 to   = h.attackerImpactPos;
            while (t < dur && !h.resolved)
            {
                float u = Mathf.Clamp01(t / dur);
                u = u * u * (3f - 2f * u); // smoothstep
                Vector3 desired = Vector3.Lerp(from, to, u);
                if (cc != null && cc.enabled)
                {
                    Vector3 delta = desired - atkMb.transform.position;
                    delta.y = -1f * Time.deltaTime;
                    cc.Move(delta);
                }
                else
                {
                    atkMb.transform.position = desired;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        // ── Tick ────────────────────────────────────────────────────

        private void Update()
        {
            if (_active.Count == 0) return;
            float now = Time.time;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var h = _active[i];
                if (h.resolved) { _active.RemoveAt(i); continue; }

                // Per-hit single-fire impact.
                for (int k = 0; k < h.impactAt.Length; k++)
                {
                    if (!h.impactDispatched[k] && now >= h.impactAt[k])
                    {
                        h.impactDispatched[k] = true;
                        DispatchImpact(h, k);
                        // Counter on first hit interrupts the chain.
                        if (k == 0 && h.defenderCountered) break;
                    }
                }

                if (h.defenderCountered)
                {
                    ResolveExchange(h, defenderCounter: true, reasonOverride: "counter");
                    continue;
                }

                if (now >= h.endsAt)
                    ResolveExchange(h, defenderCounter: false, reasonOverride: null);
            }
        }

        private void DispatchImpact(ExchangeHandle h, int hitIndex)
        {
            h.attacker.OnExchangeImpactAttacker(h, hitIndex);
            bool counter = h.defender.OnExchangeImpactDefender(h, hitIndex);
            if (counter) h.defenderCountered = true;
            OnExchangeImpact?.Invoke(h, hitIndex);
        }

        // ── Resolution ──────────────────────────────────────────────

        public void ResolveExchange(ExchangeHandle handle, bool defenderCounter, string reasonOverride = null)
        {
            if (handle == null || handle.resolved) return;
            handle.resolved = true;
            _byUnit.Remove(handle.attacker);
            _byUnit.Remove(handle.defender);
            _active.Remove(handle);

            OnExchangeResolved?.Invoke(handle);

            if (defenderCounter)
            {
                if (debugLog)
                    Debug.Log($"[H2HOrchestrator] Counter — role swap. New attacker={handle.defender.DisplayName}");
                RegisterPair(handle.defender, handle.attacker);
                return;
            }

            // If a unit died mid-exchange, don't roll separation; just drop
            // both back to NotEngaged via the death handler.
            var atkUnit = handle.attacker as H2HUnit;
            var defUnit = handle.defender as H2HUnit;
            if ((atkUnit != null && atkUnit.IsDead) || (defUnit != null && defUnit.IsDead))
                return;

            bool attSeparates = RollSeparation(handle.attacker);
            bool defSeparates = RollSeparation(handle.defender);

            if (_phaseSystem != null)
            {
                _phaseSystem.TransitionPhase((MonoBehaviour)handle.attacker,
                    attSeparates ? H2HPhase.Separating : H2HPhase.Engaged,
                    attSeparates ? "post-exchange-separate" : "post-exchange-engage");
                _phaseSystem.TransitionPhase((MonoBehaviour)handle.defender,
                    defSeparates ? H2HPhase.Separating : H2HPhase.Engaged,
                    defSeparates ? "post-exchange-separate" : "post-exchange-engage");
            }

            handle.attacker.OnExchangeResolved(handle, asAttacker: true,  separating: attSeparates);
            handle.defender.OnExchangeResolved(handle, asAttacker: false, separating: defSeparates);
        }

        // ── Initiative & separation rolls ───────────────────────────

        private int ComputeInitiative(IH2HExchangeAgent unit)
        {
            int speed = Mathf.RoundToInt(unit.CurrentSpeed);
            int stanceBonus = unit.Stance != null ? unit.Stance.initiativeBonus : 0;
            int counterBias = unit.JustDefendedLastExchange ? 10 : 0;
            int rng = UnityEngine.Random.Range(-10, 11);
            return speed + stanceBonus + counterBias + rng;
        }

        private bool RollSeparation(IH2HExchangeAgent unit)
        {
            if (!SeparationEnabled) return false;
            float chance = baseSeparationChance;
            if (unit.Stance != null) chance += unit.Stance.separationChanceModifier;
            if (unit.HpFraction < 0.4f) chance += lowHpSeparationBonus;
            if (unit.CurrentSpeed < 20f) chance += 0.25f;
            chance = Mathf.Clamp01(chance);
            float roll = UnityEngine.Random.value;
            return roll < chance;
        }
    }

    /// <summary>
    /// Contract the orchestrator uses to drive either side of an exchange.
    /// Implemented by `H2HUnit` for the training scene; future battle
    /// integration adds an adapter wrapping `TerrainBattleUnit`.
    /// </summary>
    public interface IH2HExchangeAgent
    {
        string             DisplayName { get; }
        StanceDefinition   Stance { get; }
        float              CurrentSpeed { get; }   // 0-100 speed-resource value
        float              HpFraction   { get; }   // 0-1
        bool               JustDefendedLastExchange { get; }

        /// <summary>Pick a combo to commit to. Returns null if no combo
        /// is currently affordable (resource gates failed).</summary>
        H2HUnit.Combo PickCombo();

        /// <summary>Total length of a single attack id's animation +
        /// recovery (used to schedule the first hit's impact frame).</summary>
        float GetTotalLengthSeconds(string attackId, float fallback);

        void OnAssignedAttacker(BattleH2HOrchestrator.ExchangeHandle h);
        void OnAssignedDefender(BattleH2HOrchestrator.ExchangeHandle h);

        /// <summary>Per-hit attacker callback. `hitIndex` is the index
        /// into the combo's hit list.</summary>
        void OnExchangeImpactAttacker(BattleH2HOrchestrator.ExchangeHandle h, int hitIndex);

        /// <summary>Per-hit defender callback. Returns true to trigger a
        /// counter / role swap (only honored on the first hit).</summary>
        bool OnExchangeImpactDefender(BattleH2HOrchestrator.ExchangeHandle h, int hitIndex);

        void OnExchangeResolved(BattleH2HOrchestrator.ExchangeHandle h, bool asAttacker, bool separating);
    }
}
