namespace CardGamePrototype.Core
{
    public class BattleState
    {
        public Player Player { get; set; }
        public Entity Enemy { get; set; }
        public int Seed { get; }
        public Random Rng { get; }

        public List<CardDefinition> DrawPile { get; } = new List<CardDefinition>();
        public List<CardDefinition> DiscardPile { get; } = new List<CardDefinition>();
        public List<CardDefinition> Hand { get; } = new List<CardDefinition>();

        public TurnPhase Phase { get; set; } = TurnPhase.PlayerTurn;
        public int BoardSize { get; } = 5;

        public BattleState(Player player, Entity enemy, int seed)
        {
            Player = player;
            Enemy = enemy;
            Seed = seed;
            Rng = new Random(seed);
        }
    }
}
