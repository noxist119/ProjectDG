using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    [CreateAssetMenu(fileName = "MonsterCombatTuningConfig", menuName = "Defense Game/Monster Combat Tuning")]
    public class MonsterCombatTuningConfig : ScriptableObject
    {
        public List<MonsterCombatTuningEntry> entries = new List<MonsterCombatTuningEntry>();

        public void ApplyToMonster(MonsterDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            MonsterCombatTuningEntry entry = entries.Find(candidate => candidate != null && candidate.monsterId == definition.id);
            if (entry == null && TryGetOrderedEntry(definition.id, out MonsterCombatTuningEntry orderedEntry))
            {
                entry = orderedEntry;
            }

            if (entry == null)
            {
                return;
            }

            if (entry.overrideAttackRange)
            {
                definition.attackBehavior.useCustomAttackRange = true;
                definition.attackBehavior.customAttackRange = entry.attackRange;
            }

            if (entry.overrideMoveSpeed)
            {
                definition.stats.moveSpeed = entry.moveSpeed;
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
        public bool overrideAttackRange;
        public float attackRange = 2f;
        public bool overrideMoveSpeed;
        public float moveSpeed = 1.5f;
        public bool overrideSplash;
        public float splashRadius;
        [Range(0f, 1f)] public float splashDamageRatio;
        public bool overridePierce;
        public int additionalPierceCount;
    }
}
