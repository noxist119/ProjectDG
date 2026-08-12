# Pass 2B - Preparation Skip and Strategy Baseline

## Scope

- Base commit: `5f5b8b9` (`Phase 2A Validation Results`).
- This pass changes only preparation-start gating, batch strategy policy, batch telemetry, and Smoke coverage.
- Gameplay balance was not changed: monsters, bosses, spawn pacing, summon rates/costs, rewards, Grade Upgrade values/costs, missions, shops, recipes, merge inheritance, Yahtzee, portraits, and `StopEffectKey` are unchanged.

## Preparation-start regression

### Root cause

`DefenseGameController.IsBlockingChoiceOpen` treated an Augment UI panel that was merely visible (`IsChoiceOpen`) as a mandatory blocker. A stale, invisible, or already-resolved UI state could therefore prevent `StartRound()` despite the player having no unresolved choice. This was unrelated to summon count or board occupancy.

### Fix

`BlockingChoiceReason` is now the single read-only source of truth for both diagnostics and `IsBlockingChoiceOpen`:

- `None`
- `BossForecast`
- `Augment` (only a ready pending Augment choice)
- `RunShop` (actual open shop modal)
- `TacticalMission` (actual unresolved mission selection)
- `LuckySummon`

`StartRound()` blocks only for one of those reasons. It does not inspect summon count, board-unit count, grade-upgrade use, merge use, or unspent gold.

### Added PlayMode Smoke cases

1. New run, zero summons, no blocker: starts R1.
2. Completed round, no preparation action: starts the next round.
3. A summoned unit is upgraded, with no second summon: starts the next round.
4. An actual Boss Forecast selection blocks start and reports `BossForecast`.
5. Resolving that choice allows a zero-summon start.

## Human-like strategy baseline changes

- Batch grade upgrades exclude grades with `CountUnitsOfGrade(grade) <= 0`; production `CanUpgradeGrade()` is unchanged.
- `summon-heavy` reserves two summon costs before an upgrade in R1-R5, then one from R6 onward.
- `balanced` skips upgrades while the minimum defence board is short. Candidate order is owned-grade count, lower upgrade cost, then lower grade.
- `shop-save` keeps its existing reserve and minimum-board summon priority, and also cannot select an empty grade or an upgrade that drops below reserve.

New result telemetry: first upgrade round, one upgrade count per grade, empty upgrade attempts, last actual blocking-choice reason, R3/R5/R8/R10 snapshots, and the R10 boss-start snapshot. A normal batch run should have `emptyGradeUpgradeAttemptCount = 0`.

## Previous Phase 2A baseline (measured before Pass 2B)

Source files: `BatchPlaytestResults/DefenseGame_Phase2_Classic_R30.json` and `BatchPlaytestResults/DefenseGame_Phase2_Overdrive_R30.json`. These numbers are preserved as the pre-fix baseline.

| Mode | Runs | R10 reach | R30 reach | Gameplay defeats | Technical failures | Runtime-error runs | Soft locks | Boss A/C/F | Avg. reached | Avg. end life | Avg. end gold | Summons | Merges | Grade upgrades | Mission choices/completions | Ultimate merges |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Classic | 12 | 0/12 | 0/12 | 11 | 1 | 0 | 0 | 2/0/2 | 6.08 | 0.58 | 31.50 | 62 | 2 | 26 | 47/4 | 0 |
| Overdrive | 12 | 0/12 | 0/12 | 12 | 0 | 0 | 0 | 4/0/4 | 7.17 | 0.00 | 54.83 | 71 | 6 | 48 | 66/15 | 0 |

### Previous strategy breakdown

| Mode | Strategy | Runs | Defeats | Technical failures | Boss A/C/F | Avg. reached | Avg. life | Avg. gold | Summons | Merges | Grade upgrades | Mission choices/completions |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Classic | summon-heavy | 4 | 4 | 0 | 0/0/0 | 4.00 | 0.00 | 15.50 | 12 | 0 | 4 | 8/0 |
| Classic | balanced | 4 | 4 | 0 | 1/0/1 | 8.25 | 0.00 | 45.25 | 28 | 1 | 14 | 15/3 |
| Classic | shop-save | 4 | 3 | 1 | 1/0/1 | 6.00 | 1.75 | 33.75 | 22 | 1 | 8 | 24/1 |
| Overdrive | summon-heavy | 4 | 4 | 0 | 0/0/0 | 5.00 | 0.00 | 20.75 | 8 | 0 | 12 | 12/0 |
| Overdrive | balanced | 4 | 4 | 0 | 1/0/1 | 6.75 | 0.00 | 59.00 | 26 | 1 | 12 | 15/3 |
| Overdrive | shop-save | 4 | 4 | 0 | 3/0/3 | 9.75 | 0.00 | 84.75 | 37 | 5 | 24 | 39/12 |

## New Pass 2B baseline

Source files: `BatchPlaytestResults/DefenseGame_Phase2_Classic_R30.json` and `BatchPlaytestResults/DefenseGame_Phase2_Overdrive_R30.json`. Both are paired, 12-run, R30-target results. Values below are recorded measurements only.

### Run-level results

| Mode | Runs | R3 reach | R5 reach | R8 reach | R10 reach | R30 reach | Avg. reached round | Avg. end life | Avg. end gold | Defeats | Technical failures | Runtime errors | Softlocks |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Classic | 12 | 12/12 | 12/12 | 9/12 | 8/12 | 0/12 | 9.83 | 0.17 | 45.50 | 11 | 1 | 0 | 0 |
| Overdrive | 12 | 12/12 | 12/12 | 5/12 | 4/12 | 0/12 | 7.92 | 0.00 | 63.08 | 12 | 0 | 0 | 0 |

