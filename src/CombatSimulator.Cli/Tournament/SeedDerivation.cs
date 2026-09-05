namespace CombatSimulator.Cli.Tournament;

public static class SeedDerivation
{
    public const string Algorithm = "splitmix64-v1";

    public static int Derive(int baseSeed, int gameIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gameIndex);
        ulong value = ((ulong)(uint)baseSeed << 32) | (uint)gameIndex;
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        value ^= value >> 31;
        return unchecked((int)(value ^ (value >> 32)));
    }
}
