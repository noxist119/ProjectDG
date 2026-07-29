using System;
using UnityEngine;

namespace DefenseGame;

[Serializable]
public class SkillDefinition
{
	public string id;

	public string displayName;

	[TextArea]
	public string description;

	public SkillEffectType effectType;

	public SkillCategory category = SkillCategory.Auto;

	public SkillDeliveryType deliveryType = SkillDeliveryType.Auto;

	public bool useCustomCastRange;

	public float castRange = 6f;

	[Tooltip("Monster skills only. Allows an explicitly designed boss mechanic to ignore distance when selecting defenders.")]
	public bool isGlobalTargeting;

	public float power = 1f;

	public float secondaryPower = 0.35f;

	public float duration = 3f;

	public float radius = 2.5f;

	public float manaThreshold = 100f;

	public float cooldown = 4f;

	public int hitCount = 1;

	[Header("Outgame Growth")]
	public SkillGrowthTarget growthTargets = SkillGrowthTarget.None;

	public float growthStepRatio = 0.05f;

	[Header("Skill Resources")]
	public GameObject projectilePrefab;

	public GameObject muzzleEffectPrefab;

	public GameObject hitEffectPrefab;

	public GameObject areaEffectPrefab;

	public SkillCategory ResolvedCategory => SkillDefinitionUtility.ResolveCategory(this);

	public SkillDeliveryType ResolvedDeliveryType => SkillDefinitionUtility.ResolveDeliveryType(this);
}
