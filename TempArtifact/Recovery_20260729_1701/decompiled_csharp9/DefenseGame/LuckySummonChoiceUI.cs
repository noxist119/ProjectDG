using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame
{
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
				if (!((Object)(object)choiceButtons[i] == null))
				{
					((UnityEventBase)(object)choiceButtons[i].onClick).RemoveAllListeners();
					((UnityEvent)(object)choiceButtons[i].onClick).AddListener((UnityAction)delegate
					{
						Choose((LuckySummonChoice)choiceIndex);
					});
				}
			}
			if ((Object)(object)closeButton != null)
			{
				((UnityEventBase)(object)closeButton.onClick).RemoveAllListeners();
				((UnityEvent)(object)closeButton.onClick).AddListener((UnityAction)Defer);
			}
			if (gameController != null)
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
			if (!(gameController == null))
			{
				gameController.OnLuckySummonChoiceRequested -= Open;
				gameController.OnStateChanged -= HandleStateChanged;
			}
		}

		private void Open()
		{
			if (!(gameController == null) && gameController.LuckySummonReady)
			{
				base.gameObject.SetActive(value: true);
				base.transform.SetAsLastSibling();
				if (canvasGroup != null)
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
			base.gameObject.SetActive(value: false);
		}

		private void Choose(LuckySummonChoice choice)
		{
			if (gameController != null && gameController.TryResolveLuckySummonChoice(choice))
			{
				base.gameObject.SetActive(value: false);
			}
			else
			{
				Refresh();
			}
		}

		private void HandleStateChanged()
		{
			if (base.gameObject.activeSelf)
			{
				if (gameController == null || !gameController.LuckySummonChoiceOpen)
				{
					base.gameObject.SetActive(value: false);
				}
				else
				{
					Refresh();
				}
			}
		}

		private void Refresh()
		{
			if (!(gameController == null))
			{
				if ((Object)(object)titleText != null)
				{
					titleText.text = "불운을 뒤집는 행운 소환";
				}
				if ((Object)(object)instructionText != null)
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
			if (index >= 0 && index < choiceButtons.Length)
			{
				int cost = gameController.GetLuckySummonChoiceCost(choice);
				bool canChoose = gameController.CanChooseLuckySummon(choice);
				if ((Object)(object)choiceButtons[index] != null)
				{
					((Selectable)choiceButtons[index]).interactable = canChoose;
				}
				if (index < choiceLabels.Length && (Object)(object)choiceLabels[index] != null)
				{
					choiceLabels[index].text = description + cost + "G" + (canChoose ? string.Empty : "\n골드 부족");
					((Graphic)choiceLabels[index]).color = (canChoose ? Color.white : new Color(0.68f, 0.72f, 0.78f));
				}
			}
		}

		private void Update()
		{
			if (!base.gameObject.activeSelf || choiceButtons == null)
			{
				return;
			}
			float pulse = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
			for (int i = 0; i < choiceButtons.Length; i++)
			{
				Button button = choiceButtons[i];
				Outline outline = (((Object)(object)button != null) ? ((Component)(object)button).GetComponent<Outline>() : null);
				if (!((Object)(object)outline == null))
				{
					((Shadow)outline).effectColor = (((Selectable)button).interactable ? Color.Lerp(new Color(0.56f, 0.76f, 0.34f, 0.62f), new Color(0.94f, 1f, 0.62f, 0.94f), pulse) : Color.clear);
				}
			}
		}
	}
}
