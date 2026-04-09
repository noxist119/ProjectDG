using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    [CreateAssetMenu(fileName = "CharacterCombatTuningConfig", menuName = "Defense Game/Character Combat Tuning")]
    public class CharacterCombatTuningConfig : ScriptableObject
    {
        public List<CharacterCombatTuningEntry> entries = new List<CharacterCombatTuningEntry>();

        public void ApplyToCharacter(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            CharacterCombatTuningEntry entry = entries.Find(candidate => candidate != null && candidate.characterId == definition.id);
            if (entry == null && TryGetOrderedEntry(definition.id, out CharacterCombatTuningEntry orderedEntry))
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

        private bool TryGetOrderedEntry(string definitionId, out CharacterCombatTuningEntry entry)
        {
            entry = null;
            if (!TryParseIndex(definitionId, out int index))
            {
                return false;
            }

            List<CharacterCombatTuningEntry> ordered = entries.FindAll(candidate => candidate != null);
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
    public class CharacterCombatTuningEntry
    {
        public string characterId;
        public bool overrideAttackRange;
        public float attackRange = 6f;
        public bool overrideSplash;
        public float splashRadius;
        [Range(0f, 1f)] public float splashDamageRatio;
        public bool overridePierce;
        public int additionalPierceCount;
    }
}
