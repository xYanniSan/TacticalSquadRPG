namespace TacticalRPG.DataModels
{
    /// <summary>
    /// A temporary buff applied during 3D terrain combat.
    /// Expires after a number of melee hit charges are consumed.
    /// </summary>
    public class ActiveBuff
    {
        public string buffName;
        public ElementType element;
        public int bonusDamage;
        public int chargesRemaining;

        public bool IsExpired => chargesRemaining <= 0;

        public ActiveBuff(string name, ElementType element, int bonusDamage, int charges)
        {
            buffName = name;
            this.element = element;
            this.bonusDamage = bonusDamage;
            chargesRemaining = charges;
        }

        /// <summary>
        /// Consumes one charge and returns the bonus damage for this hit.
        /// </summary>
        public int Consume()
        {
            if (chargesRemaining <= 0) return 0;
            chargesRemaining--;
            return bonusDamage;
        }
    }
}
