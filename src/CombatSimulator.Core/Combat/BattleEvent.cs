namespace CombatSimulator.Core.Combat;

public abstract record BattleEvent(int Sequence, int Round, BattleTeam ActingTeam);
