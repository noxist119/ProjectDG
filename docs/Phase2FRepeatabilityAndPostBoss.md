# Pass 2F - Repeatability and Post-R10 Localization

## Scope and frozen gameplay

- Production baseline SHA: `74e03b71fedb7c80567aa111693b13c20a3666bc` (Pass 2E).
- The four measurements below were executed with that production baseline plus this pass's batch-only telemetry instrumentation. No production gameplay file or gameplay balance value was changed.
- Overdrive remains the primary tuning reference; Classic is the control group.
- Frozen as requested: Overdrive `hordeCountMultiplier = 1.20`, R10 support override `8`, `bossHealthMultiplier = 0.72`, and `bossAttackMultiplier = 0.70`.
- No changes were made to horde cadence, boss/regular stats, economy, summon cost/rates, grade upgrades, missions, augments, shop balance, recipes, merge inheritance, slot pacing, Yahtzee, portraits, or `StopEffectKey`.

## Pass 2E reference

| Overdrive Pass 2E baseline | Value |
| --- | ---: |
| R10 reach | 10 / 12 |
| Actual R10 boss clear | 3 / 10 attempts |
| R11 / R12 / R15 reach | 5 / 12 / 5 / 12 / 2 / 12 |
| Average reached round | 10.92 |

## Measurement setup

Two sequential 12-run, paired-content-seed R30 batches were executed for each mode. Each repeat preserves its own JSON:

- `BatchPlaytestResults/DefenseGame_Phase2F_Overdrive_R30_RepeatA.json`
- `BatchPlaytestResults/DefenseGame_Phase2F_Overdrive_R30_RepeatB.json`
- `BatchPlaytestResults/DefenseGame_Phase2F_Classic_R30_RepeatA.json`
- `BatchPlaytestResults/DefenseGame_Phase2F_Classic_R30_RepeatB.json`

The four paired content seeds were `90210`, `98129`, `106048`, and `113967`; each was exercised once per strategy (`summon-heavy`, `balanced`, `shop-save`) in each repeat.

## Measured facts - repeat summaries

| Mode / repeat | Runs | R10 reach | Actual R10 clear | R11 | R12 | R13 | R14 | R15 | R30 | Avg reached | Avg life | Avg gold | Gameplay defeat | Technical | Runtime error runs | Softlocks |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Overdrive A | 12 | 5 | 2 / 5 | 3 | 2 | 2 | 0 | 0 | 0 | 8.58 | 0.83 | 55.00 | 10 | 2 | 0 | 0 |
| Overdrive B | 12 | 8 | 6 / 8 | 7 | 5 | 2 | 1 | 1 | 0 | 10.42 | 0.17 | 70.67 | 11 | 1 | 0 | 0 |
| Classic A | 12 | 10 | 3 / 10 | 5 | 3 | 1 | 1 | 1 | 0 | 10.25 | 0.00 | 65.25 | 12 | 0 | 0 | 0 |
| Classic B | 12 | 12 | 7 / 12 | 8 | 5 | 4 | 2 | 0 | 0 | 11.58 | 0.58 | 73.25 | 10 | 2 | 0 | 0 |

Technical failures were batch timeouts: two in Overdrive A, one in Overdrive B, and two in Classic B. No batch reported a runtime-error run or softlock.

### Boss accounting

| Mode / repeat | Boss attempts | Boss clears | Boss failures | R10 failure HP samples (min / max / avg) |
| --- | ---: | ---: | ---: | --- |
| Overdrive A | 5 | 2 | 3 | 0.98 / 1.00 / 0.99 |
| Overdrive B | 8 | 6 | 2 | 0.97 / 0.98 / 0.975 |
| Classic A | 10 | 3 | 7 | 0.40 / 0.99 / 0.7829 |
| Classic B | 12 | 7 | 5 | 0.06 / 0.99 / 0.59 |

Across both repeats, Overdrive recorded 13 R10 attempts, 8 actual clears, and 5 failures; Classic recorded 22 attempts, 10 actual clears, and 12 failures. This is measurement only, not a balance conclusion.

### Actual R10 clear by strategy

| Mode / repeat | summon-heavy | balanced | shop-save |
| --- | ---: | ---: | ---: |
| Overdrive A | 1 / 1 | 1 / 2 | 0 / 2 |
| Overdrive B | 2 / 2 | 1 / 2 | 3 / 4 |
| Classic A | 2 / 3 | 0 / 4 | 1 / 3 |
| Classic B | 2 / 4 | 3 / 4 | 2 / 4 |

