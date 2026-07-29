using UnityEngine;

namespace DefenseGame
{
	public class AnimationEventProxy : MonoBehaviour
	{
		private UnitAnimationDriver cachedDriver;

		private AnimationMaterialOverrideController cachedMaterialController;

		public void Hit()
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void Hit(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void Hit(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void Impact()
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void Impact(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void Impact(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void Damage()
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void Damage(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void Damage(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Auto);
		}

		public void AttackHit()
		{
			NotifyImpact(AnimationImpactType.AttackHit);
		}

		public void AttackHit(string eventKey)
		{
			NotifyImpact(AnimationImpactType.AttackHit);
		}

		public void AttackHit(int eventKey)
		{
			NotifyImpact(AnimationImpactType.AttackHit);
		}

		public void AttackImpact()
		{
			NotifyImpact(AnimationImpactType.AttackHit);
		}

		public void AttackImpact(string eventKey)
		{
			NotifyImpact(AnimationImpactType.AttackHit);
		}

		public void AttackImpact(int eventKey)
		{
			NotifyImpact(AnimationImpactType.AttackHit);
		}

		public void FireProjectile()
		{
			NotifyImpact(AnimationImpactType.FireProjectile);
		}

		public void FireProjectile(string eventKey)
		{
			NotifyImpact(AnimationImpactType.FireProjectile);
		}

		public void FireProjectile(int eventKey)
		{
			NotifyImpact(AnimationImpactType.FireProjectile);
		}

		public void Shoot()
		{
			NotifyImpact(AnimationImpactType.FireProjectile);
		}

		public void Shoot(string eventKey)
		{
			NotifyImpact(AnimationImpactType.FireProjectile);
		}

		public void Shoot(int eventKey)
		{
			NotifyImpact(AnimationImpactType.FireProjectile);
		}

		public void SkillHit()
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillHit(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillHit(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillImpact()
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillImpact(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillImpact(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillFire()
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillFire(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillFire(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillApply()
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillApply(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SkillApply(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void CastImpact()
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void CastImpact(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void CastImpact(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SpawnArea()
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SpawnArea(string eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void SpawnArea(int eventKey)
		{
			NotifyImpact(AnimationImpactType.Skill);
		}

		public void PlayEffect()
		{
		}

		public void PlayEffect(string effectName)
		{
		}

		public void PlayEffect(int effectIndex)
		{
		}

		public void PlayEffectKey()
		{
		}

		public void PlayEffectKey(string effectName)
		{
		}

		public void PlayEffectKey(int effectIndex)
		{
		}

		public void PlayEffectTile()
		{
		}

		public void PlayEffectTile(string effectName)
		{
		}

		public void PlayEffectTile(int effectIndex)
		{
		}

		public void SpawnProp()
		{
		}

		public void SpawnProp(string propName)
		{
		}

		public void SpawnProp(int propIndex)
		{
		}

		public void DespawnProp()
		{
		}

		public void DespawnProp(string propName)
		{
		}

		public void DespawnProp(int propIndex)
		{
		}

		public void PlaySound()
		{
			RuntimeAudioUtility.PlayAttack();
		}

		public void PlaySound(string soundName)
		{
			RuntimeAudioUtility.PlayNamed(soundName);
		}

		public void PlaySound(int soundIndex)
		{
			RuntimeAudioUtility.PlayIndexed(soundIndex);
		}

		public void OverrideMaterial(string materialName)
		{
			ResolveMaterialController()?.OverrideMaterial(materialName);
		}

		public void ResetMaterial(string materialName)
		{
			ResolveMaterialController()?.ResetMaterial(materialName);
		}

		private AnimationMaterialOverrideController ResolveMaterialController()
		{
			UnitAnimationDriver driver = ResolveDriver();
			object result;
			if (!(driver == null))
			{
				if (!(cachedMaterialController != null))
				{
					AnimationMaterialOverrideController obj = driver.GetComponent<AnimationMaterialOverrideController>() ?? driver.gameObject.AddComponent<AnimationMaterialOverrideController>();
					AnimationMaterialOverrideController animationMaterialOverrideController = obj;
					cachedMaterialController = obj;
					result = animationMaterialOverrideController;
				}
				else
				{
					result = cachedMaterialController;
				}
			}
			else
			{
				result = null;
			}
			return (AnimationMaterialOverrideController)result;
		}

		private void NotifyImpact(AnimationImpactType impactType)
		{
			ResolveDriver()?.NotifyAnimationImpact(impactType);
		}

		private UnitAnimationDriver ResolveDriver()
		{
			if (cachedDriver == null)
			{
				cachedDriver = GetComponentInParent<UnitAnimationDriver>();
			}
			return cachedDriver;
		}
	}
}
