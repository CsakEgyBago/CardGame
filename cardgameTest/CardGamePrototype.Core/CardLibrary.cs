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
            yield return VoidDrain();
            yield return ArcSurge();
            yield return CryoBolt();
            yield return Biowave();
            yield return VoidSentinel();
            yield return ArcLancer();
            yield return NullBlade();
            yield return BoneSpear();
            yield return EmberCore();
            yield return FrostLance();
            yield return BioBomb();
            yield return SurgeBolt();
            yield return StaticField();
            yield return PlasmaCutter();
            yield return NullTrap();
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
            var c = new CardDefinition
            {
                Id = "iron_guard",
                Name = "Iron Guard",
                Description = "Stalwart defender.\nSpecial: strikes for 4 dmg.",
                Cost = 2,
                CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 20,
                MinionAttack = 1
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 4, Target = TargetType.Enemy });
            return c;
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
                Description = "Tanky wall. Summons 2 Frost.\nSpecial: 5 dmg + 1 Frost.",
                Cost = 2, CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 18, MinionAttack = 1
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Frost, Value = 2, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 5, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Frost, Value = 1, Target = TargetType.Enemy });
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
            var c = new CardDefinition
            {
                Id = "slag_golem", Name = "Slag Golem",
                Description = "Immovable. Absorbs hits.\nSpecial: 6 dmg + 2 Fire.",
                Cost = 3, CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 24, MinionAttack = 1
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 6, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Fire, Value = 2, Target = TargetType.Enemy });
            return c;
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
                Description = "Applies 3 Fire.\nAuto: 12 dmg if Fire.",
                Cost = 2, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 6, MinionAttack = 4
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Fire, Value = 3, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ConditionalDamage, Value = 12, Target = TargetType.Enemy, ConditionElement = ElementType.Fire, ConditionStacks = 1, ConsumeStacks = 1 });
            return c;
        }

        // ── New spell cards ───────────────────────────────────────────────

        public static CardDefinition VoidDrain()
        {
            var c = new CardDefinition
            {
                Id = "void_drain", Name = "Void Drain",
                Description = "Drain enemy vitality.\nHeal yourself 8 HP.",
                Cost = 1, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Catalyst,
                MinionHp = 8, MinionAttack = 1
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.HealPlayer, Value = 8, Target = TargetType.Self });
            return c;
        }

        public static CardDefinition ArcSurge()
        {
            var c = new CardDefinition
            {
                Id = "arc_surge", Name = "Arc Surge",
                Description = "Overcharge the grid.\nApply 3 Lightning.",
                Cost = 2, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Catalyst,
                MinionHp = 7, MinionAttack = 2
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Lightning, Value = 3, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition CryoBolt()
        {
            var c = new CardDefinition
            {
                Id = "cryo_bolt", Name = "Cryo Bolt",
                Description = "Apply 2 Frost. Deal\n8 dmg if frozen.",
                Cost = 2, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 8, MinionAttack = 1
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Frost, Value = 2, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ConditionalDamage, Value = 8, Target = TargetType.Enemy, ConditionElement = ElementType.Frost, ConditionStacks = 1 });
            return c;
        }

        public static CardDefinition Biowave()
        {
            var c = new CardDefinition
            {
                Id = "biowave", Name = "Biowave",
                Description = "Release a spore pulse.\nApply 3 Bio to enemy.",
                Cost = 1, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Catalyst,
                MinionHp = 7, MinionAttack = 1
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Bio, Value = 3, Target = TargetType.Enemy });
            return c;
        }

        // ── New unit cards ────────────────────────────────────────────────

        public static CardDefinition VoidSentinel()
        {
            var c = new CardDefinition
            {
                Id = "void_sentinel", Name = "Void Sentinel",
                Description = "Void-powered protector.\nSpecial: heal 6 HP.",
                Cost = 2, CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 18, MinionAttack = 2
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.HealPlayer, Value = 6, Target = TargetType.Self });
            return c;
        }

        public static CardDefinition ArcLancer()
        {
            var c = new CardDefinition
            {
                Id = "arc_lancer", Name = "Arc Lancer",
                Description = "Volatile striker. Summons\n2 Lightning. Special: +2 Lgt +6 dmg.",
                Cost = 2, CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 8, MinionAttack = 5
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Lightning, Value = 2, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Lightning, Value = 2, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 6, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition NullBlade()
        {
            var c = new CardDefinition
            {
                Id = "null_blade", Name = "Null Blade",
                Description = "Edge fighter.\nSpecial: 9 dmg at edge.",
                Cost = 1, CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 8, MinionAttack = 3
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ConditionalDamage, Value = 9, Target = TargetType.Enemy, ConditionTargetAtEdge = true });
            return c;
        }

        public static CardDefinition BoneSpear()
        {
            var c = new CardDefinition
            {
                Id = "bone_spear", Name = "Bone Spear",
                Description = "Bone-tipped assault.\nSpecial: pierces for 8 dmg.",
                Cost = 1, CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 6, MinionAttack = 3
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 8, Target = TargetType.Enemy });
            return c;
        }

        // ── Expansion set ────────────────────────────────────────────────

        public static CardDefinition EmberCore()
        {
            var c = new CardDefinition
            {
                Id = "ember_core", Name = "Ember Core",
                Description = "Ignites on arrival. Applies\n1 Fire. Special: 5 dmg if Fire.",
                Cost = 1, CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 14, MinionAttack = 3
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Fire, Value = 1, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ConditionalDamage, Value = 5, Target = TargetType.Enemy, ConditionElement = ElementType.Fire, ConditionStacks = 1 });
            return c;
        }

        public static CardDefinition FrostLance()
        {
            var c = new CardDefinition
            {
                Id = "frost_lance", Name = "Frost Lance",
                Description = "Ice-tipped strike.\nSpecial: apply 3 Frost.",
                Cost = 2, CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 10, MinionAttack = 4
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Frost, Value = 3, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition BioBomb()
        {
            var c = new CardDefinition
            {
                Id = "bio_bomb", Name = "Bio Bomb",
                Description = "Apply 4 Bio. Explodes for\n12 dmg if Bio present.",
                Cost = 2, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 7, MinionAttack = 2
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Bio, Value = 4, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.ConditionalDamage, Value = 12, Target = TargetType.Enemy, ConditionElement = ElementType.Bio, ConditionStacks = 1, ConsumeStacks = 1 });
            return c;
        }

        public static CardDefinition SurgeBolt()
        {
            var c = new CardDefinition
            {
                Id = "surge_bolt", Name = "Surge Bolt",
                Description = "Raw kinetic discharge.\nDeals 8 flat damage.",
                Cost = 1, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Catalyst,
                MinionHp = 6, MinionAttack = 2
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 8, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition StaticField()
        {
            var c = new CardDefinition
            {
                Id = "static_field", Name = "Static Field",
                Description = "Electrify the field. Apply\n2 Lightning + 5 flat dmg.",
                Cost = 2, CardType = CardType.Incantation,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 7, MinionAttack = 2
            };
            c.CatalystEffects.Add(new EffectDefinition { Type = EffectType.ApplyElement, Element = ElementType.Lightning, Value = 2, Target = TargetType.Enemy });
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 5, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition PlasmaCutter()
        {
            var c = new CardDefinition
            {
                Id = "plasma_cutter", Name = "Plasma Cutter",
                Description = "Superheated edge.\nSpecial: 10 raw damage.",
                Cost = 2, CardType = CardType.Strike,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 10, MinionAttack = 6
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.Damage, Value = 10, Target = TargetType.Enemy });
            return c;
        }

        public static CardDefinition NullTrap()
        {
            var c = new CardDefinition
            {
                Id = "null_trap", Name = "Null Trap",
                Description = "Void shield. Absorbs all.\nSpecial: restores 12 HP.",
                Cost = 3, CardType = CardType.Construct,
                ResolutionRole = CardResolutionRole.Both,
                MinionHp = 30, MinionAttack = 1
            };
            c.ExecutionerEffects.Add(new EffectDefinition { Type = EffectType.HealPlayer, Value = 12, Target = TargetType.Self });
            return c;
        }
    }
}