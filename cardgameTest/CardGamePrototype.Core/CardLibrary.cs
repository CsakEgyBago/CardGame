namespace CardGamePrototype.Core
{
    public static class CardLibrary
    {
        public static IEnumerable<CardDefinition> GetAll()
        {
            yield return Ignite();
            yield return Firebolt();
            yield return Push();
            yield return FrostNova();
        }

        public static CardDefinition Ignite()
        {
            var c = new CardDefinition
            {
                Id = "ignite",
                Name = "Ignite",
                Cost = 1,
                CardType = CardType.Catalyst
            };
            c.Effects.Add(new EffectDefinition
            {
                Type = EffectType.ApplyStatus,
                Status = StatusId.Burn,
                Value = 2,
                Target = TargetType.Enemy
            });
            return c;
        }

        public static CardDefinition Firebolt()
        {
            var c = new CardDefinition
            {
                Id = "firebolt",
                Name = "Firebolt",
                Cost = 1,
                CardType = CardType.Executioner
            };
            c.Effects.Add(new EffectDefinition
            {
                Type = EffectType.Damage,
                Value = 6,
                Target = TargetType.Enemy
            });

            c.Effects.Add(new EffectDefinition
            {
                Type = EffectType.ConditionalDamage,
                Value = 12,
                Target = TargetType.Enemy,
                ConditionStatus = StatusId.Burn,
                ConditionStacks = 1,
                ConsumeStacks = 1
            });

            return c;
        }

        public static CardDefinition Push()
        {
            var c = new CardDefinition
            {
                Id = "push",
                Name = "Push",
                Cost = 1,
                CardType = CardType.Utility
            };
            c.Effects.Add(new EffectDefinition
            {
                Type = EffectType.Move,
                Value = 1,
                Target = TargetType.Enemy
            });

            c.Effects.Add(new EffectDefinition
            {
                Type = EffectType.ConditionalDamage,
                Value = 4,
                Target = TargetType.Enemy,
                ConditionTargetAtEdge = true
            });

            return c;
        }

        public static CardDefinition FrostNova()
        {
            var c = new CardDefinition
            {
                Id = "frost_nova",
                Name = "Frost Nova",
                Cost = 2,
                CardType = CardType.Hybrid
            };
            c.Effects.Add(new EffectDefinition
            {
                Type = EffectType.ApplyStatus,
                Status = StatusId.Frost,
                Value = 1,
                Target = TargetType.Enemy
            });

            c.Effects.Add(new EffectDefinition
            {
                Type = EffectType.ConditionalDamage,
                Value = 10,
                Target = TargetType.Enemy,
                ConditionStatus = StatusId.Frost,
                ConditionStacks = 1
            });

            return c;
        }
    }
}
