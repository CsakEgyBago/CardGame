using Xunit;

namespace CardGamePrototype.Tests;

public class UnitTest1
{
    [Fact]
    public void IgniteThenFirebolt_ConsumesFireElementForBonusDamage()
    {
        var state = new CardGamePrototype.Core.BattleState(
            new CardGamePrototype.Core.Player(50, 0),
            new CardGamePrototype.Core.Entity("Enemy", 50, 3),
            seed: 1337);

        var ignite = CardGamePrototype.Core.CardLibrary.Ignite();
        var firebolt = CardGamePrototype.Core.CardLibrary.Firebolt();

        foreach (var e in ignite.CatalystEffects)
            CardGamePrototype.Core.EffectResolver.ResolveEffect(e, state);
        foreach (var e in firebolt.ExecutionerEffects)
            CardGamePrototype.Core.EffectResolver.ResolveEffect(e, state);

        Assert.Equal(32, state.Enemy.Hp);
        Assert.Equal(1, state.Enemy.ActiveElements.GetStacks(CardGamePrototype.Core.ElementType.Fire));
    }

    [Fact]
    public void PushAtEdge_TriggersConditionalDamage()
    {
        var state = new CardGamePrototype.Core.BattleState(
            new CardGamePrototype.Core.Player(50, 0),
            new CardGamePrototype.Core.Entity("Enemy", 50, 4),
            seed: 1);

        var push = CardGamePrototype.Core.CardLibrary.Push();
        foreach (var e in push.CatalystEffects)
            CardGamePrototype.Core.EffectResolver.ResolveEffect(e, state);
        foreach (var e in push.ExecutionerEffects)
            CardGamePrototype.Core.EffectResolver.ResolveEffect(e, state);

        Assert.Equal(4, state.Enemy.Position);
        Assert.Equal(46, state.Enemy.Hp);
    }

    [Fact]
    public void NewBattle_UsesDeterministicShuffleSeed()
    {
        var serviceA = new CardGamePrototype.Core.BattleService();
        var serviceB = new CardGamePrototype.Core.BattleService();

        serviceA.NewBattle();
        serviceB.NewBattle();

        var a = string.Join("|", serviceA.State.Hand.Select(c => c.Id));
        var b = string.Join("|", serviceB.State.Hand.Select(c => c.Id));

        Assert.Equal(a, b);
    }
}
