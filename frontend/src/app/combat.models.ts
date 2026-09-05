export interface FighterConfiguration {
  creature: string;
  attack?: number;
  health?: number;
  modifiers: string[];
}

export interface BattleRunRequest {
  seed: number;
  configuration: {
    teamA: FighterConfiguration[];
    teamB: FighterConfiguration[];
    roundLimit: number;
  };
}

export interface CombatantId {
  team: 'teamA' | 'teamB';
  slot: number;
}

export interface CombatantSnapshot {
  attack: number;
  health: number;
  isAlive: boolean;
}

export interface ReplayEvent {
  type: 'attackResolved' | 'turnSkipped';
  sequence: number;
  round: number;
  actingTeam: 'teamA' | 'teamB';
  attackerId?: CombatantId;
  attackerName?: string;
  attackerBefore?: CombatantSnapshot;
  attackerAfter?: CombatantSnapshot;
  targetId?: CombatantId;
  targetName?: string;
  targetBefore?: CombatantSnapshot;
  targetAfter?: CombatantSnapshot;
  netHealthLoss?: number;
  targetDefeated?: boolean;
  skipReason?: string;
}

export interface ReplayDocument {
  seed: number;
  teamA: ReplayParticipant[];
  teamB: ReplayParticipant[];
  result: { verdict: string; endReason: string; rounds: number };
  events: ReplayEvent[];
}

export interface ReplayParticipant {
  slot: number;
  creature: string;
  configuredAttack?: number;
  configuredHealth?: number;
  modifiers: string[];
}

export interface BattleCatalog {
  creatures: string[];
  modifiers: string[];
}
