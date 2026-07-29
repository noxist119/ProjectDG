using System;
using UnityEngine;

namespace DefenseGame
{
	[Serializable]
	public class MonsterPresentationOverride
	{
		[Header("Matching")]
		public string monsterId;

		public MonsterThreatLevel threatLevel = MonsterThreatLevel.Regular;

		[Header("Roster Entry")]
		public bool useAsRosterEntry;

		public string displayName;

		public CharacterGrade grade = CharacterGrade.Normal;

		public MonsterRole role = MonsterRole.Grunt;

		public int minRound = 1;

		public int rewardGoldOverride;

		[Header("Grade Variants")]
		public bool createGradeVariants = true;

		public CharacterGrade maxVariantGrade = CharacterGrade.Mythic;

		[Tooltip("0이면 일반몹 3라운드, 중간보스 5라운드, 보스 10라운드 간격으로 자동 적용됩니다.")]
		public int variantRoundStep;

		[Range(0f, 0.35f)]
		public float variantStatBonusPerTier = 0.08f;

		[Header("Presentation")]
		public GameObject prefab;

		public bool overrideColor;

		public Color accentColor = Color.white;
	}
}
