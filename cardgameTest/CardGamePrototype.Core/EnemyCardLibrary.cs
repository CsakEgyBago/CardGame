namespace CardGamePrototype.Core
{
    public static class EnemyCardLibrary
    {
        // Standard enemy minions (cost 1–3, readable stats)
        private static CardDefinition Make(string id, string name, int cost, int hp, int atk, string desc) =>
            new CardDefinition { Id = id, Name = name, Description = desc, Cost = cost, CardType = CardType.Construct, MinionHp = hp, MinionAttack = atk };

        public static CardDefinition Crawler()    => Make("e_crawler",    "Crawler",     1, 3,  2, "Small but relentless.");
        public static CardDefinition NanoSwarm()  => Make("e_nanoswarm",  "Nano Swarm",  1, 2,  3, "Fragile. Hits fast.");
        public static CardDefinition VoltTrap()   => Make("e_volttrap",   "Volt Trap",   1, 1,  4, "Glass cannon.");
        public static CardDefinition ShieldDrone() => Make("e_shield",    "Shield Drone",2, 6,  1, "Absorbs hits.");
        public static CardDefinition BlazeCore()  => Make("e_blaze",      "Blaze Core",  2, 4,  3, "Balanced threat.");
        public static CardDefinition SpikeBot()   => Make("e_spike",      "Spike Bot",   2, 3,  5, "Low HP, high sting.");
        public static CardDefinition Bulwark()    => Make("e_bulwark",    "Bulwark Unit",3, 8,  2, "Armored wall.");
        public static CardDefinition HeavyGun()   => Make("e_heavygun",   "Heavy Gun",   3, 4,  6, "Massive firepower.");
        public static CardDefinition WarMech()    => Make("e_warmech",    "War Mech",    3, 6,  5, "Elite assault unit.");
        public static CardDefinition RaidHunter() => Make("e_raidhunter", "Raid Hunter", 2, 5,  4, "Hunter protocol.");
        public static CardDefinition ChainBeast() => Make("e_chain",      "Chain Beast", 3, 4,  7, "Unrelenting aggressor.");
        public static CardDefinition TitanCore()  => Make("e_titan",      "Titan Core",  4, 10, 7, "Apex war machine.");
        public static CardDefinition DoomVault()  => Make("e_doom",       "Doom Vault",  3, 7,  5, "Heavy siege unit.");
        public static CardDefinition VoidCrawler() => Make("e_void",      "Void Crawler",2, 6,  3, "Void-infused horror.");

        public static List<CardDefinition> GetForVariant(string variant)
        {
            var deck = new List<CardDefinition>();
            switch (variant)
            {
                case "boss":
                    deck.AddRange(Repeat(TitanCore,  2));
                    deck.AddRange(Repeat(DoomVault,  2));
                    deck.AddRange(Repeat(VoidCrawler,2));
                    deck.AddRange(Repeat(WarMech,    2));
                    deck.AddRange(Repeat(ChainBeast, 2));
                    deck.AddRange(Repeat(HeavyGun,   2));
                    deck.AddRange(Repeat(BlazeCore,  2));
                    break;
                case "elite":
                    deck.AddRange(Repeat(WarMech,    2));
                    deck.AddRange(Repeat(RaidHunter, 2));
                    deck.AddRange(Repeat(ChainBeast, 2));
                    deck.AddRange(Repeat(HeavyGun,   2));
                    deck.AddRange(Repeat(SpikeBot,   2));
                    deck.AddRange(Repeat(BlazeCore,  2));
                    deck.AddRange(Repeat(ShieldDrone,2));
                    break;
                default:
                    deck.AddRange(Repeat(Crawler,    3));
                    deck.AddRange(Repeat(NanoSwarm,  2));
                    deck.AddRange(Repeat(VoltTrap,   2));
                    deck.AddRange(Repeat(ShieldDrone,2));
                    deck.AddRange(Repeat(BlazeCore,  2));
                    deck.AddRange(Repeat(SpikeBot,   2));
                    deck.AddRange(Repeat(Bulwark,    1));
                    break;
            }
            return deck;
        }

        private static IEnumerable<CardDefinition> Repeat(Func<CardDefinition> factory, int count)
        {
            for (int i = 0; i < count; i++) yield return factory();
        }
    }
}
