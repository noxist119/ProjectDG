# Pass 7 — Run Variety, Synergy Depth & Combat Readability

## Scope

- No monster, boss, gold, RunShop price, spawn, or early-balance value was changed.
- Opening guidance, Tactical Mission drafting, combat number presentation, and Board Synergy only.

## Opening guidance

The existing Boss Warning panel is reused. Each opening step forces the panel active, alpha 1, no raycast blocking, and final sibling order before replacing title/subtitle. `WARNING` remains on the kicker. The three steps are 1.5 seconds each, then use the existing short fade-out.

## Tactical Mission bracket draft

| Bracket | Pool size | Included mission families |
| --- | ---: | --- |
| R0-R9 | 12 | PerfectDefense, SummonSprint, LastStandGambit, MergeRush, RoleCollector, LeanDefense, EmptySlotDiscipline, RareUpgrade, MonsterHunter, NoSummonHold, KillStreak, GoldReserve |
| R10-R19 | 15 | PerfectDefense, SummonSprint, MergeRush, RoleCollector, LeanDefense, BossPreparation, EmptySlotDiscipline, RareUpgrade, LegendaryHunt, MonsterHunter, NoSummonHold, KillStreak, HighGradeForge, SpendDownGambit, GradeRainbow |
| R20-R29 | 14 | PerfectDefense, MergeRush, RoleCollector, BossPreparation, RareUpgrade, LegendaryHunt, MonsterHunter, BossSlayer, NoSummonHold, KillStreak, HighGradeForge, SpendDownGambit, UltimateRecipeChase, GradeRainbow |
| R30+ | 12 | BossPreparation, LegendaryHunt, MonsterHunter, BossSlayer, NoSummonHold, KillStreak, HighGradeForge, SpendDownGambit, UltimateRecipeChase, GradeRainbow, RoleCollector, MergeRush |

The Mission RNG channel selects three distinct offers. It prefers distinct SAFE/TEMPO/GREED/BUILD categories, preserves the existing eligibility/cooldown restrictions, never falls back across a bracket, and retries a same-as-previous three-card signature within the current bracket.

## Synergy audit

| Existing duplicate-count structure | Pass 7 result |
| --- | --- |
| Role 2/4/6 and tag 2/4 thresholds | Removed from runtime application; duplicate units no longer advance these conditions. |
| Triple Barrier | New: 3 distinct Vanguard IDs. |
| Life Cycle | New: 3 distinct definitions with `HealLowestAllies`; self-heal and Support proxy are excluded. |
| Grade Lineage | New: distinct Normal + Rare + Epic + Legendary definitions. |
| Ranger/Mage, Assassin/Legendary, tag resonance, Prism formation | Replaced with distinct-ID combination conditions. |

Active rows are first in the existing expanded panel. Up to three closest locked conditions follow with `progress/target`; only the highest tag resonance stage applies and displays.

## Combat-number presentation

- Numbers use the shared screen-space combat canvas rather than a target HUD child, so kill-hit popups survive target destruction.
- Normal: white, bold, 36px, dark outline/shadow.
- Critical: red, bold, 48px, 1.42x peak scale.
- Heal: green, bold, 34px.
- Pooled motions reset scale/color/alpha and perform 0.08s pop, 0.12s settle, rise/fade through 0.72s.
- Existing loaded GROBOLD/Baloo2-ExtraBold is preferred; the runtime default remains safe fallback when neither is loaded.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: PASS; 0 errors (existing unrelated warnings only).
- Unity PlayMode `DefenseGamePlayModeSmoke`: PASS; `runtimeErrors=0`.
- P2U opening sequence, P2W toast, and P2X EventSystem choice-flow checks: PASS.
- Pass 7 Mission RNG validation (20 seeds): PASS — R0 pool 12 / 15 unique three-card signatures / 12 offered kinds; R10 15 / 18 / 13; R20 14 / 17 / 13; R30 12 / 10 / 10. Every repeated seed reproduced exactly and every draft contained three distinct choices.
- Persistent EventSystem UI run, Overdrive: R1-R10 cleared through visible Battle/Continue/choice buttons. R10 displayed and spawned `골렘 군주` (4,981.9995 HP), was not killed in this particular run (98.78% remaining at result), Continue was clicked, and R11 started. R12 ended in gameplay defeat at HP 0; there was no invisible blocker and `runtimeErrors=0`.

The R12 defeat is recorded as gameplay data only. Pass 7 intentionally makes no balancing response.