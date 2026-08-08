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
        // Legacy field retained so sequential-build saves remain readable. It is no longer used by gameplay.
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
        public int NextHoldCost => HoldCount <= 0 ? FirstHoldDiamondCost : SecondHoldDiamondCost;
        public int CurrentMultiplier
        {
            get
            {
                EnsureMode();
                return ResolveMultiplier(dice[0], dice[1], dice[2]);
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

        public static int ResolveMultiplier(int first, int second, int third)
        {
            return first >= 1 && first <= 6 && first == second && second == third ? first : 1;
        }

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
            data.currentDieIndex = 0;
            for (int i = 0; i < 3; i++)
            {
                held[i] = false;
                pending[i] = false;
                dice[i] = UnityEngine.Random.Range(1, 7);
            }

            SaveAndNotify();
            message = "첫 굴림 완료. 남길 주사위를 선택해 홀드하거나 재굴림하세요.";
            return true;
        }

        public bool TogglePendingHold(int index, out string message)
        {
            EnsureMode();
            if (!data.sessionActive || index < 0 || index >= 3) { message = "먼저 얏찌를 시작하세요."; return false; }
            if (held[index]) { message = "이미 HOLD된 주사위입니다."; return false; }
            if (data.holdCount >= 2) { message = "홀드는 한 판에 두 번까지 가능합니다."; return false; }

            pending[index] = !pending[index];
            SaveAndNotify();
            message = pending[index] ? "홀드할 주사위를 선택했습니다." : "홀드 선택을 해제했습니다.";
            return true;
        }

        public bool TryCommitHold(out string message)
        {
            EnsureMode();
            if (!data.sessionActive) { message = "먼저 얏찌를 시작하세요."; return false; }
            if (data.holdCount >= 2) { message = "홀드는 한 판에 두 번까지 가능합니다."; return false; }

            bool hasPending = pending[0] || pending[1] || pending[2];
            if (!hasPending) { message = "홀드할 주사위를 먼저 선택하세요."; return false; }

            int cost = data.holdCount == 0 ? FirstHoldDiamondCost : SecondHoldDiamondCost;
            if (progression == null || !progression.TrySpendDiamonds(cost))
            {
                message = "다이아가 부족합니다. 현재 주사위 상태는 유지됩니다.";
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (pending[i]) held[i] = true;
                pending[i] = false;
            }
            data.holdCount++;
            SaveAndNotify();
            message = data.holdCount == 1 ? "1차 HOLD 완료. 한 번 더 홀드할 수 있습니다." : "2차 HOLD 완료.";
            return true;
        }

        public bool TryReroll(out string message)
        {
            EnsureMode();
            if (!data.sessionActive) { message = "먼저 얏찌를 시작하세요."; return false; }
            if (progression == null || !progression.TrySpendGold(RerollGoldCost))
            {
                message = "골드가 부족합니다. 현재 주사위 상태는 유지됩니다.";
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (!held[i]) dice[i] = UnityEngine.Random.Range(1, 7);
                pending[i] = false;
            }
            data.rerollCount++;
            data.sessionGoldSpent += RerollGoldCost;
            SaveAndNotify();
            message = CurrentMultiplier > 1
                ? dice[0] + " 트리플! 지금 확정하면 보상 x" + CurrentMultiplier + "입니다."
                : "재굴림 완료. 남길 주사위를 선택하거나 다시 굴리세요.";
            return true;
        }

        public bool TryConfirmResult(out int multiplier, out string message)
        {
            EnsureMode();
            multiplier = 1;
            if (!data.sessionActive) { message = "확정할 얏찌 결과가 없습니다."; return false; }

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
            if (progression != null) progression.GrantYahtzeeCards(cardCount, out _);
            // A x6 chest can draw six different heroes. Represent the real total instead of showing the first hero as x6.
            return new YahtzeeRewardResult
            {
                rewardType = YahtzeeRewardType.CharacterCard,
                amount = cardCount,
                multiplier = multiplier,
                characterId = string.Empty,
                characterName = "영웅 카드",
                characterGrade = CharacterGrade.Normal
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
            NormalizeLegacySequentialSession();
            Save();
        }

        private void NormalizeLegacySequentialSession()
        {
            if (!data.sessionActive) return;
            for (int i = 0; i < 3; i++)
            {
                if (dice[i] < 1 || dice[i] > 6) dice[i] = UnityEngine.Random.Range(1, 7);
            }
            data.currentDieIndex = 0;
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

        private static string ResolveSaveKey(OutgamePlayMode mode) { return mode == OutgamePlayMode.Test ? TestSaveKey : ServiceSaveKey; }
    }
}
