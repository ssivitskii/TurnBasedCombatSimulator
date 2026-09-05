namespace CombatSimulator.Core.Combat;

public sealed record TurnSkippedEvent(
    int Sequence,
    int Round,
    BattleTeam ActingTeam,
    TurnSkipReason Reason)
    : BattleEvent(Sequence, Round, ActingTeam);
