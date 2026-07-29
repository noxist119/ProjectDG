using System;
using System.Collections;
using UnityEditor.Animations;
using UnityEngine;

namespace DefenseGame;

public class UnitAnimationDriver : MonoBehaviour
{
	[SerializeField]
	private Animator animator;

	private UnitAnimatorLodController animatorLodController;

	[SerializeField]
	private string[] spawnStates = new string[2] { "spawn", "Spawn" };

	[SerializeField]
	private string[] idleStates = new string[12]
	{
		"idle", "Idle", "dle", "Dle", "Idle 0", "Idle_0", "Idle_01", "BattleIdle", "Battle_Idle", "LobbyIdle",
		"LobbyIdle 0", "Wait"
	};

	[SerializeField]
	private string[] dormantStates = new string[8] { "spawn_idle", "Spawn_idle", "Spawn_Idle", "SpawnIdle", "spawn_loop", "Spawn_loop", "Spawn_Loop", "SpawnLoop" };

	[SerializeField]
	private string[] walkStates = new string[4] { "walk", "Walk", "Run", "Move" };

	[SerializeField]
	private string[] winStates = new string[2] { "win", "Win" };

	[SerializeField]
	private string[] attackStates = new string[16]
	{
		"attack01", "Attack01", "attack_01", "Attack_01", "Attack1", "attack1", "attack02", "Attack02", "attack_02", "Attack_02",
		"Attack2", "attack2", "basic_attack", "BasicAttack", "attack", "Attack"
	};

	[SerializeField]
	private string[] skill01States = new string[10] { "skill01", "Skill01", "skill_01", "Skill_01", "Skill1", "skill1", "Skill01 0", "skill01 0", "Skill_01 0", "skill_01 0" };

	[SerializeField]
	private string[] skill02States = new string[10] { "skill02", "Skill02", "skill_02", "Skill_02", "Skill2", "skill2", "Skill02 0", "skill02 0", "Skill_02 0", "skill_02 0" };

	[SerializeField]
	private string[] skill03StartStates = new string[10] { "skill03_start", "Skill03_Start", "Skill03Start", "skill_03_start", "Skill_03_Start", "Skill03_Start 0", "skill03_start 0", "Skill03Start 0", "Skill_03_Start 0", "skill_03_start 0" };

	[SerializeField]
	private string[] skill03LoopStates = new string[10] { "skill03_loop", "Skill03_Loop", "Skill03Loop", "skill_03_loop", "Skill_03_Loop", "Skill03_Loop 0", "skill03_loop 0", "Skill03Loop 0", "Skill_03_Loop 0", "skill_03_loop 0" };

	[SerializeField]
	private string[] skill03EndStates = new string[10] { "skill03_end", "Skill03_End", "Skill03End", "skill_03_end", "Skill_03_End", "Skill03_End 0", "skill03_end 0", "Skill03End 0", "Skill_03_End 0", "skill_03_end 0" };

	[SerializeField]
	private string[] spawnTriggers = new string[1] { "Spawn" };

	[SerializeField]
	private string[] attackTriggers = new string[1] { "Attack" };

	[SerializeField]
	private string[] skillTriggers = new string[1] { "Skill" };

	[SerializeField]
	private string[] winTriggers = new string[1] { "Win" };

	[SerializeField]
	private string[] attackIndexInts = new string[1] { "AttackIndex" };

	[SerializeField]
	private string[] skillIndexInts = new string[2] { "SkillIndex", "PlayIndex" };

	[SerializeField]
	private int defaultAttackIndex = 1;

	[SerializeField]
	private float actionBlendDuration = 0.08f;

	[SerializeField]
	private float spawnReturnDelay = 0.85f;

	[SerializeField]
	private float attackReturnDelay = 0.45f;

	[SerializeField]
	private float skillReturnDelay = 0.7f;

	[SerializeField]
	private float skill03LoopHoldDuration = 0.35f;

	[SerializeField]
	private float winHoldDuration = 2f;

	[SerializeField]
	private float attackImpactFallbackDelay = 0.18f;

	[SerializeField]
	private float attackEventFallbackDelay = 0.85f;

	[SerializeField]
	private float skillImpactFallbackDelay = 0.32f;

	[SerializeField]
	private float skillEventFallbackDelay = 0.9f;

	[SerializeField]
	private float skillSequenceImpactFallbackDelay = 2.2f;

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

