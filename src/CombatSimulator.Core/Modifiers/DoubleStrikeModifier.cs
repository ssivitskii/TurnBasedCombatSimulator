using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Decorators;

namespace CombatSimulator.Core.Modifiers;

public sealed class DoubleStrikeModifier : ICreatureModifier
{
    public ICreature Apply(ICreature creature)
    {
        return new DoubleStrike(creature);
    }
}
