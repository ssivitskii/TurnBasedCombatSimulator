using CombatSimulator.Application.Configuration;

namespace CombatSimulator.Application.Replay;

public sealed record ReplayDocument(
    int SchemaVersion,
    string SimulatorVersion,
    int Seed,
    BattleConfiguration Configuration,
    IReadOnlyList<ReplayParticipant> TeamA,
    IReadOnlyList<ReplayParticipant> TeamB,
    ReplayResult Result,
    IReadOnlyList<ReplayEvent> Events);
