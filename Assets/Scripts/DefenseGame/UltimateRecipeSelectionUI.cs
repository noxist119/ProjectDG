using System.Collections.Generic;
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
		public RectTransform materialContent;
		public Image materialTemplate;
		public ScrollRect materialScroll;
	}

	public sealed class UltimateRecipeSelectionUI : MonoBehaviour
	{
		private const float SlideDuration = 0.24f;
		private const float RefreshInterval = 0.15f;

		private DefenseGameController gameController;
		private RectTransform drawer;
		private CanvasGroup canvasGroup;
		private Button blockerButton;
		private Text headerText;
		private Text instructionText;
		private RectTransform optionContent;
		private Button optionTemplate;
		private ScrollRect optionScroll;
		private readonly List<Button> optionButtons = new List<Button>();
		private readonly List<Text> optionLabels = new List<Text>();
		private Button closeButton;
		private Button confirmButton;
		private Text confirmLabel;
		private UltimateRecipeDetailView detailView;
		private readonly List<Image> materialCards = new List<Image>();
		private readonly List<Image> materialPortraits = new List<Image>();
		private readonly List<Text> materialFallbacks = new List<Text>();
		private readonly List<Text> materialLabels = new List<Text>();
		private UltimateRecipeOption[] options = new UltimateRecipeOption[0];
		private Vector2 drawerOpenPosition;
		private Vector2 drawerClosedPosition;
		private float slideProgress;
		private float nextStateRefreshTime;
		private bool targetOpen;
		private int selectedIndex = -1;
		private string selectedRecipeName;
		private int optionStateSignature = int.MinValue;

		public void Configure(DefenseGameController controller, RectTransform drawerRect, CanvasGroup group, Button blocker, Text header, Text instruction, RectTransform optionsRect, Button template, ScrollRect scroll, Button close, Button confirm, Text confirmText, UltimateRecipeDetailView detail)
		{
			gameController = controller;
			drawer = drawerRect;
			canvasGroup = group;
			blockerButton = blocker;
			headerText = header;
			instructionText = instruction;
			optionContent = optionsRect;
			optionTemplate = template;
			optionScroll = scroll;
			closeButton = close;
			confirmButton = confirm;
			confirmLabel = confirmText;
			detailView = detail;
			drawerOpenPosition = drawer != null ? drawer.anchoredPosition : Vector2.zero;
			drawerClosedPosition = drawerOpenPosition + Vector2.down * 860f;
			if (optionTemplate != null)
			{
				optionTemplate.gameObject.SetActive(false);
			}
			if (detailView != null && detailView.materialTemplate != null)
			{
				detailView.materialTemplate.gameObject.SetActive(false);
			}
			if (blockerButton != null)
			{
				blockerButton.onClick.RemoveAllListeners();
				blockerButton.onClick.AddListener(Close);
			}
			if (closeButton != null)
			{
				closeButton.onClick.RemoveAllListeners();
				closeButton.onClick.AddListener(Close);
			}
			if (confirmButton != null)
			{
				confirmButton.onClick.RemoveAllListeners();
				confirmButton.onClick.AddListener(ConfirmSelection);
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
			selectedIndex = -1;
			selectedRecipeName = null;
			optionStateSignature = int.MinValue;
			gameObject.SetActive(true);
			transform.SetAsLastSibling();
			slideProgress = 0f;
			targetOpen = true;
			nextStateRefreshTime = 0f;
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
			RefreshOptionsFromBoard(true);
			PreviewSelectedRecipe();
		}

		public void Close()
		{
			if (!gameObject.activeSelf)
			{
				return;
			}
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

		private void Update()
		{
			float direction = targetOpen ? 1f : -1f;
			slideProgress = Mathf.Clamp01(slideProgress + direction * Time.unscaledDeltaTime / SlideDuration);
			float eased = 1f - Mathf.Pow(1f - slideProgress, 3f);
			if (drawer != null)
			{
				drawer.anchoredPosition = Vector2.LerpUnclamped(drawerClosedPosition, drawerOpenPosition, eased);
			}
			if (canvasGroup != null)
			{
				canvasGroup.alpha = eased;
			}
			if (targetOpen && Time.unscaledTime >= nextStateRefreshTime)
			{
				nextStateRefreshTime = Time.unscaledTime + RefreshInterval;
				RefreshOptionsFromBoard(false);
			}
			RefreshReadyOutlinePulse();
			if (!targetOpen && slideProgress <= 0f)
			{
				gameObject.SetActive(false);
			}
		}

		private void RefreshOptionsFromBoard(bool force)
		{
			UltimateRecipeOption[] refreshed = gameController != null ? gameController.GetRelatedUltimateRecipeOptions() : new UltimateRecipeOption[0];
			int signature = BuildOptionsSignature(refreshed);
			if (!force && signature == optionStateSignature)
			{
				return;
			}

			string previousRecipeName = selectedRecipeName;
			options = refreshed ?? new UltimateRecipeOption[0];
			optionStateSignature = signature;
			selectedIndex = FindOptionIndex(previousRecipeName);
			if (!string.IsNullOrEmpty(previousRecipeName) && selectedIndex < 0)
			{
				selectedRecipeName = null;
				if (gameController != null)
				{
					gameController.SetUltimateRecipePreview(null);
				}
			}
			EnsureOptionPool(options.Length);
			RefreshOptionVisuals();
		}

		private int FindOptionIndex(string recipeName)
		{
			if (string.IsNullOrEmpty(recipeName) || options == null)
			{
				return -1;
			}
			for (int i = 0; i < options.Length; i++)
			{
				if (options[i].recipeName == recipeName)
				{
					return i;
				}
			}
			return -1;
		}

		private void EnsureOptionPool(int requiredCount)
		{
			if (optionTemplate == null || optionContent == null)
			{
				return;
			}
			while (optionButtons.Count < requiredCount)
			{
				int poolIndex = optionButtons.Count;
				Button button = Instantiate(optionTemplate, optionContent);
				button.name = "UltimateRecipeOptionRuntime_" + poolIndex;
				button.gameObject.SetActive(true);
				Text label = button.GetComponentInChildren<Text>(true);
				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(delegate { SelectOption(poolIndex); });
				optionButtons.Add(button);
				optionLabels.Add(label);
			}
			int rows = Mathf.Max(1, Mathf.CeilToInt(requiredCount / 2f));
			optionContent.sizeDelta = new Vector2(880f, Mathf.Max(330f, rows * 66f + 8f));
			for (int i = 0; i < optionButtons.Count; i++)
			{
				RectTransform rect = optionButtons[i].GetComponent<RectTransform>();
				if (rect == null)
				{
					continue;
				}
				rect.anchorMin = new Vector2(0f, 1f);
				rect.anchorMax = new Vector2(0f, 1f);
				rect.pivot = new Vector2(0f, 1f);
				rect.anchoredPosition = new Vector2((i % 2) * 440f, -(i / 2) * 66f);
				rect.sizeDelta = new Vector2(430f, 60f);
			}
		}

		private void EnsureMaterialPool(int requiredCount)
		{
			if (detailView == null || detailView.materialTemplate == null || detailView.materialContent == null)
			{
				return;
			}
			while (materialCards.Count < requiredCount)
			{
				int poolIndex = materialCards.Count;
				Image card = Instantiate(detailView.materialTemplate, detailView.materialContent);
				card.name = "UltimateRecipeMaterialRuntime_" + poolIndex;
				card.gameObject.SetActive(true);
				materialCards.Add(card);
				materialPortraits.Add(card.transform.Find("Portrait") != null ? card.transform.Find("Portrait").GetComponent<Image>() : null);
				materialFallbacks.Add(card.transform.Find("Portrait/Fallback") != null ? card.transform.Find("Portrait/Fallback").GetComponent<Text>() : null);
				materialLabels.Add(card.transform.Find("Label") != null ? card.transform.Find("Label").GetComponent<Text>() : null);
			}
			int rows = Mathf.Max(1, Mathf.CeilToInt(requiredCount / 3f));
			detailView.materialContent.sizeDelta = new Vector2(636f, Mathf.Max(142f, rows * 74f + 8f));
			for (int i = 0; i < materialCards.Count; i++)
			{
				RectTransform rect = materialCards[i].rectTransform;
				rect.anchorMin = new Vector2(0f, 1f);
				rect.anchorMax = new Vector2(0f, 1f);
				rect.pivot = new Vector2(0f, 1f);
				rect.anchoredPosition = new Vector2((i % 3) * 210f, -(i / 3) * 74f);
				rect.sizeDelta = new Vector2(196f, 66f);
			}
		}

		private void RefreshReadyOutlinePulse()
		{
			float pulse = (Mathf.Sin(Time.unscaledTime * 5.2f) + 1f) * 0.5f;
			Color lowGlow = new Color(0.7f, 0.24f, 1f, 0.72f);
			Color highGlow = new Color(1f, 0.86f, 0.24f, 1f);
			for (int i = 0; i < optionButtons.Count; i++)
			{
				Button button = optionButtons[i];
				Outline outline = button != null ? button.GetComponent<Outline>() : null;
				if (outline == null)
				{
					continue;
				}
				if (i >= options.Length || !options[i].isReady || !button.gameObject.activeSelf)
				{
					outline.effectColor = Color.clear;
					continue;
				}
				float selectedBoost = i == selectedIndex ? 0.18f : 0f;
				outline.effectColor = Color.Lerp(lowGlow, highGlow, Mathf.Clamp01(pulse + selectedBoost));
				float width = 2.5f + pulse * 2.5f + selectedBoost * 3f;
				outline.effectDistance = new Vector2(width, -width);
			}
		}

		private void SelectOption(int index)
		{
			if (index < 0 || index >= options.Length)
			{
				return;
			}
			selectedIndex = index;
			selectedRecipeName = options[index].recipeName;
			RefreshOptionVisuals();
			PreviewSelectedRecipe();
		}

		private void PreviewSelectedRecipe()
		{
			string recipeName = selectedIndex >= 0 && selectedIndex < options.Length ? options[selectedIndex].recipeName : null;
			if (gameController != null)
			{
				gameController.SetUltimateRecipePreview(recipeName, previewActive: !string.IsNullOrEmpty(recipeName));
			}
		}

		private void ConfirmSelection()
		{
			if (gameController == null || selectedIndex < 0 || selectedIndex >= options.Length || !options[selectedIndex].isReady)
			{
				return;
			}
			string recipeName = options[selectedIndex].recipeName;
			if (gameController.TryMergeUltimateRecipe(recipeName))
			{
				Close();
				return;
			}
			gameController.RequestBanner("\ucd08\uc6d4 \uc7ac\ub8cc \uc0c1\ud0dc\uac00 \ubcc0\uacbd\ub418\uc5c8\uc2b5\ub2c8\ub2e4. \ub2e4\uc2dc \uc120\ud0dd\ud558\uc138\uc694", new Color(1f, 0.58f, 0.24f), 2f);
			RefreshOptionsFromBoard(true);
			PreviewSelectedRecipe();
		}

		private void RefreshOptionVisuals()
		{
			int readyCount = 0;
			for (int i = 0; i < options.Length; i++)
			{
				if (options[i].isReady)
				{
					readyCount++;
				}
			}
			if (headerText != null)
			{
				headerText.text = "\ucd08\uc6d4 \ub808\uc2dc\ud53c  READY " + readyCount + " / " + options.Length;
			}
			if (instructionText != null)
			{
				instructionText.text = selectedIndex < 0
					? (options.Length == 0 ? "\ubcf4\ub4dc \uc7ac\ub8cc\uac00 \uc788\ub294 \ucd08\uc6d4 \ub808\uc2dc\ud53c\ub9cc \ubcf4\uc5ec\uc90d\ub2c8\ub2e4." : "\ub808\uc2dc\ud53c\ub97c \ub204\ub974\uba74 \ubcf4\uc720 \uc7ac\ub8cc\uc640 \ubd80\uc871\ud55c \uc720\ub2db\uc744 \ud655\uc778\ud560 \uc218 \uc788\uc2b5\ub2c8\ub2e4.")
					: (options[selectedIndex].isReady ? "\uc7ac\ub8cc\uac00 \ubaa8\ub450 \uc900\ube44\ub410\uc2b5\ub2c8\ub2e4. \ubcf4\ub4dc\uc758 \ube5b\ub098\ub294 \uc7ac\ub8cc\ub97c \ud655\uc778\ud558\uace0 \uc2e4\ud589\ud558\uc138\uc694." : ("\ubd80\uc871 \uc7ac\ub8cc: " + options[selectedIndex].missingSummary));
			}
			for (int i = 0; i < optionButtons.Count; i++)
			{
				bool visible = i < options.Length;
				Button button = optionButtons[i];
				button.gameObject.SetActive(visible);
				if (!visible)
				{
					continue;
				}
				UltimateRecipeOption option = options[i];
				bool selected = i == selectedIndex;
				Color readinessColor = option.isReady ? option.accentColor : new Color(0.36f, 0.4f, 0.58f, 1f);
				Color baseColor = Color.Lerp(readinessColor, new Color(0.08f, 0.07f, 0.24f, 1f), selected ? 0.36f : 0.76f);
				Graphic graphic = button.targetGraphic;
				if (graphic != null)
				{
					graphic.color = baseColor;
				}
				ColorBlock colors = button.colors;
				colors.normalColor = baseColor;
				colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
				colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.22f);
				button.colors = colors;
				Text label = optionLabels[i];
				if (label != null)
				{
					label.text = option.isReady
						? "[READY] " + option.displayName + "\n" + option.progress + "/" + option.required
						: option.displayName + "\n" + option.progress + "/" + option.required + " \u00b7 " + option.missingMaterialCount + "\uac1c \ubd80\uc871";
					label.color = selected ? new Color(1f, 0.94f, 0.58f) : Color.white;
				}
			}
			bool canConfirm = selectedIndex >= 0 && selectedIndex < options.Length && options[selectedIndex].isReady;
			if (confirmButton != null)
			{
				confirmButton.interactable = canConfirm;
			}
			if (confirmLabel != null)
			{
				confirmLabel.text = canConfirm ? "\uc120\ud0dd\ud55c \ucd08\uc6d4 \uc2e4\ud589" : "\ub808\uc2dc\ud53c\ub97c \uc120\ud0dd\ud558\uc138\uc694";
			}
			RefreshDetailVisual();
		}

		private void RefreshDetailVisual()
		{
			if (detailView == null)
			{
				return;
			}
			if (detailView.panel != null)
			{
				detailView.panel.gameObject.SetActive(true);
			}
			if (selectedIndex < 0 || selectedIndex >= options.Length)
			{
				SetDetailEmptyState();
				return;
			}
			UltimateRecipeOption option = options[selectedIndex];
			ApplyPortrait(detailView.resultPortrait, detailView.resultFallback, option.resultDefinition, option.accentColor, option.resultSummary);
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
			int materialCount = option.materials != null ? option.materials.Length : 0;
			EnsureMaterialPool(materialCount);
			for (int i = 0; i < materialCards.Count; i++)
			{
				bool visible = i < materialCount;
				Image card = materialCards[i];
				card.gameObject.SetActive(visible);
				if (!visible)
				{
					continue;
				}
				UltimateRecipeMaterialView material = option.materials[i];
				card.color = Color.Lerp(material.isReady ? new Color(0.10f, 0.35f, 0.24f) : new Color(0.38f, 0.13f, 0.20f), material.accentColor, 0.20f);
				ApplyPortrait(materialPortraits[i], materialFallbacks[i], material.definition, material.accentColor, material.displayName);
				if (materialLabels[i] != null)
				{
					materialLabels[i].text = (material.isReady ? "\u2713 " : "\u2715 ") + material.displayName + "\n" + material.ownedCount + " / " + material.requiredCount;
					materialLabels[i].color = material.isReady ? Color.white : new Color(1f, 0.78f, 0.72f);
				}
			}
			if (detailView.materialScroll != null)
			{
				detailView.materialScroll.verticalNormalizedPosition = 1f;
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
			for (int i = 0; i < materialCards.Count; i++)
			{
				materialCards[i].gameObject.SetActive(false);
			}
		}

		private static int BuildOptionsSignature(UltimateRecipeOption[] recipeOptions)
		{
			unchecked
			{
				int hash = 17;
				int count = recipeOptions != null ? recipeOptions.Length : 0;
				for (int i = 0; i < count; i++)
				{
					UltimateRecipeOption option = recipeOptions[i];
					hash = hash * 31 + (option.recipeName != null ? option.recipeName.GetHashCode() : 0);
					hash = hash * 31 + option.progress;
					hash = hash * 31 + option.required;
					hash = hash * 31 + option.missingMaterialCount;
					hash = hash * 31 + (option.isReady ? 1 : 0);
					if (option.materials != null)
					{
						for (int j = 0; j < option.materials.Length; j++)
						{
							hash = hash * 31 + option.materials[j].ownedCount;
							hash = hash * 31 + option.materials[j].requiredCount;
						}
					}
				}
				return hash;
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
				portrait.color = sprite != null ? Color.white : Color.Lerp(accentColor, Color.white, 0.32f);
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
			return trimmed.Length <= 2 ? trimmed.ToUpperInvariant() : trimmed.Substring(0, 2).ToUpperInvariant();
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
