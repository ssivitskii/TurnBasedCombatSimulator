using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Spells;

public sealed class StaminaPotion : ISpell
{
    public ICreature Apply(ICreature target)
    {
        target.SetHealth(new HealthPoint(target.Health.Value + 5));
        return target;
    }
}
