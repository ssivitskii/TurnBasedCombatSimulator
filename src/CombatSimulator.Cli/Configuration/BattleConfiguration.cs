namespace CombatSimulator.Cli.Configuration;

public sealed class BattleConfiguration
{
    public IReadOnlyList<CreatureConfiguration>? TeamA { get; init; }

    public IReadOnlyList<CreatureConfiguration>? TeamB { get; init; }

    public int RoundLimit { get; init; } = 10000;

    public sealed class CreatureConfiguration
    {
        public string? Creature { get; init; }

        public int? Attack { get; init; }

        public int? Health { get; init; }

        public IReadOnlyList<string>? Modifiers { get; init; }
    }
}
