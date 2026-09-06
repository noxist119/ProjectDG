using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DefenseGame.Tests
{
    public sealed class PlayModeTestRunner
    {
        private const float SetupTimeoutSeconds = 20f;
        private const float SeedTimeoutSeconds = 240f;
        private const float UiStallTimeoutSeconds = 30f;
        private const float TestTimeScale = 6f;

        [UnityTest, Timeout(1800000)]
        public IEnumerator SixSeedPlayerLikeRunEvidence()
        {
            KeyValuePair<PlayerLikeStrategy, int>[] plans =
            {
                new KeyValuePair<PlayerLikeStrategy, int>(PlayerLikeStrategy.StableBoard, 101),
                new KeyValuePair<PlayerLikeStrategy, int>(PlayerLikeStrategy.StableBoard, 102),
                new KeyValuePair<PlayerLikeStrategy, int>(PlayerLikeStrategy.ContractFirst, 201),
                new KeyValuePair<PlayerLikeStrategy, int>(PlayerLikeStrategy.ContractFirst, 202),
                new KeyValuePair<PlayerLikeStrategy, int>(PlayerLikeStrategy.HighGradeInvestment, 301),
                new KeyValuePair<PlayerLikeStrategy, int>(PlayerLikeStrategy.HighGradeInvestment, 302)
            };
            float previousTimeScale = Time.timeScale;
            List<ValidationRunRecord> results = new List<ValidationRunRecord>();
            bool infrastructureFailure = false;
            try
            {
                Time.timeScale = TestTimeScale;
                for (int i = 0; i < plans.Length; i++)
                {
                    ValidationRunRecord result = null;
                    yield return RunSeed(plans[i].Key, plans[i].Value, value => result = value);
                    results.Add(result);
                    if (result == null || IsInfrastructureFailure(result.status))
                    {
                        infrastructureFailure = true;
                        break;
                    }
                }
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }

            Assert.That(infrastructureFailure, Is.False, "A seed ended with an execution/UI infrastructure failure. Per-seed JSON preserves the exact state; no retry was performed.");
            Assert.That(results.Count, Is.EqualTo(plans.Length), "Six seeds were not all attempted.");
        }

        private static IEnumerator RunSeed(PlayerLikeStrategy strategy, int seed, Action<ValidationRunRecord> complete)
        {
            ValidationRunRecorder recorder = new ValidationRunRecorder(strategy, seed);
            if (!ValidationRunLease.TryAcquire(recorder.Record.executionId))
            {
                recorder.MarkFailure("infrastructure_failed", "another_validation_run_is_active"); recorder.FinalizeRecord(null, recorder.Record.status, recorder.Record.failureReason); recorder.Save(); complete(recorder.Record); yield break;
            }

            Component controller = null;
            PlayerLikeStrategyPolicy policy = null;
            EventInfo gameOverEvent = null;
            Action gameOverHandler = null;
            Application.LogCallback logHandler = recorder.HandleLog;
            Application.logMessageReceived += logHandler;
            try
            {
                AsyncOperation load = SceneManager.LoadSceneAsync("DG", LoadSceneMode.Single);
                while (load != null && !load.isDone) yield return null;
                float setupDeadline = Time.realtimeSinceStartup + SetupTimeoutSeconds;
                while (controller == null && Time.realtimeSinceStartup < setupDeadline) { controller = RuntimeGameView.FindController(); yield return null; }
                if (controller == null || EventSystem.current == null)
                {
                    recorder.MarkFailure("infrastructure_failed", controller == null ? "DefenseGameController_missing_after_DG_load" : "production_EventSystem_missing_after_DG_load"); yield break;
                }

                RuntimeGameView.Invoke(controller, "SetRunContentSeedOverride", new object[] { (int?)seed });
                RuntimeGameView.Invoke(controller, "ResetRunForRetry", null);
                recorder.Record.actualContentSeed = RuntimeGameView.Int(controller, "ActiveRunContentSeed"); recorder.Record.infrastructureReady = true; recorder.SaveProgress(controller, "setup_ready");

                bool gameOver = false;
                gameOverHandler = () => gameOver = true;
                gameOverEvent = controller.GetType().GetEvent("OnGameOver", BindingFlags.Instance | BindingFlags.Public);
                if (gameOverEvent != null) gameOverEvent.AddEventHandler(controller, gameOverHandler);

                policy = new PlayerLikeStrategyPolicy(strategy);
                UiActionDriver ui = new UiActionDriver();
                recorder.Transition(ValidationRunState.OpeningGuide, "scene_loaded_and_seed_reset_before_ui");
                float seedDeadline = Time.realtimeSinceStartup + SeedTimeoutSeconds;
                float blockedSince = -1f;
                float initialContractOfferDeadline = -1f;
                int lastObservedRound = int.MinValue;
                int bossKillsAtR10Start = -1;
                float nextProgressSave = Time.realtimeSinceStartup + 5f;

                while (Time.realtimeSinceStartup < seedDeadline)
                {
                    recorder.Record.runnerTickCount++;
                    if (Time.realtimeSinceStartup >= nextProgressSave) { recorder.SaveProgress(controller, "heartbeat"); nextProgressSave = Time.realtimeSinceStartup + 5f; }
                    Component mission = RuntimeGameView.FindMissionSystem();
                    policy.ObserveMissionSettlement(mission, controller);
                    int round = RuntimeGameView.Int(controller, "CurrentRound");
                    bool roundRunning = RuntimeGameView.Bool(controller, "IsRoundRunning");
                    if (round != lastObservedRound)
                    {
                        lastObservedRound = round;
                        recorder.RecordSnapshot(controller, roundRunning ? "round_started" : "preparation");
                    }

                    if (gameOver || RuntimeGameView.Int(controller, "Life") <= 0)
                    {
                        recorder.Record.gameplayDefeat = true; recorder.RecordSnapshot(controller, "gameplay_defeat_final"); recorder.Transition(ValidationRunState.GameOver, "OnGameOver_or_life_zero"); recorder.Record.status = "gameplay_defeat"; yield break;
                    }

                    ObserveR10(controller, recorder, round, roundRunning, ref bossKillsAtR10Start);
                    if (round >= 11 && roundRunning)
                    {
                        recorder.Record.r11Started = true; recorder.RecordSnapshot(controller, "r11_started"); recorder.Transition(ValidationRunState.Fighting, "R11_started_after_R10_result"); recorder.Record.status = "r11_started"; yield break;
                    }

                    if (!ui.IsReadyForAction) { yield return null; continue; }
                    if (DriveVisibleUi(controller, mission, policy, ui, recorder, ref initialContractOfferDeadline))
                    {
                        blockedSince = -1f; yield return null; continue;
                    }

                    if (!roundRunning && RuntimeGameView.Text(controller, "BlockingChoiceReason") == "None" && RuntimeGameView.ActiveChoicePanelCount() == 0 && !RuntimeGameView.IsButtonInteractable("BattleButton"))
                    {
                        if (blockedSince < 0f) blockedSince = Time.realtimeSinceStartup;
                        if (Time.realtimeSinceStartup - blockedSince >= UiStallTimeoutSeconds)
                        {
                            recorder.MarkFailure("ui_blocked", "BattleButton_disabled_for_30_seconds_with_no_visible_choice_panel"); recorder.RecordSnapshot(controller, "ui_blocked"); yield break;
                        }
                    }
                    else blockedSince = -1f;
                    yield return null;
                }

                recorder.MarkFailure("timeout", "seed_timeout_before_R11_or_gameplay_defeat"); recorder.RecordSnapshot(controller, "timeout");
            }
            finally
            {
                if (policy != null)
                {
                    policy.FinalizeMission(controller);
                }
                if (gameOverEvent != null && gameOverHandler != null && controller != null) gameOverEvent.RemoveEventHandler(controller, gameOverHandler);
                Application.logMessageReceived -= logHandler;
                if (string.IsNullOrEmpty(recorder.Record.status) || recorder.Record.status == "idle") recorder.MarkFailure("infrastructure_failed", "runner_ended_without_terminal_status");
                recorder.FinalizeRecord(controller, recorder.Record.status, recorder.Record.failureReason); recorder.Save(); ValidationRunLease.Release(recorder.Record.executionId); complete(recorder.Record);
            }
        }

        private static void ObserveR10(Component controller, ValidationRunRecorder recorder, int round, bool roundRunning, ref int bossKillsAtR10Start)
        {
            if (round != 10) return;
            if (!recorder.Record.r10Reached && roundRunning)
            {
                recorder.Record.r10Reached = true; recorder.Record.r10Start = RuntimeGameView.CaptureSnapshot(controller, "r10_started"); bossKillsAtR10Start = RuntimeGameView.Int(controller, "RunBossKillCount");
            }
            ValidationBossSnapshot liveBoss = RuntimeGameView.FindLiveMajorBoss();
            if (liveBoss.spawned)
            {
                recorder.Record.r10Boss.displayName = liveBoss.displayName; recorder.Record.r10Boss.maxHealth = liveBoss.maxHealth; recorder.Record.r10Boss.remainingHealth = liveBoss.remainingHealth; recorder.Record.r10Boss.remainingHealth01 = liveBoss.remainingHealth01; recorder.Record.r10Boss.spawned = true;
            }
            if (bossKillsAtR10Start >= 0 && RuntimeGameView.Int(controller, "RunBossKillCount") > bossKillsAtR10Start) recorder.Record.r10Boss.killed = true;
        }

        private static bool DriveVisibleUi(Component controller, Component mission, PlayerLikeStrategyPolicy policy, UiActionDriver ui, ValidationRunRecorder recorder, ref float initialContractOfferDeadline)
        {
            if (!RuntimeGameView.Bool(controller, "IsOverdriveMode"))
            {
                recorder.Transition(ValidationRunState.Setup, "select_overdrive_mode"); return ui.TryClick(RuntimeGameView.FindButton("LobbyCombatModeButton"), "select_overdrive", recorder, controller);
            }
            Button lobbyBattle = RuntimeGameView.FindButton("LobbyBattleButton");
            if (lobbyBattle != null && lobbyBattle.gameObject.activeInHierarchy && lobbyBattle.interactable)
            {
                recorder.Transition(ValidationRunState.Setup, "enter_battle_preparation"); if (ui.TryClick(lobbyBattle, "enter_battle_preparation", recorder, controller)) return true;
            }
            Component openingHud = RuntimeGameView.FindHud();
            if (openingHud != null && !RuntimeGameView.Bool(openingHud, "IsOpeningTutorialCompleteForCurrentRun")) { recorder.Transition(ValidationRunState.OpeningGuide, "waiting_for_opening_guide"); return false; }

            GameObject resultPanel = RuntimeGameView.FindObject("RoundResultOverlay");
            if (resultPanel != null && resultPanel.activeInHierarchy)
            {
                // GameOver is checked before this method. Continue is strictly victory-only.
                recorder.Transition(ValidationRunState.VictoryResult, "visible_victory_result_panel"); return ui.TryClick(RuntimeGameView.FindButton("ResultContinueButton"), "victory_result_continue", recorder, controller);
            }
            if (RuntimeGameView.Int(controller, "CurrentRound") == 0 && initialContractOfferDeadline < 0f) initialContractOfferDeadline = Time.realtimeSinceStartup + 1.25f;
            GameObject missionPanel = RuntimeGameView.FindObject("TacticalMissionOverlay");
            if (missionPanel != null && missionPanel.activeInHierarchy) { recorder.Transition(ValidationRunState.ContractChoice, "visible_tactical_contract_panel"); return policy.TryResolveMission(mission, controller, ui, recorder); }
            if (RuntimeGameView.Int(controller, "CurrentRound") == 0 && Time.realtimeSinceStartup < initialContractOfferDeadline) { recorder.Transition(ValidationRunState.ContractChoice, "waiting_for_initial_contract_offer"); return false; }

            recorder.Transition(ValidationRunState.Preparing, "visible_choice_panels_resolved");
            if (policy.TryResolveGenericChoice("AugmentChoiceOverlay", "AugmentChoice_", controller, ui, recorder)) return true;
            if (policy.TryResolveRunShop(controller, ui, recorder)) return true;
            if (policy.TryResolveMission(mission, controller, ui, recorder)) return true;
            if (policy.TryResolveGenericChoice("LuckySummonChoiceOverlay", "LuckySummonChoice", controller, ui, recorder)) return true;
            if (policy.TryResolveGenericChoice("Fate", "Fate", controller, ui, recorder)) return true;
            if (policy.TryPrepare(controller, ui, recorder)) return true;
            return ui.TryClick(RuntimeGameView.FindButton("BattleButton"), "battle_r" + (RuntimeGameView.Int(controller, "CurrentRound") + 1), recorder, controller);
        }

        private static bool IsInfrastructureFailure(string status) { return status == "infrastructure_failed" || status == "ui_blocked" || status == "timeout"; }
    }
}