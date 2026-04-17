using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Systems
{
    public class CombatResolutionSystem
    {
        // Basic attack: attacker.attack - defender.defense (minimum 1)
        public CombatContext ResolveBasicAttack(UnitRuntime attacker, UnitRuntime defender)
        {
            int raw   = attacker.currentStats.attack;
            int def   = defender.currentStats.defense;
            int final_ = Mathf.Max(1, raw - def);

            defender.TakeDamage(final_);

            return new CombatContext
            {
                attacker    = attacker,
                defender    = defender,
                technique   = null,
                baseDamage  = raw,
                finalDamage = final_
            };
        }

        // Technique attack: technique.power - defender.defense (minimum 1)
        public CombatContext ResolveTechnique(UnitRuntime attacker, UnitRuntime defender,
            ResolvedTechnique technique)
        {
            int raw   = technique.power;
            int def   = defender.currentStats.defense;
            int final_ = Mathf.Max(1, raw - def);

            defender.TakeDamage(final_);

            return new CombatContext
            {
                attacker    = attacker,
                defender    = defender,
                technique   = technique,
                baseDamage  = raw,
                finalDamage = final_
            };
        }
    }
}
