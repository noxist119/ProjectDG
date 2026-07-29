using UnityEngine;

namespace DefenseGame
{
	public static class SkillDefinitionUtility
	{
		public static SkillCategory ResolveCategory(SkillDefinition skill)
		{
			if (skill == null)
			{
				return SkillCategory.Auto;
			}
			if (skill.category != SkillCategory.Auto)
			{
				return skill.category;
			}
			return ResolveCategory(skill.effectType);
		}

		public static SkillDeliveryType ResolveDeliveryType(SkillDefinition skill)
		{
			if (skill == null)
			{
				return SkillDeliveryType.Instant;
			}
			if (skill.deliveryType != SkillDeliveryType.Auto)
			{
				return skill.deliveryType;
			}
			switch (skill.effectType)
			{
			case SkillEffectType.GroundAreaDamage:
				return SkillDeliveryType.GroundArea;
			case SkillEffectType.DamageGroundField:
			case SkillEffectType.FixedPoison:
			case SkillEffectType.RandomMultiShot:
				return SkillDeliveryType.Projectile;
			case SkillEffectType.HealSelf:
			case SkillEffectType.AttackSpeedBoost:
			case SkillEffectType.CriticalBoost:
			case SkillEffectType.MoveSpeedBoost:
			case SkillEffectType.ManaSurge:
			case SkillEffectType.ShieldAlly:
			case SkillEffectType.BossFortify:
			case SkillEffectType.GoldDrain:
			case SkillEffectType.ManaBurn:
			case SkillEffectType.MonsterRally:
			case SkillEffectType.DefenseBuff:
			case SkillEffectType.Transform:
			case SkillEffectType.Taunt:
			case SkillEffectType.ManaRestoreAdjacent:
			case SkillEffectType.HealLowestAllies:
			case SkillEffectType.ThornsAura:
			case SkillEffectType.DeathPoisonField:
			case SkillEffectType.AllyAttackSpeedBoost:
			case SkillEffectType.FrontKnockbackGuard:
			case SkillEffectType.AttackPowerReduction:
			case SkillEffectType.DamageReflect:
				return SkillDeliveryType.Instant;
			default:
				return SkillDeliveryType.Melee;
			}
		}

		public static SkillCategory ResolveCategory(SkillEffectType effectType)
		{
			switch (effectType)
			{
			case SkillEffectType.DirectDamage:
			case SkillEffectType.Execute:
			case SkillEffectType.ShieldBreak:
			case SkillEffectType.PercentHealthDamage:
			case SkillEffectType.FrontKnockbackGuard:
				return SkillCategory.Damage;
			case SkillEffectType.AreaDamage:
			case SkillEffectType.MultiShot:
			case SkillEffectType.LinePierceDamage:
			case SkillEffectType.StoneLine:
			case SkillEffectType.RandomMultiShot:
				return SkillCategory.AreaAttack;
			case SkillEffectType.AttackSpeedBoost:
			case SkillEffectType.CriticalBoost:
			case SkillEffectType.MoveSpeedBoost:
			case SkillEffectType.MonsterRally:
			case SkillEffectType.AllyAttackSpeedBoost:
				return SkillCategory.Buff;
			case SkillEffectType.LifeSteal:
			case SkillEffectType.HealthDrainPercent:
				return SkillCategory.LifeSteal;
			case SkillEffectType.Slow:
			case SkillEffectType.DamageSlow:
				return SkillCategory.Slow;
			case SkillEffectType.GroundAreaDamage:
			case SkillEffectType.DamageGroundField:
				return SkillCategory.GroundDamage;
			case SkillEffectType.Poison:
			case SkillEffectType.FixedPoison:
			case SkillEffectType.DeathPoisonField:
				return SkillCategory.Poison;
			case SkillEffectType.ShieldAlly:
			case SkillEffectType.BossFortify:
			case SkillEffectType.DefenseBuff:
			case SkillEffectType.Taunt:
			case SkillEffectType.ThornsAura:
			case SkillEffectType.DamageReflect:
				return SkillCategory.Defense;
			case SkillEffectType.Stun:
			case SkillEffectType.MassStun:
			case SkillEffectType.DamageStun:
				return SkillCategory.Stun;
			case SkillEffectType.HealSelf:
			case SkillEffectType.HealLowestAllies:
				return SkillCategory.Heal;
			case SkillEffectType.ManaSurge:
			case SkillEffectType.ManaBurn:
			case SkillEffectType.ManaRestoreAdjacent:
				return SkillCategory.ManaCharge;
			case SkillEffectType.Transform:
				return SkillCategory.Transform;
			case SkillEffectType.SummonRush:
				return SkillCategory.Summon;
			case SkillEffectType.DeathPact:
			case SkillEffectType.GoldDrain:
			case SkillEffectType.AttackPowerReduction:
				return SkillCategory.BossSpecial;
			default:
				return SkillCategory.Auto;
			}
		}

