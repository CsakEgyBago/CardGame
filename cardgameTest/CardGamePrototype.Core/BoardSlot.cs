namespace CardGamePrototype.Core
{
    public class BoardSlot
    {
        public int Index { get; }

        public SummonedEntity? Occupant { get; set; }

        public int TurnsOnBoard { get; set; }

        public bool IsOccupied => Occupant != null;

        public BoardSlot(int index)
        {
            Index = index;
        }
    }
}