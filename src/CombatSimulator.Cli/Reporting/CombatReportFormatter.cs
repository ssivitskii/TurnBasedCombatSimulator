using CombatSimulator.Application.Replay;
using CombatSimulator.Cli.Replay;
using CombatSimulator.Cli.Tournament;
using CombatSimulator.Core.Combat;
using System.Globalization;
using System.Text.Json;

namespace CombatSimulator.Cli.Reporting;

public static class CombatReportFormatter
{
    public static async Task WriteBattleAsync(TextWriter output, ReplayDocument replay, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            await WriteJsonAsync(output, replay).ConfigureAwait(false);
            return;
        }

        if (format == OutputFormat.Csv)
        {
            await output.WriteLineAsync(
                "sequence,round,actingTeam,type,attackerTeam,attackerSlot,attackerName," +
                "attackerBeforeAttack,attackerBeforeHealth,attackerBeforeAlive," +
                "attackerAfterAttack,attackerAfterHealth,attackerAfterAlive," +
                "targetTeam,targetSlot,targetName,targetBeforeAttack,targetBeforeHealth,targetBeforeAlive," +
                "targetAfterAttack,targetAfterHealth,targetAfterAlive,netHealthLoss,targetDefeated,skipReason")
                .ConfigureAwait(false);
            foreach (ReplayEvent battleEvent in replay.Events)
            {
                await output.WriteLineAsync(string.Join(
                    ",",
                    battleEvent.Sequence.ToString(CultureInfo.InvariantCulture),
                    battleEvent.Round.ToString(CultureInfo.InvariantCulture),
                    battleEvent.ActingTeam,
                    battleEvent.Type,
                    FormatTeam(battleEvent.AttackerId),
                    FormatSlot(battleEvent.AttackerId),
                    Escape(battleEvent.AttackerName),
                    FormatAttack(battleEvent.AttackerBefore),
                    FormatHealth(battleEvent.AttackerBefore),
                    FormatAlive(battleEvent.AttackerBefore),
                    FormatAttack(battleEvent.AttackerAfter),
                    FormatHealth(battleEvent.AttackerAfter),
                    FormatAlive(battleEvent.AttackerAfter),
                    FormatTeam(battleEvent.TargetId),
                    FormatSlot(battleEvent.TargetId),
                    Escape(battleEvent.TargetName),
                    FormatAttack(battleEvent.TargetBefore),
                    FormatHealth(battleEvent.TargetBefore),
                    FormatAlive(battleEvent.TargetBefore),
                    FormatAttack(battleEvent.TargetAfter),
                    FormatHealth(battleEvent.TargetAfter),
                    FormatAlive(battleEvent.TargetAfter),
                    Format(battleEvent.NetHealthLoss),
                    Format(battleEvent.TargetDefeated),
                    battleEvent.SkipReason?.ToString() ?? string.Empty))
                    .ConfigureAwait(false);
            }

            return;
        }

        await output.WriteLineAsync($"Team A: {string.Join(", ", replay.TeamA.Select(ItemLabel))}")
            .ConfigureAwait(false);
        await output.WriteLineAsync($"Team B: {string.Join(", ", replay.TeamB.Select(ItemLabel))}")
            .ConfigureAwait(false);
        foreach (ReplayEvent battleEvent in replay.Events)
        {
            if (string.Equals(battleEvent.Type, "attackResolved", StringComparison.Ordinal))
            {
                string defeated = battleEvent.TargetDefeated == true ? "; target defeated" : string.Empty;
                await output.WriteLineAsync(
                    $"#{battleEvent.Sequence} round {battleEvent.Round}: {battleEvent.ActingTeam} " +
                    $"[{battleEvent.AttackerId!.Value.Slot}] {battleEvent.AttackerName} -> " +
                    $"{battleEvent.TargetId!.Value.Team}[{battleEvent.TargetId.Value.Slot}] " +
                    $"{battleEvent.TargetName}; net health loss {battleEvent.NetHealthLoss}{defeated}")
                    .ConfigureAwait(false);
            }
            else
            {
                await output.WriteLineAsync(
                    $"#{battleEvent.Sequence} round {battleEvent.Round}: {battleEvent.ActingTeam} skipped ({battleEvent.SkipReason})")
                    .ConfigureAwait(false);
            }
        }

