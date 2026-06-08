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
            while (state.Hand.Count < HandSize && state.DrawPile.Count > 0)
            {
                var top = state.DrawPile[0];
                state.DrawPile.RemoveAt(0);
                state.Hand.Add(top);
            }
        }

        public bool PlaceCard(BattleState state, int handIndex, int slotIndex)
        {
            if (handIndex < 0 || handIndex >= state.Hand.Count) return false;
            if (slotIndex < 0 || slotIndex >= state.PlayerBoard.Count) return false;

            var slot = state.PlayerBoard[slotIndex];
            if (slot.IsOccupied) return false;

            var card = state.Hand[handIndex];
            int cost = card.Cost + state.PlacementsThisTurn;
            if (cost > state.Player.Energy) return false;

            state.Player.Energy -= cost;
            state.PlacementsThisTurn++;

            slot.Occupant = new SummonedEntity(card.Name, card.MinionHp, slotIndex, card)
            {
                BaseAttack = card.MinionAttack
            };

            state.Hand.RemoveAt(handIndex);

            foreach (var effect in card.CatalystEffects)
                EffectResolver.ResolveEffect(effect, state);

            return true;
        }

        // Execute = unit fires its special ability (2× attack + card ExecutionerEffects), then is consumed
        public bool ExecuteCard(BattleState state, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= state.PlayerBoard.Count) return false;

            var slot = state.PlayerBoard[slotIndex];
            if (!slot.IsOccupied) return false;

            int executeCost = state.ExecutionsThisTurn / 2;
            if (executeCost > state.Player.Energy) return false;

            state.Player.Energy -= executeCost;
            state.ExecutionsThisTurn++;

            var unit = slot.Occupant!;
            // Execute deals double the unit's base attack before effects
            state.Enemy.ReceiveDamage(unit.BaseAttack * 2);

            foreach (var effect in unit.SourceCard.ExecutionerEffects)
                EffectResolver.ResolveEffect(effect, state);

            state.BurnPile.Add(unit.SourceCard);
            slot.Occupant = null;
            return true;
        }

        public void EndPlayerTurn(BattleState state)
        {
            // All placed units attack the enemy
            foreach (var slot in state.PlayerBoard)
            {
                if (!slot.IsOccupied) continue;
                state.Enemy.ReceiveDamage(slot.Occupant!.BaseAttack);
                slot.TurnsOnBoard++;
                if (state.Enemy.IsDead) break;
            }

            if (state.Enemy.IsDead) { state.Phase = TurnPhase.Finished; return; }

            state.Phase = TurnPhase.EnemyTurn;
            EnemyAct(state);

            if (!state.Player.IsDead && !state.Enemy.IsDead)
                StartPlayerTurn(state);
            else
                state.Phase = TurnPhase.Finished;
        }

        private void EnemyAct(BattleState state)
        {
            int attack = 8 + state.EnemyTurnCount;   // escalates each turn

            // Try to strike the unit in the enemy's current lane
            if (state.Enemy.Position >= 0 && state.Enemy.Position < state.PlayerBoard.Count)
            {
                var slot = state.PlayerBoard[state.Enemy.Position];
                if (slot.IsOccupied)
                {
                    slot.Occupant!.ReceiveDamage(attack + 2); // slightly stronger vs units
                    if (slot.Occupant.IsDead)
                        slot.Occupant = null;

                    state.EnemyTurnCount++;
                    MoveEnemy(state);
                    return;
                }
            }

            // No unit blocking — hit the player
            state.Player.ReceiveDamage(attack - 2);
            state.EnemyTurnCount++;
            MoveEnemy(state);
        }

        private static void MoveEnemy(BattleState state)
        {
            // Every other turn the enemy drifts one step
            if (state.EnemyTurnCount % 2 != 0) return;

            // Look for an occupied lane to move toward; otherwise wander
            int target = state.Enemy.Position;
            for (int i = 0; i < state.PlayerBoard.Count; i++)
            {
                if (state.PlayerBoard[i].IsOccupied) { target = i; break; }
            }

            int dir = target > state.Enemy.Position ? 1 : target < state.Enemy.Position ? -1 : (state.Rng.Next(2) == 0 ? 1 : -1);
            state.Enemy.Position = Math.Clamp(state.Enemy.Position + dir, 0, state.BoardSize - 1);
        }
    }
}
