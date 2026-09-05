using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Catalog;

public sealed class CreatureFactory : ICreatureFactory
{
    private readonly Func<ICreature> _factory;

    public CreatureFactory(Func<ICreature> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public ICreature Create()
    {
        return _factory();
    }
}
