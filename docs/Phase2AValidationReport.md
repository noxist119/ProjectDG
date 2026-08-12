# Pass 2A Validation Report

- Measured result files: `BatchPlaytestResults/DefenseGame_Phase2_Classic_R30.json` and `BatchPlaytestResults/DefenseGame_Phase2_Overdrive_R30.json`.
- Target: R30; paired-seed strategy policy; 12 runs per combat mode.
- This report records generated JSON values only. No gameplay balance values were changed.

## Classic R30

| Metric | Measured value |
| --- | ---: |
| Target reach rate | 0/12 (0.000) |
| Gameplay defeats | 11 |
| Technical failures | 1 |
| Runtime-error runs | 0 |
| Soft locks | 0 |
| Boss attempts / clears / failures | 2 / 0 / 2 |
| Boss clear rate | 0.000 |
| Average reached round | 6.08 |
| Average end life | 0.58 |
| Average end gold | 31.50 |
| Mission choices / completions | 47 / 4 |
| Grade-upgrade purchases | 26 |
| Ultimate-recipe merges | 0 |

### Strategy results

| Strategy | Runs | Target reach | Gameplay defeats | Technical failures | Runtime-error runs | Soft locks | Boss A/C/F | Avg. reached | Avg. end life | Avg. end gold | Mission choices / completions | Grade upgrades | Ultimate merges |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| summon-heavy | 4 | 0/4 | 4 | 0 | 0 | 0 | 0/0/0 | 4.00 | 0.00 | 15.50 | 8 / 0 | 4 | 0 |
| balanced | 4 | 0/4 | 4 | 0 | 0 | 0 | 1/0/1 | 8.25 | 0.00 | 45.25 | 15 / 3 | 14 | 0 |
| shop-save | 4 | 0/4 | 3 | 1 | 0 | 0 | 1/0/1 | 6.00 | 1.75 | 33.75 | 24 / 1 | 8 | 0 |

## Overdrive R30

| Metric | Measured value |
| --- | ---: |
| Target reach rate | 0/12 (0.000) |
| Gameplay defeats | 12 |
| Technical failures | 0 |
| Runtime-error runs | 0 |
| Soft locks | 0 |
| Boss attempts / clears / failures | 4 / 0 / 4 |
| Boss clear rate | 0.000 |
| Average reached round | 7.17 |
| Average end life | 0.00 |
| Average end gold | 54.83 |
| Mission choices / completions | 66 / 15 |
| Grade-upgrade purchases | 48 |
| Ultimate-recipe merges | 0 |

### Strategy results

| Strategy | Runs | Target reach | Gameplay defeats | Technical failures | Runtime-error runs | Soft locks | Boss A/C/F | Avg. reached | Avg. end life | Avg. end gold | Mission choices / completions | Grade upgrades | Ultimate merges |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| summon-heavy | 4 | 0/4 | 4 | 0 | 0 | 0 | 0/0/0 | 5.00 | 0.00 | 20.75 | 12 / 0 | 12 | 0 |
| balanced | 4 | 0/4 | 4 | 0 | 0 | 0 | 1/0/1 | 6.75 | 0.00 | 59.00 | 15 / 3 | 12 | 0 |
| shop-save | 4 | 0/4 | 4 | 0 | 0 | 0 | 3/0/3 | 9.75 | 0.00 | 84.75 | 39 / 12 | 24 | 0 |

## Measured balance observations

- Both generated R30 result files record a 0/12 target reach rate and 0 boss clears.
- Classic contains one technical failure: result index 5 (`shop-save`, seed 98129) records `round_timeout_R10_bossHp_0.94`; it is the only timeout. The remaining 11 Classic results are gameplay defeats.
- Overdrive records 12 gameplay defeats and no technical failures, runtime-error runs, or soft locks.
- No result in either file records an ultimate-recipe merge.
- `shop-save` has the highest measured average reached round and end gold in both files: Classic 6.00 / 33.75; Overdrive 9.75 / 84.75. These are recorded outcomes, not a causal balance conclusion.

## Validation interpretation

A run is exactly one of:

- **victory** - target round reached, life remains, no technical failure.
- **defeat** - life is zero from normal gameplay, without a technical failure.
- **technical failure** - runtime error, exception/assert, invariant failure, soft lock, timeout, or an otherwise nonterminal harness exit.

Boss clear rate uses actual `RunBossKillCount` increases, not merely a boss round ending. A boss failure records the last observed boss HP ratio when available.