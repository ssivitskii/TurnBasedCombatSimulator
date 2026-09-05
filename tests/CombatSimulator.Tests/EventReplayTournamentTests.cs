using CombatSimulator.Application.Configuration;
using CombatSimulator.Application.Replay;
using CombatSimulator.Cli;
using CombatSimulator.Cli.Replay;
using CombatSimulator.Cli.Reporting;
using CombatSimulator.Cli.Tournament;
using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Boards;
using CombatSimulator.Core.Combat;
using CombatSimulator.Core.Randomness;
using System.Text.Json;

namespace CombatSimulator.Tests;

public sealed class EventReplayTournamentTests
{
    private static readonly int[] ExpectedEventSequences = [1, 2];

    [Fact]
    public void EventsUseOrderedSequencesAndTeamSlotIdentityForDuplicateNames()
    {
        var teamA = new PlayerBoard();
        var teamB = new PlayerBoard();
        teamA.Add(new TestCreature("Duplicate", 1, 100));
        teamA.Add(new TestCreature("Duplicate", 1, 100));
        teamB.Add(new TestCreature("Duplicate", 1, 100));
        teamB.Add(new TestCreature("Duplicate", 1, 100));

        BattleResult result = new CombatRunner(new SystemRandomNumberGenerator(42), 1).Run(teamA, teamB);
        AttackResolvedEvent[] attacks = result.Events.OfType<AttackResolvedEvent>().ToArray();

        Assert.Equal(ExpectedEventSequences, attacks.Select(battleEvent => battleEvent.Sequence));
        Assert.Equal(BattleTeam.TeamA, attacks[0].AttackerId.Team);
        Assert.Equal(BattleTeam.TeamB, attacks[0].TargetId.Team);
        Assert.InRange(attacks[0].AttackerId.Slot, 1, 2);
        Assert.InRange(attacks[0].TargetId.Slot, 1, 2);
        Assert.Equal(1, attacks[0].NetHealthLoss);
        Assert.Equal(
            attacks[0].TargetBefore.Health - 1,
            attacks[0].TargetAfter.Health);
    }

