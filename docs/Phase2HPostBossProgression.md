# Pass 2H - Overdrive Post-R10 Progression Calibration

## Scope and exact build

- Exact source SHA: `4be009d04baf9595ef030ed819f94d05993295b0` (`Pass 2G - RNG Isolation Finalization`).
- This pass treats Overdrive as the primary tuning mode and Classic as a control/reference.
- Frozen settings retained: Overdrive horde count multiplier `1.20`; R10 support count `8`; boss health multiplier `0.72`; boss attack multiplier `0.70`.
- No R10 boss, global Horde, regular-monster, economy, summon, Grade Upgrade, shop, mission, augment, recipe, merge, slot, Yahtzee, hero_55/56/57, or `StopEffectKey` behavior was changed.

## 2G-final baseline execution

Both baseline runs used the existing paired content seeds and three existing strategies, targeting R30 with 12 runs per mode.

- Overdrive: `BatchPlaytestResults/DefenseGame_Phase2H_Overdrive_R30_Baseline.json`
- Classic control: `BatchPlaytestResults/DefenseGame_Phase2H_Classic_R30_Baseline.json`

| Mode | Runs | R10 actual clears | Boss attempts / clears / failures | R30 reach | Gameplay defeats | Technical failures | Runtime-error runs | Timeouts | Softlocks | Avg reached | Avg end life | Avg end gold |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Overdrive | 12 | 1 | 8 / 1 / 7 | 0 | 9 | 3 | 0 | 3 | 0 | 9.50 | 1.33 | 54.92 |
| Classic | 12 | 8 | 11 / 8 / 3 | 0 | 12 | 0 | 0 | 0 | 0 | 11.83 | 0.00 | 79.08 |

### Strategy results

| Mode | Strategy | Runs | R10 actual clears | Avg reached | Avg end life | Avg end gold |
|---|---|---:|---:|---:|---:|---:|
| Overdrive | summon-heavy | 4 | 0 | 8.25 | 0.00 | 41.00 |
| Overdrive | balanced | 4 | 1 | 10.25 | 2.75 | 78.25 |
| Overdrive | shop-save | 4 | 0 | 10.00 | 1.25 | 45.50 |
| Classic | summon-heavy | 4 | 3 | 10.75 | 0.00 | 53.75 |
| Classic | balanced | 4 | 3 | 12.75 | 0.00 | 101.25 |
| Classic | shop-save | 4 | 2 | 12.00 | 0.00 | 82.25 |

## Exact post-R10 round telemetry

`reached` means the run entered the round. `cleared` means that round actually completed; it is not inferred from a later reached round. Start values are recorded at round start and end values at the round conclusion/failure point. Counts below are averages among runs that reached that round.

### Overdrive

| Round | Type | Reached | Cleared | Life start/end | Gold start/end | Board/cap | Grade | Summons | Merges | Grade levels | Target/spawned/killed/escaped | Leak | Peak active | Duration s |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|
| R11 | MidBoss | 3 | 2 | 3.67 / 2.33 | 21.33 / 87.33 | 4.67 / 11 | 1.33 | 14.33 | 3.33 | 5.33 | 24 / 24 / 13.67 / 4.67 | 1.33 | 24.00 | 1.18 |
| R12 | Regular | 2 | 2 | 3.50 / 1.50 | 34.50 / 107.50 | 6.00 / 11 | 2.00 | 17.00 | 3.50 | 6.00 | 28 / 28 / 19.50 / 8.50 | 2.00 | 27.50 | 1.36 |
| R13 | Horde | 2 | 1 | 1.50 / 0.50 | 51.50 / 124.00 | 6.50 / 11 | 2.00 | 19.00 | 4.00 | 6.50 | 35 / 35 / 18.00 / 3.00 | 1.50 | 32.50 | 1.17 |
| R14 | Regular | 1 | 0 | 1.00 / 0.00 | 43.00 / 117.00 | 7.00 / 11 | 2.00 | 24.00 | 8.00 | 8.00 | 32 / 32 / 23.00 / 7.00 | 2.00 | 30.00 | 1.14 |
| R15 | Regular | 0 | 0 | - | - | - | - | - | - | - | - | - | - | - |

