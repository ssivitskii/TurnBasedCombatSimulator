namespace CombatSimulator.Core.Abstractions;

public interface ISpell
{
    ICreature Apply(ICreature target);
}
