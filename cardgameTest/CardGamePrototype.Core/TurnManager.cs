namespace CardGamePrototype.Core
{
    public class TurnManager
    {
        public int HandSize { get; } = 5;

        public int PlayerEnergyPerTurn { get; } = 5;

        public void StartPlayerTurn(BattleState state)
        {
            state.Player.Energy = PlayerEnergyPerTurn;

            state.PlacementsThisTurn = 0;
            state.ExecutionsThisTurn = 0;

            DrawToHand(state);

            state.Phase = TurnPhase.PlayerTurn;
        }

        public void DrawToHand(BattleState state)
        {
            while (state.Hand.Count < HandSize)
            {
                if (state.DrawPile.Count == 0)
                    break;

                var top = state.DrawPile[0];

                state.DrawPile.RemoveAt(0);

                state.Hand.Add(top);
            }
        }

        public bool PlaceCard(
            BattleState state,
            int handIndex,
            int slotIndex)
        {
            if (handIndex < 0 || handIndex >= state.Hand.Count)
                return false;

            if (slotIndex < 0 || slotIndex >= state.PlayerBoard.Count)
                return false;

            var slot = state.PlayerBoard[slotIndex];

            if (slot.IsOccupied)
                return false;

            var card = state.Hand[handIndex];

            int placementCost =
                card.Cost + state.PlacementsThisTurn;

            if (placementCost > state.Player.Energy)
                return false;

            state.Player.Energy -= placementCost;

            state.PlacementsThisTurn++;

            slot.Card = card;

            state.Hand.RemoveAt(handIndex);

            foreach (var effect in card.CatalystEffects)
            {
                EffectResolver.ResolveEffect(effect, state);
            }

            return true;
        }

        public bool ExecuteCard(
            BattleState state,
            int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= state.PlayerBoard.Count)
                return false;

            var slot = state.PlayerBoard[slotIndex];

            if (!slot.IsOccupied)
                return false;

            int executeCost =
                state.ExecutionsThisTurn / 2;

            if (executeCost > state.Player.Energy)
                return false;

            state.Player.Energy -= executeCost;

            state.ExecutionsThisTurn++;

            var card = slot.Card!;

            foreach (var effect in card.ExecutionerEffects)
            {
                EffectResolver.ResolveEffect(effect, state);
            }

            state.BurnPile.Add(card);

            slot.Card = null;

            return true;
        }

        public void EndPlayerTurn(BattleState state)
        {
            foreach (var slot in state.PlayerBoard)
            {
                if (slot.IsOccupied)
                {
                    slot.TurnsOnBoard++;
                }
            }

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