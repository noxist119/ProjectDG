# CODEX STATUS

- Latest completed pass: Pass 2Y — Persistent R10–R15 PlayMode Validation rerun.
- Pass 2Z prerequisite: RunShop state changes now publish the Controller UI refresh, so a closed RunShop cannot leave BattleButton stale-disabled. No RunShop content, price, reward, or balance value changed.
- Pass 2Y actual Overdrive result: PASS. The Editor-only persistent runner used only EventSystem clicks on visible Battle, result Continue, Shop, Tactical Mission, Augment, and other choice buttons; no direct `StartRound`, debug keys, round jump, reward grant, or production balance change was used. R4 RunShop purchase resolved and the next BattleButton click started R5.
- R10 record: start HP 1/10, 8 Gold, 11 board units (all Normal), 10 player summons, 0 merges. Boss warning title and spawned display name were both `골렘 군주`; spawn HP was 4,700. The boss was actually cleared, remaining boss HP was 0%, the result Continue was clicked, and R11 started.
- R11–R15 record: R11, R12, R13 (Horde), R14, and R15 all started through the visible UI path. R15 started at HP 1/10, 231 Gold, 11 Normal board units. No Invisible Blocker was observed and runtimeErrors=0.
- Pass 2X revalidation: Unity PlayMode Smoke PASS with runtimeErrors=0; opening guidance, mission-toast, and UI-flow checks all passed.
- Gameplay balance change: none. Persistent validation execution used test-only time acceleration and restores it on exit; no production stat, economy, reward, spawn, or balance value changed.
- Next task: Start the next user-requested ProjectDG pass.

## Pass 3 — Early Game Core Loop Balance

- Scope: one Overdrive balance adjustment plus the existing Editor-only UI audit. No new systems, UI, currency, units, automation, Classic balance, R16+, or boss presentation were changed.
- Baseline audit (EventSystem UI only): the conservative standard route was defeated at R6 before R10. HP fell 10 -> 8 -> 6 -> 4 -> 3 -> 2 -> 0 while all cleared rounds had `BlockingChoiceReason=None`, no active choice blocker, and `runtimeErrors=0`.
- Adjustment: Overdrive `roundLeakDamageCap` 2 -> 1. Burst packs, count multipliers, Horde rules, monster stats, rewards, economy, summoning, and boss values are unchanged.
- Revalidation: the same EventSystem UI route completed R15 with `runtimeErrors=0` and no invisible blocker.

### Before adjustment — actual UI audit

| Round | Type / target | Start HP/Gold/board | End HP/Gold/board | Summons / merges | Choice flow | Result |
| --- | --- | --- | --- | --- | --- | --- |
| R1 | Regular / 5 | 10 / 7 / N1 | 8 / 16 / N1 | 1 / 0 | — | Cleared |
| R2 | Regular / 6 | 8 / 6 / N2 | 6 / 18 / N2 | 2 / 0 | Continue -> Mission Later | Cleared |
| R3 | Regular / 8 | 6 / 7 / N3 | 4 / 22 / N4 | 3 / 0 | Continue -> Mission Later | Cleared |
| R4 | Horde / 11 | 4 / 11 / N5 | 3 / 43 / N5 | 4 / 0 | Continue -> Mission Later | Cleared |
| R5 | Regular / 9 | 3 / 31 / N6 | 2 / 58 / N6 | 5 / 0 | Continue -> RunShop purchase | Cleared |
| R6 | Regular / 9 | 2 / 11 / N7 | 0 / 37 / N7 | 6 / 0 | — | Defeat |
| R7-R15 | — | — | — | — | — | Not reached |

### After adjustment — actual UI audit

| Round | Type / target | Start HP/Gold/board | End HP/Gold/board | Summons / merges | Choice flow | Result |
| --- | --- | --- | --- | --- | --- | --- |
| R1 | Regular / 5 | 10 / 7 / N1 | 9 / 16 / N1 | 1 / 0 | — | Cleared |
| R2 | Regular / 6 | 9 / 6 / N2 | 8 / 16 / N2 | 2 / 0 | Continue -> Mission Later | Cleared |
| R3 | Regular / 8 | 8 / 5 / N3 | 7 / 20 / N4 | 3 / 0 | Continue -> Mission Later | Cleared |
| R4 | Horde / 11 | 7 / 9 / N5 | 6 / 38 / N5 | 4 / 0 | Continue -> Mission Later | Cleared |
| R5 | Regular / 9 | 6 / 26 / N6 | 5 / 50 / N6 | 5 / 0 | Continue -> RunShop purchase | Cleared |
| R6 | Regular / 9 | 5 / 30 / N7+R1 | 5 / 59 / N7+R1 | 6 / 0 | Continue -> Augment -> Mission Later | Cleared |
| R7 | Horde / 12 | 5 / 16 / N8+R1 | 5 / 61 / N8+R1 | 7 / 0 | Continue -> Mission Later | Cleared |
| R8 | Regular / 11 | 5 / 47 / N9+R1 | 5 / 90 / N9+R1 | 8 / 0 | Continue -> Mission Later | Cleared |
| R9 | Regular / 13 | 5 / 31 / N10+R1 | 5 / 83 / N10+R1 | 9 / 0 | Continue -> Mission Later | Cleared |
| R10 | Boss / 9 support | 5 / 22 / N10+R1 | 5 / 101 / N10+R1 | 9 / 0 | Continue -> Mission Later | Cleared: Gollem Lord, 4,700 HP |
| R11 | Regular / 24 | 5 / 16 / N10+R1 | 5 / 130 / N10+R1 | 9 / 0 | Continue -> RunShop Later | Cleared |
| R12 | Regular / 28 | 5 / 5 / N10+R1 | 5 / 123 / N10+R1 | 9 / 0 | Continue -> Augment -> Mission Later | Cleared |
| R13 | Horde / 35 | 5 / 128 / N10+R1 | 5 / 254 / N10+R1 | 9 / 0 | Continue -> Mission Later | Cleared |
| R14 | Regular / 32 | 5 / 70 / N10+R1 | 5 / 187 / N10+R1 | 9 / 0 | Continue -> Mission Later | Cleared |
| R15 | Regular / 37 | 5 / 193 / N10+R1 | 5 / 354 / N10+R1 | 9 / 0 | — | Cleared |

### Choice and runtime evidence

- All cleared rounds ended with `BlockingChoiceReason=None` and an interactable BattleButton. The result overlay was the only active panel at end snapshots; it was resolved through its visible Continue button before the next visible choice.
- Observed priority was preserved: Augment -> Tactical Mission at R6/R12; RunShop after Continue at R5/R11. No panels overlapped and no invisible blocker was observed.
- `runtimeErrors=0`. The audit used EventSystem clicks only; it did not call `StartRound`, use F8/F9, jump rounds, or grant rewards/resources.

### Result and interpretation

- R10: boss warning/spawn name `골렘 군주`, max HP 4,700, actual kill=true; Continue was clicked and R11 began.
- R15: started and cleared at HP 5/10, Gold 193 -> 354, rather than entering the boss sequence at HP 1.
- Direct early failure mechanism before the adjustment was repeated combat leak damage, not an economy/choice-flow lock. The one-point cap preserves every wave, pack, and monster while leaving enough HP for real growth decisions.

- Next task: monitor R16+ separately before changing any later-round or boss tuning.
