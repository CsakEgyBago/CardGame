namespace CardGamePrototype.Core
{
    public class BattleService
    {
        private readonly TurnManager _turnManager = new();

        public BattleState State { get; private set; } = null!;

        public void NewBattle()
        {
            var player = new Player(50, 0);
            var enemy  = new Entity("Enemy", 50, 2);
            int seed   = 1337;

            State = new BattleState(player, enemy, seed);

            var cards = new List<CardDefinition>();
            for (int i = 0; i < 4; i++) cards.Add(CardLibrary.Ignite());
            for (int i = 0; i < 4; i++) cards.Add(CardLibrary.Firebolt());
            for (int i = 0; i < 2; i++) cards.Add(CardLibrary.Push());
            for (int i = 0; i < 2; i++) cards.Add(CardLibrary.FrostNova());
            for (int i = 0; i < 2; i++) cards.Add(CardLibrary.BioSpore());
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = State.Rng.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
            State.DrawPile.AddRange(cards);
            _turnManager.StartPlayerTurn(State);
        }

        public bool PlaceCard(int handIndex, int slotIndex)
        {
            if (State.Phase != TurnPhase.PlayerTurn) return false;
            var ok = _turnManager.PlaceCard(State, handIndex, slotIndex);
            CheckBattleEnd();
            return ok;
        }

        public bool ExecuteCard(int slotIndex)
        {
            if (State.Phase != TurnPhase.PlayerTurn) return false;
            var ok = _turnManager.ExecuteCard(State, slotIndex);
            CheckBattleEnd();
            return ok;
        }

        // Player directs a unit to attack: enemy minion (enemySlotIndex >= 0) or hero (targetHero=true).
        public bool AttackWithUnit(int playerSlotIndex, int enemySlotIndex, bool targetHero)
        {
            if (State.Phase != TurnPhase.PlayerTurn) return false;
            var ok = _turnManager.AttackWithUnit(State, playerSlotIndex, enemySlotIndex, targetHero);
            CheckBattleEnd();
            return ok;
        }

        public void EndTurn()
        {
            if (State.Phase != TurnPhase.PlayerTurn) return;
            _turnManager.EndPlayerTurn(State);
            CheckBattleEnd();
        }

        public bool ActivateAbility()
        {
            if (State.Phase != TurnPhase.PlayerTurn) return false;
            var ok = _turnManager.ActivateAbility(State);
            CheckBattleEnd();
            return ok;
        }

        private void CheckBattleEnd()
        {
            if (State.Player.IsDead || State.Enemy.IsDead)
                State.Phase = TurnPhase.Finished;
        }

        public void Restart() => NewBattle();

        public void InitPractice(int enemyHp = 100, string variant = "standard", List<CardDefinition>? starterDeck = null)
        {
            var player = new Player(80, 0);
            var enemy  = new Entity("Training Dummy", enemyHp, 2);
            State = new BattleState(player, enemy, Environment.TickCount);
            State.EnemyVariant = variant;
            State.PlayerEnergyBonus = 6; // 10 energy per turn in practice
            if (starterDeck != null)
                State.DrawPile.AddRange(starterDeck);
            _turnManager.StartPlayerTurn(State);
        }
    }
}
