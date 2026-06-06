namespace CardGamePrototype.Core
{
    public class BattleState
    {
        public Player Player { get; set; }
        public Entity Enemy { get; set; }

        public List<CardDefinition> DrawPile { get; } = new List<CardDefinition>();
        public List<CardDefinition> DiscardPile { get; } = new List<CardDefinition>();
        public List<CardDefinition> Hand { get; } = new List<CardDefinition>();

        public TurnPhase Phase { get; set; } = TurnPhase.PlayerTurn;
        public int BoardSize { get; } = 5;

        public BattleState(Player player, Entity enemy)
        {
            Player = player;
            Enemy = enemy;
        }
    }
}
