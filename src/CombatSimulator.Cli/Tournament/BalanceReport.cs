namespace CombatSimulator.Cli.Tournament;

public sealed record BalanceReport(
    string Mode,
    int Pairs,
    int TotalBattles,
    int BaseSeed,
    string SeedAlgorithm,
    double DominanceThresholdPercentagePoints,
    int LineupAWins,
    int LineupBWins,
    int Draws,
    int FirstPositionWins,
    int SecondPositionWins,
    double FirstPositionWinPercentage,
    double SecondPositionWinPercentage,
    double LineupAWinPercentage,
    double LineupBWinPercentage,
    double DrawPercentage,
    double AverageRounds,
    double MedianRounds,
    int MinimumRounds,
    int MaximumRounds,
    string Assessment);
