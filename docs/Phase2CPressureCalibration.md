# Pass 2C - R6-R15 Pressure Localization & Overdrive Horde Calibration

## 범위

- 기준: Pass 2B 이후 최신 main.
- 변경한 게임플레이 수치는 Overdrive `hordeCountMultiplier` 한 가지뿐이다: **1.32 → 1.20**.
- `regularCountMultiplier = 1.26`, Horde 주기(R4부터 3라운드마다), Horde HP/ATK, Boss, Classic, 소환/골드/상점/등급 강화/미션/레시피는 변경하지 않았다.
- 결과 파일: `BatchPlaytestResults/DefenseGame_Phase2_Classic_R30.json`, `BatchPlaytestResults/DefenseGame_Phase2_Overdrive_R30.json`.
- 두 측정 모두 paired seed, 12 runs, target R30, 기존 human-like 3전략(summon-heavy / balanced / shop-save)으로 실행했다.

## 추가 텔레메트리

각 run은 R3, R5, R6, R7, R8, R9, R10, R11, R12, R15, R20, R30에서 다음을 기록한다.

- Life, Gold, 보드 유닛 수/용량, 최고 보유 등급, 누적 직접 소환/합성/등급 강화 레벨, 소환 비용
- 해당 라운드 Target 수, Horde/Boss/Mid-boss 여부
- 게임플레이 패배 라운드와 technical failure 라운드(서로 분리)
- 런 누적 누수 피해, 라운드별 누수 피해, 라운드별 탈출 몬스터 수

이 값들은 측정 전용이며 누수 처리나 전투 결과에 영향을 주지 않는다.

## 측정 사실

### 최종 요약

| Mode | Runs | R10 boss clear | R30 reach | Gameplay defeats | Technical failures | Runtime-error runs | Softlocks | Avg. reached | Avg. end life | Avg. end gold | Boss A/C/F | Total leak damage |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Classic | 12 | 4 / 11 attempts (36.4%) | 0 / 12 | 11 | 1 (timeout) | 0 | 0 | 10.75 | 0.33 | 53.50 | 11 / 4 / 7 | 150 |
| Overdrive | 12 | 0 / 9 attempts (0.0%) | 0 / 12 | 12 | 0 | 0 | 0 | 10.08 | 0.00 | 65.17 | 9 / 0 / 9 | 148 |

Classic의 유일한 technical failure는 R10 timeout 1건이다. 따라서 Classic의 R10 도달 스냅샷은 11건이고, Overdrive는 R10 이전에 패배한 3건을 제외한 9건이다.

### 체크포인트 평균

`도달`은 해당 checkpoint snapshot이 실제로 기록된 run 수다. `등급`은 CharacterGrade enum 평균(Normal=0, Rare=1 등)이다.

