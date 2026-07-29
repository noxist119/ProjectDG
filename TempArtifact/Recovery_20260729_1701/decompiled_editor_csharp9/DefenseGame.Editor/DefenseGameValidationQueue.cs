using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DefenseGame.Editor
{
	[InitializeOnLoad]
	public static class DefenseGameValidationQueue
	{
		private const string QueueFileName = "DefenseGameValidationQueue.txt";

		private const string VerticalSmokeFileName = "DefenseGame_PlayModeSmoke.json";

		private const string BossSmokeFileName = "DefenseGame_BossAnimationSmoke.json";

		private const string PlaytestFileName = "DefenseGame_Playtest20_Human3.json";

		private static double nextCheckTime;

		private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

		private static string ResultDirectory => Path.Combine(ProjectRoot, "BatchPlaytestResults");

		private static string QueuePath => Path.Combine(ResultDirectory, "DefenseGameValidationQueue.txt");

		static DefenseGameValidationQueue()
		{
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(Tick));
		}

		[MenuItem("DefenseGame/Validation/Run Vertical + Boss Smoke + Human 3 Strategies x20")]
		public static void StartFullValidation()
		{
			Directory.CreateDirectory(ResultDirectory);
			File.WriteAllText(QueuePath, "start");
			nextCheckTime = 0.0;
			Debug.Log("[DefenseGameValidationQueue] validation queued");
		}

		[MenuItem("DefenseGame/Validation/Run Vertical Smoke Only")]
		public static void StartVerticalSmokeOnly()
		{
			Directory.CreateDirectory(ResultDirectory);
			File.WriteAllText(QueuePath, "vertical_only_start");
			nextCheckTime = 0.0;
			Debug.Log("[DefenseGameValidationQueue] vertical smoke queued");
		}

		[MenuItem("DefenseGame/Validation/Validate Commercial Hurdle Policy")]
		public static void ValidateCommercialHurdlePolicy()
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Invalid comparison between Unknown and I4
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Invalid comparison between Unknown and I4
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Invalid comparison between Unknown and I4
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Invalid comparison between Unknown and I4
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			CommercialRoundTuning buildUp = CommercialRoundPacing.Resolve(19, false);
			CommercialRoundTuning hurdle20 = CommercialRoundPacing.Resolve(20, true);
			CommercialRoundTuning relief21 = CommercialRoundPacing.Resolve(21, false);
			CommercialRoundTuning hurdle30 = CommercialRoundPacing.Resolve(30, true);
			if ((int)buildUp.phase != 1 || (int)hurdle20.phase != 2 || (int)relief21.phase != 3 || (int)hurdle30.phase != 2)
			{
				throw new InvalidOperationException("Commercial hurdle phase schedule is invalid.");
			}
			if (hurdle20.healthMultiplier <= buildUp.healthMultiplier || relief21.healthMultiplier >= 1f || relief21.spawnCountMultiplier >= 1f || hurdle30.healthMultiplier <= hurdle20.healthMultiplier)
			{
				throw new InvalidOperationException("Commercial hurdle multipliers are not ordered correctly.");
			}
			if (CommercialRoundPacing.GetNextHurdleRound(19) != 20 || CommercialRoundPacing.GetNextHurdleRound(20) != 30 || CommercialRoundPacing.GetNextHurdleRound(49) != 50)
			{
				throw new InvalidOperationException("Commercial next-hurdle schedule is invalid.");
			}
			OutgameProgressionConfig config = ScriptableObject.CreateInstance<OutgameProgressionConfig>();
			try
			{
				if (config.scaleMonstersWithCollectionGrowth || config.startingEarnedChestKeys < 1 || config.earnedChestProgressTarget <= 0 || config.premiumChestEpicPityDraws > 10 || config.premiumChestLegendaryPityDraws > 40)
				{
					throw new InvalidOperationException("Commercial chest defaults are invalid.");
				}
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)config);
			}
			Debug.Log("[DefenseGameValidationQueue] commercial hurdle + chest policy valid.");
		}

		private static void Tick()
		{
			if (EditorApplication.timeSinceStartup < nextCheckTime || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(QueuePath))
			{
				return;
			}
			nextCheckTime = EditorApplication.timeSinceStartup + 0.75;
			string state;
			try
			{
				state = File.ReadAllText(QueuePath).Trim();
			}
			catch (Exception)
			{
				return;
			}
			switch (state)
			{
			case "vertical_only_start":
				BeginStage("vertical_only_running", "DefenseGame_PlayModeSmoke.json", DefenseGamePlayModeSmoke.RunPlayModeSmoke);
				break;
			case "vertical_only_running":
				if (File.Exists(Path.Combine(ResultDirectory, "DefenseGame_PlayModeSmoke.json")))
				{
					File.WriteAllText(QueuePath, "complete");
					Debug.Log("[DefenseGameValidationQueue] vertical smoke complete: " + ResultDirectory);
				}
				break;
			case "start":
				BeginStage("vertical_running", "DefenseGame_PlayModeSmoke.json", DefenseGamePlayModeSmoke.RunPlayModeSmoke);
				break;
			case "vertical_running":
				ContinueWhenOutputExists("DefenseGame_PlayModeSmoke.json", "boss_running", "DefenseGame_BossAnimationSmoke.json", DefenseGameBossAnimationSmoke.RunBossAnimationSmoke);
				break;
			case "boss_running":
				ContinueWhenOutputExists("DefenseGame_BossAnimationSmoke.json", "playtest_running", "DefenseGame_Playtest20_Human3.json", DefenseGameBatchPlaytest.RunHumanStrategies20);
				break;
			case "playtest_running":
				if (File.Exists(Path.Combine(ResultDirectory, "DefenseGame_Playtest20_Human3.json")))
				{
					File.WriteAllText(QueuePath, "complete");
					Debug.Log("[DefenseGameValidationQueue] validation complete: " + ResultDirectory);
				}
				break;
			}
		}

		private static void ContinueWhenOutputExists(string completedOutput, string nextState, string nextOutput, Action nextAction)
		{
			if (File.Exists(Path.Combine(ResultDirectory, completedOutput)))
			{
				BeginStage(nextState, nextOutput, nextAction);
			}
		}

		private static void BeginStage(string state, string outputFileName, Action action)
		{
			Directory.CreateDirectory(ResultDirectory);
			string outputPath = Path.Combine(ResultDirectory, outputFileName);
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
			File.WriteAllText(QueuePath, state);
			nextCheckTime = EditorApplication.timeSinceStartup + 1.0;
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				File.WriteAllText(QueuePath, "failed:" + state + ":" + ex.GetType().Name);
				Debug.LogException(ex);
			}
		}
	}
}
