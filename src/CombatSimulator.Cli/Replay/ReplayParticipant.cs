namespace CombatSimulator.Cli.Replay;

public sealed record ReplayParticipant(
    int Slot,
    string Creature,
    int? ConfiguredAttack,
    int? ConfiguredHealth,
    IReadOnlyList<string> Modifiers);
