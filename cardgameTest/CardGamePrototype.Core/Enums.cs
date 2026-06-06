namespace CardGamePrototype.Core
{
    public enum CardType
    {
        Catalyst,
        Executioner,
        Utility,
        Hybrid
    }

    public enum EffectType
    {
        ApplyStatus,
        ConsumeStatus,
        Damage,
        Move,
        ConditionalDamage,
        Composite
    }

    public enum StatusId
    {
        Burn,
        Frost
    }

    public enum TargetType
    {
        Self,
        Enemy,
        AllEnemies,
        Position
    }

    public enum TurnPhase
    {
        PlayerTurn,
        EnemyTurn,
        Resolve,
        Finished
    }
}
