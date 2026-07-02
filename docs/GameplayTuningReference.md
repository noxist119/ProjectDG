# Gameplay Tuning Reference

이 문서는 캐릭터 평타, 스킬, 사거리, 애니메이션 이벤트, projectile/effect 리소스를 나중에 다시 설정할 때 참고하기 위한 기준 문서입니다.

## 빠른 위치

- 캐릭터별 평타/스킬/사거리/리소스 설정: `Assets/Data/CharacterCombatTuningConfig.asset`
- 공용 projectile/effect 설정: `Assets/Data/DefenseGamePresentationConfig.asset`
- 평타 데이터 구조: `Assets/Scripts/DefenseGame/AttackBehavior.cs`
- 스킬 데이터 구조: `Assets/Scripts/DefenseGame/SkillDefinition.cs`
- 캐릭터 평타/스킬 실제 처리: `Assets/Scripts/DefenseGame/DefenderUnit.cs`
- projectile 충돌 처리: `Assets/Scripts/DefenseGame/Projectile.cs`
- 애니메이션 이벤트 수신: `Assets/Scripts/DefenseGame/AnimationEventProxy.cs`
- 애니메이션 fallback 시간: `Assets/Scripts/DefenseGame/UnitAnimationDriver.cs`
- projectile/effect 리소스 연결 전용 가이드: `docs/ProjectileEffectResourceGuide.md`

## 1. 평타 설정

`CharacterCombatTuningConfig.asset`의 각 `hero_xx` 항목에서 설정합니다.

- `Override Basic Attack Type`: 켜면 근거리/원거리 타입을 직접 지정합니다.
- `Basic Attack Type`: `Melee` 또는 `Ranged`입니다.
- `Override Basic Attack Range`: 켜면 평타 사거리를 단순하게 직접 지정합니다.
- `Basic Attack Range`: 평타가 발동되는 거리입니다.

처리 방식은 이렇게 나뉩니다.

- `Melee`: `AttackHit` 이벤트 시점에 바로 데미지가 들어갑니다.
- `Ranged`: `FireProjectile` 이벤트 시점에 projectile이 발사되고, projectile이 몬스터에 닿을 때 데미지가 들어갑니다.

## 2. 평타 리소스

`CharacterCombatTuningConfig.asset`의 `Basic Attack Resources`에서 캐릭터별로 바꿀 수 있습니다.

- `Basic Attack Projectile Prefab`: 원거리 평타 projectile입니다. 비어 있으면 `DefenseGamePresentationConfig.asset`의 공용 `Projectile Prefab`을 사용합니다.
- `Basic Attack Muzzle Effect Prefab`: 발사 순간 출력할 이펙트입니다. 비어 있으면 공용 `Default Muzzle Effect Prefab`을 사용합니다.
- `Basic Attack Hit Effect Prefab`: 타격 순간 출력할 이펙트입니다. 비어 있으면 공용 `Default Hit Effect Prefab`을 사용합니다.

공용 fallback은 `DefenseGamePresentationConfig.asset`에서 설정합니다.

- `Projectile Prefab`: 기본 원거리 projectile입니다.
- `Default Muzzle Effect Prefab`: 캐릭터별 발사 이펙트가 없을 때 쓰는 공용 발사 이펙트입니다.
- `Default Hit Effect Prefab`: 캐릭터별 타격 이펙트가 없을 때 쓰는 공용 타격 이펙트입니다.
- `Default Area Effect Prefab`: 스킬별 장판 이펙트가 없을 때 쓰는 공용 장판 이펙트입니다.

## 3. 스킬 설정

스킬은 `CharacterCombatTuningConfig.asset`의 각 `hero_xx` 항목에서 `Skill 01`, `Skill 02`, `Skill 03`으로 직접 지정합니다.

- `Override Skill 01`: 1번 스킬을 직접 지정합니다.
- `Override Skill 02`: 2번 스킬을 직접 지정합니다.
- `Override Skill 03`: 3번 스킬을 직접 지정합니다.

스킬 발동 우선순위는 `Skill 01`, `Skill 02`, `Skill 03` 순서입니다. 마나가 가득 차고, 쿨타임이 끝나고, 사거리 조건을 만족하는 첫 번째 스킬이 발동합니다.

## 4. 스킬 사거리

캐릭터의 모든 스킬에 같은 사거리를 주고 싶으면 아래만 씁니다.

