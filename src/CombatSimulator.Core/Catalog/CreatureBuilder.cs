using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Catalog;

public sealed class CreatureBuilder : ICreatureFactory
{
    private readonly ICreatureFactory _baseFactory;
    private readonly ICreatureModifier[] _modifiers;
    private readonly AttackPoint? _attack;
    private readonly HealthPoint? _health;

    public CreatureBuilder(ICreatureFactory baseFactory)
        : this(baseFactory, [], null, null)
    {
    }

    private CreatureBuilder(
        ICreatureFactory baseFactory,
        ICreatureModifier[] modifiers,
        AttackPoint? attack,
        HealthPoint? health)
    {
        _baseFactory = baseFactory ?? throw new ArgumentNullException(nameof(baseFactory));
        _modifiers = modifiers;
        _attack = attack;
        _health = health;
    }

    public CreatureBuilder AddModifier(ICreatureModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        return new CreatureBuilder(_baseFactory, [.. _modifiers, modifier], _attack, _health);
    }

    public CreatureBuilder WithAttack(AttackPoint attack)
    {
        return new CreatureBuilder(_baseFactory, _modifiers, attack, _health);
    }

    public CreatureBuilder WithHealth(HealthPoint health)
    {
        return new CreatureBuilder(_baseFactory, _modifiers, _attack, health);
    }

    public ICreature Create()
    {
        ICreature creature = _baseFactory.Create();
        if (_attack is AttackPoint attack)
            creature.SetAttack(attack);
        if (_health is HealthPoint health)
            creature.SetHealth(health);
        foreach (ICreatureModifier modifier in _modifiers)
            creature = modifier.Apply(creature);
        return creature;
    }
}
