namespace CombatSimulator.Core.Abstractions;

public interface ICreatureModifier
{
    ICreature Apply(ICreature creature);
}
