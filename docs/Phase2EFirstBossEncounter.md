# Pass 2E - Overdrive R10 Encounter Structure

## Scope and frozen systems

- Overdrive remains the primary gameplay reference. Classic is a control group only.
- The only gameplay adjustment is the Overdrive R10 first-boss support count.
- No boss HP or ATK, regular monster, horde, economy, summon, grade-upgrade, shop, mission, augment, recipe, merge, slot-pacing, Yahtzee, portrait, or `StopEffectKey` change was made.
- `regularCountMultiplier = 1.26`, `hordeCountMultiplier = 1.20`, `bossHealthMultiplier = 0.72`, and `bossAttackMultiplier = 0.70` remain unchanged.
- Sources: Pass 2D facts in `Phase2DBossCalibration.md`; Pass 2E paired 12-run JSON files in `BatchPlaytestResults`.

## Applied encounter-only change

The generic Overdrive boss-support scale remains available for all boss rounds. After that generic scale, `CombatModeProfile.ApplyBossSupportCount` applies an explicit `overdriveFirstBossSupportCountOverride = 8` only when the profile is Overdrive and the round is R10.

| Encounter | Previous structure | Pass 2E structure | Measured Pass 2E spawn count |
| --- | --- | --- | ---: |
| Overdrive R10 | Base 9 x 1.06, approximately 10 supports | Exactly 8 supports | 8 / 8 / 8 / 8 / 8 / 8 / 8 / 8 / 8 / 8 |
| Overdrive R20+ | Generic scale unchanged | Generic scale unchanged | Not changed by this pass |
| Classic R10 control | Generic Classic structure | Unchanged | 9 / 9 / 9 / 9 / 9 / 9 / 9 / 9 / 9 / 9 |

The first-boss override is evaluated after the existing generic support calculation, so it does not alter Classic or later Overdrive boss rounds.

## New R10 encounter telemetry

Each R10 attempt now records:

- Life at boss spawn.
- Support spawn count, support kills before boss spawn, support escapes before boss spawn, and leak damage before boss spawn.
- Time from boss spawn to first defender damage and boss health at that first damage.
- Existing kill-count-based `r10BossCleared` remains the actual-clear signal.

The timing values below are batch-harness elapsed seconds from boss spawn; they are comparison telemetry, not a claim of player-facing real-time duration.

## Measured facts: Pass 2D baseline

| Mode | Runs | R10 attempts | Actual R10 clears | R10 failures | Failure HP min / max / avg / median | R10 reach | R11 | R12 | R15 | Avg reached |
| --- | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| Overdrive | 12 | 8 | 0 | 8 | 0.11 / 0.98 / 0.5913 / 0.595 | 8 | 4 | 3 | 0 | 9.83 |
| Classic control | 12 | 9 | 4 | 5 | 0.04 / 0.98 / 0.4900 / 0.31 | 9 | 7 | 2 | 0 | 10.00 |

Pass 2D did not yet have support-spawn telemetry. Its pre-change Overdrive R10 support structure was base 9 multiplied by 1.06, yielding approximately 10 supports before the boss.

## Measured facts: Pass 2E paired 12-run revalidation

| Mode | Runs | R7 | R8 | R10 | R11 | R12 | R15 | R30 | Avg reached | Avg end life | Avg end gold |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Overdrive | 12 | 11 | 10 | 10 | 5 | 5 | 2 | 0 | 10.92 | 0.17 | 74.92 |
| Classic control | 12 | 12 | 10 | 10 | 4 | 1 | 0 | 0 | 10.08 | 0.00 | 67.17 |

### Actual boss accounting

| Mode | Boss attempts | Boss clears | Boss failures | Actual R10 clears | Gameplay defeats | Technical failures | Runtime errors | Softlocks | Total leak damage |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Overdrive | 10 | 3 | 7 | 3 / 10 | 11 | 1 | 0 | 0 | 130 |
| Classic control | 10 | 3 | 7 | 3 / 10 | 12 | 0 | 0 | 0 | 156 |

