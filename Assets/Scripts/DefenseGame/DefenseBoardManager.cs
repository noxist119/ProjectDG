using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DefenseGame
{
    public struct UltimateRecipeOption
    {
        public readonly string recipeName;
        public readonly string displayName;
        public readonly string materialSummary;
        public readonly string resultSummary;
        public readonly Color accentColor;
        public readonly bool isReady;
        public readonly int progress;
        public readonly int required;
        public readonly string missingSummary;

        public UltimateRecipeOption(string recipeName, string displayName, string materialSummary, string resultSummary, Color accentColor, bool isReady = true, int progress = 0, int required = 0, string missingSummary = "")
        {
            this.recipeName = recipeName;
            this.displayName = displayName;
            this.materialSummary = materialSummary;
            this.resultSummary = resultSummary;
            this.accentColor = accentColor;
            this.isReady = isReady;
            this.progress = progress;
            this.required = required;
            this.missingSummary = missingSummary ?? string.Empty;
        }
    }

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

        private static readonly UltimateMergeRecipe[] UltimateRecipes =
        {
            new UltimateMergeRecipe("Fever Engine Rite", "Mythic Fighter + Mana Link + Focus Dealer", 0, 0, 0, new[] { "hero_31", "hero_13", "hero_10" }, new[] { "hero_51", "hero_53" }),
            new UltimateMergeRecipe("Volcanic Core Rite", "Mythic Core + Area Damage + Shield Break", 0, 0, 0, new[] { "hero_32", "hero_01", "hero_09" }, new[] { "hero_52" }),
            new UltimateMergeRecipe("Soul Battery Rite", "Drain + Mana + Heal + Guard + Speed", 0, 0, 0, new[] { "hero_11", "hero_13", "hero_02", "hero_05", "hero_14" }, new[] { "hero_54" }),
            new UltimateMergeRecipe("Venom Bulwark Rite", "Mythic Venom + Poison + Freeze Zone", 0, 0, 0, new[] { "hero_33", "hero_04", "hero_22" }, new[] { "hero_52", "hero_54" }),
            new UltimateMergeRecipe("Thunder Control Rite", "Mythic Fighter + Life Chain + Petrify", 0, 0, 0, new[] { "hero_31", "hero_07", "hero_08" }, new[] { "hero_51" }),
            new UltimateMergeRecipe("Iron Bastion Rite", "Mythic Combat + Shield (완화 조합)", 0, 0, 0, new[] { "hero_31", "hero_05" }, new[] { "hero_55" }),
            new UltimateMergeRecipe("Clockwork Barrage Rite", "Mythic Wolf + Battery (완화 조합)", 0, 0, 0, new[] { "hero_32", "hero_13" }, new[] { "hero_56" }),
            new UltimateMergeRecipe("Fractured Arsenal Rite", "Mythic Infection + Assassin (완화 조합)", 0, 0, 0, new[] { "hero_33", "hero_12" }, new[] { "hero_57" }),
            new UltimateMergeRecipe("Crown Overflow Rite", "Mythic 2 + Legendary 1", 2, 1, 0, null, new[] { "hero_51", "hero_52", "hero_53" }),
            new UltimateMergeRecipe("Eclipse Overflow Rite", "Mythic 1 + Legendary 1 + Epic 1", 1, 1, 1, null, new[] { "hero_51", "hero_52", "hero_53", "hero_54", "hero_55", "hero_56", "hero_57" }),
            new UltimateMergeRecipe("Dragon Overflow Rite", "Mythic 2 + Epic 2", 2, 0, 2, null, new[] { "hero_53", "hero_54" })
        };

        [SerializeField] private List<BoardSlot> slots = new List<BoardSlot>();
        [SerializeField] private DefenderUnit fallbackUnitPrefab;
        [SerializeField] private float dragHeight = 1.4f;
        [SerializeField] private float unitPickupRadius = 0.48f;
        [SerializeField] private float slotDropRadius = 0.58f;
        [SerializeField] private float pointerPickupPlaneHeight = 0.75f;
        [SerializeField] private float holdToDragDelay = 0.24f;
        [SerializeField] private float dragStartScreenDistance = 18f;
        [SerializeField] private float rangeIndicatorHeight = 0.16f;
        [SerializeField] private float rangeIndicatorLineWidth = 0.06f;
        [SerializeField] private int rangeIndicatorSegments = 96;
        [SerializeField] private Color rangeIndicatorColor = new Color(0.12f, 1f, 0.86f, 0.95f);
        [SerializeField] private int baseActiveSlotCount = 10;
        [SerializeField] private int frontSlotUnlockInterval = 15;
        [SerializeField] private int maxFrontUnlockCount = 5;

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

        public event System.Action<DefenderUnit> OnSelectedUnitChanged;

        public IReadOnlyList<BoardSlot> Slots => slots;
        public int EmptySlotCount => slots.Count(slot => slot != null && slot.IsAvailable && slot.IsEmpty);
        public int UnitCount => slots.Count(slot => slot != null && slot.OccupiedUnit != null);
        public int UnlockedSlotCount => slots.Count(slot => slot != null && slot.IsAvailable);
        public int LockedSlotCount => slots.Count(slot => slot != null && slot.IsLocked);
        public DefenderUnit SelectedUnit => selectedRangeUnit;
        public string LastMergeFailureReason { get; private set; } = string.Empty;

        private void Awake()
        {
            if (slots.Count == 0)
            {
                slots = GetComponentsInChildren<BoardSlot>(true).ToList();
            }

            dragPlane = new Plane(Vector3.up, new Vector3(0f, dragHeight, 0f));
        }

        private void Update()
        {
            HandleDragging();
            UpdateRangeIndicator();
            recipeMarkerRefreshTimer -= Time.deltaTime;
            if (recipeMarkerRefreshTimer <= 0f)
            {
                recipeMarkerRefreshTimer = 0.20f;
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

        private int ResolveActiveSlotCount(int completedRound)
        {
            int baseCount = Mathf.Clamp(Mathf.Max(0, baseActiveSlotCount), 0, slots.Count);
            int interval = Mathf.Max(1, frontSlotUnlockInterval);
            int upcomingRound = Mathf.Max(1, completedRound + 1);
            int frontUnlocks = Mathf.Clamp(upcomingRound / interval, 0, Mathf.Max(0, maxFrontUnlockCount));
            return Mathf.Clamp(baseCount + frontUnlocks, 0, slots.Count);
        }

        private int ResolveSlotUnlockRound(int slotIndex)
        {
            int extraIndex = Mathf.Max(0, slotIndex - Mathf.Max(0, baseActiveSlotCount));
            return Mathf.Max(1, (extraIndex + 1) * Mathf.Max(1, frontSlotUnlockInterval));
        }

        public void ClearAllDeployedUnits()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                BoardSlot slot = slots[i];
                DefenderUnit unit = slot != null ? slot.OccupiedUnit : null;
                if (unit == null)
                {
                    continue;
                }

                unit.RemoveFromBoard();
                Destroy(unit.gameObject);
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
            return TrySpawnUnit(definition, prefabOverride, out _);
        }

        public bool TrySpawnUnit(CharacterDefinition definition, DefenderUnit prefabOverride, out DefenderUnit spawnedUnit)
        {
            spawnedUnit = null;
            BoardSlot emptySlot = slots.FirstOrDefault(slot => slot != null && slot.IsAvailable && slot.IsEmpty);
            if (emptySlot == null || definition == null)
            {
                return false;
            }

            GameObject sourcePrefab = definition.prefab != null
                ? definition.prefab
                : prefabOverride != null ? prefabOverride.gameObject : fallbackUnitPrefab != null ? fallbackUnitPrefab.gameObject : null;

            if (sourcePrefab == null)
            {
                Debug.LogError("No DefenderUnit prefab assigned.");
                return false;
            }

            GameObject spawnedObject = Instantiate(sourcePrefab, emptySlot.UnitAnchor.position, Quaternion.identity);
            DefenderUnit unit = spawnedObject.GetComponent<DefenderUnit>();
            if (unit == null)
            {
                unit = spawnedObject.AddComponent<DefenderUnit>();
            }

            unit.AdoptRuntimeTemplate(prefabOverride != null ? prefabOverride : fallbackUnitPrefab);
            unit.gameObject.SetActive(true);
            emptySlot.AssignUnit(unit);
            unit.Initialize(definition);
            RuntimeCombatFeedback.ShowGroundPulse(emptySlot.UnitAnchor.position, Color.Lerp(definition.accentColor, Color.white, 0.25f), 0.72f, 0.42f, 0.10f);
            RuntimeGameFeel.PlaySummonArrivalVfx(emptySlot.UnitAnchor.position, definition.accentColor, definition.grade);
            spawnedUnit = unit;
            return true;
        }

        public bool TryMergeUnitsOfGrade(CharacterGrade grade, CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride = null)
        {
            mergeResult = default;
            LastMergeFailureReason = string.Empty;
            if (grade == CharacterGrade.Mythic)
            {
                return TryMergeUltimate(database, out mergeResult, prefabOverride);
            }

            if (grade == CharacterGrade.Transcendent)
            {
                LastMergeFailureReason = "\uCD08\uC6D4 \uB4F1\uAE09\uC740 \uC77C\uBC18 \uD569\uC131\uC744 \uC9C4\uD589\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.";
                return false;
            }

            HashSet<DefenderUnit> reservedRecipeUnits = SelectReservedUltimateRecipeUnits();
            List<DefenderUnit> sameGradeUnits = slots
                .Where(slot => slot != null && !slot.IsEmpty)
                .Select(slot => slot.OccupiedUnit)
                .Where(unit => unit != null && unit.Grade == grade)
                .OrderBy(unit => reservedRecipeUnits.Contains(unit) ? 1 : 0)
                .Take(3)
                .ToList();

            if (sameGradeUnits.Count < 3)
            {
                LastMergeFailureReason = "\uD569\uC131 \uC7AC\uB8CC\uAC00 \uBD80\uC871\uD569\uB2C8\uB2E4.  " + sameGradeUnits.Count + "/3";
                return false;
            }

            CharacterGrade nextGrade = (CharacterGrade)((int)grade + 1);
            // Merging is an in-run progression path. The outgame collection only limits
            // direct summons, never a valid merge result earned from deployed materials.
            CharacterDefinition mergedCharacter = database.GetRandomCharacterByGrade(nextGrade, false);
            if (mergedCharacter == null)
            {
                LastMergeFailureReason = "\uC0C1\uC704 \uB4F1\uAE09 \uACB0\uACFC \uC720\uB2DB \uB370\uC774\uD130\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.";
                return false;
            }

            BoardSlot spawnSlot = sameGradeUnits[0].CurrentSlot;
            if (spawnSlot == null)
            {
                LastMergeFailureReason = "\uD569\uC131 \uACB0\uACFC\uB97C \uBC30\uCE58\uD560 \uC2AC\uB86F\uC744 \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.";
                return false;
            }

            GameObject sourcePrefab = mergedCharacter.prefab != null
                ? mergedCharacter.prefab
                : prefabOverride != null ? prefabOverride.gameObject : fallbackUnitPrefab != null ? fallbackUnitPrefab.gameObject : null;

            if (sourcePrefab == null)
            {
                LastMergeFailureReason = "\uC0C1\uC704 \uB4F1\uAE09 \uC720\uB2DB \uD504\uB9AC\uD339\uC744 \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.";
                Debug.LogError("No DefenderUnit prefab assigned for merge result.");
                return false;
            }

            float inheritedAttackPower = sameGradeUnits.Sum(unit => unit != null ? unit.EffectiveAttackPower : 0f) * 0.98f;
            float inheritedMaxHealth = sameGradeUnits.Sum(unit => unit != null ? unit.MaxHealth : 0f) * 0.94f;

            for (int i = 0; i < sameGradeUnits.Count; i++)
            {
                sameGradeUnits[i].RemoveFromBoard();
                Destroy(sameGradeUnits[i].gameObject);
            }

            GameObject spawnedObject = Instantiate(sourcePrefab, spawnSlot.UnitAnchor.position, Quaternion.identity);
            DefenderUnit unit = spawnedObject.GetComponent<DefenderUnit>();
            if (unit == null)
            {
                unit = spawnedObject.AddComponent<DefenderUnit>();
            }

            unit.AdoptRuntimeTemplate(prefabOverride != null ? prefabOverride : fallbackUnitPrefab);
            unit.gameObject.SetActive(true);
            spawnSlot.AssignUnit(unit);
            unit.Initialize(mergedCharacter);
            unit.ApplyMergeInheritance(inheritedAttackPower, inheritedMaxHealth);
            SpawnMergeVfx(spawnSlot, mergedCharacter.accentColor, nextGrade, false);
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
            return TryFindUltimateRecipe(database, out _);
        }

        public string GetUltimateMergeStatus(CharacterDatabase database = null)
        {
            if (TryFindUltimateRecipe(database, out _))
            {
                return "\uAC00\uB2A5";
            }

            if (HasBlockedReadyUltimateRecipe(database))
            {
                return "\uBCF4\uC720\uC911";
            }

            GetBestUltimateRecipeProgress(out int progress, out int required);
            return "\uC7AC\uB8CC " + progress + "/" + Mathf.Max(1, required);
        }

        public string GetUltimateMergeDetailStatus(CharacterDatabase database)
        {
            UltimateMergeRecipe recipe = GetBestUltimateRecipe(database);
            if (recipe == null)
            {
                return HasBlockedReadyUltimateRecipe(database)
                    ? "\uC774\uBBF8 \uBCF4\uC720\uD55C \uCD08\uC6D4 | \uB2E4\uB978 \uB808\uC2DC\uD53C \uD544\uC694"
                    : "\uCD08\uC6D4 \uB808\uC2DC\uD53C \uC5C6\uC74C";
            }

            int progress = GetUltimateRecipeProgress(recipe);
            int required = Mathf.Max(1, GetUltimateRecipeRequiredCount(recipe));
            string prefix = progress >= required ? "\uCD08\uC6D4 \uC900\uBE44 \uC644\uB8CC" : "\uCD08\uC6D4 " + progress + "/" + required;
            string detail = recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0
                ? BuildRequiredCharacterRecipeStatus(recipe, database)
                : BuildGradeRecipeStatus(recipe);
            return prefix + "  |  " + detail;
        }

        public string GetUltimateMergeActionStatus(CharacterDatabase database)
        {
            UltimateMergeRecipe recipe = GetBestUltimateRecipe(database);
            if (recipe == null)
            {
                return HasBlockedReadyUltimateRecipe(database)
                    ? "\uC774\uBBF8 \uBCF4\uC720\uD55C \uCD08\uC6D4"
                    : "\uCD08\uC6D4 \uB808\uC2DC\uD53C \uC5C6\uC74C";
            }

            int progress = GetUltimateRecipeProgress(recipe);
            int required = Mathf.Max(1, GetUltimateRecipeRequiredCount(recipe));
            if (progress >= required)
            {
                return "\uCD08\uC6D4 \uC870\uD569 \uC2E4\uD589";
            }

            string missing = GetFirstMissingRecipeMaterialName(recipe, database);
            if (!string.IsNullOrWhiteSpace(missing))
            {
                return progress + "/" + required + "  " + missing + " \uCC3E\uAE30";
            }

            return progress + "/" + required + "  \uD575\uC2EC \uC7AC\uB8CC \uBCF4\uC874";
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
                string state = progress >= required ? "완성" : progress + "/" + required;
                string marker = i == 0 ? "TOP " : "    ";
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
                if (recipe == null || !CanSatisfyUltimateRecipe(recipe) || !HasAvailableUltimateResult(database, recipe))
                {
                    continue;
                }

                List<CharacterDefinition> results = ResolveUltimateMergeResultCandidates(database, recipe)
                    .Where(character => !IsBlockedTranscendentResult(character))
                    .ToList();
                string resultSummary = results.Count > 0
                    ? string.Join(" / ", results.Select(character => character.displayName).Distinct().ToArray())
                    : "결과 확인 필요";
                Color accentColor = results.Count > 0
                    ? results[0].accentColor
                    : CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f));
                string materialSummary = recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0
                    ? string.Join(" + ", recipe.requiredCharacterIds.Select(id => ResolveCharacterName(database, id)).ToArray())
                    : BuildGradeRecipeStatus(recipe);
                options.Add(new UltimateRecipeOption(
                    recipe.name,
                    CompactRecipeName(recipe.name),
                    materialSummary,
                    resultSummary,
                    accentColor,
                    true,
                    GetUltimateRecipeRequiredCount(recipe),
                    GetUltimateRecipeRequiredCount(recipe),
                    string.Empty));
            }

            return options.ToArray();
        }

        public UltimateRecipeOption[] GetAllUltimateRecipeOptions(CharacterDatabase database = null)
        {
            List<UltimateRecipeOption> options = new List<UltimateRecipeOption>();
            for (int i = 0; i < UltimateRecipes.Length; i++)
            {
                UltimateMergeRecipe recipe = UltimateRecipes[i];
                if (recipe == null)
                {
                    continue;
                }

                List<CharacterDefinition> results = ResolveUltimateMergeResultCandidates(database, recipe)
                    .Where(character => !IsBlockedTranscendentResult(character))
                    .ToList();
                string resultSummary = results.Count > 0
                    ? string.Join(" / ", results.Select(character => character.displayName).Distinct().ToArray())
                    : "결과 유닛 확인 필요";
                Color accentColor = results.Count > 0
                    ? results[0].accentColor
                    : CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f));
                string materialSummary = recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0
                    ? string.Join(" + ", recipe.requiredCharacterIds.Select(id => ResolveCharacterName(database, id)).ToArray())
                    : BuildGradeRecipeStatus(recipe);
                int progress = GetUltimateRecipeProgress(recipe);
                int required = Mathf.Max(1, GetUltimateRecipeRequiredCount(recipe));
                bool ready = progress >= required && HasAvailableUltimateResult(database, recipe);
                options.Add(new UltimateRecipeOption(
                    recipe.name,
                    CompactRecipeName(recipe.name),
                    materialSummary,
                    resultSummary,
                    accentColor,
                    ready,
                    progress,
                    required,
                    BuildMissingRecipeMaterialSummary(recipe, database)));
            }

            options.Sort((left, right) =>
            {
                if (left.isReady != right.isReady)
                {
                    return left.isReady ? -1 : 1;
                }

                int scaledLeft = left.progress * Mathf.Max(1, right.required);
                int scaledRight = right.progress * Mathf.Max(1, left.required);
                return scaledRight != scaledLeft ? scaledRight.CompareTo(scaledLeft) : left.displayName.CompareTo(right.displayName);
            });
            return options.ToArray();
        }

        private string BuildMissingRecipeMaterialSummary(UltimateMergeRecipe recipe, CharacterDatabase database)
        {
            List<string> missing = new List<string>();
            if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0)
            {
                List<DefenderUnit> candidates = GetRecipeCandidateUnits();
                for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
                {
                    string id = recipe.requiredCharacterIds[i];
                    DefenderUnit match = candidates.FirstOrDefault(unit => unit.Definition.id == id);
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

            return missing.Count > 0 ? string.Join(", ", missing.ToArray()) : "없음";
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
            previewUltimateRecipeName = string.IsNullOrWhiteSpace(recipeName) ? null : recipeName;
            ultimateRecipePreviewActive = previewActive || previewUltimateRecipeName != null;
            RefreshUltimateRecipeMarkers();
        }

        public bool TryMergeUltimateRecipe(string recipeName, CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride)
        {
            mergeResult = default;
            UltimateMergeRecipe recipe = FindUltimateRecipe(recipeName);
            if (database == null || recipe == null || !CanSatisfyUltimateRecipe(recipe) || !HasAvailableUltimateResult(database, recipe))
            {
                return false;
            }

            return ExecuteUltimateMerge(database, recipe, out mergeResult, prefabOverride);
        }

        private bool TryMergeUltimate(CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride)
        {
            mergeResult = default;
            if (database == null || !TryFindUltimateRecipe(database, out UltimateMergeRecipe recipe))
            {
                return false;
            }

            return ExecuteUltimateMerge(database, recipe, out mergeResult, prefabOverride);
        }

        private bool ExecuteUltimateMerge(CharacterDatabase database, UltimateMergeRecipe recipe, out MergeResultInfo mergeResult, DefenderUnit prefabOverride)
        {
            mergeResult = default;
            CharacterDefinition mergedCharacter = ResolveUltimateMergeResult(database, recipe);

            List<DefenderUnit> selectedUnits = SelectUnitsForUltimateRecipe(recipe);
            BoardSlot spawnSlot = selectedUnits.Count > 0 ? selectedUnits[0].CurrentSlot : null;
            if (mergedCharacter == null || spawnSlot == null)
            {
                return false;
            }

            float inheritedAttackPower = selectedUnits.Sum(unit => unit != null ? unit.EffectiveAttackPower : 0f) * 0.90f;
            float inheritedMaxHealth = selectedUnits.Sum(unit => unit != null ? unit.MaxHealth : 0f) * 0.86f;

            for (int i = 0; i < selectedUnits.Count; i++)
            {
                selectedUnits[i].RemoveFromBoard();
                Destroy(selectedUnits[i].gameObject);
            }

            GameObject sourcePrefab = mergedCharacter.prefab != null
                ? mergedCharacter.prefab
                : prefabOverride != null ? prefabOverride.gameObject : fallbackUnitPrefab != null ? fallbackUnitPrefab.gameObject : null;

            if (sourcePrefab == null)
            {
                Debug.LogError("No DefenderUnit prefab assigned for ultimate merge result.");
                return false;
            }

            GameObject spawnedObject = Instantiate(sourcePrefab, spawnSlot.UnitAnchor.position, Quaternion.identity);
            DefenderUnit unit = spawnedObject.GetComponent<DefenderUnit>();
            if (unit == null)
            {
                unit = spawnedObject.AddComponent<DefenderUnit>();
            }

            unit.AdoptRuntimeTemplate(prefabOverride != null ? prefabOverride : fallbackUnitPrefab);
            unit.gameObject.SetActive(true);
            spawnSlot.AssignUnit(unit);
            unit.Initialize(mergedCharacter);
            unit.ApplyMergeInheritance(inheritedAttackPower, inheritedMaxHealth);
            SpawnMergeVfx(spawnSlot, mergedCharacter.accentColor, mergedCharacter.grade, true);
            mergeResult = new MergeResultInfo
            {
                sourceGrade = CharacterGrade.Mythic,
                resultGrade = mergedCharacter.grade,
                recipeName = recipe.name,
                sourceDescription = recipe.displayText,
                consumedUnitCount = selectedUnits.Count,
                isFinalMerge = mergedCharacter.grade == CharacterGrade.Transcendent,
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
                if (recipe != null && string.Equals(recipe.name, recipeName, System.StringComparison.Ordinal))
                {
                    return recipe;
                }
            }

            return null;
        }

        private CharacterDefinition ResolveUltimateMergeResult(CharacterDatabase database, UltimateMergeRecipe recipe)
        {
            List<CharacterDefinition> candidates = ResolveUltimateMergeResultCandidates(database, recipe)
                .Where(character => !IsBlockedTranscendentResult(character))
                .ToList();
            if (candidates.Count > 0)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }

            CharacterDefinition mythicFallback = database.GetRandomCharacterByGrade(CharacterGrade.Mythic, false);
            return mythicFallback != null && !IsBlockedTranscendentResult(mythicFallback) ? mythicFallback : null;
        }

        private List<CharacterDefinition> ResolveUltimateMergeResultCandidates(CharacterDatabase database, UltimateMergeRecipe recipe)
        {
            List<CharacterDefinition> candidates = new List<CharacterDefinition>();
            if (database == null)
            {
                return candidates;
            }

            if (recipe != null && recipe.resultCharacterIds != null && recipe.resultCharacterIds.Length > 0)
            {
                for (int i = 0; i < recipe.resultCharacterIds.Length; i++)
                {
                    CharacterDefinition candidate = database.GetCharacterById(recipe.resultCharacterIds[i]);
                    if (IsDeployableUltimateResult(candidate) && !candidates.Contains(candidate))
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                candidates.AddRange(database.GetCharactersByGrade(CharacterGrade.Transcendent, false));
            }

            return candidates;
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

            return slots.Any(slot => slot != null &&
                slot.OccupiedUnit != null &&
                slot.OccupiedUnit.Definition != null &&
                slot.OccupiedUnit.Definition.id == character.id);
        }

        private bool HasAvailableUltimateResult(CharacterDatabase database, UltimateMergeRecipe recipe)
        {
            if (database == null)
            {
                return true;
            }

            return ResolveUltimateMergeResultCandidates(database, recipe)
                .Any(character => !IsBlockedTranscendentResult(character));
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
            if (TryFindUltimateRecipe(database, out UltimateMergeRecipe readyRecipe))
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
            if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0)
            {
                return TrySelectUnitsByCharacterIds(recipe.requiredCharacterIds, out _);
            }

            return CountUnitsOfGrade(CharacterGrade.Mythic) >= recipe.mythicCount &&
                CountUnitsOfGrade(CharacterGrade.Legendary) >= recipe.legendaryCount &&
                CountUnitsOfGrade(CharacterGrade.Epic) >= recipe.epicCount;
        }

        private List<DefenderUnit> SelectUnitsForUltimateRecipe(UltimateMergeRecipe recipe)
        {
            if (recipe.requiredCharacterIds != null &&
                recipe.requiredCharacterIds.Length > 0 &&
                TrySelectUnitsByCharacterIds(recipe.requiredCharacterIds, out List<DefenderUnit> exactUnits))
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
            if (count <= 0)
            {
                return;
            }

            IEnumerable<DefenderUnit> candidates = slots
                .Where(slot => slot != null && !slot.IsEmpty)
                .Select(slot => slot.OccupiedUnit)
                .Where(unit => unit != null && unit.Grade == grade)
                .Take(count);

            result.AddRange(candidates);
        }

        private bool TrySelectUnitsByCharacterIds(string[] requiredIds, out List<DefenderUnit> selectedUnits)
        {
            selectedUnits = new List<DefenderUnit>();
            List<DefenderUnit> candidates = slots
                .Where(slot => slot != null && !slot.IsEmpty)
                .Select(slot => slot.OccupiedUnit)
                .Where(unit => unit != null && unit.Definition != null)
                .ToList();

            for (int i = 0; i < requiredIds.Length; i++)
            {
                string requiredId = requiredIds[i];
                DefenderUnit match = candidates.FirstOrDefault(unit => unit.Definition.id == requiredId);
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
            if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0)
            {
                for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
                {
                    string requiredId = recipe.requiredCharacterIds[i];
                    DefenderUnit match = candidates.FirstOrDefault(unit => unit.Definition.id == requiredId);
                    if (match == null)
                    {
                        continue;
                    }

                    reservedUnits.Add(match);
                    candidates.Remove(match);
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
                DefenderUnit match = candidates.FirstOrDefault(unit => unit.Definition.id == requiredId);
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
                return leftReady ? -1 : 1;
            }

            int leftRequired = Mathf.Max(1, GetUltimateRecipeRequiredCount(left));
            int rightRequired = Mathf.Max(1, GetUltimateRecipeRequiredCount(right));
            int leftProgress = Mathf.Max(0, GetUltimateRecipeProgress(left));
            int rightProgress = Mathf.Max(0, GetUltimateRecipeProgress(right));
            long leftScaled = (long)leftProgress * rightRequired;
            long rightScaled = (long)rightProgress * leftRequired;
            if (leftScaled != rightScaled)
            {
                return leftScaled > rightScaled ? -1 : 1;
            }

            if (leftProgress != rightProgress)
            {
                return leftProgress > rightProgress ? -1 : 1;
            }

            return leftRequired.CompareTo(rightRequired);
        }

        private string BuildRecipeBingoMaterialStatus(UltimateMergeRecipe recipe, CharacterDatabase database)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0)
            {
                List<DefenderUnit> candidates = GetRecipeCandidateUnits();
                List<string> parts = new List<string>();
                for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
                {
                    string requiredId = recipe.requiredCharacterIds[i];
                    DefenderUnit match = candidates.FirstOrDefault(unit => unit.Definition.id == requiredId);
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
            return gradeParts.Count > 0 ? string.Join("  ", gradeParts) : recipe.displayText;
        }

        private void AddRecipeBingoGradePart(List<string> parts, CharacterGrade grade, int requiredCount)
        {
            if (requiredCount <= 0)
            {
                return;
            }

            int count = Mathf.Min(CountUnitsOfGrade(grade), requiredCount);
            parts.Add((count >= requiredCount ? "[O] " : "[ ] ") + CharacterGradeUtility.GetDisplayName(grade) + " " + count + "/" + requiredCount);
        }

        private static string CompactRecipeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Recipe";
            }

            string compact = value.Replace(" Rite", string.Empty).Trim();
            return compact.Length <= 22 ? compact : compact.Substring(0, 19) + "...";
        }

        private static string CompactRecipeMaterialName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "?";
            }

            string compact = value.Trim();
            return compact.Length <= 7 ? compact : compact.Substring(0, 6) + ".";
        }

        private string GetFirstMissingRecipeMaterialName(UltimateMergeRecipe recipe, CharacterDatabase database)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0)
            {
                List<DefenderUnit> candidates = GetRecipeCandidateUnits();
                for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
                {
                    string requiredId = recipe.requiredCharacterIds[i];
                    DefenderUnit match = candidates.FirstOrDefault(unit => unit.Definition.id == requiredId);
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
            for (int i = 0; i < candidates.Count && remaining > 0; i++)
            {
                DefenderUnit unit = candidates[i];
                if (unit == null || unit.Grade != grade)
                {
                    continue;
                }

                reservedUnits.Add(unit);
                remaining--;
            }
        }

        private void RefreshUltimateRecipeMarkers()
        {
            HashSet<DefenderUnit> reservedUnits = SelectReservedUltimateRecipeUnits();
            Color markerColor = CharacterGradeUtility.GetColor(CharacterGrade.Transcendent, new Color(0.92f, 0.42f, 1f));
            for (int i = 0; i < slots.Count; i++)
            {
                BoardSlot slot = slots[i];
                DefenderUnit unit = slot != null && !slot.IsEmpty ? slot.OccupiedUnit : null;
                if (unit == null)
                {
                    continue;
                }

                bool reserved = reservedUnits.Contains(unit);
                unit.SetRecipeMaterialMarker(reserved, reserved ? "초월 재료" : string.Empty, markerColor);
            }
        }

        private string BuildGradeRecipeStatus(UltimateMergeRecipe recipe)
        {
            List<string> parts = new List<string>();
            AddGradeRecipePart(parts, CharacterGrade.Mythic, recipe.mythicCount);
            AddGradeRecipePart(parts, CharacterGrade.Legendary, recipe.legendaryCount);
            AddGradeRecipePart(parts, CharacterGrade.Epic, recipe.epicCount);
            return parts.Count > 0 ? string.Join(" / ", parts) : recipe.displayText;
        }

        private void AddGradeRecipePart(List<string> parts, CharacterGrade grade, int requiredCount)
        {
            if (requiredCount <= 0)
            {
                return;
            }

            int count = Mathf.Min(CountUnitsOfGrade(grade), requiredCount);
            parts.Add(CharacterGradeUtility.GetDisplayName(grade) + " " + count + "/" + requiredCount);
        }

        private List<DefenderUnit> GetRecipeCandidateUnits()
        {
            return slots
                .Where(slot => slot != null && !slot.IsEmpty)
                .Select(slot => slot.OccupiedUnit)
                .Where(unit => unit != null && unit.Definition != null)
                .ToList();
        }

        private string ResolveCharacterName(CharacterDatabase database, string characterId)
        {
            CharacterDefinition definition = database != null ? database.GetCharacterById(characterId) : null;
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
            long scaledProgress = (long)Mathf.Max(0, progress) * safeBestRequired;
            long scaledBestProgress = (long)Mathf.Max(0, bestProgress) * safeRequired;
            if (scaledProgress != scaledBestProgress)
            {
                return scaledProgress > scaledBestProgress;
            }

            return progress > bestProgress || (progress == bestProgress && safeRequired < safeBestRequired);
        }

        private int GetUltimateRecipeRequiredCount(UltimateMergeRecipe recipe)
        {
            if (recipe.requiredCharacterIds != null && recipe.requiredCharacterIds.Length > 0)
            {
                return recipe.requiredCharacterIds.Length;
            }

            return recipe.mythicCount + recipe.legendaryCount + recipe.epicCount;
        }

        private int GetUltimateRecipeProgress(UltimateMergeRecipe recipe)
        {
            if (recipe.requiredCharacterIds == null || recipe.requiredCharacterIds.Length == 0)
            {
                return Mathf.Min(CountUnitsOfGrade(CharacterGrade.Mythic), recipe.mythicCount) +
                    Mathf.Min(CountUnitsOfGrade(CharacterGrade.Legendary), recipe.legendaryCount) +
                    Mathf.Min(CountUnitsOfGrade(CharacterGrade.Epic), recipe.epicCount);
            }

            List<DefenderUnit> candidates = slots
                .Where(slot => slot != null && !slot.IsEmpty)
                .Select(slot => slot.OccupiedUnit)
                .Where(unit => unit != null && unit.Definition != null)
                .ToList();

            int progress = 0;
            for (int i = 0; i < recipe.requiredCharacterIds.Length; i++)
            {
                string requiredId = recipe.requiredCharacterIds[i];
                DefenderUnit match = candidates.FirstOrDefault(unit => unit.Definition.id == requiredId);
                if (match == null)
                {
                    continue;
                }

                progress++;
                candidates.Remove(match);
            }

            return progress;
        }

        public int CountUnitsOfGrade(CharacterGrade grade)
        {
            return slots.Count(slot => slot != null && slot.OccupiedUnit != null && slot.OccupiedUnit.Grade == grade);
        }

        private void SpawnMergeVfx(BoardSlot slot, Color color, CharacterGrade resultGrade, bool ultimate)
        {
            if (slot == null || slot.UnitAnchor == null)
            {
                return;
            }

            bool transcendentResult = resultGrade == CharacterGrade.Transcendent;
            bool mythicResult = resultGrade == CharacterGrade.Mythic;
            bool jackpot = ultimate || (int)resultGrade >= (int)CharacterGrade.Rare;
            bool major = ultimate || (int)resultGrade >= (int)CharacterGrade.Epic;
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
                RuntimeGameFeel.PlayJackpotPulse(slot.UnitAnchor.position, color, major ? 1.62f : 1.22f, major ? 0.15f : 0.09f, major ? 0.34f : 0.23f, major ? 0.18f : 0.30f, major ? 0.12f : 0.075f, major ? 3 : 2);
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
            return slots.Where(slot => slot != null && !slot.IsEmpty)
                .Select(slot => slot.OccupiedUnit)
                .Where(unit => unit != null)
                .ToArray();
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
            if (unit == null || unit.CurrentSlot == null || unit.IsTemporarySummon)
            {
                return false;
            }

            if (draggedUnit == unit)
            {
                draggedUnit = null;
                draggedOriginSlot = null;
                draggedColliders = null;
                dragOffset = Vector3.zero;
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

            if (sourceSlot == targetSlot)
            {
                if (targetSlot.IsEmpty || targetSlot.OccupiedUnit == unit)
                {
                    targetSlot.AssignUnit(unit);
                    return true;
                }
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

        private void HandleDragging()
        {
            if (Camera.main == null)
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
            if (originSlot == null)
            {
                return;
            }

            draggedUnit = unit;
            draggedOriginSlot = originSlot;
            draggedColliders = draggedUnit.GetComponentsInChildren<Collider>(true);
            draggedOriginSlot.Clear();
            draggedUnit.transform.SetParent(transform, true);
            HideRangeIndicator();

            for (int i = 0; i < draggedColliders.Length; i++)
            {
                draggedColliders[i].enabled = false;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                dragOffset = draggedUnit.transform.position - ray.GetPoint(enter);
            }
            else
            {
                dragOffset = Vector3.zero;
            }
        }

        private void UpdateDragPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!dragPlane.Raycast(ray, out float enter))
            {
                return;
            }

            Vector3 point = ray.GetPoint(enter) + dragOffset;
            point.y = dragHeight;
            draggedUnit.transform.position = point;
        }

        private void EndDrag()
        {
            BoardSlot targetSlot = FindSlotUnderPointer();

            for (int i = 0; i < draggedColliders.Length; i++)
            {
                if (draggedColliders[i] != null)
                {
                    draggedColliders[i].enabled = true;
                }
            }

            bool moved = targetSlot != null && TryMoveUnit(draggedUnit, targetSlot);
            if (!moved && draggedOriginSlot != null)
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
            if (unit == null || unit.CurrentSlot == null)
            {
                HideRangeIndicator();
                return;
            }

            bool changed = selectedRangeUnit != unit;
            selectedRangeUnit = unit;
            EnsureRangeIndicator();
            rangeIndicatorLine.enabled = true;
            UpdateRangeIndicator();
            if (changed)
            {
                OnSelectedUnitChanged?.Invoke(selectedRangeUnit);
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

            if (hadSelection)
            {
                OnSelectedUnitChanged?.Invoke(null);
            }
        }

        private void EnsureRangeIndicator()
        {
            if (rangeIndicatorLine != null)
            {
                return;
            }

            GameObject indicator = new GameObject("UnitAttackRangeIndicator");
            indicator.transform.SetParent(transform, false);
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
            if (rangeIndicatorLine.positionCount != segments)
            {
                rangeIndicatorLine.positionCount = segments;
            }

            rangeIndicatorLine.widthMultiplier = Mathf.Max(0.01f, rangeIndicatorLineWidth);
            rangeIndicatorLine.startColor = rangeIndicatorColor;
            rangeIndicatorLine.endColor = rangeIndicatorColor;

            Vector3 center = selectedRangeUnit.transform.position;
            center.y = selectedRangeUnit.CurrentSlot.transform.position.y + rangeIndicatorHeight;
            float radius = Mathf.Max(0.1f, selectedRangeUnit.CurrentAttackRange);
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                rangeIndicatorLine.SetPosition(i, point);
            }
        }

        private BoardSlot FindSlotUnderPointer()
        {
            if (!TryGetPointerPoint(GetSlotPointerPlaneHeight(), out Vector3 pointerPoint))
            {
                return null;
            }

            return FindClosestSlot(pointerPoint, slotDropRadius, false);
        }

        private DefenderUnit FindUnitUnderPointer()
        {
            if (!TryGetPointerPoint(pointerPickupPlaneHeight, out Vector3 pointerPoint))
            {
                return null;
            }

            BoardSlot slot = FindClosestSlot(pointerPoint, unitPickupRadius, true);
            return slot != null ? slot.OccupiedUnit : null;
        }

        private BoardSlot FindClosestSlot(Vector3 pointerPoint, float radius, bool requireOccupied)
        {
            BoardSlot closestSlot = null;
            float closestSqrDistance = radius * radius;

            for (int i = 0; i < slots.Count; i++)
            {
                BoardSlot slot = slots[i];
                if (slot == null || slot.IsLocked || (requireOccupied && slot.IsEmpty))
                {
                    continue;
                }
                Vector3 slotPosition = slot.UnitAnchor.position;
                float dx = pointerPoint.x - slotPosition.x;
                float dz = pointerPoint.z - slotPosition.z;
                float sqrDistance = dx * dx + dz * dz;
                if (sqrDistance > closestSqrDistance)
                {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                closestSlot = slot;
            }

            return closestSlot;
        }

        private bool TryGetPointerPoint(float planeHeight, out Vector3 point)
        {
            point = Vector3.zero;
            if (Camera.main == null)
            {
                return false;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane pointerPlane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
            if (!pointerPlane.Raycast(ray, out float enter))
            {
                return false;
            }

            point = ray.GetPoint(enter);
            return true;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
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
