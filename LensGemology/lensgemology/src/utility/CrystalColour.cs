using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace LensGemology
{
    static class CrystalColour
    {
        private static Dictionary<string, int> colourDict = new Dictionary<string, int>();
        private static Dictionary<string, byte[]> lightDict = new Dictionary<string, byte[]>();

        public static void InitColours()
        {
            if(colourDict.Count == 0)
            {
                colourDict.Add("alum", ColorFromRgba(197, 179, 143, 100));
                colourDict.Add("anthracite", ColorFromRgba(243, 235, 255, 100));
                colourDict.Add("bismuthinite", ColorFromRgba(227, 255, 160, 100));
                colourDict.Add("bituminouscoal", ColorFromRgba(153, 153, 153, 100));
                colourDict.Add("borax", ColorFromRgba(214, 214, 212, 100));
                colourDict.Add("cassiterite", ColorFromRgba(255, 185, 91, 100));
                colourDict.Add("chromite", ColorFromRgba(255, 245, 255, 100));
                colourDict.Add("cinnabar", ColorFromRgba(255, 69, 88, 100));
                colourDict.Add("corundum", ColorFromRgba(255, 75, 139, 100));
                colourDict.Add("diamond", ColorFromRgba(192, 248, 255, 100));
                colourDict.Add("emerald", ColorFromRgba(5, 255, 125, 100));
                colourDict.Add("flint", ColorFromRgba(255, 242, 194, 100));
                colourDict.Add("fluorite", ColorFromRgba(255, 230, 122, 100));
                colourDict.Add("galena", ColorFromRgba(144, 185, 204, 100));
                colourDict.Add("galena_nativesilver", ColorFromRgba(94, 119, 128, 100));
                colourDict.Add("graphite", ColorFromRgba(178, 123, 97, 100));
                colourDict.Add("hematite", ColorFromRgba(203, 113, 102, 100));
                colourDict.Add("ilmenite", ColorFromRgba(184, 151, 141, 100));
                colourDict.Add("kernite", ColorFromRgba(255, 220, 182, 100));
                colourDict.Add("lapislazuli", ColorFromRgba(54, 130, 255, 100));
                colourDict.Add("lignite", ColorFromRgba(132, 73, 49, 100));
                colourDict.Add("limonite", ColorFromRgba(255, 130, 13, 100));
                colourDict.Add("magnetite", ColorFromRgba(176, 178, 183, 100));
                colourDict.Add("malachite", ColorFromRgba(55, 255, 166, 100));
                colourDict.Add("nativecopper", ColorFromRgba(255, 134, 90, 100));
                colourDict.Add("olivine", ColorFromRgba(191, 209, 114, 100));
                colourDict.Add("olivine_peridot", ColorFromRgba(191, 209, 114, 100));
                colourDict.Add("pentlandite", ColorFromRgba(183, 145, 50, 100));
                colourDict.Add("phosphorite", ColorFromRgba(255, 211, 181, 100));
                colourDict.Add("quartz", ColorFromRgba(255, 255, 255, 100));
                colourDict.Add("quartz_nativegold", ColorFromRgba(255, 231, 133, 100));
                colourDict.Add("quartz_nativesilver", ColorFromRgba(206, 209, 203, 100));
                colourDict.Add("rhodochrosite", ColorFromRgba(255, 179, 171, 100));
                colourDict.Add("sphalerite", ColorFromRgba(255, 240, 218, 100));
                colourDict.Add("sulfur", ColorFromRgba(255, 217, 79, 100));
                colourDict.Add("sylvite", ColorFromRgba(255, 110, 52, 100));
                colourDict.Add("uranium", ColorFromRgba(173, 183, 59, 100));
                colourDict.Add("wolframite", ColorFromRgba(153, 169, 255, 100));
            }
        }
        public static void InitLights()
        {
            if(lightDict.Count == 0)
            {
                lightDict.Add("alum", new byte[] { 10, 3, 4 });
                lightDict.Add("anthracite", new byte[] { 47, 1, 4 });
                lightDict.Add("bismuthinite", new byte[] { 14, 3, 4 });
                lightDict.Add("bituminouscoal", new byte[] { 0, 1, 4 });
                lightDict.Add("borax", new byte[] { 11, 1, 4 });
                lightDict.Add("cassiterite", new byte[] { 6, 3, 4 });
                lightDict.Add("chromite", new byte[] { 52, 1, 4 });
                lightDict.Add("cinnabar", new byte[] { 62, 3, 4 });
                lightDict.Add("corundum", new byte[] { 59, 3, 4 });
                lightDict.Add("diamond", new byte[] { 33, 3, 4 });
                lightDict.Add("emerald", new byte[] { 26, 3, 4 });
                lightDict.Add("flint", new byte[] { 8, 5, 4 });
                lightDict.Add("fluorite", new byte[] { 9, 3, 4 });
                lightDict.Add("galena", new byte[] { 35, 3, 4 });
                lightDict.Add("galena_nativesilver", new byte[] { 35, 1, 4 });
                lightDict.Add("graphite", new byte[] { 3, 5, 4 });
                lightDict.Add("hematite", new byte[] { 1, 3, 4 });
                lightDict.Add("ilmenite", new byte[] { 2, 5, 4 });
                lightDict.Add("kernite", new byte[] { 6, 3, 4 });
                lightDict.Add("lapislazuli", new byte[] { 38, 3, 4 });
                lightDict.Add("lignite", new byte[] { 3, 5, 4 });
                lightDict.Add("limonite", new byte[] { 5, 3, 4 });
                lightDict.Add("magnetite", new byte[] { 40, 1, 4 });
                lightDict.Add("malachite", new byte[] { 27, 3, 4 });
                lightDict.Add("nativecopper", new byte[] { 3, 3, 4 });
                lightDict.Add("olivine", new byte[] { 12, 3, 4 });
                lightDict.Add("olivine_peridot", new byte[] { 15, 3, 4 });
                lightDict.Add("pentlandite", new byte[] { 7, 5, 4 });
                lightDict.Add("phosphorite", new byte[] { 4, 3, 4 });
                lightDict.Add("quartz", new byte[] { 0, 0, 4 });
                lightDict.Add("quartz_nativegold", new byte[] { 8, 3, 4 });
                lightDict.Add("quartz_nativesilver", new byte[] { 34, 1, 4 });
                lightDict.Add("rhodochrosite", new byte[] { 1, 3, 4 });
                lightDict.Add("sphalerite", new byte[] { 6, 5, 4 });
                lightDict.Add("sulfur", new byte[] { 8, 3, 4 });
                lightDict.Add("sylvite", new byte[] { 3, 3, 4 });
                lightDict.Add("uranium", new byte[] { 11, 3, 4 });
                lightDict.Add("wolframite", new byte[] { 40, 3, 4 });
            }
        }
        public static void Destroy()
        {
            colourDict.Clear();
            lightDict.Clear();
        }
        public static int GetColour(string colour)
        {
            int colourInt;

            if (colourDict.TryGetValue(colour, out colourInt))
                return colourInt;
            else
                return 0;
        }
        public static byte[] GetLight(string colour)
        {
            byte[] lightHSV;

            if (lightDict.TryGetValue(colour, out lightHSV))
                return lightHSV;
            else 
                return new byte[] { 0, 0, 0 };
        }
        public static int ColorFromRgba(int r, int g, int b, int a)
        {
            return (a << 24) | (r << 16) | (g << 8) | (b);
        }
    }
}
