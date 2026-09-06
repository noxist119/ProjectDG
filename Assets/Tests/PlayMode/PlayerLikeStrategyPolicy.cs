using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Tests
{
    internal sealed class PlayerLikeStrategyPolicy
    {
        private enum FateResolutionPhase { None, WaitingForCards, WaitingForRelease }

        private readonly PlayerLikeStrategy strategy;
        private bool shopHandled;
        private ValidationMissionRecord activeMission;
        private string activeMissionKind = string.Empty;
        private int completedMissionCountAtSelection;
        private FateResolutionPhase fatePhase;
        private float fateDeadlineRealtime;
        private ValidationFateRecord activeFate;

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

        internal bool TryResolveFateChoice(Component controller, UiActionDriver ui, ValidationRunRecorder recorder, out string failure)
        {
            failure = null;
            bool panelOpen = RuntimeGameView.Bool(controller, "FateCardChoicePanelOpen");
            float now = Time.realtimeSinceStartup;

            if (fatePhase == FateResolutionPhase.WaitingForRelease)
            {
                if (!panelOpen && Time.timeScale >= 0.99f)
                {
                    activeFate.timeScaleAfterResolution = Time.timeScale;
                    activeFate.panelOpenAfterResolution = false;
                    activeFate.result = "resolved";
                    activeFate = null; fatePhase = FateResolutionPhase.None;
                    return false;
                }
                if (now >= fateDeadlineRealtime)
                {
                    if (activeFate != null) { activeFate.timeScaleAfterResolution = Time.timeScale; activeFate.panelOpenAfterResolution = panelOpen; activeFate.result = "release_timeout"; }
                    failure = "fate_choice_not_released_within_3_seconds";
                }
                return false;
            }

            if (fatePhase == FateResolutionPhase.WaitingForCards)
            {
                if (!panelOpen)
                {
                    if (now >= fateDeadlineRealtime) { if (activeFate != null) activeFate.result = "panel_open_timeout"; failure = "fate_choice_panel_not_open_within_3_seconds"; }
                    return false;
                }
                Button[] cards = GetFateCards();
                if (cards.Length != 3)
                {
                    if (now >= fateDeadlineRealtime) { if (activeFate != null) activeFate.result = "cards_missing"; failure = "fate_choice_cards_not_visible_within_3_seconds"; }
                    return false;
                }
                int choice = ChooseFateCard(cards, controller);
                Button selected = cards[choice];
                activeFate.selectedCard = selected.name;
                activeFate.selectionReason = DescribeFateChoice(selected, controller);
                activeFate.timeScaleBeforeSelection = Time.timeScale;
                if (!ui.TryClick(selected, "fate_card_select:" + selected.name, recorder, controller))
                {
                    failure = "fate_choice_card_not_clickable";
                    activeFate.result = "card_not_clickable";
                    return false;
                }
                fatePhase = FateResolutionPhase.WaitingForRelease;
                fateDeadlineRealtime = Time.realtimeSinceStartup + 3f;
                return true;
            }

            if (panelOpen)
            {
                activeFate = new ValidationFateRecord { round = RuntimeGameView.Int(controller, "CurrentRound"), panelOpenBeforeEntry = true, timeScaleBeforeEntry = Time.timeScale, result = "panel_open_without_entry" };
                recorder.Record.fateChoices.Add(activeFate);
                fatePhase = FateResolutionPhase.WaitingForCards;
                fateDeadlineRealtime = now + 3f;
                return false;
            }

            Button entry = RuntimeGameView.FindButton("FatePanelReopenButton");
            if (entry == null || !entry.gameObject.activeInHierarchy || !entry.interactable) return false;
            activeFate = new ValidationFateRecord
            {
                round = RuntimeGameView.Int(controller, "CurrentRound"), entryClicked = true, panelOpenBeforeEntry = false,
                timeScaleBeforeEntry = Time.timeScale, result = "entry_clicked"
            };
            recorder.Record.fateChoices.Add(activeFate);
            if (!ui.TryClick(entry, "fate_entry_open", recorder, controller))
            {
                activeFate.result = "entry_not_clickable";
                failure = "fate_entry_button_not_clickable";
                return false;
            }
            fatePhase = FateResolutionPhase.WaitingForCards;
            fateDeadlineRealtime = Time.realtimeSinceStartup + 3f;
            return true;
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

        private static Button[] GetFateCards()
        {
            string[] names = { "FateChoiceCard0", "FateChoiceCard1", "FateChoiceCard2" };
            List<Button> cards = new List<Button>();
            for (int i = 0; i < names.Length; i++)
            {
                Button card = RuntimeGameView.FindButton(names[i]);
                if (card != null && card.gameObject.activeInHierarchy && card.interactable) cards.Add(card);
            }
            return cards.ToArray();
        }

        private int ChooseFateCard(Button[] cards, Component controller)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                string text = GetButtonText(cards[i]);
                if (strategy == PlayerLikeStrategy.StableBoard && (RuntimeGameView.Int(controller, "Life") <= 4 || ContainsAny(text, "회복", "방벽", "생존", "시간 정지", "붕괴"))) return i;
                if (strategy == PlayerLikeStrategy.HighGradeInvestment && ContainsAny(text, "등급", "에픽", "신화", "용병", "소환")) return i;
                if (strategy == PlayerLikeStrategy.ContractFirst && IsContractCompatibleFate(text)) return i;
            }
            return 0;
        }

        private string DescribeFateChoice(Button card, Component controller)
        {
            string text = GetButtonText(card);
            if (strategy == PlayerLikeStrategy.StableBoard) return RuntimeGameView.Int(controller, "Life") <= 4 ? "low_hp_survival_priority" : "survival_or_control_priority: " + text;
            if (strategy == PlayerLikeStrategy.HighGradeInvestment) return "grade_or_high_tier_priority: " + text;
            return "contract_compatible_immediate_power_priority: " + text;
        }

        private bool IsContractCompatibleFate(string text)
        {
            if (activeMissionKind == "NoSummonHold" || activeMissionKind == "LeanDefense") return !ContainsAny(text, "소환", "용병", "에픽", "신화");
            if (activeMissionKind == "GoldReserve") return ContainsAny(text, "골드", "방벽", "시간", "붕괴");
            return ContainsAny(text, "전장", "골드", "소환", "용병", "에픽", "붕괴", "시간");
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            for (int i = 0; i < values.Length; i++) if (!string.IsNullOrEmpty(text) && text.IndexOf(values[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string GetButtonText(Button button)
        {
            if (button == null) return string.Empty;
            Text label = button.GetComponentInChildren<Text>(true);
            return label == null ? button.name : label.text ?? button.name;
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