    [Fact]
    public void CombatHonorsCancellationAtRoundBoundary()
    {
        (PlayerBoard teamA, PlayerBoard teamB) = CreateDefinition().BuildBoards();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => new CombatRunner(
            new SystemRandomNumberGenerator(42)).Run(teamA, teamB, cancellation.Token));
    }

    [Fact]
    public async Task ReplayRoundTripIsByteDeterministic()
    {
        BattleDefinition definition = CreateDefinition();
        ReplayDocument document = RunReplay(definition, 42);
        string firstPath = Path.GetTempFileName();
        string secondPath = Path.GetTempFileName();
        try
        {
            var store = new ReplayStore();
            await store.SaveAsync(firstPath, document, CancellationToken.None);
            ReplayDocument loaded = await store.LoadAsync(firstPath, CancellationToken.None);
            await store.SaveAsync(secondPath, loaded, CancellationToken.None);

            Assert.Equal(
                await File.ReadAllTextAsync(firstPath),
                await File.ReadAllTextAsync(secondPath));
            Assert.Equal(document.Result, loaded.Result);
            Assert.Equal(document.Events, loaded.Events);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReplayRejectsUnknownSchemaAndBrokenSequence(bool invalidSchema)
    {
        ReplayDocument valid = RunReplay(CreateDefinition(), 42);
        ReplayDocument invalid = invalidSchema
            ? valid with { SchemaVersion = 99 }
            : valid with { Events = valid.Events.Skip(1).ToArray() };
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(invalid, ReplayStore.SerializerOptions));

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new ReplayStore().LoadAsync(path, CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task TournamentOutputIsIndependentOfParallelism()
    {
        BattleDefinition definition = CreateDefinition();
        var runner = new TournamentRunner();

        TournamentReport sequential = await runner.RunAsync(definition, 40, 42, 1, CancellationToken.None);
        TournamentReport parallel = await runner.RunAsync(definition, 40, 42, 4, CancellationToken.None);
        using var sequentialJson = new StringWriter();
        using var parallelJson = new StringWriter();
        await CombatReportFormatter.WriteTournamentAsync(sequentialJson, sequential, OutputFormat.Json);
        await CombatReportFormatter.WriteTournamentAsync(parallelJson, parallel, OutputFormat.Json);

        Assert.Equal(sequential, parallel);
        Assert.Equal(sequentialJson.ToString(), parallelJson.ToString());
        Assert.Equal(40, sequential.TeamAWins + sequential.TeamBWins + sequential.Draws);
    }

    [Theory]
    [InlineData(42, 0, 1422671178)]
    [InlineData(42, 1, 163628348)]
    [InlineData(42, 2, -1468133653)]
    [InlineData(0, 0, -1724029546)]
    public void SeedDerivationUsesStableSplitMixVectors(int baseSeed, int index, int expected)
    {
        Assert.Equal(expected, SeedDerivation.Derive(baseSeed, index));
    }

    [Fact]
    public void TournamentStatisticsCalculateEvenMedianAndObservableTotals()
    {
        GameSummary[] games =
        [
            Summary(0, BattleVerdict.TeamAVictory, 1, 2),
            Summary(1, BattleVerdict.TeamBVictory, 2, 3),
            Summary(2, BattleVerdict.Draw, 3, 4),
            Summary(3, BattleVerdict.TeamAVictory, 4, 5),
        ];

        var report = TournamentReport.Create("test", 42, games);

        Assert.Equal(2.5, report.MedianRounds);
        Assert.Equal(2.5, report.AverageRounds);
        Assert.Equal(14, report.ObservedNetHealthLossCausedByTeamA);
        Assert.Equal(18, report.ObservedNetHealthLossCausedByTeamB);
        Assert.Equal(2, report.TeamAWins);
    }

    [Fact]
    public async Task BalanceRunsTwoMirroredBattlesPerRequestedGame()
    {
        BalanceReport report = await new TournamentRunner().RunBalanceAsync(
            CreateDefinition(),
            12,
            42,
            4,
            10,
            CancellationToken.None);

        Assert.Equal(12, report.Pairs);
        Assert.Equal(24, report.TotalBattles);
        Assert.Equal(24, report.LineupAWins + report.LineupBWins + report.Draws);
        Assert.True(report.Assessment.Contains("threshold", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TournamentStatisticsAvoidIntegerOverflow()
    {
        GameSummary[] games =
        [
            Summary(0, BattleVerdict.TeamAVictory, int.MaxValue, int.MaxValue),
            Summary(1, BattleVerdict.TeamBVictory, int.MaxValue, int.MaxValue),
        ];

        var report = TournamentReport.Create("overflow-boundary", 42, games);

        Assert.Equal(int.MaxValue, report.AverageRounds);
        Assert.Equal(int.MaxValue, report.MedianRounds);
        Assert.Equal(2L * int.MaxValue, report.ObservedNetHealthLossCausedByTeamA);
    }

    [Fact]
    public async Task TournamentRejectsParallelismAboveDocumentedCap()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new TournamentRunner().RunAsync(
            CreateDefinition(),
            1,
            42,
            TournamentRunner.MaximumParallelism + 1,
            CancellationToken.None));
    }

    [Fact]
    public async Task ReplayAllowsDifferentNonblankSimulatorProvenanceForSupportedSchema()
    {
        ReplayDocument document = RunReplay(CreateDefinition(), 42) with { SimulatorVersion = "future-build" };
        string path = Path.GetTempFileName();
        try
        {
            await new ReplayStore().SaveAsync(path, document, CancellationToken.None);

            ReplayDocument loaded = await new ReplayStore().LoadAsync(path, CancellationToken.None);

            Assert.Equal("future-build", loaded.SimulatorVersion);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("configuration")]
    [InlineData("teamA")]
    [InlineData("teamB")]
    [InlineData("result")]
    [InlineData("events")]
    public async Task ReplayRejectsNullRequiredMembers(string member)
    {
        ReplayDocument valid = RunReplay(CreateDefinition(), 42);
        ReplayDocument invalid = member switch
        {
            "configuration" => valid with { Configuration = null! },
            "teamA" => valid with { TeamA = null! },
            "teamB" => valid with { TeamB = null! },
            "result" => valid with { Result = null! },
            "events" => valid with { Events = null! },
            _ => throw new ArgumentOutOfRangeException(nameof(member)),
        };

        await AssertInvalidReplayAsync(invalid);
    }

    [Fact]
    public async Task ReplayRejectsMalformedAttackShape()
    {
        ReplayDocument valid = RunReplay(CreateDefinition(), 42);
        ReplayEvent attack = valid.Events.First(battleEvent => battleEvent.Type == "attackResolved");
        ReplayDocument invalid = valid with
        {
            Events = [attack with { ActingTeam = attack.TargetId!.Value.Team }],
        };

        await AssertInvalidReplayAsync(invalid);
    }

    [Fact]
    public async Task ReplayRejectsMalformedTopLevelSyntaxAndNumericEnums()
    {
        await AssertInvalidJsonAsync("{");
        await AssertInvalidJsonAsync("{}");
        string validJson = JsonSerializer.Serialize(RunReplay(CreateDefinition(), 42), ReplayStore.SerializerOptions);
        string numericEnum = validJson.Replace(
            "\"actingTeam\": \"teamA\"",
            "\"actingTeam\": 99",
            StringComparison.Ordinal);
        Assert.NotEqual(validJson, numericEnum);

        await AssertInvalidJsonAsync(numericEnum);
    }

    [Fact]
    public async Task ReplayRejectsInvalidSnapshotsAndSkippedEventShape()
    {
        ReplayDocument valid = RunReplay(CreateDefinition(), 42);
        ReplayEvent first = valid.Events[0];
        ReplayDocument invalidSnapshot = valid with
        {
            Events = [first with { TargetAfter = new CombatantSnapshot(0, -1, false) }],
            Result = valid.Result with { Rounds = 1 },
        };
        ReplayDocument invalidSkip = valid with
        {
            Events = [first with { Type = "turnSkipped", SkipReason = TurnSkipReason.NoEligibleAttacker }],
            Result = valid.Result with { Rounds = 1 },
        };

        await AssertInvalidReplayAsync(invalidSnapshot);
        await AssertInvalidReplayAsync(invalidSkip);
    }

    [Theory]
    [InlineData("wrong-first-team")]
    [InlineData("round-gap")]
    [InlineData("third-event")]
    [InlineData("result-round-mismatch")]
    [InlineData("missing-team-b-before-next-round")]
    public async Task ReplayRejectsInvalidRoundTimeline(string scenario)
    {
        ReplayDocument valid = RunReplay(CreateDefinition(), 42);
        ReplayEvent first = valid.Events[0];
        ReplayEvent second = valid.Events[1];
        ReplayDocument invalid = scenario switch
        {
            "wrong-first-team" => valid with
            {
                Events = [first with { ActingTeam = BattleTeam.TeamB }],
                Result = valid.Result with { Rounds = 1 },
            },
            "round-gap" => valid with
            {
                Events = valid.Events.Select(item => item with { Round = item.Round + 1 }).ToArray(),
                Result = valid.Result with { Rounds = valid.Result.Rounds + 1 },
            },
            "third-event" => valid with
            {
                Events =
                [
                    first,
                    second,
                    second with { Sequence = 3 },
                ],
                Result = valid.Result with { Rounds = 1 },
            },
            "result-round-mismatch" => valid with
            {
                Result = valid.Result with { Rounds = valid.Result.Rounds + 1 },
            },
            "missing-team-b-before-next-round" => valid with
            {
                Events =
                [
                    first,
                    first with { Sequence = 2, Round = 2 },
                ],
                Result = valid.Result with { Rounds = 2 },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        await AssertInvalidReplayAsync(invalid);
    }

    [Fact]
    public async Task EmptyReplayTimelineRequiresZeroRounds()
    {
        ReplayDocument valid = RunReplay(CreateDefinition(), 42);
        ReplayDocument empty = valid with
        {
            Events = [],
            Result = valid.Result with { Rounds = 0 },
        };
        string path = Path.GetTempFileName();
        try
        {
            await new ReplayStore().SaveAsync(path, empty, CancellationToken.None);
            Assert.Empty((await new ReplayStore().LoadAsync(path, CancellationToken.None)).Events);
        }
        finally
        {
            File.Delete(path);
        }

        await AssertInvalidReplayAsync(empty with { Result = empty.Result with { Rounds = 1 } });
    }

    [Fact]
    public async Task BalanceThresholdEqualityIsNotReportedAsDominance()
    {
        var runner = new TournamentRunner();
        BalanceReport baseline = await runner.RunBalanceAsync(
            CreateDefinition(),
            8,
            42,
            1,
            100,
            CancellationToken.None);
        double exactDifference = Math.Abs(baseline.LineupAWinPercentage - baseline.LineupBWinPercentage);

        BalanceReport boundary = await runner.RunBalanceAsync(
            CreateDefinition(),
            8,
            42,
            1,
            exactDifference,
            CancellationToken.None);

        Assert.Equal(
            "Neither configured lineup exceeded the configured dominance threshold.",
            boundary.Assessment);
    }

    [Fact]
    public async Task MaximumDocumentedParallelismProducesDeterministicReport()
    {
        var runner = new TournamentRunner();
        TournamentReport sequential = await runner.RunAsync(CreateDefinition(), 8, 42, 1, CancellationToken.None);
        TournamentReport maximum = await runner.RunAsync(
            CreateDefinition(),
            8,
            42,
            TournamentRunner.MaximumParallelism,
            CancellationToken.None);

        Assert.Equal(sequential, maximum);
    }

    [Fact]
    public async Task CsvReportsExposeFixedMetadataAndObservableFields()
    {
        ReplayDocument replay = RunReplay(CreateDefinition(), 42);
        TournamentReport tournament = await new TournamentRunner().RunAsync(
            CreateDefinition(),
            2,
            42,
            1,
            CancellationToken.None);
        BalanceReport balance = await new TournamentRunner().RunBalanceAsync(
            CreateDefinition(),
            2,
            42,
            1,
            10,
            CancellationToken.None);
        using var replayOutput = new StringWriter();
        using var tournamentOutput = new StringWriter();
        using var balanceOutput = new StringWriter();

        await CombatReportFormatter.WriteBattleAsync(replayOutput, replay, OutputFormat.Csv);
        await CombatReportFormatter.WriteTournamentAsync(tournamentOutput, tournament, OutputFormat.Csv);
        await CombatReportFormatter.WriteBalanceAsync(balanceOutput, balance, OutputFormat.Csv);

        string[] replayLines = replayOutput.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        string[] tournamentLines = tournamentOutput.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        string[] balanceLines = balanceOutput.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(
            "sequence,round,actingTeam,type,attackerTeam,attackerSlot,attackerName," +
            "attackerBeforeAttack,attackerBeforeHealth,attackerBeforeAlive," +
            "attackerAfterAttack,attackerAfterHealth,attackerAfterAlive," +
            "targetTeam,targetSlot,targetName,targetBeforeAttack,targetBeforeHealth,targetBeforeAlive," +
            "targetAfterAttack,targetAfterHealth,targetAfterAlive,netHealthLoss,targetDefeated,skipReason",
            replayLines[0]);
        Assert.All(replayLines.Skip(1), line => Assert.Equal(25, line.Split(',').Length));
        Assert.Equal("section,metric,value,unit", tournamentLines[0]);
        Assert.Equal("\"metadata\",\"mode\",\"tournament\",\"text\"", tournamentLines[1]);
        Assert.Contains("\"metadata\",\"seed_algorithm\",\"splitmix64-v1\",\"text\"", tournamentLines);
        Assert.Contains(tournamentLines, line => line.StartsWith(
            "\"terminations\",\"round_limit\",",
            StringComparison.Ordinal));
        Assert.Equal("section,metric,value,unit", balanceLines[0]);
        Assert.Equal("\"metadata\",\"mode\",\"paired-balance\",\"text\"", balanceLines[1]);
        Assert.Contains(balanceLines, line => line.StartsWith("\"lineups\",\"draws\",", StringComparison.Ordinal));
        Assert.Contains(balanceLines, line => line.StartsWith(
            "\"assessment\",\"dominance_threshold\"",
            StringComparison.Ordinal));
        Assert.Contains(balanceLines, line => line.StartsWith("\"assessment\",\"result\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CliCanSaveAndRenderReplayWithoutRunningConfigurationAgain()
    {
        string configPath = Path.GetTempFileName();
        string replayPath = Path.GetTempFileName();
        try
        {
            const string json = """
                {
                  "teamA": [ { "creature": "amuletmaster" } ],
                  "teamB": [ { "creature": "viciousbrawler" } ],
                  "roundLimit": 10
                }
                """;
            await File.WriteAllTextAsync(configPath, json);
            using var battleOutput = new StringWriter();
            using var replayOutput = new StringWriter();
            using var error = new StringWriter();
            var application = new CliApplication(battleOutput, error);

            int battleExit = await application.RunAsync(
                ["battle", configPath, "--seed", "42", "--save-replay", replayPath, "--format", "json"],
                CancellationToken.None);
            File.Delete(configPath);
            int replayExit = await new CliApplication(replayOutput, error).RunAsync(
                ["replay", replayPath, "--format", "csv"],
                CancellationToken.None);

            Assert.Equal(0, battleExit);
            Assert.Equal(0, replayExit);
            Assert.StartsWith("sequence,round,actingTeam", replayOutput.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            File.Delete(configPath);
            File.Delete(replayPath);
        }
    }

    private static BattleDefinition CreateDefinition()
    {
        return new BattleDefinition(new BattleConfiguration
        {
            TeamA = [new BattleConfiguration.CreatureConfiguration { Creature = "amuletmaster" }],
            TeamB = [new BattleConfiguration.CreatureConfiguration { Creature = "viciousbrawler" }],
            RoundLimit = 20,
        });
    }

    private static ReplayDocument RunReplay(BattleDefinition definition, int seed)
    {
        (PlayerBoard teamA, PlayerBoard teamB) = definition.BuildBoards();
        BattleResult result = new CombatRunner(
            new SystemRandomNumberGenerator(seed),
            definition.RoundLimit).Run(teamA, teamB);
        return ReplayMapper.Create(definition, seed, result);
    }

    private static async Task AssertInvalidReplayAsync(ReplayDocument document)
    {
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(document, ReplayStore.SerializerOptions));
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new ReplayStore().LoadAsync(path, CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task AssertInvalidJsonAsync(string json)
    {
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, json);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new ReplayStore().LoadAsync(path, CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static GameSummary Summary(int index, BattleVerdict verdict, int rounds, int healthLoss)
    {
        return new GameSummary(
            index,
            index,
            verdict,
            BattleEndReason.TeamDefeated,
            rounds,
            healthLoss,
            healthLoss + 1,
            1,
            1);
    }

    private sealed class TestCreature : CombatSimulator.Core.Engine.Creature
    {
        public TestCreature(string name, int attack, int health)
            : base(name, new AttackPoint(attack), new HealthPoint(health))
        {
        }

        public override ICreature DeepCopy()
        {
            return new TestCreature(Name, Attack.Value, Health.Value);
        }
    }
}
