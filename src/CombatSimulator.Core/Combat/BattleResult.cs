namespace CombatSimulator.Core.Combat;

public sealed class BattleResult
{
    private readonly BattleEvent[] _events;

    public BattleResult(
        BattleVerdict verdict,
        BattleEndReason endReason,
        int rounds,
        IEnumerable<BattleEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        Verdict = verdict;
        EndReason = endReason;
        Rounds = rounds;
        _events = events.ToArray();
    }

    public BattleVerdict Verdict { get; }

    public BattleEndReason EndReason { get; }

    public int Rounds { get; }

    public IReadOnlyList<BattleEvent> Events => _events;
}
