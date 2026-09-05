using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Boards;

namespace CombatSimulator.Core.Combat;

public sealed class CombatRunner
{
    private readonly IRandomNumberGenerator _random;
    private readonly int _roundLimit;

    public CombatRunner(IRandomNumberGenerator random, int roundLimit = 10000)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roundLimit);
        _roundLimit = roundLimit;
    }

    public BattleResult Run(
        PlayerBoard teamA,
        PlayerBoard teamB,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teamA);
        ArgumentNullException.ThrowIfNull(teamB);
        PlayerBoard first = teamA.DeepCopy();
        PlayerBoard second = teamB.DeepCopy();
        var events = new List<BattleEvent>();
        int firstIndex = -1;
        int secondIndex = -1;

        for (int round = 1; round <= _roundLimit; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BattleResult? terminal = GetTerminal(first, second, round - 1, events);
            if (terminal is not null)
                return terminal;

            PerformTurn(first, second, ref firstIndex, BattleTeam.TeamA, round, events);
            terminal = GetTerminal(first, second, round, events);
            if (terminal is not null)
                return terminal;

            PerformTurn(second, first, ref secondIndex, BattleTeam.TeamB, round, events);
            terminal = GetTerminal(first, second, round, events);
            if (terminal is not null)
                return terminal;
        }

        return new BattleResult(BattleVerdict.Draw, BattleEndReason.RoundLimitReached, _roundLimit, events);
    }

    private static CombatantSnapshot Capture(ICreature creature)
    {
        return new CombatantSnapshot(creature.Attack.Value, creature.Health.Value, creature.IsAlive);
    }

    private static BattleResult? GetTerminal(
        PlayerBoard first,
        PlayerBoard second,
        int rounds,
        List<BattleEvent> events)
    {
        bool firstLiving = first.Living.Any();
        bool secondLiving = second.Living.Any();
        if (!firstLiving || !secondLiving)
        {
            BattleVerdict verdict = (firstLiving, secondLiving) switch
            {
                (true, false) => BattleVerdict.TeamAVictory,
                (false, true) => BattleVerdict.TeamBVictory,
                _ => BattleVerdict.Draw,
            };
            return new BattleResult(verdict, BattleEndReason.TeamDefeated, rounds, events);
        }

        if (!first.Attackers.Any() && !second.Attackers.Any())
            return new BattleResult(BattleVerdict.Draw, BattleEndReason.Stalemate, rounds, events);
        return null;
    }

    private static (int Index, ICreature Creature)? NextAttacker(PlayerBoard board, ref int lastIndex)
    {
        for (int offset = 1; offset <= board.Slots.Count; offset++)
        {
            int index = (lastIndex + offset) % board.Slots.Count;
            if (board.Slots[index].CanAttack)
            {
                lastIndex = index;
                return (index, board.Slots[index]);
            }
        }

        return null;
    }

    private static BattleTeam Opponent(BattleTeam team)
    {
        return team == BattleTeam.TeamA ? BattleTeam.TeamB : BattleTeam.TeamA;
    }

    private void PerformTurn(
        PlayerBoard attackers,
        PlayerBoard defenders,
        ref int lastIndex,
        BattleTeam attackingTeam,
        int round,
        List<BattleEvent> events)
    {
        (int Index, ICreature Creature)? attacker = NextAttacker(attackers, ref lastIndex);
        if (attacker is null)
        {
            events.Add(new TurnSkippedEvent(
                events.Count + 1,
                round,
                attackingTeam,
                TurnSkipReason.NoEligibleAttacker));
            return;
        }

        (ICreature Creature, int Index)[] livingDefenders = defenders.Slots
            .Select((creature, index) => (Creature: creature, Index: index))
            .Where(candidate => candidate.Creature.IsAlive)
            .ToArray();
        (ICreature Creature, int Index) target = livingDefenders[_random.NextInt(0, livingDefenders.Length)];
        CombatantSnapshot attackerBefore = Capture(attacker.Value.Creature);
        CombatantSnapshot targetBefore = Capture(target.Creature);
        attacker.Value.Creature.AttackTarget(target.Creature, _random);
        events.Add(new AttackResolvedEvent(
            events.Count + 1,
            round,
            attackingTeam,
            new CombatantId(attackingTeam, attacker.Value.Index + 1),
            attacker.Value.Creature.Name,
            attackerBefore,
            Capture(attacker.Value.Creature),
            new CombatantId(Opponent(attackingTeam), target.Index + 1),
            target.Creature.Name,
            targetBefore,
            Capture(target.Creature)));
    }
}