		public static string GetCategoryDisplayName(SkillCategory category)
		{
			return category switch
			{
				SkillCategory.Damage => "데미지형", 
				SkillCategory.AreaAttack => "광역공격형", 
				SkillCategory.Buff => "버프형", 
				SkillCategory.LifeSteal => "흡혈형", 
				SkillCategory.Slow => "슬로우형", 
				SkillCategory.GroundDamage => "광역바닥데미지형", 
				SkillCategory.Poison => "중독형", 
				SkillCategory.Defense => "방어형", 
				SkillCategory.Stun => "스턴형", 
				SkillCategory.Heal => "힐", 
				SkillCategory.ManaCharge => "마나 충전형", 
				SkillCategory.Transform => "변신형", 
				SkillCategory.Summon => "소환형", 
				SkillCategory.BossSpecial => "보스 특수형", 
				_ => "자동", 
			};
		}

		public static string BuildDisplayDescription(SkillDefinition skill)
		{
			if (skill == null)
			{
				return string.Empty;
			}
			return skill.effectType switch
			{
				SkillEffectType.HealthDrainPercent => "적의 현재 체력 " + FormatPercent(skill.power) + "를 흡수해 내 체력으로 전환합니다.", 
				SkillEffectType.DirectDamage => "공격력 " + FormatPercent(skill.power) + "의 피해를 줍니다.", 
				SkillEffectType.LinePierceDamage => FormatMeters(skill.radius) + " 전방의 적들에게 공격력 " + FormatPercent(skill.power) + "의 관통 피해를 줍니다.", 
				SkillEffectType.ManaRestoreAdjacent => "양옆 아군의 마나를 최대 마나의 " + FormatPercent(skill.power) + "만큼 회복합니다.", 
				SkillEffectType.DamageStun => "공격력 " + FormatPercent(skill.power) + "의 피해를 주고 " + FormatSeconds(skill.duration) + " 동안 스턴시킵니다.", 
				SkillEffectType.PercentHealthDamage => "적 현재 체력의 " + FormatPercent(skill.power) + "만큼 피해를 줍니다.", 
				SkillEffectType.AreaDamage => FormatMeters(skill.radius) + " 반경의 적에게 공격력 " + FormatPercent(skill.power) + "의 피해를 줍니다.", 
				SkillEffectType.HealLowestAllies => "체력이 낮은 아군 " + Mathf.Max(1, skill.hitCount) + "명에게 최대 체력의 " + FormatPercent(skill.power) + "를 회복시킵니다.", 
				SkillEffectType.DamageSlow => "공격력 " + FormatPercent(skill.power) + "의 피해를 주고 " + FormatSeconds(skill.duration) + " 동안 이속과 공속을 " + FormatPercent(skill.secondaryPower) + " 낮춥니다.", 
				SkillEffectType.StoneLine => FormatMeters(skill.radius) + " 전방의 적을 석상으로 만들어 " + FormatSeconds(skill.duration) + " 동안 행동 불가 상태로 만듭니다.", 
				SkillEffectType.DamageGroundField => "구체로 공격력 " + FormatPercent(skill.power) + "의 피해를 주고 " + FormatMeters(skill.radius) + " 반경에 " + FormatSeconds(skill.duration) + " 동안 용암지역을 생성합니다. 지역 안 몬스터는 초당 " + FormatNumber(skill.secondaryPower) + " 피해를 받습니다.", 
				SkillEffectType.AttackSpeedBoost => FormatSeconds(skill.duration) + " 동안 공격속도를 " + FormatPercent(skill.power) + " 올립니다.", 
				SkillEffectType.AllyAttackSpeedBoost => "사거리 안에 있는 아군의 공격속도를 " + FormatSeconds(skill.duration) + " 동안 " + FormatPercent(skill.power) + " 증가시킵니다.", 
				SkillEffectType.FixedPoison => "독화살을 발사해 " + FormatSeconds(skill.duration) + " 동안 적에게 초당 " + FormatNumber(skill.power) + " 피해를 줍니다.", 
				SkillEffectType.DefenseBuff => "최대 체력의 " + FormatPercent(skill.power) + "만큼 방어막을 생성합니다.", 
				SkillEffectType.SummonRush => BuildSummonDescription(skill), 
				SkillEffectType.ThornsAura => "쏜즈 오오라를 생성해 받은 피해의 " + FormatPercent(skill.power) + "를 공격자에게 돌려줍니다.", 
				SkillEffectType.Taunt => FormatMeters(skill.radius) + " 반경의 적을 " + FormatSeconds(skill.duration) + " 동안 도발하고 받는 피해를 " + FormatPercent(skill.secondaryPower) + " 감소시킵니다.", 
				SkillEffectType.DeathPoisonField => "죽을 때 전방에 " + FormatSeconds(skill.duration) + " 동안 독극물 지대를 만들어 초당 " + FormatNumber(skill.power) + " 피해를 줍니다.", 
				SkillEffectType.HealSelf => "최대 체력의 " + FormatPercent(skill.power) + "를 회복합니다.", 
				SkillEffectType.MultiShot => "가까운 적 " + Mathf.Max(1, skill.hitCount) + "명에게 공격력 " + FormatPercent(skill.power) + "의 피해를 줍니다.", 
				SkillEffectType.FrontKnockbackGuard => "전방의 적에게 공격력 " + FormatPercent(skill.power) + "의 피해를 주고 " + FormatMeters(skill.radius) + " 밀쳐냅니다. 사용할 때마다 받는 피해가 " + FormatPercent(skill.secondaryPower) + " 감소합니다.", 
				SkillEffectType.RandomMultiShot => "무작위 적에게 공격력 " + FormatPercent(skill.power) + "의 탄환을 " + Mathf.Max(1, skill.hitCount) + "발 발사합니다. 같은 적을 다시 노릴 수 있습니다.", 
				SkillEffectType.AttackPowerReduction => "가장 가까운 유닛의 공격력을 " + FormatPercent(skill.power) + "만큼 " + FormatSeconds(skill.duration) + " 동안 감소시킵니다.", 
				SkillEffectType.DamageReflect => FormatSeconds(skill.duration) + " 동안 받은 피해의 " + FormatPercent(skill.power) + "를 공격자에게 돌려줍니다.", 
				SkillEffectType.Execute => "적에게 공격력 " + FormatPercent(skill.power) + "의 피해를 주며, 체력이 낮은 적에게 더 강합니다.", 
				SkillEffectType.Slow => "적의 이동속도를 " + FormatSeconds(skill.duration) + " 동안 " + FormatPercent(skill.power) + " 낮춥니다.", 
				SkillEffectType.Stun => "적을 " + FormatSeconds(skill.duration) + " 동안 스턴시킵니다.", 
				SkillEffectType.ShieldAlly => "체력이 낮은 아군에게 최대 체력의 " + FormatPercent(skill.power) + "만큼 방어막을 부여합니다.", 
				SkillEffectType.LifeSteal => "공격력 " + FormatPercent(skill.power) + "의 피해를 주고 피해량의 일부를 체력으로 회복합니다.", 
				SkillEffectType.GroundAreaDamage => FormatMeters(skill.radius) + " 반경에 " + FormatSeconds(skill.duration) + " 동안 피해 장판을 생성합니다.", 
				SkillEffectType.Poison => "적을 " + FormatSeconds(skill.duration) + " 동안 중독시켜 지속 피해를 줍니다.", 
				SkillEffectType.Transform => FormatSeconds(skill.duration) + " 동안 강화된 전투 상태가 됩니다.", 
				_ => string.IsNullOrWhiteSpace(skill.description) ? "스킬 효과 정보가 없습니다." : skill.description, 
			};
		}

