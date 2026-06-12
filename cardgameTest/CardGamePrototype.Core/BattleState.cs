namespace CardGamePrototype.Core
{
    public readonly record struct DamageEvent(string Tag, int Amount);

    public class BattleState
    {
        public Player Player { get; set; }
        public Entity Enemy  { get; set; }
        public int Seed { get; }
        public Random Rng   { get; }

        public List<CardDefinition> DrawPile   { get; } = new();
        public List<CardDefinition> Hand       { get; } = new();
        public List<CardDefinition> BurnPile   { get; } = new();
        // 2-cycle cooldown: cards here return to draw pile after 2 more are played
        public List<CardDefinition> RecentPile { get; } = new();

        public List<BoardSlot> PlayerBoard { get; } = new();

        public int SelectedHandCard  { get; set; } = -1;
        public int SelectedBoardSlot { get; set; } = -1;

        public int PlacementsThisTurn  { get; set; }
        public int ExecutionsThisTurn  { get; set; }
        public int CardsPlayedTotal    { get; set; }
        public int EnemyTurnCount      { get; set; }
        public int PlayerEnergyBonus   { get; set; }

        public TurnPhase Phase { get; set; } = TurnPhase.PlayerTurn;
        public int BoardSize   { get; } = 5;

        public AbilityDefinition? EquippedAbility { get; set; }
        public float AbilityCharge                { get; set; }
        public int   AbilityUnitAttackBuff        { get; set; }

        // Damage events produced this frame; client reads then clears.
        public List<DamageEvent> DamageLog { get; } = new();

        // "standard" | "elite" | "boss"
        public string EnemyVariant { get; set; } = "standard";

        public BattleState(Player player, Entity enemy, int seed)
        {
            Player = player;
            Enemy  = enemy;
            Seed   = seed;
            Rng    = new Random(seed);
            for (int i = 0; i < 5; i++)
                PlayerBoard.Add(new BoardSlot(i));
        }
    }
}