- `Override Skill Cast Range`: 켜기
- `Skill Cast Range`: 스킬 공통 발동 사거리

스킬마다 사거리를 다르게 주고 싶으면 각 `Skill 01/02/03` 안에서 설정합니다.

- `Use Custom Cast Range`: 켜기
- `Cast Range`: 해당 스킬만 사용하는 발동 사거리

`Override Skill Cast Range`가 켜져 있으면 캐릭터 공통 스킬 사거리가 개별 스킬 사거리보다 우선 적용됩니다.

## 5. 스킬 타입과 전달 방식

`SkillDefinition`에서 자주 만지는 필드는 아래입니다.

- `effectType`: 실제 스킬 효과입니다. 데미지, 광역, 힐, 슬로우, 중독, 소환 같은 기능을 정합니다.
- `category`: UI 분류용입니다. 보통 `Auto`로 둬도 됩니다.
- `deliveryType`: 효과가 적용되는 방식을 정합니다.
- `power`: 주 효과 수치입니다.
- `secondaryPower`: 보조 수치입니다. 흡혈 비율, tick 간격, 추가 배율 등에 씁니다.
- `duration`: 지속 시간입니다.
- `radius`: 광역 범위 또는 장판 범위입니다.
- `manaThreshold`: 필요 마나입니다. 기본은 `100`입니다.
- `cooldown`: 스킬 쿨타임입니다.
- `hitCount`: 다중 타겟 수입니다.

`deliveryType` 기준은 아래처럼 쓰면 됩니다.

- `Melee`: `SkillHit` 이벤트 시점에 바로 효과가 들어갑니다.
- `Projectile`: `SkillFire` 이벤트 시점에 projectile이 발사되고, projectile이 닿을 때 효과가 들어갑니다.
- `GroundArea`: `SpawnArea` 이벤트 시점에 장판이 생성되고, 장판 범위 안 몬스터에게 tick 데미지가 들어갑니다.
- `Instant`: `SkillApply` 이벤트 시점에 힐, 버프, 마나 충전 같은 효과가 바로 들어갑니다.
- `Auto`: `effectType`에 맞춰 자동으로 처리합니다.

## 6. 스킬 리소스

각 `Skill 01/02/03` 안에서 스킬별 리소스를 직접 꽂을 수 있습니다.

- `Projectile Prefab`: projectile형 스킬에서 사용할 projectile입니다. 비어 있으면 공용 projectile을 사용합니다.
- `Muzzle Effect Prefab`: 스킬 발동 또는 projectile 발사 순간 출력할 이펙트입니다. 비어 있으면 공용 발사 이펙트를 사용합니다.
- `Hit Effect Prefab`: 스킬이 적중하는 순간 출력할 이펙트입니다. 비어 있으면 공용 타격 이펙트를 사용합니다.
- `Area Effect Prefab`: 광역/장판 스킬에서 출력할 이펙트입니다. 비어 있으면 공용 장판 이펙트를 사용합니다.

추천 연결 방식은 이렇습니다.

- 원거리 딜 스킬: `Projectile Prefab`, `Muzzle Effect Prefab`, `Hit Effect Prefab`
- 근거리 딜 스킬: `Hit Effect Prefab`
- 광역 폭발 스킬: `Hit Effect Prefab`, `Area Effect Prefab`
- 장판 스킬: `Area Effect Prefab`
- 힐/버프/마나 스킬: `Muzzle Effect Prefab`

## 7. 애니메이션 이벤트 표준

애니메이션 클립에는 아래 이름을 우선 사용합니다.

- 근거리 평타 타격 프레임: `AttackHit`
- 원거리 평타 projectile 발사 프레임: `FireProjectile`
- 근거리/즉시형 스킬 적용 프레임: `SkillHit`
- projectile형 스킬 발사 프레임: `SkillFire`
- 힐/버프/마나 충전 적용 프레임: `SkillApply`
- 장판 생성 프레임: `SpawnArea`
- 순수 연출 이펙트: `PlayEffect` 또는 `PlayEffectKey`
- 사운드: `PlaySound`

`Hit`, `Impact`, `Damage`도 fallback으로 받아주지만 새로 찍는 클립에는 위의 명확한 이름만 쓰는 것을 권장합니다.

## 8. 데미지 처리 시점

