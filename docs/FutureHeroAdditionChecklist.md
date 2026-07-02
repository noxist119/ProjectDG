# 추가 영웅 적용 체크리스트

새 영웅을 추가할 때는 아래 기준을 기본으로 따른다. 핵심 원칙은 `hero_XX` ID를 단일 기준으로 쓰고, 순서 기반 매칭은 사용하지 않는 것이다.

## 영웅 번호 구간

| 등급 | 영웅 ID 구간 | 운영 기준 |
| --- | --- | --- |
| 일반 | `hero_01` ~ `hero_05` | 서비스 시작 기본 5종 |
| 레어 | `hero_06` ~ `hero_10` | 초반 성장 목표 |
| 희귀 | `hero_11` ~ `hero_20` | 첫 보스 이후 핵심 허들 |
| 전설 | `hero_21` ~ `hero_30` | 중후반 핵심 조합/특수 유닛 |
| 신화 | `hero_31` ~ `hero_50` | 상위 보상 및 장기 성장 |
| 초월 | `hero_51` ~ `hero_100` | 최상위 등급, 현재 `hero_57`까지 명시. 이후 추가 초월은 `hero_58`부터 순서대로 확장 |

## 자동으로 따라가는 부분

- 뽑기 카드 풀: `CharacterDatabase`에 실제 등록된 영웅은 아웃게임 상자 뽑기 대상이 된다.
- 배치 후보/도감/인벤토리: `GetDeployableCharacters()`와 `Characters` 목록을 쓰므로 실제 등록된 영웅이 자동으로 보인다.
- 테스트 모드: 보유 여부와 관계없이 실제 등록된 영웅을 사용할 수 있다.
- 서비스 모드: 뽑기로 획득한 영웅만 배치 가능하다.
- 인게임 상점 보상: `RandomUnit`, `RareUnit`, `RiskChest`는 DB 랜덤 풀을 사용하므로 새 영웅이 등급 풀에 자동 포함된다.
- 증강체: 대부분 `DefenderUnit.OnDefenderSpawned` 이후 모든 유닛/역할/태그 기준으로 적용되므로 새 유닛도 자동 적용된다.
- 시너지: `CharacterCombatTuningConfig`에서 role/tags가 정해지면 `BoardSynergySystem`이 자동 집계한다.

## 새 영웅 추가 시 꼭 같이 봐야 하는 부분

1. `Assets/Data/DefenseGamePresentationConfig.asset`
   - `characterId`를 새 `hero_XX`로 추가한다.
   - 실제 유닛 프리팹을 넣는다. 프리팹이 없으면 예약 슬롯으로 보고 뽑기/배치 풀에서 제외된다.

2. `Assets/Data/CharacterCombatTuningConfig.asset`
   - 같은 `hero_XX` ID로 role, 기본 공격 타입, 기본 사거리, 스킬 수치를 설정한다.
   - 기본 사거리는 `basicAttackRange`를 메인으로 쓴다.

3. `Assets/Scripts/DefenseGame/CharacterCombatTuningConfig.cs`
   - 기존 스킬 타입으로 표현 불가능한 고유 스킬이면 `ApplyRequestedHeroSkillPreset`에 ID별 프리셋을 추가한다.
   - 성장 대상 수치는 `growthStepRatio`와 스킬 power/duration/value 기준을 같이 확인한다.

4. `Assets/Scripts/DefenseGame/RollRollUiResource.cs`
   - 사용할 초상화/미니미 UI 리소스가 있으면 `hero_XX` 매핑을 추가한다.
   - 리소스가 없으면 fallback UI로 표시되지만, 도감/로비 품질을 위해 가능하면 추가한다.

5. `Assets/Scripts/DefenseGame/DefenseBoardManager.cs`
   - 새 유닛이 초월 조합 재료나 결과가 되어야 할 때만 `UltimateRecipes`에 추가한다.
   - 모든 새 유닛을 자동으로 조합에 넣지는 않는다. 조합은 의도적인 덱 목표로 관리한다.

6. 밸런스 문서/엑셀
   - `docs/DefenseGame_Balance_Skill_Summary.xlsx`의 `영웅_등급표`에서 해당 `hero_XX` 행을 채운다.
   - `평타 데미지(Lv1)`, `스킬 데미지/효과(Lv1)`, `데미지/성장 메모`를 같이 기록한다.
   - `docs/GameDesignDecisionSummary.md`에도 스킬 요약을 반영한다.

7. 서비스 시작/튜토리얼 풀
   - 일반 5종 외 시작 지급 영웅을 바꾸려면 `OutgameProgressionConfig.serviceStarterCharacterIds`를 수정한다.
   - 단순 추가 영웅은 시작 풀에 넣지 않아도 뽑기/도감 풀에는 들어간다.

## 코드 기준

- `CharacterDatabase`는 이제 현재 리스트 개수가 아니라 가장 높은 등록 `hero_XX` 번호와 설정 파일을 기준으로 갱신한다.
- 예약 슬롯(`hero_15~20`, `hero_24~30`, `hero_34~50`, `hero_58~100`)은 프리팹과 전투 튜닝이 모두 없으면 실제 풀에서 제외된다.
- 프리팹만 있고 `CharacterCombatTuningConfig` 항목이 없는 영웅도 반쪽 설정으로 보고 실제 풀에서 제외된다.
- 순서 기반 캐릭터 프리팹/전투 튜닝 fallback은 사용하지 않는다. 반드시 `hero_XX` ID가 일치해야 한다.
- 새 초월 유닛은 `hero_58`, `hero_59`처럼 뒤로 이어 붙이면 된다.

## 빠른 검증

새 영웅 추가 후 최소 확인:

- 도감/로비 배치 후보에 보이는지
- 테스트 모드에서 바로 배치되는지
- 서비스 모드에서 뽑기 획득 전 잠금, 획득 후 배치가 맞는지
- 인게임 랜덤 보상에서 해당 등급 보상으로 나올 수 있는지
- role/tag 시너지가 의도대로 잡히는지
- 증강체 버프, 카드 성장, 몬스터 성장 보정이 정상 적용되는지
- 고유 스킬의 이벤트 키와 데미지 타이밍이 맞는지
