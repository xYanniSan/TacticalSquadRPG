namespace TacticalRPG.DataModels
{
    public class CombatContext
    {
        public UnitRuntime attacker;
        public UnitRuntime defender;
        public ResolvedTechnique technique; // null = basic attack
        public int baseDamage;
        public int finalDamage;

        public bool IsBasicAttack => technique == null;
    }
}
