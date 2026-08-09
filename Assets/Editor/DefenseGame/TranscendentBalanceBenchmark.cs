using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DefenseGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DefenseGame.Editor
{
    /// <summary>
    /// Editor-only, deterministic combat benchmark for the current Transcendent balance candidates.
    /// It uses real DefenderUnit, MonsterUnit, skill, crit, mana, cooldown, and AreaDamage paths.
    /// </summary>
    public static class TranscendentBalanceBenchmark
    {
        private const string ScenePath = "Assets/Scenes/DG.unity";
        private const string OutputDirectoryName = "BatchPlaytestResults";
        private const string OutputFileName = "DefenseGame_TranscendentBalanceBenchmark.json";
        private const int BenchmarkSeed = 314159;

        private static bool running;
        private static int runtimeErrors;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;

        private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, OutputFileName));

        [MenuItem("DefenseGame/Balance Tests/Transcendent Benchmark")]
        public static void RunTranscendentBenchmark()
        {
            if (running)
            {
                return;
            }

            running = true;
            runtimeErrors = 0;
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? string.Empty);
            if (File.Exists(OutputPath))
            {
                File.Delete(OutputPath);
            }

            previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived -= HandleLogMessage;
            Application.logMessageReceived += HandleLogMessage;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!running || state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            try
            {
                GameObject root = new GameObject("TranscendentBalanceBenchmarkRunner");
                root.AddComponent<TranscendentBalanceBenchmarkRunner>().Begin(BenchmarkSeed, Finish);
            }
            catch (Exception exception)
            {
                Finish(BenchmarkReport.CreateFailure(BenchmarkSeed, exception.ToString(), runtimeErrors));
            }
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                runtimeErrors++;
            }
        }

        private static void Finish(BenchmarkReport report)
        {
            if (!running)
            {
                return;
            }

            if (report == null)
            {
                report = BenchmarkReport.CreateFailure(BenchmarkSeed, "Benchmark produced no report.", runtimeErrors);
            }
            report.runtimeErrors = runtimeErrors;
            report.passed &= runtimeErrors == 0;
            report.status = report.passed ? "pass" : "fail";
            File.WriteAllText(OutputPath, JsonUtility.ToJson(report, true));

            running = false;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Application.logMessageReceived -= HandleLogMessage;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.passed ? 0 : 1);
            }
        }
    }

    internal sealed class TranscendentBalanceBenchmarkRunner : MonoBehaviour
    {
        private const float Hero53Duration = 25f;
        private const float Hero56RestDuration = 3f;
        private const float Hero56ActiveDuration = 15f;
        private const float DummyHealth = 1000000f;
        private const int Hero56ClusterCount = 4;

        private enum Segment
        {
            None,
            Hero53,
            Hero56Rest,
            Hero56Active
        }

        private static readonly MethodInfo Hero56RoundStartedMethod = typeof(DefenderUnit).GetMethod(
            "HandleAlternatingRoundStarted",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> scenarioObjects = new List<GameObject>();
        private readonly List<float> hero53BuffCastTimes = new List<float>();
        private readonly List<Hero56ActiveRoundResult> hero56ActiveRounds = new List<Hero56ActiveRoundResult>();

        private Action<BenchmarkReport> completion;
        private CharacterDatabase database;
        private DefenderUnit trackedUnit;
        private Segment currentSegment;
        private Hero53BenchmarkResult hero53;
        private Hero56BenchmarkResult hero56;
        private Hero56ActiveRoundResult activeRound;
        private float segmentStartTime;
        private float previousTimeScale;
        private UnityEngine.Random.State previousRandomState;
        private bool eventSubscribed;

        public void Begin(int seed, Action<BenchmarkReport> onComplete)
        {
            completion = onComplete;
            previousTimeScale = Time.timeScale;
            previousRandomState = UnityEngine.Random.state;
            Time.timeScale = 1f;
            UnityEngine.Random.InitState(seed);
            DefenderUnit.OnDamageDealt += HandleDamageDealt;
            DefenderUnit.OnSkillCast += HandleSkillCast;
            eventSubscribed = true;
            StartCoroutine(Run(seed));
        }

        private IEnumerator Run(int seed)
        {
            BenchmarkReport report = new BenchmarkReport
            {
                seed = seed,
                scenePath = "Assets/Scenes/DG.unity",
                baseline = "CharacterDatabase runtime definitions; InitializeSummon prevents outgame growth; no synergy, tile, augment, mana support, or dummy attacks.",
                notes = new List<string>()
            };

            database = UnityEngine.Object.FindObjectOfType<CharacterDatabase>();
            if (database == null)
            {
                report.passed = false;
                report.notes.Add("CharacterDatabase was not found in DG.unity.");
            }
            else if (Hero56RoundStartedMethod == null)
            {
                report.passed = false;
                report.notes.Add("DefenderUnit.HandleAlternatingRoundStarted was not found.");
            }
            else
            {
                yield return RunHero53();
                report.hero53 = hero53;
                yield return ClearScenario();
                yield return RunHero56();
                report.hero56 = hero56;

                report.passed = hero53 != null && hero53.usesRuntimeDefinition && hero53.totalDamage > 0f &&
                                hero53.skillCastCount > 0 && hero56 != null && hero56.restRoundCount == 2 &&
                                hero56.activeRoundCount == 2 && Mathf.Approximately(hero56.restRoundDamage, 0f) &&
                                hero56.restBasicAttackCount == 0 && hero56.restSkillCastCount == 0 && hero56.restManaStayedZero &&
                                hero56.activeRoundDamage > 0f && hero56.burstCastCount > 0 && hero56.basicAttackAfterBurstCount > 0 &&
                                hero56.additionalBurstObserved;
                if (!report.passed)
                {
                    report.notes.Add("Functional benchmark condition failed; inspect hero53/hero56 metrics rather than treating this as a balance score.");
                }
            }

            yield return ClearScenario();
            if (eventSubscribed)
            {
                DefenderUnit.OnDamageDealt -= HandleDamageDealt;
                DefenderUnit.OnSkillCast -= HandleSkillCast;
                eventSubscribed = false;
            }
            Time.timeScale = previousTimeScale;
            UnityEngine.Random.state = previousRandomState;
            completion?.Invoke(report);
            Destroy(gameObject);
        }

        private IEnumerator RunHero53()
        {
            CharacterDefinition definition = database.GetCharacterById("hero_53");
            if (definition == null)
            {
                throw new InvalidOperationException("hero_53 runtime definition was not found.");
            }

            MonsterUnit target = CreateDummy("Hero53_TargetDummy", new Vector3(0f, 0f, 5f));
            trackedUnit = CreateRuntimeUnit("Hero53_Runtime", definition, Vector3.zero);
            hero53 = new Hero53BenchmarkResult
            {
                heroId = definition.id,
                testDuration = Hero53Duration,
                startingTargetHealth = target.CurrentHealth,
                runtimeAttackPower = definition.stats.attackPower,
                runtimeAttackSpeed = definition.stats.attackSpeed,
                runtimeMaxHealth = definition.stats.maxHealth,
                runtimeCriticalChance = definition.stats.criticalChance,
                runtimeCriticalDamageMultiplier = definition.stats.criticalDamageMultiplier,
                runtimeMaxMana = definition.stats.maxMana,
                runtimeManaGainPerAttackRate = definition.stats.manaGainPerAttackRate,
                usesRuntimeDefinition = definition.grade == CharacterGrade.Transcendent &&
                                        Mathf.Approximately(definition.stats.attackPower, 52f) &&
                                        Mathf.Approximately(definition.stats.attackSpeed, 1.7f) &&
                                        Mathf.Approximately(definition.stats.maxHealth, 380f) &&
                                        Mathf.Approximately(definition.stats.criticalChance, 0.25f) &&
                                        Mathf.Approximately(definition.stats.criticalDamageMultiplier, 2.1f) &&
                                        Mathf.Approximately(definition.stats.maxMana, 190f) &&
                                        Mathf.Approximately(definition.stats.manaGainPerAttackRate, 0.17f) &&
                                        definition.skills != null && definition.skills.Count > 0 &&
                                        definition.skills[0].effectType == SkillEffectType.AttackSpeedBoost &&
                                        Mathf.Approximately(definition.skills[0].power, 1f) &&
                                        Mathf.Approximately(definition.skills[0].duration, 8f) &&
                                        Mathf.Approximately(definition.skills[0].cooldown, 11f) &&
                                        Mathf.Approximately(definition.skills[0].manaThreshold, 100f)
            };

            currentSegment = Segment.Hero53;
            segmentStartTime = Time.time;
            yield return new WaitForSeconds(Hero53Duration);
            hero53.testDuration = Mathf.Max(0f, Time.time - segmentStartTime);
            hero53.endingTargetHealth = target != null ? target.CurrentHealth : 0f;
            hero53.totalDamage = Mathf.Max(0f, hero53.startingTargetHealth - hero53.endingTargetHealth);
            hero53.averageDps = hero53.testDuration > 0f ? hero53.totalDamage / hero53.testDuration : 0f;
            hero53.attackSpeedBuffTotalUptime = CalculateUnionUptime(hero53BuffCastTimes, 8f, segmentStartTime, Time.time);
            hero53.attackSpeedBuffUptimeRatio = hero53.testDuration > 0f
                ? Mathf.Clamp01(hero53.attackSpeedBuffTotalUptime / hero53.testDuration)
                : 0f;
            currentSegment = Segment.None;
        }

        private IEnumerator RunHero56()
        {
            CharacterDefinition definition = database.GetCharacterById("hero_56");
            if (definition == null)
            {
                throw new InvalidOperationException("hero_56 runtime definition was not found.");
            }

            trackedUnit = CreateRuntimeUnit("Hero56_Runtime", definition, Vector3.zero);
            for (int i = 0; i < Hero56ClusterCount; i++)
            {
                float x = (i % 2 == 0 ? -0.8f : 0.8f);
                float z = 5f + (i < 2 ? -0.65f : 0.65f);
                CreateDummy("Hero56_ClusterDummy_" + i, new Vector3(x, 0f, z));
            }

            hero56 = new Hero56BenchmarkResult
            {
                heroId = definition.id,
                usesRuntimeDefinition = definition.grade == CharacterGrade.Transcendent &&
                                        Mathf.Approximately(definition.stats.maxMana, 100f) &&
                                        Mathf.Approximately(definition.stats.manaGainPerAttackRate, 0.15f) &&
                                        definition.skills != null && definition.skills.Count > 0 &&
                                        definition.skills[0].effectType == SkillEffectType.AreaDamage &&
                                        Mathf.Approximately(definition.skills[0].power, 4.2f) &&
                                        Mathf.Approximately(definition.skills[0].radius, 4.5f),
                activeRounds = hero56ActiveRounds
            };

            // hero_56 is created in its initial REST state. Drive the same round-start
            // handler that the live round controller invokes so the full intended cycle is measured.
            yield return RunHero56RestSegment(0);
            yield return RunHero56ActiveSegment(1);
            yield return RunHero56RestSegment(2);
            yield return RunHero56ActiveSegment(2);
            hero56.totalElapsedTime = hero56.restRoundCount * Hero56RestDuration + hero56.activeRoundCount * Hero56ActiveDuration;
            hero56.totalDamage = hero56.restRoundDamage + hero56.activeRoundDamage;
            hero56.cycleAverageDps = hero56.totalElapsedTime > 0f ? hero56.totalDamage / hero56.totalElapsedTime : 0f;
            float totalActiveTime = hero56.activeRoundCount * Hero56ActiveDuration;
            hero56.activeOnlyDps = totalActiveTime > 0f ? hero56.activeRoundDamage / totalActiveTime : 0f;
            currentSegment = Segment.None;
        }

        private IEnumerator RunHero56RestSegment(int roundToStart)
        {
            if (roundToStart > 0)
            {
                Hero56RoundStartedMethod.Invoke(trackedUnit, new object[] { roundToStart });
            }
            currentSegment = Segment.Hero56Rest;
            segmentStartTime = Time.time;
            hero56.restRoundCount++;
            hero56.restManaStayedZero &= Mathf.Approximately(trackedUnit.CurrentMana, 0f);
            yield return new WaitForSeconds(Hero56RestDuration);
            hero56.restManaStayedZero &= Mathf.Approximately(trackedUnit.CurrentMana, 0f);
        }

        private IEnumerator RunHero56ActiveSegment(int roundIndex)
        {
            Hero56RoundStartedMethod.Invoke(trackedUnit, new object[] { roundIndex * 2 - 1 });
            currentSegment = Segment.Hero56Active;
            segmentStartTime = Time.time;
            activeRound = new Hero56ActiveRoundResult
            {
                activeRoundIndex = roundIndex,
                startingMana = trackedUnit.CurrentMana
            };
            hero56ActiveRounds.Add(activeRound);
            hero56.activeRoundCount++;
            yield return new WaitForSeconds(Hero56ActiveDuration);
            activeRound.duration = Mathf.Max(0f, Time.time - segmentStartTime);
            activeRound.endingMana = trackedUnit.CurrentMana;
            activeRound = null;
        }

        private void HandleDamageDealt(DefenderUnit source, MonsterUnit target, float damage, bool critical)
        {
            if (source == null || source != trackedUnit || damage <= 0f)
            {
                return;
            }

            bool fromSkill = target != null && target.LastDamageSkill != null;
            if (currentSegment == Segment.Hero53 && hero53 != null)
            {
                hero53.eventDamage += damage;
                if (!fromSkill)
                {
                    hero53.basicAttackCount++;
                }
                return;
            }

            if (hero56 == null)
            {
                return;
            }
            if (currentSegment == Segment.Hero56Rest)
            {
                hero56.restRoundDamage += damage;
                if (fromSkill) hero56.restSkillCastDamageEvents++;
                else hero56.restBasicAttackCount++;
            }
            else if (currentSegment == Segment.Hero56Active)
            {
                hero56.activeRoundDamage += damage;
                if (activeRound != null)
                {
                    activeRound.damageDuringActiveRound += damage;
                }
                if (fromSkill)
                {
                    hero56.burstDamage += damage;
                }
                else
                {
                    hero56.basicAttackCount++;
                    if (activeRound != null && activeRound.burstCount > 0)
                    {
                        hero56.basicAttackAfterBurstCount++;
                    }
                }
            }
        }

        private void HandleSkillCast(DefenderUnit source, SkillDefinition skill, MonsterUnit target)
        {
            if (source == null || source != trackedUnit || skill == null)
            {
                return;
            }

            if (currentSegment == Segment.Hero53 && hero53 != null && skill.effectType == SkillEffectType.AttackSpeedBoost)
            {
                hero53.skillCastCount++;
                hero53.attackSpeedBuffActivationCount++;
                hero53BuffCastTimes.Add(Time.time);
                return;
            }

            if (currentSegment == Segment.Hero56Rest)
            {
                hero56.restSkillCastCount++;
                return;
            }

            if (currentSegment == Segment.Hero56Active && hero56 != null && skill.effectType == SkillEffectType.AreaDamage)
            {
                hero56.burstCastCount++;
                if (activeRound != null)
                {
                    activeRound.burstCount++;
                    if (activeRound.firstBurstTime < 0f)
                    {
                        activeRound.firstBurstTime = Mathf.Max(0f, Time.time - segmentStartTime);
                    }
                    if (activeRound.burstCount >= 2)
                    {
                        hero56.additionalBurstObserved = true;
                    }
                }
            }
        }

        private DefenderUnit CreateRuntimeUnit(string name, CharacterDefinition definition, Vector3 position)
        {
            GameObject actor = new GameObject(name);
            actor.transform.position = position;
            scenarioObjects.Add(actor);
            DefenderUnit unit = actor.AddComponent<DefenderUnit>();
            // Temporary runtime initialization is intentional: it executes the real combat unit path
            // while bypassing OutgameProgressionSystem growth for this zero-growth baseline.
            unit.InitializeSummon(definition);
            return unit;
        }

        private MonsterUnit CreateDummy(string name, Vector3 position)
        {
            GameObject dummyObject = new GameObject(name);
            dummyObject.transform.position = position;
            scenarioObjects.Add(dummyObject);
            MonsterUnit dummy = dummyObject.AddComponent<MonsterUnit>();
            dummy.Initialize(new MonsterDefinition
            {
                id = name,
                displayName = name,
                description = "Editor-only stationary benchmark target.",
                grade = CharacterGrade.Normal,
                role = MonsterRole.Grunt,
                threatLevel = MonsterThreatLevel.Regular,
                isBoss = false,
                rewardGold = 0,
                stats = new CombatStats
                {
                    maxHealth = DummyHealth,
                    attackPower = 0f,
                    attackSpeed = 0f,
                    criticalChance = 0f,
                    criticalDamageMultiplier = 1f,
                    maxMana = 0f,
                    manaRegenPerSecondRate = 0f,
                    manaGainWhenHitRate = 0f,
                    manaGainPerAttackRate = 0f,
                    attackRange = 0.5f,
                    moveSpeed = 0f,
                    projectileSpeed = 0f
                },
                attackBehavior = new AttackBehavior(),
                skills = new List<SkillDefinition>()
            }, null, spawnRound: 0, runtimeHealthMultiplier: 1f, runtimeAttackMultiplier: 1f);
            return dummy;
        }

        private IEnumerator ClearScenario()
        {
            trackedUnit = null;
            currentSegment = Segment.None;
            activeRound = null;
            hero53BuffCastTimes.Clear();
            for (int i = 0; i < scenarioObjects.Count; i++)
            {
                if (scenarioObjects[i] != null)
                {
                    Destroy(scenarioObjects[i]);
                }
            }
            scenarioObjects.Clear();
            yield return null;
        }

        private static float CalculateUnionUptime(List<float> castTimes, float duration, float windowStart, float windowEnd)
        {
            if (castTimes == null || castTimes.Count == 0 || duration <= 0f || windowEnd <= windowStart)
            {
                return 0f;
            }

            castTimes.Sort();
            float total = 0f;
            float intervalStart = -1f;
            float intervalEnd = -1f;
            for (int i = 0; i < castTimes.Count; i++)
            {
                float start = Mathf.Clamp(castTimes[i], windowStart, windowEnd);
                float end = Mathf.Clamp(castTimes[i] + duration, windowStart, windowEnd);
                if (end <= start)
                {
                    continue;
                }
                if (intervalStart < 0f || start > intervalEnd)
                {
                    if (intervalStart >= 0f)
                    {
                        total += intervalEnd - intervalStart;
                    }
                    intervalStart = start;
                    intervalEnd = end;
                }
                else
                {
                    intervalEnd = Mathf.Max(intervalEnd, end);
                }
            }
            if (intervalStart >= 0f)
            {
                total += intervalEnd - intervalStart;
            }
            return total;
        }
    }

    [Serializable]
    internal sealed class BenchmarkReport
    {
        public string status;
        public bool passed;
        public int seed;
        public string scenePath;
        public string baseline;
        public Hero53BenchmarkResult hero53;
        public Hero56BenchmarkResult hero56;
        public int runtimeErrors;
        public List<string> notes;

        public static BenchmarkReport CreateFailure(int seed, string note, int runtimeErrors)
        {
            return new BenchmarkReport
            {
                status = "fail",
                passed = false,
                seed = seed,
                runtimeErrors = runtimeErrors,
                notes = new List<string> { note }
            };
        }
    }

    [Serializable]
    internal sealed class Hero53BenchmarkResult
    {
        public string heroId;
        public bool usesRuntimeDefinition;
        public float testDuration;
        public float startingTargetHealth;
        public float endingTargetHealth;
        public float totalDamage;
        public float eventDamage;
        public float averageDps;
        public int basicAttackCount;
        public int skillCastCount;
        public int attackSpeedBuffActivationCount;
        public float attackSpeedBuffTotalUptime;
        public float attackSpeedBuffUptimeRatio;
        public float runtimeAttackPower;
        public float runtimeAttackSpeed;
        public float runtimeMaxHealth;
        public float runtimeCriticalChance;
        public float runtimeCriticalDamageMultiplier;
        public float runtimeMaxMana;
        public float runtimeManaGainPerAttackRate;
    }

    [Serializable]
    internal sealed class Hero56BenchmarkResult
    {
        public string heroId;
        public bool usesRuntimeDefinition;
        public int restRoundCount;
        public int activeRoundCount;
        public float totalElapsedTime;
        public float totalDamage;
        public float restRoundDamage;
        public float activeRoundDamage;
        public float cycleAverageDps;
        public float activeOnlyDps;
        public int basicAttackCount;
        public int basicAttackAfterBurstCount;
        public int burstCastCount;
        public float burstDamage;
        public int restBasicAttackCount;
        public int restSkillCastCount;
        public int restSkillCastDamageEvents;
        public bool restManaStayedZero = true;
        public bool additionalBurstObserved;
        public List<Hero56ActiveRoundResult> activeRounds;
    }

    [Serializable]
    internal sealed class Hero56ActiveRoundResult
    {
        public int activeRoundIndex;
        public float duration;
        public float startingMana;
        public float endingMana;
        public float firstBurstTime = -1f;
        public int burstCount;
        public float damageDuringActiveRound;
    }
}