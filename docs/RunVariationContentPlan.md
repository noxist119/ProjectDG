# DefenseGame 런 변동성 컨텐츠 설계

작성일: 2026-06-06

목표: 반복 소환/합성만으로 진행되는 느낌을 줄이고, 매 판마다 다른 목표와 선택을 제공한다. 초반에는 내가 강하다는 기분을 유지하고, 중반부터는 미션, 럭키 이벤트, 레시피, 보스 패턴, 전투 상점, 타일 효과 때문에 덱/배치/성장 판단이 달라지게 만든다.

## 1. 라운드 미션

적용 위치: `Assets/Scripts/DefenseGame/TacticalMissionSystem.cs`

이미 추가한 방향:

| 미션 | 핵심 재미 | 보상 감각 |
| --- | --- | --- |
| 봉인된 지갑 | 다음 라운드 동안 소환 금지 | 룰렛 골드 + 소환비 할인 |
| 처치 콤보 | 체력 손실 없이 처치 수 달성 | 룰렛 + 잭팟 |
| 고등급 도박 | 제한 시간 안에 합성으로 레어/희귀/전설 이상 뽑기 | 큰 룰렛 + 잭팟 |
| 올인 운영 | 골드를 거의 남기지 않고 라운드 클리어 | 큰 룰렛 |
| 레시피 추적 | 초월 레시피 재료 완성 또는 초월 합성 성공 | 고액 룰렛 + 잭팟 |
| 등급 무지개 | 서로 다른 등급을 동시에 보유 | 룰렛 + 소환비 할인 |

운영 기준:

- 단순 처치/소환/합성보다 "참기", "올인", "고등급 뽑기", "레시피 조립"처럼 플레이 방식을 바꾸는 미션을 우선한다.
- 미션 완료 즉시 지급하지 않고, 라운드 클리어 정산으로 보상을 몰아서 도파민을 만든다.
- 보상에는 고정 골드 외에 룰렛 골드, 잭팟 확률, 소환비 할인, 라운드 보너스를 섞는다.

추가 후보:

| 미션 | 조건 | 보상 |
| --- | --- | --- |
| 보스 전야제 | 보스 전 라운드까지 특정 역할 3종 보유 | 보스 피해 증강체 후보 확정 등장 |
| 마지막 한 칸 | 빈 슬롯 1칸 이하로 라운드 클리어 | 대량 골드 룰렛 |
| 역배팅 | 낮은 등급 유닛 수를 유지한 채 클리어 | 희귀 이상 확률 버프 |
| 생존 보험 | 체력 1~2를 잃고 클리어 | 다음 라운드 보호막 |
| 스킬 폭죽 | 한 라운드 안에 스킬 n회 사용 | 마나 관련 보상 |

## 2. 럭키 이벤트 / 증강체

적용 위치: `Assets/Scripts/DefenseGame/AugmentManager.cs`

현재는 증강체 선택 시스템을 럭키 이벤트의 1차 형태로 사용한다.

추가한 선택지 방향:

| 선택지 | 효과 | 감각 |
| --- | --- | --- |
| 도파민 버튼 | 즉시 1~96G 랜덤 | 한 번에 판이 열릴 수 있는 버튼 |
| 미니 로또 | 라운드 클리어마다 0~28G 랜덤 | 매 라운드 정산 기대감 |
| 희귀 현상금 | 처치 시 낮은 확률로 큰 골드 | 몬스터가 죽을 때 작은 복권 |
| 보스 복권 | 보스 클리어 시 8~88G | 보스 라운드 기대감 |
| 쿠폰 폭풍 | 소환비 16% 감소 | 소환을 많이 누르는 판 |

다음 개선:

- 럭키 이벤트 전용 UI를 만들면 `안전`, `도박`, `계약` 세 가지 선택지로 보여준다.
- 계약형 선택지는 "이번 라운드 몬스터 강화, 대신 보상 강화"처럼 리스크를 명확하게 보여준다.
- 실패해도 너무 허무하지 않게 소량 보정 보상을 둔다.

