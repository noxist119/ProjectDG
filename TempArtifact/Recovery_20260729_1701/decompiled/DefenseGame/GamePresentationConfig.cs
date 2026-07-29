using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

[CreateAssetMenu(fileName = "DefenseGamePresentationConfig", menuName = "Defense Game/Presentation Config")]
public class GamePresentationConfig : ScriptableObject
{
	[Header("Prefab Overrides")]
	public GameObject backgroundPrefab;

	public GameObject defaultDefenderPrefab;

	public GameObject summonedDefenderPrefab;

	public GameObject defaultMonsterPrefab;

	public GameObject monsterDeathEffectPrefab;

	public GameObject defenderDeathEffectPrefab;

	public GameObject projectilePrefab;

	public GameObject defaultMuzzleEffectPrefab;

	public GameObject defaultHitEffectPrefab;

	public GameObject defaultAreaEffectPrefab;

	public GameObject spawnPortalPrefab;

	public GameObject goalPrefab;

	public GameObject centerCrystalPrefab;

	public GameObject flankTowerPrefab;

	public GameObject skyAccentPrefab;

	[Header("UI")]
	public UiSkinResources uiSkin;

	public Font uiFont;

	public Color hudTextColor = Color.white;

	public Color buttonColor = new Color(0.16f, 0.19f, 0.26f, 0.92f);

	public Color buttonTextColor = Color.white;

	[TextArea]
	public string hintText = "Space Round | S Summon | 1-4 Merge | C Add Heroes | M Add Monsters";

	[Header("Stage Colors")]
	public Color groundColor = new Color(0.08f, 0.11f, 0.14f);

	public Color boardStripColor = new Color(0.12f, 0.18f, 0.24f);

	public Color enemyRunwayColor = new Color(0.18f, 0.1f, 0.11f);

	public Color midBridgeColor = new Color(0.25f, 0.29f, 0.36f);

	public Color northWallColor = new Color(0.17f, 0.14f, 0.22f);

	public Color southWallColor = new Color(0.13f, 0.19f, 0.24f);

	public Color sideWallColor = new Color(0.12f, 0.14f, 0.18f);

	public Color gateColor = new Color(0.24f, 0.54f, 0.72f);

	public Color gateCoreColor = new Color(0.38f, 0.89f, 1f);

	public Color crystalColor = new Color(0.3f, 0.95f, 0.86f);

	[Header("Palettes")]
	public Color[] slotColors;

	public Color[] laneColors;

	[Header("Runtime Rendering")]
	[Tooltip("OFF가 기본입니다. 유닛별 런타임 색상 틴트는 개성을 주지만 MaterialPropertyBlock 때문에 GPU Instancing/SRP Batcher 효율이 떨어질 수 있습니다.")]
	public bool usePerInstanceUnitTint;

	[Tooltip("SRP Batcher 우선이 기본입니다. 같은 Mesh와 같은 Material을 대량으로 찍는 특수 몬스터에만 켜세요.")]
	public bool enableRuntimeGpuInstancing;

	[Tooltip("지원 기기에서 동일 Mesh/Material의 SkinnedMeshRenderer를 GPU 본 버퍼 + Indirect Instancing으로 묶습니다. 미지원 셰이더/블렌드셰이프/그래픽 API는 기존 렌더러로 자동 폴백합니다.")]
	public bool enableGpuSkinnedUnitBatching = true;

	[Tooltip("저사양 모바일 프로필에서만 GPU 스키닝 배칭을 사용합니다. 표준 프로필은 기존 URP Lit 렌더링 품질을 유지합니다.")]
	public bool gpuSkinnedBatchingLowEndOnly = true;

	[Min(2f)]
	[Tooltip("같은 Mesh/Material 유닛이 이 수 이상일 때만 Indirect 배치로 전환합니다. 적은 수는 기존 SkinnedMeshRenderer가 더 저렴합니다.")]
	public int gpuSkinnedBatchMinInstanceCount = 4;

	[Tooltip("같은 외형의 소환 유닛을 포함한 수비 유닛도 GPU 스키닝 배칭 대상으로 허용합니다.")]
	public bool gpuSkinnedBatchDefenders = true;

	[Tooltip("보스는 복잡한 셰이더/블렌드셰이프 사용 가능성이 높아 기본적으로 제외합니다.")]
	public bool gpuSkinnedBatchBosses;

	[Tooltip("일반 몬스터의 대기/이동 Animator를 낮은 주기로 샘플링하고 공격/스킬 중에는 자동으로 정상 주기로 복귀합니다.")]
	public bool enableUnitAnimatorLod = true;

	[Tooltip("저사양 모바일 프로필에서만 Animator 업데이트 LOD를 적용합니다.")]
	public bool unitAnimatorLodLowEndOnly = true;

	[Range(5f, 30f)]
	[Tooltip("저사양에서 일반 몬스터의 대기/이동 애니메이션을 평가할 초당 횟수입니다.")]
	public int lowEndRegularMonsterAnimatorFps = 15;

	[Tooltip("수비 유닛도 낮은 애니메이션 주기를 사용합니다. 조작 피드백을 위해 기본값은 OFF입니다.")]
	public bool unitAnimatorLodDefenders;

	[Tooltip("보스도 낮은 애니메이션 주기를 사용합니다. 연출과 패턴 가독성을 위해 기본값은 OFF입니다.")]
	public bool unitAnimatorLodBosses;

