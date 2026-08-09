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
        private const float Hero51Duration = 18f;
        private const float Hero52Duration = 18f;
        private const float Hero53Duration = 25f;
        private const float Hero54Duration = 20f;
        private const float Hero55Duration = 28f;
        private const float Hero57Duration = 12f;
        private const float Hero56RestDuration = 3f;
        private const float Hero56ActiveDuration = 15f;
        private const float DummyHealth = 1000000f;
        private const int Hero56ClusterCount = 4;
        private static readonly int[] Hero57Seeds = { 314159, 271828, 161803 };

        private enum Segment
        {
            None,
            GenericOffense,
            TankPressure,
            Hero53,
            Hero56Rest,
            Hero56Active
        }

        private static readonly MethodInfo Hero56RoundStartedMethod = typeof(DefenderUnit).GetMethod(
            "HandleAlternatingRoundStarted",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo MonsterPerformAttackMethod = typeof(MonsterUnit).GetMethod(
            "PerformAttack",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> scenarioObjects = new List<GameObject>();
        private readonly List<float> hero53BuffCastTimes = new List<float>();
        private readonly List<Hero56ActiveRoundResult> hero56ActiveRounds = new List<Hero56ActiveRoundResult>();
        private readonly List<Hero57SeedResult> hero57SeedResults = new List<Hero57SeedResult>();

        private Action<BenchmarkReport> completion;
        private CharacterDatabase database;
        private DefenderUnit trackedUnit;
        private Segment currentSegment;
        private Hero53BenchmarkResult hero53;
        private Hero56BenchmarkResult hero56;
        private HeroCombatBenchmarkResult hero51;
        private HeroCombatBenchmarkResult hero52;
        private OffenseRunResult activeOffense;
        private OffenseRunResult hero57Single;
        private TankBenchmarkResult activeTank;
        private float activeTankSkillUntil;
        private float activeTankStartTime;
        private float activeTankRawDamagePerHit;
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
            DefenderUnit.OnDamageTaken += HandleDamageTaken;
            eventSubscribed = true;
            StartCoroutine(Run(seed));
        }

        private IEnumerator Run(int seed)
        {
            BenchmarkReport report = new BenchmarkReport
            {
                seed = seed,
                scenePath = "Assets/Scenes/DG.unity",
                baseline = "CharacterDatabase runtime definitions; InitializeSummon prevents outgame growth; no synergy, tile, augment, or mana support; offense dummies do not attack and the dedicated tank scenarios use real melee MonsterUnit attacks. hero_56 probeWindowAverageDps includes the short 3s REST probes and is diagnostic only; normalizedDutyCycleDps = activeOnlyDps * 0.5 assumes equal REST/ACTIVE round weight as a 50% duty-cycle balance reference, not literal round duration.",
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
                report.runtimeDefinitions = CaptureRuntimeDefinitions();
                yield return RunHero51();
                report.hero51 = hero51;
                yield return ClearScenario();
                yield return RunHero52();
                report.hero52 = hero52;
                yield return ClearScenario();
                yield return RunHero53();
                report.hero53 = hero53;
                yield return ClearScenario();
                yield return RunHero54();
                report.hero54 = activeTank;
                yield return ClearScenario();
                yield return RunHero55();
                report.hero55 = activeTank;
                yield return ClearScenario();
                yield return RunHero56();
                report.hero56 = hero56;
                yield return ClearScenario();
                yield return RunHero57();
                report.hero57 = new Hero57BenchmarkResult
                {
                    heroId = "hero_57",
                    single = hero57Single,
                    seedResults = hero57SeedResults,
                    averageDps = CalculateAverageHero57Dps(),
                    minDps = CalculateMinHero57Dps(),
                    maxDps = CalculateMaxHero57Dps(),
                    dpsSpread = CalculateMaxHero57Dps() - CalculateMinHero57Dps()
                };

                report.offenseComparison = new OffenseComparisonResult
                {
                    hero51SingleDps = hero51.regular.averageDps,
                    hero51ControlUptimeRatio = hero51.regular.statusUptimeRatio,
                    hero51BossDps = hero51.boss.averageDps,
                    hero52SingleDps = hero52.single.averageDps,
                    hero52ClusterDps = hero52.cluster.averageDps,
                    hero53SingleDps = hero53.averageDps,
                    hero56ClusterTargetCount = Hero56ClusterCount,
                    hero56ActiveOnlyDps = hero56.activeOnlyDps,
                    hero56NormalizedDutyCycleDps = hero56.normalizedDutyCycleDps,
                    hero57SingleDps = report.hero57.single.averageDps,
                    hero57ClusterAverageDps = report.hero57.averageDps,
                    hero57ClusterDpsSpread = report.hero57.dpsSpread
                };
                report.tankComparison = new TankComparisonResult
                {
                    hero54StartingHealth = report.hero54.startingHealth,
                    hero54DamageTaken = report.hero54.totalDamageTaken,
                    hero54DamageDuringTaunt = report.hero54.damageDuringSkill,
                    hero54DamageOutsideTaunt = report.hero54.damageOutsideSkill,
                    hero54EstimatedLateDamageReduction = report.hero54.estimatedLateDamageReduction,
                    hero55StartingHealth = report.hero55.startingHealth,
                    hero55DamageTaken = report.hero55.totalDamageTaken,
                    hero55EarlyDamagePerHit = report.hero55.earlyDamagePerHit,
                    hero55LateDamagePerHit = report.hero55.lateDamagePerHit,
                    hero55EstimatedLateDamageReduction = report.hero55.estimatedLateDamageReduction,
                    hero55KnockbackDistance = report.hero55.knockbackDistance
                };                report.passed = hero51 != null && hero51.regular.totalDamage > 0f && hero51.regular.skillCastCount > 0 &&
                                hero51.regular.statusUptime > 0f && hero51.boss.statusUptime <= 0.01f && hero51.boss.totalDamage > 0f &&
                                hero52 != null && hero52.single.totalDamage > 0f && hero52.single.skillDamage > 0f &&
                                hero52.cluster.totalDamage > hero52.single.totalDamage &&
                                hero53 != null && hero53.usesRuntimeDefinition && hero53.totalDamage > 0f &&
                                hero53.skillCastCount > 0 && hero56 != null && hero56.restRoundCount == 2 &&
                                hero56.activeRoundCount == 2 && Mathf.Approximately(hero56.restRoundDamage, 0f) &&
                                hero56.restBasicAttackCount == 0 && hero56.restSkillCastCount == 0 && hero56.restManaStayedZero &&
                                hero56.activeRoundDamage > 0f && hero56.burstCastCount > 0 && hero56.basicAttackAfterBurstCount > 0 &&
                                hero56.additionalBurstObserved && report.hero54 != null && report.hero54.damageEventCount > 0 &&
                                report.hero54.skillCastCount > 0 && report.hero55 != null && report.hero55.damageEventCount > 0 &&
                                report.hero55.skillCastCount > 0 && report.hero55.knockbackDistance > 0.1f &&
                                report.hero57 != null && report.hero57.seedResults.Count == Hero57Seeds.Length;
                if (!report.passed)
                {
                    report.notes.Add("Functional benchmark condition failed; inspect the hero_51~hero_57 functional metrics rather than treating this as a balance score.");
                }
            }

            yield return ClearScenario();
            if (eventSubscribed)
            {
                DefenderUnit.OnDamageDealt -= HandleDamageDealt;
                DefenderUnit.OnSkillCast -= HandleSkillCast;
                DefenderUnit.OnDamageTaken -= HandleDamageTaken;
                eventSubscribed = false;
            }
            Time.timeScale = previousTimeScale;
            UnityEngine.Random.state = previousRandomState;
            completion?.Invoke(report);
            Destroy(gameObject);
        }

        private IEnumerator RunHero51()
        {
            CharacterDefinition definition = GetTranscendentDefinition("hero_51");
            hero51 = new HeroCombatBenchmarkResult { heroId = definition.id, role = "High Control / Damage" };
            yield return RunOffenseScenario(definition, Hero51Duration, 1, false, true);
            hero51.regular = activeOffense;
            yield return ClearScenario();
            yield return RunOffenseScenario(definition, Hero51Duration, 1, true, true);
            hero51.boss = activeOffense;
        }

        private IEnumerator RunHero52()
        {
            CharacterDefinition definition = GetTranscendentDefinition("hero_52");
            hero52 = new HeroCombatBenchmarkResult { heroId = definition.id, role = "High AoE Damage" };
            yield return RunOffenseScenario(definition, Hero52Duration, 1, false, false);
            hero52.single = activeOffense;
            yield return ClearScenario();
            yield return RunOffenseScenario(definition, Hero52Duration, 4, false, false);
            hero52.cluster = activeOffense;
        }

        private IEnumerator RunHero54()
        {
            yield return RunTankScenario(GetTranscendentDefinition("hero_54"), Hero54Duration, false);
        }

        private IEnumerator RunHero55()
        {
            yield return RunTankScenario(GetTranscendentDefinition("hero_55"), Hero55Duration, true);
        }

        private IEnumerator RunHero57()
        {
            CharacterDefinition definition = GetTranscendentDefinition("hero_57");
            hero57SeedResults.Clear();
            UnityEngine.Random.InitState(Hero57Seeds[0]);
            yield return RunOffenseScenario(definition, Hero57Duration, 1, false, false);
            hero57Single = activeOffense;
            yield return ClearScenario();
            for (int i = 0; i < Hero57Seeds.Length; i++)
            {
                UnityEngine.Random.InitState(Hero57Seeds[i]);
                yield return RunOffenseScenario(definition, Hero57Duration, 4, false, false);
                hero57SeedResults.Add(new Hero57SeedResult
                {
                    seed = Hero57Seeds[i],
                    totalDamage = activeOffense.totalDamage,
                    averageDps = activeOffense.averageDps,
                    basicDamage = activeOffense.basicDamage,
                    skillDamage = activeOffense.skillDamage,
                    skillCastCount = activeOffense.skillCastCount,
                    targetDamage = activeOffense.targetDamage
                });
                yield return ClearScenario();
            }
        }

        private IEnumerator RunOffenseScenario(CharacterDefinition definition, float duration, int targetCount, bool bossTarget, bool sampleStun)
        {
            activeOffense = new OffenseRunResult
            {
                heroId = definition.id,
                targetCount = targetCount,
                targetIsBoss = bossTarget,
                testDuration = duration,
                usesRuntimeDefinition = definition.grade == CharacterGrade.Transcendent
            };
            List<MonsterUnit> targets = new List<MonsterUnit>();
            for (int i = 0; i < targetCount; i++)
            {
                float x = targetCount == 1 ? 0f : ((i % 2 == 0) ? -0.8f : 0.8f);
                float z = 5f + (targetCount == 1 ? 0f : (i < 2 ? -0.65f : 0.65f));
                MonsterUnit target = CreateDummy(definition.id + "_BenchmarkTarget_" + i, new Vector3(x, 0f, z), bossTarget);
                targets.Add(target);
                activeOffense.startingTargetHealth += target.CurrentHealth;
            }
            trackedUnit = CreateRuntimeUnit(definition.id + "_BenchmarkUnit", definition, Vector3.zero);
            currentSegment = Segment.GenericOffense;
            segmentStartTime = Time.time;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (sampleStun && targets.Count > 0 && targets[0] != null && targets[0].IsStunned)
                {
                    activeOffense.statusUptime += Time.deltaTime;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            activeOffense.testDuration = Mathf.Max(0f, Time.time - segmentStartTime);
            for (int i = 0; i < targets.Count; i++)
            {
                MonsterUnit target = targets[i];
                float damage = target == null ? 0f : Mathf.Max(0f, target.MaxHealth - target.CurrentHealth);
                activeOffense.targetDamage.Add(damage);
                activeOffense.totalDamage += damage;
            }
            activeOffense.averageDps = activeOffense.testDuration > 0f ? activeOffense.totalDamage / activeOffense.testDuration : 0f;
            activeOffense.statusUptimeRatio = activeOffense.testDuration > 0f ? activeOffense.statusUptime / activeOffense.testDuration : 0f;
            currentSegment = Segment.None;
        }

        private IEnumerator RunTankScenario(CharacterDefinition definition, float duration, bool observeKnockback)
        {
            activeTank = new TankBenchmarkResult
            {
                heroId = definition.id,
                role = string.Equals(definition.id, "hero_54", StringComparison.Ordinal) ? "Entry Tank" : "Mid Tank",
                note = string.Equals(definition.id, "hero_55", StringComparison.Ordinal) ? "Knockback is measured on a regular target because boss-like targets are status-immune." : "Taunt pressure uses real MonsterUnit target selection and DefenderUnit damage events.",
                testDuration = duration,
                startingHealth = definition.stats.maxHealth,
                usesRuntimeDefinition = definition.grade == CharacterGrade.Transcendent
            };
            trackedUnit = CreateRuntimeUnit(definition.id + "_Tank", definition, Vector3.zero);
            // The ally remains in the live DefenderUnit registry, allowing the Taunt path to use real target selection.
            CreateRuntimeUnit(definition.id + "_Ally", GetTranscendentDefinition("hero_53"), new Vector3(1.3f, 0f, -0.5f));
            activeTankRawDamagePerHit = 12f;
            activeTankStartTime = Time.time;
            activeTankSkillUntil = -1f;
            MonsterUnit pressure = CreateDummy(definition.id + "_Pressure", new Vector3(0f, 0f, 1.05f), false, activeTankRawDamagePerHit, 1.15f);
            activeTank.knockbackTargetStart = pressure.transform.position;
            currentSegment = Segment.TankPressure;
            // Invoke MonsterUnit's own private basic-attack path. The ordinary Update loop can
            // defer attacks in this headless isolated scene, while this still exercises the real
            // queue, cooldown, hit, DefenderUnit.TakeDamage, and OnDamageTaken production path.
            float nextAttackTime = Time.time;
            float attackInterval = 1f / Mathf.Max(0.2f, activeTankRawDamagePerHit > 0f ? 1.15f : 1f);
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                if (MonsterPerformAttackMethod != null && pressure != null && trackedUnit != null && trackedUnit.CurrentHealth > 0f && Time.time >= nextAttackTime)
                {
                    MonsterPerformAttackMethod.Invoke(pressure, new object[] { trackedUnit });
                    nextAttackTime += attackInterval;
                }
                yield return null;
            }
            activeTank.endingHealth = trackedUnit != null ? trackedUnit.CurrentHealth : 0f;
            activeTank.survived = trackedUnit != null && trackedUnit.CurrentHealth > 0f;
            activeTank.knockbackDistance = observeKnockback && pressure != null
                ? Vector3.Distance(activeTank.knockbackTargetStart, pressure.transform.position)
                : 0f;
            activeTank.earlyDamagePerHit = activeTank.earlyDamageEvents > 0 ? activeTank.earlyDamage / activeTank.earlyDamageEvents : 0f;
            activeTank.lateDamagePerHit = activeTank.lateDamageEvents > 0 ? activeTank.lateDamage / activeTank.lateDamageEvents : 0f;
            activeTank.estimatedLateDamageReduction = activeTankRawDamagePerHit > 0f && activeTank.lateDamageEvents > 0
                ? Mathf.Clamp01(1f - activeTank.lateDamagePerHit / activeTankRawDamagePerHit)
                : 0f;
            currentSegment = Segment.None;
        }

        private CharacterDefinition GetTranscendentDefinition(string heroId)
        {
            CharacterDefinition definition = database.GetCharacterById(heroId);
            if (definition == null)
            {
                throw new InvalidOperationException(heroId + " runtime definition was not found.");
            }
            return definition;
        }

        private List<RuntimeDefinitionSnapshot> CaptureRuntimeDefinitions()
        {
            List<RuntimeDefinitionSnapshot> snapshots = new List<RuntimeDefinitionSnapshot>();
            for (int heroNumber = 51; heroNumber <= 57; heroNumber++)
            {
                CharacterDefinition definition = GetTranscendentDefinition("hero_" + heroNumber);
                SkillDefinition skill = definition.skills != null && definition.skills.Count > 0 ? definition.skills[0] : null;
                snapshots.Add(new RuntimeDefinitionSnapshot
                {
                    heroId = definition.id,
                    displayName = definition.displayName,
                    role = definition.role.ToString(),
                    maxHealth = definition.stats.maxHealth,
                    attackPower = definition.stats.attackPower,
                    attackSpeed = definition.stats.attackSpeed,
                    criticalChance = definition.stats.criticalChance,
                    criticalDamageMultiplier = definition.stats.criticalDamageMultiplier,
                    maxMana = definition.stats.maxMana,
                    manaRegenPerSecondRate = definition.stats.manaRegenPerSecondRate,
                    manaGainWhenHitRate = definition.stats.manaGainWhenHitRate,
                    manaGainPerAttackRate = definition.stats.manaGainPerAttackRate,
                    attackRange = definition.stats.attackRange,
                    skillEffect = skill != null ? skill.effectType.ToString() : string.Empty,
                    skillPower = skill != null ? skill.power : 0f,
                    skillSecondaryPower = skill != null ? skill.secondaryPower : 0f,
                    skillDuration = skill != null ? skill.duration : 0f,
                    skillRadius = skill != null ? skill.radius : 0f,
                    skillManaThreshold = skill != null ? skill.manaThreshold : 0f,
                    skillCooldown = skill != null ? skill.cooldown : 0f
                });
            }
            return snapshots;
        }

        private float CalculateAverageHero57Dps()
        {
            if (hero57SeedResults.Count == 0) return 0f;
            float total = 0f;
            for (int i = 0; i < hero57SeedResults.Count; i++) total += hero57SeedResults[i].averageDps;
            return total / hero57SeedResults.Count;
        }

        private float CalculateMinHero57Dps()
        {
            float result = float.MaxValue;
            for (int i = 0; i < hero57SeedResults.Count; i++) result = Mathf.Min(result, hero57SeedResults[i].averageDps);
            return result == float.MaxValue ? 0f : result;
        }

        private float CalculateMaxHero57Dps()
        {
            float result = 0f;
            for (int i = 0; i < hero57SeedResults.Count; i++) result = Mathf.Max(result, hero57SeedResults[i].averageDps);
            return result;
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
            hero56.probeWindowAverageDps = hero56.totalElapsedTime > 0f ? hero56.totalDamage / hero56.totalElapsedTime : 0f;
            float totalActiveTime = hero56.activeRoundCount * Hero56ActiveDuration;
            hero56.activeOnlyDps = totalActiveTime > 0f ? hero56.activeRoundDamage / totalActiveTime : 0f;
            hero56.normalizedDutyCycleDps = hero56.activeOnlyDps * 0.5f;
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
            if (currentSegment == Segment.GenericOffense && activeOffense != null)
            {
                activeOffense.eventDamage += damage;
                if (fromSkill) activeOffense.skillDamage += damage;
                else
                {
                    activeOffense.basicDamage += damage;
                    activeOffense.basicAttackCount++;
                }
                return;
            }

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

            if (currentSegment == Segment.GenericOffense && activeOffense != null)
            {
                activeOffense.skillCastCount++;
                return;
            }

            if (currentSegment == Segment.TankPressure && activeTank != null)
            {
                activeTank.skillCastCount++;
                activeTankSkillUntil = Mathf.Max(activeTankSkillUntil, Time.time + Mathf.Max(0f, skill.duration));
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

        private void HandleDamageTaken(DefenderUnit target, MonsterUnit source, float damage)
        {
            if (currentSegment != Segment.TankPressure || activeTank == null || target != trackedUnit || damage <= 0f)
            {
                return;
            }

            activeTank.totalDamageTaken += damage;
            activeTank.damageEventCount++;
            float elapsed = Time.time - activeTankStartTime;
            if (elapsed <= activeTank.testDuration * 0.33f)
            {
                activeTank.earlyDamage += damage;
                activeTank.earlyDamageEvents++;
            }
            else if (elapsed >= activeTank.testDuration * 0.66f)
            {
                activeTank.lateDamage += damage;
                activeTank.lateDamageEvents++;
            }
            if (Time.time <= activeTankSkillUntil)
            {
                activeTank.damageDuringSkill += damage;
                activeTank.damageEventsDuringSkill++;
            }
            else
            {
                activeTank.damageOutsideSkill += damage;
                activeTank.damageEventsOutsideSkill++;
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

        private MonsterUnit CreateDummy(string name, Vector3 position, bool isBoss = false, float attackPower = 0f, float attackSpeed = 0f)
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
                isBoss = isBoss,
                rewardGold = 0,
                stats = new CombatStats
                {
                    maxHealth = DummyHealth,
                    attackPower = attackPower,
                    attackSpeed = attackSpeed,
                    criticalChance = 0f,
                    criticalDamageMultiplier = 1f,
                    maxMana = 0f,
                    manaRegenPerSecondRate = 0f,
                    manaGainWhenHitRate = 0f,
                    manaGainPerAttackRate = 0f,
                    attackRange = 1.5f,
                    moveSpeed = 0f,
                    projectileSpeed = 0f
                },
                attackBehavior = new AttackBehavior { basicAttackType = BasicAttackType.Melee, useCustomAttackRange = true, customAttackRange = 1.5f },
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
        public List<RuntimeDefinitionSnapshot> runtimeDefinitions;
        public HeroCombatBenchmarkResult hero51;
        public HeroCombatBenchmarkResult hero52;
        public TankBenchmarkResult hero54;
        public TankBenchmarkResult hero55;
        public Hero57BenchmarkResult hero57;
        public OffenseComparisonResult offenseComparison;
        public TankComparisonResult tankComparison;
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
    internal sealed class OffenseComparisonResult
    {
        public float hero51SingleDps;
        public float hero51ControlUptimeRatio;
        public float hero51BossDps;
        public float hero52SingleDps;
        public float hero52ClusterDps;
        public float hero53SingleDps;
        public int hero56ClusterTargetCount;
        public float hero56ActiveOnlyDps;
        public float hero56NormalizedDutyCycleDps;
        public float hero57SingleDps;
        public float hero57ClusterAverageDps;
        public float hero57ClusterDpsSpread;
    }

    [Serializable]
    internal sealed class TankComparisonResult
    {
        public float hero54StartingHealth;
        public float hero54DamageTaken;
        public float hero54DamageDuringTaunt;
        public float hero54DamageOutsideTaunt;
        public float hero54EstimatedLateDamageReduction;
        public float hero55StartingHealth;
        public float hero55DamageTaken;
        public float hero55EarlyDamagePerHit;
        public float hero55LateDamagePerHit;
        public float hero55EstimatedLateDamageReduction;
        public float hero55KnockbackDistance;
    }    [Serializable]
    internal sealed class RuntimeDefinitionSnapshot
    {
        public string heroId;
        public string displayName;
        public string role;
        public float maxHealth;
        public float attackPower;
        public float attackSpeed;
        public float criticalChance;
        public float criticalDamageMultiplier;
        public float maxMana;
        public float manaRegenPerSecondRate;
        public float manaGainWhenHitRate;
        public float manaGainPerAttackRate;
        public float attackRange;
        public string skillEffect;
        public float skillPower;
        public float skillSecondaryPower;
        public float skillDuration;
        public float skillRadius;
        public float skillManaThreshold;
        public float skillCooldown;
    }

    [Serializable]
    internal sealed class HeroCombatBenchmarkResult
    {
        public string heroId;
        public string role;
        public OffenseRunResult regular;
        public OffenseRunResult boss;
        public OffenseRunResult single;
        public OffenseRunResult cluster;
    }

    [Serializable]
    internal sealed class OffenseRunResult
    {
        public string heroId;
        public bool usesRuntimeDefinition;
        public bool targetIsBoss;
        public int targetCount;
        public float testDuration;
        public float startingTargetHealth;
        public float totalDamage;
        public float eventDamage;
        public float averageDps;
        public float basicDamage;
        public float skillDamage;
        public int basicAttackCount;
        public int skillCastCount;
        public float statusUptime;
        public float statusUptimeRatio;
        public List<float> targetDamage = new List<float>();
    }

    [Serializable]
    internal sealed class TankBenchmarkResult
    {
        public string heroId;
        public string role;
        public string note;
        public bool usesRuntimeDefinition;
        public float testDuration;
        public float startingHealth;
        public float endingHealth;
        public bool survived;
        public int skillCastCount;
        public float totalDamageTaken;
        public int damageEventCount;
        public float earlyDamage;
        public int earlyDamageEvents;
        public float earlyDamagePerHit;
        public float lateDamage;
        public int lateDamageEvents;
        public float lateDamagePerHit;
        public float estimatedLateDamageReduction;
        public float damageDuringSkill;
        public int damageEventsDuringSkill;
        public float damageOutsideSkill;
        public int damageEventsOutsideSkill;
        public Vector3 knockbackTargetStart;
        public float knockbackDistance;
    }

    [Serializable]
    internal sealed class Hero57BenchmarkResult
    {
        public string heroId;
        public OffenseRunResult single;
        public List<Hero57SeedResult> seedResults;
        public float averageDps;
        public float minDps;
        public float maxDps;
        public float dpsSpread;
    }

    [Serializable]
    internal sealed class Hero57SeedResult
    {
        public int seed;
        public float totalDamage;
        public float averageDps;
        public float basicDamage;
        public float skillDamage;
        public int skillCastCount;
        public List<float> targetDamage;
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
        public float probeWindowAverageDps;
        public float activeOnlyDps;
        public float normalizedDutyCycleDps;
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