import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Observable, Subject, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from './app';
import { CombatApiService } from './combat-api.service';
import { BattleCatalog, BattleRunRequest, ReplayDocument } from './combat.models';

class ApiStub {
  readonly responses: Subject<ReplayDocument>[] = [];
  getCatalog(): Observable<BattleCatalog> {
    return of({
      creatures: ['AmuletMaster', 'MimicChest'],
      modifiers: ['MagicShield', 'DoubleStrike'],
    });
  }
  run(_request: BattleRunRequest): Observable<ReplayDocument> {
    const response = new Subject<ReplayDocument>();
    this.responses.push(response);
    return response;
  }
}

const replay = (seed: number): ReplayDocument => ({
  seed,
  teamA: [{ slot: 1, creature: 'AmuletMaster', modifiers: [] }],
  teamB: [{ slot: 1, creature: 'MimicChest', modifiers: [] }],
  result: { verdict: 'teamAVictory', endReason: 'teamDefeated', rounds: 1 },
  events: [
    {
      type: 'attackResolved',
      sequence: 1,
      round: 1,
      actingTeam: 'teamA',
      attackerId: { team: 'teamA', slot: 1 },
      attackerName: 'AmuletMaster',
      attackerBefore: { attack: 8, health: 12, isAlive: true },
      attackerAfter: { attack: 8, health: 12, isAlive: true },
      targetId: { team: 'teamB', slot: 1 },
      targetName: 'MimicChest',
      targetBefore: { attack: 4, health: 8, isAlive: true },
      targetAfter: { attack: 4, health: 0, isAlive: false },
      netHealthLoss: 8,
      targetDefeated: true,
    },
  ],
});

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let component: App;
  let api: ApiStub;

  beforeEach(async () => {
    api = new ApiStub();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: CombatApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(App);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => vi.useRealTimers());

  it('edits teams and emits the server request shape', () => {
    component.addFighter(component.teamA);
    component.teamA.at(1).patchValue({ creature: 'MimicChest', attack: 11, doubleStrike: true });
    component.removeFighter(component.teamA, 0);

    const body = component.requestBody();
    expect(body.seed).toBe(42);
    expect(body.configuration.teamA).toEqual([
      { creature: 'MimicChest', attack: 11, modifiers: ['DoubleStrike'] },
    ]);
  });

  it('renders a server result and its event ledger', async () => {
    expect(component.form.valid).toBe(true);
    component.run();
    expect(component.state).toBe('loading');
    api.responses[0].next(replay(42));
    api.responses[0].complete();
    expect(component.replay?.seed).toBe(42);
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('teamAVictory');
    expect(fixture.nativeElement.textContent).toContain('8 net damage');
    expect(fixture.debugElement.queryAll(By.css('.timeline li'))).toHaveLength(1);
  });

  it('ignores an obsolete response after a newer run', () => {
    component.run();
    component.run();
    api.responses[0].next(replay(1));
    api.responses[1].next(replay(2));

    expect(component.replay?.seed).toBe(2);
  });

  it('invalidates an active request when a preset is loaded', () => {
    component.run();
    component.loadPreset('squads');
    api.responses[0].next(replay(999));

    expect(component.state).toBe('idle');
    expect(component.replay).toBeUndefined();
    expect(component.form.controls.seed.value).toBe(731);
  });

  it('shows cumulative authoritative state through the selected event', () => {
    component.replay = multiEventReplay();
    component.eventIndex = 1;
    const firstFighter = component.replay.teamA[0];

    expect(component.snapshot(firstFighter, 'teamA')).toContain('ATK 8 · HP 7');
    component.eventIndex = -1;
    expect(component.snapshot(firstFighter, 'teamA')).toContain('ATK 8 · HP 12');
  });

  it('keeps playback within bounds and clears its timer on destroy', () => {
    vi.useFakeTimers();
    component.replay = replay(42);
    component.eventIndex = -1;
    component.togglePlayback();
    vi.advanceTimersByTime(1600);
    expect(component.eventIndex).toBe(0);
    expect(component.playing).toBe(false);
    component.ngOnDestroy();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('shows a validation error without sending a request', () => {
    component.form.controls.roundLimit.setValue(1001);
    component.run();

    expect(component.state).toBe('error');
    expect(component.errorMessage).toContain('highlighted');
    expect(api.responses).toHaveLength(0);
  });
});

function multiEventReplay(): ReplayDocument {
  const document = replay(42);
  return {
    ...document,
    teamA: [...document.teamA, { slot: 2, creature: 'MimicChest', modifiers: [] }],
    events: [
      {
        type: 'attackResolved',
        sequence: 1,
        round: 1,
        actingTeam: 'teamB',
        attackerId: { team: 'teamB', slot: 1 },
        attackerName: 'MimicChest',
        attackerBefore: { attack: 5, health: 8, isAlive: true },
        attackerAfter: { attack: 5, health: 8, isAlive: true },
        targetId: { team: 'teamA', slot: 1 },
        targetName: 'AmuletMaster',
        targetBefore: { attack: 8, health: 12, isAlive: true },
        targetAfter: { attack: 8, health: 7, isAlive: true },
        netHealthLoss: 5,
        targetDefeated: false,
      },
      {
        type: 'attackResolved',
        sequence: 2,
        round: 2,
        actingTeam: 'teamA',
        attackerId: { team: 'teamA', slot: 2 },
        attackerName: 'MimicChest',
        attackerBefore: { attack: 4, health: 8, isAlive: true },
        attackerAfter: { attack: 4, health: 8, isAlive: true },
        targetId: { team: 'teamB', slot: 1 },
        targetName: 'MimicChest',
        targetBefore: { attack: 5, health: 8, isAlive: true },
        targetAfter: { attack: 5, health: 4, isAlive: true },
        netHealthLoss: 4,
        targetDefeated: false,
      },
    ],
  };
}
