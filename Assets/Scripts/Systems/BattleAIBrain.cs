using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Systems
{
    /// <summary>
    /// Stateless decision service consumed from UnitBrainAI.UpdateDecide.
    /// Holds no per-unit state — callers pass everything in. Implements the
    /// Decide-tick tree from COMBAT_DESIGN.md "The unit AI brain":
    ///
    ///   Step 1 — survival check (HP threshold)
    ///   Step 2 — resource posture (speed / energy / HP bands)
    ///   Step 3 — archetype selection from a posture × behavior matrix
    ///   Step 4 — skill choice from equipped skills, archetype-biased
    ///   Step 5 — movement intent
    ///
    /// Behavior personality differentials live in <see cref="BehaviorParams"/>.
    /// Implicit resource focus is inferred once at battle start via
    /// <see cref="InferArchetype"/> and cached on the caller side.
    /// </summary>
    public static class BattleAIBrain
    {
        // ── Resource postures ────────────────────────────────────────

        public enum SpeedPosture  { Sluggish, Engaged, Primed }
        public enum EnergyPosture { Drained,  Mid,     Loaded }
        public enum HpPosture     { Wounded,  Healthy }

        // ── Action archetypes (Step 3 output) ────────────────────────

        public enum ActionArchetype
        {
            BuildSpeed,        // not enough speed — go circle/run
            BuildPhase,        // load up sign casts to set up next combo
            Disengage,         // back off and recover
            BasicAttack,       // physical only, no commit
            MidCombo,          // moderate-power combo
            BigCombo,          // ultimate-tier commit
            Wait               // stand and observe (rare)
        }

        // ── Inferred unit archetypes from loadout (implicit focus) ──

        public enum LoadoutArchetype
        {
            Brawler,           // mostly Melee + Physical, low energy load
            BurstStriker,      // Melee with heavy combos, high speed dependence
            Caster,            // mostly Rooted, high energy, sign-heavy
            Hybrid,            // mixed cast types, balanced costs
            Healer             // contains heal skills
        }

        // ── Behavior personality parameters ──────────────────────────

        public class BehaviorParams
        {
            public float speedThresholdBigCombo;     // commit big at this speed
            public float hpThresholdDisengage;       // start disengaging below this HP fraction
            public float dodgeWillingnessAtLowSpeed; // 0..1
            public bool  prefersClosing;             // movement style hint
            public bool  prefersCircling;
            public bool  prefersDisengage;
        }

        public static BehaviorParams ParamsFor(BehaviorType type)
        {
            switch (type)
            {
                case BehaviorType.Aggressive:
                    return new BehaviorParams
                    {
                        speedThresholdBigCombo     = 50f,
                        hpThresholdDisengage       = 0.30f,
                        dodgeWillingnessAtLowSpeed = 0.7f,
                        prefersClosing             = true
                    };
                case BehaviorType.Defensive:
                    return new BehaviorParams
                    {
                        speedThresholdBigCombo     = 80f,
                        hpThresholdDisengage       = 0.60f,
                        dodgeWillingnessAtLowSpeed = 0.2f,
                        prefersDisengage           = true
                    };
                case BehaviorType.Balanced:
                default:
                    return new BehaviorParams
                    {
                        speedThresholdBigCombo     = 65f,
                        hpThresholdDisengage       = 0.50f,
                        dodgeWillingnessAtLowSpeed = 0.45f,
                        prefersCircling            = true
                    };
            }
        }

        /// <summary>
        /// Apply a stance asset on top of the behavior-derived defaults.
        /// Stances *modulate* — they tighten or loosen specific knobs without
        /// breaking hard rules (energy / speed gates / HP-critical survival).
        /// </summary>
        public static BehaviorParams ApplyStance(BehaviorParams baseParams, StanceDefinition stance)
        {
            if (stance == null) return baseParams;

            baseParams.speedThresholdBigCombo     = stance.speedThresholdBigCombo;
            baseParams.hpThresholdDisengage       = stance.hpThresholdDisengage;
            baseParams.dodgeWillingnessAtLowSpeed = stance.dodgeWillingnessAtLowSpeed;

            // Movement preference flags from stance preferred intent.
            baseParams.prefersClosing   = stance.preferredIntent == MovementIntent.Close;
            baseParams.prefersCircling  = stance.preferredIntent == MovementIntent.Circle;
            baseParams.prefersDisengage = stance.preferredIntent == MovementIntent.Disengage;

            return baseParams;
        }

        // ── Decision API ─────────────────────────────────────────────

        public struct Decision
        {
            public ActionArchetype archetype;
            public MovementIntent  movementIntent;
            public string          reason;       // for logs / debugging
        }

        public static Decision Decide(
            UnitRuntime unit,
            float currentSpeed,
            float currentEnergy,
            BehaviorType behavior,
            LoadoutArchetype loadout,
            StanceDefinition stance = null,
            RangeBand rangeBand = RangeBand.Mid)
        {
            BehaviorParams p = ParamsFor(behavior);
            p = ApplyStance(p, stance);

            // Stance reserve floor — Sentinel (30) won't spend speed below 30,
            // even if the archetype matrix would otherwise pick MidCombo. This
            // routes the unit to BuildSpeed instead.
            float reserveFloor = stance != null ? stance.speedReserveFloor : 0f;

            float hpFraction = unit.maxHP > 0 ? (float)unit.currentHP / unit.maxHP : 1f;
            HpPosture hp = hpFraction < p.hpThresholdDisengage ? HpPosture.Wounded : HpPosture.Healthy;

            SpeedPosture sp;
            if (currentSpeed < 30f)               sp = SpeedPosture.Sluggish;
            else if (currentSpeed < p.speedThresholdBigCombo) sp = SpeedPosture.Engaged;
            else                                   sp = SpeedPosture.Primed;

            EnergyPosture ep;
            if (currentEnergy < 20f)              ep = EnergyPosture.Drained;
            else if (currentEnergy < 70f)         ep = EnergyPosture.Mid;
            else                                   ep = EnergyPosture.Loaded;

            // Step 1 — critical HP override
            if (hpFraction < 0.25f && p.prefersDisengage)
                return new Decision
                {
                    archetype      = ActionArchetype.Disengage,
                    movementIntent = MovementIntent.Disengage,
                    reason         = "critical HP, defensive posture"
                };

            // Stance reserve floor — speed below this never spends on a combo.
            if (currentSpeed < reserveFloor)
            {
                return new Decision
                {
                    archetype      = ActionArchetype.BuildSpeed,
                    movementIntent = stance != null ? stance.preferredIntent : MovementIntent.Circle,
                    reason         = $"stance reserve floor ({reserveFloor:F0}) — rebuild before commit"
                };
            }

            // Range-band reads — too far to commit a melee combo, route to
            // close-the-gap movement (or to ranged casts if the loadout supports).
            if (rangeBand == RangeBand.Far && loadout != LoadoutArchetype.Caster)
            {
                return new Decision
                {
                    archetype      = ActionArchetype.BasicAttack,   // close + jab on the way in
                    movementIntent = MovementIntent.Close,
                    reason         = $"range=Far — close to engagement distance"
                };
            }
            if (rangeBand == RangeBand.Far && loadout == LoadoutArchetype.Caster)
            {
                return new Decision
                {
                    archetype      = ActionArchetype.BuildPhase,    // sign-cast at range
                    movementIntent = MovementIntent.Disengage,
                    reason         = $"range=Far + Caster — keep distance, cast"
                };
            }

            // Step 2-3 — posture × behavior table (matches spec lines 235-244).
            ActionArchetype archetype;
            MovementIntent move;

            if (sp == SpeedPosture.Sluggish)
            {
                archetype = ActionArchetype.BuildSpeed;
                move      = p.prefersDisengage ? MovementIntent.Disengage
                          : p.prefersCircling  ? MovementIntent.Circle
                          : MovementIntent.Close;
            }
            else if (sp == SpeedPosture.Primed && ep == EnergyPosture.Loaded && hp == HpPosture.Healthy)
            {
                archetype = ActionArchetype.BigCombo;
                move      = MovementIntent.Close;
            }
            else if (sp == SpeedPosture.Primed && ep == EnergyPosture.Loaded && hp == HpPosture.Wounded)
            {
                archetype = behavior == BehaviorType.Aggressive
                    ? ActionArchetype.MidCombo
                    : ActionArchetype.Disengage;
                move = behavior == BehaviorType.Aggressive ? MovementIntent.Close : MovementIntent.Disengage;
            }
            else if (sp == SpeedPosture.Primed && ep == EnergyPosture.Drained)
            {
                archetype = ActionArchetype.BasicAttack;
                move      = MovementIntent.Close;
            }
            else if (ep == EnergyPosture.Drained)
            {
                archetype = behavior == BehaviorType.Defensive
                    ? ActionArchetype.Disengage : ActionArchetype.BasicAttack;
                move      = behavior == BehaviorType.Defensive
                    ? MovementIntent.Disengage : MovementIntent.Close;
            }
            else if (ep == EnergyPosture.Loaded && loadout == LoadoutArchetype.Caster)
            {
                archetype = ActionArchetype.BuildPhase;
                move      = MovementIntent.Circle;   // casters circle, not close
            }
            else
            {
                archetype = ActionArchetype.MidCombo;
                move      = p.prefersClosing  ? MovementIntent.Close
                          : p.prefersCircling ? MovementIntent.Circle
                          : MovementIntent.Close;
            }

            return new Decision
            {
                archetype      = archetype,
                movementIntent = move,
                reason         = $"sp={sp} ep={ep} hp={hp} beh={behavior} arch={loadout}"
            };
        }

        // ── Implicit resource-focus inference ────────────────────────

        public static LoadoutArchetype InferArchetype(UnitRuntime unit)
        {
            if (unit?.equippedSkills == null || unit.equippedSkills.Count == 0)
                return LoadoutArchetype.Brawler;

            int meleeCount = 0, mobileCount = 0, rootedCount = 0;
            int totalEnergy = 0;
            int distinctElements = 0;
            var elementsSeen = new HashSet<ElementType>();
            int healSlots = 0;

            foreach (var skill in unit.equippedSkills)
            {
                bool slotHasSupport   = false;
                bool slotHasPhysical  = false;
                bool slotHasHeal      = false;
                int  slotEnergy       = 0;

                foreach (var slot in skill.actionSequence)
                {
                    var a = slot.action;
                    if (a == null) continue;
                    slotEnergy += (int)a.energyCost;
                    if (a.actionType == ActionType.Physical)  slotHasPhysical = true;
                    if (a.actionType == ActionType.Support)   slotHasSupport  = true;
                    if (a.element != ElementType.None) elementsSeen.Add(a.element);
                    if (a.displayName != null && a.displayName.ToLowerInvariant().Contains("heal"))
                        slotHasHeal = true;
                }

                totalEnergy += slotEnergy;
                if (slotHasHeal) healSlots++;

                // Cast type approximation from action mix
                if      (slotHasSupport)  rootedCount++;
                else if (slotHasPhysical) meleeCount++;
                else                      mobileCount++;
            }

            distinctElements = elementsSeen.Count;

            if (healSlots > 0) return LoadoutArchetype.Healer;
            if (rootedCount > meleeCount + mobileCount) return LoadoutArchetype.Caster;
            if (meleeCount >= 2 && totalEnergy < 30)    return LoadoutArchetype.Brawler;
            if (meleeCount >= 1 && distinctElements >= 2 && totalEnergy >= 30)
                return LoadoutArchetype.BurstStriker;

            return LoadoutArchetype.Hybrid;
        }
    }
}
