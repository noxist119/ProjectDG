using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

[CreateAssetMenu(fileName = "MonsterCombatTuningConfig", menuName = "Defense Game/Monster Combat Tuning")]
public class MonsterCombatTuningConfig : ScriptableObject
{
	[Header("Default Boss Skill FX")]
	public GameObject defaultBossSkillCastEffectPrefab;

	public GameObject defaultBossSkillHitEffectPrefab;

	public GameObject defaultBossSkillAreaEffectPrefab;

	public GameObject defaultBossSkillBuffEffectPrefab;

	public List<MonsterCombatTuningEntry> entries = new List<MonsterCombatTuningEntry>();

	public void ApplyToMonster(MonsterDefinition definition)
	{
		if (definition == null)
		{
			return;
		}
		ApplyDefaultSkillFx(definition);
		string tuningId = (string.IsNullOrWhiteSpace(definition.rosterSourceId) ? definition.id : definition.rosterSourceId);
		MonsterCombatTuningEntry monsterCombatTuningEntry = entries.Find((MonsterCombatTuningEntry candidate) => candidate != null && candidate.monsterId == tuningId);
		if (monsterCombatTuningEntry == null && TryGetOrderedEntry(tuningId, out var entry))
		{
			monsterCombatTuningEntry = entry;
		}
		if (monsterCombatTuningEntry != null)
		{
			if (definition.attackBehavior == null)
			{
				definition.attackBehavior = new AttackBehavior();
			}
			if (monsterCombatTuningEntry.overrideBasicAttackType)
			{
				definition.attackBehavior.basicAttackType = monsterCombatTuningEntry.basicAttackType;
			}
			if (monsterCombatTuningEntry.overrideAttackRange)
			{
				definition.attackBehavior.useCustomAttackRange = true;
				definition.attackBehavior.customAttackRange = Mathf.Max(0.5f, monsterCombatTuningEntry.attackRange);
			}
			if (monsterCombatTuningEntry.overrideProjectileSpeed)
			{
				definition.stats.projectileSpeed = Mathf.Max(2f, monsterCombatTuningEntry.projectileSpeed);
			}
			if (monsterCombatTuningEntry.overrideMoveSpeed)
			{
				definition.stats.moveSpeed = monsterCombatTuningEntry.moveSpeed;
			}
			if (monsterCombatTuningEntry.overrideVisualScale)
			{
				definition.visualScale = Mathf.Max(0.1f, monsterCombatTuningEntry.visualScale);
			}
			if (monsterCombatTuningEntry.overrideSplash)
			{
				definition.attackBehavior.splashRadius = monsterCombatTuningEntry.splashRadius;
				definition.attackBehavior.splashDamageRatio = monsterCombatTuningEntry.splashDamageRatio;
			}
			if (monsterCombatTuningEntry.overridePierce)
			{
				definition.attackBehavior.additionalPierceCount = monsterCombatTuningEntry.additionalPierceCount;
			}
			ApplyBasicAttackResources(definition, monsterCombatTuningEntry);
			ApplySkillResourceOverrides(definition, monsterCombatTuningEntry);
		}
	}

	private void ApplyDefaultSkillFx(MonsterDefinition definition)
	{
		if (definition == null || !definition.IsBossLike || definition.skills == null)
		{
			return;
		}
		for (int i = 0; i < definition.skills.Count; i++)
		{
			SkillDefinition skillDefinition = definition.skills[i];
			if (skillDefinition == null)
			{
				continue;
			}
			if ((Object)(object)skillDefinition.muzzleEffectPrefab == (Object)null)
			{
				skillDefinition.muzzleEffectPrefab = defaultBossSkillCastEffectPrefab;
			}
			bool flag = skillDefinition.effectType == SkillEffectType.BossFortify || skillDefinition.effectType == SkillEffectType.HealSelf || skillDefinition.effectType == SkillEffectType.MonsterRally || skillDefinition.effectType == SkillEffectType.MoveSpeedBoost || skillDefinition.effectType == SkillEffectType.AttackSpeedBoost || skillDefinition.effectType == SkillEffectType.CriticalBoost || skillDefinition.effectType == SkillEffectType.ManaSurge || skillDefinition.effectType == SkillEffectType.DamageReflect;
			bool flag2 = skillDefinition.effectType == SkillEffectType.AreaDamage || skillDefinition.effectType == SkillEffectType.SummonRush || skillDefinition.effectType == SkillEffectType.MonsterRally;
			if (flag)
			{
				if ((Object)(object)skillDefinition.areaEffectPrefab == (Object)null)
				{
					skillDefinition.areaEffectPrefab = (((Object)(object)defaultBossSkillBuffEffectPrefab != (Object)null) ? defaultBossSkillBuffEffectPrefab : defaultBossSkillAreaEffectPrefab);
				}
				if ((Object)(object)skillDefinition.hitEffectPrefab == (Object)null)
				{
					skillDefinition.hitEffectPrefab = skillDefinition.areaEffectPrefab;
				}
			}
			else if (flag2)
			{
				if ((Object)(object)skillDefinition.areaEffectPrefab == (Object)null)
				{
					skillDefinition.areaEffectPrefab = defaultBossSkillAreaEffectPrefab;
				}
				if ((Object)(object)skillDefinition.hitEffectPrefab == (Object)null)
				{
					skillDefinition.hitEffectPrefab = (((Object)(object)defaultBossSkillHitEffectPrefab != (Object)null) ? defaultBossSkillHitEffectPrefab : skillDefinition.areaEffectPrefab);
				}
			}
			else if ((Object)(object)skillDefinition.hitEffectPrefab == (Object)null)
			{
				skillDefinition.hitEffectPrefab = defaultBossSkillHitEffectPrefab;
			}
		}
	}

