using CombatSimulator.Core.Combat;

namespace CombatSimulator.Application.Replay;

public sealed record ReplayResult(BattleVerdict Verdict, BattleEndReason EndReason, int Rounds);
