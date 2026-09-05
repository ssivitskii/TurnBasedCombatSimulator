using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Engine;

namespace CombatSimulator.Core.Decorators;

public sealed class MagicShield : CreatureDecorator
{
    private bool _isConsumed;

    public MagicShield(ICreature inner)
        : base(inner)
    {
    }

    private MagicShield(ICreature inner, bool isConsumed)
        : base(inner)
    {
        _isConsumed = isConsumed;
    }

    public override void ReceiveDamage(int amount, IRandomNumberGenerator randomNumberGenerator)
    {
        if (!_isConsumed && amount > 0 && Inner.IsAlive)
        {
            _isConsumed = true;
            return;
        }

        base.ReceiveDamage(amount, randomNumberGenerator);
    }

    public override ICreature DeepCopy()
    {
        return new MagicShield(Inner.DeepCopy(), _isConsumed);
    }
}
