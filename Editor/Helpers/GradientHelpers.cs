using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    public static class GradientHelpers
    {
        public static int GetHashCode(Gradient gradient)
        {
            if (gradient == null)
            {
                return 0;
            }

            // Gradient's hashcode doesn't seem to respond to value changes, and
            // the colorKeys property's hashcode changes each time its retrieved.
            // So we build our own hashcode to detect the changes we care about.
            var hashCode = new HashCode();

            foreach (var key in gradient.colorKeys)
            {
                hashCode.Add(key.color.grayscale);
                hashCode.Add(key.time);
            }

            return hashCode.ToHashCode();
        }


        public static Gradient GetDefaultGradient()
        {
            var gradient = new Gradient();

            gradient.SetColorKeys(new[]
            {
                new GradientColorKey(Color.black, 0),
                new GradientColorKey(Color.gray, 0.5f),
                new GradientColorKey(Color.white, 1)
            });

            return gradient;
        }
    }
}
