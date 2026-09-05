namespace CombatSimulator.Core.Abstractions;

public interface ICreature
{
    string Name { get; }

    AttackPoint Attack { get; }

    HealthPoint Health { get; }

    bool IsAlive { get; }

    bool CanAttack { get; }

    void AttackTarget(ICreature target, IRandomNumberGenerator randomNumberGenerator);

    void ReceiveDamage(int amount, IRandomNumberGenerator randomNumberGenerator);

    void SetAttack(AttackPoint attack);

    void SetHealth(HealthPoint health);

    ICreature DeepCopy();
}
