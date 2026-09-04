# CODEX STATUS

- Latest completed pass: Pass 6 — Gold to Power Progression Finalization.
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

- Latest completed pass: Pass 6 — Gold to Power Progression Finalization.
- Next task: Start the next user-requested ProjectDG pass.

## Pass 6 — Gold to Power Progression Finalization

- Scope: actual EventSystem UI-path R1-R30 validation, then one evidence-based revalidation. No production economy, reward, spawn, combat, or UI balance value was changed.
- Baseline evidence: Pass 5 completed R30 at 2 HP and 2,758 Gold with board N9/R4/E0, 14 summons, and 1 merge. This showed that the standard conservative action policy left late Gold unused rather than that an existing power sink was unavailable.
- First Pass 6 UI spending probe: four consecutive free merge-card clicks after R10 reduced the board from 11 units to 4. That run reached R15 but was defeated at 0 HP / 110 Gold. The direct failure mechanism was lost board coverage after the repeated merges, not insufficient Gold or a blocked UI flow.
- Final actual UI revalidation: preserved the board and spent Gold through the existing grade-upgrade buttons first. It made 27 grade-upgrade purchases and 1 high-grade-chance purchase; the existing RunShop offer was also bought through its visible button at R5. R30 finished at 2 HP and 273 Gold with N9/R2/E2 (13 units), 14 player summons, and 0 merges.

| Record | R30 HP / Gold | R30 board | Summons / merges | Post-R10 Gold-to-power actions |
| --- | --- | --- | --- | --- |
| Pass 5 baseline | 2 / 2,758 | N9/R4/E0 | 14 / 1 | No dedicated post-R10 spending pass |
| Pass 6 final | 2 / 273 | N9/R2/E2 | 14 / 0 | 27 grade upgrades + 1 high-grade chance upgrade |

- Boss record: R20 was cleared with its displayed boss title and 0% remaining HP (max 1,822.464). R30 was cleared with its displayed boss title and 0% remaining HP (max 2,484.662). R10 warning/spawn was shown at 4,700 max HP; the recorded R10 boss health remaining was 11%, while the round completed and R11 began.
- Validation: runtimeErrors=0, no Invisible Blocker observed, and existing Pass 2X Unity PlayMode Smoke PASS (opening guidance, mission toast, augment-to-mission ordering, single-panel flow, and visible battle progression).
- Economy conclusion: no numeric production adjustment is justified by this evidence. Existing grade upgrades and high-grade chance investment are functional Gold sinks when selected through the visible UI; lowering or raising economy values would be speculative. The only code addition is the Editor-only Pass 6 validation path that exercises those real buttons.

- Next task: Start the next user-requested ProjectDG pass.
## Pass 7 — Run Variety, Synergy Depth & Combat Readability

- Opening guidance now reasserts panel activation, alpha, non-blocking CanvasGroup, and final sibling order for each 1.5-second step. Unity P2U/P2X Smoke PASS, runtimeErrors=0.
- Tactical Missions now use the deterministic Mission RNG channel with R0-R9/R10-R19/R20-R29/R30+ bracket pools, category preference, per-bracket fallback, and no consecutive identical three-card set.
- Damage numbers are independent pooled shared-canvas popups: white normal, red critical, green healing, pop/settle/rise/fade, and kill-hit persistence.
- Synergies now count distinct CharacterDefinition IDs. Added Triple Barrier, Life Cycle, Grade Lineage, Elemental Barrage, Shadow Crown, Tag Resonance, and Prism Formation; expanded panel shows active entries followed by closest locked progress.
- Actual Overdrive EventSystem UI run reached R10 warning/spawn and R11, then ended in a recorded R12 gameplay defeat (not a UI lock); runtimeErrors=0. No monster, boss, economy, shop, or spawn values changed.
- Full detail: `docs/Pass7RunVarietySynergyCombatReadability.md`.
- Next task: use the Pass 7 R12 gameplay evidence to decide whether a separate balance pass is warranted.
## Pass 8 — Contract Drama & Opening Combat Feedback

- Opening guidance is now its own non-blocking `OpeningGuidancePanel`; `BossWarningPanel` remains reserved for real boss arrivals. Actual PlayMode captures were generated at 0.25s, 1.75s, and 3.25s under `BatchPlaytestResults/Pass8OpeningGuidance_*.png`; the title/subtitle sequence, fade, centered layout, and input pass-through all passed.
- Tactical contracts preserve the displayed tier, deadline, fixed reward, roulette, and jackpot after selection. R0 excludes legacy low-HP `LastStandGambit`; the active 18 contract families are tier-scaled into 24+ variants, with R0/R10/R20/R30 pools of 12/15/14/12.
- Contract roulette/jackpot uses the seeded Mission RNG channel. 20-seed validation passed with draft signatures/kinds R0 15/12, R10 18/13, R20 17/13, R30 10/10.
- Damage popup curve: normal 1.00 -> 1.50 -> 1.00, critical 1.00 -> 1.60 -> 1.00, heal peak 1.38. Frame-curve validation PASS.
- Validation: `Assembly-CSharp-Editor` build PASS (0 errors), full Unity PlayMode Smoke PASS, Pass 2X UI flow PASS, `runtimeErrors=0`, no invisible blocker.
- Full detail: `docs/Pass8ContractDramaOpeningCombatFeedback.md`.
- Next task: Start the next user-requested ProjectDG pass.

## Pass 13 — R1-R20 Full Run Integration Validation

