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

            // Clear attack exhaustion on all player units
            foreach (var slot in state.PlayerBoard)
                if (slot.IsOccupied) slot.Occupant!.HasAttackedThisTurn = false;

            DrawToHand(state);
            state.Phase = TurnPhase.PlayerTurn;
        }

        public void DrawToHand(BattleState state)
        {
            int target = HandSize + state.HandSizeBonus;
            int scan = 0;
            while (state.Hand.Count < target && scan < state.DrawPile.Count)
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
            if (handIndex < 0 || handIndex >= state.Hand.Count) return false;
            var card = state.Hand[handIndex];
            if (card.Cost > state.Player.Energy) return false;

            // Incantation (spell) cards auto-cast
            if (card.CardType == CardType.Incantation)
            {
                state.Player.Energy -= card.Cost;
                state.PlacementsThisTurn++;
                state.Hand.RemoveAt(handIndex);
                CycleCard(state, card);
                foreach (var effect in card.CatalystEffects)
                    EffectResolver.ResolveEffect(effect, state);
                foreach (var effect in card.ExecutionerEffects)
                    EffectResolver.ResolveEffect(effect, state);
                return true;
            }

            if (slotIndex < 0 || slotIndex >= state.PlayerBoard.Count) return false;
            var slot = state.PlayerBoard[slotIndex];
            if (slot.IsOccupied) return false;

            state.Player.Energy -= card.Cost;
            state.PlacementsThisTurn++;

            slot.Occupant = new SummonedEntity(card.Name, card.MinionHp, slotIndex, card)
            {
                BaseAttack = card.MinionAttack + state.MinionAttackBonus
            };
            slot.TurnsOnBoard = 0;

            state.Hand.RemoveAt(handIndex);
            CycleCard(state, card);

            foreach (var effect in card.CatalystEffects)
                EffectResolver.ResolveEffect(effect, state);

            return true;
        }

        // Execute: unit uses its special ability (ExecutionerEffects) + deals ATK to enemy hero. Costs 1 energy.
        // This exhausts the unit so it cannot attack this turn.
        public bool ExecuteCard(BattleState state, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= state.PlayerBoard.Count) return false;
            var slot = state.PlayerBoard[slotIndex];
            if (!slot.IsOccupied) return false;
            if (state.Player.Energy < 1) return false;
            if (slot.Occupant!.HasAttackedThisTurn) return false;
            if (slot.TurnsOnBoard == 0) return false;

            state.Player.Energy--;
            state.ExecutionsThisTurn++;

            var unit = slot.Occupant!;
            unit.HasAttackedThisTurn = true;

            int atk = unit.BaseAttack + unit.AttackBonus + state.AbilityUnitAttackBuff;
            state.Enemy.ReceiveDamage(atk);
            state.DamageLog.Add(new DamageEvent("enemy", atk));
            ChargeAbility(state, atk, dealt: true);

            foreach (var effect in unit.SourceCard.ExecutionerEffects)
                EffectResolver.ResolveEffect(effect, state);

            return true;
        }

        // Player directs a unit to attack an enemy minion or the enemy hero.
        // Attacking a minion deals reciprocal damage. Attacking the hero has no counterattack.
        public bool AttackWithUnit(BattleState state, int playerSlotIndex, int enemySlotIndex, bool targetHero)
        {
            if (playerSlotIndex < 0 || playerSlotIndex >= state.PlayerBoard.Count) return false;
            var pSlot = state.PlayerBoard[playerSlotIndex];
            if (!pSlot.IsOccupied) return false;
            var attacker = pSlot.Occupant!;
            if (attacker.HasAttackedThisTurn) return false;
            if (pSlot.TurnsOnBoard == 0) return false; // summoning sickness

            int atk = attacker.BaseAttack + attacker.AttackBonus + state.AbilityUnitAttackBuff;
            attacker.HasAttackedThisTurn = true;

            if (targetHero)
            {
                state.Enemy.ReceiveDamage(atk);
                state.DamageLog.Add(new DamageEvent("enemy", atk));
                ChargeAbility(state, atk, dealt: true);
            }
            else
            {
                if (enemySlotIndex < 0 || enemySlotIndex >= state.EnemyBoard.Count) return false;
                var eSlot = state.EnemyBoard[enemySlotIndex];
                if (!eSlot.IsOccupied) return false;
                var defender = eSlot.Occupant!;

                // Attacker hits defender (enemy armor reduces damage)
                int actualAtk = Math.Max(0, atk - defender.Armor);
                defender.ReceiveDamage(actualAtk);
                state.DamageLog.Add(new DamageEvent($"enemy_lane_{enemySlotIndex}", actualAtk));
                ChargeAbility(state, actualAtk, dealt: true);

                // Counter-attack: defender hits attacker back (attacker armor reduces damage)
                int counterAtk = Math.Max(0, (defender.BaseAttack + defender.AttackBonus) - attacker.Armor);
                attacker.ReceiveDamage(counterAtk);
                state.DamageLog.Add(new DamageEvent($"lane_{playerSlotIndex}", -counterAtk));
                ChargeAbility(state, counterAtk, dealt: false);

                if (defender.IsDead) eSlot.Occupant = null;
                if (attacker.IsDead) { state.BurnPile.Add(attacker.SourceCard); pSlot.Occupant = null; }
            }

            return true;
        }

        public void EndPlayerTurn(BattleState state)
        {
            // Increment turns-on-board (clears summoning sickness)
            foreach (var slot in state.PlayerBoard)
                if (slot.IsOccupied) slot.TurnsOnBoard++;

            if (state.Enemy.IsDead) { state.Phase = TurnPhase.Finished; return; }

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

        // Returns a summary of what the enemy plans to do next turn.
        public static string GetEnemyIntent(BattleState state)
        {
            int minionCount = state.EnemyBoard.Count(s => s.IsOccupied);
            int handCount   = state.EnemyHand.Count;
            int energy = state.EnemyVariant switch {
                "boss"  => 5,
                "elite" => 3,
                _       => 2
            };
            int frost = state.Enemy.ActiveElements.GetStacks(ElementType.Frost);
            energy = Math.Max(1, energy - frost);
            bool willPlay = state.EnemyVariant == "standard"
                ? (state.EnemyTurnCount + 1) % 2 == 0
                : true;
            var affordable = state.EnemyHand.Where(c => c.Cost <= energy).ToList();
            string playPart = !willPlay ? "Holds back" :
                (affordable.Count > 0 ? $"Summon {affordable.First().Name}"
                : (handCount > 0 ? "Pass (no energy)" : "Draw cards"));
            string atkPart = minionCount > 0 ? $"  +  {minionCount} minion{(minionCount > 1 ? "s" : "")} attack" : "";
            return $"{playPart}{atkPart}";
        }

        private static void ProcessFieldEffect(BattleState state)
        {
            switch (state.ActiveField)
            {
                case FieldEffectType.FrozenField:
                    // 35% chance to stun each enemy minion
                    foreach (var s in state.EnemyBoard)
                        if (s.IsOccupied && state.Rng.NextDouble() < 0.35)
                            s.Occupant!.IsStunned = true;
                    break;
                case FieldEffectType.ScorchedEarth:
                    // All minions take 2 damage
                    foreach (var s in state.PlayerBoard)
                        if (s.IsOccupied)
                        {
                            s.Occupant!.ReceiveDamage(2);
                            if (s.Occupant.IsDead) { state.BurnPile.Add(s.Occupant.SourceCard); s.Occupant = null; }
                        }
                    foreach (var s in state.EnemyBoard)
                        if (s.IsOccupied)
                        {
                            s.Occupant!.ReceiveDamage(2);
                            if (s.Occupant.IsDead) s.Occupant = null;
                        }
                    break;
                case FieldEffectType.StaticStorm:
                    // All minions gain +1 ATK but take 2 damage
                    foreach (var s in state.PlayerBoard)
                        if (s.IsOccupied)
                        {
                            s.Occupant!.AttackBonus++;
                            s.Occupant.ReceiveDamage(2);
                            if (s.Occupant.IsDead) { state.BurnPile.Add(s.Occupant.SourceCard); s.Occupant = null; }
                        }
                    foreach (var s in state.EnemyBoard)
                        if (s.IsOccupied)
                        {
                            s.Occupant!.AttackBonus++;
                            s.Occupant.ReceiveDamage(2);
                            if (s.Occupant.IsDead) s.Occupant = null;
                        }
                    break;
                case FieldEffectType.VoidRift:
                    // 3 damage to all enemy minions
                    foreach (var s in state.EnemyBoard)
                        if (s.IsOccupied)
                        {
                            s.Occupant!.ReceiveDamage(3);
                            if (s.Occupant.IsDead) s.Occupant = null;
                        }
                    break;
            }
        }

        private static void TickStatusEffects(BattleState state)
        {
            int fire = state.Enemy.ActiveElements.GetStacks(ElementType.Fire);
            if (fire > 0)
            {
                int burnDmg = fire * 2;
                state.Enemy.ReceiveDamage(burnDmg);
                state.DamageLog.Add(new DamageEvent("enemy_burn", burnDmg));
                state.Enemy.ActiveElements.Consume(ElementType.Fire, 1);
                ChargeAbility(state, burnDmg, dealt: true);
            }
            int bio = state.Enemy.ActiveElements.GetStacks(ElementType.Bio);
            if (bio > 0)
            {
                int poisonDmg = bio;
                state.Enemy.ReceiveDamage(poisonDmg);
                state.DamageLog.Add(new DamageEvent("enemy_bio", poisonDmg));
                state.Enemy.ActiveElements.Consume(ElementType.Bio, 1);
                ChargeAbility(state, poisonDmg, dealt: true);
            }
            int frost = state.Enemy.ActiveElements.GetStacks(ElementType.Frost);
            if (frost > 0)
                state.Enemy.ActiveElements.Consume(ElementType.Frost, 1);
            int lightning = state.Enemy.ActiveElements.GetStacks(ElementType.Lightning);
            if (lightning > 0)
            {
                int shockDmg = lightning * 3;
                state.Enemy.ReceiveDamage(shockDmg);
                state.DamageLog.Add(new DamageEvent("enemy_lightning", shockDmg));
                state.Enemy.ActiveElements.Consume(ElementType.Lightning, lightning);
                ChargeAbility(state, shockDmg, dealt: true);
            }

            // Process active field effect
            if (state.ActiveField != FieldEffectType.None)
            {
                ProcessFieldEffect(state);
                state.FieldEffectDuration--;
                if (state.FieldEffectDuration <= 0)
                    state.ActiveField = FieldEffectType.None;
            }
        }

        private void EnemyAct(BattleState state)
        {
            state.EnemyTurnCount++;
            int frost = state.Enemy.ActiveElements.GetStacks(ElementType.Frost);

            // Reset attack exhaustion for all existing enemy minions (not newly played ones)
            foreach (var slot in state.EnemyBoard)
                if (slot.IsOccupied) { slot.Occupant!.HasAttackedThisTurn = false; slot.TurnsOnBoard++; }

            // Fixed energy — no scaling to prevent snowballing
            int energy = state.EnemyVariant switch {
                "boss"  => 5,
                "elite" => 3,
                _       => 2
            };
            energy = Math.Max(1, energy - frost);

            // Draw 1 card per turn (max hand = 5)
            DrawEnemyCards(state, 1);

            // Standard enemy only plays a card every other turn (odd turns)
            bool playsThisTurn = state.EnemyVariant == "standard"
                ? state.EnemyTurnCount % 2 == 0
                : true;
            if (playsThisTurn)
                EnemyPlayCards(state, energy);

            // All enemy minions attack
            EnemyMinionAttack(state, frost);
        }

        private static void DrawEnemyCards(BattleState state, int count)
        {
            for (int i = 0; i < count && state.EnemyHand.Count < 5; i++)
            {
                if (state.EnemyDrawPile.Count == 0) break; // deck exhausted
                state.EnemyHand.Add(state.EnemyDrawPile[0]);
                state.EnemyDrawPile.RemoveAt(0);
            }
        }

        private static void EnemyPlayCards(BattleState state, int energy)
        {
            bool played = true;
            int playsThisTurn = 0;
            int maxPlays = state.EnemyVariant switch { "boss" => 2, "elite" => 1, _ => 1 };
            while (played && energy > 0 && state.EnemyHand.Count > 0 && playsThisTurn < maxPlays)
            {
                played = false;
                // Find cheapest affordable card
                CardDefinition? toPlay = null;
                foreach (var c in state.EnemyHand.OrderBy(c => c.Cost))
                {
                    if (c.Cost <= energy) { toPlay = c; break; }
                }
                if (toPlay == null) break;

                // Find first empty enemy slot
                BoardSlot? target = null;
                foreach (var s in state.EnemyBoard)
                    if (!s.IsOccupied) { target = s; break; }
                if (target == null) break; // board full

                target.Occupant = new SummonedEntity(toPlay.Name, toPlay.MinionHp, target.Index, toPlay)
                {
                    BaseAttack = toPlay.MinionAttack,
                    HasAttackedThisTurn = true // summoning sickness: can't attack turn they're played
                };
                target.TurnsOnBoard = 0;
                energy -= toPlay.Cost;
                state.EnemyHand.Remove(toPlay);
                played = true;
                playsThisTurn++;
            }
        }

        private static void EnemyMinionAttack(BattleState state, int frost)
        {
            foreach (var eSlot in state.EnemyBoard)
            {
                if (!eSlot.IsOccupied || state.Player.IsDead) continue;
                var minion = eSlot.Occupant!;
                if (minion.HasAttackedThisTurn) continue; // summoning sickness

                // Stun: skip attack, clear stun
                if (minion.IsStunned) { minion.IsStunned = false; continue; }

                int atk = Math.Max(1, (minion.BaseAttack + minion.AttackBonus) - frost);

                // Prefer attacking player unit in same lane
                var pSlot = state.PlayerBoard[eSlot.Index];
                if (pSlot.IsOccupied)
                {
                    var defender = pSlot.Occupant!;
                    // Defender armor reduces incoming damage
                    int actualAtk = Math.Max(0, atk - defender.Armor);
                    defender.ReceiveDamage(actualAtk);
                    state.DamageLog.Add(new DamageEvent($"lane_{eSlot.Index}", -actualAtk));
                    ChargeAbility(state, actualAtk, dealt: false);

                    // Counter-attack: player unit hits minion back (minion armor reduces counter)
                    int counter = Math.Max(0, (defender.BaseAttack + defender.AttackBonus) - minion.Armor);
                    minion.ReceiveDamage(counter);

                    if (defender.IsDead) { state.BurnPile.Add(defender.SourceCard); pSlot.Occupant = null; }
                    if (minion.IsDead)   eSlot.Occupant = null;
                }
                else
                {
                    // Lane open — attack player hero
                    state.Player.ReceiveDamage(atk);
                    state.DamageLog.Add(new DamageEvent("player", -atk));
                    ChargeAbility(state, atk, dealt: false);
                }
            }
        }
    }
}
