using CombatSimulator.Core.Combat;

namespace CombatSimulator.Application.Replay;

public sealed record ReplayEvent(
    string Type,
    int Sequence,
    int Round,
    BattleTeam ActingTeam,
    CombatantId? AttackerId,
    string? AttackerName,
    CombatantSnapshot? AttackerBefore,
    CombatantSnapshot? AttackerAfter,
    CombatantId? TargetId,
    string? TargetName,
    CombatantSnapshot? TargetBefore,
    CombatantSnapshot? TargetAfter,
    int? NetHealthLoss,
    bool? TargetDefeated,
    TurnSkipReason? SkipReason);
