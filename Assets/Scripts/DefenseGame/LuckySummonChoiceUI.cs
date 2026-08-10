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

	public sealed class BossForecastBetUI : MonoBehaviour
	{
		private DefenseGameController gameController;
		private CanvasGroup canvasGroup;
		private Text instructionText;
		private Button[] choiceButtons = new Button[0];
		private Text[] choiceLabels = new Text[0];

		public void Configure(DefenseGameController controller, CanvasGroup group, Text instruction, Button[] buttons, Text[] labels)
		{
			Unsubscribe();
			gameController = controller;
			canvasGroup = group;
			instructionText = instruction;
			choiceButtons = buttons ?? new Button[0];
			choiceLabels = labels ?? new Text[0];

			for (int i = 0; i < choiceButtons.Length; i++)
			{
				int index = i;
				if (choiceButtons[i] != null)
				{
					choiceButtons[i].onClick.RemoveAllListeners();
					choiceButtons[i].onClick.AddListener(() => Choose((BossForecastBet)(index + 1)));
				}
			}

			if (gameController != null)
			{
				gameController.OnBossForecastBetRequested += Open;
				gameController.OnStateChanged += HandleStateChanged;
			}
		}

		private void OnDestroy()
		{
			Unsubscribe();
		}

		private void Unsubscribe()
		{
			if (gameController == null)
			{
				return;
			}

			gameController.OnBossForecastBetRequested -= Open;
			gameController.OnStateChanged -= HandleStateChanged;
		}

		private void Open()
		{
			if (gameController == null || !gameController.CanChooseBossForecastBet)
			{
				return;
			}

			gameObject.SetActive(true);
			transform.SetAsLastSibling();
			if (canvasGroup != null)
			{
				canvasGroup.alpha = 1f;
				canvasGroup.interactable = true;
				canvasGroup.blocksRaycasts = true;
			}

			Refresh();
		}

		private void Choose(BossForecastBet choice)
		{
			if (gameController != null && gameController.TryChooseBossForecastBet(choice))
			{
				gameObject.SetActive(false);
			}
		}

		private void HandleStateChanged()
		{
			if (!gameObject.activeSelf)
			{
				return;
			}

			if (gameController == null || !gameController.CanChooseBossForecastBet)
			{
				gameObject.SetActive(false);
				return;
			}

			Refresh();
		}

		private void Refresh()
		{
			if (gameController == null)
			{
				return;
			}

			if (instructionText != null)
			{
				instructionText.text = "\u0052\u0031\u0030\uae4c\uc9c0\u0020\ub2ec\uc131\ud560\u0020\ubaa9\ud45c\ub97c\u0020\ud558\ub098\u0020\uace0\ub974\uc138\uc694\u002e\u0020\uc120\ud0dd\ud55c\u0020\uacf5\ub7b5\uc740\u0020\ub2e4\uc74c\u0020\uc804\ud22c\uc0c1\uc810\uc5d0\ub3c4\u0020\u0031\ud68c\u0020\uc601\ud5a5\uc744\u0020\uc90d\ub2c8\ub2e4\u002e";
			}
			SetChoice(0, "\ubcf4\uae09\u0020\uacf5\ub7b5\n\n\uc989\uc2dc\u003a\u0020\ub808\uc5b4\u0020\uc720\ub2db\u0020\u0031\uae30\u0020\u002b\u0020\u0031\u0030\u0047\n\ubaa9\ud45c\u003a\u0020\u0052\u0031\u0030\u0020\uc720\ub2db\u0020\u0038\uae30\u0020\uc774\uc0c1\n\uc131\uacf5\u003a\u0020\u002b\u0034\u0035\uc810\u002c\u0020\u002b\u0031\u0038\u0047\n\ub2e4\uc74c\u0020\uc804\ud22c\uc0c1\uc810\u003a\u0020\ubcf4\uae09\u0020\uc120\ud0dd\uc9c0\u0020\u0031\ud68c\u0020\uc6b0\ub300");
			SetChoice(1, "\ube4c\ub4dc\u0020\uacf5\ub7b5\n\n\uc989\uc2dc\u003a\u0020\ud589\uc6b4\u0020\uc18c\ud658\uae4c\uc9c0\u0020\ub0a8\uc740\u0020\uc77c\ubc18\u0020\uc18c\ud658\u0020\ud69f\uc218\u0020\u002d\u0033\n\ubaa9\ud45c\u003a\u0020\u0052\u0031\u0030\u0020\uc5d0\ud53d\u0020\uc774\uc0c1\u0020\u0031\uae30\n\uc131\uacf5\u003a\u0020\u002b\u0034\u0035\uc810\u002c\u0020\u002b\u0031\u0038\u0047\n\ub2e4\uc74c\u0020\uc804\ud22c\uc0c1\uc810\u003a\u0020\ube4c\ub4dc\u0020\uc120\ud0dd\uc9c0\u0020\u0031\ud68c\u0020\uc6b0\ub300");
			SetChoice(2, "\uc804\uc220\u0020\uacf5\ub7b5\n\n\uc989\uc2dc\u003a\u0020\ucd5c\ub300\u0020\u0048\u0050\u0020\u002b\u0031\n\ubaa9\ud45c\u003a\u0020\u0052\u0031\u0030\u0020\uc885\ub8cc\u0020\ud6c4\u0020\u0048\u0050\u0020\u0036\u0030\u0025\u0020\uc774\uc0c1\n\uc131\uacf5\u003a\u0020\u002b\u0034\u0035\uc810\u002c\u0020\u002b\u0031\u0038\u0047\n\ub2e4\uc74c\u0020\uc804\ud22c\uc0c1\uc810\u003a\u0020\uc804\uc220\u0020\uc120\ud0dd\uc9c0\u0020\u0031\ud68c\u0020\uc6b0\ub300");
		}

		private void SetChoice(int index, string label)
		{
			if (index < 0 || index >= choiceButtons.Length)
			{
				return;
			}

			if (choiceButtons[index] != null)
			{
				choiceButtons[index].interactable = true;
			}

			if (index < choiceLabels.Length && choiceLabels[index] != null)
			{
				choiceLabels[index].text = label;
			}
		}
	}
}
