# Pass 2I - R10 Timeout Diagnosis and Batch Time Semantics

- Exact baseline SHA: `7162fb4b9cf873379b5fc7f1c20f8959da7fde1f` (`Pass 2H - Overdrive Post-R10 Progression Calibration`).
- Scope: timeout telemetry and batch-harness diagnostics only.
- Gameplay balance change: **NONE**.
- Frozen values retained: Overdrive horde count `1.20`, first R10 support count `8`, boss HP multiplier `0.72`, boss ATK multiplier `0.70`.

## Measurement change

The batch runner previously used only `EditorApplication.timeSinceStartup` for the 45-second round timeout. It now records both wall-clock and simulation time at every round start:

- `roundStartEditorTime` -> `wallClockDurationSeconds`
- `roundStartGameTime` (`Time.timeAsDouble`) -> `gameTimeDurationSeconds`

Post-R10 telemetry exposes wall/game duration and first leak in both clocks. Timeout snapshots use the same first-leak fields for every timed-out round. Legacy `roundDurationSeconds` and `firstLeakSeconds` remain wall-clock compatibility fields.

A timeout snapshot is read-only and contains mode, seed, strategy, round, Life/Gold, target/spawned/killed/escaped/resolved/active/peak monster counts, active boss HP/position deltas, targeting/status flags, defender count, and a classification.

Classification order:

1. `editor_or_batch_timing_issue` - wall time advances while game time barely advances.
2. `spawn_or_resolution_mismatch` - impossible spawned/resolved/active accounting.
3. `simulation_still_progressing` - recent boss HP, position, or resolve state changed.
4. `combat_stalemate` - a boss exists without recent progression.
5. `unknown`.

## Requested timeout cases

The runner has explicit diagnostic menus for the requested cases; they do not use the normal paired-seed strategy rotation.

| Seed | Strategy |
|---:|---|
| 90210 | balanced |
| 90210 | shop-save |
| 98129 | balanced |

Outputs:

- `BatchPlaytestResults/DefenseGame_Phase2I_Overdrive_R10_TimeoutDiagnostic_40x.json`
- `BatchPlaytestResults/DefenseGame_Phase2I_Overdrive_R10_TimeoutDiagnostic_10x.json`

## Measured comparison

| Speed | 90210 balanced | 90210 shop-save | 98129 balanced | R10 clears | Timeouts |
|---:|---|---|---|---:|---:|
| 40x | clear (boss 2.34 wall s) | clear (2.03 wall s) | clear (22.92 wall s) | 3/3 | 0/3 |
| 10x | clear (9.27 wall s) | clear (7.45 wall s) | timeout | 2/3 | 1/3 |

The only observed timeout was 10x, `98129 / balanced`:

| Field | Measured value |
|---|---:|
| Wall-clock elapsed | 45.04 s |
| Game-time elapsed | 450.36 s |
| Classification | `simulation_still_progressing` |
| Life / Gold | 10 / 71 |
| Target / spawned / killed / escaped / resolved / active | 9 / 9 / 8 / 0 / 8 / 1 |
| Peak active | 9 |
| Boss HP | 0.97 |
| Recent boss HP delta | 0.06 |
| Recent position delta | 0.00 |
| Observation window wall/game | 0.15 s / 1.52 s |
| Targetable / status immune / stunned / petrified | true / true / false / false |
| Defender count | 8 |

## Facts

- The timeout is not an editor/batch time freeze: game time advanced 450.36 seconds while wall-clock advanced 45.04 seconds.
- No spawned/resolved mismatch was measured: `8 resolved + 1 active = 9 spawned`.
- The boss was targetable and lost 0.06 HP in the final 0.15 wall-clock observation window.
- No runtime errors, softlocks, or invariant failures were reported in either diagnostic batch.
- The same requested seed/strategy is not an exact outcome reproduction across speed runs: it timed out at 10x but cleared at 40x. The collected data therefore shows batch/frame-order variance, not a reproducible 40x-only stall.

## Interpretation

The one captured event is a progressing R10 encounter cut off by the fixed 45-second wall-clock guard, not a confirmed combat stalemate or resolution bug. This pass does not increase the timeout and does not alter combat, boss, economy, or Overdrive balance.

The diagnostic runner itself had a command-line entry issue during the first invocation: direct `-executeMethod` returned after scheduling the asynchronous playtest. The Pass 2I batch methods now queue the existing diagnostic start with `EditorApplication.delayCall`, allowing the process to remain alive until the existing `FinishAll` path writes its JSON. This is a harness launch correction only.

Because the evidence is insufficient to identify a deterministic runtime stall, no post-R10 revalidation or gameplay adjustment was applied in this pass.