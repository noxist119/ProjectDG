# CODEX STATUS

- Latest completed pass: Pass 5 — Boss Stability and Choice Progression Finalization.
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

## Pass 4 — Midgame R16-R30 Core Loop Balance

- Scope: EventSystem UI-path audit from R1 through R30 plus Editor-only telemetry. No production balance value was changed: Pass 3 Overdrive `roundLeakDamageCap=1` remains in place, and Classic, R1-R15, R30+, economy, rewards, and boss presentation are untouched.
- Decision: no midgame adjustment was warranted. The same UI route cleared every R16-R30 round and both midgame bosses, so there was no repeated gameplay defeat to calibrate.

### Actual UI audit — R16-R30 Overdrive

| Round | Type | HP start/end | Gold start/end | Board at start | Summons / merges | Choice flow after result | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| R16 | Horde | 6 / 6 | 118 / 314 | N11 R1 | 11 / 0 | Augment -> Tactical Mission later | Cleared |
| R17 | Regular | 6 / 6 | 321 / 539 | N11 R1 | 11 / 0 | Tactical Mission later | Cleared |
| R18 | Regular | 6 / 6 | 151 / 381 | N11 R1 | 11 / 0 | Tactical Mission later | Cleared |
| R19 | Horde | 6 / 6 | 389 / 635 | N11 R1 | 11 / 0 | RunShop later | Cleared |
| R20 | Boss | 6 / 6 | 73 / 239 | N11 R1 | 11 / 0 | Augment -> Tactical Mission later | Cleared |
| R21 | Regular | 6 / 6 | 248 / 452 | N11 R1 | 11 / 0 | Tactical Mission later | Cleared |
| R22 | Horde | 6 / 6 | 461 / 731 | N11 R1 | 11 / 0 | Tactical Mission later | Cleared |
| R23 | Regular | 6 / 6 | 740 / 1023 | N11 R1 | 11 / 0 | Tactical Mission later | Cleared |
| R24 | Regular | 6 / 6 | 1012 / 1266 | N11 R2 | 12 / 0 | Augment -> Tactical Mission later | Cleared |
| R25 | Horde | 6 / 6 | 1276 / 1651 | N11 R2 | 12 / 0 | Tactical Mission later | Cleared |
| R26 | Regular | 6 / 6 | 1662 / 2004 | N11 R2 | 12 / 0 | Tactical Mission later | Cleared |
| R27 | Regular | 6 / 6 | 2015 / 2311 | N11 R2 | 12 / 0 | RunShop later | Cleared |
| R28 | Horde | 6 / 6 | 2322 / 2723 | N11 R2 | 12 / 0 | Augment -> Tactical Mission later | Cleared |
| R29 | Regular | 6 / 6 | 2735 / 3115 | N11 R2 | 12 / 0 | Tactical Mission later | Cleared |
| R30 | Boss | 6 / 6 | 3127 / 3425 | N11 R2 | 12 / 0 | — | Cleared |

### Boss, UI, and error evidence

- R20: `사자왕 레온`, maximum HP 1822.464, killed=true, remaining HP 0%.
- R30: `신비의 마법사`, maximum HP 2484.662, killed=true, remaining HP 0%. The run completed R30 at HP 6/10 and Gold 3425.
- All sampled post-round states used `BlockingChoiceReason=None`; only the visible result panel remained before its Continue action. No overlapping selection panel or invisible blocker was observed.
- Pass 2X Unity PlayMode Smoke: PASS, `runtimeErrors=0`, including its UI-flow coverage.
- Persistent R30 runner: gameplay completed, but its strict runtime log recorded 9 errors from the existing `Boss_Magician_Rig` animation events `StopEffectKey` with no receiver. This is not a midgame balance or UI-flow failure. `StopEffectKey` remains deleted per the existing compatibility-event constraint, so it was not reintroduced in this pass.

- Next task: resolve the separately tracked R30 animation-event error only if its deletion constraint is revised; otherwise use the recorded R16-R30 outcome as the midgame balance baseline.

## Pass 5 — Boss Stability and Choice Progression Finalization

- Root cause fixed: StopEffectKey was an orphaned AnimationEvent in the source FBX importer metadata for Boss_Magician_skill01 (2), skill02 (1), and skill03 (2). The five event entries were removed from the animation source metadata. No receiver method or compatibility component was recreated.
- Actual EventSystem UI run: selected a Tactical Mission at R2, clicked the real Normal merge card and completed one merge, then observed the selected mission settle as failed at R3 and resolved the following Tactical Mission offer with its visible Later button. Augment choices were selected at R6/R12/R16/R20/R24/R28. RunShop was visibly closed with Later at R11/R19/R27; BattleButton then started the next rounds.
- Boss results: R10 골렘 군주 (4700 HP), R20 사자왕 레온 (1822.464 HP), and R30 신비의 마법사 (2484.662 HP) each showed their warning title, spawned, were killed, and completed their result Continue flow. The run completed R30 at HP 2/10 and Gold 2758.
- Validation: persistent UI runner PASS, runtimeErrors=0, StopEffectKey log hits=0, no invisible blocker, and no overlapping/hidden choice panel observed. Pass 2X Unity PlayMode Smoke PASS with runtimeErrors=0.
- Gameplay balance, rewards, economy, spawns, and difficulty: unchanged.

- Latest completed pass: Pass 5 — Boss Stability and Choice Progression Finalization.
- Next task: Start the next user-requested ProjectDG pass.