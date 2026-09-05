using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Engine;

namespace CombatSimulator.Core.Creatures;

public sealed class AmuletMaster : Creature
{
    public AmuletMaster()
        : base("AmuletMaster", new AttackPoint(5), new HealthPoint(2))
    {
    }

    private AmuletMaster(AttackPoint attack, HealthPoint health)
        : base("AmuletMaster", attack, health)
    {
    }

    public override ICreature DeepCopy()
    {
        return new AmuletMaster(Attack, Health);
    }
}
