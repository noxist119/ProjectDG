# Pass 2L - Human Play Polish

## Scope

This pass is limited to retry time reset reliability and choice readability. No gameplay balance, enemy stats, economy, summon rates, shop values, mission rules, rewards, recipes, or Yahtzee behavior changed.

Human-play context recorded for this pass: a run reached R28 in roughly 30 minutes, used Fate, and found Mythic/Transcendent progression enjoyable. This is context for future observation only; it did not produce a balance adjustment in this pass.

## Defeat retry time reset

### Previous failure mode

Combat acceleration could be active at 2x (Time.timeScale 2.0 and fixedDeltaTime 0.04) when defeat began. The defeat slow-motion controller could then capture that accelerated state as its restore baseline. Retrying after the defeat could therefore retain 2x time and fixed timestep.

### Fix

`RoundManager.BeginDefeatCinematic()` now ends combat acceleration before the defeat sequence captures the slow-motion baseline. The retry/reset sequence continues to restore the normal baseline of Time.timeScale 1.0 and fixedDeltaTime 0.02.

The Fate-choice slow-motion restore path remains separate and is explicitly covered by the new smoke validation.

## Choice readability

### Tactical mission

- Mission option title: 30 pt.
- Mission option description: 23 pt.
- Mission reward: 24 pt, bold, with the `Reward  +XXG` presentation.
- Active description: 24 pt.
- Active progress: 32 pt and bold.
- The mission modal uses a taller portrait-safe layout; all three selectable cards remain within the overlay.
- Best Fit is disabled for the mission, Boss Forecast, and Lucky choice bodies to avoid unexpected shrinking.

Future mission content/condition redesign is intentionally deferred. This pass only improves readable delivery of the existing missions.

### Boss Forecast

The selection panel is titled `R10 Boss Preparation`. It explains that the player receives one immediate bonus and a further reward when the R10 target is met. The three options are presented as:

- Unit Setup
- Aim for Higher Grade
- Play Safely

Copy distinguishes the actual mode behavior: Overdrive-only immediate effects are not described as if they apply in Classic. Existing choice mechanics are unchanged.

### Lucky Summon

The panel is titled `Consecutive Normal Reward! Special Summon`. It shows the current normal streak and single-use-per-run context. The existing three choices are described as:

- Merge Material Refill
- Rare-or-Better Guaranteed
- Epic 25% Challenge

Costs, probability, and reward mechanics are unchanged.

## Smoke additions

The PlayMode smoke now verifies:

1. Default baseline -> 2x combat acceleration -> defeat cinematic -> defeat slow motion -> retry returns to 1.0 / 0.02.
2. Fate slow motion can happen earlier in the same run and does not contaminate the defeat/retry restore baseline.
3. Boss Forecast and Lucky choice labels are present and readable.
4. Tactical mission font sizes, bold reward/progress hierarchy, disabled Best Fit, and portrait overlay bounds are valid.

## Validation status

- `dotnet build Assembly-CSharp.csproj --no-restore`: pass, 0 warnings, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: pass, 1 existing unrelated CS0649 warning in `DefenseGameBatchPlaytest.RunResult.emptyGradeUpgradeAttemptCount`.
- `git diff --check`: pass.
- Unity PlayMode smoke: pass. retry baseline=True, fate=True, defeat=True, slowmo=True, retry=True; choice readability boss=True, lucky=True, missionFonts=True, active=True, portrait=True; runtime errors=0.

## Deferred

No tactical mission content redesign, reward/balance changes, or additional progression work is included here.