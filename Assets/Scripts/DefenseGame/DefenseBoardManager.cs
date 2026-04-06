using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class DefenseBoardManager : MonoBehaviour
    {
        [SerializeField] private List<BoardSlot> slots = new List<BoardSlot>();
        [SerializeField] private DefenderUnit fallbackUnitPrefab;

        public IReadOnlyList<BoardSlot> Slots => slots;
        public int EmptySlotCount => slots.Count(slot => slot != null && slot.IsEmpty);

        private void Awake()
        {
            if (slots.Count == 0)
            {
                slots = GetComponentsInChildren<BoardSlot>(true).ToList();
            }
        }

        public void Configure(List<BoardSlot> newSlots, DefenderUnit fallbackPrefab)
        {
            slots = newSlots;
            fallbackUnitPrefab = fallbackPrefab;
        }

        public bool TrySpawnUnit(CharacterDefinition definition, DefenderUnit prefabOverride = null)
        {
            BoardSlot emptySlot = slots.FirstOrDefault(slot => slot != null && slot.IsEmpty);
            if (emptySlot == null || definition == null)
            {
                return false;
            }

            GameObject sourcePrefab = definition.prefab != null
                ? definition.prefab
                : prefabOverride != null ? prefabOverride.gameObject : fallbackUnitPrefab != null ? fallbackUnitPrefab.gameObject : null;

            if (sourcePrefab == null)
            {
                Debug.LogError("No DefenderUnit prefab assigned.");
                return false;
            }

            GameObject spawnedObject = Instantiate(sourcePrefab, emptySlot.UnitAnchor.position, Quaternion.identity);
            DefenderUnit unit = spawnedObject.GetComponent<DefenderUnit>();
            if (unit == null)
            {
                unit = spawnedObject.AddComponent<DefenderUnit>();
                unit.AdoptRuntimeTemplate(prefabOverride != null ? prefabOverride : fallbackUnitPrefab);
            }

            unit.gameObject.SetActive(true);
            unit.Initialize(definition);
            emptySlot.AssignUnit(unit);
            return true;
        }

        public bool TryMergeUnitsOfGrade(CharacterGrade grade, CharacterDatabase database, out MergeResultInfo mergeResult, DefenderUnit prefabOverride = null)
        {
            mergeResult = default;
            List<DefenderUnit> sameGradeUnits = slots
                .Where(slot => slot != null && !slot.IsEmpty)
                .Select(slot => slot.OccupiedUnit)
                .Where(unit => unit != null && unit.Grade == grade)
                .Take(3)
                .ToList();

            if (sameGradeUnits.Count < 3 || grade == CharacterGrade.Mythic)
            {
                return false;
            }

            BoardSlot spawnSlot = sameGradeUnits[0].CurrentSlot;
            for (int i = 0; i < sameGradeUnits.Count; i++)
            {
                sameGradeUnits[i].RemoveFromBoard();
                Destroy(sameGradeUnits[i].gameObject);
            }

            CharacterGrade nextGrade = (CharacterGrade)((int)grade + 1);
            CharacterDefinition mergedCharacter = database.GetRandomCharacterByGrade(nextGrade);
            if (mergedCharacter == null || spawnSlot == null)
            {
                return false;
            }

            GameObject sourcePrefab = mergedCharacter.prefab != null
                ? mergedCharacter.prefab
                : prefabOverride != null ? prefabOverride.gameObject : fallbackUnitPrefab != null ? fallbackUnitPrefab.gameObject : null;

            if (sourcePrefab == null)
            {
                Debug.LogError("No DefenderUnit prefab assigned for merge result.");
                return false;
            }

            GameObject spawnedObject = Instantiate(sourcePrefab, spawnSlot.UnitAnchor.position, Quaternion.identity);
            DefenderUnit unit = spawnedObject.GetComponent<DefenderUnit>();
            if (unit == null)
            {
                unit = spawnedObject.AddComponent<DefenderUnit>();
                unit.AdoptRuntimeTemplate(prefabOverride != null ? prefabOverride : fallbackUnitPrefab);
            }

            unit.gameObject.SetActive(true);
            unit.Initialize(mergedCharacter);
            spawnSlot.AssignUnit(unit);
            mergeResult = new MergeResultInfo
            {
                sourceGrade = grade,
                resultGrade = nextGrade,
                resultCharacterName = mergedCharacter.displayName,
                resultColor = mergedCharacter.accentColor
            };
            return true;
        }

        public int CountUnitsOfGrade(CharacterGrade grade)
        {
            return slots.Count(slot => slot != null && !slot.IsEmpty && slot.OccupiedUnit.Grade == grade);
        }

        public DefenderUnit[] GetAliveDefenders()
        {
            return slots.Where(slot => slot != null && !slot.IsEmpty)
                .Select(slot => slot.OccupiedUnit)
                .Where(unit => unit != null)
                .ToArray();
        }
    }
}
