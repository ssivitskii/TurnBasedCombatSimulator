using CombatSimulator.Application.Configuration;
using CombatSimulator.Core.Combat;

namespace CombatSimulator.Application.Replay;

public static class ReplayMapper
{
    public const int CurrentSchemaVersion = 1;
    public const string CurrentSimulatorVersion = "1.0.0";

    public static ReplayDocument Create(BattleDefinition definition, int seed, BattleResult result)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(result);
        BattleConfiguration configuration = definition.Configuration;
        return new ReplayDocument(
            CurrentSchemaVersion,
            CurrentSimulatorVersion,
            seed,
            configuration,
            MapParticipants(configuration.TeamA),
            MapParticipants(configuration.TeamB),
            new ReplayResult(result.Verdict, result.EndReason, result.Rounds),
            result.Events.Select(MapEvent).ToArray());
    }

    private static ReplayEvent MapEvent(BattleEvent battleEvent) => battleEvent switch
    {
        AttackResolvedEvent attack => new ReplayEvent(
            "attackResolved",
            attack.Sequence,
            attack.Round,
            attack.ActingTeam,
            attack.AttackerId,
            attack.AttackerName,
            attack.AttackerBefore,
            attack.AttackerAfter,
            attack.TargetId,
            attack.TargetName,
            attack.TargetBefore,
            attack.TargetAfter,
            attack.NetHealthLoss,
            attack.TargetDefeated,
            null),
        TurnSkippedEvent skipped => new ReplayEvent(
            "turnSkipped",
            skipped.Sequence,
            skipped.Round,
            skipped.ActingTeam,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            skipped.Reason),
        _ => throw new InvalidOperationException("Unknown battle event type."),
    };

    private static ReplayParticipant[] MapParticipants(
        IReadOnlyList<BattleConfiguration.CreatureConfiguration>? participants) =>
        participants?.Select((participant, index) => new ReplayParticipant(
            index + 1,
            participant.Creature ?? "<missing>",
            participant.Attack,
            participant.Health,
            participant.Modifiers?.ToArray() ?? [])).ToArray() ?? [];
}
