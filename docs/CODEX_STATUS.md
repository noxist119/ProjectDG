# CODEX STATUS

- Latest completed pass: Pass 2Z — RunShop BattleButton Recovery.
- Root cause: `RunShopSystem.SetOpen()` changed only the RunShop panel GameObject. `SimpleGameHUD` derives BattleButton.interactable from `DefenseGameController.OnStateChanged`, so closing the panel could leave the previous blocked state rendered even when `BlockingChoiceReason=None`.
- Pass 2Z fix: RunShop now publishes `NotifyUiStateChanged()` only when its visible state actually changes. This refreshes the HUD on open, purchase-triggered auto-close, and `나중에`/close without changing Shop offers, prices, rounds, rewards, spawn timing, or balance.
- Pass 2Z actual UI verification: Editor-only EventSystem click scenarios reached R4 through Result Continue. Purchase path: selected `RunShopOffer_0`, RunShop resolved, BattleButton started R5 (`r4_shop_recovered`, PASS, runtimeErrors=0). Later path: clicked `RunShopCloseButton`, no active choice blocker remained, and BattleButton started R5 (`r4_shop_recovered`, PASS, runtimeErrors=0).
- Pass 2X revalidation: Unity PlayMode Smoke PASS with `runtimeErrors=0`; opening guidance, mission-toast, and UI-flow checks all passed.
- Pass 2Y status: still pending rerun after the R4 UI-block fix. Do not treat R10–R15 validation as complete yet.
- Gameplay balance change: none. The added validation scenarios are Editor-only; production reward, economy, monster, spawn, and balance values were not changed.
- Next task: rerun Pass 2Y persistent Overdrive R10–R15 validation using the repaired RunShop flow.
