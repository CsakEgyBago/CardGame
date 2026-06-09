namespace CardGamePrototype.Core
{
    public class TurnManager
    {
        public const int HandSize          = 5;
        public const int BaseEnergyPerTurn = 4;

        public void StartPlayerTurn(BattleState state)
        {
            state.Player.Energy         = BaseEnergyPerTurn + state.PlayerEnergyBonus;
            state.PlacementsThisTurn    = 0;
            state.ExecutionsThisTurn    = 0;
            state.AbilityUnitAttackBuff = 0;
            DrawToHand(state);
            state.Phase = TurnPhase.PlayerTurn;
        }

        public void DrawToHand(BattleState state)
        {
            // Scan draw pile; skip cards whose Id is already in hand
            int scan = 0;
            while (state.Hand.Count < HandSize && scan < state.DrawPile.Count)
            {
                var candidate = state.DrawPile[scan];
                if (state.Hand.Any(h => h.Id == candidate.Id))
                    scan++;
                else
                {
                    state.DrawPile.RemoveAt(scan);
                    state.Hand.Add(candidate);
                }
            }
        }

        // 2-cycle cooldown: played card sits in recent pile until 2 more are played,
        // then it returns to the bottom of the draw pile.
        private static void CycleCard(BattleState state, CardDefinition card)
        {
            if (state.RecentPile.Count >= 2)
            {
                var oldest = state.RecentPile[0];
                state.RecentPile.RemoveAt(0);
                state.DrawPile.Add(oldest);
            }
            state.RecentPile.Add(card);
            state.CardsPlayedTotal++;
        }

        public bool PlaceCard(BattleState state, int handIndex, int slotIndex)
        {
            if (handIndex < 0 || handIndex >= state.Hand.Count)        return false;
            if (slotIndex < 0 || slotIndex >= state.PlayerBoard.Count) return false;
            var slot = state.PlayerBoard[slotIndex];
            if (slot.IsOccupied) return false;

            var card = state.Hand[handIndex];
            if (card.Cost > state.Player.Energy) return false;

            state.Player.Energy -= card.Cost;
            state.PlacementsThisTurn++;

            slot.Occupant = new SummonedEntity(card.Name, card.MinionHp, slotIndex, card)
            {
                BaseAttack = card.MinionAttack
            };

            state.Hand.RemoveAt(handIndex);
            CycleCard(state, card);

            foreach (var effect in card.CatalystEffects)
                EffectResolver.ResolveEffect(effect, state);

            return true;
        }

        // Execute costs 1 energy — less punishing than placing
        public bool ExecuteCard(BattleState state, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= state.PlayerBoard.Count) return false;
            var slot = state.PlayerBoard[slotIndex];
            if (!slot.IsOccupied) return false;
            if (state.Player.Energy < 1) return false;

            state.Player.Energy--;
            state.ExecutionsThisTurn++;

            var unit = slot.Occupant!;
            int dmg = (unit.BaseAttack + state.AbilityUnitAttackBuff) * 2;
            state.Enemy.ReceiveDamage(dmg);
            ChargeAbility(state, dmg, dealt: true);

            foreach (var effect in unit.SourceCard.ExecutionerEffects)
                EffectResolver.ResolveEffect(effect, state);

            CycleCard(state, unit.SourceCard);
            state.BurnPile.Add(unit.SourceCard);
            slot.Occupant = null;
            return true;
        }

        public void EndPlayerTurn(BattleState state)
        {
            foreach (var slot in state.PlayerBoard)
            {
                if (!slot.IsOccupied) continue;
                int atk = slot.Occupant!.BaseAttack + state.AbilityUnitAttackBuff;
                state.Enemy.ReceiveDamage(atk);
                ChargeAbility(state, atk, dealt: true);
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

        public bool ActivateAbility(BattleState state)
        {
            if (state.EquippedAbility == null) return false;
            if (state.AbilityCharge < state.EquippedAbility.MaxCharge) return false;

            state.AbilityCharge = 0;
            switch (state.EquippedAbility.Effect)
            {
                case AbilityEffectType.HealPlayer:
                    state.Player.Hp = Math.Min(state.Player.MaxHp,
                        state.Player.Hp + state.EquippedAbility.EffectValue);
                    break;
                case AbilityEffectType.BuffAllUnitAttack:
                    state.AbilityUnitAttackBuff += state.EquippedAbility.EffectValue;
                    state.Player.Energy         += 2;
                    break;
                case AbilityEffectType.NukeDamage:
                    state.Enemy.ReceiveDamage(state.EquippedAbility.EffectValue);
                    break;
                case AbilityEffectType.RefundEnergy:
                    state.Player.Energy += state.EquippedAbility.EffectValue;
                    break;
            }
            return true;
        }

        private static void ChargeAbility(BattleState state, int damage, bool dealt)
        {
            if (state.EquippedAbility == null) return;
            float rate = dealt ? state.EquippedAbility.ChargePerDamageDealt
                               : state.EquippedAbility.ChargePerDamageTaken;
            state.AbilityCharge = Math.Min(state.AbilityCharge + damage * rate,
                                           state.EquippedAbility.MaxCharge);
        }

        private void EnemyAct(BattleState state)
        {
            int attack = 8 + state.EnemyTurnCount;
            state.EnemyTurnCount++;

            if (state.Enemy.Position >= 0 && state.Enemy.Position < state.PlayerBoard.Count)
            {
                var slot = state.PlayerBoard[state.Enemy.Position];
                if (slot.IsOccupied)
                {
                    int dmg = attack + 2;
                    slot.Occupant!.ReceiveDamage(dmg);
                    ChargeAbility(state, dmg, dealt: false);
                    if (slot.Occupant.IsDead) slot.Occupant = null;
                    MoveEnemy(state);
                    return;
                }
            }

            int playerDmg = attack - 2;
            state.Player.ReceiveDamage(playerDmg);
            ChargeAbility(state, playerDmg, dealt: false);
            MoveEnemy(state);
        }

        private static void MoveEnemy(BattleState state)
        {
            if (state.EnemyTurnCount % 2 != 0) return;
            int target = state.Enemy.Position;
            for (int i = 0; i < state.PlayerBoard.Count; i++)
                if (state.PlayerBoard[i].IsOccupied) { target = i; break; }
            int dir = target > state.Enemy.Position ? 1
                    : target < state.Enemy.Position ? -1
                    : (state.Rng.Next(2) == 0 ? 1 : -1);
            state.Enemy.Position = Math.Clamp(state.Enemy.Position + dir, 0, state.BoardSize - 1);
        }
    }
}
