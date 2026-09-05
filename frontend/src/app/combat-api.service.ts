import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BattleCatalog, BattleRunRequest, ReplayDocument } from './combat.models';

@Injectable({ providedIn: 'root' })
export class CombatApiService {
  private readonly http = inject(HttpClient);

  getCatalog(): Observable<BattleCatalog> {
    return this.http.get<BattleCatalog>('/api/battles/catalog');
  }

  run(request: BattleRunRequest): Observable<ReplayDocument> {
    return this.http.post<ReplayDocument>('/api/battles/run', request);
  }
}

export function problemDetail(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { detail?: unknown; title?: unknown } | null;
    if (typeof body?.detail === 'string') return body.detail;
    if (typeof body?.title === 'string') return body.title;
    if (error.status === 0) return 'The battle service is unavailable.';
  }
  return 'The battle could not be simulated.';
}
