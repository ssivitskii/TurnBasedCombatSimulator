using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Creatures;
using CombatSimulator.Core.Decorators;
using CombatSimulator.Core.Randomness;

namespace CombatSimulator.Tests;

public sealed class DeepCopyTests
{
    private static readonly SystemRandomNumberGenerator Random = new(42);

    [Fact]
    public void DeepCopyPreservesMutatedStatsAndIsIndependent()
    {
        var original = new BattleAnalyst();
        original.SetAttack(new AttackPoint(11));
        original.SetHealth(new HealthPoint(3));

        ICreature copy = original.DeepCopy();
        copy.SetAttack(new AttackPoint(2));

        Assert.Equal(11, original.Attack.Value);
        Assert.Equal(2, copy.Attack.Value);
        Assert.Equal(3, copy.Health.Value);
    }

    [Fact]
    public void DeathlessHorrorCopyPreservesConsumedRevival()
    {
        var original = new DeathlessHorror();
        original.ReceiveDamage(100, Random);
        ICreature copy = original.DeepCopy();

        copy.ReceiveDamage(1, Random);

        Assert.False(copy.IsAlive);
        Assert.True(original.IsAlive);
    }

    [Fact]
    public void MagicShieldCopyPreservesConsumedState()
    {
        var shield = new MagicShield(new ViciousBrawler());
        shield.ReceiveDamage(1, Random);
        ICreature copy = shield.DeepCopy();

        copy.ReceiveDamage(1, Random);

        Assert.Equal(5, copy.Health.Value);
        Assert.Equal(6, shield.Health.Value);
    }

    [Fact]
    public void NestedDecoratorCopyPreservesBehaviorAndIndependentState()
    {
        var original = new DoubleStrike(new MagicShield(new AmuletMaster()));
        original.ReceiveDamage(1, Random);
        ICreature copy = original.DeepCopy();

        copy.ReceiveDamage(1, Random);

        Assert.Equal(1, copy.Health.Value);
        Assert.Equal(2, original.Health.Value);
    }
}
