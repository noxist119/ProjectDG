using UnityEngine;

namespace DefenseGame
{
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

		public UltimateRecipeOption(string recipeName, string displayName, string materialSummary, string resultSummary, Color accentColor, bool isReady = true, int progress = 0, int required = 0, string missingSummary = "")
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
		}
	}
}
