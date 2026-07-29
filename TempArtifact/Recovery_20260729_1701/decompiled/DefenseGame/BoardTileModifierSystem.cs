using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

public class BoardTileModifierSystem : MonoBehaviour
{
	[SerializeField]
	private DefenseGameController gameController;

	[SerializeField]
	private DefenseBoardManager boardManager;

	[SerializeField]
	private int rerollInterval = 3;

	[SerializeField]
	private int earlyTileCount = 2;

	[SerializeField]
	private int midTileCount = 3;

	[SerializeField]
	private int lateTileCount = 4;

	[SerializeField]
	private bool guaranteeBossTileBeforeBossRound = true;

	private readonly List<BoardSlot> cachedSlots = new List<BoardSlot>();

	private bool subscribed;

	public void Configure(DefenseGameController controller, DefenseBoardManager board)
	{
		Unsubscribe();
		gameController = controller;
		boardManager = board;
		RefreshSlots();
		Subscribe();
		RerollTiles(force: true, "전술 타일 배치");
	}

	public void RerollTiles(bool force = false, string bannerLabel = null)
	{
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		RefreshSlots();
		if (cachedSlots.Count == 0)
		{
			return;
		}
		for (int i = 0; i < cachedSlots.Count; i++)
		{
			if ((Object)(object)cachedSlots[i] != (Object)null)
			{
				cachedSlots[i].ClearTileModifier();
			}
		}
		int num = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0);
		int num2 = ((num >= 12) ? lateTileCount : ((num >= 6) ? midTileCount : earlyTileCount));
		num2 = Mathf.Clamp(num2, 1, cachedSlots.Count);
		List<BoardSlot> list = new List<BoardSlot>(cachedSlots);
		bool flag = IsBossPreparationRound(num);
		bool flag2 = false;
		for (int j = 0; j < num2; j++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			int index = Random.Range(0, list.Count);
			BoardSlot boardSlot = list[index];
			list.RemoveAt(index);
			BoardTileModifierType boardTileModifierType = RollTileType(num, j, flag && !flag2);
			if (boardTileModifierType == BoardTileModifierType.BossHunter)
			{
				flag2 = true;
			}
			boardSlot.SetTileModifier(boardTileModifierType, ResolveTileColor(boardTileModifierType), ResolveTileLabel(boardTileModifierType));
		}
		if (!string.IsNullOrWhiteSpace(bannerLabel) && (Object)(object)gameController != (Object)null)
		{
			string text = (flag2 ? "  보스 타일 포함" : string.Empty);
			gameController.RequestBanner(bannerLabel + "  +" + num2 + "개" + text, new Color(0.35f, 0.92f, 1f), flag2 ? 2.6f : 2.2f);
		}
	}

	private void OnEnable()
	{
		Subscribe();
	}

	private void OnDisable()
	{
		Unsubscribe();
	}

	private void Subscribe()
	{
		if (!subscribed && !((Object)(object)gameController == (Object)null))
		{
			gameController.OnRoundBoardPreparation += HandleRoundBoardPreparation;
			subscribed = true;
		}
	}

	private void Unsubscribe()
	{
		if (subscribed && !((Object)(object)gameController == (Object)null))
		{
			gameController.OnRoundBoardPreparation -= HandleRoundBoardPreparation;
			subscribed = false;
		}
	}

	private void HandleRoundBoardPreparation(int round)
	{
		if (round > 0 && rerollInterval > 0 && round % rerollInterval == 0)
		{
			RerollTiles(force: false, "전술 타일 재배치");
		}
	}

	private void RefreshSlots()
	{
		cachedSlots.Clear();
		if ((Object)(object)boardManager == (Object)null || boardManager.Slots == null)
		{
			return;
		}
		for (int i = 0; i < boardManager.Slots.Count; i++)
		{
			BoardSlot boardSlot = boardManager.Slots[i];
			if ((Object)(object)boardSlot != (Object)null)
			{
				cachedSlots.Add(boardSlot);
			}
		}
	}

	private BoardTileModifierType RollTileType(int round, int index, bool forceBossHunter)
	{
		if (forceBossHunter)
		{
			return BoardTileModifierType.BossHunter;
		}
		if (round <= 3)
		{
			BoardTileModifierType[] array = new BoardTileModifierType[5]
			{
				BoardTileModifierType.AttackSpeed,
				BoardTileModifierType.Mana,
				BoardTileModifierType.Guard,
				BoardTileModifierType.AttackPower,
				BoardTileModifierType.Range
			};
			return array[(Random.Range(0, array.Length) + index) % array.Length];
		}
		if (round <= 9)
		{
			BoardTileModifierType[] array2 = ((round < 8) ? new BoardTileModifierType[8]
			{
				BoardTileModifierType.AttackSpeed,
				BoardTileModifierType.Mana,
				BoardTileModifierType.Guard,
				BoardTileModifierType.Range,
				BoardTileModifierType.Skill,
				BoardTileModifierType.AttackPower,
				BoardTileModifierType.LifeSteal,
				BoardTileModifierType.Overload
			} : new BoardTileModifierType[10]
			{
				BoardTileModifierType.AttackSpeed,
				BoardTileModifierType.Mana,
				BoardTileModifierType.Guard,
				BoardTileModifierType.Range,
				BoardTileModifierType.Skill,
				BoardTileModifierType.AttackPower,
				BoardTileModifierType.LifeSteal,
				BoardTileModifierType.Overload,
				BoardTileModifierType.BossHunter,
				BoardTileModifierType.AllStats
			});
			return array2[(Random.Range(0, array2.Length) + index) % array2.Length];
		}
		BoardTileModifierType[] array3 = new BoardTileModifierType[10]
		{
			BoardTileModifierType.AttackSpeed,
			BoardTileModifierType.Mana,
			BoardTileModifierType.Guard,
			BoardTileModifierType.Range,
			BoardTileModifierType.Skill,
			BoardTileModifierType.AttackPower,
			BoardTileModifierType.LifeSteal,
			BoardTileModifierType.Overload,
			BoardTileModifierType.BossHunter,
			BoardTileModifierType.AllStats
		};
		return array3[(Random.Range(0, array3.Length) + index) % array3.Length];
	}

	private bool IsBossPreparationRound(int completedRound)
	{
		if (!guaranteeBossTileBeforeBossRound || completedRound <= 0)
		{
			return false;
		}
		int num = completedRound + 1;
		return num > 0 && num % 10 == 0;
	}

	private Color ResolveTileColor(BoardTileModifierType type)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		return (Color)(type switch
		{
			BoardTileModifierType.AttackSpeed => new Color(0.2f, 1f, 0.86f, 0.72f), 
			BoardTileModifierType.Mana => new Color(0.28f, 0.64f, 1f, 0.72f), 
			BoardTileModifierType.Guard => new Color(0.36f, 1f, 0.5f, 0.72f), 
			BoardTileModifierType.Range => new Color(0.72f, 0.92f, 1f, 0.7f), 
			BoardTileModifierType.Overload => new Color(1f, 0.36f, 0.22f, 0.7f), 
			BoardTileModifierType.BossHunter => new Color(1f, 0.72f, 0.18f, 0.76f), 
			BoardTileModifierType.Skill => new Color(0.78f, 0.42f, 1f, 0.72f), 
			BoardTileModifierType.AttackPower => new Color(1f, 0.46f, 0.3f, 0.72f), 
			BoardTileModifierType.LifeSteal => new Color(1f, 0.28f, 0.52f, 0.72f), 
			BoardTileModifierType.AllStats => new Color(1f, 0.86f, 0.26f, 0.78f), 
			_ => Color.clear, 
		});
	}

	private string ResolveTileLabel(BoardTileModifierType type)
	{
		return type switch
		{
			BoardTileModifierType.AttackSpeed => "가속", 
			BoardTileModifierType.Mana => "마나", 
			BoardTileModifierType.Guard => "수호", 
			BoardTileModifierType.Range => "사거리", 
			BoardTileModifierType.Overload => "과부하", 
			BoardTileModifierType.BossHunter => "보스", 
			BoardTileModifierType.Skill => "기술", 
			BoardTileModifierType.AttackPower => "공격", 
			BoardTileModifierType.LifeSteal => "피흡", 
			BoardTileModifierType.AllStats => "전능", 
			_ => string.Empty, 
		};
	}
}