Cells are actual R10 clears / R10 attempts, not total strategy runs.

## Repeatability result

The paired content seed did **not** produce fully deterministic runs in this exact-build repeat.

| Mode | Exact same-seed + strategy matches | Meaningful mismatches |
| --- | ---: | ---: |
| Overdrive | 0 / 12 | 12 / 12 |
| Classic | 0 / 12 | 12 / 12 |

An exact match requires equality for reached/defeat/technical outcome, end life/gold, summons, merges, grade levels, R10 reached/clear state, R10 failure HP, total leak damage, and all recorded choice-sequence hashes.

| Mode | Summon sequence | Merge sequence | Augment choices | Mission choices | Shop purchases |
| --- | ---: | ---: | ---: | ---: | ---: |
| Overdrive hash mismatch | 12 / 12 | 11 / 12 | 12 / 12 | 11 / 12 | 8 / 12 |
| Classic hash mismatch | 12 / 12 | 12 / 12 | 12 / 12 | 9 / 12 | 10 / 12 |

The first observable divergence category is choice/summon sequence: summon and augment hashes diverged in every same-seed pair. The hashes have no event timestamps, so they do not establish which internal random/event source caused the first divergence. Candidate causes for later investigation are an unseeded random source, frame/event ordering, physics ordering, or asynchronous editor-batch timing. No gameplay behavior was changed to force a match.

## R11-R15 localization facts

All listed checkpoint capacity values were `11`. `Targets` is the recorded target monster count for that checkpoint; `Horde` and `Mid` are counts of snapshots marked as a horde or mid-boss round. `Leak` is cumulative leak damage at the checkpoint.

| Mode / repeat | Checkpoint | N | Life | Gold | Board | Highest grade | Summons | Merges | Grade levels | Targets | Horde | Mid | Leak |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Overdrive A | R11 | 3 | 1.33 | 94.33 | 4.67 | 1.67 | 14.67 | 3.33 | 5.00 | 24.00 | 0 | 3 | 9.00 |
|  | R12 | 2 | 1.00 | 134.50 | 4.50 | 2.00 | 17.00 | 5.00 | 6.00 | 28.00 | 0 | 2 | 9.00 |
|  | R13 | 2 | 0.00 | 40.50 | 5.50 | 2.00 | 19.00 | 5.50 | 7.00 | 35.00 | 2 | 0 | 11.00 |
|  | R14 / R15 | 0 / 0 | - | - | - | - | - | - | - | - | - | - | - |
| Overdrive B | R11 | 7 | 1.14 | 123.29 | 6.43 | 1.43 | 13.86 | 3.29 | 5.86 | 24.00 | 0 | 7 | 10.29 |
|  | R12 | 5 | 0.60 | 117.40 | 6.80 | 1.40 | 16.60 | 4.40 | 6.00 | 28.00 | 0 | 5 | 11.20 |
|  | R13 | 2 | 0.50 | 106.50 | 7.00 | 1.50 | 18.00 | 6.00 | 7.00 | 35.00 | 2 | 0 | 12.50 |
|  | R14 | 1 | 1.00 | 162.00 | 8.00 | 2.00 | 23.00 | 8.00 | 7.00 | 32.00 | 0 | 0 | 12.00 |
|  | R15 | 1 | 0.00 | 69.00 | 7.00 | 2.00 | 26.00 | 10.00 | 8.00 | 37.00 | 0 | 0 | 13.00 |
| Classic A | R11 | 5 | 2.00 | 108.80 | 6.00 | 1.00 | 14.80 | 3.60 | 4.80 | 19.00 | 0 | 5 | 10.60 |
|  | R12 | 3 | 2.33 | 75.00 | 7.33 | 1.67 | 17.67 | 4.33 | 5.67 | 22.00 | 0 | 3 | 10.67 |
|  | R13 | 1 | 7.00 | 100.00 | 7.00 | 2.00 | 22.00 | 7.00 | 5.00 | 23.00 | 0 | 1 | 6.00 |
|  | R14 | 1 | 7.00 | 141.00 | 8.00 | 2.00 | 23.00 | 7.00 | 6.00 | 25.00 | 0 | 0 | 6.00 |
|  | R15 | 1 | 0.00 | 94.00 | 2.00 | 2.00 | 25.00 | 8.00 | 7.00 | 35.00 | 0 | 0 | 17.00 |
| Classic B | R11 | 8 | 2.00 | 101.88 | 5.75 | 1.25 | 13.12 | 2.75 | 4.75 | 19.00 | 0 | 8 | 9.38 |
|  | R12 | 5 | 2.60 | 130.00 | 7.20 | 1.20 | 15.40 | 3.20 | 5.60 | 22.00 | 0 | 5 | 9.40 |
|  | R13 | 4 | 0.50 | 100.50 | 7.00 | 1.25 | 17.75 | 4.50 | 6.50 | 23.00 | 0 | 4 | 11.25 |
|  | R14 | 2 | 0.00 | 79.50 | 6.50 | 1.50 | 20.50 | 6.00 | 7.00 | 25.00 | 0 | 0 | 11.50 |
|  | R15 | 0 | - | - | - | - | - | - | - | - | - | - | - |

