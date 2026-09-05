namespace CombatSimulator.Core.Abstractions;

public readonly record struct AttackPoint
{
    public AttackPoint(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Attack cannot be negative.");
        Value = value;
    }

    public int Value { get; }
}
