using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Decorators;

namespace CombatSimulator.Core.Spells;

public sealed class ProtectionAmulet : ISpell
{
    public ICreature Apply(ICreature target)
    {
        return new MagicShield(target);
    }
}
