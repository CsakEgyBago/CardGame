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
            state.DamageLog.Add(new DamageEvent("enemy", dmg));
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
                state.DamageLog.Add(new DamageEvent($"lane_{slot.Occupant.Position}", atk));
                ChargeAbility(state, atk, dealt: true);
                slot.TurnsOnBoard++;
                if (state.Enemy.IsDead) break;
            }

            if (state.Enemy.IsDead) { state.Phase = TurnPhase.Finished; return; }

            // Status effect ticks (Fire burns, Bio poisons, Frost slows next attack)
            TickStatusEffects(state);
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

        // What will the enemy do on its next turn? Call before EndPlayerTurn.
        public static (string Action, int Damage, int Lane) GetEnemyIntent(BattleState state)
        {
            int frost = state.Enemy.ActiveElements.GetStacks(ElementType.Frost);
            int pos   = Math.Clamp(state.Enemy.Position, 0, state.PlayerBoard.Count - 1);
            var slot  = state.PlayerBoard[pos];

            switch (state.EnemyVariant)
            {
                case "elite":
                {
                    int atk = Math.Max(1, (10 + state.EnemyTurnCount) - frost * 2);
                    if (state.EnemyTurnCount % 2 == 0)
                        return ($"LUNGE → PLAYER  {atk} dmg", atk, pos);
                    return slot.IsOccupied
                        ? ($"STRIKE UNIT  {atk + 3} dmg", atk + 3, pos)
                        : ($"STRIKE PLAYER  {atk} dmg", atk, pos);
                }
                case "boss":
                {
                    int atk = Math.Max(1, (12 + state.EnemyTurnCount) - frost * 2);
                    if (state.Enemy.Hp < state.Enemy.MaxHp * 0.4f) atk = (int)(atk * 1.5f);
                    if ((state.EnemyTurnCount + 1) % 3 == 0)
                        return ($"*** AOE ALL LANES  {atk} ***", atk, -1);
                    return slot.IsOccupied
                        ? ($"CRUSH UNIT  {atk + 4} dmg", atk + 4, pos)
                        : ($"CRUSH PLAYER  {atk} dmg", atk, pos);
                }
                default:
                {
                    int atk = Math.Max(1, (8 + state.EnemyTurnCount) - frost * 2);
                    return slot.IsOccupied
                        ? ($"ATTACK UNIT  {atk + 2} dmg", atk + 2, pos)
                        : ($"ATTACK PLAYER  {atk - 2} dmg", atk - 2, pos);
                }
            }
        }

        private static void TickStatusEffects(BattleState state)
        {
            // Fire: 2 dmg per stack, -1 stack
            int fire = state.Enemy.ActiveElements.GetStacks(ElementType.Fire);
            if (fire > 0)
            {
                int burnDmg = fire * 2;
                state.Enemy.ReceiveDamage(burnDmg);
                state.DamageLog.Add(new DamageEvent("enemy_burn", burnDmg));
                state.Enemy.ActiveElements.Consume(ElementType.Fire, 1);
                ChargeAbility(state, burnDmg, dealt: true);
            }
            // Bio: 1 dmg per stack, -1 stack
            int bio = state.Enemy.ActiveElements.GetStacks(ElementType.Bio);
            if (bio > 0)
            {
                int poisonDmg = bio;
                state.Enemy.ReceiveDamage(poisonDmg);
                state.DamageLog.Add(new DamageEvent("enemy_bio", poisonDmg));
                state.Enemy.ActiveElements.Consume(ElementType.Bio, 1);
                ChargeAbility(state, poisonDmg, dealt: true);
            }
            // Frost: reduces attack (handled in GetEnemyIntent + EnemyAct), consume 1 stack
            int frost = state.Enemy.ActiveElements.GetStacks(ElementType.Frost);
            if (frost > 0)
                state.Enemy.ActiveElements.Consume(ElementType.Frost, 1);
        }

        private void EnemyAct(BattleState state)
        {
            int frost = state.Enemy.ActiveElements.GetStacks(ElementType.Frost);
            state.EnemyTurnCount++;

            switch (state.EnemyVariant)
            {
                case "elite":
                    EnemyActElite(state, frost);
                    break;
                case "boss":
                    EnemyActBoss(state, frost);
                    break;
                default:
                    EnemyActStandard(state, Math.Max(1, (8 + state.EnemyTurnCount - 1) - frost * 2));
                    break;
            }
            MoveEnemy(state);
        }

        private void EnemyActStandard(BattleState state, int attack)
        {
            int pos = Math.Clamp(state.Enemy.Position, 0, state.PlayerBoard.Count - 1);
            var slot = state.PlayerBoard[pos];
            if (slot.IsOccupied)
            {
                int dmg = attack + 2;
                slot.Occupant!.ReceiveDamage(dmg);
                state.DamageLog.Add(new DamageEvent($"lane_{pos}", -dmg));
                ChargeAbility(state, dmg, dealt: false);
                if (slot.Occupant.IsDead) slot.Occupant = null;
                return;
            }
            int playerDmg = Math.Max(1, attack - 2);
            state.Player.ReceiveDamage(playerDmg);
            state.DamageLog.Add(new DamageEvent("player", -playerDmg));
            ChargeAbility(state, playerDmg, dealt: false);
        }

        private void EnemyActElite(BattleState state, int frost)
        {
            int atk = Math.Max(1, (10 + state.EnemyTurnCount - 1) - frost * 2);
            int pos = Math.Clamp(state.Enemy.Position, 0, state.PlayerBoard.Count - 1);
            // Even turns: lunge past units, hit player directly
            if ((state.EnemyTurnCount - 1) % 2 == 0)
            {
                state.Player.ReceiveDamage(atk);
                state.DamageLog.Add(new DamageEvent("player", -atk));
                ChargeAbility(state, atk, dealt: false);
                return;
            }
            var slot = state.PlayerBoard[pos];
            if (slot.IsOccupied)
            {
                int dmg = atk + 3;
                slot.Occupant!.ReceiveDamage(dmg);
                state.DamageLog.Add(new DamageEvent($"lane_{pos}", -dmg));
                ChargeAbility(state, dmg, dealt: false);
                if (slot.Occupant.IsDead) slot.Occupant = null;
                return;
            }
            state.Player.ReceiveDamage(atk);
            state.DamageLog.Add(new DamageEvent("player", -atk));
            ChargeAbility(state, atk, dealt: false);
        }

        private void EnemyActBoss(BattleState state, int frost)
        {
            int atk = Math.Max(1, (12 + state.EnemyTurnCount - 1) - frost * 2);
            // Phase 2 below 40% HP
            if (state.Enemy.Hp < state.Enemy.MaxHp * 0.4f) atk = (int)(atk * 1.5f);

            // Every 3rd turn: AOE — damages every occupied unit AND player
            if (state.EnemyTurnCount % 3 == 0)
            {
                for (int li = 0; li < state.PlayerBoard.Count; li++)
                {
                    var sl = state.PlayerBoard[li];
                    if (sl.IsOccupied)
                    {
                        sl.Occupant!.ReceiveDamage(atk);
                        state.DamageLog.Add(new DamageEvent($"lane_{li}", -atk));
                        ChargeAbility(state, atk, dealt: false);
                        if (sl.Occupant.IsDead) sl.Occupant = null;
                    }
                }
                state.Player.ReceiveDamage(atk);
                state.DamageLog.Add(new DamageEvent("player", -atk));
                ChargeAbility(state, atk, dealt: false);
                return;
            }

            int pos = Math.Clamp(state.Enemy.Position, 0, state.PlayerBoard.Count - 1);
            var slot = state.PlayerBoard[pos];
            if (slot.IsOccupied)
            {
                int dmg = atk + 4;
                slot.Occupant!.ReceiveDamage(dmg);
                state.DamageLog.Add(new DamageEvent($"lane_{pos}", -dmg));
                ChargeAbility(state, dmg, dealt: false);
                if (slot.Occupant.IsDead) slot.Occupant = null;
                return;
            }
            state.Player.ReceiveDamage(atk);
            state.DamageLog.Add(new DamageEvent("player", -atk));
            ChargeAbility(state, atk, dealt: false);
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
