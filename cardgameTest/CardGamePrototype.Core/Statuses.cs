namespace CardGamePrototype.Core
{
    public class StatusInstance
    {
        public StatusId Id { get; }
        public int Stacks { get; private set; }

        public StatusInstance(StatusId id, int stacks)
        {
            Id = id;
            Stacks = stacks;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Stacks += amount;
        }

        public int Consume(int amount)
        {
            if (amount <= 0) return 0;
            int consumed = System.Math.Min(amount, Stacks);
            Stacks -= consumed;
            return consumed;
        }
    }

    public class StatusCollection
    {
        private readonly Dictionary<StatusId, StatusInstance> _statuses = new();

        public void Apply(StatusId id, int stacks)
        {
            if (stacks <= 0) return;
            if (!_statuses.TryGetValue(id, out var inst))
            {
                inst = new StatusInstance(id, stacks);
                _statuses[id] = inst;
            }
            else
            {
                inst.Add(stacks);
            }
        }

        public int GetStacks(StatusId id)
        {
            return _statuses.TryGetValue(id, out var inst) ? inst.Stacks : 0;
        }

        public int Consume(StatusId id, int stacks)
        {
            if (!_statuses.TryGetValue(id, out var inst)) return 0;
            int consumed = inst.Consume(stacks);
            if (inst.Stacks <= 0) _statuses.Remove(id);
            return consumed;
        }

        public bool Has(StatusId id) => GetStacks(id) > 0;

        public void Clear() => _statuses.Clear();
    }
}
