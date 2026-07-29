using UnityEngine;

namespace DefenseGame;

public class DemoInputController : MonoBehaviour
{
	[SerializeField]
	private DefenseGameController gameController;

	public void Configure(DefenseGameController controller)
	{
		gameController = controller;
	}

	private void Update()
	{
		if (!((Object)(object)gameController == (Object)null))
		{
			if (Input.GetKeyDown((KeyCode)32))
			{
				gameController.StartRound();
			}
			if (Input.GetKeyDown((KeyCode)115))
			{
				gameController.TrySummon();
			}
			if (Input.GetKeyDown((KeyCode)49))
			{
				gameController.TryMerge(CharacterGrade.Normal);
			}
			if (Input.GetKeyDown((KeyCode)50))
			{
				gameController.TryMerge(CharacterGrade.Rare);
			}
			if (Input.GetKeyDown((KeyCode)51))
			{
				gameController.TryMerge(CharacterGrade.Epic);
			}
			if (Input.GetKeyDown((KeyCode)52))
			{
				gameController.TryMerge(CharacterGrade.Legendary);
			}
			if (Input.GetKeyDown((KeyCode)53))
			{
				gameController.TryMerge(CharacterGrade.Mythic);
			}
			if (Input.GetKeyDown((KeyCode)99))
			{
				gameController.AddCharacterContent(5);
			}
			if (Input.GetKeyDown((KeyCode)109))
			{
				gameController.AddMonsterContent(3);
			}
		}
	}
}
