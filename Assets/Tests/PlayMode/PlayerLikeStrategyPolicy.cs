using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Tests
{
    internal sealed class PlayerLikeStrategyPolicy
    {
        private readonly PlayerLikeStrategy strategy;
        private bool shopHandled;
        private ValidationMissionRecord activeMission;
        private string activeMissionKind = string.Empty;
        private int completedMissionCountAtSelection;

        internal PlayerLikeStrategyPolicy(PlayerLikeStrategy strategy) { this.strategy = strategy; }

        internal bool TryResolveMission(Component missionSystem, Component controller, UiActionDriver ui, ValidationRunRecorder recorder)
        {
            GameObject panel = RuntimeGameView.FindObject("TacticalMissionOverlay");
            if (panel == null || !panel.activeInHierarchy || missionSystem == null) return false;
            int count = RuntimeGameView.Int(missionSystem, "MissionOfferCount"); List<object> offers = new List<object>();
            for (int i = 0; i < count; i++) offers.Add(RuntimeGameView.Invoke(missionSystem, "GetMissionOfferSnapshot", new object[] { i }));
            string reason; int selected = ChooseFeasibleOffer(offers, controller, out reason);
            if (selected < 0) return ui.TryClick(RuntimeGameView.FindButton("MissionCloseButton"), "contract_later:" + reason, recorder, controller);
            object offer = offers[selected];
            if (!ui.TryClick(RuntimeGameView.FindButton("MissionOption_" + selected), "contract_select:" + RuntimeGameView.FieldText(offer, "kind"), recorder, controller)) return false;
            activeMissionKind = RuntimeGameView.FieldText(offer, "kind"); completedMissionCountAtSelection = RuntimeGameView.Int(missionSystem, "CompletedMissionCount");
            activeMission = new ValidationMissionRecord
            {
                round = RuntimeGameView.Int(controller, "CurrentRound"), selected = DescribeOffer(offer), selectionReason = reason, condition = RuntimeGameView.FieldText(offer, "description"), reward = RuntimeGameView.FieldText(offer, "rewardText"), deadline = RuntimeGameView.FieldInt(offer, "targetRound"), feasibleAtSelection = RuntimeGameView.FieldBool(offer, "feasibleNow"), goldBefore = RuntimeGameView.Int(controller, "Gold"), actionResponse = "policy will prioritize matching safe actions while preserving a survivable board"
            };
            for (int i = 0; i < offers.Count; i++) activeMission.offers.Add(DescribeOffer(offers[i]));
            recorder.Record.missions.Add(activeMission); return true;
        }

        internal void ObserveMissionSettlement(Component missionSystem, Component controller)
        {
            if (activeMission == null || missionSystem == null) return;
            bool stillActive = RuntimeGameView.Bool(missionSystem, "HasActiveMissionSelection");
            if (stillActive && RuntimeGameView.Int(controller, "CurrentRound") <= activeMission.deadline) return;
            activeMission.goldDelta = RuntimeGameView.Int(controller, "Gold") - activeMission.goldBefore;
            activeMission.result = RuntimeGameView.Int(missionSystem, "CompletedMissionCount") > completedMissionCountAtSelection ? "completed" : "failed_or_expired";
            activeMission = null; activeMissionKind = string.Empty;
        }

        internal void FinalizeMission(Component controller)
        {
            if (activeMission == null) return;
            activeMission.goldDelta = RuntimeGameView.Int(controller, "Gold") - activeMission.goldBefore;
            activeMission.result = "active_at_run_end"; activeMission = null; activeMissionKind = string.Empty;
        }

        internal bool TryResolveRunShop(Component controller, UiActionDriver ui, ValidationRunRecorder recorder)
        {
            GameObject panel = RuntimeGameView.FindObject("RunShopOverlay"); if (panel == null || !panel.activeInHierarchy) return false;
            if (!shopHandled)
            {
                shopHandled = true;
                Button offer = panel.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button != null && button.gameObject.activeInHierarchy && button.interactable && button.name.StartsWith("RunShopOffer_", StringComparison.Ordinal));
                if (ui.TryClick(offer, "runshop_purchase", recorder, controller)) return true;
            }
            return ui.TryClick(RuntimeGameView.FindButton("RunShopCloseButton"), "runshop_later_or_close", recorder, controller);
        }

        internal bool TryPrepare(Component controller, UiActionDriver ui, ValidationRunRecorder recorder)
        {
            ValidationUnitGrades grades = RuntimeGameView.GetGrades(); int boardTarget = strategy == PlayerLikeStrategy.HighGradeInvestment ? 6 : 8;
            bool preserveGold = activeMissionKind == "GoldReserve" || activeMissionKind == "NoSummonHold" || activeMissionKind == "LeanDefense";
            bool summonGoal = activeMissionKind == "SummonSprint";
            bool mergeGoal = activeMissionKind == "MergeRush";
            if (!preserveGold && (grades.total < boardTarget || summonGoal) && RuntimeGameView.Int(controller, "EmptySlotCount") > 0)
            {
                if (ui.TryClick(RuntimeGameView.FindButton("SummonButton"), summonGoal ? "contract_summon" : "policy_summon", recorder, controller)) return true;
            }
            if ((mergeGoal || strategy != PlayerLikeStrategy.HighGradeInvestment) && grades.normal >= 3)
            {
                // Only executes when the game exposes its visible Normal merge control; no board mutation is synthesized.
                if (ui.TryClick(RuntimeGameView.FindButton("NormalGradeCard"), mergeGoal ? "contract_normal_merge" : "policy_normal_merge", recorder, controller)) return true;
            }
            if (!preserveGold && strategy == PlayerLikeStrategy.HighGradeInvestment && grades.total >= 6 && RuntimeGameView.Int(controller, "SummonGradeLuckLevel") < 3)
            {
                if (ui.TryClick(RuntimeGameView.FindButton("SummonGradeLuckUpgrade"), "policy_high_grade_luck", recorder, controller)) return true;
            }
            if (!preserveGold && strategy != PlayerLikeStrategy.HighGradeInvestment && grades.total >= boardTarget)
            {
                if (ui.TryClick(RuntimeGameView.FindButton("GradeUpgrade_Normal"), "policy_normal_grade_upgrade", recorder, controller)) return true;
            }
            return false;
        }

        internal bool TryResolveGenericChoice(string overlayName, string buttonPrefix, Component controller, UiActionDriver ui, ValidationRunRecorder recorder)
        {
            GameObject panel = RuntimeGameView.FindObjectContaining(overlayName); if (panel == null || !panel.activeInHierarchy) return false;
            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            Button candidate = buttons.FirstOrDefault(button => button != null && button.gameObject.activeInHierarchy && button.interactable && button.name.IndexOf(buttonPrefix, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? buttons.FirstOrDefault(button => button != null && button.gameObject.activeInHierarchy && button.interactable && button.name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) < 0 && button.name.IndexOf("Later", StringComparison.OrdinalIgnoreCase) < 0);
            return ui.TryClick(candidate, "choice:" + overlayName, recorder, controller);
        }

        private int ChooseFeasibleOffer(List<object> offers, Component controller, out string reason)
        {
            int best = -1, bestScore = int.MinValue; reason = "no_offer_is_feasible_now";
            for (int i = 0; i < offers.Count; i++)
            {
                object offer = offers[i]; if (!RuntimeGameView.FieldBool(offer, "feasibleNow") || RuntimeGameView.FieldInt(offer, "targetRound") < RuntimeGameView.Int(controller, "CurrentRound")) continue;
                string kind = RuntimeGameView.FieldText(offer, "kind"), category = RuntimeGameView.FieldText(offer, "category");
                int score = RuntimeGameView.FieldInt(offer, "goldReward") * 4 + RuntimeGameView.FieldInt(offer, "roundGoldBonus") * 20 + Mathf.Max(0, RuntimeGameView.FieldInt(offer, "roundsRemaining")) * 4;
                if (strategy == PlayerLikeStrategy.StableBoard)
                {
                    if (category == "SAFE" || kind == "PerfectDefense" || kind == "MonsterHunter" || kind == "RoleCollector") score += 120;
                    if (kind == "NoSummonHold" || kind == "LeanDefense" || kind == "EmptySlotDiscipline") score -= 240;
                }
                else if (strategy == PlayerLikeStrategy.ContractFirst)
                {
                    if (kind == "SummonSprint" || kind == "MergeRush" || kind == "MonsterHunter" || kind == "RoleCollector") score += 150;
                    if (category == "TEMPO" || category == "BUILD") score += 60;
                }
                else
                {
                    int board = RuntimeGameView.GetGrades().total;
                    if (board < 6 && (category == "SAFE" || kind == "SummonSprint" || kind == "MonsterHunter")) score += 140;
                    if (board >= 6 && (category == "GREED" || kind == "GoldReserve" || kind == "HighGradeForge" || kind == "RareUpgrade")) score += 130;
                }
                if (score > bestScore) { best = i; bestScore = score; reason = "feasible_now; " + strategy + " score=" + score; }
            }
            return best;
        }
        private static string DescribeOffer(object offer) { return RuntimeGameView.FieldText(offer, "kind") + " | " + RuntimeGameView.FieldText(offer, "title") + " | " + RuntimeGameView.FieldText(offer, "description") + " | " + RuntimeGameView.FieldText(offer, "rewardText") + " | deadline=R" + RuntimeGameView.FieldInt(offer, "targetRound") + " | feasible=" + RuntimeGameView.FieldBool(offer, "feasibleNow"); }
    }
}