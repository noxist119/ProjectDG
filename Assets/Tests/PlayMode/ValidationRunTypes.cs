using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Tests
{
    internal enum ValidationRunState
    {
        Idle,
        Setup,
        OpeningGuide,
        ContractChoice,
        Preparing,
        Fighting,
        VictoryResult,
        GameOver,
        Record,
        Finished,
        Failed
    }

    internal enum PlayerLikeStrategy
    {
        StableBoard,
        ContractFirst,
        HighGradeInvestment
    }

    [Serializable]
    internal sealed class ValidationStateTransition
    {
        public string from;
        public string to;
        public float realtime;
        public string reason;
    }

    [Serializable]
    internal sealed class ValidationActionRecord
    {
        public int index;
        public float realtime;
        public int round;
        public string action;
        public string button;
    }

    [Serializable]
    internal sealed class ValidationRoundSnapshot
    {
        public int round;
        public string phase;
        public int life;
        public int gold;
        public int boardUnits;
        public int summons;
        public int merges;
        public int summonGradeLuckLevel;
        public string blockingChoiceReason;
        public string activeChoicePanels;
        public bool battleButtonInteractable;
    }

    [Serializable]
    internal sealed class ValidationMissionRecord
    {
        public int round;
        public List<string> offers = new List<string>();
        public string selected;
        public string selectionReason;
        public string condition;
        public string reward;
        public int deadline;
        public bool feasibleAtSelection;
    }

    [Serializable]
    internal sealed class ValidationRunRecord
    {
        public string pass = "Pass 21";
        public string executionInfrastructure = "Unity Test Framework PlayMode";
        public string executionId;
        public string unityProcessId;
        public string launcher = "Unity Test Framework";
        public string strategy;
        public int requestedSeed;
        public int actualContentSeed;
        public string status = "idle";
        public string finalState = "Idle";
        public bool infrastructureReady;
        public int runnerTickCount;
        public bool actualEventSystemClick;
        public string firstUiAction;
        public int firstUiActionRound = -1;
        public bool r1Started;
        public bool gameplayDefeat;
        public int finalRound;
        public int finalLife;
        public int finalGold;
        public int runtimeErrors;
        public int boardSlotWarnings;
        public string blockingChoiceReason;
        public string activeChoicePanels;
        public bool battleButtonInteractable;
        public string failureReason;
        public List<ValidationStateTransition> stateTransitions = new List<ValidationStateTransition>();
        public List<ValidationActionRecord> actions = new List<ValidationActionRecord>();
        public List<ValidationMissionRecord> missions = new List<ValidationMissionRecord>();
        public List<ValidationRoundSnapshot> rounds = new List<ValidationRoundSnapshot>();
    }

    internal static class ValidationRunLease
    {
        private static bool held;
        private static string holder;

        internal static bool TryAcquire(string executionId)
        {
            lock (typeof(ValidationRunLease))
            {
                if (held)
                {
                    return false;
                }

                held = true;
                holder = executionId;
                return true;
            }
        }

        internal static void Release(string executionId)
        {
            lock (typeof(ValidationRunLease))
            {
                if (held && string.Equals(holder, executionId, StringComparison.Ordinal))
                {
                    held = false;
                    holder = null;
                }
            }
        }
    }

    // Reflection keeps this test assembly out of the production Assembly-CSharp graph.
    internal static class RuntimeGameView
    {
        internal static readonly string[] ChoicePanelNames =
        {
            "RoundResultOverlay", "AugmentChoiceOverlay", "RunShopOverlay", "TacticalMissionOverlay", "LuckySummonChoiceOverlay", "Fate"
        };

        private static readonly Type ControllerType = Type.GetType("DefenseGame.DefenseGameController, Assembly-CSharp");
        private static readonly Type MissionType = Type.GetType("DefenseGame.TacticalMissionSystem, Assembly-CSharp");
        private static readonly Type HudType = Type.GetType("DefenseGame.SimpleGameHUD, Assembly-CSharp");

        internal static Component FindController()
        {
            return FindComponent(ControllerType);
        }

        internal static Component FindMissionSystem()
        {
            return FindComponent(MissionType);
        }

        internal static Component FindHud()
        {
            return FindComponent(HudType);
        }

        internal static Component FindComponent(Type type)
        {
            return type == null ? null : UnityEngine.Object.FindObjectOfType(type) as Component;
        }

        internal static object Property(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            return property == null ? null : property.GetValue(target, null);
        }

        internal static object Invoke(object target, string name, object[] arguments)
        {
            if (target == null)
            {
                return null;
            }

            int count = arguments == null ? 0 : arguments.Length;
            MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == count);
            return method == null ? null : method.Invoke(target, arguments);
        }

        internal static int Int(object target, string property)
        {
            object value = Property(target, property);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        internal static bool Bool(object target, string property)
        {
            object value = Property(target, property);
            return value != null && Convert.ToBoolean(value);
        }

        internal static string Text(object target, string property)
        {
            object value = Property(target, property);
            return value == null ? string.Empty : value.ToString();
        }

        internal static GameObject FindObject(string name)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsOfType<Transform>(true);
            GameObject inactive = null;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform == null || transform.gameObject.name != name)
                {
                    continue;
                }

                if (transform.gameObject.activeInHierarchy)
                {
                    return transform.gameObject;
                }

                if (inactive == null)
                {
                    inactive = transform.gameObject;
                }
            }

            return inactive;
        }

        internal static GameObject FindObjectContaining(string namePart)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsOfType<Transform>(true);
            GameObject inactive = null;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform == null || transform.gameObject.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (transform.gameObject.activeInHierarchy)
                {
                    return transform.gameObject;
                }

                if (inactive == null)
                {
                    inactive = transform.gameObject;
                }
            }

            return inactive;
        }

        internal static Button FindButton(string name)
        {
            Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>(true);
            Button fallback = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || button.name != name)
                {
                    continue;
                }

                if (button.gameObject.activeInHierarchy && button.interactable)
                {
                    return button;
                }

                if (fallback == null)
                {
                    fallback = button;
                }
            }

            return fallback;
        }

        internal static string ActiveChoicePanels()
        {
            List<string> active = new List<string>();
            for (int i = 0; i < ChoicePanelNames.Length; i++)
            {
                GameObject panel = FindObjectContaining(ChoicePanelNames[i]);
                if (panel != null && panel.activeInHierarchy)
                {
                    active.Add(ChoicePanelNames[i]);
                }
            }

            return active.Count == 0 ? "none" : string.Join(",", active.ToArray());
        }

        internal static int ActiveChoicePanelCount()
        {
            string active = ActiveChoicePanels();
            return active == "none" ? 0 : active.Split(',').Length;
        }

        internal static bool IsButtonInteractable(string name)
        {
            Button button = FindButton(name);
            return button != null && button.gameObject.activeInHierarchy && button.interactable;
        }

        internal static ValidationRoundSnapshot CaptureSnapshot(Component controller, string phase)
        {
            return new ValidationRoundSnapshot
            {
                round = Int(controller, "CurrentRound"),
                phase = phase,
                life = Int(controller, "Life"),
                gold = Int(controller, "Gold"),
                boardUnits = Int(controller, "BoardUnitCount"),
                summons = Int(controller, "RunTotalPlayerSummons"),
                merges = Int(controller, "RunTotalMerges"),
                summonGradeLuckLevel = Int(controller, "SummonGradeLuckLevel"),
                blockingChoiceReason = Text(controller, "BlockingChoiceReason"),
                activeChoicePanels = ActiveChoicePanels(),
                battleButtonInteractable = IsButtonInteractable("BattleButton")
            };
        }
    }
}