using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    public class RoundManager : MonoBehaviour
    {
        [SerializeField] private MonsterDatabase monsterDatabase;
        [SerializeField] private MonsterUnit fallbackMonsterPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform goalPoint;
        [SerializeField] private int bossSupportMonsterCount = 4;
        [SerializeField] private int preRoundCountdown = 5;
        [SerializeField] private bool cycleSpawnPoints = true;
        [SerializeField] private GameObject spawnEffectPrefab;
        [SerializeField] private float spawnEffectLifetime = 1.15f;
        [SerializeField] private Vector3 spawnEffectOffset = new Vector3(0f, 0.04f, 0f);

        [Header("Round Spawn Balance")]
        [SerializeField] private int earlyRoundMonsterCount = 6;
        [SerializeField] private int earlyRoundMonsterStep = 1;
        [SerializeField] private int midRoundMonsterCount = 10;
        [SerializeField] private int midRoundMonsterStep = 1;
        [SerializeField] private float lateRoundMonsterStep = 2.6f;
        [SerializeField] private int maxRegularMonsterCount = 60;
        [SerializeField] private int pressureRoundFrequency = 5;
        [SerializeField] private int pressureRoundBonus = 1;
        [SerializeField] private float earlyRoundSpawnInterval = 0.78f;
        [SerializeField] private float midRoundSpawnInterval = 0.62f;
        [SerializeField] private float lateRoundSpawnInterval = 0.4f;
        [SerializeField] private float bossSupportSpawnInterval = 0.48f;
        [SerializeField] private float spawnIntervalVariance = 0.08f;
        [SerializeField] private float minimumSpawnInterval = 0.28f;
        [SerializeField] private float bossEntryDelay = 1.2f;

        [Header("Round Balance Tables")]
        [SerializeField] private List<RoundSpawnBalanceStep> regularSpawnBalanceSteps = new List<RoundSpawnBalanceStep>
        {
            new RoundSpawnBalanceStep { firstRound = 1, regularCountAtFirstRound = 4.5f, regularCountPerRound = 0.75f, pressureRoundFrequency = 5, pressureBonus = 0 },
            new RoundSpawnBalanceStep { firstRound = 4, regularCountAtFirstRound = 7f, regularCountPerRound = 0.75f, pressureRoundFrequency = 5, pressureBonus = 0 },
            new RoundSpawnBalanceStep { firstRound = 10, regularCountAtFirstRound = 18f, regularCountPerRound = 1.8f, pressureRoundFrequency = 5, pressureBonus = 2 }
        };
        [SerializeField] private List<RoundSpawnTimingStep> regularSpawnTimingSteps = new List<RoundSpawnTimingStep>
        {
            new RoundSpawnTimingStep { firstRound = 1, intervalAtFirstRound = 0.88f, intervalChangePerRound = -0.025f, maxIntervalChange = 0.05f, pressureIntervalPenalty = 0f },
            new RoundSpawnTimingStep { firstRound = 4, intervalAtFirstRound = 0.80f, intervalChangePerRound = -0.008f, maxIntervalChange = 0.08f, pressureIntervalPenalty = 0f },
            new RoundSpawnTimingStep { firstRound = 10, intervalAtFirstRound = 0.48f, intervalChangePerRound = -0.006f, maxIntervalChange = 0.14f, pressureIntervalPenalty = 0.025f }
        };
        [SerializeField] private List<RoundSpawnTimingStep> bossSpawnTimingSteps = new List<RoundSpawnTimingStep>
        {
            new RoundSpawnTimingStep { firstRound = 10, intervalAtFirstRound = 0.40f, intervalChangePerRound = -0.004f, maxIntervalChange = 0.14f }
        };
        [SerializeField] private BossSupportBalanceStep bossSupportBalance = new BossSupportBalanceStep
        {
            baseSupportCount = 9,
            firstBossRound = 10,
            supportCountPerBossEncounter = 2
        };
        [SerializeField] private List<MidBossCadenceStep> midBossCadenceSteps = new List<MidBossCadenceStep>
        {
            new MidBossCadenceStep { firstRound = 11, frequency = 6, count = 1, normalRoundsOnly = true },
            new MidBossCadenceStep { firstRound = 14, frequency = 5, count = 1, pressureRoundsOnly = true, normalRoundsOnly = true },
            new MidBossCadenceStep { firstRound = 18, frequency = 8, count = 2, normalRoundsOnly = true },
            new MidBossCadenceStep { firstRound = 30, frequency = 10, count = 1, bossRoundsOnly = true }
        };

        [Header("Mobile Performance Budget")]
        [SerializeField] private int maxActiveRegularMonsters = 42;
        [SerializeField] private int maxActiveTotalMonsters = 54;
        [SerializeField] private float spawnBudgetPollInterval = 0.08f;

        [Header("Mid Boss Cadence")]
        [SerializeField] private int midBossFirstRound = 11;
        [SerializeField] private int midBossRoundFrequency = 4;
        [SerializeField] private int bossRoundMidBossCount = 0;
        [SerializeField] private int maxMidBossesPerRound = 2;
        [SerializeField] [Range(0.15f, 0.85f)] private float midBossEntryRatio = 0.55f;
        [SerializeField] private float midBossEntryDelay = 0.65f;

        public event System.Action<int, bool, bool> OnRoundStateChanged;
        public event System.Action<int> OnCountdownChanged;

        public int CurrentRound { get; private set; }
        public bool IsRoundRunning { get; private set; }
        public bool LastRoundEndedByDefeat { get; private set; }
        public bool IsBossRound => CurrentRound > 0 && CurrentRound % 10 == 0;
        public bool IsMidBossRound => CurrentRound > 0 && !IsBossRound && CreateSpawnPlan(CurrentRound).midBossMonsterCount > 0;
        public int CurrentRoundTargetCount { get; private set; }
        public int CurrentRoundSpawnedCount { get; private set; }

        private int nextSpawnPointIndex;
        private Coroutine roundRoutine;

        private struct RoundSpawnPlan
        {
            public int regularMonsterCount;
            public int midBossMonsterCount;
            public int bossMonsterCount;
            public float interval;
            public float intervalVariance;

            public int TotalCount => regularMonsterCount + midBossMonsterCount + bossMonsterCount;
        }

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

        public void Configure(MonsterDatabase database, MonsterUnit fallbackPrefab, Transform[] newSpawnPoints, Transform newGoalPoint, GameObject newSpawnEffectPrefab = null)
        {
            monsterDatabase = database;
            fallbackMonsterPrefab = fallbackPrefab;
            spawnPoints = newSpawnPoints;
            goalPoint = newGoalPoint;
            spawnEffectPrefab = newSpawnEffectPrefab;
        }

        public void StartNextRound()
        {
            if (!IsRoundRunning)
            {
                roundRoutine = StartCoroutine(RunRound());
            }
        }

        public int GetNextBossRound(int fromRound)
        {
            int round = Mathf.Max(0, fromRound);
            int nextBossRound = Mathf.Max(10, ((round / 10) + 1) * 10);
            return nextBossRound <= round ? round + 10 : nextBossRound;
        }

        public void BeginDefeatCinematic()
        {
            if (!IsRoundRunning)
            {
                return;
            }

            if (roundRoutine != null)
            {
                StopCoroutine(roundRoutine);
                roundRoutine = null;
            }

            LastRoundEndedByDefeat = true;
        }

        public void ForceFailRound()
        {
            if (!IsRoundRunning)
            {
                return;
            }

            if (roundRoutine != null)
            {
                StopCoroutine(roundRoutine);
                roundRoutine = null;
            }

            ClearActiveMonsters();
            LastRoundEndedByDefeat = true;
            IsRoundRunning = false;
            OnCountdownChanged?.Invoke(0);
            OnRoundStateChanged?.Invoke(CurrentRound, IsBossRound, false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void CompleteCurrentRoundForDebug()
        {
            if (!IsRoundRunning)
            {
                return;
            }

            if (roundRoutine != null)
            {
                StopCoroutine(roundRoutine);
                roundRoutine = null;
            }

            ClearActiveMonsters();
            CurrentRoundSpawnedCount = CurrentRoundTargetCount;
            LastRoundEndedByDefeat = false;
            IsRoundRunning = false;
            OnCountdownChanged?.Invoke(0);
            OnRoundStateChanged?.Invoke(CurrentRound, IsBossRound, false);
        }
#endif

        public void ResetRunState()
        {
            if (roundRoutine != null)
            {
                StopCoroutine(roundRoutine);
                roundRoutine = null;
            }

            ClearActiveMonsters();
            CurrentRound = 0;
            CurrentRoundTargetCount = 0;
            CurrentRoundSpawnedCount = 0;
            nextSpawnPointIndex = 0;
            LastRoundEndedByDefeat = false;
            IsRoundRunning = false;
            OnCountdownChanged?.Invoke(0);
        }

        private IEnumerator RunRound()
        {
            IsRoundRunning = true;
            LastRoundEndedByDefeat = false;
            CurrentRound++;
            nextSpawnPointIndex = 0;
            RoundSpawnPlan spawnPlan = CreateSpawnPlan(CurrentRound);
            CurrentRoundTargetCount = spawnPlan.TotalCount;
            CurrentRoundSpawnedCount = 0;
            OnRoundStateChanged?.Invoke(CurrentRound, IsBossRound, true);

            for (int countdown = preRoundCountdown; countdown >= 1; countdown--)
            {
                OnCountdownChanged?.Invoke(countdown);
                yield return new WaitForSeconds(1f);
            }

            OnCountdownChanged?.Invoke(0);

            int midBossesSpawned = 0;
            int[] midBossSpawnIndices = BuildMidBossSpawnIndices(spawnPlan.regularMonsterCount, spawnPlan.midBossMonsterCount);
            for (int regularIndex = 0; regularIndex < spawnPlan.regularMonsterCount; regularIndex++)
            {
                while (midBossesSpawned < spawnPlan.midBossMonsterCount &&
                    midBossSpawnIndices[midBossesSpawned] == regularIndex)
                {
                    yield return SpawnMidBoss(spawnPlan, midBossesSpawned);
                    midBossesSpawned++;
                }

                yield return WaitForSpawnBudget(MonsterThreatLevel.Regular);
                if (SpawnMonster(monsterDatabase.GetRandomMonsterForRound(CurrentRound)))
                {
                    CurrentRoundSpawnedCount++;
                }

                yield return new WaitForSeconds(GetSpawnDelay(spawnPlan, regularIndex));
            }

            while (midBossesSpawned < spawnPlan.midBossMonsterCount)
            {
                yield return SpawnMidBoss(spawnPlan, midBossesSpawned);
                midBossesSpawned++;
            }

            if (spawnPlan.bossMonsterCount > 0)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, bossEntryDelay));
                yield return WaitForSpawnBudget(MonsterThreatLevel.Boss);
                if (SpawnMonster(monsterDatabase.GetBossForRound(CurrentRound)))
                {
                    CurrentRoundSpawnedCount++;
                }
            }

            while (MonsterUnit.ActiveCount > 0)
            {
                yield return null;
            }

            IsRoundRunning = false;
            LastRoundEndedByDefeat = false;
            roundRoutine = null;
            OnRoundStateChanged?.Invoke(CurrentRound, IsBossRound, false);
        }

        private void ClearActiveMonsters()
        {
            List<MonsterUnit> monsters = new List<MonsterUnit>(MonsterUnit.ActiveInstances);
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i] != null)
                {
                    Destroy(monsters[i].gameObject);
                }
            }
        }

        private IEnumerator SpawnMidBoss(RoundSpawnPlan spawnPlan, int midBossIndex)
        {
            yield return new WaitForSeconds(Mathf.Max(minimumSpawnInterval, midBossEntryDelay));
            yield return WaitForSpawnBudget(MonsterThreatLevel.MidBoss);
            if (SpawnMonster(monsterDatabase.GetMidBossForRound(CurrentRound + midBossIndex)))
            {
                CurrentRoundSpawnedCount++;
            }

            yield return new WaitForSeconds(Mathf.Max(minimumSpawnInterval, spawnPlan.interval * 0.85f));
        }

        private int CalculateSpawnCountForRound(int round)
        {
            return CreateSpawnPlan(round).TotalCount;
        }

        private RoundSpawnPlan CreateSpawnPlan(int round)
        {
            bool bossRound = round > 0 && round % 10 == 0;
            int midBossCount = CalculateMidBossCount(round, bossRound);
            if (midBossCount > 0 && (monsterDatabase == null || !monsterDatabase.HasMidBossForRound(round)))
            {
                midBossCount = 0;
            }

            int regularCount = bossRound
                ? CalculateBossSupportCount(round)
                : Mathf.Max(3, CalculateRegularMonsterCount(round) - midBossCount * 2);
            if (!bossRound)
            {
                float fateMonsterCountMultiplier = DefenseGameController.Active != null ? DefenseGameController.Active.GetFateMonsterCountMultiplierForRound(round) : 1f;
                if (fateMonsterCountMultiplier > 1f)
                {
                    regularCount = Mathf.CeilToInt(regularCount * fateMonsterCountMultiplier);
                }
            }

            regularCount = ApplyPreBossLeakEaseToRegularCount(round, bossRound, regularCount);
            regularCount = CommercialRoundPacing.ApplySpawnCount(round, bossRound, regularCount);
            float spawnInterval = CommercialRoundPacing.ApplySpawnInterval(round, bossRound, CalculateSpawnInterval(round, bossRound));

            return new RoundSpawnPlan
            {
                regularMonsterCount = Mathf.Max(0, regularCount),
                midBossMonsterCount = Mathf.Max(0, midBossCount),
                bossMonsterCount = bossRound ? 1 : 0,
                interval = spawnInterval,
                intervalVariance = Mathf.Max(0f, spawnIntervalVariance)
            };
        }

        private int ApplyPreBossLeakEaseToRegularCount(int round, bool bossRound, int count)
        {
            if (bossRound)
            {
                return count;
            }

            if (round >= 5 && round <= 9)
            {
                return Mathf.Max(3, count - 1);
            }

            return count;
        }

        private float ApplyPreBossLeakEaseToSpawnInterval(int round, bool bossRound, float interval)
        {
            return !bossRound && (round == 8 || round == 9)
                ? interval + 0.04f
                : interval;
        }

        private int CalculateRegularMonsterCount(int round)
        {
            if (round <= 0)
            {
                return 0;
            }

            if (TryResolveSpawnBalanceStep(round, out RoundSpawnBalanceStep balanceStep))
            {
                float countValue = balanceStep.regularCountAtFirstRound + (round - balanceStep.firstRound) * balanceStep.regularCountPerRound;
                int tableCount = Mathf.RoundToInt(countValue);
                if (IsPressureRound(round, balanceStep))
                {
                    tableCount += Mathf.Max(0, balanceStep.pressureBonus);
                }

                int maxCount = balanceStep.maxRegularCount > 0 ? balanceStep.maxRegularCount : maxRegularMonsterCount;
                return Mathf.Clamp(tableCount, 1, Mathf.Max(1, maxCount));
            }

            int count;
            if (round <= 3)
            {
                count = earlyRoundMonsterCount + (round - 1) * Mathf.Max(0, earlyRoundMonsterStep);
            }
            else if (round <= 9)
            {
                count = midRoundMonsterCount + (round - 4) * Mathf.Max(0, midRoundMonsterStep);
            }
            else
            {
                count = Mathf.RoundToInt(midRoundMonsterCount + 5 * Mathf.Max(0, midRoundMonsterStep) + (round - 9) * Mathf.Max(0f, lateRoundMonsterStep));
            }

            if (IsPressureRound(round))
            {
                count += Mathf.Max(0, pressureRoundBonus);
            }

            return Mathf.Clamp(count, 1, Mathf.Max(1, maxRegularMonsterCount));
        }

        private int CalculateMidBossCount(int round, bool bossRound)
        {
            if (midBossCadenceSteps != null && midBossCadenceSteps.Count > 0)
            {
                int tableCount = 0;
                for (int i = 0; i < midBossCadenceSteps.Count; i++)
                {
                    MidBossCadenceStep step = midBossCadenceSteps[i];
                    if (step == null || !step.AppliesTo(round, bossRound, IsPressureRound(round)))
                    {
                        continue;
                    }

                    tableCount = Mathf.Max(tableCount, step.count);
                }

                return Mathf.Clamp(tableCount, 0, Mathf.Max(0, maxMidBossesPerRound));
            }

            if (round < Mathf.Max(1, midBossFirstRound))
            {
                return 0;
            }

            if (bossRound)
            {
                int count = bossRoundMidBossCount;
                if (round >= 30)
                {
                    count++;
                }

                return Mathf.Clamp(count, 0, Mathf.Max(0, maxMidBossesPerRound));
            }

            int midBossCount = 0;
            if (midBossRoundFrequency > 0 && (round - midBossFirstRound) % midBossRoundFrequency == 0)
            {
                midBossCount = 1;
            }

            if (round >= 14 && IsPressureRound(round))
            {
                midBossCount = Mathf.Max(midBossCount, 1);
            }

            if (round >= 18 && midBossRoundFrequency > 0 && round % (midBossRoundFrequency * 2) == 0)
            {
                midBossCount = Mathf.Max(midBossCount, 2);
            }

            return Mathf.Clamp(midBossCount, 0, Mathf.Max(0, maxMidBossesPerRound));
        }

        private int CalculateBossSupportCount(int round)
        {
            if (bossSupportBalance != null)
            {
                int encounterOffset = Mathf.Max(0, (round - Mathf.Max(1, bossSupportBalance.firstBossRound)) / Mathf.Max(1, bossSupportBalance.bossRoundFrequency));
                int tableCount = bossSupportBalance.baseSupportCount + encounterOffset * Mathf.Max(0, bossSupportBalance.supportCountPerBossEncounter);
                int maxCount = bossSupportBalance.maxSupportCount > 0 ? bossSupportBalance.maxSupportCount : maxRegularMonsterCount;
                return Mathf.Clamp(tableCount, Mathf.Max(0, bossSupportBalance.minimumSupportCount), Mathf.Max(3, maxCount));
            }

            int supportCount = bossSupportMonsterCount + Mathf.Max(0, (round - 10) / 10) * 2;
            return Mathf.Clamp(supportCount, 3, Mathf.Max(3, maxRegularMonsterCount));
        }

        private float CalculateSpawnInterval(int round, bool bossRound)
        {
            List<RoundSpawnTimingStep> timingSteps = bossRound ? bossSpawnTimingSteps : regularSpawnTimingSteps;
            if (TryResolveSpawnTimingStep(round, timingSteps, out RoundSpawnTimingStep timingStep))
            {
                float tableInterval = timingStep.ResolveInterval(round);
                if (!bossRound && IsPressureRound(round))
                {
                    tableInterval -= Mathf.Max(0f, timingStep.pressureIntervalPenalty);
                }

                return Mathf.Max(minimumSpawnInterval, ApplyPreBossLeakEaseToSpawnInterval(round, bossRound, tableInterval));
            }

            float interval;
            if (bossRound)
            {
                interval = bossSupportSpawnInterval - Mathf.Min(0.18f, round * 0.004f);
            }
            else if (round <= 3)
            {
                interval = earlyRoundSpawnInterval - (round - 1) * 0.04f;
            }
            else if (round <= 9)
            {
                interval = midRoundSpawnInterval - (round - 4) * 0.025f;
            }
            else
            {
                interval = lateRoundSpawnInterval - Mathf.Min(0.14f, (round - 10) * 0.008f);
            }

            if (IsPressureRound(round) && !bossRound)
            {
                interval -= 0.06f;
            }

            return Mathf.Max(minimumSpawnInterval, ApplyPreBossLeakEaseToSpawnInterval(round, bossRound, interval));
        }

        private int[] BuildMidBossSpawnIndices(int regularCount, int midBossCount)
        {
            int[] indices = new int[Mathf.Max(0, midBossCount)];
            if (indices.Length == 0)
            {
                return indices;
            }

            int maxIndex = Mathf.Max(0, regularCount);
            for (int i = 0; i < indices.Length; i++)
            {
                float ratio = indices.Length == 1
                    ? midBossEntryRatio
                    : Mathf.Lerp(0.38f, 0.72f, indices.Length <= 1 ? 0f : (float)i / (indices.Length - 1));
                indices[i] = Mathf.Clamp(Mathf.RoundToInt(maxIndex * ratio), 0, maxIndex);
            }

            return indices;
        }

        private float GetSpawnDelay(RoundSpawnPlan plan, int spawnIndex)
        {
            float wave = Mathf.Sin((CurrentRound * 0.77f + spawnIndex * 1.31f) * Mathf.PI);
            float variedInterval = plan.interval + wave * plan.intervalVariance;
            return Mathf.Max(minimumSpawnInterval, variedInterval);
        }

        private IEnumerator WaitForSpawnBudget(MonsterThreatLevel threatLevel)
        {
            while (IsSpawnBudgetFull(threatLevel))
            {
                yield return new WaitForSeconds(Mathf.Max(0.02f, spawnBudgetPollInterval));
            }
        }

        private bool IsSpawnBudgetFull(MonsterThreatLevel threatLevel)
        {
            if (maxActiveTotalMonsters > 0 && MonsterUnit.ActiveCount >= maxActiveTotalMonsters)
            {
                return true;
            }

            return threatLevel == MonsterThreatLevel.Regular &&
                maxActiveRegularMonsters > 0 &&
                MonsterUnit.CountActive(MonsterThreatLevel.Regular) >= maxActiveRegularMonsters;
        }

        private bool IsPressureRound(int round)
        {
            return pressureRoundFrequency > 0 && round > 0 && round % pressureRoundFrequency == 0 && round % 10 != 0;
        }

        private bool IsPressureRound(int round, RoundSpawnBalanceStep balanceStep)
        {
            if (balanceStep == null)
            {
                return IsPressureRound(round);
            }

            int frequency = balanceStep.pressureRoundFrequency > 0 ? balanceStep.pressureRoundFrequency : pressureRoundFrequency;
            return frequency > 0 && round > 0 && round % frequency == 0 && round % 10 != 0;
        }

        private bool TryResolveSpawnBalanceStep(int round, out RoundSpawnBalanceStep step)
        {
            step = null;
            if (regularSpawnBalanceSteps == null || regularSpawnBalanceSteps.Count == 0)
            {
                return false;
            }

            int bestRound = int.MinValue;
            for (int i = 0; i < regularSpawnBalanceSteps.Count; i++)
            {
                RoundSpawnBalanceStep candidate = regularSpawnBalanceSteps[i];
                if (candidate == null || candidate.firstRound > round || candidate.firstRound < bestRound)
                {
                    continue;
                }

                bestRound = candidate.firstRound;
                step = candidate;
            }

            return step != null;
        }

        private bool TryResolveSpawnTimingStep(int round, List<RoundSpawnTimingStep> timingSteps, out RoundSpawnTimingStep step)
        {
            step = null;
            if (timingSteps == null || timingSteps.Count == 0)
            {
                return false;
            }

            int bestRound = int.MinValue;
            for (int i = 0; i < timingSteps.Count; i++)
            {
                RoundSpawnTimingStep candidate = timingSteps[i];
                if (candidate == null || candidate.firstRound > round || candidate.firstRound < bestRound)
                {
                    continue;
                }

                bestRound = candidate.firstRound;
                step = candidate;
            }

            return step != null;
        }

        [System.Serializable]
        private class RoundSpawnBalanceStep
        {
            [Tooltip("First round that uses this row.")]
            public int firstRound = 1;
            public float regularCountAtFirstRound = 7f;
            public float regularCountPerRound = 2f;
            public int maxRegularCount = 0;
            public int pressureRoundFrequency = 5;
            public int pressureBonus = 4;
        }

        [System.Serializable]
        private class RoundSpawnTimingStep
        {
            [Tooltip("First round that uses this row.")]
            public int firstRound = 1;
            public float intervalAtFirstRound = 0.72f;
            public float intervalChangePerRound = -0.04f;
            public float maxIntervalChange = 0.14f;
            public float pressureIntervalPenalty = 0.06f;

            public float ResolveInterval(int round)
            {
                float roundOffset = Mathf.Max(0, round - Mathf.Max(1, firstRound));
                float rawChange = roundOffset * intervalChangePerRound;
                float cappedChange = intervalChangePerRound < 0f
                    ? Mathf.Max(rawChange, -Mathf.Max(0f, maxIntervalChange))
                    : Mathf.Min(rawChange, Mathf.Max(0f, maxIntervalChange));
                return intervalAtFirstRound + cappedChange;
            }
        }

        [System.Serializable]
        private class BossSupportBalanceStep
        {
            public int firstBossRound = 10;
            public int bossRoundFrequency = 10;
            public int baseSupportCount = 6;
            public int supportCountPerBossEncounter = 2;
            public int minimumSupportCount = 3;
            public int maxSupportCount = 0;
        }

        [System.Serializable]
        private class MidBossCadenceStep
        {
            public int firstRound = 5;
            public int frequency = 4;
            public int count = 1;
            public bool bossRoundsOnly;
            public bool normalRoundsOnly;
            public bool pressureRoundsOnly;

            public bool AppliesTo(int round, bool bossRound, bool pressureRound)
            {
                if (round < Mathf.Max(1, firstRound))
                {
                    return false;
                }

                if (bossRoundsOnly && !bossRound)
                {
                    return false;
                }

                if (normalRoundsOnly && bossRound)
                {
                    return false;
                }

                if (pressureRoundsOnly && !pressureRound)
                {
                    return false;
                }

                if (frequency > 0 && (round - firstRound) % frequency != 0)
                {
                    return false;
                }

                return count > 0;
            }
        }

        private bool SpawnMonster(MonsterDefinition definition)
        {
            if (definition == null || spawnPoints == null || spawnPoints.Length == 0 || goalPoint == null)
            {
                Debug.LogWarning("RoundManager references are missing.");
                return false;
            }

            GameObject sourcePrefab = definition.prefab != null
                ? definition.prefab
                : fallbackMonsterPrefab != null ? fallbackMonsterPrefab.gameObject : null;

            if (sourcePrefab == null)
            {
                Debug.LogError("No MonsterUnit prefab assigned.");
                return false;
            }

            Transform spawnPoint = GetNextSpawnPoint();
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            if (spawnPoint != null && goalPoint != null)
            {
                spawnRotation = RuntimeEffectUtility.FaceTowards(spawnPoint.position, goalPoint.position, spawnRotation);
            }
            PlaySpawnEffect(spawnPoint, spawnRotation);
            GameObject spawnedObject = Instantiate(sourcePrefab, spawnPoint.position, spawnRotation);
            MonsterUnit monster = spawnedObject.GetComponent<MonsterUnit>();
            if (monster == null)
            {
                monster = spawnedObject.AddComponent<MonsterUnit>();
            }

            if (fallbackMonsterPrefab != null)
            {
                monster.AdoptRuntimeTemplate(fallbackMonsterPrefab);
            }

            monster.gameObject.SetActive(true);
            monster.Initialize(definition, goalPoint, CurrentRound);
            return true;
        }

        private void PlaySpawnEffect(Transform spawnPoint, Quaternion spawnRotation)
        {
            if (spawnPoint == null || spawnEffectPrefab == null)
            {
                return;
            }

            RuntimeEffectUtility.PlayOneShotTimed(
                spawnEffectPrefab,
                spawnPoint.position + spawnEffectOffset,
                spawnRotation,
                spawnEffectLifetime);
        }

        private Transform GetNextSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            if (!cycleSpawnPoints)
            {
                return spawnPoints[Random.Range(0, spawnPoints.Length)];
            }

            Transform spawnPoint = spawnPoints[nextSpawnPointIndex % spawnPoints.Length];
            nextSpawnPointIndex++;
            return spawnPoint;
        }
    }
    public enum CommercialRoundPhase
    {
        Stable = 0,
        BuildUp = 1,
        Hurdle = 2,
        Relief = 3
    }

    public struct CommercialRoundTuning
    {
        public CommercialRoundPhase phase;
        public int hurdleRound;
        public int hurdleTier;
        public float healthMultiplier;
        public float attackMultiplier;
        public float spawnCountMultiplier;
        public float spawnIntervalMultiplier;
    }

    public static class CommercialRoundPacing
    {
        public const int FirstHurdleRound = 20;
        public const int HurdleInterval = 10;

        public static CommercialRoundTuning Resolve(int round, bool bossLike)
        {
            int safeRound = Mathf.Max(1, round);
            int hurdleRound = ResolveRelevantHurdleRound(safeRound);
            int hurdleTier = Mathf.Max(0, (hurdleRound - FirstHurdleRound) / HurdleInterval);
            CommercialRoundPhase phase = ResolvePhase(safeRound, hurdleRound);

            CommercialRoundTuning tuning = new CommercialRoundTuning
            {
                phase = phase,
                hurdleRound = hurdleRound,
                hurdleTier = hurdleTier,
                healthMultiplier = 1f,
                attackMultiplier = 1f,
                spawnCountMultiplier = 1f,
                spawnIntervalMultiplier = 1f
            };

            switch (phase)
            {
                case CommercialRoundPhase.BuildUp:
                    tuning.healthMultiplier = 1.05f + Mathf.Min(0.05f, hurdleTier * 0.01f);
                    tuning.attackMultiplier = 1.03f + Mathf.Min(0.04f, hurdleTier * 0.01f);
                    tuning.spawnCountMultiplier = 1.06f;
                    tuning.spawnIntervalMultiplier = 0.96f;
                    break;

                case CommercialRoundPhase.Hurdle:
                    tuning.healthMultiplier = (bossLike ? 1.20f : 1.10f) + Mathf.Min(0.16f, hurdleTier * 0.04f);
                    tuning.attackMultiplier = (bossLike ? 1.10f : 1.06f) + Mathf.Min(0.10f, hurdleTier * 0.025f);
                    tuning.spawnCountMultiplier = 1.08f;
                    tuning.spawnIntervalMultiplier = 0.94f;
                    break;

                case CommercialRoundPhase.Relief:
                    tuning.healthMultiplier = 0.82f + Mathf.Min(0.05f, hurdleTier * 0.01f);
                    tuning.attackMultiplier = 0.90f + Mathf.Min(0.04f, hurdleTier * 0.01f);
                    tuning.spawnCountMultiplier = 0.86f;
                    tuning.spawnIntervalMultiplier = 1.12f;
                    break;
            }

            return tuning;
        }

        public static bool IsMajorHurdleRound(int round)
        {
            return round >= FirstHurdleRound && round % HurdleInterval == 0;
        }

        public static bool TryGetApproachingHurdleIndex(int round, out int hurdleIndex)
        {
            int hurdleRound = GetNextOrCurrentHurdleRound(round);
            bool applies = round >= hurdleRound - 2 && round <= hurdleRound;
            hurdleIndex = applies ? Mathf.Max(0, (hurdleRound - FirstHurdleRound) / HurdleInterval) : -1;
            return applies;
        }

        public static int GetNextHurdleRound(int completedRound)
        {
            if (completedRound < FirstHurdleRound)
            {
                return FirstHurdleRound;
            }

            return ((completedRound / HurdleInterval) + 1) * HurdleInterval;
        }

        public static void ResolveCombatMultipliers(int round, bool bossLike, out float healthMultiplier, out float attackMultiplier)
        {
            CommercialRoundTuning tuning = Resolve(round, bossLike);
            healthMultiplier = tuning.healthMultiplier;
            attackMultiplier = tuning.attackMultiplier;
        }

        public static int ApplySpawnCount(int round, bool bossRound, int count)
        {
            CommercialRoundTuning tuning = Resolve(round, bossRound);
            return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, count) * tuning.spawnCountMultiplier));
        }

        public static float ApplySpawnInterval(int round, bool bossRound, float interval)
        {
            CommercialRoundTuning tuning = Resolve(round, bossRound);
            return Mathf.Max(0.05f, interval * tuning.spawnIntervalMultiplier);
        }

        public static string BuildPhaseLabel(int round)
        {
            CommercialRoundTuning tuning = Resolve(round, round > 0 && round % 10 == 0);
            switch (tuning.phase)
            {
                case CommercialRoundPhase.BuildUp: return "\ud5c8\ub4e4 \uc804 \uc555\ubc15";
                case CommercialRoundPhase.Hurdle: return "\uc131\uc7a5 \ud5c8\ub4e4";
                case CommercialRoundPhase.Relief: return "\ub3cc\ud30c \ubcf4\ub108\uc2a4 \uad6c\uac04";
                default: return "\uc131\uc7a5 \uad6c\uac04";
            }
        }

        private static int ResolveRelevantHurdleRound(int round)
        {
            if (round < FirstHurdleRound)
            {
                return FirstHurdleRound;
            }

            int previous = (round / HurdleInterval) * HurdleInterval;
            if (round - previous <= 3)
            {
                return Mathf.Max(FirstHurdleRound, previous);
            }

            return Mathf.Max(FirstHurdleRound, previous + HurdleInterval);
        }

        private static int GetNextOrCurrentHurdleRound(int round)
        {
            if (round <= FirstHurdleRound)
            {
                return FirstHurdleRound;
            }

            int remainder = round % HurdleInterval;
            return remainder == 0 ? round : round + (HurdleInterval - remainder);
        }

        private static CommercialRoundPhase ResolvePhase(int round, int hurdleRound)
        {
            if (round == hurdleRound && IsMajorHurdleRound(round))
            {
                return CommercialRoundPhase.Hurdle;
            }

            if (round >= hurdleRound - 2 && round < hurdleRound)
            {
                return CommercialRoundPhase.BuildUp;
            }

            if (round > hurdleRound && round <= hurdleRound + 3)
            {
                return CommercialRoundPhase.Relief;
            }

            return CommercialRoundPhase.Stable;
        }
    }
}
