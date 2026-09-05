using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Engine;

namespace CombatSimulator.Core.Creatures;

public sealed class MimicChest : Creature
{
    public MimicChest()
        : base("MimicChest", new AttackPoint(1), new HealthPoint(1))
    {
    }

    private MimicChest(AttackPoint attack, HealthPoint health)
        : base("MimicChest", attack, health)
    {
    }

    public override void AttackTarget(ICreature target, IRandomNumberGenerator randomNumberGenerator)
    {
        SetAttack(new AttackPoint(Math.Max(Attack.Value, target.Attack.Value)));
        SetHealth(new HealthPoint(Math.Max(Health.Value, target.Health.Value)));
        base.AttackTarget(target, randomNumberGenerator);
    }

    public override ICreature DeepCopy()
    {
        return new MimicChest(Attack, Health);
    }
}
