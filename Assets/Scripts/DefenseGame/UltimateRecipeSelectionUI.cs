using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame
{
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

		private UltimateRecipeOption[] options = new UltimateRecipeOption[0];

		private Vector2 drawerOpenPosition;

		private Vector2 drawerClosedPosition;

		private float slideProgress;

		private bool targetOpen;

		private int selectedIndex = -1;

		public void Configure(DefenseGameController controller, RectTransform drawerRect, CanvasGroup group, Button blocker, Text header, Text instruction, Button[] buttons, Text[] labels, Button close, Button confirm, Text confirmText)
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
