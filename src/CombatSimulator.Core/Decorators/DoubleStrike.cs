using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Engine;

namespace CombatSimulator.Core.Decorators;

public sealed class DoubleStrike : CreatureDecorator
{
    public DoubleStrike(ICreature inner)
        : base(inner)
    {
    }

    public override void AttackTarget(ICreature target, IRandomNumberGenerator randomNumberGenerator)
    {
        if (!CanAttack)
            return;

        base.AttackTarget(target, randomNumberGenerator);
        if (IsAlive && target.IsAlive)
            base.AttackTarget(target, randomNumberGenerator);
    }

    public override ICreature DeepCopy()
    {
        return new DoubleStrike(Inner.DeepCopy());
    }
}
