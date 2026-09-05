using CombatSimulator.Core.Combat;

namespace CombatSimulator.Cli.Replay;

public sealed record ReplayResult(BattleVerdict Verdict, BattleEndReason EndReason, int Rounds);
