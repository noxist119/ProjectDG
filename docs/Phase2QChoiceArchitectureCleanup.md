# Pass 2Q — Choice Architecture Cleanup

## Scope

This pass removes mandatory early-choice friction without changing combat stats, economy tables, summon grade tables, missions' rewards, shop prices, augments, Fate, recipes, or Yahtzee.

## Boss Forecast

Boss Forecast remains implemented behind the `enableBossForecast` feature gate, which defaults to `false`.

- No R3 request, overlay, blocking reason, build handout, shop role bias, or objective resolution is available in normal play.
- The disabled system grants no compensating reward. Existing combat balance is intentionally unchanged.

## Tactical Contracts

Tactical Mission is presented to players as **전술 계약**.

- Offers are generated as before and persist until a contract is selected.
- The panel never opens automatically and is not a battle-start blocker.
- The player can open the summary, select one offer, or close it with **나중에** and begin the next round.
- Starting combat closes only the optional panel; it does not discard offers.

## Bad Luck Points

Lucky Summon is now player-facing **불운 보정**.

- Earliest active round: R11.
- Progress becomes visible at 15 points and is ready at 20 points.
- A paid, direct normal summon grants +1 point.
- A paid, direct rare summon removes 2 points (minimum 0).
- A paid, direct epic-or-higher summon resets points to 0.
- Free support, shop grants, Fate, mission rewards, special Lucky results, merges, and other reward grants do not change points.
- While ready, the player's normal summon press opens the existing three-choice Lucky UI before a normal summon is consumed. Existing choices and costs are unchanged.

Read-only runtime telemetry is available through `BadLuckPoints`, `LuckySummonVisibleThreshold`, `LuckySummonThreshold`, `LuckySummonEarliestRound`, `LuckySummonFirstReadyRound`, and `LuckySummonConsumed`.

## Validation

`DefenseGamePlayModeSmoke` includes Pass 2Q checks for:

- R1–R4 battle start without a Boss Forecast or mandatory Tactical Contract.
- Tactical Contract summary open, **나중에**, and selection paths.
- Bad Luck Point transitions, R10/R11 gates, free-grant isolation, and player-driven Lucky selection.
- Existing smoke and runtime error checks remain active.

Gameplay balance change: **NONE**.