- 근거리 평타: `AttackHit` 이벤트 시점에 데미지와 타격 이펙트가 적용됩니다.
- 원거리 평타: `FireProjectile` 이벤트 시점에 projectile과 발사 이펙트가 나오고, projectile 충돌 시점에 데미지와 타격 이펙트가 적용됩니다.
- 근거리/즉시형 스킬: `SkillHit` 또는 `SkillApply` 이벤트 시점에 효과가 적용됩니다.
- projectile형 스킬: `SkillFire` 이벤트 시점에 projectile과 발사 이펙트가 나오고, projectile 충돌 시점에 스킬 효과와 타격 이펙트가 적용됩니다.
- 장판형 스킬: `SpawnArea` 이벤트 시점에 장판 이펙트가 나오고, 범위 안 몬스터에게 tick 데미지가 들어갑니다.
- 힐/버프/마나 스킬: `SkillApply` 이벤트 시점에 바로 적용됩니다.

## 9. 체크리스트

- 평타 사거리는 `Override Basic Attack Range`와 `Basic Attack Range`에서 먼저 확인합니다.
- 스킬 사거리는 `Override Skill Cast Range`와 `Skill Cast Range`를 먼저 확인합니다.
- 캐릭터별 projectile/effect가 필요하면 `CharacterCombatTuningConfig.asset`에 꽂습니다.
- 공통으로 쓰는 projectile/effect는 `DefenseGamePresentationConfig.asset`에 꽂습니다.
- projectile prefab에는 `Projectile` 컴포넌트가 있어야 합니다.
- 캐릭터 id는 `CharacterCombatTuningConfig.asset`의 `characterId`와 실제 `hero_xx` id가 맞아야 합니다.

## 10. 1~10라운드 초반 런 계측 기준

초반 재미 튜닝은 감으로만 보지 않고 `DefenseGameController`의 Early Run Telemetry 값으로 확인합니다.

- 기록 범위: ROUND 1~10
- 기록 항목: 클리어 시간, 시작 골드, 종료 골드, 라운드 내 소환 횟수, 라운드 내 합성 횟수, 최고 합성 등급, 보스 HP 잔량
- HUD 표시: 전투 하단 빌드 판독 바의 `런` 칸
- 막힘 판정: 초반 클리어 시간이 기준보다 길거나, 보스 라운드에서 보스 HP가 많이 남거나, 1~5라운드에 골드와 소환 수가 동시에 낮은 경우
- 폭발 판정: 레어 이상 합성이 나오고 클리어 시간이 짧게 줄어든 경우

현재 기본값은 ROUND 1~10을 기록하고, 초반 느린 클리어 기준은 54초, 보스 HP 경고 기준은 30%입니다. 이 값은 ROUND 1~10 실측 2회 전 임시 기준이며, 회복 상점이 너무 자주 뜨면 `slowEarlyClearSeconds`, `highBossHealthWarningRatio`를 다시 높입니다.

## 11. 전투 중 원인-결과 피드백 기준

전투 중에는 결과창까지 기다리지 않고, 다음 사건이 발생했을 때 즉시 피드백을 줍니다.

- 타일 적중: 전술 타일 누적 피해가 일정 단계를 넘을 때 `타일 적중` 배너 표시
- 보스 패턴 대응: 보스에게 타일 피해가 누적되면 `보스 대응 적중` 배너 표시
- 보스 압박 판독: 보스 스킬 발동 횟수, 영향 대상, 피해량, 골드 손실, 마나 소각, 즉사, 강화/집결, 대응 타일 피해를 기록합니다.
- 시너지 발동: 활성 시너지 수나 대표 시너지가 바뀌면 `시너지 발동` 배너 표시
- 최고 딜러 갱신: 누적 피해 최고 유닛이 바뀌거나 피해 단계가 올라가면 `최고 딜러 갱신` 또는 `딜러 폭주` 배너 표시
- 딜러 판독: HUD는 현재 라운드 1등 딜러를 보여주고, 라운드 리캡과 결과창은 딜러 TOP 3를 보여줍니다.

보스 라운드 중 HUD는 `보스 압박` 줄에 마지막 보스 스킬과 현재 대응 피해를 보여줍니다. 결과창은 전체 런의 보스 압박 요약을 보여주며, 보스 스킬 피해에 비해 대응 타일 피해가 낮으면 다음 추천을 보스 대응 중심으로 바꿉니다.

너무 시끄러워지지 않도록 `combatFeedbackCooldown`, `tileContributionFeedbackStep`, `bossTileContributionFeedbackStep`, `topDamageFeedbackStep`로 빈도를 조절합니다.