## 3. 레시피 소환

적용 위치: `Assets/Scripts/DefenseGame/DefenseBoardManager.cs`

### Transcendent Recipe Rule

- 모든 초월 레시피는 하나의 고정된 초월 결과만 생성한다.
- 레시피 결과 RNG, 복수 결과 배열, 등급 수량만으로 결과를 뽑는 Overflow 레시피는 사용하지 않는다.
- Recipe materials only satisfy the summon condition; they are not stat inheritance inputs.
- Material tile bonuses, synergy bonuses, temporary buffs, and combat stats are never copied to the result.
- The same Transcendent character has the same base combat stats at the same outgame growth state.
- Material count never raises result attack or health automatically; character power and recipe difficulty are tuned separately.
- Stronger Transcendent characters are balanced through harder fixed recipes.
- 같은 재료 조합은 모든 런에서 같은 초월 유닛을 생성한다.
- 고급 초월 유닛은 이후 별도의 고정 레시피로만 추가한다.
- 인게임에는 현재 보드 재료와 연결된 레시피만 표시한다.

현재 구현된 영웅 ID 기준으로 레시피를 재정리했다. 영웅 번호는 오른쪽 기획표의 `프리팹이름 -> 변경된영웅번호` 기준으로 재배치했으며, 빈 예약 슬롯은 레시피 재료로 쓰지 않는다. 현재 초월 결과군은 `hero_51~hero_57`이며 이후 `hero_58~hero_100` 구간으로 확장한다.

| 레시피 | 필요 영웅 | 결과 | 컨셉 |
| --- | --- | --- | --- |
| Fever Engine Rite | hero_31 + hero_13 + hero_10 | hero_53 | 전투/마나/공격속도 빌드 |
| Volcanic Core Rite | hero_32 + hero_01 + hero_09 | hero_52 | 늑대/광역/전방 관통 빌드 |
| Soul Battery Rite | hero_11 + hero_13 + hero_02 + hero_05 + hero_14 | hero_54 | 흡혈/마나/힐/방어막/속도 빌드 |
| Thunder Control Rite | hero_31 + hero_07 + hero_08 | hero_51 | 전투/사신/석화 제어 빌드 |
| Iron Bastion Rite | hero_31 + hero_05 | hero_55 Dice Armor | 전투/방패 방어 빌드 |
| Clockwork Barrage Rite | hero_32 + hero_13 | hero_56 Dice Auto | 늑대/배터리 자동 폭격 빌드 |
| Fractured Arsenal Rite | hero_33 + hero_12 | hero_57 Dice Broken | 감염/암살 변칙 사격 빌드 |

다음 개선:

- 도감/전투 UI에 레시피 진행도 표시: `2/3`, `3/4`.
- 레시피 재료 유닛 머리 위에 작은 재료 표시.
- 레시피 완성 가능 시 합성 버튼을 별도 연출.
- 레시피 결과는 현재는 사용 가능한 상위 등급 후보로 연결하고, 이후 `hero_31+` 신화와 `hero_51+` 초월 영웅이 추가되면 특정 신화/초월 영웅으로 연결하는 것이 최종 목표.

## 4. 보스 패턴

적용 위치 후보:

- `Assets/Scripts/DefenseGame/MonsterDatabase.cs`
- `Assets/Data/MonsterCombatTuningConfig.asset`
- `Assets/Scripts/DefenseGame/MonsterUnit.cs`

도입 목표:

| 보스 패턴 | 효과 | 대응 재미 |
| --- | --- | --- |
| 마나 잠식 | 마나가 많은 아군 1~2명 마나 감소 | 마나 회복/지원 유닛 가치 상승 |
| 앞줄 강타 | 전방/가장 가까운 유닛에게 큰 피해 | 탱커 배치 필요 |
| 보호막 충전 | 일정 시간 내 딜 부족 시 보호막 생성 | 딜 타이밍/버스트 필요 |
| 광역 공포 | 짧은 시간 공격속도 감소 | 버프/해제/배치 대응 |
| 소환 지휘 | 잡몹을 추가 소환 | 광역기/장판 유닛 가치 상승 |
| 처형 표식 | 체력이 낮은 유닛을 노림 | 힐/방어막/위치 조절 필요 |

