# Pass 2K - Initial Preparation Battle Start Fix

## Scope

This pass fixes the initial preparation UI progression block only. No gameplay balance, enemy, economy, summon, grade, shop, mission, augment, recipe, Yahtzee, or hero behavior values changed.

## Root cause

The lobby hides the gameplay HUD. Entering battle preparation re-enabled that HUD, but `SimpleGameHUD.OnEnable()` only subscribed to controller state events. It did not immediately apply the already-current state to the re-enabled controls.

`SimpleGameHUD.RefreshDynamicState()` correctly calculates Battle button availability as:

`!IsRoundRunning && !IsBlockingChoiceOpen`

Before the first player action, the re-enabled button could therefore retain a stale disabled state. A summon later emitted a state change and refreshed the button, making it appear that summoning was required.

## Fix

`SimpleGameHUD.OnEnable()` now calls `Refresh()` immediately after `Subscribe()`. The current preparation state is therefore applied as soon as the gameplay HUD becomes active.

No board count or summon requirement was added. R1 remains startable at board count zero whenever no real blocking choice is pending.

The lobby and preparation guidance now say that the player can press Next Round when preparation is finished; they no longer imply that summoning is mandatory.

## PlayMode regression coverage

The smoke test now exercises the actual user-facing route:

1. Reset run and enter Lobby with an empty board and hidden gameplay HUD.
2. Invoke `LobbyBattleButton` to enter preparation.
3. Verify the re-enabled `BattleButton` is interactable before summon, merge, or grade upgrade.
4. Invoke `BattleButton` and verify R1 starts with zero board units.
5. Verify normal summon then start still works.
6. Reset, re-enter Lobby then preparation, and verify zero-summon start works again.
7. Create an actual Boss Forecast pending choice, complete the same state refresh that production round completion performs, and verify the Battle button is disabled and cannot start combat.

Latest Unity PlayMode Smoke result:

- `initialPreparationBattleStartUiPathValid`: `true`
- `lobbyHudHidden`: `true`
- `zeroReady`: `true`
- `zeroStarts`: `true`
- `normalStarts`: `true`
- `retryZeroStarts`: `true`
- `choiceBlocks`: `true`
- `runtimeErrors`: `0`

## Gameplay balance changes

NONE.