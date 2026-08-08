using UnityEngine;

namespace DefenseGame
{
	public struct UltimateRecipeMaterialView
	{
		public readonly string characterId;

		public readonly string displayName;

		public readonly CharacterGrade grade;

		public readonly Color accentColor;

		public readonly int ownedCount;

		public readonly int requiredCount;

		public readonly CharacterDefinition definition;

		public bool isReady => ownedCount >= requiredCount;

		public UltimateRecipeMaterialView(string characterId, string displayName, CharacterGrade grade, Color accentColor, int ownedCount, int requiredCount, CharacterDefinition definition)
		{
			this.characterId = characterId ?? string.Empty;
			this.displayName = displayName ?? string.Empty;
			this.grade = grade;
			this.accentColor = accentColor;
			this.ownedCount = Mathf.Max(0, ownedCount);
			this.requiredCount = Mathf.Max(0, requiredCount);
			this.definition = definition;
		}
	}

	public struct UltimateRecipeOption
	{
		public readonly string recipeName;

		public readonly string displayName;

		public readonly string materialSummary;

		public readonly string resultSummary;

		public readonly Color accentColor;

		public readonly bool isReady;

		public readonly int progress;

		public readonly int required;

		public readonly string missingSummary;

		public readonly CharacterDefinition primaryResultDefinition;

		public readonly UltimateRecipeMaterialView[] materials;

		public readonly int missingMaterialCount;

		public readonly int definitionOrder;

		public UltimateRecipeOption(string recipeName, string displayName, string materialSummary, string resultSummary, Color accentColor, bool isReady = true, int progress = 0, int required = 0, string missingSummary = "", CharacterDefinition primaryResultDefinition = null, UltimateRecipeMaterialView[] materials = null, int missingMaterialCount = 0, int definitionOrder = 0)
		{
			this.recipeName = recipeName;
			this.displayName = displayName;
			this.materialSummary = materialSummary;
			this.resultSummary = resultSummary;
			this.accentColor = accentColor;
			this.isReady = isReady;
			this.progress = progress;
			this.required = required;
			this.missingSummary = missingSummary ?? string.Empty;
			this.primaryResultDefinition = primaryResultDefinition;
			this.materials = materials ?? new UltimateRecipeMaterialView[0];
			this.missingMaterialCount = Mathf.Max(0, missingMaterialCount);
			this.definitionOrder = Mathf.Max(0, definitionOrder);
		}
	}
}
