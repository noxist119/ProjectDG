using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DefenseGame.Editor
{
    public static class DefenseGameChoiceVarietySmoke
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string OutputDirectoryName = "BatchPlaytestResults";
        private const string OutputFileName = "DefenseGame_ChoiceVarietySmoke.json";

        [MenuItem("DefenseGame/Smoke Tests/Choice Variety")]
        public static void Run()
        {
            ChoiceVarietyReport report = new ChoiceVarietyReport();
            int exitCode = 0;
            try
            {
                ValidateShop(report);
                ValidateAugments(report);
                report.passed = report.shopProgressionValid &&
                    report.fourRunShopFreshnessValid &&
                    report.augmentProgressionValid &&
                    report.augmentFamilyHistoryValid;
                report.status = report.passed ? "pass" : "fail";
                exitCode = report.passed ? 0 : 1;
            }
            catch (Exception exception)
            {
                report.status = "exception";
                report.passed = false;
                report.failureReason = exception.ToString();
                exitCode = 1;
            }

            string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectoryName, OutputFileName));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
            Debug.Log("[ChoiceVarietySmoke] " + report.status + " / " + report.summary);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }

        private static void ValidateShop(ChoiceVarietyReport report)
        {
            const int round = 11;
            string historyKey = "DefenseGame.RetryShopHistory.regular." + round;
            string previousHistory = PlayerPrefs.GetString(historyKey, string.Empty);
            bool hadPreviousHistory = PlayerPrefs.HasKey(historyKey);
            GameObject root = new GameObject("ChoiceVarietyShopSmoke");
            try
            {
                PlayerPrefs.DeleteKey(historyKey);
                RunShopSystem shop = root.AddComponent<RunShopSystem>();
                MethodInfo buildPool = GetMethod(typeof(RunShopSystem), "BuildRegularShopPool");
                int earlyCount = ((IList)buildPool.Invoke(shop, new object[] { 4 })).Count;
                IList midPool = (IList)buildPool.Invoke(shop, new object[] { 11 });
                int midCount = midPool.Count;
                int lateCount = ((IList)buildPool.Invoke(shop, new object[] { 13 })).Count;
                bool hasEpicDraft = midPool.Cast<object>().Any(item => item.ToString() == "EpicDraft");
                bool hasBossRaidWager = midPool.Cast<object>().Any(item => item.ToString() == "BossRaidWager");
                report.shopProgressionValid = earlyCount == 14 && midCount == 20 && lateCount == 20 &&
                    hasEpicDraft && hasBossRaidWager;

                MethodInfo buildOffers = GetMethod(typeof(RunShopSystem), "BuildOffers");
                FieldInfo currentOffersField = GetField(typeof(RunShopSystem), "currentOffers");
                HashSet<string> allOffers = new HashSet<string>(StringComparer.Ordinal);
                List<string> runSummaries = new List<string>();
                bool duplicateFound = false;
                for (int run = 0; run < 4; run++)
                {
                    buildOffers.Invoke(shop, new object[] { round, false, false, false });
                    IList offers = (IList)currentOffersField.GetValue(shop);
                    List<string> names = GetOfferTypeNames(offers);
                    runSummaries.Add(string.Join("/", names.ToArray()));
                    for (int i = 0; i < names.Count; i++)
                    {
                        if (!allOffers.Add(names[i]))
                        {
                            duplicateFound = true;
                        }
                    }
                }

                report.fourRunShopFreshnessValid = !duplicateFound && allOffers.Count == 12;
                report.summary = "shopPool=" + earlyCount + ">" + midCount + ">" + lateCount +
                    ", R11LateOffers=" + hasEpicDraft + "/" + hasBossRaidWager +
                    ", fourRuns=" + string.Join(" | ", runSummaries.ToArray());
            }
            finally
            {
                RestorePlayerPref(historyKey, previousHistory, hadPreviousHistory);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateAugments(ChoiceVarietyReport report)
        {
            const int round = 5;
            string idHistoryKey = "DefenseGame.LastRunAugmentChoices." + round;
            string familyHistoryKey = "DefenseGame.RetryAugmentFamilies." + round;
            string previousIds = PlayerPrefs.GetString(idHistoryKey, string.Empty);
            string previousFamilies = PlayerPrefs.GetString(familyHistoryKey, string.Empty);
            bool hadIds = PlayerPrefs.HasKey(idHistoryKey);
            bool hadFamilies = PlayerPrefs.HasKey(familyHistoryKey);
            GameObject root = new GameObject("ChoiceVarietyAugmentSmoke");
            try
            {
                PlayerPrefs.DeleteKey(idHistoryKey);
                PlayerPrefs.DeleteKey(familyHistoryKey);
                AugmentManager manager = root.AddComponent<AugmentManager>();
                GetMethod(typeof(AugmentManager), "EnsureDefaultPool").Invoke(manager, null);
                IList pool = (IList)GetField(typeof(AugmentManager), "augmentPool").GetValue(manager);

                Dictionary<string, int> expectedRounds = new Dictionary<string, int>
                {
                    { "merge_combo_engine", 3 },
                    { "merge_dividend", 5 },
                    { "boss_trophy_growth", 8 },
                    { "berserker_threshold", 6 },
                    { "forbidden_overcharge", 8 },
                    { "arcane_domino", 9 }
                };
                Dictionary<string, AugmentDefinition> definitions = new Dictionary<string, AugmentDefinition>();
                foreach (object item in pool)
                {
                    AugmentDefinition augment = item as AugmentDefinition;
                    if (augment != null && expectedRounds.ContainsKey(augment.id))
                    {
                        definitions[augment.id] = augment;
                    }
                }

                report.augmentProgressionValid = definitions.Count == expectedRounds.Count &&
                    expectedRounds.All(pair => definitions[pair.Key].minimumRound == pair.Value);

                IList currentChoices = (IList)GetField(typeof(AugmentManager), "currentChoices").GetValue(manager);
                currentChoices.Add(definitions["merge_combo_engine"]);
                GetMethod(typeof(AugmentManager), "SaveLastRunChoiceIds").Invoke(manager, new object[] { round });
                currentChoices.Clear();
                GetMethod(typeof(AugmentManager), "LoadLastRunChoiceIds").Invoke(manager, new object[] { round });
                bool exactBlocked = (bool)GetMethod(typeof(AugmentManager), "WasLastRunChoice")
                    .Invoke(manager, new object[] { definitions["merge_combo_engine"] });
                bool familyBlocked = (bool)GetMethod(typeof(AugmentManager), "WasLastRunChoice")
                    .Invoke(manager, new object[] { definitions["merge_dividend"] });
                report.augmentFamilyHistoryValid = exactBlocked && familyBlocked;
            }
            finally
            {
                RestorePlayerPref(idHistoryKey, previousIds, hadIds);
                RestorePlayerPref(familyHistoryKey, previousFamilies, hadFamilies);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static List<string> GetOfferTypeNames(IList offers)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < offers.Count; i++)
            {
                object offer = offers[i];
                FieldInfo typeField = offer.GetType().GetField("type", InstancePrivate | BindingFlags.Public);
                names.Add(typeField.GetValue(offer).ToString());
            }

            return names;
        }

        private static MethodInfo GetMethod(Type type, string name)
        {
            MethodInfo method = type.GetMethod(name, InstancePrivate);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, name);
            }

            return method;
        }

        private static FieldInfo GetField(Type type, string name)
        {
            FieldInfo field = type.GetField(name, InstancePrivate);
            if (field == null)
            {
                throw new MissingFieldException(type.FullName, name);
            }

            return field;
        }

        private static void RestorePlayerPref(string key, string value, bool existed)
        {
            if (existed)
            {
                PlayerPrefs.SetString(key, value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class ChoiceVarietyReport
        {
            public string status;
            public bool passed;
            public bool shopProgressionValid;
            public bool fourRunShopFreshnessValid;
            public bool augmentProgressionValid;
            public bool augmentFamilyHistoryValid;
            public string summary;
            public string failureReason;
        }
    }

    public static class DefenseGameBatchModeEntry
    {
        private static bool scheduled;

        public static void RunOverdrivePairedStrategies30()
        {
            if (scheduled)
            {
                return;
            }

            scheduled = true;
            EditorApplication.delayCall += StartOverdrivePairedStrategies30;
        }

        private static void StartOverdrivePairedStrategies30()
        {
            EditorApplication.delayCall -= StartOverdrivePairedStrategies30;
            DefenseGameBatchPlaytest.RunOverdrivePairedStrategies30();
        }

        public static void RunOverdriveFairPairedStrategies30()
        {
            if (scheduled)
            {
                return;
            }

            scheduled = true;
            EditorApplication.delayCall += StartOverdriveFairPairedStrategies30;
        }

        private static void StartOverdriveFairPairedStrategies30()
        {
            EditorApplication.delayCall -= StartOverdriveFairPairedStrategies30;
            DefenseGameBatchPlaytest.RunOverdriveFairPairedStrategies30();
        }

        public static void RunChoiceVarietySmoke()
        {
            if (scheduled)
            {
                return;
            }

            scheduled = true;
            EditorApplication.delayCall += StartChoiceVarietySmoke;
        }

        private static void StartChoiceVarietySmoke()
        {
            EditorApplication.delayCall -= StartChoiceVarietySmoke;
            DefenseGameChoiceVarietySmoke.Run();
        }
    }}
