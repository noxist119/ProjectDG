using System;
using UnityEngine;

namespace DefenseGame
{
	[Serializable]
	public class AugmentDefinition
	{
		public string id;

		public string title;

		[TextArea]
		public string description;

		public AugmentEffectType effectType;

		public AugmentStyle style;

		public HeroAugmentTier heroTier;

		public string requiredHeroId;

		public float value;

		public float secondaryValue;

		public float duration;

		public Color accentColor = Color.white;
	}
}
