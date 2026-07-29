using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
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
			RefreshSlots();
			if (cachedSlots.Count == 0)
			{
				return;
			}
			for (int i = 0; i < cachedSlots.Count; i++)
			{
				if (cachedSlots[i] != null)
				{
					cachedSlots[i].ClearTileModifier();
				}
			}
			int round = ((gameController != null) ? gameController.CurrentRound : 0);
			int tileCount = ((round >= 12) ? lateTileCount : ((round >= 6) ? midTileCount : earlyTileCount));
			tileCount = Mathf.Clamp(tileCount, 1, cachedSlots.Count);
			List<BoardSlot> pool = new List<BoardSlot>(cachedSlots);
			bool bossPreparationRound = IsBossPreparationRound(round);
			bool bossHunterPlaced = false;
			for (int j = 0; j < tileCount; j++)
			{
				if (pool.Count <= 0)
				{
					break;
				}
				int index = Random.Range(0, pool.Count);
				BoardSlot slot = pool[index];
				pool.RemoveAt(index);
				BoardTileModifierType type = RollTileType(round, j, bossPreparationRound && !bossHunterPlaced);
				if (type == BoardTileModifierType.BossHunter)
				{
					bossHunterPlaced = true;
				}
				slot.SetTileModifier(type, ResolveTileColor(type), ResolveTileLabel(type));
			}
			if (!string.IsNullOrWhiteSpace(bannerLabel) && gameController != null)
			{
				string bossHint = (bossHunterPlaced ? "  보스 타일 포함" : string.Empty);
				gameController.RequestBanner(bannerLabel + "  +" + tileCount + "개" + bossHint, new Color(0.35f, 0.92f, 1f), bossHunterPlaced ? 2.6f : 2.2f);
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
			if (!subscribed && !(gameController == null))
			{
				gameController.OnRoundBoardPreparation += HandleRoundBoardPreparation;
				subscribed = true;
			}
		}

		private void Unsubscribe()
		{
			if (subscribed && !(gameController == null))
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
			if (boardManager == null || boardManager.Slots == null)
			{
				return;
			}
			for (int i = 0; i < boardManager.Slots.Count; i++)
			{
				BoardSlot slot = boardManager.Slots[i];
				if (slot != null)
				{
					cachedSlots.Add(slot);
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
				BoardTileModifierType[] early = new BoardTileModifierType[5]
				{
					BoardTileModifierType.AttackSpeed,
					BoardTileModifierType.Mana,
					BoardTileModifierType.Guard,
					BoardTileModifierType.AttackPower,
					BoardTileModifierType.Range
				};
				return early[(Random.Range(0, early.Length) + index) % early.Length];
			}
			if (round <= 9)
			{
				BoardTileModifierType[] mid = ((round < 8) ? new BoardTileModifierType[8]
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
				return mid[(Random.Range(0, mid.Length) + index) % mid.Length];
			}
			BoardTileModifierType[] late = new BoardTileModifierType[10]
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
			return late[(Random.Range(0, late.Length) + index) % late.Length];
		}

		private bool IsBossPreparationRound(int completedRound)
		{
			if (!guaranteeBossTileBeforeBossRound || completedRound <= 0)
			{
				return false;
			}
			int nextRound = completedRound + 1;
			return nextRound > 0 && nextRound % 10 == 0;
		}

		private Color ResolveTileColor(BoardTileModifierType type)
		{
			return type switch
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
			};
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
}
