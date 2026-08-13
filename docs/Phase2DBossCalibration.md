# Pass 2D - Overdrive R10 Boss Calibration

## Scope and frozen systems

- Baseline: Pass 2C.
- Overdrive remains the primary gameplay reference. Classic is a control group only; matching Classic clear rate is not a goal.
- `regularCountMultiplier = 1.26` and `hordeCountMultiplier = 1.20` are unchanged.
- No boss HP, regular monster, economy, summon, grade-upgrade, shop, mission, augment, recipe, merge, slot-pacing, Yahtzee, portrait, or `StopEffectKey` change was made in this pass.
- Sources: `BatchPlaytestResults/DefenseGame_Phase2_Classic_R30.json` and `BatchPlaytestResults/DefenseGame_Phase2_Overdrive_R30.json` from the Pass 2D paired 12-run validation.

## Pass 2C baseline facts

| Mode | Runs | R10 reach | R10 boss attempts | Actual R10 boss clears | R11 reach | Average reached round | Average R10-start life | Board at R10 start | Highest grade at R10 start | Summons at R10 start | Merges at R10 start | Grade levels at R10 start |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Overdrive | 12 | 9/12 | 9 | 0 | 5/12 | 10.08 | 3.44 | 7.44 / 11 | 1.56 | 12.67 | 2.89 | 4.11 |
| Classic control | 12 | 11/12 | 11 | 4 | 6/12 | 10.75 | 3.45 | 7.64 / 11 | 1.18 | 11.45 | 2.09 | 3.82 |

### Pass 2C Overdrive R10 failure-health distribution before this pass

The nine actual R10 failure values read from the baseline JSON were:
`0.99, 0.97, 0.92, 0.98, 0.41, 0.96, 0.38, 0.93, 0.99`.

| Failure samples | Minimum | Maximum | Average | Median |
| ---: | ---: | ---: | ---: | ---: |
| 9 | 0.38 | 0.99 | 0.8367 | 0.96 |

| Strategy | Failure samples | Average R10 health remaining |
| --- | ---: | ---: |
| summon-heavy | 3 | 0.5933 |
| balanced | 3 | 0.9600 |
| shop-save | 3 | 0.9567 |

## Calibration decision

### Measured fact

The Pass 2C median remaining health is `0.96`, which is above the bounded-calibration threshold of `0.40`.

### Applied gameplay change

None. The requested rule forbids an automatic large HP reduction above that threshold. Overdrive R10 boss HP, all later boss rounds, Classic boss settings, and the Overdrive horde settings were therefore left unchanged.

### Telemetry correction applied

`DefenseGameBatchPlaytest` now records `r10BossCleared` only when the R10 active boss attempt is finalized and `RunBossKillCount` increased during that attempt. It no longer derives the R10-clear result from end-of-run Life and reached round.

The batch JSON now also writes `r10BossHealthRemainingOnFailure01`. It is captured only when the finalized attempt is R10, so a later boss attempt cannot overwrite the R10 failure-health measurement. Strategy summaries now include `r10BossClears` using this actual kill signal.

## Pass 2D revalidation facts

Both modes were rerun with the paired 12-run R30 command set after the telemetry correction.

| Mode | Runs | R3 | R5 | R7 | R8 | R9 | R10 | R11 | R12 | R15 | R30 | Average reached | Average end life | Average end gold |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Overdrive | 12 | 12 | 12 | 11 | 8 | 8 | 8 | 4 | 3 | 0 | 0 | 9.83 | 0.83 | 79.75 |
| Classic control | 12 | 12 | 12 | 11 | 9 | 9 | 9 | 7 | 2 | 0 | 0 | 10.00 | 0.00 | 74.92 |

### Actual R10 boss-clear accounting by strategy

| Mode | Strategy | Runs | R10 reaches | Actual R10 clears | Boss attempts | Boss clears | Boss failures | Leak damage |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Overdrive | summon-heavy | 4 | 3 | 0 | 3 | 0 | 3 | 48 |
| Overdrive | balanced | 4 | 2 | 0 | 2 | 0 | 2 | 53 |
| Overdrive | shop-save | 4 | 3 | 0 | 3 | 0 | 3 | 45 |
| Classic control | summon-heavy | 4 | 4 | 3 | 4 | 3 | 1 | 47 |
| Classic control | balanced | 4 | 1 | 0 | 1 | 0 | 1 | 43 |
| Classic control | shop-save | 4 | 4 | 1 | 4 | 1 | 3 | 55 |

| Mode | Actual R10 clear | Boss attempts / clears / failures | Gameplay defeats | Technical failures | Runtime-error runs | Softlocks | Total leak damage |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| Overdrive | 0 / 8 | 8 / 0 / 8 | 11 | 1 | 0 | 0 | 146 |
| Classic control | 4 / 9 | 9 / 4 / 5 | 12 | 0 | 0 | 0 | 145 |

The sole Overdrive technical failure was run 5 (`shop-save`): an R10 timeout with `r10BossHealthRemainingOnFailure01 = 0.95`; it had zero runtime errors and zero softlocks.

### R10 failure-health distribution after telemetry correction

| Mode | Failure samples | Minimum | Maximum | Average | Median |
| --- | ---: | ---: | ---: | ---: | ---: |
| Overdrive | 8 | 0.11 | 0.98 | 0.5913 | 0.595 |
| Classic control | 5 | 0.04 | 0.98 | 0.4900 | 0.31 |

| Mode | Strategy | Failure samples | Average R10 health remaining |
| --- | --- | ---: | ---: |
| Overdrive | summon-heavy | 3 | 0.4500 |
| Overdrive | balanced | 2 | 0.9200 |
| Overdrive | shop-save | 3 | 0.5133 |
| Classic control | summon-heavy | 1 | 0.1400 |
| Classic control | balanced | 1 | 0.9800 |
| Classic control | shop-save | 3 | 0.4433 |

### Leak data by round

| Mode | Leak damage by round |
| --- | --- |
| Overdrive | R1 17, R2 19, R3 24, R4 19, R5 15, R6 6, R7 12, R8 2, R9 1, R10 14, R11 7, R12 2, R13 4, R14 4 |
| Classic control | R1 21, R2 20, R3 14, R4 11, R5 18, R6 3, R7 10, R10 25, R11 19, R13 4 |

## Interpretation separated from measured facts

- The new actual-clear field confirms that this Pass 2D revalidation has no Overdrive R10 boss kill. It does not claim that every loss is an HP-only near miss: failure health ranges from `0.11` to `0.98`.
- The bounded rule selected no boss HP change from the Pass 2C before-distribution. The after data is a verification of unchanged gameplay plus corrected telemetry, not evidence of a new boss tuning value.
- Classic remains a distinct control: it produced four actual R10 boss kills in nine attempts. No effort was made to make Overdrive match that result.
- Overdrive preserves its 1.20 horde multiplier and fast pressure identity. No additional horde calibration was performed.

## Verification

- `dotnet build .\\Assembly-CSharp-Editor.csproj --no-restore`: 0 errors; one CS0649 warning for the pre-existing `emptyGradeUpgradeAttemptCount` telemetry field.
- Existing Unity PlayMode smoke: `status=pass`, `passed=true`, `runtimeErrors=0`.
- Unity batch: Classic 12/12 complete; Overdrive 12/12 complete.
- Revalidation JSON reports no runtime-error run and no softlock in either mode.
