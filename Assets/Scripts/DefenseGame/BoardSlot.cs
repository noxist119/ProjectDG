using UnityEngine;

namespace DefenseGame
{
    public class BoardSlot : MonoBehaviour
    {
        [SerializeField] private Transform unitAnchor;
        [SerializeField] private BoardTileModifierType tileModifierType = BoardTileModifierType.None;
        [SerializeField] private bool locked;

        public DefenderUnit OccupiedUnit { get; private set; }
        public bool IsEmpty => OccupiedUnit == null;
        public bool IsLocked => locked;
        public bool IsAvailable => !locked;
        public Transform UnitAnchor => unitAnchor != null ? unitAnchor : transform;
        public BoardTileModifierType TileModifierType => tileModifierType;

        private GameObject tileMarker;
        private GameObject lockMarker;

        public void AssignUnit(DefenderUnit unit)
        {
            if (locked || unit == null)
            {
                return;
            }

            OccupiedUnit = unit;
            unit.transform.SetParent(UnitAnchor);
            unit.transform.localPosition = Vector3.zero;
            unit.transform.localRotation = Quaternion.identity;
            unit.SetSlot(this);
            if (unit.Definition != null)
            {
                ApplyTileBonus(unit, true);
            }
        }

        public void Clear()
        {
            if (OccupiedUnit != null)
            {
                OccupiedUnit.ClearBoardTileBonuses();
            }

            OccupiedUnit = null;
        }

