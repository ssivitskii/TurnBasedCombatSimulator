namespace CombatSimulator.Application.Configuration;

public static class BattleConfigurationValidator
{
    public const int MaximumModifiersPerCreature = 2;
    public const int MaximumTeamSize = 7;

    public static void Validate(BattleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ValidateTeam(configuration.TeamA, "A");
        ValidateTeam(configuration.TeamB, "B");
        if (configuration.RoundLimit < 1)
            throw new ArgumentException("Round limit must be positive.");
    }

    private static void ValidateTeam(
        IReadOnlyList<BattleConfiguration.CreatureConfiguration>? team,
        string name)
    {
        if (team is null || team.Count == 0)
            throw new ArgumentException($"Team {name} must contain at least one creature.");
        if (team.Count > MaximumTeamSize)
            throw new ArgumentException($"Team {name} can contain at most {MaximumTeamSize} creatures.");
        foreach (BattleConfiguration.CreatureConfiguration item in team)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Creature))
                throw new ArgumentException($"Every Team {name} entry must specify a creature.");
            if (item.Attack < 0)
                throw new ArgumentException("Attack override cannot be negative.");
            if (item.Health < 0)
                throw new ArgumentException("Health override cannot be negative.");
            if (item.Modifiers?.Any(string.IsNullOrWhiteSpace) == true)
                throw new ArgumentException("Modifiers cannot contain empty values.");
            if (item.Modifiers?.Count > MaximumModifiersPerCreature)
            {
                throw new ArgumentException(
                    $"A creature can contain at most {MaximumModifiersPerCreature} modifiers.");
            }

            if (item.Modifiers?.Distinct(StringComparer.OrdinalIgnoreCase).Count() != item.Modifiers?.Count)
                throw new ArgumentException("A creature cannot contain duplicate modifiers.");
        }
    }
}
