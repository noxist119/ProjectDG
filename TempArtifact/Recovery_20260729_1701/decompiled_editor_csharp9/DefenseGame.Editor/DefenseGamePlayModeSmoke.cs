using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame.Editor
{
	public static class DefenseGamePlayModeSmoke
	{
		[Serializable]
		private sealed class SmokeReport
		{
			public string status;

			public bool passed;

			public bool safeAreaExists;

			public bool safeAreaAnchorsValid;

			public bool portraitProfilesValid;

			public bool hpTen;

			public bool hpTextTen;

			public bool fateEntryLayoutValid;

			public bool fateEntryPastelColorValid;

			public bool fateEntryIdleAtFullHealth;

			public bool summonHudReadable;

			public bool initialPreparationFlowValid;

			public bool resultRewardIconsValid;

			public bool rankingPageValid;

			public bool earlyMiniShopChoicesValid;

			public string earlyMiniShopSummary;

			public bool simultaneousDeathPolicyValid;

			public bool hero32SignatureValid;

			public bool gargoyleLoopDurationValid;

			public bool longCombatAccelerationValid;

			public bool defaultVfxConfigured;

			public bool animationMaterialEventsValid;

			public string animationMaterialEventsSummary;

			public int runtimeErrors;

			public PrefabSmokeResult[] prefabs = Array.Empty<PrefabSmokeResult>();

			public string[] notes = Array.Empty<string>();
		}

		[Serializable]
		private sealed class PrefabSmokeResult
		{
			public string heroId;

			public string prefabPath;

			public bool passed;

			public int missingScripts;

			public int rendererCount;

			public string animatorController;

			public string[] clipNames = Array.Empty<string>();

			public string[] eventKeys = Array.Empty<string>();

			public bool presentationBound;

			public bool combatVisualBound;

			public string failureReason;

			public static PrefabSmokeResult Failure(string heroId, string prefabPath, string reason)
			{
				return new PrefabSmokeResult
				{
					heroId = heroId,
					prefabPath = prefabPath,
					passed = false,
					failureReason = reason
				};
			}
		}

		private const string ScenePath = "Assets/Scenes/DG.unity";

		private const string OutputDirectoryName = "BatchPlaytestResults";

		private const string OutputFileName = "DefenseGame_PlayModeSmoke.json";

		private static readonly string[] PrefabPaths = new string[3] { "Assets/Prefabs/Minimi/Dice_armor.prefab", "Assets/Prefabs/Minimi/dice_auto.prefab", "Assets/Prefabs/Minimi/Dice_Broken.prefab" };

		private static readonly string[] HeroIds = new string[3] { "hero_55", "hero_56", "hero_57" };

		private static double evaluateAt;

		private static int runtimeErrors;

		private static bool running;

		private static bool previousEnterPlayModeOptionsEnabled;

		private static EnterPlayModeOptions previousEnterPlayModeOptions;

		private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BatchPlaytestResults", "DefenseGame_PlayModeSmoke.json"));

		[MenuItem("DefenseGame/Smoke Tests/Vertical UI and New Units")]
		public static void RunPlayModeSmoke()
		{
			if (!running)
			{
				running = true;
				runtimeErrors = 0;
				Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? string.Empty);
				if (File.Exists(OutputPath))
				{
					File.Delete(OutputPath);
				}
				previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
				previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
				EditorSettings.enterPlayModeOptionsEnabled = true;
				EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
				Application.logMessageReceived -= HandleLogMessage;
				Application.logMessageReceived += HandleLogMessage;
				EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
				EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
				EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
				EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
				EditorSceneManager.OpenScene("Assets/Scenes/DG.unity");
				EditorApplication.isPlaying = true;
			}
		}

		private static void HandlePlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				evaluateAt = EditorApplication.timeSinceStartup + 2.5;
			}
		}

		private static void Tick()
		{
			if (running && EditorApplication.isPlaying && !(EditorApplication.timeSinceStartup < evaluateAt))
			{
				SmokeReport report;
				try
				{
					report = Evaluate();
				}
				catch (Exception ex)
				{
					SmokeReport smokeReport = new SmokeReport();
					smokeReport.status = "exception";
					smokeReport.passed = false;
					smokeReport.runtimeErrors = runtimeErrors + 1;
					smokeReport.notes = new string[1] { ex.ToString() };
					report = smokeReport;
				}
				File.WriteAllText(OutputPath, JsonUtility.ToJson(report, prettyPrint: true));
				Finish((!report.passed) ? 1 : 0);
			}
		}

		private static SmokeReport Evaluate()
		{
			//IL_0e7b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0e82: Invalid comparison between Unknown and I4
			//IL_0f28: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f2f: Invalid comparison between Unknown and I4
			//IL_0f46: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f4c: Unknown result type (might be due to invalid IL or missing references)
			List<string> notes = new List<string>();
			RuntimeSafeAreaFitter safeAreaFitter = UnityEngine.Object.FindObjectsOfType<RuntimeSafeAreaFitter>(true).FirstOrDefault();
			GameObject safeRoot = (((UnityEngine.Object)(object)safeAreaFitter != null) ? ((Component)(object)safeAreaFitter).gameObject : null);
			RectTransform safeRect = ((safeRoot != null) ? safeRoot.GetComponent<RectTransform>() : null);
			bool safeAreaExists = safeRect != null && (UnityEngine.Object)(object)safeAreaFitter != null;
			bool safeAreaAnchorsValid = safeRect != null && Approximately(safeRect.anchorMin, Vector2.zero) && Approximately(safeRect.anchorMax, Vector2.one) && safeRect.rect.width > 0f && safeRect.rect.height > 0f;
			if (!safeAreaExists || !safeAreaAnchorsValid)
			{
				notes.Add("SafeAreaRoot 또는 RuntimeSafeAreaFitter/anchor가 유효하지 않습니다.");
			}
			bool portraitProfilesValid = ValidatePortraitSafeAreaProfiles();
			if (!portraitProfilesValid)
			{
				notes.Add("세로 실기기 Safe Area 프로필의 정규화 anchor 검증에 실패했습니다.");
			}
			DefenseGameController controller = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
			bool hpTen = (UnityEngine.Object)(object)controller != null && controller.Life == 10 && controller.MaxLife == 10;
			bool simultaneousDeathPolicyValid = ValidateSimultaneousDeathPolicy();
			if (!simultaneousDeathPolicyValid)
			{
				notes.Add("동시사망 승리 우선 정책 회귀 검증에 실패했습니다.");
			}
			Text hpText = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault((Text text) => (UnityEngine.Object)(object)text != null && ((UnityEngine.Object)(object)text).name == "TopHpText");
			bool hpTextTen = (UnityEngine.Object)(object)hpText != null && hpText.text.Contains("10/10");
			if (!hpTen || !hpTextTen)
			{
				notes.Add("플레이어 HP 10/10 런타임 표시가 일치하지 않습니다.");
			}
			Button fateEntryButton = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "FatePanelReopenButton");
			RectTransform fateEntryRect = (((UnityEngine.Object)(object)fateEntryButton != null) ? ((Component)(object)fateEntryButton).GetComponent<RectTransform>() : null);
			Shadow fateEntryShadow = (((UnityEngine.Object)(object)fateEntryButton != null) ? ((Component)(object)fateEntryButton).GetComponents<Shadow>().FirstOrDefault((Shadow effect) => !(effect is Outline)) : null);
			Outline fateEntryOutline = (((UnityEngine.Object)(object)fateEntryButton != null) ? ((Component)(object)fateEntryButton).GetComponent<Outline>() : null);
			Graphic fateEntryGraphic = (((UnityEngine.Object)(object)fateEntryButton != null && (UnityEngine.Object)(object)((Selectable)fateEntryButton).targetGraphic != null) ? ((Selectable)fateEntryButton).targetGraphic : (((UnityEngine.Object)(object)fateEntryButton != null) ? ((Component)(object)fateEntryButton).GetComponent<Graphic>() : null));
			Text fateEntryText = (((UnityEngine.Object)(object)fateEntryButton != null) ? ((Component)(object)fateEntryButton).GetComponentInChildren<Text>(true) : null);
			bool fateEntryLayoutValid = fateEntryRect != null && Approximately(fateEntryRect.sizeDelta, new Vector2(250f, 84f)) && Approximately(fateEntryRect.anchoredPosition, new Vector2(-80f, 356f)) && (UnityEngine.Object)(object)fateEntryShadow != null && Approximately(fateEntryShadow.effectDistance, new Vector2(0f, -4f)) && fateEntryShadow.useGraphicAlpha && (UnityEngine.Object)(object)fateEntryOutline != null && Approximately(((Shadow)fateEntryOutline).effectDistance, new Vector2(2f, -2f)) && ((Shadow)fateEntryOutline).useGraphicAlpha;
			bool fateEntryPastelColorValid = (UnityEngine.Object)(object)fateEntryGraphic != null && Approximately(fateEntryGraphic.color, new Color(0.4f, 0.21f, 0.85f, 0.98f)) && (UnityEngine.Object)(object)fateEntryText != null && (UnityEngine.Object)(object)fateEntryOutline != null && Approximately(((Graphic)fateEntryText).color, new Color(1f, 0.98f, 0.94f, 1f)) && Approximately(((Shadow)fateEntryOutline).effectColor, new Color(1f, 0.78f, 0.34f, 0.94f));
			bool fateEntryIdleAtFullHealth = (UnityEngine.Object)(object)controller != null && controller.Life > 3 && !controller.FateSurvivalCrisisActive;
			if (!fateEntryLayoutValid || !fateEntryPastelColorValid || !fateEntryIdleAtFullHealth)
			{
				string actualBackground = (((UnityEngine.Object)(object)fateEntryGraphic != null) ? fateEntryGraphic.color.ToString() : "null");
				string actualText = (((UnityEngine.Object)(object)fateEntryText != null) ? ((Graphic)fateEntryText).color.ToString() : "null");
				notes.Add("운명카드 버튼의 하단 HUD 정렬, 와인/골드 팔레트 또는 HP 3 초과 정지 상태가 유효하지 않습니다. background=" + actualBackground + ", text=" + actualText);
			}
			Button summonHudButton = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "SummonButton");
			Text summonCostHudText = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault((Text text) => (UnityEngine.Object)(object)text != null && ((UnityEngine.Object)(object)text).name == "SummonCostText");
			Text luckySummonHudText = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault((Text text) => (UnityEngine.Object)(object)text != null && ((UnityEngine.Object)(object)text).name == "LuckySummonProgressText");
			Image luckySummonBadge = UnityEngine.Object.FindObjectsOfType<Image>(true).FirstOrDefault((Image image) => (UnityEngine.Object)(object)image != null && ((UnityEngine.Object)(object)image).name == "LuckySummonProgressBadge");
			RectTransform summonHudRect = (((UnityEngine.Object)(object)summonHudButton != null) ? ((Component)(object)summonHudButton).GetComponent<RectTransform>() : null);
			object obj;
			if (!((UnityEngine.Object)(object)summonHudButton != null))
			{
				obj = null;
			}
			else
			{
				Graphic targetGraphic = ((Selectable)summonHudButton).targetGraphic;
				obj = ((targetGraphic is Image) ? targetGraphic : null);
			}
			Image summonHudImage = (Image)obj;
			bool summonHudReadable = summonHudRect != null && Approximately(summonHudRect.sizeDelta, new Vector2(226f, 88f)) && (UnityEngine.Object)(object)summonCostHudText != null && summonCostHudText.text.EndsWith(" GOLD", StringComparison.Ordinal) && summonCostHudText.fontSize >= 18 && (UnityEngine.Object)(object)luckySummonHudText != null && luckySummonHudText.fontSize >= 17 && (UnityEngine.Object)(object)luckySummonBadge != null && ((Component)(object)luckySummonHudText).transform.parent == ((Component)(object)luckySummonBadge).transform && (UnityEngine.Object)(object)summonHudImage != null && summonHudImage.sprite != null && !summonHudImage.sprite.name.StartsWith("RuntimeRoundedPanel", StringComparison.Ordinal);
			if (!summonHudReadable)
			{
				notes.Add("소환 버튼의 행운 진행도 배지, GOLD 비용 표기 또는 Assets/Art/Ui 버튼 스프라이트 우선 적용이 유효하지 않습니다.");
			}
			Button lobbyEntryButton = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "LobbyBattleButton");
			bool initialPreparationFlowValid = (UnityEngine.Object)(object)controller != null && controller.CurrentRound <= 0 && !controller.IsRoundRunning && (UnityEngine.Object)(object)lobbyEntryButton != null;
			if (initialPreparationFlowValid)
			{
				((UnityEvent)(object)lobbyEntryButton.onClick).Invoke();
				initialPreparationFlowValid = controller.CurrentRound <= 0 && !controller.IsRoundRunning;
			}
			if (!initialPreparationFlowValid)
			{
				notes.Add("전장 입장 후 다음 라운드를 누르기 전까지 R1 카운트다운이 대기하지 않습니다.");
			}
			Button lobbyShopButton = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "LobbyShopButton");
			bool outgameShopValid = (UnityEngine.Object)(object)lobbyShopButton != null;
			if (outgameShopValid)
			{
				((UnityEvent)(object)lobbyShopButton.onClick).Invoke();
				RectTransform shopModal = UnityEngine.Object.FindObjectsOfType<RectTransform>(includeInactive: true).FirstOrDefault((RectTransform rect) => rect != null && rect.name == "ShopModal");
				int dailyCards = UnityEngine.Object.FindObjectsOfType<Button>(true).Count((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name.StartsWith("DailyOfferCard_", StringComparison.Ordinal));
				int cashCards = UnityEngine.Object.FindObjectsOfType<Button>(true).Count((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name.StartsWith("CashBundleCard_", StringComparison.Ordinal));
				string[] chestButtonNames = new string[4] { "FiveDrawCard", "TwentyDrawCard", "FiftyDrawCard", "HundredDrawCard" };
				int chestCards = chestButtonNames.Count((string name) => UnityEngine.Object.FindObjectsOfType<Button>(true).Any((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == name));
				Text shopGold = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault((Text text) => (UnityEngine.Object)(object)text != null && ((UnityEngine.Object)(object)text).name == "ShopGoldText");
				int productIcons = UnityEngine.Object.FindObjectsOfType<Image>(true).Count((Image image) => (UnityEngine.Object)(object)image != null && ((UnityEngine.Object)(object)image).name == "ShopProductIcon" && image.sprite != null);
				string[] sectionIconNames = new string[5] { "CashSectionIcon", "DailySectionIcon", "ChestSectionIcon", "HeaderGoldIcon", "HeaderDiamondIcon" };
				int sectionIcons = sectionIconNames.Count((string name) => UnityEngine.Object.FindObjectsOfType<Image>(true).Any((Image image) => (UnityEngine.Object)(object)image != null && ((UnityEngine.Object)(object)image).name == name && image.sprite != null));
				bool decorativeShopArtValid = productIcons == 10 && sectionIcons == sectionIconNames.Length;
				Button firstCashProduct = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "CashBundleCard_0");
				GameObject purchaseConfirmOverlay = (from rect in UnityEngine.Object.FindObjectsOfType<RectTransform>(includeInactive: true)
					where rect != null && rect.name == "ShopPurchaseConfirmOverlay"
					select rect.gameObject).FirstOrDefault();
				Button purchaseCancelButton = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "ShopPurchaseConfirmCancelButton");
				bool purchaseConfirmationValid = (UnityEngine.Object)(object)firstCashProduct != null && purchaseConfirmOverlay != null && !purchaseConfirmOverlay.activeSelf;
				if (purchaseConfirmationValid)
				{
					((UnityEvent)(object)firstCashProduct.onClick).Invoke();
					purchaseConfirmationValid = purchaseConfirmOverlay.activeSelf && (UnityEngine.Object)(object)purchaseCancelButton != null;
					if (purchaseCancelButton != null)
					{
						((UnityEvent)(object)purchaseCancelButton.onClick).Invoke();
					}
					purchaseConfirmationValid &= !purchaseConfirmOverlay.activeSelf;
				}
				outgameShopValid = shopModal != null && Approximately(shopModal.sizeDelta, new Vector2(920f, 1640f)) && dailyCards == 3 && cashCards == 3 && chestCards == 4 && (UnityEngine.Object)(object)shopGold != null && shopGold.text.Contains("GOLD") && decorativeShopArtValid && purchaseConfirmationValid;
				Button shopClose = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "ShopCloseButton");
				if (shopClose != null)
				{
					((UnityEvent)(object)shopClose.onClick).Invoke();
				}
			}
			if (!outgameShopValid)
			{
				notes.Add("로비 상점의 현금 꾸러미 3개, 일일 상품 3개, 상자 5/20/50/100개 또는 GOLD/DIA 표시가 유효하지 않습니다.");
			}
			Image resultGoldIcon = UnityEngine.Object.FindObjectsOfType<Image>(true).FirstOrDefault((Image image) => (UnityEngine.Object)(object)image != null && ((UnityEngine.Object)(object)image).name == "ResultRewardGoldIcon");
			Image resultDiamondIcon = UnityEngine.Object.FindObjectsOfType<Image>(true).FirstOrDefault((Image image) => (UnityEngine.Object)(object)image != null && ((UnityEngine.Object)(object)image).name == "ResultRewardDiamondIcon");
			bool resultRewardIconsValid = (UnityEngine.Object)(object)resultGoldIcon != null && resultGoldIcon.sprite != null && (UnityEngine.Object)(object)resultDiamondIcon != null && resultDiamondIcon.sprite != null;
			if (!resultRewardIconsValid)
			{
				notes.Add("승리 결과 보상 칩에 골드 또는 다이아 아이콘이 연결되지 않았습니다.");
			}
			Button rankingButton = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "OutgameNavRanking");
			bool rankingPageValid = (UnityEngine.Object)(object)rankingButton != null;
			if (rankingPageValid)
			{
				((UnityEvent)(object)rankingButton.onClick).Invoke();
				RectTransform rankingOverlay = UnityEngine.Object.FindObjectsOfType<RectTransform>(includeInactive: true).FirstOrDefault((RectTransform rect) => rect != null && rect.name == "SeasonRankingOverlay");
				RectTransform rankingModal = UnityEngine.Object.FindObjectsOfType<RectTransform>(includeInactive: true).FirstOrDefault((RectTransform rect) => rect != null && rect.name == "SeasonRankingModal");
				int topCards = UnityEngine.Object.FindObjectsOfType<Image>(true).Count((Image image) => (UnityEngine.Object)(object)image != null && ((UnityEngine.Object)(object)image).name.StartsWith("RankingTopCard_", StringComparison.Ordinal));
				int rankingRows = UnityEngine.Object.FindObjectsOfType<Image>(true).Count((Image image) => (UnityEngine.Object)(object)image != null && ((UnityEngine.Object)(object)image).name.StartsWith("RankingRow_", StringComparison.Ordinal));
				Image rankingBackdrop = UnityEngine.Object.FindObjectsOfType<Image>(true).FirstOrDefault((Image image) => (UnityEngine.Object)(object)image != null && ((UnityEngine.Object)(object)image).name == "RankingAmbientBackdrop");
				Text rankingPlayerSummary = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault((Text textComponent) => (UnityEngine.Object)(object)textComponent != null && ((UnityEngine.Object)(object)textComponent).name == "RankingPlayerSummary");
				rankingPageValid = rankingOverlay != null && rankingOverlay.gameObject.activeSelf && rankingModal != null && Approximately(rankingModal.sizeDelta, new Vector2(920f, 1660f)) && topCards == 3 && rankingRows == 9 && (UnityEngine.Object)(object)rankingBackdrop != null && rankingBackdrop.sprite != null && (UnityEngine.Object)(object)rankingPlayerSummary != null && rankingPlayerSummary.text.Contains("내 순위");
				Button rankingClose = UnityEngine.Object.FindObjectsOfType<Button>(true).FirstOrDefault((Button button) => (UnityEngine.Object)(object)button != null && ((UnityEngine.Object)(object)button).name == "RankingCloseButton");
				if (rankingClose != null)
				{
					((UnityEvent)(object)rankingClose.onClick).Invoke();
				}
			}
			if (!rankingPageValid)
			{
				notes.Add("시즌 랭킹의 상위 3명 포디움, 4~12위 리스트, 내 순위 강조 또는 전용 아트가 유효하지 않습니다.");
			}
			string earlyMiniShopSummary;
			bool earlyMiniShopChoicesValid = ValidateRoundTieredMiniShop(out earlyMiniShopSummary);
			if (!earlyMiniShopChoicesValid)
			{
				notes.Add("R3 소형 전투상점의 3개 선택지 분류/가격 검증에 실패했습니다. " + earlyMiniShopSummary);
			}
			GamePresentationConfig presentation = AssetDatabase.LoadAssetAtPath<GamePresentationConfig>("Assets/Data/DefenseGamePresentationConfig.asset");
			bool defaultVfxConfigured = (UnityEngine.Object)(object)presentation != null && presentation.projectilePrefab != null && presentation.defaultMuzzleEffectPrefab != null && presentation.defaultHitEffectPrefab != null && presentation.defaultAreaEffectPrefab != null;
			if (!defaultVfxConfigured)
			{
				notes.Add("DefenseGamePresentationConfig 기본 투사체/머즐/히트/범위 VFX 중 빈 참조가 있습니다.");
			}
			string animationMaterialEventsSummary;
			bool animationMaterialEventsValid = ValidateAnimationMaterialEvents(out animationMaterialEventsSummary);
			if (!animationMaterialEventsValid)
			{
				notes.Add("OverrideMaterial/ResetMaterial 애니메이션 이벤트의 적용·원본 복구 검증에 실패했습니다. " + animationMaterialEventsSummary);
			}
			CharacterDatabase database = UnityEngine.Object.FindObjectOfType<CharacterDatabase>();
			CharacterDefinition hero32 = (((UnityEngine.Object)(object)database != null) ? database.GetCharacterById("hero_32") : null);
			SkillDefinition hero32Skill = ((hero32 != null && hero32.skills != null && hero32.skills.Count > 0) ? hero32.skills[0] : null);
			bool hero32SignatureValid = hero32Skill != null && (int)hero32Skill.effectType == 32 && Mathf.Approximately(hero32Skill.power, 2.2f) && Mathf.Approximately(hero32Skill.secondaryPower, 0.35f) && Mathf.Approximately(hero32Skill.duration, 4f);
			if (!hero32SignatureValid)
			{
				notes.Add("hero_32 야성의 추적탄 프리셋이 확정 수치와 일치하지 않습니다.");
			}
			CharacterDefinition hero54 = (((UnityEngine.Object)(object)database != null) ? database.GetCharacterById("hero_54") : null);
			SkillDefinition hero54Skill = ((hero54 != null && hero54.skills != null && hero54.skills.Count > 0) ? hero54.skills[0] : null);
			bool gargoyleLoopDurationValid = hero54Skill != null && (int)hero54Skill.effectType == 25 && Mathf.Approximately(hero54Skill.duration, 5f) && (hero54Skill.growthTargets & 4) != 0 && Mathf.Approximately(UnitAnimationDriver.ResolveSkill03LoopHoldDuration(hero54Skill.duration, 0.35f), 5f) && Mathf.Approximately(UnitAnimationDriver.ResolveSkill03LoopHoldDuration(6.5f, 0.35f), 6.5f);
			if (!gargoyleLoopDurationValid)
			{
				notes.Add("Dice Gargoyle의 5초 Skill03_Loop 또는 아웃게임 지속시간 성장 연결이 유효하지 않습니다.");
			}
			bool longCombatAccelerationValid = RoundManager.ResolveCombatTimeScaleMultiplier(29.99f, 30f, 5f, 10) == 1 && RoundManager.ResolveCombatTimeScaleMultiplier(30f, 30f, 5f, 10) == 2 && RoundManager.ResolveCombatTimeScaleMultiplier(35f, 30f, 5f, 10) == 3 && RoundManager.ResolveCombatTimeScaleMultiplier(45f, 30f, 5f, 10) == 5 && RoundManager.ResolveCombatTimeScaleMultiplier(90f, 30f, 5f, 10) == 10;
			if (!longCombatAccelerationValid)
			{
				notes.Add("장기전 가속 단계가 30초 2배, 이후 5초마다 1배 증가 규칙과 일치하지 않습니다.");
			}
			PrefabSmokeResult[] prefabResults = new PrefabSmokeResult[PrefabPaths.Length];
			for (int i = 0; i < PrefabPaths.Length; i++)
			{
				prefabResults[i] = EvaluatePrefab(PrefabPaths[i], HeroIds[i], database, presentation, i);
				if (!prefabResults[i].passed)
				{
					notes.Add(HeroIds[i] + " prefab smoke failed: " + prefabResults[i].failureReason);
				}
			}
			bool passed = safeAreaExists && safeAreaAnchorsValid && portraitProfilesValid && hpTen && hpTextTen && simultaneousDeathPolicyValid && fateEntryLayoutValid && fateEntryPastelColorValid && fateEntryIdleAtFullHealth && summonHudReadable && initialPreparationFlowValid && outgameShopValid && resultRewardIconsValid && rankingPageValid && earlyMiniShopChoicesValid && hero32SignatureValid && gargoyleLoopDurationValid && longCombatAccelerationValid && defaultVfxConfigured && animationMaterialEventsValid && runtimeErrors == 0;
			for (int i2 = 0; i2 < prefabResults.Length; i2++)
			{
				passed &= prefabResults[i2].passed;
			}
			return new SmokeReport
			{
				status = (passed ? "pass" : "fail"),
				passed = passed,
				safeAreaExists = safeAreaExists,
				safeAreaAnchorsValid = safeAreaAnchorsValid,
				portraitProfilesValid = portraitProfilesValid,
				hpTen = hpTen,
				hpTextTen = hpTextTen,
				fateEntryLayoutValid = fateEntryLayoutValid,
				fateEntryPastelColorValid = fateEntryPastelColorValid,
				fateEntryIdleAtFullHealth = fateEntryIdleAtFullHealth,
				summonHudReadable = summonHudReadable,
				initialPreparationFlowValid = initialPreparationFlowValid,
				resultRewardIconsValid = resultRewardIconsValid,
				rankingPageValid = rankingPageValid,
				earlyMiniShopChoicesValid = earlyMiniShopChoicesValid,
				earlyMiniShopSummary = earlyMiniShopSummary,
				simultaneousDeathPolicyValid = simultaneousDeathPolicyValid,
				hero32SignatureValid = hero32SignatureValid,
				gargoyleLoopDurationValid = gargoyleLoopDurationValid,
				longCombatAccelerationValid = longCombatAccelerationValid,
				defaultVfxConfigured = defaultVfxConfigured,
				animationMaterialEventsValid = animationMaterialEventsValid,
				animationMaterialEventsSummary = animationMaterialEventsSummary,
				runtimeErrors = runtimeErrors,
				prefabs = prefabResults,
				notes = notes.ToArray()
			};
		}

		private static PrefabSmokeResult EvaluatePrefab(string prefabPath, string heroId, CharacterDatabase database, GamePresentationConfig presentation, int index)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			if (prefab == null)
			{
				return PrefabSmokeResult.Failure(heroId, prefabPath, "prefab_load_failed");
			}
			GameObject instance = UnityEngine.Object.Instantiate(prefab, new Vector3((float)(index - 1) * 2.2f, 0f, 0f), Quaternion.identity);
			instance.name = "Smoke_" + heroId;
			int missingScripts = CountMissingScripts(instance);
			Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(includeInactive: true);
			Animator animator = instance.GetComponentInChildren<Animator>(includeInactive: true);
			RuntimeAnimatorController animatorController = ((animator != null) ? animator.runtimeAnimatorController : null);
			AnimationClip[] clips = ((animatorController != null) ? animatorController.animationClips : Array.Empty<AnimationClip>());
			string[] clipNames = (from name in (from clip in clips
					where clip != null
					select clip.name).Distinct()
				orderby name
				select name).ToArray();
			string[] eventKeys = ResolveAnimationEventKeys(clips);
			bool hasIdle = clipNames.Any((string name) => ContainsIgnoreCase(name, "idle"));
			bool hasAttack = clipNames.Any((string name) => ContainsIgnoreCase(name, "attack"));
			bool hasSkill = clipNames.Any((string name) => ContainsIgnoreCase(name, "skill"));
			bool hasSpawn = heroId != "hero_56" || clipNames.Any((string name) => ContainsIgnoreCase(name, "spawn"));
			bool expectedClips = hasIdle && hasAttack && hasSkill && hasSpawn;
			bool expectedEvents = HasExpectedEvents(heroId, eventKeys);
			CharacterDefinition definition = (((UnityEngine.Object)(object)database != null) ? database.GetCharacterById(heroId) : null);
			bool presentationBound = definition != null && definition.prefab != null;
			bool combatVisualBound = HasCombatVisualBinding(definition, presentation);
			bool passed = missingScripts == 0 && renderers.Length != 0 && animatorController != null && expectedClips && expectedEvents && presentationBound && combatVisualBound;
			string reason = (passed ? string.Empty : string.Join(",", new string[7]
			{
				(missingScripts == 0) ? null : ("missing_scripts=" + missingScripts),
				(renderers.Length != 0) ? null : "no_renderer",
				(animatorController != null) ? null : "no_animator_controller",
				expectedClips ? null : "missing_expected_clip",
				expectedEvents ? null : "missing_animation_event",
				presentationBound ? null : "presentation_prefab_unbound",
				combatVisualBound ? null : "combat_vfx_unbound"
			}.Where((string value) => !string.IsNullOrEmpty(value))));
			UnityEngine.Object.Destroy(instance);
			return new PrefabSmokeResult
			{
				heroId = heroId,
				prefabPath = prefabPath,
				passed = passed,
				missingScripts = missingScripts,
				rendererCount = renderers.Length,
				animatorController = ((animatorController != null) ? animatorController.name : string.Empty),
				clipNames = clipNames,
				eventKeys = eventKeys,
				presentationBound = presentationBound,
				combatVisualBound = combatVisualBound,
				failureReason = reason
			};
		}

		private static bool HasExpectedEvents(string heroId, string[] eventKeys)
		{
			if (heroId == "hero_55")
			{
				return eventKeys.Contains("AttackHit") && eventKeys.Contains("SkillHit");
			}
			if (heroId == "hero_56")
			{
				return eventKeys.Contains("SkillHit");
			}
			return eventKeys.Contains("FireProjectile") && eventKeys.Contains("SkillHit");
		}

		private static bool HasCombatVisualBinding(CharacterDefinition definition, GamePresentationConfig presentation)
		{
			if (definition == null || definition.attackBehavior == null || definition.skills == null || definition.skills.Count == 0)
			{
				return false;
			}
			bool defaultCombatVfx = (UnityEngine.Object)(object)presentation != null && presentation.defaultHitEffectPrefab != null && presentation.defaultAreaEffectPrefab != null;
			bool attackVisual = definition.attackBehavior.IsMelee || definition.attackBehavior.projectilePrefabOverride != null || definition.attackBehavior.muzzleEffectPrefab != null || definition.attackBehavior.hitEffectPrefab != null || ((UnityEngine.Object)(object)presentation != null && presentation.projectilePrefab != null);
			bool skillVisual = definition.skills.Any((SkillDefinition skill) => skill != null && (skill.projectilePrefab != null || skill.muzzleEffectPrefab != null || skill.hitEffectPrefab != null || skill.areaEffectPrefab != null));
			return attackVisual && (skillVisual || defaultCombatVfx);
		}

		private static bool ValidateAnimationMaterialEvents(out string summary)
		{
			Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
			if (shader == null)
			{
				summary = "shader_missing";
				return false;
			}
			Material[] previousCatalog = AnimationEventMaterialRegistry.GetConfiguredMaterials();
			GameObject root = null;
			Material original = null;
			Material replacement = null;
			try
			{
				root = new GameObject("AnimationMaterialEventSmoke");
				GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
				visual.transform.SetParent(root.transform, worldPositionStays: false);
				Renderer renderer = visual.GetComponent<Renderer>();
				original = new Material(shader)
				{
					name = "SmokeOriginalMaterial",
					color = Color.white
				};
				replacement = new Material(shader)
				{
					name = "SmokeOverrideMaterial",
					color = Color.magenta
				};
				renderer.sharedMaterial = original;
				AnimationEventMaterialRegistry.Configure((IEnumerable<Material>)new Material[1] { replacement });
				AnimationMaterialOverrideController controller = root.AddComponent<AnimationMaterialOverrideController>();
				bool overrideCall = controller.OverrideMaterial("SmokeOverrideMaterial");
				Material afterOverride = renderer.sharedMaterial;
				bool applied = overrideCall && afterOverride == replacement;
				bool resetCall = controller.ResetMaterial("SmokeOverrideMaterial");
				Material afterReset = renderer.sharedMaterial;
				bool reset = resetCall && afterReset == original;
				summary = "overrideCall=" + overrideCall + ", afterOverride=" + ((afterOverride != null) ? afterOverride.name : "null") + ", expectedOverride=" + replacement.name + ", resetCall=" + resetCall + ", afterReset=" + ((afterReset != null) ? afterReset.name : "null") + ", expectedReset=" + original.name;
				return applied && reset;
			}
			finally
			{
				AnimationEventMaterialRegistry.Configure((IEnumerable<Material>)previousCatalog);
				if (root != null)
				{
					UnityEngine.Object.DestroyImmediate(root);
				}
				if (original != null)
				{
					UnityEngine.Object.DestroyImmediate(original);
				}
				if (replacement != null)
				{
					UnityEngine.Object.DestroyImmediate(replacement);
				}
			}
		}

		private static string[] ResolveAnimationEventKeys(AnimationClip[] clips)
		{
			HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
			foreach (AnimationClip clip in clips)
			{
				if (clip == null)
				{
					continue;
				}
				AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
				foreach (AnimationEvent animationEvent in events)
				{
					if (animationEvent != null && !string.IsNullOrWhiteSpace(animationEvent.functionName))
					{
						keys.Add(animationEvent.functionName);
					}
				}
			}
			return keys.OrderBy((string key) => key).ToArray();
		}

		private static int CountMissingScripts(GameObject root)
		{
			int count = 0;
			Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < transforms.Length; i++)
			{
				if (transforms[i] != null)
				{
					count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);
				}
			}
			return count;
		}

		private static bool ValidatePortraitSafeAreaProfiles()
		{
			return ValidatePortraitSafeAreaProfile(new Vector2Int(720, 1600), new Rect(0f, 48f, 720f, 1504f)) && ValidatePortraitSafeAreaProfile(new Vector2Int(1080, 2400), new Rect(0f, 96f, 1080f, 2220f)) && ValidatePortraitSafeAreaProfile(new Vector2Int(1179, 2556), new Rect(0f, 102f, 1179f, 2277f));
		}

		private static bool ValidateSimultaneousDeathPolicy()
		{
			return DefenseGameController.IsSimultaneousDeathVictory(12, 11, 1) && DefenseGameController.IsSimultaneousDeathVictory(12, 10, 2) && DefenseGameController.IsSimultaneousDeathVictory(12, 12, 0) && !DefenseGameController.IsSimultaneousDeathVictory(12, 11, 0) && !DefenseGameController.IsSimultaneousDeathVictory(12, 9, 2) && !DefenseGameController.IsSimultaneousDeathVictory(0, 0, 1);
		}

		private static bool ValidatePortraitSafeAreaProfile(Vector2Int screenSize, Rect safeArea)
		{
			Vector2 anchorMin = default(Vector2);
			Vector2 anchorMax = default(Vector2);
			RuntimeSafeAreaFitter.CalculateSafeAreaAnchors(safeArea, screenSize, ref anchorMin, ref anchorMax);
			return screenSize.y > screenSize.x && anchorMin.x >= 0f && anchorMin.y >= 0f && anchorMax.x <= 1f && anchorMax.y <= 1f && anchorMin.x < anchorMax.x && anchorMin.y < anchorMax.y && Approximately(anchorMin, new Vector2(safeArea.xMin / (float)screenSize.x, safeArea.yMin / (float)screenSize.y)) && Approximately(anchorMax, new Vector2(safeArea.xMax / (float)screenSize.x, safeArea.yMax / (float)screenSize.y));
		}

		private static bool ValidateRoundTieredMiniShop(out string summary)
		{
			RunShopSystem shop = UnityEngine.Object.FindObjectOfType<RunShopSystem>();
			DefenseGameController controller = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
			if ((UnityEngine.Object)(object)shop == null || (UnityEngine.Object)(object)controller == null)
			{
				summary = "shop_or_controller_missing";
				return false;
			}
			BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			MethodInfo buildOffers = typeof(RunShopSystem).GetMethod("BuildOffers", instanceFlags);
			FieldInfo offersField = typeof(RunShopSystem).GetField("currentOffers", instanceFlags);
			FieldInfo goldField = typeof(DefenseGameController).GetField("<Gold>k__BackingField", instanceFlags);
			FieldInfo summonCostField = typeof(DefenseGameController).GetField("currentSummonBaseCost", instanceFlags);
			if (buildOffers == null || offersField == null || goldField == null || summonCostField == null)
			{
				summary = "reflection_target_missing";
				return false;
			}
			object originalGold = goldField.GetValue(controller);
			object originalSummonCost = summonCostField.GetValue(controller);
			IList offers = null;
			try
			{
				goldField.SetValue(controller, 34);
				summonCostField.SetValue(controller, 16);
				buildOffers.Invoke(shop, new object[4] { 3, true, false, false });
				offers = offersField.GetValue(shop) as IList;
				if (offers == null || offers.Count != 3)
				{
					summary = "offer_count=" + (offers?.Count ?? (-1));
					return false;
				}
				HashSet<string> expectedTypes = new HashSet<string> { "RandomUnit", "MergeAssist", "Coupon" };
				List<string> snapshots = new List<string>();
				Dictionary<string, int> expectedPrices = new Dictionary<string, int>
				{
					{ "RandomUnit", 19 },
					{ "MergeAssist", 20 },
					{ "Coupon", 18 }
				};
				Dictionary<string, int> firstPrices = new Dictionary<string, int>();
				bool fixedPricesValid = true;
				bool couponDurationValid = false;
				for (int i = 0; i < offers.Count; i++)
				{
					object offer = offers[i];
					Type offerType = offer.GetType();
					string typeName = offerType.GetField("type", instanceFlags)?.GetValue(offer)?.ToString() ?? string.Empty;
					string title = (offerType.GetField("title", instanceFlags)?.GetValue(offer) as string) ?? string.Empty;
					string description = (offerType.GetField("description", instanceFlags)?.GetValue(offer) as string) ?? string.Empty;
					int cost = (int)(offerType.GetField("cost", instanceFlags)?.GetValue(offer) ?? ((object)int.MaxValue));
					expectedTypes.Remove(typeName);
					fixedPricesValid &= expectedPrices.TryGetValue(typeName, out var expectedCost) && cost == expectedCost;
					firstPrices[typeName] = cost;
					if (typeName == "Coupon")
					{
						couponDurationValid = title.Contains("4라운드") && description.Contains("18%");
					}
					snapshots.Add(typeName + "=" + cost + "G");
				}
				goldField.SetValue(controller, 1);
				summonCostField.SetValue(controller, 60);
				buildOffers.Invoke(shop, new object[4] { 3, true, false, false });
				IList repricedOffers = offersField.GetValue(shop) as IList;
				bool pricesInvariant = repricedOffers != null && repricedOffers.Count == 3;
				if (repricedOffers != null)
				{
					for (int j = 0; j < repricedOffers.Count; j++)
					{
						object offer2 = repricedOffers[j];
						Type offerType2 = offer2.GetType();
						string typeName2 = offerType2.GetField("type", instanceFlags)?.GetValue(offer2)?.ToString() ?? string.Empty;
						int cost2 = (int)(offerType2.GetField("cost", instanceFlags)?.GetValue(offer2) ?? ((object)int.MaxValue));
						pricesInvariant &= firstPrices.TryGetValue(typeName2, out var firstCost) && cost2 == firstCost;
					}
				}
				summary = string.Join(", ", snapshots) + " | gold/summon invariant=" + pricesInvariant;
				return expectedTypes.Count == 0 && fixedPricesValid && pricesInvariant && couponDurationValid;
			}
			catch (Exception ex)
			{
				summary = ex.GetType().Name + ":" + ex.Message;
				return false;
			}
			finally
			{
				offers?.Clear();
				goldField.SetValue(controller, originalGold);
				summonCostField.SetValue(controller, originalSummonCost);
			}
		}

		private static bool Approximately(Vector2 left, Vector2 right)
		{
			return Vector2.SqrMagnitude(left - right) <= 0.0001f;
		}

		private static bool Approximately(Color left, Color right)
		{
			return ((Vector4)left - (Vector4)right).sqrMagnitude <= 0.0004f;
		}

		private static bool ContainsIgnoreCase(string value, string fragment)
		{
			return value != null && value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static void HandleLogMessage(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
			{
				runtimeErrors++;
			}
		}

		private static void Finish(int exitCode)
		{
			running = false;
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
			EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
			Application.logMessageReceived -= HandleLogMessage;
			EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
			EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
			EditorApplication.isPlaying = false;
			if (Application.isBatchMode)
			{
				EditorApplication.Exit(exitCode);
			}
		}
	}
}