| Mode | R | 도달 | Life | Gold | Board / Cap | 등급 | 소환 | 합성 | 강화 Lv | 소환비 | Target | Horde / Boss / Mid-boss |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Classic | 3 | 12/12 | 5.17 | 31.83 | 4.67 / 10.00 | 0.17 | 4.58 | 0.17 | 0.08 | 14.58 | 6.00 | 0 / 0 / 0 |
| Classic | 5 | 12/12 | 2.67 | 47.25 | 5.92 / 10.00 | 0.42 | 6.33 | 0.42 | 0.67 | 16.33 | 9.00 | 0 / 0 / 0 |
| Classic | 6 | 12/12 | 3.58 | 56.42 | 7.17 / 10.00 | 0.42 | 7.58 | 0.42 | 1.08 | 18.75 | 7.00 | 0 / 0 / 0 |
| Classic | 7 | 12/12 | 3.25 | 64.92 | 7.50 / 10.92 | 0.83 | 8.42 | 0.67 | 1.75 | 19.33 | 9.25 | 0 / 0 / 0 |
| Classic | 8 | 11/12 | 3.45 | 72.27 | 7.36 / 11.00 | 0.91 | 9.18 | 1.09 | 2.45 | 19.18 | 9.91 | 0 / 0 / 0 |
| Classic | 9 | 11/12 | 3.45 | 67.82 | 7.36 / 11.00 | 1.09 | 10.27 | 1.64 | 3.18 | 20.27 | 10.00 | 0 / 0 / 0 |
| Classic | 10 | 11/12 | 1.36 | 86.18 | 5.55 / 11.00 | 1.18 | 11.45 | 2.09 | 3.82 | 21.45 | 10.00 | 0 / 11 / 0 |
| Classic | 11 | 6/12 | 0.83 | 96.83 | 6.33 / 11.00 | 1.33 | 12.67 | 2.33 | 5.00 | 22.67 | 19.00 | 0 / 0 / 6 |
| Classic | 12 | 3/12 | 1.00 | 131.67 | 7.00 / 11.00 | 1.33 | 12.33 | 2.33 | 7.67 | 22.33 | 22.00 | 0 / 0 / 0 |
| Overdrive | 3 | 12/12 | 5.50 | 32.67 | 5.00 / 10.00 | 0.50 | 5.00 | 0.33 | 0.33 | 15.00 | 8.00 | 0 / 0 / 0 |
| Overdrive | 5 | 12/12 | 2.83 | 49.50 | 5.67 / 10.00 | 0.92 | 7.33 | 1.17 | 0.83 | 17.33 | 9.00 | 0 / 0 / 0 |
| Overdrive | 6 | 12/12 | 3.42 | 55.42 | 7.00 / 10.00 | 0.92 | 8.67 | 1.17 | 1.08 | 19.08 | 9.00 | 0 / 0 / 0 |
| Overdrive | 7 | 10/12 | 3.30 | 71.50 | 7.50 / 10.90 | 1.00 | 9.30 | 1.20 | 1.80 | 19.70 | 14.40 | 10 / 0 / 0 |
| Overdrive | 8 | 9/12 | 3.44 | 73.67 | 7.56 / 11.00 | 1.22 | 10.56 | 1.78 | 2.44 | 20.33 | 11.78 | 0 / 0 / 0 |
| Overdrive | 9 | 9/12 | 3.44 | 78.78 | 7.56 / 11.00 | 1.56 | 11.67 | 2.33 | 3.22 | 21.44 | 13.00 | 0 / 0 / 0 |
| Overdrive | 10 | 9/12 | 1.56 | 76.89 | 4.00 / 11.00 | 1.56 | 12.67 | 2.89 | 4.11 | 22.44 | 11.00 | 0 / 9 / 0 |
| Overdrive | 11 | 5/12 | 1.00 | 87.80 | 5.00 / 11.00 | 1.60 | 15.20 | 3.40 | 3.80 | 24.80 | 24.00 | 0 / 0 / 5 |
| Overdrive | 12 | 2/12 | 2.00 | 87.50 | 7.00 / 11.00 | 1.50 | 17.00 | 3.00 | 5.00 | 27.00 | 28.00 | 0 / 0 / 0 |
| Overdrive | 15 | 1/12 | 3.00 | 213.00 | 7.00 / 12.00 | 2.00 | 23.00 | 5.00 | 8.00 | 33.00 | 37.00 | 0 / 0 / 0 |

R20/R30은 두 모드 모두 도달 snapshot이 0건이다.

### 패배 라운드 분포

| Mode | Gameplay defeat histogram | Technical failure histogram |
| --- | --- | --- |
| Classic | R7: 1, R10: 4, R11: 3, R13: 3 | R10: 1 timeout |
| Overdrive | R6: 2, R7: 1, R10: 4, R11: 3, R12: 1, R17: 1 | 없음 |

### 라운드별 누수 기록

| Mode | R1 | R2 | R3 | R4 | R5 | R6 | R7 | R8 | R10 | R11 | R12 | R13 | R15 | R16 | R17 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Classic 피해 / 탈출 | 22/22 | 21/21 | 15/15 | 11/11 | 19/19 | 1/1 | 10/12 | 1/1 | 32/8 | 13/12 | 2/2 | 3/3 | - | - | - |
| Overdrive 피해 / 탈출 | 18/18 | 18/18 | 22/27 | 16/16 | 16/17 | 8/10 | 15/21 | 2/3 | 18/11 | 10/25 | 1/1 | - | 1/1 | 2/6 | 1/1 |

