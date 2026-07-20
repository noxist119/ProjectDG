using System.Collections;
using UnityEngine;

namespace DefenseGame
{
    public enum AnimationImpactType
    {
        Auto = 0,
        Attack = 1,
        Skill = 2,
        AttackHit = 3,
        FireProjectile = 4
    }

    public class UnitAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string[] spawnStates = { "spawn", "Spawn" };
        [SerializeField] private string[] idleStates = { "idle", "Idle", "dle", "Dle", "Idle 0", "Idle_0", "Idle_01", "BattleIdle", "Battle_Idle", "LobbyIdle", "LobbyIdle 0", "Wait" };
        [SerializeField] private string[] dormantStates = { "spawn_idle", "Spawn_idle", "Spawn_Idle", "SpawnIdle", "spawn_loop", "Spawn_loop", "Spawn_Loop", "SpawnLoop" };
        [SerializeField] private string[] walkStates = { "walk", "Walk", "Run", "Move" };
        [SerializeField] private string[] winStates = { "win", "Win" };
        [SerializeField] private string[] attackStates = { "attack01", "Attack01", "attack_01", "Attack_01", "Attack1", "attack1", "attack02", "Attack02", "attack_02", "Attack_02", "Attack2", "attack2", "basic_attack", "BasicAttack", "attack", "Attack" };
        [SerializeField] private string[] skill01States = { "skill01", "Skill01", "skill_01", "Skill_01", "Skill1", "skill1", "Skill01 0", "skill01 0", "Skill_01 0", "skill_01 0" };
        [SerializeField] private string[] skill02States = { "skill02", "Skill02", "skill_02", "Skill_02", "Skill2", "skill2", "Skill02 0", "skill02 0", "Skill_02 0", "skill_02 0" };
        [SerializeField] private string[] skill03StartStates = { "skill03_start", "Skill03_Start", "Skill03Start", "skill_03_start", "Skill_03_Start", "Skill03_Start 0", "skill03_start 0", "Skill03Start 0", "Skill_03_Start 0", "skill_03_start 0" };
        [SerializeField] private string[] skill03LoopStates = { "skill03_loop", "Skill03_Loop", "Skill03Loop", "skill_03_loop", "Skill_03_Loop", "Skill03_Loop 0", "skill03_loop 0", "Skill03Loop 0", "Skill_03_Loop 0", "skill_03_loop 0" };
        [SerializeField] private string[] skill03EndStates = { "skill03_end", "Skill03_End", "Skill03End", "skill_03_end", "Skill_03_End", "Skill03_End 0", "skill03_end 0", "Skill03End 0", "Skill_03_End 0", "skill_03_end 0" };
        [SerializeField] private string[] spawnTriggers = { "Spawn" };
        [SerializeField] private string[] attackTriggers = { "Attack" };
        [SerializeField] private string[] skillTriggers = { "Skill" };
        [SerializeField] private string[] winTriggers = { "Win" };
        [SerializeField] private string[] attackIndexInts = { "AttackIndex" };
        [SerializeField] private string[] skillIndexInts = { "SkillIndex", "PlayIndex" };
        [SerializeField] private int defaultAttackIndex = 1;
        [SerializeField] private float actionBlendDuration = 0.08f;
        [SerializeField] private float spawnReturnDelay = 0.85f;
        [SerializeField] private float attackReturnDelay = 0.45f;
        [SerializeField] private float skillReturnDelay = 0.7f;
        [SerializeField] private float skill03LoopHoldDuration = 0.35f;
        [SerializeField] private float winHoldDuration = 2f;
        [SerializeField] private float attackImpactFallbackDelay = 0.18f;
        [SerializeField] private float attackEventFallbackDelay = 0.85f;
        [SerializeField] private float skillImpactFallbackDelay = 0.32f;
        [SerializeField] private float skillEventFallbackDelay = 0.90f;
        [SerializeField] private float skillSequenceImpactFallbackDelay = 2.20f;

