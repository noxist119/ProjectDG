using System;
using UnityEngine;

namespace DefenseGame
{
	[Serializable]
	public class MonsterCombatTuningEntry
	{
		public string monsterId;

		[Header("Basic Attack Setup")]
		public bool overrideBasicAttackType;

		public BasicAttackType basicAttackType = BasicAttackType.Melee;

		public bool overrideAttackRange;

		public float attackRange = 2f;

		public bool overrideProjectileSpeed;

		public float projectileSpeed = 8f;

		public bool overrideMoveSpeed;

		public float moveSpeed = 1.5f;

		public bool overrideVisualScale;

		public float visualScale = 1f;

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

		[Header("Skill 01 Resources")]
		public GameObject skill01ProjectilePrefab;

		public GameObject skill01MuzzleEffectPrefab;

		public GameObject skill01HitEffectPrefab;

		public GameObject skill01AreaEffectPrefab;

		[Header("Skill 02 Resources")]
		public GameObject skill02ProjectilePrefab;

		public GameObject skill02MuzzleEffectPrefab;

		public GameObject skill02HitEffectPrefab;

		public GameObject skill02AreaEffectPrefab;

		[Header("Skill 03 Resources")]
		public GameObject skill03ProjectilePrefab;

		public GameObject skill03MuzzleEffectPrefab;

		public GameObject skill03HitEffectPrefab;

		public GameObject skill03AreaEffectPrefab;
	}
}