	[Range(0f, 0.5f)]
	[Tooltip("공격/스킬 종료 직후 정상 애니메이션 주기를 유지하는 시간입니다.")]
	public float unitAnimatorActionGraceDuration = 0.12f;

	[Tooltip("Keeps runtime defender and monster renderers from casting real-time shadows. Prefabs should also keep Cast Shadows off.")]
	public bool forceRuntimeUnitCastShadowsOff = true;

	[Tooltip("AnimationEvent OverrideMaterial(string)에서 이름으로 찾을 머테리얼입니다. 에디터 동기화와 빌드 전처리가 이벤트 문자열을 기준으로 자동 갱신합니다.")]
	public Material[] animationEventMaterials = Array.Empty<Material>();

	[Header("Character Overrides")]
	public List<CharacterPresentationOverride> characterOverrides = new List<CharacterPresentationOverride>();

	public bool useRandomCharacterPrefabFallback = true;

	[Header("Monster Overrides")]
	public List<MonsterPresentationOverride> monsterOverrides = new List<MonsterPresentationOverride>();

	public void ApplyToCharacter(CharacterDefinition definition)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if (definition == null)
		{
			return;
		}
		CharacterPresentationOverride characterPresentationOverride = characterOverrides.Find((CharacterPresentationOverride candidate) => candidate != null && candidate.characterId == definition.id);
		if (characterPresentationOverride == null)
		{
			if (!useRandomCharacterPrefabFallback || characterOverrides.Count != 0 || !TryGetRandomCharacterOverride(out var entry))
			{
				return;
			}
			characterPresentationOverride = entry;
		}
		if ((Object)(object)characterPresentationOverride.prefab != (Object)null)
		{
			definition.prefab = characterPresentationOverride.prefab;
		}
		if (characterPresentationOverride.overrideColor)
		{
			definition.accentColor = characterPresentationOverride.accentColor;
		}
	}

	public void ApplyToMonster(MonsterDefinition definition)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		if (definition == null)
		{
			return;
		}
		MonsterPresentationOverride monsterPresentationOverride = monsterOverrides.Find((MonsterPresentationOverride candidate) => candidate != null && candidate.monsterId == definition.id);
		if (monsterPresentationOverride == null && TryGetOrderedMonsterOverride(definition, out var entry))
		{
			monsterPresentationOverride = entry;
		}
		if (monsterPresentationOverride != null)
		{
			if ((Object)(object)monsterPresentationOverride.prefab != (Object)null)
			{
				definition.prefab = monsterPresentationOverride.prefab;
			}
			if (monsterPresentationOverride.overrideColor)
			{
				definition.accentColor = monsterPresentationOverride.accentColor;
			}
		}
	}

	public int GetHighestConfiguredCharacterIndex()
	{
		int num = 0;
		for (int i = 0; i < characterOverrides.Count; i++)
		{
			CharacterPresentationOverride characterPresentationOverride = characterOverrides[i];
			if (characterPresentationOverride != null && !((Object)(object)characterPresentationOverride.prefab == (Object)null) && TryParseIndex(characterPresentationOverride.characterId, out var index))
			{
				num = Mathf.Max(num, index + 1);
			}
		}
		return num;
	}

	private bool TryGetRandomCharacterOverride(out CharacterPresentationOverride entry)
	{
		entry = null;
		List<CharacterPresentationOverride> list = characterOverrides.FindAll((CharacterPresentationOverride candidate) => candidate != null && (Object)(object)candidate.prefab != (Object)null);
		if (list.Count == 0)
		{
			return false;
		}
		entry = list[Random.Range(0, list.Count)];
		return true;
	}

	public bool HasMonsterRosterEntries(MonsterThreatLevel threatLevel)
	{
		return monsterOverrides.Exists((MonsterPresentationOverride candidate) => candidate != null && candidate.useAsRosterEntry && (Object)(object)candidate.prefab != (Object)null && candidate.threatLevel == threatLevel);
	}

	public List<MonsterPresentationOverride> GetMonsterRosterEntries(MonsterThreatLevel threatLevel)
	{
		return monsterOverrides.FindAll((MonsterPresentationOverride candidate) => candidate != null && candidate.useAsRosterEntry && (Object)(object)candidate.prefab != (Object)null && candidate.threatLevel == threatLevel);
	}

	private bool TryGetOrderedMonsterOverride(MonsterDefinition definition, out MonsterPresentationOverride entry)
	{
		entry = null;
		if (definition == null || !TryParseIndex(definition.id, out var index))
		{
			return false;
		}
		List<MonsterPresentationOverride> list = monsterOverrides.FindAll((MonsterPresentationOverride candidate) => candidate != null && (Object)(object)candidate.prefab != (Object)null && candidate.threatLevel == definition.threatLevel);
		if (index < 0 || index >= list.Count)
		{
			return false;
		}
		entry = list[index];
		return true;
	}

	private bool TryParseIndex(string definitionId, out int index)
	{
		index = -1;
		if (string.IsNullOrWhiteSpace(definitionId))
		{
			return false;
		}
		string[] array = definitionId.Split('_');
		if (array.Length == 0)
		{
			return false;
		}
		if (!int.TryParse(array[^1], out var result))
		{
			return false;
		}
		index = result - 1;
		return index >= 0;
	}
}