        private Coroutine returnRoutine;
        private Coroutine actionRoutine;
        private string currentState;
        private string desiredLoopState;
        private float lockUntilTime;
        private bool actionInProgress;
        private bool movementLoopActive;
        private AnimationImpactType activeImpactType = AnimationImpactType.Auto;
        private float activeAttackImpactFallbackDelay;
        private float activeSkillImpactFallbackDelay;
        private bool lastPlayedActionHasImpactEvent;
        private bool lastPlayedActionIsSkillSequence;
        private float lastPlayedImpactEventTime = -1f;

        public bool IsLocked => actionInProgress || Time.time < lockUntilTime;
        public float AttackImpactFallbackDelay => Mathf.Max(0.02f, activeAttackImpactFallbackDelay > 0f ? activeAttackImpactFallbackDelay : attackImpactFallbackDelay);
        public float SkillImpactFallbackDelay => Mathf.Max(0.02f, activeSkillImpactFallbackDelay > 0f ? activeSkillImpactFallbackDelay : skillImpactFallbackDelay);

        public event System.Action<AnimationImpactType> ImpactTriggered;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                // Movement is controlled by board/combat scripts, so root motion would cause attack/skill sliding.
                animator.applyRootMotion = false;
                AnimationEventProxy proxy = animator.GetComponent<AnimationEventProxy>();
                if (proxy == null)
                {
                    animator.gameObject.AddComponent<AnimationEventProxy>();
                }
            }
        }

        public bool PlaySpawn()
        {
            if (TryPlayAction(spawnStates, spawnTriggers, null, 0, spawnReturnDelay))
            {
                return true;
            }

            PlayIdle();
            return false;
        }

        public void PlayIdle()
        {
            if (movementLoopActive)
            {
                return;
            }

            SetDesiredLoopState(idleStates);
        }

        public void PlayDormantLoop()
        {
            movementLoopActive = false;
            SetDesiredLoopState(dormantStates);
        }

        public void PlayWalk()
        {
            movementLoopActive = true;
            SetDesiredLoopState(walkStates);
        }

        public bool PlayWin()
        {
            return TryPlayAction(winStates, winTriggers, null, 0, winHoldDuration);
        }

        public bool PlayAttack()
        {
            activeImpactType = AnimationImpactType.Attack;
            activeAttackImpactFallbackDelay = 0f;
            bool played = TryPlayAction(attackStates, attackTriggers, attackIndexInts, defaultAttackIndex, attackReturnDelay);
            if (played && lastPlayedActionHasImpactEvent)
            {
                activeAttackImpactFallbackDelay = Mathf.Max(attackEventFallbackDelay, lastPlayedImpactEventTime + 0.20f);
            }

            return played;
        }

        public bool PlaySkill()
        {
            return PlaySkill(1);
        }

        public bool PlaySkill(int skillSlot)
        {
            activeImpactType = AnimationImpactType.Skill;
            activeSkillImpactFallbackDelay = 0f;
            lastPlayedActionHasImpactEvent = false;
            lastPlayedActionIsSkillSequence = false;
            lastPlayedImpactEventTime = -1f;

            bool played;
            if (skillSlot <= 1)
            {
                played = TryPlaySkillSlot(skill01States, 1)
                    || TryPlaySkillSlot(skill02States, 2)
                    || TryPlaySkill03Sequence()
                    || TryPlaySkillStartOnly();
            }
            else if (skillSlot == 2)
            {
                played = TryPlaySkillSlot(skill02States, 2)
                    || TryPlaySkill03Sequence()
                    || TryPlaySkillStartOnly()
                    || TryPlaySkillSlot(skill01States, 1);
            }
            else
            {
                played = TryPlaySkill03Sequence()
                    || TryPlaySkillStartOnly()
                    || TryPlaySkillSlot(skill01States, 1)
                    || TryPlaySkillSlot(skill02States, 2);
            }

            if (played && lastPlayedActionHasImpactEvent)
            {
                activeSkillImpactFallbackDelay = lastPlayedActionIsSkillSequence
                    ? skillSequenceImpactFallbackDelay
                    : Mathf.Max(skillEventFallbackDelay, lastPlayedImpactEventTime + 0.25f);
            }

            return played;
        }

        public void NotifyAnimationImpact(AnimationImpactType impactType)
        {
            AnimationImpactType resolvedType = impactType == AnimationImpactType.Auto ? activeImpactType : impactType;
            if (resolvedType == AnimationImpactType.Auto)
            {
                resolvedType = AnimationImpactType.Attack;
            }

            ImpactTriggered?.Invoke(resolvedType);
        }

        public void PlayMoving(bool isMoving)
        {
            movementLoopActive = isMoving;

            if (isMoving)
            {
                desiredLoopState = ResolveFirstPlayableState(walkStates);
                if (!IsLocked)
                {
                    ForceLoopState(desiredLoopState, true);
                }
            }
            else
            {
                desiredLoopState = ResolveFirstPlayableState(idleStates);
                if (!IsLocked)
                {
                    ForceLoopState(desiredLoopState, false);
                }
            }
        }

        public void ForceIdle()
        {
            movementLoopActive = false;
            actionInProgress = false;
            lockUntilTime = 0f;
            CancelActionRoutine();
            CancelScheduledReturn();
            desiredLoopState = ResolveFirstPlayableState(idleStates);
            ForceLoopState(desiredLoopState, false);
        }

        private bool TryPlayAny(string[] stateNames, bool allowRestartCurrent = false)
        {
            if (animator == null || stateNames == null)
            {
                return false;
            }

            for (int i = 0; i < stateNames.Length; i++)
            {
                string state = stateNames[i];
                if (string.IsNullOrWhiteSpace(state))
                {
                    continue;
                }

                if (HasState(state))
                {
                    bool isCurrentlyPlaying = IsCurrentlyInState(state);
                    bool shouldRestart = allowRestartCurrent && (!isCurrentlyPlaying || ShouldRestartCurrentState(state));
                    if (currentState != state || !isCurrentlyPlaying || shouldRestart)
                    {
                        if (shouldRestart)
                        {
                            animator.Play(state, 0, 0f);
                        }
                        else
                        {
                            animator.CrossFade(state, actionBlendDuration, 0);
                        }

                        currentState = state;
                    }
                    return true;
                }
            }

            return false;
        }

        private bool TryPlayLoopingState(string[] stateNames)
        {
            if (animator == null || stateNames == null)
            {
                return false;
            }

            for (int i = 0; i < stateNames.Length; i++)
            {
                string state = stateNames[i];
                if (string.IsNullOrWhiteSpace(state) || !HasState(state))
                {
                    continue;
                }

                if (IsTransitioningAwayFromState(state))
                {
                    animator.Play(state, 0, GetCurrentLoopNormalizedTime());
                    currentState = state;
                }
                else if (!IsCurrentlyInState(state))
                {
                    animator.Play(state, 0, 0f);
                    currentState = state;
                }

                return true;
            }

            return false;
        }

        private void SetDesiredLoopState(string[] stateNames)
        {
            desiredLoopState = ResolveFirstPlayableState(stateNames);
            if (string.IsNullOrWhiteSpace(desiredLoopState))
            {
                return;
            }

            if (!IsLocked)
            {
                ApplyDesiredLoopState();
            }
        }

        private void ApplyDesiredLoopState()
        {
            if (IsLocked || string.IsNullOrWhiteSpace(desiredLoopState) || animator == null)
            {
                return;
            }

            ForceLoopState(desiredLoopState, movementLoopActive);
        }

        private string ResolveFirstPlayableState(string[] stateNames)
        {
            if (stateNames == null)
            {
                return null;
            }

            for (int i = 0; i < stateNames.Length; i++)
            {
                string state = stateNames[i];
                string resolvedState = ResolveAnimatorStateName(state);
                if (!string.IsNullOrWhiteSpace(resolvedState))
                {
                    return resolvedState;
                }
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return null;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null)
            {
                return null;
            }

            for (int i = 0; i < stateNames.Length; i++)
            {
                string keyword = NormalizeAnimationName(stateNames[i]);
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                for (int j = 0; j < clips.Length; j++)
                {
                    AnimationClip clip = clips[j];
                    if (clip == null || clip.empty)
                    {
                        continue;
                    }

                    string clipName = NormalizeAnimationName(clip.name);
                    string resolvedClipState = ResolveAnimatorStateName(clip.name);
                    if (clipName.Contains(keyword) && !string.IsNullOrWhiteSpace(resolvedClipState))
                    {
                        return resolvedClipState;
                    }
                }
            }

            return null;
        }

        private bool TryPlaySkillSlot(string[] stateNames, int skillIndex)
        {
            if (IsLocked)
            {
                return false;
            }

            string actionState = ResolveFirstDirectSkillSlotState(stateNames);
            if (string.IsNullOrWhiteSpace(actionState))
            {
                return false;
            }

            TrySetFirstInt(skillIndexInts, skillIndex);
            return TryPlayResolvedActionState(actionState, stateNames, skillReturnDelay);
        }

        private string ResolveFirstDirectSkillSlotState(string[] stateNames)
        {
#if UNITY_EDITOR
            string editorStateWithMotion = ResolveFirstEditorAnimatorState(stateNames, true);
            if (!string.IsNullOrWhiteSpace(editorStateWithMotion))
            {
                return editorStateWithMotion;
            }

            if (!string.IsNullOrWhiteSpace(ResolveFirstEditorAnimatorState(stateNames, false)))
            {
                return null;
            }
#endif

            if (!HasDirectSkillClipMatching(stateNames))
            {
                return null;
            }

            return ResolveFirstPlayableState(stateNames);
        }

        private string ResolveFirstSkillSequenceState(string[] stateNames, string sequencePart)
        {
#if UNITY_EDITOR
            string editorStateWithMotion = ResolveFirstEditorAnimatorState(stateNames, true);
            if (!string.IsNullOrWhiteSpace(editorStateWithMotion))
            {
                return editorStateWithMotion;
            }

            if (!string.IsNullOrWhiteSpace(ResolveFirstEditorAnimatorState(stateNames, false)))
            {
                return null;
            }
#endif

            if (!HasSkillSequenceClipPart(sequencePart))
            {
                return null;
            }

            return ResolveFirstPlayableState(stateNames);
        }

        private bool TryPlaySkill03Sequence()
        {
            if (IsLocked)
            {
                return false;
            }

            string startState = ResolveFirstSkillSequenceState(skill03StartStates, "start");
            string loopState = ResolveFirstSkillSequenceState(skill03LoopStates, "loop");
            string endState = ResolveFirstSkillSequenceState(skill03EndStates, "end");
            if (string.IsNullOrWhiteSpace(startState) || string.IsNullOrWhiteSpace(loopState) || string.IsNullOrWhiteSpace(endState))
            {
                return false;
            }

            lastPlayedActionIsSkillSequence = true;
            lastPlayedActionHasImpactEvent = true;
            lastPlayedImpactEventTime = 0f;

            CancelScheduledReturn();
            CancelActionRoutine();
            actionRoutine = StartCoroutine(PlaySkill03Sequence(startState, loopState, endState));
            return true;
        }

        private bool TryPlaySkillStartOnly()
        {
            if (IsLocked)
            {
                return false;
            }

            string startState = ResolveFirstSkillSequenceState(skill03StartStates, "start");
            if (string.IsNullOrWhiteSpace(startState))
            {
                return false;
            }

            TrySetFirstInt(skillIndexInts, 3);
            return TryPlayResolvedActionState(startState, skill03StartStates, skillReturnDelay);
        }

        private bool HasDirectSkillClipMatching(string[] stateNames)
        {
            if (animator == null || animator.runtimeAnimatorController == null || stateNames == null)
            {
                return false;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null)
            {
                return false;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null || clip.empty || IsSkillSequenceClipName(clip.name))
                {
                    continue;
                }

                string clipName = NormalizeAnimationName(clip.name);
                if (string.IsNullOrWhiteSpace(clipName))
                {
                    continue;
                }

                for (int j = 0; j < stateNames.Length; j++)
                {
                    string keyword = NormalizeAnimationName(stateNames[j]);
                    if (!string.IsNullOrWhiteSpace(keyword) && (clipName.Contains(keyword) || keyword.Contains(clipName)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasSkillSequenceClipPart(string sequencePart)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null)
            {
                return false;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null || clip.empty)
                {
                    continue;
                }

                string clipName = NormalizeAnimationName(clip.name);
                if (LooksLikeSkillClip(clipName) && ClipNameHasSequencePart(clip.name, sequencePart))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSkillSequenceClipName(string clipName)
        {
            return ClipNameHasSequencePart(clipName, "start")
                || ClipNameHasSequencePart(clipName, "loop")
                || ClipNameHasSequencePart(clipName, "end");
        }

        private static bool ClipNameHasSequencePart(string clipName, string sequencePart)
        {
            if (string.IsNullOrWhiteSpace(clipName) || string.IsNullOrWhiteSpace(sequencePart))
            {
                return false;
            }

            string lowerName = clipName.ToLowerInvariant();
            string part = sequencePart.ToLowerInvariant();
            if (lowerName.Contains("_" + part) || lowerName.Contains(part + "_")
                || lowerName.Contains("-" + part) || lowerName.Contains(part + "-")
                || lowerName.Contains(" " + part) || lowerName.Contains(part + " "))
            {
                return true;
            }

            string normalizedName = NormalizeAnimationName(clipName);
            string normalizedPart = NormalizeAnimationName(sequencePart);
            return !string.IsNullOrWhiteSpace(normalizedName)
                && !string.IsNullOrWhiteSpace(normalizedPart)
                && normalizedName.EndsWith(normalizedPart);
        }

        private static bool LooksLikeSkillClip(string normalizedClipName)
        {
            return !string.IsNullOrWhiteSpace(normalizedClipName)
                && (normalizedClipName.Contains("skill") || normalizedClipName.Contains("cast"));
        }

        private bool TryPlayResolvedActionState(string actionState, string[] stateNames, float fallbackDuration)
        {
            lastPlayedActionHasImpactEvent = false;
            lastPlayedActionIsSkillSequence = false;

            if (animator == null || IsLocked || string.IsNullOrWhiteSpace(actionState))
            {
                return false;
            }

            CancelScheduledReturn();
            lastPlayedActionHasImpactEvent = HasImpactEventForAction(actionState, activeImpactType, stateNames);
            animator.CrossFade(actionState, actionBlendDuration, 0);
            currentState = actionState;
            StartActionObservation(actionState, fallbackDuration);
            return true;
        }

        private IEnumerator PlaySkill03Sequence(string startState, string loopState, string endState)
        {
            actionInProgress = true;
            lockUntilTime = 0f;

            PlayActionState(startState);
            yield return WaitForStateToComplete(startState, skillReturnDelay * 0.45f);

            PlayActionState(loopState);
            float loopEndTime = Time.time + Mathf.Max(0.05f, skill03LoopHoldDuration);
            while (Time.time < loopEndTime && IsCurrentlyInState(loopState))
            {
                yield return null;
            }

            PlayActionState(endState);
            yield return WaitForStateToComplete(endState, skillReturnDelay * 0.45f);

            actionInProgress = false;
            actionRoutine = null;
            ApplyDesiredLoopState();
        }

        private void PlayActionState(string stateName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            animator.CrossFade(stateName, actionBlendDuration, 0);
            currentState = stateName;
        }

        private IEnumerator WaitForStateToComplete(string stateName, float fallbackDuration)
        {
            float enterDeadline = Time.time + 0.5f;
            while (Time.time < enterDeadline && !IsCurrentlyInState(stateName))
            {
                yield return null;
            }

            if (!IsCurrentlyInState(stateName))
            {
                yield return new WaitForSeconds(fallbackDuration);
                yield break;
            }

            while (IsCurrentlyInState(stateName))
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.normalizedTime >= 0.98f)
                {
                    break;
                }

                yield return null;
            }
        }

        private static string NormalizeAnimationName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.ToLowerInvariant()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);
        }

        private string ResolveAnimatorStateName(string stateName)
        {
            if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            {
                return null;
            }

            string trimmed = stateName.Trim();
            string[] candidates =
            {
                trimmed,
                "Base Layer." + trimmed,
                "Base Layer.Attack StateMachine." + trimmed,
                "Base Layer.Skill StateMachine." + trimmed,
                "Base Layer.Lobby StateMachine." + trimmed
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (animator.HasState(0, Animator.StringToHash(candidate)))
                {
                    return candidate;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private string ResolveFirstEditorAnimatorState(string[] stateNames, bool requireMotion)
        {
            if (stateNames == null)
            {
                return null;
            }

            UnityEditor.Animations.AnimatorController controller = ResolveEditorAnimatorController();
            if (controller == null || controller.layers == null)
            {
                return null;
            }

            for (int i = 0; i < stateNames.Length; i++)
            {
                string normalizedStateName = NormalizeAnimationName(stateNames[i]);
                if (string.IsNullOrWhiteSpace(normalizedStateName))
                {
                    continue;
                }

                for (int j = 0; j < controller.layers.Length; j++)
                {
                    UnityEditor.Animations.AnimatorControllerLayer layer = controller.layers[j];
                    if (layer == null || layer.stateMachine == null)
                    {
                        continue;
                    }

                    string resolvedState = ResolveFirstEditorAnimatorState(layer.stateMachine, layer.name, normalizedStateName, requireMotion);
                    if (!string.IsNullOrWhiteSpace(resolvedState))
                    {
                        return resolvedState;
                    }
                }
            }

            return null;
        }

        private UnityEditor.Animations.AnimatorController ResolveEditorAnimatorController()
        {
            if (animator == null)
            {
                return null;
            }

            RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
            while (runtimeController is AnimatorOverrideController overrideController)
            {
                runtimeController = overrideController.runtimeAnimatorController;
            }

            return runtimeController as UnityEditor.Animations.AnimatorController;
        }

        private static string ResolveFirstEditorAnimatorState(
            UnityEditor.Animations.AnimatorStateMachine stateMachine,
            string statePath,
            string normalizedStateName,
            bool requireMotion)
        {
            if (stateMachine == null)
            {
                return null;
            }

            UnityEditor.Animations.ChildAnimatorState[] states = stateMachine.states;
            if (states != null)
            {
                for (int i = 0; i < states.Length; i++)
                {
                    UnityEditor.Animations.AnimatorState state = states[i].state;
                    if (state == null || NormalizeAnimationName(state.name) != normalizedStateName)
                    {
                        continue;
                    }

                    if (requireMotion && state.motion == null)
                    {
                        continue;
                    }

                    return string.IsNullOrWhiteSpace(statePath) ? state.name : statePath + "." + state.name;
                }
            }

            UnityEditor.Animations.ChildAnimatorStateMachine[] childMachines = stateMachine.stateMachines;
            if (childMachines == null)
            {
                return null;
            }

            for (int i = 0; i < childMachines.Length; i++)
            {
                UnityEditor.Animations.AnimatorStateMachine childMachine = childMachines[i].stateMachine;
                if (childMachine == null)
                {
                    continue;
                }

                string childPath = string.IsNullOrWhiteSpace(statePath) ? childMachine.name : statePath + "." + childMachine.name;
                string resolvedState = ResolveFirstEditorAnimatorState(childMachine, childPath, normalizedStateName, requireMotion);
                if (!string.IsNullOrWhiteSpace(resolvedState))
                {
                    return resolvedState;
                }
            }

            return null;
        }
#endif

        private bool HasState(string stateName)
        {
            return !string.IsNullOrWhiteSpace(ResolveAnimatorStateName(stateName));
        }

        private bool ShouldRestartCurrentState(string stateName)
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(stateName))
            {
                return false;
            }

            return stateInfo.normalizedTime >= 0.98f;
        }

        private bool IsCurrentlyInState(string stateName)
        {
            if (animator == null)
            {
                return false;
            }

            return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
        }

        private bool IsTransitioningAwayFromState(string stateName)
        {
            if (animator == null || !animator.IsInTransition(0))
            {
                return false;
            }

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            return current.IsName(stateName) && !next.IsName(stateName);
        }

        private float GetCurrentLoopNormalizedTime()
        {
            if (animator == null)
            {
                return 0f;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateInfo.normalizedTime;
            return normalizedTime - Mathf.Floor(normalizedTime);
        }

        private void ForceLoopState(string stateName, bool preserveNormalizedTime)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            bool shouldReapply = IsTransitioningAwayFromState(stateName) || !IsCurrentlyInState(stateName);
            if (!shouldReapply)
            {
                return;
            }

            float normalizedTime = preserveNormalizedTime ? GetCurrentLoopNormalizedTime() : 0f;
            animator.Play(stateName, 0, normalizedTime);
            currentState = stateName;
        }

        private bool TryPlayAction(string[] stateNames, string[] triggerNames, string[] intParameterNames, int intValue, float fallbackDuration)
        {
            lastPlayedActionHasImpactEvent = false;
            lastPlayedActionIsSkillSequence = false;

            if (IsLocked)
            {
                return false;
            }

            CancelScheduledReturn();

            string actionState = ResolveFirstPlayableState(stateNames);
            if (!string.IsNullOrWhiteSpace(actionState))
            {
                lastPlayedActionHasImpactEvent = HasImpactEventForAction(actionState, activeImpactType, stateNames);
                animator.CrossFade(actionState, actionBlendDuration, 0);
                currentState = actionState;
                StartActionObservation(actionState, fallbackDuration);
                return true;
            }

            if (intParameterNames != null)
            {
                TrySetFirstInt(intParameterNames, intValue);
            }

            if (TrySetAnyTrigger(triggerNames))
            {
                lastPlayedActionHasImpactEvent = HasImpactEventForAction(null, activeImpactType, stateNames);
                LockFor(fallbackDuration);
                ScheduleReturnToDesiredLoop(fallbackDuration);
                return true;
            }

            return false;
        }

        private bool HasImpactEventForAction(string actionState, AnimationImpactType impactType, string[] stateNames)
        {
            lastPlayedImpactEventTime = -1f;

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null)
            {
                return false;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null || clip.events == null || clip.events.Length == 0)
                {
                    continue;
                }

                if (!ClipMatchesAction(clip, actionState, stateNames))
                {
                    continue;
                }

                bool found = false;
                for (int j = 0; j < clip.events.Length; j++)
                {
                    if (IsImpactEventFunction(clip.events[j].functionName, impactType))
                    {
                        lastPlayedImpactEventTime = Mathf.Max(lastPlayedImpactEventTime, clip.events[j].time);
                        found = true;
                    }
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ClipMatchesAction(AnimationClip clip, string actionState, string[] stateNames)
        {
            string clipName = NormalizeAnimationName(clip != null ? clip.name : null);
            if (string.IsNullOrWhiteSpace(clipName))
            {
                return false;
            }

            string normalizedState = NormalizeAnimationName(actionState);
            if (!string.IsNullOrWhiteSpace(normalizedState) && (normalizedState.Contains(clipName) || clipName.Contains(normalizedState)))
            {
                return true;
            }

            if (stateNames == null)
            {
                return false;
            }

            for (int i = 0; i < stateNames.Length; i++)
            {
                string keyword = NormalizeAnimationName(stateNames[i]);
                if (!string.IsNullOrWhiteSpace(keyword) && (clipName.Contains(keyword) || keyword.Contains(clipName)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsImpactEventFunction(string functionName, AnimationImpactType impactType)
        {
            string normalized = NormalizeAnimationName(functionName);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (normalized == "hit" || normalized == "impact" || normalized == "damage")
            {
                return true;
            }

            switch (impactType)
            {
                case AnimationImpactType.Attack:
                    return normalized == "attackhit"
                        || normalized == "attackimpact"
                        || normalized == "fireprojectile"
                        || normalized == "shoot";
                case AnimationImpactType.AttackHit:
                    return normalized == "attackhit" || normalized == "attackimpact";
                case AnimationImpactType.FireProjectile:
                    return normalized == "fireprojectile" || normalized == "shoot";
                case AnimationImpactType.Skill:
                    return normalized == "skillhit"
                        || normalized == "skillimpact"
                        || normalized == "skillfire"
                        || normalized == "skillapply"
                        || normalized == "castimpact"
                        || normalized == "spawnarea";
                default:
                    return false;
            }
        }

        private bool TrySetAnyTrigger(string[] triggerNames)
        {
            if (animator == null || animator.runtimeAnimatorController == null || triggerNames == null)
            {
                return false;
            }

            for (int i = 0; i < triggerNames.Length; i++)
            {
                if (HasParameter(triggerNames[i], AnimatorControllerParameterType.Trigger))
                {
                    animator.SetTrigger(triggerNames[i]);
                    currentState = triggerNames[i];
                    return true;
                }
            }

            return false;
        }

        private void TrySetFirstInt(string[] parameterNames, int value)
        {
            if (animator == null || animator.runtimeAnimatorController == null || parameterNames == null)
            {
                return;
            }

            for (int i = 0; i < parameterNames.Length; i++)
            {
                if (HasParameter(parameterNames[i], AnimatorControllerParameterType.Int))
                {
                    animator.SetInteger(parameterNames[i], value);
                    return;
                }
            }
        }

        private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.parameters == null)
            {
                return false;
            }

            for (int i = 0; i < animator.parameters.Length; i++)
            {
                if (animator.parameters[i].name == parameterName && animator.parameters[i].type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private void ScheduleReturnToIdle(float delay)
        {
            CancelScheduledReturn();

            returnRoutine = StartCoroutine(ReturnToIdleAfter(delay));
        }

        private void ScheduleReturnToDesiredLoop(float delay)
        {
            CancelScheduledReturn();

            returnRoutine = StartCoroutine(ReturnToDesiredLoopAfter(delay));
        }

        private void CancelScheduledReturn()
        {
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
        }

        private void CancelActionRoutine()
        {
            if (actionRoutine != null)
            {
                StopCoroutine(actionRoutine);
                actionRoutine = null;
            }
        }

        private IEnumerator ReturnToIdleAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            returnRoutine = null;
            if (Time.time >= lockUntilTime)
            {
                PlayIdle();
            }
        }

        private IEnumerator ReturnToDesiredLoopAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            returnRoutine = null;
            lockUntilTime = 0f;
            ApplyDesiredLoopState();
        }

        private void StartActionObservation(string stateName, float fallbackDuration)
        {
            CancelActionRoutine();
            actionRoutine = StartCoroutine(ObserveActionState(stateName, fallbackDuration));
        }

        private IEnumerator ObserveActionState(string stateName, float fallbackDuration)
        {
            actionInProgress = true;
            lockUntilTime = 0f;

            float enterDeadline = Time.time + 0.5f;
            while (Time.time < enterDeadline && !IsCurrentlyInState(stateName))
            {
                yield return null;
            }

            if (!IsCurrentlyInState(stateName))
            {
                yield return new WaitForSeconds(fallbackDuration);
            }
            else
            {
                while (IsCurrentlyInState(stateName))
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.normalizedTime >= 0.98f)
                    {
                        break;
                    }

                    yield return null;
                }
            }

            actionInProgress = false;
            actionRoutine = null;
            ApplyDesiredLoopState();
        }

        private bool IsBusyState()
        {
            return IsLocked;
        }

        private void LockFor(float duration)
        {
            lockUntilTime = Mathf.Max(lockUntilTime, Time.time + duration);
        }
    }
}