### Classic control

| Round | Type | Reached | Cleared | Life start/end | Gold start/end | Board/cap | Grade | Summons | Merges | Grade levels | Target/spawned/killed/escaped | Leak | Peak active | Duration s |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|
| R11 | MidBoss | 10 | 7 | 3.70 / 2.70 | 30.90 / 129.50 | 7.60 / 11 | 1.50 | 13.50 | 2.80 | 4.80 | 19 / 19 / 17.50 / 1.00 | 1.10 | 18.70 | 1.32 |
| R12 | Regular | 7 | 5 | 3.86 / 2.71 | 40.29 / 121.71 | 7.43 / 11 | 1.43 | 15.29 | 3.43 | 5.86 | 22 / 22 / 19.71 / 1.14 | 1.14 | 20.71 | 1.26 |
| R13 | Regular | 5 | 2 | 3.80 / 2.60 | 27.80 / 78.40 | 7.20 / 11 | 1.40 | 17.00 | 4.20 | 6.80 | 23 / 23 / 13.80 / 1.60 | 1.60 | 20.60 | 1.17 |
| R14 | Regular | 2 | 2 | 6.50 / 4.50 | 36.50 / 129.50 | 7.00 / 11 | 2.00 | 22.50 | 7.50 | 6.50 | 25 / 25 / 23.00 / 2.00 | 2.00 | 23.50 | 1.58 |
| R15 | Regular | 2 | 0 | 4.50 / 0.00 | 34.50 / 101.00 | 6.00 / 11 | 2.00 | 24.50 | 8.00 | 7.00 | 35 / 35 / 14.50 / 4.50 | 4.50 | 30.00 | 1.13 |

## R13 first post-boss Horde diagnosis

### Measured facts

- R13 is an Overdrive Horde round with target count 35.
- Overdrive R13 was reached by 2/12 runs and actually cleared by 1/12.
- R13 start life averaged 1.50; peak active monsters averaged 32.50; first leak occurred at 0.78 seconds; escapes averaged 3.00; duration averaged 1.17 seconds; end life averaged 0.50.
- The failed R13 shop-save run began at 1 life, leaked first at 0.78 seconds, escaped 5 monsters, and ended at 0 life. The cleared balanced run began at 2 life, first leaked at 0.79 seconds, escaped 1 monster, and ended at 1 life.
- More importantly, only 3/12 Overdrive runs reached R11 and only 2/12 reached R12. Three R10 timeouts were technical failures, not gameplay defeats.

### Conditional calibration decision

The required condition did **not** match:

- A. Overdrive R13 reach >= 6/12: **failed** (2/12).
- B. Actual R13 clear <= 2/12: passed (1/12).
- C. R11/R12 are not hard walls: **not supported** by this baseline (R11 reach 3/12; R12 reach 2/12).

Therefore **no R13 gameplay calibration was applied**. In particular, the R13 Horde target count remains 35; no 35 -> 31/32 modifier was added, and no revalidation pass was run because there was no balance change.

## Interpretation (not a balance change)

The measured post-R10 bottleneck occurs before the R13-specific condition has enough qualifying samples. This dataset does not justify attributing the progression loss solely to the R13 Horde count: most Overdrive runs did not enter R13, and the R10 timeout cohort must remain separated from normal gameplay defeats. A later decision pass should first obtain a cleaner R10/R11 sample with technical timeout behavior understood, then reassess whether the first post-boss Horde is independently responsible.

The frozen FirstBossSummonRush remains unreachable in this validation context, consistent with the prior audit (thresholds are 34 summons / 15 merges; prior observed maxima 18 / 6 and trigger count 0). This pass intentionally did not change it.

## Human-play caveat

These are accelerated strategy-bot runs, not a human difficulty verdict. They are useful for locating the sequence of reachable rounds and comparing deterministic content setup, but human positioning, timing, optional purchases, and subjective pressure must still be verified separately before changing production balance.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: 0 warnings, 0 errors.
- Unity PlayMode Smoke: pass; runtime errors `0`.
- Baseline JSONs completed for 12 Overdrive and 12 Classic runs.
- Gameplay balance changes in this pass: **NONE**.