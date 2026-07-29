using UnityEngine;

namespace DefenseGame;

public class BoardSlot : MonoBehaviour
{
	[SerializeField]
	private Transform unitAnchor;

	[SerializeField]
	private BoardTileModifierType tileModifierType = BoardTileModifierType.None;

	[SerializeField]
	private bool locked;

	private GameObject tileMarker;

	private GameObject lockMarker;

	public DefenderUnit OccupiedUnit { get; private set; }

	public bool IsEmpty => (Object)(object)OccupiedUnit == (Object)null;

	public bool IsLocked => locked;

	public bool IsAvailable => !locked;

	public Transform UnitAnchor => ((Object)(object)unitAnchor != (Object)null) ? unitAnchor : ((Component)this).transform;

	public BoardTileModifierType TileModifierType => tileModifierType;

	public void AssignUnit(DefenderUnit unit)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (!locked && !((Object)(object)unit == (Object)null))
		{
			OccupiedUnit = unit;
			((Component)unit).transform.SetParent(UnitAnchor);
			((Component)unit).transform.localPosition = Vector3.zero;
			((Component)unit).transform.localRotation = Quaternion.identity;
			unit.SetSlot(this);
			if (unit.Definition != null)
			{
				ApplyTileBonus(unit, showFeedback: true);
			}
		}
	}

	public void Clear()
	{
		if ((Object)(object)OccupiedUnit != (Object)null)
		{
			OccupiedUnit.ClearBoardTileBonuses();
		}
		OccupiedUnit = null;
	}

	public void SetLocked(bool value, string label)
	{
		locked = value;
		if (!locked && !((Component)this).gameObject.activeSelf)
		{
			((Component)this).gameObject.SetActive(true);
		}
		EnsureLockMarker();
		if ((Object)(object)lockMarker != (Object)null)
		{
			lockMarker.SetActive(false);
			TextMesh componentInChildren = lockMarker.GetComponentInChildren<TextMesh>(true);
			if ((Object)(object)componentInChildren != (Object)null)
			{
				componentInChildren.text = string.Empty;
				((Component)componentInChildren).gameObject.SetActive(false);
			}
		}
		if (locked && ((Component)this).gameObject.activeSelf)
		{
			((Component)this).gameObject.SetActive(false);
		}
	}

	public void PlayUnlockFeedback()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if (!((Component)this).gameObject.activeSelf)
		{
			((Component)this).gameObject.SetActive(true);
		}
		Vector3 position = UnitAnchor.position;
		RuntimeCombatFeedback.ShowGroundPulse(position, new Color(0.42f, 0.95f, 1f, 1f), 0.96f, 0.68f, 0.12f);
		RuntimeGameFeel.PlayJackpotPulse(position, new Color(0.46f, 1f, 0.82f, 1f), 1.3f, 0.09f, 0.24f, 0.22f, 0.08f, 3);
	}

	public void SetTileModifier(BoardTileModifierType type, Color color, string label)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		tileModifierType = type;
		EnsureTileMarker();
		if ((Object)(object)tileMarker != (Object)null)
		{
			bool flag = type != BoardTileModifierType.None;
			tileMarker.SetActive(flag);
			if (flag)
			{
				((Object)tileMarker).name = "Tile_" + type;
				Renderer component = tileMarker.GetComponent<Renderer>();
				if ((Object)(object)component != (Object)null)
				{
					component.sharedMaterial = CreateMarkerMaterial(color);
				}
			}
		}
		if ((Object)(object)OccupiedUnit != (Object)null)
		{
			ApplyTileBonus(OccupiedUnit, showFeedback: false);
		}
	}

	public void RefreshTileBonus(bool showFeedback = false)
	{
		if ((Object)(object)OccupiedUnit != (Object)null)
		{
			ApplyTileBonus(OccupiedUnit, showFeedback);
		}
	}

	public void ClearTileModifier()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		SetTileModifier(BoardTileModifierType.None, Color.clear, null);
	}

	private void EnsureTileMarker()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)tileMarker != (Object)null))
		{
			tileMarker = GameObject.CreatePrimitive((PrimitiveType)2);
			((Object)tileMarker).name = "Tile_None";
			tileMarker.transform.SetParent(((Component)this).transform, false);
			tileMarker.transform.localPosition = new Vector3(0f, 0.74f, 0f);
			tileMarker.transform.localRotation = Quaternion.identity;
			tileMarker.transform.localScale = new Vector3(0.56f, 0.018f, 0.56f);
			Collider component = tileMarker.GetComponent<Collider>();
			if ((Object)(object)component != (Object)null)
			{
				Object.Destroy((Object)(object)component);
			}
			tileMarker.SetActive(false);
		}
	}

	private void EnsureLockMarker()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)lockMarker != (Object)null))
		{
			lockMarker = new GameObject("SlotLockMarker");
			lockMarker.transform.SetParent(((Component)this).transform, false);
			lockMarker.transform.localPosition = new Vector3(0f, 0.93f, 0f);
			lockMarker.transform.localRotation = Quaternion.identity;
			GameObject val = GameObject.CreatePrimitive((PrimitiveType)3);
			((Object)val).name = "LockPlate";
			val.transform.SetParent(lockMarker.transform, false);
			val.transform.localPosition = Vector3.zero;
			val.transform.localRotation = Quaternion.identity;
			val.transform.localScale = new Vector3(0.92f, 0.035f, 0.78f);
			Renderer component = val.GetComponent<Renderer>();
			if ((Object)(object)component != (Object)null)
			{
				component.sharedMaterial = CreateMarkerMaterial(new Color(0.03f, 0.04f, 0.09f, 0.92f));
			}
			Collider component2 = val.GetComponent<Collider>();
			if ((Object)(object)component2 != (Object)null)
			{
				Object.Destroy((Object)(object)component2);
			}
			lockMarker.SetActive(false);
		}
	}

	private Material CreateMarkerMaterial(Color color)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		Material val = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
		val.color = color;
		if (val.HasProperty("_BaseColor"))
		{
			val.SetColor("_BaseColor", color);
		}
		return val;
	}

	private void ApplyTileBonus(DefenderUnit unit, bool showFeedback)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)unit == (Object)null))
		{
			float attackPowerRatio = 0f;
			float attackSpeedRatio = 0f;
			float manaRegenRate = 0f;
			float maxHealthRatio = 0f;
			float skillPowerRatio = 0f;
			float bossDamageRatio = 0f;
			float attackRangeFlat = 0f;
			float damageReductionRatio = 0f;
			float lifeStealRatio = 0f;
			string text = null;
			Color white = Color.white;
			switch (tileModifierType)
			{
			case BoardTileModifierType.AttackSpeed:
				attackSpeedRatio = 0.22f;
				text = "가속 강화";
				((Color)(ref white))._002Ector(0.3f, 1f, 0.86f);
				break;
			case BoardTileModifierType.Mana:
				manaRegenRate = 0.035f;
				text = "마나 강화";
				((Color)(ref white))._002Ector(0.34f, 0.72f, 1f);
				break;
			case BoardTileModifierType.Guard:
				maxHealthRatio = 0.18f;
				damageReductionRatio = 0.08f;
				text = "방어 강화";
				((Color)(ref white))._002Ector(0.4f, 1f, 0.58f);
				break;
			case BoardTileModifierType.Range:
				attackRangeFlat = 0.75f;
				text = "사거리 강화";
				((Color)(ref white))._002Ector(0.72f, 0.9f, 1f);
				break;
			case BoardTileModifierType.Overload:
				attackPowerRatio = 0.2f;
				attackSpeedRatio = 0.08f;
				text = "과부하 강화";
				((Color)(ref white))._002Ector(1f, 0.48f, 0.3f);
				break;
			case BoardTileModifierType.BossHunter:
				bossDamageRatio = 0.28f;
				text = "보스 강화";
				((Color)(ref white))._002Ector(1f, 0.7f, 0.22f);
				break;
			case BoardTileModifierType.Skill:
				skillPowerRatio = 0.2f;
				manaRegenRate = 0.018f;
				text = "기술 강화";
				((Color)(ref white))._002Ector(0.82f, 0.48f, 1f);
				break;
			case BoardTileModifierType.AttackPower:
				attackPowerRatio = 0.18f;
				text = "공격 강화";
				((Color)(ref white))._002Ector(1f, 0.46f, 0.32f);
				break;
			case BoardTileModifierType.LifeSteal:
				lifeStealRatio = 0.1f;
				maxHealthRatio = 0.08f;
				text = "피흡 강화";
				((Color)(ref white))._002Ector(1f, 0.3f, 0.52f);
				break;
			case BoardTileModifierType.AllStats:
				attackPowerRatio = 0.1f;
				attackSpeedRatio = 0.1f;
				manaRegenRate = 0.018f;
				maxHealthRatio = 0.1f;
				skillPowerRatio = 0.1f;
				damageReductionRatio = 0.04f;
				text = "전능 강화";
				((Color)(ref white))._002Ector(1f, 0.86f, 0.32f);
				break;
			}
			unit.SetBoardTileBonuses(attackPowerRatio, attackSpeedRatio, manaRegenRate, maxHealthRatio, skillPowerRatio, bossDamageRatio, attackRangeFlat, damageReductionRatio, lifeStealRatio, showFeedback ? text : null, white);
		}
	}
}
