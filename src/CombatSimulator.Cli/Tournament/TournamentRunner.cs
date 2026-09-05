using CombatSimulator.Cli.Configuration;
using CombatSimulator.Core.Boards;
using CombatSimulator.Core.Combat;
using CombatSimulator.Core.Randomness;

namespace CombatSimulator.Cli.Tournament;

public sealed class TournamentRunner
{
    public const int MaximumGames = 100000;
    public const int MaximumParallelism = 64;

    public async Task<TournamentReport> RunAsync(
        BattleDefinition definition,
        int games,
        int baseSeed,
        int parallelism,
        CancellationToken cancellationToken)
    {
        ValidateWork(games, parallelism);
        var summaries = new GameSummary[games];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, games),
            CreateParallelOptions(parallelism, cancellationToken),
            (index, token) =>
            {
                summaries[index] = RunGame(definition, index, baseSeed, swapTeams: false, token);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
        return TournamentReport.Create("tournament", baseSeed, summaries);
    }

    public async Task<BalanceReport> RunBalanceAsync(
        BattleDefinition definition,
        int pairs,
        int baseSeed,
        int parallelism,
        double dominanceThreshold,
        CancellationToken cancellationToken)
    {
        ValidateWork(pairs, parallelism);
        if (!double.IsFinite(dominanceThreshold) || dominanceThreshold is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(dominanceThreshold));

        var original = new GameSummary[pairs];
        var mirrored = new GameSummary[pairs];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, pairs),
            CreateParallelOptions(parallelism, cancellationToken),
            (index, token) =>
            {
                original[index] = RunGame(definition, index, baseSeed, swapTeams: false, token);
                mirrored[index] = RunGame(definition, index, baseSeed, swapTeams: true, token);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

        int lineupAWins = original.Count(game => game.Verdict == BattleVerdict.TeamAVictory)
            + mirrored.Count(game => game.Verdict == BattleVerdict.TeamBVictory);
        int lineupBWins = original.Count(game => game.Verdict == BattleVerdict.TeamBVictory)
            + mirrored.Count(game => game.Verdict == BattleVerdict.TeamAVictory);
        int draws = original.Count(game => game.Verdict == BattleVerdict.Draw)
            + mirrored.Count(game => game.Verdict == BattleVerdict.Draw);
        int firstPositionWins = original.Count(game => game.Verdict == BattleVerdict.TeamAVictory)
            + mirrored.Count(game => game.Verdict == BattleVerdict.TeamAVictory);
        int secondPositionWins = original.Count(game => game.Verdict == BattleVerdict.TeamBVictory)
            + mirrored.Count(game => game.Verdict == BattleVerdict.TeamBVictory);
        int[] rounds = original.Concat(mirrored).Select(game => game.Rounds).Order().ToArray();
        double median = rounds.Length % 2 == 1
            ? rounds[rounds.Length / 2]
            : ((long)rounds[(rounds.Length / 2) - 1] + rounds[rounds.Length / 2]) / 2.0;
        int total = pairs * 2;
        double difference = (lineupAWins - lineupBWins) * 100.0 / total;
        string assessment = difference > dominanceThreshold
            ? "Configured lineup A exceeded the configured dominance threshold."
            : difference < -dominanceThreshold
                ? "Configured lineup B exceeded the configured dominance threshold."
                : "Neither configured lineup exceeded the configured dominance threshold.";
        return new BalanceReport(
            "paired-balance",
            pairs,
            total,
            baseSeed,
            SeedDerivation.Algorithm,
            dominanceThreshold,
            lineupAWins,
            lineupBWins,
            draws,
            firstPositionWins,
            secondPositionWins,
            firstPositionWins * 100.0 / total,
            secondPositionWins * 100.0 / total,
            lineupAWins * 100.0 / total,
            lineupBWins * 100.0 / total,
            draws * 100.0 / total,
            rounds.Average(),
            median,
            rounds[0],
            rounds[^1],
            assessment);
    }

    private static ParallelOptions CreateParallelOptions(int parallelism, CancellationToken cancellationToken)
    {
        return new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = parallelism,
        };
    }

    private static GameSummary RunGame(
        BattleDefinition definition,
        int index,
        int baseSeed,
        bool swapTeams,
        CancellationToken cancellationToken)
    {
        int seed = SeedDerivation.Derive(baseSeed, index);
        (PlayerBoard teamA, PlayerBoard teamB) = definition.BuildBoards(swapTeams);
        BattleResult result = new CombatRunner(
            new SystemRandomNumberGenerator(seed),
            definition.RoundLimit).Run(teamA, teamB, cancellationToken);
        AttackResolvedEvent[] attacks = result.Events.OfType<AttackResolvedEvent>().ToArray();
        return new GameSummary(
            index,
            seed,
            result.Verdict,
            result.EndReason,
            result.Rounds,
            attacks.Where(battleEvent => battleEvent.ActingTeam == BattleTeam.TeamA)
                .Sum(battleEvent => (long)battleEvent.NetHealthLoss),
            attacks.Where(battleEvent => battleEvent.ActingTeam == BattleTeam.TeamB)
                .Sum(battleEvent => (long)battleEvent.NetHealthLoss),
            attacks.Count(battleEvent => battleEvent.ActingTeam == BattleTeam.TeamA && battleEvent.TargetDefeated),
            attacks.Count(battleEvent => battleEvent.ActingTeam == BattleTeam.TeamB && battleEvent.TargetDefeated));
    }

    private static void ValidateWork(int games, int parallelism)
    {
        if (games is < 1 or > MaximumGames)
            throw new ArgumentOutOfRangeException(nameof(games), $"Game count must be between 1 and {MaximumGames}.");
        if (parallelism is < 1 or > MaximumParallelism)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parallelism),
                $"Parallelism must be between 1 and {MaximumParallelism}.");
        }
    }
}
