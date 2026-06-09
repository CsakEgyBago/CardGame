namespace CardGamePrototype.Core
{
    public enum AbilityEffectType { HealPlayer, BuffAllUnitAttack, NukeDamage, RefundEnergy }

    public class AbilityDefinition
    {
        public string Id   { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int   MaxCharge             { get; set; } = 20;
        public float ChargePerDamageDealt  { get; set; } = 1f;
        public float ChargePerDamageTaken  { get; set; } = 0.5f;
        public AbilityEffectType Effect    { get; set; }
        public int   EffectValue           { get; set; }
    }

    public static class AbilityLibrary
    {
        public static List<AbilityDefinition> GetAll() =>
            [Overclock(), EmergencyShield(), CatalystBurst(), EnergySurge()];

        public static AbilityDefinition Overclock() => new()
        {
            Id = "overclock", Name = "Overclock",
            Description = "All units +3 ATK / +2 energy",
            MaxCharge = 20, ChargePerDamageDealt = 1f, ChargePerDamageTaken = 0.35f,
            Effect = AbilityEffectType.BuffAllUnitAttack, EffectValue = 3
        };

        public static AbilityDefinition EmergencyShield() => new()
        {
            Id = "shield", Name = "Shield",
            Description = "Restore 20 HP",
            MaxCharge = 15, ChargePerDamageDealt = 0.2f, ChargePerDamageTaken = 1.5f,
            Effect = AbilityEffectType.HealPlayer, EffectValue = 20
        };

        public static AbilityDefinition CatalystBurst() => new()
        {
            Id = "catalyst_burst", Name = "Catalyst Burst",
            Description = "Deal 18 direct damage",
            MaxCharge = 25, ChargePerDamageDealt = 1.4f, ChargePerDamageTaken = 0.4f,
            Effect = AbilityEffectType.NukeDamage, EffectValue = 18
        };

        public static AbilityDefinition EnergySurge() => new()
        {
            Id = "energy_surge", Name = "Energy Surge",
            Description = "Gain 5 energy this turn",
            MaxCharge = 18, ChargePerDamageDealt = 0.8f, ChargePerDamageTaken = 0.8f,
            Effect = AbilityEffectType.RefundEnergy, EffectValue = 5
        };
    }
}
