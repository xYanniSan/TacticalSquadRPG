using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Per-unit active status / CC effect ticking with magnitude (e.g. slow strength).
    /// Source isn't tracked yet — will be added if/when stack rules need it.
    /// </summary>
    public class StatusEffectInstance
    {
        public CCEffectType type;
        public float remainingDuration;
        public float magnitude;

        public StatusEffectInstance(CCEffectType type, float duration, float magnitude)
        {
            this.type = type;
            this.remainingDuration = duration;
            this.magnitude = magnitude;
        }
    }

    /// <summary>
    /// Central status / CC registry. Ticks all active effects per frame, expires
    /// them, exposes queries for the brain and resolver.
    ///
    /// Phase 10 foundation: Stun and Slow are wired end-to-end. Interrupt /
    /// Knockdown / Ragdoll are reserved enum values; the formal Knockback
    /// effect is still owned by BattleKnockbackSystem.
    /// </summary>
    public class BattleStatusEffectSystem : MonoBehaviour
    {
        private readonly Dictionary<TerrainBattleUnit, List<StatusEffectInstance>> _effects
            = new Dictionary<TerrainBattleUnit, List<StatusEffectInstance>>();

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Apply a CC effect. If the unit already has an effect of the same
        /// type, refresh whichever is longer (no stacking — keeps math simple
        /// for Phase 10; revisit when status authoring grows).
        /// </summary>
        public void Apply(TerrainBattleUnit unit, CCEffectType type, float duration, float magnitude = 1f)
        {
            if (unit == null || unit.IsDead || duration <= 0f) return;

            var list = GetOrCreate(unit);

            // Refresh existing instance of same type (longest duration wins).
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type != type) continue;
                if (duration > list[i].remainingDuration)
                {
                    list[i].remainingDuration = duration;
                    list[i].magnitude         = magnitude;
                }
                OnApplied(unit, type);
                return;
            }

            list.Add(new StatusEffectInstance(type, duration, magnitude));
            OnApplied(unit, type);
        }

        public bool Has(TerrainBattleUnit unit, CCEffectType type)
        {
            if (unit == null) return false;
            if (!_effects.TryGetValue(unit, out var list)) return false;
            for (int i = 0; i < list.Count; i++)
                if (list[i].type == type) return true;
            return false;
        }

        public float GetMagnitude(TerrainBattleUnit unit, CCEffectType type)
        {
            if (unit == null) return 0f;
            if (!_effects.TryGetValue(unit, out var list)) return 0f;
            for (int i = 0; i < list.Count; i++)
                if (list[i].type == type) return list[i].magnitude;
            return 0f;
        }

        public void RemoveAll(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            _effects.Remove(unit);
        }

        /// <summary>
        /// Movement / timing rate multiplier from active Slow effects.
        /// 1.0 = unaffected; 0.6 = move/cast at 60% speed. Stacks multiplicatively
        /// across multiple slows (rare under the no-stacking apply rule, but
        /// guarded for completeness if we relax that later).
        /// </summary>
        public float GetSlowFactor(TerrainBattleUnit unit)
        {
            if (unit == null) return 1f;
            if (!_effects.TryGetValue(unit, out var list)) return 1f;
            float factor = 1f;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type != CCEffectType.Slow) continue;
                float strength = Mathf.Clamp01(list[i].magnitude);
                factor *= (1f - strength);
            }
            return Mathf.Clamp(factor, 0.1f, 1f);
        }

        // ── Tick ─────────────────────────────────────────────────────

        private void Update()
        {
            if (_effects.Count == 0) return;
            float dt = Time.deltaTime;

            // Iterate over a snapshot so we can mutate
            var keys = new List<TerrainBattleUnit>(_effects.Keys);
            for (int u = 0; u < keys.Count; u++)
            {
                TerrainBattleUnit unit = keys[u];
                if (unit == null || unit.IsDead) { _effects.Remove(unit); continue; }

                var list = _effects[unit];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    list[i].remainingDuration -= dt;
                    if (list[i].remainingDuration <= 0f)
                    {
                        CCEffectType expired = list[i].type;
                        list.RemoveAt(i);
                        OnExpired(unit, expired);
                    }
                }
            }
        }

        // ── Hooks: drive UnitBrainAI when CC starts/ends ─────────────

        private void OnApplied(TerrainBattleUnit unit, CCEffectType type)
        {
            switch (type)
            {
                case CCEffectType.Stun:
                    unit.OnStunApplied();
                    break;
                // Slow is read passively by the consumer (movement, timing); no event needed.
            }
        }

        private void OnExpired(TerrainBattleUnit unit, CCEffectType type)
        {
            switch (type)
            {
                case CCEffectType.Stun:
                    unit.OnStunExpired();
                    break;
            }
        }

        // ── Internals ────────────────────────────────────────────────

        private List<StatusEffectInstance> GetOrCreate(TerrainBattleUnit unit)
        {
            if (!_effects.TryGetValue(unit, out var list))
            {
                list = new List<StatusEffectInstance>(2);
                _effects[unit] = list;
            }
            return list;
        }
    }
}
