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
        public GameObject summonedDefenderPrefab;
        public GameObject defaultMonsterPrefab;
        public GameObject monsterDeathEffectPrefab;
        public GameObject defenderDeathEffectPrefab;
        public GameObject projectilePrefab;
        public GameObject defaultMuzzleEffectPrefab;
        public GameObject defaultHitEffectPrefab;
        public GameObject defaultAreaEffectPrefab;
        public GameObject spawnPortalPrefab;
        public GameObject goalPrefab;
        public GameObject centerCrystalPrefab;
        public GameObject flankTowerPrefab;
        public GameObject skyAccentPrefab;

        [Header("UI")]
        public UiSkinResources uiSkin;
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

        [Header("Runtime Rendering")]
        [Tooltip("OFF가 기본입니다. 유닛별 런타임 색상 틴트는 개성을 주지만 MaterialPropertyBlock 때문에 GPU Instancing/SRP Batcher 효율이 떨어질 수 있습니다.")]
        public bool usePerInstanceUnitTint;
        [Tooltip("SRP Batcher 우선이 기본입니다. 같은 Mesh와 같은 Material을 대량으로 찍는 특수 몬스터에만 켜세요.")]
        public bool enableRuntimeGpuInstancing;
        [Tooltip("Keeps runtime defender and monster renderers from casting real-time shadows. Prefabs should also keep Cast Shadows off.")]
        public bool forceRuntimeUnitCastShadowsOff = true;

        [Header("Character Overrides")]
        public List<CharacterPresentationOverride> characterOverrides = new List<CharacterPresentationOverride>();
        public bool useRandomCharacterPrefabFallback = true;

        [Header("Monster Overrides")]
        public List<MonsterPresentationOverride> monsterOverrides = new List<MonsterPresentationOverride>();

        public void ApplyToCharacter(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            CharacterPresentationOverride entry = characterOverrides.Find(candidate => candidate != null && candidate.characterId == definition.id);
            if (entry == null)
            {
                if (useRandomCharacterPrefabFallback && characterOverrides.Count == 0 && TryGetRandomCharacterOverride(out CharacterPresentationOverride randomEntry))
                {
                    entry = randomEntry;
                }
                else
                {
                    return;
                }
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
            if (entry == null && TryGetOrderedMonsterOverride(definition, out MonsterPresentationOverride orderedEntry))
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

        public int GetHighestConfiguredCharacterIndex()
        {
            int highestIndex = 0;
            for (int i = 0; i < characterOverrides.Count; i++)
            {
                CharacterPresentationOverride entry = characterOverrides[i];
                if (entry == null || entry.prefab == null || !TryParseIndex(entry.characterId, out int zeroBasedIndex))
                {
                    continue;
                }

                highestIndex = Mathf.Max(highestIndex, zeroBasedIndex + 1);
            }

            return highestIndex;
        }

        private bool TryGetRandomCharacterOverride(out CharacterPresentationOverride entry)
        {
            entry = null;
            List<CharacterPresentationOverride> candidates = characterOverrides.FindAll(candidate => candidate != null && candidate.prefab != null);
            if (candidates.Count == 0)
            {
                return false;
            }

            entry = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return true;
        }

        public bool HasMonsterRosterEntries(MonsterThreatLevel threatLevel)
        {
            return monsterOverrides.Exists(candidate =>
                candidate != null &&
                candidate.useAsRosterEntry &&
                candidate.prefab != null &&
                candidate.threatLevel == threatLevel);
        }

        public List<MonsterPresentationOverride> GetMonsterRosterEntries(MonsterThreatLevel threatLevel)
        {
            return monsterOverrides.FindAll(candidate =>
                candidate != null &&
                candidate.useAsRosterEntry &&
                candidate.prefab != null &&
                candidate.threatLevel == threatLevel);
        }

        private bool TryGetOrderedMonsterOverride(MonsterDefinition definition, out MonsterPresentationOverride entry)
        {
            entry = null;
            if (definition == null || !TryParseIndex(definition.id, out int index))
            {
                return false;
            }

            List<MonsterPresentationOverride> ordered = monsterOverrides.FindAll(candidate =>
                candidate != null &&
                candidate.prefab != null &&
                candidate.threatLevel == definition.threatLevel);
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
        [Range(0f, 0.35f)] public float variantStatBonusPerTier = 0.08f;

        [Header("Presentation")]
        public GameObject prefab;
        public bool overrideColor;
        public Color accentColor = Color.white;
    }
}
