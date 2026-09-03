# Pass 8 — Contract Drama & Opening Combat Feedback

Base: `ddc881a` (Pass 7). This pass leaves monster, boss, spawn, gold-income, and shop-price tuning unchanged.

## Opening guidance

`OpeningGuidancePanel` is independent from `BossWarningPanel`: it owns its own GameObject, text fields, CanvasGroup, timer, and sibling order. It is non-interactive (`blocksRaycasts=false`, `interactable=false`), appears once after the actual Lobby -> Battle entry path, and fades over 0.22 seconds after 4.5 seconds.

| Capture | Kicker | Title | Sub |
| --- | --- | --- | --- |
| 0.25s | WARNING | 방어선 돌파 주의! | hidden |
| 1.75s | WARNING | 몬스터가 아래 끝에 도달하면 | HP가 감소합니다 |
| 3.25s | WARNING | 수호자를 소환해 | 막아내세요! |

Actual PlayMode captures are retained in `BatchPlaytestResults/Pass8OpeningGuidance_0.25.png`, `..._1.75.png`, and `..._3.25.png`. The PlayMode stage-state smoke also verified title active/opaque, subtitle visibility, centered alignment, fade, hidden completion, and non-blocking CanvasGroup.

## Tactical contracts

A selected card no longer reinitializes into the older small-reward mission path. Its displayed objective, deadline, contract grade, fixed reward, roulette, and jackpot settings are preserved when selected. Contract reward draws use `RunContentRandomChannel.Mission`; no `UnityEngine.Random` call is used for roulette or jackpot rewards.

There are 18 active primary families with tier-scaled conditions/rewards, producing more than 24 meaningful contract variants across progression. The obsolete `LastStandGambit` remains only as a legacy enum/save-compatible evaluator and is absent from normal draft pools; R0 no longer asks the player to force HP 7 or below.

| Family / tier variants | Grade tendency | Compound objective | Fixed reward / extra |
| --- | --- | --- | --- |
| Gold Reserve I-III | Safe / Challenge | Hold target Gold and survive within the damage allowance | Gold |
| Perfect Defense I-III | Safe | Maintain required board strength and clear without HP loss | Gold + round Gold |
| Merge Rush I-III | Challenge | Merge count plus Rare+ merge result | Gold |
| Role Collector I-III | Challenge | Distinct role composition | Gold + summon discount |
| Lean Defense I-III | Challenge | Small board plus limited HP loss | Gold + roulette |
| Summon Sprint I-III | Challenge | Direct summon target plus limited HP loss | Gold |
| Empty Slot Discipline I-III | Challenge | Empty slots plus Rare+ merge | Gold |
| Rare Lineup I-III | Challenge | Rare+ units plus distinct roles | Gold |
| Monster Hunter I-III | Challenge | Kill target within HP-loss limit | Gold |
| No Summon Hold I-III | Gamble | No direct summon and survive the round | Gold + roulette + summon discount |
| Kill Streak I-III | Gamble | Kill target with zero HP loss | Gold + roulette / jackpot |
| High Grade Forge I-III | Gamble | Rare/Epic/Legendary+ merge by deadline | Gold + roulette / jackpot |
| Spend Down Gambit I-III | Gamble | Spend down Gold and survive | Gold + roulette / jackpot |
| Grade Rainbow I-III | Challenge | Simultaneous distinct-grade board | Gold + summon discount + roulette |
| Boss Preparation I-III | Challenge | Legendary+ preparation before next boss | Gold + round Gold |
| Legendary Hunt I-III | Challenge | Legendary+ unit plus Epic+ merge | Gold + round Gold |
| Boss Slayer I-III | Legend | Kill the next boss | Gold + round Gold + roulette / jackpot |
| Ultimate Recipe Chase I-III | Legend | Ultimate-ready state or final merge before boss | Gold + round Gold + roulette / jackpot |

Draft pool counts are R0=12, R10=15, R20=14, and R30=12. Each draft chooses three distinct cards with category preference and blocks an immediately repeated three-card signature.

### Removed or replaced checklist contracts

- R0 `LastStandGambit` / forced low-HP opening contract: removed from normal draft pools.
- Plain no-damage clear: now requires board strength (`Perfect Defense`).
- Plain merge count: now requires a Rare+ result (`Merge Rush`).
- Plain summon count: now includes a survival limit (`Summon Sprint`).
- Plain empty slots: now includes Rare+ merge progress (`Empty Slot Discipline`).
- Plain grade possession or kill count: now includes role/merge or survival constraints (`Rare Lineup`, `Legendary Hunt`, `Monster Hunter`, `Kill Streak`).

## Damage popup feedback

Normal damage now scales 1.00 -> 1.50 over 0.11s, settles to 1.00 over 0.18s, then rises/fades during its existing 0.72s lifetime. Critical damage uses the same curve to 1.60; healing uses 1.38. The curve runs on `Time.unscaledDeltaTime`. Reuse still resets scale, alpha/color, outline/shadow, position, velocity, and timers before each popup is configured.

## Validation

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: PASS, 0 errors (two pre-existing warnings).
- Unity `DefenseGamePlayModeSmoke`: PASS; `runtimeErrors=0`.
- UI flow: visible Battle -> Result Continue -> Augment -> Tactical Mission -> Battle ordering PASS; one visible choice panel only; invisible blocker absent.
- Opening guidance: dedicated panel and 0.25/1.75/3.25s actual PlayMode captures generated; stage/fade/non-blocking smoke PASS.
- Contract determinism, 20 seeds: PASS. R0 `pool=12, signatures=15, kinds=12`; R10 `15/18/13`; R20 `14/17/13`; R30 `12/10/10`. Same seed also reproduced Mission-channel roulette and jackpot draws.
- Damage frame curve: PASS — normal peak `1.50`, settled `1.00`; critical peak `1.60`, settled `1.00`; heal peak `1.38`.