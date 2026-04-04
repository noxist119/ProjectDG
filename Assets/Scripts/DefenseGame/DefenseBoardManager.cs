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

        public bool TrySpawnUnit(CharacterDefinition definition, DefenderUnit prefabOverride = null)
        {
            BoardSlot emptySlot = slots.FirstOrDefault(slot => slot != null && slot.IsEmpty);
            if (emptySlot == null || definition == null)
            {
                return false;
            }

            DefenderUnit prefabToUse = definition.prefab != null
                ? definition.prefab.GetComponent<DefenderUnit>()
                : prefabOverride != null ? prefabOverride : fallbackUnitPrefab;

            if (prefabToUse == null)
            {
                Debug.LogError("No DefenderUnit prefab assigned.");
                return false;
            }

            DefenderUnit unit = Instantiate(prefabToUse, emptySlot.UnitAnchor.position, Quaternion.identity);
            unit.Initialize(definition);
            emptySlot.AssignUnit(unit);
            return true;
        }

        public bool TryMergeUnitsOfGrade(CharacterGrade grade, CharacterDatabase database, DefenderUnit prefabOverride = null)
        {
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

            DefenderUnit prefabToUse = mergedCharacter.prefab != null
                ? mergedCharacter.prefab.GetComponent<DefenderUnit>()
                : prefabOverride != null ? prefabOverride : fallbackUnitPrefab;

            if (prefabToUse == null)
            {
                Debug.LogError("No DefenderUnit prefab assigned for merge result.");
                return false;
            }

            DefenderUnit unit = Instantiate(prefabToUse, spawnSlot.UnitAnchor.position, Quaternion.identity);
            unit.Initialize(mergedCharacter);
            spawnSlot.AssignUnit(unit);
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

