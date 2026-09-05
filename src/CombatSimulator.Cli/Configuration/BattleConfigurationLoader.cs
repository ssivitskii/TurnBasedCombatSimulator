using System.Text.Json;

namespace CombatSimulator.Cli.Configuration;

public sealed class BattleConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<BattleDefinition> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        BattleConfiguration? configuration = await JsonSerializer.DeserializeAsync<BattleConfiguration>(
            stream,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);
        if (configuration is null)
            throw new ArgumentException("Battle configuration is empty.");
        Validate(configuration);
        return new BattleDefinition(configuration);
    }

    private static void Validate(BattleConfiguration configuration)
    {
        if (configuration.TeamA is null || configuration.TeamA.Count == 0)
            throw new ArgumentException("Team A must contain at least one creature.");
        if (configuration.TeamB is null || configuration.TeamB.Count == 0)
            throw new ArgumentException("Team B must contain at least one creature.");
        if (configuration.TeamA.Count > 7 || configuration.TeamB.Count > 7)
            throw new ArgumentException("A team can contain at most seven creatures.");
        if (configuration.RoundLimit <= 0)
            throw new ArgumentException("Round limit must be positive.");
        if (configuration.TeamA.Concat(configuration.TeamB).Any(item => string.IsNullOrWhiteSpace(item.Creature)))
            throw new ArgumentException("Every team entry must specify a creature.");
    }
}
