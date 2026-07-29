using UnityEngine;

namespace DefenseGame;

public struct UltimateRecipeOption(string recipeName, string displayName, string materialSummary, string resultSummary, Color accentColor, bool isReady = true, int progress = 0, int required = 0, string missingSummary = "")
{
	public readonly string recipeName = recipeName;

	public readonly string displayName = displayName;

	public readonly string materialSummary = materialSummary;

	public readonly string resultSummary = resultSummary;

	public readonly Color accentColor = accentColor;

	public readonly bool isReady = isReady;

	public readonly int progress = progress;

	public readonly int required = required;

	public readonly string missingSummary = missingSummary ?? string.Empty;
}
