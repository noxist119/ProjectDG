# Projectile & Effect Resource Guide

이 문서는 평타와 스킬에 projectile, 발사 이펙트, 타격 이펙트, 장판 이펙트를 연결하는 방법만 따로 정리한 문서입니다.

## 1. 전체 구조

이번에 적용된 구조는 아래 순서로 리소스를 찾습니다.

1. 캐릭터별 또는 스킬별로 직접 넣은 리소스를 먼저 사용합니다.
2. 직접 넣은 리소스가 없으면 `DefenseGamePresentationConfig.asset`의 공용 리소스를 사용합니다.
3. 공용 리소스도 없으면 기존 동작을 유지합니다. projectile은 기존 공용 projectile을 쓰고, 별도 이펙트는 출력하지 않습니다.

즉, 모든 캐릭터에 기본 projectile/effect를 한 번만 깔아두고, 특별한 캐릭터만 개별 리소스로 교체하는 방식입니다.

## 2. 공용 리소스 설정

공용 리소스는 아래 파일에서 설정합니다.

`Assets/Data/DefenseGamePresentationConfig.asset`

사용하는 필드는 아래입니다.

- `Projectile Prefab`: 기본 projectile입니다. 평타 원거리와 projectile형 스킬에서 개별 projectile이 없을 때 사용합니다.
- `Default Muzzle Effect Prefab`: 발사 순간 또는 스킬 발동 순간 출력할 공용 이펙트입니다.
- `Default Hit Effect Prefab`: projectile 충돌 또는 근거리 타격 순간 출력할 공용 이펙트입니다.
- `Default Area Effect Prefab`: 장판형 또는 광역형 스킬에서 사용할 공용 범위 이펙트입니다.

추천 세팅은 이렇습니다.

- 처음에는 `Projectile Prefab`만 반드시 연결합니다.
- 공용 발사/타격 이펙트가 생기면 `Default Muzzle Effect Prefab`, `Default Hit Effect Prefab`에 넣습니다.
- 장판형 스킬이 많아지면 `Default Area Effect Prefab`도 넣습니다.

## 3. 평타 리소스 설정

캐릭터별 평타 리소스는 아래 파일에서 설정합니다.

`Assets/Data/CharacterCombatTuningConfig.asset`

각 `hero_xx` 항목의 `Basic Attack Resources`에 연결합니다.

- `Basic Attack Projectile Prefab`: 이 캐릭터만 사용하는 평타 projectile입니다.
- `Basic Attack Muzzle Effect Prefab`: 이 캐릭터가 평타를 발사할 때 나오는 이펙트입니다.
- `Basic Attack Hit Effect Prefab`: 이 캐릭터의 평타가 적중할 때 나오는 이펙트입니다.

동작 방식은 아래와 같습니다.

- `Basic Attack Type = Melee`: `AttackHit` 이벤트 시점에 데미지와 hit effect가 바로 적용됩니다.
- `Basic Attack Type = Ranged`: `FireProjectile` 이벤트 시점에 projectile과 muzzle effect가 나오고, projectile 충돌 시 hit effect와 데미지가 적용됩니다.

비워두면 공용 리소스를 사용합니다.

## 4. 스킬 리소스 설정

스킬별 리소스도 아래 파일에서 설정합니다.

`Assets/Data/CharacterCombatTuningConfig.asset`

각 `hero_xx` 항목의 `Skill 01`, `Skill 02`, `Skill 03` 안에 연결합니다.

- `Projectile Prefab`: projectile형 스킬에서 사용하는 projectile입니다.
- `Muzzle Effect Prefab`: 스킬 발동 또는 projectile 발사 순간 출력할 이펙트입니다.
- `Hit Effect Prefab`: 스킬이 적중할 때 출력할 이펙트입니다.
- `Area Effect Prefab`: 광역/장판 스킬에서 출력할 범위 이펙트입니다.

비워두면 공용 리소스를 사용합니다.

## 5. 스킬 타입별 추천 연결

- 단일 원거리 딜 스킬: `Projectile Prefab`, `Muzzle Effect Prefab`, `Hit Effect Prefab`
- 단일 근거리 딜 스킬: `Hit Effect Prefab`
- 광역 폭발 스킬: `Hit Effect Prefab`, `Area Effect Prefab`
- 장판 데미지 스킬: `Area Effect Prefab`
- 중독 projectile 스킬: `Projectile Prefab`, `Hit Effect Prefab`
- 슬로우 projectile 스킬: `Projectile Prefab`, `Hit Effect Prefab`
- 힐 스킬: `Muzzle Effect Prefab`
- 버프 스킬: `Muzzle Effect Prefab` 또는 `Area Effect Prefab`
- 마나 충전 스킬: `Muzzle Effect Prefab`