## Gameplay defeat histogram

| Mode | Exact gameplay-defeat round histogram |
| --- | --- |
| Overdrive A | R5: 1, R6: 2, R7: 3, R8: 1, R11: 1, R13: 2 |
| Overdrive B | R5: 1, R8: 3, R11: 2, R12: 3, R13: 1, R15: 1 |
| Classic A | R5: 1, R7: 1, R10: 5, R11: 2, R12: 2, R15: 1 |
| Classic B | R10: 2, R11: 3, R12: 1, R13: 2, R14: 2 |
| Overdrive combined | R5: 2, R6: 2, R7: 3, R8: 4, R11: 3, R12: 3, R13: 3, R15: 1 |
| Classic combined | R5: 1, R7: 1, R10: 7, R11: 5, R12: 3, R13: 2, R14: 2, R15: 1 |

## R10 encounter freeze telemetry

| Mode / repeat | R10 support (min / max / avg) | Life at boss spawn avg | First boss damage avg seconds |
| --- | --- | ---: | ---: |
| Overdrive A | 8 / 8 / 8 | 3.80 | 0.50 |
| Overdrive B | 8 / 8 / 8 | 2.75 | 0.53 |
| Classic A | 9 / 9 / 9 | 4.70 | 0.54 |
| Classic B | 9 / 9 / 9 | 4.08 | 0.53 |

The requested Overdrive R10 support structure stayed at exactly eight in all 13 R10 attempts.

## First-boss summon-rush audit

The unchanged trigger remains `summons >= 34 OR merges >= 15` at the first boss.

| Population | R10 attempts | Maximum summons at R10 start | Maximum merges at R10 start | Threshold reached | Trigger observed |
| --- | ---: | ---: | ---: | ---: | ---: |
| Overdrive Repeat A | 5 | 16 | 5 | 0 | 0 |
| Overdrive Repeat B | 8 | 18 | 6 | 0 | 0 |
| Overdrive combined | 13 | 18 | 6 | 0 | 0 |

Measured result: in the 24 Overdrive runs, the system had 13 R10 starts and zero eligible starts; it did not trigger. This records the current normal-play reachability only. Thresholds and rewards were not changed.

## Shop-save observation

Pass 2E recorded four Overdrive shop-save R10 attempts, zero actual clears, and average end gold `82.50`.

| Dataset | R10 shop-save attempts | Actual R10 clears | Average end gold |
| --- | ---: | ---: | ---: |
| Pass 2F Overdrive A | 2 | 0 | 54.50 |
| Pass 2F Overdrive B | 4 | 3 | 98.25 |
| Pass 2F combined | 6 | 3 | 83.67 |

The repeat result is variable: the same shop-save policy recorded zero of two clears in A and three of four in B. R10-start shop-save snapshots averaged `35.50` gold, `7.50 / 11` board units, `6.00` grade levels, `9.83` summons, and `1.50` merges across its six R10 attempts. No economic change was made.

## Interpretation, kept separate from measurements

- R10 is no longer a zero-clear wall in these measurements, but the actual clear count varied from `2 / 5` to `6 / 8` in Overdrive repeat samples. This pass does not infer a balance adjustment from that variance.
- Overdrive post-R10 survivors concentrated at R11-R12; only one Repeat B run reached R14/R15. Repeat A reached no R14/R15 checkpoint.
- The repeatability check disproves full determinism for the current paired content seed. The added hashes localize the first observable difference to the run's choice/summon sequence, but further cause-specific instrumentation is required before changing random or timing behavior.
- Gameplay balance change: **NONE**.

## Verification

- `dotnet build .\\Assembly-CSharp-Editor.csproj --no-restore`: 0 errors; one existing CS0649 warning for `emptyGradeUpgradeAttempts`.
- Unity batch completed all four requested 12-run measurements and wrote four separate JSON files.
- Measured batches: runtime-error runs `0`; softlocks `0`.
