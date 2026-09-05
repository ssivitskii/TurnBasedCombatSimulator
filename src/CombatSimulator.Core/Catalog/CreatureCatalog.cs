using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Catalog;

public sealed class CreatureCatalog
{
    private readonly Dictionary<string, ICreatureFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, ICreatureFactory factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _factories[name] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public CreatureBuilder GetBuilder(string name)
    {
        if (!_factories.TryGetValue(name, out ICreatureFactory? factory))
            throw new KeyNotFoundException($"Creature '{name}' is not registered.");
        return new CreatureBuilder(factory);
    }
}