## 12. 초반 선택지 노출 기준

정식 전투 상점은 ROUND 6부터 열지만, ROUND 3~5 사이에 선택이 전혀 없으면 초반 반복감이 커집니다. 그래서 ROUND 3 클리어 후 소형 전투 상점을 한 번 열어 3개의 선택지를 제공합니다.

- 소형 상점 라운드: 기본 ROUND 3
- 선택지 수: 기본 3개
- 상품 풀: 합성 부스터 고정 1개 + 긴급 소환권, 위험한 상자, 소환 쿠폰, 현장 의무병 등
- 가격: 정식 상점 대비 72%

이 선택지는 “초반 고점 보정”과 “위험 선택”을 동시에 주기 위한 장치입니다. 초반이 복잡해 보이면 `earlyMiniShopOfferCount`를 2로 낮추거나 `earlyMiniShopRound`를 4 또는 5로 미룹니다.

## 13. 초반 런 회복 상점 기준

계측 결과가 막힘으로 판단되면 ROUND 4~10 사이에 `런 회복 상점`을 한 번 엽니다. 이 상점은 실패를 무효화하는 장치가 아니라, 사용자가 다시 선택할 힘을 얻도록 돕는 안전망입니다.

- 발동 조건: 라운드 실패, 느린 클리어, 보스 HP 과다 잔존, 1~5라운드 소환/골드 동시 부족
- 노출 횟수: 한 런에 1회
- 상품 수: 기본 3개
- 상품 예시: 응급 골드, 구제 레어 지원, 보스 대비 패키지, 현장 의무병, 소환 쿠폰
- 조정 위치: `RunShopSystem`의 `enableEarlyRecoveryShop`, `earlyRecoveryShopFirstRound`, `earlyRecoveryShopLastRound`, `earlyRecoveryShopOfferCount`

이 시스템이 너무 자주 보이면 `slowEarlyClearSeconds`를 높이거나 `highBossHealthWarningRatio`를 높입니다. 반대로 초반 이탈이 크면 `earlyRecoveryShopFirstRound`를 3으로 낮추거나 무료 보상량을 올립니다.

## 14. 결과창 런 리캡 기준

라운드 종료 결과창은 단순 보상 확인이 아니라, 사용자가 “왜 이겼고 다음에 무엇을 바꿔야 하는지”를 즉시 읽는 화면으로 둡니다.

- 런 점수: 라운드 진행도, 총 피해량, 최고 시너지 수, 최고 콤보, 치명타 수, 남은 라이프를 합산합니다.
- 등급 표시: `C/B/A/S/SS/SSS`로 표시하고, 실패한 판도 점수와 원인을 남깁니다.
- 핵심 리캡: 최고 딜러, 최고 시너지, 타일 기여, 1~10라운드 초반 계측 요약을 함께 보여줍니다.
- 딜러 리캡: 누적 딜러 TOP 3를 보여줘서 어떤 유닛/태그를 유지해야 하는지 판단하게 합니다.
- 다음 추천: 초반 회복이 필요하면 막힘 원인을 우선 표시하고, 보스가 가까우면 보스 대응 타일과 고등급 딜러 유지를 안내합니다.

이 값은 `DefenseGameController`의 `RunPerformanceScore`, `RunPerformanceGrade`, `RunResultRecapSummary`, `RunNextActionSummary`에서 계산하고 `MetaFlowUI` 결과창에 표시합니다. 점수 체감이 너무 후하면 `CalculateRunPerformanceScore`의 피해량/콤보/시너지 가중치를 낮추고, 너무 박하면 같은 값을 올립니다.

## 15. 당첨 연출 기준

랜덤 방어 게임의 손맛은 “좋은 결과가 나왔을 때 즉시 알아보는 것”이 핵심입니다. 다음 이벤트는 전용 배너, 사운드, 카메라 흔들림, 화면 펄스, 짧은 슬로우를 붙입니다.

- Rare 이상 소환: `희귀 소환` 또는 `초반 찬스 소환` 배너, 작은 펄스, 약한 히트스톱
- Epic 이상 소환: `대박 소환` 배너, 큰 펄스, 강한 사운드, 짧은 슬로우
- Epic 이상 합성: 합성 위치 중심 펄스와 슬로우, `대박 합성` 배너
- 초월 완성: `초월 완성` 배너, 다중 펄스, 강한 카메라 흔들림, 전용 사운드
- 보스 처치: 보스 위치 중심 펄스와 대응 피해 요약, 강한 카메라 흔들림

