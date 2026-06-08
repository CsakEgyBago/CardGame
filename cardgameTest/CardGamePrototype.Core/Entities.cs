namespace CardGamePrototype.Core
{
    public class Entity
    {
        public string Name { get; set; }
        public int MaxHp { get; set; }
        public int Hp { get; set; }
        public int Position { get; set; }

        public ElementCollection ActiveElements { get; } = new ElementCollection();

        public Entity(string name, int maxHp, int position)
        {
            Name = name;
            MaxHp = maxHp;
            Hp = maxHp;
            Position = position;
        }

        public bool IsDead => Hp <= 0;

        public void ReceiveDamage(int amount)
        {
            Hp -= amount;
            if (Hp < 0) Hp = 0;
        }
    }

    public class Player : Entity
    {
        public int Energy { get; set; }

        public Player(int maxHp, int position) : base("Player", maxHp, position)
        {
        }
    }

    public class SummonedEntity : Entity
    {
        public CardDefinition SourceCard { get; }
        public int BaseAttack { get; set; }

        public SummonedEntity(string name, int maxHp, int position, CardDefinition sourceCard) 
            : base(name, maxHp, position)
        {
            SourceCard = sourceCard;
            BaseAttack = 2; 
        }
    }
}