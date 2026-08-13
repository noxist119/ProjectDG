# Pass 2G — Run Content RNG Isolation

## Scope

- Commit baseline: ed3258e (Pass 2G partial implementation).
- Gameplay balance was not changed.
- This pass introduces independent deterministic streams for run-content selection only. Combat, combat timing, targets, procs, presentation, and physics/frame ordering remain on their existing random/runtime paths.

## Implementation

`RunContentRandomService` owns independently salted streams for `Summon`, `Augment`, `Mission`, `Shop`, `Board`, `Lucky`, `Fate`, and `Merge`.

The active run seed is reset from a daily/override seed when one exists, otherwise from a fresh per-run seed. The combat mode is included before stream initialization. Each stream records its initial seed, draw count, 64-entry trace prefix, and FNV outcome hash.

Run-content selection now uses the appropriate stream for player summons, Lucky results, Fate offer/grade-lock choices, normal merge results, board tiles, augment offer construction, mini-shop appearance/rarity/role/offer selection, shop grants, and mission-support grants. The batch harness no longer calls `UnityEngine.Random.InitState`, so it does not force combat RNG deterministic.

The batch JSON now contains `runContentChannels` per run. The smoke test checks both same-seed reset repeatability and that an Augment-channel draw does not alter the next Summon-channel draw.

## Validation setup

Unity 2022.3.62f3, target R30, paired content seeds, three existing strategies, 12 runs per repeat.

- Classic Repeat A: `DefenseGame_Phase2G_Classic_R30_RepeatA.json`
- Classic Repeat B: `DefenseGame_Phase2G_Classic_R30_RepeatB.json`
- Overdrive Repeat A: `DefenseGame_Phase2G_Overdrive_R30_RepeatA.json`
- Overdrive Repeat B: `DefenseGame_Phase2G_Overdrive_R30_RepeatB.json`

## Measured results

| Mode / repeat | Runs | R10 clears | Boss attempts / clears / failures | Target R30 | Defeats | Technical failures | Runtime-error runs | Timeouts | Softlocks | Avg reached | Avg end life | Avg end gold |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Classic A | 12 | 7 | 10 / 7 / 3 | 0 | 11 | 1 | 0 | 1 | 0 | 11.33 | 0.42 | 71.00 |
| Classic B | 12 | 6 | 8 / 6 / 2 | 0 | 11 | 1 | 0 | 1 | 0 | 10.83 | 0.33 | 60.42 |
| Overdrive A | 12 | 3 | 9 / 3 / 6 | 0 | 10 | 2 | 0 | 2 | 0 | 9.92 | 0.67 | 68.83 |
| Overdrive B | 12 | 3 | 9 / 3 / 6 | 0 | 10 | 2 | 0 | 2 | 0 | 10.58 | 0.83 | 62.08 |

### Same-seed / same-strategy comparison

Every paired run had identical per-channel initialization seeds (12/12 for every channel in both modes).

| Mode | Exact full gameplay match | Summon hash | Augment hash | Mission hash | Shop hash | Board hash | Lucky hash | Fate hash | Merge hash |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Classic | 0/12 | 1/12 | 6/12 | 2/12 | 9/12 | 6/12 | 10/12 | 11/12 | 4/12 |
| Overdrive | 0/12 | 0/12 | 5/12 | 4/12 | 8/12 | 4/12 | 11/12 | 8/12 | 5/12 |

The numbers above are final run hashes, not direct stream-seed mismatches. A content stream is deterministic from its own seed, but a later combat/timing divergence can change whether and how often the strategy bot requests that stream. Therefore later conditional content traces can legitimately differ even though stream initialization and an isolated cross-channel regression check remain deterministic.

## Interpretation

1. The old batch-level `UnityEngine.Random.InitState(contentSeed)` was removed. Combat remains non-deterministic by design; this validation does not claim full-game exact replay.
2. The high Lucky/Fate agreement is consistent with their comparatively fixed early-flow requests. Summon, Merge, Board, and some Augment paths are more sensitive to a preceding combat-driven branch.
3. The only technical failures in these runs were timeouts (Classic 1 per repeat; Overdrive 2 per repeat). There were no runtime-error runs and no softlocks.
4. No gameplay balance conclusion is drawn from this pass. These results are validation telemetry only.

## Static checks

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: 0 errors; one existing CS0649 warning for `emptyGradeUpgradeAttemptCount`.
- `git diff --check`: pass.
- Unity PlayMode Smoke: pass (`runSeedRepeatValid=true`, `runContentChannelIsolationValid=true`, `earlyMiniShopChoicesValid=true`, `runtimeErrors=0`).
- No combat balancing constants were edited.