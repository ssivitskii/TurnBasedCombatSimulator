using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Engine;

namespace CombatSimulator.Core.Creatures;

public sealed class ViciousBrawler : Creature
{
    public ViciousBrawler()
        : base("ViciousBrawler", new AttackPoint(1), new HealthPoint(6))
    {
    }

    private ViciousBrawler(AttackPoint attack, HealthPoint health)
        : base("ViciousBrawler", attack, health)
    {
    }

    public override void ReceiveDamage(int amount, IRandomNumberGenerator randomNumberGenerator)
    {
        int healthBefore = Health.Value;
        base.ReceiveDamage(amount, randomNumberGenerator);
        if (amount > 0 && healthBefore > 0 && IsAlive)
            SetAttack(new AttackPoint((int)Math.Min(int.MaxValue, (long)Attack.Value * 2)));
    }

    public override ICreature DeepCopy()
    {
        return new ViciousBrawler(Attack, Health);
    }
}
