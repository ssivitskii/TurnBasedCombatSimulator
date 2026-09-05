using CombatSimulator.Cli.Configuration;
using CombatSimulator.Cli.Replay;
using CombatSimulator.Cli.Reporting;
using CombatSimulator.Cli.Tournament;
using CombatSimulator.Core.Boards;
using CombatSimulator.Core.Combat;
using CombatSimulator.Core.Randomness;
using System.Globalization;
using System.Text.Json;

namespace CombatSimulator.Cli;

public sealed class CliApplication
{
    private const int DefaultParallelism = 4;
    private const double DefaultDominanceThreshold = 10;
    private static readonly HashSet<string> BalanceOptions = new(StringComparer.Ordinal)
    {
        "--format",
        "--games",
        "--parallelism",
        "--seed",
        "--threshold",
    };

    private static readonly HashSet<string> BattleOptions = new(StringComparer.Ordinal)
    {
        "--format",
        "--save-replay",
        "--seed",
    };

    private static readonly HashSet<string> ReplayOptions = new(StringComparer.Ordinal) { "--format" };

    private static readonly HashSet<string> TournamentOptions = new(StringComparer.Ordinal)
    {
        "--format",
        "--games",
        "--parallelism",
        "--seed",
    };

    private readonly TextWriter _error;
    private readonly BattleConfigurationLoader _loader = new();
    private readonly TextWriter _output;
    private readonly ReplayStore _replayStore = new();
    private readonly TournamentRunner _tournament = new();

    public CliApplication(TextWriter output, TextWriter error)
    {
        _output = output;
        _error = error;
    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || args is ["--help"] or ["help"])
        {
            await WriteHelpAsync().ConfigureAwait(false);
            return args.Length == 0 ? 2 : 0;
        }

