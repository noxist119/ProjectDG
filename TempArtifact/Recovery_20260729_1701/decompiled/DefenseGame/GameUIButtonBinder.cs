using UnityEngine;

namespace DefenseGame;

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
		if ((Object)(object)gameController != (Object)null)
		{
			gameController.StartRound();
		}
	}

	public void OnClickSummon()
	{
		if ((Object)(object)gameController != (Object)null)
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
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gameController == (Object)null)
		{
			Debug.LogWarning((object)"Merge input ignored because DefenseGameController is not configured.");
			return;
		}
		string displayName = CharacterGradeUtility.GetDisplayName(grade);
		if (gameController.IsCombatInteractionLocked)
		{
			gameController.RequestBanner("전투 중에는 합성할 수 없습니다. 운명 카드의 전투 편집 효과가 필요합니다.", new Color(1f, 0.66f, 0.24f), 2f);
			return;
		}
		int num = gameController.CountUnitsOfGrade(grade);
		if (grade != CharacterGrade.Mythic && num < 3)
		{
			gameController.RequestBanner(displayName + " 합성 재료 부족  " + num + "/3", new Color(1f, 0.66f, 0.24f), 1.8f);
		}
		else if (!gameController.TryMerge(grade))
		{
			string message = (string.IsNullOrWhiteSpace(gameController.LastMergeFailureReason) ? (displayName + " 합성을 완료하지 못했습니다.") : gameController.LastMergeFailureReason);
			gameController.RequestBanner(message, new Color(1f, 0.42f, 0.3f), 2f);
		}
	}

	public void OnClickAddCharacters()
	{
		if ((Object)(object)gameController != (Object)null)
		{
			gameController.AddCharacterContent(5);
		}
	}

	public void OnClickAddMonsters()
	{
		if ((Object)(object)gameController != (Object)null)
		{
			gameController.AddMonsterContent(3);
		}
	}
}
