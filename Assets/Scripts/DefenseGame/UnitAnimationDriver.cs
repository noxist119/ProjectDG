using System.Collections;
using UnityEngine;

namespace DefenseGame
{
    public class UnitAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string[] spawnStates = { "spawn", "Spawn" };
        [SerializeField] private string[] idleStates = { "idle", "Idle", "dle", "Dle" };
        [SerializeField] private string[] walkStates = { "walk", "Walk" };
        [SerializeField] private string[] winStates = { "win", "Win" };
        [SerializeField] private string[] attackStates = { "attack", "Attack", "Attack1", "Attack2", "Attack01", "Attack02", "attack1", "attack2" };
        [SerializeField] private string[] skillStates = { "skill", "Skill", "Skill1", "Skill2", "Skill01", "Skill02", "skill1", "skill2" };
        [SerializeField] private string[] spawnTriggers = { "Spawn" };
        [SerializeField] private string[] attackTriggers = { "Attack" };
        [SerializeField] private string[] skillTriggers = { "Skill" };
        [SerializeField] private string[] winTriggers = { "Win" };
        [SerializeField] private string[] attackIndexInts = { "AttackIndex" };
        [SerializeField] private string[] skillIndexInts = { "SkillIndex", "PlayIndex" };
        [SerializeField] private float actionBlendDuration = 0.08f;
        [SerializeField] private float spawnReturnDelay = 0.85f;
        [SerializeField] private float attackReturnDelay = 0.45f;
        [SerializeField] private float skillReturnDelay = 0.7f;
        [SerializeField] private float winHoldDuration = 2f;

        private Coroutine returnRoutine;
        private string currentState;
        private float lockUntilTime;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public void PlaySpawn()
        {
            if (TryPlayAny(spawnStates) || TrySetAnyTrigger(spawnTriggers))
            {
                LockFor(spawnReturnDelay);
                ScheduleReturnToIdle(spawnReturnDelay);
            }
            else
            {
                PlayIdle();
            }
        }

        public void PlayIdle()
        {
            TryPlayAny(idleStates);
        }

        public void PlayWalk()
        {
            TryPlayAny(walkStates);
        }

        public void PlayWin()
        {
            if (TryPlayAny(winStates) || TrySetAnyTrigger(winTriggers))
            {
                LockFor(winHoldDuration);
            }
        }

        public void PlayAttack()
        {
            TrySetFirstInt(attackIndexInts, Random.Range(0, 2));
            if (TryPlayAny(attackStates) || TrySetAnyTrigger(attackTriggers))
            {
                LockFor(attackReturnDelay);
                ScheduleReturnToIdle(attackReturnDelay);
            }
        }

        public void PlaySkill()
        {
            TrySetFirstInt(skillIndexInts, Random.Range(0, 3));
            if (TryPlayAny(skillStates) || TrySetAnyTrigger(skillTriggers))
            {
                LockFor(skillReturnDelay);
                ScheduleReturnToIdle(skillReturnDelay);
            }
        }

        public void PlayMoving(bool isMoving)
        {
            if (Time.time < lockUntilTime)
            {
                return;
            }

            if (isMoving)
            {
                PlayWalk();
            }
            else if (!IsBusyState())
            {
                PlayIdle();
            }
        }

        private bool TryPlayAny(string[] stateNames)
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
                    if (currentState != state)
                    {
                        animator.CrossFade(state, actionBlendDuration, 0);
                        currentState = state;
                    }
                    return true;
                }
            }

            return false;
        }

        private bool HasState(string stateName)
        {
            return animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash(stateName));
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
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
            }

            returnRoutine = StartCoroutine(ReturnToIdleAfter(delay));
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

        private bool IsBusyState()
        {
            return Time.time < lockUntilTime;
        }

        private void LockFor(float duration)
        {
            lockUntilTime = Mathf.Max(lockUntilTime, Time.time + duration);
        }
    }
}
