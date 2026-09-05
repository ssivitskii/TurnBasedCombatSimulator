using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Engine;

namespace CombatSimulator.Core.Creatures;

public sealed class DeathlessHorror : Creature
{
    private bool _hasRevived;

    public DeathlessHorror()
        : base("DeathlessHorror", new AttackPoint(4), new HealthPoint(4))
    {
    }

    private DeathlessHorror(AttackPoint attack, HealthPoint health, bool hasRevived)
        : base("DeathlessHorror", attack, health)
    {
        _hasRevived = hasRevived;
    }

    public override void ReceiveDamage(int amount, IRandomNumberGenerator randomNumberGenerator)
    {
        int healthBefore = Health.Value;
        base.ReceiveDamage(amount, randomNumberGenerator);
        if (!_hasRevived && healthBefore > 0 && !IsAlive)
        {
            _hasRevived = true;
            SetHealth(new HealthPoint(1));
        }
    }

    public override ICreature DeepCopy()
    {
        return new DeathlessHorror(Attack, Health, _hasRevived);
    }
}
