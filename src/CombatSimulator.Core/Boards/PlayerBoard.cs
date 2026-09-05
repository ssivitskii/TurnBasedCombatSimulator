using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Boards;

public sealed class PlayerBoard
{
    public const int Capacity = 7;
    private readonly List<ICreature> _slots = [];

    public IReadOnlyList<ICreature> Slots => _slots;

    public IEnumerable<ICreature> Living => _slots.Where(creature => creature.IsAlive);

    public IEnumerable<ICreature> Attackers => _slots.Where(creature => creature.CanAttack);

    public void Add(ICreature creature)
    {
        ArgumentNullException.ThrowIfNull(creature);
        if (_slots.Count >= Capacity)
            throw new InvalidOperationException($"A board can contain at most {Capacity} creatures.");
        _slots.Add(creature);
    }

    public void ApplySpell(int index, ISpell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        _slots[index] = spell.Apply(_slots[index]);
    }

    public PlayerBoard DeepCopy()
    {
        var copy = new PlayerBoard();
        foreach (ICreature creature in _slots)
            copy._slots.Add(creature.DeepCopy());
        return copy;
    }
}
