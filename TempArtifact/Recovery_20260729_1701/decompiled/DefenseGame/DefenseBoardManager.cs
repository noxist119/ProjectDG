using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DefenseGame;

public class DefenseBoardManager : MonoBehaviour
{
	private class UltimateMergeRecipe
	{
		public readonly string name;

		public readonly string displayText;

		public readonly int mythicCount;

		public readonly int legendaryCount;

		public readonly int epicCount;

		public readonly string[] requiredCharacterIds;

		public readonly string[] resultCharacterIds;

		public UltimateMergeRecipe(string name, string displayText, int mythicCount, int legendaryCount, int epicCount, string[] requiredCharacterIds, string[] resultCharacterIds = null)
		{
			this.name = name;
			this.displayText = displayText;
			this.mythicCount = mythicCount;
			this.legendaryCount = legendaryCount;
			this.epicCount = epicCount;
			this.requiredCharacterIds = requiredCharacterIds ?? new string[0];
			this.resultCharacterIds = resultCharacterIds ?? new string[0];
		}
	}

	private static readonly UltimateMergeRecipe[] UltimateRecipes = new UltimateMergeRecipe[11]
	{
		new UltimateMergeRecipe("Fever Engine Rite", "Mythic Fighter + Mana Link + Focus Dealer", 0, 0, 0, new string[3] { "hero_31", "hero_13", "hero_10" }, new string[2] { "hero_51", "hero_53" }),
		new UltimateMergeRecipe("Volcanic Core Rite", "Mythic Core + Area Damage + Shield Break", 0, 0, 0, new string[3] { "hero_32", "hero_01", "hero_09" }, new string[1] { "hero_52" }),
		new UltimateMergeRecipe("Soul Battery Rite", "Drain + Mana + Heal + Guard + Speed", 0, 0, 0, new string[5] { "hero_11", "hero_13", "hero_02", "hero_05", "hero_14" }, new string[1] { "hero_54" }),
		new UltimateMergeRecipe("Venom Bulwark Rite", "Mythic Venom + Poison + Freeze Zone", 0, 0, 0, new string[3] { "hero_33", "hero_04", "hero_22" }, new string[2] { "hero_52", "hero_54" }),
		new UltimateMergeRecipe("Thunder Control Rite", "Mythic Fighter + Life Chain + Petrify", 0, 0, 0, new string[3] { "hero_31", "hero_07", "hero_08" }, new string[1] { "hero_51" }),
		new UltimateMergeRecipe("Iron Bastion Rite", "Mythic Combat + Shield (완화 조합)", 0, 0, 0, new string[2] { "hero_31", "hero_05" }, new string[1] { "hero_55" }),
		new UltimateMergeRecipe("Clockwork Barrage Rite", "Mythic Wolf + Battery (완화 조합)", 0, 0, 0, new string[2] { "hero_32", "hero_13" }, new string[1] { "hero_56" }),
		new UltimateMergeRecipe("Fractured Arsenal Rite", "Mythic Infection + Assassin (완화 조합)", 0, 0, 0, new string[2] { "hero_33", "hero_12" }, new string[1] { "hero_57" }),
		new UltimateMergeRecipe("Crown Overflow Rite", "Mythic 2 + Legendary 1", 2, 1, 0, null, new string[3] { "hero_51", "hero_52", "hero_53" }),
		new UltimateMergeRecipe("Eclipse Overflow Rite", "Mythic 1 + Legendary 1 + Epic 1", 1, 1, 1, null, new string[7] { "hero_51", "hero_52", "hero_53", "hero_54", "hero_55", "hero_56", "hero_57" }),
		new UltimateMergeRecipe("Dragon Overflow Rite", "Mythic 2 + Epic 2", 2, 0, 2, null, new string[2] { "hero_53", "hero_54" })
	};

	[SerializeField]
	private List<BoardSlot> slots = new List<BoardSlot>();

	[SerializeField]
	private DefenderUnit fallbackUnitPrefab;

	[SerializeField]
	private float dragHeight = 1.4f;

	[SerializeField]
	private float unitPickupRadius = 0.48f;

	[SerializeField]
	private float slotDropRadius = 0.58f;

	[SerializeField]
	private float pointerPickupPlaneHeight = 0.75f;

	[SerializeField]
	private float holdToDragDelay = 0.24f;

	[SerializeField]
	private float dragStartScreenDistance = 18f;

	[SerializeField]
	private float rangeIndicatorHeight = 0.16f;

	[SerializeField]
	private float rangeIndicatorLineWidth = 0.06f;

	[SerializeField]
	private int rangeIndicatorSegments = 96;

	[SerializeField]
	private Color rangeIndicatorColor = new Color(0.12f, 1f, 0.86f, 0.95f);

	[SerializeField]
	private int baseActiveSlotCount = 10;

	[SerializeField]
	private int frontSlotUnlockInterval = 15;

	[SerializeField]
	private int maxFrontUnlockCount = 5;

	private DefenderUnit draggedUnit;

	private BoardSlot draggedOriginSlot;

	private Collider[] draggedColliders;

	private Plane dragPlane;

	private Vector3 dragOffset;

	private DefenderUnit pendingPointerUnit;

	private Vector2 pointerDownScreenPosition;

	private float pointerDownTime;

	private LineRenderer rangeIndicatorLine;

	private DefenderUnit selectedRangeUnit;

	private float recipeMarkerRefreshTimer;

	private string previewUltimateRecipeName;

	private bool ultimateRecipePreviewActive;

	private Camera cachedGameplayCamera;

	private bool rangeIndicatorDirty = true;

	private Vector3 lastRangeIndicatorCenter;

	private float lastRangeIndicatorRadius = -1f;

	private float lastRangeIndicatorWidth = -1f;

	private int lastRangeIndicatorSegments = -1;

	private Color lastRangeIndicatorColor;

	public IReadOnlyList<BoardSlot> Slots => slots;

	public int EmptySlotCount => slots.Count((BoardSlot slot) => (Object)(object)slot != (Object)null && slot.IsAvailable && slot.IsEmpty);

	public int UnitCount => slots.Count((BoardSlot slot) => (Object)(object)slot != (Object)null && (Object)(object)slot.OccupiedUnit != (Object)null);

	public int UnlockedSlotCount => slots.Count((BoardSlot slot) => (Object)(object)slot != (Object)null && slot.IsAvailable);

	public int LockedSlotCount => slots.Count((BoardSlot slot) => (Object)(object)slot != (Object)null && slot.IsLocked);

	public DefenderUnit SelectedUnit => selectedRangeUnit;

	public string LastMergeFailureReason { get; private set; } = string.Empty;

	public event Action<DefenderUnit> OnSelectedUnitChanged;

