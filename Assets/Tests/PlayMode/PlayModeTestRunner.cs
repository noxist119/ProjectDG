using System;
using System.Collections;
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
        private const float RunTimeoutSeconds = 90f;

        [UnityTest, Timeout(120000)]
        public IEnumerator StableBoard101_RecordsOneUiRunWithoutRestart()
        {
            ValidationRunRecorder recorder = new ValidationRunRecorder(PlayerLikeStrategy.StableBoard, 101);
            if (!ValidationRunLease.TryAcquire(recorder.Record.executionId))
            {
                recorder.MarkFailure("infrastructure_failed", "another_validation_run_is_active");
                recorder.FinalizeRecord(null, "infrastructure_failed", recorder.Record.failureReason);
                recorder.Save();
                Assert.Fail("A validation run is already active.");
            }

            Component controller = null;
            EventInfo gameOverEvent = null;
            Action gameOverHandler = null;
            bool complete = false;
            Application.LogCallback logHandler = recorder.HandleLog;
            Application.logMessageReceived += logHandler;
            try
            {
                yield return RunStable101(recorder, value => controller = value, value => gameOverHandler = value, value => gameOverEvent = value);
                complete = recorder.Record.status == "r1_started" || recorder.Record.status == "gameplay_defeat";
            }
            finally
            {
                if (gameOverEvent != null && gameOverHandler != null && controller != null)
                {
                    gameOverEvent.RemoveEventHandler(controller, gameOverHandler);
                }

                Application.logMessageReceived -= logHandler;
                if (string.IsNullOrEmpty(recorder.Record.status) || recorder.Record.status == "idle")
                {
                    recorder.MarkFailure("infrastructure_failed", "runner_ended_without_terminal_status");
                }
                recorder.FinalizeRecord(controller, recorder.Record.status, recorder.Record.failureReason);
                recorder.Save();
                ValidationRunLease.Release(recorder.Record.executionId);
            }

            Assert.That(complete, Is.True, "Stable 101 did not reach R1 or record a gameplay defeat. JSON contains the exact harness failure state.");
            Assert.That(recorder.Record.actualEventSystemClick, Is.True, "Stable 101 made no real EventSystem UI click.");
            Assert.That(recorder.Record.runnerTickCount, Is.GreaterThanOrEqualTo(3), "Stable 101 did not receive enough PlayMode test ticks.");
        }

        private static IEnumerator RunStable101(ValidationRunRecorder recorder, Action<Component> setController, Action<Action> setHandler, Action<EventInfo> setEvent)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("DG", LoadSceneMode.Single);
            while (load != null && !load.isDone)
            {
                yield return null;
            }

            float setupDeadline = Time.realtimeSinceStartup + SetupTimeoutSeconds;
            Component controller = null;
            while (controller == null && Time.realtimeSinceStartup < setupDeadline)
            {
                controller = RuntimeGameView.FindController();
                yield return null;
            }

            setController(controller);
            if (controller == null || EventSystem.current == null)
            {
                recorder.MarkFailure("infrastructure_failed", controller == null ? "DefenseGameController_missing_after_DG_load" : "production_EventSystem_missing_after_DG_load");
                yield break;
            }

            RuntimeGameView.Invoke(controller, "SetRunContentSeedOverride", new object[] { (int?)101 });
            RuntimeGameView.Invoke(controller, "ResetRunForRetry", null);
            recorder.Record.actualContentSeed = RuntimeGameView.Int(controller, "ActiveRunContentSeed");
            recorder.Record.infrastructureReady = true;

            bool gameOver = false;
            Action handler = () => gameOver = true;
            EventInfo gameOverEvent = controller.GetType().GetEvent("OnGameOver", BindingFlags.Instance | BindingFlags.Public);
            if (gameOverEvent != null)
            {
                gameOverEvent.AddEventHandler(controller, handler);
            }
            setHandler(handler);
            setEvent(gameOverEvent);

            PlayerLikeStrategyPolicy policy = new PlayerLikeStrategyPolicy(PlayerLikeStrategy.StableBoard);
            UiActionDriver ui = new UiActionDriver();
            recorder.Transition(ValidationRunState.OpeningGuide, "scene_loaded_and_seed_reset_before_ui");
            float deadline = Time.realtimeSinceStartup + RunTimeoutSeconds;
            float blockedSince = -1f;
            float initialContractOfferDeadline = -1f;

            while (Time.realtimeSinceStartup < deadline)
            {
                recorder.Record.runnerTickCount++;
                if (gameOver || RuntimeGameView.Int(controller, "Life") <= 0)
                {
                    recorder.Record.gameplayDefeat = true;
                    recorder.RecordSnapshot(controller, "gameplay_defeat_final");
                    recorder.Transition(ValidationRunState.GameOver, "OnGameOver_or_life_zero");
                    recorder.Record.status = "gameplay_defeat";
                    yield break;
                }

                if (RuntimeGameView.Bool(controller, "IsRoundRunning") && RuntimeGameView.Int(controller, "CurrentRound") >= 1)
                {
                    recorder.Record.r1Started = true;
                    recorder.RecordSnapshot(controller, "r1_started");
                    recorder.Transition(ValidationRunState.Fighting, "R1_started_from_visible_BattleButton");
                    recorder.Record.status = "r1_started";
                    yield break;
                }

                if (!ui.IsReadyForAction)
                {
                    yield return null;
                    continue;
                }

                if (DriveVisibleUi(controller, policy, ui, recorder, ref initialContractOfferDeadline))
                {
                    blockedSince = -1f;
                    yield return null;
                    continue;
                }

                if (RuntimeGameView.Text(controller, "BlockingChoiceReason") == "None" && RuntimeGameView.ActiveChoicePanelCount() == 0 && !RuntimeGameView.IsButtonInteractable("BattleButton"))
                {
                    if (blockedSince < 0f) blockedSince = Time.realtimeSinceStartup;
                    if (Time.realtimeSinceStartup - blockedSince > 4f)
                    {
                        recorder.MarkFailure("ui_blocked", "BattleButton_disabled_with_no_visible_choice_panel");
                        recorder.RecordSnapshot(controller, "ui_blocked");
                        yield break;
                    }
                }
                else
                {
                    blockedSince = -1f;
                }

                yield return null;
            }

            recorder.MarkFailure("timeout", "R1_did_not_start_before_realtime_timeout");
            recorder.RecordSnapshot(controller, "timeout");
        }

        private static bool DriveVisibleUi(Component controller, PlayerLikeStrategyPolicy policy, UiActionDriver ui, ValidationRunRecorder recorder, ref float initialContractOfferDeadline)
        {
            if (!RuntimeGameView.Bool(controller, "IsOverdriveMode"))
            {
                recorder.Transition(ValidationRunState.Setup, "select_overdrive_mode");
                return ui.TryClick(RuntimeGameView.FindButton("LobbyCombatModeButton"), "select_overdrive", recorder, controller);
            }

            Button lobbyBattle = RuntimeGameView.FindButton("LobbyBattleButton");
            if (lobbyBattle != null && lobbyBattle.gameObject.activeInHierarchy && lobbyBattle.interactable)
            {
                recorder.Transition(ValidationRunState.Setup, "enter_battle_preparation");
                if (ui.TryClick(lobbyBattle, "enter_battle_preparation", recorder, controller))
                {
                    return true;
                }
            }

            Component openingHud = RuntimeGameView.FindHud();
            if (openingHud != null && !RuntimeGameView.Bool(openingHud, "IsOpeningTutorialCompleteForCurrentRun"))
            {
                recorder.Transition(ValidationRunState.OpeningGuide, "waiting_for_opening_guide");
                return false;
            }

            GameObject resultPanel = RuntimeGameView.FindObject("RoundResultOverlay");
            if (resultPanel != null && resultPanel.activeInHierarchy)
            {
                // The GameOver branch above runs before this method, so Continue is a victory-only action.
                recorder.Transition(ValidationRunState.VictoryResult, "visible_victory_result_panel");
                return ui.TryClick(RuntimeGameView.FindButton("ResultContinueButton"), "victory_result_continue", recorder, controller);
            }

            if (RuntimeGameView.Int(controller, "CurrentRound") == 0 && initialContractOfferDeadline < 0f)
            {
                initialContractOfferDeadline = Time.realtimeSinceStartup + 1.25f;
            }

            GameObject missionPanel = RuntimeGameView.FindObject("TacticalMissionOverlay");
            if (missionPanel != null && missionPanel.activeInHierarchy)
            {
                recorder.Transition(ValidationRunState.ContractChoice, "visible_tactical_contract_panel");
                return policy.TryResolveMission(RuntimeGameView.FindMissionSystem(), controller, ui, recorder);
            }

            if (RuntimeGameView.Int(controller, "CurrentRound") == 0 && Time.realtimeSinceStartup < initialContractOfferDeadline)
            {
                recorder.Transition(ValidationRunState.ContractChoice, "waiting_for_initial_contract_offer");
                return false;
            }

            recorder.Transition(ValidationRunState.Preparing, "visible_choice_panels_resolved");
            if (policy.TryResolveGenericChoice("AugmentChoiceOverlay", "AugmentChoice_", controller, ui, recorder)) return true;
            if (policy.TryResolveRunShop(controller, ui, recorder)) return true;
            if (policy.TryResolveGenericChoice("LuckySummonChoiceOverlay", "LuckySummonChoice", controller, ui, recorder)) return true;
            if (policy.TryResolveGenericChoice("Fate", "Fate", controller, ui, recorder)) return true;
            if (policy.TryPrepare(controller, ui, recorder)) return true;
            return ui.TryClick(RuntimeGameView.FindButton("BattleButton"), "battle_r" + (RuntimeGameView.Int(controller, "CurrentRound") + 1), recorder, controller);
        }
    }
}