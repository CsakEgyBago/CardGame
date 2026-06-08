namespace CardGamePrototype.Core
{
    public class BoardSlot
    {
        public int Index { get; }

        public CardDefinition? Card { get; set; }

        public int TurnsOnBoard { get; set; }

        public bool IsOccupied => Card != null;

        public BoardSlot(int index)
        {
            Index = index;
        }
    }
}