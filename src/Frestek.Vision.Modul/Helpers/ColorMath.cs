using System;
using System.Drawing;

namespace Frestek.Vision.Client.Helpers
{
    public static class ColorMath
    {
        // Kiszámítja két szín távolságát (minél kisebb, annál hasonlóbb)
        public static double GetDistance(Color c1, Color c2)
        {
            double r = Math.Pow(c1.R - c2.R, 2);
            double g = Math.Pow(c1.G - c2.G, 2);
            double b = Math.Pow(c1.B - c2.B, 2);

            return Math.Sqrt(r + g + b);
        }

        // Hex kód konvertálása Color objektummá
        public static Color FromHex(string hex)
        {
            return ColorTranslator.FromHtml(hex);
        }
    }
}