using System.Collections.Generic;

namespace TacticalRPG.DataModels
{
    public class BattleState
    {
        // Battle Identity
        public string battleId;
        public BattlePhase currentPhase;

        // Units
        public List<UnitRuntime> playerUnits;
        public List<UnitRuntime> enemyUnits;

        // Grid
        public GridMap grid;

        // Timing
        public float battleTime;
        public int currentTick;

        // Victory State
        public bool isBattleOver;
        public BattleOutcome outcome;

        // Combat Context (active resolution)
        public CombatContext activeCombat;

        public BattleState(string battleId, int gridWidth, int gridHeight)
        {
            this.battleId = battleId;
            currentPhase = BattlePhase.NotStarted;
            playerUnits = new List<UnitRuntime>();
            enemyUnits = new List<UnitRuntime>();
            grid = new GridMap(gridWidth, gridHeight);
            battleTime = 0f;
            currentTick = 0;
            isBattleOver = false;
            outcome = BattleOutcome.None;
            activeCombat = null;
        }

        public List<UnitRuntime> GetAllUnits()
        {
            var all = new List<UnitRuntime>(playerUnits);
            all.AddRange(enemyUnits);
            return all;
        }

        public List<UnitRuntime> GetLivingUnits(UnitTeam team)
        {
            var units = team == UnitTeam.Player ? playerUnits : enemyUnits;
            return units.FindAll(u => !u.isDead);
        }

        public bool IsTeamDefeated(UnitTeam team)
        {
            var units = team == UnitTeam.Player ? playerUnits : enemyUnits;
            return units.Count > 0 && units.TrueForAll(u => u.isDead);
        }

        public void StartBattle()
        {
            currentPhase = BattlePhase.Combat;
            battleTime = 0f;
            currentTick = 0;
        }

        public void EndBattle(BattleOutcome result)
        {
            isBattleOver = true;
            outcome = result;
            currentPhase = result == BattleOutcome.Victory ? BattlePhase.Victory : BattlePhase.Defeat;
        }
    }
}
