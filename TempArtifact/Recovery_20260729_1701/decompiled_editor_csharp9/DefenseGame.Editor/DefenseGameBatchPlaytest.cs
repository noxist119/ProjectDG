using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame.Editor
{
	public static class DefenseGameBatchPlaytest
	{
		[Serializable]
		private sealed class RunResult
		{
			public int index;

			public string strategy;

			public int startGold;

			public int reachedRound;

			public bool clearedR10;

			public int summons;

			public int merges;

			public int shopPurchases;

			public int shopGoldSpent;

			public int fateUses;

			public bool r3ShopSeen;

			public bool r6ShopSeen;

			public int firstRarePlusRound;

			public int firstMergeRound;

			public string fateCardTitle = string.Empty;

			public string fateCardDetail = string.Empty;

			public int fateCardDebt;

			public int fateChoiceIndex = -1;

			public int fateActivationRound;

			public string fateTrigger = string.Empty;

			public int endGold;

			public int endLife;

			public float r10BossHealthRemaining01 = -1f;

			public bool timeout;

			public readonly List<string> notes = new List<string>();
		}

		private const int TargetRuns = 20;

		private const int TargetRound = 10;

		private const float BatchTimeScale = 40f;

		private const float BatchFixedDeltaTime = 0.025f;

		private const float BatchMaximumDeltaTime = 0.33f;

		private const double RoundTimeoutSeconds = 45.0;

		private const double RunTimeoutSeconds = 300.0;

		private const string ScenePath = "Assets/Scenes/DG.unity";

		private const string OutputDirectoryName = "BatchPlaytestResults";

		private const string OutputFileName = "DefenseGame_Playtest20_Human3.json";

		private const string MissingScriptReportFileName = "RuntimeMissingScripts.json";

		private static readonly List<RunResult> Results = new List<RunResult>();

		private static DefenseGameController controller;

		private static RunResult current;

		private static int runIndex;

		private static double nextActionTime;

		private static double runStartEditorTime;

		private static double roundStartEditorTime;

		private static int lastObservedRound;

		private static bool waitingRoundEnd;

		private static bool started;

		private static bool previousEnterPlayModeOptionsEnabled;

		private static EnterPlayModeOptions previousEnterPlayModeOptions;

		private static LogType previousFilterLogType;

		private static float previousAudioListenerVolume = 1f;

		private static bool previousAudioListenerPause;

		private static float previousFixedDeltaTime;

		private static float previousMaximumDeltaTime;

		private static int previousTargetFrameRate;

		private static int previousVSyncCount;

		private static bool previousRunInBackground;

		private static int missingScriptWarningsObserved;

		private static readonly List<string> MissingScriptWarningSamples = new List<string>();

		private static string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BatchPlaytestResults"));

		private static string OutputPath => Path.Combine(OutputDirectory, "DefenseGame_Playtest20_Human3.json");

		private static string MissingScriptReportPath => Path.Combine(OutputDirectory, "RuntimeMissingScripts.json");

		[MenuItem("DefenseGame/Batch Playtest/Run Human Strategies 20 Total")]
		public static void RunHumanStrategies20()
		{
			Results.Clear();
			controller = null;
			current = null;
			runIndex = 0;
			nextActionTime = 0.0;
			runStartEditorTime = 0.0;
			roundStartEditorTime = 0.0;
			lastObservedRound = 0;
			waitingRoundEnd = false;
			started = false;
			missingScriptWarningsObserved = 0;
			MissingScriptWarningSamples.Clear();
			Directory.CreateDirectory(OutputDirectory);
			if (File.Exists(OutputPath))
			{
				File.Delete(OutputPath);
			}
			if (File.Exists(OutputPath + ".partial"))
			{
				File.Delete(OutputPath + ".partial");
			}
			if (File.Exists(MissingScriptReportPath))
			{
				File.Delete(MissingScriptReportPath);
			}
			previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
			previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
			previousFilterLogType = Debug.unityLogger.filterLogType;
			previousAudioListenerVolume = AudioListener.volume;
			previousAudioListenerPause = AudioListener.pause;
			previousFixedDeltaTime = Time.fixedDeltaTime;
			previousMaximumDeltaTime = Time.maximumDeltaTime;
			previousTargetFrameRate = Application.targetFrameRate;
			previousVSyncCount = QualitySettings.vSyncCount;
			previousRunInBackground = Application.runInBackground;
			AudioListener.volume = 0f;
			AudioListener.pause = true;
			Time.fixedDeltaTime = 0.025f;
			Time.maximumDeltaTime = 0.33f;
			Application.targetFrameRate = 240;
			Application.runInBackground = true;
			QualitySettings.vSyncCount = 0;
			EditorSettings.enterPlayModeOptionsEnabled = true;
			EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
			Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
			Debug.unityLogger.filterLogType = LogType.Error;
			Application.logMessageReceived -= HandleLogMessage;
			Application.logMessageReceived += HandleLogMessage;
			EditorSceneManager.OpenScene("Assets/Scenes/DG.unity");
			EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
			EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
			EditorApplication.isPlaying = true;
		}

		private static void HandlePlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				nextActionTime = EditorApplication.timeSinceStartup + 1.0;
			}
		}

		private static void ApplyBatchSpeedSettings()
		{
			if (!Mathf.Approximately(Time.timeScale, 40f))
			{
				Time.timeScale = 40f;
			}
			if (!Mathf.Approximately(Time.fixedDeltaTime, 0.025f))
			{
				Time.fixedDeltaTime = 0.025f;
			}
			if (!Mathf.Approximately(Time.maximumDeltaTime, 0.33f))
			{
				Time.maximumDeltaTime = 0.33f;
			}
			Application.targetFrameRate = 240;
			Application.runInBackground = true;
			QualitySettings.vSyncCount = 0;
		}

		private static void Tick()
		{
			if (!EditorApplication.isPlaying)
			{
				return;
			}
			ApplyBatchSpeedSettings();
			if (EditorApplication.timeSinceStartup < nextActionTime)
			{
				return;
			}
			if (!started)
			{
				controller = UnityEngine.Object.FindObjectOfType<DefenseGameController>();
				if ((UnityEngine.Object)(object)controller == null)
				{
					nextActionTime = EditorApplication.timeSinceStartup + 0.5;
					return;
				}
				ApplyBatchSpeedSettings();
				started = true;
				WriteRuntimeMissingScriptReport("batch_start");
				StartRun();
				return;
			}
			if ((UnityEngine.Object)(object)controller == null)
			{
				FinishAll("controller_missing");
				return;
			}
			if (current != null && EditorApplication.timeSinceStartup - runStartEditorTime > 300.0)
			{
				current.timeout = true;
				current.notes.Add("run_timeout_R" + Mathf.Max(1, controller.CurrentRound));
				CompleteRun();
				return;
			}
			CloseResultOverlayIfOpen();
			ChooseAugmentIfOpen();
			if (waitingRoundEnd)
			{
				if (controller.Life <= 0)
				{
					current.notes.Add("life_zero_during_round_R" + lastObservedRound);
					CompleteRun();
				}
				else if (controller.IsRoundRunning)
				{
					TryUsePreferredFateCardByStrategy();
					if (EditorApplication.timeSinceStartup - roundStartEditorTime > 45.0)
					{
						current.timeout = true;
						current.r10BossHealthRemaining01 = ResolveRemainingBossHealth01();
						current.notes.Add("round_timeout_R" + lastObservedRound + "_bossHp_" + FormatFloat(current.r10BossHealthRemaining01));
						CompleteRun();
					}
					nextActionTime = EditorApplication.timeSinceStartup + 0.15;
				}
				else
				{
					current.reachedRound = Mathf.Max(current.reachedRound, controller.CurrentRound);
					current.endGold = controller.Gold;
					current.endLife = controller.Life;
					current.r10BossHealthRemaining01 = ResolveRemainingBossHealth01();
					if (lastObservedRound >= 10)
					{
						CompleteRun();
						return;
					}
					waitingRoundEnd = false;
					nextActionTime = EditorApplication.timeSinceStartup + 0.35;
				}
			}
			else if (controller.Life <= 0 || controller.CurrentRound > 10)
			{
				CompleteRun();
			}
			else
			{
				HandleShopIfOpen();
				CloseResultOverlayIfOpen();
				ChooseAugmentIfOpen();
				TryUsePreferredFateCardByStrategy();
				TryUseFateSummonQualityBoost();
				TryUseFateSurvivalInCrisis();
				ExecutePrepPolicy();
				StartNextRound();
			}
		}

		private static void StartRun()
		{
			UnityEngine.Random.InitState(90210 + runIndex * 7919);
			current = new RunResult
			{
				index = runIndex + 1,
				strategy = ResolveStrategy(runIndex),
				startGold = controller.Gold
			};
			controller.ResetRunForRetry();
			runStartEditorTime = EditorApplication.timeSinceStartup;
			current.startGold = controller.Gold;
			lastObservedRound = controller.CurrentRound;
			waitingRoundEnd = false;
			nextActionTime = EditorApplication.timeSinceStartup + 0.3;
		}

		private static string ResolveStrategy(int index)
		{
			return (index % 3) switch
			{
				0 => "summon-heavy", 
				1 => "balanced", 
				_ => "shop-save", 
			};
		}

		private static void CompleteRun()
		{
			current.reachedRound = Mathf.Max(current.reachedRound, controller.CurrentRound);
			current.endGold = controller.Gold;
			current.endLife = controller.Life;
			current.r10BossHealthRemaining01 = ResolveRemainingBossHealth01();
			CaptureFateCardSnapshot("complete");
			current.clearedR10 = controller.Life > 0 && lastObservedRound >= 10 && !current.timeout;
			Results.Add(current);
			File.WriteAllText(OutputPath + ".partial", BuildJson("partial"), Encoding.UTF8);
			runIndex++;
			if (runIndex >= 20)
			{
				FinishAll("complete");
			}
			else
			{
				StartRun();
			}
		}

		private static void FinishAll(string status)
		{
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
			EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
			string json = BuildJson(status);
			File.WriteAllText(OutputPath, json, Encoding.UTF8);
			WriteRuntimeMissingScriptReport("batch_finish");
			Time.timeScale = 1f;
			Time.fixedDeltaTime = previousFixedDeltaTime;
			Time.maximumDeltaTime = previousMaximumDeltaTime;
			AudioListener.volume = previousAudioListenerVolume;
			AudioListener.pause = previousAudioListenerPause;
			Application.targetFrameRate = previousTargetFrameRate;
			Application.runInBackground = previousRunInBackground;
			QualitySettings.vSyncCount = previousVSyncCount;
			Debug.unityLogger.filterLogType = previousFilterLogType;
			EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
			EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
			Application.logMessageReceived -= HandleLogMessage;
			Debug.Log("[DefenseGameBatchPlaytest] wrote " + OutputPath + "\n" + json);
			EditorApplication.isPlaying = false;
			if (Application.isBatchMode)
			{
				EditorApplication.Exit((!(status == "complete")) ? 1 : 0);
			}
		}

		private static void ExecutePrepPolicy()
		{
			TryMaintainHumanMergeTempo();
			int reserve = ResolveGoldReserve();
			for (int safetyLoops = 0; safetyLoops < 24; safetyLoops++)
			{
				int summonCost = controller.SummonCost;
				if (controller.Gold < summonCost || (controller.Gold - summonCost < reserve && controller.BoardUnitCount >= MinimumBoardUnits()) || (controller.EmptySlotCount <= 0 && !TryMergeOneAvailable()) || controller.EmptySlotCount <= 0 || !controller.TrySummon())
				{
					break;
				}
				current.summons++;
				RefreshFirstRareRound();
				TryMaintainHumanMergeTempo();
			}
			TryMaintainHumanMergeTempo();
			RefreshFirstRareRound();
		}

		private static void TryMaintainHumanMergeTempo()
		{
			if ((UnityEngine.Object)(object)controller == null || current == null)
			{
				return;
			}
			for (int safetyLoops = 0; safetyLoops < 8; safetyLoops++)
			{
				if (controller.BoardUnitCount < MinimumBoardUnits() + 2)
				{
					break;
				}
				int beforeCount = controller.BoardUnitCount;
				bool merged = TryMerge((CharacterGrade)0) || TryMerge((CharacterGrade)1) || TryMerge((CharacterGrade)2);
				int nextRound = controller.CurrentRound + 1;
				if (!merged && (nextRound >= 8 || controller.BoardUnitCount >= MinimumBoardUnits() + 4))
				{
					merged = TryMerge((CharacterGrade)3);
				}
				if (!merged || controller.BoardUnitCount >= beforeCount)
				{
					break;
				}
			}
		}

		private static bool TryMergeOneAvailable()
		{
			return TryMerge((CharacterGrade)0) || TryMerge((CharacterGrade)1) || TryMerge((CharacterGrade)2) || TryMerge((CharacterGrade)3);
		}

		private static void TryUseFateSurvivalInCrisis()
		{
			if ((UnityEngine.Object)(object)controller == null || current == null || current.fateUses > 0)
			{
				return;
			}
			int nextRound = controller.CurrentRound + 1;
			if (nextRound >= 5 && nextRound <= 9 && controller.MaxLife > 0)
			{
				float lifeRatio = (float)controller.Life / (float)controller.MaxLife;
				float threshold = ((current.strategy == "shop-save") ? 0.78f : ((current.strategy == "balanced") ? 0.75f : 0.72f));
				if (!(lifeRatio > threshold) && controller.CanUseFateSurvival && controller.TryActivateFateSurvival())
				{
					RecordFateUse("crisis_slot0", 0);
				}
			}
		}

		private static void TryUsePreferredFateCardByStrategy()
		{
			if ((UnityEngine.Object)(object)controller == null || current == null || current.fateUses > 0 || !controller.CanOpenFateCard)
			{
				return;
			}
			int nextRound = controller.CurrentRound + 1;
			if (ShouldUseFateCardForStrategy(nextRound))
			{
				int choiceIndex = FindPreferredFateChoiceIndex(ResolveFateCardPreferences());
				if (controller.TryOpenFateCardChoicePanel() && choiceIndex >= 0 && controller.TryActivateFateCardChoice(choiceIndex))
				{
					RecordFateUse("strategy_prefer", choiceIndex);
				}
			}
		}

		private static bool ShouldUseFateCardForStrategy(int nextRound)
		{
			if ((UnityEngine.Object)(object)controller == null || current == null || !controller.IsRoundRunning || controller.CurrentRound < 5)
			{
				return false;
			}
			float lifeRatio = ((controller.MaxLife > 0) ? ((float)controller.Life / (float)controller.MaxLife) : 1f);
			if (controller.CurrentRound >= 10)
			{
				return true;
			}
			if (current.strategy == "summon-heavy")
			{
				return controller.CurrentRound >= 7 && lifeRatio <= 0.75f;
			}
			if (current.strategy == "balanced")
			{
				return controller.CurrentRound >= 6 && lifeRatio <= 0.78f;
			}
			if (current.strategy == "shop-save")
			{
				return controller.CurrentRound >= 6 && (lifeRatio <= 0.82f || controller.BoardUnitCount < MinimumBoardUnits());
			}
			return false;
		}

		private static string[] ResolveFateCardPreferences()
		{
			if (current == null)
			{
				return new string[0];
			}
			if (current.strategy == "summon-heavy")
			{
				return new string[9] { "등급 조작", "에픽 선불", "용병 호출", "황금 대출", "밀수 루트", "응급 방벽", "왕의 공포", "금단의 소환", "전장 개방" };
			}
			if (current.strategy == "balanced")
			{
				return new string[11]
				{
					"응급 방벽", "황금 대출", "용병 호출", "등급 조작", "에픽 선불", "밀수 루트", "왕의 공포", "최후의 방벽", "피의 계약", "암시장 개장",
					"생명 주조"
				};
			}
			return new string[11]
			{
				"암시장 개장", "황금 대출", "응급 방벽", "용병 호출", "등급 조작", "밀수 루트", "왕의 공포", "최후의 방벽", "피의 계약", "전장 개방",
				"생명 주조"
			};
		}

		private static int FindPreferredFateChoiceIndex(string[] preferences)
		{
			if ((UnityEngine.Object)(object)controller == null || preferences == null || preferences.Length == 0)
			{
				return -1;
			}
			for (int p = 0; p < preferences.Length; p++)
			{
				for (int i = 0; i < 3; i++)
				{
					string label = controller.GetFateCardChoiceHudLabel(i);
					if (!string.IsNullOrWhiteSpace(label) && !ShouldSkipRiskyFateChoice(label) && label.IndexOf(preferences[p], StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return i;
					}
				}
			}
			for (int j = 0; j < 3; j++)
			{
				string label2 = controller.GetFateCardChoiceHudLabel(j);
				if (!string.IsNullOrWhiteSpace(label2) && !ShouldSkipRiskyFateChoice(label2))
				{
					return j;
				}
			}
			for (int k = 0; k < 3; k++)
			{
				if (!string.IsNullOrWhiteSpace(controller.GetFateCardChoiceHudLabel(k)))
				{
					return k;
				}
			}
			return -1;
		}

		private static bool ShouldSkipRiskyFateChoice(string label)
		{
			if ((UnityEngine.Object)(object)controller == null || string.IsNullOrWhiteSpace(label))
			{
				return false;
			}
			int nextRound = controller.CurrentRound + 1;
			float lifeRatio = ((controller.MaxLife > 0) ? ((float)controller.Life / (float)controller.MaxLife) : 1f);
			if (ContainsAny(label, "시간 정지", "심판 번개") && !controller.IsRoundRunning)
			{
				return false;
			}
			if (ContainsAny(label, "생명 주조") && nextRound <= 6 && controller.Gold >= controller.SummonCost * 2)
			{
				return true;
			}
			if (!ContainsAny(label, "HP-", "HP -", "HP 절반", "HP 1", "라이프 -", "라이프-", "도박"))
			{
				return false;
			}
			return nextRound <= 4 || lifeRatio <= 0.85f || ContainsAny(label, "도박사의 판");
		}

		private static void RecordFateUse(string trigger, int choiceIndex)
		{
			if (current != null && !((UnityEngine.Object)(object)controller == null))
			{
				current.fateUses++;
				current.fateTrigger = trigger ?? string.Empty;
				current.fateChoiceIndex = choiceIndex;
				current.fateActivationRound = Mathf.Max(1, controller.CurrentRound);
				CaptureFateCardSnapshot(trigger);
				if (!string.IsNullOrWhiteSpace(current.fateCardTitle))
				{
					current.notes.Add("fate_" + current.fateActivationRound + "_" + current.fateCardTitle);
				}
			}
		}

		private static void CaptureFateCardSnapshot(string trigger)
		{
			if (current != null && !((UnityEngine.Object)(object)controller == null) && controller.FateCardWasUsed)
			{
				current.fateCardTitle = controller.FateCardLastTitle;
				current.fateCardDetail = controller.FateCardLastDetail;
				current.fateCardDebt = controller.FateCardLastDebt;
				if (string.IsNullOrWhiteSpace(current.fateTrigger))
				{
					current.fateTrigger = trigger ?? string.Empty;
				}
			}
		}

		private static void TryUseFateSummonQualityBoost()
		{
			if (!((UnityEngine.Object)(object)controller == null) && current != null && !(current.strategy != "summon-heavy") && !controller.CanUseFateCard)
			{
				int nextRound = controller.CurrentRound + 1;
				if (nextRound >= 3 && nextRound <= 6 && current.fateUses <= 0 && controller.CanUseFateNormalBan && controller.TryActivateFateNormalBan(4))
				{
					RecordFateUse("summon_quality_slot2", 2);
				}
			}
		}

		private static int ResolveGoldReserve()
		{
			if (current.strategy == "summon-heavy")
			{
				return 0;
			}
			if (current.strategy == "balanced")
			{
				int nextBalancedRound = controller.CurrentRound + 1;
				if (nextBalancedRound <= 3)
				{
					return controller.SummonCost * 2;
				}
				return controller.SummonCost;
			}
			int nextRound = controller.CurrentRound + 1;
			if (nextRound <= 3)
			{
				return (current.strategy == "shop-save") ? 10 : 12;
			}
			if (nextRound <= 6)
			{
				return (current.strategy == "shop-save") ? 22 : 32;
			}
			return 20;
		}

		private static int MinimumBoardUnits()
		{
			int nextRound = controller.CurrentRound + 1;
			if (nextRound <= 2)
			{
				return 3;
			}
			if (current != null && current.strategy == "shop-save" && nextRound <= 5)
			{
				return 6;
			}
			if (nextRound <= 5)
			{
				return 5;
			}
			return 7;
		}

		private static void MergeAllPossible()
		{
			for (int pass = 0; pass < 4; pass++)
			{
				bool merged = false;
				merged |= TryMerge((CharacterGrade)0);
				merged |= TryMerge((CharacterGrade)1);
				merged |= TryMerge((CharacterGrade)2);
				if (!(merged | TryMerge((CharacterGrade)3)))
				{
					break;
				}
			}
		}

		private static bool TryMerge(CharacterGrade grade)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (!controller.TryMerge(grade))
			{
				return false;
			}
			current.merges++;
			if (current.firstMergeRound <= 0)
			{
				current.firstMergeRound = Mathf.Max(1, controller.CurrentRound + 1);
			}
			RefreshFirstRareRound();
			return true;
		}

		private static void RefreshFirstRareRound()
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Invalid comparison between Unknown and I4
			if (current.firstRarePlusRound > 0)
			{
				return;
			}
			DefenderUnit[] units = UnityEngine.Object.FindObjectsOfType<DefenderUnit>();
			for (int i = 0; i < units.Length; i++)
			{
				if ((UnityEngine.Object)(object)units[i] != null && (int)units[i].Grade >= 1)
				{
					current.firstRarePlusRound = Mathf.Max(1, controller.CurrentRound + 1);
					break;
				}
			}
		}

		private static void StartNextRound()
		{
			lastObservedRound = controller.CurrentRound + 1;
			roundStartEditorTime = EditorApplication.timeSinceStartup;
			controller.StartRound();
			waitingRoundEnd = true;
			nextActionTime = EditorApplication.timeSinceStartup + 0.2;
		}

		private static float ResolveRemainingBossHealth01()
		{
			float highest = 0f;
			bool found = false;
			IReadOnlyList<MonsterUnit> monsters = MonsterUnit.ActiveInstances;
			for (int i = 0; i < monsters.Count; i++)
			{
				MonsterUnit monster = monsters[i];
				if (!((UnityEngine.Object)(object)monster == null) && monster.IsBoss && !(monster.MaxHealth <= 0f))
				{
					found = true;
					highest = Mathf.Max(highest, Mathf.Clamp01(monster.CurrentHealth / monster.MaxHealth));
				}
			}
			return found ? highest : 0f;
		}

		private static void HandleShopIfOpen()
		{
			GameObject overlay = GameObject.Find("RunShopOverlay");
			if (overlay == null || !overlay.activeInHierarchy)
			{
				return;
			}
			int round = Mathf.Max(1, controller.CurrentRound);
			if (round == 3)
			{
				current.r3ShopSeen = true;
			}
			if (round == 6)
			{
				current.r6ShopSeen = true;
			}
			bool shopFocused = current.strategy == "shop-save";
			bool shouldConsiderPurchase = current.shopPurchases < ResolveShopPurchaseLimit();
			bool bought = false;
			if (shouldConsiderPurchase)
			{
				Button[] buttons = overlay.GetComponentsInChildren<Button>(includeInactive: true);
				Button bestButton = null;
				int bestCost = 0;
				int bestScore = int.MinValue;
				foreach (Button button in buttons)
				{
					if ((UnityEngine.Object)(object)button == null || !((Component)(object)button).gameObject.activeInHierarchy || !((UnityEngine.Object)(object)button).name.StartsWith("RunShopOffer_", StringComparison.Ordinal))
					{
						continue;
					}
					int offerCost = ResolveOfferGoldCost(button);
					int reserve = ResolveShopGoldReserve(shopFocused, round);
					if (offerCost >= 0 && controller.Gold >= offerCost + reserve)
					{
						int score = ScoreShopOfferForStrategy(button, offerCost, shopFocused);
						int minimumScore = ResolveShopMinimumScore(shopFocused, offerCost);
						if (score >= minimumScore && score > bestScore)
						{
							bestButton = button;
							bestCost = offerCost;
							bestScore = score;
						}
					}
				}
				if ((UnityEngine.Object)(object)bestButton != null)
				{
					int before = controller.Gold;
					((UnityEvent)(object)bestButton.onClick).Invoke();
					if (controller.Gold < before || overlay == null || !overlay.activeInHierarchy || !((Component)(object)bestButton).gameObject.activeInHierarchy)
					{
						bought = true;
						current.shopPurchases++;
						current.shopGoldSpent += before - controller.Gold;
						current.notes.Add("shop_" + Mathf.Max(1, controller.CurrentRound) + "_" + CompactNote(BuildButtonSearchText(bestButton), 24) + "_" + bestCost + "G");
					}
				}
			}
			if (!bought)
			{
				Button close = FindButton("RunShopCloseButton");
				if ((UnityEngine.Object)(object)close != null)
				{
					((UnityEvent)(object)close.onClick).Invoke();
				}
			}
		}

		private static int ResolveShopPurchaseLimit()
		{
			if (current == null)
			{
				return 0;
			}
			if (current.strategy == "shop-save")
			{
				return 2;
			}
			return 1;
		}

		private static int ResolveShopGoldReserve(bool shopFocused, int round)
		{
			if ((UnityEngine.Object)(object)controller == null || current == null)
			{
				return 0;
			}
			if (shopFocused)
			{
				return (round > 3) ? Mathf.Max(0, controller.SummonCost / 2) : 0;
			}
			if (current.strategy == "balanced")
			{
				return (round <= 3) ? Mathf.Max(0, controller.SummonCost / 2) : Mathf.Max(0, controller.SummonCost);
			}
			return (round > 3) ? Mathf.Max(0, controller.SummonCost) : 0;
		}

		private static int ResolveShopMinimumScore(bool shopFocused, int offerCost)
		{
			if (shopFocused)
			{
				return (offerCost <= 0) ? 20 : 35;
			}
			if (current != null && current.strategy == "summon-heavy")
			{
				return (offerCost <= 0) ? 35 : 65;
			}
			return (offerCost <= 0) ? 30 : 45;
		}

		private static int ResolveOfferGoldCost(Button button)
		{
			if ((UnityEngine.Object)(object)button == null)
			{
				return -1;
			}
			Text[] texts = ((Component)(object)button).GetComponentsInChildren<Text>(true);
			foreach (Text text in texts)
			{
				if ((UnityEngine.Object)(object)text == null || !string.Equals(((UnityEngine.Object)(object)text).name, "Price", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(text.text))
				{
					continue;
				}
				int goldMarker = text.text.IndexOf('G');
				if (goldMarker > 0)
				{
					string numeric = text.text.Substring(0, goldMarker).Trim();
					if (int.TryParse(numeric, out var cost))
					{
						return Mathf.Max(0, cost);
					}
				}
				if (ContainsAny(text.text, "무료", "보험", "운명", "라이프", "HP -"))
				{
					return 0;
				}
				if (ContainsAny(text.text, "골드+HP"))
				{
					return 999;
				}
			}
			return -1;
		}

		private static int ScoreShopOfferForStrategy(Button button, int offerCost, bool shopFocused)
		{
			string text = BuildButtonSearchText(button);
			bool risky = ContainsAny(text, "위험", "HP -", "HP-", "라이프 -", "라이프-", "빚", "계약");
			int score = ((offerCost <= 0) ? 70 : Mathf.Max(0, 125 - offerCost));
			score += (ContainsAny(text, "보험", "구제", "대응권") ? 92 : 0);
			score += (ContainsAny(text, "긴급 소환", "소환권", "레어 보급", "레어 지원", "용병") ? 70 : 0);
			score += (ContainsAny(text, "보스 대비", "보스 정보", "보스 피해", "화력", "증강체") ? 62 : 0);
			score += (ContainsAny(text, "합성", "재료", "부스터") ? 60 : 0);
			score += (ContainsAny(text, "쿠폰", "할인", "소환비") ? 42 : 0);
			score += (ContainsAny(text, "회복", "의무병", "수호", "방벽", "체력") ? 32 : 0);
			if (shopFocused)
			{
				score += (ContainsAny(text, "구제", "레어", "용병") ? 90 : 0);
				score += (ContainsAny(text, "회복", "의무병", "수호", "방벽", "체력") ? 72 : 0);
				score += (ContainsAny(text, "합성", "재료", "Merge") ? 58 : 0);
				score += (ContainsAny(text, "할인", "쿠폰", "소환비") ? 42 : 0);
				score += (ContainsAny(text, "화력", "스킬", "공격") ? 32 : 0);
				return score - (risky ? ResolveRiskyShopPenalty() : 0);
			}
			score += (ContainsAny(text, "합성", "재료", "레어", "화력", "스킬") ? 42 : 0);
			score += (ContainsAny(text, "회복", "수호") ? 22 : 0);
			return score - (risky ? ResolveRiskyShopPenalty() : 0);
		}

		private static int ResolveRiskyShopPenalty()
		{
			if ((UnityEngine.Object)(object)controller == null || controller.MaxLife <= 0)
			{
				return 120;
			}
			float lifeRatio = (float)controller.Life / (float)controller.MaxLife;
			return (lifeRatio <= 0.55f) ? 190 : ((lifeRatio <= 0.75f) ? 145 : 105);
		}

		private static string BuildButtonSearchText(Button button)
		{
			if ((UnityEngine.Object)(object)button == null)
			{
				return string.Empty;
			}
			Text[] texts = ((Component)(object)button).GetComponentsInChildren<Text>(true);
			StringBuilder builder = new StringBuilder(128);
			foreach (Text text in texts)
			{
				if (!((UnityEngine.Object)(object)text == null) && !string.IsNullOrWhiteSpace(text.text))
				{
					if (builder.Length > 0)
					{
						builder.Append(' ');
					}
					builder.Append(text.text);
				}
			}
			return builder.ToString();
		}

		private static bool ContainsAny(string text, params string[] tokens)
		{
			if (string.IsNullOrWhiteSpace(text) || tokens == null)
			{
				return false;
			}
			for (int i = 0; i < tokens.Length; i++)
			{
				if (!string.IsNullOrWhiteSpace(tokens[i]) && text.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		private static void ChooseAugmentIfOpen()
		{
			GameObject overlay = GameObject.Find("AugmentChoiceOverlay");
			if (!(overlay == null) && overlay.activeInHierarchy)
			{
				Button choice = FindButton("AugmentChoice_0");
				if ((UnityEngine.Object)(object)choice != null && ((Component)(object)choice).gameObject.activeInHierarchy)
				{
					((UnityEvent)(object)choice.onClick).Invoke();
				}
			}
		}

		private static void CloseResultOverlayIfOpen()
		{
			GameObject overlay = GameObject.Find("RoundResultOverlay");
			if (!(overlay == null) && overlay.activeInHierarchy)
			{
				Button continueButton = FindButton("ResultContinueButton");
				if ((UnityEngine.Object)(object)continueButton != null && ((Component)(object)continueButton).gameObject.activeInHierarchy)
				{
					((UnityEvent)(object)continueButton.onClick).Invoke();
				}
			}
		}

		private static Button FindButton(string name)
		{
			Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>(true);
			for (int i = 0; i < buttons.Length; i++)
			{
				if ((UnityEngine.Object)(object)buttons[i] != null && ((UnityEngine.Object)(object)buttons[i]).name == name)
				{
					return buttons[i];
				}
			}
			return null;
		}

		private static void HandleLogMessage(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Warning && !string.IsNullOrEmpty(condition) && condition.Contains("referenced script") && condition.Contains("missing"))
			{
				missingScriptWarningsObserved++;
				if (MissingScriptWarningSamples.Count < 12 && !MissingScriptWarningSamples.Contains(condition))
				{
					MissingScriptWarningSamples.Add(condition);
				}
			}
		}

		private static void WriteRuntimeMissingScriptReport(string phase)
		{
			Directory.CreateDirectory(OutputDirectory);
			GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
			List<string> entries = new List<string>();
			int totalMissing = 0;
			foreach (GameObject gameObject in objects)
			{
				if (!(gameObject == null))
				{
					int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
					if (missingCount > 0)
					{
						totalMissing += missingCount;
						string scenePath = (gameObject.scene.IsValid() ? gameObject.scene.path : string.Empty);
						entries.Add("    {\"path\":\"" + EscapeJson(BuildHierarchyPath(gameObject)) + "\",\"scene\":\"" + EscapeJson(scenePath) + "\",\"activeInHierarchy\":" + JsonBool(gameObject.activeInHierarchy) + ",\"persistent\":" + JsonBool(EditorUtility.IsPersistent(gameObject)) + ",\"missingCount\":" + missingCount + "}");
					}
				}
			}
			StringBuilder builder = new StringBuilder(4096);
			builder.AppendLine("{");
			builder.AppendLine("  \"phase\": \"" + EscapeJson(phase) + "\",");
			builder.AppendLine("  \"warningsObserved\": " + missingScriptWarningsObserved + ",");
			builder.AppendLine("  \"liveObjectsWithMissingScripts\": " + entries.Count + ",");
			builder.AppendLine("  \"totalMissingScriptsOnLiveObjects\": " + totalMissing + ",");
			builder.AppendLine("  \"liveAuditPassed\": " + JsonBool(totalMissing == 0) + ",");
			builder.AppendLine("  \"historyStatus\": \"" + ((missingScriptWarningsObserved == 0) ? "clean" : ((totalMissing == 0) ? "warning_history_only_no_live_missing_scripts" : "active_missing_scripts_detected")) + "\",");
			builder.AppendLine("  \"warningSamples\": [");
			for (int j = 0; j < MissingScriptWarningSamples.Count; j++)
			{
				builder.Append("    \"").Append(EscapeJson(MissingScriptWarningSamples[j])).Append('"');
				if (j < MissingScriptWarningSamples.Count - 1)
				{
					builder.Append(',');
				}
				builder.AppendLine();
			}
			builder.AppendLine("  ],");
			builder.AppendLine("  \"objects\": [");
			for (int k = 0; k < entries.Count; k++)
			{
				builder.Append(entries[k]);
				if (k < entries.Count - 1)
				{
					builder.Append(',');
				}
				builder.AppendLine();
			}
			builder.AppendLine("  ]");
			builder.AppendLine("}");
			File.WriteAllText(MissingScriptReportPath, builder.ToString(), Encoding.UTF8);
		}

		private static string BuildHierarchyPath(GameObject gameObject)
		{
			if (gameObject == null)
			{
				return string.Empty;
			}
			List<string> names = new List<string>();
			Transform currentTransform = gameObject.transform;
			while (currentTransform != null)
			{
				names.Add(currentTransform.name);
				currentTransform = currentTransform.parent;
			}
			names.Reverse();
			return string.Join("/", names);
		}

		private static string EscapeJson(string value)
		{
			return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r")
				.Replace("\n", "\\n");
		}

		private static string CompactNote(string value, int maxChars)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "none";
			}
			string compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
			int safeMax = Mathf.Max(4, maxChars);
			return (compact.Length <= safeMax) ? compact : (compact.Substring(0, safeMax - 1) + "…");
		}

		private static string JsonBool(bool value)
		{
			return value ? "true" : "false";
		}

		private static string BuildJson(string status)
		{
			int cleared = 0;
			int r3Seen = 0;
			int r6Seen = 0;
			int shopPurchases = 0;
			int shopGoldSpent = 0;
			int fateUses = 0;
			int summonHeavyRuns = 0;
			int summonHeavyClears = 0;
			int summonHeavyReachedR10 = 0;
			int summonHeavyReachedR9Plus = 0;
			int balancedRuns = 0;
			int balancedClears = 0;
			int shopSaveRuns = 0;
			int shopSaveClears = 0;
			float rareRoundSum = 0f;
			int rareRoundCount = 0;
			float mergeRoundSum = 0f;
			int mergeRoundCount = 0;
			Dictionary<string, int> fateCardUsesByTitle = new Dictionary<string, int>();
			Dictionary<string, int> fateCardClearsByTitle = new Dictionary<string, int>();
			for (int i = 0; i < Results.Count; i++)
			{
				RunResult result = Results[i];
				if (result.clearedR10)
				{
					cleared++;
				}
				if (result.r3ShopSeen)
				{
					r3Seen++;
				}
				if (result.r6ShopSeen)
				{
					r6Seen++;
				}
				shopPurchases += result.shopPurchases;
				shopGoldSpent += result.shopGoldSpent;
				fateUses += result.fateUses;
				if (result.strategy == "summon-heavy")
				{
					summonHeavyRuns++;
					if (result.clearedR10)
					{
						summonHeavyClears++;
					}
					if (result.reachedRound >= 10)
					{
						summonHeavyReachedR10++;
					}
					if (result.reachedRound >= 9)
					{
						summonHeavyReachedR9Plus++;
					}
				}
				else if (result.strategy == "balanced")
				{
					balancedRuns++;
					if (result.clearedR10)
					{
						balancedClears++;
					}
				}
				else if (result.strategy == "shop-save")
				{
					shopSaveRuns++;
					if (result.clearedR10)
					{
						shopSaveClears++;
					}
				}
				if (!string.IsNullOrWhiteSpace(result.fateCardTitle) && result.fateCardTitle != "미사용")
				{
					AddCount(fateCardUsesByTitle, result.fateCardTitle, 1);
					if (result.clearedR10)
					{
						AddCount(fateCardClearsByTitle, result.fateCardTitle, 1);
					}
				}
				if (result.firstRarePlusRound > 0)
				{
					rareRoundSum += (float)result.firstRarePlusRound;
					rareRoundCount++;
				}
				if (result.firstMergeRound > 0)
				{
					mergeRoundSum += (float)result.firstMergeRound;
					mergeRoundCount++;
				}
			}
			StringBuilder builder = new StringBuilder(4096);
			builder.AppendLine("{");
			builder.AppendLine("  \"status\": \"" + status + "\",");
			builder.AppendLine("  \"runs\": " + Results.Count + ",");
			builder.AppendLine("  \"targetRuns\": " + 20 + ",");
			builder.AppendLine("  \"r10Clears\": " + cleared + ",");
			builder.AppendLine("  \"r10SuccessRate\": " + FormatRatio(cleared, Results.Count) + ",");
			builder.AppendLine("  \"r3ShopSeen\": " + r3Seen + ",");
			builder.AppendLine("  \"r6ShopSeen\": " + r6Seen + ",");
			builder.AppendLine("  \"shopPurchases\": " + shopPurchases + ",");
			builder.AppendLine("  \"shopGoldSpent\": " + shopGoldSpent + ",");
			builder.AppendLine("  \"fateUses\": " + fateUses + ",");
			builder.AppendLine("  \"summonHeavyRuns\": " + summonHeavyRuns + ",");
			builder.AppendLine("  \"summonHeavyR10Clears\": " + summonHeavyClears + ",");
			builder.AppendLine("  \"summonHeavyR10SuccessRate\": " + FormatRatio(summonHeavyClears, summonHeavyRuns) + ",");
			builder.AppendLine("  \"summonHeavyReachedR10\": " + summonHeavyReachedR10 + ",");
			builder.AppendLine("  \"summonHeavyReachedR9Plus\": " + summonHeavyReachedR9Plus + ",");
			builder.AppendLine("  \"balancedRuns\": " + balancedRuns + ",");
			builder.AppendLine("  \"balancedR10Clears\": " + balancedClears + ",");
			builder.AppendLine("  \"balancedR10SuccessRate\": " + FormatRatio(balancedClears, balancedRuns) + ",");
			builder.AppendLine("  \"shopSaveRuns\": " + shopSaveRuns + ",");
			builder.AppendLine("  \"shopSaveR10Clears\": " + shopSaveClears + ",");
			builder.AppendLine("  \"shopSaveR10SuccessRate\": " + FormatRatio(shopSaveClears, shopSaveRuns) + ",");
			builder.AppendLine("  \"avgFirstRarePlusRound\": " + FormatFloat((rareRoundCount > 0) ? (rareRoundSum / (float)rareRoundCount) : (-1f)) + ",");
			builder.AppendLine("  \"avgFirstMergeRound\": " + FormatFloat((mergeRoundCount > 0) ? (mergeRoundSum / (float)mergeRoundCount) : (-1f)) + ",");
			builder.AppendLine("  \"fateCardBreakdown\": [");
			int fateCardIndex = 0;
			foreach (KeyValuePair<string, int> pair in fateCardUsesByTitle)
			{
				int clearCount;
				int cardClears = (fateCardClearsByTitle.TryGetValue(pair.Key, out clearCount) ? clearCount : 0);
				builder.Append("    {");
				builder.Append("\"title\":\"").Append(EscapeJson(pair.Key)).Append("\",");
				builder.Append("\"uses\":").Append(pair.Value).Append(',');
				builder.Append("\"r10Clears\":").Append(cardClears).Append(',');
				builder.Append("\"successRate\":").Append(FormatRatio(cardClears, pair.Value));
				builder.Append("}");
				if (++fateCardIndex < fateCardUsesByTitle.Count)
				{
					builder.Append(',');
				}
				builder.AppendLine();
			}
			builder.AppendLine("  ],");
			builder.AppendLine("  \"results\": [");
			for (int j = 0; j < Results.Count; j++)
			{
				RunResult result2 = Results[j];
				builder.Append("    {");
				builder.Append("\"index\":").Append(result2.index).Append(',');
				builder.Append("\"strategy\":\"").Append(result2.strategy).Append("\",");
				builder.Append("\"reachedRound\":").Append(result2.reachedRound).Append(',');
				builder.Append("\"clearedR10\":").Append(result2.clearedR10 ? "true" : "false").Append(',');
				builder.Append("\"summons\":").Append(result2.summons).Append(',');
				builder.Append("\"merges\":").Append(result2.merges).Append(',');
				builder.Append("\"shopPurchases\":").Append(result2.shopPurchases).Append(',');
				builder.Append("\"shopGoldSpent\":").Append(result2.shopGoldSpent).Append(',');
				builder.Append("\"fateUses\":").Append(result2.fateUses).Append(',');
				builder.Append("\"fateCardTitle\":\"").Append(EscapeJson(result2.fateCardTitle)).Append("\",");
				builder.Append("\"fateCardDebt\":").Append(result2.fateCardDebt).Append(',');
				builder.Append("\"fateChoiceIndex\":").Append(result2.fateChoiceIndex).Append(',');
				builder.Append("\"fateActivationRound\":").Append(result2.fateActivationRound).Append(',');
				builder.Append("\"fateTrigger\":\"").Append(EscapeJson(result2.fateTrigger)).Append("\",");
				builder.Append("\"fateCardDetail\":\"").Append(EscapeJson(result2.fateCardDetail)).Append("\",");
				builder.Append("\"r3ShopSeen\":").Append(result2.r3ShopSeen ? "true" : "false").Append(',');
				builder.Append("\"r6ShopSeen\":").Append(result2.r6ShopSeen ? "true" : "false").Append(',');
				builder.Append("\"firstRarePlusRound\":").Append(result2.firstRarePlusRound).Append(',');
				builder.Append("\"firstMergeRound\":").Append(result2.firstMergeRound).Append(',');
				builder.Append("\"endGold\":").Append(result2.endGold).Append(',');
				builder.Append("\"endLife\":").Append(result2.endLife).Append(',');
				builder.Append("\"r10BossHealthRemaining01\":").Append(FormatFloat(result2.r10BossHealthRemaining01)).Append(',');
				builder.Append("\"timeout\":").Append(result2.timeout ? "true" : "false").Append(',');
				builder.Append("\"notes\":\"").Append(EscapeJson(string.Join(";", result2.notes))).Append("\"");
				builder.Append("}");
				if (j < Results.Count - 1)
				{
					builder.Append(',');
				}
				builder.AppendLine();
			}
			builder.AppendLine("  ]");
			builder.AppendLine("}");
			return builder.ToString();
		}

		private static void AddCount(Dictionary<string, int> target, string key, int amount)
		{
			if (target != null && !string.IsNullOrWhiteSpace(key))
			{
				target[key] = (target.TryGetValue(key, out var currentCount) ? (currentCount + amount) : amount);
			}
		}

		private static string FormatFloat(float value)
		{
			return (value < 0f) ? "-1" : value.ToString("0.00", CultureInfo.InvariantCulture);
		}

		private static string FormatRatio(int numerator, int denominator)
		{
			return (denominator <= 0) ? "0.00" : ((float)numerator / (float)denominator).ToString("0.000", CultureInfo.InvariantCulture);
		}
	}
}
