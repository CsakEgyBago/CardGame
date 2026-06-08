namespace CardGamePrototype.Core
{
    public class BattleState
    {
        public Player Player { get; set; }
        public Entity Enemy { get; set; }

        public int Seed { get; }

        public Random Rng { get; }

        public List<CardDefinition> DrawPile { get; } = new();

        public List<CardDefinition> Hand { get; } = new();

        public List<CardDefinition> BurnPile { get; } = new();

        public List<BoardSlot> PlayerBoard { get; } = new();

        public int SelectedHandCard { get; set; } = -1;

        public int SelectedBoardSlot { get; set; } = -1;

        public int PlacementsThisTurn { get; set; }

        public int ExecutionsThisTurn { get; set; }

        public TurnPhase Phase { get; set; } = TurnPhase.PlayerTurn;

        public int BoardSize { get; } = 5;

        public BattleState(Player player, Entity enemy, int seed)
        {
            Player = player;
            Enemy = enemy;
            Seed = seed;
            Rng = new Random(seed);

            for (int i = 0; i < 3; i++)
            {
                PlayerBoard.Add(new BoardSlot(i));
            }
        }
    }
}