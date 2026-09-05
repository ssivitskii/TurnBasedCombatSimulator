using CombatSimulator.Cli.Configuration;

namespace CombatSimulator.Cli.Replay;

public sealed record ReplayDocument(
    int SchemaVersion,
    string SimulatorVersion,
    int Seed,
    BattleConfiguration Configuration,
    IReadOnlyList<ReplayParticipant> TeamA,
    IReadOnlyList<ReplayParticipant> TeamB,
    ReplayResult Result,
    IReadOnlyList<ReplayEvent> Events);