피드백 기준:

- 보스 스킬 준비: 바닥 경고 원 또는 보스 머리 위 아이콘
- 발동 순간: 카메라 흔들림 + 타격 FX
- 맞은 아군: 상태 텍스트 표시
- 보스 사망: 즉시 결과창이 아니라 1~1.5초 사망 연출 후 결과창

## 5. 라운드 사이 전투 상점

적용 위치 후보:

- 신규 `RunShopSystem`
- 기존 `MetaFlowUI` 또는 라운드 종료 UI 확장

상점은 아웃게임 상점이 아니라 "이번 판 안에서만 쓰는 전투 상점"이다.

상품 후보:

| 상품 | 효과 | 재미 |
| --- | --- | --- |
| 랜덤 유닛 3택1 | 현재 판에 바로 유닛 추가 | 선택 도파민 |
| 레시피 재료 추천 | 현재 레시피에 필요한 재료 등장 확률 증가 | 목표 추적 |
| 위험한 상자 | 낮은 확률 전설, 실패 시 일반 | 운빨 선택 |
| 자리 교체권 | 다음 라운드 전 무료 자리 교체 | 배치 대응 |
| 보스 정보 | 다음 보스 패턴 공개 | 전략 보상 |
| 타일 재배치 | 보드 특수 타일을 다시 뽑음 | 배치 변동성 |

운영 기준:

- 매 라운드가 아니라 2~3라운드마다 등장시켜 리듬을 유지한다.
- 상품은 3개만 보여주고, 리롤은 비용을 받는다.
- 전투 상점 구매는 "이번 판 성장"이고, 아웃게임 성장과 분리한다.

## 6. 필드/타일 변동

적용 위치 후보:

- `DefenseBoardManager`
- `BoardSlot`
- 신규 `BoardTileModifierSystem`

타일 후보:

| 타일 | 효과 | 배치 재미 |
| --- | --- | --- |
| 가속 타일 | 공격속도 증가 | 캐리 유닛 배치 |
| 마나 타일 | 초당 마나 회복 증가 | 스킬 유닛 배치 |
| 수호 타일 | 받는 피해 감소 | 탱커 배치 |
| 사거리 타일 | 기본 사거리 증가 | 원거리 유닛 가치 |
| 위험 타일 | 라운드마다 체력 소량 감소, 대신 공격력 증가 | 리스크 보상 |
| 보스 저주 타일 | 보스 라운드에만 디버프 | 라운드별 재배치 압박 |

운영 기준:

- 초반에는 이로운 타일 위주로 플레이어가 강하다는 느낌을 준다.
- 중반부터 위험/저주 타일을 섞어 배치 선택을 강제한다.
- 타일 효과는 머리 위 UI와 겹치지 않게 바닥 이펙트 중심으로 보여준다.

## 우선순위

1. 미션/정산 룰렛 체감 확인
2. 증강체 선택지 체감 확인
3. 레시피 진행도 UI 추가
4. 보스 패턴 3종 먼저 구현
5. 라운드 사이 전투 상점 MVP
6. 타일 효과 MVP

## 구현 상태

