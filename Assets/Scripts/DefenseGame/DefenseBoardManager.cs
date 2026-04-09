using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenseGame
{
    public class DefenseBoardManager : MonoBehaviour
    {
        [SerializeField] private List<BoardSlot> slots = new List<BoardSlot>();
        [SerializeField] private DefenderUnit fallbackUnitPrefab;
        [SerializeField] private float dragHeight = 1.4f;

        private DefenderUnit draggedUnit;
        private BoardSlot draggedOriginSlot;
        private Collider[] draggedColliders;
        private Plane dragPlane;
        private Vector3 dragOffset;

        public IReadOnlyList<BoardSlot> Slots => slots;
        public int EmptySlotCount => slots.Count(slot => slot != null && slot.IsEmpty);

        private void Awake()
        {
            if (slots.Count == 0)
            {
                slots = GetComponentsInChildren<BoardSlot>(true).ToList();
            }

            dragPlane = new Plane(Vector3.up, new Vector3(0f, dragHeight, 0f));
        }

        private void Update()
        {
            HandleDragging();
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
            emptySlot.AssignUnit(unit);
            unit.Initialize(definition);
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
            spawnSlot.AssignUnit(unit);
            unit.Initialize(mergedCharacter);
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

        public bool TryMoveUnit(DefenderUnit unit, BoardSlot targetSlot)
        {
            if (unit == null || targetSlot == null)
            {
                return false;
            }

            BoardSlot sourceSlot = unit.CurrentSlot;
            if (sourceSlot == null)
            {
                return false;
            }

            if (sourceSlot == targetSlot)
            {
                if (targetSlot.IsEmpty || targetSlot.OccupiedUnit == unit)
                {
                    targetSlot.AssignUnit(unit);
                    return true;
                }
            }

            DefenderUnit targetUnit = targetSlot.OccupiedUnit;
            if (targetUnit == unit)
            {
                targetSlot.AssignUnit(unit);
                return true;
            }

            sourceSlot.Clear();
            if (targetUnit != null)
            {
                targetSlot.Clear();
            }

            targetSlot.AssignUnit(unit);

            if (targetUnit != null)
            {
                sourceSlot.AssignUnit(targetUnit);
            }

            return true;
        }

        private void HandleDragging()
        {
            if (Camera.main == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryBeginDrag();
            }

            if (draggedUnit != null)
            {
                UpdateDragPosition();

                if (Input.GetMouseButtonUp(0))
                {
                    EndDrag();
                }
            }
        }

        private void TryBeginDrag()
        {
            if (!TryGetPointerHit(out RaycastHit hit))
            {
                return;
            }

            DefenderUnit unit = hit.collider.GetComponentInParent<DefenderUnit>();
            if (unit == null)
            {
                return;
            }

            BoardSlot originSlot = unit.CurrentSlot;
            if (originSlot == null)
            {
                return;
            }

            draggedUnit = unit;
            draggedOriginSlot = originSlot;
            draggedColliders = draggedUnit.GetComponentsInChildren<Collider>(true);
            draggedOriginSlot.Clear();
            draggedUnit.transform.SetParent(transform, true);

            for (int i = 0; i < draggedColliders.Length; i++)
            {
                draggedColliders[i].enabled = false;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                dragOffset = draggedUnit.transform.position - ray.GetPoint(enter);
            }
            else
            {
                dragOffset = Vector3.zero;
            }
        }

        private void UpdateDragPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!dragPlane.Raycast(ray, out float enter))
            {
                return;
            }

            Vector3 point = ray.GetPoint(enter) + dragOffset;
            point.y = dragHeight;
            draggedUnit.transform.position = point;
        }

        private void EndDrag()
        {
            BoardSlot targetSlot = FindSlotUnderPointer();

            for (int i = 0; i < draggedColliders.Length; i++)
            {
                if (draggedColliders[i] != null)
                {
                    draggedColliders[i].enabled = true;
                }
            }

            if (targetSlot != null)
            {
                TryMoveUnit(draggedUnit, targetSlot);
            }
            else if (draggedOriginSlot != null)
            {
                draggedOriginSlot.AssignUnit(draggedUnit);
            }

            draggedUnit = null;
            draggedOriginSlot = null;
            draggedColliders = null;
            dragOffset = Vector3.zero;
        }

        private BoardSlot FindSlotUnderPointer()
        {
            if (!TryGetPointerHit(out RaycastHit hit))
            {
                return null;
            }

            return hit.collider.GetComponentInParent<BoardSlot>();
        }

        private bool TryGetPointerHit(out RaycastHit hit)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, 200f);
        }
    }
}
