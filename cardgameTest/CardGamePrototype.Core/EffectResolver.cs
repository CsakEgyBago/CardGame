namespace CardGamePrototype.Core
{
    public static class EffectResolver
    {
        public static void ResolveEffect(EffectDefinition e, BattleState state)
        {
            switch (e.Type)
            {
                case EffectType.ApplyElement:
                    ApplyElement(e, state);
                    break;
                case EffectType.Damage:
                    Damage(e, state);
                    break;
                case EffectType.ConditionalDamage:
                    ConditionalDamage(e, state);
                    break;
                case EffectType.Move:
                    Move(e, state);
                    break;
                case EffectType.ConsumeElement:
                    ConsumeElement(e, state);
                    break;
                case EffectType.HealPlayer:
                    state.Player.Hp = Math.Min(state.Player.MaxHp, state.Player.Hp + e.Value);
                    break;
                case EffectType.Composite:
                    break;
            }
        }

        private static Entity? ResolveTarget(EffectDefinition e, BattleState state)
        {
            return e.Target switch
            {
                TargetType.Enemy => state.Enemy,
                TargetType.Self => state.Player,
                _ => state.Enemy
            };
        }

        private static void ApplyElement(EffectDefinition e, BattleState state)
        {
            var target = ResolveTarget(e, state);
            if (target == null || e.Element == null) return;
            target.ActiveElements.Apply(e.Element.Value, e.Value);
        }

        private static void Damage(EffectDefinition e, BattleState state)
        {
            var target = ResolveTarget(e, state);
            if (target == null) return;
            target.ReceiveDamage(e.Value);
        }

        private static void ConditionalDamage(EffectDefinition e, BattleState state)
        {
            var target = ResolveTarget(e, state);
            if (target == null) return;

            if (e.ConditionElement != null)
            {
                int stacks = target.ActiveElements.GetStacks(e.ConditionElement.Value);
                if (stacks >= e.ConditionStacks)
                {
                    target.ReceiveDamage(e.Value);
                    if (e.ConsumeStacks > 0)
                    {
                        target.ActiveElements.Consume(e.ConditionElement.Value, e.ConsumeStacks);
                    }
                }
                return;
            }

            if (e.ConditionTargetAtEdge)
            {
                bool atEdge = target.Position >= state.BoardSize - 1 || target.Position <= 0;
                if (atEdge)
                {
                    target.ReceiveDamage(e.Value);
                }
                return;
            }

            target.ReceiveDamage(e.Value);
        }

        private static void ConsumeElement(EffectDefinition e, BattleState state)
        {
            var target = ResolveTarget(e, state);
            if (target == null || e.Element == null) return;
            target.ActiveElements.Consume(e.Element.Value, e.Value);
        }

        private static void Move(EffectDefinition e, BattleState state)
        {
            var target = ResolveTarget(e, state);
            if (target == null) return;

            int newPos = target.Position + e.Value;
            if (newPos < 0) newPos = 0;
            if (newPos > state.BoardSize - 1) newPos = state.BoardSize - 1;
            target.Position = newPos;
        }
    }
}