	public float AttackImpactFallbackDelay => Mathf.Max(0.02f, (activeAttackImpactFallbackDelay > 0f) ? activeAttackImpactFallbackDelay : attackImpactFallbackDelay);

	public float SkillImpactFallbackDelay => Mathf.Max(0.02f, (activeSkillImpactFallbackDelay > 0f) ? activeSkillImpactFallbackDelay : skillImpactFallbackDelay);

	public event Action<AnimationImpactType> ImpactTriggered;

	private void Awake()
	{
		animatorLodController = ((Component)this).GetComponent<UnitAnimatorLodController>();
		if ((Object)(object)animator == (Object)null)
		{
			animator = ((Component)this).GetComponentInChildren<Animator>();
		}
		if ((Object)(object)animator != (Object)null)
		{
			animator.applyRootMotion = false;
			AnimationEventProxy component = ((Component)animator).GetComponent<AnimationEventProxy>();
			if ((Object)(object)component == (Object)null)
			{
				((Component)animator).gameObject.AddComponent<AnimationEventProxy>();
			}
		}
	}

	public bool PlaySpawn()
	{
		PrepareAnimatorForAction(spawnReturnDelay);
		if (TryPlayAction(spawnStates, spawnTriggers, null, 0, spawnReturnDelay))
		{
			return true;
		}
		PlayIdle();
		return false;
	}

	public void PlayIdle()
	{
		if (!movementLoopActive)
		{
			SetDesiredLoopState(idleStates);
		}
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
		PrepareAnimatorForAction(winHoldDuration);
		return TryPlayAction(winStates, winTriggers, null, 0, winHoldDuration);
	}

	public bool PlayAttack()
	{
		PrepareAnimatorForAction(attackReturnDelay);
		activeImpactType = AnimationImpactType.Attack;
		activeAttackImpactFallbackDelay = 0f;
		bool flag = TryPlayAction(attackStates, attackTriggers, attackIndexInts, defaultAttackIndex, attackReturnDelay);
		if (flag && lastPlayedActionHasImpactEvent)
		{
			activeAttackImpactFallbackDelay = Mathf.Max(attackEventFallbackDelay, lastPlayedImpactEventTime + 0.2f);
		}
		return flag;
	}

	public bool PlaySkill()
	{
		return PlaySkill(1);
	}

	public bool PlaySkill(int skillSlot)
	{
		return PlaySkill(skillSlot, -1f);
	}

	public bool PlaySkill(int skillSlot, float skillDuration)
	{
		PrepareAnimatorForAction((skillDuration > 0f) ? Mathf.Max(skillReturnDelay, skillDuration) : skillReturnDelay);
		activeImpactType = AnimationImpactType.Skill;
		activeSkillImpactFallbackDelay = 0f;
		lastPlayedActionHasImpactEvent = false;
		lastPlayedActionIsSkillSequence = false;
		lastPlayedImpactEventTime = -1f;
		bool flag = ((skillSlot <= 1) ? (TryPlaySkillSlot(skill01States, 1) || TryPlaySkillSlot(skill02States, 2) || TryPlaySkill03Sequence(skillDuration) || TryPlaySkillStartOnly()) : ((skillSlot != 2) ? (TryPlaySkill03Sequence(skillDuration) || TryPlaySkillStartOnly() || TryPlaySkillSlot(skill01States, 1) || TryPlaySkillSlot(skill02States, 2)) : (TryPlaySkillSlot(skill02States, 2) || TryPlaySkill03Sequence(skillDuration) || TryPlaySkillStartOnly() || TryPlaySkillSlot(skill01States, 1))));
		if (flag && lastPlayedActionHasImpactEvent)
		{
			activeSkillImpactFallbackDelay = (lastPlayedActionIsSkillSequence ? skillSequenceImpactFallbackDelay : Mathf.Max(skillEventFallbackDelay, lastPlayedImpactEventTime + 0.25f));
		}
		return flag;
	}

	public void NotifyAnimationImpact(AnimationImpactType impactType)
	{
		AnimationImpactType animationImpactType = ((impactType == AnimationImpactType.Auto) ? activeImpactType : impactType);
		if (animationImpactType == AnimationImpactType.Auto)
		{
			animationImpactType = AnimationImpactType.Attack;
		}
		this.ImpactTriggered?.Invoke(animationImpactType);
	}

