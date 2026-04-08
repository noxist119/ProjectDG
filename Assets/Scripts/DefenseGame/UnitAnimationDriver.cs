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
        private string currentState;
        private float lockUntilTime;

        public bool IsLocked => Time.time < lockUntilTime;

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
            bool played = TryPlayAny(attackStates);
            if (!played)
            {
                TrySetFirstInt(attackIndexInts, defaultAttackIndex);
                played = TrySetAnyTrigger(attackTriggers);
            }

            if (played)
            {
                LockFor(attackReturnDelay);
                ScheduleReturnToIdle(attackReturnDelay);
            }
        }

        public void PlaySkill()
        {
            bool played = TryPlayAny(skillStates);
            if (!played)
            {
                TrySetFirstInt(skillIndexInts, defaultSkillIndex);
                played = TrySetAnyTrigger(skillTriggers);
            }

            if (played)
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

        public void ForceIdle()
        {
            lockUntilTime = 0f;
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }

            PlayIdle();
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
            return IsLocked;
        }

        private void LockFor(float duration)
        {
            lockUntilTime = Mathf.Max(lockUntilTime, Time.time + duration);
        }
    }
}
