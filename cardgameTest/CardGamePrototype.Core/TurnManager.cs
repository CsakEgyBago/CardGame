namespace CardGamePrototype.Core
{
    public class TurnManager
    {
        private readonly Random _rng = new();
        public int HandSize { get; } = 5;
        public int PlayerEnergyPerTurn { get; } = 3;

        public void StartPlayerTurn(BattleState state)
        {
            state.Player.Energy = PlayerEnergyPerTurn;
            DrawToHand(state);
            state.Phase = TurnPhase.PlayerTurn;
        }

        public void DrawToHand(BattleState state)
        {
            while (state.Hand.Count < HandSize)
            {
                if (state.DrawPile.Count == 0)
                {
                    if (state.DiscardPile.Count == 0) break;
                    ShuffleIntoDraw(state);
                }
                var top = state.DrawPile[0];
                state.DrawPile.RemoveAt(0);
                state.Hand.Add(top);
            }
        }

        private void ShuffleIntoDraw(BattleState state)
        {
            var list = state.DiscardPile;
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
            state.DrawPile.AddRange(list);
            state.DiscardPile.Clear();
        }

        public bool PlayCard(BattleState state, int handIndex)
        {
            if (handIndex < 0 || handIndex >= state.Hand.Count) return false;
            var card = state.Hand[handIndex];
            if (card.Cost > state.Player.Energy) return false;

            state.Player.Energy -= card.Cost;

            foreach (var e in card.Effects)
            {
                EffectResolver.ResolveEffect(e, state);
            }

            state.DiscardPile.Add(card);
            state.Hand.RemoveAt(handIndex);
            return true;
        }

        public void EndPlayerTurn(BattleState state)
        {
            state.DiscardPile.AddRange(state.Hand);
            state.Hand.Clear();
            state.Phase = TurnPhase.EnemyTurn;
            EnemyAct(state);
            if (!state.Player.IsDead && !state.Enemy.IsDead)
            {
                StartPlayerTurn(state);
            }
            else
            {
                state.Phase = TurnPhase.Finished;
            }
        }

        private void EnemyAct(BattleState state)
        {
            state.Player.ReceiveDamage(5);
        }
    }
}
