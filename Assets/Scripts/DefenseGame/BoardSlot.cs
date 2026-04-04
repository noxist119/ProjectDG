using UnityEngine;

namespace DefenseGame
{
    public class BoardSlot : MonoBehaviour
    {
        [SerializeField] private Transform unitAnchor;

        public DefenderUnit OccupiedUnit { get; private set; }
        public bool IsEmpty => OccupiedUnit == null;
        public Transform UnitAnchor => unitAnchor != null ? unitAnchor : transform;

        public void AssignUnit(DefenderUnit unit)
        {
            OccupiedUnit = unit;
            unit.transform.SetParent(UnitAnchor);
            unit.transform.localPosition = Vector3.zero;
            unit.transform.localRotation = Quaternion.identity;
            unit.SetSlot(this);
        }

        public void Clear()
        {
            OccupiedUnit = null;
        }
    }
}