        try
        {
            if (args.Length < 2)
                throw new ArgumentException("A command and input file are required.");
            Dictionary<string, string> options = ParseOptions(args.Skip(2).ToArray());
            OutputFormat format = ParseFormat(options);
            return args[0].ToLowerInvariant() switch
            {
                "battle" => await RunBattleAsync(args[1], options, format, cancellationToken).ConfigureAwait(false),
                "replay" => await RunReplayAsync(args[1], options, format, cancellationToken).ConfigureAwait(false),
                "tournament" => await RunTournamentAsync(args[1], options, format, cancellationToken).ConfigureAwait(false),
                "balance" => await RunBalanceAsync(args[1], options, format, cancellationToken).ConfigureAwait(false),
                _ => throw new ArgumentException($"Unknown command '{args[0]}'."),
            };
        }
        catch (ArgumentException exception)
        {
            await _error.WriteLineAsync($"Configuration or usage error: {exception.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (KeyNotFoundException exception)
        {
            await _error.WriteLineAsync($"Configuration error: {exception.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (JsonException exception)
        {
            await _error.WriteLineAsync($"Invalid JSON: {exception.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (InvalidDataException exception)
        {
            await _error.WriteLineAsync($"Invalid replay: {exception.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (IOException exception)
        {
            await _error.WriteLineAsync($"I/O error: {exception.Message}").ConfigureAwait(false);
            return 3;
        }
    }

    private static void EnsureOnly(Dictionary<string, string> options, IReadOnlySet<string> allowed)
    {
        string? unknown = options.Keys.FirstOrDefault(option => !allowed.Contains(option, StringComparer.Ordinal));
        if (unknown is not null)
            throw new ArgumentException($"Unknown option '{unknown}'.");
    }

    private static double ParseDouble(
        Dictionary<string, string> options,
        string name,
        double defaultValue)
    {
        if (!options.TryGetValue(name, out string? value))
            return defaultValue;
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            throw new ArgumentException($"Option '{name}' must be a number using invariant notation.");
        return result;
    }

    private static OutputFormat ParseFormat(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("--format", out string? value))
            return OutputFormat.Text;
        return value.ToLowerInvariant() switch
        {
            "text" => OutputFormat.Text,
            "json" => OutputFormat.Json,
            "csv" => OutputFormat.Csv,
            _ => throw new ArgumentException("Option '--format' must be text, json, or csv."),
        };
    }

    private static int ParseInteger(
        Dictionary<string, string> options,
        string name,
        bool required,
        int defaultValue = 0)
    {
        if (!options.TryGetValue(name, out string? value))
        {
            if (required)
                throw new ArgumentException($"Option '{name}' is required.");
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result) || result < 0)
            throw new ArgumentException($"Option '{name}' must be a non-negative integer.");
        return result;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        if (args.Length % 2 != 0)
            throw new ArgumentException("Every option must have a value.");
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index += 2)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Unexpected argument '{args[index]}'.");
            if (!options.TryAdd(args[index], args[index + 1]))
                throw new ArgumentException($"Duplicate option '{args[index]}'.");
        }

        return options;
    }

    private async Task<int> RunBalanceAsync(
        string path,
        Dictionary<string, string> options,
        OutputFormat format,
        CancellationToken cancellationToken)
    {
        EnsureOnly(options, BalanceOptions);
        int pairs = ParseInteger(options, "--games", required: true);
        int seed = ParseInteger(options, "--seed", required: true);
        int parallelism = ParseInteger(options, "--parallelism", required: false, DefaultParallelism);
        double threshold = ParseDouble(options, "--threshold", DefaultDominanceThreshold);
        BattleDefinition definition = await _loader.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        BalanceReport report = await _tournament.RunBalanceAsync(
            definition,
            pairs,
            seed,
            parallelism,
            threshold,
            cancellationToken).ConfigureAwait(false);
        await CombatReportFormatter.WriteBalanceAsync(_output, report, format).ConfigureAwait(false);
        return 0;
    }

    private async Task<int> RunBattleAsync(
        string path,
        Dictionary<string, string> options,
        OutputFormat format,
        CancellationToken cancellationToken)
    {
        EnsureOnly(options, BattleOptions);
        int seed = ParseInteger(options, "--seed", required: true);
        BattleDefinition definition = await _loader.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        (PlayerBoard teamA, PlayerBoard teamB) = definition.BuildBoards();
        BattleResult result = new CombatRunner(
            new SystemRandomNumberGenerator(seed),
            definition.RoundLimit).Run(teamA, teamB, cancellationToken);
        ReplayDocument replay = ReplayMapper.Create(definition, seed, result);
        if (options.TryGetValue("--save-replay", out string? replayPath))
            await _replayStore.SaveAsync(replayPath, replay, cancellationToken).ConfigureAwait(false);
        await CombatReportFormatter.WriteBattleAsync(_output, replay, format).ConfigureAwait(false);
        return 0;
    }

    private async Task<int> RunReplayAsync(
        string path,
        Dictionary<string, string> options,
        OutputFormat format,
        CancellationToken cancellationToken)
    {
        EnsureOnly(options, ReplayOptions);
        ReplayDocument replay = await _replayStore.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        await CombatReportFormatter.WriteBattleAsync(_output, replay, format).ConfigureAwait(false);
        return 0;
    }

    private async Task<int> RunTournamentAsync(
        string path,
        Dictionary<string, string> options,
        OutputFormat format,
        CancellationToken cancellationToken)
    {
        EnsureOnly(options, TournamentOptions);
        int games = ParseInteger(options, "--games", required: true);
        int seed = ParseInteger(options, "--seed", required: true);
        int parallelism = ParseInteger(options, "--parallelism", required: false, DefaultParallelism);
        BattleDefinition definition = await _loader.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        TournamentReport report = await _tournament.RunAsync(
            definition,
            games,
            seed,
            parallelism,
            cancellationToken).ConfigureAwait(false);
        await CombatReportFormatter.WriteTournamentAsync(_output, report, format).ConfigureAwait(false);
        return 0;
    }

    private async Task WriteHelpAsync()
    {
        await _output.WriteLineAsync("Combat Simulator").ConfigureAwait(false);
        await _output.WriteLineAsync(
            "battle <battle.json> --seed N [--format text|json|csv] [--save-replay path]")
            .ConfigureAwait(false);
        await _output.WriteLineAsync("replay <replay.json> [--format text|json|csv]").ConfigureAwait(false);
        await _output.WriteLineAsync(
            "tournament <battle.json> --games N --seed N [--parallelism N] [--format text|json|csv]")
            .ConfigureAwait(false);
        await _output.WriteLineAsync(
            "balance <battle.json> --games N --seed N [--parallelism N] [--threshold points] [--format text|json|csv]")
            .ConfigureAwait(false);
        await _output.WriteLineAsync("For balance, --games is the pair count and exactly 2*N battles run.")
            .ConfigureAwait(false);
    }
}