| 순위 | 컨텐츠 | 현재 상태 | 주요 파일 |
| --- | --- | --- | --- |
| 1 | 라운드 미션 | 완료. 참기/올인/콤보/고등급 도박/레시피 추적/등급 무지개 미션과 룰렛, 잭팟 보상 연결 | `TacticalMissionSystem.cs` |
| 2 | 럭키 이벤트/증강체 | 완료. 도파민 버튼, 미니 로또, 희귀 현상금, 보스 복권, 쿠폰 폭풍 등 선택지 추가 | `AugmentManager.cs` |
| 3 | 레시피 소환 | 완료. 전용 레시피 8종과 등급 범용 레시피 3종, 하단 `재료 n/m` 상태 연결. 빈 예약 슬롯은 재료로 쓰지 않음 | `DefenseBoardManager.cs`, `SimpleGameHUD.cs` |
| 4 | 보스 패턴 | 완료. 보스별 8개 패턴 로테이션으로 광역 피해, 기절, 마나 번, 골드 징수, 보스 강화, 처형을 섞음 | `MonsterDatabase.cs`, `MonsterUnit.cs` |
| 5 | 라운드 사이 전투 상점 | 완료. 2라운드 이후 3라운드 간격으로 3개 상품 등장. 유닛 보급, 위험 상자, 타일 재배치, 보스 정보, 쿠폰, 회복 상품 연결 | `RunShopSystem.cs`, `RuntimeSceneBootstrap.cs` |
| 6 | 필드/타일 변동 | 완료. 라운드별 전술 타일을 배치하고 유닛이 올라가면 공격속도, 마나, 수호, 사거리, 스킬, 과부하, 보스 피해 보너스 적용 | `BoardTileModifierSystem.cs`, `BoardSlot.cs`, `DefenderUnit.cs` |

## 조절 포인트

- 미션 종류/보상: `TacticalMissionSystem.CreateMission` 계열에서 목표 수치, 룰렛 범위, 잭팟 확률을 조절한다.
- 증강체 등장 간격: `AugmentManager`의 `firstChoiceRound`, `minChoiceInterval`, `maxChoiceInterval`을 조절한다.
- 레시피 재료/상위 합성: `DefenseBoardManager.UltimateRecipes`의 영웅 ID 배열을 수정한다. 초월 결과는 `hero_51+` 구간 추가 후 연결한다.
- 보스 패턴 난이도: `MonsterDatabase.BuildBossSkills`의 `power`, `duration`, `targetCount`, `cooldown`을 조절한다.
- 전투 상점 주기/가격: `RunShopSystem`의 `firstShopRound`, `shopInterval`, `CreateOffer` 가격식을 조절한다.
- 전술 타일 수/주기: `BoardTileModifierSystem`의 `rerollInterval`, `earlyTileCount`, `midTileCount`, `lateTileCount`를 조절한다.
- 전술 타일 효과량: `BoardSlot.ApplyTileBonus`의 각 보너스 수치를 조절한다.

## 밸런스 기준

- 초반 1~5라운드: 보상은 작지만 자주 터지게 한다.
- 중반 6~15라운드: 미션 실패/성공 선택이 덱 운영을 바꾸게 한다.
- 보스 전후: 레시피, 보스 대응, 럭키 이벤트가 겹쳐 강한 판단 지점을 만든다.
- 고정 보상보다 랜덤 보상은 평균값을 낮추고 최고점을 높인다.

## Transcendent Combat Identity — Balance Patch 1

| Hero | Combat identity | Balance tier within the current seven results |
| --- | --- | --- |
| hero_51 | High Control / Damage | High control damage |
| hero_52 | High AoE Damage | High area damage |
| hero_53 | Apex Sustained Single-Target DPS | Apex sustained DPS |
| hero_54 | Entry Tank | Entry tank |
| hero_55 | Mid Tank | Mid tank |
| hero_56 | Apex High-Risk Burst / AoE DPS | Apex high-risk burst / area DPS |
| hero_57 | High Variance DPS | High variance DPS |

- These labels are combat-role power budgets among the seven current Transcendent results, not a separate character-grade tier.
- Tanks are evaluated through durability, damage reduction, frontline control, and survival time—not direct DPS alone.
- Hard attack recipes must receive suitably strong attack rewards; special-condition units may have higher peaks only in their intended condition.
- Recipe difficulty and total combat value are reviewed together. Fixed recipe results and material inheritance rules remain unchanged.