- Scope: persistent Unity PlayMode validation through EventSystem UI clicks only. No `StartRound`, debug round jump, or reward/resource fixture was used.
- Initial flow: opening tutorial completed, then the R0 three-card Tactical Mission panel auto-opened. Its visible `Later` button closed it once; BattleButton was interactable and the panel did not reopen before R1.
- R1-R6 actual Overdrive record: R1 cleared HP 10->7 / Gold 7->16 (MainBGM); R2 cleared 7->5 / 6->20 (Continue -> Tactical Later); R3 cleared 5->3 / 9->28 (Continue -> Tactical Later); R4 Horde cleared 3->1 / 17->50 (Continue -> RunShop purchase -> Tactical Later); R5 cleared 1->1 / 3->35; R6 gameplay defeat 1->0 / 26->55.
- R7-R10 and R11-R20 were not reached. This was a recorded gameplay defeat, not an environment, visible-choice, or stale-BattleButton failure; `runtimeErrors=0` and no invisible blocker was observed. No balance change was made in this integration pass.
- Full Unity PlayMode Smoke: PASS, runtimeErrors=0. Pass10 BGM mapping, Pass11 monster exit/leak integrity, Summon Grade Luck, initial Tactical Mission flow, opening guidance, and Pass2X UI flow all passed.
- Test-only change: the persistent runner has a Pass 13 R1-R20 mode that waits for and resolves the actual initial auto-open before starting R1, and records active BGM clip names in round snapshots.
- Next task: treat the R6 defeat as balance evidence only if a separate balance pass is requested; do not classify it as a UI-flow blocker.

## Pass 14 — Early Game Balance After Per-Monster Leak

- Scope: EventSystem UI-click validation only. No direct `StartRound`, debug shortcut, round jump, or resource fixture was used. Normal-monster leaks remain one HP per monster; no per-round leak cap was restored.
- Evidence before the R1-R5 tuning: the Area Stability run died in R9 and the Luck Investment run died in R6/R7, with `runtimeErrors=0`. The direct cause was accumulated regular-wave leak damage, not a choice-panel or BattleButton block.
- Single balance axis changed: `RoundManager.ApplyPreBossLeakEaseToRegularCount` now removes 4 regular monsters from the pre-profile R1-R5 count and retains the existing one-monster ease for R6-R9. R10+ and boss/support counts, health, rewards, contracts, and Luck costs are unchanged.

### Final actual UI records

| Strategy | R1-R9 HP end sequence | R10 result | Board / summons at R10 | Gold at R10 start/end | Errors |
| --- | --- | --- | --- | --- | --- |
| Area Stability | 9, 8, 7, 6, 6, 6, 6, 6, 5 | Boss warning and spawn observed; defeated in the boss round (HP 5 -> 0) | 8 / 7 | 43 -> 79 | 0 |
| Luck Investment | 9, 8, 7, 6, 5, 3, 0 | Defeated in R7 before R10 | 6 / 5 at R7 | 89 -> 129 | 0 |

- Area Stability used visible summoning first, filled to eight board units by R6, selected the visible R6 Augment, and used the Normal grade-upgrade button at R6/R7/R8. It reached the R10 boss starting with HP 5, satisfying the required minimum R10-boss-entry evidence without changing boss values.
- Luck Investment used the visible minimal-board summons and attempted the visible high-grade-chance upgrade only after stabilizing. It did not reach the 100G first purchase before the R7 defeat, so this real-run sample records no Luck level purchase rather than claiming an unobserved early Epic conversion. The full Unity Smoke independently passed `pass2MSummonGradeLuckValid`, including the early Epic conversion/cost regression.
- Final full Unity PlayMode Smoke: PASS (`status=pass`, `runtimeErrors=0`); Pass10 round-BGM rotation, Pass11 exit/leak integrity, Summon Grade Luck, and Pass2X UI flow all passed.
- Next task: R10 boss clear pressure remains a separate boss-calibration question; Pass 14 changed no R6+ or boss value.
## Pass 15 — Player-Like Strategy Validation

- Added only an Editor-only policy runner. It uses actual EventSystem button clicks after test setup: Stable Board targets eight units, merges Normal only at three units, and uses Normal grade upgrades; Contract First ranks visible Tactical Mission offer tags; High Grade Investment targets six units before attempting visible high-grade chance Lv.1–Lv.3.
- The runner records each visible mission offer, selected option, policy tag, settlement outcome, and observed Gold/board delta. It also records R10/R11 snapshots, rare-or-better composition, merges, upgrades, UI block state, and runtime errors.
- Six fixed seed entry points are prepared: Stable 101/102, Contract 201/202, High Grade 301/302. Seed configuration is test-only and precedes all player actions; no resources, units, rewards, round jumps, or combat outcomes are injected.
- Actual Unity execution is currently BLOCKED, not passed: the first Stable 101 run repeatedly emitted the pre-existing DefenseBoardManager slot invariant warning (defender has no matching BoardSlot) and did not reach runner completion or emit a Pass 15 JSON. The test-only Unity process was stopped after the runner’s wall-clock safety timeout did not complete. Therefore no six-seed result, success rate, R10 conclusion, or runtimeErrors=0 claim is recorded.
- Production gameplay/balance/UI code changed: NONE. No boss HP, early wave, leak, reward, economy, or combat value was modified.
- Next task: resolve or isolate the BoardSlot invariant warning in a separately authorized diagnostics pass, then rerun the six policy validations before drawing balance conclusions.