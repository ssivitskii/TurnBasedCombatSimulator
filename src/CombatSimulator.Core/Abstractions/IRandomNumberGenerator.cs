namespace CombatSimulator.Core.Abstractions;

public interface IRandomNumberGenerator
{
    int NextInt(int minimumInclusive, int maximumExclusive);
}
