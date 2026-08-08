using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    public enum YahtzeeRewardType { Gold, Diamond, CharacterCard }

    [Serializable]
    public sealed class YahtzeeRewardResult
    {
        public YahtzeeRewardType rewardType;
        public int amount;
        public int multiplier;
        public string characterId;
        public string characterName;
        public CharacterGrade characterGrade;
    }

    [Serializable]
    public sealed class YahtzeeSaveData
    {
        public int tickets;
        public bool initialTicketsGranted;
        public List<int> chestMultipliers = new List<int>();
        public bool sessionActive;
        public int diceA, diceB, diceC;
        public int currentDieIndex;
        public bool heldA, heldB, heldC;
        public bool pendingA, pendingB, pendingC;
        public int holdCount;
        public int rerollCount;
        public int sessionGoldSpent;
    }

    public sealed class YahtzeeProgressionSystem : MonoBehaviour
    {
        private const string ServiceSaveKey = "DefenseGame.Yahtzee.Service.v1";
        private const string TestSaveKey = "DefenseGame.Yahtzee.Test.v1";
        public const int RerollGoldCost = 100;
        public const int FirstHoldDiamondCost = 20;
        public const int SecondHoldDiamondCost = 50;

        private readonly int[] dice = new int[3];
        private readonly bool[] held = new bool[3];
        private readonly bool[] pending = new bool[3];
        private OutgameProgressionSystem progression;
        private YahtzeeSaveData data;
        private OutgamePlayMode loadedMode;
        private bool hasLoadedMode;

        public event Action OnChanged;
        public int TicketCount { get { EnsureMode(); return data.tickets; } }
        public int ChestCount { get { EnsureMode(); return data.chestMultipliers.Count; } }
        public bool SessionActive { get { EnsureMode(); return data.sessionActive; } }
        public int HoldCount { get { EnsureMode(); return data.holdCount; } }
        public int RerollCount { get { EnsureMode(); return data.rerollCount; } }
        public int SessionGoldSpent { get { EnsureMode(); return data.sessionGoldSpent; } }
        public int CurrentDieIndex { get { EnsureMode(); return Mathf.Clamp(data.currentDieIndex, 0, 3); } }
        public int NextHoldCost => HoldCount <= 0 ? FirstHoldDiamondCost : SecondHoldDiamondCost;
        public int CurrentMultiplier
        {
            get
            {
                EnsureMode();
                return data.sessionActive && data.currentDieIndex >= 3 && dice[0] > 0 && dice[0] == dice[1] && dice[1] == dice[2] ? dice[0] : 1;
            }
        }

        public void Configure(OutgameProgressionSystem outgameProgression)
        {
            progression = outgameProgression;
            hasLoadedMode = false;
            EnsureMode();
        }

        public int GetDie(int index) { EnsureMode(); return index >= 0 && index < 3 ? dice[index] : 0; }
        public bool IsHeld(int index) { EnsureMode(); return index >= 0 && index < 3 && held[index]; }
        public bool IsPendingHold(int index) { EnsureMode(); return index >= 0 && index < 3 && pending[index]; }

        public bool TryStartSession(out string message)
        {
            EnsureMode();
            if (data.sessionActive) { message = "이미 진행 중인 얏찌가 있습니다."; return false; }
            if (data.tickets <= 0) { message = "얏찌 티켓이 부족합니다."; return false; }

            data.tickets--;
            data.sessionActive = true;
            data.holdCount = 0;
            data.rerollCount = 0;
            data.sessionGoldSpent = 0;
            for (int i = 0; i < 3; i++)
            {
                held[i] = false;
                pending[i] = false;
                dice[i] = 0;
            }
            data.currentDieIndex = 0;
            RollCurrentDie();

            SaveAndNotify();
            message = "첫 번째 주사위의 무료 굴림입니다. 원하는 숫자가 나올 때까지 재굴림하거나 홀드하세요.";
            return true;
        }

        public bool TogglePendingHold(int index, out string message)
        {
            EnsureMode();
            if (!data.sessionActive || index != data.currentDieIndex || index < 0 || index >= 3)
            {
                message = "현재 굴리고 있는 주사위만 선택할 수 있습니다.";
                return false;
            }

            return TryReroll(out message);
        }

        public bool TryCommitHold(out string message)
        {
            EnsureMode();
            if (!data.sessionActive) { message = "먼저 얏찌를 시작하세요."; return false; }
            if (data.currentDieIndex >= 3) { message = "세 주사위가 모두 확정되었습니다. 결과를 확정하세요."; return false; }
            if (data.holdCount >= 2) { message = "홀드는 한 판에 두 번까지 가능합니다."; return false; }

            int cost = data.holdCount == 0 ? FirstHoldDiamondCost : SecondHoldDiamondCost;
            if (progression == null || !progression.TrySpendDiamonds(cost))
            {
                message = "다이아가 부족합니다. 현재 주사위 상태는 유지됩니다.";
                return false;
            }

            held[data.currentDieIndex] = true;
            data.holdCount++;
            AdvanceCurrentDie();
            SaveAndNotify();
            message = data.currentDieIndex < 3
                ? (data.holdCount == 1 ? "1차 홀드 완료. 다음 주사위의 무료 굴림입니다." : "2차 홀드 완료. 다음 주사위의 무료 굴림입니다.")
                : "2차 홀드 완료. 세 주사위가 확정되었습니다.";
            return true;
        }

        public bool TryReroll(out string message)
        {
            EnsureMode();
            if (!data.sessionActive) { message = "먼저 얏찌를 시작하세요."; return false; }
            if (data.currentDieIndex >= 3) { message = "세 주사위가 모두 확정되었습니다. 결과를 확정하세요."; return false; }
            if (progression == null || !progression.TrySpendGold(RerollGoldCost))
            {
                message = "골드가 부족합니다. 현재 주사위 상태는 유지됩니다.";
                return false;
            }

            RollCurrentDie();
            data.rerollCount++;
            data.sessionGoldSpent += RerollGoldCost;
            SaveAndNotify();
            message = "주사위 " + (data.currentDieIndex + 1) + " 재굴림 완료. 원하는 숫자가 나올 때까지 다시 굴릴 수 있습니다.";
            return true;
        }

        public bool TryAdvanceDie(out string message)
        {
            EnsureMode();
            if (!data.sessionActive) { message = "먼저 얏찌를 시작하세요."; return false; }
            if (data.currentDieIndex >= 3) { message = "세 주사위가 모두 확정되었습니다. 결과를 확정하세요."; return false; }

            AdvanceCurrentDie();
            SaveAndNotify();
            message = data.currentDieIndex < 3
                ? "다음 주사위의 무료 굴림입니다."
                : "세 주사위가 확정되었습니다. 결과를 확인하세요.";
            return true;
        }

        public bool TryConfirmResult(out int multiplier, out string message)
        {
            EnsureMode();
            multiplier = 1;
            if (!data.sessionActive) { message = "확정할 얏찌 결과가 없습니다."; return false; }
            if (data.currentDieIndex < 3) { message = "세 번째 주사위까지 굴린 뒤 결과를 확정하세요."; return false; }
            multiplier = CurrentMultiplier;
            data.chestMultipliers.Add(multiplier);
            ResetSession();
            SaveAndNotify();
            message = "x" + multiplier + " 보상 상자를 보관함에 넣었습니다.";
            return true;
        }

        public bool TryOpenChests(int requestedCount, out List<YahtzeeRewardResult> rewards, out string message)
        {
            EnsureMode();
            rewards = new List<YahtzeeRewardResult>();
            if (data.chestMultipliers.Count == 0) { message = "열 수 있는 얏찌 상자가 없습니다."; return false; }
            int count = requestedCount <= 0 ? data.chestMultipliers.Count : Mathf.Min(requestedCount, data.chestMultipliers.Count);
            for (int i = 0; i < count; i++)
            {
                int multiplier = Mathf.Clamp(data.chestMultipliers[0], 1, 6);
                data.chestMultipliers.RemoveAt(0);
                rewards.Add(GrantReward(multiplier));
            }
            SaveAndNotify();
            message = count + "개 상자를 열었습니다.";
            return true;
        }

        public bool GrantTestTickets(int amount)
        {
            EnsureMode();
            if (progression == null || !progression.IsTestMode || amount <= 0) return false;
            data.tickets += amount;
            SaveAndNotify();
            return true;
        }

        private YahtzeeRewardResult GrantReward(int multiplier)
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.70f)
            {
                int amount = 150 * multiplier;
                progression?.AddGold(amount);
                return new YahtzeeRewardResult { rewardType = YahtzeeRewardType.Gold, amount = amount, multiplier = multiplier };
            }
            if (roll < 0.92f)
            {
                int amount = 4 * multiplier;
                progression?.AddDiamonds(amount);
                return new YahtzeeRewardResult { rewardType = YahtzeeRewardType.Diamond, amount = amount, multiplier = multiplier };
            }

            int cardCount = Mathf.Max(1, multiplier);
            List<OutgameDrawResult> draws = null;
            if (progression != null) progression.GrantYahtzeeCards(cardCount, out draws);
            OutgameDrawResult featured = draws != null && draws.Count > 0 ? draws[0] : null;
            return new YahtzeeRewardResult
            {
                rewardType = YahtzeeRewardType.CharacterCard,
                amount = cardCount,
                multiplier = multiplier,
                characterId = featured != null && featured.character != null ? featured.character.id : string.Empty,
                characterName = featured != null && featured.character != null ? featured.character.displayName : "영웅 카드",
                characterGrade = featured != null && featured.character != null ? featured.character.grade : CharacterGrade.Normal
            };
        }

        private void EnsureMode()
        {
            OutgamePlayMode mode = progression != null ? progression.CurrentPlayMode : OutgamePlayMode.Test;
            if (data != null && hasLoadedMode && loadedMode == mode) return;
            loadedMode = mode;
            hasLoadedMode = true;
            string json = PlayerPrefs.GetString(ResolveSaveKey(mode), string.Empty);
            data = string.IsNullOrWhiteSpace(json) ? new YahtzeeSaveData() : JsonUtility.FromJson<YahtzeeSaveData>(json);
            if (data == null) data = new YahtzeeSaveData();
            if (data.chestMultipliers == null) data.chestMultipliers = new List<int>();
            if (!data.initialTicketsGranted)
            {
                data.initialTicketsGranted = true;
                data.tickets = mode == OutgamePlayMode.Test ? 10 : Mathf.Max(0, data.tickets);
            }
            ReadArrays();
            Save();
        }

        private void ResetSession()
        {
            data.sessionActive = false;
            data.holdCount = 0;
            data.rerollCount = 0;
            data.sessionGoldSpent = 0;
            data.currentDieIndex = 0;
            for (int i = 0; i < 3; i++) { dice[i] = 0; held[i] = false; pending[i] = false; }
        }

        private void SaveAndNotify() { Save(); OnChanged?.Invoke(); }
        private void Save()
        {
            WriteArrays();
            PlayerPrefs.SetString(ResolveSaveKey(loadedMode), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void ReadArrays()
        {
            dice[0] = Mathf.Clamp(data.diceA, 0, 6);
            dice[1] = Mathf.Clamp(data.diceB, 0, 6);
            dice[2] = Mathf.Clamp(data.diceC, 0, 6);
            held[0] = data.heldA; held[1] = data.heldB; held[2] = data.heldC;
            pending[0] = data.pendingA; pending[1] = data.pendingB; pending[2] = data.pendingC;
        }

        private void WriteArrays()
        {
            data.diceA = dice[0]; data.diceB = dice[1]; data.diceC = dice[2];
            data.heldA = held[0]; data.heldB = held[1]; data.heldC = held[2];
            data.pendingA = pending[0]; data.pendingB = pending[1]; data.pendingC = pending[2];
        }

        private void RollCurrentDie()
        {
            int index = Mathf.Clamp(data.currentDieIndex, 0, 2);
            dice[index] = UnityEngine.Random.Range(1, 7);
            pending[index] = false;
        }

        private void AdvanceCurrentDie()
        {
            data.currentDieIndex = Mathf.Min(3, data.currentDieIndex + 1);
            for (int i = 0; i < 3; i++) pending[i] = false;
            if (data.currentDieIndex < 3)
            {
                RollCurrentDie();
            }
        }

        private static string ResolveSaveKey(OutgamePlayMode mode) { return mode == OutgamePlayMode.Test ? TestSaveKey : ServiceSaveKey; }
    }
}
