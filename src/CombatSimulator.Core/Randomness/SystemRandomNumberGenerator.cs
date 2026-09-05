using CombatSimulator.Core.Abstractions;

namespace CombatSimulator.Core.Randomness;

public sealed class SystemRandomNumberGenerator : IRandomNumberGenerator
{
    private readonly Random _random;

    public SystemRandomNumberGenerator(int seed)
    {
        _random = new Random(seed);
    }

    public int NextInt(int minimumInclusive, int maximumExclusive)
    {
        return _random.Next(minimumInclusive, maximumExclusive);
    }
}
