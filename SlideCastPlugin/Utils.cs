using System;
using System.Linq;

namespace SlideCastPlugin
{
    public class Colour
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        private float A { get; set; }

        public int Hue { get; set; }
        public int Saturation { get; set; }
        public int Brightness { get; set; }

        public Colour(float rc, float gc, float bc)
        {
            R = rc * 255f;
            G = gc * 255f;
            B = bc * 255f;
            A = 255f;

            Hue = (int)GetHue();
            Saturation = (int)GetSaturation();
            Brightness = (int)GetBrightness();
        }

        public Colour(float rc, float gc, float bc, float ac)
        {
            R = rc * 255f;
            G = gc * 255f;
            B = bc * 255f;
            A = ac * 255f;

            Hue = (int)GetHue();
            Saturation = (int)GetSaturation();
            Brightness = (int)GetBrightness();
        }

        private float GetHue()
        {
            if (R == G && G == B)
                return 0;
            var r = R / 255f;
            var g = G / 255f;
            var b = B / 255f;
            float hue;
            var min = Numbers.Min(r, g, b);
            var max = Numbers.Max(r, g, b);
            var delta = max - min;
            if (r.AlmostEquals(max))
                hue = (g - b) / delta; // between yellow & magenta
            else if (g.AlmostEquals(max))
                hue = 2 + (b - r) / delta; // between cyan & yellow
            else
                hue = 4 + (r - g) / delta; // between magenta & cyan
            hue *= 60; // degrees
            if (hue < 0)
                hue += 360;
            return hue * 182.04f;
        }

        private float GetSaturation()
        {
            var r = R / 255f;
            var g = G / 255f;
            var b = B / 255f;
            var min = Numbers.Min(r, g, b);
            var max = Numbers.Max(r, g, b);
            if (max.AlmostEquals(min))
                return 0;
            return ((max.AlmostEquals(0f)) ? 0f : 1f - (1f * min / max)) * 255;
        }

        private float GetBrightness()
        {
            var r = R / 255f;
            var g = G / 255f;
            var b = B / 255f;
            return Numbers.Max(r, g, b) * 255;
        }
    }

    public static class FloatExtension
    {
        public static bool AlmostEquals(this float a, float b, double precision = float.Epsilon)
        {
            return Math.Abs(a - b) <= precision;
        }
    }

    public static class Numbers
    {
        public static float Max(params float[] numbers)
        {
            return numbers.Max();
        }

        public static float Min(params float[] numbers)
        {
            return numbers.Min();
        }
    }
}