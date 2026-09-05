using CombatSimulator.Application.Configuration;
using CombatSimulator.Application.Replay;
using CombatSimulator.Core.Boards;
using CombatSimulator.Core.Combat;
using CombatSimulator.Core.Randomness;

namespace CombatSimulator.Application;

public sealed class BattleSimulationService
{
    public ReplayDocument Run(BattleConfiguration configuration, int seed, CancellationToken cancellationToken)
    {
        var definition = new BattleDefinition(configuration);
        (PlayerBoard teamA, PlayerBoard teamB) = definition.BuildBoards();
        BattleResult result = new CombatRunner(
            new SystemRandomNumberGenerator(seed),
            definition.RoundLimit).Run(teamA, teamB, cancellationToken);
        return ReplayMapper.Create(definition, seed, result);
    }
}
