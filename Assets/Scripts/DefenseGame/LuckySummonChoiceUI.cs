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
				instructionText.text = gameController.DailyFateCupEnabled
					? gameController.DailyFateCupSummary + "\n첫 소형 상점이 선택한 방향으로 기울어집니다."
					: "첫 소형 상점이 선택한 방향으로 기울어집니다. R10 조건 성공 시 +45점, +18G";
			}

			string supplyBonus = gameController.IsOverdriveMode ? "폭주 시동 골드 +10\n" : string.Empty;
            string buildBonus = gameController.IsOverdriveMode ? "폭주 운 보호 +2칸\n" : string.Empty;
            string tacticalBonus = gameController.IsOverdriveMode ? "폭주 최대 HP +1\n" : string.Empty;
            SetChoice(0, "보급 예측\n\n" + supplyBonus + "R10 유닛 8기 이상\n첫 상점 보급 편향");
            SetChoice(1, "빌드 예측\n\n" + buildBonus + "R10 에픽+ 1기\n첫 상점 빌드 편향");
            SetChoice(2, "전술 예측\n\n" + tacticalBonus + "R10 HP 60% 이상\n첫 상점 전술 편향");
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
