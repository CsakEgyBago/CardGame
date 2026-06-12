namespace CardGamePrototype.Core
{
    public enum ElementType : byte
    {
        None,
        Physical,
        Fire,
        Frost,
        Void,
        Lightning,
        Bio
    }

    public enum CardType
    {
        Strike,
        Incantation,
        Construct,
        Reaction
    }

    public enum EffectType
    {
        ApplyElement,
        ConsumeElement,
        Damage,
        Move,
        ConditionalDamage,
        Composite,
        HealPlayer,
        BuffPlayerUnits,
        DebuffEnemyUnits,
        StunAllEnemies,
        SetFieldEffect,
        SacrificeSelf,
        DamageAllEnemyMinions,
        AddArmorAllUnits,
        DamageMissingHp,
        GainEnergy
    }

    public enum FieldEffectType
    {
        None,
        FrozenField,
        ScorchedEarth,
        StaticStorm,
        VoidRift
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