연출 강도는 `RuntimeGameFeel.PlayJackpotPulse`, `RuntimeAudioUtility.PlayJackpotMinor/Major/Ultimate`에서 조절합니다. 너무 과하면 슬로우 duration부터 낮추고, 밋밋하면 pulse radius와 shake intensity를 먼저 올립니다.

## 16. 빌드 목표 가이드 기준

HUD는 단순 상태 표시가 아니라 다음 행동을 압축해서 보여줘야 합니다.

- 보스 전: `다음 보스 ROUND n | 보스 대비: 보스 타일 + 주력 딜러 유지`
- 초월 가능: `초월 준비 완료: 신화 조합을 실행하세요`
- 초월 진행 중: `초월 목표: 현재 레시피 진행도`
- 시너지 부족: `시너지 목표: 대표 시너지 3개 이상`
- 딜러 확보 후: `딜러 목표: 최고 딜러 태그 유지`

이 값은 `DefenseGameController.CurrentBuildGoalSummary`에서 계산하고, `SimpleGameHUD`의 보스 HUD 줄과 결과창 다음 추천으로 이어집니다.

## 17. 1~10라운드 실측 루프

코드 계측만으로는 충분하지 않습니다. 실제 플레이에서는 최소 20회 이상 ROUND 1~10을 돌려 HUD의 `런` 줄과 결과창 리캡을 확인합니다. 현재 자동 누적 로그는 `DefenseGame.EarlyRunTuningLog.v1` PlayerPrefs 키에 최근 표본을 저장하고, 결과창 `로그` 줄에 표본 진행도를 표시합니다.

- 확인 값: 첫 Rare+ 라운드, 첫 합성 라운드, ROUND 3 부스터 노출/구매율, 회복 상점 발생/구매율, ROUND 10 보스 HP 잔량
- 추가 확인 값: 운 나쁨 보험 발동률, 보험 선택 후 이탈감, 결과창 `도감`/`상점`/`계속` 버튼 클릭 동선
- 튜닝 판단: 회복 상점이 2회 테스트 모두 뜨면 초반 난도가 높거나 보상이 부족한 상태입니다. 한 번도 안 뜨고 초반이 밋밋하면 ROUND 3 소형 상점 보상을 더 자극적으로 조정합니다.
- 우선 조정 위치: `slowEarlyClearSeconds`, `highBossHealthWarningRatio`, `earlyMiniShopOfferCount`, `earlyRecoveryShopFirstRound`, `earlyRecoveryShopOfferCount`

## 18. 2026-06-16 초반 15분 튜닝 체크

- Unity 실측 목표: ROUND 1~10을 최소 2회 플레이하면서 10초 튜토리얼 이해도, ROUND 3 소형 상점 구매율, 첫 Rare+ 획득 시점, 첫 합성 발생 시점, ROUND 10 보스 HP 잔량을 기록합니다.
- ROUND 3 합성 부스터: 같은 등급 2마리 보유 조건에만 기대지 않고, 보유 중인 가장 유효한 등급의 부족 재료를 채워 첫 합성선에 닿도록 보정합니다. 빈 슬롯이 부족하면 가능한 만큼만 지급하고, 슬롯 확보 후 재시도해야 합니다.
- 결과창 다음 행동: 행동 목록만 나열하지 않고 `다음 판은 보스 사냥덱 추천`처럼 대표 빌드명을 첫 줄에 표시합니다. 대표 빌드명은 초월 완성덱, 보스 대응덱, 보스 사냥덱, 시너지덱, 치명타 폭발덱, 캐리덱, 회복 재정비덱 중 현재 런 상태로 고릅니다.
- 다음 실측 후 조정 위치: `slowEarlyClearSeconds`, `highBossHealthWarningRatio`, `earlyMiniShopOfferCount`, `earlyRecoveryShopFirstRound`, `earlyRecoveryShopOfferCount`, `earlyPityEpicChance`.

### 2026-06-16 Unity R1~R10 실측 메모

