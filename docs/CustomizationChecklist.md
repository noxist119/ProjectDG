# Defense Game Customization Checklist

나중에 리소스 교체, 밸런스 조정, UI 개선을 할 때 확인할 항목입니다.

## Art And Prefabs

- [ ] Hero prefab 교체: `CharacterDefinition`의 character id와 prefab 연결 상태 확인
- [ ] Monster prefab 교체: `MonsterDefinition`의 monster id와 prefab 연결 상태 확인
- [ ] Monster roster 분류: `DefenseGamePresentationConfig > Monster Overrides`에서 `Threat Level`을 Regular/MidBoss/Boss로 명확히 지정
- [ ] Monster grade variant 적용: 같은 FBX를 강화 개체로 쓸 때 `Use As Roster Entry`, `Create Grade Variants`, `Max Variant Grade`, `Variant Round Step` 확인
- [ ] Monster material/color 변종 확인: 등급이 올라갈수록 같은 메쉬라도 `accentColor`와 머테리얼 톤이 구분되는지 플레이 테스트
- [ ] Boss prefab 교체: 10라운드 보스용 prefab, 스킬 2개, 방향값 확인
- [ ] 기본 fallback prefab 교체: 데이터 연결이 비었을 때 나오는 기본 hero/monster 확인
- [ ] 3D 배경 교체: 기존 runtime 배경 오브젝트와 겹치지 않는지 확인
- [ ] Shader Graph material 교체: URP 호환, magenta 표시 여부 확인

## Animation

- [ ] Hero animation clip 연결: spawn, idle, walk, win, attack, skill
- [ ] Monster animation clip 연결: spawn, idle, walk loop, attack, skill, death
- [ ] Skill fallback 규칙 확인: skill01/skill02가 비어 있으면 skill03_start, skill03_loop, skill03_end 사용
- [ ] Animation Event 수신 컴포넌트 확인: PlayEffect, PlayEffectTile, PlayEffectKey, SpawnProp, DespawnProp
- [ ] T-pose 발생 여부 확인: 이동 중 walk loop, 공격/스킬 종료 후 idle 전환

## UI And Icons

- [ ] UI skin image 교체: `UiSkinResources` 또는 runtime UI prefab 이미지 확인
- [ ] Lobby UI 이미지 교체: 로비, 매칭, 결과, 인벤토리, 캐릭터 정보, 증강체 패널
- [ ] Bottom HUD 정렬 확인: 소환, 대전, 로비, 덱, 도감 버튼
- [ ] Synergy UI 아이콘 교체: `SynergyExpandedRow_X/IconSlot/IconImage` 위치에 시너지 아이콘 적용
- [ ] Grade card icon 교체: 일반, 레어, 희귀, 전설, 초월 카드 아이콘 적용
- [ ] Text readability 확인: 어두운 배경 위 글자는 흰색/외곽선/그림자 유지
- [ ] Device safe area 확인: 노치/긴 화면/태블릿 비율에서 잘림 없는지 확인

## Gameplay Data

- [ ] Character stats 조정: 체력, 공격력, 치명타, 공격속도, 마나
- [ ] Character range 조정: 캐릭터별 사거리 데이터 확인
- [ ] Monster stats 조정: 체력, 공격력, 공격속도, 스킬, 보스 스킬
- [ ] Monster 등장 라운드 조정: 일반몹/중간보스/보스 변종의 `minRound`가 의도한 난이도 곡선에 맞는지 확인
- [ ] Monster 등급 풀 확인: 초반 Regular는 약한 외형 위주, 후반 Regular/MidBoss/Boss는 같은 메쉬라도 등급 변종으로 강화 느낌을 주는지 확인
- [ ] Monster movement speed 조정: `MonsterCombatTuningConfig`에서 monster id별 이동속도 확인
- [ ] Mana gain 조정: 시간경과, 피격, 공격, 스킬 사용 조건 확인
- [ ] Merge recipe 조정: 등급별 합성 조건, Mythic 이후 최종 조합식 확인
- [ ] Summon board capacity 확인: 10칸 제한, 자리 교체 드래그, 합성 불가 상태 처리
- [ ] Tactical mission 조정: 골드 창고, 무결 방어, 합성 러시, 역할 컬렉터, 소수 정예, 보스 브레이커 조건/보상 확인
- [ ] Mission reward 밸런스 확인: 보너스 골드, 라운드 보너스, 소환비용 할인 수치가 과하지 않은지 테스트

## Effects And Feedback

- [ ] Hit feedback 확인: 피격 시 하이라이트/림라이트/흰색 플래시 적용
- [ ] Damage text 확인: 크리티컬, 스킬, 일반 데미지 구분
- [ ] Mana bar / HP bar 확인: 체력 감소와 마나 증가가 실시간 반영되는지 확인
- [ ] Death VFX 교체: `Effect_Die_Friendly` 연결 확인
- [ ] Merge success popup 확인: 높은 등급 합성 시 축하 연출 강화
- [ ] Round flow feedback 확인: 5초 카운트다운, 라운드 종료 UI, win motion

## Build And Release

- [ ] Android build settings 확인: package name, orientation, resolution, keystore
- [ ] APK build smoke test: 시작, 소환, 전투, 합성, 보스 라운드, 결과 화면 확인
- [ ] Warning-free compile 확인: `dotnet build .\Assembly-CSharp.csproj --no-restore`
- [ ] Unity console 확인: error 0개, animation event error 0개
