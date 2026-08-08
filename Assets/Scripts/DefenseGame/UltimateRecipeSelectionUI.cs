using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame
{
	public sealed class UltimateRecipeDetailView
	{
		public Image panel;
		public Image resultPortrait;
		public Text resultFallback;
		public Text resultName;
		public Text resultState;
		public Text materialHeader;
		public Text missingText;
		public Image[] materialCards;
		public Image[] materialPortraits;
		public Text[] materialFallbacks;
		public Text[] materialLabels;
	}

	public sealed class UltimateRecipeSelectionUI : MonoBehaviour
	{
		private const float SlideDuration = 0.24f;

		private DefenseGameController gameController;

		private RectTransform drawer;

		private CanvasGroup canvasGroup;

		private Button blockerButton;

		private Text headerText;

		private Text instructionText;

		private Button[] optionButtons;

		private Text[] optionLabels;

		private Button closeButton;

		private Button confirmButton;

		private Text confirmLabel;

		private UltimateRecipeDetailView detailView;

		private UltimateRecipeOption[] options = new UltimateRecipeOption[0];

		private Vector2 drawerOpenPosition;

		private Vector2 drawerClosedPosition;

		private float slideProgress;

		private bool targetOpen;

		private int selectedIndex = -1;

		public void Configure(DefenseGameController controller, RectTransform drawerRect, CanvasGroup group, Button blocker, Text header, Text instruction, Button[] buttons, Text[] labels, Button close, Button confirm, Text confirmText, UltimateRecipeDetailView detail)
		{
			gameController = controller;
			drawer = drawerRect;
			canvasGroup = group;
			blockerButton = blocker;
			headerText = header;
			instructionText = instruction;
			optionButtons = (Button[])(((object)buttons) ?? ((object)new Button[0]));
			optionLabels = (Text[])(((object)labels) ?? ((object)new Text[0]));
			closeButton = close;
			confirmButton = confirm;
			confirmLabel = confirmText;
			detailView = detail;
			drawerOpenPosition = ((drawer != null) ? drawer.anchoredPosition : Vector2.zero);
			drawerClosedPosition = drawerOpenPosition + Vector2.down * 860f;
			if ((Object)(object)blockerButton != null)
			{
				((UnityEventBase)(object)blockerButton.onClick).RemoveAllListeners();
				((UnityEvent)(object)blockerButton.onClick).AddListener((UnityAction)Close);
			}
			if ((Object)(object)closeButton != null)
			{
				((UnityEventBase)(object)closeButton.onClick).RemoveAllListeners();
				((UnityEvent)(object)closeButton.onClick).AddListener((UnityAction)Close);
			}
			if ((Object)(object)confirmButton != null)
			{
				((UnityEventBase)(object)confirmButton.onClick).RemoveAllListeners();
				((UnityEvent)(object)confirmButton.onClick).AddListener((UnityAction)ConfirmSelection);
			}
			for (int i = 0; i < optionButtons.Length; i++)
			{
				int optionIndex = i;
				if (!((Object)(object)optionButtons[i] == null))
				{
					((UnityEventBase)(object)optionButtons[i].onClick).RemoveAllListeners();
					((UnityEvent)(object)optionButtons[i].onClick).AddListener((UnityAction)delegate
					{
						SelectOption(optionIndex);
					});
				}
			}
			if (canvasGroup != null)
			{
				canvasGroup.alpha = 0f;
				canvasGroup.interactable = false;
				canvasGroup.blocksRaycasts = true;
			}
			if (drawer != null)
			{
				drawer.anchoredPosition = drawerClosedPosition;
			}
		}

		public void Open()
		{
			if (gameController == null || gameController.IsCombatInteractionLocked)
			{
				return;
			}
			options = gameController.GetAllUltimateRecipeOptions();
			if (options == null || options.Length == 0)
			{
				gameController.RequestBanner("초월 레시피 정보를 불러오지 못했습니다", new Color(0.72f, 0.82f, 1f), 1.8f);
				return;
			}
			selectedIndex = -1;
			base.gameObject.SetActive(value: true);
			base.transform.SetAsLastSibling();
			slideProgress = 0f;
			targetOpen = true;
			if (drawer != null)
			{
				drawer.anchoredPosition = drawerClosedPosition;
			}
			if (canvasGroup != null)
			{
				canvasGroup.alpha = 0f;
				canvasGroup.interactable = true;
				canvasGroup.blocksRaycasts = true;
			}
			RefreshOptionVisuals();
			PreviewSelectedRecipe();
		}

		public void Close()
		{
			if (base.gameObject.activeSelf)
			{
				targetOpen = false;
				if (canvasGroup != null)
				{
					canvasGroup.interactable = false;
				}
				if (gameController != null)
				{
					gameController.SetUltimateRecipePreview(null);
				}
			}
		}

		private void Update()
		{
			float direction = (targetOpen ? 1f : (-1f));
			slideProgress = Mathf.Clamp01(slideProgress + direction * Time.unscaledDeltaTime / 0.24f);
			float eased = 1f - Mathf.Pow(1f - slideProgress, 3f);
			if (drawer != null)
			{
				drawer.anchoredPosition = Vector2.LerpUnclamped(drawerClosedPosition, drawerOpenPosition, eased);
			}
			if (canvasGroup != null)
			{
				canvasGroup.alpha = eased;
			}
			RefreshReadyOutlinePulse();
			if (!targetOpen && slideProgress <= 0f)
			{
				base.gameObject.SetActive(value: false);
			}
		}

		private void RefreshReadyOutlinePulse()
		{
			if (optionButtons == null)
			{
				return;
			}
			int optionCount = ((options != null) ? options.Length : 0);
			float pulse = (Mathf.Sin(Time.unscaledTime * 5.2f) + 1f) * 0.5f;
			Color lowGlow = new Color(0.7f, 0.24f, 1f, 0.72f);
			Color highGlow = new Color(1f, 0.86f, 0.24f, 1f);
			for (int i = 0; i < optionButtons.Length; i++)
			{
				Button button = optionButtons[i];
				if ((Object)(object)button == null)
				{
					continue;
				}
				Outline outline = ((Component)(object)button).GetComponent<Outline>();
				if (!((Object)(object)outline == null))
				{
					if (i >= optionCount || !options[i].isReady || !((Component)(object)button).gameObject.activeSelf)
					{
						((Shadow)outline).effectColor = Color.clear;
						continue;
					}
					float selectedBoost = ((i == selectedIndex) ? 0.18f : 0f);
					((Shadow)outline).effectColor = Color.Lerp(lowGlow, highGlow, Mathf.Clamp01(pulse + selectedBoost));
					float width = 2.5f + pulse * 2.5f + selectedBoost * 3f;
					((Shadow)outline).effectDistance = new Vector2(width, 0f - width);
				}
			}
		}

		private void SelectOption(int index)
		{
			if (index >= 0 && index < options.Length)
			{
				selectedIndex = index;
				RefreshOptionVisuals();
				PreviewSelectedRecipe();
			}
		}

		private void PreviewSelectedRecipe()
		{
			string recipeName = ((selectedIndex >= 0 && selectedIndex < options.Length) ? options[selectedIndex].recipeName : null);
			if (gameController != null)
			{
				gameController.SetUltimateRecipePreview(recipeName, previewActive: true);
			}
		}

		private void ConfirmSelection()
		{
			if (!(gameController == null) && selectedIndex >= 0 && selectedIndex < options.Length && options[selectedIndex].isReady)
			{
				string recipeName = options[selectedIndex].recipeName;
				if (gameController.TryMergeUltimateRecipe(recipeName))
				{
					Close();
					return;
				}
				gameController.RequestBanner("초월 재료 상태가 변경되었습니다. 다시 선택하세요", new Color(1f, 0.58f, 0.24f), 2f);
				options = gameController.GetAllUltimateRecipeOptions();
				selectedIndex = ((options == null || options.Length != 1) ? (-1) : 0);
				RefreshOptionVisuals();
				PreviewSelectedRecipe();
			}
		}

		private void RefreshOptionVisuals()
		{
			//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0227: Unknown result type (might be due to invalid IL or missing references)
			int optionCount = ((options != null) ? options.Length : 0);
			int readyCount = 0;
			for (int i = 0; i < optionCount; i++)
			{
				if (options[i].isReady)
				{
					readyCount++;
				}
			}
			if ((Object)(object)headerText != null)
			{
				headerText.text = "초월 레시피  READY " + readyCount + " / " + optionCount;
			}
			if ((Object)(object)instructionText != null)
			{
				instructionText.text = ((selectedIndex < 0) ? "레시피를 누르면 보유 재료와 부족한 유닛을 확인할 수 있습니다." : (options[selectedIndex].isReady ? "재료가 모두 준비됐습니다. 보드의 빛나는 재료를 확인하고 실행하세요." : ("부족 재료: " + options[selectedIndex].missingSummary)));
			}
			RefreshDetailVisual();
			for (int j = 0; j < optionButtons.Length; j++)
			{
				Button button = optionButtons[j];
				bool visible = j < optionCount;
				if ((Object)(object)button == null)
				{
					continue;
				}
				((Component)(object)button).gameObject.SetActive(visible);
				if (visible)
				{
					UltimateRecipeOption option = options[j];
					bool selected = j == selectedIndex;
					Color readinessColor = (option.isReady ? option.accentColor : new Color(0.36f, 0.4f, 0.58f, 1f));
					Color baseColor = Color.Lerp(readinessColor, new Color(0.08f, 0.07f, 0.24f, 1f), selected ? 0.36f : 0.76f);
					Graphic graphic = ((Selectable)button).targetGraphic;
					if ((Object)(object)graphic != null)
					{
						graphic.color = baseColor;
					}
					ColorBlock colors = ((Selectable)button).colors;
					colors.normalColor = baseColor;
					colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
					colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.22f);
					((Selectable)button).colors = colors;
					if (j < optionLabels.Length && (Object)(object)optionLabels[j] != null)
					{
						string state = (option.isReady ? "READY" : (option.progress + "/" + option.required));
						optionLabels[j].text = (selected ? "▶ " : string.Empty) + "[" + state + "] " + option.displayName + "\n결과  " + Compact(option.resultSummary, 32) + "\n" + (option.isReady ? ("소모  " + Compact(option.materialSummary, 46)) : ("부족  " + Compact(option.missingSummary, 46)));
						((Graphic)optionLabels[j]).color = (selected ? new Color(1f, 0.94f, 0.58f) : Color.white);
					}
				}
			}
			bool canConfirm = selectedIndex >= 0 && selectedIndex < optionCount && options[selectedIndex].isReady;
			if ((Object)(object)confirmButton != null)
			{
				((Selectable)confirmButton).interactable = canConfirm;
			}
			if ((Object)(object)confirmLabel != null)
			{
				confirmLabel.text = (canConfirm ? "선택한 초월 실행" : "레시피를 선택하세요");
			}
		}

		private void RefreshDetailVisual()
		{
			if (detailView == null)
			{
				return;
			}
			bool hasSelection = selectedIndex >= 0 && selectedIndex < ((options != null) ? options.Length : 0);
			if (detailView.panel != null)
			{
				detailView.panel.gameObject.SetActive(true);
			}
			if (!hasSelection)
			{
				SetDetailEmptyState();
				return;
			}
			UltimateRecipeOption option = options[selectedIndex];
			ApplyPortrait(detailView.resultPortrait, detailView.resultFallback, option.primaryResultDefinition, option.accentColor, option.resultSummary);
			if (detailView.resultName != null)
			{
				detailView.resultName.text = option.resultSummary;
			}
			if (detailView.resultState != null)
			{
				detailView.resultState.text = option.isReady ? "READY - \ucd08\uc6d4 \uc18c\ud658 \uac00\ub2a5" : ("\uc9c4\ud589 " + option.progress + "/" + option.required);
				detailView.resultState.color = option.isReady ? new Color(1f, 0.86f, 0.24f) : new Color(0.74f, 0.84f, 1f);
			}
			if (detailView.materialHeader != null)
			{
				detailView.materialHeader.text = "\ud544\uc694 \uc7ac\ub8cc";
			}
			if (detailView.missingText != null)
			{
				detailView.missingText.text = option.isReady ? "\ubaa8\ub4e0 \uc7ac\ub8cc \uc900\ube44 \uc644\ub8cc" : ("\ubd80\uc871 \uc7ac\ub8cc: " + option.missingSummary);
				detailView.missingText.color = option.isReady ? new Color(0.38f, 1f, 0.72f) : new Color(1f, 0.56f, 0.36f);
			}
			int materialCount = (option.materials != null) ? option.materials.Length : 0;
			for (int i = 0; detailView.materialCards != null && i < detailView.materialCards.Length; i++)
			{
				bool visible = i < materialCount;
				Image card = detailView.materialCards[i];
				if (card != null)
				{
					card.gameObject.SetActive(visible);
				}
				if (!visible)
				{
					continue;
				}
				UltimateRecipeMaterialView material = option.materials[i];
				if (card != null)
				{
					card.color = Color.Lerp(material.isReady ? new Color(0.10f, 0.35f, 0.24f) : new Color(0.38f, 0.13f, 0.20f), material.accentColor, 0.20f);
				}
				Text fallback = (detailView.materialFallbacks != null && i < detailView.materialFallbacks.Length) ? detailView.materialFallbacks[i] : null;
				Image portrait = (detailView.materialPortraits != null && i < detailView.materialPortraits.Length) ? detailView.materialPortraits[i] : null;
				ApplyPortrait(portrait, fallback, material.definition, material.accentColor, material.displayName);
				if (detailView.materialLabels != null && i < detailView.materialLabels.Length && detailView.materialLabels[i] != null)
				{
					detailView.materialLabels[i].text = (material.isReady ? "\u2713 " : "\u2715 ") + material.displayName + "\n" + material.ownedCount + " / " + material.requiredCount;
					detailView.materialLabels[i].color = material.isReady ? Color.white : new Color(1f, 0.78f, 0.72f);
				}
			}
		}

		private void SetDetailEmptyState()
		{
			if (detailView.resultName != null)
			{
				detailView.resultName.text = "\ucd08\uc6d4 \ub808\uc2dc\ud53c\ub97c \uc120\ud0dd\ud558\uc138\uc694";
			}
			if (detailView.resultState != null)
			{
				detailView.resultState.text = "\uc120\ud0dd \uc2dc \ud544\uc694 \uc7ac\ub8cc\uc640 \ubcf4\ub4dc \ubbf8\ub9ac\ubcf4\uae30\ub97c \ud655\uc778\ud569\ub2c8\ub2e4.";
			}
			if (detailView.materialHeader != null)
			{
				detailView.materialHeader.text = "\uc7ac\ub8cc \uc0c1\ud0dc";
			}
			if (detailView.missingText != null)
			{
				detailView.missingText.text = "\ub808\uc2dc\ud53c \uc120\ud0dd \ud6c4 \uc7ac\ub8cc\ub97c \ud655\uc778\ud560 \uc218 \uc788\uc2b5\ub2c8\ub2e4.";
			}
			if (detailView.materialCards != null)
			{
				for (int i = 0; i < detailView.materialCards.Length; i++)
				{
					if (detailView.materialCards[i] != null)
					{
						detailView.materialCards[i].gameObject.SetActive(false);
					}
				}
			}
		}

		private static void ApplyPortrait(Image portrait, Text fallback, CharacterDefinition definition, Color accentColor, string fallbackValue)
		{
			Sprite sprite = RollRollUiResource.ResolveCharacterSprite(definition);
			if (portrait != null)
			{
				portrait.sprite = sprite;
				portrait.type = Image.Type.Simple;
				portrait.preserveAspect = sprite != null;
				portrait.color = (sprite != null) ? Color.white : Color.Lerp(accentColor, Color.white, 0.32f);
			}
			if (fallback != null)
			{
				fallback.gameObject.SetActive(sprite == null);
				fallback.text = BuildPortraitLabel(fallbackValue);
			}
		}

		private static string BuildPortraitLabel(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "?";
			}
			string trimmed = value.Trim();
			return (trimmed.Length <= 2) ? trimmed.ToUpperInvariant() : trimmed.Substring(0, 2).ToUpperInvariant();
		}

		private static string Compact(string value, int maxLength)
		{
			if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
			{
				return string.IsNullOrWhiteSpace(value) ? "-" : value;
			}
			return value.Substring(0, Mathf.Max(1, maxLength - 3)) + "...";
		}

		private void OnDisable()
		{
			targetOpen = false;
			if (gameController != null)
			{
				gameController.SetUltimateRecipePreview(null);
			}
		}
	}
}
