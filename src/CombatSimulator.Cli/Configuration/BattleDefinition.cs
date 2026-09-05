using CombatSimulator.Core.Abstractions;
using CombatSimulator.Core.Boards;
using CombatSimulator.Core.Catalog;
using CombatSimulator.Core.Modifiers;

namespace CombatSimulator.Cli.Configuration;

public sealed class BattleDefinition
{
    private readonly BattleConfiguration _configuration;

    public BattleDefinition(BattleConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int RoundLimit => _configuration.RoundLimit;

    public IReadOnlyList<string> TeamANames => GetNames(_configuration.TeamA);

    public IReadOnlyList<string> TeamBNames => GetNames(_configuration.TeamB);

    public BattleConfiguration Configuration => CloneConfiguration(_configuration);

    public (PlayerBoard TeamA, PlayerBoard TeamB) BuildBoards(bool swapTeams = false)
    {
        CreatureCatalog catalog = DefaultCatalogFactory.Create();
        return swapTeams
            ? (Build(_configuration.TeamB, catalog), Build(_configuration.TeamA, catalog))
            : (Build(_configuration.TeamA, catalog), Build(_configuration.TeamB, catalog));
    }

    private static BattleConfiguration CloneConfiguration(BattleConfiguration configuration)
    {
        return new BattleConfiguration
        {
            RoundLimit = configuration.RoundLimit,
            TeamA = CloneTeam(configuration.TeamA),
            TeamB = CloneTeam(configuration.TeamB),
        };
    }

    private static BattleConfiguration.CreatureConfiguration[] CloneTeam(
        IReadOnlyList<BattleConfiguration.CreatureConfiguration>? team)
    {
        return team?.Select(item => new BattleConfiguration.CreatureConfiguration
        {
            Creature = item.Creature,
            Attack = item.Attack,
            Health = item.Health,
            Modifiers = item.Modifiers?.ToArray(),
        }).ToArray() ?? [];
    }

    private static PlayerBoard Build(
        IReadOnlyList<BattleConfiguration.CreatureConfiguration>? configurations,
        CreatureCatalog catalog)
    {
        var board = new PlayerBoard();
        foreach (BattleConfiguration.CreatureConfiguration configuration in configurations ?? [])
        {
            CreatureBuilder builder = catalog.GetBuilder(configuration.Creature ?? string.Empty);
            if (configuration.Attack is int attack)
                builder = builder.WithAttack(new AttackPoint(attack));
            if (configuration.Health is int health)
                builder = builder.WithHealth(new HealthPoint(health));
            foreach (string modifier in configuration.Modifiers ?? [])
                builder = builder.AddModifier(ParseModifier(modifier));
            board.Add(builder.Create());
        }

        return board;
    }

    private static ICreatureModifier ParseModifier(string modifier)
    {
        return modifier.ToLowerInvariant() switch
        {
            "magicshield" => new MagicShieldModifier(),
            "doublestrike" => new DoubleStrikeModifier(),
            _ => throw new ArgumentException($"Unknown modifier '{modifier}'."),
        };
    }

    private static string[] GetNames(IReadOnlyList<BattleConfiguration.CreatureConfiguration>? team)
    {
        return team?.Select(configuration => configuration.Creature ?? "<missing>").ToArray() ?? [];
    }
}