The sole Overdrive technical failure was run 11, `balanced`, content seed `113967`: R10 round timeout with `r10BossHealthRemainingOnFailure01 = 0.97`. It recorded zero runtime errors and zero softlocks.

### R10 support and pre-boss state

| Mode | R10 attempts | Support spawn count | Support kills before boss | Support escapes before boss | Leak damage before boss | Life at boss spawn (min / max / avg / median) |
| --- | ---: | --- | --- | --- | --- | --- |
| Overdrive | 10 | 8 / 8 / 8 / 8 / 8 / 8 / 8 / 8 / 8 / 8 | 0 | 0 | 0 | 1 / 6 / 2.80 / 2.50 |
| Classic control | 10 | 9 / 9 / 9 / 9 / 9 / 9 / 9 / 9 / 9 / 9 | 0 | 0 | 0 | 1 / 8 / 3.40 / 3.00 |

### Boss first-damage telemetry

| Mode | First-damage samples | Seconds from boss spawn (min / max / avg / median) | Boss HP at first damage (min / max / avg / median) |
| --- | ---: | --- | --- |
| Overdrive | 10 | 0.49 / 0.65 / 0.547 / 0.545 | 0.99 / 1.00 / 0.994 / 0.990 |
| Classic control | 10 | 0.47 / 0.64 / 0.563 / 0.560 | 0.99 / 1.00 / 0.997 / 1.000 |

### R10 failure-health distribution

| Mode | Failure samples | Minimum | Maximum | Average | Median |
| --- | ---: | ---: | ---: | ---: | ---: |
| Overdrive | 7 | 0.02 | 0.99 | 0.8071 | 0.97 |
| Classic control | 7 | 0.40 | 0.99 | 0.7829 | 0.97 |

### Actual R10 clear by strategy

| Mode | Strategy | Runs | R10 attempts | Actual R10 clears | Avg reached | Avg end life | Avg end gold | Failure HP average |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Overdrive | summon-heavy | 4 | 3 | 2 | 11.50 | 0.00 | 48.25 | 0.99 |
| Overdrive | balanced | 4 | 3 | 1 | 10.75 | 0.50 | 94.00 | 0.96 |
| Overdrive | shop-save | 4 | 4 | 0 | 10.50 | 0.00 | 82.50 | 0.685 |
| Classic control | summon-heavy | 4 | 2 | 1 | 8.75 | 0.00 | 63.50 | 0.96 |
| Classic control | balanced | 4 | 4 | 0 | 10.25 | 0.00 | 63.25 | 0.77 |
| Classic control | shop-save | 4 | 4 | 2 | 11.25 | 0.00 | 74.75 | 0.72 |

## First-boss summon-rush note

`firstBossSummonRushMinSummons = 34` and `firstBossSummonRushMinMerges = 15` were not changed. Among the 10 Overdrive R10 starts measured in Pass 2E, the R10-start snapshot averaged 12.30 summons and 2.80 merges (median 11.50 and 2.00). This is a measured difference from those thresholds only; it was not calibrated in this pass.

## Interpretation kept separate from facts

- The narrow encounter change reached its direct structural target: every measured Overdrive R10 spawned exactly eight supports, while Classic remained at nine.
- Actual Overdrive R10 kills occurred in 3 of 10 R10 attempts, whereas the Pass 2D baseline recorded 0 of 8. The paired 12-run samples differ in which runs reached R10, so this result records the observed outcome and does not claim a guaranteed causal clear-rate change beyond this sample.
- Zero support escape and zero leak damage before boss spawn were recorded in all measured R10 attempts. The data therefore does not show a pre-boss support leak in these runs; the telemetry remains in place for later samples that may expose one.
- Overdrive remains intentionally distinct from Classic. This pass did not target parity and did not flatten horde pressure or alter boss stats.

## Verification

- `dotnet build .\\Assembly-CSharp-Editor.csproj --no-restore`: 0 errors; one pre-existing CS0649 warning for `emptyGradeUpgradeAttemptCount`.
- Unity batch: Classic R30 12/12 `complete`; Overdrive R30 12/12 `complete`.
- Both batch JSON files recorded 0 runtime-error runs and 0 softlocks.