### 전략별 실제 결과

| Mode | Strategy | Runs | R10 clears | Gameplay defeats | Avg. reached | Avg. life | Avg. gold | Summons | Merges | Grade upgrades | Leak damage |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Classic | summon-heavy | 4 | 0 | 4 | 10.50 | 0.00 | 53.25 | 63 | 15 | 11 | 52 |
| Classic | balanced | 4 | 0 | 3 | 9.25 | 1.00 | 61.75 | 37 | 4 | 14 | 47 |
| Classic | shop-save | 4 | 0 | 4 | 12.50 | 0.00 | 45.50 | 51 | 10 | 31 | 51 |
| Overdrive | summon-heavy | 4 | 0 | 4 | 10.00 | 0.00 | 35.75 | 62 | 17 | 11 | 48 |
| Overdrive | balanced | 4 | 0 | 4 | 11.00 | 0.00 | 82.00 | 61 | 12 | 19 | 54 |
| Overdrive | shop-save | 4 | 0 | 4 | 9.25 | 0.00 | 77.75 | 43 | 8 | 19 | 46 |

### Grade Upgrade / 미션 / 레시피

| Mode | Grade upgrades | N / R / E / L / M / T | Empty attempts | Mission choices / completions | Shop purchases | Ultimate Recipe merges |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| Classic | 56 | 36 / 20 / 0 / 0 / 0 / 0 | 0 | 84 / 23 | 8 | 0 |
| Overdrive | 49 | 32 / 16 / 1 / 0 / 0 / 0 | 0 | 79 / 18 | 7 | 0 |

첫 Grade Upgrade 라운드(업그레이드하지 않은 run은 R0): Classic `R7, R5, R6, R4, R5, R6, R6, R7, R4, R7, R5, R3`; Overdrive `R7, R4, R3, R4, R3, R8, R3, R7, R5, R0, R5, R3`.

### R10 boss-start snapshot

| Mode | Snapshot count | Life avg. | Gold avg. | Board avg. | Highest grade avg. | Target avg. | Summons avg. | Merges avg. | Grade levels avg. | Summon cost avg. |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Classic | 11 | 3.45 | 24.55 | 7.64 / 11 | 1.18 | 10.00 | 11.45 | 2.09 | 3.82 | 21.45 |
| Overdrive | 9 | 3.44 | 25.89 | 7.44 / 11 | 1.56 | 11.00 | 12.67 | 2.89 | 4.11 | 22.44 |

## 해석 (측정값과 분리)

- Overdrive는 horde count 1.20 적용 뒤에도 R6과 R7에서 각각 2건과 1건의 게임플레이 패배가 기록됐다. R7 snapshot 10건 중 10건이 Horde로 기록되었고 Target 평균은 14.40이다.
- 양 모드 모두 R10 boss 직전 snapshot에서 Life 평균이 약 3.4이며, 보스 종료 후 R11 도달 수가 Classic 6/12, Overdrive 5/12로 줄었다. 이 표는 압박 지점의 위치를 보여 주지만 원인을 단정하지 않는다.
- Overdrive는 R10 보스 처치가 0/9이고, Classic은 4/11이다. 이 보고서는 이를 사실로 기록할 뿐 Boss/유닛/경제 수치 변경을 제안하거나 수행하지 않는다.
- 양 모드 모두 R30 도달은 0/12이며 Ultimate Recipe merge도 0건이다. Pass 2C는 이 결과를 바꾸기 위한 추가 조정을 하지 않는다.

## 검증

- `Assembly-CSharp-Editor.csproj --no-restore`: 경고 0, 오류 0.
- Unity batch: Classic 12/12, Overdrive 12/12 최종 JSON 생성 완료.
- 두 batch 결과의 runtime-error run은 0, softlock은 0이다.
- Gameplay balance 변경은 Overdrive `hordeCountMultiplier` 1.32 → 1.20 외에는 없다.
