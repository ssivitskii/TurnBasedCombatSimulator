namespace CombatSimulator.Api;

public static class ApiLimits
{
    public const int MaximumConcurrentSimulations = 4;
    public const int MaximumQueueLength = 0;
    public const long MaximumRequestBytes = 128 * 1024;
    public const int MaximumRoundLimit = 1000;
}
