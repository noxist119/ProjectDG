using UnityEngine;

namespace DefenseGame;

public static class RuntimeFontProvider
{
	private static Font cachedFont;

	public static Font GetDefaultFont()
	{
		if ((Object)(object)cachedFont == (Object)null)
		{
			cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		}
		return cachedFont;
	}
}
