using UnityEngine;

namespace DefenseGame
{
    public static class RuntimeFontProvider
    {
        private static Font cachedFont;

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
