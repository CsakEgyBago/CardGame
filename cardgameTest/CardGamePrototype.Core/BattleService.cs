namespace CardGamePrototype.Core
{
    public class BattleService
    {
        private readonly TurnManager _turnManager = new();

        public BattleState State { get; private set; } = null!;

        public void NewBattle()
        {
            var player = new Player(50, 0);

            var enemy = new Entity("Enemy", 50, 3);

            int seed = 1337;

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

                var tmp = cards[i];
                cards[i] = cards[j];
                cards[j] = tmp;
            }

            State.DrawPile.AddRange(cards);

            _turnManager.StartPlayerTurn(State);
        }

        public bool PlaceCard(
            int handIndex,
            int slotIndex)
        {
            if (State.Phase != TurnPhase.PlayerTurn)
                return false;

            var ok =
                _turnManager.PlaceCard(
                    State,
                    handIndex,
                    slotIndex);

            CheckBattleEnd();

            return ok;
        }

        public bool ExecuteCard(
            int slotIndex)
        {
            if (State.Phase != TurnPhase.PlayerTurn)
                return false;

            var ok =
                _turnManager.ExecuteCard(
                    State,
                    slotIndex);

            CheckBattleEnd();

            return ok;
        }

        public void EndTurn()
        {
            if (State.Phase != TurnPhase.PlayerTurn)
                return;

            _turnManager.EndPlayerTurn(State);

            CheckBattleEnd();
        }

        private void CheckBattleEnd()
        {
            if (State.Player.IsDead || State.Enemy.IsDead)
            {
                State.Phase = TurnPhase.Finished;
            }
        }

        public void Restart()
        {
            NewBattle();
        }
    }
}