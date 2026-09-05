import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Subscription, finalize } from 'rxjs';
import { CombatApiService, problemDetail } from './combat-api.service';
import {
  BattleCatalog,
  BattleRunRequest,
  CombatantId,
  CombatantSnapshot,
  FighterConfiguration,
  ReplayDocument,
  ReplayEvent,
  ReplayParticipant,
} from './combat.models';

type FighterForm = FormGroup<{
  creature: FormControl<string>;
  attack: FormControl<number | null>;
  health: FormControl<number | null>;
  magicShield: FormControl<boolean>;
  doubleStrike: FormControl<boolean>;
}>;

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit, OnDestroy {
  private readonly api = inject(CombatApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private request?: Subscription;
  private catalogRequest?: Subscription;
  private timer?: ReturnType<typeof setInterval>;
  private generation = 0;

  readonly form = new FormGroup({
    seed: new FormControl(42, { nonNullable: true, validators: [Validators.required] }),
    roundLimit: new FormControl(100, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(1000)],
    }),
    teamA: new FormArray<FighterForm>([]),
    teamB: new FormArray<FighterForm>([]),
  });

  catalog: BattleCatalog = {
    creatures: ['AmuletMaster', 'BattleAnalyst', 'DeathlessHorror', 'MimicChest', 'ViciousBrawler'],
    modifiers: ['MagicShield', 'DoubleStrike'],
  };
  state: 'idle' | 'loading' | 'success' | 'error' = 'idle';
  errorMessage = '';
  replay?: ReplayDocument;
  eventIndex = -1;
  playing = false;

  get teamA(): FormArray<FighterForm> {
    return this.form.controls.teamA;
  }

  get teamB(): FormArray<FighterForm> {
    return this.form.controls.teamB;
  }

  get currentEvent(): ReplayEvent | undefined {
    return this.replay?.events[this.eventIndex];
  }

  ngOnInit(): void {
    this.loadPreset('duel');
    this.catalogRequest = this.api.getCatalog().subscribe({
      next: (catalog) => {
        this.catalog = catalog;
        this.changeDetector.markForCheck();
      },
    });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.catalogRequest?.unsubscribe();
    this.stopPlayback();
  }

  fighter(value?: Partial<FighterConfiguration>): FighterForm {
    return new FormGroup({
      creature: new FormControl(value?.creature ?? this.catalog.creatures[0], {
        nonNullable: true,
        validators: Validators.required,
      }),
      attack: new FormControl(value?.attack ?? null, [Validators.min(0)]),
      health: new FormControl(value?.health ?? null, [Validators.min(0)]),
      magicShield: new FormControl(value?.modifiers?.includes('MagicShield') ?? false, {
        nonNullable: true,
      }),
      doubleStrike: new FormControl(value?.modifiers?.includes('DoubleStrike') ?? false, {
        nonNullable: true,
      }),
    });
  }

  addFighter(team: FormArray<FighterForm>): void {
    if (team.length < 7) team.push(this.fighter());
  }

  removeFighter(team: FormArray<FighterForm>, index: number): void {
    if (team.length > 1) team.removeAt(index);
  }

  moveFighter(team: FormArray<FighterForm>, index: number, delta: number): void {
    const destination = index + delta;
    if (destination < 0 || destination >= team.length) return;
    const item = team.at(index);
    team.removeAt(index);
    team.insert(destination, item);
  }

  loadPreset(name: 'duel' | 'squads'): void {
    this.generation++;
    this.request?.unsubscribe();
    this.request = undefined;
    this.stopPlayback();
    this.teamA.clear();
    this.teamB.clear();
    const preset =
      name === 'duel'
        ? {
            seed: 42,
            limit: 100,
            a: [{ creature: 'AmuletMaster', modifiers: ['MagicShield'] }],
            b: [{ creature: 'ViciousBrawler', modifiers: ['DoubleStrike'] }],
          }
        : {
            seed: 731,
            limit: 250,
            a: [
              { creature: 'BattleAnalyst', modifiers: [] },
              { creature: 'DeathlessHorror', modifiers: ['MagicShield'] },
              { creature: 'MimicChest', modifiers: [] },
            ],
            b: [
              { creature: 'ViciousBrawler', modifiers: ['DoubleStrike'] },
              { creature: 'AmuletMaster', modifiers: [] },
              { creature: 'MimicChest', modifiers: ['MagicShield'] },
            ],
          };
    this.form.controls.seed.setValue(preset.seed);
    this.form.controls.roundLimit.setValue(preset.limit);
    preset.a.forEach((fighter) => this.teamA.push(this.fighter(fighter)));
    preset.b.forEach((fighter) => this.teamB.push(this.fighter(fighter)));
    this.replay = undefined;
    this.eventIndex = -1;
    this.state = 'idle';
  }

  run(): void {
    if (this.form.invalid || this.teamA.length === 0 || this.teamB.length === 0) {
      this.form.markAllAsTouched();
      this.state = 'error';
      this.errorMessage = 'Fix the highlighted fields before entering the arena.';
      return;
    }
    this.stopPlayback();
    this.request?.unsubscribe();
    const generation = ++this.generation;
    this.state = 'loading';
    this.errorMessage = '';
    this.request = this.api
      .run(this.requestBody())
      .pipe(finalize(() => generation === this.generation && (this.request = undefined)))
      .subscribe({
        next: (replay) => {
          if (generation !== this.generation) return;
          this.replay = replay;
          this.eventIndex = replay.events.length > 0 ? 0 : -1;
          this.state = 'success';
          this.changeDetector.markForCheck();
        },
        error: (error: unknown) => {
          if (generation !== this.generation) return;
          this.state = 'error';
          this.errorMessage = problemDetail(error);
          this.changeDetector.markForCheck();
        },
      });
  }

  requestBody(): BattleRunRequest {
    return {
      seed: this.form.controls.seed.value,
      configuration: {
        roundLimit: this.form.controls.roundLimit.value,
        teamA: this.teamA.controls.map((control) => this.toFighter(control)),
        teamB: this.teamB.controls.map((control) => this.toFighter(control)),
      },
    };
  }

  step(delta: number): void {
    this.stopPlayback();
    if (!this.replay) return;
    this.eventIndex = Math.max(
      -1,
      Math.min(this.eventIndex + delta, this.replay.events.length - 1),
    );
  }

  scrub(value: string): void {
    this.stopPlayback();
    this.eventIndex = Number(value);
  }

  togglePlayback(): void {
    if (!this.replay?.events.length) return;
    if (this.playing) {
      this.stopPlayback();
      return;
    }
    if (this.eventIndex >= this.replay.events.length - 1) this.eventIndex = -1;
    this.playing = true;
    this.timer = setInterval(() => {
      if (!this.replay || this.eventIndex >= this.replay.events.length - 1) {
        this.stopPlayback();
      } else {
        this.eventIndex++;
        this.changeDetector.markForCheck();
      }
    }, 750);
  }

  resetPlayback(): void {
    this.stopPlayback();
    this.eventIndex = -1;
  }

  active(participant: ReplayParticipant, team: 'teamA' | 'teamB'): boolean {
    const event = this.currentEvent;
    return (
      (event?.attackerId?.team === team && event.attackerId.slot === participant.slot) ||
      (event?.targetId?.team === team && event.targetId.slot === participant.slot)
    );
  }

  snapshot(participant: ReplayParticipant, team: 'teamA' | 'teamB'): string {
    const event = this.currentEvent;
    const isAttacker =
      event?.attackerId?.team === team && event.attackerId.slot === participant.slot;
    const isTarget = event?.targetId?.team === team && event.targetId.slot === participant.slot;
    const before = isAttacker ? event?.attackerBefore : isTarget ? event?.targetBefore : undefined;
    const after = isAttacker ? event?.attackerAfter : isTarget ? event?.targetAfter : undefined;
    if (before && after)
      return `ATK ${before.attack} → ${after.attack} · HP ${before.health} → ${after.health}`;
    const latest = this.lastKnownSnapshot({ team, slot: participant.slot });
    if (latest) return `ATK ${latest.attack} · HP ${latest.health}`;
    return `ATK ${participant.configuredAttack ?? 'base'} · HP ${participant.configuredHealth ?? 'base'}`;
  }

  trackControl(_index: number, control: AbstractControl): AbstractControl {
    return control;
  }

  private toFighter(control: FighterForm): FighterConfiguration {
    const value = control.getRawValue();
    return {
      creature: value.creature,
      ...(value.attack === null ? {} : { attack: value.attack }),
      ...(value.health === null ? {} : { health: value.health }),
      modifiers: [
        ...(value.magicShield ? ['MagicShield'] : []),
        ...(value.doubleStrike ? ['DoubleStrike'] : []),
      ],
    };
  }

  private lastKnownSnapshot(id: CombatantId): CombatantSnapshot | undefined {
    if (!this.replay) return undefined;
    for (
      let index = Math.min(this.eventIndex, this.replay.events.length - 1);
      index >= 0;
      index--
    ) {
      const event = this.replay.events[index];
      if (event.attackerId?.team === id.team && event.attackerId.slot === id.slot)
        return event.attackerAfter;
      if (event.targetId?.team === id.team && event.targetId.slot === id.slot)
        return event.targetAfter;
    }

    for (const event of this.replay.events) {
      if (event.attackerId?.team === id.team && event.attackerId.slot === id.slot)
        return event.attackerBefore;
      if (event.targetId?.team === id.team && event.targetId.slot === id.slot)
        return event.targetBefore;
    }

    return undefined;
  }

  private stopPlayback(): void {
    if (this.timer) clearInterval(this.timer);
    this.timer = undefined;
    this.playing = false;
    this.changeDetector.markForCheck();
  }
}
