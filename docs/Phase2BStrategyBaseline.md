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
- `balanced` skips upgrades while the minimum defence board is short. Candidate order is owned-grade count, lower upgrade cost, then higher grade.
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

**NOT EXECUTED.** A Unity batch invocation was attempted, but Unity reported that this project is already open in another Unity instance and exited before it could execute the batch method. It did not update either Phase 2 result JSON, so the values above remain the only measured data.

When the project is free, run the existing paired 12-run commands:

- `DefenseGame/Batch Playtest/Phase2 Classic R30`
- `DefenseGame/Batch Playtest/Phase2 Overdrive R30`

Then replace this section with only the newly generated JSON values. Do not treat any difference as gameplay balance evidence until a valid post-Pass-2B paired run exists.

## Interpretation boundary

The changed failure mode is an automation/preparation-gating issue, not a balance adjustment. Any subsequent reach-rate change can be attributed to bot-policy and gating correction only after paired remeasurement. Remaining defeats after valid reruns are gameplay-pressure observations; this pass deliberately does not compensate for them with balance changes.
