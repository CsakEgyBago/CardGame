namespace CardGamePrototype.Core
{
    public static class EffectResolver
    {
        public static void ResolveEffect(EffectDefinition e, BattleState state)
        {
            switch (e.Type)
            {
                case EffectType.ApplyStatus:
                    ApplyStatus(e, state);
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
                case EffectType.ConsumeStatus:
                    ConsumeStatus(e, state);
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

        private static void ApplyStatus(EffectDefinition e, BattleState state)
        {
            var target = ResolveTarget(e, state);
            if (target == null || e.Status == null) return;
            target.Statuses.Apply(e.Status.Value, e.Value);
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

            if (e.ConditionStatus != null)
            {
                int stacks = target.Statuses.GetStacks(e.ConditionStatus.Value);
                if (stacks >= e.ConditionStacks)
                {
                    target.ReceiveDamage(e.Value);
                    if (e.ConsumeStacks > 0)
                    {
                        target.Statuses.Consume(e.ConditionStatus.Value, e.ConsumeStacks);
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

        private static void ConsumeStatus(EffectDefinition e, BattleState state)
        {
            var target = ResolveTarget(e, state);
            if (target == null || e.Status == null) return;
            target.Statuses.Consume(e.Status.Value, e.Value);
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
