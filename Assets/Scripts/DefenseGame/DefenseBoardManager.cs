using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DefenseGame
{
	public class DefenseBoardManager : MonoBehaviour
	{
		private struct BoardDragBounds
		{
			public bool isValid;
			public float minX;
			public float maxX;
			public float minZ;
			public float maxZ;
			public float paddingX;
			public float paddingZ;
			public float slotSpacingX;
			public float slotSpacingZ;
		}

		private class UltimateMergeRecipe
		{
			public readonly string name;

			public readonly string displayText;

			public readonly int mythicCount;

			public readonly int legendaryCount;

			public readonly int epicCount;

			public readonly string[] requiredCharacterIds;

			public readonly string resultCharacterId;

			public UltimateMergeRecipe(string name, string displayText, int mythicCount, int legendaryCount, int epicCount, string[] requiredCharacterIds, string resultCharacterId)
			{
				this.name = name;
				this.displayText = displayText;
				this.mythicCount = mythicCount;
				this.legendaryCount = legendaryCount;
				this.epicCount = epicCount;
				this.requiredCharacterIds = requiredCharacterIds ?? new string[0];
				this.resultCharacterId = resultCharacterId ?? string.Empty;
			}
		}

		private const float BoardRowTolerance = 0.01f;

		private static readonly UltimateMergeRecipe[] UltimateRecipes =
		{
			new UltimateMergeRecipe("Thunder Control Rite", "Mythic Fighter + Life Chain + Petrify", 0, 0, 0, new string[3] { "hero_31", "hero_07", "hero_08" }, "hero_51"),
			new UltimateMergeRecipe("Volcanic Core Rite", "Mythic Core + Area Damage + Shield Break", 0, 0, 0, new string[3] { "hero_32", "hero_01", "hero_09" }, "hero_52"),
			new UltimateMergeRecipe("Fever Engine Rite", "Mythic Fighter + Mana Link + Focus Dealer", 0, 0, 0, new string[3] { "hero_31", "hero_13", "hero_10" }, "hero_53"),
			new UltimateMergeRecipe("Soul Battery Rite", "Drain + Mana + Heal + Guard + Speed", 0, 0, 0, new string[5] { "hero_11", "hero_13", "hero_02", "hero_05", "hero_14" }, "hero_54"),
			new UltimateMergeRecipe("Iron Bastion Rite", "Mythic Combat + Shield (완화 조합)", 0, 0, 0, new string[2] { "hero_31", "hero_05" }, "hero_55"),
			new UltimateMergeRecipe("Clockwork Barrage Rite", "Mythic Wolf + Battery (완화 조합)", 0, 0, 0, new string[2] { "hero_32", "hero_13" }, "hero_56"),
			new UltimateMergeRecipe("Fractured Arsenal Rite", "Mythic Infection + Assassin (완화 조합)", 0, 0, 0, new string[2] { "hero_33", "hero_12" }, "hero_57")
		};

		[SerializeField]
		private List<BoardSlot> slots = new List<BoardSlot>();

		[SerializeField]
		private DefenderUnit fallbackUnitPrefab;

		[SerializeField]
		private float dragHeight = 1.4f;

		[SerializeField, Range(0.4f, 0.5f)]
		private float boardDragPaddingRatio = 0.45f;

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
		private int frontSlotUnlockInterval = 8;

		[SerializeField]
		private int maxFrontUnlockCount = 5;

		private DefenderUnit draggedUnit;

		private BoardSlot draggedOriginSlot;

		private Collider[] draggedColliders;

		private bool[] draggedColliderEnabledStates;

		private Plane dragPlane;

		private Vector3 dragOffset;

		private BoardDragBounds cachedBoardDragBounds;

		private Vector2 draggedUnitLocalExtent;

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

		public int EmptySlotCount => slots.Count((BoardSlot slot) => slot != null && slot.IsAvailable && slot.IsEmpty);

		public int UnitCount => slots.Count((BoardSlot slot) => slot != null && slot.OccupiedUnit != null);

		public int UnlockedSlotCount => slots.Count((BoardSlot slot) => slot != null && slot.IsAvailable);

		public int LockedSlotCount => slots.Count((BoardSlot slot) => slot != null && slot.IsLocked);

		public DefenderUnit SelectedUnit => selectedRangeUnit;

		public string LastMergeFailureReason { get; private set; } = string.Empty;

		public event Action<DefenderUnit> OnSelectedUnitChanged;

		private void Awake()
		{
			if (slots.Count == 0)
			{
				slots = GetComponentsInChildren<BoardSlot>(includeInactive: true).ToList();
			}
			dragPlane = new Plane(Vector3.up, new Vector3(0f, dragHeight, 0f));
			cachedGameplayCamera = Camera.main;
			RefreshCachedBoardDragBounds();
		}

		private void OnDisable()
		{
			CancelActiveDrag();
		}

		private void Update()
		{
			HandleDragging();
			if (Time.frameCount % 45 == 0)
			{
				ValidateBoardSlotInvariant();
			}
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
			CancelActiveDrag();
			slots = newSlots;
			fallbackUnitPrefab = fallbackPrefab;
			RefreshSlotLocks(0);
			RefreshCachedBoardDragBounds();
		}

		public int RefreshSlotLocks(int completedRound, bool playUnlockFeedback = false)
		{
			CancelActiveDrag();
			int activeCount = ResolveActiveSlotCount(completedRound);
			int newlyUnlocked = 0;
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot slot = slots[i];
				if (slot == null)
				{
					continue;
				}
				bool wasLocked = slot.IsLocked;
				bool shouldLock = i >= activeCount;
				slot.SetLocked(shouldLock, string.Empty);
				if (wasLocked && !shouldLock)
				{
					newlyUnlocked++;
					if (playUnlockFeedback)
					{
						slot.PlayUnlockFeedback();
					}
				}
			}
			return newlyUnlocked;
		}

		public int GetSlotUnlockRound(int slotIndex)
		{
			int baseCount = Mathf.Clamp(Mathf.Max(0, baseActiveSlotCount), 0, slots.Count);
			if (slotIndex < baseCount)
			{
				return 0;
			}

			int extraIndex = slotIndex - baseCount;
			if (extraIndex >= Mathf.Max(0, maxFrontUnlockCount) || slotIndex >= slots.Count)
			{
				return -1;
			}

			return (extraIndex + 1) * Mathf.Max(1, frontSlotUnlockInterval);
		}

		public int GetNextSlotUnlockRound(int completedRound)
		{
			int currentUnlocked = ResolveActiveSlotCount(completedRound);
			return currentUnlocked < slots.Count ? GetSlotUnlockRound(currentUnlocked) : -1;
		}

		private int ResolveActiveSlotCount(int completedRound)
		{
			int baseCount = Mathf.Clamp(Mathf.Max(0, baseActiveSlotCount), 0, slots.Count);
			int interval = Mathf.Max(1, frontSlotUnlockInterval);
			int upcomingRound = Mathf.Max(1, completedRound + 1);
			int frontUnlocks = Mathf.Clamp(upcomingRound / interval, 0, Mathf.Max(0, maxFrontUnlockCount));
			return Mathf.Clamp(baseCount + frontUnlocks, 0, slots.Count);
		}


		public void ClearAllDeployedUnits()
		{
			CancelActiveDrag();
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot slot = slots[i];
				DefenderUnit unit = ((slot != null) ? slot.OccupiedUnit : null);
				if (!(unit == null))
				{
					unit.RemoveFromBoard();
					UnityEngine.Object.Destroy(unit.gameObject);
				}
			}
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
			CancelActiveDrag();
			spawnedUnit = null;
			BoardSlot emptySlot = slots.FirstOrDefault((BoardSlot slot) => slot != null && slot.IsAvailable && slot.IsEmpty);
			if (emptySlot == null || definition == null)
			{
				return false;
			}
			GameObject sourcePrefab = ((definition.prefab != null) ? definition.prefab : ((prefabOverride != null) ? prefabOverride.gameObject : ((fallbackUnitPrefab != null) ? fallbackUnitPrefab.gameObject : null)));
			if (sourcePrefab == null)
			{
				Debug.LogError("No DefenderUnit prefab assigned.");
				return false;
			}
			GameObject spawnedObject = UnityEngine.Object.Instantiate(sourcePrefab, emptySlot.UnitAnchor.position, Quaternion.identity);
			DefenderUnit unit = spawnedObject.GetComponent<DefenderUnit>();
			if (unit == null)
			{
				unit = spawnedObject.AddComponent<DefenderUnit>();
			}
			unit.AdoptRuntimeTemplate((prefabOverride != null) ? prefabOverride : fallbackUnitPrefab);
			unit.gameObject.SetActive(value: true);
			emptySlot.AssignUnit(unit);
			unit.Initialize(definition);
			RuntimeCombatFeedback.ShowGroundPulse(emptySlot.UnitAnchor.position, Color.Lerp(definition.accentColor, Color.white, 0.25f), 0.72f, 0.42f, 0.1f);
			RuntimeGameFeel.PlaySummonArrivalVfx(emptySlot.UnitAnchor.position, definition.accentColor, definition.grade);
			spawnedUnit = unit;
			return true;
		}

		public bool TryMergeUnitsOfGrade(CharacterGrade grade, CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride = null, RunContentRandomService contentRandom = null)
		{
			CancelActiveDrag();
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
			List<DefenderUnit> sameGradeUnits = (from slot in slots
				where slot != null && !slot.IsEmpty
				select slot.OccupiedUnit into item
				where item != null && item.Grade == grade
				orderby reservedRecipeUnits.Contains(item) ? 1 : 0
				select item).Take(3).ToList();
			if (sameGradeUnits.Count < 3)
			{
				LastMergeFailureReason = "합성 재료가 부족합니다.  " + sameGradeUnits.Count + "/3";
				return false;
			}
			CharacterGrade nextGrade = grade + 1;
			CharacterDefinition mergedCharacter = contentRandom != null
			? database.GetRunContentRandomCharacterByGrade(nextGrade, contentRandom, RunContentRandomChannel.Merge, "normalMerge", false)
			: database.GetRandomCharacterByGrade(nextGrade);
			if (mergedCharacter == null)
			{
				LastMergeFailureReason = "상위 등급 결과 유닛 데이터가 없습니다.";
				return false;
			}
			BoardSlot spawnSlot = sameGradeUnits[0].CurrentSlot;
			if (spawnSlot == null)
			{
				LastMergeFailureReason = "합성 결과를 배치할 슬롯을 찾지 못했습니다.";
				return false;
			}
			GameObject sourcePrefab = ((mergedCharacter.prefab != null) ? mergedCharacter.prefab : ((prefabOverride != null) ? prefabOverride.gameObject : ((fallbackUnitPrefab != null) ? fallbackUnitPrefab.gameObject : null)));
			if (sourcePrefab == null)
			{
				LastMergeFailureReason = "상위 등급 유닛 프리팹을 찾지 못했습니다.";
				Debug.LogError("No DefenderUnit prefab assigned for merge result.");
				return false;
			}
			float inheritedAttackPower = sameGradeUnits.Sum((DefenderUnit defenderUnit) => (defenderUnit != null) ? defenderUnit.EffectiveAttackPowerWithoutRunGradeUpgrade : 0f) * 0.98f;
			float inheritedMaxHealth = sameGradeUnits.Sum((DefenderUnit defenderUnit) => (defenderUnit != null) ? defenderUnit.MaxHealthWithoutRunGradeUpgrade : 0f) * 0.94f;
			for (int i = 0; i < sameGradeUnits.Count; i++)
			{
				sameGradeUnits[i].RemoveFromBoard();
				UnityEngine.Object.Destroy(sameGradeUnits[i].gameObject);
			}
			GameObject spawnedObject = UnityEngine.Object.Instantiate(sourcePrefab, spawnSlot.UnitAnchor.position, Quaternion.identity);
			DefenderUnit unit = spawnedObject.GetComponent<DefenderUnit>();
			if (unit == null)
			{
				unit = spawnedObject.AddComponent<DefenderUnit>();
			}
			unit.AdoptRuntimeTemplate((prefabOverride != null) ? prefabOverride : fallbackUnitPrefab);
			unit.gameObject.SetActive(value: true);
			spawnSlot.AssignUnit(unit);
			unit.Initialize(mergedCharacter);
			unit.ApplyMergeInheritance(inheritedAttackPower, inheritedMaxHealth);
			SpawnMergeVfx(spawnSlot, mergedCharacter.accentColor, nextGrade, ultimate: false);
			mergeResult = new MergeResultInfo
			{
				sourceGrade = grade,
				resultGrade = nextGrade,
				sourceDescription = CharacterGradeUtility.GetDisplayName(grade) + " x3",
				consumedUnitCount = sameGradeUnits.Count,
				resultCharacterName = mergedCharacter.displayName,
				resultColor = mergedCharacter.accentColor
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
			GetBestUltimateRecipeProgress(out var progress, out var required);
			return "재료 " + progress + "/" + Mathf.Max(1, required);
		}

		public bool HasAnyUltimateRecipeProgress()
		{
			GetBestUltimateRecipeProgress(out int progress, out int required);
			return required > 0 && progress > 0;
		}

		public string GetUltimateMergeDetailStatus(CharacterDatabase database)
		{
			UltimateMergeRecipe recipe = GetBestUltimateRecipe(database);
			if (recipe == null)
			{
				return HasBlockedReadyUltimateRecipe(database) ? "이미 보유한 초월 | 다른 레시피 필요" : "초월 레시피 없음";
			}
			int progress = GetUltimateRecipeProgress(recipe);
			int required = Mathf.Max(1, GetUltimateRecipeRequiredCount(recipe));
			string prefix = ((progress >= required) ? "초월 준비 완료" : ("초월 " + progress + "/" + required));
			string detail = ((recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0) ? BuildRequiredCharacterRecipeStatus(recipe, database) : BuildGradeRecipeStatus(recipe));
			return prefix + "  |  " + detail;
		}

		public string GetUltimateMergeActionStatus(CharacterDatabase database)
		{
			UltimateMergeRecipe recipe = GetBestUltimateRecipe(database);
			if (recipe == null)
			{
				return HasBlockedReadyUltimateRecipe(database) ? "이미 보유한 초월" : "초월 레시피 없음";
			}
			int progress = GetUltimateRecipeProgress(recipe);
			int required = Mathf.Max(1, GetUltimateRecipeRequiredCount(recipe));
			if (progress >= required)
			{
				return "초월 조합 실행";
			}
			string missing = GetFirstMissingRecipeMaterialName(recipe, database);
			if (!string.IsNullOrWhiteSpace(missing))
			{
				return progress + "/" + required + "  " + missing + " 찾기";
			}
			return progress + "/" + required + "  핵심 재료 보존";
		}

		public string GetUltimateRecipeBingoStatus(CharacterDatabase database)
		{
			if (UltimateRecipes == null || UltimateRecipes.Length == 0)
			{
				return "레시피 빙고 없음";
			}
			List<UltimateMergeRecipe> recipes = new List<UltimateMergeRecipe>(UltimateRecipes);
			recipes.Sort(CompareUltimateRecipeBingoPriority);
			List<string> rows = new List<string>();
			rows.Add("레시피 빙고  한 줄 완성: 운명 +20");
			for (int i = 0; i < recipes.Count; i++)
			{
				UltimateMergeRecipe recipe = recipes[i];
				int progress = GetUltimateRecipeProgress(recipe);
				int required = Mathf.Max(1, GetUltimateRecipeRequiredCount(recipe));
				string state = ((progress >= required) ? "완성" : (progress + "/" + required));
				string marker = ((i == 0) ? "TOP " : "    ");
				rows.Add(marker + state + "  " + CompactRecipeName(recipe.name) + "  " + BuildRecipeBingoMaterialStatus(recipe, database));
			}
			return string.Join("\n", rows);
		}

		public string[] GetReadyUltimateRecipeNames(CharacterDatabase database = null)
		{
			List<string> readyRecipes = new List<string>();
			for (int i = 0; i < UltimateRecipes.Length; i++)
			{
				UltimateMergeRecipe recipe = UltimateRecipes[i];
				if (recipe != null && CanSatisfyUltimateRecipe(recipe) && HasAvailableUltimateResult(database, recipe))
				{
					readyRecipes.Add(recipe.name);
				}
			}
			return readyRecipes.ToArray();
		}

		public UltimateRecipeOption[] GetReadyUltimateRecipeOptions(CharacterDatabase database = null)
		{
			List<UltimateRecipeOption> options = new List<UltimateRecipeOption>();
			for (int i = 0; i < UltimateRecipes.Length; i++)
			{
				UltimateMergeRecipe recipe = UltimateRecipes[i];
				if (recipe != null && CanSatisfyUltimateRecipe(recipe) && HasAvailableUltimateResult(database, recipe))
				{
					CharacterDefinition result = GetUltimateResultDefinition(database, recipe);
					string resultSummary = (result != null) ? result.displayName : "결과 유닛 확인 필요";
					Color accentColor = (result != null) ? result.accentColor : CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f));
					string materialSummary = ((recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0) ? string.Join(" + ", recipe.requiredCharacterIds.Select((string id) => ResolveCharacterName(database, id)).ToArray()) : BuildGradeRecipeStatus(recipe));
					UltimateRecipeMaterialView[] materials = BuildUltimateRecipeMaterialViews(recipe, database);
					options.Add(new UltimateRecipeOption(recipe.name, CompactRecipeName(recipe.name), materialSummary, resultSummary, accentColor, isReady: true, GetUltimateRecipeRequiredCount(recipe), GetUltimateRecipeRequiredCount(recipe), string.Empty, recipe.resultCharacterId, result, materials, 0, i));
				}
			}
			return options.ToArray();
		}

		public UltimateRecipeOption[] GetAllUltimateRecipeOptions(CharacterDatabase database = null)
		{
			List<UltimateRecipeOption> options = new List<UltimateRecipeOption>();
			for (int i = 0; i < UltimateRecipes.Length; i++)
			{
				UltimateMergeRecipe recipe = UltimateRecipes[i];
				if (recipe != null)
				{
					CharacterDefinition result = GetUltimateResultDefinition(database, recipe);
					string resultSummary = (result != null) ? result.displayName : "결과 유닛 확인 필요";
					Color accentColor = (result != null) ? result.accentColor : CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f));
					string materialSummary = ((recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0) ? string.Join(" + ", recipe.requiredCharacterIds.Select((string id) => ResolveCharacterName(database, id)).ToArray()) : BuildGradeRecipeStatus(recipe));
					int progress = GetUltimateRecipeProgress(recipe);
					int required = Mathf.Max(1, GetUltimateRecipeRequiredCount(recipe));
					bool ready = progress >= required && HasAvailableUltimateResult(database, recipe);
					UltimateRecipeMaterialView[] materials = BuildUltimateRecipeMaterialViews(recipe, database);
					int missingMaterialCount = materials.Sum((UltimateRecipeMaterialView material) => Mathf.Max(0, material.requiredCount - material.ownedCount));
					options.Add(new UltimateRecipeOption(recipe.name, CompactRecipeName(recipe.name), materialSummary, resultSummary, accentColor, ready, progress, required, BuildMissingRecipeMaterialSummary(recipe, database), recipe.resultCharacterId, result, materials, missingMaterialCount, i));
				}
			}
			options.Sort(delegate(UltimateRecipeOption left, UltimateRecipeOption right)
			{
				if (left.isReady != right.isReady)
				{
					return (!left.isReady) ? 1 : (-1);
				}
				if (left.missingMaterialCount != right.missingMaterialCount)
				{
					return left.missingMaterialCount.CompareTo(right.missingMaterialCount);
				}
				int num = left.progress * Mathf.Max(1, right.required);
				int num2 = right.progress * Mathf.Max(1, left.required);
				return (num2 != num) ? num2.CompareTo(num) : left.definitionOrder.CompareTo(right.definitionOrder);
			});
			return options.ToArray();
		}

		public UltimateRecipeOption[] GetRelatedUltimateRecipeOptions(CharacterDatabase database = null)
		{
			UltimateRecipeOption[] allOptions = GetAllUltimateRecipeOptions(database);
			return allOptions.Where((UltimateRecipeOption option) => option.isReady || option.progress > 0 || (option.materials != null && option.materials.Sum((UltimateRecipeMaterialView material) => material.ownedCount) > 0)).ToArray();
		}

		private UltimateRecipeMaterialView[] BuildUltimateRecipeMaterialViews(UltimateMergeRecipe recipe, CharacterDatabase database)
		{
			if (recipe == null)
			{
				return new UltimateRecipeMaterialView[0];
			}
			List<UltimateRecipeMaterialView> materials = new List<UltimateRecipeMaterialView>();
			if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
			{
				List<DefenderUnit> candidates = GetRecipeCandidateUnits();
				for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
				{
					string requiredId = recipe.requiredCharacterIds[i];
					if (materials.Any((UltimateRecipeMaterialView material) => material.characterId == requiredId))
					{
						continue;
					}
					int requiredCount = recipe.requiredCharacterIds.Count((string id) => id == requiredId);
					int ownedCount = candidates.Count((DefenderUnit unit) => unit != null && unit.Definition != null && unit.Definition.id == requiredId);
					CharacterDefinition definition = (database != null) ? database.GetCharacterById(requiredId) : null;
					Color accentColor = (definition != null) ? definition.accentColor : CharacterGradeUtility.GetColor(CharacterGrade.Mythic, new Color(0.9f, 0.55f, 0.25f));
					materials.Add(new UltimateRecipeMaterialView(requiredId, ResolveCharacterName(database, requiredId), (definition != null) ? definition.grade : CharacterGrade.Mythic, accentColor, Mathf.Min(ownedCount, requiredCount), requiredCount, definition));
				}
			}
			else
			{
				AddUltimateRecipeGradeMaterial(materials, CharacterGrade.Mythic, recipe.mythicCount);
				AddUltimateRecipeGradeMaterial(materials, CharacterGrade.Legendary, recipe.legendaryCount);
				AddUltimateRecipeGradeMaterial(materials, CharacterGrade.Epic, recipe.epicCount);
			}
			return materials.ToArray();
		}

		private void AddUltimateRecipeGradeMaterial(List<UltimateRecipeMaterialView> materials, CharacterGrade grade, int requiredCount)
		{
			if (requiredCount <= 0)
			{
				return;
			}
			int ownedCount = Mathf.Min(CountUnitsOfGrade(grade), requiredCount);
			materials.Add(new UltimateRecipeMaterialView(string.Empty, CharacterGradeUtility.GetDisplayName(grade), grade, CharacterGradeUtility.GetColor(grade, new Color(0.72f, 0.54f, 1f)), ownedCount, requiredCount, null));
		}

		private string BuildMissingRecipeMaterialSummary(UltimateMergeRecipe recipe, CharacterDatabase database)
		{
			List<string> missing = new List<string>();
			if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
			{
				List<DefenderUnit> candidates = GetRecipeCandidateUnits();
				for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
				{
					string id = recipe.requiredCharacterIds[i];
					DefenderUnit match = candidates.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == id);
					if (match == null)
					{
						missing.Add(ResolveCharacterName(database, id));
					}
					else
					{
						candidates.Remove(match);
					}
				}
			}
			else
			{
				AddMissingGradeSummary(missing, CharacterGrade.Mythic, recipe.mythicCount);
				AddMissingGradeSummary(missing, CharacterGrade.Legendary, recipe.legendaryCount);
				AddMissingGradeSummary(missing, CharacterGrade.Epic, recipe.epicCount);
			}
			return (missing.Count > 0) ? string.Join(", ", missing.ToArray()) : "없음";
		}

		private void AddMissingGradeSummary(List<string> missing, CharacterGrade grade, int requiredCount)
		{
			int shortage = Mathf.Max(0, requiredCount - CountUnitsOfGrade(grade));
			if (shortage > 0)
			{
				missing.Add(CharacterGradeUtility.GetDisplayName(grade) + " ×" + shortage);
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
			UltimateMergeRecipe recipe = FindUltimateRecipe(recipeName);
			if (database == null || recipe == null || !CanSatisfyUltimateRecipe(recipe) || !HasAvailableUltimateResult(database, recipe))
			{
				return false;
			}
			return ExecuteUltimateMerge(database, recipe, out mergeResult, prefabOverride);
		}

		private bool TryMergeUltimate(CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride)
		{
			mergeResult = default(MergeResultInfo);
			if (database == null || !TryFindUltimateRecipe(database, out var recipe))
			{
				return false;
			}
			return ExecuteUltimateMerge(database, recipe, out mergeResult, prefabOverride);
		}

		private bool ExecuteUltimateMerge(CharacterDatabase database, UltimateMergeRecipe recipe, out MergeResultInfo mergeResult, DefenderUnit prefabOverride)
		{
			CancelActiveDrag();
			mergeResult = default(MergeResultInfo);
			CharacterDefinition mergedCharacter = ResolveUltimateMergeResult(database, recipe);
			List<DefenderUnit> selectedUnits = SelectUnitsForUltimateRecipe(recipe);
			BoardSlot spawnSlot = ((selectedUnits.Count > 0) ? selectedUnits[0].CurrentSlot : null);
			if (mergedCharacter == null || spawnSlot == null)
			{
				return false;
			}
			for (int i = 0; i < selectedUnits.Count; i++)
			{
				selectedUnits[i].RemoveFromBoard();
				UnityEngine.Object.Destroy(selectedUnits[i].gameObject);
			}
			GameObject sourcePrefab = ((mergedCharacter.prefab != null) ? mergedCharacter.prefab : ((prefabOverride != null) ? prefabOverride.gameObject : ((fallbackUnitPrefab != null) ? fallbackUnitPrefab.gameObject : null)));
			if (sourcePrefab == null)
			{
				Debug.LogError("No DefenderUnit prefab assigned for ultimate merge result.");
				return false;
			}
			GameObject spawnedObject = UnityEngine.Object.Instantiate(sourcePrefab, spawnSlot.UnitAnchor.position, Quaternion.identity);
			DefenderUnit unit = spawnedObject.GetComponent<DefenderUnit>();
			if (unit == null)
			{
				unit = spawnedObject.AddComponent<DefenderUnit>();
			}
			unit.AdoptRuntimeTemplate((prefabOverride != null) ? prefabOverride : fallbackUnitPrefab);
			unit.gameObject.SetActive(value: true);
			spawnSlot.AssignUnit(unit);
			unit.Initialize(mergedCharacter);
			SpawnMergeVfx(spawnSlot, mergedCharacter.accentColor, mergedCharacter.grade, ultimate: true);
			mergeResult = new MergeResultInfo
			{
				sourceGrade = CharacterGrade.Mythic,
				resultGrade = mergedCharacter.grade,
				recipeName = recipe.name,
				sourceDescription = recipe.displayText,
				consumedUnitCount = selectedUnits.Count,
				isFinalMerge = (mergedCharacter.grade == CharacterGrade.Transcendent),
				resultCharacterName = mergedCharacter.displayName,
				resultColor = mergedCharacter.accentColor
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
				UltimateMergeRecipe recipe = UltimateRecipes[i];
				if (recipe != null && string.Equals(recipe.name, recipeName, StringComparison.Ordinal))
				{
					return recipe;
				}
			}
			return null;
		}

		private CharacterDefinition ResolveUltimateMergeResult(CharacterDatabase database, UltimateMergeRecipe recipe)
		{
			CharacterDefinition result = GetUltimateResultDefinition(database, recipe);
			return IsDeployableUltimateResult(result) && !IsBlockedTranscendentResult(result) ? result : null;
		}

		private static CharacterDefinition GetUltimateResultDefinition(CharacterDatabase database, UltimateMergeRecipe recipe)
		{
			if (database == null || recipe == null || string.IsNullOrWhiteSpace(recipe.resultCharacterId))
			{
				return null;
			}
			CharacterDefinition result = database.GetCharacterById(recipe.resultCharacterId);
			return IsDeployableUltimateResult(result) ? result : null;
		}

		private static bool IsDeployableUltimateResult(CharacterDefinition character)
		{
			return character != null && character.grade == CharacterGrade.Transcendent;
		}

		private bool IsBlockedTranscendentResult(CharacterDefinition character)
		{
			if (character == null || character.grade != CharacterGrade.Transcendent)
			{
				return false;
			}
			return slots.Any((BoardSlot slot) => slot != null && slot.OccupiedUnit != null && slot.OccupiedUnit.Definition != null && slot.OccupiedUnit.Definition.id == character.id);
		}

		private bool HasAvailableUltimateResult(CharacterDatabase database, UltimateMergeRecipe recipe)
		{
			if (database == null)
			{
				return true;
			}
			return ResolveUltimateMergeResult(database, recipe) != null;
		}

		private bool HasBlockedReadyUltimateRecipe(CharacterDatabase database)
		{
			if (database == null)
			{
				return false;
			}
			for (int i = 0; i < UltimateRecipes.Length; i++)
			{
				UltimateMergeRecipe candidate = UltimateRecipes[i];
				if (candidate != null && CanSatisfyUltimateRecipe(candidate) && !HasAvailableUltimateResult(database, candidate))
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
				UltimateMergeRecipe candidate = UltimateRecipes[i];
				if (CanSatisfyUltimateRecipe(candidate) && HasAvailableUltimateResult(database, candidate))
				{
					recipe = candidate;
					return true;
				}
			}
			recipe = null;
			return false;
		}

		private UltimateMergeRecipe GetBestUltimateRecipe(CharacterDatabase database = null)
		{
			if (TryFindUltimateRecipe(database, out var readyRecipe))
			{
				return readyRecipe;
			}
			UltimateMergeRecipe bestRecipe = null;
			int bestProgress = -1;
			int bestRequired = 1;
			for (int i = 0; i < UltimateRecipes.Length; i++)
			{
				UltimateMergeRecipe recipe = UltimateRecipes[i];
				int required = GetUltimateRecipeRequiredCount(recipe);
				int progress = GetUltimateRecipeProgress(recipe);
				if (IsBetterUltimateRecipeProgress(progress, required, bestProgress, bestRequired))
				{
					bestRecipe = recipe;
					bestProgress = progress;
					bestRequired = required;
				}
			}
			return bestRecipe;
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
			if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0 && TrySelectUnitsByCharacterIds(recipe.requiredCharacterIds, out var exactUnits))
			{
				return exactUnits;
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
				IEnumerable<DefenderUnit> candidates = (from slot in slots
					where slot != null && !slot.IsEmpty
					select slot.OccupiedUnit into unit
					where unit != null && unit.Grade == grade
					select unit).Take(count);
				result.AddRange(candidates);
			}
		}

		private bool TrySelectUnitsByCharacterIds(string[] requiredIds, out List<DefenderUnit> selectedUnits)
		{
			selectedUnits = new List<DefenderUnit>();
			List<DefenderUnit> candidates = (from slot in slots
				where slot != null && !slot.IsEmpty
				select slot.OccupiedUnit into unit
				where unit != null && unit.Definition != null
				select unit).ToList();
			foreach (string requiredId in requiredIds)
			{
				DefenderUnit match = candidates.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
				if (match == null)
				{
					selectedUnits.Clear();
					return false;
				}
				selectedUnits.Add(match);
				candidates.Remove(match);
			}
			return true;
		}

		private HashSet<DefenderUnit> SelectReservedUltimateRecipeUnits()
		{
			HashSet<DefenderUnit> reservedUnits = new HashSet<DefenderUnit>();
			UltimateMergeRecipe recipe = FindUltimateRecipe(previewUltimateRecipeName);
			if (ultimateRecipePreviewActive)
			{
				if (recipe == null || !CanSatisfyUltimateRecipe(recipe))
				{
					return reservedUnits;
				}
			}
			else
			{
				recipe = GetBestUltimateRecipe();
			}
			if (recipe == null)
			{
				return reservedUnits;
			}
			List<DefenderUnit> candidates = GetRecipeCandidateUnits();
			if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
			{
				for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
				{
					string requiredId = recipe.requiredCharacterIds[i];
					DefenderUnit match = candidates.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
					if (!(match == null))
					{
						reservedUnits.Add(match);
						candidates.Remove(match);
					}
				}
				return reservedUnits;
			}
			AddReservedUnitsOfGrade(reservedUnits, candidates, CharacterGrade.Mythic, recipe.mythicCount);
			AddReservedUnitsOfGrade(reservedUnits, candidates, CharacterGrade.Legendary, recipe.legendaryCount);
			AddReservedUnitsOfGrade(reservedUnits, candidates, CharacterGrade.Epic, recipe.epicCount);
			return reservedUnits;
		}

		private string BuildRequiredCharacterRecipeStatus(UltimateMergeRecipe recipe, CharacterDatabase database)
		{
			List<DefenderUnit> candidates = GetRecipeCandidateUnits();
			List<string> owned = new List<string>();
			List<string> missing = new List<string>();
			for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
			{
				string requiredId = recipe.requiredCharacterIds[i];
				DefenderUnit match = candidates.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
				bool hasMaterial = match != null;
				if (hasMaterial)
				{
					candidates.Remove(match);
				}
				string materialName = ResolveCharacterName(database, requiredId);
				if (hasMaterial)
				{
					owned.Add(materialName);
				}
				else
				{
					missing.Add(materialName);
				}
			}
			List<string> parts = new List<string>();
			if (owned.Count > 0)
			{
				parts.Add("보존 " + string.Join(", ", owned));
			}
			if (missing.Count > 0)
			{
				parts.Add("부족 " + string.Join(", ", missing));
			}
			return string.Join(" / ", parts);
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
			bool leftReady = CanSatisfyUltimateRecipe(left);
			bool rightReady = CanSatisfyUltimateRecipe(right);
			if (leftReady != rightReady)
			{
				return (!leftReady) ? 1 : (-1);
			}
			int leftRequired = Mathf.Max(1, GetUltimateRecipeRequiredCount(left));
			int rightRequired = Mathf.Max(1, GetUltimateRecipeRequiredCount(right));
			int leftProgress = Mathf.Max(0, GetUltimateRecipeProgress(left));
			int rightProgress = Mathf.Max(0, GetUltimateRecipeProgress(right));
			long leftScaled = (long)leftProgress * (long)rightRequired;
			long rightScaled = (long)rightProgress * (long)leftRequired;
			if (leftScaled != rightScaled)
			{
				return (leftScaled <= rightScaled) ? 1 : (-1);
			}
			if (leftProgress != rightProgress)
			{
				return (leftProgress <= rightProgress) ? 1 : (-1);
			}
			return leftRequired.CompareTo(rightRequired);
		}

		private string BuildRecipeBingoMaterialStatus(UltimateMergeRecipe recipe, CharacterDatabase database)
		{
			if (recipe == null)
			{
				return string.Empty;
			}
			if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
			{
				List<DefenderUnit> candidates = GetRecipeCandidateUnits();
				List<string> parts = new List<string>();
				for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
				{
					string requiredId = recipe.requiredCharacterIds[i];
					DefenderUnit match = candidates.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
					bool hasMaterial = match != null;
					if (hasMaterial)
					{
						candidates.Remove(match);
					}
					string materialName = CompactRecipeMaterialName(ResolveCharacterName(database, requiredId));
					parts.Add((hasMaterial ? "[O] " : "[ ] ") + materialName);
				}
				return string.Join("  ", parts);
			}
			List<string> gradeParts = new List<string>();
			AddRecipeBingoGradePart(gradeParts, CharacterGrade.Mythic, recipe.mythicCount);
			AddRecipeBingoGradePart(gradeParts, CharacterGrade.Legendary, recipe.legendaryCount);
			AddRecipeBingoGradePart(gradeParts, CharacterGrade.Epic, recipe.epicCount);
			return (gradeParts.Count > 0) ? string.Join("  ", gradeParts) : recipe.displayText;
		}

		private void AddRecipeBingoGradePart(List<string> parts, CharacterGrade grade, int requiredCount)
		{
			if (requiredCount > 0)
			{
				int count = Mathf.Min(CountUnitsOfGrade(grade), requiredCount);
				parts.Add(((count >= requiredCount) ? "[O] " : "[ ] ") + CharacterGradeUtility.GetDisplayName(grade) + " " + count + "/" + requiredCount);
			}
		}

		private static string CompactRecipeName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "Recipe";
			}
			string compact = value.Replace(" Rite", string.Empty).Trim();
			return (compact.Length <= 22) ? compact : (compact.Substring(0, 19) + "...");
		}

		private static string CompactRecipeMaterialName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "?";
			}
			string compact = value.Trim();
			return (compact.Length <= 7) ? compact : (compact.Substring(0, 6) + ".");
		}

		private string GetFirstMissingRecipeMaterialName(UltimateMergeRecipe recipe, CharacterDatabase database)
		{
			if (recipe == null)
			{
				return string.Empty;
			}
			if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length != 0)
			{
				List<DefenderUnit> candidates = GetRecipeCandidateUnits();
				for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
				{
					string requiredId = recipe.requiredCharacterIds[i];
					DefenderUnit match = candidates.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
					if (match == null)
					{
						return ResolveCharacterName(database, requiredId);
					}
					candidates.Remove(match);
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
			int remaining = requiredCount;
			for (int i = 0; i < candidates.Count; i++)
			{
				if (remaining <= 0)
				{
					break;
				}
				DefenderUnit unit = candidates[i];
				if (!(unit == null) && unit.Grade == grade)
				{
					reservedUnits.Add(unit);
					remaining--;
				}
			}
		}

		private void RefreshUltimateRecipeMarkers()
		{
			HashSet<DefenderUnit> reservedUnits = SelectReservedUltimateRecipeUnits();
			Color markerColor = CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f));
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot slot = slots[i];
				DefenderUnit unit = ((slot != null && !slot.IsEmpty) ? slot.OccupiedUnit : null);
				if (!(unit == null))
				{
					bool reserved = reservedUnits.Contains(unit);
					unit.SetRecipeMaterialMarker(reserved, reserved ? "초월 재료" : string.Empty, markerColor);
				}
			}
		}

		private string BuildGradeRecipeStatus(UltimateMergeRecipe recipe)
		{
			List<string> parts = new List<string>();
			AddGradeRecipePart(parts, CharacterGrade.Mythic, recipe.mythicCount);
			AddGradeRecipePart(parts, CharacterGrade.Legendary, recipe.legendaryCount);
			AddGradeRecipePart(parts, CharacterGrade.Epic, recipe.epicCount);
			return (parts.Count > 0) ? string.Join(" / ", parts) : recipe.displayText;
		}

		private void AddGradeRecipePart(List<string> parts, CharacterGrade grade, int requiredCount)
		{
			if (requiredCount > 0)
			{
				int count = Mathf.Min(CountUnitsOfGrade(grade), requiredCount);
				parts.Add(CharacterGradeUtility.GetDisplayName(grade) + " " + count + "/" + requiredCount);
			}
		}

		private List<DefenderUnit> GetRecipeCandidateUnits()
		{
			return (from slot in slots
				where slot != null && !slot.IsEmpty
				select slot.OccupiedUnit into unit
				where unit != null && unit.Definition != null
				select unit).ToList();
		}

		private string ResolveCharacterName(CharacterDatabase database, string characterId)
		{
			CharacterDefinition definition = ((database != null) ? database.GetCharacterById(characterId) : null);
			if (definition == null || string.IsNullOrWhiteSpace(definition.displayName))
			{
				return characterId;
			}
			return definition.displayName;
		}

		private void GetBestUltimateRecipeProgress(out int bestProgress, out int bestRequired)
		{
			bestProgress = 0;
			bestRequired = 3;
			for (int i = 0; i < UltimateRecipes.Length; i++)
			{
				UltimateMergeRecipe recipe = UltimateRecipes[i];
				int required = GetUltimateRecipeRequiredCount(recipe);
				int progress = GetUltimateRecipeProgress(recipe);
				if (IsBetterUltimateRecipeProgress(progress, required, bestProgress, bestRequired))
				{
					bestProgress = progress;
					bestRequired = required;
				}
			}
		}

		private bool IsBetterUltimateRecipeProgress(int progress, int required, int bestProgress, int bestRequired)
		{
			if (bestProgress < 0)
			{
				return true;
			}
			int safeRequired = Mathf.Max(1, required);
			int safeBestRequired = Mathf.Max(1, bestRequired);
			long scaledProgress = (long)Mathf.Max(0, progress) * (long)safeBestRequired;
			long scaledBestProgress = (long)Mathf.Max(0, bestProgress) * (long)safeRequired;
			if (scaledProgress != scaledBestProgress)
			{
				return scaledProgress > scaledBestProgress;
			}
			return progress > bestProgress || (progress == bestProgress && safeRequired < safeBestRequired);
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
			List<DefenderUnit> candidates = (from slot in slots
				where slot != null && !slot.IsEmpty
				select slot.OccupiedUnit into unit
				where unit != null && unit.Definition != null
				select unit).ToList();
			int progress = 0;
			for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
			{
				string requiredId = recipe.requiredCharacterIds[i];
				DefenderUnit match = candidates.FirstOrDefault((DefenderUnit unit) => unit.Definition.id == requiredId);
				if (!(match == null))
				{
					progress++;
					candidates.Remove(match);
				}
			}
			return progress;
		}

		public int CountUnitsOfGrade(CharacterGrade grade)
		{
			return slots.Count((BoardSlot slot) => slot != null && slot.OccupiedUnit != null && slot.OccupiedUnit.Grade == grade);
		}

		private void SpawnMergeVfx(BoardSlot slot, Color color, CharacterGrade resultGrade, bool ultimate)
		{
			if (slot == null || slot.UnitAnchor == null)
			{
				return;
			}
			bool transcendentResult = resultGrade == CharacterGrade.Transcendent;
			bool mythicResult = resultGrade == CharacterGrade.Mythic;
			bool jackpot = ultimate || resultGrade >= CharacterGrade.Rare;
			bool major = ultimate || resultGrade >= CharacterGrade.Epic;
			RuntimeGameFeel.PlayMergeResultVfx(slot.UnitAnchor.position, color, resultGrade, ultimate);
			if (ultimate)
			{
				RuntimeGameFeel.PlayJackpotPulse(slot.UnitAnchor.position, color, 2.08f, 0.22f, 0.46f, 0.12f, 0.16f, 4);
				if (transcendentResult)
				{
					RuntimeAudioUtility.PlayJackpotUltimate();
				}
				else if (mythicResult)
				{
					RuntimeAudioUtility.PlayMythicSpawn();
				}
				else
				{
					RuntimeAudioUtility.PlayJackpotMajor();
				}
			}
			else if (jackpot)
			{
				RuntimeGameFeel.PlayJackpotPulse(slot.UnitAnchor.position, color, major ? 1.62f : 1.22f, major ? 0.15f : 0.09f, major ? 0.34f : 0.23f, major ? 0.18f : 0.3f, major ? 0.12f : 0.075f, major ? 3 : 2);
				if (mythicResult)
				{
					RuntimeAudioUtility.PlayMythicSpawn();
				}
				else if (major)
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
				where slot != null && !slot.IsEmpty
				select slot.OccupiedUnit into unit
				where unit != null
				select unit).ToArray();
		}

		public bool IsReservedForUltimateRecipeUnit(DefenderUnit unit)
		{
			if (unit == null)
			{
				return false;
			}
			return SelectReservedUltimateRecipeUnits().Contains(unit);
		}

		public bool TryRemoveUnitFromBoard(DefenderUnit unit)
		{
			if (draggedUnit == unit)
			{
				CancelActiveDrag();
			}
			if (unit == null || unit.CurrentSlot == null || unit.IsTemporarySummon)
			{
				return false;
			}
			if (selectedRangeUnit == unit)
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
			if (unit == null || targetSlot == null || targetSlot.IsLocked)
			{
				return false;
			}
			BoardSlot sourceSlot = unit.CurrentSlot;
			if (sourceSlot == null)
			{
				return false;
			}
			if (sourceSlot == targetSlot && (targetSlot.IsEmpty || targetSlot.OccupiedUnit == unit))
			{
				targetSlot.AssignUnit(unit);
				return true;
			}
			DefenderUnit targetUnit = targetSlot.OccupiedUnit;
			if (targetUnit == unit)
			{
				targetSlot.AssignUnit(unit);
				return true;
			}
			sourceSlot.Clear();
			if (targetUnit != null)
			{
				targetSlot.Clear();
			}
			targetSlot.AssignUnit(unit);
			if (targetUnit != null)
			{
				sourceSlot.AssignUnit(targetUnit);
			}
			return true;
		}

		public void CancelActiveDrag()
		{
			DefenderUnit unit = draggedUnit;
			BoardSlot originSlot = draggedOriginSlot;
			RestoreDraggedColliders();
			if (unit != null)
			{
				unit.SetBoardDragCombatSuspended(false);
				BoardSlot currentSlot = unit.CurrentSlot;
				if (currentSlot != null && currentSlot.OccupiedUnit == unit)
				{
					currentSlot.AssignUnit(unit);
				}
				else if (originSlot != null && !originSlot.IsLocked && (originSlot.IsEmpty || originSlot.OccupiedUnit == unit))
				{
					originSlot.AssignUnit(unit);
				}
				SharedFloatingCombatCanvas.SetPoseRefreshOverride(unit.transform, false);
			}
			draggedUnit = null;
			draggedOriginSlot = null;
			draggedColliders = null;
			draggedColliderEnabledStates = null;
			dragOffset = Vector3.zero;
			draggedUnitLocalExtent = Vector2.zero;
			pendingPointerUnit = null;
		}

		public void RestoreDraggedUnitToOrigin()
		{
			CancelActiveDrag();
		}

		private void RestoreDraggedColliders()
		{
			if (draggedColliders == null)
			{
				return;
			}
			for (int i = 0; i < draggedColliders.Length; i++)
			{
				Collider collider = draggedColliders[i];
				if (collider != null)
				{
					collider.enabled = draggedColliderEnabledStates != null && i < draggedColliderEnabledStates.Length ? draggedColliderEnabledStates[i] : true;
				}
			}
		}

		private void CompleteActiveDrag()
		{
			DefenderUnit unit = draggedUnit;
			RestoreDraggedColliders();
			if (unit != null)
			{
				unit.SetBoardDragCombatSuspended(false);
				SharedFloatingCombatCanvas.SetPoseRefreshOverride(unit.transform, false);
			}
			draggedUnit = null;
			draggedOriginSlot = null;
			draggedColliders = null;
			draggedColliderEnabledStates = null;
			dragOffset = Vector3.zero;
			draggedUnitLocalExtent = Vector2.zero;
			pendingPointerUnit = null;
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		private void ValidateBoardSlotInvariant()
		{
			HashSet<DefenderUnit> slottedUnits = new HashSet<DefenderUnit>();
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot slot = slots[i];
				DefenderUnit unit = slot != null ? slot.OccupiedUnit : null;
				if (unit == null || unit.IsTemporarySummon)
				{
					continue;
				}
				if (unit.CurrentSlot != slot || !slottedUnits.Add(unit))
				{
					Debug.LogWarning("DefenseBoardManager slot invariant warning: occupied slot and defender CurrentSlot are inconsistent.", this);
				}
			}
			// Runtime fallback templates live under this manager but are intentionally inactive and never own a slot. The invariant applies only to live board units.
			DefenderUnit[] defenders = GetComponentsInChildren<DefenderUnit>(includeInactive: false);
			for (int j = 0; j < defenders.Length; j++)
			{
				DefenderUnit defender = defenders[j];
				if (defender != null && !defender.IsTemporarySummon && (defender.CurrentSlot == null || defender.CurrentSlot.OccupiedUnit != defender))
				{
					Debug.LogWarning("DefenseBoardManager slot invariant warning: defender has no matching BoardSlot.", defender);
				}
			}
		}

		private void HandleDragging()
		{
			if (GetGameplayCamera() == null)
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
			if (draggedUnit == null && pendingPointerUnit != null && Input.GetMouseButton(0) && ShouldStartDragFromPress())
			{
				TryBeginDrag(pendingPointerUnit);
				pendingPointerUnit = null;
			}
			if (draggedUnit != null)
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
			pendingPointerUnit = FindUnitUnderPointer();
			pointerDownScreenPosition = Input.mousePosition;
			pointerDownTime = Time.unscaledTime;
			if (pendingPointerUnit == null)
			{
				HideRangeIndicator();
			}
		}

		private bool ShouldStartDragFromPress()
		{
			float heldTime = Time.unscaledTime - pointerDownTime;
			Vector2 currentPosition = Input.mousePosition;
			float sqrDistance = (currentPosition - pointerDownScreenPosition).sqrMagnitude;
			return heldTime >= holdToDragDelay || sqrDistance >= dragStartScreenDistance * dragStartScreenDistance;
		}

		private void CompletePointerPress()
		{
			if (pendingPointerUnit != null)
			{
				ShowRangeIndicator(pendingPointerUnit);
			}
			pendingPointerUnit = null;
		}

		private void TryBeginDrag(DefenderUnit unit)
		{
			if (unit == null)
			{
				return;
			}
			BoardSlot originSlot = unit.CurrentSlot;
			if (originSlot == null || originSlot.OccupiedUnit != unit)
			{
				return;
			}
			CancelActiveDrag();
			draggedUnit = unit;
			draggedOriginSlot = originSlot;
			draggedColliders = draggedUnit.GetComponentsInChildren<Collider>(includeInactive: true);
			draggedColliderEnabledStates = new bool[draggedColliders.Length];
			for (int i = 0; i < draggedColliders.Length; i++)
			{
				Collider collider = draggedColliders[i];
				if (collider != null)
				{
					draggedColliderEnabledStates[i] = collider.enabled;
					collider.enabled = false;
				}
			}
			draggedUnit.transform.SetParent(base.transform, worldPositionStays: true);
			CacheDraggedUnitLocalExtent();
			draggedUnit.SetBoardDragCombatSuspended(true);
			SharedFloatingCombatCanvas.SetPoseRefreshOverride(draggedUnit.transform, true);
			HideRangeIndicator();
			Camera camera = GetGameplayCamera();
			if (!(camera == null))
			{
				Ray ray = camera.ScreenPointToRay(Input.mousePosition);
				if (dragPlane.Raycast(ray, out var enter))
				{
					dragOffset = draggedUnit.transform.position - ray.GetPoint(enter);
				}
				else
				{
					dragOffset = Vector3.zero;
				}
			}
		}

		private void UpdateDragPosition()
		{
			Camera camera = GetGameplayCamera();
			if (!(camera == null))
			{
				Ray ray = camera.ScreenPointToRay(Input.mousePosition);
				if (dragPlane.Raycast(ray, out var enter))
				{
					Vector3 point = ray.GetPoint(enter) + dragOffset;
					point.y = dragHeight;
					draggedUnit.transform.position = ClampDragPointToBoard(point);
				}
			}
		}

		private void RefreshCachedBoardDragBounds()
		{
			cachedBoardDragBounds = default(BoardDragBounds);
			if (slots == null)
			{
				return;
			}
			int validSlotCount = 0;
			float minX = float.PositiveInfinity;
			float maxX = float.NegativeInfinity;
			float minZ = float.PositiveInfinity;
			float maxZ = float.NegativeInfinity;
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot slot = slots[i];
				if (slot == null || slot.UnitAnchor == null)
				{
					continue;
				}
				Vector3 local = base.transform.InverseTransformPoint(slot.UnitAnchor.position);
				if (!IsFinite(local))
				{
					continue;
				}
				validSlotCount++;
				minX = Mathf.Min(minX, local.x);
				maxX = Mathf.Max(maxX, local.x);
				minZ = Mathf.Min(minZ, local.z);
				maxZ = Mathf.Max(maxZ, local.z);
			}
			if (validSlotCount < 2)
			{
				return;
			}

			float spacingX = FindMinimumRowAwareXSpacing();
			float spacingZ = FindMinimumRowSpacing();
			float fallbackSpacing = Mathf.Max(spacingX, spacingZ);
			if (fallbackSpacing <= 0.0001f)
			{
				return;
			}
			if (spacingX <= 0.0001f)
			{
				spacingX = fallbackSpacing;
			}
			if (spacingZ <= 0.0001f)
			{
				spacingZ = fallbackSpacing;
			}
			float paddingRatio = Mathf.Clamp(boardDragPaddingRatio, 0.4f, 0.5f);
			float paddingX = spacingX * paddingRatio;
			float paddingZ = spacingZ * paddingRatio;
			cachedBoardDragBounds = new BoardDragBounds
			{
				isValid = true,
				minX = minX - paddingX,
				maxX = maxX + paddingX,
				minZ = minZ - paddingZ,
				maxZ = maxZ + paddingZ,
				paddingX = paddingX,
				paddingZ = paddingZ,
				slotSpacingX = spacingX,
				slotSpacingZ = spacingZ
			};
		}

		private float FindMinimumRowAwareXSpacing()
		{
			float spacing = float.PositiveInfinity;
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot first = slots[i];
				if (first == null || first.UnitAnchor == null)
				{
					continue;
				}
				Vector3 firstLocal = base.transform.InverseTransformPoint(first.UnitAnchor.position);
				for (int j = i + 1; j < slots.Count; j++)
				{
					BoardSlot second = slots[j];
					if (second == null || second.UnitAnchor == null)
					{
						continue;
					}
					Vector3 secondLocal = base.transform.InverseTransformPoint(second.UnitAnchor.position);
					if (Mathf.Abs(firstLocal.z - secondLocal.z) > BoardRowTolerance)
					{
						continue;
					}
					float delta = Mathf.Abs(firstLocal.x - secondLocal.x);
					if (delta > 0.0001f)
					{
						spacing = Mathf.Min(spacing, delta);
					}
				}
			}
			return float.IsPositiveInfinity(spacing) ? 0f : spacing;
		}

		private float FindMinimumRowSpacing()
		{
			float spacing = float.PositiveInfinity;
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot first = slots[i];
				if (first == null || first.UnitAnchor == null)
				{
					continue;
				}
				float firstZ = base.transform.InverseTransformPoint(first.UnitAnchor.position).z;
				for (int j = i + 1; j < slots.Count; j++)
				{
					BoardSlot second = slots[j];
					if (second == null || second.UnitAnchor == null)
					{
						continue;
					}
					float delta = Mathf.Abs(firstZ - base.transform.InverseTransformPoint(second.UnitAnchor.position).z);
					if (delta > BoardRowTolerance)
					{
						spacing = Mathf.Min(spacing, delta);
					}
				}
			}
			return float.IsPositiveInfinity(spacing) ? 0f : spacing;
		}

		private void CacheDraggedUnitLocalExtent()
		{
			draggedUnitLocalExtent = Vector2.zero;
			if (draggedUnit == null || !cachedBoardDragBounds.isValid)
			{
				return;
			}
			Vector3 localRoot = base.transform.InverseTransformPoint(draggedUnit.transform.position);
			float extentX = 0f;
			float extentZ = 0f;
			Renderer[] renderers = draggedUnit.GetComponentsInChildren<Renderer>(includeInactive: true);
			for (int i = 0; i < renderers.Length; i++)
			{
				if (renderers[i] != null)
				{
					AccumulateLocalExtent(renderers[i].bounds, localRoot, ref extentX, ref extentZ);
				}
			}
			Collider[] colliders = draggedUnit.GetComponentsInChildren<Collider>(includeInactive: true);
			for (int j = 0; j < colliders.Length; j++)
			{
				if (colliders[j] != null)
				{
					AccumulateLocalExtent(colliders[j].bounds, localRoot, ref extentX, ref extentZ);
				}
			}
			draggedUnitLocalExtent = new Vector2(extentX, extentZ);
		}

		private void AccumulateLocalExtent(Bounds bounds, Vector3 localRoot, ref float extentX, ref float extentZ)
		{
			Vector3 min = bounds.min;
			Vector3 max = bounds.max;
			for (int x = 0; x < 2; x++)
			{
				for (int y = 0; y < 2; y++)
				{
					for (int z = 0; z < 2; z++)
					{
						Vector3 corner = new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
						Vector3 localCorner = base.transform.InverseTransformPoint(corner);
						extentX = Mathf.Max(extentX, Mathf.Abs(localCorner.x - localRoot.x));
						extentZ = Mathf.Max(extentZ, Mathf.Abs(localCorner.z - localRoot.z));
					}
				}
			}
		}

		private Vector3 ClampDragPointToBoard(Vector3 worldPoint)
		{
			if (!cachedBoardDragBounds.isValid)
			{
				return worldPoint;
			}
			Vector3 local = base.transform.InverseTransformPoint(worldPoint);
			float appliedInsetX = Mathf.Min(draggedUnitLocalExtent.x, cachedBoardDragBounds.paddingX);
			float appliedInsetZ = Mathf.Min(draggedUnitLocalExtent.y, cachedBoardDragBounds.paddingZ);
			float minX = cachedBoardDragBounds.minX + appliedInsetX;
			float maxX = cachedBoardDragBounds.maxX - appliedInsetX;
			float minZ = cachedBoardDragBounds.minZ + appliedInsetZ;
			float maxZ = cachedBoardDragBounds.maxZ - appliedInsetZ;
			local.x = minX <= maxX ? Mathf.Clamp(local.x, minX, maxX) : (cachedBoardDragBounds.minX + cachedBoardDragBounds.maxX) * 0.5f;
			local.z = minZ <= maxZ ? Mathf.Clamp(local.z, minZ, maxZ) : (cachedBoardDragBounds.minZ + cachedBoardDragBounds.maxZ) * 0.5f;
			Vector3 clampedWorld = base.transform.TransformPoint(local);
			clampedWorld.y = dragHeight;
			return clampedWorld;
		}

		private static bool IsFinite(Vector3 value)
		{
			return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
				!float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
				!float.IsNaN(value.z) && !float.IsInfinity(value.z);
		}

		private void EndDrag()
		{
			if (draggedUnit == null)
			{
				CancelActiveDrag();
				return;
			}
			BoardSlot targetSlot = FindSlotUnderPointer();
			if (targetSlot == null || !TryMoveUnit(draggedUnit, targetSlot))
			{
				CancelActiveDrag();
				return;
			}
			CompleteActiveDrag();
		}

		private void ShowRangeIndicator(DefenderUnit unit)
		{
			if (unit == null || unit.CurrentSlot == null)
			{
				HideRangeIndicator();
				return;
			}
			bool changed = selectedRangeUnit != unit;
			selectedRangeUnit = unit;
			rangeIndicatorDirty |= changed;
			EnsureRangeIndicator();
			rangeIndicatorLine.enabled = true;
			UpdateRangeIndicator();
			if (changed)
			{
				this.OnSelectedUnitChanged?.Invoke(selectedRangeUnit);
			}
		}

		private void HideRangeIndicator()
		{
			bool hadSelection = selectedRangeUnit != null;
			selectedRangeUnit = null;
			if (rangeIndicatorLine != null)
			{
				rangeIndicatorLine.enabled = false;
			}
			rangeIndicatorDirty = true;
			if (hadSelection)
			{
				this.OnSelectedUnitChanged?.Invoke(null);
			}
		}

		private void EnsureRangeIndicator()
		{
			if (!(rangeIndicatorLine != null))
			{
				GameObject indicator = new GameObject("UnitAttackRangeIndicator");
				indicator.transform.SetParent(base.transform, worldPositionStays: false);
				rangeIndicatorLine = indicator.AddComponent<LineRenderer>();
				rangeIndicatorLine.useWorldSpace = true;
				rangeIndicatorLine.loop = true;
				rangeIndicatorLine.positionCount = Mathf.Max(12, rangeIndicatorSegments);
				rangeIndicatorLine.widthMultiplier = Mathf.Max(0.01f, rangeIndicatorLineWidth);
				rangeIndicatorLine.numCapVertices = 4;
				rangeIndicatorLine.numCornerVertices = 4;
				rangeIndicatorLine.material = new Material(Shader.Find("Sprites/Default"));
				rangeIndicatorLine.startColor = rangeIndicatorColor;
				rangeIndicatorLine.endColor = rangeIndicatorColor;
				rangeIndicatorLine.enabled = false;
				rangeIndicatorDirty = true;
			}
		}

		private void UpdateRangeIndicator()
		{
			if (selectedRangeUnit == null || selectedRangeUnit.CurrentSlot == null)
			{
				HideRangeIndicator();
				return;
			}
			EnsureRangeIndicator();
			int segments = Mathf.Max(12, rangeIndicatorSegments);
			float width = Mathf.Max(0.01f, rangeIndicatorLineWidth);
			Vector3 center = selectedRangeUnit.transform.position;
			center.y = selectedRangeUnit.CurrentSlot.transform.position.y + rangeIndicatorHeight;
			float radius = Mathf.Max(0.1f, selectedRangeUnit.CurrentAttackRange);
			if (rangeIndicatorDirty || lastRangeIndicatorSegments != segments || !(Mathf.Abs(lastRangeIndicatorWidth - width) <= 0.0001f) || !(Mathf.Abs(lastRangeIndicatorRadius - radius) <= 0.0001f) || !((lastRangeIndicatorCenter - center).sqrMagnitude <= 1E-06f) || !(lastRangeIndicatorColor == rangeIndicatorColor))
			{
				if (rangeIndicatorLine.positionCount != segments)
				{
					rangeIndicatorLine.positionCount = segments;
				}
				rangeIndicatorLine.widthMultiplier = width;
				rangeIndicatorLine.startColor = rangeIndicatorColor;
				rangeIndicatorLine.endColor = rangeIndicatorColor;
				for (int i = 0; i < segments; i++)
				{
					float angle = MathF.PI * 2f * (float)i / (float)segments;
					Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
					rangeIndicatorLine.SetPosition(i, point);
				}
				lastRangeIndicatorSegments = segments;
				lastRangeIndicatorWidth = width;
				lastRangeIndicatorRadius = radius;
				lastRangeIndicatorCenter = center;
				lastRangeIndicatorColor = rangeIndicatorColor;
				rangeIndicatorDirty = false;
			}
		}

		private BoardSlot FindSlotUnderPointer()
		{
			if (!TryGetPointerPoint(GetSlotPointerPlaneHeight(), out var pointerPoint))
			{
				return null;
			}
			return FindClosestSlot(pointerPoint, slotDropRadius, requireOccupied: false);
		}

		private DefenderUnit FindUnitUnderPointer()
		{
			if (!TryGetPointerPoint(pointerPickupPlaneHeight, out var pointerPoint))
			{
				return null;
			}
			BoardSlot slot = FindClosestSlot(pointerPoint, unitPickupRadius, requireOccupied: true);
			return (slot != null) ? slot.OccupiedUnit : null;
		}

		private BoardSlot FindClosestSlot(Vector3 pointerPoint, float radius, bool requireOccupied)
		{
			BoardSlot closestSlot = null;
			float closestSqrDistance = radius * radius;
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot slot = slots[i];
				if (!(slot == null) && !slot.IsLocked && (!requireOccupied || !slot.IsEmpty))
				{
					Vector3 slotPosition = slot.UnitAnchor.position;
					float dx = pointerPoint.x - slotPosition.x;
					float dz = pointerPoint.z - slotPosition.z;
					float sqrDistance = dx * dx + dz * dz;
					if (!(sqrDistance > closestSqrDistance))
					{
						closestSqrDistance = sqrDistance;
						closestSlot = slot;
					}
				}
			}
			return closestSlot;
		}

		private bool TryGetPointerPoint(float planeHeight, out Vector3 point)
		{
			point = Vector3.zero;
			Camera camera = GetGameplayCamera();
			if (camera == null)
			{
				return false;
			}
			Ray ray = camera.ScreenPointToRay(Input.mousePosition);
			if (!new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f)).Raycast(ray, out var enter))
			{
				return false;
			}
			point = ray.GetPoint(enter);
			return true;
		}

		private Camera GetGameplayCamera()
		{
			if (cachedGameplayCamera == null)
			{
				cachedGameplayCamera = Camera.main;
			}
			return cachedGameplayCamera;
		}

		private static bool IsPointerOverUi()
		{
			return (UnityEngine.Object)(object)EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
		}

		private float GetSlotPointerPlaneHeight()
		{
			for (int i = 0; i < slots.Count; i++)
			{
				BoardSlot slot = slots[i];
				if (slot != null && slot.UnitAnchor != null)
				{
					return slot.UnitAnchor.position.y;
				}
			}
			return 0f;
		}
	}
}
