import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CombatApiService } from './combat-api.service';

describe('CombatApiService', () => {
  it('posts the explicit seed and configuration to the run endpoint', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const service = TestBed.inject(CombatApiService);
    const http = TestBed.inject(HttpTestingController);
    const request = {
      seed: 73,
      configuration: {
        roundLimit: 25,
        teamA: [{ creature: 'MimicChest', modifiers: [] }],
        teamB: [{ creature: 'AmuletMaster', modifiers: ['MagicShield'] }],
      },
    };

    service.run(request).subscribe();
    const pending = http.expectOne('/api/battles/run');
    expect(pending.request.method).toBe('POST');
    expect(pending.request.body).toEqual(request);
    pending.flush({});
    http.verify();
  });
});