        await output.WriteLineAsync($"Winner: {FormatVerdict(replay.Result.Verdict)}").ConfigureAwait(false);
        await output.WriteLineAsync($"Rounds: {replay.Result.Rounds}; reason: {replay.Result.EndReason}")
            .ConfigureAwait(false);
    }

    public static async Task WriteTournamentAsync(TextWriter output, TournamentReport report, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            await WriteJsonAsync(output, report).ConfigureAwait(false);
            return;
        }

        if (format == OutputFormat.Csv)
        {
            await output.WriteLineAsync("section,metric,value,unit").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "mode", report.Mode, "text").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "games", Format(report.Games), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "base_seed", Format(report.BaseSeed), "seed").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "seed_algorithm", report.SeedAlgorithm, "text").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "outcomes", "team_a_wins", Format(report.TeamAWins), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "outcomes", "team_a_win_percentage", Format(report.TeamAWinPercentage), "percent").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "outcomes", "team_b_wins", Format(report.TeamBWins), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "outcomes", "team_b_win_percentage", Format(report.TeamBWinPercentage), "percent").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "outcomes", "draws", Format(report.Draws), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "outcomes", "draw_percentage", Format(report.DrawPercentage), "percent").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "rounds", "average", Format(report.AverageRounds), "rounds").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "rounds", "median", Format(report.MedianRounds), "rounds").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "rounds", "minimum", Format(report.MinimumRounds), "rounds").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "rounds", "maximum", Format(report.MaximumRounds), "rounds").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "observable", "net_health_loss_caused_by_team_a", Format(report.ObservedNetHealthLossCausedByTeamA), "health").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "observable", "net_health_loss_caused_by_team_b", Format(report.ObservedNetHealthLossCausedByTeamB), "health").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "observable", "team_a_target_defeats", Format(report.TeamATargetDefeats), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "observable", "team_b_target_defeats", Format(report.TeamBTargetDefeats), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "terminations", "team_defeated", Format(report.TeamDefeatedEnds), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "terminations", "stalemate", Format(report.Stalemates), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "terminations", "round_limit", Format(report.RoundLimitEnds), "count").ConfigureAwait(false);
            return;
        }

        await output.WriteLineAsync($"Simulations: {report.Games}").ConfigureAwait(false);
        await output.WriteLineAsync($"Base seed: {report.BaseSeed}; seed algorithm: {report.SeedAlgorithm}")
            .ConfigureAwait(false);
        await output.WriteLineAsync($"Team A victories: {report.TeamAWins} ({Format(report.TeamAWinPercentage)}%)")
            .ConfigureAwait(false);
        await output.WriteLineAsync($"Team B victories: {report.TeamBWins} ({Format(report.TeamBWinPercentage)}%)")
            .ConfigureAwait(false);
        await output.WriteLineAsync($"Draws: {report.Draws} ({Format(report.DrawPercentage)}%)")
            .ConfigureAwait(false);
        await output.WriteLineAsync(
            $"Rounds avg/median/min/max: {Format(report.AverageRounds)} / {Format(report.MedianRounds)} / " +
            $"{report.MinimumRounds} / {report.MaximumRounds}")
            .ConfigureAwait(false);
        await output.WriteLineAsync(
            $"Observable net health loss caused by A/B: {report.ObservedNetHealthLossCausedByTeamA} / " +
            $"{report.ObservedNetHealthLossCausedByTeamB}; " +
            $"target defeats A/B: {report.TeamATargetDefeats} / {report.TeamBTargetDefeats}")
            .ConfigureAwait(false);
        await output.WriteLineAsync(
            "Outcome frequencies describe this configuration and seed set; they are not proof of statistical balance.")
            .ConfigureAwait(false);
    }

    public static async Task WriteBalanceAsync(TextWriter output, BalanceReport report, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            await WriteJsonAsync(output, report).ConfigureAwait(false);
            return;
        }

        if (format == OutputFormat.Csv)
        {
            await output.WriteLineAsync("section,metric,value,unit").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "mode", report.Mode, "text").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "pairs", Format(report.Pairs), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "total_battles", Format(report.TotalBattles), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "base_seed", Format(report.BaseSeed), "seed").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "metadata", "seed_algorithm", report.SeedAlgorithm, "text").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "lineups", "lineup_a_wins", Format(report.LineupAWins), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "lineups", "lineup_a_win_percentage", Format(report.LineupAWinPercentage), "percent").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "lineups", "lineup_b_wins", Format(report.LineupBWins), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "lineups", "lineup_b_win_percentage", Format(report.LineupBWinPercentage), "percent").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "lineups", "draws", Format(report.Draws), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "lineups", "draw_percentage", Format(report.DrawPercentage), "percent").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "positions", "first_position_wins", Format(report.FirstPositionWins), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "positions", "first_position_win_percentage", Format(report.FirstPositionWinPercentage), "percent").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "positions", "second_position_wins", Format(report.SecondPositionWins), "count").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "positions", "second_position_win_percentage", Format(report.SecondPositionWinPercentage), "percent").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "rounds", "average", Format(report.AverageRounds), "rounds").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "rounds", "median", Format(report.MedianRounds), "rounds").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "rounds", "minimum", Format(report.MinimumRounds), "rounds").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "rounds", "maximum", Format(report.MaximumRounds), "rounds").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "assessment", "dominance_threshold", Format(report.DominanceThresholdPercentagePoints), "percentage_points").ConfigureAwait(false);
            await WriteCsvMetricAsync(output, "assessment", "result", report.Assessment, "text").ConfigureAwait(false);
            return;
        }

        await output.WriteLineAsync($"Mirrored pairs: {report.Pairs}; total battles: {report.TotalBattles}")
            .ConfigureAwait(false);
        await output.WriteLineAsync($"Base seed: {report.BaseSeed}; seed algorithm: {report.SeedAlgorithm}")
            .ConfigureAwait(false);
        await output.WriteLineAsync($"Configured lineup A wins: {report.LineupAWins} ({Format(report.LineupAWinPercentage)}%)")
            .ConfigureAwait(false);
        await output.WriteLineAsync($"Configured lineup B wins: {report.LineupBWins} ({Format(report.LineupBWinPercentage)}%)")
            .ConfigureAwait(false);
        await output.WriteLineAsync($"Draws: {report.Draws} ({Format(report.DrawPercentage)}%)")
            .ConfigureAwait(false);
        await output.WriteLineAsync(
            $"First/second position wins: {report.FirstPositionWins} ({Format(report.FirstPositionWinPercentage)}%) / " +
            $"{report.SecondPositionWins} ({Format(report.SecondPositionWinPercentage)}%)")
            .ConfigureAwait(false);
        await output.WriteLineAsync(
            $"Dominance threshold: {Format(report.DominanceThresholdPercentagePoints)} percentage points; {report.Assessment}")
            .ConfigureAwait(false);
        await output.WriteLineAsync(
            "Paired outcome frequencies are diagnostics for this configuration and seed set, not rigorous balance proof.")
            .ConfigureAwait(false);
    }

    private static string Escape(string? value)
    {
        return value is null ? string.Empty : $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string Format(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string Format(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string Format(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Format(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Format(bool? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatAlive(CombatantSnapshot? snapshot)
    {
        return Format(snapshot?.IsAlive);
    }

    private static string FormatAttack(CombatantSnapshot? snapshot)
    {
        return Format(snapshot?.Attack);
    }

    private static string FormatHealth(CombatantSnapshot? snapshot)
    {
        return Format(snapshot?.Health);
    }

    private static string FormatSlot(CombatantId? id)
    {
        return Format(id?.Slot);
    }

    private static string FormatTeam(CombatantId? id)
    {
        return id?.Team.ToString() ?? string.Empty;
    }

    private static string FormatVerdict(BattleVerdict verdict)
    {
        return verdict switch
        {
            BattleVerdict.TeamAVictory => "Team A",
            BattleVerdict.TeamBVictory => "Team B",
            BattleVerdict.Draw => "Draw",
            _ => throw new ArgumentOutOfRangeException(nameof(verdict)),
        };
    }

    private static string ItemLabel(ReplayParticipant participant)
    {
        return $"[{participant.Slot}] {participant.Creature}";
    }

    private static async Task WriteCsvMetricAsync(
        TextWriter output,
        string section,
        string metric,
        string value,
        string unit)
    {
        await output.WriteLineAsync(string.Join(
            ",",
            Escape(section),
            Escape(metric),
            Escape(value),
            Escape(unit))).ConfigureAwait(false);
    }

    private static async Task WriteJsonAsync<T>(TextWriter output, T report)
    {
        await output.WriteLineAsync(JsonSerializer.Serialize(report, ReplayStore.SerializerOptions)).ConfigureAwait(false);
    }
}
