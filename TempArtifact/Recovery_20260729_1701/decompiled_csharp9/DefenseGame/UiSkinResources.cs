using UnityEngine;

namespace DefenseGame
{
	[CreateAssetMenu(fileName = "DefenseGameUiSkin", menuName = "Defense Game/UI Skin Resources")]
	public class UiSkinResources : ScriptableObject
	{
		[Header("Optional Font Override")]
		public Font fontOverride;

		[Header("Shared Sprites")]
		public Sprite panelSprite;

		public Sprite modalSprite;

		public Sprite buttonSprite;

		public Sprite cardSprite;

		public Sprite accentSprite;

		public Sprite portraitSprite;

		public Sprite iconPlateSprite;

		public Sprite progressBarSprite;

		public Sprite progressFillSprite;

		public Sprite circleSprite;

		[Header("Button Variants")]
		public Sprite primaryButtonSprite;

		public Sprite positiveButtonSprite;

		public Sprite dangerButtonSprite;

		public Sprite warningButtonSprite;

		[Header("Optional Icons")]
		public Sprite coinIconSprite;

		public Sprite gemIconSprite;

		public Sprite closeIconSprite;

		public Sprite checkIconSprite;

		public Sprite plusIconSprite;

		public Sprite infoIconSprite;

		public Sprite deckIconSprite;

		public Sprite battleIconSprite;

		public Sprite heartIconSprite;

		public Sprite missionIconSprite;

		public Sprite heroIconSprite;

		public Sprite shopIconSprite;

		public Sprite augmentIconSprite;

		[Header("Import Hints")]
		public bool preserveAspect;
	}
}