| Mode | Summons | Merges | Grade-upgrade purchases | Upgrade counts by grade (N/R/E/L/M/T) | Empty upgrade attempts | Boss attempts / clears / failures | Ultimate Recipe merges | Mission choices / completions |
|---|---:|---:|---:|---|---:|---|---:|---|
| Classic | 145 | 23 | 46 | 31 / 15 / 0 / 0 / 0 / 0 | 0 | 8 / 4 / 4 | 0 | 75 / 17 |
| Overdrive | 131 | 21 | 33 | 25 / 8 / 0 / 0 / 0 / 0 | 0 | 4 / 0 / 4 | 0 | 67 / 20 |

### Strategy detail

| Mode | Strategy | Runs | R3/R5/R8/R10/R30 reach | Avg. round | Avg. life | Avg. gold | Summons | Merges | Upgrades (N/R) | Empty attempts | Boss A/C/F | Defeats / tech / errors / softlocks | Ultimate merges | Mission choices/completions |
|---|---|---:|---|---:|---:|---:|---:|---:|---|---:|---|---|---:|---|
| Classic | summon-heavy | 4 | 4/4/3/3/0 | 10.75 | 0.00 | 43.25 | 68 | 16 | 6/8 | 0 | 3/3/0 | 4/0/0/0 | 0 | 18/6 |
| Classic | balanced | 4 | 4/4/3/2/0 | 8.50 | 0.00 | 44.25 | 36 | 3 | 10/3 | 0 | 2/0/2 | 4/0/0/0 | 0 | 16/3 |
| Classic | shop-save | 4 | 4/4/3/3/0 | 10.25 | 0.50 | 49.00 | 41 | 4 | 15/4 | 0 | 3/1/2 | 3/1/0/0 | 0 | 41/8 |
| Overdrive | summon-heavy | 4 | 4/4/0/0/0 | 6.00 | 0.00 | 43.00 | 44 | 12 | 1/0 | 0 | 0/0/0 | 4/0/0/0 | 0 | 12/8 |
| Overdrive | balanced | 4 | 4/4/2/2/0 | 8.75 | 0.00 | 78.00 | 42 | 3 | 11/3 | 0 | 2/0/2 | 4/0/0/0 | 0 | 19/3 |
| Overdrive | shop-save | 4 | 4/4/3/2/0 | 9.00 | 0.00 | 68.25 | 45 | 6 | 13/5 | 0 | 2/0/2 | 4/0/0/0 | 0 | 36/9 |

First grade-upgrade rounds were recorded as follows; a missing value means that run made no grade upgrade. Classic: summon-heavy `6, 7, 7`; balanced `4, 4, 4, 5`; shop-save `6, 6, 5, 5`. Overdrive: summon-heavy `7`; balanced `5, 4, 4, 5`; shop-save `3, 3, 3, 3`.

### R10 boss-start snapshots

| Mode | Snapshot count | Strategy distribution | Life | Gold | Board | Highest owned grade | Summons | Merges | Grade-upgrade levels | Summon cost |
|---|---:|---|---|---|---|---|---|---|---|---|
| Classic | 8 | summon-heavy 3, balanced 2, shop-save 3 | 1-10 (avg. 5.88) | 5-42 (avg. 25.13) | 7-8 / 11 | 0:1, 1:4, 2:3 | 7-16 (avg. 11.25) | 0-4 (avg. 2.00) | 2-5 (avg. 3.63) | 17-26 (avg. 21.25) |
| Overdrive | 4 | summon-heavy 0, balanced 2, shop-save 2 | 4-9 (avg. 6.00) | 24-40 (avg. 32.25) | 7 / 11 | 1:2, 2:2 | 9-15 (avg. 11.50) | 1-4 (avg. 2.25) | 5-6 (avg. 5.25) | 19-25 (avg. 21.50) |

### Comparison with the recorded Phase 2A baseline

- Classic: R10 reach changed from 0/12 to 8/12; average reached round from 6.08 to 9.83; summons from 62 to 145; merges from 2 to 23; grade-upgrade purchases from 26 to 46; boss attempts/clears/failures from 2/0/2 to 8/4/4. Technical failures remain 1; runtime errors, softlocks, R30 reach, and Ultimate Recipe merges remain 0.
- Overdrive: R10 reach changed from 0/12 to 4/12; average reached round from 7.17 to 7.92; average end gold from 54.83 to 63.08; summons from 71 to 131; merges from 6 to 21; grade-upgrade purchases from 48 to 33. Boss attempts/clears/failures remain 4/0/4; technical failures, runtime errors, softlocks, R30 reach, and Ultimate Recipe merges remain 0.
- The Phase 2A table did not record R3, R5, or R8 reach counts, so those new checkpoints have no direct recorded Phase 2A comparison.

### Measured observations only

- No run reached R30 in either mode.
- Classic recorded four boss clears from eight attempts; Overdrive recorded zero boss clears from four attempts.
- All recorded grade upgrades were Normal or Rare, and `emptyGradeUpgradeAttemptCount` was zero for every run.
## Interpretation boundary

This report records post-finalization measurements. It does not change gameplay balance or attribute causes beyond the values recorded in the source JSON.
