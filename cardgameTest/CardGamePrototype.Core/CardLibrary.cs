namespace CardGamePrototype.Core
{
    public static class CardLibrary
    {
        public static IEnumerable<CardDefinition> GetAll()
        {
            yield return Ignite();
            yield return Firebolt();
            yield return Push();
            yield return FrostNova();
            yield return BioSpore();
            yield return IronGuard();
            yield return StormStrike();
            yield return CryoShell();
            yield return SporeCloud();
            yield return PhoenixAsh();
            yield return SlagGolem();
            yield return VoltStrike();
            yield return Inferno();
        }

        public static CardDefinition Ignite()
        {
            var c = new CardDefinition
            {
                Id = "ignite",
                Name = "Ignite",
                Description = "Applies 2 Fire to the enemy.",
                Cost = 1,
                CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Catalyst,
                MinionHp = 8,
                MinionAttack = 2
            };
            c.CatalystEffects.Add(new EffectDefinition
            {
                Type = EffectType.ApplyElement,
                Element = ElementType.Fire,
                Value = 2,
                Target = TargetType.Enemy
            });
            return c;
        }

        public static CardDefinition Firebolt()
        {
            var c = new CardDefinition
            {
                Id = "firebolt",
                Name = "Firebolt",
                Description = "Deal 6 DMG. (12 if target\nhas Fire).",
                Cost = 1,
                CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Executioner,
                MinionHp = 6,
                MinionAttack = 4
            };
            c.ExecutionerEffects.Add(new EffectDefinition
            {
                Type = EffectType.Damage,
                Value = 6,
                Target = TargetType.Enemy
            });

            c.ExecutionerEffects.Add(new EffectDefinition
            {
                Type = EffectType.ConditionalDamage,
                Value = 12,
                Target = TargetType.Enemy,
                ConditionElement = ElementType.Fire,
                ConditionStacks = 1,
                ConsumeStacks = 1
            });

            return c;
        }

        public static CardDefinition Push()
        {
            var c = new CardDefinition
            {
                Id = "push",
                Name = "Push",
                Description = "Push enemy back 1 space.\nDeals 4 DMG at edge.",
                Cost = 1,
                CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 14,
                MinionAttack = 2
            };
            c.CatalystEffects.Add(new EffectDefinition
            {
                Type = EffectType.Move,
                Value = 1,
                Target = TargetType.Enemy
            });

            c.ExecutionerEffects.Add(new EffectDefinition
            {
                Type = EffectType.ConditionalDamage,
                Value = 4,
                Target = TargetType.Enemy,
                ConditionTargetAtEdge = true
            });

            return c;
        }

        public static CardDefinition FrostNova()
        {
            var c = new CardDefinition
            {
                Id = "frost_nova",
                Name = "Frost Nova",
                Description = "Apply 1 Frost. Deals 10 DMG\nif enemy has Frost.",
                Cost = 2,
                CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 10,
                MinionAttack = 1
            };
            c.CatalystEffects.Add(new EffectDefinition
            {
                Type = EffectType.ApplyElement,
                Element = ElementType.Frost,
                Value = 1,
                Target = TargetType.Enemy
            });

            c.ExecutionerEffects.Add(new EffectDefinition
            {
                Type = EffectType.ConditionalDamage,
                Value = 10,
                Target = TargetType.Enemy,
                ConditionElement = ElementType.Frost,
                ConditionStacks = 1
            });

            return c;
        }

        public static CardDefinition BioSpore()
        {
            var c = new CardDefinition
            {
                Id = "bio_spore",
                Name = "Bio Spore",
                Description = "Apply 1 Bio. Deals 8 DMG\nif enemy has Bio.",
                Cost = 1,
                CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 12,
                MinionAttack = 2
            };

            c.CatalystEffects.Add(new EffectDefinition
            {
                Type = EffectType.ApplyElement,
                Element = ElementType.Bio,
                Value = 1,
                Target = TargetType.Enemy
            });

            c.ExecutionerEffects.Add(new EffectDefinition
            {
                Type = EffectType.ConditionalDamage,
                Target = TargetType.Enemy,
                Value = 8,
                ConditionElement = ElementType.Bio,
                ConditionStacks = 1,
                ConsumeStacks = 1
            });

            return c;
        }

        public static CardDefinition IronGuard()
        {
            return new CardDefinition
            {
                Id = "iron_guard",
                Name = "Iron Guard",
                Description = "Stalwart defender. Endures hits so you don't have to.",
                Cost = 2,
                CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Catalyst,
                MinionHp = 20,
                MinionAttack = 1
            };
        }

        public static CardDefinition StormStrike()
        {
            var c = new CardDefinition
            {
                Id = "storm_strike", Name = "Storm Strike",
                Description = "Fragile but hits hard.\nApplies 2 Fire.",
                Cost = 1, CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 6, MinionAttack = 5
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Fire, Value = 2, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 8, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition CryoShell()
        {
            var c = new CardDefinition
            {
                Id = "cryo_shell", Name = "Cryo Shell",
                Description = "Tanky wall. Applies\n2 Frost on summon.",
                Cost = 2, CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Catalyst,
                MinionHp = 18, MinionAttack = 1
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Frost, Value = 2, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition SporeCloud()
        {
            var c = new CardDefinition
            {
                Id = "spore_cloud", Name = "Spore Cloud",
                Description = "Applies 2 Bio. Execute:\n10 dmg if Bio present.",
                Cost = 1, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 7, MinionAttack = 3
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Bio, Value = 2, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ConditionalDamage, Value = 10, Target = TargetType.Enemy, ConditionElement = ElementType.Bio, ConditionStacks = 1, ConsumeStacks = 1 });
            return c;
        }

        public static CardDefinition PhoenixAsh()
        {
            var c = new CardDefinition
            {
                Id = "phoenix_ash", Name = "Phoenix Ash",
                Description = "Execute: 10 dmg.\nBonus 8 if Fire.",
                Cost = 2, CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Executioner,
                MinionHp = 10, MinionAttack = 3
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 10, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ConditionalDamage, Value = 8, Target = TargetType.Enemy, ConditionElement = ElementType.Fire, ConditionStacks = 1, ConsumeStacks = 1 });
            return c;
        }

        public static CardDefinition SlagGolem()
        {
            return new CardDefinition
            {
                Id = "slag_golem", Name = "Slag Golem",
                Description = "Immovable. Absorbs\nhits for your core.",
                Cost = 3, CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Catalyst,
                MinionHp = 24, MinionAttack = 1
            };
        }

        public static CardDefinition VoltStrike()
        {
            var c = new CardDefinition
            {
                Id = "volt_strike", Name = "Volt Strike",
                Description = "Applies 2 Lightning.\nExecute: 10 flat dmg.",
                Cost = 2, CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 7, MinionAttack = 5
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Lightning, Value = 2, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 10, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition Inferno()
        {
            var c = new CardDefinition
            {
                Id = "inferno", Name = "Inferno",
                Description = "Applies 3 Fire.\nExecute: 12 dmg if Fire.",
                Cost = 2, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 6, MinionAttack = 4
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Fire, Value = 3, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ConditionalDamage, Value = 12, Target = TargetType.Enemy, ConditionElement = ElementType.Fire, ConditionStacks = 1, ConsumeStacks = 1 });
            return c;
        }
    }
}