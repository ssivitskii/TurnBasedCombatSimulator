using CombatSimulator.Core.Combat;

namespace CombatSimulator.Cli.Tournament;

public sealed record GameSummary(
    int Index,
    int Seed,
    BattleVerdict Verdict,
    BattleEndReason EndReason,
    int Rounds,
    long ObservedNetHealthLossCausedByTeamA,
    long ObservedNetHealthLossCausedByTeamB,
    int TeamATargetDefeats,
    int TeamBTargetDefeats);
