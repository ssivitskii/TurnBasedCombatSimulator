namespace CombatSimulator.Cli;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        return new CliApplication(Console.Out, Console.Error).RunAsync(args, CancellationToken.None);
    }
}
