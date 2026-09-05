using CombatSimulator.Core.Combat;

namespace CombatSimulator.Cli.Tournament;

public sealed record TournamentReport(
    string Mode,
    int Games,
    int BaseSeed,
    string SeedAlgorithm,
    int TeamAWins,
    int TeamBWins,
    int Draws,
    double TeamAWinPercentage,
    double TeamBWinPercentage,
    double DrawPercentage,
    double AverageRounds,
    double MedianRounds,
    int MinimumRounds,
    int MaximumRounds,
    long ObservedNetHealthLossCausedByTeamA,
    long ObservedNetHealthLossCausedByTeamB,
    int TeamATargetDefeats,
    int TeamBTargetDefeats,
    int TeamDefeatedEnds,
    int Stalemates,
    int RoundLimitEnds)
{
    public static TournamentReport Create(string mode, int baseSeed, IReadOnlyList<GameSummary> games)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        ArgumentNullException.ThrowIfNull(games);
        if (games.Count == 0)
            throw new ArgumentException("At least one game is required.", nameof(games));

        int teamAWins = games.Count(game => game.Verdict == BattleVerdict.TeamAVictory);
        int teamBWins = games.Count(game => game.Verdict == BattleVerdict.TeamBVictory);
        int draws = games.Count - teamAWins - teamBWins;
        int[] rounds = games.Select(game => game.Rounds).Order().ToArray();
        double median = rounds.Length % 2 == 1
            ? rounds[rounds.Length / 2]
            : ((long)rounds[(rounds.Length / 2) - 1] + rounds[rounds.Length / 2]) / 2.0;
        return new TournamentReport(
            mode,
            games.Count,
            baseSeed,
            SeedDerivation.Algorithm,
            teamAWins,
            teamBWins,
            draws,
            Percentage(teamAWins, games.Count),
            Percentage(teamBWins, games.Count),
            Percentage(draws, games.Count),
            rounds.Average(),
            median,
            rounds[0],
            rounds[^1],
            games.Sum(game => game.ObservedNetHealthLossCausedByTeamA),
            games.Sum(game => game.ObservedNetHealthLossCausedByTeamB),
            games.Sum(game => game.TeamATargetDefeats),
            games.Sum(game => game.TeamBTargetDefeats),
            games.Count(game => game.EndReason == BattleEndReason.TeamDefeated),
            games.Count(game => game.EndReason == BattleEndReason.Stalemate),
            games.Count(game => game.EndReason == BattleEndReason.RoundLimitReached));
    }

    private static double Percentage(int count, int total)
    {
        return count * 100.0 / total;
    }
}
