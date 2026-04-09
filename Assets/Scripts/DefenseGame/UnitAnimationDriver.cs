using System.Collections;
using UnityEngine;

namespace DefenseGame
{
    public class UnitAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string[] spawnStates = { "spawn", "Spawn" };
        [SerializeField] private string[] idleStates = { "idle", "Idle", "dle", "Dle", "Idle 0", "Idle_0", "Idle_01", "BattleIdle", "Battle_Idle", "LobbyIdle", "LobbyIdle 0", "Wait" };
        [SerializeField] private string[] walkStates = { "walk", "Walk", "Run", "Move" };
        [SerializeField] private string[] winStates = { "win", "Win" };
        [SerializeField] private string[] attackStates = { "Attack01", "Attack1", "attack1", "Attack02", "Attack2", "attack2", "attack", "Attack" };
        [SerializeField] private string[] skillStates = { "Skill01", "Skill1", "skill1", "Skill02", "Skill2", "skill2", "Skill03", "Skill3", "skill3", "skill", "Skill" };
        [SerializeField] private string[] spawnTriggers = { "Spawn" };
        [SerializeField] private string[] attackTriggers = { "Attack" };
        [SerializeField] private string[] skillTriggers = { "Skill" };
        [SerializeField] private string[] winTriggers = { "Win" };
        [SerializeField] private string[] attackIndexInts = { "AttackIndex" };
        [SerializeField] private string[] skillIndexInts = { "SkillIndex", "PlayIndex" };
        [SerializeField] private int defaultAttackIndex = 1;
        [SerializeField] private int defaultSkillIndex = 1;
        [SerializeField] private float actionBlendDuration = 0.08f;
        [SerializeField] private float spawnReturnDelay = 0.85f;
        [SerializeField] private float attackReturnDelay = 0.45f;
        [SerializeField] private float skillReturnDelay = 0.7f;
        [SerializeField] private float winHoldDuration = 2f;

        private Coroutine returnRoutine;
        private Coroutine actionRoutine;
        private string currentState;
        private string desiredLoopState;
        private float lockUntilTime;
        private bool actionInProgress;
        private bool movementLoopActive;

        public bool IsLocked => actionInProgress || Time.time < lockUntilTime;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                AnimationEventProxy proxy = animator.GetComponent<AnimationEventProxy>();
                if (proxy == null)
                {
                    animator.gameObject.AddComponent<AnimationEventProxy>();
                }
            }
        }

        public void PlaySpawn()
        {
            if (TryPlayAction(spawnStates, spawnTriggers, null, 0, spawnReturnDelay))
            {
                return;
            }

            PlayIdle();
        }

        public void PlayIdle()
        {
            if (movementLoopActive)
            {
                return;
            }

            SetDesiredLoopState(idleStates);
        }

        public void PlayWalk()
        {
            movementLoopActive = true;
            SetDesiredLoopState(walkStates);
        }

        public void PlayWin()
        {
            TryPlayAction(winStates, winTriggers, null, 0, winHoldDuration);
        }

        public void PlayAttack()
        {
            TryPlayAction(attackStates, attackTriggers, attackIndexInts, defaultAttackIndex, attackReturnDelay);
        }

        public void PlaySkill()
        {
            TryPlayAction(skillStates, skillTriggers, skillIndexInts, defaultSkillIndex, skillReturnDelay);
        }

        public void PlayMoving(bool isMoving)
        {
            movementLoopActive = isMoving;

            if (isMoving)
            {
                desiredLoopState = ResolveFirstPlayableState(walkStates);
                ForceLoopState(desiredLoopState, true);
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
                if (!string.IsNullOrWhiteSpace(state) && HasState(state))
                {
                    return state;
                }
            }

            return null;
        }

        private bool HasState(string stateName)
        {
            return animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash(stateName));
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
            CancelScheduledReturn();

            string actionState = ResolveFirstPlayableState(stateNames);
            if (!string.IsNullOrWhiteSpace(actionState))
            {
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
                LockFor(fallbackDuration);
                ScheduleReturnToDesiredLoop(fallbackDuration);
                return true;
            }

            return false;
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
