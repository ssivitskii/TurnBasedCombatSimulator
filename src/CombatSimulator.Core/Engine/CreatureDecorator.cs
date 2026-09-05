using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Engine;

public abstract class CreatureDecorator : ICreature
{
    public virtual string Name => Inner.Name;

    public virtual AttackPoint Attack => Inner.Attack;

    public virtual HealthPoint Health => Inner.Health;

    public virtual bool IsAlive => Inner.IsAlive;

    public virtual bool CanAttack => Inner.CanAttack;

    public virtual void AttackTarget(ICreature target, IRandomNumberGenerator randomNumberGenerator)
    {
        Inner.AttackTarget(target, randomNumberGenerator);
    }

    public virtual void ReceiveDamage(int amount, IRandomNumberGenerator randomNumberGenerator)
    {
        Inner.ReceiveDamage(amount, randomNumberGenerator);
    }

    public void SetAttack(AttackPoint attack)
    {
        Inner.SetAttack(attack);
    }

    public void SetHealth(HealthPoint health)
    {
        Inner.SetHealth(health);
    }

    public abstract ICreature DeepCopy();

    protected CreatureDecorator(ICreature inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    protected ICreature Inner { get; }
}
