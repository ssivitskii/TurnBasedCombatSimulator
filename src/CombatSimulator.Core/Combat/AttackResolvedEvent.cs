namespace CombatSimulator.Core.Combat;

public sealed record AttackResolvedEvent(
    int Sequence,
    int Round,
    BattleTeam ActingTeam,
    CombatantId AttackerId,
    string AttackerName,
    CombatantSnapshot AttackerBefore,
    CombatantSnapshot AttackerAfter,
    CombatantId TargetId,
    string TargetName,
    CombatantSnapshot TargetBefore,
    CombatantSnapshot TargetAfter)
    : BattleEvent(Sequence, Round, ActingTeam)
{
    public int NetHealthLoss => Math.Max(0, TargetBefore.Health - TargetAfter.Health);

    public bool TargetDefeated => TargetBefore.IsAlive && !TargetAfter.IsAlive;
}