		public static string BuildGrowthDisplayText(SkillDefinition skill)
		{
			if (skill == null || skill.growthTargets == SkillGrowthTarget.None || skill.growthStepRatio <= 0f)
			{
				return string.Empty;
			}
			return "성장 대상: " + BuildGrowthTargetText(skill) + " / 레벨당 +" + FormatPercent(skill.growthStepRatio) + " 선형 증가";
		}

		private static string BuildSummonDescription(SkillDefinition skill)
		{
			string health = FormatPercent(skill.power);
			string attack = FormatPercent((skill.secondaryPower > 0f) ? skill.secondaryPower : skill.power);
			if (health == attack)
			{
				return "현재 체력과 공격력의 " + health + " 수준인 미니미를 소환해 적과 싸우게 합니다.";
			}
			return "현재 체력의 " + health + ", 공격력의 " + attack + " 수준인 미니미를 소환해 적과 싸우게 합니다.";
		}

		private static string BuildGrowthTargetText(SkillDefinition skill)
		{
			string result = string.Empty;
			AppendGrowthTarget(ref result, skill, SkillGrowthTarget.Power);
			AppendGrowthTarget(ref result, skill, SkillGrowthTarget.SecondaryPower);
			AppendGrowthTarget(ref result, skill, SkillGrowthTarget.Duration);
			AppendGrowthTarget(ref result, skill, SkillGrowthTarget.Radius);
			AppendGrowthTarget(ref result, skill, SkillGrowthTarget.HitCount);
			return string.IsNullOrEmpty(result) ? "주요 수치" : result;
		}

