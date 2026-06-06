namespace CardGamePrototype.Core
{
    public class EffectDefinition
    {
        public EffectType Type { get; set; }
        public StatusId? Status { get; set; }
        public int Value { get; set; }
        public TargetType Target { get; set; }
        public StatusId? ConditionStatus { get; set; }
        public int ConditionStacks { get; set; }
        public int ConsumeStacks { get; set; }
        public bool ConditionTargetAtEdge { get; set; }
    }

    public class CardDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Cost { get; set; }
        public CardType CardType { get; set; }
        public List<EffectDefinition> Effects { get; } = new List<EffectDefinition>();
    }
}