	private void Awake()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (slots.Count == 0)
		{
			slots = ((Component)this).GetComponentsInChildren<BoardSlot>(true).ToList();
		}
		dragPlane = new Plane(Vector3.up, new Vector3(0f, dragHeight, 0f));
		cachedGameplayCamera = Camera.main;
	}

	private void Update()
	{
		HandleDragging();
		UpdateRangeIndicator();
		recipeMarkerRefreshTimer -= Time.deltaTime;
		if (recipeMarkerRefreshTimer <= 0f)
		{
			recipeMarkerRefreshTimer = 0.2f;
			RefreshUltimateRecipeMarkers();
		}
	}

	public void Configure(List<BoardSlot> newSlots, DefenderUnit fallbackPrefab)
	{
		slots = newSlots;
		fallbackUnitPrefab = fallbackPrefab;
		RefreshSlotLocks(0);
	}

	public int RefreshSlotLocks(int completedRound, bool playUnlockFeedback = false)
	{
		int num = ResolveActiveSlotCount(completedRound);
		int num2 = 0;
		for (int i = 0; i < slots.Count; i++)
		{
			BoardSlot boardSlot = slots[i];
			if ((Object)(object)boardSlot == (Object)null)
			{
				continue;
			}
			bool isLocked = boardSlot.IsLocked;
			bool flag = i >= num;
			boardSlot.SetLocked(flag, string.Empty);
			if (isLocked && !flag)
			{
				num2++;
				if (playUnlockFeedback)
				{
					boardSlot.PlayUnlockFeedback();
				}
			}
		}
		return num2;
	}

	private int ResolveActiveSlotCount(int completedRound)
	{
		int num = Mathf.Clamp(Mathf.Max(0, baseActiveSlotCount), 0, slots.Count);
		int num2 = Mathf.Max(1, frontSlotUnlockInterval);
		int num3 = Mathf.Max(1, completedRound + 1);
		int num4 = Mathf.Clamp(num3 / num2, 0, Mathf.Max(0, maxFrontUnlockCount));
		return Mathf.Clamp(num + num4, 0, slots.Count);
	}

	private int ResolveSlotUnlockRound(int slotIndex)
	{
		int num = Mathf.Max(0, slotIndex - Mathf.Max(0, baseActiveSlotCount));
		return Mathf.Max(1, (num + 1) * Mathf.Max(1, frontSlotUnlockInterval));
	}

	public void ClearAllDeployedUnits()
	{
		for (int i = 0; i < slots.Count; i++)
		{
			BoardSlot boardSlot = slots[i];
			DefenderUnit defenderUnit = (((Object)(object)boardSlot != (Object)null) ? boardSlot.OccupiedUnit : null);
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				defenderUnit.RemoveFromBoard();
				Object.Destroy((Object)(object)((Component)defenderUnit).gameObject);
			}
		}
		draggedUnit = null;
		draggedOriginSlot = null;
		draggedColliders = null;
		pendingPointerUnit = null;
		HideRangeIndicator();
		previewUltimateRecipeName = null;
		ultimateRecipePreviewActive = false;
	}

	public bool TrySpawnUnit(CharacterDefinition definition, DefenderUnit prefabOverride = null)
	{
		DefenderUnit spawnedUnit;
		return TrySpawnUnit(definition, prefabOverride, out spawnedUnit);
	}

	public bool TrySpawnUnit(CharacterDefinition definition, DefenderUnit prefabOverride, out DefenderUnit spawnedUnit)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		spawnedUnit = null;
		BoardSlot boardSlot = slots.FirstOrDefault((BoardSlot slot) => (Object)(object)slot != (Object)null && slot.IsAvailable && slot.IsEmpty);
		if ((Object)(object)boardSlot == (Object)null || definition == null)
		{
			return false;
		}
		GameObject val = (((Object)(object)definition.prefab != (Object)null) ? definition.prefab : (((Object)(object)prefabOverride != (Object)null) ? ((Component)prefabOverride).gameObject : (((Object)(object)fallbackUnitPrefab != (Object)null) ? ((Component)fallbackUnitPrefab).gameObject : null)));
		if ((Object)(object)val == (Object)null)
		{
			Debug.LogError((object)"No DefenderUnit prefab assigned.");
			return false;
		}
		GameObject val2 = Object.Instantiate<GameObject>(val, boardSlot.UnitAnchor.position, Quaternion.identity);
		DefenderUnit defenderUnit = val2.GetComponent<DefenderUnit>();
		if ((Object)(object)defenderUnit == (Object)null)
		{
			defenderUnit = val2.AddComponent<DefenderUnit>();
		}
		defenderUnit.AdoptRuntimeTemplate(((Object)(object)prefabOverride != (Object)null) ? prefabOverride : fallbackUnitPrefab);
		((Component)defenderUnit).gameObject.SetActive(true);
		boardSlot.AssignUnit(defenderUnit);
		defenderUnit.Initialize(definition);
		RuntimeCombatFeedback.ShowGroundPulse(boardSlot.UnitAnchor.position, Color.Lerp(definition.accentColor, Color.white, 0.25f), 0.72f, 0.42f, 0.1f);
		RuntimeGameFeel.PlaySummonArrivalVfx(boardSlot.UnitAnchor.position, definition.accentColor, definition.grade);
		spawnedUnit = defenderUnit;
		return true;
	}

	public bool TryMergeUnitsOfGrade(CharacterGrade grade, CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride = null)
	{
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		mergeResult = default(MergeResultInfo);
		LastMergeFailureReason = string.Empty;
		if (grade == CharacterGrade.Mythic)
		{
			return TryMergeUltimate(database, out mergeResult, prefabOverride);
		}
		if (grade == CharacterGrade.Transcendent)
		{
			LastMergeFailureReason = "초월 등급은 일반 합성을 진행할 수 없습니다.";
			return false;
		}
		HashSet<DefenderUnit> reservedRecipeUnits = SelectReservedUltimateRecipeUnits();
		List<DefenderUnit> list = (from slot in slots
			where (Object)(object)slot != (Object)null && !slot.IsEmpty
			select slot.OccupiedUnit into unit
			where (Object)(object)unit != (Object)null && unit.Grade == grade
			orderby reservedRecipeUnits.Contains(unit) ? 1 : 0
			select unit).Take(3).ToList();
		if (list.Count < 3)
		{
			LastMergeFailureReason = "합성 재료가 부족합니다.  " + list.Count + "/3";
			return false;
		}
		CharacterGrade characterGrade = grade + 1;
		CharacterDefinition randomCharacterByGrade = database.GetRandomCharacterByGrade(characterGrade);
		if (randomCharacterByGrade == null)
		{
			LastMergeFailureReason = "상위 등급 결과 유닛 데이터가 없습니다.";
			return false;
		}
		BoardSlot currentSlot = list[0].CurrentSlot;
		if ((Object)(object)currentSlot == (Object)null)
		{
			LastMergeFailureReason = "합성 결과를 배치할 슬롯을 찾지 못했습니다.";
			return false;
		}
		GameObject val = (((Object)(object)randomCharacterByGrade.prefab != (Object)null) ? randomCharacterByGrade.prefab : (((Object)(object)prefabOverride != (Object)null) ? ((Component)prefabOverride).gameObject : (((Object)(object)fallbackUnitPrefab != (Object)null) ? ((Component)fallbackUnitPrefab).gameObject : null)));
		if ((Object)(object)val == (Object)null)
		{
			LastMergeFailureReason = "상위 등급 유닛 프리팹을 찾지 못했습니다.";
			Debug.LogError((object)"No DefenderUnit prefab assigned for merge result.");
			return false;
		}
		float inheritedAttackPower = list.Sum((DefenderUnit unit) => ((Object)(object)unit != (Object)null) ? unit.EffectiveAttackPower : 0f) * 0.98f;
		float inheritedMaxHealth = list.Sum((DefenderUnit unit) => ((Object)(object)unit != (Object)null) ? unit.MaxHealth : 0f) * 0.94f;
		for (int num = 0; num < list.Count; num++)
		{
			list[num].RemoveFromBoard();
			Object.Destroy((Object)(object)((Component)list[num]).gameObject);
		}
		GameObject val2 = Object.Instantiate<GameObject>(val, currentSlot.UnitAnchor.position, Quaternion.identity);
		DefenderUnit defenderUnit = val2.GetComponent<DefenderUnit>();
		if ((Object)(object)defenderUnit == (Object)null)
		{
			defenderUnit = val2.AddComponent<DefenderUnit>();
		}
		defenderUnit.AdoptRuntimeTemplate(((Object)(object)prefabOverride != (Object)null) ? prefabOverride : fallbackUnitPrefab);
		((Component)defenderUnit).gameObject.SetActive(true);
		currentSlot.AssignUnit(defenderUnit);
		defenderUnit.Initialize(randomCharacterByGrade);
		defenderUnit.ApplyMergeInheritance(inheritedAttackPower, inheritedMaxHealth);
		SpawnMergeVfx(currentSlot, randomCharacterByGrade.accentColor, characterGrade, ultimate: false);
		mergeResult = new MergeResultInfo
		{
			sourceGrade = grade,
			resultGrade = characterGrade,
			sourceDescription = CharacterGradeUtility.GetDisplayName(grade) + " x3",
			consumedUnitCount = list.Count,
			resultCharacterName = randomCharacterByGrade.displayName,
			resultColor = randomCharacterByGrade.accentColor
		};
		return true;
	}

	public bool CanMergeUltimate(CharacterDatabase database = null)
	{
		UltimateMergeRecipe recipe;
		return TryFindUltimateRecipe(database, out recipe);
	}

	public string GetUltimateMergeStatus(CharacterDatabase database = null)
	{
		if (TryFindUltimateRecipe(database, out var _))
		{
			return "가능";
		}
		if (HasBlockedReadyUltimateRecipe(database))
		{
			return "보유중";
		}
		GetBestUltimateRecipeProgress(out var bestProgress, out var bestRequired);
		return "재료 " + bestProgress + "/" + Mathf.Max(1, bestRequired);
	}

	public string GetUltimateMergeDetailStatus(CharacterDatabase database)
	{
		UltimateMergeRecipe bestUltimateRecipe = GetBestUltimateRecipe(database);
		if (bestUltimateRecipe == null)
		{
			return HasBlockedReadyUltimateRecipe(database) ? "이미 보유한 초월 | 다른 레시피 필요" : "초월 레시피 없음";
		}
		int ultimateRecipeProgress = GetUltimateRecipeProgress(bestUltimateRecipe);
		int num = Mathf.Max(1, GetUltimateRecipeRequiredCount(bestUltimateRecipe));
		string text = ((ultimateRecipeProgress >= num) ? "초월 준비 완료" : ("초월 " + ultimateRecipeProgress + "/" + num));
		string text2 = ((bestUltimateRecipe.requiredCharacterIds != null && bestUltimateRecipe.requiredCharacterIds.Length != 0) ? BuildRequiredCharacterRecipeStatus(bestUltimateRecipe, database) : BuildGradeRecipeStatus(bestUltimateRecipe));
		return text + "  |  " + text2;
	}

	public string GetUltimateMergeActionStatus(CharacterDatabase database)
	{
		UltimateMergeRecipe bestUltimateRecipe = GetBestUltimateRecipe(database);
		if (bestUltimateRecipe == null)
		{
			return HasBlockedReadyUltimateRecipe(database) ? "이미 보유한 초월" : "초월 레시피 없음";
		}
		int ultimateRecipeProgress = GetUltimateRecipeProgress(bestUltimateRecipe);
		int num = Mathf.Max(1, GetUltimateRecipeRequiredCount(bestUltimateRecipe));
		if (ultimateRecipeProgress >= num)
		{
			return "초월 조합 실행";
		}
		string firstMissingRecipeMaterialName = GetFirstMissingRecipeMaterialName(bestUltimateRecipe, database);
		if (!string.IsNullOrWhiteSpace(firstMissingRecipeMaterialName))
		{
			return ultimateRecipeProgress + "/" + num + "  " + firstMissingRecipeMaterialName + " 찾기";
		}
		return ultimateRecipeProgress + "/" + num + "  핵심 재료 보존";
	}

	public string GetUltimateRecipeBingoStatus(CharacterDatabase database)
	{
		if (UltimateRecipes == null || UltimateRecipes.Length == 0)
		{
			return "레시피 빙고 없음";
		}
		List<UltimateMergeRecipe> list = new List<UltimateMergeRecipe>(UltimateRecipes);
		list.Sort(CompareUltimateRecipeBingoPriority);
		List<string> list2 = new List<string>();
		list2.Add("레시피 빙고  한 줄 완성: 운명 +20");
		for (int i = 0; i < list.Count; i++)
		{
			UltimateMergeRecipe ultimateMergeRecipe = list[i];
			int ultimateRecipeProgress = GetUltimateRecipeProgress(ultimateMergeRecipe);
			int num = Mathf.Max(1, GetUltimateRecipeRequiredCount(ultimateMergeRecipe));
			string text = ((ultimateRecipeProgress >= num) ? "완성" : (ultimateRecipeProgress + "/" + num));
			string text2 = ((i == 0) ? "TOP " : "    ");
			list2.Add(text2 + text + "  " + CompactRecipeName(ultimateMergeRecipe.name) + "  " + BuildRecipeBingoMaterialStatus(ultimateMergeRecipe, database));
		}
		return string.Join("\n", list2);
	}

	public string[] GetReadyUltimateRecipeNames(CharacterDatabase database = null)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < UltimateRecipes.Length; i++)
		{
			UltimateMergeRecipe ultimateMergeRecipe = UltimateRecipes[i];
			if (ultimateMergeRecipe != null && CanSatisfyUltimateRecipe(ultimateMergeRecipe) && HasAvailableUltimateResult(database, ultimateMergeRecipe))
			{
				list.Add(ultimateMergeRecipe.name);
			}
		}
		return list.ToArray();
	}

	public UltimateRecipeOption[] GetReadyUltimateRecipeOptions(CharacterDatabase database = null)
	{
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		List<UltimateRecipeOption> list = new List<UltimateRecipeOption>();
		for (int i = 0; i < UltimateRecipes.Length; i++)
		{
			UltimateMergeRecipe ultimateMergeRecipe = UltimateRecipes[i];
			if (ultimateMergeRecipe != null && CanSatisfyUltimateRecipe(ultimateMergeRecipe) && HasAvailableUltimateResult(database, ultimateMergeRecipe))
			{
				List<CharacterDefinition> list2 = (from character in ResolveUltimateMergeResultCandidates(database, ultimateMergeRecipe)
					where !IsBlockedTranscendentResult(character)
					select character).ToList();
				string resultSummary = ((list2.Count > 0) ? string.Join(" / ", list2.Select((CharacterDefinition character) => character.displayName).Distinct().ToArray()) : "결과 확인 필요");
				Color accentColor = ((list2.Count > 0) ? list2[0].accentColor : CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f)));
				string materialSummary = ((ultimateMergeRecipe.requiredCharacterIds != null && ultimateMergeRecipe.requiredCharacterIds.Length != 0) ? string.Join(" + ", ultimateMergeRecipe.requiredCharacterIds.Select((string id) => ResolveCharacterName(database, id)).ToArray()) : BuildGradeRecipeStatus(ultimateMergeRecipe));
				list.Add(new UltimateRecipeOption(ultimateMergeRecipe.name, CompactRecipeName(ultimateMergeRecipe.name), materialSummary, resultSummary, accentColor, isReady: true, GetUltimateRecipeRequiredCount(ultimateMergeRecipe), GetUltimateRecipeRequiredCount(ultimateMergeRecipe), string.Empty));
			}
		}
		return list.ToArray();
	}

	public UltimateRecipeOption[] GetAllUltimateRecipeOptions(CharacterDatabase database = null)
	{
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		List<UltimateRecipeOption> list = new List<UltimateRecipeOption>();
		for (int i = 0; i < UltimateRecipes.Length; i++)
		{
			UltimateMergeRecipe ultimateMergeRecipe = UltimateRecipes[i];
			if (ultimateMergeRecipe != null)
			{
				List<CharacterDefinition> list2 = (from character in ResolveUltimateMergeResultCandidates(database, ultimateMergeRecipe)
					where !IsBlockedTranscendentResult(character)
					select character).ToList();
				string resultSummary = ((list2.Count > 0) ? string.Join(" / ", list2.Select((CharacterDefinition character) => character.displayName).Distinct().ToArray()) : "결과 유닛 확인 필요");
				Color accentColor = ((list2.Count > 0) ? list2[0].accentColor : CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f)));
				string materialSummary = ((ultimateMergeRecipe.requiredCharacterIds != null && ultimateMergeRecipe.requiredCharacterIds.Length != 0) ? string.Join(" + ", ultimateMergeRecipe.requiredCharacterIds.Select((string id) => ResolveCharacterName(database, id)).ToArray()) : BuildGradeRecipeStatus(ultimateMergeRecipe));
				int ultimateRecipeProgress = GetUltimateRecipeProgress(ultimateMergeRecipe);
				int num = Mathf.Max(1, GetUltimateRecipeRequiredCount(ultimateMergeRecipe));
				bool isReady = ultimateRecipeProgress >= num && HasAvailableUltimateResult(database, ultimateMergeRecipe);
				list.Add(new UltimateRecipeOption(ultimateMergeRecipe.name, CompactRecipeName(ultimateMergeRecipe.name), materialSummary, resultSummary, accentColor, isReady, ultimateRecipeProgress, num, BuildMissingRecipeMaterialSummary(ultimateMergeRecipe, database)));
			}
		}
		list.Sort(delegate(UltimateRecipeOption left, UltimateRecipeOption right)
		{
			if (left.isReady != right.isReady)
			{
				return (!left.isReady) ? 1 : (-1);
			}
			int num2 = left.progress * Mathf.Max(1, right.required);
			int num3 = right.progress * Mathf.Max(1, left.required);
			return (num3 != num2) ? num3.CompareTo(num2) : left.displayName.CompareTo(right.displayName);
		});
		return list.ToArray();
	}

	private string BuildMissingRecipeMaterialSummary(UltimateMergeRecipe recipe, CharacterDatabase database)
	{
		List<string> list = new List<string>();
		if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
		{
			List<DefenderUnit> recipeCandidateUnits = GetRecipeCandidateUnits();
			for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
			{
				string id = recipe.requiredCharacterIds[i];
				DefenderUnit defenderUnit = recipeCandidateUnits.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == id);
				if ((Object)(object)defenderUnit == (Object)null)
				{
					list.Add(ResolveCharacterName(database, id));
				}
				else
				{
					recipeCandidateUnits.Remove(defenderUnit);
				}
			}
		}
		else
		{
			AddMissingGradeSummary(list, CharacterGrade.Mythic, recipe.mythicCount);
			AddMissingGradeSummary(list, CharacterGrade.Legendary, recipe.legendaryCount);
			AddMissingGradeSummary(list, CharacterGrade.Epic, recipe.epicCount);
		}
		return (list.Count > 0) ? string.Join(", ", list.ToArray()) : "없음";
	}

	private void AddMissingGradeSummary(List<string> missing, CharacterGrade grade, int requiredCount)
	{
		int num = Mathf.Max(0, requiredCount - CountUnitsOfGrade(grade));
		if (num > 0)
		{
			missing.Add(CharacterGradeUtility.GetDisplayName(grade) + " ×" + num);
		}
	}

	public void SetUltimateRecipePreview(string recipeName, bool previewActive = false)
	{
		previewUltimateRecipeName = (string.IsNullOrWhiteSpace(recipeName) ? null : recipeName);
		ultimateRecipePreviewActive = previewActive || previewUltimateRecipeName != null;
		RefreshUltimateRecipeMarkers();
	}

	public bool TryMergeUltimateRecipe(string recipeName, CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride)
	{
		mergeResult = default(MergeResultInfo);
		UltimateMergeRecipe ultimateMergeRecipe = FindUltimateRecipe(recipeName);
		if ((Object)(object)database == (Object)null || ultimateMergeRecipe == null || !CanSatisfyUltimateRecipe(ultimateMergeRecipe) || !HasAvailableUltimateResult(database, ultimateMergeRecipe))
		{
			return false;
		}
		return ExecuteUltimateMerge(database, ultimateMergeRecipe, out mergeResult, prefabOverride);
	}

	private bool TryMergeUltimate(CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride)
	{
		mergeResult = default(MergeResultInfo);
		if ((Object)(object)database == (Object)null || !TryFindUltimateRecipe(database, out var recipe))
		{
			return false;
		}
		return ExecuteUltimateMerge(database, recipe, out mergeResult, prefabOverride);
	}

	private bool ExecuteUltimateMerge(CharacterDatabase database, UltimateMergeRecipe recipe, out MergeResultInfo mergeResult, DefenderUnit prefabOverride)
	{
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		mergeResult = default(MergeResultInfo);
		CharacterDefinition characterDefinition = ResolveUltimateMergeResult(database, recipe);
		List<DefenderUnit> list = SelectUnitsForUltimateRecipe(recipe);
		BoardSlot boardSlot = ((list.Count > 0) ? list[0].CurrentSlot : null);
		if (characterDefinition == null || (Object)(object)boardSlot == (Object)null)
		{
			return false;
		}
		float inheritedAttackPower = list.Sum((DefenderUnit unit) => ((Object)(object)unit != (Object)null) ? unit.EffectiveAttackPower : 0f) * 0.9f;
		float inheritedMaxHealth = list.Sum((DefenderUnit unit) => ((Object)(object)unit != (Object)null) ? unit.MaxHealth : 0f) * 0.86f;
		for (int num = 0; num < list.Count; num++)
		{
			list[num].RemoveFromBoard();
			Object.Destroy((Object)(object)((Component)list[num]).gameObject);
		}
		GameObject val = (((Object)(object)characterDefinition.prefab != (Object)null) ? characterDefinition.prefab : (((Object)(object)prefabOverride != (Object)null) ? ((Component)prefabOverride).gameObject : (((Object)(object)fallbackUnitPrefab != (Object)null) ? ((Component)fallbackUnitPrefab).gameObject : null)));
		if ((Object)(object)val == (Object)null)
		{
			Debug.LogError((object)"No DefenderUnit prefab assigned for ultimate merge result.");
			return false;
		}
		GameObject val2 = Object.Instantiate<GameObject>(val, boardSlot.UnitAnchor.position, Quaternion.identity);
		DefenderUnit defenderUnit = val2.GetComponent<DefenderUnit>();
		if ((Object)(object)defenderUnit == (Object)null)
		{
			defenderUnit = val2.AddComponent<DefenderUnit>();
		}
		defenderUnit.AdoptRuntimeTemplate(((Object)(object)prefabOverride != (Object)null) ? prefabOverride : fallbackUnitPrefab);
		((Component)defenderUnit).gameObject.SetActive(true);
		boardSlot.AssignUnit(defenderUnit);
		defenderUnit.Initialize(characterDefinition);
		defenderUnit.ApplyMergeInheritance(inheritedAttackPower, inheritedMaxHealth);
		SpawnMergeVfx(boardSlot, characterDefinition.accentColor, characterDefinition.grade, ultimate: true);
		mergeResult = new MergeResultInfo
		{
			sourceGrade = CharacterGrade.Mythic,
			resultGrade = characterDefinition.grade,
			recipeName = recipe.name,
			sourceDescription = recipe.displayText,
			consumedUnitCount = list.Count,
			isFinalMerge = (characterDefinition.grade == CharacterGrade.Transcendent),
			resultCharacterName = characterDefinition.displayName,
			resultColor = characterDefinition.accentColor
		};
		previewUltimateRecipeName = null;
		ultimateRecipePreviewActive = false;
		return true;
	}

	private static UltimateMergeRecipe FindUltimateRecipe(string recipeName)
	{
		if (string.IsNullOrWhiteSpace(recipeName))
		{
			return null;
		}
		for (int i = 0; i < UltimateRecipes.Length; i++)
		{
			UltimateMergeRecipe ultimateMergeRecipe = UltimateRecipes[i];
			if (ultimateMergeRecipe != null && string.Equals(ultimateMergeRecipe.name, recipeName, StringComparison.Ordinal))
			{
				return ultimateMergeRecipe;
			}
		}
		return null;
	}

	private CharacterDefinition ResolveUltimateMergeResult(CharacterDatabase database, UltimateMergeRecipe recipe)
	{
		List<CharacterDefinition> list = (from character in ResolveUltimateMergeResultCandidates(database, recipe)
			where !IsBlockedTranscendentResult(character)
			select character).ToList();
		if (list.Count > 0)
		{
			return list[Random.Range(0, list.Count)];
		}
		CharacterDefinition randomCharacterByGrade = database.GetRandomCharacterByGrade(CharacterGrade.Mythic);
		return (randomCharacterByGrade != null && !IsBlockedTranscendentResult(randomCharacterByGrade)) ? randomCharacterByGrade : null;
	}

	private List<CharacterDefinition> ResolveUltimateMergeResultCandidates(CharacterDatabase database, UltimateMergeRecipe recipe)
	{
		List<CharacterDefinition> list = new List<CharacterDefinition>();
		if ((Object)(object)database == (Object)null)
		{
			return list;
		}
		if (recipe != null && recipe.resultCharacterIds != null && recipe.resultCharacterIds.Length != 0)
		{
			for (int i = 0; i < recipe.resultCharacterIds.Length; i++)
			{
				CharacterDefinition characterById = database.GetCharacterById(recipe.resultCharacterIds[i]);
				if (IsDeployableUltimateResult(characterById) && !list.Contains(characterById))
				{
					list.Add(characterById);
				}
			}
		}
		if (list.Count == 0)
		{
			list.AddRange(database.GetCharactersByGrade(CharacterGrade.Transcendent));
		}
		return list;
	}

	private static bool IsDeployableUltimateResult(CharacterDefinition character)
	{
		return character != null;
	}

	private bool IsBlockedTranscendentResult(CharacterDefinition character)
	{
		if (character == null || character.grade != CharacterGrade.Transcendent)
		{
			return false;
		}
		return slots.Any((BoardSlot slot) => (Object)(object)slot != (Object)null && (Object)(object)slot.OccupiedUnit != (Object)null && slot.OccupiedUnit.Definition != null && slot.OccupiedUnit.Definition.id == character.id);
	}

	private bool HasAvailableUltimateResult(CharacterDatabase database, UltimateMergeRecipe recipe)
	{
		if ((Object)(object)database == (Object)null)
		{
			return true;
		}
		return ResolveUltimateMergeResultCandidates(database, recipe).Any((CharacterDefinition character) => !IsBlockedTranscendentResult(character));
	}

	private bool HasBlockedReadyUltimateRecipe(CharacterDatabase database)
	{
		if ((Object)(object)database == (Object)null)
		{
			return false;
		}
		for (int i = 0; i < UltimateRecipes.Length; i++)
		{
			UltimateMergeRecipe ultimateMergeRecipe = UltimateRecipes[i];
			if (ultimateMergeRecipe != null && CanSatisfyUltimateRecipe(ultimateMergeRecipe) && !HasAvailableUltimateResult(database, ultimateMergeRecipe))
			{
				return true;
			}
		}
		return false;
	}

	private bool TryFindUltimateRecipe(CharacterDatabase database, out UltimateMergeRecipe recipe)
	{
		for (int i = 0; i < UltimateRecipes.Length; i++)
		{
			UltimateMergeRecipe ultimateMergeRecipe = UltimateRecipes[i];
			if (CanSatisfyUltimateRecipe(ultimateMergeRecipe) && HasAvailableUltimateResult(database, ultimateMergeRecipe))
			{
				recipe = ultimateMergeRecipe;
				return true;
			}
		}
		recipe = null;
		return false;
	}

	private UltimateMergeRecipe GetBestUltimateRecipe(CharacterDatabase database = null)
	{
		if (TryFindUltimateRecipe(database, out var recipe))
		{
			return recipe;
		}
		UltimateMergeRecipe result = null;
		int bestProgress = -1;
		int bestRequired = 1;
		for (int i = 0; i < UltimateRecipes.Length; i++)
		{
			UltimateMergeRecipe ultimateMergeRecipe = UltimateRecipes[i];
			int ultimateRecipeRequiredCount = GetUltimateRecipeRequiredCount(ultimateMergeRecipe);
			int ultimateRecipeProgress = GetUltimateRecipeProgress(ultimateMergeRecipe);
			if (IsBetterUltimateRecipeProgress(ultimateRecipeProgress, ultimateRecipeRequiredCount, bestProgress, bestRequired))
			{
				result = ultimateMergeRecipe;
				bestProgress = ultimateRecipeProgress;
				bestRequired = ultimateRecipeRequiredCount;
			}
		}
		return result;
	}

	private bool CanSatisfyUltimateRecipe(UltimateMergeRecipe recipe)
	{
		List<DefenderUnit> selectedUnits;
		if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
		{
			return TrySelectUnitsByCharacterIds(recipe.requiredCharacterIds, out selectedUnits);
		}
		return CountUnitsOfGrade(CharacterGrade.Mythic) >= recipe.mythicCount && CountUnitsOfGrade(CharacterGrade.Legendary) >= recipe.legendaryCount && CountUnitsOfGrade(CharacterGrade.Epic) >= recipe.epicCount;
	}

	private List<DefenderUnit> SelectUnitsForUltimateRecipe(UltimateMergeRecipe recipe)
	{
		if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0 && TrySelectUnitsByCharacterIds(recipe.requiredCharacterIds, out var selectedUnits))
		{
			return selectedUnits;
		}
		List<DefenderUnit> result = new List<DefenderUnit>();
		AddUnitsOfGrade(result, CharacterGrade.Mythic, recipe.mythicCount);
		AddUnitsOfGrade(result, CharacterGrade.Legendary, recipe.legendaryCount);
		AddUnitsOfGrade(result, CharacterGrade.Epic, recipe.epicCount);
		return result;
	}

	private void AddUnitsOfGrade(List<DefenderUnit> result, CharacterGrade grade, int count)
	{
		if (count > 0)
		{
			IEnumerable<DefenderUnit> collection = (from slot in slots
				where (Object)(object)slot != (Object)null && !slot.IsEmpty
				select slot.OccupiedUnit into unit
				where (Object)(object)unit != (Object)null && unit.Grade == grade
				select unit).Take(count);
			result.AddRange(collection);
		}
	}

	private bool TrySelectUnitsByCharacterIds(string[] requiredIds, out List<DefenderUnit> selectedUnits)
	{
		selectedUnits = new List<DefenderUnit>();
		List<DefenderUnit> list = (from slot in slots
			where (Object)(object)slot != (Object)null && !slot.IsEmpty
			select slot.OccupiedUnit into unit
			where (Object)(object)unit != (Object)null && unit.Definition != null
			select unit).ToList();
		foreach (string requiredId in requiredIds)
		{
			DefenderUnit defenderUnit = list.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
			if ((Object)(object)defenderUnit == (Object)null)
			{
				selectedUnits.Clear();
				return false;
			}
			selectedUnits.Add(defenderUnit);
			list.Remove(defenderUnit);
		}
		return true;
	}

	private HashSet<DefenderUnit> SelectReservedUltimateRecipeUnits()
	{
		HashSet<DefenderUnit> hashSet = new HashSet<DefenderUnit>();
		UltimateMergeRecipe ultimateMergeRecipe = FindUltimateRecipe(previewUltimateRecipeName);
		if (ultimateRecipePreviewActive)
		{
			if (ultimateMergeRecipe == null || !CanSatisfyUltimateRecipe(ultimateMergeRecipe))
			{
				return hashSet;
			}
		}
		else
		{
			ultimateMergeRecipe = GetBestUltimateRecipe();
		}
		if (ultimateMergeRecipe == null)
		{
			return hashSet;
		}
		List<DefenderUnit> recipeCandidateUnits = GetRecipeCandidateUnits();
		if (ultimateMergeRecipe.requiredCharacterIds != null && ultimateMergeRecipe.requiredCharacterIds.Length != 0)
		{
			for (int i = 0; i < ultimateMergeRecipe.requiredCharacterIds.Length; i++)
			{
				string requiredId = ultimateMergeRecipe.requiredCharacterIds[i];
				DefenderUnit defenderUnit = recipeCandidateUnits.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
				if (!((Object)(object)defenderUnit == (Object)null))
				{
					hashSet.Add(defenderUnit);
					recipeCandidateUnits.Remove(defenderUnit);
				}
			}
			return hashSet;
		}
		AddReservedUnitsOfGrade(hashSet, recipeCandidateUnits, CharacterGrade.Mythic, ultimateMergeRecipe.mythicCount);
		AddReservedUnitsOfGrade(hashSet, recipeCandidateUnits, CharacterGrade.Legendary, ultimateMergeRecipe.legendaryCount);
		AddReservedUnitsOfGrade(hashSet, recipeCandidateUnits, CharacterGrade.Epic, ultimateMergeRecipe.epicCount);
		return hashSet;
	}

	private string BuildRequiredCharacterRecipeStatus(UltimateMergeRecipe recipe, CharacterDatabase database)
	{
		List<DefenderUnit> recipeCandidateUnits = GetRecipeCandidateUnits();
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
		{
			string requiredId = recipe.requiredCharacterIds[i];
			DefenderUnit defenderUnit = recipeCandidateUnits.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
			bool flag = (Object)(object)defenderUnit != (Object)null;
			if (flag)
			{
				recipeCandidateUnits.Remove(defenderUnit);
			}
			string item = ResolveCharacterName(database, requiredId);
			if (flag)
			{
				list.Add(item);
			}
			else
			{
				list2.Add(item);
			}
		}
		List<string> list3 = new List<string>();
		if (list.Count > 0)
		{
			list3.Add("보존 " + string.Join(", ", list));
		}
		if (list2.Count > 0)
		{
			list3.Add("부족 " + string.Join(", ", list2));
		}
		return string.Join(" / ", list3);
	}

	private int CompareUltimateRecipeBingoPriority(UltimateMergeRecipe left, UltimateMergeRecipe right)
	{
		if (left == right)
		{
			return 0;
		}
		if (left == null)
		{
			return 1;
		}
		if (right == null)
		{
			return -1;
		}
		bool flag = CanSatisfyUltimateRecipe(left);
		bool flag2 = CanSatisfyUltimateRecipe(right);
		if (flag != flag2)
		{
			return (!flag) ? 1 : (-1);
		}
		int num = Mathf.Max(1, GetUltimateRecipeRequiredCount(left));
		int num2 = Mathf.Max(1, GetUltimateRecipeRequiredCount(right));
		int num3 = Mathf.Max(0, GetUltimateRecipeProgress(left));
		int num4 = Mathf.Max(0, GetUltimateRecipeProgress(right));
		long num5 = (long)num3 * (long)num2;
		long num6 = (long)num4 * (long)num;
		if (num5 != num6)
		{
			return (num5 <= num6) ? 1 : (-1);
		}
		if (num3 != num4)
		{
			return (num3 <= num4) ? 1 : (-1);
		}
		return num.CompareTo(num2);
	}

	private string BuildRecipeBingoMaterialStatus(UltimateMergeRecipe recipe, CharacterDatabase database)
	{
		if (recipe == null)
		{
			return string.Empty;
		}
		if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
		{
			List<DefenderUnit> recipeCandidateUnits = GetRecipeCandidateUnits();
			List<string> list = new List<string>();
			for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
			{
				string requiredId = recipe.requiredCharacterIds[i];
				DefenderUnit defenderUnit = recipeCandidateUnits.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
				bool flag = (Object)(object)defenderUnit != (Object)null;
				if (flag)
				{
					recipeCandidateUnits.Remove(defenderUnit);
				}
				string text = CompactRecipeMaterialName(ResolveCharacterName(database, requiredId));
				list.Add((flag ? "[O] " : "[ ] ") + text);
			}
			return string.Join("  ", list);
		}
		List<string> list2 = new List<string>();
		AddRecipeBingoGradePart(list2, CharacterGrade.Mythic, recipe.mythicCount);
		AddRecipeBingoGradePart(list2, CharacterGrade.Legendary, recipe.legendaryCount);
		AddRecipeBingoGradePart(list2, CharacterGrade.Epic, recipe.epicCount);
		return (list2.Count > 0) ? string.Join("  ", list2) : recipe.displayText;
	}

	private void AddRecipeBingoGradePart(List<string> parts, CharacterGrade grade, int requiredCount)
	{
		if (requiredCount > 0)
		{
			int num = Mathf.Min(CountUnitsOfGrade(grade), requiredCount);
			parts.Add(((num >= requiredCount) ? "[O] " : "[ ] ") + CharacterGradeUtility.GetDisplayName(grade) + " " + num + "/" + requiredCount);
		}
	}

	private static string CompactRecipeName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "Recipe";
		}
		string text = value.Replace(" Rite", string.Empty).Trim();
		return (text.Length <= 22) ? text : (text.Substring(0, 19) + "...");
	}

	private static string CompactRecipeMaterialName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "?";
		}
		string text = value.Trim();
		return (text.Length <= 7) ? text : (text.Substring(0, 6) + ".");
	}

	private string GetFirstMissingRecipeMaterialName(UltimateMergeRecipe recipe, CharacterDatabase database)
	{
		if (recipe == null)
		{
			return string.Empty;
		}
		if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
		{
			List<DefenderUnit> recipeCandidateUnits = GetRecipeCandidateUnits();
			for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
			{
				string requiredId = recipe.requiredCharacterIds[i];
				DefenderUnit defenderUnit = recipeCandidateUnits.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
				if ((Object)(object)defenderUnit == (Object)null)
				{
					return ResolveCharacterName(database, requiredId);
				}
				recipeCandidateUnits.Remove(defenderUnit);
			}
			return string.Empty;
		}
		if (recipe.mythicCount > CountUnitsOfGrade(CharacterGrade.Mythic))
		{
			return CharacterGradeUtility.GetDisplayName(CharacterGrade.Mythic);
		}
		if (recipe.legendaryCount > CountUnitsOfGrade(CharacterGrade.Legendary))
		{
			return CharacterGradeUtility.GetDisplayName(CharacterGrade.Legendary);
		}
		if (recipe.epicCount > CountUnitsOfGrade(CharacterGrade.Epic))
		{
			return CharacterGradeUtility.GetDisplayName(CharacterGrade.Epic);
		}
		return string.Empty;
	}

	private void AddReservedUnitsOfGrade(HashSet<DefenderUnit> reservedUnits, List<DefenderUnit> candidates, CharacterGrade grade, int requiredCount)
	{
		if (requiredCount <= 0)
		{
			return;
		}
		int num = requiredCount;
		for (int i = 0; i < candidates.Count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			DefenderUnit defenderUnit = candidates[i];
			if (!((Object)(object)defenderUnit == (Object)null) && defenderUnit.Grade == grade)
			{
				reservedUnits.Add(defenderUnit);
				num--;
			}
		}
	}

	private void RefreshUltimateRecipeMarkers()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		HashSet<DefenderUnit> hashSet = SelectReservedUltimateRecipeUnits();
		Color color = CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f));
		for (int i = 0; i < slots.Count; i++)
		{
			BoardSlot boardSlot = slots[i];
			DefenderUnit defenderUnit = (((Object)(object)boardSlot != (Object)null && !boardSlot.IsEmpty) ? boardSlot.OccupiedUnit : null);
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				bool flag = hashSet.Contains(defenderUnit);
				defenderUnit.SetRecipeMaterialMarker(flag, flag ? "초월 재료" : string.Empty, color);
			}
		}
	}

	private string BuildGradeRecipeStatus(UltimateMergeRecipe recipe)
	{
		List<string> list = new List<string>();
		AddGradeRecipePart(list, CharacterGrade.Mythic, recipe.mythicCount);
		AddGradeRecipePart(list, CharacterGrade.Legendary, recipe.legendaryCount);
		AddGradeRecipePart(list, CharacterGrade.Epic, recipe.epicCount);
		return (list.Count > 0) ? string.Join(" / ", list) : recipe.displayText;
	}

	private void AddGradeRecipePart(List<string> parts, CharacterGrade grade, int requiredCount)
	{
		if (requiredCount > 0)
		{
			int num = Mathf.Min(CountUnitsOfGrade(grade), requiredCount);
			parts.Add(CharacterGradeUtility.GetDisplayName(grade) + " " + num + "/" + requiredCount);
		}
	}

	private List<DefenderUnit> GetRecipeCandidateUnits()
	{
		return (from slot in slots
			where (Object)(object)slot != (Object)null && !slot.IsEmpty
			select slot.OccupiedUnit into unit
			where (Object)(object)unit != (Object)null && unit.Definition != null
			select unit).ToList();
	}

	private string ResolveCharacterName(CharacterDatabase database, string characterId)
	{
		CharacterDefinition characterDefinition = (((Object)(object)database != (Object)null) ? database.GetCharacterById(characterId) : null);
		if (characterDefinition == null || string.IsNullOrWhiteSpace(characterDefinition.displayName))
		{
			return characterId;
		}
		return characterDefinition.displayName;
	}

	private void GetBestUltimateRecipeProgress(out int bestProgress, out int bestRequired)
	{
		bestProgress = 0;
		bestRequired = 3;
		for (int i = 0; i < UltimateRecipes.Length; i++)
		{
			UltimateMergeRecipe recipe = UltimateRecipes[i];
			int ultimateRecipeRequiredCount = GetUltimateRecipeRequiredCount(recipe);
			int ultimateRecipeProgress = GetUltimateRecipeProgress(recipe);
			if (IsBetterUltimateRecipeProgress(ultimateRecipeProgress, ultimateRecipeRequiredCount, bestProgress, bestRequired))
			{
				bestProgress = ultimateRecipeProgress;
				bestRequired = ultimateRecipeRequiredCount;
			}
		}
	}

	private bool IsBetterUltimateRecipeProgress(int progress, int required, int bestProgress, int bestRequired)
	{
		if (bestProgress < 0)
		{
			return true;
		}
		int num = Mathf.Max(1, required);
		int num2 = Mathf.Max(1, bestRequired);
		long num3 = (long)Mathf.Max(0, progress) * (long)num2;
		long num4 = (long)Mathf.Max(0, bestProgress) * (long)num;
		if (num3 != num4)
		{
			return num3 > num4;
		}
		return progress > bestProgress || (progress == bestProgress && num < num2);
	}

	private int GetUltimateRecipeRequiredCount(UltimateMergeRecipe recipe)
	{
		if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
		{
			return recipe.requiredCharacterIds.Length;
		}
		return recipe.mythicCount + recipe.legendaryCount + recipe.epicCount;
	}

	private int GetUltimateRecipeProgress(UltimateMergeRecipe recipe)
	{
		if (recipe.requiredCharacterIds == null || recipe.requiredCharacterIds.Length == 0)
		{
			return Mathf.Min(CountUnitsOfGrade(CharacterGrade.Mythic), recipe.mythicCount) + Mathf.Min(CountUnitsOfGrade(CharacterGrade.Legendary), recipe.legendaryCount) + Mathf.Min(CountUnitsOfGrade(CharacterGrade.Epic), recipe.epicCount);
		}
		List<DefenderUnit> list = (from slot in slots
			where (Object)(object)slot != (Object)null && !slot.IsEmpty
			select slot.OccupiedUnit into unit
			where (Object)(object)unit != (Object)null && unit.Definition != null
			select unit).ToList();
		int num = 0;
		for (int num2 = 0; num2 < recipe.requiredCharacterIds.Length; num2++)
		{
			string requiredId = recipe.requiredCharacterIds[num2];
			DefenderUnit defenderUnit = list.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				num++;
				list.Remove(defenderUnit);
			}
		}
		return num;
	}

	public int CountUnitsOfGrade(CharacterGrade grade)
	{
		return slots.Count((BoardSlot slot) => (Object)(object)slot != (Object)null && (Object)(object)slot.OccupiedUnit != (Object)null && slot.OccupiedUnit.Grade == grade);
	}

	private void SpawnMergeVfx(BoardSlot slot, Color color, CharacterGrade resultGrade, bool ultimate)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)slot == (Object)null || (Object)(object)slot.UnitAnchor == (Object)null)
		{
			return;
		}
		bool flag = resultGrade == CharacterGrade.Transcendent;
		bool flag2 = resultGrade == CharacterGrade.Mythic;
		bool flag3 = ultimate || resultGrade >= CharacterGrade.Rare;
		bool flag4 = ultimate || resultGrade >= CharacterGrade.Epic;
		RuntimeGameFeel.PlayMergeResultVfx(slot.UnitAnchor.position, color, resultGrade, ultimate);
		if (ultimate)
		{
			RuntimeGameFeel.PlayJackpotPulse(slot.UnitAnchor.position, color, 2.08f, 0.22f, 0.46f, 0.12f, 0.16f, 4);
			if (flag)
			{
				RuntimeAudioUtility.PlayJackpotUltimate();
			}
			else if (flag2)
			{
				RuntimeAudioUtility.PlayMythicSpawn();
			}
			else
			{
				RuntimeAudioUtility.PlayJackpotMajor();
			}
		}
		else if (flag3)
		{
			RuntimeGameFeel.PlayJackpotPulse(slot.UnitAnchor.position, color, flag4 ? 1.62f : 1.22f, flag4 ? 0.15f : 0.09f, flag4 ? 0.34f : 0.23f, flag4 ? 0.18f : 0.3f, flag4 ? 0.12f : 0.075f, flag4 ? 3 : 2);
			if (flag2)
			{
				RuntimeAudioUtility.PlayMythicSpawn();
			}
			else if (flag4)
			{
				RuntimeAudioUtility.PlayJackpotMajor();
			}
			else
			{
				RuntimeAudioUtility.PlayJackpotMinor();
			}
		}
		else
		{
			RuntimeCameraShake.Request(0.055f, 0.13f);
		}
	}

	public DefenderUnit[] GetAliveDefenders()
	{
		return (from slot in slots
			where (Object)(object)slot != (Object)null && !slot.IsEmpty
			select slot.OccupiedUnit into unit
			where (Object)(object)unit != (Object)null
			select unit).ToArray();
	}

	public bool IsReservedForUltimateRecipeUnit(DefenderUnit unit)
	{
		if ((Object)(object)unit == (Object)null)
		{
			return false;
		}
		return SelectReservedUltimateRecipeUnits().Contains(unit);
	}

	public bool TryRemoveUnitFromBoard(DefenderUnit unit)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)unit == (Object)null || (Object)(object)unit.CurrentSlot == (Object)null || unit.IsTemporarySummon)
		{
			return false;
		}
		if ((Object)(object)draggedUnit == (Object)(object)unit)
		{
			draggedUnit = null;
			draggedOriginSlot = null;
			draggedColliders = null;
			dragOffset = Vector3.zero;
		}
		if ((Object)(object)selectedRangeUnit == (Object)(object)unit)
		{
			HideRangeIndicator();
		}
		unit.RetireFromBoard();
		RefreshUltimateRecipeMarkers();
		return true;
	}

	public void ClearSelectedUnit()
	{
		HideRangeIndicator();
	}

	public bool TryMoveUnit(DefenderUnit unit, BoardSlot targetSlot)
	{
		if ((Object)(object)unit == (Object)null || (Object)(object)targetSlot == (Object)null || targetSlot.IsLocked)
		{
			return false;
		}
		BoardSlot currentSlot = unit.CurrentSlot;
		if ((Object)(object)currentSlot == (Object)null)
		{
			return false;
		}
		if ((Object)(object)currentSlot == (Object)(object)targetSlot && (targetSlot.IsEmpty || (Object)(object)targetSlot.OccupiedUnit == (Object)(object)unit))
		{
			targetSlot.AssignUnit(unit);
			return true;
		}
		DefenderUnit occupiedUnit = targetSlot.OccupiedUnit;
		if ((Object)(object)occupiedUnit == (Object)(object)unit)
		{
			targetSlot.AssignUnit(unit);
			return true;
		}
		currentSlot.Clear();
		if ((Object)(object)occupiedUnit != (Object)null)
		{
			targetSlot.Clear();
		}
		targetSlot.AssignUnit(unit);
		if ((Object)(object)occupiedUnit != (Object)null)
		{
			currentSlot.AssignUnit(occupiedUnit);
		}
		return true;
	}

	private void HandleDragging()
	{
		if ((Object)(object)GetGameplayCamera() == (Object)null)
		{
			return;
		}
		if (Input.GetMouseButtonDown(0))
		{
			if (IsPointerOverUi())
			{
				pendingPointerUnit = null;
				return;
			}
			BeginPointerPress();
		}
		if ((Object)(object)draggedUnit == (Object)null && (Object)(object)pendingPointerUnit != (Object)null && Input.GetMouseButton(0) && ShouldStartDragFromPress())
		{
			TryBeginDrag(pendingPointerUnit);
			pendingPointerUnit = null;
		}
		if ((Object)(object)draggedUnit != (Object)null)
		{
			UpdateDragPosition();
			if (Input.GetMouseButtonUp(0))
			{
				EndDrag();
			}
		}
		else if (Input.GetMouseButtonUp(0))
		{
			CompletePointerPress();
		}
	}

	private void BeginPointerPress()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		pendingPointerUnit = FindUnitUnderPointer();
		pointerDownScreenPosition = Vector2.op_Implicit(Input.mousePosition);
		pointerDownTime = Time.unscaledTime;
		if ((Object)(object)pendingPointerUnit == (Object)null)
		{
			HideRangeIndicator();
		}
	}

	private bool ShouldStartDragFromPress()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		float num = Time.unscaledTime - pointerDownTime;
		Vector2 val = Vector2.op_Implicit(Input.mousePosition);
		Vector2 val2 = val - pointerDownScreenPosition;
		float sqrMagnitude = ((Vector2)(ref val2)).sqrMagnitude;
		return num >= holdToDragDelay || sqrMagnitude >= dragStartScreenDistance * dragStartScreenDistance;
	}

	private void CompletePointerPress()
	{
		if ((Object)(object)pendingPointerUnit != (Object)null)
		{
			ShowRangeIndicator(pendingPointerUnit);
		}
		pendingPointerUnit = null;
	}

	private void TryBeginDrag(DefenderUnit unit)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)unit == (Object)null)
		{
			return;
		}
		BoardSlot currentSlot = unit.CurrentSlot;
		if ((Object)(object)currentSlot == (Object)null)
		{
			return;
		}
		draggedUnit = unit;
		draggedOriginSlot = currentSlot;
		draggedColliders = ((Component)draggedUnit).GetComponentsInChildren<Collider>(true);
		draggedOriginSlot.Clear();
		((Component)draggedUnit).transform.SetParent(((Component)this).transform, true);
		HideRangeIndicator();
		for (int i = 0; i < draggedColliders.Length; i++)
		{
			draggedColliders[i].enabled = false;
		}
		Camera gameplayCamera = GetGameplayCamera();
		if (!((Object)(object)gameplayCamera == (Object)null))
		{
			Ray val = gameplayCamera.ScreenPointToRay(Input.mousePosition);
			float num = default(float);
			if (((Plane)(ref dragPlane)).Raycast(val, ref num))
			{
				dragOffset = ((Component)draggedUnit).transform.position - ((Ray)(ref val)).GetPoint(num);
			}
			else
			{
				dragOffset = Vector3.zero;
			}
		}
	}

	private void UpdateDragPosition()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		Camera gameplayCamera = GetGameplayCamera();
		if (!((Object)(object)gameplayCamera == (Object)null))
		{
			Ray val = gameplayCamera.ScreenPointToRay(Input.mousePosition);
			float num = default(float);
			if (((Plane)(ref dragPlane)).Raycast(val, ref num))
			{
				Vector3 position = ((Ray)(ref val)).GetPoint(num) + dragOffset;
				position.y = dragHeight;
				((Component)draggedUnit).transform.position = position;
			}
		}
	}

	private void EndDrag()
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		BoardSlot boardSlot = FindSlotUnderPointer();
		for (int i = 0; i < draggedColliders.Length; i++)
		{
			if ((Object)(object)draggedColliders[i] != (Object)null)
			{
				draggedColliders[i].enabled = true;
			}
		}
		if ((!((Object)(object)boardSlot != (Object)null) || !TryMoveUnit(draggedUnit, boardSlot)) && (Object)(object)draggedOriginSlot != (Object)null)
		{
			draggedOriginSlot.AssignUnit(draggedUnit);
		}
		draggedUnit = null;
		draggedOriginSlot = null;
		draggedColliders = null;
		dragOffset = Vector3.zero;
	}

	private void ShowRangeIndicator(DefenderUnit unit)
	{
		if ((Object)(object)unit == (Object)null || (Object)(object)unit.CurrentSlot == (Object)null)
		{
			HideRangeIndicator();
			return;
		}
		bool flag = (Object)(object)selectedRangeUnit != (Object)(object)unit;
		selectedRangeUnit = unit;
		rangeIndicatorDirty |= flag;
		EnsureRangeIndicator();
		((Renderer)rangeIndicatorLine).enabled = true;
		UpdateRangeIndicator();
		if (flag)
		{
			this.OnSelectedUnitChanged?.Invoke(selectedRangeUnit);
		}
	}

	private void HideRangeIndicator()
	{
		bool flag = (Object)(object)selectedRangeUnit != (Object)null;
		selectedRangeUnit = null;
		if ((Object)(object)rangeIndicatorLine != (Object)null)
		{
			((Renderer)rangeIndicatorLine).enabled = false;
		}
		rangeIndicatorDirty = true;
		if (flag)
		{
			this.OnSelectedUnitChanged?.Invoke(null);
		}
	}

	private void EnsureRangeIndicator()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)rangeIndicatorLine != (Object)null))
		{
			GameObject val = new GameObject("UnitAttackRangeIndicator");
			val.transform.SetParent(((Component)this).transform, false);
			rangeIndicatorLine = val.AddComponent<LineRenderer>();
			rangeIndicatorLine.useWorldSpace = true;
			rangeIndicatorLine.loop = true;
			rangeIndicatorLine.positionCount = Mathf.Max(12, rangeIndicatorSegments);
			rangeIndicatorLine.widthMultiplier = Mathf.Max(0.01f, rangeIndicatorLineWidth);
			rangeIndicatorLine.numCapVertices = 4;
			rangeIndicatorLine.numCornerVertices = 4;
			((Renderer)rangeIndicatorLine).material = new Material(Shader.Find("Sprites/Default"));
			rangeIndicatorLine.startColor = rangeIndicatorColor;
			rangeIndicatorLine.endColor = rangeIndicatorColor;
			((Renderer)rangeIndicatorLine).enabled = false;
			rangeIndicatorDirty = true;
		}
	}

	private void UpdateRangeIndicator()
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)selectedRangeUnit == (Object)null || (Object)(object)selectedRangeUnit.CurrentSlot == (Object)null)
		{
			HideRangeIndicator();
			return;
		}
		EnsureRangeIndicator();
		int num = Mathf.Max(12, rangeIndicatorSegments);
		float num2 = Mathf.Max(0.01f, rangeIndicatorLineWidth);
		Vector3 position = ((Component)selectedRangeUnit).transform.position;
		position.y = ((Component)selectedRangeUnit.CurrentSlot).transform.position.y + rangeIndicatorHeight;
		float num3 = Mathf.Max(0.1f, selectedRangeUnit.CurrentAttackRange);
		if (!rangeIndicatorDirty && lastRangeIndicatorSegments == num && Mathf.Abs(lastRangeIndicatorWidth - num2) <= 0.0001f && Mathf.Abs(lastRangeIndicatorRadius - num3) <= 0.0001f)
		{
			Vector3 val = lastRangeIndicatorCenter - position;
			if (((Vector3)(ref val)).sqrMagnitude <= 1E-06f && lastRangeIndicatorColor == rangeIndicatorColor)
			{
				return;
			}
		}
		if (rangeIndicatorLine.positionCount != num)
		{
			rangeIndicatorLine.positionCount = num;
		}
		rangeIndicatorLine.widthMultiplier = num2;
		rangeIndicatorLine.startColor = rangeIndicatorColor;
		rangeIndicatorLine.endColor = rangeIndicatorColor;
		for (int i = 0; i < num; i++)
		{
			float num4 = MathF.PI * 2f * (float)i / (float)num;
			Vector3 val2 = position + new Vector3(Mathf.Cos(num4) * num3, 0f, Mathf.Sin(num4) * num3);
			rangeIndicatorLine.SetPosition(i, val2);
		}
		lastRangeIndicatorSegments = num;
		lastRangeIndicatorWidth = num2;
		lastRangeIndicatorRadius = num3;
		lastRangeIndicatorCenter = position;
		lastRangeIndicatorColor = rangeIndicatorColor;
		rangeIndicatorDirty = false;
	}

	private BoardSlot FindSlotUnderPointer()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (!TryGetPointerPoint(GetSlotPointerPlaneHeight(), out var point))
		{
			return null;
		}
		return FindClosestSlot(point, slotDropRadius, requireOccupied: false);
	}

	private DefenderUnit FindUnitUnderPointer()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (!TryGetPointerPoint(pointerPickupPlaneHeight, out var point))
		{
			return null;
		}
		BoardSlot boardSlot = FindClosestSlot(point, unitPickupRadius, requireOccupied: true);
		return ((Object)(object)boardSlot != (Object)null) ? boardSlot.OccupiedUnit : null;
	}

	private BoardSlot FindClosestSlot(Vector3 pointerPoint, float radius, bool requireOccupied)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		BoardSlot result = null;
		float num = radius * radius;
		for (int i = 0; i < slots.Count; i++)
		{
			BoardSlot boardSlot = slots[i];
			if (!((Object)(object)boardSlot == (Object)null) && !boardSlot.IsLocked && (!requireOccupied || !boardSlot.IsEmpty))
			{
				Vector3 position = boardSlot.UnitAnchor.position;
				float num2 = pointerPoint.x - position.x;
				float num3 = pointerPoint.z - position.z;
				float num4 = num2 * num2 + num3 * num3;
				if (!(num4 > num))
				{
					num = num4;
					result = boardSlot;
				}
			}
		}
		return result;
	}

	private bool TryGetPointerPoint(float planeHeight, out Vector3 point)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		point = Vector3.zero;
		Camera gameplayCamera = GetGameplayCamera();
		if ((Object)(object)gameplayCamera == (Object)null)
		{
			return false;
		}
		Ray val = gameplayCamera.ScreenPointToRay(Input.mousePosition);
		Plane val2 = default(Plane);
		((Plane)(ref val2))._002Ector(Vector3.up, new Vector3(0f, planeHeight, 0f));
		float num = default(float);
		if (!((Plane)(ref val2)).Raycast(val, ref num))
		{
			return false;
		}
		point = ((Ray)(ref val)).GetPoint(num);
		return true;
	}

	private Camera GetGameplayCamera()
	{
		if ((Object)(object)cachedGameplayCamera == (Object)null)
		{
			cachedGameplayCamera = Camera.main;
		}
		return cachedGameplayCamera;
	}

	private static bool IsPointerOverUi()
	{
		return (Object)(object)EventSystem.current != (Object)null && EventSystem.current.IsPointerOverGameObject();
	}

	private float GetSlotPointerPlaneHeight()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < slots.Count; i++)
		{
			BoardSlot boardSlot = slots[i];
			if ((Object)(object)boardSlot != (Object)null && (Object)(object)boardSlot.UnitAnchor != (Object)null)
			{
				return boardSlot.UnitAnchor.position.y;
			}
		}
		return 0f;
	}
}
