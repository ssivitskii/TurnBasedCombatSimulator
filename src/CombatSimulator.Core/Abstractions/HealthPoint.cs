namespace CombatSimulator.Core.Abstractions;

public readonly record struct HealthPoint
{
    public HealthPoint(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Health cannot be negative.");
        Value = value;
    }

    public int Value { get; }
}
