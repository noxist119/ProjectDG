using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace DefenseGame.Tests
{
    internal sealed class ValidationRunRecorder
    {
        private readonly ValidationRunRecord record;
        private ValidationRunState state;
        private int runtimeErrors;
        private int boardSlotWarnings;

        internal ValidationRunRecorder(PlayerLikeStrategy strategy, int seed)
        {
            record = new ValidationRunRecord { executionId = Guid.NewGuid().ToString("N"), unityProcessId = Process.GetCurrentProcess().Id.ToString(), strategy = strategy.ToString(), requestedSeed = seed };
            state = ValidationRunState.Idle;
            Transition(ValidationRunState.Setup, "test_created");
        }

        internal ValidationRunRecord Record { get { return record; } }
        internal ValidationRunState State { get { return state; } }
        internal void Transition(ValidationRunState next, string reason)
        {
            if (state == next) return;
            record.stateTransitions.Add(new ValidationStateTransition { from = state.ToString(), to = next.ToString(), realtime = Time.realtimeSinceStartup, reason = reason });
            state = next; record.finalState = next.ToString();
        }
        internal void RecordAction(string action, string button, int round)
        {
            record.actualEventSystemClick = true;
            if (string.IsNullOrEmpty(record.firstUiAction)) { record.firstUiAction = button; record.firstUiActionRound = round; }
            record.actions.Add(new ValidationActionRecord { index = record.actions.Count + 1, realtime = Time.realtimeSinceStartup, round = round, action = action, button = button });
        }
        internal void RecordSnapshot(Component controller, string phase) { if (controller != null) record.rounds.Add(RuntimeGameView.CaptureSnapshot(controller, phase)); }
        internal void SaveProgress(Component controller, string reason)
        {
            if (controller != null)
            {
                record.finalRound = RuntimeGameView.Int(controller, "CurrentRound");
                record.finalLife = RuntimeGameView.Int(controller, "Life");
                record.finalGold = RuntimeGameView.Int(controller, "Gold");
                record.finalGrades = RuntimeGameView.GetGrades();
                record.blockingChoiceReason = RuntimeGameView.Text(controller, "BlockingChoiceReason");
            }
            record.activeChoicePanels = RuntimeGameView.ActiveChoicePanels();
            record.battleButtonInteractable = RuntimeGameView.IsButtonInteractable("BattleButton");
            record.runtimeErrors = runtimeErrors;
            record.boardSlotWarnings = boardSlotWarnings;
            record.status = "running";
            record.failureReason = reason;
            Save();
            UnityEngine.Debug.Log("[Pass23] " + record.executionId + " " + reason + " round=" + record.finalRound + " ticks=" + record.runnerTickCount);
        }
        internal void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeErrors++;
            if (!string.IsNullOrEmpty(condition) && condition.IndexOf("BoardSlot", StringComparison.OrdinalIgnoreCase) >= 0) boardSlotWarnings++;
        }
        internal void MarkFailure(string status, string reason) { record.status = status; record.failureReason = reason; Transition(ValidationRunState.Failed, reason); }
        internal void FinalizeRecord(Component controller, string status, string reason)
        {
            if (controller != null)
            {
                record.finalRound = RuntimeGameView.Int(controller, "CurrentRound"); record.finalLife = RuntimeGameView.Int(controller, "Life"); record.finalGold = RuntimeGameView.Int(controller, "Gold"); record.finalGrades = RuntimeGameView.GetGrades(); record.blockingChoiceReason = RuntimeGameView.Text(controller, "BlockingChoiceReason");
            }
            else record.blockingChoiceReason = "controller_missing";
            record.activeChoicePanels = RuntimeGameView.ActiveChoicePanels(); record.battleButtonInteractable = RuntimeGameView.IsButtonInteractable("BattleButton"); record.runtimeErrors = runtimeErrors; record.boardSlotWarnings = boardSlotWarnings; record.status = status;
            if (!string.IsNullOrEmpty(reason)) record.failureReason = reason;
            Transition(ValidationRunState.Record, status); Transition(ValidationRunState.Finished, status);
        }
        internal string Save()
        {
            string root = ResolveProjectRoot(); string directory = Path.Combine(root, "BatchPlaytestResults"); Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "DefenseGame_Pass23_" + record.strategy + "_Seed" + record.requestedSeed + ".json"); File.WriteAllText(path, JsonUtility.ToJson(record, true)); return path;
        }
        private static string ResolveProjectRoot()
        {
            string[] starts = { Directory.GetCurrentDirectory(), Application.dataPath };
            for (int i = 0; i < starts.Length; i++)
            {
                if (string.IsNullOrEmpty(starts[i])) continue; DirectoryInfo directory = new DirectoryInfo(Path.GetFullPath(starts[i])); if (string.Equals(directory.Name, "Assets", StringComparison.OrdinalIgnoreCase)) directory = directory.Parent;
                for (int depth = 0; directory != null && depth < 8; depth++, directory = directory.Parent) if (Directory.Exists(Path.Combine(directory.FullName, "Assets")) && Directory.Exists(Path.Combine(directory.FullName, "ProjectSettings"))) return directory.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}