using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Engine;

namespace CombatSimulator.Core.Creatures;

public sealed class BattleAnalyst : Creature
{
    public BattleAnalyst()
        : base("BattleAnalyst", new AttackPoint(2), new HealthPoint(4))
    {
    }

    private BattleAnalyst(AttackPoint attack, HealthPoint health)
        : base("BattleAnalyst", attack, health)
    {
    }

    public override void AttackTarget(ICreature target, IRandomNumberGenerator randomNumberGenerator)
    {
        SetAttack(new AttackPoint((int)Math.Min(int.MaxValue, (long)Attack.Value + 2)));
        base.AttackTarget(target, randomNumberGenerator);
    }

    public override ICreature DeepCopy()
    {
        return new BattleAnalyst(Attack, Health);
    }
}