- Run 1: 첫 Rare+ R1, 첫 합성 R1, ROUND 3 부스터 구매 후 즉시 합성 성공. 회복 선택지를 실제로 구매하지 않은 흐름에서는 R9에서 생명력 0으로 종료했습니다.
- Run 2: 첫 Rare+ R1, 첫 합성 R2, ROUND 3 부스터 구매 후 합성 성공. ROUND 7에서 회복 보급 1회 사용 후 ROUND 10 보스 HP 0%, 생명력 4/20으로 클리어했습니다.
- 판단: 첫 5분 보장 사건 루프는 작동합니다. 현재 핵심 위험은 보상이 아니라 생명력 압박 누적이므로, 회복 상점은 ROUND 4~10에 1회만 선명하게 노출하고 결과창에서 `다음 판 목표 + 도감 강화 보상`을 강하게 보여줍니다.

## 19. 오늘의 운세 룰

매일 하나의 전역 규칙을 날짜 기반으로 고정합니다. 규칙은 `DailyFortuneSystem`에서 계산하고, 로비/결과창/전투 상점에 표시합니다.

- 적용 축: Epic 소환률 보너스, 보스 체력 보정, 전투 상점 할인, 시작 골드 보너스, 회복 상품 라이프 보너스
- 목적: 새 콘텐츠 제작 없이 소환/상점/보스 압박을 재조합해 매일 한 판 더 들어갈 명분을 만듭니다.
- 주의: 보너스만 주면 난도가 너무 낮아지므로, 강한 보너스에는 보스 체력 보정을 짝으로 붙입니다.

## 20. 2026-06-19 오늘 점검 반영

상용 유사 장르 기준으로 초반 15분의 핵심은 "소환한다 -> 막는다 -> 합친다 -> 더 센 게 나온다"를 10초 안에 보여주고, 운이 나빠도 선택으로 판을 비틀 여지를 주는 것입니다. 오늘 수정은 기능 수보다 초반 손맛과 재도전 동기에 직접 닿는 항목만 반영했습니다.

- 10초 진입: `SimpleGameHUD` 오프닝 문구를 네 단계 행동 문장으로 압축하고, 초반 검증 문구를 HUD 인사이트에 다시 노출합니다.
- 운빨 속 선택: `RunShopSystem`에 운명 계약 2종을 추가합니다. 골드 대신 라이프를 걸고 합성 올인 또는 보스 사냥 방향으로 판을 전환합니다.
- 억울함 완화: 계약은 생명력이 부족하면 구매되지 않고, 보상 적용 실패 시 골드 fallback을 줘서 "라이프만 잃은 실패 구매"가 생기지 않게 합니다.
- 반복 플레이 욕구: 결과창 행동 목록에 `R1~R10 로그 n/20` 목표를 함께 노출해 다음 판을 플레이테스트 표본 수집과 연결합니다.
- 성장/랭킹: 시즌 랭킹 표시는 프리시즌 비동기 목표로 정리합니다. 실제 상용화 전에는 친구/협동 서버, 시즌 미션 보상 수령 UX, 랭킹 리플레이/덱 공유가 남아 있습니다.

다음 실측 기준은 최소 20회 R1~R10 플레이입니다. 확인할 값은 첫 Rare+ 라운드, 첫 합성 라운드, ROUND 3 부스터 선택률, 운명 계약 선택률, 회복상점 선택률, ROUND 10 보스 잔여 HP입니다.

## 21. 2026-06-22 상용화 루프 보강

목표는 기능 수 추가가 아니라 초반 15분의 손맛, 선택의 의미, 반복 복귀 이유를 더 명확히 만드는 것입니다.

- 실측 튜닝 루프: `DefenseGameController.EarlyRunTuningDecisionSummary`가 최근 R1~R10 표본 20회를 기준으로 첫 Rare+, 첫 합성, R10 도달/클리어, R3 상점 구매율, 회복 상점 구매율, 운명 사용률, R10 보스 HP를 판정합니다.
- 운명 개입 HUD: `RuntimeSceneBootstrap`의 `FateInterventionPanel`에서 운명 게이지, 빚, Boss HP 대가, 이득/대가 요약과 Rare+ 잠금/일반 금지/상점 강제 버튼을 직접 노출합니다.
- 협동/시즌/리플레이 루프: 결과창은 `OutgameProgressionSystem.BuildSeasonResultLoopSummary()`를 붙여 협동 보스 점수, 다음 시즌 목표, 덱 공유 코드, 리플레이 요약, MVP를 다음 행동으로 보여줍니다.

다음 검증은 Unity Play Mode에서 R1~R10 20회 표본을 채운 뒤 HUD의 실측 판정이 실제 체감과 맞는지 확인하는 것입니다.
