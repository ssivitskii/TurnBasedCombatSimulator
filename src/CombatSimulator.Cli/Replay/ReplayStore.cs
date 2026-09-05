using CombatSimulator.Application.Replay;
using CombatSimulator.Core.Combat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CombatSimulator.Cli.Replay;

public sealed class ReplayStore
{
    private static readonly JsonSerializerOptions SharedSerializerOptions = CreateOptions();

    public static JsonSerializerOptions SerializerOptions => new(SharedSerializerOptions);

    public async Task SaveAsync(string path, ReplayDocument document, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Validate(document);
        await using FileStream stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReplayDocument> LoadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using FileStream stream = File.OpenRead(path);
        ReplayDocument? document;
        try
        {
            document = await JsonSerializer.DeserializeAsync<ReplayDocument>(
                stream,
                SharedSerializerOptions,
                cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Replay document JSON is malformed.", exception);
        }

        Validate(document ?? throw new InvalidDataException("Replay document is empty."));
        return document;
    }

    public string Serialize(ReplayDocument document)
    {
        Validate(document);
        return JsonSerializer.Serialize(document, SharedSerializerOptions);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private static void Validate(ReplayDocument document)
    {
        ValidateHeader(document);
        ValidateSlots(document.TeamA, BattleTeam.TeamA);
        ValidateSlots(document.TeamB, BattleTeam.TeamB);
        ValidateEvents(document);
    }

    private static void ValidateAttackEvent(ReplayDocument document, ReplayEvent battleEvent)
    {
        if (battleEvent.AttackerId is null || battleEvent.TargetId is null
            || battleEvent.AttackerBefore is null || battleEvent.AttackerAfter is null
            || battleEvent.TargetBefore is null || battleEvent.TargetAfter is null
            || string.IsNullOrWhiteSpace(battleEvent.AttackerName)
            || string.IsNullOrWhiteSpace(battleEvent.TargetName)
            || battleEvent.NetHealthLoss is null
            || battleEvent.TargetDefeated is null
            || battleEvent.SkipReason is not null)
        {
            throw new InvalidDataException("Attack event is incomplete.");
        }

        if (battleEvent.AttackerId.Value.Team != battleEvent.ActingTeam
            || battleEvent.TargetId.Value.Team == battleEvent.ActingTeam)
        {
            throw new InvalidDataException("Attack event team identities are inconsistent.");
        }

        ValidateCombatant(document, battleEvent.AttackerId.Value);
        ValidateCombatant(document, battleEvent.TargetId.Value);
        ValidateSnapshot(battleEvent.AttackerBefore);
        ValidateSnapshot(battleEvent.AttackerAfter);
        ValidateSnapshot(battleEvent.TargetBefore);
        ValidateSnapshot(battleEvent.TargetAfter);
        int loss = Math.Max(0, battleEvent.TargetBefore.Health - battleEvent.TargetAfter.Health);
        bool defeated = battleEvent.TargetBefore.IsAlive && !battleEvent.TargetAfter.IsAlive;
        if (battleEvent.NetHealthLoss != loss || battleEvent.TargetDefeated != defeated)
            throw new InvalidDataException("Attack event derived fields do not match its snapshots.");
    }

    private static void ValidateEvents(ReplayDocument document)
    {
        if (document.Events.Any(battleEvent => battleEvent is null))
            throw new InvalidDataException("Replay event list contains a null event.");
        if (document.Events.Select(battleEvent => battleEvent.Sequence).Where((sequence, index) => sequence != index + 1).Any())
            throw new InvalidDataException("Replay event sequence must be contiguous and one-based.");
        if (document.Events.Any(battleEvent => battleEvent.Round < 1 || battleEvent.Round > document.Result.Rounds))
            throw new InvalidDataException("Replay event round is outside the recorded result.");
        ValidateTimeline(document);

        foreach (ReplayEvent battleEvent in document.Events)
        {
            if (!Enum.IsDefined(battleEvent.ActingTeam))
                throw new InvalidDataException("Replay event contains an undefined acting team.");
            if (battleEvent.Type is "attackResolved")
                ValidateAttackEvent(document, battleEvent);
            else if (battleEvent.Type is "turnSkipped")
                ValidateSkippedEvent(battleEvent);
            else
                throw new InvalidDataException($"Unknown replay event type '{battleEvent.Type}'.");
        }
    }

    private static void ValidateHeader(ReplayDocument document)
    {
        if (document.SchemaVersion != ReplayMapper.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported replay schema version '{document.SchemaVersion}'.");
        if (string.IsNullOrWhiteSpace(document.SimulatorVersion))
            throw new InvalidDataException("Replay simulator provenance is missing.");
        if (document.Configuration is null
            || document.Result is null
            || document.Events is null
            || document.TeamA is null
            || document.TeamB is null)
        {
            throw new InvalidDataException("Replay document contains a null required member.");
        }

        if (!Enum.IsDefined(document.Result.Verdict) || !Enum.IsDefined(document.Result.EndReason))
            throw new InvalidDataException("Replay result contains an undefined value.");
        if (document.Result.Rounds < 0)
            throw new InvalidDataException("Replay round count cannot be negative.");
    }

    private static void ValidateSkippedEvent(ReplayEvent battleEvent)
    {
        bool hasAttackData = battleEvent.AttackerId is not null
            || battleEvent.AttackerName is not null
            || battleEvent.AttackerBefore is not null
            || battleEvent.AttackerAfter is not null
            || battleEvent.TargetId is not null
            || battleEvent.TargetName is not null
            || battleEvent.TargetBefore is not null
            || battleEvent.TargetAfter is not null
            || battleEvent.NetHealthLoss is not null
            || battleEvent.TargetDefeated is not null;
        if (hasAttackData || battleEvent.SkipReason is null || !Enum.IsDefined(battleEvent.SkipReason.Value))
            throw new InvalidDataException("Skipped-turn event has an invalid shape.");
    }

    private static void ValidateCombatant(ReplayDocument document, CombatantId id)
    {
        if (!Enum.IsDefined(id.Team))
            throw new InvalidDataException("Replay references an undefined combatant team.");
        IReadOnlyList<ReplayParticipant> team = id.Team == BattleTeam.TeamA ? document.TeamA : document.TeamB;
        if (id.Slot < 1 || id.Slot > team.Count)
            throw new InvalidDataException($"Replay references unknown combatant {id.Team} slot {id.Slot}.");
    }

    private static void ValidateSlots(IReadOnlyList<ReplayParticipant> participants, BattleTeam team)
    {
        if (participants.Count == 0 || participants.Any(participant => participant is null))
            throw new InvalidDataException($"{team} replay participants are missing.");
        if (participants.Select(participant => participant.Slot).Distinct().Count() != participants.Count
            || participants.Where((participant, index) => participant.Slot != index + 1).Any())
        {
            throw new InvalidDataException($"{team} replay slots must be unique and one-based.");
        }

        if (participants.Any(participant => string.IsNullOrWhiteSpace(participant.Creature)
            || participant.ConfiguredAttack < 0
            || participant.ConfiguredHealth < 0
            || participant.Modifiers is null
            || participant.Modifiers.Any(string.IsNullOrWhiteSpace)))
        {
            throw new InvalidDataException($"{team} replay participant data is invalid.");
        }
    }

    private static void ValidateSnapshot(CombatantSnapshot snapshot)
    {
        if (snapshot.Attack < 0 || snapshot.Health < 0 || snapshot.IsAlive != (snapshot.Health > 0))
            throw new InvalidDataException("Replay combatant snapshot is invalid.");
    }

    private static void ValidateTimeline(ReplayDocument document)
    {
        if (document.Events.Count == 0)
        {
            if (document.Result.Rounds != 0)
                throw new InvalidDataException("An empty replay timeline must record zero rounds.");

            return;
        }

        int eventIndex = 0;
        int expectedRound = 1;
        while (eventIndex < document.Events.Count)
        {
            ReplayEvent first = document.Events[eventIndex];
            if (first.Round != expectedRound || first.ActingTeam != BattleTeam.TeamA)
                throw new InvalidDataException("Each replay round must start with Team A and rounds must be continuous.");
            eventIndex++;
            bool hasTeamBEvent = false;
            if (eventIndex < document.Events.Count && document.Events[eventIndex].Round == expectedRound)
            {
                if (document.Events[eventIndex].ActingTeam != BattleTeam.TeamB)
                    throw new InvalidDataException("The optional second event in a replay round must belong to Team B.");
                hasTeamBEvent = true;
                eventIndex++;
            }

            if (eventIndex < document.Events.Count && document.Events[eventIndex].Round == expectedRound)
                throw new InvalidDataException("A replay round cannot contain more than two events.");
            if (!hasTeamBEvent && eventIndex < document.Events.Count)
                throw new InvalidDataException("Only the final replay round may omit Team B's event.");
            expectedRound++;
        }

        if (document.Result.Rounds != expectedRound - 1)
            throw new InvalidDataException("The replay result round count must match the recorded timeline.");
    }
}
