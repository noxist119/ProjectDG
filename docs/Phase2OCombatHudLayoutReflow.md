# Pass 2O - Combat HUD Layout Reflow

## Scope

This pass changes only the combat HUD layout and the related smoke coverage. Gameplay balance, high-grade chance costs and rates, grade-upgrade values, combat-time access rules, and Fate effects are unchanged.

## Root cause

Screenshot A showed a discoverability and placement issue, not round gating. The high-grade chance purchase already existed from the early run, but it was compressed into the lower upgrade bar. The Fate entry action occupied the same lower-center space, which made the purchase target look like a footer label and made the area visually compete with the lower combat controls.

## Reference direction

Screenshot B was used as the layout direction: one distinct high-grade chance purchase card above a separate grade-upgrade row, with the Fate action moved to its own lower-right position.

## Final layout

- `SummonGradeLuckUpgrade` is an independent 350 x 84 compact card. Its hierarchy is High-grade chance, level / Epic+ bonus, then the next GOLD cost.
- The existing Info icon is placed inside the card's top-right corner. Its concise tooltip remains on demand only.
- The six grade-stat upgrade buttons are a separate row below the high-grade card. They remain visible and purchasable in preparation and combat.
- `FatePanelReopenButton` is a separate 238 x 82 lower-right action at `(right -150, bottom 478)`, leaving vertical spacing from both the economy area and the build readout.
- The high-grade chance card remains active from the first preparation round even if the player has insufficient GOLD or Epic is not yet naturally available. Insufficient GOLD only disables the purchase interaction.

## Validation

`ValidatePass2OCombatHudLayout` covers:

- R1-compatible active high-grade card presence and readable card dimensions.
- Visibility with insufficient GOLD, without permitting purchase.
- Visibility and purchase in combat with sufficient GOLD.
- Immediate level and next-cost text refresh after purchase.
- Info tooltip opening and safe reset close.
- Six grade-upgrade buttons remaining present and interactive.
- RectTransform major-overlap checks between the Fate action and the high-grade card / each grade button.
- Legacy base HUD cleanup still holds: `HintText` and `UltimateRecipeHudPanel` are absent.

The supplied Screenshot A is the before reference and Screenshot B is the intended after-direction reference. Runtime screenshots are intentionally not fabricated by this code-only pass.

## Balance impact

Gameplay balance changes: **NONE**.
## Executed validation

Unity PlayMode Smoke was executed after the layout change.

- Result: `pass`
- `pass2OCombatHudLayoutValid`: `true`
- `runtimeErrors`: `0`
- Existing full smoke assertions: passed
- Result file: `BatchPlaytestResults/DefenseGame_PlayModeSmoke.json`

Assembly validation also passed: `Assembly-CSharp` built with 0 warnings / 0 errors. `Assembly-CSharp-Editor` built with 0 errors; it retains one pre-existing `CS0649` warning in `DefenseGameBatchPlaytest.RunResult.emptyGradeUpgradeAttemptCount`, outside this pass.