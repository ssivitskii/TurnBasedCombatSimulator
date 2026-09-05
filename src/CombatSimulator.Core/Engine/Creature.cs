using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Engine;

public abstract class Creature : ICreature
{
    private AttackPoint _attack;
    private HealthPoint _health;

    protected Creature(string name, AttackPoint attack, HealthPoint health)
    {
        Name = name;
        _attack = attack;
        _health = health;
    }

    public string Name { get; }

    public virtual AttackPoint Attack => _attack;

    public virtual HealthPoint Health => _health;

    public virtual bool IsAlive => Health.Value > 0;

    public virtual bool CanAttack => IsAlive && Attack.Value > 0;

    public virtual void AttackTarget(ICreature target, IRandomNumberGenerator randomNumberGenerator)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (CanAttack)
            target.ReceiveDamage(Attack.Value, randomNumberGenerator);
    }

    public virtual void ReceiveDamage(int amount, IRandomNumberGenerator randomNumberGenerator)
    {
        if (amount > 0 && IsAlive)
            _health = new HealthPoint(Math.Max(0, _health.Value - amount));
    }

    public void SetAttack(AttackPoint attack)
    {
        _attack = attack;
    }

    public void SetHealth(HealthPoint health)
    {
        _health = health;
    }

    public abstract ICreature DeepCopy();
}
