using CombatSimulator.Application.Configuration;

namespace CombatSimulator.Api;

public sealed class BattleRunRequest
{
    public int Seed { get; init; }

    public BattleConfiguration? Configuration { get; init; }
}