        public void SetLocked(bool value, string label)
        {
            locked = value;
            if (!locked && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            EnsureLockMarker();
            if (lockMarker != null)
            {
                lockMarker.SetActive(false);
                TextMesh text = lockMarker.GetComponentInChildren<TextMesh>(true);
                if (text != null)
                {
                    text.text = string.Empty;
                    text.gameObject.SetActive(false);
                }
            }

            if (locked && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
        public void PlayUnlockFeedback()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            Vector3 position = UnitAnchor.position;
            RuntimeCombatFeedback.ShowGroundPulse(position, new Color(0.42f, 0.95f, 1f, 1f), 0.96f, 0.68f, 0.12f);
            RuntimeGameFeel.PlayJackpotPulse(position, new Color(0.46f, 1f, 0.82f, 1f), 1.30f, 0.09f, 0.24f, 0.22f, 0.08f, 3);
        }
        public void SetTileModifier(BoardTileModifierType type, Color color, string label)
        {
            tileModifierType = type;
            EnsureTileMarker();
            if (tileMarker != null)
            {
                bool visible = type != BoardTileModifierType.None;
                tileMarker.SetActive(visible);
                if (visible)
                {
                    tileMarker.name = "Tile_" + type;
                    Renderer markerRenderer = tileMarker.GetComponent<Renderer>();
                    if (markerRenderer != null)
                    {
                        markerRenderer.sharedMaterial = CreateMarkerMaterial(color);
                    }
                }
            }

            if (OccupiedUnit != null)
            {
                ApplyTileBonus(OccupiedUnit, false);
            }
        }

        public void RefreshTileBonus(bool showFeedback = false)
        {
            if (OccupiedUnit != null)
            {
                ApplyTileBonus(OccupiedUnit, showFeedback);
            }
        }

        public void ClearTileModifier()
        {
            SetTileModifier(BoardTileModifierType.None, Color.clear, null);
        }

        private void EnsureTileMarker()
        {
            if (tileMarker != null)
            {
                return;
            }

            tileMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tileMarker.name = "Tile_None";
            tileMarker.transform.SetParent(transform, false);
            tileMarker.transform.localPosition = new Vector3(0f, 0.74f, 0f);
            tileMarker.transform.localRotation = Quaternion.identity;
            tileMarker.transform.localScale = new Vector3(0.56f, 0.018f, 0.56f);

            Collider markerCollider = tileMarker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            tileMarker.SetActive(false);
        }


        private void EnsureLockMarker()
        {
            if (lockMarker != null)
            {
                return;
            }

            lockMarker = new GameObject("SlotLockMarker");
            lockMarker.transform.SetParent(transform, false);
            lockMarker.transform.localPosition = new Vector3(0f, 0.93f, 0f);
            lockMarker.transform.localRotation = Quaternion.identity;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "LockPlate";
            plate.transform.SetParent(lockMarker.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localRotation = Quaternion.identity;
            plate.transform.localScale = new Vector3(0.92f, 0.035f, 0.78f);
            Renderer plateRenderer = plate.GetComponent<Renderer>();
            if (plateRenderer != null)
            {
                plateRenderer.sharedMaterial = CreateMarkerMaterial(new Color(0.03f, 0.04f, 0.09f, 0.92f));
            }

            Collider plateCollider = plate.GetComponent<Collider>();
            if (plateCollider != null)
            {
                Destroy(plateCollider);
            }

            // Unlock timing is announced through HUD, so locked slots stay textless on the board.

            lockMarker.SetActive(false);
        }
        private Material CreateMarkerMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private void ApplyTileBonus(DefenderUnit unit, bool showFeedback)
        {
            if (unit == null)
            {
                return;
            }

            float attackPower = 0f;
            float attackSpeed = 0f;
            float manaRegen = 0f;
            float maxHealth = 0f;
            float skillPower = 0f;
            float bossDamage = 0f;
            float range = 0f;
            float damageReduction = 0f;
            float lifeSteal = 0f;
            string label = null;
            Color color = Color.white;

            switch (tileModifierType)
            {
                case BoardTileModifierType.AttackSpeed:
                    attackSpeed = 0.22f;
                    label = "\uAC00\uC18D \uAC15\uD654";
                    color = new Color(0.30f, 1f, 0.86f);
                    break;
                case BoardTileModifierType.Mana:
                    manaRegen = 0.035f;
                    label = "\uB9C8\uB098 \uAC15\uD654";
                    color = new Color(0.34f, 0.72f, 1f);
                    break;
                case BoardTileModifierType.Guard:
                    maxHealth = 0.18f;
                    damageReduction = 0.08f;
                    label = "\uBC29\uC5B4 \uAC15\uD654";
                    color = new Color(0.40f, 1f, 0.58f);
                    break;
                case BoardTileModifierType.Range:
                    range = 0.75f;
                    label = "\uC0AC\uAC70\uB9AC \uAC15\uD654";
                    color = new Color(0.72f, 0.90f, 1f);
                    break;
                case BoardTileModifierType.Overload:
                    attackPower = 0.20f;
                    attackSpeed = 0.08f;
                    label = "\uACFC\uBD80\uD558 \uAC15\uD654";
                    color = new Color(1f, 0.48f, 0.30f);
                    break;
                case BoardTileModifierType.BossHunter:
                    bossDamage = 0.28f;
                    label = "\uBCF4\uC2A4 \uAC15\uD654";
                    color = new Color(1f, 0.70f, 0.22f);
                    break;
                case BoardTileModifierType.Skill:
                    skillPower = 0.20f;
                    manaRegen = 0.018f;
                    label = "\uAE30\uC220 \uAC15\uD654";
                    color = new Color(0.82f, 0.48f, 1f);
                    break;
                case BoardTileModifierType.AttackPower:
                    attackPower = 0.18f;
                    label = "\uACF5\uACA9 \uAC15\uD654";
                    color = new Color(1f, 0.46f, 0.32f);
                    break;
                case BoardTileModifierType.LifeSteal:
                    lifeSteal = 0.10f;
                    maxHealth = 0.08f;
                    label = "\uD53C\uD761 \uAC15\uD654";
                    color = new Color(1f, 0.30f, 0.52f);
                    break;
                case BoardTileModifierType.AllStats:
                    attackPower = 0.10f;
                    attackSpeed = 0.10f;
                    manaRegen = 0.018f;
                    maxHealth = 0.10f;
                    skillPower = 0.10f;
                    damageReduction = 0.04f;
                    label = "\uC804\uB2A5 \uAC15\uD654";
                    color = new Color(1f, 0.86f, 0.32f);
                    break;
            }

            unit.SetBoardTileBonuses(
                attackPower,
                attackSpeed,
                manaRegen,
                maxHealth,
                skillPower,
                bossDamage,
                range,
                damageReduction,
                lifeSteal,
                showFeedback ? label : null,
                color);
        }
    }
}