## 6. 애니메이션 이벤트와 출력 시점

평타와 스킬은 애니메이션 이벤트 이름에 따라 실제 처리 시점이 달라집니다.

- `AttackHit`: 근거리 평타 타격 시점입니다. 데미지와 hit effect가 바로 적용됩니다.
- `FireProjectile`: 원거리 평타 발사 시점입니다. projectile과 muzzle effect가 나갑니다.
- `SkillHit`: 근거리/즉시형 스킬 적용 시점입니다. 스킬 효과와 hit effect가 적용됩니다.
- `SkillFire`: projectile형 스킬 발사 시점입니다. projectile과 muzzle effect가 나갑니다.
- `SkillApply`: 힐, 버프, 마나 충전 같은 즉시형 스킬 적용 시점입니다.
- `SpawnArea`: 장판형 스킬 생성 시점입니다. area effect가 출력되고 tick 효과가 시작됩니다.
- `PlayEffect`, `PlayEffectKey`: 데미지와 무관한 순수 연출용 이벤트입니다.
- `PlaySound`: 사운드용 이벤트입니다.

권장 규칙은 아래입니다.

- 데미지/효과 타이밍은 `AttackHit`, `FireProjectile`, `SkillHit`, `SkillFire`, `SkillApply`, `SpawnArea`만 사용합니다.
- 단순 이펙트 장식은 `PlayEffect`를 사용합니다.
- projectile 발사형은 이벤트 시점에 데미지를 주지 않고, projectile이 닿을 때 데미지 또는 스킬 효과를 적용합니다.

## 7. prefab 준비 체크리스트

- projectile prefab에는 `Projectile` 컴포넌트가 있어야 합니다.
- 이펙트 prefab은 `ParticleSystem`이나 `AudioSource`가 있으면 재생 길이를 계산해서 자동 제거됩니다.
- 이펙트 prefab이 단순 Mesh나 빈 오브젝트라면 기본 2초 뒤 자동 제거됩니다.
- muzzle effect는 보통 캐릭터의 `FirePoint` 위치에 생성됩니다.
- hit effect는 적중한 몬스터 위치에 생성됩니다.
- area effect는 타겟 위치 또는 캐릭터 위치에 생성됩니다.

## 8. 가장 흔한 세팅 예시

원거리 궁수 캐릭터라면 이렇게 설정합니다.

- `Basic Attack Type`: `Ranged`
- `Basic Attack Projectile Prefab`: 화살 projectile
- `Basic Attack Muzzle Effect Prefab`: 활 시위 발사 이펙트
- `Basic Attack Hit Effect Prefab`: 화살 피격 이펙트
- 애니메이션 이벤트: `FireProjectile`

근거리 전사 캐릭터라면 이렇게 설정합니다.

- `Basic Attack Type`: `Melee`
- `Basic Attack Hit Effect Prefab`: 검격 피격 이펙트
- 애니메이션 이벤트: `AttackHit`

projectile형 스킬이라면 이렇게 설정합니다.

- `Skill Delivery Type`: `Projectile`
- `Projectile Prefab`: 스킬 projectile
- `Muzzle Effect Prefab`: 스킬 발사 이펙트
- `Hit Effect Prefab`: 스킬 적중 이펙트
- 애니메이션 이벤트: `SkillFire`

장판형 스킬이라면 이렇게 설정합니다.

- `Skill Delivery Type`: `GroundArea`
- `Area Effect Prefab`: 장판 이펙트
- `Duration`: 장판 유지 시간
- `Radius`: 장판 범위
- 애니메이션 이벤트: `SpawnArea`

## 9. 코드 기준

리소스 연결 구조가 들어간 코드는 아래 파일에 있습니다.

- 평타 리소스 필드: `Assets/Scripts/DefenseGame/AttackBehavior.cs`
- 스킬 리소스 필드: `Assets/Scripts/DefenseGame/SkillDefinition.cs`
- 공용 fallback 필드: `Assets/Scripts/DefenseGame/GamePresentationConfig.cs`
- 캐릭터별 튜닝 적용: `Assets/Scripts/DefenseGame/CharacterCombatTuningConfig.cs`
- 실제 발사/타격/이펙트 처리: `Assets/Scripts/DefenseGame/DefenderUnit.cs`
- projectile 충돌 시 hit effect 처리: `Assets/Scripts/DefenseGame/Projectile.cs`
- 이펙트 생성/자동 제거 유틸: `Assets/Scripts/DefenseGame/RuntimeEffectUtility.cs`

