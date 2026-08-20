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
					titleText.text = "\uc5f0\uc18d \uc77c\ubc18 \ubcf4\uc0c1! \ud2b9\ubcc4 \uc18c\ud658";
				}
				if ((Object)(object)instructionText != null)
				{
					instructionText.text = "\uc77c\ubc18 \ub4f1\uae09\uc774 \uc5f0\uc18d\uc73c\ub85c \ub098\uc640 \ud2b9\ubcc4 \uc18c\ud658\uc774 \uc5f4\ub838\uc2b5\ub2c8\ub2e4.\n\ud604\uc7ac " + gameController.LuckySummonNormalStreak + "\ud68c \uc5f0\uc18d \u00b7 \uc544\ub798 1\uac1c\ub97c \uace0\ub974\uc138\uc694. \u00b7 \ud55c \ud310 1\ud68c";
				}
				SetChoice(0, LuckySummonChoice.MergeLink, "\ud569\uc131 \uc7ac\ub8cc \ubcf4\ucda9\n\n\ud604\uc7ac \ubcf4\ub4dc\uc5d0\uc11c\n\ud569\uc131\uc5d0 \uac00\uc7a5 \uac00\uae4c\uc6b4 \uc720\ub2db 1\uae30\n\n");
				SetChoice(1, LuckySummonChoice.SafeRare, "\ub808\uc5b4 \uc774\uc0c1 \ud655\uc815\n\n\ub808\uc5b4 \uc774\uc0c1 \uc720\ub2db\n1\uae30 \ud655\uc815 \uc18c\ud658\n\n");
				SetChoice(2, LuckySummonChoice.Jackpot, "\uc5d0\ud53d 25% \ub3c4\uc804\n\n\uc131\uacf5: \uc5d0\ud53d\n\uc2e4\ud328: \uc77c\ubc18 + \uc0ac\uc6a9 \uace8\ub4dc 50% \ud658\uae09\n\n");
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
			if (gameController == null)
			{
				if (gameObject.activeSelf)
				{
					gameObject.SetActive(false);
				}
				return;
			}

			if (gameController.CanChooseBossForecastBet)
			{
				if (!gameObject.activeSelf)
				{
					Open();
				}
				else
				{
					Refresh();
				}
				return;
			}

			if (gameObject.activeSelf)
			{
				gameObject.SetActive(false);
			}
		}

		private void Refresh()
		{
			if (gameController == null)
			{
				return;
			}

			if (instructionText != null)
			{
				instructionText.text = "R10\uc744 \uc5b4\ub5bb\uac8c \uc900\ube44\ud560\uae4c\uc694?\n\uc9c0\uae08 \ubcf4\ub108\uc2a4\ub97c \ud558\ub098 \ubc1b\uace0, \ubaa9\ud45c\uae4c\uc9c0 \ub2ec\uc131\ud558\uba74 \ucd94\uac00 \ubcf4\uc0c1\uc744 \ubc1b\uc2b5\ub2c8\ub2e4.";
			}

			SetChoice(0, BuildChoiceLabel(BossForecastBet.Supply));
			SetChoice(1, BuildChoiceLabel(BossForecastBet.Build));
			SetChoice(2, BuildChoiceLabel(BossForecastBet.Tactical));
		}

		private string BuildChoiceLabel(BossForecastBet choice)
		{
			bool overdrive = gameController != null && gameController.IsOverdriveMode;
			string title;
			string immediate;
			string goal;
			string nextShop;
			switch (choice)
			{
				case BossForecastBet.Supply:
					title = "\uc720\ub2db \ud655\ubcf4";
					immediate = overdrive ? "\ub808\uc5b4 \uc720\ub2db 1\uae30 + 10G" : "\ub2e4\uc74c \uc0c1\uc810 \ubcf4\uae09 \uc635\uc158 \uc6b0\uc120";
					goal = "\uc720\ub2db 8\uae30 \ud655\ubcf4";
					nextShop = "\ubcf4\uae09 \uc635\uc158 \uc6b0\uc120";
					break;
				case BossForecastBet.Build:
					title = "\uace0\ub4f1\uae09 \ub178\ub9ac\uae30";
					immediate = overdrive ? "\ud589\uc6b4 \uc18c\ud658 3\ud68c \ub2f9\uae40" : "\ub2e4\uc74c \uc0c1\uc810 \uc131\uc7a5 \uc635\uc158 \uc6b0\uc120";
					goal = "\uc5d0\ud53d \uc774\uc0c1 1\uae30";
					nextShop = "\uc131\uc7a5 \uc635\uc158 \uc6b0\uc120";
					break;
				default:
					title = "\uc548\uc804\ud558\uac8c \ubc84\ud2f0\uae30";
					immediate = overdrive ? "\ucd5c\ub300 HP +1" : "\ub2e4\uc74c \uc0c1\uc810 \uc0dd\uc874 \uc635\uc158 \uc6b0\uc120";
					goal = "\ud074\ub9ac\uc5b4 \ud6c4 HP 60% \uc774\uc0c1";
					nextShop = "\uc0dd\uc874 \uc635\uc158 \uc6b0\uc120";
					break;
			}

			return title + "\n\n\uc9c0\uae08\n" + immediate + "\n\nR10 \ubaa9\ud45c\n" + goal +
				"\n\n\ub2ec\uc131 \ubcf4\uc0c1\n18G + \uc810\uc218 45\n\n\ub2e4\uc74c \uc0c1\uc810\n" + nextShop;
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
