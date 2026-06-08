namespace CardGamePrototype.Core
{
    public enum CardResolutionRole
    {
        Catalyst,
        Executioner,
        Both
    }

    public class EffectDefinition
    {
        public EffectType Type { get; set; }
        public ElementType? Element { get; set; }
        public int Value { get; set; }
        public TargetType Target { get; set; }
        public ElementType? ConditionElement { get; set; }
        public int ConditionStacks { get; set; }
        public int ConsumeStacks { get; set; }
        public bool ConditionTargetAtEdge { get; set; }
    }

    public class CardDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; // Added Description
        public int Cost { get; set; }
        public CardType CardType { get; set; }
        public CardResolutionRole ResolutionRole { get; set; } = CardResolutionRole.Both;
        public List<EffectDefinition> CatalystEffects { get; } = new List<EffectDefinition>();
        public List<EffectDefinition> ExecutionerEffects { get; } = new List<EffectDefinition>();
    }
}