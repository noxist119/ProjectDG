using System.Collections;
using UnityEngine;

namespace DefenseGame
{
    public class RoundManager : MonoBehaviour
    {
        [SerializeField] private MonsterDatabase monsterDatabase;
        [SerializeField] private MonsterUnit fallbackMonsterPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform goalPoint;
        [SerializeField] private float spawnInterval = 0.7f;
        [SerializeField] private int baseMonsterCount = 8;
        [SerializeField] private int bossSupportMonsterCount = 6;

        public event System.Action<int, bool, bool> OnRoundStateChanged;

        public int CurrentRound { get; private set; }
        public bool IsRoundRunning { get; private set; }
        public bool IsBossRound => CurrentRound > 0 && CurrentRound % 10 == 0;

        private void Awake()
        {
            if ((spawnPoints == null || spawnPoints.Length == 0) && transform.childCount > 0)
            {
                spawnPoints = new Transform[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    spawnPoints[i] = transform.GetChild(i);
                }
            }
        }

        public void Configure(MonsterDatabase database, MonsterUnit fallbackPrefab, Transform[] newSpawnPoints, Transform newGoalPoint)
        {
            monsterDatabase = database;
            fallbackMonsterPrefab = fallbackPrefab;
            spawnPoints = newSpawnPoints;
            goalPoint = newGoalPoint;
        }

        public void StartNextRound()
        {
            if (!IsRoundRunning)
            {
                StartCoroutine(RunRound());
            }
        }

        private IEnumerator RunRound()
        {
            IsRoundRunning = true;
            CurrentRound++;
            OnRoundStateChanged?.Invoke(CurrentRound, IsBossRound, true);

            if (IsBossRound)
            {
                int supportCount = bossSupportMonsterCount + CurrentRound / 5;
                for (int i = 0; i < supportCount; i++)
                {
                    SpawnMonster(monsterDatabase.GetRandomMonsterForRound(CurrentRound));
                    yield return new WaitForSeconds(spawnInterval);
                }

                SpawnMonster(monsterDatabase.GetBossForRound(CurrentRound));
            }
            else
            {
                int spawnCount = baseMonsterCount + CurrentRound * 2;
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnMonster(monsterDatabase.GetRandomMonsterForRound(CurrentRound));
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            while (FindObjectsOfType<MonsterUnit>().Length > 0)
            {
                yield return null;
            }

            IsRoundRunning = false;
            OnRoundStateChanged?.Invoke(CurrentRound, IsBossRound, false);
        }

        private void SpawnMonster(MonsterDefinition definition)
        {
            if (definition == null || spawnPoints == null || spawnPoints.Length == 0 || goalPoint == null)
            {
                Debug.LogWarning("RoundManager references are missing.");
                return;
            }

            GameObject sourcePrefab = definition.prefab != null
                ? definition.prefab
                : fallbackMonsterPrefab != null ? fallbackMonsterPrefab.gameObject : null;

            if (sourcePrefab == null)
            {
                Debug.LogError("No MonsterUnit prefab assigned.");
                return;
            }

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject spawnedObject = Instantiate(sourcePrefab, spawnPoint.position, spawnPoint.rotation);
            MonsterUnit monster = spawnedObject.GetComponent<MonsterUnit>();
            if (monster == null)
            {
                monster = spawnedObject.AddComponent<MonsterUnit>();
                monster.AdoptRuntimeTemplate(fallbackMonsterPrefab);
            }

            monster.gameObject.SetActive(true);
            monster.Initialize(definition, goalPoint);
        }
    }
}
