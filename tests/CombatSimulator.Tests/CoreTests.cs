using CombatSimulator.Cli.Configuration;
using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Boards;
using CombatSimulator.Core.Catalog;
using CombatSimulator.Core.Combat;
using CombatSimulator.Core.Creatures;
using CombatSimulator.Core.Decorators;
using CombatSimulator.Core.Modifiers;
using CombatSimulator.Core.Randomness;

namespace CombatSimulator.Tests;

public sealed class CoreTests
{
    [Fact]
    public void BuilderAppliesStatsAndModifiers()
    {
        ICreature creature = new CreatureBuilder(new CreatureFactory(() => new AmuletMaster()))
            .WithAttack(new AttackPoint(9))
            .WithHealth(new HealthPoint(8))
            .AddModifier(new MagicShieldModifier())
            .Create();

        creature.ReceiveDamage(3, new SystemRandomNumberGenerator(1));

        Assert.Equal(9, creature.Attack.Value);
        Assert.Equal(8, creature.Health.Value);
        Assert.IsType<MagicShield>(creature);
    }

    [Fact]
    public void DefaultCatalogCreatesKnownCreature()
    {
        ICreature creature = DefaultCatalogFactory.Create().GetBuilder("deathlesshorror").Create();

        Assert.IsType<DeathlessHorror>(creature);
    }

    [Fact]
    public void DoubleStrikeAttacksTwiceWhileTargetLives()
    {
        var attacker = new DoubleStrike(new AmuletMaster());
        var target = new ViciousBrawler();

        attacker.AttackTarget(target, new SystemRandomNumberGenerator(1));

        Assert.False(target.IsAlive);
    }

    [Fact]
    public void BoardRejectsEighthCreature()
    {
        var board = new PlayerBoard();
        for (int index = 0; index < PlayerBoard.Capacity; index++)
            board.Add(new AmuletMaster());

        Assert.Throws<InvalidOperationException>(() => board.Add(new AmuletMaster()));
    }

    [Fact]
    public void CombatReportsStalemateWhenNeitherTeamCanAttack()
    {
        var first = new PlayerBoard();
        var second = new PlayerBoard();
        first.Add(new TestCreature(0, 5));
        second.Add(new TestCreature(0, 5));

        BattleResult result = new CombatRunner(new SystemRandomNumberGenerator(1)).Run(first, second);

        Assert.Equal(BattleVerdict.Draw, result.Verdict);
        Assert.Equal(BattleEndReason.Stalemate, result.EndReason);
    }

    [Fact]
    public void CombatReportsRoundLimitInsteadOfGenericFailure()
    {
        var first = new PlayerBoard();
        var second = new PlayerBoard();
        first.Add(new TestCreature(1, 100));
        second.Add(new TestCreature(1, 100));

        BattleResult result = new CombatRunner(new SystemRandomNumberGenerator(1), 1).Run(first, second);

        Assert.Equal(BattleEndReason.RoundLimitReached, result.EndReason);
        Assert.Equal(1, result.Rounds);
        AttackResolvedEvent[] attacks = result.Events.OfType<AttackResolvedEvent>().ToArray();
        Assert.Collection(
            attacks,
            action => Assert.Equal(BattleTeam.TeamA, action.ActingTeam),
            action => Assert.Equal(BattleTeam.TeamB, action.ActingTeam));
    }

    [Fact]
    public void SeededRandomProducesRepeatableSequence()
    {
        var first = new SystemRandomNumberGenerator(42);
        var second = new SystemRandomNumberGenerator(42);

        int[] firstValues = Enumerable.Range(0, 5).Select(_ => first.NextInt(0, 100)).ToArray();
        int[] secondValues = Enumerable.Range(0, 5).Select(_ => second.NextInt(0, 100)).ToArray();

        Assert.Equal(firstValues, secondValues);
    }

    [Fact]
    public async Task JsonConfigurationProducesRepeatableSeededBattle()
    {
        string path = Path.GetTempFileName();
        try
        {
            const string json = """
                {
                  "teamA": [ { "creature": "amuletmaster" } ],
                  "teamB": [ { "creature": "viciousbrawler" } ],
                  "roundLimit": 10
                }
                """;
            await File.WriteAllTextAsync(path, json);
            BattleDefinition definition = await new BattleConfigurationLoader().LoadAsync(path, CancellationToken.None);
            (PlayerBoard firstA, PlayerBoard firstB) = definition.BuildBoards();
            (PlayerBoard secondA, PlayerBoard secondB) = definition.BuildBoards();

            BattleResult first = new CombatRunner(
                new SystemRandomNumberGenerator(42),
                definition.RoundLimit).Run(firstA, firstB);
            BattleResult second = new CombatRunner(
                new SystemRandomNumberGenerator(42),
                definition.RoundLimit).Run(secondA, secondB);

            Assert.Equal(first.Verdict, second.Verdict);
            Assert.Equal(first.EndReason, second.EndReason);
            Assert.Equal(first.Events, second.Events);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class TestCreature : CombatSimulator.Core.Engine.Creature
    {
        public TestCreature(int attack, int health)
            : base("TestCreature", new AttackPoint(attack), new HealthPoint(health))
        {
        }

        public override ICreature DeepCopy()
        {
            return new TestCreature(Attack.Value, Health.Value);
        }
    }
}
