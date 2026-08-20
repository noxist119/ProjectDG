# Pass 2P — Post-Round and Run Lifecycle Finalization

## Scope

This pass fixes post-round choice visibility and validates run/UI lifecycle behavior. Gameplay balance values are unchanged.

## Human Regression

After early round clears, especially R3/R4, pressing the next-round button without another summon could appear to do nothing.

Root cause: R3 legitimately creates the R10 Boss Forecast blocker. `BossForecastBetUI.HandleStateChanged()` previously returned while its overlay was inactive. If the one-shot presentation event was missed or covered, the controller remained at `BlockingChoiceReason = BossForecast` without a visible resolution UI.

## Fix

- `BossForecastBetUI` now self-heals from controller state: an available forecast always opens the overlay; an already open overlay refreshes; no available forecast closes it.
- `DefenseGameController.StartRound()` re-raises the existing `OnBossForecastBetRequested` event before returning when Boss Forecast blocks round start.
- No board count, summon, merge, upgrade, or spending prerequisite was added to round start.
- Added `TopFullBleedBackdrop` under the safe-area HUD to cover the top screen inset. It is opaque, full-width, non-interactive, and prevents the purple page/background strip from showing above the combat HUD.

## PlayMode Smoke Results

Full Unity PlayMode Smoke: **PASS**. Runtime errors: **0**.

### Post-round no-action UI path

Actual UI buttons were used for Battle, Result Continue, and Boss Forecast choice.

- R1 -> R2: pass without preparation action.
- R2 -> R3: pass without preparation action.
- R3 -> R4: Boss Forecast was the explicit blocker; overlay active, visible, raycast-blocking, and its choice button actionable.
- After selecting Boss Forecast: R4 started with **0 player summons after R3**.
- Board count recorded: before forecast `1`, after selected forecast `2`; this is the selected Supply forecast's free reward, not a player summon.
- R4 -> R5: pass with no summon, merge, grade upgrade, or high-grade-chance upgrade; blocker was `None`.

Smoke summary:

```text
r1=True/True, r2=True/True, r3=True/True/True,
r4=True/True/blocker=None, r5=True,
boardBeforeForecast=1, boardAfterForecast=2,
postR3PlayerSummons=0
```

### Run content seed lifecycle

- Ordinary run seed is initialized before summon-channel draws and is non-zero.
- Retry produces a fresh ordinary seed.
- Exit-to-outgame followed by a new run produces a fresh ordinary seed.
- Explicit seed override reproduces the same Summon-channel prefix.
- Daily Fate Cup reproduces the same Summon-channel prefix for the same daily seed.
- Existing Pass 2G channel isolation smoke remains passing.

### Menu pause lifecycle

- Opening the hamburger at 1x preserves `timeScale = 1` and `fixedDeltaTime = 0.02`.
- Opening it at 2x preserves `timeScale = 2` and `fixedDeltaTime = 0.04`.
- Settings -> Exit Confirm pauses at `timeScale = 0`.
- Continue restores both prior 1x and 2x speeds exactly.
- Confirm Exit restores the fresh-run 1x baseline; neither paused 0x nor stale 2x leaks into the next run.

## Balance

Gameplay balance changes: **NONE**.

Not changed: enemy/boss values, Horde 1.20, R10 support 8, R13, economy, summon rate/cost, Grade Upgrade, Summon Grade Luck values, missions, shops, augments, Boss Forecast rewards/conditions, recipes, Fate, Yahtzee, or `StopEffectKey`.