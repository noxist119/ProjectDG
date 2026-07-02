using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
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

            string tuningId = string.IsNullOrWhiteSpace(definition.rosterSourceId)
                ? definition.id
                : definition.rosterSourceId;
            MonsterCombatTuningEntry entry = entries.Find(candidate => candidate != null && candidate.monsterId == tuningId);
            if (entry == null && TryGetOrderedEntry(tuningId, out MonsterCombatTuningEntry orderedEntry))
            {
                entry = orderedEntry;
            }

            if (entry == null)
            {
                return;
            }

            if (definition.attackBehavior == null)
            {
                definition.attackBehavior = new AttackBehavior();
            }

            if (entry.overrideBasicAttackType)
            {
                definition.attackBehavior.basicAttackType = entry.basicAttackType;
            }

            if (entry.overrideAttackRange)
            {
                definition.attackBehavior.useCustomAttackRange = true;
                definition.attackBehavior.customAttackRange = Mathf.Max(0.5f, entry.attackRange);
            }

            if (entry.overrideProjectileSpeed)
            {
                definition.stats.projectileSpeed = Mathf.Max(2f, entry.projectileSpeed);
            }

            if (entry.overrideMoveSpeed)
            {
                definition.stats.moveSpeed = entry.moveSpeed;
            }

            if (entry.overrideVisualScale)
            {
                definition.visualScale = Mathf.Max(0.1f, entry.visualScale);
            }

            if (entry.overrideSplash)
            {
                definition.attackBehavior.splashRadius = entry.splashRadius;
                definition.attackBehavior.splashDamageRatio = entry.splashDamageRatio;
            }

            if (entry.overridePierce)
            {
                definition.attackBehavior.additionalPierceCount = entry.additionalPierceCount;
            }

            ApplyBasicAttackResources(definition, entry);
            ApplySkillResourceOverrides(definition, entry);
        }

        private void ApplyDefaultSkillFx(MonsterDefinition definition)
        {
            if (definition == null || !definition.IsBossLike || definition.skills == null)
            {
                return;
            }

            for (int i = 0; i < definition.skills.Count; i++)
            {
                SkillDefinition skill = definition.skills[i];
                if (skill == null)
                {
                    continue;
                }

                if (skill.muzzleEffectPrefab == null)
                {
                    skill.muzzleEffectPrefab = defaultBossSkillCastEffectPrefab;
                }

                bool supportSkill =
                    skill.effectType == SkillEffectType.BossFortify ||
                    skill.effectType == SkillEffectType.HealSelf ||
                    skill.effectType == SkillEffectType.MonsterRally ||
                    skill.effectType == SkillEffectType.MoveSpeedBoost ||
                    skill.effectType == SkillEffectType.AttackSpeedBoost ||
                    skill.effectType == SkillEffectType.CriticalBoost ||
                    skill.effectType == SkillEffectType.ManaSurge;

                bool areaSkill =
                    skill.effectType == SkillEffectType.AreaDamage ||
                    skill.effectType == SkillEffectType.SummonRush ||
                    skill.effectType == SkillEffectType.MonsterRally;

                if (supportSkill)
                {
                    if (skill.areaEffectPrefab == null)
                    {
                        skill.areaEffectPrefab = defaultBossSkillBuffEffectPrefab != null ? defaultBossSkillBuffEffectPrefab : defaultBossSkillAreaEffectPrefab;
                    }

                    if (skill.hitEffectPrefab == null)
                    {
                        skill.hitEffectPrefab = skill.areaEffectPrefab;
                    }
                }
                else if (areaSkill)
                {
                    if (skill.areaEffectPrefab == null)
                    {
                        skill.areaEffectPrefab = defaultBossSkillAreaEffectPrefab;
                    }

                    if (skill.hitEffectPrefab == null)
                    {
                        skill.hitEffectPrefab = defaultBossSkillHitEffectPrefab != null ? defaultBossSkillHitEffectPrefab : skill.areaEffectPrefab;
                    }
                }
                else if (skill.hitEffectPrefab == null)
                {
                    skill.hitEffectPrefab = defaultBossSkillHitEffectPrefab;
                }
            }
        }

        private void ApplyBasicAttackResources(MonsterDefinition definition, MonsterCombatTuningEntry entry)
        {
            if (definition == null || entry == null)
            {
                return;
            }

            if (definition.attackBehavior == null)
            {
                definition.attackBehavior = new AttackBehavior();
            }

            if (entry.basicAttackProjectilePrefab != null)
            {
                definition.attackBehavior.projectilePrefabOverride = entry.basicAttackProjectilePrefab;
            }

            if (entry.basicAttackMuzzleEffectPrefab != null)
            {
                definition.attackBehavior.muzzleEffectPrefab = entry.basicAttackMuzzleEffectPrefab;
            }

            if (entry.basicAttackHitEffectPrefab != null)
            {
                definition.attackBehavior.hitEffectPrefab = entry.basicAttackHitEffectPrefab;
            }
        }

        private void ApplySkillResourceOverrides(MonsterDefinition definition, MonsterCombatTuningEntry entry)
        {
            if (definition == null || definition.skills == null || entry == null)
            {
                return;
            }

            ApplySkillResourceOverride(
                definition.skills,
                0,
                entry.skill01ProjectilePrefab,
                entry.skill01MuzzleEffectPrefab,
                entry.skill01HitEffectPrefab,
                entry.skill01AreaEffectPrefab);
            ApplySkillResourceOverride(
                definition.skills,
                1,
                entry.skill02ProjectilePrefab,
                entry.skill02MuzzleEffectPrefab,
                entry.skill02HitEffectPrefab,
                entry.skill02AreaEffectPrefab);
            ApplySkillResourceOverride(
                definition.skills,
                2,
                entry.skill03ProjectilePrefab,
                entry.skill03MuzzleEffectPrefab,
                entry.skill03HitEffectPrefab,
                entry.skill03AreaEffectPrefab);
        }

        private void ApplySkillResourceOverride(
            List<SkillDefinition> skills,
            int index,
            GameObject projectilePrefab,
            GameObject muzzleEffectPrefab,
            GameObject hitEffectPrefab,
            GameObject areaEffectPrefab)
        {
            if (index < 0 || index >= skills.Count)
            {
                return;
            }

            SkillDefinition skill = skills[index];
            if (skill == null)
            {
                return;
            }

            if (projectilePrefab != null)
            {
                skill.projectilePrefab = projectilePrefab;
            }

            if (muzzleEffectPrefab != null)
            {
                skill.muzzleEffectPrefab = muzzleEffectPrefab;
            }

            if (hitEffectPrefab != null)
            {
                skill.hitEffectPrefab = hitEffectPrefab;
            }

            if (areaEffectPrefab != null)
            {
                skill.areaEffectPrefab = areaEffectPrefab;
            }
        }

        private bool TryGetOrderedEntry(string definitionId, out MonsterCombatTuningEntry entry)
        {
            entry = null;
            if (!TryParseIndex(definitionId, out int index))
            {
                return false;
            }

            List<MonsterCombatTuningEntry> ordered = entries.FindAll(candidate => candidate != null);
            if (index < 0 || index >= ordered.Count)
            {
                return false;
            }

            entry = ordered[index];
            return true;
        }

        private bool TryParseIndex(string definitionId, out int index)
        {
            index = -1;
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                return false;
            }

            string[] parts = definitionId.Split('_');
            if (parts.Length == 0)
            {
                return false;
            }

            if (!int.TryParse(parts[parts.Length - 1], out int parsed))
            {
                return false;
            }

            index = parsed - 1;
            return index >= 0;
        }
    }

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
        [Range(0f, 1f)] public float splashDamageRatio;
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
