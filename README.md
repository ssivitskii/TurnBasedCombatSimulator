# Turn-Based Combat Simulator

A deterministic command-line battle engine for configurable creature teams. Battles preserve the existing object-oriented creature abilities, spells, builders, factories, decorators, board rules, and injectable random-number strategy.

## Features

- Runs a narrated battle, bounded-parallel tournament, recorded replay, or paired mirrored balance diagnostic.
- Loads teams, stat overrides, modifiers, and round limits from JSON.
- Records ordered events with stable team/slot combatant IDs and observable before/after attack and health snapshots.
- Saves versioned deterministic replay JSON containing configuration, participants, seed, events, and final result.
- Supports deterministic per-game seeds and scheduling-independent tournament aggregation.
- Reports win/draw counts, round distribution, observable net health loss caused by each acting team, and target defeats in text, JSON, or CSV.
- Distinguishes team defeat, true stalemate, and configured round-limit draws.
- Deep-copies complete runtime state before combat, including consumed shields and revivals.

## Tech Stack

C# · .NET 9 · System.Text.Json · LINQ · xUnit

## Architecture

`CombatSimulator.Core` contains the battle domain, explicit ordered event model, creatures, boards, spells, Factory/Builder, Decorator modifiers, and RNG strategy. `CombatSimulator.Cli` maps JSON configuration into fresh boards, persists versioned replay documents, derives deterministic game seeds, coordinates bounded parallel work, aggregates reports in input order, and renders text/JSON/CSV. Parallelism is explicitly capped at 64 workers.

## Project Structure

- `src/CombatSimulator.Core` — deterministic domain engine.
- `src/CombatSimulator.Cli` — JSON configuration and executable entry point.
- `tests/CombatSimulator.Tests` — behavior and deep-copy regression tests.
- `examples/battle.json` — runnable teams.

## Getting Started

Requires the .NET 9 SDK.

## Build

```bash
dotnet build TurnBasedCombatSimulator.slnx -c Release
```

## Run

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
```

## Examples

The example demonstrates two-creature teams and both available decorators. Creature names are case-insensitive. Supported modifiers are `MagicShield` and `DoubleStrike`.

## Design Decisions

The RNG is an interface with a production adapter over `System.Random`, making individual battles reproducible without embedding test behavior in the domain. Combat always operates on deep copies, so configured boards can be reused concurrently. A turn produces an ordered event with one-based `(team, slot)` identity, which distinguishes duplicate creature names. The event reports net observed health change across the complete logical attack; it does not claim attempted damage, individual DoubleStrike hits, shield absorption, revival events, or other internal ability steps that the current interfaces cannot observe.

Replay schema version `1` stores no timestamp or machine metadata. The `replay` command validates and renders the recorded events directly; it never reruns the battle or RNG. A replay is immutable playback, not a resumable domain checkpoint.

Tournament indices use the documented `splitmix64-v1` derivation from the base seed. Each index owns fresh boards and RNG, parallel work writes to a fixed result slot, and aggregation occurs after completion in index order. Therefore output is independent of worker scheduling and configured parallelism. The round limit remains an explicit draw reason rather than an exceptional failure.

Balance mode runs every derived seed twice: original team orientation and swapped orientation. It reports configured-lineup outcomes separately from first/second-position wins. Outcome frequencies describe only the supplied configuration and deterministic seed set; they are not proof of rigorous balance or statistical significance.

## Limitations / Future Improvements

The simulator has a compact fixed catalog and no interactive editor. Replay snapshots expose public attack/health state but cannot reconstruct hidden decorator internals for resuming a battle. Tournament statistics are descriptive and do not provide ELO, confidence intervals, DPS, or low-level ability telemetry.
