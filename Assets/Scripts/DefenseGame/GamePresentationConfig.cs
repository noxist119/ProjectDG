using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    [CreateAssetMenu(fileName = "DefenseGamePresentationConfig", menuName = "Defense Game/Presentation Config")]
    public class GamePresentationConfig : ScriptableObject
    {
        [Header("Prefab Overrides")]
        public GameObject backgroundPrefab;
        public GameObject defaultDefenderPrefab;
        public GameObject defaultMonsterPrefab;
        public GameObject projectilePrefab;
        public GameObject spawnPortalPrefab;
        public GameObject goalPrefab;
        public GameObject centerCrystalPrefab;
        public GameObject flankTowerPrefab;
        public GameObject skyAccentPrefab;

        [Header("UI")]
        public Font uiFont;
        public Color hudTextColor = Color.white;
        public Color buttonColor = new Color(0.16f, 0.19f, 0.26f, 0.92f);
        public Color buttonTextColor = Color.white;
        [TextArea] public string hintText = "Space Round | S Summon | 1-4 Merge | C Add Heroes | M Add Monsters";

        [Header("Stage Colors")]
        public Color groundColor = new Color(0.08f, 0.11f, 0.14f);
        public Color boardStripColor = new Color(0.12f, 0.18f, 0.24f);
        public Color enemyRunwayColor = new Color(0.18f, 0.10f, 0.11f);
        public Color midBridgeColor = new Color(0.25f, 0.29f, 0.36f);
        public Color northWallColor = new Color(0.17f, 0.14f, 0.22f);
        public Color southWallColor = new Color(0.13f, 0.19f, 0.24f);
        public Color sideWallColor = new Color(0.12f, 0.14f, 0.18f);
        public Color gateColor = new Color(0.24f, 0.54f, 0.72f);
        public Color gateCoreColor = new Color(0.38f, 0.89f, 1f);
        public Color crystalColor = new Color(0.30f, 0.95f, 0.86f);

        [Header("Palettes")]
        public Color[] slotColors;
        public Color[] laneColors;

        [Header("Character Overrides")]
        public List<CharacterPresentationOverride> characterOverrides = new List<CharacterPresentationOverride>();

        [Header("Monster Overrides")]
        public List<MonsterPresentationOverride> monsterOverrides = new List<MonsterPresentationOverride>();

        public void ApplyToCharacter(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            CharacterPresentationOverride entry = characterOverrides.Find(candidate => candidate != null && candidate.characterId == definition.id);
            if (entry == null && TryGetOrderedCharacterOverride(definition.id, out CharacterPresentationOverride orderedEntry))
            {
                entry = orderedEntry;
            }
            if (entry == null)
            {
                return;
            }

            if (entry.prefab != null)
            {
                definition.prefab = entry.prefab;
            }

            if (entry.overrideColor)
            {
                definition.accentColor = entry.accentColor;
            }
        }

        public void ApplyToMonster(MonsterDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            MonsterPresentationOverride entry = monsterOverrides.Find(candidate => candidate != null && candidate.monsterId == definition.id);
            if (entry == null && TryGetOrderedMonsterOverride(definition.id, out MonsterPresentationOverride orderedEntry))
            {
                entry = orderedEntry;
            }
            if (entry == null)
            {
                return;
            }

            if (entry.prefab != null)
            {
                definition.prefab = entry.prefab;
            }

            if (entry.overrideColor)
            {
                definition.accentColor = entry.accentColor;
            }
        }

        private bool TryGetOrderedCharacterOverride(string definitionId, out CharacterPresentationOverride entry)
        {
            entry = null;
            if (!TryParseIndex(definitionId, out int index))
            {
                return false;
            }

            List<CharacterPresentationOverride> ordered = characterOverrides.FindAll(candidate => candidate != null && candidate.prefab != null);
            if (index < 0 || index >= ordered.Count)
            {
                return false;
            }

            entry = ordered[index];
            return true;
        }

        private bool TryGetOrderedMonsterOverride(string definitionId, out MonsterPresentationOverride entry)
        {
            entry = null;
            if (!TryParseIndex(definitionId, out int index))
            {
                return false;
            }

            List<MonsterPresentationOverride> ordered = monsterOverrides.FindAll(candidate => candidate != null && candidate.prefab != null);
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
    public class CharacterPresentationOverride
    {
        public string characterId;
        public GameObject prefab;
        public bool overrideColor;
        public Color accentColor = Color.white;
    }

    [Serializable]
    public class MonsterPresentationOverride
    {
        public string monsterId;
        public GameObject prefab;
        public bool overrideColor;
        public Color accentColor = Color.white;
    }
}
