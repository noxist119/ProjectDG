using UnityEngine;

namespace DefenseGame
{
	public class GameUIButtonBinder : MonoBehaviour
	{
		[SerializeField]
		private DefenseGameController gameController;

		public void Configure(DefenseGameController controller)
		{
			gameController = controller;
		}

		public void OnClickStartRound()
		{
			if (gameController != null)
			{
				gameController.StartRound();
			}
		}

		public void OnClickSummon()
		{
			if (gameController != null)
			{
				gameController.TrySummon();
			}
		}

		public void OnClickMergeNormal()
		{
			TryMergeWithFeedback(CharacterGrade.Normal);
		}

		public void OnClickMergeRare()
		{
			TryMergeWithFeedback(CharacterGrade.Rare);
		}

		public void OnClickMergeEpic()
		{
			TryMergeWithFeedback(CharacterGrade.Epic);
		}

		public void OnClickMergeLegendary()
		{
			TryMergeWithFeedback(CharacterGrade.Legendary);
		}

		public void OnClickMergeMythic()
		{
			TryMergeWithFeedback(CharacterGrade.Mythic);
		}

		private void TryMergeWithFeedback(CharacterGrade grade)
		{
			if (gameController == null)
			{
				Debug.LogWarning("Merge input ignored because DefenseGameController is not configured.");
				return;
			}
			string gradeName = CharacterGradeUtility.GetDisplayName(grade);
			if (gameController.IsCombatInteractionLocked)
			{
				gameController.RequestBanner("전투 중에는 합성할 수 없습니다. 운명 카드의 전투 편집 효과가 필요합니다.", new Color(1f, 0.66f, 0.24f), 2f);
				return;
			}
			int ownedCount = gameController.CountUnitsOfGrade(grade);
			if (grade != CharacterGrade.Mythic && ownedCount < 3)
			{
				gameController.RequestBanner(gradeName + " 합성 재료 부족  " + ownedCount + "/3", new Color(1f, 0.66f, 0.24f), 1.8f);
			}
			else if (!gameController.TryMerge(grade))
			{
				string reason = (string.IsNullOrWhiteSpace(gameController.LastMergeFailureReason) ? (gradeName + " 합성을 완료하지 못했습니다.") : gameController.LastMergeFailureReason);
				gameController.RequestBanner(reason, new Color(1f, 0.42f, 0.3f), 2f);
			}
		}

		public void OnClickAddCharacters()
		{
			if (gameController != null)
			{
				gameController.AddCharacterContent(5);
			}
		}

		public void OnClickAddMonsters()
		{
			if (gameController != null)
			{
				gameController.AddMonsterContent(3);
			}
		}
	}
}
