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
        [SerializeField] private int roundStartGold = 4;

        public event System.Action OnStateChanged;

        public int Gold { get; private set; }
        public int Life => life;
        public int CurrentRound => roundManager != null ? roundManager.CurrentRound : 0;
        public bool IsRoundRunning => roundManager != null && roundManager.IsRoundRunning;
        public bool IsBossRound => roundManager != null && roundManager.IsBossRound;
        public int BoardUnitCount => boardManager != null ? boardManager.Slots.Count - boardManager.EmptySlotCount : 0;
        public int CharacterCount => characterDatabase != null ? characterDatabase.Characters.Count : 0;
        public int MonsterCount => monsterDatabase != null ? monsterDatabase.Monsters.Count : 0;
        public string CurrentStateSummary => "Gold " + Gold + " | Life " + Life + " | Round " + CurrentRound + (IsBossRound ? " Boss" : string.Empty);

        private void Awake()
        {
            if (characterDatabase == null) characterDatabase = GetComponent<CharacterDatabase>();
            if (monsterDatabase == null) monsterDatabase = GetComponent<MonsterDatabase>();
            if (boardManager == null) boardManager = GetComponent<DefenseBoardManager>();
            if (roundManager == null) roundManager = GetComponent<RoundManager>();
        }

        private void OnEnable()
        {
            MonsterUnit.OnMonsterKilled += HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped += HandleMonsterEscaped;
            SubscribeRoundManager();
        }

        private void OnDisable()
        {
            MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
            MonsterUnit.OnMonsterEscaped -= HandleMonsterEscaped;
            UnsubscribeRoundManager();
        }

        private void Start()
        {
            if (Gold <= 0)
            {
                Gold = startGold;
            }

            NotifyStateChanged();
        }

        public void Configure(CharacterDatabase characters, MonsterDatabase monsters, DefenseBoardManager board, RoundManager rounds, DefenderUnit fallbackUnit)
        {
            UnsubscribeRoundManager();
            characterDatabase = characters;
            monsterDatabase = monsters;
            boardManager = board;
            roundManager = rounds;
            defaultUnitPrefab = fallbackUnit;
            SubscribeRoundManager();
            if (Gold <= 0)
            {
                Gold = startGold;
            }
            NotifyStateChanged();
        }

        public bool TrySummon()
        {
            if (Gold < summonCost || characterDatabase == null || boardManager == null)
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
            if (boardManager == null || characterDatabase == null)
            {
                return false;
            }

            bool merged = boardManager.TryMergeUnitsOfGrade(grade, characterDatabase, defaultUnitPrefab);
            if (merged)
            {
                NotifyStateChanged();
            }

            return merged;
        }

        public void StartRound()
        {
            if (roundManager == null)
            {
                return;
            }

            Gold += roundStartGold + CurrentRound;
            roundManager.StartNextRound();
            NotifyStateChanged();
        }

        public void AddCharacterContent(int additionalCount)
        {
            if (characterDatabase == null)
            {
                return;
            }

            int nextCount = Mathf.Max(characterDatabase.Characters.Count + additionalCount, characterDatabase.Characters.Count);
            characterDatabase.GenerateStarterCharacters(nextCount);
            NotifyStateChanged();
        }

        public void AddMonsterContent(int additionalCount)
        {
            if (monsterDatabase == null)
            {
                return;
            }

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

        private void SubscribeRoundManager()
        {
            if (roundManager != null)
            {
                roundManager.OnRoundStateChanged -= HandleRoundStateChanged;
                roundManager.OnRoundStateChanged += HandleRoundStateChanged;
            }
        }

        private void UnsubscribeRoundManager()
        {
            if (roundManager != null)
            {
                roundManager.OnRoundStateChanged -= HandleRoundStateChanged;
            }
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}