	private void ApplyBasicAttackResources(MonsterDefinition definition, MonsterCombatTuningEntry entry)
	{
		if (definition != null && entry != null)
		{
			if (definition.attackBehavior == null)
			{
				definition.attackBehavior = new AttackBehavior();
			}
			if ((Object)(object)entry.basicAttackProjectilePrefab != (Object)null)
			{
				definition.attackBehavior.projectilePrefabOverride = entry.basicAttackProjectilePrefab;
			}
			if ((Object)(object)entry.basicAttackMuzzleEffectPrefab != (Object)null)
			{
				definition.attackBehavior.muzzleEffectPrefab = entry.basicAttackMuzzleEffectPrefab;
			}
			if ((Object)(object)entry.basicAttackHitEffectPrefab != (Object)null)
			{
				definition.attackBehavior.hitEffectPrefab = entry.basicAttackHitEffectPrefab;
			}
		}
	}

	private void ApplySkillResourceOverrides(MonsterDefinition definition, MonsterCombatTuningEntry entry)
	{
		if (definition == null || definition.skills == null || entry == null)
		{
			return;
		}
		ApplySkillResourceOverride(definition.skills, 0, entry.skill01ProjectilePrefab, entry.skill01MuzzleEffectPrefab, entry.skill01HitEffectPrefab, entry.skill01AreaEffectPrefab);
		ApplySkillResourceOverride(definition.skills, 1, entry.skill02ProjectilePrefab, entry.skill02MuzzleEffectPrefab, entry.skill02HitEffectPrefab, entry.skill02AreaEffectPrefab);
		ApplySkillResourceOverride(definition.skills, 2, entry.skill03ProjectilePrefab, entry.skill03MuzzleEffectPrefab, entry.skill03HitEffectPrefab, entry.skill03AreaEffectPrefab);
		if (definition.skills.Count <= 1 || definition.skills[0] == null)
		{
			return;
		}
		SkillDefinition skillDefinition = definition.skills[0];
		for (int i = 1; i < definition.skills.Count; i++)
		{
			SkillDefinition skillDefinition2 = definition.skills[i];
			if (skillDefinition2 != null)
			{
				if ((Object)(object)skillDefinition2.projectilePrefab == (Object)null)
				{
					skillDefinition2.projectilePrefab = skillDefinition.projectilePrefab;
				}
				if ((Object)(object)skillDefinition2.muzzleEffectPrefab == (Object)null)
				{
					skillDefinition2.muzzleEffectPrefab = skillDefinition.muzzleEffectPrefab;
				}
				if ((Object)(object)skillDefinition2.hitEffectPrefab == (Object)null)
				{
					skillDefinition2.hitEffectPrefab = skillDefinition.hitEffectPrefab;
				}
				if ((Object)(object)skillDefinition2.areaEffectPrefab == (Object)null)
				{
					skillDefinition2.areaEffectPrefab = skillDefinition.areaEffectPrefab;
				}
			}
		}
	}

	private void ApplySkillResourceOverride(List<SkillDefinition> skills, int index, GameObject projectilePrefab, GameObject muzzleEffectPrefab, GameObject hitEffectPrefab, GameObject areaEffectPrefab)
	{
		if (index < 0 || index >= skills.Count)
		{
			return;
		}
		SkillDefinition skillDefinition = skills[index];
		if (skillDefinition != null)
		{
			if ((Object)(object)projectilePrefab != (Object)null)
			{
				skillDefinition.projectilePrefab = projectilePrefab;
			}
			if ((Object)(object)muzzleEffectPrefab != (Object)null)
			{
				skillDefinition.muzzleEffectPrefab = muzzleEffectPrefab;
			}
			if ((Object)(object)hitEffectPrefab != (Object)null)
			{
				skillDefinition.hitEffectPrefab = hitEffectPrefab;
			}
			if ((Object)(object)areaEffectPrefab != (Object)null)
			{
				skillDefinition.areaEffectPrefab = areaEffectPrefab;
			}
		}
	}

	private bool TryGetOrderedEntry(string definitionId, out MonsterCombatTuningEntry entry)
	{
		entry = null;
		if (!TryParseIndex(definitionId, out var index))
		{
			return false;
		}
		List<MonsterCombatTuningEntry> list = entries.FindAll((MonsterCombatTuningEntry candidate) => candidate != null);
		if (index < 0 || index >= list.Count)
		{
			return false;
		}
		entry = list[index];
		return true;
	}

	private bool TryParseIndex(string definitionId, out int index)
	{
		index = -1;
		if (string.IsNullOrWhiteSpace(definitionId))
		{
			return false;
		}
		string[] array = definitionId.Split('_');
		if (array.Length == 0)
		{
			return false;
		}
		if (!int.TryParse(array[^1], out var result))
		{
			return false;
		}
		index = result - 1;
		return index >= 0;
	}
}
