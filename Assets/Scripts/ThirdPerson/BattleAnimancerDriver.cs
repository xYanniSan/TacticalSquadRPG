using System.Collections;
using System.Collections.Generic;
using Animancer;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Centralized Animancer playback subsystem owned by TerrainBattleManager.
    ///
    /// Per-unit AnimancerComponents register on TerrainBattleUnit.Initialize.
    /// The driver is the single entry point for AttackProfile playback and
    /// per-unit hit-stop, so future visual modulation (speed-band slowdowns,
    /// CC visuals, attacker-only freeze, status tints) lives in one place
    /// instead of being threaded through per-unit drivers.
    ///
    /// Replaces the per-unit UnitAnimancerDriver. See
    /// Docs/Design/COMBAT_DESIGN.md "Phase 1 — Animancer driver foundation"
    /// and Docs/07_PRESENTATION.md "Animation runtime (Animancer Pro)".
    /// </summary>
    public class BattleAnimancerDriver : MonoBehaviour
    {
        // Default per-unit Animancer pause used when an impact event fires.
        // Independent of BattleHitStopSystem's global Time.timeScale freeze;
        // this is per-unit so we can asymmetrically slow attacker vs defender,
        // or apply CC-specific freezes later without touching the world clock.
        private const float DefaultImpactPauseSeconds = 0.05f;

        private readonly Dictionary<TerrainBattleUnit, AnimancerComponent> _animancers
            = new Dictionary<TerrainBattleUnit, AnimancerComponent>();

        private readonly HashSet<TerrainBattleUnit> _impactDispatched
            = new HashSet<TerrainBattleUnit>();

        private BattleSpeedSystem _speedSubscribed;

        private void Start()
        {
            // Suppress Animancer's NativeControllerHumanoid warning. Our
            // architecture intentionally runs both an Animator Controller
            // (legacy clip path) AND Animancer (data-driven AttackProfiles)
            // on humanoid rigs. They don't blend — they're parallel playback
            // paths chosen per-skill — so the warning is a false positive.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Animancer.OptionalWarning.NativeControllerHumanoid.Disable();
#endif

            // Subscribe once; speed system is created in TerrainBattleManager.Awake
            // and lives on the same GameObject.
            _speedSubscribed = TerrainBattleManager.Instance?.Speed;
            if (_speedSubscribed != null)
                _speedSubscribed.OnSpeedChanged += HandleSpeedChanged;
        }

        private void OnDestroy()
        {
            if (_speedSubscribed != null)
                _speedSubscribed.OnSpeedChanged -= HandleSpeedChanged;
        }

        private void HandleSpeedChanged(TerrainBattleUnit unit, float newValue)
        {
            if (_speedSubscribed == null) return;
            ApplySpeedBandModulation(unit, _speedSubscribed.GetSpeedBand(unit));
        }

        // ── Registration ────────────────────────────────────────────

        public void RegisterUnit(TerrainBattleUnit unit, AnimancerComponent animancer)
        {
            if (unit == null || animancer == null) return;
            _animancers[unit] = animancer;
        }

        public void UnregisterUnit(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            _animancers.Remove(unit);
            _impactDispatched.Remove(unit);
            _bandMultipliers.Remove(unit);
        }

        // ── Speed-band animation modulation ────────────────────────

        /// <summary>
        /// Per-unit animation playback rate driven by the unit's current
        /// SpeedBand. Sluggish bodies move slow, Primed bodies move fast.
        /// Applied to both Animancer (Graph.Speed) and the legacy Animator
        /// (Animator.speed) so both playback paths see the modulation.
        ///
        /// Spec: COMBAT_DESIGN.md "Speed-band as visual-cheating budget".
        /// </summary>
        private readonly Dictionary<TerrainBattleUnit, float> _bandMultipliers
            = new Dictionary<TerrainBattleUnit, float>();

        // Per-band playback multipliers — purely cosmetic, not gameplay-truth.
        // Wider spread so band-shifts feel dramatic: sluggish bodies move
        // visibly heavier, primed bodies snap.
        private static float MultiplierFor(SpeedBand band)
        {
            switch (band)
            {
                case SpeedBand.Sluggish: return 0.70f;
                case SpeedBand.Engaged:  return 0.95f;
                case SpeedBand.Sharp:    return 1.25f;
                case SpeedBand.Primed:   return 1.55f;
                default:                 return 1.00f;
            }
        }

        public void ApplySpeedBandModulation(TerrainBattleUnit unit, SpeedBand band)
        {
            if (unit == null) return;
            float mult = MultiplierFor(band);
            if (_bandMultipliers.TryGetValue(unit, out float prev) && Mathf.Approximately(prev, mult))
                return;
            _bandMultipliers[unit] = mult;

            // Animancer side
            if (_animancers.TryGetValue(unit, out var animancer)
                && animancer != null && animancer.Graph != null)
            {
                animancer.Graph.Speed = mult;
            }

            // Animator side (legacy clip path)
            var animator = unit.GetComponentInChildren<Animator>();
            if (animator != null) animator.speed = mult;
        }

        public bool IsAvailable(TerrainBattleUnit unit)
        {
            return unit != null
                && _animancers.TryGetValue(unit, out var animancer)
                && animancer != null;
        }

        // ── Playback ────────────────────────────────────────────────

        /// <summary>
        /// Plays the AttackProfile's transition on the unit's AnimancerComponent.
        /// Routes the named impact event and OnEnd back through the unit's
        /// existing event surface (OnHitFrame / OnAttackEnd) so abilities
        /// receive the same notifications regardless of playback path.
        ///
        /// Returns false if the unit isn't registered or the profile has no
        /// transition — callers fall back to the legacy Animator path.
        /// </summary>
        public bool PlayAttack(TerrainBattleUnit unit, AttackProfile profile)
        {
            if (unit == null || profile == null || profile.transition == null) return false;
            if (!_animancers.TryGetValue(unit, out var animancer) || animancer == null) return false;

            _impactDispatched.Remove(unit);

            AnimancerState state = animancer.Play(profile.transition);

            // Per-instance event registration — never wire to the shared
            // TransitionAsset directly, that would persist across all units
            // playing this profile.
            if (profile.impactEventName != null)
                state.Events(this).SetCallback(profile.impactEventName, () => OnImpactNamedEvent(unit));

            state.Events(this).OnEnd = () => OnTransitionEnd(unit);

            return true;
        }

        private void OnImpactNamedEvent(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            if (!_impactDispatched.Add(unit)) return;

            // Per-unit Animancer freeze on the attacker. Phase 1 demonstrates
            // the integration; later phases will fan out to defender, camera,
            // and CC-specific visuals through the strike-impact event hook.
            ApplyHitStop(unit, DefaultImpactPauseSeconds);

            unit.OnHitFrame();
        }

        private void OnTransitionEnd(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            unit.OnAttackEnd();
        }

        // ── Hit-stop ────────────────────────────────────────────────

        /// <summary>
        /// Pauses this unit's Animancer playback for `duration` real seconds,
        /// then restores. Independent of Time.timeScale, so it can apply to
        /// a single unit without freezing the world.
        /// </summary>
        public void ApplyHitStop(TerrainBattleUnit unit, float duration)
        {
            if (unit == null || duration <= 0f) return;
            if (!_animancers.TryGetValue(unit, out var animancer) || animancer == null) return;
            StartCoroutine(HitStopCoroutine(animancer, duration));
        }

        public void ApplyHitStop(TerrainBattleUnit attacker, TerrainBattleUnit defender, float duration)
        {
            ApplyHitStop(attacker, duration);
            ApplyHitStop(defender, duration);
        }

        /// <summary>
        /// Reference 5 — Zuko-style frozen pose at the moment of impact.
        /// Attacker locks their pose for `holdMs` real seconds while the hit
        /// resolves; defender freezes too. Visually this turns each heavy
        /// strike into a "panel break" that the player can read. Longer than
        /// regular ApplyHitStop, and it pauses the legacy Animator too.
        /// </summary>
        public void ApplyPoseHold(TerrainBattleUnit attacker, TerrainBattleUnit defender, float duration)
        {
            if (duration <= 0f) return;
            StartCoroutine(PoseHoldCoroutine(attacker, defender, duration));
        }

        private System.Collections.IEnumerator PoseHoldCoroutine(
            TerrainBattleUnit attacker, TerrainBattleUnit defender, float duration)
        {
            // Snapshot prior playback rates so we can restore them.
            float aMult = _bandMultipliers.TryGetValue(attacker, out float am) ? am : 1f;
            float dMult = defender != null && _bandMultipliers.TryGetValue(defender, out float dm) ? dm : 1f;

            FreezeUnit(attacker);
            if (defender != null) FreezeUnit(defender);

            yield return new WaitForSecondsRealtime(duration);

            UnfreezeUnit(attacker, aMult);
            if (defender != null) UnfreezeUnit(defender, dMult);
        }

        private void FreezeUnit(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            if (_animancers.TryGetValue(unit, out var animancer) && animancer?.Graph != null)
                animancer.Graph.Speed = 0f;
            var animator = unit.GetComponentInChildren<Animator>();
            if (animator != null) animator.speed = 0f;
        }

        private void UnfreezeUnit(TerrainBattleUnit unit, float restoreMultiplier)
        {
            if (unit == null) return;
            if (_animancers.TryGetValue(unit, out var animancer) && animancer?.Graph != null)
                animancer.Graph.Speed = restoreMultiplier;
            var animator = unit.GetComponentInChildren<Animator>();
            if (animator != null) animator.speed = restoreMultiplier;
        }

        private IEnumerator HitStopCoroutine(AnimancerComponent animancer, float duration)
        {
            // Speed lives on the playable graph node, not the component itself.
            // Setting it to 0 freezes the unit's animation without touching Time.timeScale.
            float prev = animancer.Graph.Speed;
            animancer.Graph.Speed = 0f;
            yield return new WaitForSecondsRealtime(duration);
            if (animancer != null && animancer.Graph != null)
                animancer.Graph.Speed = prev;
        }

        // ── Upper-body layering ─────────────────────────────────────
        // Layered playback per `Docs/Design/ENGAGEMENT_ANIMATIONS.md` §4.1.
        // Layer 0 = base (locomotion, full body — Kubold loops live here).
        // Layer 1 = upper-body overlay, masked via the unit's `UpperBody.mask`
        //           (configured by `H2HUnit.Awake` from a serialized field).
        //
        // A combat-guard pose on layer 1 over a sprint loop on layer 0
        // transforms how movement reads — legs sprint, hands hold guard.
        // This is the main fix for the "Kubold movement looks robotic in
        // combat" problem (legs are fine; upper body is what's wrong).
        //
        // Static helpers operate on any AnimancerComponent (decoupled from
        // TerrainBattleUnit) so the H2H training scene's layering tests can
        // call them directly with the unit's animancer reference. Per-unit
        // overloads taking `TerrainBattleUnit` look up the animancer from
        // the registration dict, matching the existing driver pattern.
        //
        // Pre-condition: layer 1 must already be configured with an
        // AvatarMask. `H2HUnit.Awake` does this from `_upperBodyMask`. If
        // the layer has no mask, calling these still "works" (Animancer
        // creates the layer on demand) but the upper layer will replace
        // the full body — defeating the point of layering.

        public const int BaseLayerIndex      = 0;
        public const int UpperBodyLayerIndex = 1;

        /// <summary>
        /// Plays `clip` on the base layer (full-body locomotion). Equivalent
        /// to `animancer.Layers[0].Play(clip, fadeDuration)`. The base layer
        /// is never masked, so this drives the entire body unless layer 1's
        /// weight pulls some bones toward an upper-body overlay.
        /// </summary>
        public static AnimancerState PlayBaseLayer(AnimancerComponent animancer, AnimationClip clip, float fadeDuration)
        {
            if (animancer == null || clip == null) return null;
            return animancer.Layers[BaseLayerIndex].Play(clip, fadeDuration);
        }

        /// <summary>
        /// Plays `clip` on the upper-body layer and fades the layer's overall
        /// weight to 1 over `fadeDuration`. Combine with a base-layer
        /// locomotion clip for "running while guarding" / "walking while
        /// punching" composites.
        ///
        /// Pre-condition: layer 1 must have an AvatarMask set (typically
        /// done in `H2HUnit.Awake`). Without a mask the upper clip
        /// REPLACES the entire body, which is rarely what you want.
        /// </summary>
        public static AnimancerState PlayUpperBody(AnimancerComponent animancer, AnimationClip clip, float fadeDuration)
        {
            if (animancer == null || clip == null) return null;
            var upper = animancer.Layers[UpperBodyLayerIndex];
            var state = upper.Play(clip, fadeDuration);
            upper.StartFade(1f, fadeDuration);
            return state;
        }

        /// <summary>
        /// Fades the upper-body layer's overall weight to 0 over
        /// `fadeDuration`. The current upper clip continues to play under
        /// the hood until the next `PlayUpperBody` call swaps it; only the
        /// layer's contribution to the final pose is faded out. Use after
        /// a punch or hand-sign overlay to release back to "base layer
        /// drives full body."
        /// </summary>
        public static void ReleaseUpperBody(AnimancerComponent animancer, float fadeDuration)
        {
            if (animancer == null) return;
            animancer.Layers[UpperBodyLayerIndex].StartFade(0f, fadeDuration);
        }

        /// <summary>
        /// Sets a partial weight on the upper-body layer (0 = invisible,
        /// 1 = fully drives masked bones). Useful for blending — e.g.
        /// a "subtle guard" at 0.4 reads as a tense posture without fully
        /// overriding the natural arm swing of the locomotion clip.
        /// </summary>
        public static void SetUpperBodyWeight(AnimancerComponent animancer, float weight, float fadeDuration)
        {
            if (animancer == null) return;
            animancer.Layers[UpperBodyLayerIndex].StartFade(Mathf.Clamp01(weight), fadeDuration);
        }

        // ── Per-TerrainBattleUnit convenience overloads ─────────────

        public AnimancerState PlayBaseLayer(TerrainBattleUnit unit, AnimationClip clip, float fadeDuration)
        {
            if (unit == null) return null;
            return _animancers.TryGetValue(unit, out var a)
                ? PlayBaseLayer(a, clip, fadeDuration)
                : null;
        }

        public AnimancerState PlayUpperBody(TerrainBattleUnit unit, AnimationClip clip, float fadeDuration)
        {
            if (unit == null) return null;
            return _animancers.TryGetValue(unit, out var a)
                ? PlayUpperBody(a, clip, fadeDuration)
                : null;
        }

        public void ReleaseUpperBody(TerrainBattleUnit unit, float fadeDuration)
        {
            if (unit == null) return;
            if (_animancers.TryGetValue(unit, out var a)) ReleaseUpperBody(a, fadeDuration);
        }

        public void SetUpperBodyWeight(TerrainBattleUnit unit, float weight, float fadeDuration)
        {
            if (unit == null) return;
            if (_animancers.TryGetValue(unit, out var a)) SetUpperBodyWeight(a, weight, fadeDuration);
        }
    }
}
