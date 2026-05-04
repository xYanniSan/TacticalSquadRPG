using System;

namespace TacticalRPG.DataModels
{
    [Serializable]
    public struct StatBlock
    {
        public int maxHP;
        public int attack;
        public int defense;
        public float moveSpeed;

        public StatBlock(int maxHP, int attack, int defense, float moveSpeed)
        {
            this.maxHP = maxHP;
            this.attack = attack;
            this.defense = defense;
            this.moveSpeed = moveSpeed;
        }

        public static StatBlock Default => new StatBlock(100, 10, 5, 3f);

        public override string ToString() =>
            $"HP:{maxHP} ATK:{attack} DEF:{defense} SPD:{moveSpeed}";
    }
}
