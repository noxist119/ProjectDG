using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    public class BoardTileModifierSystem : MonoBehaviour
    {
        [SerializeField] private DefenseGameController gameController;
        [SerializeField] private DefenseBoardManager boardManager;
        [SerializeField] private int rerollInterval = 3;
        [SerializeField] private int earlyTileCount = 2;
        [SerializeField] private int midTileCount = 3;
        [SerializeField] private int lateTileCount = 4;
        [SerializeField] private bool guaranteeBossTileBeforeBossRound = true;

        private readonly List<BoardSlot> cachedSlots = new List<BoardSlot>();
        private bool subscribed;

        public void Configure(DefenseGameController controller, DefenseBoardManager board)
        {
            Unsubscribe();
            gameController = controller;
            boardManager = board;
            RefreshSlots();
            Subscribe();
            RerollTiles(true, "전술 타일 배치");
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

            int round = gameController != null ? gameController.CurrentRound : 0;
            int tileCount = round >= 12 ? lateTileCount : round >= 6 ? midTileCount : earlyTileCount;
            tileCount = Mathf.Clamp(tileCount, 1, cachedSlots.Count);

            List<BoardSlot> pool = new List<BoardSlot>(cachedSlots);
            bool bossPreparationRound = IsBossPreparationRound(round);
            bool bossHunterPlaced = false;
            for (int i = 0; i < tileCount && pool.Count > 0; i++)
            {
                int index = ContentRange(0, pool.Count, "board.tile.slot");
                BoardSlot slot = pool[index];
                pool.RemoveAt(index);

                BoardTileModifierType type = RollTileType(round, i, bossPreparationRound && !bossHunterPlaced);
                if (type == BoardTileModifierType.BossHunter)
                {
                    bossHunterPlaced = true;
                }

                slot.SetTileModifier(type, ResolveTileColor(type), ResolveTileLabel(type));
            }

            if (!string.IsNullOrWhiteSpace(bannerLabel) && gameController != null)
            {
                string bossHint = bossHunterPlaced ? "  보스 타일 포함" : string.Empty;
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
            if (subscribed || gameController == null)
            {
                return;
            }

            gameController.OnRoundBoardPreparation += HandleRoundBoardPreparation;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || gameController == null)
            {
                return;
            }

            gameController.OnRoundBoardPreparation -= HandleRoundBoardPreparation;
            subscribed = false;
        }

        private void HandleRoundBoardPreparation(int round)
        {
            if (round <= 0 || rerollInterval <= 0 || round % rerollInterval != 0)
            {
                return;
            }

            RerollTiles(false, "전술 타일 재배치");
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
                BoardTileModifierType[] early =
                {
                    BoardTileModifierType.AttackSpeed,
                    BoardTileModifierType.Mana,
                    BoardTileModifierType.Guard,
                    BoardTileModifierType.AttackPower,
                    BoardTileModifierType.Range
                };
                return early[(ContentRange(0, early.Length, "board.tile.early") + index) % early.Length];
            }

            if (round <= 9)
            {
                BoardTileModifierType[] mid = round >= 8
                    ? new[]
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
                    }
                    : new[]
                {
                    BoardTileModifierType.AttackSpeed,
                    BoardTileModifierType.Mana,
                    BoardTileModifierType.Guard,
                    BoardTileModifierType.Range,
                    BoardTileModifierType.Skill,
                    BoardTileModifierType.AttackPower,
                    BoardTileModifierType.LifeSteal,
                    BoardTileModifierType.Overload
                };
                return mid[(ContentRange(0, mid.Length, "board.tile.mid") + index) % mid.Length];
            }

            BoardTileModifierType[] late =
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
            return late[(ContentRange(0, late.Length, "board.tile.late") + index) % late.Length];
        }
        private int ContentRange(int minInclusive, int maxExclusive, string eventType)
        {
            return gameController != null
                ? gameController.RunContentRandom.Range(RunContentRandomChannel.Board, minInclusive, maxExclusive, eventType)
                : Random.Range(minInclusive, maxExclusive);
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
            switch (type)
            {
                case BoardTileModifierType.AttackSpeed: return new Color(0.20f, 1f, 0.86f, 0.72f);
                case BoardTileModifierType.Mana: return new Color(0.28f, 0.64f, 1f, 0.72f);
                case BoardTileModifierType.Guard: return new Color(0.36f, 1f, 0.50f, 0.72f);
                case BoardTileModifierType.Range: return new Color(0.72f, 0.92f, 1f, 0.70f);
                case BoardTileModifierType.Overload: return new Color(1f, 0.36f, 0.22f, 0.70f);
                case BoardTileModifierType.BossHunter: return new Color(1f, 0.72f, 0.18f, 0.76f);
                case BoardTileModifierType.Skill: return new Color(0.78f, 0.42f, 1f, 0.72f);
                case BoardTileModifierType.AttackPower: return new Color(1f, 0.46f, 0.30f, 0.72f);
                case BoardTileModifierType.LifeSteal: return new Color(1f, 0.28f, 0.52f, 0.72f);
                case BoardTileModifierType.AllStats: return new Color(1f, 0.86f, 0.26f, 0.78f);
                default: return Color.clear;
            }
        }

        private string ResolveTileLabel(BoardTileModifierType type)
        {
            switch (type)
            {
                case BoardTileModifierType.AttackSpeed: return "\uAC00\uC18D";
                case BoardTileModifierType.Mana: return "\uB9C8\uB098";
                case BoardTileModifierType.Guard: return "\uC218\uD638";
                case BoardTileModifierType.Range: return "\uC0AC\uAC70\uB9AC";
                case BoardTileModifierType.Overload: return "\uACFC\uBD80\uD558";
                case BoardTileModifierType.BossHunter: return "\uBCF4\uC2A4";
                case BoardTileModifierType.Skill: return "\uAE30\uC220";
                case BoardTileModifierType.AttackPower: return "\uACF5\uACA9";
                case BoardTileModifierType.LifeSteal: return "\uD53C\uD761";
                case BoardTileModifierType.AllStats: return "\uC804\uB2A5";
                default: return string.Empty;
            }
        }
    }
}
