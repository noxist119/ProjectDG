using UnityEngine;

namespace DefenseGame
{
    public class DefenseGameController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private CharacterDatabase characterDatabase;
        [SerializeField] private MonsterDatabase monsterDatabase;
        [SerializeField] private DefenseBoardManager boardManager;
        [SerializeField] private RoundManager roundManager;
        [SerializeField] private DefenderUnit defaultUnitPrefab;

        [Header("Economy")]
        [SerializeField] private int startGold = 30;
        [SerializeField] private int summonCost = 10;
        [SerializeField] private int life = 20;

        public event System.Action OnStateChanged;

        public int Gold { get; private set; }
        public int Life => life;
        public int CurrentRound => roundManager != null ? roundManager.CurrentRound : 0;
        public bool IsRoundRunning => roundManager != null && roundManager.IsRoundRunning;
        public bool IsBossRound => roundManager != null && roundManager.IsBossRound;
        public int BoardUnitCount => boardManager != null ? boardManager.Slots.Count - boardManager.EmptySlotCount : 0;
        public int CharacterCount => characterDatabase != null ? characterDatabase.Characters.Count : 0;
        public int MonsterCount => monsterDatabase != null ? monsterDatabase.Monsters.Count : 0;

        private void OnEnable()
        {
            MonsterUnit.OnMonsterKilled += HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped += HandleMonsterEscaped;

            if (roundManager != null)
            {
                roundManager.OnRoundStateChanged += HandleRoundStateChanged;
            }
        }

        private void OnDisable()
        {
            MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped -= HandleMonsterEscaped;

            if (roundManager != null)
            {
                roundManager.OnRoundStateChanged -= HandleRoundStateChanged;
            }
        }

        private void Start()
        {
            Gold = startGold;
            NotifyStateChanged();
        }

        public bool TrySummon()
        {
            if (Gold < summonCost)
            {
                return false;
            }

            CharacterDefinition summon = characterDatabase.GetRandomSummonableCharacter();
            if (summon == null)
            {
                return false;
            }

            bool spawned = boardManager.TrySpawnUnit(summon, defaultUnitPrefab);
            if (!spawned)
            {
                return false;
            }

            Gold -= summonCost;
            NotifyStateChanged();
            return true;
        }

        public bool TryMerge(CharacterGrade grade)
        {
            bool merged = boardManager.TryMergeUnitsOfGrade(grade, characterDatabase, defaultUnitPrefab);
            if (merged)
            {
                NotifyStateChanged();
            }

            return merged;
        }

        public void StartRound()
        {
            roundManager.StartNextRound();
            NotifyStateChanged();
        }

        public void AddCharacterContent(int additionalCount)
        {
            int nextCount = Mathf.Max(characterDatabase.Characters.Count + additionalCount, characterDatabase.Characters.Count);
            characterDatabase.GenerateStarterCharacters(nextCount);
            NotifyStateChanged();
        }

        public void AddMonsterContent(int additionalCount)
        {
            int nextCount = Mathf.Max(monsterDatabase.Monsters.Count + additionalCount, monsterDatabase.Monsters.Count);
            monsterDatabase.GenerateStarterMonsters(nextCount);
            NotifyStateChanged();
        }

        private void HandleMonsterKilled(MonsterUnit monster)
        {
            Gold += monster.GetRewardGold();
            NotifyStateChanged();
        }

        private void HandleMonsterEscaped(MonsterUnit monster)
        {
            life--;
            NotifyStateChanged();

            if (life <= 0)
            {
                Debug.Log("Game Over");
            }
        }

        private void HandleRoundStateChanged(int round, bool bossRound, bool running)
        {
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}

