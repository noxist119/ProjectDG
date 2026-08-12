# Pass 2A Validation Report

- Tested commit: `NOT EXECUTED` - Pass 2A.1 adds validation instrumentation only.
- Unity validation: `NOT EXECUTED` - attempted in headless Unity on 2026-08-12, but Unity stopped before Play Mode because no license token was available.
- Classic R30: `NOT EXECUTED` (planned: 12 paired-seed runs)
- Overdrive R30: `NOT EXECUTED` (planned: 12 paired-seed runs)
- Classic R50: `NOT EXECUTED` (planned: 6 paired-seed runs)

## Required report fields after execution

The generated files under `BatchPlaytestResults/` are intentionally gitignored. Copy the measured values here only after running the editor menu entries:

- Target reach rate
- Gameplay defeats
- Technical failures
- Runtime errors
- Soft locks
- Boss attempts / real boss clears / boss failures
- Average reached round
- Average end life
- Average end gold
- Per-strategy summary
- Mission choice / completion totals
- Grade upgrade purchase total
- Ultimate recipe merge total
- Balance observations

## Validation interpretation

A run is exactly one of:

- **victory** - target round reached, life remains, no technical failure.
- **defeat** - life is zero from normal gameplay, without a technical failure.
- **technical failure** - runtime error, exception/assert, invariant failure, soft lock, timeout, or an otherwise nonterminal harness exit.

Boss clear rate uses actual `RunBossKillCount` increases, not merely a boss round ending. A boss failure records the last observed boss HP ratio when available.

## Current balance observations

`NOT EXECUTED` - no R30/R50 result JSON was produced, so no balance conclusion is recorded.