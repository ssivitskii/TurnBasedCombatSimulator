# Turn-Based Combat Simulator

A deterministic .NET battle engine with a command-line workflow and an interactive Angular replay console. Build two teams, run a seeded battle on the server, then step through every authoritative event in the browser.

## Features

- Runs a narrated battle, bounded-parallel tournament, recorded replay, or paired mirrored balance diagnostic.
- Loads teams, stat overrides, modifiers, and round limits from JSON.
- Records ordered events with stable team/slot combatant IDs and observable before/after attack and health snapshots.
- Saves versioned deterministic replay JSON containing configuration, participants, seed, events, and final result.
- Supports deterministic per-game seeds and scheduling-independent tournament aggregation.
- Reports win/draw counts, round distribution, observable net health loss caused by each acting team, and target defeats in text, JSON, or CSV.
- Distinguishes team defeat, true stalemate, and configured round-limit draws.
- Deep-copies complete runtime state before combat, including consumed shields and revivals.
- Provides a responsive team editor, presets, arena highlights, event scrubber, and timed playback.

## Tech Stack

C# · .NET 9 · ASP.NET Core · Angular 22 · TypeScript · xUnit · Vitest

## Architecture

`CombatSimulator.Core` owns all combat rules. `CombatSimulator.Application` validates shared configuration, builds fresh boards, runs seeded combat, and maps the versioned replay contract. `CombatSimulator.Cli` keeps file I/O, replay persistence, tournaments, and reports. `CombatSimulator.Api` exposes only in-memory battle requests and never accepts file paths or invokes the CLI. `frontend` replays server events without reimplementing combat.

## Project Structure

- `src/CombatSimulator.Core` — deterministic domain engine.
- `src/CombatSimulator.Application` — shared configuration and single-battle orchestration.
- `src/CombatSimulator.Cli` — file I/O, reporting, tournaments, and executable entry point.
- `src/CombatSimulator.Api` — bounded HTTP adapter and health endpoint.
- `frontend` — standalone strict Angular battle editor and replay UI.
- `tests/CombatSimulator.Tests` — behavior and deep-copy regression tests.
- `tests/CombatSimulator.ApiTests` — HTTP contract and application-boundary tests.
- `examples/battle.json` — runnable teams.

## Getting Started

Requires the .NET 9 SDK and Node.js with npm.

## Build

```bash
dotnet build TurnBasedCombatSimulator.slnx -c Release
```

## Run

### Interactive web app

Run the API and UI in two terminals:

```bash
ASPNETCORE_URLS=http://localhost:8080 dotnet run --project src/CombatSimulator.Api
```

```bash
cd frontend
npm install
npm start
```

Open `http://localhost:4200`. Angular proxies `/api` and `/health` to the API, so no broad CORS policy is enabled.

### CLI

```bash
dotnet run --project src/CombatSimulator.Cli -- battle examples/battle.json --seed 42
dotnet run --project src/CombatSimulator.Cli -- battle examples/battle.json --seed 42 --save-replay /tmp/combat-replay.json --format json
dotnet run --project src/CombatSimulator.Cli -- replay /tmp/combat-replay.json --format text
dotnet run --project src/CombatSimulator.Cli -- tournament examples/battle.json --games 1000 --seed 42 --parallelism 4 --format csv
dotnet run --project src/CombatSimulator.Cli -- balance examples/battle.json --games 500 --seed 42 --parallelism 4 --threshold 10
```

`battle`, `replay`, `tournament`, and `balance` accept `--format text|json|csv`. In balance mode, `--games N` means N mirrored pairs, so exactly `2*N` battles run. `--threshold` is an absolute configured-lineup win-rate difference in percentage points; it is a diagnostic threshold, not statistical significance.

## Tests

```bash
dotnet test TurnBasedCombatSimulator.slnx -c Release
cd frontend && npm test
cd frontend && npm run build
```

## HTTP API

- `POST /api/battles/run` accepts `{ "seed": 42, "configuration": { "roundLimit": 100, "teamA": [...], "teamB": [...] } }` and returns the deterministic replay document.
- `GET /api/battles/catalog` returns the supported creature and modifier names used by the editor.
- `GET /health/live` reports process liveness. Swagger UI is available in Development.

Requests are capped at 128 KiB. Each team must contain 1–7 fighters; overrides must be nonnegative; the API round limit is 1–1000; creatures and modifiers must come from the catalog. A fighter accepts at most two unique modifiers, compared case-insensitively. At most four simulations run concurrently and excess requests receive `429 application/problem+json` without queuing. Configuration failures use safe `400` Problem Details responses.

## Examples

The example demonstrates two-creature teams and both available decorators. Creature names are case-insensitive. Supported modifiers are `MagicShield` and `DoubleStrike`.

## Design Decisions

The RNG is an interface with a production adapter over `System.Random`, making individual battles reproducible without embedding test behavior in the domain. Combat always operates on deep copies, so configured boards can be reused concurrently. A turn produces an ordered event with one-based `(team, slot)` identity, which distinguishes duplicate creature names. The event reports net observed health change across the complete logical attack; it does not claim attempted damage, individual DoubleStrike hits, shield absorption, revival events, or other internal ability steps that the current interfaces cannot observe.

Stat-growth abilities use saturating integer arithmetic: attack or health that would exceed `Int32.MaxValue` remains at `Int32.MaxValue`. This keeps every accepted nonnegative API override inside the domain value-object invariant.

Replay schema version `1` stores no timestamp or machine metadata. The `replay` command validates and renders the recorded events directly; it never reruns the battle or RNG. A replay is immutable playback, not a resumable domain checkpoint.

Tournament indices use the documented `splitmix64-v1` derivation from the base seed. Each index owns fresh boards and RNG, parallel work writes to a fixed result slot, and aggregation occurs after completion in index order. Therefore output is independent of worker scheduling and configured parallelism. The round limit remains an explicit draw reason rather than an exceptional failure.

Balance mode runs every derived seed twice: original team orientation and swapped orientation. It reports configured-lineup outcomes separately from first/second-position wins. Outcome frequencies describe only the supplied configuration and deterministic seed set; they are not proof of rigorous balance or statistical significance.

## Limitations / Future Improvements

The simulator has a compact fixed catalog. Replay snapshots expose public attack/health state but cannot reconstruct hidden decorator internals for resuming a battle, so the UI visualizes observable events rather than individual shield, revival, or double-strike substeps. The web API intentionally exposes one battle only; tournaments remain CLI workflows. Tournament statistics are descriptive and do not provide ELO, confidence intervals, DPS, or low-level ability telemetry.
