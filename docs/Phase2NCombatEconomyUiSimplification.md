# Pass 2N - Combat Economy UI Simplification

## Scope

This pass changes only combat-HUD presentation and the interaction window for two existing run investments:

- Grade stat upgrade
- High-grade summon chance

No combat, economy, summon-rate, cost-curve, reward, recipe, enemy, or hero balance value changed.

## HUD cleanup

| Before | After |
| --- | --- |
| `HintText` was created in the base combat HUD. | `HintText` is not created in the base combat HUD. |
| `UltimateRecipeHudPanel` occupied the base combat HUD. | `UltimateRecipeHudPanel` is not created in the base combat HUD. Recipe information remains in the existing ultimate merge / recipe-selection UI. |
| Grade upgrades were preparation-only. | Grade upgrades stay visible and can be purchased during combat when no blocking state is open. |
| Summon Grade Luck was preparation-only and used a system-facing label. | The compact `High-grade chance` control stays visible during combat and displays level, Epic+ bonus, and next cost. |

## Live investment behavior

- Grade upgrades are allowed during preparation and active combat.
- On purchase, every deployed non-temporary defender of that grade is immediately refreshed with the new run-grade attack and health bonus. Future summons continue to receive the same run-grade bonus.
- Summon Grade Luck is allowed during preparation and active combat.
- Both investment actions are unavailable during a blocking choice, Fate-card choice, defeat adjudication/finalization, or game-over state.
- This pass does not let investment controls bypass another modal's input rules.

## High-grade chance Info UX

`SummonGradeLuckInfoButton` uses the existing UI skin `info` icon. It opens a compact tooltip with these player-facing rules:

- Applies to normal summons only.
- Each level adds +1 percentage point to Epic+ chance.
- The bonus starts applying when high grades are available.
- Special summons, shop grants, recipes, and reward summons are excluded.

The tooltip closes on game over and on retry / outgame reset.

## Screenshot checklist

No binary screenshots are committed by this code pass. The PlayMode smoke validates the following before/after runtime states for capture on device/editor:

1. Base combat HUD has no `HintText` or `UltimateRecipeHudPanel`.
2. Grade Upgrade Bar remains visible during preparation and combat.
3. A combat-time High-grade chance purchase updates `Lv.0` to `Lv.1`, charges 50 GOLD, and keeps the bar visible.
4. The info button opens the tooltip and retry closes it.

## Validation

- `Assembly-CSharp.csproj`: 0 warnings, 0 errors.
- `Assembly-CSharp-Editor.csproj`: 0 warnings, 0 errors.
- PlayMode Smoke was not launched by this pass while the ProjectDG Unity Editor instance was already open; the smoke was updated with the Pass 2N assertions and should be run after that instance closes.