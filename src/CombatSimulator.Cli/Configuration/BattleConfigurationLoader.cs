using CombatSimulator.Application.Configuration;
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
        return new BattleDefinition(configuration ?? throw new ArgumentException("Battle configuration is empty."));
    }
}
