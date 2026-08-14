# Pass 2J - First Boss Preparation Reward Calibration

## Scope

- Baseline commit: `24c3e33763c9cc628d475d9f44b7d17fdf14d1a7`.
- Overdrive is the primary tuning mode; Classic is the control/reference mode.
- This pass changes only the R10 First Boss Preparation Reward trigger.
- Enemy balance changes: **NONE**. Overdrive regular count `1.26`, horde count `1.20`, R10 support `8`, boss HP multiplier `0.72`, and boss ATK multiplier `0.70` are unchanged.
- The reward values and its R10-only duration semantics are unchanged: attack `+6%`, boss damage `+10%`, and existing maximum boss-damage bonus `+18%`.

## Trigger calibration

| Setting | Before | Pass 2J |
|---|---:|---:|
| R10 summon requirement | 34 | 14 |
| R10 merge requirement | 15 | 4 |
| Rule | Summons OR merges | Summons OR merges |

The historical threshold was unreachable in the observed preparation range (historical observed maximum: 18 summons / 6 merges, zero triggers). The final paired-seed Overdrive measurement contains six non-technical R10 starts. Two meet `summons >= 14 OR merges >= 4`, so the measured primary-mode normal-run trigger rate is `2/6 = 33.3%`, inside the 25-50% target band.

At actual R10 activation, the controller records a clear feedback banner and run-highlight card: **First Boss Preparation Reward** with the player summon/merge counts, `Attack +6%`, and the applied boss-damage percentage. This reuses the existing lightweight feedback path; no new UI system was added.

## Final validation inputs

- Overdrive: `BatchPlaytestResults/DefenseGame_Phase2J_Overdrive_R30.json`, 12 paired runs, target R30.
- Classic control: `BatchPlaytestResults/DefenseGame_Phase2J_Classic_R30.json`, 12 paired runs, target R30.
- Technical timeouts are listed separately from gameplay results.

## R10 preparation distribution

### Overdrive R10 starts

| Run | Strategy | Summons | Merges | Board | Highest grade | Grade levels | Technical | Reward |
|---:|---|---:|---:|---:|---:|---:|---|---|
| 1 | summon-heavy | 17 | 5 | 8 | 2 | 3 | no | yes |
| 2 | balanced | 10 | 1 | 8 | 1 | 6 | yes | no |
| 3 | shop-save | 13 | 3 | 7 | 1 | 6 | no | no |
| 4 | balanced | 13 | 3 | 8 | 1 | 4 | yes | no |
| 5 | shop-save | 12 | 2 | 8 | 1 | 6 | no | no |
| 7 | shop-save | 8 | 1 | 7 | 1 | 6 | no | no |
| 8 | summon-heavy | 18 | 6 | 7 | 2 | 4 | no | yes |
| 9 | balanced | 11 | 2 | 7 | 1 | 5 | yes | no |
| 12 | shop-save | 12 | 3 | 7 | 2 | 6 | no | no |

- R10 starts: 9; non-technical R10 starts: 6; technical timeouts: 3.
- Reward triggers: 2/9 across all R10 starts; **2/6 (33.3%)** among non-technical R10 starts.

### Classic R10 starts

| Run | Strategy | Summons | Merges | Board | Highest grade | Grade levels | Technical | Reward |
|---:|---|---:|---:|---:|---:|---:|---|---|
| 1 | summon-heavy | 14 | 4 | 7 | 2 | 3 | no | yes |
| 2 | balanced | 8 | 1 | 8 | 2 | 5 | no | no |
| 4 | balanced | 11 | 2 | 7 | 1 | 4 | no | no |
| 5 | shop-save | 9 | 1 | 8 | 1 | 5 | no | no |
| 6 | summon-heavy | 15 | 4 | 7 | 1 | 2 | no | yes |
| 8 | summon-heavy | 14 | 3 | 8 | 2 | 3 | no | yes |
| 11 | balanced | 9 | 1 | 8 | 1 | 4 | no | no |
| 12 | shop-save | 8 | 1 | 7 | 1 | 4 | no | no |

- R10 starts: 8; non-technical R10 starts: 8; technical timeouts: 0.
- Reward triggers: **3/8 (37.5%)**.

## Final measured results

| Mode | Runs | R10 starts | Reward triggers (normal R10) | R10 actual clears | R11 | R12 | R13 | Avg reached | Avg end life | Avg end gold | Technical | Runtime errors | Softlocks |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Overdrive | 12 | 9 | 2/6 | 1 | 4 | 3 | 1 | 9.83 | 1.08 | 54.50 | 3 | 0 | 0 |
| Classic | 12 | 8 | 3/8 | 4 | 6 | 6 | 5 | 10.67 | 0.00 | 80.25 | 0 | 0 | 0 |

### Triggered versus non-triggered R10 starts

| Mode | Group | Starts | Non-technical | R10 actual clears | R11 | R12 | R13 | Avg reached |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Overdrive | triggered | 2 | 2 | 1 | 1 | 1 | 1 | 12.50 |
| Overdrive | non-triggered | 7 | 4 | 0 | 3 | 2 | 0 | 10.71 |
| Classic | triggered | 3 | 3 | 3 | 3 | 3 | 3 | 14.33 |
| Classic | non-triggered | 5 | 5 | 1 | 3 | 3 | 2 | 12.00 |

### Strategy breakdown

| Mode | Strategy | Runs | R10 starts | Reward triggers | R10 actual clears | R11 | R12 | R13 | Avg reached | Technical |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Overdrive | summon-heavy | 4 | 2 | 2 | 1 | 1 | 1 | 1 | 9.50 | 0 |
| Overdrive | balanced | 4 | 3 | 0 | 0 | 0 | 0 | 0 | 8.75 | 3 |
| Overdrive | shop-save | 4 | 4 | 0 | 0 | 3 | 2 | 0 | 11.25 | 0 |
| Classic | summon-heavy | 4 | 3 | 3 | 3 | 3 | 3 | 3 | 12.00 | 0 |
| Classic | balanced | 4 | 3 | 0 | 1 | 2 | 2 | 1 | 11.00 | 0 |
| Classic | shop-save | 4 | 2 | 0 | 0 | 1 | 1 | 1 | 9.00 | 0 |

### Boss accounting

| Mode | Boss attempts | Boss clears | Boss failures |
|---|---:|---:|---:|
| Overdrive | 9 | 1 | 8 |
| Classic | 8 | 4 | 4 |

## Measured facts and interpretation

### Measured facts

- The final Overdrive normal-R10 trigger rate is 33.3%, which meets the stated 25-50% calibration target.
- The final Overdrive R10 actual-boss-clear result is 1/9 attempts. The one clear is in a triggered run; no causal claim is made from this two-run triggered sample.
- The final Classic control result is 3/8 triggers and 4/8 actual R10 clears.
- Overdrive produced three technical timeouts, with zero runtime-error runs and zero softlocks. Classic produced zero technical failures, runtime-error runs, and softlocks.

### Interpretation limits

- These are deterministic batch-bot measurements, not a replacement for human-play validation. The bot's resource conversion, merge timing, and tactical choices can differ materially from player behaviour.
- The preparation reward is a reward for active R10 preparation, not a catch-up grant. No enemy, economy, shop, summon, upgrade, recipe, or R13 setting was changed in this pass.
- R13 was measured only. Its enemy count and pacing were not modified.