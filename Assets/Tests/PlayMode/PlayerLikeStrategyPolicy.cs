using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Tests
{
    internal sealed class PlayerLikeStrategyPolicy
    {
        private readonly PlayerLikeStrategy strategy;
        private bool shopHandled;

        internal PlayerLikeStrategyPolicy(PlayerLikeStrategy strategy)
        {
            this.strategy = strategy;
        }

        internal bool TryResolveMission(Component missionSystem, Component controller, UiActionDriver ui, ValidationRunRecorder recorder)
        {
            GameObject panel = RuntimeGameView.FindObject("TacticalMissionOverlay");
            if (panel == null || !panel.activeInHierarchy || missionSystem == null)
            {
                return false;
            }

            int count = RuntimeGameView.Int(missionSystem, "MissionOfferCount");
            List<object> offers = new List<object>();
            for (int i = 0; i < count; i++)
            {
                offers.Add(RuntimeGameView.Invoke(missionSystem, "GetMissionOfferSnapshot", new object[] { i }));
            }

            string reason;
            int selected = ChooseFeasibleOffer(offers, controller, out reason);
            if (selected < 0)
            {
                return ui.TryClick(RuntimeGameView.FindButton("MissionCloseButton"), "contract_later:" + reason, recorder, controller);
            }

            object offer = offers[selected];
            Button button = RuntimeGameView.FindButton("MissionOption_" + selected);
            if (!ui.TryClick(button, "contract_select:" + FieldText(offer, "kind"), recorder, controller))
            {
                return false;
            }

            ValidationMissionRecord mission = new ValidationMissionRecord
            {
                round = RuntimeGameView.Int(controller, "CurrentRound"),
                selected = DescribeOffer(offer),
                selectionReason = reason,
                condition = FieldText(offer, "description"),
                reward = FieldText(offer, "rewardText"),
                deadline = FieldInt(offer, "targetRound"),
                feasibleAtSelection = FieldBool(offer, "feasibleNow")
            };
            for (int i = 0; i < offers.Count; i++)
            {
                mission.offers.Add(DescribeOffer(offers[i]));
            }
            recorder.Record.missions.Add(mission);
            return true;
        }

        internal bool TryResolveRunShop(Component controller, UiActionDriver ui, ValidationRunRecorder recorder)
        {
            GameObject panel = RuntimeGameView.FindObject("RunShopOverlay");
            if (panel == null || !panel.activeInHierarchy)
            {
                return false;
            }

            if (!shopHandled)
            {
                shopHandled = true;
                Button offer = panel.GetComponentsInChildren<Button>(true).FirstOrDefault(button =>
                    button != null && button.gameObject.activeInHierarchy && button.interactable && button.name.StartsWith("RunShopOffer_", StringComparison.Ordinal));
                if (ui.TryClick(offer, "runshop_purchase", recorder, controller))
                {
                    return true;
                }
            }

            return ui.TryClick(RuntimeGameView.FindButton("RunShopCloseButton"), "runshop_later_or_close", recorder, controller);
        }

        internal bool TryPrepare(Component controller, UiActionDriver ui, ValidationRunRecorder recorder)
        {
            int target = strategy == PlayerLikeStrategy.HighGradeInvestment ? 6 : 8;
            int board = RuntimeGameView.Int(controller, "BoardUnitCount");
            int emptySlots = RuntimeGameView.Int(controller, "EmptySlotCount");
            if (board < target && emptySlots > 0 && ui.TryClick(RuntimeGameView.FindButton("SummonButton"), "policy_summon", recorder, controller))
            {
                return true;
            }

            if (strategy == PlayerLikeStrategy.HighGradeInvestment && board >= 6 && RuntimeGameView.Int(controller, "SummonGradeLuckLevel") < 3)
            {
                if (ui.TryClick(RuntimeGameView.FindButton("SummonGradeLuckUpgrade"), "policy_high_grade_luck", recorder, controller))
                {
                    return true;
                }
            }

            if (strategy != PlayerLikeStrategy.HighGradeInvestment && board >= target)
            {
                if (ui.TryClick(RuntimeGameView.FindButton("GradeUpgrade_Normal"), "policy_normal_grade_upgrade", recorder, controller))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryResolveGenericChoice(string overlayName, string buttonPrefix, Component controller, UiActionDriver ui, ValidationRunRecorder recorder)
        {
            GameObject panel = RuntimeGameView.FindObjectContaining(overlayName);
            if (panel == null || !panel.activeInHierarchy)
            {
                return false;
            }

            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            Button candidate = buttons.FirstOrDefault(button => button != null && button.gameObject.activeInHierarchy && button.interactable && button.name.IndexOf(buttonPrefix, StringComparison.OrdinalIgnoreCase) >= 0);
            if (candidate == null)
            {
                candidate = buttons.FirstOrDefault(button => button != null && button.gameObject.activeInHierarchy && button.interactable && button.name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) < 0 && button.name.IndexOf("Later", StringComparison.OrdinalIgnoreCase) < 0);
            }

            return ui.TryClick(candidate, "choice:" + overlayName, recorder, controller);
        }

        private int ChooseFeasibleOffer(List<object> offers, Component controller, out string reason)
        {
            int best = -1;
            int bestScore = int.MinValue;
            reason = "no_offer_is_feasible_now";
            for (int i = 0; i < offers.Count; i++)
            {
                object offer = offers[i];
                if (!FieldBool(offer, "feasibleNow") || FieldInt(offer, "targetRound") < RuntimeGameView.Int(controller, "CurrentRound"))
                {
                    continue;
                }

                string kind = FieldText(offer, "kind");
                string category = FieldText(offer, "category");
                int score = FieldInt(offer, "goldReward") * 4 + FieldInt(offer, "roundGoldBonus") * 20 + Mathf.Max(0, FieldInt(offer, "roundsRemaining")) * 4;
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
                    int board = RuntimeGameView.Int(controller, "BoardUnitCount");
                    if (board < 6 && (category == "SAFE" || kind == "SummonSprint" || kind == "MonsterHunter")) score += 140;
                    if (board >= 6 && (category == "GREED" || kind == "GoldReserve" || kind == "HighGradeForge" || kind == "RareUpgrade")) score += 130;
                }

                if (score > bestScore)
                {
                    best = i;
                    bestScore = score;
                    reason = "feasible_now; " + strategy + " score=" + score;
                }
            }

            return best;
        }

        private static string DescribeOffer(object offer)
        {
            return FieldText(offer, "kind") + " | " + FieldText(offer, "title") + " | " + FieldText(offer, "description") + " | " + FieldText(offer, "rewardText") + " | deadline=R" + FieldInt(offer, "targetRound") + " | feasible=" + FieldBool(offer, "feasibleNow");
        }

        private static string FieldText(object target, string field)
        {
            if (target == null) return string.Empty;
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public);
            object value = info == null ? null : info.GetValue(target);
            return value == null ? string.Empty : value.ToString();
        }

        private static int FieldInt(object target, string field)
        {
            int value;
            return int.TryParse(FieldText(target, field), out value) ? value : 0;
        }

        private static bool FieldBool(object target, string field)
        {
            bool value;
            return bool.TryParse(FieldText(target, field), out value) && value;
        }
    }
}