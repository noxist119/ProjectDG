using UnityEngine;

namespace DefenseGame
{
    public static class RuntimeFontProvider
    {
        private static Font cachedFont;
        private static Font cachedCombatFont;

        public static Font GetCombatNumberFont()
        {
            if (cachedCombatFont != null)
            {
                return cachedCombatFont;
            }

            Font[] loadedFonts = Resources.FindObjectsOfTypeAll<Font>();
            for (int i = 0; i < loadedFonts.Length; i++)
            {
                Font font = loadedFonts[i];
                if (font != null && (font.name == "GROBOLD" || font.name == "Baloo2-ExtraBold"))
                {
                    cachedCombatFont = font;
                    return cachedCombatFont;
                }
            }

            return GetDefaultFont();
        }
        public static Font GetDefaultFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return cachedFont;
        }
    }
}