	public void PlayMoving(bool isMoving)
	{
		movementLoopActive = isMoving;
		if (isMoving)
		{
			desiredLoopState = ResolveFirstPlayableState(walkStates);
			if (!IsLocked)
			{
				ForceLoopState(desiredLoopState, preserveNormalizedTime: true);
			}
		}
		else
		{
			desiredLoopState = ResolveFirstPlayableState(idleStates);
			if (!IsLocked)
			{
				ForceLoopState(desiredLoopState, preserveNormalizedTime: false);
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
		ForceLoopState(desiredLoopState, preserveNormalizedTime: false);
	}

	private bool TryPlayAny(string[] stateNames, bool allowRestartCurrent = false)
	{
		if ((Object)(object)animator == (Object)null || stateNames == null)
		{
			return false;
		}
		foreach (string text in stateNames)
		{
			if (string.IsNullOrWhiteSpace(text) || !HasState(text))
			{
				continue;
			}
			bool flag = IsCurrentlyInState(text);
			bool flag2 = allowRestartCurrent && (!flag || ShouldRestartCurrentState(text));
			if (currentState != text || !flag || flag2)
			{
				if (flag2)
				{
					animator.Play(text, 0, 0f);
				}
				else
				{
					animator.CrossFade(text, actionBlendDuration, 0);
				}
				currentState = text;
			}
			return true;
		}
		return false;
	}

	private bool TryPlayLoopingState(string[] stateNames)
	{
		if ((Object)(object)animator == (Object)null || stateNames == null)
		{
			return false;
		}
		foreach (string text in stateNames)
		{
			if (!string.IsNullOrWhiteSpace(text) && HasState(text))
			{
				if (IsTransitioningAwayFromState(text))
				{
					animator.Play(text, 0, GetCurrentLoopNormalizedTime());
					currentState = text;
				}
				else if (!IsCurrentlyInState(text))
				{
					animator.Play(text, 0, 0f);
					currentState = text;
				}
				return true;
			}
		}
		return false;
	}

	private void SetDesiredLoopState(string[] stateNames)
	{
		desiredLoopState = ResolveFirstPlayableState(stateNames);
		if (!string.IsNullOrWhiteSpace(desiredLoopState) && !IsLocked)
		{
			ApplyDesiredLoopState();
		}
	}

	private void ApplyDesiredLoopState()
	{
		if (!IsLocked && !string.IsNullOrWhiteSpace(desiredLoopState) && !((Object)(object)animator == (Object)null))
		{
			ForceLoopState(desiredLoopState, movementLoopActive);
		}
	}

	private string ResolveFirstPlayableState(string[] stateNames)
	{
		if (stateNames == null)
		{
			return null;
		}
		foreach (string stateName in stateNames)
		{
			string text = ResolveAnimatorStateName(stateName);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		if ((Object)(object)animator == (Object)null || (Object)(object)animator.runtimeAnimatorController == (Object)null)
		{
			return null;
		}
		AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
		if (animationClips == null)
		{
			return null;
		}
		for (int j = 0; j < stateNames.Length; j++)
		{
			string value = NormalizeAnimationName(stateNames[j]);
			if (string.IsNullOrWhiteSpace(value))
			{
				continue;
			}
			foreach (AnimationClip val in animationClips)
			{
				if (!((Object)(object)val == (Object)null) && !val.empty)
				{
					string text2 = NormalizeAnimationName(((Object)val).name);
					string text3 = ResolveAnimatorStateName(((Object)val).name);
					if (text2.Contains(value) && !string.IsNullOrWhiteSpace(text3))
					{
						return text3;
					}
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
		string text = ResolveFirstDirectSkillSlotState(stateNames);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		TrySetFirstInt(skillIndexInts, skillIndex);
		return TryPlayResolvedActionState(text, stateNames, skillReturnDelay);
	}

	private string ResolveFirstDirectSkillSlotState(string[] stateNames)
	{
		string text = ResolveFirstEditorAnimatorState(stateNames, requireMotion: true);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (!string.IsNullOrWhiteSpace(ResolveFirstEditorAnimatorState(stateNames, requireMotion: false)))
		{
			return null;
		}
		if (!HasDirectSkillClipMatching(stateNames))
		{
			return null;
		}
		return ResolveFirstPlayableState(stateNames);
	}

	private string ResolveFirstSkillSequenceState(string[] stateNames, string sequencePart)
	{
		string text = ResolveFirstEditorAnimatorState(stateNames, requireMotion: true);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (!string.IsNullOrWhiteSpace(ResolveFirstEditorAnimatorState(stateNames, requireMotion: false)))
		{
			return null;
		}
		if (!HasSkillSequenceClipPart(sequencePart))
		{
			return null;
		}
		return ResolveFirstPlayableState(stateNames);
	}

	private bool TryPlaySkill03Sequence(float requestedLoopDuration)
	{
		if (IsLocked)
		{
			return false;
		}
		string text = ResolveFirstSkillSequenceState(skill03StartStates, "start");
		string text2 = ResolveFirstSkillSequenceState(skill03LoopStates, "loop");
		string text3 = ResolveFirstSkillSequenceState(skill03EndStates, "end");
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3))
		{
			return false;
		}
		lastPlayedActionIsSkillSequence = true;
		lastPlayedActionHasImpactEvent = true;
		lastPlayedImpactEventTime = 0f;
		CancelScheduledReturn();
		CancelActionRoutine();
		actionRoutine = ((MonoBehaviour)this).StartCoroutine(PlaySkill03Sequence(text, text2, text3, ResolveSkill03LoopHoldDuration(requestedLoopDuration, skill03LoopHoldDuration)));
		return true;
	}

	private bool TryPlaySkillStartOnly()
	{
		if (IsLocked)
		{
			return false;
		}
		string text = ResolveFirstSkillSequenceState(skill03StartStates, "start");
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		TrySetFirstInt(skillIndexInts, 3);
		return TryPlayResolvedActionState(text, skill03StartStates, skillReturnDelay);
	}

	private bool HasDirectSkillClipMatching(string[] stateNames)
	{
		if ((Object)(object)animator == (Object)null || (Object)(object)animator.runtimeAnimatorController == (Object)null || stateNames == null)
		{
			return false;
		}
		AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
		if (animationClips == null)
		{
			return false;
		}
		foreach (AnimationClip val in animationClips)
		{
			if ((Object)(object)val == (Object)null || val.empty || IsSkillSequenceClipName(((Object)val).name))
			{
				continue;
			}
			string text = NormalizeAnimationName(((Object)val).name);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			for (int j = 0; j < stateNames.Length; j++)
			{
				string text2 = NormalizeAnimationName(stateNames[j]);
				if (!string.IsNullOrWhiteSpace(text2) && (text.Contains(text2) || text2.Contains(text)))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool HasSkillSequenceClipPart(string sequencePart)
	{
		if ((Object)(object)animator == (Object)null || (Object)(object)animator.runtimeAnimatorController == (Object)null)
		{
			return false;
		}
		AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
		if (animationClips == null)
		{
			return false;
		}
		foreach (AnimationClip val in animationClips)
		{
			if (!((Object)(object)val == (Object)null) && !val.empty)
			{
				string normalizedClipName = NormalizeAnimationName(((Object)val).name);
				if (LooksLikeSkillClip(normalizedClipName) && ClipNameHasSequencePart(((Object)val).name, sequencePart))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsSkillSequenceClipName(string clipName)
	{
		return ClipNameHasSequencePart(clipName, "start") || ClipNameHasSequencePart(clipName, "loop") || ClipNameHasSequencePart(clipName, "end");
	}

	private static bool ClipNameHasSequencePart(string clipName, string sequencePart)
	{
		if (string.IsNullOrWhiteSpace(clipName) || string.IsNullOrWhiteSpace(sequencePart))
		{
			return false;
		}
		string text = clipName.ToLowerInvariant();
		string text2 = sequencePart.ToLowerInvariant();
		if (text.Contains("_" + text2) || text.Contains(text2 + "_") || text.Contains("-" + text2) || text.Contains(text2 + "-") || text.Contains(" " + text2) || text.Contains(text2 + " "))
		{
			return true;
		}
		string text3 = NormalizeAnimationName(clipName);
		string value = NormalizeAnimationName(sequencePart);
		return !string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(value) && text3.EndsWith(value);
	}

	private static bool LooksLikeSkillClip(string normalizedClipName)
	{
		return !string.IsNullOrWhiteSpace(normalizedClipName) && (normalizedClipName.Contains("skill") || normalizedClipName.Contains("cast"));
	}

	private bool TryPlayResolvedActionState(string actionState, string[] stateNames, float fallbackDuration)
	{
		lastPlayedActionHasImpactEvent = false;
		lastPlayedActionIsSkillSequence = false;
		if ((Object)(object)animator == (Object)null || IsLocked || string.IsNullOrWhiteSpace(actionState))
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

	public static float ResolveSkill03LoopHoldDuration(float requestedLoopDuration, float fallbackDuration)
	{
		float num = Mathf.Max(0.05f, fallbackDuration);
		return (requestedLoopDuration > 0f && !float.IsNaN(requestedLoopDuration) && !float.IsInfinity(requestedLoopDuration)) ? Mathf.Max(0.05f, requestedLoopDuration) : num;
	}

	private IEnumerator PlaySkill03Sequence(string startState, string loopState, string endState, float loopHoldDuration)
	{
		actionInProgress = true;
		lockUntilTime = 0f;
		PlayActionState(startState);
		yield return WaitForStateToComplete(startState, skillReturnDelay * 0.45f);
		PlayActionState(loopState);
		float loopEndTime = Time.time + Mathf.Max(0.05f, loopHoldDuration);
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
		if (!((Object)(object)animator == (Object)null) && !string.IsNullOrWhiteSpace(stateName))
		{
			animator.CrossFade(stateName, actionBlendDuration, 0);
			currentState = stateName;
		}
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
			yield return (object)new WaitForSeconds(fallbackDuration);
			yield break;
		}
		while (IsCurrentlyInState(stateName))
		{
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			if (((AnimatorStateInfo)(ref stateInfo)).normalizedTime >= 0.98f)
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
		return value.ToLowerInvariant().Replace("_", string.Empty).Replace("-", string.Empty)
			.Replace(" ", string.Empty);
	}

	private string ResolveAnimatorStateName(string stateName)
	{
		if ((Object)(object)animator == (Object)null || (Object)(object)animator.runtimeAnimatorController == (Object)null || string.IsNullOrWhiteSpace(stateName))
		{
			return null;
		}
		string text = stateName.Trim();
		string[] array = new string[5]
		{
			text,
			"Base Layer." + text,
			"Base Layer.Attack StateMachine." + text,
			"Base Layer.Skill StateMachine." + text,
			"Base Layer.Lobby StateMachine." + text
		};
		foreach (string text2 in array)
		{
			if (animator.HasState(0, Animator.StringToHash(text2)))
			{
				return text2;
			}
		}
		return null;
	}

	private string ResolveFirstEditorAnimatorState(string[] stateNames, bool requireMotion)
	{
		if (stateNames == null)
		{
			return null;
		}
		AnimatorController val = ResolveEditorAnimatorController();
		if ((Object)(object)val == (Object)null || val.layers == null)
		{
			return null;
		}
		for (int i = 0; i < stateNames.Length; i++)
		{
			string text = NormalizeAnimationName(stateNames[i]);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			for (int j = 0; j < val.layers.Length; j++)
			{
				AnimatorControllerLayer val2 = val.layers[j];
				if (val2 != null && !((Object)(object)val2.stateMachine == (Object)null))
				{
					string text2 = ResolveFirstEditorAnimatorState(val2.stateMachine, val2.name, text, requireMotion);
					if (!string.IsNullOrWhiteSpace(text2))
					{
						return text2;
					}
				}
			}
		}
		return null;
	}

	private AnimatorController ResolveEditorAnimatorController()
	{
		if ((Object)(object)animator == (Object)null)
		{
			return null;
		}
		RuntimeAnimatorController runtimeAnimatorController = animator.runtimeAnimatorController;
		while (true)
		{
			AnimatorOverrideController val = (AnimatorOverrideController)(object)((runtimeAnimatorController is AnimatorOverrideController) ? runtimeAnimatorController : null);
			if (val == null)
			{
				break;
			}
			runtimeAnimatorController = val.runtimeAnimatorController;
		}
		return (AnimatorController)(object)((runtimeAnimatorController is AnimatorController) ? runtimeAnimatorController : null);
	}

	private static string ResolveFirstEditorAnimatorState(AnimatorStateMachine stateMachine, string statePath, string normalizedStateName, bool requireMotion)
	{
		if ((Object)(object)stateMachine == (Object)null)
		{
			return null;
		}
		ChildAnimatorState[] states = stateMachine.states;
		if (states != null)
		{
			for (int i = 0; i < states.Length; i++)
			{
				AnimatorState state = ((ChildAnimatorState)(ref states[i])).state;
				if (!((Object)(object)state == (Object)null) && !(NormalizeAnimationName(((Object)state).name) != normalizedStateName) && (!requireMotion || !((Object)(object)state.motion == (Object)null)))
				{
					return string.IsNullOrWhiteSpace(statePath) ? ((Object)state).name : (statePath + "." + ((Object)state).name);
				}
			}
		}
		ChildAnimatorStateMachine[] stateMachines = stateMachine.stateMachines;
		if (stateMachines == null)
		{
			return null;
		}
		for (int j = 0; j < stateMachines.Length; j++)
		{
			AnimatorStateMachine stateMachine2 = ((ChildAnimatorStateMachine)(ref stateMachines[j])).stateMachine;
			if (!((Object)(object)stateMachine2 == (Object)null))
			{
				string statePath2 = (string.IsNullOrWhiteSpace(statePath) ? ((Object)stateMachine2).name : (statePath + "." + ((Object)stateMachine2).name));
				string text = ResolveFirstEditorAnimatorState(stateMachine2, statePath2, normalizedStateName, requireMotion);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		return null;
	}

	private bool HasState(string stateName)
	{
		return !string.IsNullOrWhiteSpace(ResolveAnimatorStateName(stateName));
	}

	private bool ShouldRestartCurrentState(string stateName)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)animator == (Object)null)
		{
			return false;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		if (!((AnimatorStateInfo)(ref currentAnimatorStateInfo)).IsName(stateName))
		{
			return false;
		}
		return ((AnimatorStateInfo)(ref currentAnimatorStateInfo)).normalizedTime >= 0.98f;
	}

	private bool IsCurrentlyInState(string stateName)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)animator == (Object)null)
		{
			return false;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		return ((AnimatorStateInfo)(ref currentAnimatorStateInfo)).IsName(stateName);
	}

	private bool IsTransitioningAwayFromState(string stateName)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)animator == (Object)null || !animator.IsInTransition(0))
		{
			return false;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		AnimatorStateInfo nextAnimatorStateInfo = animator.GetNextAnimatorStateInfo(0);
		return ((AnimatorStateInfo)(ref currentAnimatorStateInfo)).IsName(stateName) && !((AnimatorStateInfo)(ref nextAnimatorStateInfo)).IsName(stateName);
	}

	private float GetCurrentLoopNormalizedTime()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)animator == (Object)null)
		{
			return 0f;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		float normalizedTime = ((AnimatorStateInfo)(ref currentAnimatorStateInfo)).normalizedTime;
		return normalizedTime - Mathf.Floor(normalizedTime);
	}

	private void ForceLoopState(string stateName, bool preserveNormalizedTime)
	{
		if (!((Object)(object)animator == (Object)null) && !string.IsNullOrWhiteSpace(stateName) && (IsTransitioningAwayFromState(stateName) || !IsCurrentlyInState(stateName)))
		{
			float num = (preserveNormalizedTime ? GetCurrentLoopNormalizedTime() : 0f);
			animator.Play(stateName, 0, num);
			currentState = stateName;
		}
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
		string text = ResolveFirstPlayableState(stateNames);
		if (!string.IsNullOrWhiteSpace(text))
		{
			lastPlayedActionHasImpactEvent = HasImpactEventForAction(text, activeImpactType, stateNames);
			animator.CrossFade(text, actionBlendDuration, 0);
			currentState = text;
			StartActionObservation(text, fallbackDuration);
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
		if ((Object)(object)animator == (Object)null || (Object)(object)animator.runtimeAnimatorController == (Object)null)
		{
			return false;
		}
		AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
		if (animationClips == null)
		{
			return false;
		}
		foreach (AnimationClip val in animationClips)
		{
			if ((Object)(object)val == (Object)null || val.events == null || val.events.Length == 0 || !ClipMatchesAction(val, actionState, stateNames))
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < val.events.Length; j++)
			{
				if (IsImpactEventFunction(val.events[j].functionName, impactType))
				{
					lastPlayedImpactEventTime = Mathf.Max(lastPlayedImpactEventTime, val.events[j].time);
					flag = true;
				}
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	private static bool ClipMatchesAction(AnimationClip clip, string actionState, string[] stateNames)
	{
		string text = NormalizeAnimationName(((Object)(object)clip != (Object)null) ? ((Object)clip).name : null);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = NormalizeAnimationName(actionState);
		if (!string.IsNullOrWhiteSpace(text2) && (text2.Contains(text) || text.Contains(text2)))
		{
			return true;
		}
		if (stateNames == null)
		{
			return false;
		}
		for (int i = 0; i < stateNames.Length; i++)
		{
			string text3 = NormalizeAnimationName(stateNames[i]);
			if (!string.IsNullOrWhiteSpace(text3) && (text.Contains(text3) || text3.Contains(text)))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsImpactEventFunction(string functionName, AnimationImpactType impactType)
	{
		string text = NormalizeAnimationName(functionName);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text == "hit" || text == "impact" || text == "damage")
		{
			return true;
		}
		switch (impactType)
		{
		case AnimationImpactType.Attack:
		{
			int result2;
			switch (text)
			{
			default:
				result2 = ((text == "shoot") ? 1 : 0);
				break;
			case "attackhit":
			case "attackimpact":
			case "fireprojectile":
				result2 = 1;
				break;
			}
			return (byte)result2 != 0;
		}
		case AnimationImpactType.AttackHit:
			return text == "attackhit" || text == "attackimpact";
		case AnimationImpactType.FireProjectile:
			return text == "fireprojectile" || text == "shoot";
		case AnimationImpactType.Skill:
		{
			int result;
			switch (text)
			{
			default:
				result = ((text == "spawnarea") ? 1 : 0);
				break;
			case "skillhit":
			case "skillimpact":
			case "skillfire":
			case "skillapply":
			case "castimpact":
				result = 1;
				break;
			}
			return (byte)result != 0;
		}
		default:
			return false;
		}
	}

	private bool TrySetAnyTrigger(string[] triggerNames)
	{
		if ((Object)(object)animator == (Object)null || (Object)(object)animator.runtimeAnimatorController == (Object)null || triggerNames == null)
		{
			return false;
		}
		for (int i = 0; i < triggerNames.Length; i++)
		{
			if (HasParameter(triggerNames[i], (AnimatorControllerParameterType)9))
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
		if ((Object)(object)animator == (Object)null || (Object)(object)animator.runtimeAnimatorController == (Object)null || parameterNames == null)
		{
			return;
		}
		for (int i = 0; i < parameterNames.Length; i++)
		{
			if (HasParameter(parameterNames[i], (AnimatorControllerParameterType)3))
			{
				animator.SetInteger(parameterNames[i], value);
				break;
			}
		}
	}

	private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)animator == (Object)null || animator.parameters == null)
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
		returnRoutine = ((MonoBehaviour)this).StartCoroutine(ReturnToIdleAfter(delay));
	}

	private void ScheduleReturnToDesiredLoop(float delay)
	{
		CancelScheduledReturn();
		returnRoutine = ((MonoBehaviour)this).StartCoroutine(ReturnToDesiredLoopAfter(delay));
	}

	private void CancelScheduledReturn()
	{
		if (returnRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(returnRoutine);
			returnRoutine = null;
		}
	}

	private void CancelActionRoutine()
	{
		if (actionRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(actionRoutine);
			actionRoutine = null;
		}
	}

	private IEnumerator ReturnToIdleAfter(float delay)
	{
		yield return (object)new WaitForSeconds(delay);
		returnRoutine = null;
		if (Time.time >= lockUntilTime)
		{
			PlayIdle();
		}
	}

	private IEnumerator ReturnToDesiredLoopAfter(float delay)
	{
		yield return (object)new WaitForSeconds(delay);
		returnRoutine = null;
		lockUntilTime = 0f;
		ApplyDesiredLoopState();
	}

	private void StartActionObservation(string stateName, float fallbackDuration)
	{
		CancelActionRoutine();
		actionRoutine = ((MonoBehaviour)this).StartCoroutine(ObserveActionState(stateName, fallbackDuration));
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
			yield return (object)new WaitForSeconds(fallbackDuration);
		}
		else
		{
			while (IsCurrentlyInState(stateName))
			{
				AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
				if (((AnimatorStateInfo)(ref stateInfo)).normalizedTime >= 0.98f)
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

	private void PrepareAnimatorForAction(float minimumFullRateDuration)
	{
		if ((Object)(object)animatorLodController == (Object)null)
		{
			animatorLodController = ((Component)this).GetComponent<UnitAnimatorLodController>();
		}
		animatorLodController?.PrepareForAction(minimumFullRateDuration);
	}
}
