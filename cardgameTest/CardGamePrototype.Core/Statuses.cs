namespace CardGamePrototype.Core
{
    public class ElementStack
    {
        public ElementType Element { get; }
        public int Stacks { get; private set; }

        public ElementStack(ElementType element, int stacks)
        {
            Element = element;
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

    public class ElementCollection
    {
        private readonly Dictionary<ElementType, ElementStack> _elements = new();

        public void Apply(ElementType element, int stacks)
        {
            if (stacks <= 0) return;
            if (!_elements.TryGetValue(element, out var inst))
            {
                inst = new ElementStack(element, stacks);
                _elements[element] = inst;
            }
            else
            {
                inst.Add(stacks);
            }
        }

        public int GetStacks(ElementType element)
        {
            return _elements.TryGetValue(element, out var inst) ? inst.Stacks : 0;
        }

        public int Consume(ElementType element, int stacks)
        {
            if (!_elements.TryGetValue(element, out var inst)) return 0;
            int consumed = inst.Consume(stacks);
            if (inst.Stacks <= 0) _elements.Remove(element);
            return consumed;
        }

        public bool Has(ElementType element) => GetStacks(element) > 0;

        public void Clear() => _elements.Clear();
    }
}
