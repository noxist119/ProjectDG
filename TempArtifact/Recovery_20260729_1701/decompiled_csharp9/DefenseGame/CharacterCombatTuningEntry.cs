using System;
using UnityEngine;

namespace DefenseGame
{
	[Serializable]
	public class CharacterCombatTuningEntry
	{
		public string characterId;

		[Header("Role")]
		public bool overrideRole;

		public CharacterRole role = CharacterRole.Ranger;

		[Header("Basic Attack")]
		public bool overrideBasicAttackType;

		public BasicAttackType basicAttackType = BasicAttackType.Ranged;

		[Header("Basic Attack Range")]
		public bool overrideBasicAttackRange;

		public float basicAttackRange = 6f;

		public bool overrideSkillCastRange;

		public float skillCastRange = 6f;

		[Header("Basic Attack Extras")]
		public bool overrideSplash;

		public float splashRadius;

		[Range(0f, 1f)]
		public float splashDamageRatio;

		public bool overridePierce;

		public int additionalPierceCount;

		[Header("Basic Attack Resources")]
		public GameObject basicAttackProjectilePrefab;

		public GameObject basicAttackMuzzleEffectPrefab;

		public GameObject basicAttackHitEffectPrefab;

		[Header("Skill Slots")]
		public bool overrideSkill01;

		public SkillDefinition skill01 = CharacterCombatTuningConfig.CreateDefaultSkill(null, 0);

		public bool overrideSkill02;

		public SkillDefinition skill02 = CharacterCombatTuningConfig.CreateDefaultSkill(null, 1);

		public bool overrideSkill03;

		public SkillDefinition skill03 = CharacterCombatTuningConfig.CreateDefaultSkill(null, 2);
	}
}
