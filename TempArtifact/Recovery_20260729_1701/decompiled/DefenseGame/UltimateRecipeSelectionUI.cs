using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

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
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected O, but got Unknown
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Expected O, but got Unknown
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
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
		drawerOpenPosition = (((Object)(object)drawer != (Object)null) ? drawer.anchoredPosition : Vector2.zero);
		drawerClosedPosition = drawerOpenPosition + Vector2.down * 860f;
		if ((Object)(object)blockerButton != (Object)null)
		{
			((UnityEventBase)blockerButton.onClick).RemoveAllListeners();
			((UnityEvent)blockerButton.onClick).AddListener(new UnityAction(Close));
		}
		if ((Object)(object)closeButton != (Object)null)
		{
			((UnityEventBase)closeButton.onClick).RemoveAllListeners();
			((UnityEvent)closeButton.onClick).AddListener(new UnityAction(Close));
		}
		if ((Object)(object)confirmButton != (Object)null)
		{
			((UnityEventBase)confirmButton.onClick).RemoveAllListeners();
			((UnityEvent)confirmButton.onClick).AddListener(new UnityAction(ConfirmSelection));
		}
		for (int i = 0; i < optionButtons.Length; i++)
		{
			int optionIndex = i;
			if (!((Object)(object)optionButtons[i] == (Object)null))
			{
				((UnityEventBase)optionButtons[i].onClick).RemoveAllListeners();
				((UnityEvent)optionButtons[i].onClick).AddListener((UnityAction)delegate
				{
					SelectOption(optionIndex);
				});
			}
		}
		if ((Object)(object)canvasGroup != (Object)null)
		{
			canvasGroup.alpha = 0f;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = true;
		}
		if ((Object)(object)drawer != (Object)null)
		{
			drawer.anchoredPosition = drawerClosedPosition;
		}
	}

	public void Open()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameController == (Object)null || gameController.IsCombatInteractionLocked)
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
		((Component)this).gameObject.SetActive(true);
		((Component)this).transform.SetAsLastSibling();
		slideProgress = 0f;
		targetOpen = true;
		if ((Object)(object)drawer != (Object)null)
		{
			drawer.anchoredPosition = drawerClosedPosition;
		}
		if ((Object)(object)canvasGroup != (Object)null)
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
		if (((Component)this).gameObject.activeSelf)
		{
			targetOpen = false;
			if ((Object)(object)canvasGroup != (Object)null)
			{
				canvasGroup.interactable = false;
			}
			if ((Object)(object)gameController != (Object)null)
			{
				gameController.SetUltimateRecipePreview(null);
			}
		}
	}

	private void Update()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		float num = (targetOpen ? 1f : (-1f));
		slideProgress = Mathf.Clamp01(slideProgress + num * Time.unscaledDeltaTime / 0.24f);
		float num2 = 1f - Mathf.Pow(1f - slideProgress, 3f);
		if ((Object)(object)drawer != (Object)null)
		{
			drawer.anchoredPosition = Vector2.LerpUnclamped(drawerClosedPosition, drawerOpenPosition, num2);
		}
		if ((Object)(object)canvasGroup != (Object)null)
		{
			canvasGroup.alpha = num2;
		}
		RefreshReadyOutlinePulse();
		if (!targetOpen && slideProgress <= 0f)
		{
			((Component)this).gameObject.SetActive(false);
		}
	}

	private void RefreshReadyOutlinePulse()
	{
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		if (optionButtons == null)
		{
			return;
		}
		int num = ((options != null) ? options.Length : 0);
		float num2 = (Mathf.Sin(Time.unscaledTime * 5.2f) + 1f) * 0.5f;
		Color val = default(Color);
		((Color)(ref val))._002Ector(0.7f, 0.24f, 1f, 0.72f);
		Color val2 = default(Color);
		((Color)(ref val2))._002Ector(1f, 0.86f, 0.24f, 1f);
		for (int i = 0; i < optionButtons.Length; i++)
		{
			Button val3 = optionButtons[i];
			if ((Object)(object)val3 == (Object)null)
			{
				continue;
			}
			Outline component = ((Component)val3).GetComponent<Outline>();
			if (!((Object)(object)component == (Object)null))
			{
				if (i >= num || !options[i].isReady || !((Component)val3).gameObject.activeSelf)
				{
					((Shadow)component).effectColor = Color.clear;
					continue;
				}
				float num3 = ((i == selectedIndex) ? 0.18f : 0f);
				((Shadow)component).effectColor = Color.Lerp(val, val2, Mathf.Clamp01(num2 + num3));
				float num4 = 2.5f + num2 * 2.5f + num3 * 3f;
				((Shadow)component).effectDistance = new Vector2(num4, 0f - num4);
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
		if ((Object)(object)gameController != (Object)null)
		{
			gameController.SetUltimateRecipePreview(recipeName, previewActive: true);
		}
	}

	private void ConfirmSelection()
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)gameController == (Object)null) && selectedIndex >= 0 && selectedIndex < options.Length && options[selectedIndex].isReady)
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
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		int num = ((options != null) ? options.Length : 0);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			if (options[i].isReady)
			{
				num2++;
			}
		}
		if ((Object)(object)headerText != (Object)null)
		{
			headerText.text = "초월 레시피  READY " + num2 + " / " + num;
		}
		if ((Object)(object)instructionText != (Object)null)
		{
			instructionText.text = ((selectedIndex < 0) ? "레시피를 누르면 보유 재료와 부족한 유닛을 확인할 수 있습니다." : (options[selectedIndex].isReady ? "재료가 모두 준비됐습니다. 보드의 빛나는 재료를 확인하고 실행하세요." : ("부족 재료: " + options[selectedIndex].missingSummary)));
		}
		for (int j = 0; j < optionButtons.Length; j++)
		{
			Button val = optionButtons[j];
			bool flag = j < num;
			if ((Object)(object)val == (Object)null)
			{
				continue;
			}
			((Component)val).gameObject.SetActive(flag);
			if (flag)
			{
				UltimateRecipeOption ultimateRecipeOption = options[j];
				bool flag2 = j == selectedIndex;
				Color val2 = (Color)(ultimateRecipeOption.isReady ? ultimateRecipeOption.accentColor : new Color(0.36f, 0.4f, 0.58f, 1f));
				Color val3 = Color.Lerp(val2, new Color(0.08f, 0.07f, 0.24f, 1f), flag2 ? 0.36f : 0.76f);
				Graphic targetGraphic = ((Selectable)val).targetGraphic;
				if ((Object)(object)targetGraphic != (Object)null)
				{
					targetGraphic.color = val3;
				}
				ColorBlock colors = ((Selectable)val).colors;
				((ColorBlock)(ref colors)).normalColor = val3;
				((ColorBlock)(ref colors)).highlightedColor = Color.Lerp(val3, Color.white, 0.16f);
				((ColorBlock)(ref colors)).selectedColor = Color.Lerp(val3, Color.white, 0.22f);
				((Selectable)val).colors = colors;
				if (j < optionLabels.Length && (Object)(object)optionLabels[j] != (Object)null)
				{
					string text = (ultimateRecipeOption.isReady ? "READY" : (ultimateRecipeOption.progress + "/" + ultimateRecipeOption.required));
					optionLabels[j].text = (flag2 ? "▶ " : string.Empty) + "[" + text + "] " + ultimateRecipeOption.displayName + "\n결과  " + Compact(ultimateRecipeOption.resultSummary, 32) + "\n" + (ultimateRecipeOption.isReady ? ("소모  " + Compact(ultimateRecipeOption.materialSummary, 46)) : ("부족  " + Compact(ultimateRecipeOption.missingSummary, 46)));
					((Graphic)optionLabels[j]).color = (Color)(flag2 ? new Color(1f, 0.94f, 0.58f) : Color.white);
				}
			}
		}
		bool flag3 = selectedIndex >= 0 && selectedIndex < num && options[selectedIndex].isReady;
		if ((Object)(object)confirmButton != (Object)null)
		{
			((Selectable)confirmButton).interactable = flag3;
		}
		if ((Object)(object)confirmLabel != (Object)null)
		{
			confirmLabel.text = (flag3 ? "선택한 초월 실행" : "레시피를 선택하세요");
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
		if ((Object)(object)gameController != (Object)null)
		{
			gameController.SetUltimateRecipePreview(null);
		}
	}
}
