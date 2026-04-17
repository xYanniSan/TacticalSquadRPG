namespace TacticalRPG.DataModels
{
    public class GridTile
    {
        public GridPosition position;
        public bool isWalkable;
        public UnitRuntime occupyingUnit;

        public bool IsEmpty => occupyingUnit == null;

        public GridTile(GridPosition position, bool isWalkable = true)
        {
            this.position = position;
            this.isWalkable = isWalkable;
            this.occupyingUnit = null;
        }
    }
}
