using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Spells;

public sealed class MagicMirror : ISpell
{
    public ICreature Apply(ICreature target)
    {
        int attack = target.Attack.Value;
        target.SetAttack(new AttackPoint(target.Health.Value));
        target.SetHealth(new HealthPoint(attack));
        return target;
    }
}
