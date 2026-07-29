using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

public sealed class LuckySummonChoiceUI : MonoBehaviour
{
	private DefenseGameController gameController;

	private CanvasGroup canvasGroup;

	private Text titleText;

	private Text instructionText;

	private Button[] choiceButtons = (Button[])(object)new Button[0];

	private Text[] choiceLabels = (Text[])(object)new Text[0];

	private Button closeButton;

	public void Configure(DefenseGameController controller, CanvasGroup group, Text title, Text instruction, Button[] buttons, Text[] labels, Button close)
	{
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		Unsubscribe();
		gameController = controller;
		canvasGroup = group;
		titleText = title;
		instructionText = instruction;
		choiceButtons = (Button[])(((object)buttons) ?? ((object)new Button[0]));
		choiceLabels = (Text[])(((object)labels) ?? ((object)new Text[0]));
		closeButton = close;
		for (int i = 0; i < choiceButtons.Length; i++)
		{
			int choiceIndex = i;
			if (!((Object)(object)choiceButtons[i] == (Object)null))
			{
				((UnityEventBase)choiceButtons[i].onClick).RemoveAllListeners();
				((UnityEvent)choiceButtons[i].onClick).AddListener((UnityAction)delegate
				{
					Choose((LuckySummonChoice)choiceIndex);
				});
			}
		}
		if ((Object)(object)closeButton != (Object)null)
		{
			((UnityEventBase)closeButton.onClick).RemoveAllListeners();
			((UnityEvent)closeButton.onClick).AddListener(new UnityAction(Defer));
		}
		if ((Object)(object)gameController != (Object)null)
		{
			gameController.OnLuckySummonChoiceRequested += Open;
			gameController.OnStateChanged += HandleStateChanged;
		}
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	private void Unsubscribe()
	{
		if (!((Object)(object)gameController == (Object)null))
		{
			gameController.OnLuckySummonChoiceRequested -= Open;
			gameController.OnStateChanged -= HandleStateChanged;
		}
	}

	private void Open()
	{
		if (!((Object)(object)gameController == (Object)null) && gameController.LuckySummonReady)
		{
			((Component)this).gameObject.SetActive(true);
			((Component)this).transform.SetAsLastSibling();
			if ((Object)(object)canvasGroup != (Object)null)
			{
				canvasGroup.alpha = 1f;
				canvasGroup.interactable = true;
				canvasGroup.blocksRaycasts = true;
			}
			Refresh();
		}
	}

	private void Defer()
	{
		gameController?.CancelLuckySummonChoice();
		((Component)this).gameObject.SetActive(false);
	}

	private void Choose(LuckySummonChoice choice)
	{
		if ((Object)(object)gameController != (Object)null && gameController.TryResolveLuckySummonChoice(choice))
		{
			((Component)this).gameObject.SetActive(false);
		}
		else
		{
			Refresh();
		}
	}

	private void HandleStateChanged()
	{
		if (((Component)this).gameObject.activeSelf)
		{
			if ((Object)(object)gameController == (Object)null || !gameController.LuckySummonChoiceOpen)
			{
				((Component)this).gameObject.SetActive(false);
			}
			else
			{
				Refresh();
			}
		}
	}

	private void Refresh()
	{
		if (!((Object)(object)gameController == (Object)null))
		{
			if ((Object)(object)titleText != (Object)null)
			{
				titleText.text = "불운을 뒤집는 행운 소환";
			}
			if ((Object)(object)instructionText != (Object)null)
			{
				instructionText.text = "일반 " + gameController.LuckySummonNormalStreak + "회 연속 누적  |  보유 " + gameController.Gold + "G  |  한 판 1회";
			}
			SetChoice(0, LuckySummonChoice.MergeLink, "연결의 주사위\n\n가장 가까운\n합성 재료 1기\n\n");
			SetChoice(1, LuckySummonChoice.SafeRare, "안전의 주사위\n\n레어 이상 확정\n소환비 150%\n\n");
			SetChoice(2, LuckySummonChoice.Jackpot, "승부의 주사위\n\n25% 에픽\n실패 시 일반 + 50% 환급\n\n");
		}
	}

	private void SetChoice(int index, LuckySummonChoice choice, string description)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		if (index >= 0 && index < choiceButtons.Length)
		{
			int luckySummonChoiceCost = gameController.GetLuckySummonChoiceCost(choice);
			bool flag = gameController.CanChooseLuckySummon(choice);
			if ((Object)(object)choiceButtons[index] != (Object)null)
			{
				((Selectable)choiceButtons[index]).interactable = flag;
			}
			if (index < choiceLabels.Length && (Object)(object)choiceLabels[index] != (Object)null)
			{
				choiceLabels[index].text = description + luckySummonChoiceCost + "G" + (flag ? string.Empty : "\n골드 부족");
				((Graphic)choiceLabels[index]).color = (Color)(flag ? Color.white : new Color(0.68f, 0.72f, 0.78f));
			}
		}
	}

	private void Update()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		if (!((Component)this).gameObject.activeSelf || choiceButtons == null)
		{
			return;
		}
		float num = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
		for (int i = 0; i < choiceButtons.Length; i++)
		{
			Button val = choiceButtons[i];
			Outline val2 = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Outline>() : null);
			if (!((Object)(object)val2 == (Object)null))
			{
				((Shadow)val2).effectColor = (((Selectable)val).interactable ? Color.Lerp(new Color(0.56f, 0.76f, 0.34f, 0.62f), new Color(0.94f, 1f, 0.62f, 0.94f), num) : Color.clear);
			}
		}
	}
}