		private static void AppendGrowthTarget(ref string result, SkillDefinition skill, SkillGrowthTarget target)
		{
			if (skill == null || (skill.growthTargets & target) == 0)
			{
				return;
			}
			string label = GetGrowthTargetLabel(skill, target);
			if (!string.IsNullOrEmpty(label))
			{
				if (!string.IsNullOrEmpty(result))
				{
					result += ", ";
				}
				result += label;
			}
		}

		private static string GetGrowthTargetLabel(SkillDefinition skill, SkillGrowthTarget target)
		{
			return target switch
			{
				SkillGrowthTarget.Power => GetPowerGrowthLabel(skill.effectType), 
				SkillGrowthTarget.SecondaryPower => GetSecondaryPowerGrowthLabel(skill.effectType), 
				SkillGrowthTarget.Duration => "지속 시간", 
				SkillGrowthTarget.Radius => "범위", 
				SkillGrowthTarget.HitCount => "대상 수", 
				_ => string.Empty, 
			};
		}

		private static string GetPowerGrowthLabel(SkillEffectType effectType)
		{
			switch (effectType)
			{
			case SkillEffectType.HealthDrainPercent:
				return "흡수량";
			case SkillEffectType.ManaRestoreAdjacent:
				return "마나 회복량";
			case SkillEffectType.HealSelf:
			case SkillEffectType.HealLowestAllies:
				return "체력 회복량";
			case SkillEffectType.FixedPoison:
			case SkillEffectType.DeathPoisonField:
				return "초당 피해";
			case SkillEffectType.AllyAttackSpeedBoost:
				return "공격속도";
			case SkillEffectType.ShieldAlly:
			case SkillEffectType.DefenseBuff:
				return "방어막량";
			case SkillEffectType.SummonRush:
				return "소환체 체력";
			case SkillEffectType.ThornsAura:
				return "반사량";
			default:
				return "피해량";
			}
		}

		private static string GetSecondaryPowerGrowthLabel(SkillEffectType effectType)
		{
			return effectType switch
			{
				SkillEffectType.DamageSlow => "감속률", 
				SkillEffectType.DamageGroundField => "장판 초당 피해", 
				SkillEffectType.SummonRush => "소환체 공격력", 
				SkillEffectType.Taunt => "피해 감소율", 
				SkillEffectType.FrontKnockbackGuard => "스킬당 방어력", 
				_ => "보조 수치", 
			};
		}

		private static string FormatPercent(float value)
		{
			return FormatNumber(value * 100f) + "%";
		}

		private static string FormatSeconds(float value)
		{
			return FormatNumber(value) + "초";
		}

		private static string FormatMeters(float value)
		{
			return FormatNumber(value) + "m";
		}

		private static string FormatNumber(float value)
		{
			float rounded = Mathf.Round(value);
			if (Mathf.Abs(value - rounded) < 0.01f)
			{
				return Mathf.RoundToInt(value).ToString();
			}
			return value.ToString("0.#");
		}
	}
}
